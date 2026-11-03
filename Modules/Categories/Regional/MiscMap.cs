/*
 * Qubit Menu  Modules/Categories/Regional/MiscMap.cs
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

using GorillaTag.Gravity;
using GorillaTagScripts.ScavengerHunt;
using System.Linq;
using Voxels;
using Photon.Realtime;
using Photon.Pun;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Modules.Classes;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories.Regional
{
    public static class MiscMap
    {
        private static float tapDelay;
        private static float buttonDelay;

        public static void ToggleSpaceGravity(bool on) =>
            GetObject("SpaceMap/Space_Landing_V3/GravityZone_Spherical").SetActive(on);
        public static void TogglePlanetZones(bool on)
        {
            foreach (var zone in GetAllType<PlanetZone>())
                zone.enabled = on;
        }
        public static void RemoveForestColliders()
        {
            Transform ForestCollisions = GetObject("Environment Objects/LocalObjects_Prefab/ForestToHoverboard/TurnOnInForestAndHoverboard/ForestDome_CollisionOnly").transform;

            if (ForestCollisions == null)
                return;

            for (int i = 2; i < 4; i++)
            {
                GameObject c = ForestCollisions.transform.GetChild(i).gameObject;
                c.SetActive(false);
                Movement.forestColliders.Add(c);
            }
        }
        public static void RestoreForestColliders()
        {
            foreach (GameObject c in Movement.forestColliders)
                c.SetActive(true);

            Movement.forestColliders.Clear();
        }
        public static void TapAllClass<T>() where T : Tappable
        {
            if (rightGrab)
            {
                if (Time.time > tapDelay)
                {
                    foreach (Tappable TappableObject in GetAllType<T>())
                        TappableObject.OnTap(1f);

                    Security.RPCProtection();
                    tapDelay = Time.time + 0.1f;
                }
            }
        }
        public static void TriggerLeafPileGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : -10f));
                    SendSerialize(References.LocalRig.GetPhotonView());

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : 0f));
                }
                else
                    References.LocalRig.enabled = true;
            });
        }
        public static void ActivateAllDoors()
        {
            if (rightGrab)
            {
                if (Time.time > buttonDelay)
                {
                    foreach (GhostLabButton button in GetAllType<GhostLabButton>())
                    {
                        button.ButtonActivation();
                        Security.RPCProtection();
                    }
                    buttonDelay = Time.time + 0.1f;
                }
            }
        }

        #region Misc Utilities

        /// <summary>
        /// Grabs all the ghost reactor collectible cores to your hand when holding grip.
        /// </summary>
        public static void GrabGhostReactorCores()
        {
            foreach (GameEntity entity in UnityEngine.Object.FindObjectsOfType<GameEntity>())
            {
                if (entity == null || !entity.name.Contains("GhostReactorCollectibleCore"))
                    continue;

                if (rightGrab)
                    entity.transform.position = References.RightHand.position;
            }
        }

        /// <summary>
        /// Grabs all the ghost reactor sentient seeds to your hand when holding grip.
        /// </summary>
        public static void GrabGhostReactorSeeds()
        {
            foreach (GameEntity entity in UnityEngine.Object.FindObjectsOfType<GameEntity>())
            {
                if (entity == null || !entity.name.Contains("GhostReactorCollectibleSentientCore"))
                    continue;

                if (rightGrab)
                    entity.transform.position = References.RightHand.position;
            }
        }

        /// <summary>
        /// Kills all the enemies in ghost reactor, only for you.
        /// </summary>
        public static void KillAllGhostReactorEnemies()
        {
            foreach (GREnemyMonkeye enemy in GetAllType<GREnemyMonkeye>())
                enemy.InstantDeath();
        }

        /// <summary>
        /// Saves everyone's player ids and info to a txt file in your game folder.
        /// </summary>
        public static void GetAllIds()
        {
            if (!PhotonNetwork.InRoom)
                return;

            string output = $"IDS GRABBED WITH QUBIT | ROOM: {PhotonNetwork.CurrentRoom.Name} | MODE: {ManagerRegistry.GorillaGameManager.GameType()}\n\n";
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                VRRig rig = Qubit.Extensions.Players.GetVRRigFromPlayer(player);
                if (rig == null)
                    continue;

                Color color = rig.playerColor;
                output += $"NAME: {player.NickName} | ID: {player.UserId} | COLOR CODE: {Mathf.RoundToInt(color.r * 9f)}{Mathf.RoundToInt(color.g * 9f)}{Mathf.RoundToInt(color.b * 9f)}\n";
            }

            System.IO.File.WriteAllText(System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "Player_IDS.txt"), output);
            Qubit.Libraries.Interface.Notifications.SendFormat("green", "SUCCESS", "Saved all ids to your BepInEx folder.");
        }

        private static readonly string[] GhostCodes = { "SREN17", "SREN18", "DAISY09", "PBBV", "J3VU", "ECHO", "STATUE", "BANSHEE", "NULL", "RUN", "TIPTOE" };

        /// <summary>
        /// Joins a random ghost code room.
        /// </summary>
        public static void JoinGhostCode()
        {
            string code = GhostCodes[UnityEngine.Random.Range(0, GhostCodes.Length)];
            PhotonNetwork.JoinRoom(code);
        }

        /// <summary>
        /// Completes the current active scavenger hunt.
        /// </summary>
        public static void CompleteScavengerHunt()
        {
            foreach (ScavengerManager.Hunt hunt in ScavengerManager.Instance.Hunts.Where(h => !h.IsCompleted))
            {
                foreach (ScavengerTarget target in hunt.Targets.Where(t => !hunt.IsCollected(t)))
                    hunt.Collect(target, false);
            }
        }

        /// <summary>
        /// Completes all of the MonkeBiz quests for you.
        /// </summary>
        public static void CompleteMonkeBizQuests()
        {
            foreach (QuestDisplay display in UnityEngine.Object.FindObjectsOfType<QuestDisplay>())
            {
                display.quest.ApplySavedProgress(display.quest.requiredOccurenceCount);
                display.quest.RemoveEventListener();
                display.quest.questManager.HandleQuestCompleted(display.quest.questID);
                display.UpdateDisplay();
            }
        }

        /// <summary>
        /// Digs voxels wherever your hand desires in a normal radius.
        /// </summary>
        public static void NormalVoxelDigGun()
        {
            GunLibrary.Aim(GunData =>
            {
                ChunkComponent chunk = GunData.Ray.collider.GetComponent<ChunkComponent>();
                if (chunk != null && GetGunInput(true))
                    VoxelExtensions.Mine(chunk.World, GunData.Ray, new VoxelAction { strength = 1f, radius = 0.5f, operation = (OperationType)0 });
            });
        }

        /// <summary>
        /// Digs voxels wherever your hand desires in a big radius.
        /// </summary>
        public static void BigVoxelDigGun()
        {
            GunLibrary.Aim(GunData =>
            {
                ChunkComponent chunk = GunData.Ray.collider.GetComponent<ChunkComponent>();
                if (chunk != null && GetGunInput(true) && Time.time > voxelDigDelay)
                {
                    voxelDigDelay = Time.time + 0.09f;
                    VoxelExtensions.Mine(chunk.World, GunData.Ray, new VoxelAction { strength = 1f, radius = 4.67f, operation = (OperationType)0 });
                }
            });
        }

        private static float voxelDigDelay;

        /// <summary>
        /// Makes the mining pickaxe dig voxels in a big radius.
        /// </summary>
        public static void BigMiningRadius()
        {
            foreach (Voxel_Pickaxe pickaxe in GetAllType<Voxel_Pickaxe>())
                pickaxe.mine.radius = 4.67f;
        }

        public static void DisableBigMiningRadius()
        {
            foreach (Voxel_Pickaxe pickaxe in GetAllType<Voxel_Pickaxe>())
                pickaxe.mine.radius = 0.5f;
        }

        /// <summary>
        /// Pick up a snowball and instantly make it big.
        /// </summary>
        public static void AutoBigSnowballs()
        {
            foreach (GrowingSnowballThrowable snowball in GetAllType<GrowingSnowballThrowable>())
            {
                if (snowball.SizeLevel != 5)
                    typeof(GrowingSnowballThrowable).GetMethod(PhotonNetwork.InRoom ? "GrowSnowballRPC" : "GrowSnowball", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(snowball, new object[] { 5 });
            }
        }

        /// <summary>
        /// Be able to pick up snowballs from the forest ground.
        /// </summary>
        public static void SnowballForestGround()
        {
            GorillaSurfaceOverride surface = GetObject("Environment Objects/LocalObjects_Prefab/rooms/pit ground bottom")?.GetComponent<GorillaSurfaceOverride>();
            if (surface != null)
                surface.overrideIndex = 60; // growing snowball surface
        }

        public static void DisableSnowballForestGround()
        {
            GorillaSurfaceOverride surface = GetObject("Environment Objects/LocalObjects_Prefab/rooms/pit ground bottom")?.GetComponent<GorillaSurfaceOverride>();
            if (surface != null)
                surface.overrideIndex = 0;
        }

        #endregion
    }
}
