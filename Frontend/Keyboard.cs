/*
 * Qubit Menu  Frontend/Keyboard.cs
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

using GorillaExtensions;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Classes;
using Qubit.Frontend.Colors;
using Qubit.Frontend.Resources;
using Qubit.Libraries.Controls;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Modules.Manager;
using ButtonCollider = Qubit.Frontend.Classes.ButtonCollider;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Frontend
{
    public static class Keyboard
    {

        public static List<Key> lastPressedKeys = new List<Key>();
        public static readonly Dictionary<Key, (float, float)> keyPressedTimes = new Dictionary<Key, (float, float)>();
        public static readonly Key[] detectedKeys = {
            Key.A, Key.B, Key.C, Key.D, Key.E,
            Key.F, Key.G, Key.H, Key.I, Key.J,
            Key.K, Key.L, Key.M, Key.N, Key.O,
            Key.P, Key.Q, Key.R, Key.S, Key.T,
            Key.U, Key.V, Key.W, Key.X, Key.Y,
            Key.Z,

            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3,
            Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7,
            Key.Digit8, Key.Digit9,

            Key.Comma, Key.Period, Key.Slash, Key.Backslash,
            Key.Minus, Key.Equals,Key.Semicolon, Key.Quote,
            Key.LeftBracket, Key.RightBracket,

            Key.Space, Key.Backspace, Key.Enter, Key.Escape
        };

        public static void UpdateKeyboard()
        {
            if (VRKeyboard != null)
            {
                if (Vector3.Distance(VRKeyboard.transform.position, References.BodyCollider.transform.position) > menuScale && !leftSecondary)
                {
                    VRKeyboard.transform.position = References.BodyCollider.transform.position;
                    VRKeyboard.transform.rotation = References.BodyCollider.transform.rotation;
                }
            }

            if (!inTextInput || !isKeyboardPc)
            {
                lastPressedKeys.Clear();
                foreach (Key key in detectedKeys)
                {
                    if (UnityInput.GetKey(key))
                    {
                        lastPressedKeys.Add(key);
                    }
                }
                keyPressedTimes.Clear();
                return;
            }

            List<Key> keysPressed = new List<Key>();
            foreach (Key key in detectedKeys)
            {
                if (UnityInput.GetKey(key))
                {
                    bool isRepeatFire = false;
                    if (keyPressedTimes.TryGetValue(key, out (float, float) delay))
                    {
                        float newDelay = Mathf.Max(delay.Item2 * 0.75f, 0.05f);

                        if (Time.time > delay.Item1)
                        {
                            keyPressedTimes[key] = (Time.time + newDelay, newDelay);
                            isRepeatFire = true;
                        }
                        else
                        {
                            keysPressed.Add(key);
                            continue;
                        }
                    }
                    else
                        keyPressedTimes[key] = (Time.time + 0.5f, 0.5f);

                    keysPressed.Add(key);

                    // Backspace repeats while held (initial 0.5s delay, then decaying interval down to 0.05s); other keys keep edge-trigger behavior.
                    if (lastPressedKeys.Contains(key) && !(isRepeatFire && key == Key.Backspace)) continue;

                    if (UnityInput.GetKey(Key.LeftCtrl))
                    {
                        switch (key)
                        {
                            case Key.A:
                                keyboardInput = "";
                                break;
                            case Key.C:
                                GUIUtility.systemCopyBuffer = keyboardInput;
                                break;
                            case Key.V:
                                keyboardInput += GUIUtility.systemCopyBuffer;
                                break;
                            case Key.Backspace:
                                if (!string.IsNullOrEmpty(keyboardInput))
                                    keyboardInput = keyboardInput[..^1];
                                break;
                        }
                    }
                    else
                    {
                        switch (key)
                        {
                            case Key.Backspace:
                                if (!string.IsNullOrEmpty(keyboardInput))
                                    keyboardInput = keyboardInput[..^1];
                                break;
                            case Key.Escape:
                                Toggle(CurrentPrompt != null ? "Decline Prompt" : "Search");
                                break;
                            case Key.Enter:
                                HandleSearchOrPrompt();
                                break;
                            case Key.Space:
                                keyboardInput += " ";
                                break;
                            default:
                                string keyName = key.ToString();
                                if (keyName.Length == 1)
                                {
                                    bool isShift = UnityInput.GetKey(Key.LeftShift) || UnityInput.GetKey(Key.RightShift);
                                    keyboardInput += isShift ? keyName.ToUpper() : keyName.ToLower();
                                }
                                else if (keyName.StartsWith("Digit"))
                                {
                                    keyboardInput += keyName.Replace("Digit", "");
                                }
                                break;
                        }
                    }

                    if (pcKeyboardSounds)
                        References.LocalRig.PlayHandTapLocal(66, false, buttonClickVolume / 10f);

                    pageNumber = 0;

                    if (!clickGUI)
                        ReloadMenu();
                    else
                        Modules.Categories.Settings.UpdateSearch();
                }
                else
                {
                    keyPressedTimes.Remove(key);
                }
            }

            lastPressedKeys = keysPressed;
        }
        private static void HandleSearchOrPrompt()
        {
            if (CurrentPrompt != null)
            {
                if (CurrentPrompt.IsText) Toggle("Accept Prompt");
                return;
            }
            if (isSearching)
            {
                List<Module> searchedMods = new List<Module>();
                if (nonGlobalSearch && Manager.CurrentCategoryName != "Main")
                {
                    foreach (Module v in Manager.modules[Manager.CurrentCategoryName])
                    {
                        try
                        {
                            List<string> texts = v.aliases == null ? new List<string>() : v.aliases.ToList();
                            texts.Add(v.RenderName);

                            if (texts.Any(buttonText => buttonText.ClearTags().Replace(" ", "").ToLower().Contains(keyboardInput.Replace(" ", "").ToLower())))
                                searchedMods.Add(v);
                        }
                        catch { }
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                    {
                        Module[] buttonInfos = categoryEntry.Value;
                        foreach (Module v in buttonInfos)
                        {
                            try
                            {
                                if (((categoryEntry.Key.Contains("Admin") ||
                                     categoryEntry.Key == "Mod Givers") && !isAdmin) || (v.detected && !allowDetected) || (v.legacy && !allowLegacy))
                                    continue;

                                List<string> texts = v.aliases == null ? new List<string>() : v.aliases.ToList();
                                texts.Add(v.RenderName);

                                if (texts.Any(buttonText => buttonText.ClearTags().Replace(" ", "").ToLower().Contains(keyboardInput.Replace(" ", "").ToLower())))
                                    searchedMods.Add(v);
                            }
                            catch { }
                        }
                    }
                }

                if (searchedMods.Count > 0)
                {
                    Module[] buttons = StringsToInfos(Alphabetize(InfosToStrings(searchedMods.ToArray())));
                    Module button = buttons[0];

                    if (button.incremental)
                        ToggleIncremental(button.name, UnityInput.GetKey(Key.LeftShift));
                    else
                        Toggle(buttons[0].name, true);
                }
            }
            else if (CurrentPrompt != null && CurrentPrompt.IsText)
            {
                Toggle("Accept Prompt");
            }
        }

        public static void PressKeyboardKey(string key)
        {
            switch (key)
            {
                case "Enter":
                    HandleSearchOrPrompt();
                    return;

                case "Escape":
                    if (CurrentPrompt != null) Toggle("Decline Prompt");
                    else if (isSearching) Toggle("Search");
                    else { Modules.Categories.Settings.DestroyKeyboard(); ReloadMenu(); }
                    return;
                case "Space":
                    keyboardInput += " ";
                    break;
                case "Backspace":
                    if (!string.IsNullOrEmpty(keyboardInput))
                        keyboardInput = keyboardInput[..^1];
                    break;
                case "Shift":
                    shift = !shift;
                    break;
                case "CapsLock":
                    lockShift = !lockShift;

                    break;
                case "Clear":
                    keyboardInput = "";
                    break;
                case "Copy":
                    GUIUtility.systemCopyBuffer = keyboardInput;
                    break;
                case "Paste":
                    keyboardInput += GUIUtility.systemCopyBuffer;
                    break;
                default:
                    Dictionary<string, string> shiftMap = new Dictionary<string, string>
       {
  { "1", "!" }, { "2", "@" }, { "3", "#" }, { "4", "$" }, { "5", "%" },
                  { "6", "^" }, { "7", "&" }, { "8", "*" }, { "9", "(" }, { "0", ")" },
    { "-", "_" }, { "=", "+" }, { "[", "{" }, { "]", "}" }, { "\\", "|" },
 { ";", ":" }, { "'", "\"" }, { ",", "<" }, { ".", ">" }, { "/", "?" },

            { "`", "~" }
   };
                    bool isShifted = key.Length == 1 && char.IsLetter(key[0]) ? lockShift ^ shift : shift;
                    string keyStr = key.ToLower();
                    if (isShifted)
                    {
                        if (shiftMap.TryGetValue(keyStr, out var value))
                            keyboardInput += value;
                        else
                            keyboardInput += keyStr.ToUpper();
                    }
                    else
                        keyboardInput += keyStr.ToLower();
                    shift = false;
                    break;
            }
            var qubitKeyboard = VRKeyboard != null ? VRKeyboard.GetComponent<View>() : null;

            if (qubitKeyboard != null) qubitKeyboard.RefreshKeys();
            else
            {
                if (KeyboardKey.keyLookupDictionary.TryGetValue("CapsLock", out var caps) && caps != null)
                    caps.gameObject.GetOrAddComponent<ObjectColors>().colors = buttonColors[lockShift ? 1 : 0];
                if (KeyboardKey.keyLookupDictionary.TryGetValue("Shift", out var shiftKey) && shiftKey != null)
                    shiftKey.gameObject.GetOrAddComponent<ObjectColors>().colors = buttonColors[shift ? 1 : 0];
            }
            pageNumber = 0;
            if (!clickGUI)
                ReloadMenu();
        }

        public static void AddSearchButton()
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UnityInput.GetKey(Settings.KeyboardBind) && !isKeyboardPc)
                buttonObject.layer = 2;

            buttonObject.GetComponent<BoxCollider>().isTrigger = true;
            buttonObject.transform.parent = menu.transform;
            buttonObject.transform.rotation = Quaternion.identity;

            buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
            buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, -0.450f, -0.58f) : new Vector3(0.56f, -0.7f, -0.58f);

            buttonObject.AddComponent<ButtonCollider>().relatedText = "Search";

            ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[isSearching ^ !swapButtonColors ? 0 : 1];

            if (joystickMenuSearching && joystickButtonSelected == PageSize)
            {
                joystickSelectedButton = "Search";

                ExtGradient gradient = colorChanger.colors.Clone();
                gradient.SetColor(0, Color.red);

                colorChanger.colors = gradient;
            }

            FollowMenuSettings(buttonObject, isSearching ^ !swapButtonColors);

            Image searchImage = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<Image>();
            if (searchIcon == null)
                searchIcon = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.search.png");

            if (searchMat == null)
                searchMat = new Material(searchImage.material);

            searchImage.material = searchMat;
            searchImage.material.SetTexture("_MainTex", searchIcon);
            searchImage.AddComponent<UIColors>().colors = textColors[isSearching ? 2 : 1];

            RectTransform imageTransform = searchImage.GetComponent<RectTransform>();
            imageTransform.localPosition = Vector3.zero;
            imageTransform.sizeDelta = new Vector2(.03f, .03f);

            imageTransform.localPosition = thinMenu ? new Vector3(.064f, -0.35f / 2.6f, -0.58f / 2.6f) : new Vector3(.064f, -0.54444444444f / 2.6f, -0.58f / 2.6f);

            imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(searchImage);
        }

        public static bool pcKeyboardSounds = true;
        public static bool keyboardWithToggleButton;
        public static int characterDistance;
        public static bool joystickMenuSearching;
        public static bool disableSearchButton;

        public static bool isSearching;
        public static bool nonGlobalSearch;
        public static bool isKeyboardPc;
        public static bool inTextInput;
        public static string keyboardInput = "";
        public static TextMeshPro keyboardInputObject;
        public static Material searchMat;

        public static GameObject lKeyReference;
        public static SphereCollider lKeyCollider;

        public static GameObject rKeyReference;
        public static SphereCollider rKeyCollider;

        public static GameObject VRKeyboard;
        public static Texture2D searchIcon;

        public static bool shift;
        public static bool lockShift;

        public static bool lowercaseMode;
        public static bool uppercaseMode;

        public static string inputTextColor = "green";

        public sealed class View : MonoBehaviour
        {

            private static GameObject prefab;
            private Canvas face;
            private TMP_Text input;
            private GameObject placeholder;
            private Transform keys;
            private string lastText;
            private bool lastShift, lastCaps;
            private string pressed;
            private float pressedUntil;
            public static GameObject Create()
            {
                if (prefab == null) prefab = QubitAssets.Load<GameObject>("QubitKeyboard");
                var instance = Instantiate(prefab);
                instance.name = "Qubit Keyboard";
                var body = instance.AddComponent<Rigidbody>();
                body.isKinematic = true;

                body.useGravity = false;
                instance.transform.position = References.BodyCollider.transform.position;
                instance.transform.rotation = References.BodyCollider.transform.rotation;
                instance.transform.localScale *= scaleWithPlayer ? References.Player.scale * menuScale : menuScale;
                var view = instance.AddComponent<View>();
                view.face = instance.transform.Find("Canvas").GetComponent<Canvas>();
                view.face.renderMode = RenderMode.WorldSpace;
                view.face.worldCamera = null;
                view.face.transform.localPosition = new Vector3(0, .1f, 1.1f);
                view.face.transform.localRotation = Quaternion.Euler(35f, 0, 0);
                view.keys = view.face.transform.Find("Keys");
                view.input = view.face.transform.Find("InputBar/InputText").GetComponent<TMP_Text>();
                view.input.richText = false;
                view.placeholder = view.face.transform.Find("InputBar/Placeholder").gameObject;
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 0;
                foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true)) if (graphic is Image) graphic.material = null;
                Fonts.Apply(instance);
                KeyboardKey.keyLookupDictionary.Clear();

                foreach (Transform key in view.keys)
                {
                    var handler = key.gameObject.AddComponent<KeyboardKey>();
                    handler.key = key.name;
                    handler.qubitKeyboard = view;
                    KeyboardKey.keyLookupDictionary[key.name] = handler;
                    key.gameObject.layer = 2;
                    key.GetComponent<Button>().transition = Selectable.Transition.None;
                    key.GetComponent<Image>().color = Color.white;
                }
                menuSpawnPosition = instance.transform.Find("MenuSpawnPosition").gameObject;
                menuSpawnPosition.transform.localPosition = new Vector3(0, .68f, 1.1f);
                menuSpawnPosition.transform.localRotation = Quaternion.identity;
                view.RefreshKeys();
                return instance;
            }
            public bool CanTouch(Collider pointer)

            {
                return VRKeyboard == gameObject && inTextInput &&
           Vector3.Dot(pointer.bounds.center - face.transform.position, -face.transform.forward) > 0;
            }
            public void Flash(string key)
            {
                pressed = key;
                pressedUntil = Time.unscaledTime + .13f;
                RefreshKeys();
            }
            public void RefreshKeys()
            {
                foreach (Transform key in keys)
                {
                    bool selected = key.name == "CapsLock" && lockShift || key.name == "Shift" && shift;
                    bool down = key.name == pressed && Time.unscaledTime < pressedUntil;

                    key.GetComponent<Image>().color = down ? new Color(.78f, .64f, 1f) : selected ? new Color(.65f, .49f, .96f) : Color.white;
                    if (key.name.Length == 1)
                    {
                        string value = key.name;
                        const string unshifted = "1234567890-=[]\\;',./";
                        const string shifted = "!@#$%^&*()_+{}|:\"<>?";
                        int index = unshifted.IndexOf(value, StringComparison.Ordinal);
                        key.Find("Label").GetComponent<TMP_Text>().text = index >= 0 && shift ? shifted[index].ToString() :
                               char.IsLetter(value[0]) && !(shift ^ lockShift) ? value.ToLowerInvariant() : value.ToUpperInvariant();
                    }
                }
                lastShift = shift;
                lastCaps = lockShift;
            }
            private void LateUpdate()
            {

                face.enabled = Vector3.Dot(References.HeadCollider.transform.position - face.transform.position, -face.transform.forward) >= 0;
                if (lastText != keyboardInput)
                {
                    lastText = keyboardInput;
                    input.text = lastText ?? "";
                    placeholder.SetActive(string.IsNullOrEmpty(lastText));
                }
                if (lastShift != shift || lastCaps != lockShift || pressed != null && Time.unscaledTime >= pressedUntil)
                {
                    if (Time.unscaledTime >= pressedUntil) pressed = null;
                    RefreshKeys();
                }
            }
            private void OnDestroy()
            {
                if (keys == null) return;
                foreach (Transform key in keys)

                    if (KeyboardKey.keyLookupDictionary.TryGetValue(key.name, out var registered) && registered == key.GetComponent<KeyboardKey>())
                        KeyboardKey.keyLookupDictionary.Remove(key.name);
            }
        }
    }
}
