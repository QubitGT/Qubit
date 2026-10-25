/*
 * Qubit Menu  Modules/Categories/Rope.cs
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
using GorillaLocomotion.Gameplay;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Modules.Classes;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Rope
    {
        public static void JoystickRopeControl()
        {
            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;

            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            if (stick.sqrMagnitude > 0.0025f && Time.time > RopeDelay)
            {
                RopeDelay = Time.time + 0.125f;

                GorillaRopeSwing rope = GetRandomType<GorillaRopeSwing>(0.25f);
                SetRopeVelocity(rope, new Vector3(stick.x * 100f, stick.y * 100f, 0f));
            }
        }
        public static void SpazRopeGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    GorillaRopeSwing gunTarget = Ray.collider.GetComponentInParent<GorillaRopeSwing>();
                    if (gunTarget && Time.time > RopeDelay)
                    {
                        RopeDelay = Time.time + 0.25f;
                        SetRopeVelocity(gunTarget, RandomVector3(100f));
                    }
                }
            });
        }
        public static void SpazAllRopes()
        {
            if (rightTrigger > 0.5f && Time.time > RopeDelay)
            {
                RopeDelay = Time.time + 0.125f;

                GorillaRopeSwing rope = GetRandomRope();
                SetRopeVelocity(rope, RandomVector3(100f));
            }
        }
        public static void SpazGrabbedRopes()
        {
            if (Time.time > RopeDelay)
            {
                RopeDelay = Time.time + 0.125f;
                VRRig randomRig = Players.ActiveRigs
                    .Where(rig => rig.currentRopeSwing != null)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();

                if (randomRig != null)
                    SetRopeVelocity(randomRig.currentRopeSwing, RandomVector3(100f));
            }
        }
        public static void FlingRopeGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    GorillaRopeSwing gunTarget = Ray.collider.GetComponentInParent<GorillaRopeSwing>();

                    if (gunTarget && Time.time > RopeDelay)
                    {
                        RopeDelay = Time.time + 0.125f;
                        SetRopeVelocity(gunTarget, RandomVector3(100f));
                    }
                }
            });
        }
        public static void FlingAllRopesGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                RopeDelay = Time.time + 0.125f;

                GorillaRopeSwing rope = GetRandomRope();
                SetRopeVelocity(rope, (NewPointer.transform.position - rope.transform.position).normalized * 100f);
            }, new GunLibrary.GunSettings { when = () => Time.time > RopeDelay });
        }
        public static void RopeGrabReach()
        {
            foreach (GorillaHandClimber climber in new[] { ManagerRegistry.EquipmentInteractor.LeftClimber, ManagerRegistry.EquipmentInteractor.rightClimber })
                (climber.col as SphereCollider).radius = 0.5f;
        }
        private static float randomRopeDelay;
        private static float RopeDelay;
        public static void SetRopeVelocity(int RopeId, Vector3 Velocity)
        {
            Velocity = Velocity.ClampMagnitudeSafe(15f);

            if (ManagerRegistry.RopeSwingManager.ropes.TryGetValue(RopeId, out GorillaRopeSwing Rope))
            {
                var ClosestNode = Rope.nodes
                    .Skip(1)
                    .Select((v, i) => new
                    {
                        index = i,
                        transform = v,
                        distance = Vector3.Distance(References.BodyCollider.transform.position, v.transform.position)
                    })
                    .OrderBy(x => x.distance)
                    .First();

                if (ClosestNode.distance > 5f)
                {
                    if (RopeCoroutine != null)
                        Coroutines.instance.StopCoroutine(RopeCoroutine);

                    RopeCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());

                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = ClosestNode.transform.position;
                }

                if (Vector3.Distance(ServerPos, ClosestNode.transform.position) < 5f)
                    ManagerRegistry.RopeSwingManager.SendSetVelocity_RPC(RopeId, ClosestNode.index, Velocity, true);
                else
                    RopeDelay = 0f;

                RPCProtection();
            }
        }
        public static void SetRopeVelocity(GorillaRopeSwing Rope, Vector3 Velocity) =>
            SetRopeVelocity(ManagerRegistry.RopeSwingManager.ropes.FirstOrDefault(x => x.Value == Rope).Key, Velocity);
        public static GorillaRopeSwing GetRandomRope()
        {
            if (Time.time > randomRopeDelay)
            {
                randomRopeDelay = Time.time + 0.5f;
                randomRope = ManagerRegistry.RopeSwingManager.ropes.Values.OrderBy(_ => Random.value).FirstOrDefault();
            }
            return randomRope;
        }

        public static Coroutine RopeCoroutine;
        private static GorillaRopeSwing randomRope;
    }
}
