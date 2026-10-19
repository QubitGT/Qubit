/*
 * Qubit Menu  Libraries/Entities/Grabbing.cs
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

using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Categories;
using Qubit.Modules.Classes;
using Qubit.Patches.Locomotion;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Extensions.Players;
using static Qubit.Modules.Classes.GunLibrary;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// Forcibly grabbing other players' hands (and the mods that fling, bring or crash them once grabbed).
    /// </summary>
    public static class Grabbing
    {
        public static float grabDelay = 0;

        public static bool GrabStatus
        {
            get => HandLink.enabled;
            set
            {
                HandLink.enabled = value;

                if (!value && !References.LocalRig.enabled)
                    References.LocalRig.enabled = true;
            }
        }

        public static void GiveFlyOnGrab()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.isLocal)
                {
                    if (rig.leftHandLink.grabbedPlayer == Qubit.Core.Networking.LocalPlayer || rig.rightHandLink.grabbedPlayer == Qubit.Core.Networking.LocalPlayer)
                    {
                        bool grabbedOnLeft = References.LocalRig.leftHandLink.grabbedPlayer == rig.GetPlayer();
                        if (grabbedOnLeft ? rig.leftIndex.calcT > 0 : rig.rightIndex.calcT > 0)
                        {
                            References.Player.transform.position += rig.headMesh.transform.forward * (Time.deltaTime * Movement.FlySpeed);
                            References.Rigidbody.linearVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        public static void ForceGrab(Vector3 targetTransform)
        {
            foreach (VRRig rig in Players.ActiveRigs)
                ForceGrab(rig, targetTransform);
        }

        public static bool ForceGrab(VRRig rig, Vector3 targetTransform, bool returnOnGrab = false, bool enableRigOnceDone = false)
        {
            if (rig == null || rig.IsLocal()) return false;

            bool isLeftHand = rig.IsLeftHandGrabbable();
            bool isRightHand = rig.IsRightHandGrabbable();

            if (!isLeftHand && !isRightHand)
            {
                References.LocalRig.enabled = true;
                return false;
            }

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = rig.syncPos;

            var localLink = isLeftHand ? References.LocalRig.leftHandLink : References.LocalRig.rightHandLink;
            var remoteLink = isLeftHand ? rig.leftHandLink : rig.rightHandLink;

            if (remoteLink.grabbedPlayer != Qubit.Core.Networking.LocalPlayer)
            {
                if (grabDelay == 0)
                    grabDelay = remoteLink.rejectGrabsUntilTimestamp > Time.time ? remoteLink.rejectGrabsUntilTimestamp : Time.time + 1f;

                if (Time.time > grabDelay)
                {
                    localLink.TentacleTryCreateLink(remoteLink);
                    References.LocalRig.transform.position = targetTransform;
                    Notifications.SendFormat("purple", "MENU", "Tried to grab " + rig.GetPlayer().NickName + ".");
                    grabDelay = remoteLink.rejectGrabsUntilTimestamp > Time.time ? remoteLink.rejectGrabsUntilTimestamp : Time.time + 1f;
                }
            }
            else if (returnOnGrab)
            {
                if (enableRigOnceDone && !References.LocalRig.enabled)
                    References.LocalRig.enabled = true;
                return true;
            }

            return false;
        }

        public static void FlingOnGrab()
        {
            if (References.LocalRig.IsBeingHeld())
            {
                Transform transform = References.LocalRig.leftHandLink.IsLinkActive() ? References.LocalRig.leftHandTransform : References.LocalRig.rightHandTransform;
                VRRig rig = References.LocalRig.leftHandLink.grabbedPlayer.VRRig() ?? References.LocalRig.rightHandLink.grabbedPlayer.VRRig();
                Vector3 velocity = rig.transform.up * 3f;
                rig.GetNetView().SendRPC("DroppedByPlayer", rig.GetPlayer(), velocity);
            }
        }

        public static void ForceGrabGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ForceGrab(lockTarget, References.LocalRig.transform.position);

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
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                }
            });
        }

        public static void GrabFlingGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!lockTarget.IsBeingHeld())
                    {
                        if (!lockTarget.IsNear())
                        {
                            References.LocalRig.enabled = false;
                            References.LocalRig.transform.position = lockTarget.transform.position;
                        }
                        else if (!References.LocalRig.enabled)
                            References.LocalRig.enabled = true;
                        References.LocalRig.rightHandLink.TentacleTryCreateLink(lockTarget.rightHandLink);
                    }
                    else if (lockTarget.IsBeingHeld(References.LocalRig))
                    {
                        if (lockTarget.rightHandLink.grabbedPlayer == References.LocalRig.GetPlayer())
                        {
                            Vector3 velocity = lockTarget.transform.up * 3f;
                            lockTarget.GetNetView().SendRPC("DroppedByPlayer", lockTarget.GetPlayer(), velocity);
                        }

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
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                }
            });
        }

        public static void GrabFlingAll()
        {
            if (!References.LocalRig.IsBeingHeld() && !lockTarget.IsNear())
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = lockTarget.transform.position;
                References.LocalRig.rightHandLink.TentacleTryCreateLink(lockTarget.rightHandLink);
            }
            else if (References.LocalRig.IsBeingHeld())
            {
                FlingOnGrab();
                if (!References.LocalRig.enabled)
                    References.LocalRig.enabled = true;
            }
        }

        public static void BringPlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                ForceGrab(lockTarget, References.LocalRig.transform.position - References.LocalRig.transform.forward * 10f);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BringAllPlayers() =>
            ForceGrab(References.LocalRig.transform.position - References.LocalRig.transform.forward * 10f);

        public static void PushPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ForceGrab(lockTarget, References.LocalRig.transform.position = lockTarget.transform.position - lockTarget.transform.forward * 10f);

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
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                }
            });
        }

        public static void PushAllPlayers()
        {
            foreach (VRRig rig in Players.ActiveRigs)
                ForceGrab(rig, rig.transform.position - rig.transform.forward * 10f);
        }

        public static void GrabCrashGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;
                float value = float.MaxValue / 4f;
                if (gunLocked && lockTarget != null)
                    ForceGrab(lockTarget, new Vector3(value, -value, value));

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
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                }
            });
        }

        public static void GrabCrashAll()
        {
            float value = float.MaxValue / 4f;
            ForceGrab(new Vector3(value, -value, value));
        }
    }
}
