/*
 * Qubit Menu  Modules/Categories/Rig.cs
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

using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using Qubit.Patches.Rigs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using Audio = Qubit.Modules.Categories.Sound.Audio;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Rig
    {
        public static bool wasDisabledAlready;
        public static bool invisMonke;
        public static bool lastHit;
        public static bool lastHit2;
        private static bool ghostMonke;
        public static int weepingAngelMode;
        public static readonly string[] WeepingAngelModeNames = { "Specific Player", "Anyone" };
        public static void ApplyWeepingAngelMode(int index)
        {
            weepingAngelMode = index;
            weepingAngelPlayer = null;
        }
        private static NetPlayer weepingAngelPlayer;
        private static bool weepingAngelFrozen;
        public static Vector3? startPosition;
        public static Vector3 stillBeybladeStartPos = Vector3.zero;
        public static readonly Dictionary<VRRig, Vector3> followPositions = new Dictionary<VRRig, Vector3>();
        public static void ResetRig()
        {
            References.LocalRig.enabled = true;
            References.LocalRig.transform.localRotation = Quaternion.identity;
            References.HeadCollider.transform.localRotation = Quaternion.identity;
        }
        public static void BecomePlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = lockTarget.transform.position;
                References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                References.LocalRig.head.rigTarget.position = lockTarget.head.rigTarget.position;
                References.LocalRig.head.rigTarget.rotation = lockTarget.head.rigTarget.rotation;
                References.LocalRig.leftHand.rigTarget.position = lockTarget.leftHand.rigTarget.position;
                References.LocalRig.leftHand.rigTarget.rotation = lockTarget.leftHand.rigTarget.rotation;
                References.LocalRig.rightHand.rigTarget.position = lockTarget.rightHand.rigTarget.position;
                References.LocalRig.rightHand.rigTarget.rotation = lockTarget.rightHand.rigTarget.rotation;

                References.LocalRig.bodyTransform.rotation = lockTarget.bodyTransform.rotation;
                References.LocalRig.headMesh.transform.rotation = lockTarget.headMesh.transform.rotation;
            }, new GunLibrary.GunSettings { locks = true, onIdle = () => { if (gunLocked) { gunLocked = false; References.LocalRig.enabled = true; } } });
        }
        public static void Invisible()
        {
            bool hit = rightSecondary;
            if (Manager.GetIndex("Non-Togglable Invisible").enabled)
                invisMonke = hit;
            if (invisMonke)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;
            }
            if (hit && !lastHit2)
            {
                invisMonke = !invisMonke;
                if (invisMonke)
                    wasDisabledAlready = References.LocalRig.enabled;
                else
                    References.LocalRig.enabled = wasDisabledAlready;
            }
            lastHit2 = hit;
        }
        public static void Ghost()
        {
            bool hit = rightPrimary;
            if (Manager.GetIndex("Non-Togglable Ghost").enabled)
                ghostMonke = hit;

            References.LocalRig.enabled = !ghostMonke;
            if (hit && !lastHit)
                ghostMonke = !ghostMonke;

            lastHit = hit;
        }
        public static void EnableRig()
        {
            References.LocalRig.enabled = true;
            ghostException = false;
        }
        public static void WeepingAngel()
        {
            if (weepingAngelMode == 0)
            {
                GunLibrary.Aim(GunData =>
                {
                    if (GetGunInput(true))
                    {
                        VRRig gunTarget = GunData.Ray.collider.GetComponentInParent<VRRig>();
                        NetPlayer gunPlayer = gunTarget && !gunTarget.IsLocal() ? GetPlayerFromVRRig(gunTarget) : null;
                        if (gunPlayer != null && gunPlayer != weepingAngelPlayer)
                        {
                            weepingAngelPlayer = gunPlayer;
                            Notifications.SendFormat("green", "WEEPING ANGEL", $"Only {gunPlayer.NickName} can freeze you now.");
                        }
                    }
                });
            }

            VRRig target = weepingAngelPlayer == null ? null : GetVRRigFromPlayer(weepingAngelPlayer);
            if (weepingAngelMode == 0 && target == null)
                weepingAngelPlayer = null;

            bool observed = weepingAngelMode == 0
                ? target != null && IsWatching(target)
                : Players.ActiveRigs.Any(rig => !rig.IsLocal() && IsWatching(rig));

            VRRig prey = !observed && Manager.GetIndex("Weeping Angel Chase").enabled
                ? (weepingAngelMode == 0 ? target : References.LocalRig.GetClosest())
                : null;

            bool detached = observed || prey != null;
            if (detached != weepingAngelFrozen)
            {
                weepingAngelFrozen = detached;
                References.LocalRig.enabled = !detached;
            }

            if (prey != null) ChaseWeepingAngelPrey(prey);
        }
        public static void DisableWeepingAngel()
        {
            if (weepingAngelFrozen) EnableRig();
            weepingAngelFrozen = false;
            weepingAngelPlayer = null;
        }
        public static void RigGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = NewPointer.transform.position + new Vector3(0, 1, 0);
                }
                else
                    References.LocalRig.enabled = true;
            });
        }
        public static void LookAtGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    References.LocalRig.head.rigTarget.LookAt(lockTarget.head.rigTarget.transform);

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
                }
            });
        }
        public static void ShadowCloneGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;
                    Vector3 offset = lockTarget.transform.right * 0.9f;

                    References.LocalRig.transform.position = lockTarget.transform.position + offset;
                    References.LocalRig.transform.rotation = lockTarget.transform.rotation;
                    References.LocalRig.head.rigTarget.rotation = lockTarget.head.rigTarget.rotation;

                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.leftHand.rigTarget.transform.position + offset;
                    References.LocalRig.leftHand.rigTarget.transform.rotation = lockTarget.leftHand.rigTarget.transform.rotation;

                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.rightHand.rigTarget.transform.position + offset;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = lockTarget.rightHand.rigTarget.transform.rotation;
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
        public static void PCRigPose()
        {
            if (XRSettings.isDeviceActive || !References.LocalRig.enabled) return;

            Transform bodyTransform = References.LocalRig.bodyRenderer.transform;

            References.Player.leftHand.controllerTransform.position = bodyTransform.position + bodyTransform.right * -0.7f;
            References.Player.leftHand.controllerTransform.rotation = bodyTransform.rotation;

            References.Player.rightHand.controllerTransform.position = bodyTransform.position + bodyTransform.right * 0.7f;
            References.Player.rightHand.controllerTransform.rotation = bodyTransform.rotation;
        }
        public static void JerkOffAnimation()
        {
            Transform hand = References.Player.rightHand.controllerTransform;
            Transform body = References.BodyCollider.transform;

            float bob = Mathf.Sin(Time.time * 6f) * 0.1f;
            hand.position = body.position + Vector3.down * 0.2f + body.rotation * new Vector3(0f, 0f, bob + 0.1f);
            hand.rotation = Quaternion.Euler(0f, 10f, 0f) * body.rotation;

            References.InputPoller.rightControllerGripFloat = 1f;
            References.InputPoller.rightControllerIndexFloat = 1f;
            References.InputPoller.rightControllerPrimaryButton = true;
        }
        public static void GrabRig()
        {
            if (rightGrab)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.RightHand.position;
                References.LocalRig.transform.rotation = References.RightHand.rotation;
            }
            else
            {
                References.LocalRig.enabled = true;
            }
        }
        public static void EnableSpazRig()
        {
            ghostException = true;
            offsetLH = References.LocalRig.leftHand.trackingPositionOffset;
            offsetRH = References.LocalRig.rightHand.trackingPositionOffset;
            offsetH = References.LocalRig.head.trackingPositionOffset;
        }
        public static void SpazRig()
        {
            if (rightPrimary)
            {
                float spazAmount = 0.1f;
                ghostException = true;
                References.LocalRig.leftHand.trackingPositionOffset = offsetLH + RandomVector3(spazAmount);
                References.LocalRig.rightHand.trackingPositionOffset = offsetRH + RandomVector3(spazAmount);
                References.LocalRig.head.trackingPositionOffset = offsetH + RandomVector3(spazAmount);
            }
            else
            {
                ghostException = false;
                References.LocalRig.leftHand.trackingPositionOffset = offsetLH;
                References.LocalRig.rightHand.trackingPositionOffset = offsetRH;
                References.LocalRig.head.trackingPositionOffset = offsetH;
            }
        }
        public static void DisableSpazRig()
        {
            ghostException = false;
            References.LocalRig.leftHand.trackingPositionOffset = offsetLH;
            References.LocalRig.rightHand.trackingPositionOffset = offsetRH;
            References.LocalRig.head.trackingPositionOffset = offsetH;
        }
        public static void SpazHands()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                References.LocalRig.leftHand.rigTarget.transform.position = References.LeftHand.position + References.LocalRig.leftHand.rigTarget.transform.forward * 3f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.RightHand.position + References.LocalRig.rightHand.rigTarget.transform.forward * 3f;
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void SpazRealHands()
        {
            if (rightPrimary)
            {
                References.Player.GetControllerTransform(true).rotation = RandomQuaternion();
                References.Player.GetControllerTransform(true).position = References.LeftHand.position + References.Player.GetControllerTransform(true).forward * 3f;

                References.Player.GetControllerTransform(false).rotation = RandomQuaternion();
                References.Player.GetControllerTransform(false).position = References.RightHand.position + References.Player.GetControllerTransform(false).forward * 3f;
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void FreezeRigLimbs()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
        }
        public static void FreezeRigBody()
        {
            References.LocalRig.enabled = false;

            var (leftPosition, leftRotation, _, _, _) = Controllers.GetTrueLeftHand();
            var (rightPosition, rightRotation, _, _, _) = Controllers.GetTrueRightHand();

            References.LocalRig.leftHand.rigTarget.transform.position = leftPosition;
            References.LocalRig.rightHand.rigTarget.transform.position = rightPosition;

            References.LocalRig.leftHand.rigTarget.transform.rotation = leftRotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = rightRotation;

            FixRigHandRotation();

            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;
        }
        public static void FreezeRig()
        {
            if (startPosition == null)
                startPosition = References.LocalRig.transform.position;

            References.LocalRig.enabled = true;
            References.LocalRig.PostTick();
            References.LocalRig.transform.position = startPosition.Value;
            References.LocalRig.enabled = false;
        }
        public static void ParalyzeRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.08f + References.BodyCollider.transform.up * 0.12f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.08f + References.BodyCollider.transform.up * 0.12f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
        }
        public static void ChickenRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.2f + References.BodyCollider.transform.up * -0.2f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.2f + References.BodyCollider.transform.up * -0.2f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
        }
        public static void AmputateRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(160f, 90f, 0f);

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.08f + References.BodyCollider.transform.up * 0.12f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.08f + References.BodyCollider.transform.up * 0.12f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
        }
        public static void DecapitateRigUpdate() =>
            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(160f, 90f, 0f);
        public static void SetBodyPatch(bool enabled, int mode = 0)
        {
            Torso.enabled = enabled;
            Torso.mode = mode;

            if (!enabled && recBodyRotary != null)
                Object.Destroy(recBodyRotary);
        }
        public static void RecRoomBody()
        {
            SetBodyPatch(true, 3);

            if (recBodyRotary == null)
                recBodyRotary = new GameObject("ii_recBodyRotary");

            recBodyRotary.transform.rotation = Quaternion.Lerp(recBodyRotary.transform.rotation, Quaternion.Euler(0f, References.HeadCollider.transform.rotation.eulerAngles.y, 0f), Time.deltaTime * 6.5f);
        }
        public static void FreezeBodyRotation()
        {
            SetBodyPatch(true, 3);

            if (recBodyRotary == null)
                recBodyRotary = new GameObject("ii_recBodyRotary");

            recBodyRotary.transform.rotation = rightGrab ? recBodyRotary.transform.rotation : Quaternion.Euler(0f, References.HeadCollider.transform.rotation.eulerAngles.y, 0f);
        }
        public static void AutoDance()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                Vector3 bodyOffset = References.BodyCollider.transform.right * (Mathf.Cos(Time.frameCount / 20f) * 0.3f) + new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.frameCount / 20f) * 0.2f), 0f);
                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f) + bodyOffset;
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.forward * 0.2f + References.LocalRig.transform.right * -0.4f + References.LocalRig.transform.up * (0.3f + Mathf.Sin(Time.frameCount / 20f) * 0.2f);
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.forward * 0.2f + References.LocalRig.transform.right * 0.4f + References.LocalRig.transform.up * (0.3f + Mathf.Sin(Time.frameCount / 20f) * -0.2f);

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void AutoGriddy()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                Vector3 bodyOffset = References.LocalRig.transform.forward * (5f * Time.deltaTime);
                References.LocalRig.transform.position = References.LocalRig.transform.position + bodyOffset;
                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -0.33f + References.LocalRig.transform.forward * (0.5f * Mathf.Cos(Time.frameCount / 10f)) + References.LocalRig.transform.up * (-0.5f * Mathf.Abs(Mathf.Sin(Time.frameCount / 10f)));
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 0.33f + References.LocalRig.transform.forward * (0.5f * Mathf.Cos(Time.frameCount / 10f)) + References.LocalRig.transform.up * (-0.5f * Mathf.Abs(Mathf.Sin(Time.frameCount / 10f)));

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void AutoTPose()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void Helicopter()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position += new Vector3(0f, 0.05f, 0f);
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void Beyblade()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void StillBeyblade()
        {
            if (rightPrimary)
            {
                if (stillBeybladeStartPos == Vector3.zero)
                    stillBeybladeStartPos = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);

                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = stillBeybladeStartPos;
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
            {
                stillBeybladeStartPos = Vector3.zero;
                References.LocalRig.enabled = true;
            }
        }
        public static void TorsoPatch_VRRigLateUpdate() =>
            References.LocalRig.transform.rotation *= Quaternion.Euler(0f, Time.time * 180f % 360f, 0f);
        public static void Fan()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + (References.LocalRig.transform.up * (Mathf.Cos(Time.time * 15f) * 2f) + References.LocalRig.transform.right * (Mathf.Sin(Time.time * 15f) * 2f));
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position - (References.LocalRig.transform.up * (Mathf.Cos(Time.time * 15f) * 2f) + References.LocalRig.transform.right * (Mathf.Sin(Time.time * 15f) * 2f));

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void GhostAnimations()
        {
            References.LocalRig.enabled = false;

            if (headPos == Vector3.zero)
                headPos = References.HeadCollider.transform.position;
            if (headRot == Vector3.zero)
                headRot = References.HeadCollider.transform.rotation.eulerAngles;

            if (handPos_L == Vector3.zero)
                handPos_L = References.LeftHand.transform.position;
            if (handRot_L == Vector3.zero)
                handRot_L = References.LeftHand.transform.rotation.eulerAngles;

            if (handPos_R == Vector3.zero)
                handPos_R = References.RightHand.transform.position;
            if (handRot_R == Vector3.zero)
                handRot_R = References.RightHand.transform.rotation.eulerAngles;

            float positionSpeed = 0.01f;
            float rotationSpeed = 2.0f;
            float positionThreshold = 0.05f;
            float rotationThreshold = 11.5f;
            if (Vector3.Distance(headPos, References.HeadCollider.transform.position) > positionThreshold)
                headPos += Vector3.Normalize(References.HeadCollider.transform.position - headPos) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(headRot), References.HeadCollider.transform.rotation) > rotationThreshold)
                headRot = Quaternion.RotateTowards(Quaternion.Euler(headRot), References.HeadCollider.transform.rotation, rotationSpeed).eulerAngles;

            if (Vector3.Distance(handPos_L, References.LeftHand.transform.position) > positionThreshold)
                handPos_L += Vector3.Normalize(References.LeftHand.transform.position - handPos_L) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(handRot_L), References.LeftHand.transform.rotation) > rotationThreshold)
                handRot_L = Quaternion.RotateTowards(Quaternion.Euler(handRot_L), References.LeftHand.transform.rotation, rotationSpeed).eulerAngles;

            if (Vector3.Distance(handPos_R, References.RightHand.transform.position) > positionThreshold)
                handPos_R += Vector3.Normalize(References.RightHand.transform.position - handPos_R) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(handRot_R), References.RightHand.transform.rotation) > rotationThreshold)
                handRot_R = Quaternion.RotateTowards(Quaternion.Euler(handRot_R), References.RightHand.transform.rotation, rotationSpeed).eulerAngles;

            References.LocalRig.transform.position = headPos - new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = Quaternion.Euler(new Vector3(0f, headRot.y, 0f));

            References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(headRot);

            References.LocalRig.leftHand.rigTarget.transform.position = handPos_L;
            References.LocalRig.rightHand.rigTarget.transform.position = handPos_R;

            References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(handRot_L);
            References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(handRot_R);

            References.LocalRig.leftIndex.calcT = leftTrigger;
            References.LocalRig.leftMiddle.calcT = leftGrab ? 1 : 0;
            References.LocalRig.leftThumb.calcT = leftPrimary || leftSecondary ? 1 : 0;

            References.LocalRig.leftIndex.LerpFinger(1f, false);
            References.LocalRig.leftMiddle.LerpFinger(1f, false);
            References.LocalRig.leftThumb.LerpFinger(1f, false);

            References.LocalRig.rightIndex.calcT = rightTrigger;
            References.LocalRig.rightMiddle.calcT = rightGrab ? 1 : 0;
            References.LocalRig.rightThumb.calcT = rightPrimary || rightSecondary ? 1 : 0;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
            References.LocalRig.rightThumb.LerpFinger(1f, false);

            FixRigHandRotation();
        }
        public static void DisableGhostAnimations()
        {
            headPos = Vector3.zero;
            headRot = Vector3.zero;

            handPos_L = Vector3.zero;
            handRot_L = Vector3.zero;

            handPos_R = Vector3.zero;
            handRot_R = Vector3.zero;

            References.LocalRig.enabled = true;
        }
        public static void MinecraftAnimations()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

            if (rightPrimary)
            {
                References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.25f + References.BodyCollider.transform.up * -1f + References.BodyCollider.transform.forward * Mathf.Sin(Time.frameCount / 10f);
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + References.BodyCollider.transform.up * -1f + -(References.BodyCollider.transform.forward * Mathf.Sin(Time.frameCount / 10f));
            }
            else
            {
                References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.25f + References.BodyCollider.transform.up * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + References.BodyCollider.transform.up * -1f;
            }

            if (rightSecondary)
            {
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + Vector3.Lerp(References.RightHand.forward, -References.RightHand.up, 0.5f) * 2f;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.RightHand.rotation;
            }

            FixRigHandRotation();
        }
        public static void StareAtNearby() =>
            References.LocalRig.head.rigTarget.LookAt(GetClosestVRRig().headMesh.transform.position);
        public static void StareAtTarget() =>
            References.LocalRig.head.rigTarget.LookAt(lockTarget.headMesh.transform.position);
        public static void StareAtGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!hasAdded)
                {
                    hasAdded = true;
                    Torso.VRRigLateUpdate += StareAtTarget;
                }

                gunLocked = true;
                lockTarget = gunTarget;
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        if (hasAdded)
                        {
                            hasAdded = false;
                            Torso.VRRigLateUpdate -= StareAtTarget;
                        }
                    }
                }
            });
        }
        public static void StareAtAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Quaternion headRotArchive = References.LocalRig.head.rigTarget.transform.rotation;
                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    References.LocalRig.head.rigTarget.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(GetVRRigFromPlayer(Player).headMesh.transform.position));
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();
                References.LocalRig.head.rigTarget.transform.rotation = headRotArchive;

                return false;
            };
        }
        public static void EyeContact()
        {
            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                if (Physics.SphereCast(rig.headMesh.transform.position + (rig.headMesh.transform.forward * 0.25f), 0.25f, rig.headMesh.transform.forward, out _, 512f, NoInvisLayerMask()))
                {
                    References.LocalRig.head.rigTarget.LookAt(rig.headMesh.transform.position);
                    break;
                }
            }
        }
        public static void EnableFloatingRig() =>
            offsetH = References.LocalRig.head.trackingPositionOffset;
        public static void FloatingRig() =>
            References.LocalRig.head.trackingPositionOffset = offsetH + new Vector3(0f, 0.65f + Mathf.Sin(Time.frameCount / 40f) * 0.2f, 0f);
        public static void DisableFloatingRig() =>
            References.LocalRig.head.trackingPositionOffset = offsetH;
        public static void Bees()
        {
            References.LocalRig.enabled = false;
            if (Time.time > beesDelay)
            {
                VRRig target = GetRandomVRRig();

                References.LocalRig.transform.position = target.transform.position + Vector3.up;

                References.LocalRig.leftHand.rigTarget.transform.position = target.transform.position;
                References.LocalRig.rightHand.rigTarget.transform.position = target.transform.position;

                beesDelay = Time.time + 0.777f;
            }
        }
        public static void VRRigLateUpdate_Dinnerbone() =>
            References.LocalRig.transform.RotateAround(References.LocalRig.bodyTransform.position, References.LocalRig.bodyTransform.forward, 180f);
        public static void VRRigLateUpdate_SpazBody() => Rotate(Random.rotation);
        public static void VRRigLateUpdate_FakeFBT()
        {
            Rotate(Camera.main.transform.rotation);
            References.LocalRig.head.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
            References.LocalRig.leftHand.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
            References.LocalRig.rightHand.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
        }
        public static void VRRigLateUpdate_Control()
        {
            if (rightJoystickClick || leftJoystickClick || UnityInput.GetKeyDown(Key.Enter))
                vrrigJoystickRot = null;

            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;
            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            Vector2 keys = Vector2.zero;
            if (UnityInput.GetKey(Key.LeftArrow)) keys.x -= 1f;
            if (UnityInput.GetKey(Key.RightArrow)) keys.x += 1f;
            if (UnityInput.GetKey(Key.UpArrow)) keys.y += 1f;
            if (UnityInput.GetKey(Key.DownArrow)) keys.y -= 1f;

            if (keys.sqrMagnitude > 0f)
                keys.Normalize();

            if (keys.sqrMagnitude > stick.sqrMagnitude)
                stick = keys;

            if (stick.sqrMagnitude > 0.0025)
            {
                if (!vrrigJoystickRot.HasValue)
                    vrrigJoystickRot = References.LocalRig.transform.rotation;

                float yaw = stick.x * 200f * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;
                float pitch = -stick.y * 200f * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;

                Quaternion rot = vrrigJoystickRot.Value;
                rot = Quaternion.AngleAxis(yaw, Vector3.up) * rot;
                Vector3 localRight = rot * Vector3.right;
                rot = Quaternion.AngleAxis(pitch, localRight) * rot;
                rot.Normalize();

                vrrigJoystickRot = rot;
            }

            if (vrrigJoystickRot != null)
                References.LocalRig.transform.rotation = vrrigJoystickRot.Value;
        }
        public static void ControlBodyRotation()
        {
            if (rightJoystickClick || leftJoystickClick || UnityInput.GetKeyDown(Key.Enter))
                bodyJoystickRot = null;

            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;
            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            Vector2 keys = Vector2.zero;
            if (UnityInput.GetKey(Key.LeftArrow)) keys.x -= 1f;
            if (UnityInput.GetKey(Key.RightArrow)) keys.x += 1f;
            if (UnityInput.GetKey(Key.UpArrow)) keys.y += 1f;
            if (UnityInput.GetKey(Key.DownArrow)) keys.y -= 1f;

            if (keys.sqrMagnitude > 0f)
                keys.Normalize();

            if (keys.sqrMagnitude > stick.sqrMagnitude)
                stick = keys;

            float yawSpeed = 200f;
            float pitchSpeed = 200f;

            if (stick.sqrMagnitude > 0.0025)
            {
                if (!bodyJoystickRot.HasValue)
                    bodyJoystickRot = References.LocalRig.transform.rotation;

                float yaw = stick.x * yawSpeed * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;
                float pitch = -stick.y * pitchSpeed * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;

                Quaternion rot = bodyJoystickRot.Value;
                rot = Quaternion.AngleAxis(yaw, Vector3.up) * rot;
                Vector3 localRight = rot * Vector3.right;
                rot = Quaternion.AngleAxis(pitch, localRight) * rot;
                rot.Normalize();

                bodyJoystickRot = rot;
            }

            if (bodyJoystickRot != null)
                References.LocalRig.transform.rotation = bodyJoystickRot.Value;
        }
        public static void Flip()
        {
            bool silentFlip = Manager.GetIndex("Silent Flip").enabled;
            if (!flipping && (rightPrimary || rightSecondary) && References.LocalRig.enabled)
            {
                if (References.Player.playerRigidBody)
                {
                    flipping = true;
                    flipStart = Time.time;
                    flipAxis = rightPrimary ? References.LocalRig.transform.right : -References.LocalRig.transform.right;
                    flipFrom = References.Player?.playerRigidBody?.rotation ?? Quaternion.identity;
                }
            }
            if (!flipping) return;

            float t = (Time.time - flipStart) / flipDuration;
            if (t >= 1f)
            {
                flipping = false;
                if (silentFlip)
                    References.LocalRig.transform.rotation = flipFrom;
                else
                    GTPlayerTransform.ApplyRotationOverride(flipFrom, Time.frameCount);

                return;
            }
            var rot = Quaternion.AngleAxis(-360f * t, flipAxis) * flipFrom;
            if (silentFlip)
                References.LocalRig.transform.rotation = Quaternion.Euler(0f, References.BodyCollider.transform.eulerAngles.y, 0f) * rot;
            else
                GTPlayerTransform.ApplyRotationOverride(rot, Time.frameCount);
        }
        public static void SpiderCrawl() =>
            References.HeadCollider.transform.rotation = Quaternion.Euler(-270, References.HeadCollider.transform.rotation.eulerAngles.y, 0);
        public static void ButtHead() => RigPose(270, 0);
        public static void LayDown() => RigPose(90, 0);
        public static void FlatOnBack() => RigPose(270, 0);
        public static void Sideways() => RigPose(90, 90);
        public static void TiltLeft() => RigPose(0, 45);
        public static void TiltRight() => RigPose(0, -45);
        public static void LeanForward() => RigPose(45, 0);
        public static void LeanBack() => RigPose(-45, 0);
        public static void UpwardsBody() =>
             References.HeadCollider.transform.rotation = Quaternion.Euler(-180, References.HeadCollider.transform.rotation.eulerAngles.y, References.HeadCollider.transform.rotation.eulerAngles.z);
        public static void PiggybackGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                TeleportPlayer(lockTarget.transform.position + new Vector3(0f, 0.5f, 0f));
                References.Rigidbody.linearVelocity = Vector3.zero;
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void PiggybackAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    References.LocalRig.transform.position = GetVRRigFromPlayer(Player).headMesh.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };
        }
        public static void CopyMovementGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    CopyMovementPlayer(lockTarget.GetPlayer());

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
        public static void CopyMovementPlayer(NetPlayer player, bool fingers = true)
        {
            VRRig targetRig = GetVRRigFromPlayer(player);
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = targetRig.syncPos;
            References.LocalRig.transform.rotation = targetRig.syncRotation;

            References.LocalRig.leftHand.rigTarget.transform.position = targetRig.leftHand.rigTarget.transform.position;
            References.LocalRig.rightHand.rigTarget.transform.position = targetRig.rightHand.rigTarget.transform.position;

            References.LocalRig.leftHand.rigTarget.transform.rotation = targetRig.leftHand.rigTarget.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = targetRig.rightHand.rigTarget.transform.rotation;

            if (fingers)
            {
                References.LocalRig.leftIndex.calcT = targetRig.leftIndex.calcT;
                References.LocalRig.leftMiddle.calcT = targetRig.leftMiddle.calcT;
                References.LocalRig.leftThumb.calcT = targetRig.leftThumb.calcT;

                References.LocalRig.leftIndex.LerpFinger(1f, false);
                References.LocalRig.leftMiddle.LerpFinger(1f, false);
                References.LocalRig.leftThumb.LerpFinger(1f, false);

                References.LocalRig.rightIndex.calcT = targetRig.rightIndex.calcT;
                References.LocalRig.rightMiddle.calcT = targetRig.rightMiddle.calcT;
                References.LocalRig.rightThumb.calcT = targetRig.rightThumb.calcT;

                References.LocalRig.rightIndex.LerpFinger(1f, false);
                References.LocalRig.rightMiddle.LerpFinger(1f, false);
                References.LocalRig.rightThumb.LerpFinger(1f, false);
            }

            References.LocalRig.head.rigTarget.transform.rotation = targetRig.head.rigTarget.transform.rotation;
        }
        public static void CopyMovementAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    CopyMovementPlayer(Player, false);
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.enabled = true;

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void FollowPlayer(NetPlayer player, bool fingers = true)
        {
            VRRig targetRig = GetVRRigFromPlayer(player);
            References.LocalRig.enabled = false;

            Vector3 look = targetRig.transform.position - References.LocalRig.transform.position;
            look.Normalize();

            Vector3 position = References.LocalRig.transform.position + look * (Movement.FlySpeed / 2f * Time.deltaTime);

            References.LocalRig.transform.position = position;
            References.LocalRig.transform.LookAt(targetRig.transform.position);

            References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
            References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

            FixRigHandRotation();

            if (fingers)
            {
                References.LocalRig.leftIndex.calcT = 0f;
                References.LocalRig.leftMiddle.calcT = 0f;
                References.LocalRig.leftThumb.calcT = 0f;

                References.LocalRig.leftIndex.LerpFinger(1f, false);
                References.LocalRig.leftMiddle.LerpFinger(1f, false);
                References.LocalRig.leftThumb.LerpFinger(1f, false);

                References.LocalRig.rightIndex.calcT = 0f;
                References.LocalRig.rightMiddle.calcT = 0f;
                References.LocalRig.rightThumb.calcT = 0f;

                References.LocalRig.rightIndex.LerpFinger(1f, false);
                References.LocalRig.rightMiddle.LerpFinger(1f, false);
                References.LocalRig.rightThumb.LerpFinger(1f, false);
            }
        }
        public static void FollowPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    FollowPlayer(lockTarget.GetPlayer());

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
        public static void FollowAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    Vector3 position = followPositions.GetValueOrDefault(targetRig, archivePos);

                    Vector3 look = targetRig.transform.position - position;
                    look.Normalize();

                    position += look * (Movement.FlySpeed / 2f * Time.deltaTime);

                    followPositions.Remove(targetRig);

                    followPositions.Add(targetRig, position);

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void OrbitPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = lockTarget.transform.position + new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f));
                    References.LocalRig.transform.LookAt(lockTarget.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    FixRigHandRotation();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
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
        public static void OrbitAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position + new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f));
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void JumpscareGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.forward * Random.Range(0.1f, 0.5f);
                    References.LocalRig.head.rigTarget.transform.LookAt(lockTarget.headMesh.transform.position);
                    Quaternion dirLook = References.LocalRig.head.rigTarget.transform.rotation;

                    References.LocalRig.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.right * 0.2f;
                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.right * -0.2f;

                    References.LocalRig.head.rigTarget.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                    FixRigHandRotation();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
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
        public static void JumpscareAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.forward * Random.Range(0.1f, 0.5f);
                    References.LocalRig.head.rigTarget.transform.LookAt(targetRig.headMesh.transform.position);
                    Quaternion dirLook = References.LocalRig.head.rigTarget.transform.rotation;

                    References.LocalRig.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.right * 0.2f;
                    References.LocalRig.rightHand.rigTarget.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.right * -0.2f;

                    References.LocalRig.head.rigTarget.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void AnnoyPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    Vector3 position = lockTarget.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(lockTarget.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    Audio.SoundSpam(337, true);
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
        public static void AnnoyAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    Vector3 position = targetRig.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void IntercourseGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * -(0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = lockTarget.transform.rotation;
                        References.LocalRig.rightHand.rigTarget.transform.rotation = lockTarget.transform.rotation;
                    }
                    else
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = lockTarget.transform.rotation;
                    }

                    FixRigHandRotation();
                    IntercourseNoises();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    References.LocalRig.head.rigTarget.transform.rotation = lockTarget.transform.rotation;
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
        public static void IntercourseNoises()
        {
            if (Time.frameCount % 45 == 0)
            {
                if (Networking.InRoom)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 64, true, 999999f);

                    if (Manager.GetIndex("Splash Intercourse").enabled)
                        Fun.WaterSplash(References.LocalRig.transform.position, References.LocalRig.transform.rotation);

                    RPCProtection();
                }
                else
                    References.LocalRig.PlayHandTapLocal(64, true, 999999f);
            }
        }
        public static void IntercourseAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * -(0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = targetRig.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = targetRig.transform.rotation;
                        References.LocalRig.rightHand.rigTarget.transform.rotation = targetRig.transform.rotation;
                    }
                    else
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = targetRig.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = targetRig.transform.rotation;
                    }

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void HeadGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + lockTarget.transform.up * -0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }
                    else
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + lockTarget.transform.up * 0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * 0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * 0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    FixRigHandRotation();
                    IntercourseNoises();
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
        public static void HeadAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + targetRig.transform.up * -0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }
                    else
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + targetRig.transform.up * 0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * 0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * 0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void SpazHead()
        {
            if (References.LocalRig.enabled)
            {
                References.LocalRig.head.trackingRotationOffset.x = Random.Range(0f, 360f);
                References.LocalRig.head.trackingRotationOffset.y = Random.Range(0f, 360f);
                References.LocalRig.head.trackingRotationOffset.z = Random.Range(0f, 360f);
            }
            else
                References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
        }
        public static void RandomSpazHead()
        {
            if (headspazType)
            {
                SpazHead();
                if (Time.time > headspazDelay)
                {
                    headspazType = false;
                    headspazDelay = Time.time + Random.Range(1000f, 4000f) / 1000f;
                }
            }
            else
            {
                FixHead();
                if (Time.time > headspazDelay)
                {
                    headspazType = true;
                    headspazDelay = Time.time + Random.Range(200f, 1000f) / 1000f;
                }
            }
        }
        public static void EnableSpazHead() =>
            headoffs = References.LocalRig.head.trackingPositionOffset;
        public static void SpazHeadPosition() =>
            References.LocalRig.head.trackingPositionOffset = headoffs + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
        public static void FixHeadPosition() =>
            References.LocalRig.head.trackingPositionOffset = headoffs;
        public static void RandomSpazHeadPosition()
        {
            if (headspazType)
            {
                SpazHeadPosition();
                if (Time.time > headspazDelay)
                {
                    headspazType = false;
                    headspazDelay = Time.time + Random.Range(1000f, 4000f) / 1000f;
                }
            }
            else
            {
                FixHeadPosition();
                if (Time.time > headspazDelay)
                {
                    headspazType = true;
                    headspazDelay = Time.time + Random.Range(200f, 1000f) / 1000f;
                }
            }
        }
        public static void LaggyRig()
        {
            References.LocalRig.enabled = false;
            ghostException = true;
            if (Time.time > laggyRigDelay)
            {
                References.LocalRig.enabled = true;
                References.LocalRig.PostTick();
                References.LocalRig.enabled = false;

                laggyRigDelay = Time.time + 0.5f;
            }
        }
        public static void UpdateRig()
        {
            References.LocalRig.enabled = false;
            ghostException = true;
            if (rightPrimary && !wasRightPrimaryPressed)
            {
                References.LocalRig.enabled = true;
                References.LocalRig.PostTick();
                References.LocalRig.enabled = false;
            }

            wasRightPrimaryPressed = rightPrimary;
        }
        public static void SpinHead(string axis)
        {
            if (References.LocalRig.enabled)
            {
                switch (axis.ToLower())
                {
                    case "x":
                        References.LocalRig.head.trackingRotationOffset.x += Fun.headSpinSpeed;
                        break;
                    case "y":
                        References.LocalRig.head.trackingRotationOffset.y += Fun.headSpinSpeed;
                        break;
                    case "z":
                        References.LocalRig.head.trackingRotationOffset.z += Fun.headSpinSpeed;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (axis.ToLower())
                {
                    case "x":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(Fun.headSpinSpeed, 0f, 0f));
                        break;
                    case "y":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(0f, Fun.headSpinSpeed, 0f));
                        break;
                    case "z":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(0f, 0f, Fun.headSpinSpeed));
                        break;
                    default:
                        return;
                }
            }
        }
        public static void FixHead()
        {
            References.LocalRig.head.trackingRotationOffset.x = 0f;
            References.LocalRig.head.trackingRotationOffset.y = 0f;
            References.LocalRig.head.trackingRotationOffset.z = 0f;
        }
        public static void SpazHead(string axis)
        {
            int offset = Random.Range(0, 360);
            switch (axis.ToLower())
            {
                case "x":
                    References.LocalRig.head.trackingRotationOffset.x = offset;
                    break;
                case "y":
                    References.LocalRig.head.trackingRotationOffset.y = offset;
                    break;
                case "z":
                    References.LocalRig.head.trackingRotationOffset.z = offset;
                    break;
                default:
                    return;
            }
        }

        public static float headspazDelay;
        public static bool headspazType;
        private static Vector3 headoffs = Vector3.zero;
        public static float laggyRigDelay;
        public static bool wasRightPrimaryPressed;
        private static void ChaseWeepingAngelPrey(VRRig prey)
        {
            Transform rig = References.LocalRig.transform;
            Vector3 goal = prey.transform.position;
            float distance = Vector3.Distance(rig.position, goal);
            if (distance > 1f)
                rig.position = Vector3.MoveTowards(rig.position, goal, Mathf.Min(Movement.FlySpeed / 2f * Time.deltaTime, distance - 1f));

            Vector3 facing = goal - rig.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f) rig.rotation = Quaternion.LookRotation(facing);

            References.LocalRig.head.rigTarget.transform.rotation = rig.rotation;
            References.LocalRig.leftHand.rigTarget.transform.position = rig.position + rig.forward * 0.5f - rig.right * 0.3f + rig.up * 0.2f;
            References.LocalRig.rightHand.rigTarget.transform.position = rig.position + rig.forward * 0.5f + rig.right * 0.3f + rig.up * 0.2f;
            References.LocalRig.leftHand.rigTarget.transform.rotation = rig.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = rig.rotation;
            FixRigHandRotation();
        }
        private static bool IsWatching(VRRig rig)
        {
            Transform eyes = rig.headMesh.transform;
            Vector3 head = References.LocalRig.headMesh.transform.position;
            Vector3 direction = head - eyes.position;
            if (Vector3.Angle(eyes.forward, direction) > 45f) return false;

            Vector3 offset = direction.normalized * Mathf.Min(0.25f, direction.magnitude * 0.25f);
            return !Physics.Linecast(eyes.position + offset, head - offset, GTPlayer.LocomotionEnabledLayers);
        }
        public static void FixRigHandRotation()
        {
            References.LocalRig.leftHand.rigTarget.transform.rotation *= Quaternion.Euler(References.LocalRig.leftHand.trackingRotationOffset);
            References.LocalRig.rightHand.rigTarget.transform.rotation *= Quaternion.Euler(References.LocalRig.rightHand.trackingRotationOffset);
        }
        public static Vector3 offsetLH = Vector3.zero;
        public static Vector3 offsetRH = Vector3.zero;
        public static Vector3 offsetH = Vector3.zero;
        private static Vector3 headPos = Vector3.zero;
        private static Vector3 headRot = Vector3.zero;
        private static Vector3 handPos_L = Vector3.zero;
        private static Vector3 handRot_L = Vector3.zero;
        private static Vector3 handPos_R = Vector3.zero;
        private static Vector3 handRot_R = Vector3.zero;
        private static bool hasAdded;
        public static float beesDelay;
        private static Quaternion? vrrigJoystickRot;
        private static Quaternion? bodyJoystickRot;
        static bool flipping;
        static float flipStart;
        static Quaternion flipFrom;
        static Vector3 flipAxis;
        const float flipDuration = 1f;
        public static void Rotate(Quaternion rot) =>
            References.LocalRig.transform.rotation = rot;
        public static void RigPose(float pitch, float roll) =>
            References.HeadCollider.transform.rotation = Quaternion.Euler(pitch, References.HeadCollider.transform.rotation.eulerAngles.y, roll);

        public static GameObject recBodyRotary;

    }
}

