/*
 * Qubit Menu  Frontend/LegacyDesktopUI.cs
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

using Photon.Pun;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Classes;
using Qubit.Libraries.Core;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using Qubit.Modules.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Core.Assets;

namespace Qubit.Frontend
{
    public interface IDesktopOverlay
    {
        void Refresh();
    }

    public sealed class LegacyDesktopUI : MonoBehaviour, IDesktopOverlay
    {
        public static Texture2D watermarkImage;

        private GameObject uiPrefab;
        private GameObject debugUI;
        private GameObject templateLine;

        private Image watermark;
        private TextMeshProUGUI versionLabel;
        private TextMeshProUGUI roomStatus;
        private TextMeshProUGUI arraylist;
        private Image controlBackground;

        private TMP_InputField r, g, b, textInput;
        private readonly List<TextMeshProUGUI> textObjects = new List<TextMeshProUGUI>();
        private readonly List<Image> imageObjects = new List<Image>();

        private float uiUpdateDelay;
        private bool lastInRoom;
        private string lastRoomName;
        private bool focusedOnDebug;

        public static LegacyDesktopUI Attach(Transform parent)
        {
            GameObject instance = null;
            try
            {
                instance = LoadObject<GameObject>("UI");
                instance.transform.SetParent(parent, false);
                var overlay = instance.AddComponent<LegacyDesktopUI>();
                overlay.Bind();
                return overlay;
            }
            catch (Exception exception)
            {
                if (instance != null) Destroy(instance);
                Logs.LogError("Qubit legacy desktop interface failed to load: " + exception);
                return null;
            }
        }

        private void Bind()
        {
            uiPrefab = gameObject;
            Transform canvas = uiPrefab.transform.Find("Canvas");
            if (canvas == null) return;

            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true)) graphic.material = null;

            watermark = canvas.Find("Watermark")?.GetComponent<Image>();
            versionLabel = canvas.Find("VersionLabel")?.GetComponent<TextMeshProUGUI>();
            roomStatus = canvas.Find("RoomStatus")?.GetComponent<TextMeshProUGUI>();
            arraylist = canvas.Find("Arraylist")?.GetComponent<TextMeshProUGUI>();
            controlBackground = canvas.Find("ControlUI")?.GetComponent<Image>();

            debugUI = canvas.Find("DebugUI")?.gameObject;
            if (debugUI != null)
            {
                debugUI.AddComponent<UIDragWindow>();
                debugUI.SetActive(false);
            }
            templateLine = debugUI?.transform.Find("Lines/Line")?.gameObject;

            r = canvas.Find("ControlUI/R")?.GetComponent<TMP_InputField>();
            g = canvas.Find("ControlUI/G")?.GetComponent<TMP_InputField>();
            b = canvas.Find("ControlUI/B")?.GetComponent<TMP_InputField>();
            textInput = canvas.Find("ControlUI/TextInput")?.GetComponent<TMP_InputField>();

            if (textInput != null)
                textInput.text = "Qubit";

            Button queueButton = canvas.Find("ControlUI/QueueButton")?.GetComponent<Button>();
            queueButton?.onClick.AddListener(() => Room.QueueRoom(textInput?.text ?? ""));

            Button joinButton = canvas.Find("ControlUI/JoinButton")?.GetComponent<Button>();
            joinButton?.onClick.AddListener(() => Rooms.JoinRoom(textInput?.text ?? ""));

            Button colorButton = canvas.Find("ControlUI/ColorButton")?.GetComponent<Button>();
            colorButton?.onClick.AddListener(() =>
            {
                if (byte.TryParse(r?.text, out byte red) &&
                    byte.TryParse(g?.text, out byte green) &&
                    byte.TryParse(b?.text, out byte blue))
                    LocalPlayer.ChangeColor(new Color32(red, green, blue, 255));
            });

            Button nameButton = canvas.Find("ControlUI/NameButton")?.GetComponent<Button>();
            nameButton?.onClick.AddListener(() => LocalPlayer.ChangeName(textInput?.text ?? ""));

            TMP_InputField debugInput = debugUI?.transform.Find("TextInput")?.GetComponent<TMP_InputField>();
            if (debugInput != null)
            {
                debugInput.onSelect.AddListener(_ => focusedOnDebug = true);
                debugInput.onDeselect.AddListener(_ => focusedOnDebug = false);
                debugInput.onEndEdit.AddListener(text =>
                {
                    if (focusedOnDebug && !string.IsNullOrEmpty(debugInput.text))
                        UI.Instance?.HandleDebugCommand(text);
                    debugInput.text = string.Empty;
                });
            }

            textObjects.AddRange(new[]
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
            }.Where(textObject => textObject != null));

            imageObjects.AddRange(new[]
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
            }.Where(imageObject => imageObject != null));

            if (watermark != null) watermark.material = new Material(watermark.material);
            watermarkImage = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.icon.png");

            if (!Bootstrapper.FirstLaunch)
                canvas.Find("HideMessage")?.gameObject.SetActive(false);
        }

        private void ToggleDebug()
        {
            if (debugUI == null) return;
            if (debugUI.activeSelf)
                debugUI.SetActive(false);
            else
            {
                if (dynamicSounds)
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/console.ogg", "Audio/Menu/console.ogg", clip => clip.Play(buttonClickVolume / 10f));
                debugUI.SetActive(true);
            }
        }

        public void DebugPrint(string text)
        {
            if (debugUI == null || templateLine == null || !debugUI.activeSelf) return;
            Transform lines = debugUI.transform.Find("Lines");
            if (lines == null) return;

            GameObject line = Instantiate(templateLine, lines, false);
            line.SetActive(true);

            TextMeshProUGUI lineText = line.GetComponent<TextMeshProUGUI>();
            if (lineText != null) lineText.text = text;

            if (lines.childCount > 14) Destroy(lines.GetChild(1).gameObject);
        }

        public void Refresh()
        {
            if (uiPrefab == null) return;

            if (UnityEngine.InputSystem.Keyboard.current?.backquoteKey.wasPressedThisFrame == true)
                ToggleDebug();

            Color guiColor = Manager.GetIndex("Swap GUI Colors").enabled
                ? textColors[1].GetCurrentColor()
                : backgroundColor.GetCurrentColor();

            if (versionLabel != null) versionLabel.color = guiColor;
            if (roomStatus != null) roomStatus.color = guiColor;
            if (arraylist != null) arraylist.color = guiColor;

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

            if (controlBackground != null) controlBackground.color = menuBackgroundColor.GetCurrentColor();

            foreach (var textObject in textObjects)
            {
                if (textObject == null) continue;
                textObject.color = textColors[1].GetCurrentColor();
                textObject.SafeSetFont(activeFont);
                textObject.SafeSetFontStyle(activeFontStyle);
            }

            foreach (var imageObject in imageObjects)
                if (imageObject != null) imageObject.color = buttonColors[0].GetCurrentColor();

            if (watermark != null)
                watermark.transform.rotation = Quaternion.Euler(0f, 0f, rockWatermark ? Mathf.Sin(Time.time * 2f) * 10f : 0f);

            versionLabel?.SafeSetText(FollowMenuSettings("Build") + " " + PluginInfo.Version + "\n" + PluginInfo.DiscordInvite.Replace("https://", ""));

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
                if (debugImage != null) debugImage.color = backgroundColor.GetCurrentColor();

                List<TextMeshProUGUI> debugTextObjects = new List<TextMeshProUGUI>
                {
                    debugUI.transform.Find("Title")?.GetComponent<TextMeshProUGUI>(),
                    debugUI.transform.Find("TextInput/Text Area/Text")?.GetComponent<TextMeshProUGUI>(),
                    debugUI.transform.Find("TextInput/Text Area/Placeholder")?.GetComponent<TextMeshProUGUI>()
                }.Where(textObject => textObject != null).ToList();

                Transform lines = debugUI.transform.Find("Lines");
                if (lines != null) debugTextObjects.AddRange(lines.GetComponentsInChildren<TextMeshProUGUI>());

                foreach (var textObject in debugTextObjects)
                {
                    textObject.color = textColors[1].GetCurrentColor();
                    textObject.SafeSetFont(activeFont);
                    textObject.SafeSetFontStyle(activeFontStyle);
                }

                TextMeshProUGUI title = debugUI.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null) title.color = textColors[0].GetCurrentColor();
            }

            if (!(Time.time > uiUpdateDelay)) return;

            Texture2D watermarkTexture = customWatermark ?? watermarkImage;
            if (watermark != null && watermarkTexture != null && (watermark.sprite == null || watermark.sprite.texture == null || watermark.sprite.texture != watermarkTexture))
                watermark.sprite = Sprite.Create(watermarkTexture, new Rect(0, 0, watermarkTexture.width, watermarkTexture.height), new Vector2(0.5f, 0.5f), 100f);

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
            foreach (KeyValuePair<string, Module[]> category in Manager.modules)
            {
                if (category.Key == "Temporary Category" || (hideSettings && category.Key.Contains("Settings"))
                    || (hideMacros && category.Key.Contains("Macro"))) continue;

                foreach (Module button in category.Value)
                {
                    try
                    {
                        if (button == null || !button.enabled || button.hideFromArraylist || button.label) continue;

                        string buttonText = button.RenderName;
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
                        $"<color=#{ColorToHex(textColors[1].GetCurrentColor())}>{sortedMods[i]} </color><mark=#{ColorToHex(buttonColors[1].GetCurrentColor(i * -0.1f))}> </mark>" :
                        $"<mark=#{ColorToHex(buttonColors[1].GetCurrentColor(i * -0.1f))}> </mark><color=#{ColorToHex(textColors[1].GetCurrentColor())}>{sortedMods[i]} </color>") + "\n";
                else
                    modListText += sortedMods[i] + "\n";
            }

            arraylist.SafeSetText(modListText);
        }
    }
}
