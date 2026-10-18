/*
 * Qubit Menu  Frontend/UI.cs
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
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Classes;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using ButtonCollider = Qubit.Frontend.Classes.ButtonCollider;
using JoinType = GorillaNetworking.JoinType;

namespace Qubit.Frontend
{
    [AddOnStart]
    public class UI : MonoBehaviour
    {
        public static UI Instance;
        public static Texture2D watermarkImage;

        private void Awake()
        {
            Instance = this;

            if (File.Exists(hideGUIPath))
                isOpen = false;

            uiPrefab = LoadObject<GameObject>("UI");
            if (uiPrefab == null)
                return;

            Transform canvas = uiPrefab.transform.Find("Canvas");
            if (canvas == null)
                return;

            watermark = canvas.Find("Watermark")?.GetComponent<Image>();
            versionLabel = canvas.Find("VersionLabel")?.GetComponent<TextMeshProUGUI>();
            roomStatus = canvas.Find("RoomStatus")?.GetComponent<TextMeshProUGUI>();
            arraylist = canvas.Find("Arraylist")?.GetComponent<TextMeshProUGUI>();
            controlBackground = canvas.Find("ControlUI")?.GetComponent<Image>();

            debugUI = canvas.Find("DebugUI")?.gameObject;
            if (debugUI != null)
                debugUI.AddComponent<UIDragWindow>();

            templateLine = debugUI?.transform.Find("Lines/Line")?.gameObject;

            r = canvas.Find("ControlUI/R")?.GetComponent<TMP_InputField>();
            g = canvas.Find("ControlUI/G")?.GetComponent<TMP_InputField>();
            b = canvas.Find("ControlUI/B")?.GetComponent<TMP_InputField>();
            textInput = canvas.Find("ControlUI/TextInput")?.GetComponent<TMP_InputField>();

            Button queueButton = canvas.Find("ControlUI/QueueButton")?.GetComponent<Button>();
            queueButton?.onClick.AddListener(() => Modules.Categories.Room.QueueRoom(textInput?.text ?? ""));

            Button joinButton = canvas.Find("ControlUI/JoinButton")?.GetComponent<Button>();
            joinButton?.onClick.AddListener(() => ManagerRegistry.PhotonNetworkController?.AttemptToJoinSpecificRoom(textInput?.text ?? "", JoinType.Solo));

            Button colorButton = canvas.Find("ControlUI/ColorButton")?.GetComponent<Button>();
            colorButton?.onClick.AddListener(() =>
                {
                    if (byte.TryParse(r?.text, out byte red) &&
                        byte.TryParse(g?.text, out byte green) &&
                        byte.TryParse(b?.text, out byte blue))
                        ChangeColor(new Color32(red, green, blue, 255));
                });

            Button nameButton = canvas.Find("ControlUI/NameButton")?.GetComponent<Button>();
            nameButton?.onClick.AddListener(() => ChangeName(textInput?.text ?? ""));

            TMP_InputField inputField = debugUI?.transform.Find("TextInput")?.gameObject.GetComponent<TMP_InputField>();

            if (inputField != null)
            {
                inputField.onSelect.AddListener(_ => focusedOnDebug = true);
                inputField.onDeselect.AddListener(_ => focusedOnDebug = false);

                inputField.onEndEdit.AddListener((string text) =>
                {
                    if (focusedOnDebug && !inputField.text.IsNullOrEmpty())
                        HandleDebugCommand(text);

                    inputField.text = string.Empty;
                });
            }

            textObjects = new List<TextMeshProUGUI>
            {
                canvas.Find("ControlUI/TextInput/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/R/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/G/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/B/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/QueueButton/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/JoinButton/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/ColorButton/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("ControlUI/NameButton/Text")?.GetComponent<TextMeshProUGUI>(),
                canvas.Find("HideMessage")?.GetComponent<TextMeshProUGUI>()
            }.Where(textObject => textObject != null).ToList();

            imageObjects = new List<Image>
            {
                canvas.Find("ControlUI/TextInput")?.GetComponent<Image>(),
                canvas.Find("ControlUI/R")?.GetComponent<Image>(),
                canvas.Find("ControlUI/G")?.GetComponent<Image>(),
                canvas.Find("ControlUI/B")?.GetComponent<Image>(),
                canvas.Find("ControlUI/QueueButton")?.GetComponent<Image>(),
                canvas.Find("ControlUI/JoinButton")?.GetComponent<Image>(),
                canvas.Find("ControlUI/ColorButton")?.GetComponent<Image>(),
                canvas.Find("ControlUI/NameButton")?.GetComponent<Image>(),
                debugUI?.transform.Find("TextInput")?.GetComponent<Image>(),
                debugUI?.transform.Find("Lines")?.GetComponent<Image>()
            }.Where(imageObject => imageObject != null).ToList();

            if (watermark != null)
                watermark.material = new Material(watermark.material);

            watermarkImage = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.icon.png");

            if (!Bootstrapper.FirstLaunch)
            {
                GameObject closeMessage = uiPrefab.transform.Find("Canvas")?.Find("HideMessage")?.gameObject;
                closeMessage?.SetActive(false);
            }

            if (versionLabel != null)
            {
                versionLabelDefaultAnchorMin = versionLabel.rectTransform.anchorMin;
                versionLabelDefaultAnchorMax = versionLabel.rectTransform.anchorMax;
                versionLabelDefaultPivot = versionLabel.rectTransform.pivot;
                versionLabelDefaultPosition = versionLabel.rectTransform.anchoredPosition;
            }

            desktopOverlay = QubitDesktopOverlay.Attach(uiPrefab.transform);
            if (desktopOverlay != null)
            {
                foreach (Transform child in canvas)
                    if (child.gameObject != debugUI && child.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) == null)
                        child.gameObject.SetActive(false);
            }
            Update();
        }

        private bool isOpen = true;
        private bool focusedOnDebug;

        private GameObject uiPrefab;
        private GameObject debugUI;
        private QubitDesktopOverlay desktopOverlay;

        private Image watermark;
        private TextMeshProUGUI versionLabel;
        private Vector2 versionLabelDefaultAnchorMin,
                        versionLabelDefaultAnchorMax,
                        versionLabelDefaultPivot,
                        versionLabelDefaultPosition;
        private TextMeshProUGUI roomStatus;
        private TextMeshProUGUI arraylist;

        private TMP_InputField r;
        private TMP_InputField g;
        private TMP_InputField b;
        private TMP_InputField textInput;

        private Image controlBackground;
        private List<TextMeshProUGUI> textObjects = new List<TextMeshProUGUI>();
        private List<Image> imageObjects = new List<Image>();

        private float uiUpdateDelay;

        private bool lastInRoom;
        private string lastRoomName;

        private void Update()
        {
            if (uiPrefab == null)
                return;

            if (UnityEngine.InputSystem.Keyboard.current?.backslashKey.wasPressedThisFrame == true)
                ToggleGUI();

            if (isOpen)
            {
                uiPrefab.SetActive(true);

                if (UnityEngine.InputSystem.Keyboard.current?.backquoteKey.wasPressedThisFrame == true)
                    ToggleDebug();

                if (desktopOverlay != null)
                {
                    desktopOverlay.Refresh();
                    return;
                }

                Color guiColor = Manager.GetIndex("Swap GUI Colors").enabled
                    ? textColors[1].GetCurrentColor()
                    : backgroundColor.GetCurrentColor();

                if (versionLabel != null)
                    versionLabel.color = guiColor;

                if (roomStatus != null)
                    roomStatus.color = guiColor;

                if (arraylist != null)
                    arraylist.color = guiColor;

                if (watermark != null)
                {
                    watermark.color = guiColor;
                    watermark.gameObject.SetActive(!disableWatermark);
                }

                versionLabel.SafeSetFont(activeFont);
                roomStatus.SafeSetFont(activeFont);
                arraylist.SafeSetFont(activeFont);

                versionLabel.SafeSetFontStyle(activeFontStyle);
                roomStatus.SafeSetFontStyle(activeFontStyle);
                arraylist.SafeSetFontStyle(activeFontStyle);

                if (controlBackground != null)
                    controlBackground.color = menuBackgroundColor.GetCurrentColor();

                foreach (var textObject in textObjects)
                {
                    if (textObject == null)
                        continue;

                    textObject.color = textColors[1].GetCurrentColor();
                    textObject.SafeSetFont(activeFont);
                    textObject.SafeSetFontStyle(activeFontStyle);
                }

                foreach (var imageObject in imageObjects)
                {
                    if (imageObject != null)
                        imageObject.color = buttonColors[0].GetCurrentColor();
                }

                if (watermark != null)
                    watermark.transform.rotation = Quaternion.Euler(0f, 0f, rockWatermark ? Mathf.Sin(Time.time * 2f) * 10f : 0f);

                if (!versionLabel.text.Contains(PluginInfo.Version) || !versionLabel.text.Contains(PluginInfo.DiscordInvite))
                    versionLabel.SafeSetText(FollowMenuSettings("Build") + " " + PluginInfo.Version + "\n" +
                                        PluginInfo.DiscordInvite.Replace("https://", ""));

                if (versionLabel != null)
                {
                    if (disableWatermark)
                    {
                        versionLabel.rectTransform.anchorMin = new Vector2(1f, versionLabel.rectTransform.anchorMin.y);
                        versionLabel.rectTransform.anchorMax = new Vector2(1f, versionLabel.rectTransform.anchorMax.y);
                        versionLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
                        versionLabel.rectTransform.anchoredPosition = new Vector2(-10f, versionLabel.rectTransform.anchoredPosition.y);
                    }
                    else
                    {
                        versionLabel.rectTransform.anchorMin = versionLabelDefaultAnchorMin;
                        versionLabel.rectTransform.anchorMax = versionLabelDefaultAnchorMax;
                        versionLabel.rectTransform.pivot = versionLabelDefaultPivot;
                        versionLabel.rectTransform.anchoredPosition = versionLabelDefaultPosition;
                    }
                }

                bool inRoom = Networking.InRoom;
                string roomName = inRoom ? PhotonNetwork.CurrentRoom.Name : "";

                if (inRoom != lastInRoom || roomName != lastRoomName)
                {
                    lastInRoom = inRoom;
                    lastRoomName = roomName;
                    roomStatus.SafeSetText(FollowMenuSettings(!inRoom ? "Not connected to room" : "Connected to room ") + roomName);
                }

                if (debugUI != null && debugUI.activeSelf)
                {
                    Image debugImage = debugUI.GetComponent<Image>();
                    if (debugImage != null)
                        debugImage.color = backgroundColor.GetCurrentColor();

                    List<TextMeshProUGUI> debugTextObjects = new List<TextMeshProUGUI>
                    {
                        debugUI.transform.Find("Title")?.GetComponent<TextMeshProUGUI>(),
                        debugUI.transform.Find("TextInput/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                        debugUI.transform.Find("TextInput/Text Area/Placeholder")?.GetComponent<TextMeshProUGUI>()
                    }.Where(textObject => textObject != null).ToList();

                    Transform lines = debugUI.transform.Find("Lines");
                    if (lines != null)
                        debugTextObjects.AddRange(lines.GetComponentsInChildren<TextMeshProUGUI>());

                    foreach (var textObject in debugTextObjects)
                    {
                        textObject.color = textColors[1].GetCurrentColor();
                        textObject.SafeSetFont(activeFont);
                        textObject.SafeSetFontStyle(activeFontStyle);
                    }

                    TextMeshProUGUI title = debugUI.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    if (title != null)
                        title.color = textColors[0].GetCurrentColor();
                }

                if (!(Time.time > uiUpdateDelay)) return;

                Texture2D watermarkTexture = customWatermark ?? watermarkImage;

                if (watermark != null && watermarkTexture != null && (watermark.sprite == null || watermark.sprite.texture == null || watermark.sprite.texture != watermarkTexture))
                {
                    Sprite sprite = Sprite.Create(
                        watermarkTexture,
                        new Rect(0, 0, watermarkTexture.width, watermarkTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                    watermark.sprite = sprite;
                }

                if (flipArraylist)
                {
                    if (controlBackground != null)
                    {
                        controlBackground.rectTransform.anchoredPosition = new Vector2(10f, -10f);
                        controlBackground.rectTransform.anchorMin = new Vector2(0f, 1f);
                        controlBackground.rectTransform.anchorMax = new Vector2(0f, 1f);
                    }

                    if (arraylist != null)
                    {
                        arraylist.rectTransform.anchoredPosition = new Vector2(-837.5001f, -523f);
                        arraylist.rectTransform.anchorMin = new Vector2(1f, 1f);
                        arraylist.rectTransform.anchorMax = new Vector2(1f, 1f);

                        arraylist.alignment = TextAlignmentOptions.TopRight;
                    }
                }
                else
                {
                    if (controlBackground != null)
                    {
                        controlBackground.rectTransform.anchoredPosition = new Vector2(-250f, -10f);
                        controlBackground.rectTransform.anchorMin = new Vector2(1f, 1f);
                        controlBackground.rectTransform.anchorMax = new Vector2(1f, 1f);
                    }

                    if (arraylist != null)
                    {
                        arraylist.rectTransform.anchoredPosition = new Vector2(837.5001f, -523f);
                        arraylist.rectTransform.anchorMin = new Vector2(0f, 1f);
                        arraylist.rectTransform.anchorMax = new Vector2(0f, 1f);

                        arraylist.alignment = TextAlignmentOptions.TopLeft;
                    }
                }

                uiUpdateDelay = Time.time + (advancedArraylist ? 0.1f : 0.5f);

                List<string> enabledMods = new List<string>();
                foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                {
                    Module[] buttonList = categoryEntry.Value;
                    foreach (Module button in buttonList)
                    {
                        try
                        {
                            if (button == null || !button.enabled || (hideSettings && categoryEntry.Key.Contains("Settings")))
                                continue;

                            if (Manager.modules["Temporary Category"].Contains(button) || button.hideFromArraylist)
                                continue;

                            if (!button.enabled || (hideSettings && (!hideSettings ||
                                                                     categoryEntry.Key
                                                                         .Contains("Settings")))) continue;
                            string buttonText = button.overlapText ?? button.buttonText;

                            if (inputTextColor != "green")
                                buttonText = buttonText.Replace(" <color=grey>[</color><color=green>", " <color=grey>[</color><color=" + inputTextColor + ">");

                            buttonText = FixTMProTags(buttonText);

                            buttonText = FollowMenuSettings(buttonText);
                            enabledMods.Add(buttonText);
                        }
                        catch { }
                    }

                }

                string[] sortedMods = enabledMods
                    .Select(s => (text: s, width: arraylist != null ? arraylist.GetPreferredValues(NoRichtextTags(s)).x : s.Length))
                    .OrderByDescending(t => t.width)
                    .Select(t => t.text)
                    .ToArray();

                string modListText = "";
                for (int i = 0; i < sortedMods.Length; i++)
                {
                    if (advancedArraylist)
                        modListText += (flipArraylist ?
                            /* Flipped */ $"<mark=#{ColorToHex(backgroundColor.GetCurrentColor(i * -0.1f))}C0> {sortedMods[i]} </mark><mark=#{ColorToHex(buttonColors[1].GetCurrentColor(i * -0.1f))}> </mark>" :
                            /* Normal  */ $"<mark=#{ColorToHex(buttonColors[1].GetCurrentColor(i * -0.1f))}> </mark><mark=#{ColorToHex(backgroundColor.GetCurrentColor(i * -0.1f))}C0> {sortedMods[i]} </mark>") + "\n";
                    else
                        modListText += sortedMods[i] + "\n";
                }

                arraylist.SafeSetText(modListText);
            }
            else
                uiPrefab.SetActive(false);
        }

        private readonly string hideGUIPath = $"{PluginInfo.BaseDirectory}/Qubit_HideGUI.txt";
        private void ToggleGUI()
        {
            isOpen = !isOpen;

            try
            {
                if (isOpen)
                {
                    if (File.Exists(hideGUIPath))
                        File.Delete(hideGUIPath);
                }
                else
                {
                    if (!File.Exists(hideGUIPath))
                        File.WriteAllText(hideGUIPath, PluginInfo.GeneratedFileFooter);
                }
            }
            catch { }

            GameObject closeMessage = uiPrefab?.transform.Find("Canvas")?.Find("HideMessage")?.gameObject;
            closeMessage?.SetActive(false);
        }

        private void ToggleDebug()
        {
            if (debugUI == null)
                return;

            if (debugUI.activeSelf)
                debugUI.SetActive(false);
            else
            {
                if (dynamicSounds)
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/console.ogg", "Audio/Menu/console.ogg", clip => clip.Play(buttonClickVolume / 10f));

                debugUI.SetActive(true);
            }
        }

        private GameObject templateLine;
        public void DebugPrint(string text)
        {
            if (debugUI == null || templateLine == null || !debugUI.activeSelf)
                return;

            Transform lines = debugUI.transform.Find("Lines");
            if (lines == null)
                return;

            GameObject line = Instantiate(templateLine, lines, false);
            line.SetActive(true);

            TextMeshProUGUI lineText = line.GetComponent<TextMeshProUGUI>();
            if (lineText != null)
                lineText.text = text;

            if (lines.childCount > 14)
                Destroy(lines.GetChild(1).gameObject);
        }

        public void HandleDebugCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string[] args = command.Split(' ');
            string commandName = args[0].ToLower();

            switch (commandName)
            {
                case "print":
                    {
                        DebugPrint(args.Skip(1).Join(" "));
                        break;
                    }
                case "admin":
                    {
                        string id = args.Length > 1 ? args[1] : PhotonNetwork.LocalPlayer?.UserId;
                        string name = args.Length > 2 ? args[2] : PhotonNetwork.LocalPlayer?.NickName;

                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            ServerData.LocalAdmins.Add(id, name);
                            DebugPrint($"Added ({id}, {name}) to local administrators");
                        }

                        break;
                    }
                case "beta":
                    {
                        PluginInfo.BetaBuild = args.Length > 1 && args[1].ToLower() == "true";
                        DebugPrint($"PluginInfo.BetaBuild is now {PluginInfo.BetaBuild}");
                        break;
                    }
                case "telemetry":
                    {
                        ServerData.DisableTelemetry = args.Length < 2 || args[1] == "false";
                        DebugPrint($"Telemetry is now {(ServerData.DisableTelemetry ? "disabled" : "enabled")}");
                        break;
                    }
                case "prompt":
                    {
                        MatchCollection matches = Regex.Matches(args.Skip(1).Join(" "), @"\[(.*?)\]");
                        List<string> results = matches.Select(matches => matches.Groups).SelectMany(group => group).Select(group => group.Value).ToList();

                        string promptText = results.Count > 1 ? results[1] : args.Length > 1 ? args[1] : "Prompt text";
                        string acceptText = results.Count > 3 ? results[3] : args.Length > 2 ? args[2] : "Accept";
                        string declineText = results.Count > 5 ? results[5] : args.Length > 3 ? args[3] : "Decline";

                        Prompt(promptText, () => DebugPrint("Prompt accepted"), () => DebugPrint("Prompt declined"), acceptText, declineText);
                        DebugPrint($"Prompted user {promptText} {acceptText} {declineText}");

                        break;
                    }
                case "exit":
                case "quit":
                case "close":
                    {
                        Application.Quit();
                        break;
                    }
                default:
                    {
                        DebugPrint($"Unknown command: '{commandName}'");
                        break;
                    }
            }
        }

        private void OnGUI() // Legacy plugin OnGUI compatibility
        {
            if (isOpen)
                Plugins.ExecuteOnGUI();
        }
        public static void PositionMenuOnPC(bool isKeyboardCondition)
        {
            if (isKeyboardCondition)
            {
                GetObject("Shoulder Camera")?.transform.Find("CM vcam1").gameObject.SetActive(false);
                if (TPC != null)
                {
                    isOnPC = true;

                    if (!XRSettings.isDeviceActive)
                        ManagerRegistry.PrivateUIRoom.ToggleLevelVisibility(true);

                    if (joystickMenu)
                        Manager.Toggle("Joystick Menu");

                    if (watchMenu)
                        Manager.Toggle("Watch Menu");

                    if (physicalMenu)
                        Manager.Toggle("Physical Menu");

                    Vector3[] pcPositions = {
                        TPC?.transform.position ?? References.HeadCollider.transform.position,
                        new Vector3(10f, 10f, 10f),
                        new Vector3(10f, 10f, 10f),
                        new Vector3(-67.9299f, 11.9144f, -84.2019f),
                        new Vector3(-63f, 3.634f, -65f),
                        References.LocalRig.transform.position + References.LocalRig.transform.forward * 1.2f
                    };

                    TPC.transform.position = pcPositions[pcBackgroundIndex];
                    if (pcBackgroundIndex != 4 && pcBackgroundIndex != 0)
                        TPC.transform.rotation = Quaternion.identity;

                    if (pcBackgroundIndex == 5)
                    {
                        if (pcBackground == null)
                        {
                            pcBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            pcBackground.transform.localScale = new Vector3(10f, 10f, 0.01f);
                            pcBackground.transform.transform.position = TPC.transform.position + TPC.transform.forward;

                            OnMenuClosed += () => Destroy(pcBackground);
                        }

                        Color realcolor = menuBackgroundColor.GetCurrentColor();
                        pcBackground.GetComponent<Renderer>().material.color = new Color32((byte)(realcolor.r * 50), (byte)(realcolor.g * 50), (byte)(realcolor.b * 50), 255);
                    }

                    menu.transform.parent = TPC.transform;
                    menu.transform.position = TPC.transform.position + TPC.transform.forward * 0.5f;
                    menu.transform.rotation = clickGUI && !XRSettings.isDeviceActive ? Quaternion.identity : TPC.transform.rotation * Quaternion.Euler(-90f, 90f, 0f);

                    if (reference != null)
                    {
                        if (Mouse.current.leftButton.isPressed && !isMouseDown)
                        {
                            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                            bool worked = Physics.Raycast(ray, out RaycastHit hit, 512f, NoInvisLayerMask());
                            if (worked)
                            {
                                ButtonCollider collide = hit.transform.gameObject.GetComponent<ButtonCollider>();
                                if (collide != null)
                                {
                                    collide.OnTriggerEnter(buttonCollider);
                                    buttonCooldown = -1f;
                                }
                            }
                        }
                        else
                            reference.transform.position = new Vector3(999f, -999f, -999f);

                        isMouseDown = Mouse.current.leftButton.isPressed;
                    }
                }
            }
            else
                isOnPC = false;
        }

        public static bool IsKeyboardCondition() =>
            (!QubitDesktopOverlay.IsInputFocused && UnityInput.GetKey(Settings.KeyboardBind)) || (inTextInput && isKeyboardPc);

        public static bool IsKeyboardActive() =>
            UnityInput.GetKey(Settings.KeyboardBind) || isKeyboardPc;

        public static bool IsKeyboardHeld() =>
            UnityInput.GetKey(Settings.KeyboardBind) || (inTextInput && isKeyboardPc);

        public static void AimGunAtMouse(out RaycastHit hit, ref Vector3 direction)
        {
            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
            Physics.Raycast(ray, out hit, 512f, NoInvisLayerMask());
            direction = ray.direction;
        }

        public static bool IsMouseShooting() =>
            Mouse.current.leftButton.isPressed;

        public static bool IsMouseGripping() =>
            Mouse.current.rightButton.isPressed;

        public static Ray GetMouseRay() =>
            TPC.ScreenPointToRay(Mouse.current.position.ReadValue());

        public static bool IsMousePressed() =>
            Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool isMouseDown;
        public static GameObject pcBackground;

    }
}
