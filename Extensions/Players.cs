/*
 * Qubit Menu  Extensions/Players.cs
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
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Qubit.Core;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.Serialization;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Qubit.Extensions
{
    public static class Players
    {
        #region NetPlayer
        public static Player GetPlayer(this NetPlayer self) =>
            Players.NetPlayerToPlayer(self);

        public static VRRig VRRig(this NetPlayer self) =>
            Players.GetVRRigFromPlayer(self);

        public static bool InRoom(this NetPlayer self) =>
            ManagerRegistry.NetworkSystem.AllNetPlayers.Contains(self);

        public static Hashtable GetCustomProperties(this NetPlayer self) =>
            self.GetPlayer().CustomProperties;
        #endregion

        #region Player
        public static VRRig VRRig(this Player self) =>
            Players.GetVRRigFromPlayer(self);

        public static bool InRoom(this Player self) =>
            PhotonNetwork.PlayerList.Contains(self);

        #endregion

        #region GorillaTagger
        public static bool IsGrounded(this GorillaTagger tagger, float maxDistance = 0.15f) =>
                Physics.Raycast(tagger.bodyCollider.transform.position - new Vector3(0f, 0.2f, 0f), Vector3.down, maxDistance, References.Player.locomotionEnabledLayers);
        #endregion

        public static VRRig GetVRRigFromPlayer(NetPlayer p) =>
            GorillaGameManager.StaticFindRigForPlayer(p);

        public static NetPlayer GetPlayerFromVRRig(VRRig p) =>
            p.Creator ?? ManagerRegistry.NetworkSystem.GetPlayer(ManagerRegistry.NetworkSystem.GetOwningPlayerID(p.rigSerializer.gameObject));

        public static NetPlayer GetPlayerFromID(string id) =>
            PhotonNetwork.PlayerList.FirstOrDefault(player => player.UserId == id);

        public static Player NetPlayerToPlayer(NetPlayer p) =>
            p.GetPlayerRef();

        public static Player GetRandomPlayer(bool includeSelf = false) =>
            includeSelf ?
            PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)] :
            PhotonNetwork.PlayerListOthers[UnityEngine.Random.Range(0, PhotonNetwork.PlayerListOthers.Length)];

        private static VRRig rigTarget;
        private static float rigTargetChange;
        public static VRRig GetTargetPlayer(float targetChangeDelay = 1f)
        {
            if (!(Time.time > rigTargetChange) && rigTarget.Active()) return rigTarget;
            rigTargetChange = Time.time + targetChangeDelay;
            rigTarget = GetRandomVRRig();

            return rigTarget;
        }

        public static VRRig GetRandomVRRig(bool includeSelf = false) =>
            GetVRRigFromPlayer(GetRandomPlayer(includeSelf));

        public static NetworkView GetNetworkViewFromVRRig(VRRig p) =>
            p.netView;

        public static PhotonView GetPhotonViewFromVRRig(VRRig p) =>
            GetNetworkViewFromVRRig(p).GetView;

        public static VRRig GetClosestVRRig() =>
            References.LocalRig.GetClosest();

        public static readonly Dictionary<string, float> waitingForCreationDate = new Dictionary<string, float>();
        public static readonly Dictionary<string, string> creationDateCache = new Dictionary<string, string>();
        public static string GetCreationDate(string input, Action<string> onTranslated = null, string format = "MMMM dd, yyyy h:mm tt")
        {
            if (creationDateCache.TryGetValue(input, out string date))
                return date;
            if (!waitingForCreationDate.ContainsKey(input))
            {
                waitingForCreationDate[input] = Time.time + 10f;
                GetCreationCoroutine(input, onTranslated, format);
            }
            else
            {
                if (!(Time.time > waitingForCreationDate[input])) return "Loading...";
                waitingForCreationDate[input] = Time.time + 10f;
                GetCreationCoroutine(input, onTranslated, format);
            }

            return "Loading...";
        }

        public static void GetCreationCoroutine(string userId, Action<string> onTranslated = null, string format = "MMMM dd, yyyy h:mm tt")
        {
            if (creationDateCache.TryGetValue(userId, out string date))
            {
                onTranslated?.Invoke(date);
                return;
            }

            PlayFabClientAPI.GetPlayerProfile(
                new GetPlayerProfileRequest
                {
                    PlayFabId = userId,
                    ProfileConstraints = new PlayerProfileViewConstraints
                    { ShowCreated = true }
                }, delegate (GetPlayerProfileResult result)
            {
                string creationDate = result.PlayerProfile.Created.Value.ToString(format);
                creationDateCache[userId] = creationDate;

                onTranslated?.Invoke(creationDate);
            }, delegate { creationDateCache[userId] = "Error"; onTranslated?.Invoke("Error"); });
        }

        public static bool IsLocal(this VRRig rig, bool ghostRig = true) =>
            rig != null && (rig.isLocal || (GhostRig != null && rig == GhostRig));

        public static bool IsBeingHeld(this VRRig rig, VRRig remoteRig = null) =>
            rig != null && (
                (!rig.IsLeftHandGrabbable() && (remoteRig == null || rig.leftHandLink.grabbedPlayer == remoteRig.GetPlayer())) ||
                (!rig.IsRightHandGrabbable() && (remoteRig == null || rig.rightHandLink.grabbedPlayer == remoteRig.GetPlayer()))
            );

        public static bool IsLeftHandGrabbable(this VRRig rig) =>
            rig != null && rig.leftHandLink.CanBeGrabbed();

        public static bool IsRightHandGrabbable(this VRRig rig) =>
           rig != null && rig.rightHandLink.CanBeGrabbed();

        public static bool IsHoldingLeftIndex(this VRRig rig) =>
            rig != null && rig.leftIndex.calcT > 0.8f;

        public static bool IsHoldingRightIndex(this VRRig rig) =>
              rig != null && rig.rightIndex.calcT > 0.8f;

        public static bool IsHoldingLeftGrip(this VRRig rig) =>
            rig != null && rig.leftMiddle.calcT > 0.8f;

        public static bool IsHoldingRightGrip(this VRRig rig) =>
           rig != null && rig.rightMiddle.calcT > 0.8f;

        public static float GetRecorderLoudness(this VRRig rig)
        {
            GorillaSpeakerLoudness recorder = rig.GetComponent<GorillaSpeakerLoudness>();
            return recorder != null ? recorder.Loudness * 3f : 0f;
        }
        public static bool IsBeingTouched(this VRRig rig, VRRig otherRig = null, float distance = 0.35f)
        {
            if (rig == null) return false;
            VRRig targetRig = otherRig ?? References.LocalRig;
            return rig.Distance(targetRig.leftHand.rigTarget.position) <= distance || rig.Distance(targetRig.rightHand.rigTarget.position) <= distance;
        }

        public static bool IsNear(this VRRig rig, VRRig otherRig = null, float distance = 3f)
        {
            if (rig == null) return false;
            VRRig targetRig = otherRig ?? References.LocalRig;
            return rig.Distance(targetRig.transform.position) <= distance;
        }

        public static bool IsTouchingMe(this VRRig rig, float distance = 0.35f)
        {
            return rig == null
                ? false
                : rig.Distance(References.LocalRig.leftHand.rigTarget.position) <= distance || rig.Distance(References.LocalRig.rightHand.rigTarget.position) <= distance;
        }

        public static bool IsTagged(this VRRig rig)
        {
            if (rig == null) return false;
            List<NetPlayer> infectedPlayers = InfectedList();
            NetPlayer targetPlayer = rig.GetPlayer();

            return infectedPlayers.Contains(targetPlayer);
        }

        public enum PlatformType
        {
            Steam,
            Quest,
            Rift,
            PC,
            Unknown
        }

        public static bool IsSteam(this VRRig rig) =>
            rig != null && rig.GetPlatform() is PlatformType.Steam;

        public static bool IsKIDRestricted(this VRRig rig) =>
            rig != null && !rig.IsMicEnabled && rig.GetName().ToLower().StartsWith("gorilla");

        public static PlatformType GetPlatform(this VRRig rig)
        {
            if (rig == null) return PlatformType.Unknown;
            string platform = ManagerRegistry.NetworkSystem.GetPlayerPlatform(rig.GetPlayer().ActorNumber);

            int suspiciouslySteam = 0;
            int suspiciouslyPC = 0;
            int suspiciouslyQuest = 0;
            string concatStringOfCosmeticsAllowed = rig.Cosmetics();

            if (concatStringOfCosmeticsAllowed.Contains("S. FIRST LOGIN"))
                suspiciouslySteam++;

            if (concatStringOfCosmeticsAllowed.Contains("FIRST LOGIN") || rig.GetPhotonPlayer().CustomProperties.Count >= 2)
                suspiciouslyPC++;

            /*
            if (rig.currentRankedSubTierPC > 0)
                suspiciouslyPC++;
            else if (rig.currentRankedSubTierQuest > 0)
                suspiciouslyQuest++;
            */

            if (!string.IsNullOrEmpty(platform) && Enum.TryParse(platform, out PlatformType result))
                return result;
            int max = Math.Max(suspiciouslySteam, Math.Max(suspiciouslyPC, suspiciouslyQuest));

            return suspiciouslySteam == max
                ? PlatformType.Steam
                : suspiciouslyQuest == max ? PlatformType.Quest : suspiciouslyPC == max ? PlatformType.PC : PlatformType.Unknown;
        }

        public static string GetCreationDate(this VRRig rig, Action<string> onTranslated = null, string format = "MMMM dd, yyyy h:mm tt") =>
            Players.GetCreationDate(rig.Creator.UserId, onTranslated, format);

        // Legacy cosmetics are named like "LBAAD." (L + 4 letters + "."); a handful of owned items
        // (e.g. "Early Access Supporter Pack") don't follow this format and are excluded by the regex.
        private static readonly System.Text.RegularExpressions.Regex LegacyCosmeticCodeRegex = new System.Text.RegularExpressions.Regex(@"L([A-Z]{4})\.");
        private static readonly System.Text.RegularExpressions.Regex LegacyCosmeticCodeExactRegex = new System.Text.RegularExpressions.Regex(@"^L([A-Z]{4})\.$");

        public static readonly DateTime CosmeticDateMinValue = new DateTime(2021, 4, 1);

        private static long CosmeticCodeToBase26(string code)
        {
            long value = 0;
            foreach (char c in code)
                value = value * 26 + (c - 'A');
            return value;
        }

        private static long GetSmallestLegacyCosmeticCodeValue()
        {
            long min = long.MaxValue;
            foreach (var item in ManagerRegistry.CosmeticsController.allCosmetics)
            {
                var match = LegacyCosmeticCodeExactRegex.Match(item.itemName);
                if (!match.Success) continue;

                long value = CosmeticCodeToBase26(match.Groups[1].Value);
                if (value < min) min = value;
            }
            return min;
        }

        private static long GetLargestLegacyCosmeticCodeValue()
        {
            long max = long.MinValue;
            foreach (var item in ManagerRegistry.CosmeticsController.allCosmetics)
            {
                var match = LegacyCosmeticCodeExactRegex.Match(item.itemName);
                if (!match.Success) continue;

                long value = CosmeticCodeToBase26(match.Groups[1].Value);
                if (value > max) max = value;
            }
            return max;
        }

        public static string GetCosmeticBasedCreationDate(this VRRig rig, string format = "MMMM dd, yyyy")
        {
            var match = LegacyCosmeticCodeRegex.Match(rig.Cosmetics());
            if (!match.Success) return "Error";

            long minValue = GetSmallestLegacyCosmeticCodeValue();
            long maxValue = GetLargestLegacyCosmeticCodeValue();
            long value = CosmeticCodeToBase26(match.Groups[1].Value);

            DateTime now = DateTime.Now;
            DateTime maxDate = new DateTime(now.Year, now.Month, 1);

            double fraction = Math.Clamp((double)(value - minValue) / (maxValue - minValue), 0d, 1d);
            DateTime estimatedDate = CosmeticDateMinValue + TimeSpan.FromTicks((long)((maxDate - CosmeticDateMinValue).Ticks * fraction));

            return estimatedDate.ToString(format);
        }

        public static Color GetColor(this VRRig rig)
        {
            if (Manager.GetIndex("Follow Player Colors").enabled)
                return rig.playerColor;

            if (rig.bodyRenderer.cosmeticBodyType == GorillaBodyType.Skeleton)
                return Color.green;

            switch (rig.setMatIndex)
            {
                case 1:
                    return Color.red;
                case 2:
                case 11:
                    return new Color32(255, 128, 0, 255);
                case 3:
                case 7:
                    return Color.blue;
                case 12:
                    return Color.green;
                default:
                    return rig.playerColor;
            }
        }

        public static bool Active(this VRRig rig) =>
            rig != null && ActiveRigs.Contains(rig);

        public static float Distance(this VRRig rig, Vector3 position) =>
            Vector3.Distance(rig.transform.position, position);

        public static float Distance(this VRRig rig, VRRig otherRig) =>
            rig.Distance(otherRig.transform.position);

        public static float Distance(this VRRig rig) =>
            rig.Distance(References.BodyCollider.transform.position);

        public static VRRig GetClosest(this VRRig rig) =>
            ActiveRigs.Where(targetRig => targetRig != null && targetRig != rig)
                                         .OrderBy(rig.Distance)
                                         .FirstOrDefault();

        public static int GetFPS(this VRRig rig) =>
            Mathf.Max(rig.rigContainer.PlayerStats.FPS, rig.fps);

        public static int GetTargetFPS(this VRRig rig) =>
            rig.rigContainer.PlayerStats.TargetFPS;
        public static int GetPing(this VRRig rig) =>
            Mathf.Max(rig.rigContainer.PlayerStats.Ping, playerPing.TryGetValue(rig, out int ping) ? ping : PhotonNetwork.GetPing());

        public static int GetTruePing(this VRRig rig)
        {
            double ping = Math.Abs((rig.velocityHistoryList[0].time - PhotonNetwork.Time) * 1000);
            int safePing = (int)Math.Clamp(Math.Round(ping), 0, int.MaxValue);

            return safePing;
        }

        public static string GetName(this VRRig rig) =>
            Players.GetPlayerFromVRRig(rig)?.NickName ?? string.Empty;

        public static NetPlayer GetPlayer(this VRRig rig) =>
           Players.GetPlayerFromVRRig(rig);

        public static Photon.Realtime.Player GetPhotonPlayer(this VRRig rig) =>
            Players.NetPlayerToPlayer(Players.GetPlayerFromVRRig(rig));

        public static NetworkView GetNetView(this VRRig rig) =>
            rig.netView;

        public static PhotonView GetPhotonView(this VRRig rig) =>
            rig.netView.GetView;

        public static ProjectileWeapon GetSlingshot(this VRRig rig) =>
            rig.projectileWeapon;

        public static float[] GetSpeed(this VRRig rig)
        {
            NetPlayer player = rig.GetPlayer();
            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    GorillaTagManager tagManager = ManagerRegistry.GorillaTagManager;
                    return tagManager.isCurrentlyTag
                        ? player == tagManager.currentIt
                            ? (new[]
                            {
                                tagManager.fastJumpLimit,
                                tagManager.fastJumpMultiplier
                            })
                            : (new[]
                        {
                            tagManager.slowJumpLimit,
                            tagManager.slowJumpMultiplier
                        })
                        : tagManager.currentInfected.Contains(player)
                            ? (new[]
                            {
                                tagManager.InterpolatedInfectedJumpSpeed(tagManager.currentInfected.Count),
                                tagManager.InterpolatedInfectedJumpMultiplier(tagManager.currentInfected.Count)
                            })
                            : (new[]
                        {
                            tagManager.InterpolatedNoobJumpSpeed(tagManager.currentInfected.Count),
                            tagManager.InterpolatedNoobJumpMultiplier(tagManager.currentInfected.Count)
                        });
                default:
                    return new[] { 6.5f, 1.1f };
            }
        }

        public static float GetMaxSpeed(this VRRig rig) =>
            rig.GetSpeed()[0];

        public static float GetSpeedMultiplier(this VRRig rig) =>
            rig.GetSpeed()[1];

        public static string Cosmetics(this VRRig rig) =>
            rig._playerOwnedCosmetics.Concat();

        public static bool IsVIMSubscriber(this VRRig rig) =>
            ManagerRegistry.SubscriptionManager.subData[rig.GetPlayer()].active;

        private static readonly List<VRRig> _rigs = new List<VRRig>();
        private static int _lastFrame = -1;
        private static readonly object _lock = new object();
        public static List<VRRig> ActiveRigs
        {
            get
            {
                int frame = Time.frameCount;
                if (frame == _lastFrame)
                    return _rigs;

                lock (_lock)
                {
                    if (frame == _lastFrame)
                        return _rigs;

                    _lastFrame = frame;
                    _rigs.Clear();

                    foreach (var rig in VRRigCache.ActiveRigs)
                    {
                        if (rig != null && !Modules.Categories.Settings.Blocked.Contains(rig))
                            _rigs.Add(rig);
                    }
                }

                return _rigs;
            }
        }

        public static string CleanPlayerName(string input, int length = 12)
        {
            input = NoRichtextTags(input);

            if (input.Length > length)
                input = input[..(length - 1)];

            return input;
        }
    }
}
