/*
 * Qubit Menu  Modules/Categories/Overpowered.cs
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

using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Gameplay;
using GorillaNetworking;
using GorillaTag;
using GorillaTagScripts;
using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using JoinType = GorillaNetworking.JoinType;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Overpowered
    {
        public static void AlwaysGuardian() =>
            GameModes.BecomeGuardian();


        public static float guardianDelay;

        private static float playerColorDelay;
        public static void SetColorSelf(int color) =>
            GameModes.SetMonkeBallTeams(new Dictionary<int, int> { { Networking.LocalPlayer.ActorNumber, color } });

        public static void SetColorGun(int color)
        {
            GunLibrary.Target(gunTarget =>
            {
                playerColorDelay = Time.time + 0.1f;
                if (PhotonNetwork.IsMasterClient)
                    GameModes.SetMonkeBallTeams(new Dictionary<int, int> { { GetPlayerFromVRRig(gunTarget).ActorNumber, color } });
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
            }, new GunLibrary.GunSettings { when = () => Time.time > playerColorDelay });
        }

        public static void SetColorAll(int color)
        {
            if (PhotonNetwork.IsMasterClient)
                GameModes.SetMonkeBallTeams(References.Network.AllNetPlayers.ToDictionary(p => p.ActorNumber, p => color));
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
        }

        public static void StrobeColorSelf()
        {
            if (Time.time > playerColorDelay)
            {
                playerColorDelay = Time.time + 0.1f;
                if (Networking.IsMasterClient)
                    SetColorSelf(Time.time % 0.2f > 0.1f ? 1 : 0);
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
            }
        }

        public static void StrobeColorGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > playerColorDelay)
                    {
                        playerColorDelay = Time.time + 0.1f;
                        if (Networking.IsMasterClient)
                            GameModes.SetMonkeBallTeams(new Dictionary<int, int> { { lockTarget.GetPlayer().ActorNumber, Time.time % 0.2f > 0.1f ? 1 : 0 } });
                        else
                            Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal() && !gunTarget.IsTagged())
                    {
                        if (PhotonNetwork.IsMasterClient)
                        {
                            gunLocked = true;
                            lockTarget = gunTarget;
                        }
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                } });
        }

        public static void StrobeColorAll()
        {
            if (Time.time > playerColorDelay)
            {
                playerColorDelay = Time.time + 0.1f;
                if (Networking.IsMasterClient)
                    SetColorAll(Time.time % 0.2f > 0.1f ? 1 : 0);
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
            }
        }

        public static readonly Dictionary<VRRig, int> materialState = new Dictionary<VRRig, int>();

        public static float guardianSpazDelay;
        public static bool guardianSpazToggle;

        public static float guardianProtectorDelay;
        public static void GuardianProtector()
        {
            if (Networking.InRoom)
            {

                if (!GameModes.IsGuardian(PhotonNetwork.LocalPlayer)) return;
                foreach (TappableGuardianIdol tgi in GetAllType<TappableGuardianIdol>())
                {
                    if (!tgi.manager || !tgi.manager.photonView) continue;
                    foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal && Vector3.Distance(rig.transform.position, tgi.transform.position) < 2f && Time.time > guardianProtectorDelay))
                    {
                        BetaSetVelocityPlayer(rig.GetPlayer(), (rig.transform.position - tgi.transform.position).normalized * 50f);
                        guardianProtectorDelay = Time.time + 0.1f;
                    }
                }
            }
        }

        public static void GuardianKickTarget(NetPlayer target)
        {
            if (Time.time > crashAllDelay)
            {
                crashAllDelay = Time.time + 0.1f;
                VRRig rig = GetVRRigFromPlayer(target);
                BetaSetVelocityPlayer(target, rig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - rig.transform.position).normalized * 50f : rig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
                RPCProtection();
            }
        }

        private static float crashAllDelay;
        public static void GuardianKickGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > crashAllDelay)
                {
                    crashAllDelay = Time.time + 0.1f;
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), lockTarget.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - lockTarget.transform.position).normalized * 50f : lockTarget.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
                    RPCProtection();
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianKickAll()
        {
            if (rightTrigger > 0.5f && Time.time > crashAllDelay)
            {
                crashAllDelay = Time.time + 0.1f;
                foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal))
                {
                    BetaSetVelocityPlayer(rig.GetPlayer(), rig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - rig.transform.position).normalized * 50f : rig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
                    RPCProtection();
                }
            }
        }

        public static void GuardianCrashPlayer(NetPlayer target)
        {
            VRRig rig = GetVRRigFromPlayer(target);
            if (Time.time > crashAllDelay && rig.transform.position.x < -5)
            {
                crashAllDelay = Time.time + 0.1f;

                BetaSetVelocityPlayer(target, (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                RPCProtection();
            }
        }

        public static void GuardianCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > crashAllDelay && lockTarget.transform.position.x < -5)
                {
                    crashAllDelay = Time.time + 0.1f;
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), (lockTarget.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                    RPCProtection();
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianCrashAll()
        {
            if (rightTrigger > 0.5f && Time.time > crashAllDelay)
            {
                crashAllDelay = Time.time + 0.1f;
                foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal && rig.transform.position.x < -5))
                {
                    BetaSetVelocityPlayer(rig.GetPlayer(), (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                    RPCProtection();
                }
            }
        }

        public static float spazDriverDelay;

        public static void BecomeDriver()
        {
            if (PhotonNetwork.IsMasterClient)
                CustomMapsTerminal.instance.mapTerminalNetworkObject.SendRPC("SetTerminalControlStatus_RPC", true, true, PhotonNetwork.LocalPlayer.ActorNumber);
            else
            {
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                return;
            }

            CustomMapsTerminal.instance.mapTerminalNetworkObject.photonView.OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
        }

        public static long? id;
        public static float setMapDelay;

        public const int ItemCrashCount = 500;
        public static void GameEntityCrash(GameEntityManager manager, object target, Vector3? targetPosition = null)
        {
            if (manager == null)
                return;

            targetPosition ??= References.BodyCollider.transform.position;

            int[] objectIds = manager.itemPrefabFactory.Keys.ToArray();
            int[] ids = new int[ItemCrashCount];
            Vector3[] positions = new Vector3[ItemCrashCount];
            Quaternion[] rotations = new Quaternion[ItemCrashCount];

            for (int i = 0; i < ItemCrashCount; i++)
            {
                ids[i] = objectIds[Random.Range(0, objectIds.Length)];
                positions[i] = targetPosition.Value;
                rotations[i] = Quaternion.identity;
            }

            CreateItems(target, ids, positions, rotations, manager: manager);
        }

        public static int masterVisualizationType;
        public static readonly string[] VisualizationTypeNames = { "Sphere", "Cube", "Tracer" };
        public static void ApplyMasterVisualizationType(int index) => masterVisualizationType = index;

        public static void VisualizeMasterClient()
        {
            if (Visuals.DoPerformanceCheck())
                return;

            if (Networking.IsMasterClient)
                return;

            VRRig rig = References.Network.MasterClient.VRRig();
            if (rig == null)
                return;

            long visualizeId = 2017928;
            switch (masterVisualizationType)
            {
                case 0:
                    Visuals.Visualize(PrimitiveType.Sphere, rig.transform.position, Quaternion.identity, new Vector3(0.15f, 0.15f, 0.15f), Color.blue, visualizeId, 0.1f);
                    break;
                case 1:
                    Visuals.Visualize(PrimitiveType.Cube, rig.transform.position, Quaternion.Euler(Time.time * 90, Time.time * 60, Time.time * 30), new Vector3(0.3f, 0.3f, 0.3f), Color.blue);
                    break;
                case 2:
                    LineRenderer line = Visuals.GetLineRender();

                    line.startColor = Color.blue;
                    line.endColor = Color.blue;
                    float width = 0.025f;
                    line.startWidth = width;
                    line.endWidth = width;
                    line.SetPosition(0, rig.transform.position);
                    line.SetPosition(1, References.RightHand.position);
                    break;
            }
        }

        private static float reportDelay;
        public static void DelayBanGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                gunLocked = true;
                lockTarget = gunTarget;

                if (References.LocalRig.IsTagged())
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must not be tagged.");
                    return;
                }

                if (!lockTarget.IsTagged())
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> The target must be tagged.");
                    return;
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must not be master client.");
                    return;
                }

                if (Time.time > reportDelay)
                {
                    reportDelay = Time.time + 0.5f;
                    GorillaPlayerScoreboardLine.ReportPlayer(lockTarget.GetPlayer().UserId, GorillaPlayerLineButton.ButtonType.Cheating, lockTarget.GetPlayer().NickName);
                }

                SerializePatch.OverrideSerialization = () =>
                {
                    lockTarget.GetPlayer();
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 positionArchive = References.LocalRig.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => !(new[] { PhotonNetwork.MasterClient.ActorNumber, lockTarget.GetPlayer().ActorNumber }).Contains(plr.ActorNumber)).Select(plr => plr.ActorNumber).ToArray() });

                    References.LocalRig.transform.position = new Vector3(99999f, 99999f, 99999f);
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });

                    References.LocalRig.transform.position = lockTarget.rightHandTransform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

                    RPCProtection();
                    References.LocalRig.transform.position = positionArchive;

                    return false;
                };
            }, new GunLibrary.GunSettings { when = () => !gunLocked, onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        SerializePatch.OverrideSerialization = null;
                    }
                } });
        }

        public static void DelayBanAll()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                if (References.LocalRig.IsTagged())
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must not be tagged.");
                    return true;
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must not be master client.");
                    return true;
                }

                lockTarget.GetPlayer();
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 positionArchive = References.LocalRig.transform.position;
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(player => !player.IsMasterClient && player.VRRig().IsTagged()).Select(player => player.ActorNumber).ToArray() });

                References.LocalRig.transform.position = new Vector3(99999f, 99999f, 99999f);
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });

                foreach (NetPlayer player in References.Network.PlayerListOthers)
                {
                    VRRig rig = GetVRRigFromPlayer(player);
                    if (!player.IsMasterClient && rig.IsTagged())
                    {
                        References.LocalRig.transform.position = rig.rightHandTransform.position;
                        SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                    }
                }

                RPCProtection();
                References.LocalRig.transform.position = positionArchive;

                return false;
            };
        }

        public static void GuardianObliteratePlayer(NetPlayer target)
        {
            if (Time.time > crashAllDelay)
            {
                crashAllDelay = Time.time + 0.1f;
                VRRig rig = GetVRRigFromPlayer(target);
                BetaSetVelocityPlayer(target, (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                RPCProtection();
            }
        }


        public static void GiveFlyOnGrab()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.isLocal)
                {
                    if (rig.leftHandLink.grabbedPlayer == Networking.LocalPlayer || rig.rightHandLink.grabbedPlayer == Networking.LocalPlayer)
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

            if (remoteLink.grabbedPlayer != Networking.LocalPlayer)
            {
                if (grabDelay == 0)
                    grabDelay = remoteLink.rejectGrabsUntilTimestamp > Time.time ? remoteLink.rejectGrabsUntilTimestamp : Time.time + 1f;

                if (Time.time > grabDelay)
                {
                    localLink.TentacleTryCreateLink(remoteLink);
                    References.LocalRig.transform.position = targetTransform;
                    Notifications.SendNotification("<color=grey>[</color><color=purple>MENU</color><color=grey>]</color> Tried to grab " + rig.GetPlayer().NickName + ".");
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

        public static float propHuntSpazDelay;
        public static bool propHuntSpazMode;

        public static void CreateItem(object target, int hash, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, long sendData = 0L, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (Networking.IsMasterClient)
            {
                if (gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                {
                    int netId = gameEntityManager.CreateTypeNetId(hash);

                    if (target is NetPlayer netPlayer)
                        target = NetPlayerToPlayer(netPlayer);

                    object[] createData = {
                        new[] { netId },
                        new[] { hash },
                        new[] { BitPackUtils.PackWorldPosForNetwork(position) },
                        new[] { BitPackUtils.PackQuaternionForNetwork(rotation) },
                        new[] { sendData },
                        new[] { gameEntityManager.GetInvalidNetId() }
                    };

                    switch (target)
                    {
                        case RpcTarget rpcTarget:
                            gameEntityManager.photonView.RPC("CreateItemRPC", rpcTarget, createData);
                            break;
                        case Player player:
                            gameEntityManager.photonView.RPC("CreateItemRPC", player, createData);
                            break;
                    }

                    if ((velocity != Vector3.zero || angVelocity != Vector3.zero || Manager.GetIndex("Entity Gravity").enabled) && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.ThrowEntity].CanCallNow())
                    {

                        velocity = velocity.ClampSqrMagnitude(1600f);

                        object[] dropData = {
                            netId,
                            true,
                            position,
                            rotation,
                            velocity,
                            angVelocity,
                            PhotonNetwork.LocalPlayer,
                            PhotonNetwork.Time
                        };

                        switch (target)
                        {
                            case RpcTarget rpcTarget:
                                gameEntityManager.photonView.RPC("ThrowEntityRPC", rpcTarget, dropData);
                                break;
                            case Player player:
                                gameEntityManager.photonView.RPC("ThrowEntityRPC", player, dropData);
                                break;
                        }
                    }
                }

                RPCProtection();
            }
            else
            {
                float maxDistance = 12f;
                if (Vector3.Distance(ServerLeftHandPos, position) > maxDistance)
                    position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * maxDistance;

                GamePlayer gamePlayer = GamePlayer.GetGamePlayer(PhotonNetwork.LocalPlayer);
                if (gamePlayer.IsHoldingEntity(gameEntityManager, true) && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                {
                    References.LocalRig.enabled = true;
                    if (ServerLeftHandPos.Distance(position) < maxDistance)
                        gameEntityManager.GetGameEntity(gamePlayer.GetGrabbedGameEntityId(GamePlayer.GetHandIndex(true))).RequestThrow(true, position, rotation, velocity, angVelocity, gameEntityManager);
                    else
                        return;
                }

                List<GameEntity> entities = gameEntityManager.entities.Where(e =>
                    e != null &&
                    e.typeId == hash &&
                    Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                    Vector3.Distance(References.BodyCollider.transform.position, e.transform.position) > 3f &&
                    gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                if (entities.Count <= 0)
                    entities = gameEntityManager.entities.Where(e =>
                    e != null &&
                    e.typeId == hash &&
                    Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                    gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                if (entities.Count <= 0)
                    entities = gameEntityManager.entities.Where(e =>
                    e != null &&
                    e.typeId == hash &&
                    gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                if (entities.Count <= 0) // Desperate measures
                    entities = gameEntityManager.entities.Where(e =>
                    e != null &&
                    e.typeId == hash).ToList();

                if (entities.Count <= 0)
                    return;

                GameEntity entity = entities.OrderByDescending(entity => entity.transform.position.Distance(References.BodyCollider.transform.position)).FirstOrDefault();

                if (Vector3.Distance(entity.transform.position, References.BodyCollider.transform.position) > maxDistance)
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = entity.transform.position - Vector3.one * 5f;

                    if (CritterCoroutine != null)
                        Coroutines.instance.StopCoroutine(CritterCoroutine);

                    CritterCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());
                }

                if (Vector3.Distance(entity.transform.position, ServerPos) < maxDistance && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                {
                    entity.transform.position = References.RightHand.position;
                    entity.transform.rotation = RandomQuaternion();

                    entity.RequestGrab(true, Vector3.zero, Quaternion.identity, gameEntityManager);
                    RPCProtection();
                }
            }
        }

        public static void CreateItems(object target, int[] hashes, Vector3[] positions, Quaternion[] rotations, long[] sendData = null, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (Networking.IsMasterClient)
            {
                if (gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItems].CanCallNow())
                {
                    if (target is NetPlayer netPlayer)
                        target = NetPlayerToPlayer(netPlayer);

                    sendData ??= Enumerable.Repeat(0L, hashes.Length).ToArray();

                    byte[] data = new byte[15360];
                    MemoryStream memoryStream = new MemoryStream(data);
                    BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                    binaryWriter.Write(hashes.Length);

                    for (int i = 0; i < hashes.Length; i++)
                    {
                        binaryWriter.Write(manager.CreateTypeNetId(hashes[i]));
                        binaryWriter.Write(hashes[i]);
                        binaryWriter.Write(BitPackUtils.PackWorldPosForNetwork(positions[i]));
                        binaryWriter.Write(BitPackUtils.PackQuaternionForNetwork(rotations[i]));
                        binaryWriter.Write(sendData[i]);
                        binaryWriter.Write(gameEntityManager.GetInvalidNetId());
                    }

                    byte[] array = GZipStream.CompressBuffer(data);

                    object[] createData = {
                        (int)manager.zone,
                        array
                    };

                    switch (target)
                    {
                        case RpcTarget rpcTarget:
                            gameEntityManager.photonView.RPC("CreateItemsRPC", rpcTarget, createData);
                            break;
                        case Player player:
                            gameEntityManager.photonView.RPC("CreateItemsRPC", player, createData);
                            break;
                    }

                    RPCProtection();
                }


            }
            else
                CreateItem(target, hashes[0], positions[0], rotations[0], Vector3.zero, Vector3.zero, sendData.Length > 0 ? sendData[1] : 0L, manager);
        }

        public static Dictionary<string, bool[][]> Letters = new Dictionary<string, bool[][]> {
            { "A", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "B", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "C", new[]
            {
                new[] { false, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { false, true, true, true, true },
            } },
            { "D", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
            } },
            { "E", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { "F", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { "G", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, false, false, true, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "H", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "I", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { true, true, true, true, true },
            } },
            { "J", new[]
            {
                new[] { false, false, false, false, true },
                new[] { false, false, false, false, true },
                new[] { false, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "K", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, true, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "L", new[]
            {
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { "M", new[]
            {
                new[] { true, true, false, true, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "N", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, true, false, false, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, true, true },
                new[] { true, false, false, false, true },
            } },
            { "O", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "P", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { "Q", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, true, false },
                new[] { false, true, true, false, true },
            } },
            { "R", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "S", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
                new[] { false, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "T", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
            } },
            { "U", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "V", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
            } },
            { "W", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, true, false, true },
                new[] { false, true, false, true, false },
            } },
            { "X", new[]
            {
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "Y", new[]
            {
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
            } },
            { "Z", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { ".", new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, true, false, false },
            } },
            { "/", new[]
            {
                new[] { false, false, false, false, true },
                new[] { false, false, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { " ", new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
            } }
        };
        public static float textDelay;
        public static int characterIndex;
        public static Vector3? basePosition;

        public static IEnumerator DrawSmallDelay(Vector3 position, int id, GameEntityManager manager)
        {
            GameObject Temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Temporary.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Temporary.transform.position = position;
            Object.Destroy(Temporary.GetComponent<Collider>());
            yield return new WaitForSeconds(0.5f);
            CreateItem(RpcTarget.All, id, Temporary.transform.position + new Vector3(0f, 0.1f, 0f), RandomQuaternion(), Vector3.zero, Vector3.zero, manager: manager);
            Object.Destroy(Temporary);
            RPCProtection();
        }

        public static float destroyDelay;

        public static float resourceIncrementDelay;

        public static int? spawnedNetId;
        public static int spawnedFrame;

        public static SIGadgetChargeBlaster GetBlaster()
        {
            References.InputPoller.leftGrab = true;
            References.InputPoller.leftControllerGripFloat = 1f;

            int hash = Fun.GadgetByName["MegaChargeBlasterGadget"];

            GameEntityManager gameEntityManager = ManagerRegistry.SuperInfection.GameEntityManager;
            GamePlayer gamePlayer = GamePlayer.GetGamePlayer(PhotonNetwork.LocalPlayer);
            if (gamePlayer.IsHoldingEntity(gameEntityManager, true))
            {
                GameEntity entity = gameEntityManager.GetGameEntity(gamePlayer.GetGrabbedGameEntityId(GamePlayer.GetHandIndex(true)));
                if (entity.gameObject.TryGetComponent<SIGadgetChargeBlaster>(out var blaster))
                    return blaster;
                else
                    entity.RequestThrow(true, entity.transform.position, entity.transform.rotation, Vector3.zero, Vector3.zero, gameEntityManager);
            }
            else
            {
                if (Networking.IsMasterClient)
                {
                    if (spawnedNetId != null && spawnedFrame > Time.frameCount - 10)
                    {
                        GameEntity entity = gameEntityManager.GetGameEntity(spawnedNetId.Value);
                        entity.RequestGrab(true, Vector3.zero, Quaternion.identity, gameEntityManager);
                        if (entity.gameObject.TryGetComponent<SIGadgetChargeBlaster>(out var blaster))
                            return blaster;
                    }

                    if (!gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                        return null;


                    int netId = gameEntityManager.CreateTypeNetId(hash);

                    object[] createData = {
                        new[] { netId },
                        new[] { hash },
                        new[] { BitPackUtils.PackWorldPosForNetwork(References.LeftHand.position) },
                        new[] { BitPackUtils.PackQuaternionForNetwork(References.LeftHand.rotation) },
                        new[] { 0L },
                        new[] { gameEntityManager.GetInvalidNetId() }
                    };

                    gameEntityManager.photonView.RPC("CreateItemRPC", RpcTarget.All, createData);
                    spawnedNetId = netId;
                    spawnedFrame = Time.frameCount;

                    RPCProtection();
                }
                else
                {
                    Vector3 position = References.LeftHand.position;

                    float maxDistance = 12f;
                    if (Vector3.Distance(ServerLeftHandPos, position) > maxDistance)
                        position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * maxDistance;

                    List<GameEntity> entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                        Vector3.Distance(References.BodyCollider.transform.position, e.transform.position) > 3f &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0)
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0)
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0) // Desperate measures
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash).ToList();

                    if (entities.Count <= 0)
                        return null;

                    GameEntity entity = entities.OrderByDescending(entity => entity.transform.position.Distance(References.BodyCollider.transform.position)).FirstOrDefault();

                    if (Vector3.Distance(entity.transform.position, References.BodyCollider.transform.position) > maxDistance)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = entity.transform.position - Vector3.one * 5f;

                        if (CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(CritterCoroutine);

                        CritterCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());
                    }

                    if (Vector3.Distance(entity.transform.position, ServerPos) < maxDistance && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                    {
                        entity.transform.position = References.RightHand.position;
                        entity.transform.rotation = RandomQuaternion();

                        entity.RequestGrab(true, Vector3.zero, Quaternion.identity, gameEntityManager);
                        RPCProtection();
                    }
                }
            }

            return null;
        }

        public static float blasterDelay;
        public static Coroutine BlasterCoroutine;

        public static void BetaFireBlaster(Vector3 position, Vector3 direction)
        {
            SIGadgetChargeBlaster blaster = GetBlaster();
            if (blaster == null)
                return;

            Quaternion rotation = Quaternion.LookRotation(direction);
            if (Time.time < blasterDelay)
                return;

            blasterDelay = Time.time + SIPlayer.LocalPlayer.clientToClientRPCLimiter.GetDelay();

            if ((position - References.LeftHand.position).magnitude > blaster.blaster.maxLagDistance)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = position - Vector3.one;

                if (BlasterCoroutine != null)
                    Coroutines.instance.StopCoroutine(BlasterCoroutine);

                BlasterCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());
            }

            blaster.blaster.lastFired = 0f;
            blaster.FireProjectile(blaster.maxChargeDiff, blaster.blaster.NextFireId(), position, rotation);

            RPCProtection();
        }

        public static readonly Dictionary<NetPlayer, float> perPlayerDictionary = new Dictionary<NetPlayer, float>();
        public static void BetaFireBlaster(Vector3 position, Vector3 direction, NetPlayer target, bool ignoreDistnace = false)
        {
            perPlayerDictionary.TryGetValue(target, out float perPlayerDelay);

            Quaternion rotation = Quaternion.LookRotation(direction);
            if (Time.time < perPlayerDelay)
                return;

            SIGadgetChargeBlaster blaster = GetBlaster();
            if (blaster == null)
                return;

            perPlayerDictionary[target] = Time.time + SIPlayer.LocalPlayer.clientToClientRPCLimiter.GetDelay();

            if (!ignoreDistnace && (position - References.LeftHand.position).magnitude > blaster.blaster.maxLagDistance)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = position - Vector3.one;

                if (BlasterCoroutine != null)
                    Coroutines.instance.StopCoroutine(BlasterCoroutine);

                BlasterCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());
            }

            blaster.blaster.lastFired = 0f;
            SuperInfectionManager.GetSIManagerForZone(blaster.blaster.gameEntity.manager.zone)?.photonView.RPC("SIClientToClientRPC", target.GetPlayer(), new object[]
            {
                (int)SuperInfectionManager.ClientToClientRPC.CallEntityRPCData,
                new object[]
                {
                    blaster.blaster.gameEntity.GetNetId(),
                    0,
                    new object[]
                    {
                        blaster.maxChargeDiff,
                        blaster.blaster.NextFireId(),
                        position,
                        rotation
                    }
                }
            });

            RPCProtection();
        }

        public static HalloweenGhostChaser _lucy;
        public static HalloweenGhostChaser Lucy
        {
            get
            {
                _lucy ??= GameObject.Find("Environment Objects/05Maze_PersistentObjects/Ghosts/Halloween Ghost/FloatingChaseSkeleton").GetComponent<HalloweenGhostChaser>();
                return _lucy;
            }
            set => _lucy = value;
        }
        public static HalloweenGhostChaser GetChaser() => Lucy;

        
        public static void BetaSetVelocityPlayer(NetPlayer victim, Vector3 velocity)
        {
            if (velocity.sqrMagnitude > 20f)
                velocity = Vector3.Normalize(velocity) * 20f;

            if (GameModes.IsGuardian(Networking.LocalPlayer))
            {
                GetNetworkViewFromVRRig(GetVRRigFromPlayer(victim)).SendRPC("GrabbedByPlayer", victim, true, false, false);
                GetNetworkViewFromVRRig(GetVRRigFromPlayer(victim)).SendRPC("DroppedByPlayer", victim, velocity);
            }
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");
        }

        public static void BetaSetVelocityTargetGroup(RpcTarget victim, Vector3 velocity)
        {
            if (velocity.sqrMagnitude > 20f)
                velocity = Vector3.Normalize(velocity) * 20f;

            if (GameModes.IsGuardian(Networking.LocalPlayer))
            {
                switch (victim)
                {
                    case RpcTarget.All:
                        {
                            foreach (VRRig rig in Players.ActiveRigs)
                            {
                                GetNetworkViewFromVRRig(rig).SendRPC("GrabbedByPlayer", rig.GetPlayer(), true, false, false);
                                GetNetworkViewFromVRRig(rig).SendRPC("DroppedByPlayer", rig.GetPlayer(), velocity);
                            }
                            break;
                        }
                    case RpcTarget.Others:
                        {
                            foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal))
                            {
                                GetNetworkViewFromVRRig(rig).SendRPC("GrabbedByPlayer", rig.GetPlayer(), true, false, false);
                                GetNetworkViewFromVRRig(rig).SendRPC("DroppedByPlayer", rig.GetPlayer(), velocity);
                            }

                            break;
                        }
                    case RpcTarget.MasterClient:
                        {
                            GetNetworkViewFromVRRig(GetVRRigFromPlayer(References.Network.MasterClient)).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                            GetNetworkViewFromVRRig(GetVRRigFromPlayer(References.Network.MasterClient)).SendRPC("DroppedByPlayer", RpcTarget.Others, velocity);
                            break;
                        }
                }
            }
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");
        }

        private static float grabDelay = 0;
        public static void GuardianGrabGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    GetNetworkViewFromVRRig(gunTarget).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                    RPCProtection();
                }
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");
                grabDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > grabDelay });
        }

        public static void GuardianGrabAll()
        {
            if (rightGrab && Time.time > grabDelay)
            {
                grabDelay = Time.time + 0.1f;
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                    {
                        GetNetworkViewFromVRRig(plr).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");
            }
        }

        private static float releaseDelay;
        public static void GuardianReleaseGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    GetNetworkViewFromVRRig(gunTarget).SendRPC("DroppedByPlayer", RpcTarget.Others, new Vector3(0f, 0f, 0f));
                    RPCProtection();
                }
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");

                releaseDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > releaseDelay });
        }

        public static void GuardianReleaseAll()
        {
            if (rightTrigger > 0.5f && Time.time > releaseDelay)
            {
                releaseDelay = Time.time + 0.1f;
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                    {
                        GetNetworkViewFromVRRig(plr).SendRPC("DroppedByPlayer", RpcTarget.Others, new Vector3(0f, 0f, 0f));
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");
            }
        }

        private static float flingDelay;
        public static void GuardianFlingGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                BetaSetVelocityPlayer(GetPlayerFromVRRig(gunTarget), new Vector3(0f, 19.9f, 0f));
                RPCProtection();
                flingDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > flingDelay });
        }

        public static void GuardianFlingAll()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.1f;

                BetaSetVelocityTargetGroup(RpcTarget.Others, new Vector3(0f, 19.9f, 0f));
                RPCProtection();
            }
        }

        public static void GuardianSpazPlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), RandomVector3(50f));
                    RPCProtection();
                    flingDelay = Time.time + 0.1f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianSpazAllPlayers()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.1f;
                BetaSetVelocityTargetGroup(RpcTarget.Others, RandomVector3(50f));
                RPCProtection();
            }
        }


        private static float antiReportFlingDelay;
        public static void AntiReportFling()
        {
            if (Time.time > antiReportFlingDelay)
            {
                Safety.AntiReport((vrrig, position) =>
                {
                    antiReportFlingDelay = Time.time + 0.1f;
                    BetaSetVelocityPlayer(GetPlayerFromVRRig(vrrig), (vrrig.transform.position - position) * 50f);
                    Notifications.SendNotification("<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, they have been flung.");
                });
            }
        }

        public static bool SpecialTimeRPC(PhotonView photonView, int timeOffset, string method, RaiseEventOptions options, params object[] parameters)
        {
            if (photonView != null && parameters != null && !string.IsNullOrEmpty(method))
            {
                Hashtable rpcData = new Hashtable
                {
                    { 0, photonView.ViewID },
                    { 2, PhotonNetwork.ServerTimestamp + timeOffset },
                    { 3, method },
                    { 4, parameters }
                };

                if (photonView.Prefix > 0)
                    rpcData[1] = (short)photonView.Prefix;

                if (PhotonNetwork.PhotonServerSettings.RpcList.Contains(method))
                    rpcData[5] = (byte)PhotonNetwork.PhotonServerSettings.RpcList.IndexOf(method);

                if (options.Receivers == ReceiverGroup.All || (options.TargetActors != null && options.TargetActors.Contains(Networking.LocalPlayer.ActorNumber)))
                {
                    if (options.Receivers == ReceiverGroup.All)
                        options.Receivers = ReceiverGroup.Others;

                    if (options.TargetActors != null && options.TargetActors.Contains(Networking.LocalPlayer.ActorNumber))
                        options.TargetActors = options.TargetActors.Where(id => id != Networking.LocalPlayer.ActorNumber).ToArray();

                    PhotonNetwork.ExecuteRpc(rpcData, PhotonNetwork.LocalPlayer);
                }

                else
                {
                    PhotonNetwork.NetworkingClient.LoadBalancingPeer.OpRaiseEvent(Photon.Pun.PunEvent.RPC, rpcData, options, new SendOptions
                    {
                        Reliability = true,
                        DeliveryMode = DeliveryMode.ReliableUnsequenced,
                        Encrypt = false
                    });
                }
            }
            return false;
        }

        public static void GuardianPhysicalFreezeGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), Vector3.zero);
                    RPCProtection();
                    flingDelay = Time.time + 0.1f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianPhysicalFreezeAll()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.1f;
                BetaSetVelocityTargetGroup(RpcTarget.Others, Vector3.zero);
                RPCProtection();
            }
        }

        public static void GuardianBringPlayer(NetPlayer player)
        {
            if (Time.time > flingDelay)
            {
                BetaSetVelocityPlayer(player, (References.BodyCollider.transform.position - GetVRRigFromPlayer(player).transform.position).normalized * 20f);
                RPCProtection();
                flingDelay = Time.time + 0.1f;
            }
        }

        public static void GuardianBringPlayerGun(NetPlayer player)
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > flingDelay)
                {
                    BetaSetVelocityPlayer(player, Vector3.Normalize(NewPointer.transform.position - GetVRRigFromPlayer(player).transform.position) * 50f);
                    RPCProtection();
                    flingDelay = Time.time + 0.2f;
                }
            });
        }

        public static void GuardianBringGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), (References.BodyCollider.transform.position - lockTarget.transform.position).normalized * 20f);
                    RPCProtection();
                    flingDelay = Time.time + 0.1f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianBringAll()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.2f;
                foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                {
                    BetaSetVelocityPlayer(GetPlayerFromVRRig(plr), (References.BodyCollider.transform.position - plr.transform.position).normalized * 20f);
                    RPCProtection();
                }
            }
        }

        public static void GuardianBringAwayGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    BetaSetVelocityPlayer(lockTarget.GetPlayer(), (lockTarget.transform.position - References.BodyCollider.transform.position).normalized * 20f);
                    RPCProtection();
                    flingDelay = Time.time + 0.1f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianBringAwayAll()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.2f;
                foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                {
                    BetaSetVelocityPlayer(GetPlayerFromVRRig(plr), (plr.transform.position - References.BodyCollider.transform.position).normalized * 20f);
                    RPCProtection();
                }
            }
        }

        public static void GuardianOrbitAll()
        {
            float scale = 5f;
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.2f;
                int index = 0;

                VRRig[] rigs = Players.ActiveRigs.Where(rig => !rig.isLocal).ToArray();
                foreach (VRRig rig in rigs)
                {
                    float offset = 360f / rigs.Length * index;
                    Vector3 targetPosition = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + Time.time) * scale, 2, MathF.Sin(offset + Time.time) * scale);

                    BetaSetVelocityPlayer(rig.GetPlayer(), (targetPosition - rig.transform.position) * 1f);
                    RPCProtection();
                    index++;
                }
            }
        }

        private static float thingdeb;
        public static void GuardianGiveFlyGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > thingdeb)
                {
                    if (lockTarget.rightThumb.calcT > 0.5f)
                    {
                        BetaSetVelocityPlayer(lockTarget.GetPlayer(), lockTarget.headMesh.transform.forward * Movement._flySpeed);
                        RPCProtection();
                    }
                    thingdeb = Time.time + 0.1f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GuardianGiveFlyAll()
        {
            if (Time.time > thingdeb)
            {
                thingdeb = Time.time + 0.1f;
                foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal).Where(plr => plr.rightThumb.calcT > 0.5f))
                {
                    BetaSetVelocityPlayer(GetPlayerFromVRRig(plr), plr.headMesh.transform.forward * Movement._flySpeed);
                    RPCProtection();
                }
            }
        }

        public static void GuardianPunchMod()
        {
            if (Time.time > thingdeb)
            {
                foreach (VRRig rig in Players.ActiveRigs)
                {
                    bool leftHand = Vector3.Distance(References.LeftHand.position, rig.headMesh.transform.position) < 0.25f;
                    bool rightHand = Vector3.Distance(References.RightHand.position, rig.headMesh.transform.position) < 0.25f;

                    if (!rig.isLocal && (leftHand || rightHand))
                    {
                        Vector3 vel = rightHand ? References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0) : References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0);

                        BetaSetVelocityPlayer(rig.GetPlayer(), vel);
                        thingdeb = Time.time + 0.1f;

                        if (Manager.GetIndex("Graphic Punch Mod").enabled)
                            Projectiles.SendProjectile(Projectiles.FindProjectile("Apple"), rig.headMesh.transform.position, Vector3.down * 600f, new Color32(100, 0, 0, 255));
                    }
                }
            }
        }

        public static void GuardianBoxing()
        {
            foreach (VRRig rig1 in Players.ActiveRigs)
            {
                if (Time.time < Projectiles.GetBoxingDelay(rig1))
                    continue;

                foreach (var targetDirection in from rig2 in Players.ActiveRigs where rig2 != rig1 where Vector3.Distance(rig2.leftHandTransform.position, rig1.headMesh.transform.position) < 0.25f || Vector3.Distance(rig2.rightHandTransform.position, rig1.headMesh.transform.position) < 0.25f select (rig1.headMesh.transform.position - rig2.headMesh.transform.position) * 20f)
                {
                    BetaSetVelocityPlayer(GetPlayerFromVRRig(rig1), targetDirection);
                    Projectiles.SetBoxingDelay(rig1);
                }
            }
        }

        public static void GuardianBringAllGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > flingDelay)
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                        BetaSetVelocityPlayer(GetPlayerFromVRRig(plr), Vector3.Normalize(NewPointer.transform.position - plr.transform.position) * 50f);

                    RPCProtection();
                    flingDelay = Time.time + 0.2f;
                }
            });
        }

        public static void GuardianBringAwayAllGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > flingDelay)
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                        BetaSetVelocityPlayer(GetPlayerFromVRRig(plr), Vector3.Normalize(plr.transform.position - NewPointer.transform.position) * 50f);

                    RPCProtection();
                    flingDelay = Time.time + 0.2f;
                }
            });
        }

        public static void GuardianAntiStump()
        {
            if (Time.time > flingDelay)
            {
                foreach (VRRig rig in Players.ActiveRigs)
                {
                    if (!rig.isLocal)
                    {
                        Vector3 stump = new Vector3(-66f, 12f, -79f);
                        if (Vector3.Distance(stump, rig.transform.position) < 3f)
                        {
                            BetaSetVelocityPlayer(rig.GetPlayer(), (rig.transform.position - stump).normalized * 20f);
                            flingDelay = Time.time + 0.2f;
                        }
                    }
                }
            }
        }

        private static float slamDel;
        private static bool flip;
        public static void GuardianEffectSpamHands()
        {
            if (rightGrab)
            {
                if (Time.time > slamDel)
                {
                    if (GameModes.IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, References.RightHand.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");

                    slamDel = Time.time + 0.05f;
                }
            }
            if (leftGrab)
            {
                if (Time.time > slamDel)
                {
                    if (GameModes.IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, References.LeftHand.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");

                    slamDel = Time.time + 0.05f;
                }
            }
        }

        public static void GuardianEffectSpamGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > slamDel)
                {
                    if (GameModes.IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, NewPointer.transform.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be guardian.");

                    slamDel = Time.time + 0.05f;
                }
            });
        }

        //private static float freezeAllDelay;
        //public static bool muteOnFreeze;
        //public static bool acklowledgeFreeze;
        public static void FreezeServer(float delay = 1f, int eventCount = 11, RaiseEventOptions options = null)
        {
            return;
            /*
            if (!NetworkSystem.Instance.InRoom) return;

            if (!acklowledgeFreeze)
            {
                acklowledgeFreeze = true;
                PromptSingle("Using mods that rely on freezing the server could not work unless a player leaves or joins. Just so you know.");
            }
                
            options ??= new RaiseEventOptions
            {
                Flags = new WebFlags(byte.MaxValue),
                TargetActors = new[] { -1 }
            };

            if (muteOnFreeze)
            {
                for (int i = 0; i < 10; i++)
                    MuteTarget(options);
            }

            if (Time.time > freezeAllDelay)
            {
                for (int i = 0; i < eventCount; i++)
                    PhotonNetwork.NetworkingClient.OpRaiseEvent(200, new object[] { serverLink }, options, SendOptions.SendUnreliable);

                RPCProtection();
                freezeAllDelay = Time.time + delay;
            }
            */
        }

        private static float closeRoomDelay;
        public static void CloseRoom()
        {
            if (!Networking.InRoom) return;
            if (Time.time < closeRoomDelay) return;

            closeRoomDelay = Time.time + 0.1f;

            for (int i = 0; i < 40; i++)
            {
                WebFlags flags = new WebFlags(byte.MaxValue);
                RaiseEventOptions options = new RaiseEventOptions
                {
                    Flags = flags,
                    Receivers = ReceiverGroup.All,
                    CachingOption = EventCaching.AddToRoomCacheGlobal
                };
                byte code = 51;
                PhotonNetwork.RaiseEvent(code, new object[] { serverLink }, options, SendOptions.SendUnreliable);
            }

            RPCProtection();
        }

        public static float zaWarudoNotificationDelay;

        public static Coroutine ZaWarudo_StartCoroutineVariable;
        public static Coroutine ZaWarudo_EndCoroutineVariable;

        public static AudioClip ZaWarudo_Start;
        public static AudioClip ZaWarudo_Stop;

        public static void ZaWarudo_enableMethod()
        {
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Overpowered/Timestop/start.ogg", "Audio/Mods/Overpowered/Timestop/start.ogg", clip => ZaWarudo_Start = clip);
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Overpowered/Timestop/end.ogg", "Audio/Mods/Overpowered/Timestop/end.ogg", clip => ZaWarudo_Stop = clip);
        }

        private static bool zaWarudoTrigger;
        public static void ZaWarudo()
        {
            if (!Networking.InRoom) return;

            if (rightTrigger > 0.5f)
            {
                if (Manager.GetIndex("No Freeze Za Warudo").enabled)
                {
                    if (!Manager.GetIndex("No Freeze Za Warudo").enabled)
                        SerializePatch.OverrideSerialization = () => false;

                    if (!zaWarudoTrigger)
                    {
                        if (ZaWarudo_StartCoroutineVariable != null)
                        {
                            Coroutines.instance.StopCoroutine(ZaWarudo_StartCoroutineVariable);
                            ZaWarudo_StartCoroutineVariable = null;
                        }

                        if (ZaWarudo_EndCoroutineVariable != null)
                        {
                            Coroutines.instance.StopCoroutine(ZaWarudo_StartCoroutineVariable);
                            ZaWarudo_EndCoroutineVariable = null;
                        }

                        ZaWarudo_StartCoroutineVariable = Coroutines.instance.StartCoroutine(ZaWarudo_StartCoroutine());
                    }

                    zaWarudoTrigger = true;

                    Movement.LowGravity();

                    if (!Manager.GetIndex("No Freeze Za Warudo").enabled)
                        FreezeServer();
                }
            }
            else
            {
                if (zaWarudoTrigger)
                {
                    if (ZaWarudo_StartCoroutineVariable != null)
                    {
                        Coroutines.instance.StopCoroutine(ZaWarudo_StartCoroutineVariable);
                        ZaWarudo_StartCoroutineVariable = null;
                    }

                    if (ZaWarudo_EndCoroutineVariable != null)
                    {
                        Coroutines.instance.StopCoroutine(ZaWarudo_EndCoroutineVariable);
                        ZaWarudo_EndCoroutineVariable = null;
                    }

                    ZaWarudo_EndCoroutineVariable = Coroutines.instance.StartCoroutine(ZaWarudo_StopCoroutine());
                }

                SerializePatch.OverrideSerialization = null;
                zaWarudoTrigger = false;
            }
        }

        public static IEnumerator ZaWarudo_StartCoroutine()
        {
            Fun.PlayAudio(ZaWarudo_Start);
            yield return new WaitForSeconds(2.4f);

            float endWhiteFadeTime = Time.time;
            Vector3 originPoint = References.BodyCollider.transform.position;

            while (Time.time < endWhiteFadeTime + 0.2f)
            {
                float t = (Time.time - endWhiteFadeTime) / 0.2f;
                Fun.HueShift(Color.Lerp(Color.clear, Color.white, t));

                TeleportPlayer(originPoint + RandomVector3(t * 0.2f));

                yield return null;
            }

            float purpleFadeTime = Time.time;

            while (Time.time < purpleFadeTime + 2f)
            {
                float t = (Time.time - purpleFadeTime) / 2f;
                Fun.HueShift(Color.Lerp(Color.white, new Color32(120, 47, 196, 100), t));

                TeleportPlayer(originPoint + RandomVector3((1 - t) * 0.2f));

                yield return null;
            }

            TeleportPlayer(originPoint);

            Fun.HueShift(new Color32(120, 47, 196, 100));

            ZaWarudo_StartCoroutineVariable = null;
        }

        public static IEnumerator ZaWarudo_StopCoroutine()
        {
            Fun.PlayAudio(ZaWarudo_Stop);
            yield return new WaitForSeconds(0.5f);

            float purpleFadeTime = Time.time;

            while (Time.time < purpleFadeTime + 1f)
            {
                float t = Time.time - purpleFadeTime;
                Fun.HueShift(Color.Lerp(new Color32(120, 47, 196, 100), new Color32(120, 47, 196, 0), t));

                yield return null;
            }

            Fun.HueShift(Color.clear);

            ZaWarudo_EndCoroutineVariable = null;
        }

        public static int lagIndex = 2;
        public static int lagAmount;
        public static float lagDelay;
        public static readonly string[] LagPowerNames = { "Light", "Heavy", "Spike", "Stutter", "Freeze" };
        public static readonly int[] LagAmounts = { 40, 113, 425, 1000, 3800 };
        public static readonly float[] LagDelays = { 0.1f, 0.25f, 1f, 3f, 8f };
        public static void ApplyLagPower(int index)
        {
            lagIndex = index;
            lagAmount = LagAmounts[index];
            lagDelay = LagDelays[index];
        }

        public static int lagTypeIndex;
        public static readonly string[] LagTypeNames = { "Default", "Backup" };
        public static void ApplyLagType(int index) => lagTypeIndex = index;

        private static float lagDebounce;

        public static byte LagEvent
        {
            get
            {
                return lagTypeIndex switch
                {
                    0 => 3,
                    1 => 186,
                    _ => 3,
                };
            }
        }

        public static object LagData
        {
            get
            {
                return lagTypeIndex switch
                {
                    0 => default,
                    1 => new object[] { float.NaN },
                    _ => default,
                };
            }
        }

        public static void LagPlayer(object target)
        {
            if (!Networking.InRoom) return;
            if (Time.time < lagDebounce) return;

            if (target is VRRig rig) target = rig.GetPhotonPlayer();
            if (target is NetPlayer NetPlayer) target = NetPlayer.GetPlayer();

            lagDebounce = Time.time + lagDelay;

            byte eventIndex = LagEvent;
            object data = LagData;
            SendOptions sendOptions = SendOptions.SendUnreliable;
            RaiseEventOptions raiseEventOptions = RaiseEventOptions.Default;

            switch (target)
            {
                case RpcTarget rpcTarget:
                    raiseEventOptions.Receivers =
                        rpcTarget == RpcTarget.All ? ReceiverGroup.All :
                        rpcTarget == RpcTarget.MasterClient ? ReceiverGroup.MasterClient :
                        ReceiverGroup.Others;
                    break;

                case Player player:
                    raiseEventOptions.TargetActors = new[] { player.ActorNumber };
                    break;

                case int[] actorNumbers:
                    raiseEventOptions.TargetActors = actorNumbers;
                    break;
            }

            raiseEventOptions.CachingOption = EventCaching.DoNotCache;

            for (int i = 0; i < lagAmount; i++)
                PhotonNetwork.NetworkingClient.OpRaiseEvent(eventIndex, data, raiseEventOptions, sendOptions);

            RPCProtection();
        }

        public static bool GrabStatus
        {
            get => HandLinkPatch.enabled;
            set
            {
                HandLinkPatch.enabled = value;

                if (!value && !References.LocalRig.enabled)
                    References.LocalRig.enabled = true;
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                } });
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                } });
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                } });
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                    References.LocalRig.enabled = true;
                } });
        }

        public static void GrabCrashAll()
        {
            float value = float.MaxValue / 4f;
            ForceGrab(new Vector3(value, -value, value));
        }

        public static void LagGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                LagPlayer(lockTarget.GetPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void LagAll() =>
            LagPlayer(RpcTarget.Others);

        public static void LagAura()
        {
            if (!Networking.InRoom) return;
            List<int> nearbyPlayers = new List<int>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsTagged())
                    nearbyPlayers.Add(GetPlayerFromVRRig(vrrig).ActorNumber);
                else if (nearbyPlayers.Contains(GetPlayerFromVRRig(vrrig).ActorNumber))
                    nearbyPlayers.Remove(GetPlayerFromVRRig(vrrig).ActorNumber);
            }

            if (nearbyPlayers.Count > 0)
                LagPlayer(nearbyPlayers);
        }

        public static void LagOnTouch()
        {
            if (!Networking.InRoom) return;

            List<int> touchedPlayers = new List<int>();

            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                if (rig.IsBeingTouched())
                    touchedPlayers.Add(rig.GetPlayer().ActorNumber);
            }

            if (touchedPlayers.Count > 0)
                LagPlayer(touchedPlayers);
        }

        public static void MuteTarget(object target)
        {
            RaiseEventOptions raiseOptions = new RaiseEventOptions();

            if (target is ReceiverGroup group)
                raiseOptions.Receivers = group;
            else if (target is int[] actors)
                raiseOptions.TargetActors = actors;
            else if (target is RaiseEventOptions options)
                raiseOptions = options;
            else
                return;

            SendOptions sendOptions = new SendOptions
            {
                Reliability = false,
                Channel = 0
            };


            Dictionary<byte, object> voiceData = new Dictionary<byte, object>
            {
                { 1, 255 },
                { 2, Voices.Get().SamplingRate },
                { 3, 2 },
                { 4, 20000 },
                { 5, 30000 },
                { 10, null },
                { 11, (byte)0 },
                { 12, Codec.AudioOpus }
            };

            object[] eventData =
            {
                (byte)0,
                (byte)1,
                new object[] { voiceData }
            };

            PhotonVoiceNetwork.Instance.Client.OpRaiseEvent(
                202,
                eventData,
                raiseOptions,
                sendOptions
            );
        }

        public static void ServerMuteAll()
        {
            for (int i = 0; i < 2; i++)
                MuteTarget(ReceiverGroup.All);
        }

        public static void DeafenGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    for (int i = 0; i < 2; i++)
                        MuteTarget(new int[] { lockTarget.GetPlayer().ActorNumber });
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void DeafenAll()
        {
            for (int i = 0; i < 2; i++)
                MuteTarget(ReceiverGroup.Others);
        }

        public static void BarrelFlingGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, Vector3.up * 15000, Random.rotation, new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }
        public static void BarrelFlingAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, Vector3.up * 15000, Random.rotation, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }
        public static void BarrelObliterateGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * int.MaxValue, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void BarrelObliterateAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * int.MaxValue, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }
        public static void BarrelPunchMod()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.isLocal && (Vector3.Distance(References.LeftHand.position, rig.headMesh.transform.position) < 0.25f || Vector3.Distance(References.RightHand.position, rig.headMesh.transform.position) < 0.25f))
                {
                    Vector3 targetDirection = rig.headMesh.transform.position - References.HeadCollider.transform.position;
                    SendBarrelProjectile(rig.transform.position + (References.HeadCollider.transform.position - rig.headMesh.transform.position).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });

                    if (Manager.GetIndex("Graphic Punch Mod").enabled)
                        Projectiles.SendProjectile(Projectiles.FindProjectile("Apple"), rig.headMesh.transform.position, Vector3.down * 600f, new Color32(100, 0, 0, 255));
                }
            }
        }

        public static void BarrelCrashGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * 10000f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void BarrelCrashAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * 10000f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }

        public const int BarrelIndex = 618;

        public static void SendBarrelProjectile(Vector3 pos, Vector3 vel, Quaternion rot, RaiseEventOptions options = null, bool disableCooldown = false)
        {
            options ??= new RaiseEventOptions { Receivers = ReceiverGroup.Others };

            int index = BarrelIndex;

            if (Fun.DisableThrowableCoroutine != null)
                Coroutines.instance.StopCoroutine(Fun.DisableThrowableCoroutine);

            Fun.DisableThrowableCoroutine = Coroutines.instance.StartCoroutine(Fun.DisableThrowable(index));
            TransferrableObject transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];

            if (transferrableObject == null)
            {
                if (!CosmeticsOwned.Contains("Lucky Smash Barrel"))
                {
                    References.LocalRig.transform.position = TryOnRoom.transform.position;
                    CosmeticsController.instance.PressWardrobeItemButton(CosmeticsController.instance.allCosmetics.FirstOrDefault(c => string.Equals(c.overrideDisplayName, "Lucky Smash Barrel", StringComparison.OrdinalIgnoreCase)), false, false);
                }
                References.LocalRig.SetActiveTransferrableObjectIndex(1, index);
                transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];
            }

            if (transferrableObject == null)
            {
                Logs.LogError("Lucky smash barrel not found, cannot send barrel projectile.");
                return;
            }

            DeployableObject barrel = transferrableObject.GetComponent<DeployableObject>();
            if (!disableCooldown && barrel.m_spamChecker.CanCallNow(Time.unscaledTime))
            {
                transferrableObject.currentState = TransferrableObject.PositionState.InRightHand;

                object[] data = {
                    barrel._deploySignal._signalID,
                    References.Network.ServerTimestamp,
                    BitPackUtils.PackWorldPosForNetwork(pos),
                    BitPackUtils.PackQuaternionForNetwork(rot),
                    BitPackUtils.PackWorldPosForNetwork(vel)
                };
                PhotonNetwork.RaiseEvent(177, data, options, SendOptions.SendReliable);
                barrel._child.Deploy(barrel, pos, rot, vel);
                RPCProtection();
            }
        }

        public static void BarrelKickGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 targetDirection = new Vector3(-71.33718f, 101.4977f, -93.09029f) - lockTarget.headMesh.transform.position;
                    SendBarrelProjectile(lockTarget.transform.position + (lockTarget.headMesh.transform.position - new Vector3(-71.33718f, 101.4977f, -93.09029f)).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void BarrelKickAll()
        {
            foreach (VRRig TargetRig in Players.ActiveRigs)
            {
                if (TargetRig.IsTagged()) continue;

                Vector3 targetDirection = new Vector3(-71.33718f, 101.4977f, -93.09029f) - TargetRig.headMesh.transform.position;
                SendBarrelProjectile(TargetRig.transform.position + (TargetRig.headMesh.transform.position - new Vector3(-71.33718f, 101.4977f, -93.09029f)).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
            }
        }

        public static void BarrelFlingTowardsGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    SendBarrelProjectile(lockTarget.transform.position + (References.HeadCollider.transform.position - lockTarget.headMesh.transform.position).normalized * 0.1f, (References.BodyCollider.transform.position - lockTarget.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(lockTarget.GetPlayer()).ActorNumber } });

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void BarrelFlingTowardsAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                SendBarrelProjectile(TargetRig.transform.position + (References.HeadCollider.transform.position - TargetRig.headMesh.transform.position).normalized * 0.1f, (References.BodyCollider.transform.position - TargetRig.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(TargetRig.GetPlayer()).ActorNumber } }, true);
        }

        public static void CityKickGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    SendBarrelProjectile(lockTarget.transform.position + (lockTarget.transform.position - new Vector3(-71.14215f, 13.73829f, -95.17883f)).normalized * 0.1f, (new Vector3(-71.14215f, 13.73829f, -95.17883f) - lockTarget.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(lockTarget.GetPlayer()).ActorNumber } });

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void CityKickAll()
        {
            foreach (VRRig TargetRig in Players.ActiveRigs)
            {
                if (TargetRig.IsTagged()) continue;
                SendBarrelProjectile(TargetRig.transform.position + (TargetRig.transform.position - new Vector3(-71.14215f, 13.73829f, -95.17883f)).normalized * 0.1f, (new Vector3(-71.14215f, 13.73829f, -95.17883f) - TargetRig.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
            }
        }

        private static float notifyTime;
        public static bool IsModded(bool notify)
        {
            if (!Networking.InRoom) return false;
            bool modded = References.Network.GameModeString.Contains("MODDED_");
            if (!modded && notify && Time.time > notifyTime)
            {
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not in a modded gamemode. Use Utilla to create one, or join an already existing modded room.");
                notifyTime = Time.time + 1;
            }
            return modded;
        }

        public static float delay;

        public static void BetaNearbyFollowCommand(GorillaFriendCollider friendCollider, Player player)
        {
            PhotonNetworkController.Instance.FriendIDList.Add(player.UserId);

            object[] groupJoinSendData = new object[2];
            groupJoinSendData[0] = PhotonNetworkController.Instance.shuffler;
            groupJoinSendData[1] = PhotonNetworkController.Instance.keyStr;
            NetEventOptions netEventOptions = new NetEventOptions { TargetActors = new[] { player.ActorNumber } };

            if (friendCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId) && friendCollider.playerIDsCurrentlyTouching.Contains(player.UserId) && player != PhotonNetwork.LocalPlayer)
                RoomSystem.SendEvent(4, groupJoinSendData, netEventOptions, false);
            else if (!friendCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId))
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not in stump.");
        }

        public static IEnumerator StumpKickDelay(Action action, Action action2, float extraDelay = 0f, bool changeQueue = false)
        {
            PhotonNetworkController.Instance.FriendIDList.Clear();
            yield return new WaitForSeconds(extraDelay);

            bool joinedRoomPatchEnabled = JoinedRoomPatch.enabled;

            string queueArchive = References.Computer.currentQueue;
            if (changeQueue)
                References.Computer.currentQueue = RandomString();

            action?.Invoke();
            yield return new WaitForSeconds(0.3f);
            action2?.Invoke();
            yield return new WaitForSeconds(1f);

            if (changeQueue)
                References.Computer.currentQueue = queueArchive;

            yield return new WaitForSeconds(30f);

            JoinedRoomPatch.enabled = joinedRoomPatchEnabled;
        }

        public static bool kickToPublic;
        public static bool rejoinOnKick;
        public static string specificRoom;
        public static void CreateKickRoom()
        {
            if (rejoinOnKick)
            {
                Important.BroadcastRoom(specificRoom ?? RandomString(), true, PhotonNetworkController.Instance.keyToFollow, PhotonNetworkController.Instance.shuffler);
                Room.Reconnect();

                return;
            }

            Room.CreateRoom(specificRoom ?? RandomString(), kickToPublic, 0, JoinType.JoinWithNearby);
        }

        private static float kickDelay;
        public static void StumpKickGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer player = GetPlayerFromVRRig(gunTarget);
                kickDelay = Time.time + 0.5f;

                if (!References.Computer.friendJoinCollider.playerIDsCurrentlyTouching.Contains(player.UserId))
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> The player must be in stump.");
                    return;
                }

                if (!References.Network.SessionIsPrivate)
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be in a private room.");
                    return;
                }

                Coroutines.instance.StartCoroutine(StumpKickDelay(() =>
                {
                    PhotonNetworkController.Instance.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
                    PhotonNetworkController.Instance.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');

                    BetaNearbyFollowCommand(References.Computer.friendJoinCollider, NetPlayerToPlayer(player));
                    RPCProtection();
                }, () =>
                {
                    CreateKickRoom();
                }));
            }, new GunLibrary.GunSettings { when = () => Time.time > kickDelay });
        }

        public static void StumpKickAll()
        {
            if (Networking.InRoom)
            {
                if (!References.Network.SessionIsPrivate)
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be in a private room.");
                    return;
                }

                Coroutines.instance.StartCoroutine(StumpKickDelay(() =>
                {
                    PhotonNetworkController.Instance.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
                    PhotonNetworkController.Instance.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');

                    foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal() && References.Computer.friendJoinCollider.playerIDsCurrentlyTouching.Contains(rig.GetPlayer().UserId)))
                        BetaNearbyFollowCommand(References.Computer.friendJoinCollider, NetPlayerToPlayer(rig.GetPlayer()));

                    RPCProtection();
                }, () =>
                {
                    CreateKickRoom();
                }));
            }
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not in a room.");
        }

        private static float elevatorKickDelay;
        public static void ElevatorKickGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                elevatorKickDelay = Time.time + 0.5f;

                if (PhotonNetwork.IsMasterClient)
                    SpecialTimeRPC(GRElevatorManager._instance.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { gunTarget.GetPlayer().ActorNumber } }, (int)GRElevatorManager._instance.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                else
                    GRElevatorManager._instance.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)GRElevatorManager._instance.currentLocation });

                RPCProtection();
            }, new GunLibrary.GunSettings { when = () => Time.time > elevatorKickDelay });
        }

        public static void ElevatorKickAll()
        {
            if (PhotonNetwork.IsMasterClient)
                SpecialTimeRPC(GRElevatorManager._instance.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { Receivers = ReceiverGroup.Others }, (int)GRElevatorManager._instance.currentLocation, 2, GRElevatorManager.LowestActorNumberInElevator());
            else
                GRElevatorManager._instance.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)GRElevatorManager._instance.currentLocation });
        }

        public static void ElevatorKickAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    if (PhotonNetwork.IsMasterClient)
                        SpecialTimeRPC(GRElevatorManager._instance.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { nearbyPlayer.GetPlayer().ActorNumber } }, (int)GRElevatorManager._instance.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                    else
                        GRElevatorManager._instance.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)GRElevatorManager._instance.currentLocation });

                    RPCProtection();
                }
            }
        }

        public static void ElevatorKickOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0)
            {
                foreach (VRRig rig in touchedPlayers)
                {
                    if (PhotonNetwork.IsMasterClient)
                        SpecialTimeRPC(GRElevatorManager._instance.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { rig.GetPlayer().ActorNumber } }, (int)GRElevatorManager._instance.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                    else
                        GRElevatorManager._instance.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)GRElevatorManager._instance.currentLocation });

                    RPCProtection();
                }
            }
        }

        public static void CreatePeerBase()
        {
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Tcp;
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.peerBase = new TPeer()
            {
                DoFraming = true,
                photonPeer = PhotonNetwork.NetworkingClient.LoadBalancingPeer,
                usedTransportProtocol = ConnectionProtocol.Tcp
            };
        }

        public static void UnloadPeerBase()
        {
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Udp;
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.peerBase = new EnetPeer
            {
                photonPeer = PhotonNetwork.NetworkingClient.LoadBalancingPeer,
                usedTransportProtocol = ConnectionProtocol.Udp
            };
            References.Network.ReturnToSinglePlayer();
        }

        public static void BetaShuttleFollowCommand(Player player)
        {
            PhotonNetworkController.Instance.FriendIDList.Add(player.UserId);

            object[] groupJoinSendData = new object[2];
            groupJoinSendData[0] = PhotonNetworkController.Instance.shuffler;
            groupJoinSendData[1] = PhotonNetworkController.Instance.keyStr;
            NetEventOptions netEventOptions = new NetEventOptions { TargetActors = new[] { player.ActorNumber } };

            RoomSystem.SendEvent(11, groupJoinSendData, netEventOptions, false);
        }

        public static float greyZoneDelay;

        public static Coroutine wipeOverride;
        public static IEnumerator ClearOverride()
        {
            yield return new WaitUntil(() => !Networking.InRoom);
            SerializePatch.OverrideSerialization = null;

            wipeOverride = null;
        }

        public static float spazGreyDelay;
        public static bool greyState;

        public static void KickAllInParty()
        {
            if (FriendshipGroupDetection.Instance.IsInParty)
            {
                partyLastCode = PhotonNetwork.CurrentRoom.Name;
                waitForPlayerJoin = false;
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(Room.RandomRoomName(), JoinType.ForceJoinWithParty);
                partyTime = Time.time + 0.25f;
                partyKickReconnecting = false;
                amountPartying = FriendshipGroupDetection.Instance.myPartyMemberIDs.Count - 1;
                Notifications.SendNotification("<color=grey>[</color><color=purple>PARTY</color><color=grey>]</color> Kicking " + amountPartying + " party members, please be patient..");
            }
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not in a party.");
        }

        public static void BanAllInParty()
        {
            if (FriendshipGroupDetection.Instance.IsInParty)
            {
                partyLastCode = PhotonNetwork.CurrentRoom.Name;
                waitForPlayerJoin = true;
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(References.Computer.anywhereTwoWeek[Random.Range(0, References.Computer.anywhereTwoWeek.Length)], JoinType.ForceJoinWithParty);
                partyTime = Time.time + 0.25f;
                partyKickReconnecting = false;
                amountPartying = FriendshipGroupDetection.Instance.myPartyMemberIDs.Count - 1;
                Notifications.SendNotification("<color=grey>[</color><color=purple>PARTY</color><color=grey>]</color> Banning " + amountPartying + " party members, please be patient..");
            }
            else
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not in a party.");
        }

        public static Coroutine partyKickDelayCoroutine;
        public static IEnumerator PartyKickDelay(bool ban)
        {
            yield return new WaitForSeconds(0.25f);

            if (ban)
                BanAllInParty();
            else
                KickAllInParty();

            Coroutine thisCoroutine = partyKickDelayCoroutine;
            partyKickDelayCoroutine = null;

            Coroutines.instance.StopCoroutine(thisCoroutine);
        }

        public static bool previousInParty;
        public static void AutoPartyKick()
        {
            if (FriendshipGroupDetection.Instance.IsInParty && !previousInParty)
                partyKickDelayCoroutine ??= Coroutines.instance.StartCoroutine(PartyKickDelay(false));

            previousInParty = FriendshipGroupDetection.Instance.IsInParty;
        }

        public static void AutoPartyBan()
        {
            if (FriendshipGroupDetection.Instance.IsInParty && !previousInParty)
                partyKickDelayCoroutine ??= Coroutines.instance.StartCoroutine(PartyKickDelay(true));

            previousInParty = FriendshipGroupDetection.Instance.IsInParty;
        }

        private static float breakDelay;
        public static void PartyBreakNetworkTriggers()
        {
            if (FriendshipGroupDetection.Instance.IsInParty && Time.time > breakDelay)
            {
                breakDelay = Time.time + 1f;
                FriendshipGroupDetection.Instance.photonView.RPC("PartyMemberIsAboutToGroupJoin", RpcTarget.All, Array.Empty<object>());
            }
        }

        public static bool legacyKickFreeze;

        /// <summary>
        /// Indicates whether event optimization is enabled. When enabled, it reduces network load by limiting certain RPC calls and adjusting serialization rates.
        /// </summary>
        private static bool _optimizeEvents;
        public static bool OptimizeEvents
        {
            get => _optimizeEvents;
            set
            {
                if (_optimizeEvents != value)
                {
                    _optimizeEvents = value;
                    if (_optimizeEvents)
                    {
                        if (legacyKickFreeze)
                            SerializePatch.OverrideSerialization = () => false;
                        else
                        {
                            PhotonNetwork.SerializationRate = 2;
                            RPCFilter.FilteredRPCs["OnHandTapRPC"] = () => false;
                            RPCFilter.FilteredRPCs["RPC_UpdateCosmeticsWithTryonPacked"] = () => false;

                            SerializePatch.OverrideSerialization = () =>
                            {
                                References.LocalRig.GetPhotonView().Serialize();
                                return true;
                            };
                        }
                    }
                    else
                    {
                        if (SerializePatch.OverrideSerialization != null)
                            SerializePatch.OverrideSerialization = null;

                        PhotonNetwork.SerializationRate = 10;
                        RPCFilter.FilteredRPCs.Remove("OnHandTapRPC");
                        RPCFilter.FilteredRPCs.Remove("RPC_UpdateCosmeticsWithTryonPacked");
                    }
                }
            }
        }

        public static void PartyKickGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    OptimizeEvents = true;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        if (lockTarget == null && FriendshipGroupDetection.Instance.IsInMyGroup(gunTarget.GetPlayer().UserId))
                        {
                            for (int i = 0; i < 3970; i++)
                                FriendshipGroupDetection.Instance.photonView.RPC("RequestPartyGameMode", gunTarget.GetPhotonPlayer(),
                                    new object[] { GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)] });

                            RPCProtection();
                        }

                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    OptimizeEvents = false;
                    if (gunLocked)
                        gunLocked = false;
                } });
        }

        public static void PartyKickAll()
        {
            SerializePatch.OverrideSerialization = () => false;

            if (Time.time > kickDelay)
            {
                kickDelay = Time.time + 10f;
                for (int i = 0; i < 3950; i++)
                    FriendshipGroupDetection.Instance.photonView.RPC
                    (
                        "RequestPartyGameMode",
                        new RaiseEventOptions
                        {
                            TargetActors =
                            References.Network.PlayerListOthers
                                .Where(plr => FriendshipGroupDetection.Instance.IsInMyGroup(plr.UserId))
                                .Select(plr => plr.ActorNumber).ToArray()
                        },
                        new object[]
                        {
                            GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)]
                        }
                    );

                RPCProtection();
            }
        }

        public static void PartyKickAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                SerializePatch.OverrideSerialization = () => false;
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    for (int i = 0; i < 3950; i++)
                        FriendshipGroupDetection.Instance.photonView.RPC("RequestPartyGameMode", nearbyPlayer.GetPhotonPlayer(),
                            new object[] { GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)] });

                    RPCProtection();
                }
            }
            else
                OptimizeEvents = false;
        }

        public static void PartyKickOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0)
            {
                SerializePatch.OverrideSerialization = () => false;
                foreach (VRRig rig in touchedPlayers)
                {
                    for (int i = 0; i < 3950; i++)
                        FriendshipGroupDetection.Instance.photonView.RPC("RequestPartyGameMode", rig.GetPhotonPlayer(),
                            new object[] { GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)] });

                    RPCProtection();
                }
            }
            else
                OptimizeEvents = false;
        }

        private static float antiReportLagDelay;
        public static void AntiReportLag()
        {
            if (Time.time > antiReportLagDelay)
            {
                List<int> actors = new List<int>();

                Safety.AntiReport((vrrig, position) =>
                {
                    antiReportLagDelay = Time.time + 0.1f;
                    actors.Add(GetPlayerFromVRRig(vrrig).ActorNumber);
                    Notifications.SendNotification("<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, they are being lagged.");
                });

                if (actors.Count > 0)
                    LagPlayer(actors);
            }
        }

        public static float setMasterDelay;
        public static void SetMasterClient(bool skip = false)
        {
            if (Networking.IsMasterClient)
                return;
            if (PhotonNetwork.PlayerList.Length > 5)
            {
                if (!skip)
                    Notifications.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> {PhotonNetwork.PlayerList.Length - 5} people must leave for this mod to work.");
                return;
            }
            if (Time.time > setMasterDelay)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                setMasterDelay = Time.time + 5f;
            }
        }

        public static void SetRoomStatus(bool status)
        {
            Dictionary<byte, object> dictionary = new Dictionary<byte, object>
            {
                { OperationCode.GetProperties, new Hashtable { { GamePropertyKey.IsOpen, status }, { GamePropertyKey.IsVisible, status }, { GamePropertyKey.MaxPlayers, status ? 0 : PhotonNetworkController.Instance.currentJoinTrigger.GetRoomSize(SubscriptionManager.IsLocalSubscribed()) } } },
                { OperationCode.AuthenticateOnce, null }
            };

            PhotonNetwork.CurrentRoom.LoadBalancingClient.LoadBalancingPeer.SendOperation(
                OperationCode.SetProperties,
                dictionary,
                SendOptions.SendReliable
            );
            GorillaScoreboardTotalUpdater.instance.UpdateActiveScoreboards();
        }

        public static void DestroyGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                DestroyPlayer(NetPlayerToPlayer(GetPlayerFromVRRig(gunTarget)));
                destroyDelay = Time.time + 0.5f;
            }, new GunLibrary.GunSettings { when = () => Time.time > destroyDelay });
        }

        public static void DestroyAll()
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
                DestroyPlayer(player);
        }

        public static void DestroyAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    DestroyPlayer(NetPlayerToPlayer(GetPlayerFromVRRig(nearbyPlayer)));
                }
            }
        }

        public static void DestroyOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0)
            {
                foreach (VRRig rig in touchedPlayers)
                {
                    DestroyPlayer(NetPlayerToPlayer(rig.GetPlayer()));
                }
            }
        }

        public static void DestroyPlayer(NetPlayer player) =>
            PhotonNetwork.OpRemoveCompleteCacheOfPlayer(player.ActorNumber);

        public static float rockDebounce;

        public static void BetaSetStatus(RoomSystem.StatusEffects state, RaiseEventOptions reo)
        {
            if (!Networking.IsMasterClient)
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
            else
            {
                object[] statusSendData = new object[1];
                statusSendData[0] = (int)state;
                object[] sendEventData = new object[3];
                sendEventData[0] = References.Network.ServerTimestamp;
                sendEventData[1] = (byte)2;
                sendEventData[2] = statusSendData;
                PhotonNetwork.RaiseEvent(Constants.Network.ROOM_SYSTEM, sendEventData, reo, SendOptions.SendUnreliable);
            }
        }

        public static float slowDelay;

        public static float vibrateDelay;

        public static void BreakAudioGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", lockTarget.GetPlayer(), 111, false, 999999f);
                    RPCProtection();
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void BreakAudioAll()
        {
            if (rightTrigger > 0.5f)
            {
                References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 111, false, 999999f);
            }
        }

        public static Coroutine RopeCoroutine;
        public static IEnumerator RopeEnableRig()
        {
            yield return new WaitForSeconds(0.3f);
            References.LocalRig.enabled = true;
        }

        public static void BetaSetRopeVelocity(int RopeId, Vector3 Velocity)
        {
            Velocity = Velocity.ClampMagnitudeSafe(15f);

            if (RopeSwingManager.instance.ropes.TryGetValue(RopeId, out GorillaRopeSwing Rope))
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

                    RopeCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());

                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = ClosestNode.transform.position;
                }

                if (Vector3.Distance(ServerPos, ClosestNode.transform.position) < 5f)
                    RopeSwingManager.instance.SendSetVelocity_RPC(RopeId, ClosestNode.index, Velocity, true);
                else
                    RopeDelay = 0f;

                RPCProtection();
            }
        }

        public static void BetaSetRopeVelocity(GorillaRopeSwing Rope, Vector3 Velocity) =>
            BetaSetRopeVelocity(RopeSwingManager.instance.ropes.FirstOrDefault(x => x.Value == Rope).Key, Velocity);

        private static float randomRopeDelay;
        private static GorillaRopeSwing randomRope;
        public static GorillaRopeSwing GetRandomRope()
        {
            if (Time.time > randomRopeDelay)
            {
                randomRopeDelay = Time.time + 0.5f;
                randomRope = RopeSwingManager.instance.ropes.Values.OrderBy(_ => Random.value).FirstOrDefault();
            }
            return randomRope;
        }

        private static float RopeDelay;
        public static void JoystickRopeControl()
        {
            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;

            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            if (stick.sqrMagnitude > 0.0025f && Time.time > RopeDelay)
            {
                RopeDelay = Time.time + 0.125f;

                GorillaRopeSwing rope = GetRandomType<GorillaRopeSwing>(0.25f);
                BetaSetRopeVelocity(rope, new Vector3(stick.x * 100f, stick.y * 100f, 0f));
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
                        BetaSetRopeVelocity(gunTarget, RandomVector3(100f));
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
                BetaSetRopeVelocity(rope, RandomVector3(100f));
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
                    BetaSetRopeVelocity(randomRig.currentRopeSwing, RandomVector3(100f));
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
                        BetaSetRopeVelocity(gunTarget, RandomVector3(100f));
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
                BetaSetRopeVelocity(rope, (NewPointer.transform.position - rope.transform.position).normalized * 100f);
            }, new GunLibrary.GunSettings { when = () => Time.time > RopeDelay });
        }

        public static Coroutine CritterCoroutine;
        public static float critterGrabDelay;
        public static void InstantParty()
        {
            if (Time.time > Fun.instantPartyDelay)
            {
                Fun.instantPartyDelay = Time.time + 0.1f;

                FriendshipGroupDetection.Instance.suppressPartyCreationUntilTimestamp = 0f;
                FriendshipGroupDetection.Instance.groupCreateAfterTimestamp = 0f;

                List<int> provisionalMembers = FriendshipGroupDetection.Instance.playersInProvisionalGroup;

                if (provisionalMembers.Count > 0)
                {
                    Color targetColor = GTColor.RandomHSV(FriendshipGroupDetection.Instance.braceletRandomColorHSVRanges);
                    FriendshipGroupDetection.Instance.myBraceletColor = targetColor;

                    List<int> members = new List<int> { PhotonNetwork.LocalPlayer.ActorNumber };
                    members.AddRange(from player in PhotonNetwork.PlayerListOthers where FriendshipGroupDetection.Instance.IsInMyGroup(player.UserId) || provisionalMembers.Contains(player.ActorNumber) select player.ActorNumber);
                    FriendshipGroupDetection.Instance.SendPartyFormedRPC(FriendshipGroupDetection.PackColor(targetColor), members.ToArray(), false);
                    RPCProtection();
                }
            }
        }

        public static bool instantCreate;


        public static void KickToSpecificRoom()
        {
            if (Time.time < timeMenuStarted + 5f)
                return;

            PromptText("What would you like the room code to be?", () => Overpowered.specificRoom = keyboardInput.ToUpper(), () => Manager.Toggle("Kick to Specific Room"), "Done", "Cancel");
        }
        public static bool isRightHand;
        public static int amountPartying;
        public static bool waitForPlayerJoin;

        public static string partyLastCode;
        public static float partyTime;
        public static bool partyKickReconnecting;
    }
}
