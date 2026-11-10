/*
 * Qubit Menu  Modules/Categories/Gamemode/SIGadgets.cs
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

using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Projectile;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Extensions.GameObj;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;
using static Qubit.Core.Security;

namespace Qubit.Modules.Categories.Gamemode
{
    public static class SIGadgets
    {
        public static void SuperInfectionTextGun()
        {
            TextGun.Render(Fun.textToRender, position =>
                GameEntities.CreateItems(RpcTarget.All, Enumerable.Repeat(Gadgets.GadgetByName["SIGadgetDashYoyo"], position.Count).ToArray(), position.ToArray(), Enumerable.Repeat(Quaternion.identity, position.Count).ToArray(), null, ManagerRegistry.SuperInfection.GameEntityManager));
        }
        public static void SuperInfectionDrawGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
                Coroutines.instance.StartCoroutine(GameEntities.DrawSmallDelay(NewPointer.transform.position, objectIds[Random.Range(0, objectIds.Length)], ManagerRegistry.SuperInfection.GameEntityManager));
            });
        }
        public static void InfiniteResources()
        {
            var player = SIPlayer.Get(Networking.LocalPlayer.ActorNumber);
            for (int i = 0; i < player.CurrentProgression.resourceArray.Length; i++)
                player.CurrentProgression.resourceArray[i] = int.MaxValue;

            if (Time.time > Overpowered.resourceIncrementDelay)
            {
                Overpowered.resourceIncrementDelay = Time.time + 1f;
                for (int i = 0; i < (int)SIResource.ResourceType.Count; i++)
                {
                    var resourceType = (SIResource.ResourceType)i;
                    ManagerRegistry.ProgressionManager.IncrementSIResource(resourceType.ToString(), OnFailure: (error) => Overpowered.resourceIncrementDelay = Time.time + 10f);
                }
            }
        }
        public static void CompleteAllQuests()
        {
            for (int i = 0; i < ManagerRegistry.SIProgression.activeQuestIds.Length; i++)
            {
                RotatingQuest quest = ManagerRegistry.SIProgression.questSourceList.GetQuestById(ManagerRegistry.SIProgression.activeQuestIds[i]);
                quest.SetProgress(quest.requiredOccurenceCount);
            }
        }
        public static void ClaimAllTerminals()
        {
            foreach (var terminal in ManagerRegistry.SuperInfection.ZoneSuperInfection.siTerminals)
                terminal?.PlayerHandScanned(Networking.LocalPlayer.ActorNumber);
        }
        public static void UnlockAllGadgets()
        {
            foreach (var page in ManagerRegistry.SIProgression.unlockedTechTreeData)
            {
                for (int i = 0; i < page.Length; i++)
                    page[i] = true;
            }
        }
        public static void DebugBlasterAimbot()
        {
            List<NetPlayer> infected = InfectedList();
            List<VRRig> rigs = Players.ActiveRigs
                .Where(rig => !rig.isLocal)
                .Where(rig => !infected.Contains(rig.GetPlayer()))
                .ToList();

            Transform head = References.HeadCollider.transform;
            VRRig targetRig = rigs
                .Where(rig => rig != null)
                .Select(rig => new
                {
                    Rig = rig,
                    ToRig = (rig.transform.position - head.position).normalized,
                    Distance = Vector3.Distance(head.position, rig.transform.position)
                })
                .OrderBy(x => Vector3.Angle(head.forward, x.ToRig))
                .ThenBy(x => x.Distance)
                .Select(x => x.Rig)
                .FirstOrDefault();

            if (targetRig == null)
                return;

            Visual.Visualize(PrimitiveType.Sphere, targetRig.headMesh.transform.position, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), Color.green, -91752, 0.1f);
        }
        public static void BlasterLaserSpam()
        {
            if (rightGrab)
                Gadgets.FireBlaster(References.RightHand.position, References.RightHand.forward);
        }
        public static void BlasterFlingGun(Vector3 direction)
        {
            GunLibrary.Target(lockTarget =>
            {
                Gadgets.FireBlaster(lockTarget.transform.position, direction.normalized);
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void BlasterFlingAll(Vector3 direction)
        {
            if (Gadgets.GetBlaster() == null)
                ViewSerialization.OverrideSerialization = null;
            else ViewSerialization.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Gadgets.FireBlaster(targetRig.transform.position, direction, Player, true);
            }
        }
        public static void BlasterFlingTowardsGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Gadgets.FireBlaster(lockTarget.transform.position, References.BodyCollider.transform.position - lockTarget.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void BlasterFlingTowardsAll()
        {
            if (Gadgets.GetBlaster() == null)
                ViewSerialization.OverrideSerialization = null;
            else ViewSerialization.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Gadgets.FireBlaster(targetRig.transform.position, References.BodyCollider.transform.position - targetRig.transform.position, Player, true);
            }
        }
        public static void BlasterFlingAwayGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Gadgets.FireBlaster(lockTarget.transform.position, lockTarget.transform.position - References.BodyCollider.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void BlasterFlingAwayAll()
        {
            if (Gadgets.GetBlaster() == null)
                ViewSerialization.OverrideSerialization = null;
            else ViewSerialization.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Gadgets.FireBlaster(targetRig.transform.position, targetRig.transform.position - References.BodyCollider.transform.position, Player, true);
            }
        }
        public static void BlasterKickGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Gadgets.FireBlaster(lockTarget.transform.position, lockTarget.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - lockTarget.transform.position).normalized : lockTarget.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left);
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void BlasterKickAll()
        {
            if (Gadgets.GetBlaster() == null)
                ViewSerialization.OverrideSerialization = null;
            else ViewSerialization.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Gadgets.FireBlaster(targetRig.transform.position, targetRig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - targetRig.transform.position).normalized : targetRig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left, Player, true);
            }
        }
        public static void BlasterCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Gadgets.FireBlaster(lockTarget.transform.position, lockTarget.transform.position.y > 55f ? Vector3.right : Vector3.up);
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void BlasterCrashAll()
        {
            if (Gadgets.GetBlaster() == null)
                ViewSerialization.OverrideSerialization = null;
            else ViewSerialization.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Gadgets.FireBlaster(targetRig.transform.position, targetRig.transform.position.y > 55f ? Vector3.right : Vector3.up, Player, true);
            }
        }
        public static void BlasterControlGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null && lockTarget.Distance(References.BodyCollider.transform.position) > 0.5f)
                    Gadgets.FireBlaster(lockTarget.transform.position, References.BodyCollider.transform.position - lockTarget.transform.position);

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
        public static void SpamGadgetGrip(int objectId)
        {
            if (rightGrab)
                GameEntities.CreateItem(RpcTarget.All, objectId, References.RightHand.position, RandomQuaternion(), References.RightHand.forward * ShootStrength, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }
        public static void SpamGadgetGun(int objectId)
        {
            GunLibrary.Point(NewPointer =>
            {
                GameEntities.CreateItem(RpcTarget.All, objectId, NewPointer.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            });
        }
        public static void GadgetSpamGrip()
        {
            int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
            SpamGadgetGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void GadgetSpamGun()
        {
            int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
            SpamGadgetGun(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void RainGadgets()
        {
            int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + new Vector3(Random.Range(-3f, 3f), 4f, Random.Range(-3f, 3f)), Quaternion.identity, Vector3.down, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }
        public static void GadgetAura()
        {
            int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + RandomVector3().normalized * 2f, Quaternion.identity, Vector3.down, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }
        public static void GadgetFountain()
        {
            int[] objectIds = Gadgets.GadgetByName.Select(element => element.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + Vector3.up * 3f, Quaternion.identity, RandomVector3(15f), Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }
        public static void ResourceSpamGrip()
        {
            int[] objectIds = Gadgets.GadgetByName.Where(x => x.Key.Contains("Resource")).Select(x => x.Value).ToArray();
            SpamGadgetGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void ResourceSpamGun()
        {
            int[] objectIds = Gadgets.GadgetByName.Where(x => x.Key.Contains("Resource")).Select(x => x.Value).ToArray();
            SpamGadgetGun(objectIds[Random.Range(0, objectIds.Length)]);
        }

        #region Selected Item Suite

        /// <summary>The currently selected entity/item id used by the item mods. Defaults to a vibrating spring resource.</summary>
        public static int SelectedItemId { get; set; } =
            Gadgets.GadgetByName.TryGetValue("SI_Resource_VibratingSpring", out int springId) ? springId : 0;

        /// <summary>Random item mode: 0 off, 1 all, 2 SuperInfection, 3 GhostReactor, 4 grabbables, 5 GR grabbables, 6 non-grabbables.</summary>
        public static int ItemRandomizerMode;

        public static void SetItemRandomizer(int mode) =>
            ItemRandomizerMode = mode;

        private static int cachedRandomItemId;
        private static float randomItemCacheTime;

        /// <summary>
        /// Resolves the item id to spawn: the selected item, or a random pick per spawn
        /// based on the randomizer mode.
        /// </summary>
        private static int ResolveItemId()
        {
            if (ItemRandomizerMode == 0)
                return SelectedItemId;

            // Re-roll every 0.2s so bursts share one pick, like a per-shot random.
            if (Time.time - randomItemCacheTime > 0.2f)
            {
                randomItemCacheTime = Time.time;

                GameEntityManager manager = ManagerRegistry.SuperInfection.GameEntityManager;
                IEnumerable<KeyValuePair<int, GameObject>> factory = GameEntityManager.itemPrefabFactory;

                IEnumerable<KeyValuePair<int, GameObject>> pool = ItemRandomizerMode switch
                {
                    2 => factory.Where(e => e.Value.name.StartsWith("SI_")),
                    3 => factory.Where(e => e.Value.name.Contains("GhostReactor")),
                    4 => factory.Where(e => e.Value.GetComponent<GameEntity>() != null && e.Value.GetComponent<GameEntity>().pickupable),
                    5 => factory.Where(e => e.Value.name.Contains("GhostReactor") && e.Value.GetComponent<GameEntity>() != null && e.Value.GetComponent<GameEntity>().pickupable),
                    6 => factory.Where(e => e.Value.GetComponent<GameEntity>() == null || !e.Value.GetComponent<GameEntity>().pickupable),
                    _ => factory
                };

                List<KeyValuePair<int, GameObject>> candidates = pool.ToList();
                cachedRandomItemId = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)].Key : SelectedItemId;
            }

            return cachedRandomItemId;
        }

        private static float itemDelay;

        /// <summary>
        /// Spawns the selected item out of either hand when holding grip.
        /// </summary>
        public static void ItemSpammer()
        {
            if (Time.time > itemDelay)
            {
                itemDelay = Time.time + 0.1f;
                if (rightGrab)
                    GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), References.RightHand.position + new Vector3(0f, 0.15f, 0f), References.RightHand.rotation, Vector3.down * 0.1f, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
                else if (leftGrab)
                    GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), References.LeftHand.position + new Vector3(0f, 0.15f, 0f), References.LeftHand.rotation, Vector3.down * 0.1f, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            }
        }

        /// <summary>
        /// Shoots the selected item out of either hand when holding grip.
        /// </summary>
        public static void ItemMinigun()
        {
            if (Time.time > itemDelay)
            {
                itemDelay = Time.time + 0.1f;
                if (rightGrab)
                    GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), References.RightHand.position, References.RightHand.rotation, References.RightHand.rotation * Vector3.forward * -15f, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
                else if (leftGrab)
                    GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), References.LeftHand.position, References.LeftHand.rotation, References.LeftHand.rotation * Vector3.forward * -15f, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            }
        }

        /// <summary>
        /// Spawns the selected item orbiting around you when holding grip.
        /// </summary>
        public static void ItemOrbit()
        {
            if (rightGrab && Time.time > itemDelay)
            {
                itemDelay = Time.time + 0.1f;
                float angle = Time.frameCount / 30f;
                Vector3 position = References.LocalRig.transform.position + new Vector3(Mathf.Cos(angle) * 0.9f, 0f, Mathf.Sin(angle) * 0.9f);
                GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), position, Quaternion.LookRotation(Vector3.up, References.LocalRig.transform.position - position), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            }
        }

        /// <summary>
        /// Spawns the selected item at wherever your hand desires.
        /// </summary>
        public static void ItemGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), NewPointer.transform.position + new Vector3(0f, 0.14f, 0f), Quaternion.identity, Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            });
        }

        /// <summary>
        /// Spawns the selected item in a ball around you.
        /// </summary>
        public static void ItemSphere()
        {
            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 position = References.LocalRig.transform.position + randomDirection * 1.2f;
            GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), position, Quaternion.LookRotation(References.LocalRig.transform.position - position), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }

        /// <summary>
        /// Spawns the selected item in a sphere at wherever your hand desires.
        /// </summary>
        public static void ItemSphereGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Vector3 randomDirection = Random.onUnitSphere;
                Vector3 position = NewPointer.transform.position + new Vector3(0f, 0.9f, 0f) + randomDirection * 0.85f;
                GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), position, Quaternion.LookRotation(NewPointer.transform.position + new Vector3(0f, 0.9f, 0f) - position), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            });
        }

        /// <summary>
        /// Destroys all the entities in the current super infection zone.
        /// </summary>
        public static void DestroyAllItems()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            GameEntityManager manager = ManagerRegistry.SuperInfection.GameEntityManager;
            if (manager == null)
                return;

            List<int> netIds = new List<int>();
            foreach (GameEntity entity in manager.entities)
            {
                if (entity == null || entity.IsScenePlaced)
                    continue;

                entity.gameObject.SetActive(true);
                int netId = entity.GetNetId();
                if (!netIds.Contains(netId))
                    netIds.Add(netId);
            }

            if (netIds.Count > 0)
            {
                manager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new object[] { netIds.ToArray() });
                Security.RPCProtection();
            }
        }

        #endregion
        public static void DestroyGadgetGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > Overpowered.destroyDelay)
                {
                    GameEntity gameEntity = null;
                    float closestDist = float.MaxValue;

                    foreach (GameEntity entity in ManagerRegistry.SuperInfection.GameEntityManager.entities)
                    {
                        if (entity != null)
                        {
                            float distance = Vector3.Distance(NewPointer.transform.position, entity.transform.position);
                            if (distance < 0.75f && distance < closestDist)
                            {
                                gameEntity = entity;
                                closestDist = distance;
                            }
                        }
                    }

                    if (gameEntity != null)
                    {
                        Overpowered.destroyDelay = Time.time + 0.02f;
                        if (Networking.IsMasterClient)
                        {
                            ManagerRegistry.SuperInfection.GameEntityManager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new[] { gameEntity.GetNetId() });
                            Security.RPCProtection();
                        }
                        else
                        {
                            gameEntity.RequestGrab(true, Vector3.zero, Quaternion.identity, ManagerRegistry.SuperInfection.GameEntityManager);
                            gameEntity.RequestThrow(true, References.BodyCollider.transform.position - (Vector3.up * 14f), Quaternion.identity, Vector3.zero, Vector3.zero, ManagerRegistry.SuperInfection.GameEntityManager);
                        }
                    }
                }
            });
        }

        #region SuperGameMode / Barriers / Bonus

        /// <summary>
        /// Breaks the super infection gamemode by destroying its entity manager.
        /// Makes terminals unusable, items wont spawn, and resources cant be claimed.
        /// </summary>
        public static void BreakSuperGameMode()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            GameEntityManager manager = GameEntityManager.GetManagerForZone(References.LocalRig.zoneEntity.currentZone);
            if (manager != null)
                PhotonNetwork.Destroy(manager.photonView);
        }

        private static bool barriersDisabled;

        /// <summary>
        /// Removes the map barriers in super infection.
        /// </summary>
        public static void DisableMapBarriers()
        {
            barriersDisabled = !barriersDisabled;

            string[] barriers =
            {
                "Environment Objects/LocalObjects_Prefab/Forest/Super Infection Zone - Forest Variant/ForestDome_Prefab",
                "Environment Objects/LocalObjects_Prefab/Mountain/Super Infection Zone - Mountain Variant/MountainDome_Prefab",
                "Environment Objects/LocalObjects_Prefab/MetroMain/Super Infection Zone - Metropolis Variant/MetroDome_Prefab",
                "Environment Objects/LocalObjects_Prefab/Canyon/Super Infection Zone - Canyon Variant/CanyonDome_Prefab",
                "Environment Objects/LocalObjects_Prefab/Canyon/Canyon/Canyon_ForceVolumes/CanyonDome_CollisionOnly",
                "Environment Objects/LocalObjects_Prefab/Beach/Super Infection Zone - Beach Variant/BeachDome_Prefab",
                "Environment Objects/LocalObjects_Prefab/Beach/ForceVolumesOcean_Combo_V2/MapDome_Beach_Collision (1)"
            };

            foreach (string barrier in barriers)
            {
                GameObject obj = GetObject(barrier);
                if (obj != null)
                    obj.SetActive(!barriersDisabled);
            }
        }

        /// <summary>
        /// Instantly completes your bonus points progress.
        /// </summary>
        public static void CompleteBonusPoints()
        {
            for (int i = 0; i < 10; i++)
                SIPlayer.LocalPlayer.GetBonusProgress(SuperInfectionManager.activeSuperInfectionManager);
        }

        #endregion

        #region Malachi 9.1 Additions

        private const int BreakMovementEntityHash = 1517967647;
        private const int AudioBreakEntityHash = 1551901997;
        private const int InstantBatchCount = 102;

        /// <summary>
        /// Sends a CreateItemsRPC payload straight to specific actor numbers through a raw
        /// Photon raise event, bypassing the rpc spam limiter and hitting only the targets.
        /// </summary>
        private static void RaiseCreateItems(GameEntityManager manager, int[] actorNumbers, object[] createData)
        {
            PhotonView view = manager.photonView;

            // Split payloads over 45 items in half with a 75ms gap, like the game's own chunking.
            bool oversized = ((int[])createData[0]).Length > 45;
            object[][] parts = oversized ? new[] { SplitPayload(createData, true), SplitPayload(createData, false) } : new[] { createData };

            foreach (object[] part in parts)
            {
                SendRaiseEvent(view, actorNumbers, part);
                if (oversized)
                    System.Threading.Tasks.Task.Delay(75).Wait();
            }
        }

        private static object[] SplitPayload(object[] payload, bool firstHalf)
        {
            return payload.Select(x =>
            {
                Array array = (Array)x;
                Array half = Array.CreateInstance(array.GetType().GetElementType(), array.Length / 2);
                if (firstHalf)
                    Array.Copy(array, half, half.Length);
                else
                    Array.Copy(array, array.Length - half.Length, half, 0, half.Length);
                return half;
            }).ToArray();
        }

        private static void SendRaiseEvent(PhotonView view, int[] actorNumbers, object[] parameters)
        {
            ExitGames.Client.Photon.Hashtable content = new ExitGames.Client.Photon.Hashtable();
            content.Add(0, view.ViewID);
            content.Add(2, PhotonNetwork.ServerTimestamp);
            content.Add(3, "CreateItemsRPC");
            content.Add(4, parameters);
            content.Add(5, (byte)PhotonNetwork.PhotonServerSettings.RpcList.IndexOf("CreateItemsRPC"));

            RaiseEventOptions options = new RaiseEventOptions();
            options.TargetActors = actorNumbers;
            ExitGames.Client.Photon.SendOptions sendOptions = default(ExitGames.Client.Photon.SendOptions);
            sendOptions.DeliveryMode = ExitGames.Client.Photon.DeliveryMode.Reliable;
            sendOptions.Encrypt = false;
            sendOptions.Reliability = true;

            PhotonNetwork.NetworkingClient.LoadBalancingPeer.OpRaiseEvent(200, content, options, sendOptions);
        }

        /// <summary>
        /// Builds the CreateItemsRPC payload the game expects — net ids, hashes,
        /// packed positions, packed rotations, create data and invalid net ids.
        /// </summary>
        private static object[] BuildCreateItemsPayload(GameEntityManager manager, int[] hashes, Vector3[] positions, Quaternion[] rotations)
        {
            int invalidNetId = manager.GetNetIdFromEntityId(GameEntityId.Invalid);

            List<int> netIds = new List<int>();
            List<long> packedPositions = new List<long>();
            List<int> packedRotations = new List<int>();
            List<long> createData = new List<long>();

            for (int i = 0; i < hashes.Length; i++)
            {
                netIds.Add(manager.CreateTypeNetId(hashes[i]));
                packedPositions.Add(BitPackUtils.PackWorldPosForNetwork(positions[i]));
                packedRotations.Add(BitPackUtils.PackQuaternionForNetwork(rotations[i]));
                createData.Add(0L);
            }

            return new object[]
            {
                netIds.ToArray(),
                hashes.ToArray(),
                packedPositions.ToArray(),
                packedRotations.ToArray(),
                createData.ToArray(),
                Enumerable.Repeat(invalidNetId, hashes.Length).ToArray()
            };
        }

        private static bool ZoneHasEntities(GameEntityManager manager, int typeId, int minCount)
        {
            int count = 0;
            foreach (GameEntity entity in manager.entities)
            {
                if (entity != null && entity.typeId == typeId)
                {
                    count++;
                    if (count >= minCount)
                        return true;
                }
            }
            return false;
        }

        private static void CleanZoneEntities(GameEntityManager manager, int typeId)
        {
            List<int> netIds = new List<int>();
            foreach (GameEntity entity in manager.entities)
            {
                if (entity == null || entity.IsScenePlaced)
                    continue;

                if (typeId != 0 && entity.typeId != typeId)
                    continue;

                entity.gameObject.SetActive(true);
                netIds.Add(entity.GetNetId());
            }

            if (netIds.Count > 0)
            {
                manager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new object[] { netIds.ToArray() });
                Security.RPCProtection();
            }
        }

        private static GameEntityManager CurrentZoneManager() =>
            GameEntityManager.GetManagerForZone(References.LocalRig.zoneEntity.currentZone);

        private static bool InGhostReactor(VRRig rig) =>
            rig.zoneEntity.currentZone == GTZone.ghostReactor || rig.zoneEntity.currentZone == GTZone.ghostReactorDrill;

        /// <summary>
        /// Breaks everyone's audio in ghost reactor by burying them in audio entities.
        /// </summary>
        public static void BreakAudioAll()
        {
            List<Player> inZone = PhotonNetwork.PlayerList.Where(p => p.IsLocal == false && InGhostReactor(Qubit.Extensions.Players.GetVRRigFromPlayer(p))).ToList();
            if (inZone.Count == 0)
            {
                Notifications.SendFormat("red", "ERROR", "People must be in ghost reactor.");
                return;
            }

            GameEntityManager manager = CurrentZoneManager();
            if (manager == null)
                return;

            if (ZoneHasEntities(manager, AudioBreakEntityHash, 100))
                CleanZoneEntities(manager, AudioBreakEntityHash);

            Quaternion[] rotations = Enumerable.Repeat(Quaternion.Euler(90f, 0f, 0f), InstantBatchCount).ToArray();
            int[] hashes = Enumerable.Repeat(AudioBreakEntityHash, InstantBatchCount).ToArray();

            RaiseCreateItems(manager, inZone.Select(p => p.ActorNumber).ToArray(), BuildCreateItemsPayload(manager, hashes, new Vector3[InstantBatchCount], rotations));
            RPCProtection();
        }

        /// <summary>
        /// Breaks whoever your hand desires's audio in ghost reactor by burying them in audio entities.
        /// </summary>
        public static void BreakAudioGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!InGhostReactor(gunTarget))
                {
                    Notifications.SendFormat("red", "ERROR", "This player must be in ghost reactor.");
                    return;
                }

                GameEntityManager manager = GameEntityManager.GetManagerForZone(gunTarget.zoneEntity.currentZone);
                if (manager == null)
                    return;

                Quaternion[] rotations = Enumerable.Repeat(Quaternion.Euler(90f, 0f, 0f), InstantBatchCount).ToArray();
                int[] hashes = Enumerable.Repeat(AudioBreakEntityHash, InstantBatchCount).ToArray();
                Vector3[] positions = Enumerable.Repeat(gunTarget.transform.position, InstantBatchCount).ToArray();

                RaiseCreateItems(manager, new[] { gunTarget.Creator.ActorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, BuildCreateItemsPayload(manager, hashes, positions, rotations));
                RPCProtection();
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Practically instantly crashes everyone in ghost reactor with a raw targeted item blitz.
        /// </summary>
        public static void InstantCrashAll()
        {
            List<Player> inZone = PhotonNetwork.PlayerList.Where(p => p.IsLocal == false && InGhostReactor(Qubit.Extensions.Players.GetVRRigFromPlayer(p))).ToList();
            if (inZone.Count == 0)
            {
                Notifications.SendFormat("red", "ERROR", "People must be in ghost reactor.");
                return;
            }

            GameEntityManager manager = CurrentZoneManager();
            if (manager == null)
                return;

            Quaternion[] rotations = Enumerable.Repeat(Quaternion.Euler(90f, 0f, 0f), InstantBatchCount).ToArray();
            int[] hashes = Enumerable.Repeat(AudioBreakEntityHash, InstantBatchCount).ToArray();

            RaiseCreateItems(manager, inZone.Select(p => p.ActorNumber).ToArray(), BuildCreateItemsPayload(manager, hashes, new Vector3[InstantBatchCount], rotations));

            // Hide the mess locally after the blitz lands.
            foreach (GameEntity entity in manager.entities)
            {
                if (entity != null && entity.typeId == AudioBreakEntityHash)
                    entity.gameObject.SetActive(false);
            }

            RPCProtection();
        }

        /// <summary>
        /// Practically instantly crashes whoever your hand desires with a raw targeted item blitz.
        /// </summary>
        public static void InstantCrashGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!InGhostReactor(gunTarget))
                {
                    Notifications.SendFormat("red", "ERROR", "This player must be in ghost reactor.");
                    return;
                }

                GameEntityManager manager = GameEntityManager.GetManagerForZone(gunTarget.zoneEntity.currentZone);
                if (manager == null)
                    return;

                Quaternion[] rotations = Enumerable.Repeat(Quaternion.Euler(90f, 0f, 0f), InstantBatchCount).ToArray();
                int[] hashes = Enumerable.Repeat(AudioBreakEntityHash, InstantBatchCount).ToArray();
                Vector3[] positions = Enumerable.Repeat(gunTarget.transform.position - new Vector3(0f, 4f, 0f), InstantBatchCount).ToArray();

                RaiseCreateItems(manager, new[] { gunTarget.Creator.ActorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, BuildCreateItemsPayload(manager, hashes, positions, rotations));

                foreach (GameEntity entity in manager.entities)
                {
                    if (entity != null && entity.typeId == AudioBreakEntityHash)
                        entity.gameObject.SetActive(false);
                }

                RPCProtection();
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Shoot and instantly spawn a sphere of 102 selected items.
        /// </summary>
        public static void ItemInstaSphereGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Vector3 center = NewPointer.transform.position + new Vector3(0f, 0.9f, 0f);

                Vector3[] positions = new Vector3[InstantBatchCount];
                Quaternion[] rotations = new Quaternion[InstantBatchCount];
                for (int i = 0; i < InstantBatchCount; i++)
                {
                    positions[i] = center + Random.onUnitSphere * 0.85f;
                    rotations[i] = Quaternion.LookRotation(center - positions[i]);
                }

                GameEntityManager manager = ManagerRegistry.SuperInfection.GameEntityManager;
                RaiseCreateItems(manager, new[] { PhotonNetwork.LocalPlayer.ActorNumber }, BuildCreateItemsPayload(manager, Enumerable.Repeat(ResolveItemId(), InstantBatchCount).ToArray(), positions, rotations));
                RPCProtection();
            });
        }

        private static int ellipsoidAxis = 1;
        private static float ellipsoidAngle;
        private static Vector3 ellipsoidCenter;
        private static bool ellipsoidActive;

        /// <summary>
        /// Shoot and make circles along each axis with selected items, cycling X, Y, Z every full revolution.
        /// </summary>
        public static void ItemEllipsoidGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (!ellipsoidActive || ellipsoidCenter == Vector3.zero)
                {
                    ellipsoidCenter = NewPointer.transform.position + new Vector3(0f, 1f, 0f);
                    ellipsoidActive = true;
                }

                ellipsoidAngle += 3f * Time.deltaTime * 60f;
                float a = ellipsoidAngle * Mathf.Deg2Rad;

                Vector3 position;
                Quaternion rotation;
                switch (ellipsoidAxis)
                {
                    case 1: // horizontal circle in X/Z
                        position = ellipsoidCenter + new Vector3(Mathf.Cos(a) * 0.8f, 0f, Mathf.Sin(a) * 0.8f);
                        rotation = Quaternion.LookRotation(ellipsoidCenter - position);
                        break;
                    case 2: // vertical circle in X/Y
                        position = ellipsoidCenter + new Vector3(Mathf.Cos(a) * 0.9f, Mathf.Sin(a) * 0.8f, 0f);
                        rotation = Quaternion.LookRotation(Vector3.forward, ellipsoidCenter - position);
                        break;
                    default: // vertical circle in Y/Z
                        position = ellipsoidCenter + new Vector3(0f, Mathf.Cos(a) * 0.8f, Mathf.Sin(a) * 0.9f);
                        rotation = Quaternion.LookRotation(Vector3.right, ellipsoidCenter - position);
                        break;
                }

                GameEntities.CreateItem(RpcTarget.All, ResolveItemId(), position, rotation, Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
                RPCProtection();

                if (ellipsoidAngle >= 360f)
                {
                    ellipsoidAngle = 0f;
                    ellipsoidAxis = ellipsoidAxis >= 3 ? 1 : ellipsoidAxis + 1;
                }
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    ellipsoidCenter = Vector3.zero;
                    ellipsoidActive = false;
                    ellipsoidAngle = 0f;
                    ellipsoidAxis = 1;
                }
            });
        }

        #endregion
    }
}
