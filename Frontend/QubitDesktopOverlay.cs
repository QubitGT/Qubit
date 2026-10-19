/*
 * Qubit Menu  Frontend/QubitDesktopOverlay.cs
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
using Qubit.Libraries.Core;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Qubit.Frontend
{
    public sealed class QubitDesktopOverlay : MonoBehaviour
    {

        private static QubitDesktopOverlay current;
        private static Sprite roundedSprite;
        private InputField roomCode, playerName, red, green, blue;
        private Text roomStatus, feedback, count;
        private Button join, disconnect;
        private RectTransform roomPanel, modsPanel, content, rowTemplate;
        private GameObject empty, brand;
        private ScrollRect scroll;
        private readonly List<RectTransform> rows = new List<RectTransform>();
        private float nextRefresh;
        private string previousList;
        private bool flipped;
        private float baseScrollHeight;
        private float baseViewportHeight;

        public static bool IsInputFocused => current != null && current.isActiveAndEnabled
            && (current.roomCode.isFocused || current.playerName.isFocused || current.red.isFocused
                || current.green.isFocused || current.blue.isFocused);

        public static QubitDesktopOverlay Attach(Transform parent)
        {
            GameObject instance = null;
            try
            {
                instance = Instantiate(Qubit.Frontend.Resources.QubitAssets.Load<GameObject>("QubitDesktopOverlay"), parent, false);
                var overlay = instance.AddComponent<QubitDesktopOverlay>();
                overlay.Bind();
                current = overlay;
                return overlay;
            }
            catch (Exception exception)
            {
                if (instance != null) Destroy(instance);
                Logs.LogError("Qubit desktop overlay failed to load: " + exception);
                return null;
            }
        }

        private T Find<T>(string path) where T : Component => transform.Find(path).GetComponent<T>();

        private void Bind()
        {
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.material = null;
            roomPanel = Find<RectTransform>("RoomPanel");
            modsPanel = Find<RectTransform>("ModsPanel");
            roomCode = Find<InputField>("RoomPanel/RoomCode");
            playerName = Find<InputField>("RoomPanel/PlayerName");
            red = Find<InputField>("RoomPanel/Red");
            green = Find<InputField>("RoomPanel/Green");
            blue = Find<InputField>("RoomPanel/Blue");
            roomStatus = Find<Text>("RoomPanel/RoomStatus");
            feedback = Find<Text>("RoomPanel/Feedback");
            join = Find<Button>("RoomPanel/Join");
            disconnect = Find<Button>("RoomPanel/Disconnect");
            count = Find<Text>("ModsPanel/Count");
            scroll = Find<ScrollRect>("ModsPanel/Scroll");
            content = Find<RectTransform>("ModsPanel/Scroll/Viewport/Content");
            rowTemplate = Find<RectTransform>("ModsPanel/Scroll/Viewport/Content/RowTemplate");
            empty = transform.Find("ModsPanel/Empty").gameObject;
            brand = transform.Find("Brand").gameObject;

            join.onClick.AddListener(JoinRoom);
            var queueButton = transform.Find("RoomPanel/Queue");
            if (queueButton != null) queueButton.gameObject.SetActive(false);
            disconnect.onClick.AddListener(() =>
            {
                ManagerRegistry.NetworkSystem?.ReturnToSinglePlayer();
                feedback.text = "Disconnect requested.";
            });
            Button setName = Find<Button>("RoomPanel/SetName");
            setName.onClick.AddListener(() =>
            {
                if (string.IsNullOrWhiteSpace(playerName.text)) { feedback.text = "Enter a player name first."; return; }
                LocalPlayer.ChangeName(playerName.text.Trim());
                feedback.text = "Player name updated.";
            });
            Find<Button>("RoomPanel/SetColor").onClick.AddListener(() =>
            {
                if (!byte.TryParse(red.text, out byte r) || !byte.TryParse(green.text, out byte g) || !byte.TryParse(blue.text, out byte b))
                { feedback.text = "Enter R, G and B values from 0 to 255."; return; }
                LocalPlayer.ChangeColor(new Color32(r, g, b, 255));
                feedback.text = "Player color updated.";
            });
            ApplyLayout(setName);
            StyleRoomJoiner();
            if (EventSystem.current == null)
            {
                var events = new GameObject("Qubit Overlay Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Refresh();
        }

        private static void SetSize(RectTransform rect, float width, float height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private static void PlaceAt(RectTransform rect, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
        }

        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 32, radius = 9;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - .5f, 0, x + .5f - (size - radius));
                    float dy = Mathf.Max(radius - y - .5f, 0, y + .5f - (size - radius));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        private static void Round(Image image)
        {
            if (image == null) return;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }

        private void ApplyLayout(Button setName)
        {
            roomPanel.localScale = Vector3.one * .82f;
            modsPanel.localScale = Vector3.one * .82f;
            SetSize(modsPanel, 290, 320);
            baseScrollHeight = scroll.GetComponent<RectTransform>().rect.height;
            baseViewportHeight = scroll.viewport.rect.height;
            RemoveModsBackground();
            HideModsHeaders();
            count.gameObject.SetActive(false);
            empty.SetActive(false);
            var scrollbar = scroll.verticalScrollbar;
            if (scrollbar != null) scrollbar.gameObject.SetActive(false);
            flipped = Frontend.Settings.flipArraylist;
            PlacePanel(roomPanel, !flipped);
            PlacePanel(modsPanel, flipped);

            var queueTransform = transform.Find("RoomPanel/Queue") as RectTransform;
            var nameTransform = setName.GetComponent<RectTransform>();
            if (queueTransform != null)
            {
                nameTransform.anchorMin = queueTransform.anchorMin;
                nameTransform.anchorMax = queueTransform.anchorMax;
                nameTransform.pivot = queueTransform.pivot;
                nameTransform.anchoredPosition = queueTransform.anchoredPosition;
                nameTransform.sizeDelta = queueTransform.sizeDelta;
            }
            var joinTransform = join.GetComponent<RectTransform>();
            if (queueTransform != null) SetSize(nameTransform, joinTransform.rect.width, joinTransform.rect.height);

            var brandRect = brand.GetComponent<RectTransform>();
            if (brandRect != null)
            {
                brandRect.localScale = Vector3.one * .78f;
                PlaceAt(brandRect, new Vector2(1, 0), new Vector2(-20, 20));
            }
            Image brandBackground = brand.GetComponent<Image>();
            if (brandBackground == null)
            {
                Transform background = brand.transform.Find("Background");
                if (background != null) brandBackground = background.GetComponent<Image>();
            }
            Round(brandBackground);
            var discord = transform.Find("Discord");
            if (discord != null && discord != brand.transform && !discord.IsChildOf(brand.transform))
            {
                var discordRect = discord as RectTransform;
                if (discordRect != null)
                {
                    discordRect.localScale = Vector3.one * .78f;
                    PlaceAt(discordRect, new Vector2(1, 0), new Vector2(-20, 64));
                }
            }

            foreach (Text label in GetComponentsInChildren<Text>(true))
            {
                if (string.IsNullOrEmpty(label.text)) continue;
                string text = label.text.ToLowerInvariant();
                if (text.Contains("hide overlay") || text.Contains("show / hide") || text.Contains("to hide") || text.Contains("to close"))
                    label.gameObject.SetActive(false);
            }

            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.transform == brand.transform || image.transform.IsChildOf(brand.transform)) continue;
                if (image.name.IndexOf("Discord", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (image.GetComponent<Mask>() != null || image.GetComponent<RectMask2D>() != null) continue;
                if (image.GetComponent<Button>() != null || image.GetComponent<InputField>() != null ||
                    image.transform == roomPanel || image.transform == modsPanel || image.transform == rowTemplate ||
                    image.name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0)
                    Round(image);
            }
        }

        private void StyleRoomJoiner()
        {
            SetSize(roomPanel, 360, 326);
            roomPanel.GetComponent<Image>().color = new Color32(27, 31, 51, 250);
            var border = roomPanel.GetComponent<Outline>();
            if (border != null)
            {
                border.effectColor = new Color32(146, 136, 193, 100);
                border.effectDistance = new Vector2(1, -1);
            }
            foreach (Text label in roomPanel.GetComponentsInChildren<Text>(true))
            {
                label.color = new Color32(237, 237, 249, 255);
                label.resizeTextForBestFit = false;
                label.raycastTarget = false;
            }
            RoomRect("Eyebrow", 76, 23, 260, 18);
            RoomLabel("Eyebrow", "QUBIT", 12);
            RoomRect("Title", 76, 44, 260, 32);
            RoomLabel("Title", "Room joiner", 24);
            var logo = new GameObject("SessionLogo", typeof(RectTransform), typeof(Image));
            logo.transform.SetParent(roomPanel, false);
            var logoImage = logo.GetComponent<Image>();
            logoImage.sprite = brand.transform.Find("Logo").GetComponent<Image>().sprite;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            RoomRect("SessionLogo", 24, 28, 36, 36);
            RoomRect("RoomStatus", 24, 91, 312, 24);
            roomStatus.fontSize = 13;
            roomStatus.color = new Color32(181, 184, 211, 255);
            RoomRect("CodeLabel", 24, 137, 312, 18);
            RoomLabel("CodeLabel", "ROOM CODE", 11);
            RoomRect("RoomCode", 24, 162, 312, 50);
            roomCode.GetComponent<Image>().color = new Color32(42, 46, 70, 255);
            roomCode.textComponent.fontSize = 20;
            roomCode.textComponent.alignment = TextAnchor.MiddleLeft;
            StretchRoomText(roomCode.textComponent.rectTransform, 14);
            var placeholder = (Text)roomCode.placeholder;
            placeholder.fontSize = 14;
            placeholder.color = new Color32(146, 151, 180, 255);
            placeholder.alignment = TextAnchor.MiddleLeft;
            StretchRoomText(placeholder.rectTransform, 14);
            roomCode.caretColor = new Color32(196, 164, 255, 255);
            roomCode.selectionColor = new Color32(157, 117, 235, 90);
            RoomButton(join, 24, 226, 184, 44, true);
            RoomButton(disconnect, 220, 226, 116, 44, false);
            RoomRect("Feedback", 24, 279, 312, 38);
            feedback.fontSize = 12;
            feedback.color = new Color32(194, 181, 233, 255);
            feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            foreach (string name in new[] { "PlayerLabel", "PlayerName", "SetName", "Red", "Green", "Blue", "SetColor" })
                roomPanel.Find(name).gameObject.SetActive(false);
        }

        private void RoomRect(string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)roomPanel.Find(name);
            PlaceAt(rect, new Vector2(0, 1), new Vector2(x, -y));
            SetSize(rect, width, height);
        }

        private void RoomLabel(string name, string value, int size)
        {
            var label = roomPanel.Find(name).GetComponent<Text>();
            label.text = value;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            if (size < 16) label.color = new Color32(177, 157, 225, 255);
        }

        private static void StretchRoomText(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, 0);
            rect.offsetMax = new Vector2(-padding, 0);
        }

        private void RoomButton(Button button, float x, float y, float width, float height, bool primary)
        {
            RoomRect(button.name, x, y, width, height);
            button.GetComponent<Image>().color = Color.white;
            var colors = button.colors;
            colors.normalColor = primary ? new Color32(157, 117, 235, 255) : new Color32(57, 61, 89, 255);
            colors.highlightedColor = primary ? new Color32(178, 143, 250, 255) : new Color32(76, 79, 111, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = primary ? new Color32(128, 90, 207, 255) : new Color32(43, 47, 72, 255);
            colors.disabledColor = new Color32(41, 44, 65, 180);
            colors.fadeDuration = .12f;
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = primary ? 15 : 13;
            label.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            StretchRoomText(label.rectTransform, 6);
        }

        private void HideModsHeaders()
        {
            foreach (Text label in modsPanel.GetComponentsInChildren<Text>(true))
            {
                if (label.transform.IsChildOf(rowTemplate)) continue;
                label.gameObject.SetActive(false);
            }

            foreach (Transform child in modsPanel)
            {
                if (child == scroll.transform || child == empty.transform) continue;
                string name = child.name.ToLowerInvariant();
                if (name.Contains("header") || name.Contains("title") || name.Contains("active") || name.Contains("count"))
                    child.gameObject.SetActive(false);
            }

            RectTransform scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(scrollRect.anchorMin.x, 1);
            scrollRect.anchorMax = new Vector2(scrollRect.anchorMax.x, 1);
            scrollRect.pivot = new Vector2(scrollRect.pivot.x, 1);
            scrollRect.anchoredPosition = new Vector2(scrollRect.anchoredPosition.x, -8);
        }

        private void RemoveModsBackground()
        {
            var panelImage = modsPanel.GetComponent<Image>();
            if (panelImage != null) panelImage.enabled = false;

            foreach (Transform child in modsPanel)
            {
                if (child.name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var image = child.GetComponent<Image>();
                    if (image != null) image.enabled = false;
                }
            }

            if (scroll.viewport != null)
            {
                var mask = scroll.viewport.GetComponent<Mask>();
                if (mask != null) mask.enabled = false;
                var rectMask = scroll.viewport.GetComponent<RectMask2D>();
                if (rectMask != null) rectMask.enabled = false;
                var viewportImage = scroll.viewport.GetComponent<Image>();
                if (viewportImage != null) viewportImage.enabled = false;
            }

            scroll.enabled = false;
        }

        private string GetRoomCode()
        {
            string code = roomCode.text.Trim().ToUpperInvariant();
            if (code.Length == 0 || code.Any(character => !char.IsLetterOrDigit(character)))
            {
                feedback.text = "Enter a room code using letters and numbers.";
                return null;
            }
            roomCode.SetTextWithoutNotify(code);
            return code;
        }

        private void JoinRoom()
        {
            string code = GetRoomCode();
            if (code == null || ManagerRegistry.PhotonNetworkController == null) return;
            Rooms.JoinRoom(code);
            feedback.text = "Join requested for " + code + ".";
        }

        public void Refresh()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + (Frontend.Settings.advancedArraylist ? .1f : .5f);
            bool inRoom = ManagerRegistry.NetworkSystem != null && Networking.InRoom;
            roomStatus.text = inRoom ? "Connected  /  " + (PhotonNetwork.CurrentRoom?.Name ?? "Room") : "Not connected to a room";
            disconnect.interactable = inRoom;
            join.interactable = ManagerRegistry.PhotonNetworkController != null;
            brand.SetActive(!Frontend.Settings.disableWatermark);

            if (flipped != Frontend.Settings.flipArraylist)
            {
                flipped = Frontend.Settings.flipArraylist;
                PlacePanel(roomPanel, !flipped);
                PlacePanel(modsPanel, flipped);
            }

            var enabled = new List<string>();
            foreach (KeyValuePair<string, Module[]> category in Manager.modules)
            {
                string name = category.Key;
                if (name == "Temporary Category" || (Frontend.Settings.hideSettings && name.Contains("Settings"))
                    || (Frontend.Settings.hideMacros && name.Contains("Macro"))) continue;
                foreach (var button in category.Value)
                {
                    if (button == null || !button.enabled || button.hideFromArraylist || button.label) continue;
                    enabled.Add(Strings.NoRichtextTags(button.RenderName));
                }
            }
            enabled.Sort(StringComparer.OrdinalIgnoreCase);
            string signature = string.Join("\n", enabled);
            if (signature == previousList) return;
            previousList = signature;
            count.gameObject.SetActive(false);
            empty.SetActive(false);
            while (rows.Count < enabled.Count)
            {
                var row = Instantiate(rowTemplate, content, false);
                row.name = "ModRow" + rows.Count;
                rows.Add(row);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(i < enabled.Count);
                if (i >= enabled.Count) continue;
                rows[i].anchoredPosition = new Vector2(0, -44 * i);
                var label = rows[i].Find("Label").GetComponent<Text>();
                label.supportRichText = false;
                label.text = enabled[i];
            }
            float targetViewportHeight = Mathf.Max(44, enabled.Count * 44);
            float heightChange = targetViewportHeight - baseViewportHeight;
            SetSize(modsPanel, 290, Mathf.Max(100, 320 + heightChange));
            SetSize(scroll.GetComponent<RectTransform>(), scroll.GetComponent<RectTransform>().rect.width,
                Mathf.Max(44, baseScrollHeight + heightChange));
            SetSize(scroll.viewport, scroll.viewport.rect.width, targetViewportHeight);
            content.sizeDelta = new Vector2(content.sizeDelta.x, targetViewportHeight);
        }

        private static void PlacePanel(RectTransform panel, bool right)
        {
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(right ? 1 : 0, 1);
            panel.anchoredPosition = new Vector2(right ? -20 : 20, -20);
        }

        private void OnEnable() { nextRefresh = 0; }
        private void OnDestroy() { if (current == this) current = null; }
    }
}
