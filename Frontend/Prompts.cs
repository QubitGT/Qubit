/*
 * Qubit Menu  Frontend/Prompts.cs
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

using Qubit.Frontend.Colors;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.Files;
using ButtonCollider = Qubit.Frontend.Classes.ButtonCollider;

namespace Qubit.Frontend
{
    public static class Prompts
    {

        public static void RenderPrompt()
        {
            TextMeshPro promptText = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<TextMeshPro>();
            promptText.font = activeFont;
            promptText.text = CurrentPrompt.Message;

            string promptImageUrl = ExtractPromptImage(CurrentPrompt.Message);
            if (promptImageUrl != null)
                promptText.text = promptText.text.Replace($"<{promptImageUrl}>", "");

            promptText.text = FollowMenuSettings(promptText.text, true);

            promptText.fontSize = 1;
            promptText.lineSpacing = 0.8f;
            promptText.AddComponent<UIColors>().colors = textColors[0];

            promptText.richText = true;
            promptText.fontStyle = activeFontStyle;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.enableAutoSizing = true;
            promptText.fontSizeMin = 0;
            RectTransform component = promptText.GetComponent<RectTransform>();
            component.sizeDelta = new Vector2(0.28f, CurrentPrompt.IsText ? 0.25f : 0.28f);

            component.localPosition = new Vector3(0.06f, 0f, CurrentPrompt.IsText ? -0.025f : 0f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (promptImageUrl != null)
            {
                string fileName = promptImageUrl.Split("/")[^1];
                string fileExtension = GetFileExtension(fileName);

                Image promptImage = new GameObject
                {
                    transform =
                    {
                        parent = canvasObj.transform
                    }
                }.AddComponent<Image>();

                component.sizeDelta = new Vector2(component.sizeDelta.x, 0.03f);
                component.localPosition = new Vector3(0.06f, 0f, 0.1f);

                if (promptMat == null)
                    promptMat = new Material(promptImage.material);

                promptImage.material = promptMat;

                RectTransform imageTransform = promptImage.GetComponent<RectTransform>();
                imageTransform.localPosition = Vector3.zero;
                imageTransform.sizeDelta = new Vector2(.2f, .2f);

                imageTransform.localPosition = new Vector3(0.06f, 0f, promptText.text.IsNullOrEmpty() ? 0f : -0.03f);
                imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                FollowMenuSettings(promptImage);

                switch (fileExtension)
                {
                    case "png":
                    case "jpg":
                        {
                            promptImage.material.SetTexture("_MainTex", LoadTextureFromURL(promptImageUrl, fileName));
                            promptImage.material.color = Color.white;
                        }
                        break;
                    case "mp4":
                    case "webm":
                    case "mov":
                        {
                            promptVideoPlayer = new GameObject("Qubit_PromptVideoPlayer").AddComponent<VideoPlayer>();
                            promptVideoPlayer.playOnAwake = true;
                            promptVideoPlayer.isLooping = true;
                            promptVideoPlayer.url = promptImageUrl;

                            RenderTexture rt = new RenderTexture(192, 144, 0);
                            rt.Create();

                            promptVideoPlayer.targetTexture = rt;

                            promptImage.material = promptMat;
                            promptImage.material.color = Color.white;
                            promptImage.material.SetTexture("_MainTex", rt);
                        }
                        break;
                    case "mat":
                        {
                            promptImage.material = promptMaterial;
                            break;
                        }
                }
            }

            FollowMenuSettings(promptText);

            joystickButtonSelected %= 2;

            {
                GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (!UnityInput.GetKey(Key.Q) && !(inTextInput && isKeyboardPc))
                    button.layer = 2;

                button.GetComponent<BoxCollider>().isTrigger = true;
                button.transform.parent = menu.transform;
                button.transform.rotation = Quaternion.identity;
                button.transform.localScale = new Vector3(0.09f, CurrentPrompt.DeclineText == null ? 0.9f : 0.4375f, 0.08f);
                button.transform.localPosition = new Vector3(0.56f, CurrentPrompt.DeclineText == null ? 0f : 0.2375f, -0.43f);

                button.AddComponent<ButtonCollider>().relatedText = "Accept Prompt";

                if (lastClickedName != "Accept Prompt")
                {
                    ObjectColors colorChanger = button.AddComponent<ObjectColors>();
                    colorChanger.colors = buttonColors[0];

                    if (joystickMenu && joystickButtonSelected == 0)
                    {
                        joystickSelectedButton = "Accept Prompt";

                        ExtGradient gradient = colorChanger.colors.Clone();
                        gradient.SetColor(0, Color.red);

                        colorChanger.colors = gradient;
                    }
                }
                else
                    Coroutines.instance.StartCoroutine(ButtonClick(0, button.GetComponent<Renderer>()));

                TextMeshPro text = new GameObject { transform = { parent = canvasObj.transform } }.AddComponent<TextMeshPro>();
                text.font = activeFont;
                text.fontStyle = activeFontStyle;
                text.text = FollowMenuSettings(CurrentPrompt.AcceptText, true);
                text.fontSize = 1;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;

                text.AddComponent<UIColors>().colors = textColors[1];

                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(0.2f, 0.03f);

                if (arrowType == 11)
                    textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textRect.sizeDelta.y * 6f);

                if (NoAutoSizeText)
                    textRect.sizeDelta = new Vector2(9f, 0.015f);

                if (hideTextOnCamera)
                    textRect.gameObject.layer = 19;

                textRect.localPosition = new Vector3(0.064f, CurrentPrompt.DeclineText != null ? 0.075f : 0f, -0.16f);
                textRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                FollowMenuSettings(text);
                FollowMenuSettings(button);
            }

            if (CurrentPrompt.DeclineText == null) return;
            {
                GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (!UnityInput.GetKey(Key.Q) && !(inTextInput && isKeyboardPc))
                    button.layer = 2;

                button.GetComponent<BoxCollider>().isTrigger = true;
                button.transform.parent = menu.transform;
                button.transform.rotation = Quaternion.identity;
                button.transform.localScale = new Vector3(0.09f, 0.4375f, 0.08f);
                button.transform.localPosition = new Vector3(0.56f, -0.2375f, -0.43f);

                button.AddComponent<ButtonCollider>().relatedText = "Decline Prompt";

                if (lastClickedName != "Decline Prompt")
                {
                    ObjectColors colorChanger = button.AddComponent<ObjectColors>();
                    colorChanger.colors = buttonColors[0];

                    if (joystickMenu && joystickButtonSelected == 1)
                    {
                        joystickSelectedButton = "Decline Prompt";

                        ExtGradient gradient = colorChanger.colors.Clone();
                        gradient.SetColor(0, Color.red);

                        colorChanger.colors = gradient;
                    }
                }
                else
                    Coroutines.instance.StartCoroutine(ButtonClick(1, button.GetComponent<Renderer>()));

                TextMeshPro text = new GameObject { transform = { parent = canvasObj.transform } }.AddComponent<TextMeshPro>();
                text.font = activeFont;
                text.fontStyle = activeFontStyle;
                text.text = FollowMenuSettings(CurrentPrompt.DeclineText, true);
                text.fontSize = 1;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;

                text.AddComponent<UIColors>().colors = textColors[1];

                RectTransform textRect = text.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(0.2f, 0.03f);

                if (arrowType == 11)
                    textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textRect.sizeDelta.y * 6f);

                if (NoAutoSizeText)
                    textRect.sizeDelta = new Vector2(9f, 0.015f);

                if (hideTextOnCamera)
                    textRect.gameObject.layer = 19;

                textRect.localPosition = new Vector3(0.064f, -0.075f, -0.16f);
                textRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                FollowMenuSettings(text);
                FollowMenuSettings(button);
            }
        }

        public static string ExtractPromptImage(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var match = Regex.Match(input, @"<(?<url>https?://[^>]+)>");
            return match.Success ? match.Groups["url"].Value : null;
        }

        public class PromptData
        {
            public bool IsText;
            public string Message;

            public string AcceptText;
            public string DeclineText;

            public Action AcceptAction;
            public Action DeclineAction;
        }

        public static List<PromptData> prompts = new List<PromptData>();
        public static PromptData CurrentPrompt
        {
            get
            {
                return prompts.Count > 0 ? prompts[0] : null;
            }
        }

        public static Material promptMaterial;

        













        public static void Prompt(string Message, Action Accept = null, Action Decline = null, string AcceptButton = "Yes", string DeclineButton = "No")
        {
            Modules.Categories.Settings.DestroyKeyboard();
            keyboardInput = "";
            prompts.Add(new PromptData { Message = Message, AcceptAction = Accept, DeclineAction = Decline, AcceptText = AcceptButton, DeclineText = DeclineButton, IsText = false });

            if (menu != null && prompts.Count <= 1)
                ReloadMenu();
        }

        












        public static void PromptSingle(string Message, Action Accept = null, string AcceptButton = "Ok")
        {
            Modules.Categories.Settings.DestroyKeyboard();
            keyboardInput = "";
            prompts.Add(new PromptData { Message = Message, AcceptAction = Accept, DeclineAction = null, AcceptText = AcceptButton, DeclineText = null, IsText = false });

            if (menu != null && prompts.Count <= 1)
                ReloadMenu();
        }

        













        public static void PromptText(string Message, Action Accept = null, Action Decline = null, string AcceptButton = "Yes", string DeclineButton = "No")
        {
            Modules.Categories.Settings.DestroyKeyboard();
            keyboardInput = "";
            prompts.Add(new PromptData { Message = Message, AcceptAction = Accept, DeclineAction = Decline, AcceptText = AcceptButton, DeclineText = DeclineButton, IsText = true });

            if (menu != null && prompts.Count <= 1)
                ReloadMenu();
        }

        











        public static void PromptSingleText(string Message, Action Accept = null, string AcceptButton = "Ok")
        {
            Modules.Categories.Settings.DestroyKeyboard();
            keyboardInput = "";
            prompts.Add(new PromptData { Message = Message, AcceptAction = Accept, DeclineAction = null, AcceptText = AcceptButton, DeclineText = null, IsText = true });

            if (menu != null && prompts.Count <= 1)
                ReloadMenu();
        }

        private static string versionArchive;
        public static void UpdatePrompt(string newVersion = null)
        {
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/Notifications/win7-exc.ogg", "Audio/Menu/Notifications/win7-exc.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));

            versionArchive ??= newVersion;
            Prompt($"A new version is available ({versionArchive}). Would you like to update?", Modules.Categories.Settings.UpdateMenu);
        }
        public static VideoPlayer promptVideoPlayer;
        public static Material promptMat;
    }
}
