/*
 * Qubit Menu  Libraries/Networking/Patreon.cs
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

using Qubit.Libraries.Audio;
using Qubit.Core;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Extensions;
using Qubit.Frontend.Resources;
using Qubit.Libraries.Interface;
using Qubit.Modules;
using Qubit.Modules.Categories;
using Qubit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static Qubit.Extensions.Players;
using static Qubit.Utilities.Assets;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Libraries.Networking
{
    public class Patreon : MonoBehaviour
    {
        public static Patreon instance = null;

        public void Awake()
        {
            instance = this;
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived;
        }

        public readonly List<PatreonMembership> PatreonMembers = new List<PatreonMembership>();
        public readonly struct PatreonMembership
        {
            public readonly string UserId;
            public readonly string TierName;
            public readonly string IconURL;
            public readonly Color Color;

            public PatreonMembership(string userId, string tierName, string iconURL, Color color)
            {
                UserId = userId;
                TierName = tierName;
                IconURL = iconURL;
                Color = color;
            }
        }

        private Material iconMaterial;
        private readonly Dictionary<VRRig, GameObject> iconPool = new Dictionary<VRRig, GameObject>();
        private static readonly List<Player> excludedIndicators = new List<Player>();

        public static KeyValuePair<NetPlayer, PatreonMembership>[] GetAllMembersInRoom()
        {
            return !Qubit.Core.Networking.InRoom
                ? Array.Empty<KeyValuePair<NetPlayer, PatreonMembership>>()
                : ManagerRegistry.NetworkSystem.PlayerListOthers
                .Select(player => new { player, membership = instance.PatreonMembers.FirstOrDefault(m => m.UserId == player.UserId) })
                .Where(x => x.membership.UserId != null)
                .Select(x => new KeyValuePair<NetPlayer, PatreonMembership>(x.player, x.membership))
                .ToArray();
        }

        public static bool IsPlayerPatreonMember(NetPlayer player) =>
            instance.PatreonMembers.Any(m => m.UserId == player.UserId);

        public static bool IndicatorsEnabled = true;
        public void Update()
        {
            List<VRRig> toRemoveRigs = new List<VRRig>();

            foreach (var indicator in iconPool.Where(indicator => !IndicatorsEnabled || !indicator.Key.Active() || !IsPlayerPatreonMember(GetPlayerFromVRRig(indicator.Key)) || excludedIndicators.Contains(indicator.Key.GetPhotonPlayer())))
            {
                toRemoveRigs.Add(indicator.Key);
                Destroy(indicator.Value);
            }

            foreach (VRRig rig in toRemoveRigs)
                iconPool.Remove(rig);

            if (!IndicatorsEnabled) return;

            if (!Qubit.Core.Networking.InRoom) return;
            var members = GetAllMembersInRoom();
            foreach (var member in members)
            {
                VRRig playerRig = GetVRRigFromPlayer(member.Key);
                if (playerRig == null) continue;
                if (excludedIndicators.Contains(member.Key.GetPlayer())) continue;

                if (!iconPool.TryGetValue(playerRig, out GameObject playerIndicator))
                {
                    playerIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(playerIndicator.GetComponent<Collider>());

                    if (iconMaterial == null)
                    {
                        iconMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

                        iconMaterial.SetFloat("_Surface", 1);
                        iconMaterial.SetFloat("_Blend", 0);
                        iconMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        iconMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        iconMaterial.SetFloat("_ZWrite", 0);
                        iconMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        iconMaterial.renderQueue = (int)RenderQueue.Transparent;
                    }

                    playerIndicator.GetComponent<Renderer>().material = iconMaterial;
                    playerIndicator.GetComponent<Renderer>().material.mainTexture = LoadTextureFromURL(member.Value.IconURL, $"Images/Patreon/{member.Key.UserId}.{Files.GetFileExtension(member.Value.IconURL)}"); 
                    playerIndicator.GetComponent<Renderer>().material.color = Color.white;

                    GameObject go = new GameObject("Qubit_Nametag");
                    go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    TextMeshPro textMesh = go.AddComponent<TextMeshPro>();
                    textMesh.fontSize = 4.8f;
                    textMesh.alignment = TextAlignmentOptions.Center;

                    textMesh.SafeSetText(member.Value.TierName);
                    textMesh.SafeSetFontStyle(Fonts.activeFontStyle);
                    textMesh.SafeSetFont(Fonts.activeFont);
                    textMesh.color = member.Value.Color;
                    textMesh.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    textMesh.transform.SetParent(playerIndicator.transform, false);

                    iconPool.Add(playerRig, playerIndicator);
                }

                float distance = Qubit.Backend.Console.GetIndicatorDistance(playerRig);
                playerIndicator.transform.localScale = new Vector3(0.4f, 0.4f, 0.01f) * playerRig.scaleFactor;
                playerIndicator.transform.position = Visuals.GetNameTagTransform(playerRig).position + Visuals.GetNameTagTransform(playerRig).up * (distance * playerRig.scaleFactor);
                playerIndicator.transform.LookAt(References.HeadCollider.transform.position);

                GameObject nameTag = playerIndicator.transform.Find("Qubit_Nametag").gameObject;
                nameTag.transform.position = Visuals.GetNameTagTransform(playerRig).position + Visuals.GetNameTagTransform(playerRig).up * ((distance + 0.25f) * playerRig.scaleFactor);
                nameTag.transform.LookAt(Camera.main.transform.position);
                nameTag.transform.Rotate(0f, 180f, 0f);
            }
        }

        public const byte PatreonByte = 74;
        public static void EventReceived(EventData data)
        {
            try
            {
                NetPlayer sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (data.Code != PatreonByte || !IsPlayerPatreonMember(sender)) return;
                VRRig senderRig = GetVRRigFromPlayer(sender);
                object[] args = data.CustomData == null ? new object[] { } : (object[])data.CustomData;
                string command = args.Length > 0 ? (string)args[0] : "";

                switch (command)
                {
                    case "indicator":
                        {
                            if (args.Length > 1 && args[1] is bool enabled)
                            {
                                if (enabled)
                                    if (!excludedIndicators.Contains(sender.GetPlayer()))
                                        excludedIndicators.Add(sender.GetPlayer());
                                    else
                                    if (excludedIndicators.Contains(sender.GetPlayer()))
                                        excludedIndicators.Remove(sender.GetPlayer());
                            }
                            break;
                        }
                }
            }
            catch { }
        }

        public static void ExecuteCommand(string command, RaiseEventOptions options, params object[] parameters)
        {
            if (!Qubit.Core.Networking.InRoom)
                return;

            PhotonNetwork.RaiseEvent(PatreonByte,
                new object[] { command }
                    .Concat(parameters)
                    .ToArray(),
            options, SendOptions.SendReliable);
        }

        public static void ExecuteCommand(string command, int[] targets, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = targets }, parameters);

        public static void ExecuteCommand(string command, int target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { TargetActors = new[] { target } }, parameters);

        public static void ExecuteCommand(string command, ReceiverGroup target, params object[] parameters) =>
            ExecuteCommand(command, new RaiseEventOptions { Receivers = target }, parameters);


        #region Patreon Mods
        public static void SetupPatreonMods(string patreonName)
        {
            Notifications.SendFormat("purple", "PATREON", $"Welcome, {patreonName}! Patreon mods have been enabled.", 10000);

            List<Module> buttons = Manager.modules["Main"].ToList();
            buttons.Add(new Module { buttonText = "Patreon Mods", method = () => Manager.CurrentCategoryName = "Patreon Mods", isTogglable = false, toolTip = "Opens the patreon mods." });
            Manager.modules["Main"] = buttons.ToArray();

            if (Frontend.Settings.dynamicSounds)
            {
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/patreon.ogg", "Audio/Menu/patreon.ogg", clip =>
                {
                    clip?.Play(Frontend.Settings.buttonClickVolume / 10f);
                });
            }
        }

        public static void ShowIndicator(bool enabled) =>
            ExecuteCommand("indicator", ReceiverGroup.All, enabled);

        private static int lastPlayerCount;
        public static void ConstantHideIndicator()
        {
            if (!Qubit.Core.Networking.InRoom)
                lastPlayerCount = -1;

            if (PhotonNetwork.PlayerList.Length != lastPlayerCount && Qubit.Core.Networking.InRoom)
            {
                ShowIndicator(false);
                lastPlayerCount = PhotonNetwork.PlayerList.Length;
            }
        }
        #endregion
    }
}