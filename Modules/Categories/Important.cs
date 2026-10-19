/*
 * Qubit Menu  Modules/Categories/Important.cs
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
using GorillaExtensions;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using Qubit.Backend;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.DiscordRPC;
using Qubit.Libraries.DiscordRPC.Entities;
using Qubit.Libraries.Interface;
using Qubit.Patches.Events;
using Qubit.Patches.Terms;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.TextCore;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Valve.Newtonsoft.Json;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using Object = UnityEngine.Object;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules.Categories
{
    public static class Important
    {
        #region Types
        public class CustomRoomConfig
        {
            public string name;
            public bool isPublic;
            public byte size;
        }

        
        internal enum VirtualKeyCodes : uint
        {
            NEXT_TRACK = 0xB0,
            PREVIOUS_TRACK = 0xB1,
            PLAY_PAUSE = 0xB3,
        }
        #endregion

        #region Settings
        public static string oldId = "";

        public static Coroutine queueCoroutine;
        public static int reconnectDelay = 1;

        private static DiscordRpcClient discord;
        private static DateTime? startTime;
        private static DateTime? endTime;
        private static float updateTime;

        private static bool kuikkusonguExists;

        public static string Title { get; private set; } = "Unknown";
        public static string Artist { get; private set; } = "Unknown";
        public static Texture2D Icon { get; private set; } = new Texture2D(2, 2);
        public static bool Paused { get; private set; } = true;

        public static float StartTime { get; private set; }
        public static float EndTime { get; private set; }
        public static float ElapsedTime { get; private set; }

        public static bool ValidData { get; private set; }

        private static float updateDataDelay;
        private static float inputDelay;

        private static GameObject mediaIcon;
        private static Material mediaIconMaterial;

        private static TextMeshPro mediaText;

        private static TMP_SpriteAsset _mediaSpriteSheet;
        public static TMP_SpriteAsset MediaSpriteSheet
        {
            get
            {
                if (_mediaSpriteSheet == null)
                {
                    _mediaSpriteSheet = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                    _mediaSpriteSheet.name = "Qubit_SpriteSheet";

                    var textureList = new List<Texture2D>();
                    var spriteDataList = new List<(string name, int index)>();

                    void AddSprite(string name, Texture2D tex)
                    {
                        spriteDataList.Add((name, textureList.Count));
                        textureList.Add(tex);
                    }

                    AddSprite("Pause", LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Mods/Important/pause.png", $"Images/Mods/Important/pause.png"));

                    int maxSize = 512;
                    Texture2D spriteSheet = new Texture2D(maxSize, maxSize);
                    Rect[] rects = spriteSheet.PackTextures(textureList.ToArray(), 2, maxSize);

                    _mediaSpriteSheet.spriteSheet = spriteSheet;
                    _mediaSpriteSheet.material = new Material(Shader.Find("TextMeshPro/Sprite"))
                    {
                        mainTexture = spriteSheet
                    };

                    _mediaSpriteSheet.spriteInfoList = new List<TMP_Sprite>();
                    Traverse.Create(_mediaSpriteSheet).Field("m_Version").SetValue("1.1.0"); 

                    _mediaSpriteSheet.spriteGlyphTable.Clear();
                    for (int i = 0; i < spriteDataList.Count; i++)
                    {
                        var rect = rects[i];

                        var glyph = new TMP_SpriteGlyph
                        {
                            index = (uint)i,
                            metrics = new GlyphMetrics(
                                width: rect.width * spriteSheet.width,
                                height: rect.height * spriteSheet.height,
                                bearingX: -(rect.width * spriteSheet.width) / 2f,
                                bearingY: rect.height * spriteSheet.height * 0.8f,
                                advance: rect.width * spriteSheet.width
                            ),
                            glyphRect = new GlyphRect(
                                x: (int)(rect.x * spriteSheet.width),
                                y: (int)(rect.y * spriteSheet.height),
                                width: (int)(rect.width * spriteSheet.width),
                                height: (int)(rect.height * spriteSheet.height)
                            ),
                            scale = 1f,
                            atlasIndex = 0
                        };
                        _mediaSpriteSheet.spriteGlyphTable.Add(glyph);
                    }

                    _mediaSpriteSheet.spriteCharacterTable.Clear();
                    for (int i = 0; i < spriteDataList.Count; i++)
                    {
                        var (name, _) = spriteDataList[i];

                        var character = new TMP_SpriteCharacter(0xFFFE, _mediaSpriteSheet.spriteGlyphTable[i])
                        {
                            name = name,
                            scale = 1f,
                            glyphIndex = (uint)i
                        };
                        _mediaSpriteSheet.spriteCharacterTable.Add(character);
                    }

                    _mediaSpriteSheet.UpdateLookupTables();
                }
                return _mediaSpriteSheet;
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static bool wasenabled = true;

        public static float zoomFOV = 35f;

        private static bool reportMenuToggle;

        private static bool acceptedTOS;

        public static GameObject physicalQuitBox;

        private static float lastTime;

        private static Vector3? oldLocalPosition;

        public static bool lastTagLag;

        public static bool lastSteam;
        #endregion

        #region Methods
        public async static void CheckNewAcc()
        {
            await Task.Delay(10000);

            if (PhotonNetwork.LocalPlayer.UserId != oldId)
                playTime = 0f;
        }

        public static IEnumerator QueueRoomCoroutine(string roomName)
        {
            NetworkSystemPUN instance = (NetworkSystemPUN)ManagerRegistry.NetworkSystem;

            instance.ReturnToSinglePlayer();
            yield return new WaitUntil(() => instance.netState == NetSystemState.Idle);
            yield return new WaitForSeconds(0.5f);

            

            while (!instance.InRoom)
            {
                ManagerRegistry.PhotonNetworkController.AttemptToJoinSpecificRoom(roomName, JoinType.Solo);
                yield return new WaitForSeconds(reconnectDelay);
            }
        }

        public static IEnumerator JoinRandomDelay()
        {
            yield return new WaitForSeconds(1.5f);
            Room.JoinRandom();
        }

        public static async Task ForceCreateRoom(string name, RoomConfig options)
        {
            if (Networking.InRoom)
                await ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();

            await (ManagerRegistry.NetworkSystem as NetworkSystemPUN).TryCreateRoom(name, options);
        }

        public static void BroadcastRoom(string roomName, bool create, string key, string shuffler)
        {
            string text = NetworkSystem.ShuffleRoomName(roomName, shuffler.Substring(2, 8), true) + "|" + NetworkSystem.ShuffleRoomName("ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Substring(ManagerRegistry.NetworkSystem.currentRegionIndex, 1), shuffler[..2], true);

            BroadcastMyRoomRequest broadcastMyRoomRequest = new BroadcastMyRoomRequest
            {
                KeyToFollow = key,
                RoomToJoin = text,
                Set = create
            };

            ManagerRegistry.GorillaServer.BroadcastMyRoom(broadcastMyRoomRequest, delegate { }, delegate { });
        }

        
        public static void RestartGame()
        {
            Preferences.Save();
            string logoLines = PluginInfo.Logo.Split(@"
")
                .Aggregate("", (current, line) => current + (Environment.NewLine + "echo      " + line));

            string restartScript = @"@echo off
title Qubit - Restarting Gorilla Tag
color 5

cls
echo." + logoLines + @"
echo.

echo Your game is restarting, please wait...
echo.

:WAIT_LOOP
tasklist /FI ""IMAGENAME eq Gorilla Tag.exe"" | find /I ""Gorilla Tag.exe"" >nul
if %ERRORLEVEL%==0 (
    timeout /t 1 >nul
    goto WAIT_LOOP
)

start steam://run/1533390
exit";

            string fileName = $"{PluginInfo.BaseDirectory}/RestartScript.bat";

            File.WriteAllText(fileName, restartScript);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
            Application.Quit();
        }

        public static void OpenGorillaTagFolder()
        {
            string filePath = Assembly.GetExecutingAssembly().Location.Split("BepInEx\\")[0];
            Files.Start(filePath);
        }
        public static void DiscordRPC()
        {
            if (discord == null)
            {
                discord = new DiscordRpcClient("1555010806343667764")
                {
                    Logger = new Libraries.DiscordRPC.Logging.DiscordLogManager()
                };

                discord.Initialize();
            }

            if (Networking.InRoom)
            {
                endTime = null;

                if (startTime == null)
                    startTime = DateTime.UtcNow;
            }
            else
            {
                startTime = null;

                if (endTime == null)
                    endTime = DateTime.UtcNow;
            }

            if (Time.time > updateTime)
            {
                updateTime = Time.time + 1f;
                bool inRoom = Networking.InRoom;
                string roomName = inRoom ? ManagerRegistry.NetworkSystem.RoomName : "-";

                discord.SetPresence(new RichPresence
                {
                    Details = inRoom ? $"Playing {ManagerRegistry.GorillaGameManager.GameType().ToString().ToLower()}" : "Playing alone",
                    State = inRoom ? $"Room: {roomName} ({PhotonNetwork.PlayerList.Length}/{PhotonNetwork.CurrentRoom.MaxPlayers})" : "Not in a room",
                    Assets = new Libraries.DiscordRPC.Entities.Assets
                    {
                        LargeImageKey = "qubit",
                        LargeImageText = "Qubit",
                        SmallImageKey = inRoom ? "online" : "offline",
                        SmallImageText = inRoom ? "Online" : "Offline"
                    },
                    Timestamps = inRoom ? new Timestamps
                    {
                        Start = startTime ?? endTime ?? DateTime.UtcNow
                    } : null,
                    Buttons = new[]
                    {
                        new Button
                        {
                            Label = "Discord Server",
                            Url = PluginInfo.DiscordInvite
                        },
                        new Button
                        {
                            Label = "Download",
                            Url = PluginInfo.GitMaster + "/"
                        }
                    }
                });
            }
        }

        public static void DisableDiscordRPC()
        {
            if (discord != null)
            {
                discord.ClearPresence();
                discord.Dispose();
                discord = null;
            }
        }
        public static void EnsureIntegrationProgram()
        {
            kuikkusonguExists = File.Exists($"{PluginInfo.BaseDirectory}/Kuikkusongu.exe");
            if (!kuikkusonguExists)
            {
                Prompt("This mod requires the \"Kuikkusongu\" library. Would you like to automatically download it? (16.3mb)", () =>
                {
                    using UnityWebRequest request = UnityWebRequest.Get(PluginInfo.GitMaster.Substring(0, PluginInfo.GitMaster.LastIndexOf('/')) + "/Kuikkusongu/releases/latest/download/Kuikkusongu.exe");
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();

                    while (!operation.isDone) { }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes($"{PluginInfo.BaseDirectory}/Kuikkusongu.exe", request.downloadHandler.data);
                        Notifications.SendFormat("green", "SUCCESS", $"Successfully downloaded Kuikkusongu to {PluginInfo.BaseDirectory}/Kuikkusongu.exe.");
                    }
                    else
                        Notifications.SendFormat("red", "ERROR", $"Could not download Kuikkusongu: {(request.error.IsNullOrEmpty() ? "Unknown error" : request.error)}");

                    kuikkusonguExists = File.Exists($"{PluginInfo.BaseDirectory}/Kuikkusongu.exe");
                }, () => Toggle("Media Integration"));
            }
        }

        public static async Task UpdateDataAsync()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = $"{Files.GetGamePath()}/{PluginInfo.BaseDirectory}/Kuikkusongu.exe",
                Arguments = "-subete",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using Process proc = new Process { StartInfo = psi };
            proc.Start();
            string output = await proc.StandardOutput.ReadToEndAsync();

            await Task.Run(() => proc.WaitForExit());

            Paused = true;
            Title = "Unknown";
            Artist = "Unknown";

            StartTime = 0f;
            EndTime = 0f;
            ElapsedTime = 0f;

            try
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
                Title = (string)data["taitoru"];
                Artist = (string)data["aatisuto"];

                StartTime = Convert.ToSingle(data["kaishiJikan"]);
                EndTime = Convert.ToSingle(data["shuuryouJikan"]);
                ElapsedTime = Convert.ToSingle(data["keikaJikan"]);

                Paused = (string)data["joutai"] != "Playing";
                Icon.LoadImage(Convert.FromBase64String((string)data["samuneiruBase64"]));

                ValidData = true;
            }
            catch { }
        }

        private static IEnumerator UpdateDataCoroutine(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);

            _ = UpdateDataAsync();
            yield return null;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);
        internal static void SendKey(VirtualKeyCodes virtualKeyCode) => keybd_event((uint)virtualKeyCode, 0, 0, 0);

        public static void PreviousTrack()
        {
            Coroutines.instance.StartCoroutine(UpdateDataCoroutine(0.1f));
            ElapsedTime = 0f;
            SendKey(VirtualKeyCodes.PREVIOUS_TRACK);
        }

        public static void PauseTrack()
        {
            Paused = !Paused;
            SendKey(VirtualKeyCodes.PLAY_PAUSE);
        }

        public static void SkipTrack()
        {
            Coroutines.instance.StartCoroutine(UpdateDataCoroutine(0.1f));
            ElapsedTime = 0f;
            SendKey(VirtualKeyCodes.NEXT_TRACK);
        }

        public static void MediaIntegration()
        {
            if (kuikkusonguExists)
            {
                if (mediaIcon == null)
                {
                    mediaIcon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(mediaIcon.GetComponent<Collider>());

                    if (mediaIconMaterial == null)
                        mediaIconMaterial = new Material(LoadAsset<Shader>("Chams"));

                    mediaIcon.GetComponent<Renderer>().material = mediaIconMaterial;
                }

                mediaIcon.transform.localScale = new Vector3(0.25f, 0.25f, 0.01f) * References.LocalRig.scaleFactor;
                mediaIcon.transform.position = References.HeadCollider.transform.TransformPoint(new Vector3(-0.5f, 0.2f, 1f));
                mediaIcon.transform.LookAt(References.HeadCollider.transform.position);

                if (mediaText == null)
                {
                    GameObject textHolder = new GameObject("Qubit_MediaText");

                    TextMeshPro text = textHolder.GetOrAddComponent<TextMeshPro>();
                    text.color = Color.white;
                    text.fontSize = 0.75f;
                    text.fontStyle = activeFontStyle;
                    text.font = activeFont;
                    text.alignment = TextAlignmentOptions.Left;
                    text.spriteAsset = MediaSpriteSheet;
                    text.margin = new Vector4(0.5f, 0, 0, 0);

                    if (text != null && text.fontMaterial != null)
                        text.fontMaterial.shader = Extensions.TextMeshPro.TmpShader;

                    mediaText = text;
                }

                mediaText.transform.localScale = Vector3.one * References.LocalRig.scaleFactor;
                mediaText.transform.position = References.HeadCollider.transform.TransformPoint(new Vector3(-0.35f, 0.2f, 1f));
                mediaText.transform.LookAt(Camera.main.transform.position);
                mediaText.transform.Rotate(0f, 180f, 0f);
                mediaText.transform.position += mediaText.transform.right * mediaText.bounds.size.x;

                FollowMenuSettings(mediaText);

                float clampedElapsed = Mathf.Clamp(ElapsedTime, StartTime, EndTime);
                mediaText.text =
                    $@"{Artist} - {Title}
{(Paused ? "  <sprite name=\"Pause\"> " : "")}{Mathf.Floor(clampedElapsed / 60)}:{Mathf.Floor(clampedElapsed % 60):00} - {Mathf.Floor(EndTime / 60)}:{Mathf.Floor(EndTime % 60):00}";

                if (Time.time > updateDataDelay)
                {
                    updateDataDelay = Time.time + 5f;
                    Coroutines.instance.StartCoroutine(UpdateDataCoroutine());
                }

                if (!Paused)
                    ElapsedTime += Time.deltaTime;

                if (Time.time > inputDelay)
                {
                    if (Mathf.Abs(leftJoystick.x) > 0.5f)
                    {
                        inputDelay = Time.time + 0.5f;
                        Sounds.Play(Sounds.DefaultSounds["Button"]);

                        if (leftJoystick.x > 0f)
                            SkipTrack();
                        else
                            PreviousTrack();
                    }

                    if (leftJoystickClick)
                    {
                        inputDelay = Time.time + 0.5f;
                        Sounds.Play(Sounds.DefaultSounds["Button"]);

                        PauseTrack();
                    }
                }

                Texture2D targetIcon = Icon == null || !ValidData ? null : Icon;
                Renderer icon = mediaIcon.GetComponent<Renderer>();

                if (icon.material.GetTexture("_MainTex") != targetIcon)
                    icon.material.SetTexture("_MainTex", targetIcon);
            }
        }

        public static void DisableMediaIntegration()
        {
            kuikkusonguExists = false;

            if (mediaIcon != null)
                Object.Destroy(mediaIcon);

            if (mediaText != null)
                Object.Destroy(mediaText.gameObject);

            mediaIcon = null;
            mediaText = null;
        }

        public static void EnableFPC()
        {
            if (TPC != null)
                wasenabled = TPC.gameObject.transform.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>().enabled;
        }
        public static void MoveFPC()
        {
            if (TPC != null)
            {
                if (menu != null && !XRSettings.isDeviceActive)
                    return;

                float FOV = 90f;
                if (UnityEngine.InputSystem.Keyboard.current.cKey.isPressed)
                {
                    Vector2 scroll = Mouse.current.scroll.ReadValue();
                    zoomFOV += -scroll.y * 5f;
                    zoomFOV = Mathf.Clamp(zoomFOV, 10f, 90f);
                    TPC.fieldOfView = Mathf.Lerp(TPC.fieldOfView, zoomFOV, 0.1f);
                }
                else
                {
                    zoomFOV = 35f;
                    TPC.fieldOfView = Mathf.Lerp(TPC.fieldOfView, FOV, 0.1f);
                }
                TPC.gameObject.transform.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>().enabled = false;
                TPC.gameObject.transform.position = UnityEngine.InputSystem.Keyboard.current.cKey.isPressed ? Vector3.Lerp(TPC.transform.position, References.HeadCollider.transform.position, 0.1f) : References.HeadCollider.transform.position;
                TPC.gameObject.transform.rotation = Quaternion.Lerp(TPC.transform.rotation, References.HeadCollider.transform.rotation, 0.075f);
            }
        }

        public static void DisableFPC()
        {
            if (TPC != null)
            {
                TPC.GetComponent<Camera>().fieldOfView = 60f;
                TPC.gameObject.transform.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>().enabled = wasenabled;
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public static void ForceEnableHands(bool enabled = true)
        {
            if (!XRSettings.isDeviceActive)
                return;

            ManagerRegistry.ConnectedControllerHandler.overrideLeftEnable = enabled;
            ManagerRegistry.ConnectedControllerHandler.overrideRightEnable = enabled;
            ManagerRegistry.ConnectedControllerHandler.UpdateControllerStates();
        }
        public static void OculusReportMenu()
        {
            if (leftPrimary && !reportMenuToggle)
            {
                GorillaMetaReport metaReporting = GetObject("Miscellaneous Scripts").transform.Find("MetaReporting").GetComponent<GorillaMetaReport>();
                metaReporting.gameObject.SetActive(true);
                metaReporting.enabled = true;

                metaReporting.StartOverlay();
            }
            reportMenuToggle = leftPrimary;
        }
        public static void AcceptTOS()
        {
            try
            {
                GameObject RoomObject = GetObject("Miscellaneous Scripts/PrivateUIRoom_HandRays");
                if (RoomObject == null)
                    return;

                HandRayController HandRayController = RoomObject.GetComponent<HandRayController>();
                PrivateUIRoom PrivateUIRoom = RoomObject.GetComponent<PrivateUIRoom>();

                if (!acceptedTOS && PrivateUIRoom.inOverlay)
                {
                    HandRayController.DisableHandRays();

                    PrivateUIRoom.StopOverlay();
                    PrivateUIRoom.overlayForcedSources = 0;
                    if (!TermsOfService.enabled)
                    {
                        References.Tagger.tapHapticStrength = 0.5f;
                        GorillaSnapTurn.LoadSettingsFromCache();
                        TermsOfService.enabled = true;
                    }

                    acceptedTOS = true;
                }

                if (RoomObject.activeSelf)
                    RoomObject.SetActive(false);
            }
            catch { }
        }

        public static IEnumerator RedeemShinyRocks()
        {
            Task<GetPlayerData_Data> newSessionDataTask = KIDManager.TryGetPlayerData(true);

            while (!newSessionDataTask.IsCompleted)
                yield return null;
            if (newSessionDataTask.IsFaulted)
                Notifications.SendFormat("red", "ERROR", "Failed to redeem shiny rocks.");

            GetPlayerData_Data newSessionData = newSessionDataTask.Result;
            if (newSessionData.responseType == GetSessionResponseType.NOT_FOUND)
            {
                Task optInTask = KIDManager.Server_OptIn();

                while (!optInTask.IsCompleted)
                    yield return null;
                if (optInTask.IsFaulted)
                    Notifications.SendFormat("red", "ERROR", "Failed to redeem shiny rocks.");

                Notifications.SendFormat("green", "SUCCESS", "Successfully redeemed shiny rocks!");
                ManagerRegistry.CosmeticsController.GetCurrencyBalance();
            }
            else
                Notifications.SendFormat("red", "ERROR", "You have already redeemed the shiny rocks.");
            yield break;
        }

        public static void JoinDiscord() =>
            Files.OpenURL(PluginInfo.DiscordInvite);

        public static void CopyPlayerPosition()
        {
            string text = "Body\n";
            Transform p = References.BodyCollider.transform;
            text += $"new Vector3({p.position.x}f, {p.position.y}f, {p.position.z}f);";
            text += $"new Quaternion({p.rotation.x}f, {p.rotation.y}f, {p.rotation.z}f, {p.rotation.w}f);\n\n";

            text += "Head\n";
            p = References.HeadCollider.transform;
            text += $"new Vector3({p.position.x}f, {p.position.y}f, {p.position.z}f);";
            text += $"new Quaternion({p.rotation.x}f, {p.rotation.y}f, {p.rotation.z}f, {p.rotation.w}f);\n\n";

            text += "Left Hand\n";
            p = References.LocalRig.leftHand.rigTarget.transform;
            text += $"new Vector3({p.position.x}f, {p.position.y}f, {p.position.z}f);";
            text += $"new Quaternion({p.rotation.x}f, {p.rotation.y}f, {p.rotation.z}f, {p.rotation.w}f);\n\n";

            text += "Right Hand\n";
            p = References.LocalRig.rightHand.rigTarget.transform;
            text += $"new Vector3({p.position.x}f, {p.position.y}f, {p.position.z}f);";
            text += $"new Quaternion({p.rotation.x}f, {p.rotation.y}f, {p.rotation.z}f, {p.rotation.w}f);";

            GUIUtility.systemCopyBuffer = text;
        }
        public static void PhysicalQuitbox()
        {
            GameObject quitBox = GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox");
            physicalQuitBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            physicalQuitBox.transform.position = quitBox.transform.position;
            physicalQuitBox.transform.rotation = quitBox.transform.rotation;
            physicalQuitBox.transform.localScale = quitBox.transform.localScale;
            physicalQuitBox.GetComponent<Renderer>().material = CustomBoards.BoardMaterial;

            quitBox.SetActive(false);
        }

        public static void DisablePhysicalQuitbox()
        {
            Object.Destroy(physicalQuitBox);
            GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox").SetActive(true);
        }

        public static void BlockOnMute()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal() && rig.rigContainer.IsMuted)
                    Settings.BlockPlayer(rig);
                else
                    if (Settings.Blocked.Contains(rig))
                    Settings.UnblockPlayer(rig);
            }
        }
        public static void CapFPS(int fps)
        {
            float targetDelta = 1f / fps;
            float elapsed = Time.realtimeSinceStartup - lastTime;

            if (elapsed < targetDelta)
            {
                int sleepMs = Mathf.FloorToInt((targetDelta - elapsed) * 1000);
                if (sleepMs > 0)
                    Thread.Sleep(sleepMs);
            }

            lastTime = Time.realtimeSinceStartup;
        }

        public static void UncapFPS()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = int.MaxValue;
        }
        public static void PCButtonClick()
        {
            if (Mouse.current.leftButton.isPressed && GunPointer == null)
            {
                Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                Physics.Raycast(ray, out var Ray, 512f, NoInvisLayerMask());

                oldLocalPosition ??= References.Tagger.rightHandTriggerCollider.transform.localPosition;
                References.Tagger.rightHandTriggerCollider.GetComponent<TransformFollow>().enabled = false;
                References.Tagger.rightHandTriggerCollider.transform.position = Ray.point;
            }
            else
            {
                if (oldLocalPosition != null)
                {
                    References.Tagger.rightHandTriggerCollider.transform.localPosition = oldLocalPosition.Value;
                    oldLocalPosition = null;
                }
                References.Tagger.rightHandTriggerCollider.GetComponent<TransformFollow>().enabled = true;
            }
        }

        public static void DisablePCButtonClick()
        {
            if (oldLocalPosition != null)
            {
                References.Tagger.rightHandTriggerCollider.transform.localPosition = oldLocalPosition.Value;
                oldLocalPosition = null;
            }
        }

        public static void PCControllerEmulation()
        {
            References.InputPoller.rightControllerPrimaryButton |= UnityInput.GetKey(Key.E);
            References.InputPoller.rightControllerSecondaryButton |= UnityInput.GetKey(Key.R);

            References.InputPoller.leftControllerPrimaryButton |= UnityInput.GetKey(Key.F);
            References.InputPoller.leftControllerSecondaryButton |= UnityInput.GetKey(Key.G);

            References.InputPoller.leftGrab |= UnityInput.GetKey(Key.LeftBracket);
            References.InputPoller.leftControllerGripFloat += UnityInput.GetKey(Key.LeftBracket) ? 1f : 0f;

            References.InputPoller.rightGrab |= UnityInput.GetKey(Key.RightBracket);
            References.InputPoller.rightControllerGripFloat += UnityInput.GetKey(Key.RightBracket) ? 1f : 0f;

            References.InputPoller.rightControllerTriggerButton |= UnityInput.GetKey(Key.Equals);
            References.InputPoller.rightControllerIndexFloat += UnityInput.GetKey(Key.Equals) ? 1f : 0f;

            References.InputPoller.leftControllerTriggerButton |= UnityInput.GetKey(Key.Minus);
            References.InputPoller.leftControllerIndexFloat += UnityInput.GetKey(Key.Minus) ? 1f : 0f;

            References.InputPoller.rightControllerTriggerButton |= UnityInput.GetKey(Key.Equals);
            References.InputPoller.rightControllerIndexFloat += UnityInput.GetKey(Key.Equals) ? 1f : 0f;
        }

        
        public static void DisableAprilFoolsFX()
        {
            try
            {
                AprilFoolsGravityFX[] gravityFxManagerThingies = Object.FindObjectsByType<AprilFoolsGravityFX>(FindObjectsSortMode.None);

                foreach (AprilFoolsGravityFX gravityFxManager in gravityFxManagerThingies)
                {
                    gravityFxManager.BackToNormal();
                    gravityFxManager.Destroy();
                }

                GravityFXToggle.enabled = true;
            }
            catch { }
        }

        public static void EnableAprilFoolsFX()
        {
            try
            {
                GravityFXToggle.enabled = false;
                new GameObject().AddComponent<AprilFoolsGravityFX>().Start();
            }
            catch { }
        }

        public static void ConnectToRegion(string region)
        {
            string currentRegion = PhotonNetwork.CloudRegion;
            if (!string.IsNullOrEmpty(currentRegion))
                currentRegion = currentRegion.Replace("/*", "");

            if (currentRegion != region)
                PhotonNetwork.ConnectToRegion(region);

            ManagerRegistry.NetworkSystem.currentRegionIndex = Array.IndexOf(ManagerRegistry.NetworkSystem.regionNames, region);

            NetworkSystemPUN punNetwork = (NetworkSystemPUN)ManagerRegistry.NetworkSystem;
            for (int i = 0; i < punNetwork.regionData.Length; i++)
            {
                NetworkRegionInfo regionInfo = punNetwork.regionData[i];
                regionInfo.pingToRegion = Array.IndexOf(ManagerRegistry.NetworkSystem.regionNames, regionInfo) == i ? 0 : 9999;
            }
        }
        #endregion
    }
}
