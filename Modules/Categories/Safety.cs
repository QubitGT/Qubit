/*
 * Qubit Menu  Modules/Categories/Safety.cs
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
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Patches.Netcode;
using Qubit.Patches.Safety.AntiBan;
using Qubit.Patches.Safety.AntiCheat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Players;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories
{
    public static class Safety
    {
        #region Settings
        private static bool antiOculusReportHooked;

        public static Vector3 deadPosition = Vector3.zero;
        public static Vector3 lvel = Vector3.zero;

        private static string previousNickName;

        public static float flushCooldown;

        private static float lastCacheClearedTime;

        public static int antiReportRangeIndex;
        public static float threshold = 0.35f;
        public static readonly string[] AntiReportRangeNames = { "Default", "Large", "Massive" };
        public static readonly float[] AntiReportRangeDistances = { 0.35f, 0.7f, 1.5f };

        public static bool smartAntiReport;
        public static int buttonClickTime;
        public static string buttonClickPlayer;

        public static bool antiMute;

        public static VRRig reportRig;

        public static float antiReportDelay;

        public static float antiReportNotifyDelay;

        private static bool previousSpecial;

        private static float lastVol;
        private static float startSilenceTime = -1f;
        private static bool reloaded;

        private static Vector3 smoothedLeftHandPosition;
        private static Vector3 smoothedRightHandPosition;

        private static bool previouslyInLobby;

        private static readonly List<VRRig> nameSpoofRigs = new List<VRRig>();

        private static readonly List<VRRig> colorSpoofRigs = new List<VRRig>();

        public static readonly string[] namePrefix = {
            "EPIC", "EPIK", "REAL", "NOT", "SILLY", "LITTLE", "BIG", "MAYBE", "MONKE", "SUB2", "OG", "FUN", "FR", "NOT", "NOTA"
        };
        public static readonly string[] nameSuffix = {
            "GT", "VR", "LOL", "GTVR", "FAN", "XD", "LOL", "MONK", "YT", "NOT", "FR"
        };
        public static readonly string[] names = {
            "0", "SHIBA", "PBBV", "J3VU", "BEES", "NAMO", "MANGO", "FROSTY", "FRISH", "LEMMING",
            "BILLY", "TIMMY", "MINIGAMES", "JMANCURLY", "VMT", "ELLIOT", "POLAR", "3CLIPCE", "DAISY09",
            "SHARKPUPPET", "DUCKY", "EDDIE", "EDDY", "RAKZZ", "CASEOH", "SKETCH", "SKY", "RETURN",
            "WATERMELON", "CRAZY", "MONK", "MONKE", "MONKI", "MONKEY", "MONKIY", "GORILL", "GOORILA", "GORILLA",
            "REDBERRY", "FOX", "RUFUS", "TTT", "TTTPIG", "PPPTIG", "K9", "BTC", "TICKLETIPJR", "BANANA",
            "PEANUTBUTTER", "GHOSTMONKE", "STATUE", "TURBOALLEN", "NOVA", "LUNAR", "MOON", "SUN", "RANDOM", "UNKNOWN",
            "GLITCH", "BUG", "ERROR", "CODE", "HACKER", "MODDER", "INVIS", "INVISIBLE", "TAGGER", "UNTAGGED",
            "BLUE", "RED", "GREEN", "PURPLE", "YELLOW", "BLACK", "WHITE", "BROWN", "CYAN", "GRAY",
            "GREY", "BANNED", "LEMON", "PLUSHIE", "CHEETO", "TIKTOK", "YOUTUBE", "TWITCH", "DISCORD", "MODDER", "HACKER"
        };
        public static int rankIndex = 2;

        public static readonly string[] BadgeNames = { "Wood", "Rock", "Bronze", "Silver", "Gold", "Platinum", "Crystal", "Banana" };

        public static int targetElo = 4000;
        public static int targetBadge = 7;
        #endregion

        #region Methods
        public static void GeneralSafety()
        {
            if (!Manager.GetIndex("Anti Report <color=grey>[</color><color=green>Disconnect</color><color=grey>]</color>").enabled) AntiReportDisconnect();
            if (!Manager.GetIndex("Anti Report <color=grey>[</color><color=green>Anti Cheat</color><color=grey>]</color>").enabled) SendReport.AntiACReport = true;
            if (!Manager.GetIndex("Anti Moderator").enabled) AntiModerator();
            if (!Manager.GetIndex("Anti Report <color=grey>[</color><color=green>Oculus</color><color=grey>]</color>").enabled && !antiOculusReportHooked) { antiOculusReportHooked = true; EnableAntiOculusReport(); }
        }

        public static void DisableGeneral()
        {
            if (!Manager.GetIndex("Anti Report <color=grey>[</color><color=green>Anti Cheat</color><color=grey>]</color>").enabled) SendReport.AntiACReport = false;
            if (!Manager.GetIndex("Anti Report <color=grey>[</color><color=green>Oculus</color><color=grey>]</color>").enabled) DisableAntiOculusReport();
        }

        public static void NoFinger()
        {
            References.InputPoller.leftControllerGripFloat = 0f;
            References.InputPoller.rightControllerGripFloat = 0f;
            References.InputPoller.leftControllerIndexFloat = 0f;
            References.InputPoller.rightControllerIndexFloat = 0f;
            References.InputPoller.leftControllerPrimaryButton = false;
            References.InputPoller.leftControllerSecondaryButton = false;
            References.InputPoller.rightControllerPrimaryButton = false;
            References.InputPoller.rightControllerSecondaryButton = false;
            References.InputPoller.leftControllerPrimaryButtonTouch = false;
            References.InputPoller.leftControllerSecondaryButtonTouch = false;
            References.InputPoller.rightControllerPrimaryButtonTouch = false;
            References.InputPoller.rightControllerSecondaryButtonTouch = false;
        }

        public static void SetGamemodeButtonActive(bool active = true) =>
            GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/ModeSelector_Group").SetActive(active);

        public static void FakeOculusMenu()
        {
            if (leftPrimary)
            {
                NoFinger();
                ManagerRegistry.ConnectedControllerHandler.leftHandFollower.UpdatePositionRotation();
                ManagerRegistry.ConnectedControllerHandler.rightHandFollower.UpdatePositionRotation();
            }

            Movement.SetHandEnabled(!leftPrimary);
        }

        public static void FakeReportMenu()
        {
            if (leftSecondary)
                NoFinger();

            References.Player.inOverlay = leftPrimary;
        }

        public static void FakeBrokenController()
        {
            Vector3 Position = leftPrimary ? References.LeftHand.position : References.RightHand.position;
            Quaternion Rotation = leftPrimary ? References.LeftHand.rotation : References.RightHand.rotation;

            References.Player.GetControllerTransform(true).position = References.Player.headCollider.transform.position + References.Player.headCollider.transform.up * (-0.5f * References.Player.scale);
            References.Player.GetControllerTransform(true).rotation = Camera.main.transform.rotation * Quaternion.Euler(-55f, 90f, 0f);

            References.Player.GetControllerTransform(false).position = Position;
            References.Player.GetControllerTransform(false).rotation = Rotation;

            References.InputPoller.leftControllerGripFloat = 0f;
            References.InputPoller.leftControllerIndexFloat = 0f;
            References.InputPoller.leftControllerPrimaryButton = false;
            References.InputPoller.leftControllerSecondaryButton = false;
            References.InputPoller.leftControllerPrimaryButtonTouch = false;
            References.InputPoller.leftControllerSecondaryButtonTouch = false;
        }
        public static void FakePowerOff()
        {
            if (leftJoystickClick)
            {
                if (deadPosition == Vector3.zero)
                {
                    deadPosition = References.Rigidbody.transform.position;
                    lvel = References.Rigidbody.linearVelocity;
                }
                References.LocalRig.enabled = false;
                References.Rigidbody.transform.position = deadPosition;
                References.Rigidbody.linearVelocity = lvel;
            }
            else
            {
                deadPosition = Vector3.zero;
                References.LocalRig.enabled = true;
            }
        }

        public static void FakeValveTracking()
        {
            if (rightJoystickClick)
                References.LocalRig.head.rigTarget.transform.rotation = Quaternion.identity;
        }

        public static void SpoofSupportPage() =>
            References.Computer.screenText.Set(
                References.Computer.screenText.stringBuilder.ToString()
                    .Replace("STEAM", "QUEST")
                    .Replace(References.Computer.buildDate,
                        $"{References.Computer.buildDate}\nBUILD CODE 4893\nMANAGED ACCOUNT: NO")
            );
        public static void AntiNameBan()
        {
            if (previousNickName != PhotonNetwork.LocalPlayer.NickName)
            {
                if (!CheckAutoBanListForName.CheckBanList(PhotonNetwork.LocalPlayer.NickName))
                {
                    Notifications.SendFormat("red", "WARNING", $"Your name, {PhotonNetwork.LocalPlayer.NickName}, is not allowed. It has been reset for your safety.");
                    ChangeName(Qubit.Extensions.Random.RandomString(8));
                }
            }

            previousNickName = PhotonNetwork.LocalPlayer.NickName;
        }
        public static void FlushRPCs()
        {
            if (Time.time > flushCooldown)
            {
                Security.RPCProtection();
                flushCooldown = Time.time + 5f;
                return;
            }
            Notifications.SendFormat("red", "ERROR", "You are not meant to spam Flush RPCs. Only call it once after you are done spamming RPCs.");
        }
        public static void AutoClearCache()
        {
            if (Time.time > lastCacheClearedTime)
            {
                lastCacheClearedTime = Time.time + 60f;
                GC.Collect();
            }
        }
        public static void ApplyAntiReportRange(int index) { antiReportRangeIndex = index; threshold = AntiReportRangeDistances[index]; }

        public static bool SmartAntiReport(NetPlayer linePlayer) =>
            smartAntiReport && linePlayer.UserId == buttonClickPlayer && Time.frameCount == buttonClickTime && PhotonNetwork.CurrentRoom.IsVisible && !PhotonNetwork.CurrentRoom.CustomProperties.ToString().Contains("MODDED");

        public static void EventReceived_SmartAntiReport(EventData data)
        {
            try
            {
                if (data.Code == Photon.Pun.PunEvent.RPC)
                {
                    string rpcName = PhotonNetwork.PhotonServerSettings.RpcList[int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                    object[] args = (object[])((Hashtable)data.CustomData)[4];
                    if (rpcName == "RPC_PlayHandTap" && (int)args[0] == 67)
                    {
                        buttonClickTime = Time.frameCount;
                        buttonClickPlayer = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender).UserId;
                    }
                }
            }
            catch { }
        }

        public static void EnableSmartAntiReport()
        {
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_SmartAntiReport;
            smartAntiReport = true;
        }

        public static void DisableSmartAntiReport()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_SmartAntiReport;
            smartAntiReport = false;
        }

        public static void VisualizeAntiReport()
        {
            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != Networking.LocalPlayer) continue;
                Transform report = line.reportButton.gameObject.transform;

                Visuals.Visualize(PrimitiveType.Sphere, report.position, Quaternion.identity, new Vector3(threshold, threshold, threshold), Color.red, alpha: 0.1f);

                if (antiMute)
                    Visuals.Visualize(PrimitiveType.Sphere, line.muteButton.gameObject.transform.position, Quaternion.identity, new Vector3(threshold, threshold, threshold), Color.red, -1, 0.1f);
            }
        }

        private static bool OverlappingButton(VRRig vrrig, Vector3 position) =>
            new[] {
                vrrig.rightHandTransform.position,
                vrrig.leftHandTransform.position,
                vrrig.rightHand.syncPos,
                vrrig.leftHand.syncPos
            }.Any(handPos => Vector3.Distance(handPos, position) < threshold);
        public static void AntiReport(Action<VRRig, Vector3> onReport)
        {
            if (!Networking.InRoom) return;

            if (reportRig != null)
            {
                if (reportRig.IsLocal())
                    return;

                onReport?.Invoke(reportRig, reportRig.transform.position);
                reportRig = null;
                Achievements.Unlock(new Achievements.Achievement
                {
                    name = "Troublemaker",
                    description = "Evade a player report.",
                    icon = "Images/Achievements/troublemaker.png"
                });
                return;
            }

            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != Networking.LocalPlayer) continue;
                Transform report = line.reportButton.gameObject.transform;

                foreach (var vrrig in from vrrig in Players.ActiveRigs where !vrrig.isLocal where OverlappingButton(vrrig, report.position) || (antiMute && OverlappingButton(vrrig, line.muteButton.gameObject.transform.position)) where !smartAntiReport || SmartAntiReport(line.linePlayer) select vrrig)
                    onReport?.Invoke(vrrig, report.transform.position);
            }
        }
        public static void AntiReportDisconnect()
        {
            AntiReport((vrrig, position) =>
            {
                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                Security.RPCProtection();

                if (!(Time.time > antiReportDelay)) return;
                antiReportDelay = Time.time + 1f;
                Notifications.SendFormat("purple", "ANTI-REPORT", "" + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, you have been disconnected.");
            });
        }

        public static void AntiReportReconnect()
        {
            AntiReport((vrrig, position) =>
            {
                if (!(Time.time > antiReportDelay)) return;
                Room.Reconnect();
                Security.RPCProtection();

                antiReportDelay = Time.time + 1f;
                Notifications.SendFormat("purple", "ANTI-REPORT", "" + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, you have been disconnected and will be reconnected shortly.");
            });
        }

        public static void AntiReportJoinRandom()
        {
            AntiReport((vrrig, position) =>
            {
                if (!(Time.time > antiReportDelay)) return;

                Room.JoinRandom();
                Security.RPCProtection();

                antiReportDelay = Time.time + 1f;
                Notifications.SendFormat("purple", "ANTI-REPORT", "" + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, you have been disconnected and will be reconnected shortly.");
            });
        }

        public static void EventReceived_AntiOculusReport(EventData data)
        {
            try
            {
                if (data.Code == Photon.Pun.PunEvent.RPC)
                {
                    var sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                    if (sender.IsLocal)
                        return;

                    string rpcName = PhotonNetwork.PhotonServerSettings.RpcList[int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                    object[] args = (object[])((Hashtable)data.CustomData)[4];
                    if (rpcName == "RPC_PlayHandTap" && (int)args[0] == 67)
                    {
                        VRRig target = GetVRRigFromPlayer(sender);
                        if (Vector3.Distance(target.leftHandTransform.position, target.rightHandTransform.position) < 0.1f)
                            AntiReportFRT(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender));
                    }
                }
            }
            catch { }
        }

        public static void EnableAntiOculusReport() =>
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_AntiOculusReport;

        public static void DisableAntiOculusReport() =>
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_AntiOculusReport;
        public static void AntiReportNotify()
        {
            if (!(Time.time > antiReportNotifyDelay)) return;

            string notifyText = "";
            AntiReport((vrrig, position) =>
            {
                antiReportNotifyDelay = Time.time + 0.1f;

                if (notifyText == "")
                    notifyText = GetPlayerFromVRRig(vrrig).NickName;
                else
                {
                    if (notifyText.Contains("&"))
                        notifyText = GetPlayerFromVRRig(vrrig).NickName + ", " + notifyText;
                    else
                        notifyText += " & " + GetPlayerFromVRRig(vrrig).NickName;
                }
            });

            if (notifyText != "")
                Notifications.SendFormat("purple", "ANTI-REPORT", $"{notifyText} {(notifyText.Contains("&") || notifyText.Contains(",") ? "are" : "is")} reporting you.");
        }

        public static void AntiReportOverlay()
        {
            if (!(Time.time > antiReportNotifyDelay)) return;
            string notifyText = null;
            AntiReport((vrrig, position) =>
            {
                if (notifyText == null)
                    notifyText = GetPlayerFromVRRig(vrrig).NickName;
                else
                {
                    if (notifyText.Contains("&"))
                        notifyText = GetPlayerFromVRRig(vrrig).NickName + ", " + notifyText;
                    else
                        notifyText += " & " + GetPlayerFromVRRig(vrrig).NickName;
                }
            });

            if (notifyText.IsNullOrEmpty())
                Notifications.information.Remove("Anti-Report");
            else
                Notifications.information["Anti-Report"] = notifyText;
        }

        public static void AntiReportFRT(Player subject)
        {
            if (subject.IsLocal)
                return;

            reportRig = subject.VRRig();
        }

        public static void AntiModerator()
        {
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isOfflineVRRig && vrrig.Cosmetics().Contains("LBAAK") || vrrig.Cosmetics().Contains("LBAAD") || vrrig.Cosmetics().Contains("LMAPY")))
            {
                try
                {

                    VRRig plr = vrrig;
                    NetPlayer player = GetPlayerFromVRRig(plr);
                    if (player != null)
                    {
                        string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
                        float r = 0f;
                        float g = 0f;
                        float b = 0f;
                        try
                        {

                            r = plr.playerColor.r * 255;
                            g = plr.playerColor.r * 255;
                            b = plr.playerColor.r * 255;
                        }
                        catch { Logs.Log("Failed to log colors, rig most likely nonexistent"); }

                        try
                        {
                            text += "\n====================================\n";
                            text += string.Concat("Player Name: \"", player.NickName, "\", Player ID: \"", player.UserId, "\", Player Color: (R: ", r.ToString(), ", G: ", g.ToString(), ", B: ", b.ToString(), ")");
                        }
                        catch { Logs.Log("Failed to log player"); }

                        text += "\n====================================\n";
                        text += PluginInfo.GeneratedFileFooter;
                        string fileName = $"{PluginInfo.BaseDirectory}/" + player.NickName + " - Anti Moderator.txt";

                        File.WriteAllText(fileName, text);
                    }
                }
                catch { }
                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                Notifications.SendFormat("purple", "ANTI-MODERATOR", $"{vrrig.GetName()} is a moderator, you have been disconnected. Their player ID and room code have been saved to a file.");
            }
        }

        public static void AntiContentCreator()
        {
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isOfflineVRRig && Visuals.specialCosmetics.Keys.Any(x => vrrig.Cosmetics().Contains(x))))
            {
                try
                {

                    VRRig plr = vrrig;
                    NetPlayer player = GetPlayerFromVRRig(plr);
                    if (player != null)
                    {
                        string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
                        float r = 0f;
                        float g = 0f;
                        float b = 0f;
                        try
                        {

                            r = plr.playerColor.r * 255;
                            g = plr.playerColor.r * 255;
                            b = plr.playerColor.r * 255;
                        }
                        catch { Logs.Log("Failed to log colors, rig most likely nonexistent"); }

                        try
                        {
                            text += "\n====================================\n";
                            text += string.Concat("Player Name: \"", player.NickName, "\", Player ID: \"", player.UserId, "\", Player Color: (R: ", r.ToString(), ", G: ", g.ToString(), ", B: ", b.ToString(), ")");
                        }
                        catch { Logs.Log("Failed to log player"); }

                        text += "\n====================================\n";
                        text += PluginInfo.GeneratedFileFooter;
                        string fileName = $"{PluginInfo.BaseDirectory}/" + player.NickName + " - Anti Content Creator.txt";

                        File.WriteAllText(fileName, text);
                    }
                }
                catch { }
                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                Notifications.SendFormat("purple", "ANTI-CONTENT CREATOR", $"{vrrig.GetName()} is a content creator, you have been disconnected. Their player ID and room code have been saved to a file.");
            }
        }
        public static void CosmeticNotifications()
        {
            VRRig specialRig = null;
            string specialCosmetic = null;

            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                foreach (var cosmetic in Visuals.specialCosmetics.Where(cosmetic => rig.Cosmetics().Contains(cosmetic.Key)))
                {
                    specialRig = rig;
                    specialCosmetic = cosmetic.Value;
                    break;
                }

                if (specialRig != null)
                    break;
            }

            if (specialRig != null && !previousSpecial)
                Notifications.SendFormat($"#{specialRig.GetColor().ToHex()}", "COSMETIC", $"{specialRig.GetName()} has {specialCosmetic}.");

            previousSpecial = specialRig != null;
        }

        public static void BypassAutomod()
        {
            GorillaTagger.moderationMutedTime = -1f;

            if (References.Computer.autoMuteType != "OFF")
            {
                References.Computer.autoMuteType = "OFF";
                PlayerPrefs.SetInt("autoMute", 0);
                PlayerPrefs.Save();
            }

            Recorder mic = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;
            if (mic == null)
                return;

            if (mic.SourceType == Recorder.InputSourceType.AudioClip)
                return;

            float volume = 0f;
            GorillaSpeakerLoudness recorder = References.LocalRig.GetComponent<GorillaSpeakerLoudness>();
            if (recorder != null)
                volume = recorder.Loudness;

            if (volume == 0f)
            {
                if (lastVol != 0f)
                {
                    startSilenceTime = Time.time;
                    reloaded = false;
                }

                if (startSilenceTime > 0f && !reloaded && Time.time - startSilenceTime >= 0.25f)
                {
                    mic.RestartRecording(true);
                    reloaded = true;
                }
            }
            else
            {
                startSilenceTime = -1f;
                reloaded = false;
            }

            lastVol = volume;
        }

        public static void BypassModCheckers()
        {
            var player = PhotonNetwork.LocalPlayer;
            if (player == null) return;

            if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

            Hashtable toRemove = new Hashtable();

            foreach (var key in from keyObj in player.CustomProperties.Keys.ToList() select keyObj?.ToString() into key where key != null where !key.Equals(PlayerConfig.Player_HasDoneTutorial) select key)
                toRemove[key] = null;

            if (toRemove.Count > 0)
                player.SetCustomProperties(toRemove);
        }
        public static void AntiPredictions()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 leftHandPosition = References.LocalRig.leftHand.rigTarget.localPosition;
                Vector3 rightHandPosition = References.LocalRig.rightHand.rigTarget.localPosition;

                smoothedLeftHandPosition = Vector3.Lerp(smoothedLeftHandPosition, leftHandPosition, 0.75f);
                smoothedRightHandPosition = Vector3.Lerp(smoothedRightHandPosition, rightHandPosition, 0.75f);

                References.LocalRig.leftHand.rigTarget.localPosition = smoothedLeftHandPosition;
                References.LocalRig.rightHand.rigTarget.localPosition = smoothedRightHandPosition;

                SendSerialize(References.LocalRig.GetPhotonView());

                References.LocalRig.leftHand.rigTarget.localPosition = leftHandPosition;
                References.LocalRig.rightHand.rigTarget.localPosition = rightHandPosition;

                return false;
            };
        }

        public static void ChangeIdentity()
        {
            string randomName = "gorilla";
            for (var i = 0; i < 4; i++)
                randomName += Random.Range(0, 9).ToString();

            ChangeName(randomName);

            byte randA = (byte)Random.Range(0, 255);
            byte randB = (byte)Random.Range(0, 255);
            byte randC = (byte)Random.Range(0, 255);
            ChangeColor(new Color32(randA, randB, randC, 255));
        }

        public static void ChangeIdentityRegular()
        {
            string prefix = Random.Range(0, 3) == 0 ? namePrefix[Random.Range(0, namePrefix.Length)] : "";
            string suffix = Random.Range(0, 3) == 0 ? nameSuffix[Random.Range(0, nameSuffix.Length)] : "";
            string fName = prefix + names[Random.Range(0, names.Length)] + suffix;
            ChangeName(fName.Length > 12 ? fName[..12] : fName);

            Color[] colors = {
                Color.cyan,
                Color.yellow,
                Color.blue,
                Color.gray,
                Color.black,
                Color.white,
                Color.magenta,
                Color.yellow,
                Color.green,
                new Color(1f, 0.5f, 1f, 255f),
                new Color(0f, 0.5f, 0f, 255f),
                new Color32(113, 0, 198, 255),
                new Color32(170, 198, 170, 255),
                new Color32(170, 170, 170, 255),
                new Color32(227, 170, 85, 255),
                new Color32(0, 226, 255, 255)
            };
            ChangeColor(colors[Random.Range(0, colors.Length)]);
        }

        public static void ChangeIdentityCustom()
        {
            string[] names = { "Qubit", "me" };
            Color[] colors = { new Color32(255, 128, 0, 255), Color.white };

            string fileName = $"{PluginInfo.BaseDirectory}/CustomIdentities.txt";
            if (File.Exists(fileName))
            {
                string[] data = File.ReadAllText(fileName).Split("\n");
                names = data[0].Split(";");
                colors = data[1].Split(";").Select(HexToColor).ToArray();
            }
            else
                File.WriteAllText(fileName, "Qubit;me\n9b59b6;ffffff");

            string name = names[Random.Range(0, names.Length)];
            Color color = colors[Random.Range(0, colors.Length)];

            ChangeName(name.Length > 12 ? name[..12] : name);
            ChangeColor(color);
        }
        public static void ChangeIdentityOnDisconnect(Action identityType)
        {
            if (!Networking.InRoom && previouslyInLobby)
                identityType?.Invoke();

            previouslyInLobby = Networking.InRoom;
        }
        public static void NameSpoof()
        {
            List<VRRig> toRemove = new List<VRRig>();
            foreach (VRRig rig in nameSpoofRigs)
            {
                if (!Players.ActiveRigs.Contains(rig))
                    toRemove.Add(rig);
            }

            foreach (VRRig rig in toRemove)
                nameSpoofRigs.Remove(rig);

            toRemove.Clear();

            string archiveNickname = PhotonNetwork.NickName;
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (rig.isLocal) continue;
                if (!nameSpoofRigs.Contains(rig))
                {
                    string prefix = Random.Range(0, 3) == 0 ? namePrefix[Random.Range(0, namePrefix.Length)] : "";
                    string suffix = Random.Range(0, 3) == 0 ? nameSuffix[Random.Range(0, nameSuffix.Length)] : "";
                    string fName = prefix + names[Random.Range(0, names.Length)] + suffix;
                    ChangeName(fName.EnforceLength(12), true);

                    References.MyRigView.SendRPC("RPC_InitializeNoobMaterial", rig.GetPlayer(), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                    nameSpoofRigs.Add(rig);
                }
            }

            if (PhotonNetwork.NickName != archiveNickname)
                PhotonNetwork.NickName = archiveNickname;
        }
        public static void ColorSpoof()
        {
            List<VRRig> toRemove = new List<VRRig>();
            foreach (VRRig rig in colorSpoofRigs)
            {
                if (!Players.ActiveRigs.Contains(rig))
                    toRemove.Add(rig);
            }

            foreach (VRRig rig in toRemove)
                colorSpoofRigs.Remove(rig);

            toRemove.Clear();

            foreach (var rig in Players.ActiveRigs.Where(rig => !rig.isLocal).Where(rig => !colorSpoofRigs.Contains(rig)))
            {
                References.MyRigView.SendRPC("RPC_InitializeNoobMaterial", rig.GetPlayer(), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                colorSpoofRigs.Add(rig);
            }
        }
        public static void ApplyRankedTier(int index)
        {
            rankIndex = index;
            Fun.targetRank = ((RankedProgressionManager.ERankedMatchmakingTier)index).ToString();
        }
        public static void ApplyBadgeTier(int index) => targetBadge = index;

        public static void OnlySerializeNecessary()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                SendSerialize(References.LocalRig.GetPhotonView());
                
                return false;
            };
        }
        public static void SteamDetector()
        {
            bool playerOnSteam = Players.ActiveRigs.Any(vrrig => !vrrig.IsLocal() && vrrig.IsSteam());
            if (playerOnSteam && !Important.lastSteam)
            {
                VRRig vrrig = Players.ActiveRigs.First(vrrig => !vrrig.IsLocal() && vrrig.IsSteam());
                Notifications.SendFormat("red", "STEAM", $"{vrrig.GetName()} is on Steam.");

                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Safety/steam.ogg", "Audio/Mods/Safety/steam.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
            }

            Important.lastSteam = playerOnSteam;
        }
        #endregion
    }
}
