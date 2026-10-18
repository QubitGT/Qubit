/*
 * Qubit Menu  Modules/Categories/Detected.cs
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
using ExitGames.Client.Photon;
using GorillaGameModes;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Modules.Classes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Utilities.Assets;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Detected
    {
        #region Settings
        public static Dictionary<VRRig, int> viewIdArchive = new Dictionary<VRRig, int>();

        public static float muteDelay;

        public static string name = "Qubit";

        private static float customPropertyDelay;

        private static float spazGamemodeDelay;

        public static bool moddedGamemode;
        #endregion

        #region Methods
        public static void EnterDetectedTab()
        {
            if (!allowDetected)
            {
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/danger.ogg", "Audio/Menu/danger.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                Prompt("The mods in this category are detected. <b>Unless you know what you're doing, you will get banned.</b> Are you sure you would like to continue?",
                    () =>
                    {
                        allowDetected = true; Manager.CurrentCategoryName = "Detected Mods";

                        Achievements.Unlock(new Achievements.Achievement
                        {
                            name = "Sinister",
                            description = "Open the \"Detected Mods\" category.",
                            icon = "Images/Achievements/sinister.png"

                        });
                    });
            }
            else Manager.CurrentCategoryName = "Detected Mods";
        }

        public static void Destroy(object target, Hashtable hashtable = null, RaiseEventOptions raiseEventOptions = null, int viewID = -1)
        {
            switch (target)
            {
                case VRRig rig:
                    if (hashtable == null)
                    {
                        PhotonView view = GetPhotonViewFromVRRig(rig);
                        hashtable = new Hashtable { { 0, viewID == -1 ? view.ViewID : viewID } };
                    }
                    raiseEventOptions ??= new RaiseEventOptions { TargetActors = new[] { rig.GetPlayer().ActorNumber } };
                    PhotonNetwork.NetworkingClient.OpRaiseEvent(PunEvent.Destroy, hashtable, raiseEventOptions, SendOptions.SendReliable);
                    break;
                case Player player:
                    hashtable ??= new Hashtable { { 0, player.ActorNumber } };
                    raiseEventOptions ??= new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } };
                    PhotonNetwork.NetworkingClient.OpRaiseEvent(PunEvent.DestroyPlayer, hashtable, raiseEventOptions, SendOptions.SendReliable);
                    break;
                case GameObject _:
                    break;
            }
        }

        public static void QuarantineGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                PhotonView view = gunTarget.GetPhotonView();
                if (view != null)
                {
                    Destroy(gunTarget, new Hashtable
                    {
                        { 0, view.ViewID }
                    }, new RaiseEventOptions
                    {
                        TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                    });
                    foreach (VRRig rig in Players.ActiveRigs)
                    {
                        if (rig != gunTarget)
                        {
                            Destroy(rig, new Hashtable
                            {
                                { 0, rig.GetPhotonView().ViewID }
                            }, new RaiseEventOptions
                            {
                                TargetActors = new[] { gunTarget.GetPlayer().ActorNumber }
                            });
                        }
                    }
                }
            });
        }

        public static void QuarantineAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    PhotonView view = rig.GetPhotonView();
                    if (view != null)
                    {
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                        foreach (VRRig otherRig in Players.ActiveRigs)
                        {
                            if (otherRig != rig)
                            {
                                Destroy(otherRig, new Hashtable
                                {
                                    { 0, otherRig.GetPhotonView().ViewID }
                                }, new RaiseEventOptions
                                {
                                    TargetActors = new[] { rig.GetPlayer().ActorNumber }
                                });
                            }
                        }
                    }
                }
            }
        }

        public static void QuarantineAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();
            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }
            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig rig in nearbyPlayers)
                {
                    PhotonView view = rig.GetPhotonView();
                    if (view != null)
                    {
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                        foreach (VRRig otherRig in Players.ActiveRigs)
                        {
                            if (otherRig != rig)
                            {
                                Destroy(otherRig, new Hashtable
                                {
                                    { 0, otherRig.GetPhotonView().ViewID }
                                }, new RaiseEventOptions
                                {
                                    TargetActors = new[] { rig.GetPlayer().ActorNumber }
                                });
                            }
                        }
                    }
                }
            }
        }

        public static void QuarantineOnTouch()
        {
            if (!Networking.InRoom) return;
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    PhotonView view = rig.GetPhotonView();
                    if (view != null)
                    {
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                    }
                    foreach (VRRig otherRig in Players.ActiveRigs)
                    {
                        if (otherRig != rig)
                        {
                            Destroy(otherRig, new Hashtable
                            {
                                { 0, otherRig.GetPhotonView().ViewID }
                            }, new RaiseEventOptions
                            {
                                TargetActors = new[] { rig.GetPlayer().ActorNumber }
                            });
                        }
                    }
                }
            }
        }

        public static void LeaderboardQuarantine()
        {
            foreach (var scoreboard in GorillaScoreboardTotalUpdater.allScoreboards.Where(scoreboard => scoreboard.buttonText.text.Contains("REPORT")))
                scoreboard.buttonText.text = scoreboard.buttonText.text.Replace("REPORT", "QUARANTINE");
            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.linePlayer != Networking.LocalPlayer))
            {
                PhotonView view = line.linePlayer.VRRig().GetPhotonView();

                if (line.reportInProgress)
                {
                    line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cancel);
                    line.reportButton.isOn = true;
                    line.reportButton.UpdateColor();
                    if (view != null)
                    {
                        Destroy(line.linePlayer.VRRig(), new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                    }
                }
                if (!line.reportButton.isOn || !line.reportInProgress) continue;
                line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cancel);
                line.reportButton.isOn = false;
                line.reportButton.UpdateColor();
                Destroy(line.linePlayer, new Hashtable
                {
                    { 0, view.ViewID }
                }, new RaiseEventOptions
                {
                    TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                });
                foreach (VRRig otherRig in Players.ActiveRigs)
                {
                    if (otherRig != line.linePlayer.VRRig())
                    {
                        Destroy(otherRig, new Hashtable
                        {
                            { 0, otherRig.GetPhotonView().ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = new[] { line.linePlayer.ActorNumber }
                        });
                    }
                }
            }
        }

        public static void GhostGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                PhotonView view = gunTarget.GetPhotonView();
                if (view != null)
                {
                    viewIdArchive[gunTarget] = view.ViewID;
                    Destroy(gunTarget, new Hashtable
                    {
                        { 0, view.ViewID }
                    }, new RaiseEventOptions
                    {
                        TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                    });
                }
            });
        }

        public static void GhostAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                try
                {
                    if (!rig.IsLocal())
                    {
                        PhotonView view = rig.GetPhotonView();

                        if (view != null)
                        {
                            viewIdArchive[rig] = view.ViewID;
                            int[] targets = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray();

                            Destroy(rig, new Hashtable
                            {
                                { 0, view.ViewID }
                            },
                            new RaiseEventOptions
                            {
                                TargetActors = targets
                            });
                        }
                    }
                }
                catch { }
            }
        }

        public static void GhostAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig rig in nearbyPlayers.ToList())
                {
                    PhotonView view = rig.GetPhotonView();

                    if (view != null)
                    {
                        viewIdArchive[rig] = view.ViewID;
                        int[] targets = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray();

                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        },
                        new RaiseEventOptions
                        {
                            TargetActors = targets
                        });
                        nearbyPlayers.Remove(rig);
                    }
                }
            }
        }

        public static void GhostOnTouch()
        {
            if (!Networking.InRoom) return;


            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && !viewIdArchive.ContainsKey(rig) && rig.IsBeingTouched())
                {
                    PhotonView view = rig.GetPhotonView();
                    if (view != null)
                    {
                        viewIdArchive[rig] = view.ViewID;
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                    }
                }
            }
        }

        public static void LeaderboardGhost()
        {
            foreach (var scoreboard in GorillaScoreboardTotalUpdater.allScoreboards.Where(scoreboard => scoreboard.buttonText.text.Contains("REPORT")))
                scoreboard.buttonText.text = scoreboard.buttonText.text.Replace("REPORT", "GHOST");

            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.linePlayer != Networking.LocalPlayer))
            {
                if (line.reportInProgress)
                {
                    line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cancel);
                    line.reportButton.isOn = true;
                    line.reportButton.UpdateColor();
                    PhotonView view = GetPhotonViewFromVRRig(line.linePlayer.VRRig());
                    if (view != null)
                    {
                        viewIdArchive[line.linePlayer.VRRig()] = view.ViewID;
                        Destroy(line.linePlayer.VRRig(), new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = PhotonNetwork.PlayerList.Where(p => p != view.Owner).Select(p => p.ActorNumber).ToArray()
                        });
                    }
                }

                if (!line.reportButton.isOn || !line.reportInProgress) continue;
                line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cancel);
                line.reportButton.isOn = false;
                line.reportButton.UpdateColor();
                int viewID = viewIdArchive[line.linePlayer.VRRig()];
                Destroy(line.linePlayer.VRRig(), null, null, viewID);
            }
        }

        public static void RevertLeaderboard(string query)
        {
            foreach (var scoreboard in GorillaScoreboardTotalUpdater.allScoreboards.Where(scoreboard => scoreboard.buttonText.text.Contains(query)))
                scoreboard.buttonText.text = scoreboard.buttonText.text.Replace(query, "REPORT");

            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cancel);
                line.reportButton.isOn = false;
                line.reportButton.UpdateColor();
            }
        }

        public static void LeaderboardMute()
        {
            if (Time.time > muteDelay)
            {
                muteDelay = Time.time + 0.15f;
                foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal() && rig.muted))
                {
                    try
                    {
                        Destroy(rig);
                    }
                    catch { }
                }
            }
        }

        public static void UnghostGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                int viewID = viewIdArchive[gunTarget];
                Destroy(gunTarget, null, null, viewID);
            });
        }

        public static void UnghostAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (viewIdArchive.TryGetValue(rig, out int viewID))
                    Destroy(rig, null, null, viewID);
            }
        }

        public static void UnghostAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig rig in nearbyPlayers)
                {
                    int viewID = viewIdArchive[rig];
                    Destroy(rig, null, null, viewID);
                }
            }
        }

        public static void UnghostOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (rig.IsBeingTouched() && viewIdArchive.ContainsKey(rig))
                {
                    int viewID = viewIdArchive[rig];
                    Destroy(rig, null, null, viewID);
                }
            }
        }

        public static void IsolateGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                foreach (VRRig rig in Players.ActiveRigs)
                {
                    bool includeLocal = !Manager.GetIndex("Isolate Others").enabled || !rig.IsLocal();
                    PhotonView view = GetPhotonViewFromVRRig(rig);
                    if (includeLocal && rig != gunTarget)
                    {
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = new[] { gunTarget.GetPlayer().ActorNumber }
                        });
                    }
                }
            });
        }

        public static void IsolateAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                bool includeLocal = !Manager.GetIndex("Isolate Others").enabled || !rig.IsLocal();
                if (includeLocal)
                {
                    PhotonView view = GetPhotonViewFromVRRig(rig);
                    Destroy(rig, new Hashtable
                    {
                        { 0, view.ViewID }
                    }, new RaiseEventOptions
                    {
                        TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != view.Owner.ActorNumber).Select(plr => plr.ActorNumber).ToArray()
                    });
                }
            }
        }



        public static void IsolateAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig rig in nearbyPlayers)
                {
                    bool includeLocal = !Manager.GetIndex("Isolate Others").enabled || !rig.IsLocal();
                    if (includeLocal)
                    {
                        PhotonView view = GetPhotonViewFromVRRig(rig);
                        Destroy(rig, new Hashtable
                        {
                            { 0, view.ViewID }
                        }, new RaiseEventOptions
                        {
                            TargetActors = new[] { view.Owner.ActorNumber }
                        });
                    }
                }
            }
        }

        public static void IsolateOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && !viewIdArchive.ContainsKey(rig) && rig.IsBeingTouched())
                {
                    foreach (VRRig otherRig in Players.ActiveRigs)
                    {
                        bool includeLocal = !Manager.GetIndex("Isolate Others").enabled || !otherRig.IsLocal();
                        PhotonView view = GetPhotonViewFromVRRig(otherRig);
                        if (includeLocal && otherRig != rig)
                        {
                            Destroy(otherRig, new Hashtable
                            {
                                { 0, view.ViewID }
                            }, new RaiseEventOptions
                            {
                                TargetActors = new[] { rig.GetPlayer().ActorNumber }
                            });
                        }
                    }
                }

            }
        }

        public static void LagGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Destroy(lockTarget.GetPhotonPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void LagAll()
        {
            foreach (var rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
                Destroy(rig.GetPhotonPlayer());
        }

        public static void LagAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                    Destroy(nearbyPlayer.GetPhotonPlayer());
            }
        }

        public static void LagOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                    Destroy(rig.GetPhotonPlayer());
            }
        }
        public static void MuteGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (Time.time > muteDelay)
                {
                    Destroy(lockTarget.GetPhotonPlayer());
                    muteDelay = Time.time + 0.15f;
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void MuteAll()
        {
            if (!(Time.time > muteDelay)) return;
            foreach (var rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
                Destroy(rig.GetPhotonPlayer());

            muteDelay = Time.time + 0.15f;
        }

        public static void MuteAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0 && Time.time > muteDelay)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    Destroy(nearbyPlayer.GetPhotonPlayer());
                    muteDelay = Time.time + 0.15f;
                }
            }
        }

        public static void MuteOnTouch()
        {
            if (!Networking.InRoom) return;


            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched() && Time.time > muteDelay)
                {
                    Destroy(rig.GetPhotonPlayer());
                    muteDelay = Time.time + 0.15f;
                }
            }


        }

        public static void PromptNameChange() =>
            Prompt("Would you like to set a name?", () => PromptSingleText("Please enter the name you'd like to use:", () => name = keyboardInput, "Done"));

        public static void ChangeNameGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Hashtable hashtable = new Hashtable
                {
                    [ActorProperties.PlayerName] = name
                };
                PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(lockTarget.GetPlayer().ActorNumber, hashtable);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void ChangeNameAll()
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                Hashtable hashtable = new Hashtable
                {
                    [ActorProperties.PlayerName] = name
                };
                PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(player.ActorNumber, hashtable);
            }
        }

        public static void ChangeNameAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    Hashtable hashtable = new Hashtable
                    {
                        [ActorProperties.PlayerName] = name
                    };
                    PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(nearbyPlayer.GetPlayer().ActorNumber, hashtable);
                }
            }
        }

        public static void ChangeNameOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    Hashtable hashtable = new Hashtable
                    {
                        [ActorProperties.PlayerName] = name
                    };
                    PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(rig.GetPlayer().ActorNumber, hashtable);
                }
            }

        }

        public static void BanGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Hashtable hashtable = new Hashtable
                {
                    [ActorProperties.PlayerName] = References.Computer.anywhereTwoWeek[Random.Range(0, References.Computer.anywhereTwoWeek.Length)]
                };
                PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(lockTarget.GetPlayer().ActorNumber, hashtable);
                ManagerRegistry.MonkeAgent.SendReport("evading the name ban", lockTarget.GetPlayer().UserId, lockTarget.GetPlayer().NickName);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BanAll()
        {
            foreach (Player player in PhotonNetwork.PlayerListOthers)
            {
                Hashtable hashtable = new Hashtable
                {
                    [ActorProperties.PlayerName] = References.Computer.anywhereTwoWeek[Random.Range(0, References.Computer.anywhereTwoWeek.Length)]
                };
                PhotonNetwork.CurrentRoom.LoadBalancingClient.OpSetPropertiesOfActor(player.ActorNumber, hashtable);
                ManagerRegistry.MonkeAgent.SendReport("evading the name ban", player.UserId, player.NickName);
            }
        }
        public static void BypassModCheckersGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                customPropertyDelay = Time.time + 0.25f;

                var player = gunTarget.GetPhotonPlayer();
                if (player == null) return;

                if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

                Hashtable toRemove = new Hashtable();

                foreach (var key in from keyObj in player.CustomProperties.Keys.ToList() select keyObj?.ToString() into key where key != null where !key.Equals(PlayerConfig.Player_HasDoneTutorial) select key)
                    toRemove[key] = null;

                if (toRemove.Count > 0)
                    player.SetCustomProperties(toRemove);
            }, new GunLibrary.GunSettings { when = () => Time.time > customPropertyDelay });

        }

        public static void BypassModCheckersAll()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player == null) continue;

                if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

                Hashtable toRemove = new Hashtable();

                foreach (var key in from keyObj in player.CustomProperties.Keys.ToList() select keyObj?.ToString() into key where key != null where !key.Equals(PlayerConfig.Player_HasDoneTutorial) select key)
                    toRemove[key] = null;

                if (toRemove.Count > 0)
                    player.SetCustomProperties(toRemove);
            }
        }

        public static void BypassModCheckersAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count <= 0) return;
            foreach (var player in nearbyPlayers.Select(nearbyPlayer => nearbyPlayer.GetPhotonPlayer()).Where(player => player != null))
            {
                if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

                Hashtable toRemove = new Hashtable();

                foreach (var key in from keyObj in player.CustomProperties.Keys.ToList() select keyObj?.ToString() into key where key != null where !key.Equals(PlayerConfig.Player_HasDoneTutorial) select key)
                    toRemove[key] = null;

                if (toRemove.Count > 0)
                    player.SetCustomProperties(toRemove);
            }
        }

        public static void BypassModCheckersOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    Player player = rig.GetPhotonPlayer();
                    if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

                    Hashtable toRemove = new Hashtable();

                    var keysToRemove = player.CustomProperties.Keys
                        .Select(keyObj => keyObj?.ToString())
                        .Where(key => key != null && !key.Equals(PlayerConfig.Player_HasDoneTutorial));

                    foreach (var key in keysToRemove)
                        toRemove[key] = null;

                    if (toRemove.Count > 0)
                        player.SetCustomProperties(toRemove);
                }
            }
        }

        public static void BreakModCheckersGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                customPropertyDelay = Time.time + 0.25f;

                Hashtable props = new Hashtable();
                foreach (string mod in Visuals.modDictionary.Keys)
                    props[mod] = true;

                gunTarget.GetPhotonPlayer().SetCustomProperties(props);
            }, new GunLibrary.GunSettings { when = () => Time.time > customPropertyDelay });
        }

        public static void BreakModCheckersAll()
        {
            Hashtable props = new Hashtable();
            foreach (string mod in Visuals.modDictionary.Keys)
                props[mod] = true;

            foreach (Player player in PhotonNetwork.PlayerList)
                player.SetCustomProperties(props);
        }

        public static void BreakModCheckersAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    Hashtable props = new Hashtable();
                    foreach (string mod in Visuals.modDictionary.Keys)
                        props[mod] = true;

                    nearbyPlayer.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void BreakModCheckersOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    Hashtable props = new Hashtable();
                    foreach (string mod in Visuals.modDictionary.Keys)
                        props[mod] = true;

                    rig.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void GamemodeIncludeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                customPropertyDelay = Time.time + 0.25f;

                Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, true } };
                gunTarget.GetPhotonPlayer().SetCustomProperties(props);
            }, new GunLibrary.GunSettings { when = () => Time.time > customPropertyDelay });
        }

        public static void GamemodeIncludeAll()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, true } };
                player.SetCustomProperties(props);
            }
        }

        public static void GamemodeIncludeAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, true } };
                    nearbyPlayer.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void GamemodeIncludeOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, true } };
                    rig.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void GamemodeExcludeGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                customPropertyDelay = Time.time + 0.25f;

                Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, false } };
                gunTarget.GetPhotonPlayer().SetCustomProperties(props);
            }, new GunLibrary.GunSettings { when = () => Time.time > customPropertyDelay });
        }

        public static void GamemodeExcludeAll()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, false } };
                player.SetCustomProperties(props);
            }
        }

        public static void GamemodeExcludeAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (vrrig.Distance(References.LocalRig) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (VRRig nearbyPlayer in nearbyPlayers)
                {
                    Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, false } };
                    nearbyPlayer.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void GamemodeExcludeOnTouch()
        {
            if (!Networking.InRoom) return;

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.IsBeingTouched())
                {
                    Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, false } };
                    rig.GetPhotonPlayer().SetCustomProperties(props);
                }
            }
        }

        public static void BreakGamemode(bool breaking)
        {
            Hashtable props = new Hashtable { { PlayerConfig.Player_HasDoneTutorial, !breaking } };

            foreach (Player player in PhotonNetwork.PlayerList)
                player.SetCustomProperties(props);
        }

        public static void BreakNetworkTriggers()
        {
            string queue = Manager.GetIndex("Switch to Modded Gamemode").enabled ? References.Computer.currentQueue + "MODDED_" : References.Computer.currentQueue;
            Hashtable hash = new Hashtable
            {
                {RoomConfig.Room_GameModePropKey, string.Join("", References.Computer.allowedMapsToJoin) + queue + References.Computer.currentGameMode.Value }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        public static void KickNetworkTriggers()
        {
            if (ManagerRegistry.NetworkSystem.SessionIsPrivate)
                Overpowered.SetRoomStatus(false);

            Hashtable hash = new Hashtable
            {
                { RoomConfig.Room_GameModePropKey, string.Empty }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
        public static void SpazGamemode()
        {
            if (Time.time > spazGamemodeDelay)
            {
                ChangeGamemode((GameModeType)Random.Range(0, (int)GameModeType.Count));
                spazGamemodeDelay = Time.time + 0.1f;
            }
        }
        public static void ChangeGamemode(GameModeType gamemode)
        {
            if (!Networking.InRoom)
                return;

            ManagerRegistry.NetworkSystem.NetDestroy(GameMode.activeNetworkHandler.NetView.gameObject);

            Hashtable hash = new Hashtable
            {
                {
                    RoomConfig.Room_GameModePropKey,
                    new GameModeString
                    {
                        zone = ManagerRegistry.PhotonNetworkController.currentJoinTrigger.networkZone,
                        queue = moddedGamemode ? References.Computer.currentQueue + "MODDED_" : References.Computer.currentQueue,
                        gameType = gamemode.ToString()
                    }.ToString()
                }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        public static void BanSelf()
        {
            for (int i = 0; i > 2; i++)
            {
                ManagerRegistry.GorillaServer.CheckForBadName(new CheckForBadNameRequest
                {
                    name = References.Computer.anywhereTwoWeek[Random.Range(0, References.Computer.anywhereTwoWeek.Length)],
                    forRoom = true,
                    forTroop = false
                }, null, null);
            }
        }
        #endregion
    }
}
