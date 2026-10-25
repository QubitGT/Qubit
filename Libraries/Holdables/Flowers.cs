/*
 * Qubit Menu  Libraries/Holdables/Flowers.cs
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
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Modules.Categories.Fun;

namespace Qubit.Libraries.Holdables
{
    public static class Flowers
    {
        public static ThrowableSetDressing GetFlowerObject(string objectName)
        {
            GameObject flowerObject = GameObject.Find(objectName);
            if (flowerObject == null)
                return null;

            return flowerObject.GetComponent<ThrowableSetDressing>();
        }

        public static ThrowableSetDressing GetFlower(string objectName)
        {
            ThrowableSetDressing flower = GetFlowerObject(objectName);
            if (flower == null)
                return null;

            if (!Qubit.Core.Networking.InRoom)
                return flower;

            RequestableOwnershipGuard guard = flower.worldShareableInstance.guard;
            if (guard == null)
                return null;

            if (!flower.IsMyItem())
            {
                if (flower.currentState != TransferrableObject.PositionState.Dropped && flower.currentState != TransferrableObject.PositionState.None)
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

            flower.worldShareableInstance.transferableObjectState = TransferrableObject.PositionState.Dropped;
            return flower;
        }

        public static void PermanentOwnership(string objectName)
        {
            Ownership.enabled = true;

            ThrowableSetDressing flowerObject = GetFlowerObject(objectName);
            if (flowerObject == null)
                return;

            if (!Qubit.Core.Networking.InRoom)
            {
                Ownership.blacklistedGuards.Clear();
                return;
            }

            if (!flowerObject.IsMyItem())
                return;

            if (flowerObject.targetRig != References.LocalRig)
                flowerObject.SetTargetRig(References.LocalRig);

            if (!Ownership.blacklistedGuards.Contains(flowerObject.worldShareableInstance.guard))
                Ownership.blacklistedGuards.Add(flowerObject.worldShareableInstance.guard);
        }

        public static void StealObject(string objectName)
        {
            ThrowableSetDressing flowerObject = GetFlowerObject(objectName);
            if (flowerObject != null && !flowerObject.IsMyItem())
                flowerObject.WorldShareableRequestOwnership();
        }

        public static void HolsterObject(string objectName, TransferrableObject.PositionState state)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (flower != null && (flower.currentState == TransferrableObject.PositionState.Dropped || flower.currentState == TransferrableObject.PositionState.None))
                flower.currentState = state;
        }

        public static void SetRespawnDistance(string objectName, float respawnDistance = float.MaxValue)
        {
            ThrowableSetDressing flowerObject = GetFlowerObject(objectName);
            if (flowerObject == null)
                return;

            flowerObject.maxDistanceFromOriginBeforeRespawn = respawnDistance;
            flowerObject.maxDistanceFromTargetPlayerBeforeRespawn = respawnDistance;
        }

        public static Dictionary<string, float> objectSizeScale = new Dictionary<string, float>();
        public static void ObjectSizeChanger(string objectName)
        {
            ThrowableSetDressing flower = GetFlowerObject(objectName);
            if (flower == null) return;

            objectSizeScale.TryGetValue(objectName, out float scale);
            if (scale <= 0f) scale = 1f;

            if (rightTrigger > 0.5f)
                scale += 0.1f;
            if (rightGrab)
                scale -= 0.1f;

            scale = Mathf.Max(scale, 0.05f);
            objectSizeScale[objectName] = scale;

            flower.transform.localScale = Vector3.one * scale;
        }

        public static void ObjectToHand(string objectName)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (rightGrab && flower != null)
            {
                flower.transform.position = References.RightHand.position;
                flower.transform.rotation = References.RightHand.rotation;
            }
        }

        public static void ObjectToPointGun(string objectName)
        {
            GunLibrary.Point(NewPointer =>
            {
                ThrowableSetDressing flower = GetFlower(objectName);
                if (flower != null)
                    flower.transform.position = NewPointer.transform.position + Vector3.up;
            });
        }

        public static void DestroyObject(string objectName)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (flower != null)
                flower.transform.position = new Vector3(99999f, 99999f, 99999f);
        }

        public static void SpazObject(string objectName)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (flower != null)
                flower.transform.rotation = RandomQuaternion();
        }

        public static void OrbitObject(string objectName, float offset = 0)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (flower != null)
                flower.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30), 1, MathF.Sin(offset + (float)Time.frameCount / 30));
        }

        public static void ObjectAura(string objectName)
        {
            ThrowableSetDressing flower = GetFlower(objectName);
            if (flower != null)
            {
                flower.transform.position = References.HeadCollider.transform.position + RandomVector3();
                flower.transform.rotation = RandomQuaternion();
            }
        }

        public static void RideObject(string objectName)
        {
            ThrowableSetDressing flowerObject = GetFlowerObject(objectName);
            if (flowerObject == null)
                return;

            TeleportPlayer(flowerObject.transform.position);
            References.Rigidbody.linearVelocity = Vector3.zero;
        }

        private static readonly Dictionary<string, bool> lastWasNull = new Dictionary<string, bool>();
        public static void BecomeObject(string objectName)
        {
            ThrowableSetDressing flower = GetFlower(objectName);

            if (flower != null)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

                flower.transform.position = References.BodyCollider.transform.position;
                flower.transform.rotation = References.HeadCollider.transform.rotation;
            }
            else
            {
                lastWasNull.TryGetValue(objectName, out bool wasNull);
                if (wasNull)
                    References.LocalRig.enabled = true;
            }

            lastWasNull[objectName] = flower != null;
        }
    }
}
