/*
 * Qubit Menu  Libraries/Interface/Notifications.cs
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
using GorillaLocomotion;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Language;
using Qubit.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Transcription;

namespace Qubit.Libraries.Interface
{
    public class Notifications : MonoBehaviour
    {
        public static Notifications Instance { get; private set; }

        public GameObject canvas;
        private GameObject mainCamera;
        

        public static string PreviousNotifi;
        



        public static readonly Dictionary<string, string> information = new Dictionary<string, string>();

        public static TextMeshProUGUI notificationText;
        public static TextMeshProUGUI arraylistText;
        public static TextMeshProUGUI informationText;

        private bool hasInitialized;
        public static bool noRichText;
        public static bool soundOnError;
        public static bool noPrefix;
        public static bool narrateNotifications;

        public static int NotifiCounter;
        private class NotificationEntry
        {
            public string RawText;
            public string BaseDisplay;
            public int Counter;
            public float ExpireTime;
        }

        private static readonly List<NotificationEntry> activeNotifications = new List<NotificationEntry>();

        private void Start()
        {
            Instance = this;
            Logs.Log("Notifications loaded");
        }

        private void Init()
        {
            mainCamera = Camera.main.gameObject;

            GameObject canvasParent = new GameObject("Qubit_NotificationParent");
            canvasParent.transform.position = mainCamera.transform.position;

            canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.enabled = true;
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.worldCamera = mainCamera.GetComponent<Camera>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(5f, 5f);
            canvasRect.position = mainCamera.transform.position;

            canvas.transform.parent = canvasParent.transform;
            canvasRect.localPosition = new Vector3(0f, 0f, 1.6f);
            canvasRect.localScale = Vector3.one;

            Vector3 rotation = canvasRect.rotation.eulerAngles;
            rotation.y = -270f;
            canvasRect.rotation = Quaternion.Euler(rotation);

            

            notificationText = CreateText(canvas.transform, new Vector3(-1f, -1f, -0.5f),
                new Vector2(450f, 210f), 30, TextAlignmentOptions.BottomLeft);

            arraylistText = CreateText(canvas.transform, new Vector3(-1f, -1f, -0.5f),
                new Vector2(450f, 1000f), 20, TextAlignmentOptions.TopLeft);

            informationText = CreateText(canvas.transform, new Vector3(-1f, -1f, 0.5f),
                new Vector2(450f, 1000f), 30, TextAlignmentOptions.TopRight);

            StartCoroutine(SetShaderAfterInit());
        }

        private TextMeshProUGUI CreateText(Transform parent, Vector3 localPos, Vector2 size, int fontSize, TextAlignmentOptions anchor)
        {
            GameObject textObj = new GameObject { transform = { parent = parent } };
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

            text.SafeSetText("");
            text.fontSize = fontSize;
            text.SafeSetFont(AgencyFB);
            text.rectTransform.sizeDelta = size;
            text.alignment = anchor;
            text.overflowMode = anchor == TextAlignmentOptions.BottomLeft ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            text.rectTransform.localScale = new Vector3(0.00333333333f, 0.00333333333f, 0.33333333f);
            text.rectTransform.localPosition = localPos;
            
            text.characterSpacing = -9f;

            return text;
        }

        private float updateArraylistTimer;
        private void FixedUpdate()
        {
            try
            {
                if (!hasInitialized && Camera.main != null)
                {
                    Init();
                    hasInitialized = true;
                }

                canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;

                canvas.transform.position = mainCamera.transform.TransformPoint(0f, 0f, 1.6f);
                canvas.transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(0, 90, 0);
                canvas.transform.localScale = Vector3.one * (scaleWithPlayer ? References.Player.scale : 1f);

                UpdateNotificationExpiry();

                try
                {
                    arraylistText.SafeSetFont(activeFont);
                    arraylistText.SafeSetFontStyle(activeFontStyle);
                    arraylistText.SafeSetFontSize(arraylistScale);
                    arraylistText.Chams();

                    notificationText.SafeSetFont(activeFont);
                    notificationText.SafeSetFontStyle(activeFontStyle);
                    notificationText.SafeSetFontSize(notificationScale);
                    notificationText.rectTransform.localPosition = new Vector3(-1f, disableNotifications ? -100f : -1f, -0.5f);
                    notificationText.Chams();

                    informationText.SafeSetFont(activeFont);
                    informationText.SafeSetFontStyle(activeFontStyle);
                    informationText.SafeSetFontSize(overlayScale);
                    informationText.Chams();

                    FollowMenuSettings(arraylistText);
                    FollowMenuSettings(notificationText);
                    FollowMenuSettings(informationText);
                }
                catch { }

                arraylistText.rectTransform.localPosition = new Vector3(-1f, -1f, flipArraylist ? 0.5f : -0.5f);
                arraylistText.alignment = flipArraylist ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

                informationText.rectTransform.localPosition = new Vector3(-1f, -1f, flipArraylist ? -0.5f : 0.5f);
                informationText.alignment = flipArraylist ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;

                if (information.Count > 0)
                {
                    Color targetColor = Manager.GetIndex("Swap GUI Colors").enabled ? buttonColors[1].GetCurrentColor() : backgroundColor.GetCurrentColor();

                    List<string> statsLines = information
                        .Select(item => $"<color=#{ColorToHex(targetColor)}>{item.Key}</color> <color=#{ColorToHex(textColors[1].GetColor(0))}>{item.Value}</color>")
                        .Select(line => (text: line, width: informationText.GetPreferredValues(NoRichtextTags(line)).x))
                        .OrderByDescending(t => t.width)
                        .Select(t => t.text)
                        .ToList();

                    informationText.SafeSetText(string.Join("\n", statsLines));
                    informationText.color = Color.white;
                }
                else if (!informationText.text.IsNullOrEmpty())
                    informationText.SafeSetText("");

                if (showEnabledModsVR)
                {
                    if (Time.time > updateArraylistTimer)
                    {
                        updateArraylistTimer = Time.time + (advancedArraylist ? 0.1f : 0.5f);
                        List<string> enabledMods = new List<string>();
                        foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                        {
                            Module[] buttonList = categoryEntry.Value;
                            foreach (Module button in buttonList)
                            {
                                try
                                {
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
                            .Select(s => (text: s, width: arraylistText.GetPreferredValues(NoRichtextTags(s)).x))
                            .OrderByDescending(t => t.width)
                            .Select(t => t.text)
                            .ToArray();


                        var sb = new System.Text.StringBuilder();
                        for (int i = 0; i < sortedMods.Length; i++)
                        {
                            if (advancedArraylist)
                                /* Flipped */
                                sb.Append(flipArraylist ? $"..." : $"...").Append('\n');
                            else
                                /* Normal  */
                                sb.Append(sortedMods[i]).Append('\n');
                        }
                        arraylistText.SafeSetText(sb.ToString());

                        arraylistText.color = Manager.GetIndex("Swap GUI Colors").enabled ? textColors[1].GetColor(0) : backgroundColor.GetCurrentColor();
                    }
                }
                else if (!arraylistText.text.IsNullOrEmpty())
                    arraylistText.SafeSetText("");

                if (lowercaseMode)
                {
                    if (!arraylistText.text.IsNullOrEmpty())
                        arraylistText.SafeSetText(arraylistText.text.ToLower());

                    if (!notificationText.text.IsNullOrEmpty())
                        notificationText.SafeSetText(notificationText.text.ToLower());

                    if (!informationText.text.IsNullOrEmpty())
                        informationText.SafeSetText(informationText.text.ToLower());
                }

                if (uppercaseMode)
                {
                    if (!arraylistText.text.IsNullOrEmpty())
                        arraylistText.SafeSetText(arraylistText.text.ToUpper());

                    if (!notificationText.text.IsNullOrEmpty())
                        notificationText.SafeSetText(notificationText.text.ToUpper());

                    if (!informationText.text.IsNullOrEmpty())
                        informationText.SafeSetText(informationText.text.ToUpper());
                }

                canvas.layer = Manager.GetIndex("Hide Notifications on Camera").enabled ? 19 : 0;
            }
            catch (Exception e) { Logs.Log(e); }
        }

        












        public static void Send(string notificationText, int clearTime = -1)
        {
            if (clearTime < 0)
                clearTime = notificationDecayTime;

            if (disableNotifications && !Manager.GetIndex("Conduct Notifications").enabled) return;
            try
            {
                if (translate)
                {
                    if (Translations.translateCache.ContainsKey(notificationText))
                        notificationText = Translations.TranslateText(notificationText);
                    else
                    {
                        Translations.TranslateText(notificationText, delegate { Send(notificationText, clearTime); });
                        return;
                    }
                }

                if (/*notificationSoundIndex != 0 && */(!soundOnError || notificationText.Contains("<color=red>ERROR</color>")) && Time.time > timeMenuStarted + 5f)
                    Sounds.Play(Sounds.DefaultSounds["Notification"], action: clip => AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, buttonClickVolume / 10f));

                if (inputTextColor != "green")
                    notificationText = notificationText.Replace("<color=green>", "<color=" + inputTextColor + ">");

                notificationText = FixTMProTags(notificationText);

                if (hideBrackets)
                    notificationText = notificationText.Replace("[", "").Replace("]", "");

                notificationText = notificationText.TrimEnd('\n', '\r');

                float expireTime = Time.time + (clearTime / 1000f);
                NotificationEntry lastEntry = activeNotifications.Count > 0 ? activeNotifications[^1] : null;

                if (lastEntry != null && PreviousNotifi == notificationText && stackNotifications)
                {
                    lastEntry.Counter++;
                    lastEntry.ExpireTime = expireTime;
                    NotifiCounter = lastEntry.Counter;
                }
                else
                {
                    NotifiCounter = 0;
                    PreviousNotifi = notificationText;

                    activeNotifications.Add(new NotificationEntry
                    {
                        RawText = notificationText,
                        BaseDisplay = notificationText,
                        Counter = 0,
                        ExpireTime = expireTime
                    });
                }

                RebuildNotificationText();

                if (lowercaseMode)
                    Notifications.notificationText.SafeSetText(Notifications.notificationText.text.ToLower());

                if (uppercaseMode)
                    Notifications.notificationText.SafeSetText(Notifications.notificationText.text.ToUpper());

                if (narrateNotifications)
                    NarrateText(NoRichtextTags(noPrefix ? RemovePrefix(notificationText) : notificationText));
            }
            catch (Exception e)
            {
                Logs.LogError($"Notification failed.\nNotification: {notificationText}\nException: {e.Message}");
            }
        }

        







        public static void SendFormat(string color, string tag, string message, int clearTime = -1) =>
            Send($"{tag.FormatTag(color)} {message}", clearTime);

        public static void PlayNotificationSound() =>
            Sounds.Play(Sounds.DefaultSounds["Notification"], action: clip => AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, buttonClickVolume / 10f));

        





        public static void Clear()
        {
            activeNotifications.Clear();
            notificationText.SafeSetText("");
        }

        




        public static void ClearPastNotifications(int amount)
        {
            if (activeNotifications.Count == 0)
                return;

            int removeCount = Math.Min(amount, activeNotifications.Count);
            activeNotifications.RemoveRange(0, removeCount);
            RebuildNotificationText();
        }

        



        private static void UpdateNotificationExpiry()
        {
            if (activeNotifications.Count == 0)
                return;

            bool changed = false;
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                if (Time.time >= activeNotifications[i].ExpireTime)
                {
                    activeNotifications.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
                RebuildNotificationText();
        }

        


        private static void RebuildNotificationText()
        {
            List<string> lines = new List<string>(activeNotifications.Count);
            foreach (NotificationEntry entry in activeNotifications)
            {
                lines.Add(entry.Counter > 0
                    ? $"{entry.BaseDisplay} <color=grey>(x{entry.Counter + 1})</color>"
                    : entry.BaseDisplay);
            }

            string combined = string.Join(Environment.NewLine, lines);

            if (noRichText)
                combined = NoRichtextTags(combined);

            notificationText.SafeSetText(combined);
            notificationText.richText = !noRichText;
        }

        private IEnumerator SetShaderAfterInit()
        {
            yield return null; yield return null; yield return null; yield return null; yield return null;

            notificationText.Chams();
            arraylistText.Chams();
            informationText.Chams();
        }

        private static readonly System.Text.RegularExpressions.Regex PrefixRegex =
            new System.Text.RegularExpressions.Regex(@"^<color=grey>\[</color><color=[^>]+>.*?</color><color=grey>\]</color> ", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string RemovePrefix(string text)
        {
            return string.IsNullOrEmpty(text) ? text : PrefixRegex.Replace(text, "");
        }

        public static void RoomJoined(string room)
        {
            if (!Frontend.Settings.disableRoomNotifications)
                SendFormat("blue", "JOIN ROOM", $"Room Code: {room}");
        }

        public static void RoomLeft(string room)
        {
            if (Frontend.Settings.clearNotificationsOnDisconnect)
                Clear();

            if (!Frontend.Settings.disableRoomNotifications)
                SendFormat("blue", "LEAVE ROOM", $"Room Code: {room}");
        }

        public static void MasterClientSwitched()
        {
            if (Qubit.Core.Networking.IsMasterClient)
                SendFormat("purple", "MASTER", "You are now master client.");
        }

        public static void PlayerJoined(NetPlayer player)
        {
            if (player != Qubit.Core.Networking.LocalPlayer && !Frontend.Settings.disablePlayerNotifications)
                SendFormat("green", "JOIN", $"Name: {Extensions.Players.CleanPlayerName(player.NickName)}");
        }

        public static void PlayerLeft(NetPlayer player)
        {
            if (player != Qubit.Core.Networking.LocalPlayer && !Frontend.Settings.disablePlayerNotifications)
                SendFormat("red", "LEAVE", $"Name: {Extensions.Players.CleanPlayerName(player.NickName)}");
        }
    }
}