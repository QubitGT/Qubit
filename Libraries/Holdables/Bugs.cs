/*
 * Qubit Menu  Libraries/Holdables/Bugs.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using GorillaExtensions;
using GorillaLocomotion.Climbing;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Fun;
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Modules.Classes.GunLibrary;
using Manager = Qubit.Modules.Manager;
using Object = UnityEngine.Object;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Holdables
{
    public static class Bugs
    {

        public static Coroutine BugCoroutine;

        public static ThrowableBug _firefly;
        public static ThrowableBug Firefly
        {
            get
            {
                if (_firefly == null)
                    _firefly = GetAllType<ThrowableBug>().Where(bug => bug.gameObject.activeInHierarchy && bug.gameObject.name == "Floating Bug Holdable").ToArray()[0];

                return _firefly;
            }
        }

        public static void ObjectToPointGun(string objectName)
        {
            GunLibrary.Point(NewPointer =>
            {
                ThrowableBug bug = GetBug(objectName);
                if (bug != null)
                    bug.transform.position = NewPointer.transform.position + Vector3.up;
            });
        }

        public static void SetRespawnDistance(string objectName, float respawnDistance = float.MaxValue)
        {
            ThrowableBug bugObject = GetBugObject(objectName);

            bugObject.maxDistanceFromOriginBeforeRespawn = respawnDistance;
            bugObject.maxDistanceFromTargetPlayerBeforeRespawn = respawnDistance;
        }

        public static void PermanentOwnership(string objectName)
        {
            Ownership.enabled = true;

            ThrowableBug bugObject = GetBugObject(objectName);

            if (!Qubit.Core.Networking.InRoom)
            {
                Ownership.blacklistedGuards.Clear();
                return;
            }

            if (!bugObject.IsMyItem())
                return;

            if (bugObject.targetRig != References.LocalRig)
                bugObject.SetTargetRig(References.LocalRig);

            if (!Ownership.blacklistedGuards.Contains(bugObject.worldShareableInstance.guard))
                Ownership.blacklistedGuards.Add(bugObject.worldShareableInstance.guard);
        }
        public static ThrowableBug GetBugObject(string name)
        {
            GameObject bugObject;
            bugObject = name == "Firefly" ? Firefly.gameObject : GetObject(name);
            if (bugObject == null)
                return null;

            ThrowableBug bug = bugObject.GetComponent<ThrowableBug>();
            return bug ?? null;
        }

        public static ThrowableBug GetBug(string name)
        {
            ThrowableBug bug = GetBugObject(name);

            if (bug == null)
                return null;

            GameObject bugObject = bug.gameObject;

            if (!Qubit.Core.Networking.InRoom)
                return bug;

            RequestableOwnershipGuard guard = bug.worldShareableInstance.guard;
            if (guard == null)
                return null;

            if (!bug.IsMyItem())
            {
                if (bug.currentState != TransferrableObject.PositionState.Dropped && bug.currentState != TransferrableObject.PositionState.None)
                    return null;

                References.LocalRig.enabled = true;
                if (Vector3.SqrMagnitude(bugObject.transform.position - References.BodyCollider.transform.position) > 15f)
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = bugObject.transform.position;

                    if (BugCoroutine != null)
                        Coroutines.instance.StopCoroutine(BugCoroutine);

                    BugCoroutine = Coroutines.instance.StartCoroutine(ReturnRig());
                }

                if (Vector3.SqrMagnitude(bugObject.transform.position - ServerPos) > 15f)
                    return null;

                if (Time.time < getOwnershipDelay)
                    return null;

                getOwnershipDelay = Time.time + 0.5f;

                NetworkingState guardState = guard.currentState;
                Action failureAction = null;
                if ((int)guardState < 3)
                    failureAction = () => guard.currentState = guardState;

                switch (guard.currentState)
                {
                    case NetworkingState.IsOwner:
                        return null;
                    case NetworkingState.IsBlindClient:
                        guard.ownershipDenied = (Action)Delegate.Combine(guard.ownershipDenied, failureAction);
                        guard.currentState = NetworkingState.RequestingOwnershipWaitingForSight;
                        return null;
                    case NetworkingState.IsClient:
                        guard.ownershipDenied = (Action)Delegate.Combine(guard.ownershipDenied, failureAction);
                        guard.ownershipRequestNonce = Guid.NewGuid().ToString();
                        guard.currentState = NetworkingState.RequestingOwnership;
                        guard.netView.SendRPC("OwnershipRequested", guard.actualOwner, guard.ownershipRequestNonce);
                        return null;
                    case NetworkingState.ForcefullyTakingOver:
                    case NetworkingState.RequestingOwnership:
                    case NetworkingState.RequestingOwnershipWaitingForSight:
                    case NetworkingState.ForcefullyTakingOverWaitingForSight:
                        guard.ownershipDenied = (Action)Delegate.Combine(guard.ownershipDenied, failureAction);
                        return null;
                    default:
                        return null;
                }
            }

            if (BugCoroutine != null)
            {
                Coroutines.instance.StopCoroutine(BugCoroutine);
                References.LocalRig.enabled = true;
            }

            bug.worldShareableInstance.transferableObjectState = TransferrableObject.PositionState.Dropped;
            return bug;
        }
        public static void BugSpam()
        {
            ThrowableBug bugObject = GetBugObject("Floating Bug Holdable");

            if ((!bugObject.IsMyItem() || (bugObject.currentState != TransferrableObject.PositionState.Dropped && bugObject.currentState != TransferrableObject.PositionState.None)) && bugObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(bugObject.GetComponent<ClampPosition>());

            ThrowableBug fireflyObject = GetBugObject("Firefly");

            if ((!fireflyObject.IsMyItem() || (fireflyObject.currentState != TransferrableObject.PositionState.Dropped && fireflyObject.currentState != TransferrableObject.PositionState.None)) && fireflyObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(fireflyObject.GetComponent<ClampPosition>());

            ThrowableBug bug = GetBug("Floating Bug Holdable");
            ThrowableBug firefly = bug != null ? GetBug("Firefly") : bug;

            if (rightGrab)
            {
                if (Time.time > bugSpamDelay)
                {
                    bugSpamToggle = !bugSpamToggle;
                    bugSpamDelay = Time.time + 0.5f;

                    ThrowableBug targetBug = bugSpamToggle ? bug : firefly;

                    GameObject bugSpamObject = new GameObject("Qubit_BugSpamObject");
                    bugSpamObject.transform.localScale = Vector3.one * 0.2f;
                    bugSpamObject.layer = 3;

                    if (Manager.GetIndex("Bug Colliders").enabled)
                    {
                        SphereCollider collider = bugSpamObject.AddComponent<SphereCollider>();

                        if (Manager.GetIndex("Bouncy Bug").enabled)
                        {
                            collider.material.bounciness = 1f;
                            collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                            collider.material.dynamicFriction = 0f;
                        }
                    }

                    bugSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                    bugSpamObject.transform.rotation = References.RightHand.rotation;

                    Rigidbody rigidbody = bugSpamObject.AddComponent<Rigidbody>();
                    rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                    rigidbody.angularVelocity = RandomVector3(100f);

                    rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                    targetBug.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = bugSpamObject.transform;

                    bugSpamObject.AddComponent<DestroyOnRest>();
                    Object.Destroy(bugSpamObject, 30f);
                }
            }
        }

        public static void DisableBugSpam()
        {
            ThrowableBug bugObject = GetBugObject("Floating Bug Holdable");

            if (bugObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(bugObject.GetComponent<ClampPosition>());

            ThrowableBug fireflyObject = GetBugObject("Firefly");

            if (fireflyObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(fireflyObject.GetComponent<ClampPosition>());
        }

        public static void BugPhallus()
        {
            ThrowableBug Bug = GetBug("Floating Bug Holdable");
            ThrowableBug Firefly = GetBug("Firefly");

            if (Bug != null && Firefly != null)
            {
                Bug.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.TransformDirection(new Vector3(0f, -0.22f, 0.123f));
                Firefly.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.TransformDirection(new Vector3(0f, -0.22f, 0.24f));

                Bug.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0, 270, 0);
                Firefly.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0, 90, 0);
            }
        }

        public static void BugPhallusGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    ThrowableBug Bug = GetBug("Floating Bug Holdable");
                    ThrowableBug Firefly = GetBug("Firefly");

                    if (Bug != null && Firefly != null)
                    {
                        Bug.transform.position = lockTarget.transform.position + lockTarget.transform.TransformDirection(new Vector3(0f, -0.4f, 0.123f));
                        Firefly.transform.position = lockTarget.transform.position + lockTarget.transform.TransformDirection(new Vector3(0f, -0.4f, 0.24f));

                        Bug.transform.rotation = lockTarget.transform.rotation * Quaternion.Euler(0, 270, 0);
                        Firefly.transform.rotation = lockTarget.transform.rotation * Quaternion.Euler(0, 90, 0);
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void BugVibrateGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                ThrowableBug Bug = GetBug("Floating Bug Holdable");
                ThrowableBug Firefly = Bug != null ? GetBug("Firefly") : null;

                if (Bug != null)
                {
                    Bug.transform.position = lockTarget.leftHandTransform.position;
                    Bug.transform.rotation = RandomQuaternion();
                }

                if (Firefly != null)
                {
                    Firefly.transform.position = lockTarget.rightHandTransform.position;
                    Firefly.transform.rotation = RandomQuaternion();
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BugVibrateAll()
        {
            ThrowableBug Bug = GetBug("Floating Bug Holdable");
            if (Bug != null)
                GetBug("Firefly");
        }

        public static void EnableBugVibrateAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                ThrowableBug Bug = GetBug("Floating Bug Holdable");
                ThrowableBug Firefly = Bug != null ? GetBug("Firefly") : Bug;

                PhotonView BugView = Bug?.reliableState?.gameObject != null
                    ? Bug.reliableState.gameObject.GetComponent<GorillaNetworkTransform>().punView
                    : null;

                PhotonView FireflyView = Firefly?.reliableState?.gameObject != null
                    ? Firefly.reliableState.gameObject.GetComponent<GorillaNetworkTransform>().punView
                    : null;

                if (BugView == null || FireflyView == null)
                    return true;

                MassSerialize(true, new[] { BugView, FireflyView }.Where(v => v != null).ToArray());

                Vector3 bugArchivePosition = Bug.reliableState.gameObject?.transform?.position ?? Vector3.zero;
                Quaternion bugArchiveRotation = Bug.reliableState.gameObject?.transform?.rotation ?? Quaternion.identity;

                Vector3 fireflyArchivePosition = Firefly.reliableState.gameObject?.transform?.position ?? Vector3.zero;
                Quaternion fireflyArchiveRotation = Firefly.reliableState.gameObject?.transform?.rotation ?? Quaternion.identity;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);
                    if (targetRig == null)
                        continue;

                    if (Bug != null && Bug.transform != null && targetRig.leftHandTransform != null && BugView != null)
                    {
                        Bug.reliableState.gameObject.transform.position = targetRig.leftHandTransform.position;
                        Bug.reliableState.gameObject.transform.rotation = RandomQuaternion();
                        SendSerialize(BugView, new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                    }

                    if (Firefly != null && Firefly.transform != null && targetRig.rightHandTransform != null && FireflyView != null)
                    {
                        Firefly.reliableState.gameObject.transform.position = targetRig.rightHandTransform.position;
                        Firefly.reliableState.gameObject.transform.rotation = RandomQuaternion();
                        SendSerialize(FireflyView, new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                    }
                }

                if (Bug?.transform != null)
                {
                    Bug.reliableState.gameObject.transform.position = bugArchivePosition;
                    Bug.reliableState.gameObject.transform.rotation = bugArchiveRotation;
                }

                if (Firefly?.transform != null)
                {
                    Firefly.reliableState.gameObject.transform.position = fireflyArchivePosition;
                    Firefly.reliableState.gameObject.transform.rotation = fireflyArchiveRotation;
                }

                Security.RPCProtection();
                return false;
            };
        }

        public static void HolsterObject(string objectName, TransferrableObject.PositionState state)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null && (bug.currentState == TransferrableObject.PositionState.Dropped || bug.currentState == TransferrableObject.PositionState.None))
                bug.currentState = state;
        }

        public static void FreezeObject(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
            {
                bug.bugRotationalVelocity = Quaternion.identity;
                bug.targetVelocity = Vector3.zero;
                bug.thrownVeloicity = Vector3.zero;
                bug.thrownYVelocity = 0f;
                bug.reliableState.travelingDirection = Vector3.zero;
            }
        }

        public static void SetObjectSpeed(string objectName, float speed = 1f)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
                bug.maxNaturalSpeed = speed;
        }
        public static void PhysicalObject(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
            {
                GorillaVelocityTracker tracker = bug.gameObject.GetOrAddComponent<GorillaVelocityTracker>();

                if ((bug.currentState == TransferrableObject.PositionState.InLeftHand || bug.currentState == TransferrableObject.PositionState.InRightHand) && bug.GetComponent<ClampPosition>() != null)
                    Object.Destroy(bug.GetComponent<ClampPosition>());

                bool inAir = bug.currentState == TransferrableObject.PositionState.None || bug.currentState == TransferrableObject.PositionState.Dropped;
                lastInAirValues.TryGetValue(objectName, out bool lastInAir);

                if (inAir && !lastInAir)
                {
                    GameObject bugSpamObject = new GameObject("Qubit_BugSpamObject");
                    bugSpamObject.transform.localScale = Vector3.one * 0.2f;
                    bugSpamObject.layer = 3;

                    if (Manager.GetIndex("Bug Colliders").enabled)
                    {
                        SphereCollider collider = bugSpamObject.AddComponent<SphereCollider>();

                        if (Manager.GetIndex("Bouncy Bug").enabled)
                        {
                            collider.material.bounciness = 1f;
                            collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                            collider.material.dynamicFriction = 0f;
                        }
                    }

                    bugSpamObject.transform.position = bug.transform.position;
                    bugSpamObject.transform.rotation = bug.transform.rotation;

                    Rigidbody rigidbody = bugSpamObject.AddComponent<Rigidbody>();
                    rigidbody.linearVelocity = tracker.GetAverageVelocity(true, 0);
                    rigidbody.angularVelocity = bug.velocityEstimator.angularVelocity;

                    rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                    bug.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = bugSpamObject.transform;

                    bugSpamObject.AddComponent<DestroyOnRest>();
                    Object.Destroy(bugSpamObject, 30f);
                }

                lastInAirValues[objectName] = inAir;
            }
        }

        public static void StealObject(string objectName)
        {
            ThrowableBug bugObject = GetBugObject(objectName);
            if (bugObject != null && !bugObject.IsMyItem())
                bugObject.WorldShareableRequestOwnership();
        }

        public static Dictionary<string, float> objectSizeScale = new Dictionary<string, float>();
        public static void ObjectSizeChanger(string objectName)
        {
            ThrowableBug bug = GetBugObject(objectName);
            if (bug == null) return;

            objectSizeScale.TryGetValue(objectName, out float scale);
            if (scale <= 0f) scale = 1f;

            if (rightTrigger > 0.5f)
                scale += 0.1f;
            if (rightGrab)
                scale -= 0.1f;

            scale = Mathf.Max(scale, 0.05f);
            objectSizeScale[objectName] = scale;

            bug.transform.localScale = Vector3.one * scale;
        }

        public static void ObjectToHand(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (rightGrab && bug != null)
            {
                bug.transform.position = References.RightHand.position;
                bug.transform.rotation = References.RightHand.rotation;
            }
        }

        public static void DestroyObject(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
                bug.transform.position = new Vector3(99999f, 99999f, 99999f);
        }

        public static void SpazObject(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
                bug.transform.rotation = RandomQuaternion();
        }

        public static void OrbitObject(string objectName, float offset = 0)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
                bug.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30), 1, MathF.Sin(offset + (float)Time.frameCount / 30));
        }

        public static void ObjectAura(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);
            if (bug != null)
            {
                bug.transform.position = References.HeadCollider.transform.position + RandomVector3();
                bug.transform.rotation = RandomQuaternion();
            }
        }

        public static void RideObject(string objectName)
        {
            GameObject bugObject = GetBugObject(objectName).gameObject;

            TeleportPlayer(bugObject.transform.position);
            References.Rigidbody.linearVelocity = Vector3.zero;
        }
        public static void BecomeObject(string objectName)
        {
            ThrowableBug bug = GetBug(objectName);

            if (bug != null)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

                bug.transform.position = References.BodyCollider.transform.position;
                bug.transform.rotation = References.HeadCollider.transform.rotation;
            }
            else
            {
                if (lastWasNull)
                    References.LocalRig.enabled = true;
            }

            lastWasNull = bug != null;
        }
    }
}
