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
using GorillaTagScripts.VirtualStumpCustomMaps;
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
using Qubit.Patches.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Prompts;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
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
        public static void SpamTagSelf()
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                if (!(Time.time > Advantages.spamTagDelay)) return;
                Advantages.spamTagDelay = Time.time + 0.1f;
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
                        if (Time.time > Advantages.spamTagDelay)
                        {
                            Advantages.spamTagDelay = Time.time + 0.1f;
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
                if (!(Time.time > Advantages.spamTagDelay)) return;
                Advantages.spamTagDelay = Time.time + 0.1f;
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
            if (Time.time < Advantages.paintbrawlSpamDelay)
                return;

            Advantages.paintbrawlSpamDelay = Time.time + 0.1f;

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
                        if (Time.time > Advantages.paintbrawlSpamDelay)
                        {
                            Advantages.paintbrawlSpamDelay = Time.time + 0.1f;

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
            if (Time.time < Advantages.paintbrawlSpamDelay)
                return;

            Advantages.paintbrawlSpamDelay = Time.time + 0.1f;

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
                Sound.PlayTag(id, 999999f);
        }

        public static void UpdateMasterLabel() =>
            Manager.GetIndex("MasterLabel").displayName = Networking.IsMasterClient ? "You are master client." : "You are not master client.";
        #endregion
    }
}
