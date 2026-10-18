/*
 * Qubit Menu  Modules/Categories/Admin.cs
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
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Modules.Categories.Experimental;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.GameObj;
using Console = Qubit.Backend.Console;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules.Categories
{
    public static class Admin
    {
        #region Settings
        public static float FindUserTime;

        private static float adminEventDelay;

        private static float jumpscareDelay;

        private static readonly Dictionary<VRRig, Coroutine> freezePool = new Dictionary<VRRig, Coroutine>();

        private static bool lastInRoom2;
        private static int lastPlayerCount2 = -1;

        private static float stdell;
        private static VRRig thestrangled;
        private static VRRig thestrangledleft;

        private static float lastnetscale = 1f;
        private static float scalenetdel;
        private static int lastplayercount;

        private static Vector3 whereOriginalPlayerPos = Vector3.zero;
        private static Vector3 originalMePosition = Vector3.zero;

        private static bool lastInRoom;
        private static int lastPlayerCount = -1;

        private static readonly Dictionary<VRRig, string> menuUsers = new Dictionary<VRRig, string>();

        private static float thingdeb;

        private static bool lastLasering;

        private static float beamDelay;

        private static float startTimeTrigger;
        private static bool lastTriggerLaserSpam;
        #endregion

        #region Methods
        public static void AdminKickGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("kick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }
        public static void AdminPlatToggleGun(bool exclude)
        {
            GunLibrary.Target(gunTarget =>
            {
                string id = GetPlayerFromVRRig(gunTarget).UserId;
                adminEventDelay = Time.time + 0.1f;
                if (exclude)
                {
                    if (!platExcluded.Contains(id))
                    {
                        platExcluded.Add(id);
                        Notifications.SendFormat("green", "SUCCESS", "Player is now excluded.");
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", "Player is already excluded!");
                }
                else
                {
                    if (platExcluded.Contains(id))
                    {
                        platExcluded.Remove(id);
                        Notifications.SendFormat("green", "SUCCESS", "Player is now included.");
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", "Player is already included!");
                }
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminKickAll() =>
            Console.ExecuteCommand("kickall", ReceiverGroup.All);

        public static void AdminCrashGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("crash", GetPlayerFromVRRig(gunTarget).ActorNumber);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminCrashAll() =>
            Console.ExecuteCommand("crash", ReceiverGroup.Others);

        public static void AdminLagSpikeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.5f;
                Console.ExecuteCommand("sleep", GetPlayerFromVRRig(gunTarget).ActorNumber, 1000);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminLagGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("sleep", lockTarget.GetPlayer().ActorNumber, 50);
                        RPCProtection();
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
                    gunLocked = false;
                } });
        }

        public static void AdminLagSpikeAll() =>
            Console.ExecuteCommand("sleep", ReceiverGroup.Others, 1000);

        public static void AdminLagAll()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("sleep", ReceiverGroup.Others, 50);
                RPCProtection();
            }
        }

        public static void AdminGiveFlyGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.rightThumb.calcT > 0.5f)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("vel", lockTarget.GetPlayer().ActorNumber, lockTarget.headMesh.transform.forward * Movement._flySpeed);
                            RPCProtection();
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
                    gunLocked = false;
                } });
        }
        public static void AdminGivePlatforms()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.leftMiddle.calcT > 0.5f && !AdminPlatformsLastLeft)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("platf", lockTarget.GetPlayer().ActorNumber, lockTarget.leftHandTransform.position - new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.5f, 0.3f), lockTarget.leftHandTransform.eulerAngles, Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f, 10f);
                            RPCProtection();
                        }
                        if (lockTarget.rightMiddle.calcT > 0.5f && !AdminPlatformsLastRight)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("platf", lockTarget.GetPlayer().ActorNumber, lockTarget.rightHandTransform.position - new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.5f, 0.3f), lockTarget.rightHandTransform.eulerAngles, Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f, 10f);
                            RPCProtection();
                        }
                        AdminPlatformsLastLeft = lockTarget.leftMiddle.calcT > 0.5f;
                        AdminPlatformsLastRight = lockTarget.rightMiddle.calcT > 0.5f;
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
                    gunLocked = false;
                } });
        }

        public static void AdminGiveTriggerFlyGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.rightIndex.calcT > 0.5f)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("vel", lockTarget.GetPlayer().ActorNumber, lockTarget.headMesh.transform.forward * Movement._flySpeed);
                            RPCProtection();
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
                    gunLocked = false;
                } });
        }
        public static void AdminGiveSpeedGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("vel", lockTarget.GetPlayer().ActorNumber, (lockTarget.bodyTransform.position - speedLastVel) * 6f);
                        speedLastVel = lockTarget.bodyTransform.position;
                        RPCProtection();
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        speedLastVel = gunTarget.bodyTransform.position;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    gunLocked = false;
                } });
        }

        public static void AdminGiveLowGravity()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("vel", lockTarget.GetPlayer().ActorNumber, (lockTarget.bodyTransform.position - speedLastVel) * 5f + Vector3.up * 0.5f);
                        speedLastVel = lockTarget.bodyTransform.position;
                        RPCProtection();
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        speedLastVel = gunTarget.bodyTransform.position;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    gunLocked = false;
                } });
        }

        public static void AdminVibrateGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.2f;
                Console.ExecuteCommand("vibrate", GetPlayerFromVRRig(gunTarget).ActorNumber, 3, 1f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminVibrateAll() =>
            Console.ExecuteCommand("vibrate", ReceiverGroup.Others, 3, 1f);

        public static void AdminBMuteGun(bool mute)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.5f;
                Console.ExecuteCommand(mute ? "mute" : "unmute", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminBlockGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 5f;
                Console.ExecuteCommand("block", GetPlayerFromVRRig(gunTarget).ActorNumber, 300L);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminABlockGun(bool Silent)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 5f;
                Console.ExecuteCommand("notify", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).NickName + " has been blocked" + (Silent ? "" : " by " + ServerData.Administrators[PhotonNetwork.LocalPlayer.UserId]) + ".");
                Console.ExecuteCommand("block", GetPlayerFromVRRig(gunTarget).ActorNumber, 300L);
                RPCProtection();
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminBMuteAll(bool mute) =>
            Console.ExecuteCommand(mute ? "muteall" : "unmuteall", ReceiverGroup.All);

        public static void AdminButtonPressGun(string key)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.8f;
                Console.ExecuteCommand("controller", GetPlayerFromVRRig(gunTarget).ActorNumber, key, 1f, 1f);
                RPCProtection();
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void FlipMenuGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("toggle", GetPlayerFromVRRig(gunTarget).ActorNumber, "Right Hand");
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminEnableGun(bool enable, string mod)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("forceenable", GetPlayerFromVRRig(gunTarget).ActorNumber, mod, enable);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }
        public static void AdminJumpscareGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                jumpscareDelay = Time.time + 0.2f;
                Console.ExecuteCommand("toggle", GetPlayerFromVRRig(gunTarget).ActorNumber, "Jumpscare");
            }, new GunLibrary.GunSettings { when = () => Time.time > jumpscareDelay });
        }

        public static void AdminJumpscareAll() =>
            Console.ExecuteCommand("toggle", ReceiverGroup.Others, "Jumpscare");
        public static void AdminMute()
        {
            if (leftTrigger > 0.5f && !muted)
            {
                Console.ExecuteCommand("forceenable", ReceiverGroup.Others, "Mute Microphone", true);
                muted = true;
            }
            else if (leftTrigger < 0.5f && muted)
            {
                Console.ExecuteCommand("forceenable", ReceiverGroup.Others, "Mute Microphone", false);
                muted = false;
            }

        }

        public static void AdminFreezeGun(bool freeze)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                switch (freeze)
                {
                    case true when !freezePool.ContainsKey(gunTarget):
                        freezePool.Add(gunTarget, Coroutines.instance.StartCoroutine(FreezeCoroutine(gunTarget)));
                        break;
                    case false when freezePool.ContainsKey(gunTarget):
                        Coroutines.instance.StopCoroutine(freezePool[gunTarget]);
                        Console.ExecuteCommand("forceenable", GetPlayerFromVRRig(gunTarget).ActorNumber, "Zero Gravity", false);
                        freezePool.Remove(gunTarget);
                        break;
                }
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminTeleportGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("tp", ReceiverGroup.Others, NewPointer.transform.position);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminFlingGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("vel", GetPlayerFromVRRig(gunTarget).ActorNumber, new Vector3(0f, 50f, 0f));
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminCrashBypassGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (ServerData.Administrators.ContainsKey(GetPlayerFromVRRig(gunTarget).UserId))
                    return;
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("tp", GetPlayerFromVRRig(gunTarget).ActorNumber, new Vector3(0f, 1000000f, 0f));
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminLockdownGun(bool enable)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("togglemenu", GetPlayerFromVRRig(gunTarget).ActorNumber, enable);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminFullLockdownGun(bool enable)
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                FullToggleMenu(GetPlayerFromVRRig(gunTarget).ActorNumber, enable);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }
        public static void AdminLockdownAll(bool enable)
        {
            if (Networking.InRoom && (!lastInRoom2 || PhotonNetwork.PlayerList.Length != lastPlayerCount2))
                Console.ExecuteCommand("togglemenu", ReceiverGroup.Others, enable);

            lastInRoom2 = Networking.InRoom;
            lastPlayerCount2 = PhotonNetwork.PlayerList.Length;
            if (!Networking.InRoom)
                lastPlayerCount2 = -1;
        }

        public static void AdminFullLockdownAll(bool enable)
        {
            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                FullToggleMenu(Player.ActorNumber, enable);
        }
        public static void AdminStrangle()
        {
            if (leftGrab)
            {
                if (thestrangledleft == null)
                {
                    foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal).Where(rig => Vector3.Distance(rig.headMesh.transform.position, References.LeftHand.position) < 0.2f))
                    {
                        thestrangledleft = rig;
                        if (Networking.InRoom)
                            References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                        else
                            References.LocalRig.PlayHandTapLocal(89, true, 999999f);
                    }
                }
                else
                {
                    if (Time.time > stdell)
                    {
                        stdell = Time.time + 0.05f;
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber, References.LeftHand.position);
                    }
                }
            }
            else
            {
                if (thestrangledleft != null)
                {
                    try
                    {
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber, References.LeftHand.position);
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangledleft).ActorNumber, References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0));
                    }
                    catch { }
                    thestrangledleft = null;
                    if (Networking.InRoom)
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                    else
                        References.LocalRig.PlayHandTapLocal(89, true, 999999f);
                }
            }

            if (rightGrab)
            {
                if (thestrangled == null)
                {
                    foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal).Where(rig => Vector3.Distance(rig.headMesh.transform.position, References.RightHand.position) < 0.2f))
                    {
                        thestrangled = rig;
                        if (Networking.InRoom)
                            References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                        else
                            References.LocalRig.PlayHandTapLocal(89, false, 999999f);
                    }
                }
                else
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.05f;
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber, References.RightHand.position);
                    }
                }
            }
            else
            {
                if (thestrangled != null)
                {
                    try
                    {
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber, References.RightHand.position);
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangled).ActorNumber, References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0));
                    }
                    catch { }
                    thestrangled = null;
                    if (Networking.InRoom)
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                    else
                        References.LocalRig.PlayHandTapLocal(89, false, 999999f);
                }
            }
        }

        public static void AdminObjectGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("platf", ReceiverGroup.All, NewPointer.transform.position);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void AdminRandomObjectGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("platf", ReceiverGroup.All, NewPointer.transform.position, RandomVector3(), RandomVector3(360f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }
        public static void AdminNetworkScale()
        {
            if (Time.time > scalenetdel && (!Mathf.Approximately(lastnetscale, References.LocalRig.scaleFactor) || PhotonNetwork.PlayerList.Length != lastplayercount))
            {
                Console.ExecuteCommand("scale", ReceiverGroup.All, References.LocalRig.scaleFactor);
                scalenetdel = Time.time + 0.05f;
                lastnetscale = References.LocalRig.scaleFactor;
                lastplayercount = PhotonNetwork.PlayerList.Length;
            }
        }

        public static void UnAdminNetworkScale() =>
            Console.ExecuteCommand("scale", ReceiverGroup.All, 1f);

        public static void LightningGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("strike", ReceiverGroup.All, NewPointer.transform.position);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void LightningAura()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("strike", ReceiverGroup.All, References.HeadCollider.transform.position + new Vector3(MathF.Cos((float)Time.frameCount / 30), 1f, MathF.Sin((float)Time.frameCount / 30)));
            }
        }

        public static void LightningRain()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.1f;
                Physics.Raycast(References.HeadCollider.transform.position + new Vector3(Random.Range(-10f, 10f), 10f, Random.Range(-10f, 10f)), Vector3.down, out var Ray, 512f, NoInvisLayerMask());
                VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                if (gunTarget && !gunTarget.IsLocal())
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("kick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                }
                else
                    Console.ExecuteCommand("strike", ReceiverGroup.All, Ray.point);
            }
        }
        public static void AdminFearGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    TeleportPlayer(lockTarget.transform.position + lockTarget.transform.forward);
                    if (Time.time > adminEventDelay)
                        adminEventDelay = Time.time + 0.1f;
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        originalMePosition = References.BodyCollider.transform.position;
                        whereOriginalPlayerPos = gunTarget.transform.position;

                        int actorNumber = GetPlayerFromVRRig(gunTarget).ActorNumber;
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(0f, 16f, 0f), new Vector3(10f, 1f, 10f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(0f, 24f, 0f), new Vector3(10f, 1f, 10f));

                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(4f, 20f, 0f), new Vector3(1f, 10f, 10f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(-4f, 20f, 0f), new Vector3(1f, 10f, 10f));

                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(0f, 20f, 4f), new Vector3(10f, 10f, 1f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber }, new Vector3(0f, 20f, -4f), new Vector3(10f, 10f, 1f));

                        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(platform, 60f);
                        platform.GetComponent<Renderer>().material.color = Color.black;
                        platform.transform.position = new Vector3(0f, 20f, 0f);
                        platform.transform.localScale = new Vector3(10f, 1f, 10f);

                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;

                        TeleportPlayer(originalMePosition);
                        Console.ExecuteCommand("tpnv", lockTarget.GetPlayer().ActorNumber, whereOriginalPlayerPos);
                        Console.ExecuteCommand("unmuteall", lockTarget.GetPlayer().ActorNumber);
                    }
                } });
        }

        public static void EnableNoAdminIndicator()
        {
            Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
            lastplayercount = -1;
        }

        public static void NoAdminIndicator()
        {
            if (!Networking.InRoom)
                lastplayercount = -1;

            if (PhotonNetwork.PlayerList.Length != lastplayercount && Networking.InRoom)
            {
                Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
                lastplayercount = PhotonNetwork.PlayerList.Length;
            }
        }

        public static void AdminIndicatorBack() =>
            Console.ExecuteCommand("nocone", ReceiverGroup.All, false);

        public static void EnableAdminMenuUserTags()
        {
            if (!userTagHooked)
            {
                userTagHooked = true;
                PhotonNetwork.NetworkingClient.EventReceived += AdminUserTagSys;
            }
        }
        public static void AdminMenuUserTags()
        {
            if (Networking.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount))
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            lastInRoom = Networking.InRoom;
            lastPlayerCount = PhotonNetwork.PlayerList.Length;
            if (!Networking.InRoom)
                lastPlayerCount = -1;

            foreach (KeyValuePair<VRRig, GameObject> nametag in nametags.ToList())
            {
                if (!Players.ActiveRigs.Contains(nametag.Key))
                {
                    Object.Destroy(nametag.Value);
                    nametags.Remove(nametag.Key);
                }
                else
                {
                    nametag.Value.GetComponent<TextMeshPro>().fontStyle = activeFontStyle;
                    nametag.Value.GetComponent<TextMeshPro>().font = activeFont;

                    if (Visuals.nameTagChams)
                        nametag.Value.GetComponent<TextMeshPro>().Chams();

                    nametag.Value.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f) * nametag.Key.scaleFactor;

                    nametag.Value.transform.position = Visuals.GetNameTagPosition(nametag.Key);
                    nametag.Value.transform.LookAt(Camera.main.transform.position);
                    nametag.Value.transform.Rotate(0f, 180f, 0f);
                }
            }
        }

        public static void DisableAdminMenuUserTags()
        {
            foreach (KeyValuePair<VRRig, GameObject> nametag in nametags)
                Object.Destroy(nametag.Value);

            nametags.Clear();
        }
        public static void EnableAdminMenuUserTracers()
        {
            if (!tracerTagHooked)
            {
                tracerTagHooked = true;
                PhotonNetwork.NetworkingClient.EventReceived += AdminTracerSys;
            }
        }

        public static void MenuUserTracers()
        {
            if (Networking.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount))
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            lastInRoom = Networking.InRoom;
            lastPlayerCount = PhotonNetwork.PlayerList.Length;
            if (!Networking.InRoom)
                lastPlayerCount = -1;

            if (Visuals.DoPerformanceCheck())
                return;

            bool followMenuTheme = Manager.GetIndex("Follow Menu Theme").enabled;
            bool transparentTheme = Manager.GetIndex("Transparent Theme").enabled;
            _ = Manager.GetIndex("Hidden on Camera").enabled;
            float lineWidth = (Manager.GetIndex("Thin Tracers").enabled ? 0.0075f : 0.025f) * (scaleWithPlayer ? References.Player.scale : 1f);

            Color menuColor = backgroundColor.GetCurrentColor();

            foreach (KeyValuePair<VRRig, string> userData in menuUsers)
            {
                VRRig playerRig = userData.Key;
                if (playerRig.isLocal)
                    continue;

                Color lineColor = Console.GetMenuTypeName(userData.Value);

                LineRenderer line = Visuals.GetLineRender();

                if (followMenuTheme)
                    lineColor = menuColor;

                if (transparentTheme)
                    lineColor.a = 0.5f;

                line.startColor = lineColor;
                line.endColor = lineColor;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.SetPosition(0, References.RightHand.position);
                line.SetPosition(1, playerRig.transform.position);
            }
        }
        public static void ConsoleOnConduct()
        {
            if (Networking.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount) && !Manager.GetIndex("Menu User Name Tags").enabled)
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            string conductText = "";
            conductText += "<color=red>" + PhotonNetwork.LocalPlayer.NickName + " - " + Strings.ToTitleCase(Console.MenuName) + "</color>\\n";
            foreach (KeyValuePair<string, string> item in onConduct)
            {
                if (GetPlayerFromID(item.Key) == null)
                    onConduct.Remove(item.Key);
                else
                    conductText += item.Value + "\\n";
            }
            GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData").GetComponent<TextMeshPro>().text = conductText;
        }
        public static void AdminFindUser()
        {
            if (Time.time < FindUserTime)
                return;

            if (!Networking.InRoom)
            {
                Room.JoinRandom();
                isUserFound = false;
                FindUserTime = Time.time + 7f;
            }
            else
            {
                if (isUserFound)
                {
                    Notifications.SendFormat("green", "SUCCESS", "Found menu user!");
                    Manager.GetIndex("Admin Find User").SetEnabled(false);
                    isUserFound = false;
                    return;
                }
                Notifications.Send("Nobody found, searching for players.");
                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                FindUserTime = Time.time + 2f;
            }
        }
        public static void AdminPunchMod()
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

                        Console.ExecuteCommand("vel", rig.GetPlayer().ActorNumber, vel);
                        thingdeb = Time.time + 0.1f;
                    }
                }
            }
        }

        public static void JoinGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("join", GetPlayerFromVRRig(gunTarget).ActorNumber, targetRoom.ToUpper());
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void JoinAll() =>
            PromptText("What room would you like the users to join?", () => Console.ExecuteCommand("join", ReceiverGroup.Others, keyboardInput.ToUpper()), null, "Done", "Cancel");
        public static void GetTargetNotification()
        {
            PromptText("What notification would you like to send?", () =>
            {
                targetNotification = keyboardInput;
                Manager.GetIndex("NotifLabel").overlapText = "Notif: " + keyboardInput;
            }, null, "Done", "Cancel");
        }

        public static void NotifySelf() =>
            Console.ExecuteCommand("notify", PhotonNetwork.LocalPlayer.ActorNumber, targetNotification);

        public static void NotifyGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("notify", GetPlayerFromVRRig(gunTarget).ActorNumber, targetNotification);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void NotifyAll() =>
            Console.ExecuteCommand("notify", ReceiverGroup.All, targetNotification);

        public static void GetMenuUsers()
        {
            Console.indicatorDelay = Time.time + 2f;
            Console.ExecuteCommand("isusing", ReceiverGroup.All);
        }
        public static void AdminLaser()
        {
            if (leftPrimary || rightPrimary)
            {
                Vector3 dir = rightPrimary ? References.LocalRig.rightHandTransform.right : -References.LocalRig.leftHandTransform.right;
                Vector3 startPos = (rightPrimary ? References.LocalRig.rightHandTransform.position : References.LocalRig.leftHandTransform.position) + dir * 0.1f;
                try
                {
                    Physics.Raycast(startPos + dir / 3f, dir, out var Ray, 512f, NoInvisLayerMask());
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                        Console.ExecuteCommand("silkick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                }
                catch { }
                if (Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("laser", ReceiverGroup.All, true, rightPrimary);
                }
            }
            bool isLasering = leftPrimary || rightPrimary;
            if (lastLasering && !isLasering)
                Console.ExecuteCommand("laser", ReceiverGroup.All, false, false);

            lastLasering = isLasering;
        }
        public static void AdminBeam()
        {
            if (rightTrigger > 0.5f && Time.time > beamDelay)
            {
                beamDelay = Time.time + 0.05f;
                float h = Time.frameCount / 180f % 1f;
                Color color = Color.HSVToRGB(h, 1f, 1f);
                Console.ExecuteCommand("lr", ReceiverGroup.All, color.r, color.g, color.b, color.a, 0.5f, References.HeadCollider.transform.position + new Vector3(0f, 0.5f, 0f), References.HeadCollider.transform.position + new Vector3(Mathf.Cos((float)Time.frameCount / 30) * 100f, 0.5f, Mathf.Sin((float)Time.frameCount / 30) * 100f), 0.1f);
            }
        }
        public static void AdminFractals()
        {
            if (rightTrigger > 0.5f && !lastTriggerLaserSpam)
                startTimeTrigger = Time.time;

            lastTriggerLaserSpam = rightTrigger > 0.5f;

            if (rightTrigger > 0.5f && Time.time > beamDelay)
            {
                beamDelay = Time.time + 0.5f;
                float h = Time.frameCount / 180f % 1f;
                Color.HSVToRGB(h, 1f, 1f);
                Console.ExecuteCommand("lr", ReceiverGroup.All, "lr", 0f, 1f, 1f, 0.3f, 0.25f, References.BodyCollider.transform.position, References.HeadCollider.transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 1000f, 20f - (Time.time - startTimeTrigger));
            }
        }

        public static void FlyAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("vel", ReceiverGroup.Others, new Vector3(0f, 10f, 0f));
            }
        }

        public static void BouncyAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;

                var users = Console.userDictionary.Keys.Where(u => !u.IsLocal).ToList();

                foreach (var rig in users.Select(player => GetVRRigFromPlayer(player)))
                {
                    if (!Physics.Raycast(rig.bodyTransform.position - new Vector3(0f, 0.2f, 0f), Vector3.down,
                            out RaycastHit hit, 512f, References.Player.locomotionEnabledLayers)) continue;
                    if (!(hit.distance < 0.1f)) continue;
                    Vector3 surfaceNormal = hit.normal;
                    Vector3 bodyVelocity = rig.LatestVelocity();
                    Vector3 reflectedVelocity = Vector3.Reflect(bodyVelocity, surfaceNormal);
                    Vector3 finalVelocity = reflectedVelocity * 2f;
                    Console.ExecuteCommand("vel", rig.GetPlayer().ActorNumber, finalVelocity);
                }
            }
        }



        public static void AdminBringGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("tpnv", GetPlayerFromVRRig(gunTarget).ActorNumber, References.HeadCollider.transform.position + new Vector3(0f, 1.5f, 0f));
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void BringAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others, References.HeadCollider.transform.position + new Vector3(0f, 1.5f, 0f));
            }
        }

        public static void AdminOrganizeGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                var users = Console.userDictionary.Keys.Where(u => !u.IsLocal).ToList();
                if (users.Count == 1)
                {
                    Console.ExecuteCommand("tpnv", users.FirstOrDefault().ActorNumber, NewPointer.transform.position);
                    return;
                }

                float spacing = 0.8f;
                for (int i = 0; i < users.Count; i++)
                {
                    Console.ExecuteCommand("tpnv", users[i].ActorNumber, NewPointer.transform.position - Vector3.right * ((users.Count - 1) * spacing / 2f) + Vector3.right * (spacing * i));
                }
                adminEventDelay = Time.time + 0.05f;
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void BringHandAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others, Controllers.GetTrueRightHand().position + Controllers.GetTrueRightHand().forward);
            }
        }

        public static void BringHeadAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others, References.HeadCollider.transform.position + References.HeadCollider.transform.forward);
            }
        }

        public static void OrbitAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others, References.HeadCollider.transform.position + new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f)));
            }
        }

        public static void ConfirmNotifyAllUsing()
        {
            if (ServerData.Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out var adminName))
            {
                string message = $"Yes, I am {adminName}. " + (ServerData.Owners.Contains(adminName) ? "I own the menu." : "I am a Console admin.");
                Console.ExecuteCommand("notify", ReceiverGroup.All, message);
            }
        }





        public static void LightGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("lighting", GetPlayerFromVRRig(gunTarget).ActorNumber, false);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }
        public static void LightAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("lighting", ReceiverGroup.Others, false);
            }
        }
        public static void DarkGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("lighting", GetPlayerFromVRRig(gunTarget).ActorNumber, true);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void DarkAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("lighting", ReceiverGroup.Others, true);
            }
        }
        public static void WorldOffGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("environmentobj", GetPlayerFromVRRig(gunTarget).ActorNumber, false);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void WorldOffAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("environmentobj", ReceiverGroup.Others, false);
            }
        }
        public static void WorldOnGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("environmentobj", GetPlayerFromVRRig(gunTarget).ActorNumber, true);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void WorldOnAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("environmentobj", ReceiverGroup.Others, true);
            }
        }
        public static void EarthquakeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("earthquake", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void EarthquakeAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("earthquake", ReceiverGroup.Others, 3f);
            }
        }

        public static void DrunkGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("drunk", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void DrunkAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("drunk", ReceiverGroup.Others, 3f);
            }
        }

        public static void LowGravityGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("setgravity", GetPlayerFromVRRig(gunTarget).ActorNumber, 6.66f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void LowGravityAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("setgravity", ReceiverGroup.Others, 6.66f);
            }
        }
        public static void HighGravityGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("setgravity", GetPlayerFromVRRig(gunTarget).ActorNumber, 7.77f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void HighGravityAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("setgravity", ReceiverGroup.Others, 7.77f);
            }
        }
        public static void ZeroGravityGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("setgravity", GetPlayerFromVRRig(gunTarget).ActorNumber, 9.81f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void ZeroGravityAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("setgravity", ReceiverGroup.Others, 9.81f);
            }
        }
        public static void ResetGravityGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("setgravity", GetPlayerFromVRRig(gunTarget).ActorNumber, 1f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void ResetGravityAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("setgravity", ReceiverGroup.Others, 1f);
            }
        }


        public static void CreeperGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("swaphands", GetPlayerFromVRRig(gunTarget).ActorNumber, 5f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void CreeperAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("swaphands", ReceiverGroup.Others, 5f);
            }
        }

        public static void WeirdGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("weirdrendering", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void WeirdAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("weirdrendering", ReceiverGroup.Others, 3f);
            }
        }


        public static void SpinGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("spin", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void SpinAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("spin", ReceiverGroup.Others, 3f);
            }
        }
        public static void InvertGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("invert", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void InvertAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("invert", ReceiverGroup.Others, 3f);
            }
        }
        public static void BounceGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("bounce", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void BounceAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("bounce", ReceiverGroup.Others, 3f);
            }
        }
        public static void RainbowGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("rainbow", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void RainbowAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("rainbow", ReceiverGroup.Others, 3f);
            }
        }
        public static void FlipGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("flip", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void FlipAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("flip", ReceiverGroup.Others, 3f);
            }
        }
        public static void ZeroGGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("zeroG", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void ZeroGAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("zeroG", ReceiverGroup.Others, 3f);
            }
        }
        public static void SuperjumpGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("superjump", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void SuperjumpAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("superjump", ReceiverGroup.Others, 3f);
            }
        }
        public static void BlurGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("blur", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void BlurAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("blur", ReceiverGroup.Others, 3f);
            }
        }
        public static void TposeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("tpose", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void TposeAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpose", ReceiverGroup.Others, 3f);
            }
        }
        public static void HeadspinGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("headspin", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void HeadspinAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("headspin", ReceiverGroup.Others, 3f);
            }
        }
        public static void FreezeandpullGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("freezeandpull", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void FreezeandpullAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("freezeandpull", ReceiverGroup.Others, 3f);
            }
        }
        public static void MagnetGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("magnet", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void MagnetAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("magnet", ReceiverGroup.Others, 3f);
            }
        }
        public static void RagdollGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("ragdoll", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void RagdollAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("ragdoll", ReceiverGroup.Others, 3f);
            }
        }

        public static void BlackholeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("blackhole", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void BlackholeAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("blackhole", ReceiverGroup.Others, 3f);
            }
        }
        public static void YeetGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("yeet", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void YeetAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("yeet", ReceiverGroup.Others, 3f);
            }
        }
        public static void LastmomentGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("lastmoment", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void LastmomentAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("lastmoment", ReceiverGroup.Others, 3f);
            }
        }
        public static void StunGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("stun", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void StunAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("stun", ReceiverGroup.Others, 3f);
            }
        }

        public static void LSDGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("lsd", GetPlayerFromVRRig(gunTarget).ActorNumber, 3f);
            }, new GunLibrary.GunSettings { when = () => Time.time > adminEventDelay });
        }

        public static void LSDAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("lsd", ReceiverGroup.Others, 3f);
            }
        }
        private static IEnumerator FreezeCoroutine(VRRig rig)
        {
            Console.ExecuteCommand("forceenable", rig.GetPlayer().ActorNumber, "Zero Gravity", true);
            Vector3 pos = rig.transform.position;
            while (Players.ActiveRigs.Contains(rig))
            {
                Console.ExecuteCommand("tp", rig.GetPlayer().ActorNumber, pos);
                yield return new WaitForSeconds(0.1f);
            }
        }
        #endregion
    }
}
