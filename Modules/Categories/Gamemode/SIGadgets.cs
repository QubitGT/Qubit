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
using Qubit.Libraries.Networking;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
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
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

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
    }
}
