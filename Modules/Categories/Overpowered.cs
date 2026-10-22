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
using GorillaLocomotion.Gameplay;
using GorillaTag;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using Qubit.Patches.Safety.RPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using JoinType = GorillaNetworking.JoinType;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Overpowered
    {
        #region Settings
        public static float guardianDelay;

        private static float playerColorDelay;

        public static readonly Dictionary<VRRig, int> materialState = new Dictionary<VRRig, int>();

        public static float guardianSpazDelay;
        public static bool guardianSpazToggle;

        public static float guardianProtectorDelay;

        private static float crashAllDelay;

        public static float spazDriverDelay;

        public static long? id;
        public static float setMapDelay;

        public static int masterVisualizationType;
        public static readonly string[] VisualizationTypeNames = { "Sphere", "Cube", "Tracer" };

        private static float reportDelay;

        public static float propHuntSpazDelay;
        public static bool propHuntSpazMode;

        public static float destroyDelay;

        public static float resourceIncrementDelay;

        public static HalloweenGhostChaser _lucy;
        private static float nextLucySearchTime;
        public static HalloweenGhostChaser Lucy
        {
            get
            {
                bool needsSearch = _lucy == null || _lucy.GetView.ViewID == 0 || (Networking.IsMasterClient && !_lucy.IsMine);
                if (needsSearch && Time.time > nextLucySearchTime)
                {
                    nextLucySearchTime = Time.time + 5f;

                    GameObject lucyObject = GameObject.Find("Environment Objects/05Maze_PersistentObjects/2025_Halloween1_PersistentObjects/Halloween Ghosts/Lucy/Halloween Ghost/FloatingChaseSkeleton");

                    // Fall back to whichever spawned ghost we own (or is active) if the scene path changes again
                    _lucy = lucyObject != null
                        ? lucyObject.GetComponent<HalloweenGhostChaser>()
                        : GetAllType<HalloweenGhostChaser>()
                            .Where(ghost => ghost != null && ghost.HasView && ghost.GetView.ViewID != 0)
                            .OrderByDescending(ghost => ghost.IsMine)
                            .ThenBy(ghost => ghost.gameObject.activeInHierarchy ? 0 : 1)
                            .FirstOrDefault();
                }

                return _lucy;
            }
            set => _lucy = value;
        }

        public static HalloweenGhostChaser GetChaser() => Lucy;

        public static void ForceUnloadCustomMap()
        {
            VirtualStumpSerializer serializer = GetAllType<VirtualStumpSerializer>().FirstOrDefault();
            if (serializer == null)
                return;

            serializer.UnloadMapSynced();
            RPCProtection();
        }

        private static float scDelay;

        public static void SizeChanger()
        {
            Movement.sizeScale = Mathf.Clamp(Movement.sizeScale, 0.1f, 10f);
            Movement.SizeChanger();
            Movement.sizeScale = Mathf.Clamp(Movement.sizeScale, 0.1f, 10f);

            if (Time.time > scDelay)
            {
                scDelay = Time.time + 1.01f;
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateNativeSize", RpcTarget.Others, new object[] { Movement.sizeScale });
            }
        }

        public static void DisableSizeChanger()
        {
            Movement.DisableSizeChanger();
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateNativeSize", RpcTarget.Others, new object[] { 1f });
        }

        public static LurkerGhost _lurker;
        public static float lurkerDelay;
        public static LurkerGhost Lurker
        {
            get
            {
                bool needsSearch = _lurker == null || _lurker.GetView.ViewID == 0 || (Networking.IsMasterClient && !_lurker.IsMine);
                if (needsSearch)
                {
                    _lurker = GetAllType<LurkerGhost>()
                        .Where(ghost => ghost != null && ghost.HasView && ghost.GetView.ViewID != 0)
                        .OrderByDescending(ghost => ghost.IsMine)
                        .ThenBy(ghost => ghost.gameObject.activeInHierarchy ? 0 : 1)
                        .FirstOrDefault();
                }

                return _lurker;
            }
            set => _lurker = value;
        }

        public static void LurkerAttack(NetPlayer player)
        {
            LurkerGhost lurker = Lurker;
            if (lurker == null)
                return;

            if (lurker.IsMine)
            {
                lurker.currentState = LurkerGhost.ghostState.possess;
                lurker.targetPlayer = player;
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void LurkerAttackGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                LurkerAttack(lockTarget.GetPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void LurkerAttackAll()
        {
            LurkerGhost lurker = Lurker;
            if (lurker == null)
                return;

            if (ViewSerialization.OverrideSerialization != null)
            {
                ViewSerialization.OverrideSerialization = () =>
                {
                    MassSerialize(true, new[] { lurker.GetView });
                    return false;
                };
            }

            if (lurker.IsMine)
            {
                foreach (NetPlayer player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    lurker.currentState = LurkerGhost.ghostState.possess;
                    lurker.targetPlayer = player;
                    SendSerialize(lurker.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void MoveLurkerGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                LurkerGhost lurker = Lurker;
                if (lurker == null)
                    return;

                if (lurker.IsMine)
                    lurker.transform.position = NewPointer.transform.position + Vector3.up;
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            });
        }

        public static void DespawnLurker()
        {
            LurkerGhost lurker = Lurker;
            if (lurker == null)
                return;

            if (lurker.IsMine)
            {
                lurker.currentState = LurkerGhost.ghostState.patrol;
                lurker.targetPlayer = null;
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void SpazLurker()
        {
            LurkerGhost lurker = Lurker;
            if (lurker == null)
                return;

            if (lurker.IsMine)
            {
                if (Time.time > lurkerDelay)
                {
                    lurker.currentState = lurker.currentState == LurkerGhost.ghostState.charge ? LurkerGhost.ghostState.patrol : LurkerGhost.ghostState.charge;
                    lurkerDelay = Time.time + 0.1f;
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void BreakLurker()
        {
            SpazLurker();
            lurkerDelay = 0f;
        }

        public static void AnnoyingLurker()
        {
            LurkerGhost lurker = Lurker;
            if (lurker == null)
                return;

            if (lurker.IsMine)
            {
                if (Time.time > lurkerDelay)
                {
                    lurker.currentState = lurker.currentState == LurkerGhost.ghostState.possess ? LurkerGhost.ghostState.charge : LurkerGhost.ghostState.possess;
                    lurker.targetPlayer = GetRandomPlayer(true);
                    lurkerDelay = Time.time + 0.1f;
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void BecomeLurker()
        {
            LurkerGhost lurker = Lurker;
            if (!Networking.IsMasterClient || lurker == null)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

            lurker.transform.position = References.BodyCollider.transform.position;
            lurker.transform.rotation = References.HeadCollider.transform.rotation;

            lurker.currentState = LurkerGhost.ghostState.charge;
            lurker.targetPlayer = null;
        }

        private static float releaseDelay;

        private static float flingDelay;

        private static float antiReportFlingDelay;

        private static float thingdeb;

        private static float slamDel;
        private static bool flip;

        private static float closeRoomDelay;

        public static float zaWarudoNotificationDelay;

        public static Coroutine ZaWarudo_StartCoroutineVariable;
        public static Coroutine ZaWarudo_EndCoroutineVariable;

        public static AudioClip ZaWarudo_Start;
        public static AudioClip ZaWarudo_Stop;

        private static bool zaWarudoTrigger;

        public static int lagIndex = 2;
        public static int lagAmount;
        public static float lagDelay;
        public static readonly string[] LagPowerNames = { "Light", "Heavy", "Spike", "Stutter", "Freeze" };
        public static readonly int[] LagAmounts = { 40, 113, 425, 1000, 3800 };
        public static readonly float[] LagDelays = { 0.1f, 0.25f, 1f, 3f, 8f };

        public static int lagTypeIndex;
        public static readonly string[] LagTypeNames = { "Default", "Backup" };

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

        public const int BarrelIndex = 618;

        private static float notifyTime;

        public static float delay;

        public static bool kickToPublic;
        public static bool rejoinOnKick;
        public static string specificRoom;

        private static float kickDelay;

        private static float elevatorKickDelay;

        public static float greyZoneDelay;

        public static Coroutine wipeOverride;

        public static float spazGreyDelay;
        public static bool greyState;

        public static Coroutine partyKickDelayCoroutine;

        public static bool previousInParty;

        private static float breakDelay;

        public static bool legacyKickFreeze;




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
                            ViewSerialization.OverrideSerialization = () => false;
                        else
                        {
                            PhotonNetwork.SerializationRate = 2;
                            RPCFilter.FilteredRPCs["OnHandTapRPC"] = () => false;
                            RPCFilter.FilteredRPCs["RPC_UpdateCosmeticsWithTryonPacked"] = () => false;

                            ViewSerialization.OverrideSerialization = () =>
                            {
                                References.LocalRig.GetPhotonView().Serialize();
                                return true;
                            };
                        }
                    }
                    else
                    {
                        if (ViewSerialization.OverrideSerialization != null)
                            ViewSerialization.OverrideSerialization = null;

                        PhotonNetwork.SerializationRate = 10;
                        RPCFilter.FilteredRPCs.Remove("OnHandTapRPC");
                        RPCFilter.FilteredRPCs.Remove("RPC_UpdateCosmeticsWithTryonPacked");
                    }
                }
            }
        }

        private static float antiReportLagDelay;

        public static float setMasterDelay;

        public static float rockDebounce;

        public static float slowDelay;

        public static float vibrateDelay;

        public static Coroutine RopeCoroutine;

        private static float randomRopeDelay;
        private static GorillaRopeSwing randomRope;

        private static float RopeDelay;

        public static bool instantCreate;
        public static bool isRightHand;
        public static int amountPartying;
        public static bool waitForPlayerJoin;

        public static string partyLastCode;
        public static float partyTime;
        public static bool partyKickReconnecting;
        #endregion

        #region Methods
        public static void AlwaysGuardian() =>
            BecomeGuardian();
        public static void SetColorSelf(int color) =>
            SetMonkeBallTeams(new Dictionary<int, int> { { Networking.LocalPlayer.ActorNumber, color } });

        public static void SetColorGun(int color)
        {
            GunLibrary.Target(gunTarget =>
            {
                playerColorDelay = Time.time + 0.1f;
                if (PhotonNetwork.IsMasterClient)
                    SetMonkeBallTeams(new Dictionary<int, int> { { GetPlayerFromVRRig(gunTarget).ActorNumber, color } });
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings { when = () => Time.time > playerColorDelay });
        }

        public static void SetColorAll(int color)
        {
            if (PhotonNetwork.IsMasterClient)
                SetMonkeBallTeams(ManagerRegistry.NetworkSystem.AllNetPlayers.ToDictionary(p => p.ActorNumber, p => color));
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void StrobeColorSelf()
        {
            if (Time.time > playerColorDelay)
            {
                playerColorDelay = Time.time + 0.1f;
                if (Networking.IsMasterClient)
                    SetColorSelf(Time.time % 0.2f > 0.1f ? 1 : 0);
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
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
                            SetMonkeBallTeams(new Dictionary<int, int> { { lockTarget.GetPlayer().ActorNumber, Time.time % 0.2f > 0.1f ? 1 : 0 } });
                        else
                            Notifications.SendFormat("red", "ERROR", "You are not master client.");
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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void StrobeColorAll()
        {
            if (Time.time > playerColorDelay)
            {
                playerColorDelay = Time.time + 0.1f;
                if (Networking.IsMasterClient)
                    SetColorAll(Time.time % 0.2f > 0.1f ? 1 : 0);
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }
        }
        public static void GuardianProtector()
        {
            if (Networking.InRoom)
            {

                if (!IsGuardian(PhotonNetwork.LocalPlayer)) return;
                foreach (TappableGuardianIdol tgi in GetAllType<TappableGuardianIdol>())
                {
                    if (!tgi.manager || !tgi.manager.photonView) continue;
                    foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal && Vector3.Distance(rig.transform.position, tgi.transform.position) < 2f && Time.time > guardianProtectorDelay))
                    {
                        SetVelocityPlayer(rig.GetPlayer(), (rig.transform.position - tgi.transform.position).normalized * 50f);
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
                SetVelocityPlayer(target, rig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - rig.transform.position).normalized * 50f : rig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
                RPCProtection();
            }
        }
        public static void GuardianKickGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > crashAllDelay)
                {
                    crashAllDelay = Time.time + 0.1f;
                    SetVelocityPlayer(lockTarget.GetPlayer(), lockTarget.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - lockTarget.transform.position).normalized * 50f : lockTarget.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
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
                    SetVelocityPlayer(rig.GetPlayer(), rig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - rig.transform.position).normalized * 50f : rig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left * 50f);
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

                SetVelocityPlayer(target, (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
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
                    SetVelocityPlayer(lockTarget.GetPlayer(), (lockTarget.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
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
                    SetVelocityPlayer(rig.GetPlayer(), (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                    RPCProtection();
                }
            }
        }

        public static void BecomeDriver()
        {
            if (PhotonNetwork.IsMasterClient)
                ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, true, PhotonNetwork.LocalPlayer.ActorNumber);
            else
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            ManagerRegistry.VirtualStumpSerializer.photonView.OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
        }
        public static void ApplyMasterVisualizationType(int index) => masterVisualizationType = index;

        public static void VisualizeMasterClient()
        {
            if (Visuals.DoPerformanceCheck())
                return;

            if (Networking.IsMasterClient)
                return;

            VRRig rig = ManagerRegistry.NetworkSystem.MasterClient.VRRig();
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
        public static void DelayBanGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                gunLocked = true;
                lockTarget = gunTarget;

                if (References.LocalRig.IsTagged())
                {
                    Notifications.SendFormat("red", "ERROR", "You must not be tagged.");
                    return;
                }

                if (!lockTarget.IsTagged())
                {
                    Notifications.SendFormat("red", "ERROR", "The target must be tagged.");
                    return;
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You must not be master client.");
                    return;
                }

                if (Time.time > reportDelay)
                {
                    reportDelay = Time.time + 0.5f;
                    GorillaPlayerScoreboardLine.ReportPlayer(lockTarget.GetPlayer().UserId, GorillaPlayerLineButton.ButtonType.Cheating, lockTarget.GetPlayer().NickName);
                }

                ViewSerialization.OverrideSerialization = () =>
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
            }, new GunLibrary.GunSettings
            {
                when = () => !gunLocked,
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        ViewSerialization.OverrideSerialization = null;
                    }
                }
            });
        }

        public static void DelayBanAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                if (References.LocalRig.IsTagged())
                {
                    Notifications.SendFormat("red", "ERROR", "You must not be tagged.");
                    return true;
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You must not be master client.");
                    return true;
                }

                lockTarget.GetPlayer();
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 positionArchive = References.LocalRig.transform.position;
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(player => !player.IsMasterClient && player.VRRig().IsTagged()).Select(player => player.ActorNumber).ToArray() });

                References.LocalRig.transform.position = new Vector3(99999f, 99999f, 99999f);
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });

                foreach (NetPlayer player in ManagerRegistry.NetworkSystem.PlayerListOthers)
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
                SetVelocityPlayer(target, (rig.transform.position.y > 55f ? Vector3.right : Vector3.up) * 50f);
                RPCProtection();
            }
        }

        public static void SetVelocityPlayer(NetPlayer victim, Vector3 velocity)
        {
            if (velocity.sqrMagnitude > 20f)
                velocity = Vector3.Normalize(velocity) * 20f;

            if (IsGuardian(Networking.LocalPlayer))
            {
                GetNetworkViewFromVRRig(GetVRRigFromPlayer(victim)).SendRPC("GrabbedByPlayer", victim, true, false, false);
                GetNetworkViewFromVRRig(GetVRRigFromPlayer(victim)).SendRPC("DroppedByPlayer", victim, velocity);
            }
            else
                Notifications.SendFormat("red", "ERROR", "You must be guardian.");
        }

        public static void SetVelocityTargetGroup(RpcTarget victim, Vector3 velocity)
        {
            if (velocity.sqrMagnitude > 20f)
                velocity = Vector3.Normalize(velocity) * 20f;

            if (IsGuardian(Networking.LocalPlayer))
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
                            GetNetworkViewFromVRRig(GetVRRigFromPlayer(ManagerRegistry.NetworkSystem.MasterClient)).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                            GetNetworkViewFromVRRig(GetVRRigFromPlayer(ManagerRegistry.NetworkSystem.MasterClient)).SendRPC("DroppedByPlayer", RpcTarget.Others, velocity);
                            break;
                        }
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You must be guardian.");
        }
        public static void GuardianGrabGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (IsGuardian(Networking.LocalPlayer))
                {
                    GetNetworkViewFromVRRig(gunTarget).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                    RPCProtection();
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
                Grabbing.grabDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Grabbing.grabDelay });
        }

        public static void GuardianGrabAll()
        {
            if (rightGrab && Time.time > Grabbing.grabDelay)
            {
                Grabbing.grabDelay = Time.time + 0.1f;
                if (IsGuardian(Networking.LocalPlayer))
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                    {
                        GetNetworkViewFromVRRig(plr).SendRPC("GrabbedByPlayer", RpcTarget.Others, true, false, false);
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
            }
        }
        public static void GuardianReleaseGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (IsGuardian(Networking.LocalPlayer))
                {
                    GetNetworkViewFromVRRig(gunTarget).SendRPC("DroppedByPlayer", RpcTarget.Others, new Vector3(0f, 0f, 0f));
                    RPCProtection();
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");

                releaseDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > releaseDelay });
        }

        public static void GuardianReleaseAll()
        {
            if (rightTrigger > 0.5f && Time.time > releaseDelay)
            {
                releaseDelay = Time.time + 0.1f;
                if (IsGuardian(Networking.LocalPlayer))
                {
                    foreach (var plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                    {
                        GetNetworkViewFromVRRig(plr).SendRPC("DroppedByPlayer", RpcTarget.Others, new Vector3(0f, 0f, 0f));
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
            }
        }
        public static void GuardianFlingGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                SetVelocityPlayer(GetPlayerFromVRRig(gunTarget), new Vector3(0f, 19.9f, 0f));
                RPCProtection();
                flingDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > flingDelay });
        }

        public static void GuardianFlingAll()
        {
            if (rightTrigger > 0.5f && Time.time > flingDelay)
            {
                flingDelay = Time.time + 0.1f;

                SetVelocityTargetGroup(RpcTarget.Others, new Vector3(0f, 19.9f, 0f));
                RPCProtection();
            }
        }

        public static void GuardianSpazPlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    SetVelocityPlayer(lockTarget.GetPlayer(), RandomVector3(50f));
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
                SetVelocityTargetGroup(RpcTarget.Others, RandomVector3(50f));
                RPCProtection();
            }
        }
        public static void AntiReportFling()
        {
            if (Time.time > antiReportFlingDelay)
            {
                Safety.AntiReport((vrrig, position) =>
                {
                    antiReportFlingDelay = Time.time + 0.1f;
                    SetVelocityPlayer(GetPlayerFromVRRig(vrrig), (vrrig.transform.position - position) * 50f);
                    Notifications.SendFormat("purple", "ANTI-REPORT", "" + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, they have been flung.");
                });
            }
        }

        public static void GuardianPhysicalFreezeGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > flingDelay)
                {
                    SetVelocityPlayer(lockTarget.GetPlayer(), Vector3.zero);
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
                SetVelocityTargetGroup(RpcTarget.Others, Vector3.zero);
                RPCProtection();
            }
        }

        public static void GuardianBringPlayer(NetPlayer player)
        {
            if (Time.time > flingDelay)
            {
                SetVelocityPlayer(player, (References.BodyCollider.transform.position - GetVRRigFromPlayer(player).transform.position).normalized * 20f);
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
                    SetVelocityPlayer(player, Vector3.Normalize(NewPointer.transform.position - GetVRRigFromPlayer(player).transform.position) * 50f);
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
                    SetVelocityPlayer(lockTarget.GetPlayer(), (References.BodyCollider.transform.position - lockTarget.transform.position).normalized * 20f);
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
                    SetVelocityPlayer(GetPlayerFromVRRig(plr), (References.BodyCollider.transform.position - plr.transform.position).normalized * 20f);
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
                    SetVelocityPlayer(lockTarget.GetPlayer(), (lockTarget.transform.position - References.BodyCollider.transform.position).normalized * 20f);
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
                    SetVelocityPlayer(GetPlayerFromVRRig(plr), (plr.transform.position - References.BodyCollider.transform.position).normalized * 20f);
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

                    SetVelocityPlayer(rig.GetPlayer(), (targetPosition - rig.transform.position) * 1f);
                    RPCProtection();
                    index++;
                }
            }
        }
        public static void GuardianGiveFlyGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > thingdeb)
                {
                    if (lockTarget.rightThumb.calcT > 0.5f)
                    {
                        SetVelocityPlayer(lockTarget.GetPlayer(), lockTarget.headMesh.transform.forward * Movement._flySpeed);
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
                    SetVelocityPlayer(GetPlayerFromVRRig(plr), plr.headMesh.transform.forward * Movement._flySpeed);
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

                        SetVelocityPlayer(rig.GetPlayer(), vel);
                        thingdeb = Time.time + 0.1f;

                        if (Manager.GetIndex("Graphic Punch Mod").enabled)
                            Projectiles.SendProjectile(ThrowableProjectiles.FindProjectile("Apple"), rig.headMesh.transform.position, Vector3.down * 600f, new Color32(100, 0, 0, 255));
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
                    SetVelocityPlayer(GetPlayerFromVRRig(rig1), targetDirection);
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
                        SetVelocityPlayer(GetPlayerFromVRRig(plr), Vector3.Normalize(NewPointer.transform.position - plr.transform.position) * 50f);

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
                        SetVelocityPlayer(GetPlayerFromVRRig(plr), Vector3.Normalize(plr.transform.position - NewPointer.transform.position) * 50f);

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
                            SetVelocityPlayer(rig.GetPlayer(), (rig.transform.position - stump).normalized * 20f);
                            flingDelay = Time.time + 0.2f;
                        }
                    }
                }
            }
        }
        public static void GuardianEffectSpamHands()
        {
            if (rightGrab)
            {
                if (Time.time > slamDel)
                {
                    if (IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, References.RightHand.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", "You must be guardian.");

                    slamDel = Time.time + 0.05f;
                }
            }
            if (leftGrab)
            {
                if (Time.time > slamDel)
                {
                    if (IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, References.LeftHand.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", "You must be guardian.");

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
                    if (IsGuardian(Networking.LocalPlayer))
                    {
                        GameMode.ActiveNetworkHandler.NetView.GetView.RPC(flip ? "ShowSlamEffect" : "ShowSlapEffects", RpcTarget.All, NewPointer.transform.position, new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        RPCProtection();
                        flip = !flip;
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", "You must be guardian.");

                    slamDel = Time.time + 0.05f;
                }
            });
        }

        private static float freezeAllDelay;
        public static bool muteOnFreeze;
        private static bool acknowledgedFreeze;
        public static void FreezeServer(float delay = 1f, int eventCount = 11, RaiseEventOptions options = null)
        {
            if (!Networking.InRoom) return;

            if (!acknowledgedFreeze)
            {
                acknowledgedFreeze = true;
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
                    VoiceControl.MuteTarget(options);
            }

            if (Time.time > freezeAllDelay)
            {
                for (int i = 0; i < eventCount; i++)
                    ManagerRegistry.NetworkingClient.OpRaiseEvent(200, new object[] { PluginInfo.DiscordInvite }, options, SendOptions.SendUnreliable);

                RPCProtection();
                freezeAllDelay = Time.time + delay;
            }
        }
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
                PhotonNetwork.RaiseEvent(code, new object[] { PluginInfo.DiscordInvite }, options, SendOptions.SendUnreliable);
            }

            RPCProtection();
        }

        public static void ZaWarudo_enableMethod()
        {
            Libraries.Core.Assets.LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Overpowered/Timestop/start.ogg", "Audio/Mods/Overpowered/Timestop/start.ogg", clip => ZaWarudo_Start = clip);
            Libraries.Core.Assets.LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Overpowered/Timestop/end.ogg", "Audio/Mods/Overpowered/Timestop/end.ogg", clip => ZaWarudo_Stop = clip);
        }
        public static void ZaWarudo()
        {
            if (!Networking.InRoom) return;

            if (rightTrigger > 0.5f)
            {
                if (Manager.GetIndex("No Freeze Za Warudo").enabled)
                {
                    if (!Manager.GetIndex("No Freeze Za Warudo").enabled)
                        ViewSerialization.OverrideSerialization = () => false;

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

                ViewSerialization.OverrideSerialization = null;
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
        public static void ApplyLagPower(int index)
        {
            lagIndex = index;
            lagAmount = LagAmounts[index];
            lagDelay = LagDelays[index];
        }
        public static void ApplyLagType(int index) => lagTypeIndex = index;

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
                ManagerRegistry.NetworkingClient.OpRaiseEvent(eventIndex, data, raiseEventOptions, sendOptions);

            RPCProtection();
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

        public static void BarrelFlingGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, Vector3.up * 15000, Random.rotation, new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

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
        public static void BarrelFlingAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, Vector3.up * 15000, Random.rotation, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }
        public static void BarrelObliterateGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * int.MaxValue, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

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

        public static void BarrelObliterateAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * int.MaxValue, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }
        public static void BarrelPunchMod()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.isLocal && (Vector3.Distance(References.LeftHand.position, rig.headMesh.transform.position) < 0.25f || Vector3.Distance(References.RightHand.position, rig.headMesh.transform.position) < 0.25f))
                {
                    Vector3 targetDirection = rig.headMesh.transform.position - References.HeadCollider.transform.position;
                    ThrowableProjectiles.SendBarrelProjectile(rig.transform.position + (References.HeadCollider.transform.position - rig.headMesh.transform.position).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });

                    if (Manager.GetIndex("Graphic Punch Mod").enabled)
                        Projectiles.SendProjectile(ThrowableProjectiles.FindProjectile("Apple"), rig.headMesh.transform.position, Vector3.down * 600f, new Color32(100, 0, 0, 255));
                }
            }
        }

        public static void BarrelCrashGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * 10000f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });

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

        public static void BarrelCrashAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + Vector3.down * 0.2f, lockTarget.bodyTransform.up * 10000f, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
        }

        public static void BarrelKickGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 targetDirection = new Vector3(-71.33718f, 101.4977f, -93.09029f) - lockTarget.headMesh.transform.position;
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + (lockTarget.headMesh.transform.position - new Vector3(-71.33718f, 101.4977f, -93.09029f)).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { lockTarget.GetPlayer().ActorNumber } });
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

        public static void BarrelKickAll()
        {
            foreach (VRRig TargetRig in Players.ActiveRigs)
            {
                if (TargetRig.IsTagged()) continue;

                Vector3 targetDirection = new Vector3(-71.33718f, 101.4977f, -93.09029f) - TargetRig.headMesh.transform.position;
                ThrowableProjectiles.SendBarrelProjectile(TargetRig.transform.position + (TargetRig.headMesh.transform.position - new Vector3(-71.33718f, 101.4977f, -93.09029f)).normalized * 0.1f, targetDirection.normalized * 50f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
            }
        }

        public static void BarrelFlingTowardsGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + (References.HeadCollider.transform.position - lockTarget.headMesh.transform.position).normalized * 0.1f, (References.BodyCollider.transform.position - lockTarget.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(lockTarget.GetPlayer()).ActorNumber } });

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

        public static void BarrelFlingTowardsAll()
        {
            foreach (var TargetRig in Players.ActiveRigs.Where(TargetRig => !TargetRig.IsTagged()))
                ThrowableProjectiles.SendBarrelProjectile(TargetRig.transform.position + (References.HeadCollider.transform.position - TargetRig.headMesh.transform.position).normalized * 0.1f, (References.BodyCollider.transform.position - TargetRig.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(TargetRig.GetPlayer()).ActorNumber } }, true);
        }

        public static void CityKickGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ThrowableProjectiles.SendBarrelProjectile(lockTarget.transform.position + (lockTarget.transform.position - new Vector3(-71.14215f, 13.73829f, -95.17883f)).normalized * 0.1f, (new Vector3(-71.14215f, 13.73829f, -95.17883f) - lockTarget.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(lockTarget.GetPlayer()).ActorNumber } });

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

        public static void CityKickAll()
        {
            foreach (VRRig TargetRig in Players.ActiveRigs)
            {
                if (TargetRig.IsTagged()) continue;
                ThrowableProjectiles.SendBarrelProjectile(TargetRig.transform.position + (TargetRig.transform.position - new Vector3(-71.14215f, 13.73829f, -95.17883f)).normalized * 0.1f, (new Vector3(-71.14215f, 13.73829f, -95.17883f) - TargetRig.transform.position).normalized * 5000f, Quaternion.identity, new RaiseEventOptions { TargetActors = new[] { TargetRig.GetPlayer().ActorNumber } });
            }
        }
        public static bool IsModded(bool notify)
        {
            if (!Networking.InRoom) return false;
            bool modded = ManagerRegistry.NetworkSystem.GameModeString.Contains("MODDED_");
            if (!modded && notify && Time.time > notifyTime)
            {
                Notifications.SendFormat("red", "ERROR", "You are not in a modded gamemode. Use Utilla to create one, or join an already existing modded room.");
                notifyTime = Time.time + 1;
            }
            return modded;
        }

        public static void NearbyFollowCommand(GorillaFriendCollider friendCollider, Player player)
        {
            ManagerRegistry.PhotonNetworkController.FriendIDList.Add(player.UserId);

            object[] groupJoinSendData = new object[2];
            groupJoinSendData[0] = ManagerRegistry.PhotonNetworkController.shuffler;
            groupJoinSendData[1] = ManagerRegistry.PhotonNetworkController.keyStr;
            NetEventOptions netEventOptions = new NetEventOptions { TargetActors = new[] { player.ActorNumber } };

            if (friendCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId) && friendCollider.playerIDsCurrentlyTouching.Contains(player.UserId) && player != PhotonNetwork.LocalPlayer)
                RoomSystem.SendEvent(4, groupJoinSendData, netEventOptions, false);
            else if (!friendCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork.LocalPlayer.UserId))
                Notifications.SendFormat("red", "ERROR", "You are not in stump.");
        }

        public static IEnumerator StumpKickDelay(Action action, Action action2, float extraDelay = 0f, bool changeQueue = false)
        {
            ManagerRegistry.PhotonNetworkController.FriendIDList.Clear();
            yield return new WaitForSeconds(extraDelay);

            bool joinedRoomPatchEnabled = JoinedRoom.enabled;

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

            JoinedRoom.enabled = joinedRoomPatchEnabled;
        }

        public static void CreateKickRoom()
        {
            if (rejoinOnKick)
            {
                Libraries.Networking.Rooms.BroadcastRoom(specificRoom ?? RandomString(), true, ManagerRegistry.PhotonNetworkController.keyToFollow, ManagerRegistry.PhotonNetworkController.shuffler);
                Room.Reconnect();

                return;
            }

            Libraries.Networking.Rooms.CreateRoom(specificRoom ?? RandomString(), kickToPublic, 0, JoinType.JoinWithNearby);
        }

        public static void StumpKickGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer player = GetPlayerFromVRRig(gunTarget);
                kickDelay = Time.time + 0.5f;

                if (!References.Computer.friendJoinCollider.playerIDsCurrentlyTouching.Contains(player.UserId))
                {
                    Notifications.SendFormat("red", "ERROR", "The player must be in stump.");
                    return;
                }

                if (!ManagerRegistry.NetworkSystem.SessionIsPrivate)
                {
                    Notifications.SendFormat("red", "ERROR", "You must be in a private room.");
                    return;
                }

                Coroutines.instance.StartCoroutine(StumpKickDelay(() =>
                {
                    ManagerRegistry.PhotonNetworkController.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
                    ManagerRegistry.PhotonNetworkController.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');

                    NearbyFollowCommand(References.Computer.friendJoinCollider, NetPlayerToPlayer(player));
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
                if (!ManagerRegistry.NetworkSystem.SessionIsPrivate)
                {
                    Notifications.SendFormat("red", "ERROR", "You must be in a private room.");
                    return;
                }

                Coroutines.instance.StartCoroutine(StumpKickDelay(() =>
                {
                    ManagerRegistry.PhotonNetworkController.shuffler = Random.Range(0, 99).ToString().PadLeft(2, '0') + Random.Range(0, 99999999).ToString().PadLeft(8, '0');
                    ManagerRegistry.PhotonNetworkController.keyStr = Random.Range(0, 99999999).ToString().PadLeft(8, '0');

                    foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal() && References.Computer.friendJoinCollider.playerIDsCurrentlyTouching.Contains(rig.GetPlayer().UserId)))
                        NearbyFollowCommand(References.Computer.friendJoinCollider, NetPlayerToPlayer(rig.GetPlayer()));

                    RPCProtection();
                }, () =>
                {
                    CreateKickRoom();
                }));
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not in a room.");
        }
        public static void ElevatorKickGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                elevatorKickDelay = Time.time + 0.5f;

                if (PhotonNetwork.IsMasterClient)
                    SendTimedRPC(ManagerRegistry.GRElevatorManager.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { gunTarget.GetPlayer().ActorNumber } }, (int)ManagerRegistry.GRElevatorManager.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                else
                    ManagerRegistry.GRElevatorManager.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)ManagerRegistry.GRElevatorManager.currentLocation });

                RPCProtection();
            }, new GunLibrary.GunSettings { when = () => Time.time > elevatorKickDelay });
        }

        public static void ElevatorKickAll()
        {
            if (PhotonNetwork.IsMasterClient)
                SendTimedRPC(ManagerRegistry.GRElevatorManager.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { Receivers = ReceiverGroup.Others }, (int)ManagerRegistry.GRElevatorManager.currentLocation, 2, GRElevatorManager.LowestActorNumberInElevator());
            else
                ManagerRegistry.GRElevatorManager.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)ManagerRegistry.GRElevatorManager.currentLocation });
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
                        SendTimedRPC(ManagerRegistry.GRElevatorManager.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { nearbyPlayer.GetPlayer().ActorNumber } }, (int)ManagerRegistry.GRElevatorManager.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                    else
                        ManagerRegistry.GRElevatorManager.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)ManagerRegistry.GRElevatorManager.currentLocation });

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
                        SendTimedRPC(ManagerRegistry.GRElevatorManager.photonView, -750, "RemoteActivateTeleport", new RaiseEventOptions { TargetActors = new[] { rig.GetPlayer().ActorNumber } }, (int)ManagerRegistry.GRElevatorManager.currentLocation, 3, GRElevatorManager.LowestActorNumberInElevator());
                    else
                        ManagerRegistry.GRElevatorManager.SendRPC("RemoteElevatorButtonPress", RpcTarget.MasterClient, new[] { 3, (int)ManagerRegistry.GRElevatorManager.currentLocation });

                    RPCProtection();
                }
            }
        }

        public static void CreatePeerBase()
        {
            ManagerRegistry.NetworkingClient.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Tcp;
            ManagerRegistry.NetworkingClient.LoadBalancingPeer.peerBase = new TPeer()
            {
                DoFraming = true,
                photonPeer = ManagerRegistry.NetworkingClient.LoadBalancingPeer,
                usedTransportProtocol = ConnectionProtocol.Tcp
            };
        }

        public static void UnloadPeerBase()
        {
            ManagerRegistry.NetworkingClient.LoadBalancingPeer.TransportProtocol = ConnectionProtocol.Udp;
            ManagerRegistry.NetworkingClient.LoadBalancingPeer.peerBase = new EnetPeer
            {
                photonPeer = ManagerRegistry.NetworkingClient.LoadBalancingPeer,
                usedTransportProtocol = ConnectionProtocol.Udp
            };
            ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
        }

        public static void ShuttleFollowCommand(Player player)
        {
            ManagerRegistry.PhotonNetworkController.FriendIDList.Add(player.UserId);

            object[] groupJoinSendData = new object[2];
            groupJoinSendData[0] = ManagerRegistry.PhotonNetworkController.shuffler;
            groupJoinSendData[1] = ManagerRegistry.PhotonNetworkController.keyStr;
            NetEventOptions netEventOptions = new NetEventOptions { TargetActors = new[] { player.ActorNumber } };

            RoomSystem.SendEvent(11, groupJoinSendData, netEventOptions, false);
        }
        public static IEnumerator ClearOverride()
        {
            yield return new WaitUntil(() => !Networking.InRoom);
            ViewSerialization.OverrideSerialization = null;

            wipeOverride = null;
        }

        public static void KickAllInParty()
        {
            if (ManagerRegistry.FriendshipGroupDetection.IsInParty)
            {
                partyLastCode = PhotonNetwork.CurrentRoom.Name;
                waitForPlayerJoin = false;
                Libraries.Networking.Rooms.JoinRoom(Libraries.Networking.Rooms.RandomRoomName(), JoinType.ForceJoinWithParty);
                partyTime = Time.time + 0.25f;
                partyKickReconnecting = false;
                amountPartying = ManagerRegistry.FriendshipGroupDetection.myPartyMemberIDs.Count - 1;
                Notifications.SendFormat("purple", "PARTY", "Kicking " + amountPartying + " party members, please be patient..");
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not in a party.");
        }

        public static void BanAllInParty()
        {
            if (ManagerRegistry.FriendshipGroupDetection.IsInParty)
            {
                partyLastCode = PhotonNetwork.CurrentRoom.Name;
                waitForPlayerJoin = true;
                Libraries.Networking.Rooms.JoinRoom(References.Computer.anywhereTwoWeek[Random.Range(0, References.Computer.anywhereTwoWeek.Length)], JoinType.ForceJoinWithParty);
                partyTime = Time.time + 0.25f;
                partyKickReconnecting = false;
                amountPartying = ManagerRegistry.FriendshipGroupDetection.myPartyMemberIDs.Count - 1;
                Notifications.SendFormat("purple", "PARTY", "Banning " + amountPartying + " party members, please be patient..");
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not in a party.");
        }
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
        public static void AutoPartyKick()
        {
            if (ManagerRegistry.FriendshipGroupDetection.IsInParty && !previousInParty)
                partyKickDelayCoroutine ??= Coroutines.instance.StartCoroutine(PartyKickDelay(false));

            previousInParty = ManagerRegistry.FriendshipGroupDetection.IsInParty;
        }

        public static void AutoPartyBan()
        {
            if (ManagerRegistry.FriendshipGroupDetection.IsInParty && !previousInParty)
                partyKickDelayCoroutine ??= Coroutines.instance.StartCoroutine(PartyKickDelay(true));

            previousInParty = ManagerRegistry.FriendshipGroupDetection.IsInParty;
        }
        public static void PartyBreakNetworkTriggers()
        {
            if (ManagerRegistry.FriendshipGroupDetection.IsInParty && Time.time > breakDelay)
            {
                breakDelay = Time.time + 1f;
                ManagerRegistry.FriendshipGroupDetection.photonView.RPC("PartyMemberIsAboutToGroupJoin", RpcTarget.All, Array.Empty<object>());
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
                        if (lockTarget == null && ManagerRegistry.FriendshipGroupDetection.IsInMyGroup(gunTarget.GetPlayer().UserId))
                        {
                            for (int i = 0; i < 3970; i++)
                                ManagerRegistry.FriendshipGroupDetection.photonView.RPC("RequestPartyGameMode", gunTarget.GetPhotonPlayer(),
                                    new object[] { GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)] });

                            RPCProtection();
                        }

                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    OptimizeEvents = false;
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void PartyKickAll()
        {
            ViewSerialization.OverrideSerialization = () => false;

            if (Time.time > kickDelay)
            {
                kickDelay = Time.time + 10f;
                for (int i = 0; i < 3950; i++)
                    ManagerRegistry.FriendshipGroupDetection.photonView.RPC
                    (
                        "RequestPartyGameMode",
                        new RaiseEventOptions
                        {
                            TargetActors =
                            ManagerRegistry.NetworkSystem.PlayerListOthers
                                .Where(plr => ManagerRegistry.FriendshipGroupDetection.IsInMyGroup(plr.UserId))
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
                ViewSerialization.OverrideSerialization = () => false;
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    for (int i = 0; i < 3950; i++)
                        ManagerRegistry.FriendshipGroupDetection.photonView.RPC("RequestPartyGameMode", nearbyPlayer.GetPhotonPlayer(),
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
                ViewSerialization.OverrideSerialization = () => false;
                foreach (VRRig rig in touchedPlayers)
                {
                    for (int i = 0; i < 3950; i++)
                        ManagerRegistry.FriendshipGroupDetection.photonView.RPC("RequestPartyGameMode", rig.GetPhotonPlayer(),
                            new object[] { GameMode.gameModeKeyByName.Keys.ToArray()[Random.Range(0, GameMode.gameModeKeyByName.Keys.Count)] });

                    RPCProtection();
                }
            }
            else
                OptimizeEvents = false;
        }
        public static void AntiReportLag()
        {
            if (Time.time > antiReportLagDelay)
            {
                List<int> actors = new List<int>();

                Safety.AntiReport((vrrig, position) =>
                {
                    antiReportLagDelay = Time.time + 0.1f;
                    actors.Add(GetPlayerFromVRRig(vrrig).ActorNumber);
                    Notifications.SendFormat("purple", "ANTI-REPORT", "" + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, they are being lagged.");
                });

                if (actors.Count > 0)
                    LagPlayer(actors);
            }
        }
        public static void SetMasterClient(bool skip = false)
        {
            if (Networking.IsMasterClient)
                return;
            if (PhotonNetwork.PlayerList.Length > 5)
            {
                if (!skip)
                    Notifications.SendFormat("red", "ERROR", $"{PhotonNetwork.PlayerList.Length - 5} people must leave for this mod to work.");
                return;
            }
            if (Time.time > setMasterDelay)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                setMasterDelay = Time.time + 5f;
            }
        }

        public static void SetMasterClientGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > setMasterDelay)
                {
                    PhotonNetwork.SetMasterClient(NetPlayerToPlayer(GetPlayerFromVRRig(lockTarget)));
                    setMasterDelay = Time.time + 5f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SetRoomStatus(bool status)
        {
            Dictionary<byte, object> dictionary = new Dictionary<byte, object>
            {
                { OperationCode.GetProperties, new Hashtable { { GamePropertyKey.IsOpen, status }, { GamePropertyKey.IsVisible, status }, { GamePropertyKey.MaxPlayers, status ? 0 : ManagerRegistry.PhotonNetworkController.currentJoinTrigger.GetRoomSize(SubscriptionManager.IsLocalSubscribed()) } } },
                { OperationCode.AuthenticateOnce, null }
            };

            PhotonNetwork.CurrentRoom.LoadBalancingClient.LoadBalancingPeer.SendOperation(
                OperationCode.SetProperties,
                dictionary,
                SendOptions.SendReliable
            );
            ManagerRegistry.GorillaScoreboardTotalUpdater.UpdateActiveScoreboards();
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

        public static void SetStatus(RoomSystem.StatusEffects state, RaiseEventOptions reo)
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                object[] statusSendData = new object[1];
                statusSendData[0] = (int)state;
                object[] sendEventData = new object[3];
                sendEventData[0] = ManagerRegistry.NetworkSystem.ServerTimestamp;
                sendEventData[1] = (byte)2;
                sendEventData[2] = statusSendData;
                PhotonNetwork.RaiseEvent(Constants.Network.ROOM_SYSTEM, sendEventData, reo, SendOptions.SendUnreliable);
            }
        }

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

        public static void BreakAudioAll()
        {
            if (rightTrigger > 0.5f)
            {
                References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.Others, 111, false, 999999f);
            }
        }
        public static IEnumerator RopeEnableRig()
        {
            yield return new WaitForSeconds(0.3f);
            References.LocalRig.enabled = true;
        }

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

                    RopeCoroutine = Coroutines.instance.StartCoroutine(RopeEnableRig());

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
        public static void InstantParty()
        {
            if (Time.time > Fun.instantPartyDelay)
            {
                Fun.instantPartyDelay = Time.time + 0.1f;

                ManagerRegistry.FriendshipGroupDetection.suppressPartyCreationUntilTimestamp = 0f;
                ManagerRegistry.FriendshipGroupDetection.groupCreateAfterTimestamp = 0f;

                List<int> provisionalMembers = ManagerRegistry.FriendshipGroupDetection.playersInProvisionalGroup;

                if (provisionalMembers.Count > 0)
                {
                    Color targetColor = GTColor.RandomHSV(ManagerRegistry.FriendshipGroupDetection.braceletRandomColorHSVRanges);
                    ManagerRegistry.FriendshipGroupDetection.myBraceletColor = targetColor;

                    List<int> members = new List<int> { PhotonNetwork.LocalPlayer.ActorNumber };
                    members.AddRange(from player in PhotonNetwork.PlayerListOthers where ManagerRegistry.FriendshipGroupDetection.IsInMyGroup(player.UserId) || provisionalMembers.Contains(player.ActorNumber) select player.ActorNumber);
                    ManagerRegistry.FriendshipGroupDetection.SendPartyFormedRPC(FriendshipGroupDetection.PackColor(targetColor), members.ToArray(), false);
                    RPCProtection();
                }
            }
        }

        public static void KickToSpecificRoom()
        {
            if (Time.time < timeMenuStarted + 5f)
                return;

            PromptText("What would you like the room code to be?", () => Overpowered.specificRoom = keyboardInput.ToUpper(), () => Manager.Toggle("Kick to Specific Room"), "Done", "Cancel");
        }
        #endregion
    }
}
