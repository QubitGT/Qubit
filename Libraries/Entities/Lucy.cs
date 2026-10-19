/*
 * Qubit Menu  Libraries/Entities/Lucy.cs
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
using Qubit.Libraries.Interface;
using Qubit.Modules.Categories;
using Qubit.Patches.Events;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Modules.Classes.GunLibrary;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// Mods that control Lucy, the Halloween ghost chaser.
    /// Most of them need the master client, because only the owner of the ghost can change its state for everyone.
    /// </summary>
    public static class Lucy
    {
        private const float PlayerGrabRange = 25f;

        // Original values, kept while a mod overrides them so disabling the mod can put them back.
        private static float? originalMinGrabCooldown;
        private static float? originalMaxChaseTime;
        private static float? originalCatchDistance;
        private static float? originalVelocityStep;
        private static float? originalVelocityIncreaseTime;
        private static float? originalSpeed;
        private static float? originalGrabSpeed;
        private static float? originalGrabDuration;

        private static float nextRainTime;
        private static float nextGrabTime;
        private static float nextBodyguardTime;
        private static float nextSiegeTime;

        /// <summary>
        /// Returns Lucy if the local player controls her, otherwise tells the player why it can't be done.
        /// </summary>
        private static HalloweenGhostChaser GetControlledChaser()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null)
                return null;

            if (chaser.IsMine)
                return chaser;

            Notifications.SendFormat("red", "ERROR", "You are not master client.");
            return null;
        }

        /// <summary>
        /// Makes Lucy walk towards the transform and treat the local player as the one she is chasing.
        /// </summary>
        private static void FollowLocally(HalloweenGhostChaser chaser, Transform target)
        {
            chaser.currentState = HalloweenGhostChaser.ChaseState.Chasing;
            chaser.targetPlayer = Qubit.Core.Networking.LocalPlayer;
            chaser.followTarget = target;
        }

        public static void EnableRestless()
        {
            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            originalMinGrabCooldown ??= chaser.minGrabCooldown;
            originalMaxChaseTime ??= chaser.maxNextTimeToChasePlayer;

            chaser.minGrabCooldown = 0f;
            chaser.maxNextTimeToChasePlayer = 5f;
        }

        public static void DisableRestless()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || originalMinGrabCooldown == null)
                return;

            chaser.minGrabCooldown = originalMinGrabCooldown.Value;
            chaser.maxNextTimeToChasePlayer = originalMaxChaseTime.Value;
            originalMinGrabCooldown = originalMaxChaseTime = null;
        }

        public static void EnablePet()
        {
            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            originalCatchDistance ??= chaser.catchDistance;
            chaser.catchDistance = 0f; // she can never catch you

            FollowLocally(chaser, References.LocalRig.transform);
        }

        public static void DisablePet()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || !chaser.IsMine)
                return;

            if (originalCatchDistance != null)
            {
                chaser.catchDistance = originalCatchDistance.Value;
                originalCatchDistance = null;
            }

            chaser.currentState = HalloweenGhostChaser.ChaseState.Dormant;
            chaser.targetPlayer = null;
        }

        public static void SteerGun()
        {
            Point(pointer =>
            {
                HalloweenGhostChaser chaser = GetControlledChaser();
                if (chaser != null)
                    FollowLocally(chaser, pointer.transform);
            });
        }

        public static void Rain()
        {
            if (Time.time < nextRainTime)
                return;

            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null || chaser.spawnTransforms == null || chaser.spawnTransforms.Length == 0)
                return;

            nextRainTime = Time.time + 0.25f;

            chaser.spawnIndex = Random.Range(0, chaser.spawnTransforms.Length);
            chaser.timeGongStarted = Time.time;
            chaser.currentState = HalloweenGhostChaser.ChaseState.Gong;
            chaser.isSummoned = Random.value > 0.5f;
        }

        public static void Banish()
        {
            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null || chaser.spawnTransforms == null || chaser.spawnTransforms.Length == 0)
                return;

            chaser.currentState = HalloweenGhostChaser.ChaseState.Dormant;
            chaser.transform.position = chaser.spawnTransforms[0].position - Vector3.up * 50f;
            chaser.ghostBody.SetActive(false);
            chaser.isSummoned = false;
        }

        public static void EnableHyper()
        {
            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            originalVelocityStep ??= chaser.velocityStep;
            originalVelocityIncreaseTime ??= chaser.velocityIncreaseTime;

            chaser.velocityStep = 10f;
            chaser.velocityIncreaseTime = 2f;
        }

        public static void DisableHyper()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || originalVelocityStep == null || !chaser.IsMine)
                return;

            chaser.velocityStep = originalVelocityStep.Value;
            chaser.velocityIncreaseTime = originalVelocityIncreaseTime.Value;
            originalVelocityStep = originalVelocityIncreaseTime = null;
        }

        public static void RainbowColor()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser != null)
                chaser.ghostMaterial.color = Color.HSVToRGB(Time.time % 2f / 2f, 1f, 1f);
        }

        public static void ResetColor()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser != null)
                chaser.ghostMaterial.color = chaser.isSummoned ? chaser.summonedColor : chaser.defaultColor;
        }

        public static void EnableCarry()
        {
            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            originalSpeed ??= chaser.currentSpeed;
            chaser.currentSpeed = 0f;

            FollowLocally(chaser, References.LocalRig.transform);
        }

        public static void Carry()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || !chaser.IsMine)
                return;

            chaser.transform.position = References.Tagger.rightHandTransform.position;
            chaser.transform.rotation = References.Tagger.rightHandTransform.rotation;
        }

        public static void DisableCarry()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || originalSpeed == null || !chaser.IsMine)
                return;

            chaser.currentSpeed = originalSpeed.Value;
            originalSpeed = null;
            chaser.currentState = HalloweenGhostChaser.ChaseState.Dormant;
        }

        public static void EnableFlight()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null)
                return;

            originalGrabSpeed ??= chaser.grabSpeed;
            originalGrabDuration ??= chaser.grabDuration;

            chaser.grabSpeed = 5f;
            chaser.grabDuration = 99999f;
            chaser.minGrabCooldown = 0.5f;

            LucySync.enabled = true; // the master client would otherwise overwrite the local grab
        }

        public static void Flight()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null)
                return;

            chaser.grabTime = Time.time;
            chaser.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            chaser.targetPlayer = Qubit.Core.Networking.LocalPlayer;
            chaser.followTarget = References.LocalRig.transform;
        }

        public static void DisableFlight()
        {
            HalloweenGhostChaser chaser = Overpowered.Lucy;
            if (chaser == null || originalGrabSpeed == null)
                return;

            chaser.grabSpeed = originalGrabSpeed.Value;
            chaser.grabDuration = originalGrabDuration.Value;
            originalGrabSpeed = originalGrabDuration = null;

            LucySync.enabled = false;

            chaser.targetPlayer = null;
            chaser.currentState = HalloweenGhostChaser.ChaseState.Dormant;
        }

        public static void GrabGun()
        {
            Target(rig =>
            {
                if (Time.time < nextGrabTime)
                    return;

                HalloweenGhostChaser chaser = GetControlledChaser();
                if (chaser == null)
                    return;

                nextGrabTime = Time.time + 1f;

                chaser.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
                chaser.grabTime = Time.time;
                chaser.targetPlayer = rig.GetPlayer();
                chaser.followTarget = rig.transform;
            }, new GunSettings { locks = true });
        }

        public static void Bodyguard()
        {
            if (Time.time < nextBodyguardTime)
                return;

            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            nextBodyguardTime = Time.time + 1f;

            foreach (VRRig rig in ActiveRigs)
            {
                if (rig.IsLocal() || chaser.targetPlayer == rig.GetPlayer())
                    continue;

                if ((rig.transform.position - References.LocalRig.transform.position).magnitude >= PlayerGrabRange)
                    continue;

                FollowRig(chaser, rig);
                break;
            }
        }

        private static void FollowRig(HalloweenGhostChaser chaser, VRRig rig)
        {
            chaser.currentState = HalloweenGhostChaser.ChaseState.Chasing;
            chaser.targetPlayer = rig.GetPlayer();
            chaser.followTarget = rig.transform;
        }

        public static void Siege()
        {
            if (Time.time < nextSiegeTime)
                return;

            HalloweenGhostChaser chaser = GetControlledChaser();
            if (chaser == null)
                return;

            nextSiegeTime = Time.time + 2f;

            chaser.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            chaser.grabTime = Time.time;
            chaser.targetPlayer = GetRandomPlayer();
        }
    }
}
