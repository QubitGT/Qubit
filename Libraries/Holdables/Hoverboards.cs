/*
 * Qubit Menu  Libraries/Holdables/Hoverboards.cs
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
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using System;
using System.Collections;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Fun;
using static Qubit.Modules.Categories.Projectile;
using static Qubit.Modules.Classes.GunLibrary;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Holdables
{
    public static class Hoverboards
    {
        public static Coroutine DisableHoverboardCoroutine;
        // ReSharper disable once ParameterHidesMember
        public static void DropBoard(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 avelocity, Color boardColor, Vector3? globalScale = null)
        {
            if (Vector3.Distance(References.BodyCollider.transform.position, position) > 5f)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = position + Vector3.down * 4f;

                if (dropBoard != null)
                    Coroutines.instance.StopCoroutine(dropBoard);

                dropBoard = Coroutines.instance.StartCoroutine(EnableRig());
            }

            ManagerRegistry.FreeHoverboardManager.SendDropBoardRPC(position, rotation, velocity, avelocity, boardColor, globalScale ?? Vector3.one);
            Security.RPCProtection();
        }
        public static void HoverboardGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                hoverboardGunDelay = Time.time + 0.25f;
                DropBoard(NewPointer.transform.position + Vector3.up, RandomQuaternion(), Vector3.zero, Vector3.zero, RandomColor());
            }, new GunLibrary.GunSettings { when = () => Time.time > hoverboardGunDelay });
        }

        public static void FastHoverboard()
        {
            References.Player.hoverboardPaddleBoostMax = float.MaxValue;
            References.Player.hoverboardPaddleBoostMultiplier = 5f;
            References.Player.hoverboardBoostGracePeriod = 0f;
            References.Player.hoverTiltAdjustsForwardFactor = 1f;
        }

        public static void SlowHoverboard()
        {
            References.Player.hoverboardPaddleBoostMax = 3.5f;
            References.Player.hoverboardPaddleBoostMultiplier = 0.025f;
            References.Player.hoverboardBoostGracePeriod = 3f;
            References.Player.hoverTiltAdjustsForwardFactor = 0.1f;
        }

        public static void FixHoverboard()
        {
            References.Player.hoverboardPaddleBoostMax = 10f;
            References.Player.hoverboardPaddleBoostMultiplier = 0.1f;
            References.Player.hoverboardBoostGracePeriod = 1f;
            References.Player.hoverTiltAdjustsForwardFactor = 0.2f;
        }
        public static IEnumerator DisableHoverboard()
        {
            yield return new WaitForSeconds(0.3f);

            References.Player.SetHoverActive(false);
            References.LocalRig.hoverboardVisual.SetNotHeld();
        }

        public static void HoverboardScreenTarget(VRRig rig, Color color)
        {
            if (DisableHoverboardCoroutine != null)
                Coroutines.instance.StopCoroutine(DisableHoverboardCoroutine);

            DisableHoverboardCoroutine = Coroutines.instance.StartCoroutine(DisableHoverboard());

            Vector3 angVel = rig.headMesh.GetOrAddComponent<GorillaVelocityEstimator>().angularVelocity;

            Vector3 HoverboardPos = rig.headMesh.transform.TransformPoint(-0.3f, 0.1f, 0.3725f) + rig.LatestVelocity() * 0.5f;
            Quaternion HoverboardRotation = rig.headMesh.transform.rotation * Quaternion.Euler(angVel * (Mathf.Rad2Deg * 0.1f)) * Quaternion.Euler(0f, 90f, 270f);

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = HoverboardPos - Vector3.up * 0.5f;

            References.Player.isHoverAllowed = true;

            HoverboardVisual hoverboardVisual = References.LocalRig.hoverboardVisual;

            hoverboardVisual.SetIsHeld(true, hoverboardVisual.NominalParentTransform.InverseTransformPoint(HoverboardPos), hoverboardVisual.NominalParentTransform.InverseTransformRotation(HoverboardRotation), color);
            References.Player.SetHoverActive(false);

            hoverboardVisual.interpolatedLocalPosition = hoverboardVisual.NominalLocalPosition;
            hoverboardVisual.interpolatedLocalRotation = hoverboardVisual.NominalLocalRotation;

            References.Player.SetHoverboardPosRot(HoverboardPos, HoverboardRotation);
        }

        public static void HoverboardScreenGun(Color color)
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    HoverboardScreenTarget(lockTarget, color);

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

        public static void HoverboardScreenAll(Color color)
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                if (Qubit.Core.Networking.InRoom)
                {
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 archivePos = References.LocalRig.transform.position;

                    foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                    {
                        HoverboardScreenTarget(GetVRRigFromPlayer(Player), color);
                        SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                    }

                    Security.RPCProtection();

                    References.LocalRig.enabled = true;

                    References.LocalRig.transform.position = archivePos;

                    return false;
                }

                return true;
            };
        }

        public static void SpawnHoverboard()
        {
            DropBoard(References.LocalRig.transform.position, References.LocalRig.transform.rotation, Vector3.zero, Vector3.zero, RandomColor());
            References.Player.isHoverAllowed = true;
        }
        public static void HoverboardSpam()
        {
            if (rightGrab && Time.time > hoverboardSpamDelay)
            {
                hoverboardSpamDelay = Time.time + 0.5f;

                DropBoard(References.RightHand.position, References.RightHand.rotation, GetGunDirection(References.RightHand) * ShootStrength, Vector3.zero, RandomColor());
            }
        }

        public static void OrbitHoverboards()
        {
            if (Time.time > hoverboardSpamDelay)
            {
                hoverboardSpamDelay = Time.time + 0.25f;

                float offset = 0f;
                Vector3 vector3 = new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 2f, 1f, MathF.Sin(offset + (float)Time.frameCount / 30) * 2f);

                offset = -25f;
                Vector3 position2 = new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 2f, 1f, MathF.Sin(offset + (float)Time.frameCount / 30) * 2f);

                DropBoard(References.HeadCollider.transform.position + vector3, Quaternion.Euler((References.HeadCollider.transform.position - vector3).normalized), (position2 - vector3).normalized * 6.5f, new Vector3(0f, 360f, 0f), RandomColor());

                offset = 180f;
                vector3 = new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 2f, 1f, MathF.Sin(offset + (float)Time.frameCount / 30) * 2f);

                offset = 155f;
                position2 = new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 2f, 1f, MathF.Sin(offset + (float)Time.frameCount / 30) * 2f);

                DropBoard(References.HeadCollider.transform.position + vector3, Quaternion.Euler((References.HeadCollider.transform.position - vector3).normalized), (position2 - vector3).normalized * 6.5f, new Vector3(0f, 360f, 0f), RandomColor());
            }
        }

        public static void RainbowHoverboard()
        {
            if (References.LocalRig.hoverboardVisual != null && References.LocalRig.hoverboardVisual.IsHeld)
            {
                float h = Time.frameCount / 180f % 1f;
                Color rgbColor = Color.HSVToRGB(h, 1f, 1f);
                References.LocalRig.hoverboardVisual.SetIsHeld(References.LocalRig.hoverboardVisual.IsLeftHanded, References.LocalRig.hoverboardVisual.NominalLocalPosition, References.LocalRig.hoverboardVisual.NominalLocalRotation, rgbColor);
            }
        }
        public static void StrobeHoverboard()
        {
            if (References.LocalRig.hoverboardVisual != null && References.LocalRig.hoverboardVisual.IsHeld)
            {
                if (Time.time > flashDelay)
                {
                    flashDelay = Time.time + 0.1f;
                    flashColor = !flashColor;
                }

                Color rgbColor = flashColor ? Color.white : Color.black;
                References.LocalRig.hoverboardVisual.SetIsHeld(References.LocalRig.hoverboardVisual.IsLeftHanded, References.LocalRig.hoverboardVisual.NominalLocalPosition, References.LocalRig.hoverboardVisual.NominalLocalRotation, rgbColor);
            }
        }

        public static void RandomHoverboard()
        {
            if (References.LocalRig.hoverboardVisual != null && References.LocalRig.hoverboardVisual.IsHeld)
                References.LocalRig.hoverboardVisual.SetIsHeld(References.LocalRig.hoverboardVisual.IsLeftHanded, References.LocalRig.hoverboardVisual.NominalLocalPosition, References.LocalRig.hoverboardVisual.NominalLocalRotation, RandomColor());
        }

        public static void SpazHoverboard()
        {
            if (References.LocalRig.hoverboardVisual != null && References.LocalRig.hoverboardVisual.IsHeld)
                References.LocalRig.hoverboardVisual.SetIsHeld(References.LocalRig.hoverboardVisual.IsLeftHanded, References.LocalRig.hoverboardVisual.NominalLocalPosition, RandomQuaternion(), RandomColor());
        }
        public static void HoverboardAura()
        {
            if (Time.time > hoverboardAuraDelay)
            {
                hoverboardAuraDelay = Time.time + 0.25f;

                for (int i = 0; i < 2; i++)
                    DropBoard(References.HeadCollider.transform.position + RandomVector3(), RandomQuaternion(), RandomVector3() * 20f, RandomVector3() * 20f, RandomColor());
            }
        }

        public static void BecomeHoverboard()
        {
            Vector3 HoverboardPos = References.BodyCollider.transform.position;
            Quaternion HoverboardRotation = References.HeadCollider.transform.rotation;

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = HoverboardPos - Vector3.up * 1f;

            References.Player.isHoverAllowed = true;
            References.Player.SetHoverActive(true);

            HoverboardVisual hoverboardVisual = References.LocalRig.hoverboardVisual;

            hoverboardVisual.SetIsHeld(true, hoverboardVisual.NominalParentTransform.InverseTransformPoint(HoverboardPos), hoverboardVisual.NominalParentTransform.InverseTransformRotation(HoverboardRotation), References.LocalRig.playerColor);

            hoverboardVisual.interpolatedLocalPosition = hoverboardVisual.NominalLocalPosition;
            hoverboardVisual.interpolatedLocalRotation = hoverboardVisual.NominalLocalRotation;

            References.Player.SetHoverboardPosRot(HoverboardPos, HoverboardRotation);
        }

        public static float hoverboardSpawnDelay;
    }
}
