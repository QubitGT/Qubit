/*
 * Qubit Menu  Frontend/Legacy.cs
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

using HarmonyLib;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Colors;
using Qubit.Libraries.Core;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Modules.Manager;
using ButtonCollider = Qubit.Frontend.Classes.ButtonCollider;
using Random = UnityEngine.Random;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Frontend
{
    public static class Legacy
    {
        public static bool Enabled = true;

        public static GameObject CreateMenu()
        {
            if (clickGUI)
            {
                menu = LoadObject<GameObject>("ClickGUI");
                menu.transform.localScale *= (menuScale * (scaleWithPlayer ? References.Player.scale : 1f));

                Modules.Categories.Settings.InitializeClickGUI();

                RecenterMenu();

                return menu;
            }

            menu = GameObject.CreatePrimitive(PrimitiveType.Cube);

            UnityEngine.Object.Destroy(menu.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(menu.GetComponent<Renderer>());

            menu.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);

            if (annoyingMode)
            {
                menu.transform.localScale = new Vector3(0.1f, Random.Range(10f, 40f) / 100f, 0.3825f);
                backgroundColor = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };

                buttonColors[0] = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };
                buttonColors[1] = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };

                textColors[0] = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };
                textColors[1] = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };
                textColors[2] = new ExtGradient { colors = ExtGradient.GetSimpleGradient(RandomColor(), RandomColor()) };
            }

            /*
            if (themeType == 6)
            {
                GameObject coneBackground = LoadObject<GameObject>("Cone");

                coneBackground.transform.parent = menu.transform;
                coneBackground.transform.localPosition = Vector3.zero;
                coneBackground.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
            */
            menuBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menuBackground.GetComponent<BoxCollider>());

            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.localPosition = new Vector3(0.50f, 0f, 0f);
            menuBackground.transform.rotation = Quaternion.identity;

            // Size is calculated in depth, width, height
            menuBackground.transform.localScale = thinMenu ? new Vector3(0.1f, 1f, 1f) : new Vector3(0.1f, 1.5f, 1f);

            if (innerOutline || themeType == 32)
            {
                GameObject innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, -0.4840625f, 0f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.0065f, 0.98f);

                ObjectColors colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, 0.4840625f, 0f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.0065f, 0.98f);

                colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, 0f, -0.4875f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.968125f, 0.005f);

                colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, 0f, 0.4875f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.968125f, 0.005f);

                colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];
            }

            if (themeType == 23 || themeType == 24 || themeType == 25 || themeType == 61)
            {
                Renderer menuBackgroundRenderer = menuBackground.GetComponent<Renderer>();
                switch (themeType)
                {
                    case 23:
                        if (pride == null)
                        {
                            pride = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Themes/pride.png", "Images/Themes/pride.png");
                            pride.filterMode = FilterMode.Point;
                            pride.wrapMode = TextureWrapMode.Clamp;
                        }
                        menuBackgroundRenderer.material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                        menuBackgroundRenderer.material.color = Color.white;
                        menuBackgroundRenderer.material.mainTexture = pride;
                        break;
                    case 24:
                        if (trans == null)
                        {
                            trans = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Themes/trans.png", "Images/Themes/trans.png");
                            trans.filterMode = FilterMode.Point;
                            trans.wrapMode = TextureWrapMode.Clamp;
                        }
                        menuBackgroundRenderer.material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                        menuBackgroundRenderer.material.color = Color.white;
                        menuBackgroundRenderer.material.mainTexture = trans;
                        break;
                    case 25:
                        if (gay == null)
                        {
                            gay = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Themes/mlm.png", "Images/Themes/mlm.png");
                            gay.filterMode = FilterMode.Point;
                            gay.wrapMode = TextureWrapMode.Clamp;
                        }
                        menuBackgroundRenderer.material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                        menuBackgroundRenderer.material.color = Color.white;
                        menuBackgroundRenderer.material.mainTexture = gay;
                        break;
                    case 61:
                        if (videoPlayer == null)
                        {
                            videoPlayer = new GameObject("Qubit_VideoPlayer").AddComponent<VideoPlayer>();
                            videoPlayer.playOnAwake = true;
                            videoPlayer.isLooping = true;
                            videoPlayer.url = $"{PluginInfo.ServerResourcePath}/Videos/Themes/badapple.mp4";

                            RenderTexture rt = new RenderTexture(192, 144, 0);
                            rt.Create();

                            videoPlayer.targetTexture = rt;
                        }

                        menuBackgroundRenderer.material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                        menuBackgroundRenderer.material.color = Color.white;
                        menuBackgroundRenderer.material.SetTexture("_BaseMap", videoPlayer.targetTexture);

                        videoPlayer.time = badAppleTime;

                        break;
                }
            }
            else
            {
                if (customMenuBackground)
                {
                    menuBackground.GetComponent<Renderer>().material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                    menuBackground.GetComponent<Renderer>().material.color = Color.white;
                    menuBackground.GetComponent<Renderer>().material.mainTexture = customMenuBackgroundImage;
                }
                else
                {
                    ObjectColors colorChanger = menuBackground.AddComponent<ObjectColors>();
                    colorChanger.colors = menuBackgroundColor;
                }
            }

            FollowMenuSettings(menuBackground, false);

            canvasObj = new GameObject();
            canvasObj.transform.parent = menu.transform;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            if (hideTextOnCamera)
                canvasObj.layer = 19;

            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScaler.dynamicPixelsPerUnit = 2500f;

            canvasObj.AddComponent<GraphicRaycaster>();

            if (!hideTitle)
            {
                title = new GameObject
                {
                    transform =
                    {
                        parent = canvasObj.transform
                    }
                }.AddComponent<TextMeshPro>();
                title.font = activeFont;
                title.text = translate ? "Qubit" : "<b>Qubit</b>";

                if (doCustomName)
                    title.text = customMenuName;

                if (annoyingMode)
                {
                    string[] randomMenuNames = {
                    "ModderX",
                    "ShibaGT Gold",
                    "Kman Menu",
                    "WM TROLLING MENU",
                    "ShibaGT Dark",
                    "ShibaGT-X v5.5",
                    "ii stupid",
                    "ii's <b>Stupid</b> Menu",
                    "bvunt menu",
                    "GorillaTaggingKid Menu",
                    "fart",
                    "steal.lol",
                    "Unttile menu"
                };

                    if (Random.Range(1, 5) == 2)
                        title.text = randomMenuNames[Random.Range(0, randomMenuNames.Length)] + " v" + Random.Range(8, 159);
                }

                title.text = Strings.FollowMenuSettings(title.text, !doCustomName, true);

                if (!noPageNumber)
                    title.text += $" {Strings.Bracket("white", ((pageScrolling ? pageOffset : pageNumber) + 1).ToString())}";

                if (gradientTitle)
                    title.text = RichtextGradient(NoRichtextTags(title.text),
                        new[]
                        {
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0)), 0f),
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0), 0.95f), 0.5f),
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0)), 1f)
                        });

                if (animatedTitle)
                {
                    string targetString = doCustomName ? NoRichtextTags(customMenuName) : "Qubit";
                    int length = (int)Mathf.PingPong(Time.time / 0.25f, targetString.Length);
                    title.text = length > 0 ? targetString[..length] : "";
                }

                title.fontSize = 1;
                title.AddComponent<UIColors>().colors = textColors[0];

                title.richText = true;
                title.fontStyle = activeFontStyle;
                title.alignment = TextAlignmentOptions.Center;
                title.enableAutoSizing = true;
                title.fontSizeMin = 0;
                RectTransform component = title.GetComponent<RectTransform>();
                component.localPosition = Vector3.zero;
                component.sizeDelta = new Vector2(0.28f, 0.05f);
                if (noAutoSizeText)
                    component.sizeDelta = new Vector2(0.28f, 0.015f);

                if (hideTextOnCamera)
                    component.gameObject.layer = 19;

                component.localPosition = new Vector3(0.06f, 0f, 0.165f);
                component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                FollowMenuSettings(title);
            }

            if (!backgroundColor.transparent)
            {
                TextMeshPro buildLabel = new GameObject
                {
                    transform =
                    {
                        parent = canvasObj.transform
                    }
                }.AddComponent<TextMeshPro>();
                buildLabel.font = activeFont;

#if LEGAL
                buildLabel.text = $"Build {PluginInfo.Version} Legal";
#elif DEBUG
                buildLabel.text = $"Build {PluginInfo.Version} Debug";
#elif LEGAL_DEBUG
                buildLabel.text = $"Build {PluginInfo.Version} Legal, Debug";
#else
                buildLabel.text = $"Build {PluginInfo.Version}";
#endif

                buildLabel.text = Strings.FollowMenuSettings(buildLabel.text);

                buildLabel.fontSize = 1;
                buildLabel.AddComponent<UIColors>().colors = textColors[0];
                buildLabel.richText = true;
                buildLabel.fontStyle = activeFontStyle;
                buildLabel.alignment = TextAlignmentOptions.Right;
                buildLabel.enableAutoSizing = true;
                buildLabel.fontSizeMin = 0;

                RectTransform component = buildLabel.GetComponent<RectTransform>();
                component.localPosition = Vector3.zero;
                component.sizeDelta = new Vector2(0.28f, 0.02f);
                component.position = thinMenu ? new Vector3(0.04f, 0.0f, -0.17f) : new Vector3(0.04f, 0.07f, -0.17f);
                component.rotation = Quaternion.Euler(new Vector3(0f, 90f, 90f));

                FollowMenuSettings(buildLabel);

                if (!disableWatermark)
                {
                    watermarkImage = new GameObject
                    {
                        transform =
                        {
                            parent = canvasObj.transform
                        }
                    }.AddComponent<Image>();

                    if (watermarkMat == null)
                        watermarkMat = new Material(watermarkImage.material);

                    watermarkImage.material = watermarkMat;
                    watermarkImage.material.SetTexture("_MainTex", customWatermark ?? LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.icon.png"));

                    RectTransform imageTransform = watermarkImage.GetComponent<RectTransform>();
                    imageTransform.localPosition = Vector3.zero;
                    imageTransform.sizeDelta = new Vector2(.15f, .15f);

                    imageTransform.localPosition = new Vector3(0.04f, 0f, 0f);

                    FollowMenuSettings(watermarkImage);

                    imageTransform.localRotation = Quaternion.Euler(new Vector3(0f, 90f, 90f - (rockWatermark ? (Mathf.Sin(Time.time * 2f) * 10f) : 0f)));

                    if (customWatermark == null)
                        watermarkImage.AddComponent<UIColors>().colors = textColors[0];
                    else
                        watermarkImage.material.color = Color.white;
                }
            }

            if (!disableFpsCounter)
            {
                TextMeshPro fps = new GameObject
                {
                    transform =
                    {
                        parent = canvasObj.transform
                    }
                }.AddComponent<TextMeshPro>();
                fps.font = activeFont;

                string textToSet = ftCount ? $"FT: {Mathf.Floor(1f / lastDeltaTime * 10000f) / 10f} ms" : $"FPS: {lastDeltaTime}";
                if (hideTitle && !noPageNumber) textToSet += "      ";
                if (hideTitle && !noPageNumber) textToSet += "Page " + (pageNumber + 1);

                fps.text = Strings.FollowMenuSettings(textToSet, false);

                fps.AddComponent<UIColors>().colors = textColors[0];
                fpsCount = fps;
                fps.fontSize = 1;
                fps.richText = true;
                fps.SafeSetFontStyle(activeFontStyle);
                fps.SafeSetAlignment(TextAlignmentOptions.Center);
                fps.SafeSetOverflowMode(TextOverflowModes.Overflow);
                fps.enableAutoSizing = true;
                fps.fontSizeMin = 0;
                RectTransform fpsTransform = fps.GetComponent<RectTransform>();
                fpsTransform.sizeDelta = noAutoSizeText ? new Vector2(9f, 0.015f) : new Vector2(0.28f, 0.02f);
                fpsTransform.localPosition = new Vector3(0.06f, 0f, hideTitle ? 0.175f : 0.135f);
                fpsTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                if (hideTextOnCamera)
                    fpsTransform.gameObject.layer = 19;

                FollowMenuSettings(fps);
            }

            float hkbStartTime = -0.3f;
            if (!disableDisconnectButton)
            {
                AddButton(-0.3f, -1, Manager.GetIndex("Disconnect"));
                hkbStartTime -= ButtonDistance;
            }

            if (quickActions.Count > 0)
            {
                foreach (string action in quickActions.ToList())
                {
                    Module button = Manager.GetIndex(action);
                    if (button == null)
                    {
                        quickActions.Remove(action);
                        continue;
                    }

                    AddButton(hkbStartTime, -1, button);
                    hkbStartTime -= ButtonDistance;
                }
            }

            // Search button
            if (!disableSearchButton)
            {
                AddSearchButton();
                if (!disableReturnButton && Manager.CurrentCategoryName != "Main")
                    AddReturnButton(true);
            }
            else
            {
                if (!disableReturnButton && Manager.CurrentCategoryName != "Main")
                    AddReturnButton(false);
            }

            if (enableDebugButton)
                AddDebugButton();
            else
            {
                if (ServerData.OutdatedVersion)
                    AddUpdateButton();
            }

            if (!disablePageButtons && CurrentPrompt == null && !pageScrolling)
                AddPageButtons();

            if (inTextInput)
            {
                GameObject searchBoxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UI.IsKeyboardActive())
                    searchBoxObject.layer = 2;

                searchBoxObject.GetComponent<BoxCollider>().isTrigger = true;
                searchBoxObject.transform.parent = menu.transform;
                searchBoxObject.transform.rotation = Quaternion.identity;

                searchBoxObject.transform.localScale = thinMenu ? new Vector3(0.09f, 0.9f, ButtonDistance * 0.8f) : new Vector3(0.09f, 1.3f, ButtonDistance * 0.8f);

                searchBoxObject.transform.localPosition = new Vector3(0.56f, 0f, 0.28f - buttonOffset * ButtonDistance);

                ObjectColors colorChanger = searchBoxObject.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[0];

                FollowMenuSettings(searchBoxObject);

                keyboardInputObject = new GameObject
                {
                    transform =
                        {
                            parent = canvasObj.transform
                        }
                }.AddComponent<TextMeshPro>();

                keyboardInputObject.font = activeFont;
                keyboardInputObject.text = Strings.FollowMenuSettings(keyboardInput, false) + ((Time.time % 1f) > 0.5f ? "|" : "");

                keyboardInputObject.richText = true;
                keyboardInputObject.fontSize = 1;

                if (joystickMenu && joystickButtonSelected == 0 && themeType == 29)
                    keyboardInputObject.color = Color.red;
                else
                    keyboardInputObject.AddComponent<UIColors>().colors = textColors[1];

                keyboardInputObject.alignment = TextAlignmentOptions.Center;
                keyboardInputObject.fontStyle = activeFontStyle;
                keyboardInputObject.enableAutoSizing = true;
                keyboardInputObject.fontSizeMin = 0;

                RectTransform textTransform = keyboardInputObject.GetComponent<RectTransform>();
                textTransform.localPosition = Vector3.zero;
                textTransform.sizeDelta = new Vector2(.2f, .03f * (ButtonDistance / 0.1f));
                if (noAutoSizeText)
                    textTransform.sizeDelta = new Vector2(9f, 0.015f);

                if (hideTextOnCamera)
                    textTransform.gameObject.layer = 19;

                textTransform.localPosition = new Vector3(.064f, 0, .111f - buttonOffset * ButtonDistance / 2.6f);
                textTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

                FollowMenuSettings(keyboardInputObject);
            }

            if (promptVideoPlayer != null)
            {
                promptVideoPlayer.Stop();
                promptVideoPlayer.gameObject.Destroy();
                promptVideoPlayer = null;
            }

            if (CurrentPrompt != null)
                RenderPrompt();
            else
            {
                // Button render code
                int buttonIndexOffset = 0;
                Module[] renderButtons;

                try
                {
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
                                Module[] buttonlist = categoryEntry.Value;
                                foreach (Module v in buttonlist)
                                {
                                    try
                                    {
                                        if (((categoryEntry.Key.Contains("Admin") ||
                                            categoryEntry.Key == "Mod Givers") &&
                                            !isAdmin)
                                            || (v.detected && !allowDetected)
                                            || (v.legacy && !allowLegacy))
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

                        buttonIndexOffset += 1;
                        renderButtons = searchedMods.ToArray();
                    }
                    else if (annoyingMode && Random.Range(1, 5) == 3)
                    {
                        Module disconnectButton = Manager.GetIndex("Disconnect");
                        renderButtons = Enumerable.Repeat(disconnectButton, 15).ToArray();
                    }
                    else switch (Manager.CurrentCategoryName)
                        {
                            case "Main":
                                {
                                    List<Module> buttons = new List<Module>();
                                    foreach (var button in Manager.modules[Manager.CurrentCategoryName])
                                    {
                                        if (!skipButtons.Contains(button.name))
                                            buttons.Add(button);
                                    }
                                    renderButtons = buttons.ToArray();
                                    break;
                                }
                            case "Favorite Mods":
                                {
                                    foreach (var favoriteMod in favorites.Where(favoriteMod => Manager.GetIndex(favoriteMod) == null).ToList())
                                        favorites.Remove(favoriteMod);

                                    renderButtons = StringsToInfos(favorites.ToArray());
                                    break;
                                }
                            case "Enabled Mods":
                                {
                                    List<Module> enabledMods = new List<Module>();
                                    foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                                    {
                                        Module[] buttonList = categoryEntry.Value;
                                        enabledMods.AddRange(buttonList.Where(v => v.enabled && (!hideSettings || !categoryEntry.Key.Contains("Settings")) && (!hideMacros || !categoryEntry.Key.Contains("Macro"))));
                                    }
                                    enabledMods = enabledMods.OrderBy(v => v.name).ToList();
                                    enabledMods.Insert(0, Manager.GetIndex("Exit Enabled Mods"));

                                    renderButtons = enabledMods.ToArray();
                                    break;
                                }
                            default:
                                renderButtons = Manager.modules[Manager.CurrentCategoryName];
                                break;
                        }

#if LEGAL || LEGAL_DEBUG
                    renderButtons = renderButtons.Where(b => b.legal || b.label).ToArray();
#endif

                    if (renderButtons.Length == 0 || (renderButtons.Length == 1 && renderButtons.FirstOrDefault().toolTip.Contains("Returns you back to the")))
                    {
                        renderButtons = renderButtons.AddToArray(
                            new Module
                            {
                                name = "This category seems to be empty.",
                                legal = true,
                                label = true
                            }
                        );
                    }

                    if (Manager.GetIndex("Alphabetize Menu").enabled || isSearching)
                        renderButtons = StringsToInfos(Alphabetize(InfosToStrings(renderButtons)));

                    if (!longMenu)
                        renderButtons = renderButtons
                            .Skip(pageNumber * (PageSize - buttonIndexOffset) + pageOffset)
                            .Take(PageSize - buttonIndexOffset)
                            .ToArray();

                    for (int i = 0; i < renderButtons.Length; i++)
                        AddButton((i + buttonIndexOffset + buttonOffset) * ButtonDistance, i, renderButtons[i]);
                }
                catch (Exception ex)
                {
                    Logs.Log($"Menu draw is erroring, returning to home page\nException: {ex}");
                    Manager.CurrentCategoryName = "Main";
                }
            }

            RecenterMenu();

            if (themeType == 48)
            {
                for (int i = 0; i < 15; i++)
                {
                    GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    particle.transform.position = menuBackground.transform.position;
                    particle.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                    UnityEngine.Object.Destroy(particle.GetComponent<BoxCollider>());
                    UnityEngine.Object.Destroy(particle, 2f);

                    particle.GetComponent<Renderer>().material.shader = Shader.Find("Universal Render Pipeline/Unlit");
                    particle.GetComponent<Renderer>().material.color = Color.white;

                    if (cannabisMat == null)
                    {
                        cannabisMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                        {
                            color = Color.white
                        };

                        if (cann == null)
                            cann = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Themes/cannabis.png", "Images/Themes/cannabis.png");

                        cannabisMat.mainTexture = cann;

                        cannabisMat.SetFloat("_Surface", 1);
                        cannabisMat.SetFloat("_Blend", 0);
                        cannabisMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        cannabisMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        cannabisMat.SetFloat("_ZWrite", 0);
                        cannabisMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        cannabisMat.renderQueue = (int)RenderQueue.Transparent;
                    }
                    particle.GetComponent<Renderer>().material = cannabisMat;

                    Rigidbody comp = particle.AddComponent(typeof(Rigidbody)) as Rigidbody;
                    comp.position = menuBackground.transform.position;
                    comp.linearVelocity = new Vector3(Random.Range(-3f, 3f), Random.Range(3f, 5f), Random.Range(-3f, 3f));
                    comp.angularVelocity = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
                }
            }

            menu.transform.localScale *= scaleWithPlayer && XRSettings.isDeviceActive ? References.Player.scale * menuScale : menuScale;
            return menu;
        }

        public static void AddButton(float offset, int buttonIndex, Module method)
        {
            bool shouldCreate = false;
#if LEGAL || LEGAL_DEBUG
            shouldCreate = method.legal;
#else
            shouldCreate = true;
#endif
            if (!shouldCreate)
                return;
            if (method != null && !method.label)
            {
                GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UI.IsKeyboardActive())
                    buttonObject.layer = 2;

                buttonObject.GetComponent<BoxCollider>().isTrigger = true;
                buttonObject.transform.parent = menu.transform;
                buttonObject.transform.rotation = Quaternion.identity;

                buttonObject.transform.localScale = thinMenu ? new Vector3(0.09f, 0.9f, ButtonDistance * 0.8f) : new Vector3(0.09f, 1.3f, ButtonDistance * 0.8f);

                if (longMenu && buttonIndex >= PageSize)
                {
                    menuBackground.transform.localScale += new Vector3(0f, 0f, 0.1f);
                    menuBackground.transform.localPosition += new Vector3(0f, 0f, -0.05f);
                }

                buttonObject.transform.localPosition = new Vector3(0.56f, 0f, 0.28f - offset);
                if (checkMode && buttonIndex > -1)
                {
                    // The Checkbox Theorem ; TO BE THE SQUARE, YOU MUST circumvent the inconvenient menu localScale parameter
                    // Variable calculations: (menu scale y)0.3825 / (menu scale z)0.3 = 1.275 = Y
                    // 0.08 x Y = 0.102

                    buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
                    buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, 0.399f, 0.28f - offset) : new Vector3(0.56f, 0.599f, 0.28f - offset);
                }

                ButtonCollider Button = buttonObject.AddComponent<ButtonCollider>();
                Button.relatedText = method.name;

                if (incrementalButtons)
                {
                    if (method.incremental)
                    {
                        if (checkMode && buttonIndex > -1)
                        {
                            Button.incremental = true;
                            Button.positive = false;

                            RenderIncrementalText(false, offset);
                            RenderIncrementalButton(true, offset, buttonIndex, method);
                        }
                        else
                        {
                            buttonObject.transform.localScale -= new Vector3(0f, 0.254f, 0f);
                            UnityEngine.Object.Destroy(Button);

                            RenderIncrementalButton(false, offset, buttonIndex, method);
                            RenderIncrementalButton(true, offset, buttonIndex, method);
                        }
                    }
                }

                bool shouldSwap = swapButtonColors && buttonIndex < 0;

                if (lastClickedName != method.name)
                {
                    ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
                    colorChanger.colors = buttonColors[shouldSwap ^ method.enabled ? 1 : 0];

                    if (joystickMenu && buttonIndex == joystickButtonSelected)
                    {
                        joystickSelectedButton = method.name;

                        if (!colorChanger.colors.transparent)
                        {
                            ExtGradient gradient = colorChanger.colors.Clone();
                            gradient.SetColor(0, Color.red);

                            colorChanger.colors = gradient;
                        }
                    }
                }
                else
                    Coroutines.instance.StartCoroutine(ButtonClick(buttonIndex, buttonObject.GetComponent<Renderer>()));

                FollowMenuSettings(buttonObject, shouldSwap ? method.enabled : !method.enabled);
            }
            TextMeshPro buttonText = new GameObject
            {
                transform = { parent = canvasObj.transform }
            }.AddComponent<TextMeshPro>();

            UpdateButtonText updater = buttonText.gameObject.AddComponent<UpdateButtonText>();
            updater.Init(method, buttonIndex, offset);
            updater.UpdateText();
        }

        public static void AddDebugButton()
        {
            bool infoScreenEnabled = Manager.GetIndex("Info Screen").enabled;

            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UI.IsKeyboardActive())
                buttonObject.layer = 2;

            buttonObject.GetComponent<BoxCollider>().isTrigger = true;
            buttonObject.transform.parent = menu.transform;
            buttonObject.transform.rotation = Quaternion.identity;

            buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
            buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, 0.450f, -0.58f) : new Vector3(0.56f, 0.7f, -0.58f);

            buttonObject.AddComponent<ButtonCollider>().relatedText = "Info Screen";

            ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[infoScreenEnabled ^ swapButtonColors ? 0 : 1];

            FollowMenuSettings(buttonObject, infoScreenEnabled ^ swapButtonColors);

            Image debugImage = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<Image>();
            if (debugIcon == null)
                debugIcon = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.debug.png");

            if (debugMat == null)
                debugMat = new Material(debugImage.material);

            debugImage.material = debugMat;
            debugImage.material.SetTexture("_MainTex", debugIcon);
            debugImage.AddComponent<UIColors>().colors = textColors[1];

            RectTransform imageTransform = debugImage.GetComponent<RectTransform>();
            imageTransform.localPosition = Vector3.zero;
            imageTransform.sizeDelta = new Vector2(.03f, .03f);

            imageTransform.localPosition = thinMenu ? new Vector3(.064f, 0.35f / 2.6f, -0.58f / 2.6f) : new Vector3(.064f, 0.54444444444f / 2.6f, -0.58f / 2.6f);

            imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(debugImage);
        }

        public static void AddDonateButton()
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UI.IsKeyboardActive())
                buttonObject.layer = 2;

            buttonObject.GetComponent<BoxCollider>().isTrigger = true;
            buttonObject.transform.parent = menu.transform;
            buttonObject.transform.rotation = Quaternion.identity;

            buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
            buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, 0.450f, -0.58f) : new Vector3(0.56f, 0.7f, -0.58f);

            buttonObject.AddComponent<ButtonCollider>().relatedText = "Donate Button";

            ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[swapButtonColors ? 1 : 0];

            FollowMenuSettings(buttonObject, !swapButtonColors);

            Image donateImage = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<Image>();
            if (donateIcon == null)
                donateIcon = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.donate.png");

            if (donateMat == null)
                donateMat = new Material(donateImage.material);

            donateImage.material = donateMat;
            donateImage.material.SetTexture("_MainTex", donateIcon);
            donateImage.AddComponent<UIColors>().colors = textColors[1];

            RectTransform imageTransform = donateImage.GetComponent<RectTransform>();
            imageTransform.localPosition = Vector3.zero;
            imageTransform.sizeDelta = new Vector2(.03f, .03f);

            imageTransform.localPosition = thinMenu ? new Vector3(.064f, 0.35f / 2.6f, -0.58f / 2.6f) : new Vector3(.064f, 0.54444444444f / 2.6f, -0.58f / 2.6f);

            imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(donateImage);
        }

        public static void AddUpdateButton()
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UI.IsKeyboardActive())
                buttonObject.layer = 2;

            buttonObject.GetComponent<BoxCollider>().isTrigger = true;
            buttonObject.transform.parent = menu.transform;
            buttonObject.transform.rotation = Quaternion.identity;

            buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
            buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, 0.450f, -0.58f) : new Vector3(0.56f, 0.7f, -0.58f);

            buttonObject.AddComponent<ButtonCollider>().relatedText = "Update Button";

            ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[swapButtonColors ? 1 : 0];

            FollowMenuSettings(buttonObject, !swapButtonColors);

            Image updateImage = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<Image>();
            if (updateIcon == null)
                updateIcon = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.update.png");

            if (updateMat == null)
                updateMat = new Material(updateImage.material);

            updateImage.material = updateMat;
            updateImage.material.SetTexture("_MainTex", updateIcon);
            updateImage.AddComponent<UIColors>().colors = textColors[1];

            RectTransform imageTransform = updateImage.GetComponent<RectTransform>();
            imageTransform.localPosition = Vector3.zero;
            imageTransform.sizeDelta = new Vector2(.03f, .03f);

            imageTransform.localPosition = thinMenu ? new Vector3(.064f, 0.35f / 2.6f, -0.58f / 2.6f) : new Vector3(.064f, 0.54444444444f / 2.6f, -0.58f / 2.6f);

            imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(updateImage);
        }

        public static void AddReturnButton(bool offcenteredPosition)
        {
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UI.IsKeyboardActive())
                buttonObject.layer = 2;

            buttonObject.GetComponent<BoxCollider>().isTrigger = true;
            buttonObject.transform.parent = menu.transform;
            buttonObject.transform.rotation = Quaternion.identity;

            buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, 0.08f);
            buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, -0.450f, -0.58f) : new Vector3(0.56f, -0.7f, -0.58f);

            if (offcenteredPosition)
                buttonObject.transform.localPosition += new Vector3(0f, 0.16f, 0f);

            buttonObject.AddComponent<ButtonCollider>().relatedText = "Global Return";

            if (lastClickedName != "Global Return")
            {
                ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
                colorChanger.colors = colorChanger.colors = buttonColors[swapButtonColors ? 1 : 0];
            }
            else
                Coroutines.instance.StartCoroutine(ButtonClick(-99, buttonObject.GetComponent<Renderer>()));

            FollowMenuSettings(buttonObject, !swapButtonColors);

            Image returnImage = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<Image>();

            if (returnIcon == null)
                returnIcon = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.return.png");

            if (returnMat == null)
                returnMat = new Material(returnImage.material);

            returnImage.material = returnMat;
            returnImage.material.SetTexture("_MainTex", returnIcon);
            returnImage.AddComponent<UIColors>().colors = textColors[1];

            RectTransform imageTransform = returnImage.GetComponent<RectTransform>();
            imageTransform.localPosition = Vector3.zero;
            imageTransform.sizeDelta = new Vector2(.03f, .03f);

            imageTransform.localPosition = thinMenu ? new Vector3(.064f, -0.35f / 2.6f, -0.58f / 2.6f) : new Vector3(.064f, -0.54444444444f / 2.6f, -0.58f / 2.6f);

            if (offcenteredPosition)
                imageTransform.localPosition += new Vector3(0f, 0.0475f, 0f);

            imageTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(returnImage);
        }

        public static void RenderIncrementalButton(bool increment, float offset, int buttonIndex, Module method)
        {
            if (!method.label)
            {
                GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UI.IsKeyboardActive())
                    buttonObject.layer = 2;

                buttonObject.GetComponent<BoxCollider>().isTrigger = true;
                buttonObject.transform.parent = menu.transform;
                buttonObject.transform.rotation = Quaternion.identity;

                buttonObject.transform.localScale = new Vector3(0.09f, 0.102f, ButtonDistance * 0.8f);
                buttonObject.transform.localPosition = thinMenu ? new Vector3(0.56f, 0.399f, 0.28f - offset) : new Vector3(0.56f, 0.599f, 0.28f - offset);

                ButtonCollider button = buttonObject.AddComponent<ButtonCollider>();
                button.relatedText = method.name;
                button.incremental = true;
                button.positive = increment;

                if (increment)
                    buttonObject.transform.localPosition = new Vector3(buttonObject.transform.localPosition.x, -buttonObject.transform.localPosition.y, buttonObject.transform.localPosition.z);

                if (lastClickedName != method.name + (increment ? "+" : "-"))
                {
                    ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
                    colorChanger.colors = buttonColors[0];
                }
                else
                    Coroutines.instance.StartCoroutine(ButtonClick(buttonIndex, buttonObject.GetComponent<Renderer>()));

                FollowMenuSettings(buttonObject);
            }

            RenderIncrementalText(increment, offset);
        }

        public static void RenderIncrementalText(bool increment, float offset)
        {
            TextMeshPro buttonText = new GameObject
            {
                transform =
                {
                    parent = canvasObj.transform
                }
            }.AddComponent<TextMeshPro>();

            buttonText.font = activeFont;
            buttonText.text = increment ? "+" : "-";
            buttonText.richText = true;
            buttonText.fontSize = 1;
            buttonText.AddComponent<UIColors>().colors = textColors[1];

            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = activeFontStyle;
            buttonText.enableAutoSizing = true;
            buttonText.fontSizeMin = 0;

            RectTransform textTransform = buttonText.GetComponent<RectTransform>();
            textTransform.localPosition = Vector3.zero;
            textTransform.sizeDelta = new Vector2(.2f, .03f * (ButtonDistance / 0.1f));
            if (noAutoSizeText)
                textTransform.sizeDelta = new Vector2(9f, 0.015f);

            if (hideTextOnCamera)
                textTransform.gameObject.layer = 19;

            textTransform.localPosition = thinMenu ? new Vector3(.064f, increment ? -0.12f : 0.12f, .111f - offset / 2.6f) : new Vector3(.064f, increment ? -0.18f : 0.18f, .111f - offset / 2.6f);
            textTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(buttonText);
        }

        public static void AddPageButtons()
        {
            ExtGradient Gradient = buttonColors[swapButtonColors ? 1 : 0];

            switch (pageButtonType)
            {
                case 1:
                    CreatePageButtonPair(
                        "PreviousPage", "NextPage",
                        new Vector3(0.09f, 0.2f, 0.9f),
                        new Vector3(0.56f, thinMenu ? 0.65f : 0.9f, 0f),
                        new Vector3(0.56f, thinMenu ? -0.65f : -0.9f, 0f),
                        new Vector3(0.064f, thinMenu ? 0.195f : 0.267f, 0f),
                        new Vector3(0.064f, thinMenu ? -0.195f : -0.267f, 0f),
                        Gradient
                    );
                    break;

                case 2:
                    CreatePageButtonPair(
                        "PreviousPage", "NextPage",
                        new Vector3(0.09f, thinMenu ? 0.9f : 1.3f, ButtonDistance * 0.8f),
                        new Vector3(0.56f, 0f, 0.28f - ButtonDistance * (buttonOffset - 2)),
                        new Vector3(0.56f, 0f, 0.28f - ButtonDistance * (buttonOffset - 1)),
                        new Vector3(0.064f, 0f, 0.109f - ButtonDistance * (buttonOffset - 2) / 2.55f),
                        new Vector3(0.064f, 0f, 0.109f - ButtonDistance * (buttonOffset - 1) / 2.55f),
                        Gradient
                    );
                    break;

                case 5:
                    CreatePageButtonPair(
                        "PreviousPage", "NextPage",
                        new Vector3(0.09f, hideTitle ? 0.1f : 0.3f, 0.05f),
                        new Vector3(0.56f, (thinMenu ? 0.299f : 0.499f) + (hideTitle ? 0.1f : 0f), 0.355f + (hideTitle ? 0.1f : 0f)),
                        new Vector3(0.56f, (thinMenu ? -0.299f : -0.499f) - (hideTitle ? 0.1f : 0f), 0.355f + (hideTitle ? 0.1f : 0f)),
                        new Vector3(0.064f, (thinMenu ? 0.09f : 0.15f) + (hideTitle ? 0.035f : 0f), 0.135f + (hideTitle ? 0.0375f : 0f)),
                        new Vector3(0.064f, (thinMenu ? -0.09f : -0.15f) - (hideTitle ? 0.035f : 0f), 0.135f + (hideTitle ? 0.0375f : 0f)),
                        Gradient
                    );
                    break;

                case 6:
                    CreatePageButtonPair(
                        "PreviousPage", "NextPage",
                        new Vector3(0.09f, 0.102f, 0.08f),
                        new Vector3(0.56f, thinMenu ? 0.450f : 0.7f, -0.58f),
                        new Vector3(0.56f, thinMenu ? 0.450f : 0.7f, -0.58f) - new Vector3(0f, 0.16f, 0f),
                        new Vector3(0.064f, thinMenu ? 0.35f / 2.6f : 0.54444444444f / 2.6f, -0.58f / 2.7f),
                        new Vector3(0.064f, thinMenu ? 0.35f / 2.6f : 0.54444444444f / 2.6f, -0.58f / 2.7f) - new Vector3(0f, 0.0475f, 0f),
                        Gradient,
                        new Vector2(0.03f, 0.03f)
                    );
                    break;
            }
        }

        public static void CreatePageButtonPair(string prevButtonName, string nextButtonName, Vector3 buttonScale, Vector3 prevButtonPos, Vector3 nextButtonPos, Vector3 prevTextPos, Vector3 nextTextPos, ExtGradient color, Vector2? textSize = null)
        {
            AdvancedAddButton(prevButtonName, buttonScale, prevButtonPos, prevTextPos, color, textSize, 0);
            AdvancedAddButton(nextButtonName, buttonScale, nextButtonPos, nextTextPos, color, textSize, 1);
        }

        public static GameObject AdvancedAddButton(string buttonName, Vector3 scale, Vector3 position, Vector3 textPosition, ExtGradient color, Vector2? textSize, int arrowIndex)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (!UI.IsKeyboardHeld())
                button.layer = 2;

            button.GetComponent<BoxCollider>().isTrigger = true;
            button.transform.parent = menu.transform;
            button.transform.rotation = Quaternion.identity;
            button.transform.localScale = scale;
            button.transform.localPosition = position;

            button.AddComponent<ButtonCollider>().relatedText = buttonName;

            if (lastClickedName != buttonName)
            {
                ObjectColors colorChanger = button.AddComponent<ObjectColors>();
                colorChanger.colors = color;
            }
            else
                Coroutines.instance.StartCoroutine(ButtonClick(-99, button.GetComponent<Renderer>()));

            TextMeshPro text = new GameObject { transform = { parent = canvasObj.transform } }.AddComponent<TextMeshPro>();
            text.font = activeFont;
            text.text = arrowTypes[arrowType][arrowIndex];
            text.fontSize = 1;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.spriteAsset = ButtonSpriteSheet;

            text.AddComponent<UIColors>().colors = textColors[1];

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.sizeDelta = textSize ?? new Vector2(0.2f, 0.03f);

            if (arrowType == 11)
                textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, textRect.sizeDelta.y * 6f);

            if (noAutoSizeText)
                textRect.sizeDelta = new Vector2(9f, 0.015f);

            if (hideTextOnCamera)
                textRect.gameObject.layer = 19;

            textRect.localPosition = textPosition;
            textRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(text);
            FollowMenuSettings(button, !swapButtonColors);

            return button;
        }
        public static Material updateMat;
        public static Material returnMat;
        public static Material debugMat;
        public static Material donateMat;

        public static Material cannabisMat;
        public static Texture2D cann;
        public static Texture2D pride;
        public static Texture2D trans;
        public static Texture2D gay;
        public static Texture2D returnIcon;
        public static Texture2D debugIcon;
        public static Texture2D donateIcon;
        public static Texture2D updateIcon;
    }
}
