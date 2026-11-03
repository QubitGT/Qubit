/*
 * Qubit Menu  Libraries/Entities/MinesSkeleton.cs
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
using Qubit.Modules.Classes;
using UnityEngine;
using static Qubit.Extensions.GameObj;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// Mods that control the Second Look Skeleton, the ghost inside the mines maze.
    /// Triggering a catch on a player whose skeleton urn is opened quits their game.
    /// Most of these need the master client, because only the owner of the skeleton can change its state.
    /// </summary>
    public static class MinesSkeleton
    {
        private const string SkeletonPath = "Environment Objects/05Maze_PersistentObjects/MinesSecondLookSkeleton";

        private static SecondLookSkeleton _skeleton;
        private static float nextSearchTime;

        /// <summary>
        /// Returns the mines skeleton if it exists in the current map, otherwise null.
        /// </summary>
        public static SecondLookSkeleton Skeleton
        {
            get
            {
                if ((_skeleton == null || _skeleton.gameObject == null) && Time.time > nextSearchTime)
                {
                    nextSearchTime = Time.time + 5f;

                    GameObject skeletonObject = GetObject(SkeletonPath);
                    if (skeletonObject != null)
                        _skeleton = skeletonObject.GetComponent<SecondLookSkeleton>();
                }

                return _skeleton;
            }
        }

        /// <summary>
        /// Returns the skeleton if the local player controls it, otherwise tells the player why it can't be done.
        /// </summary>
        private static SecondLookSkeleton GetControlledSkeleton()
        {
            SecondLookSkeleton skeleton = Skeleton;
            if (skeleton == null)
            {
                Notifications.SendFormat("red", "ERROR", "The mines skeleton was not found.");
                return null;
            }

            if (skeleton.GetComponent<SecondLookSkeletonSynchValues>().IsMine)
                return skeleton;

            Notifications.SendFormat("red", "ERROR", "You are not master client.");
            return null;
        }

        /// <summary>
        /// Core skeleton control,'s Skel method.
        /// Passing a rig catches that player with the skeleton; passing a bool activates or resets the skeleton.
        /// </summary>
        public static void Catch(object o)
        {
            SecondLookSkeleton skeleton = GetControlledSkeleton();
            if (skeleton == null)
                return;

            if (o is VRRig rig)
            {
                if (skeleton.currentState == SecondLookSkeleton.GhostState.Unactivated)
                {
                    skeleton.tapped = true;
                    skeleton.RemoteActivateGhost();
                }
                else if (skeleton.currentState == SecondLookSkeleton.GhostState.Patrolling)
                {
                    skeleton.playersSeen.Clear();
                    skeleton.RemotePlayerSeen(rig.Creator);
                    skeleton.RemotePlayerCaught(rig.Creator);
                    skeleton.spookyGhost.transform.position = rig.transform.position;
                }
            }
            else if (o is bool activate)
            {
                skeleton.tapped = true;
                if (activate)
                    skeleton.RemoteActivateGhost();
                else
                    skeleton.ChangeState(SecondLookSkeleton.GhostState.Unactivated);
            }
        }

        /// <summary>
        /// Forcefully takes ownership of the skeleton's network view so it can be controlled next press.
        /// </summary>
        public static void RequestOwnership()
        {
            SecondLookSkeleton skeleton = Skeleton;
            if (skeleton != null)
                skeleton.GetComponent<SecondLookSkeletonSynchValues>().RequestOwnership();
        }

        /// <summary>
        /// App Quit Gun. Catching a player with the skeleton force quits their game if their skeleton urn is opened.
        /// </summary>
        public static void AppQuitGun() =>
            GunLibrary.Target(gunTarget => Catch(gunTarget), new GunLibrary.GunSettings
            {
                // The gun needs the skeleton owned by us; grab ownership every frame we're not master so the next shot lands.
                onIdle = RequestOwnership
            });
    }
}
