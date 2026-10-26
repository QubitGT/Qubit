/*
 * Qubit Menu  Frontend/Core.cs
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
using HarmonyLib;
using Photon.Pun;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Colors;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Interface;
using Qubit.Modules;
using Qubit.Modules.Categories;
using Qubit.Modules.Classes;
using Qubit.Patches.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;
using UnityEngine.XR;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Classes.QubitMenuBody;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Overpowered;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
using Audio = Qubit.Modules.Categories.Sound.Audio;
using Console = Qubit.Backend.Console;
using Random = UnityEngine.Random;
using Soundboard = Qubit.Modules.Categories.Sound.Soundboard;

namespace Qubit.Frontend
{
    public class Core : MonoBehaviour
    {
        public static bool UpdateFrame()
        {
            #region Controls
            try
            {
                Controllers.Poll();

                if (adaptiveButtons)
                {
                    switch (Controllers.GetLeftControllerType())
                    {
                        case Controllers.ControllerType.ValveIndex:
                            leftGrab = References.InputPoller.leftControllerGripFloat > 0.75f;
                            break;
                        case Controllers.ControllerType.VIVE:
                            leftPrimary = leftJoystickClick;
                            break;
                    }

                    switch (Controllers.GetRightControllerType())
                    {
                        case Controllers.ControllerType.ValveIndex:
                            rightGrab = References.InputPoller.rightControllerGripFloat > 0.75f;
                            break;
                        case Controllers.ControllerType.VIVE:
                            rightPrimary = rightJoystickClick;
                            break;
                    }
                }

                shouldBePC = !XRSettings.isDeviceActive;
            }
            catch { }
            #endregion

            try
            {
                #region Menu Spawn Condition
                bool isKeyboardCondition = UI.IsKeyboardCondition();
                bool buttonCondition = VR.GetMenuButtonCondition();

                if (toggleButton)
                {
                    if ((buttonCondition || isKeyboardCondition) && !toggleButtonHeld)
                    {
                        toggleButtonActive = !toggleButtonActive;
                        keyboardWithToggleButton = isKeyboardCondition;
                    }

                    toggleButtonHeld = buttonCondition || isKeyboardCondition;
                    buttonCondition = toggleButtonActive;
                    isKeyboardCondition = toggleButtonActive && keyboardWithToggleButton;
                }

                buttonCondition |= isKeyboardCondition;
                buttonCondition |= inTextInput;
                buttonCondition &= !Lockdown;

                if (watchMenu)
                    buttonCondition = isKeyboardCondition;

                if (barkMenu)
                    buttonCondition = isKeyboardCondition || barkMenuOpen;

                isMenuButtonHeld = buttonCondition;
                switch (buttonCondition)
                {
                    case true when menu == null:
                        OpenMenu();
                        break;
                    case false when menu != null:
                        CloseMenu();
                        break;
                }

                if (buttonCondition && menu != null)
                    RecenterMenu();
                #endregion

                #region Get Camera
                try
                {
                    if (TPC == null)
                    {
                        try
                        {
                            TPC = GetObject("Player Objects/Third Person Camera/Shoulder Camera").GetComponent<Camera>();
                        }
                        catch
                        {
                            TPC = GetObject("Shoulder Camera").GetComponent<Camera>();
                        }
                    }
                }
                catch { }
                #endregion

                #region Menu Animations
                if (fpsCountAverage)
                {
                    fpsAverageNumber *= 99f;
                    fpsAverageNumber += 1f / Time.unscaledDeltaTime;
                    fpsAverageNumber /= 100f;
                }
                else
                    fpsAverageNumber = 1f / Time.unscaledDeltaTime;

                if (Time.time > fpsAvgTime || !fpsCountTimed)
                {
                    lastDeltaTime = Mathf.Ceil(fpsAverageNumber);
                    fpsAvgTime = Time.time + 1f;
                }

                UpdateDisplay();
                if (fpsCount != null)
                {
                    string textToSet = ftCount ? $"FT: {Mathf.Floor(1f / lastDeltaTime * 10000f) / 10f} ms" : $"FPS: {lastDeltaTime}";
                    if (hideTitle && !noPageNumber) textToSet += "      ";
                    if (disableFpsCounter) textToSet = "";
                    if (hideTitle && !noPageNumber) textToSet += "Page " + (pageNumber + 1);

                    fpsCount.text = Strings.FollowMenuSettings(textToSet, false);
                }

                if (potatoTime != null)
                {
                    if (1f / Time.unscaledDeltaTime < 15f)
                    {
                        potatoTime += Time.unscaledDeltaTime;
                        if (potatoTime > 60f)
                        {
                            potatoTime = null;
                            Achievements.Unlock(new Achievements.Achievement
                            {
                                name = "Potato",
                                description = "Have 15 FPS for over a minute.",
                                icon = "Images/Achievements/potato.png"
                            });
                        }
                    }
                    else
                        potatoTime = 0f;
                }

                if (adminTime != null && Networking.InRoom)
                {
                    if (PhotonNetwork.PlayerListOthers.Any(player => ServerData.Administrators.ContainsKey(player.UserId) && !Console.excludedCones.Contains(player)))
                    {
                        adminTime += Time.unscaledDeltaTime;
                        if (adminTime > 10f)
                        {
                            adminTime = null;
                            Achievements.Unlock(new Achievements.Achievement
                            {
                                name = "EEEEKK!",
                                description = "Be in the same room as a Console administrator.",
                                icon = "Images/Achievements/eeeekk.png"
                            });
                        }
                    }
                    else
                        adminTime = 0f;
                }

                if (watermarkImage != null)
                    watermarkImage.GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0f, 90f, 90f - (rockWatermark ? (Mathf.Sin(Time.time * 2f) * 10f) : 0f)));

                if (animatedTitle && title != null)
                {
                    string targetString = doCustomName ? NoRichtextTags(customMenuName) : "Qubit";
                    int length = (int)Mathf.PingPong(Time.time / 0.25f, targetString.Length + 1);
                    title.text = length > 0 ? targetString[..length] : "";
                }

                if (gradientTitle && title != null)
                    title.text = RichtextGradient(NoRichtextTags(title.text),
                        new[]
                        {
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0)), 0f),
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0), 0.95f), 0.5f),
                            new GradientColorKey(BrightenColor(buttonColors[0].GetColor(0)), 1f)
                        });

                if (keyboardInputObject != null)
                    keyboardInputObject.text = Strings.FollowMenuSettings(keyboardInput, false) + (Time.frameCount / 45 % 2 == 0 ? "|" : " ");
                #endregion

                #region Menu Features

                Watches.ForEach(watch => watch.Update());

                // Load projectiles
                ThrowableProjectiles.LoadProjectileCache();

                if (disorganized && Manager.CurrentCategoryName != "Main")
                {
                    Manager.CurrentCategoryName = "Main";
                    ReloadMenu();
                }

                // Long Menu has no layout implementation for the Modern (Qubit) menu body; forcing a reload
                // every frame here while Modern's own pagination kept pageNumber non-zero softlocked the menu.
                if (longMenu && !UseQubit && pageNumber != 0)
                {
                    pageNumber = 0;
                    ReloadMenu();
                }

                VR.UpdatePhysicalReference();

                if (themeType == 61)
                {
                    if (menu != null)
                    {
                        badAppleTime += Time.deltaTime;
                        badAppleTime %= 203f;
                    }

                    if (videoPlayer != null)
                    {
                        if (videoPlayer.isPlaying && menu == null)
                            videoPlayer.Stop();

                        if (!videoPlayer.isPlaying && menu != null)
                            videoPlayer.Play();
                    }
                }
                else
                {
                    if (videoPlayer != null)
                    {
                        Destroy(videoPlayer.gameObject);
                        videoPlayer = null;
                    }
                }

                try
                {
                    if (GunSounds)
                    {
                        if (GunSpawned)
                        {
                            if (!lastGunSpawned)
                            {
                                AudioSource audioSource = SwapGunHand ? References.LocalRig.leftHandPlayer : References.LocalRig.rightHandPlayer;
                                audioSource.volume = buttonClickVolume / 10f;
                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/grip-press.ogg", "Audio/Guns/grip-press.ogg", clip => audioSource.PlayOneShot(clip));
                            }

                            if (GetGunInput(true) && (!lastGunTrigger || (handAudioManager != null && !handAudioManager.GetComponent<AudioSource>().isPlaying)))
                            {
                                AudioSource audioSource = SwapGunHand ? References.LocalRig.leftHandPlayer : References.LocalRig.rightHandPlayer;
                                audioSource.volume = buttonClickVolume / 10f;
                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/trigger-press.ogg", "Audio/Guns/trigger-press.ogg", clip => audioSource.PlayOneShot(clip));

                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/trigger-hold.ogg", "Audio/Guns/trigger-hold.ogg", clip => PlayHandAudio(clip, buttonClickVolume / 10f, SwapGunHand));
                            }

                            if (!GetGunInput(true) && lastGunTrigger)
                            {
                                AudioSource audioSource = SwapGunHand ? References.LocalRig.leftHandPlayer : References.LocalRig.rightHandPlayer;
                                audioSource.volume = buttonClickVolume / 10f;
                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/trigger-release.ogg", "Audio/Guns/trigger-release.ogg", clip => audioSource.PlayOneShot(clip));

                                handAudioManager?.GetComponent<AudioSource>().Stop();
                            }
                        }
                        else
                        {
                            if (lastGunSpawned)
                            {
                                AudioSource audioSource = SwapGunHand ? References.LocalRig.leftHandPlayer : References.LocalRig.rightHandPlayer;
                                audioSource.volume = buttonClickVolume / 10f;
                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/grip-release.ogg", "Audio/Guns/grip-release.ogg", clip => audioSource.PlayOneShot(clip));
                            }

                            if (handAudioManager != null && handAudioManager.GetComponent<AudioSource>().isPlaying)
                            {
                                handAudioManager.GetComponent<AudioSource>().Stop();

                                AudioSource audioSource = SwapGunHand ? References.LocalRig.leftHandPlayer : References.LocalRig.rightHandPlayer;
                                audioSource.volume = buttonClickVolume / 10f;
                                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Guns/trigger-release.ogg", "Audio/Guns/trigger-release.ogg", clip => audioSource.PlayOneShot(clip));
                            }
                        }

                        lastGunSpawned = GunSpawned;
                        lastGunTrigger = GetGunInput(true);
                    }
                }
                catch { }

                try
                {
                    if (GunVibrations)
                    {
                        if (GunSpawned)
                        {
                            if (!lastGunSpawnedVibration)
                                References.Tagger.StartVibration(SwapGunHand, References.Tagger.tagHapticStrength / 2f, 0.05f);

                            if (GetGunInput(true))
                                References.Tagger.StartVibration(SwapGunHand, References.Tagger.tagHapticStrength / 2f, References.Tagger.tagHapticDuration / 2f);
                        }
                        else
                        {
                            if (lastGunSpawnedVibration)
                                References.Tagger.StartVibration(SwapGunHand, References.Tagger.tagHapticStrength / 2f, 0.015f);
                        }

                        lastGunSpawnedVibration = GunSpawned;
                    }
                }
                catch { }

                GunSpawned = false;

                UpdateKeyboard();

                if (annoyingMode)
                {
                    CustomBoards.BoardMaterial.color = new Color32(226, 74, 44, 255);
                    int randomChance = Random.Range(1, 400);
                    if (randomChance == 21)
                    {
                        References.LocalRig.PlayHandTapLocal(84, true, 0.4f);
                        References.LocalRig.PlayHandTapLocal(84, false, 0.4f);
                        Notifications.SendFormat("#FF00FF", "FUN FACT", "" + facts[Random.Range(0, facts.Length - 1)] + "");
                    }
                }

                if (Networking.InRoom)
                {
                    if (partyKickReconnecting)
                    {
                        partyLastCode = null;
                        partyKickReconnecting = false;
                        Notifications.SendFormat("green", "SUCCESS", "Successfully " + (waitForPlayerJoin ? "banned" : "kicked") + " " + amountPartying + " party member.");
                        ManagerRegistry.FriendshipGroupDetection.LeaveParty();
                    }
                    else
                    {
                        if (partyLastCode != null && Time.time > partyTime && (!waitForPlayerJoin || PhotonNetwork.PlayerListOthers.Length > 0))
                        {
                            if (Manager.GetIndex("Rejoin on Kick").enabled)
                            {
                                Logs.Log("Attempting rejoin");
                                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                                partyKickReconnecting = true;
                            }
                            else
                            {
                                Notifications.SendFormat("green", "SUCCESS", "Successfully " + (waitForPlayerJoin ? "banned" : "kicked") + " " + amountPartying + " party member.");
                                partyKickReconnecting = false;
                                partyLastCode = null;
                            }
                        }
                    }
                }
                else
                {
                    if (partyKickReconnecting)
                    {
                        if (partyLastCode != null && Time.time > partyTime && (!waitForPlayerJoin || PhotonNetwork.PlayerListOthers.Length > 0))
                        {
                            Logs.Log("Attempting rejoin");
                            Libraries.Networking.Rooms.JoinRoom(partyLastCode);
                            partyTime = Time.time + Important.reconnectDelay;
                        }
                    }
                }

                try
                {
                    if (!VoiceRecorder.enabled)
                    {
                        if (Audio.AudioIsPlaying)
                        {
                            if (Time.time > Audio.RecoverTime)
                            {
                                Audio.StopAllSounds();
                            }
                        }
                    }
                    else if (VoiceRecorder.enabled)
                    {
                        bool enabled = (Voices.Get().AudioClips.Any() && !Soundboard.disableLocalSoundboard)
                         || Voices.Get().PostProcessors.Any()
                         || Voices.Get().Pitch != 1f;

                        if (toggleDebugEchoMode != enabled)
                        {
                            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = enabled;
                            toggleDebugEchoMode = enabled;
                        }
                    }
                }
                catch { }

                if (CurrentPrompt != null && CurrentPrompt.IsText && !inTextInput)
                    Modules.Categories.Settings.SpawnKeyboard();
                #endregion

                #region Menu Navigation Features
                if (menu != null)
                {
                    if (pageButtonType == 3 || pageButtonType == 4)
                    {
                        bool previousButton = pageButtonType == 3 ? leftGrab : leftTrigger > 0.5f;
                        bool nextButton = pageButtonType == 3 ? rightGrab : rightTrigger > 0.5f;

                        if (Time.time > pageButtonChangeDelay)
                        {
                            if (previousButton)
                            {
                                pageButtonChangeDelay = Time.time + 0.2f;
                                if (exclusivePageSounds || (!string.IsNullOrEmpty(Sounds.DefaultSoundpack) && Sounds.DefaultSoundpack != "None"))
                                    Sounds.Play("Previous");
                                else
                                    Sounds.Play("Button");
                                Toggle("PreviousPage");
                            }

                            if (nextButton)
                            {
                                pageButtonChangeDelay = Time.time + 0.2f;
                                if (exclusivePageSounds || (!string.IsNullOrEmpty(Sounds.DefaultSoundpack) && Sounds.DefaultSoundpack != "None"))
                                    Sounds.Play("Next");
                                else
                                    Sounds.Play("Button");
                                Toggle("NextPage");
                            }
                        }

                        if (!previousButton && !nextButton)
                            pageButtonChangeDelay = -1f;
                    }
                }

                VR.UpdateJoystickMenu();

                VR.UpdateBarkMenu();

                if (pageScrolling && !(joystickMenu && qubitFront != null))
                {
                    bool shouldReload = false;
                    if (pageNumber != 0)
                    {
                        pageNumber = 0;
                        shouldReload = true;
                    }

                    Vector2 js = rightHand ? leftJoystick : rightJoystick;
                    if (Time.time > scrollDelay)
                    {
                        if (js.y > 0.5f)
                        {
                            pageOffset = Mathf.Clamp(pageOffset - 1, 0, DisplayedItemCount - PageSize);

                            shouldReload = true;
                            scrollDelay = Time.time + 0.1f;
                        }

                        if (js.y < -0.5f)
                        {
                            pageOffset = Mathf.Clamp(pageOffset + 1, 0, DisplayedItemCount - PageSize);

                            shouldReload = true;
                            scrollDelay = Time.time + 0.1f;
                        }
                    }

                    if (shouldReload)
                        ReloadMenu();
                }

                VR.UpdateWatchMenu();
                #endregion

                #region Preferences
                Preferences.RetryLoad();

                Preferences.BackupPeriodically();
                #endregion

                #region Ghostview
                try
                {
                    if (!legacyGhostview && GhostRig == null)
                    {
                        GameObject ghostRigHolder = new GameObject("ghostRigHolder");
                        ghostRigHolder.SetActive(false);

                        GhostRig = Instantiate(References.LocalRig, References.Player.transform.position, References.Player.transform.rotation, ghostRigHolder.transform);
                        GhostRig.headBodyOffset = Vector3.zero;

                        GhostRig.gameObject.SetActive(false);
                        GhostRig.transform.SetParent(References.LocalRig.transform.parent);

                        Destroy(ghostRigHolder);

                        GhostRig.transform.Find("VR Constraints/LeftArm/Left Arm IK/SlideAudio").gameObject.SetActive(false);
                        GhostRig.transform.Find("VR Constraints/RightArm/Right Arm IK/SlideAudio").gameObject.SetActive(false);
                        GhostRig.transform.Find("rig/body_pivot/SlideAudio").gameObject.SetActive(false);
                        GhostRig.GetComponent<OwnershipGaurd>().enabled = false;

                        Visual.FixRigMaterialESPColors(GhostRig);

                        GhostRig.transform.position = Vector3.one * float.MaxValue;
                    }

                    if (GhostMaterial == null)
                        GhostMaterial = new Material(Shader.Find("GUI/Text Shader"));

                    if (legacyGhostViewLeft == null)
                    {
                        legacyGhostViewLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Destroy(legacyGhostViewLeft.GetComponent<SphereCollider>());

                        legacyGhostViewLeft.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    }

                    if (legacyGhostViewRight == null)
                    {
                        legacyGhostViewRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Destroy(legacyGhostViewRight.GetComponent<SphereCollider>());

                        legacyGhostViewRight.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    }

                    if ((!References.LocalRig.enabled || ghostException) && !disableGhostview)
                    {
                        Color color = Manager.GetIndex("Swap Ghostview Colors").enabled ? buttonColors[1].GetCurrentColor() : backgroundColor.GetCurrentColor();

                        if (legacyGhostview)
                        {
                            if (GhostRig != null && GhostRig.gameObject.activeSelf)
                            {
                                GhostRig.gameObject.SetActive(false);
                                GhostRig.transform.position = Vector3.one * float.MaxValue;
                            }

                            legacyGhostViewLeft.SetActive(true);
                            legacyGhostViewLeft.transform.position = Controllers.GetTrueLeftHand().position;
                            legacyGhostViewLeft.GetComponent<Renderer>().material.color = color;

                            legacyGhostViewRight.SetActive(true);
                            legacyGhostViewRight.transform.position = Controllers.GetTrueRightHand().position;
                            legacyGhostViewRight.GetComponent<Renderer>().material.color = color;
                        }
                        else
                        {
                            if (legacyGhostViewLeft != null && legacyGhostViewLeft.activeSelf)
                                legacyGhostViewLeft.SetActive(false);
                            if (legacyGhostViewRight != null && legacyGhostViewRight.activeSelf)
                                legacyGhostViewRight.SetActive(false);

                            GhostRig.gameObject.SetActive(true);

                            Color ghm = color;
                            ghm.a = 0.5f;

                            GhostMaterial.color = ghm;
                            GhostRig.mainSkin.material = GhostMaterial;
                        }
                    }
                    else
                    {
                        if (GhostRig != null)
                        {
                            GhostRig.gameObject.SetActive(false);
                            GhostRig.transform.position = Vector3.one * float.MaxValue;
                        }

                        legacyGhostViewLeft.SetActive(false);
                        legacyGhostViewRight.SetActive(false);
                    }
                }
                catch { }
                #endregion

                #region Miscellaneous
                frameCount++;
                playTime += Time.unscaledDeltaTime;

                if (Modules.Categories.Settings.TutorialObject != null)
                    Modules.Categories.Settings.UpdateTutorial();

                ServerPos = ServerPos == Vector3.zero ? ServerSyncPos : Vector3.Lerp(ServerPos, References.LocalRig.SanitizeVector3(ServerSyncPos), References.LocalRig.lerpValueBody * 0.66f);
                ServerLeftHandPos = ServerLeftHandPos == Vector3.zero ? ServerSyncLeftHandPos : Vector3.Lerp(ServerLeftHandPos, References.LocalRig.SanitizeVector3(ServerSyncLeftHandPos), References.LocalRig.lerpValueBody);
                ServerRightHandPos = ServerRightHandPos == Vector3.zero ? ServerSyncRightHandPos : Vector3.Lerp(ServerRightHandPos, References.LocalRig.SanitizeVector3(ServerSyncRightHandPos), References.LocalRig.lerpValueBody);
                #endregion

                #region Mod Bindings
                try
                {
                    Inputs["A"] = rightPrimary;
                    Inputs["B"] = rightSecondary;
                    Inputs["X"] = leftPrimary;
                    Inputs["Y"] = leftSecondary;
                    Inputs["LG"] = leftGrab;
                    Inputs["RG"] = rightGrab;
                    Inputs["LT"] = leftTrigger > 0.5f;
                    Inputs["RT"] = rightTrigger > 0.5f;
                    Inputs["LJ"] = leftJoystickClick;
                    Inputs["RJ"] = rightJoystickClick;

                    foreach (KeyValuePair<string, List<string>> binding in ModBindings)
                    {
                        string bindInput = binding.Key;
                        List<string> boundMods = binding.Value;

                        if (boundMods.Count <= 0) continue;
                        bool bindValue = Inputs[bindInput];
                        foreach (string modName in boundMods)
                        {
                            Module buttonInfo = Manager.GetIndex(modName);
                            if (buttonInfo == null) continue;
                            buttonInfo.customBind = bindInput;

                            if (ToggleBindings || !buttonInfo.isTogglable)
                            {
                                if (bindValue && !BindStates[bindInput])
                                    Toggle(modName, true, true);
                            }

                            if (ToggleBindings) continue;
                            if ((bindValue && !buttonInfo.enabled) || (!bindValue && buttonInfo.enabled))
                                Toggle(modName, true, true);
                        }

                        BindStates[bindInput] = bindValue;
                    }
                }
                catch { }
                #endregion

                #region Visual Clean Up
                try
                {
                    Visual.ClearLinePool();
                    Visual.ClearNameTagPool();

                    if (GunPointer != null)
                    {
                        if (!GunPointer.activeSelf)
                            Destroy(GunPointer);
                        else
                            GunPointer.SetActive(false);
                    }

                    if (GunLine != null)
                    {
                        if (!GunLine.gameObject.activeSelf)
                        {
                            Destroy(GunLine.gameObject);
                            GunLine = null;
                        }
                        else
                            GunLine.gameObject.SetActive(false);
                    }

                    List<(long, float, PrimitiveType)> toRemoveVisualize = new List<(long, float, PrimitiveType)>();
                    foreach (KeyValuePair<(long, float, PrimitiveType), GameObject> key in Visual.visualizePool)
                    {
                        if (!key.Value.activeSelf)
                        {
                            toRemoveVisualize.Add(key.Key);
                            Destroy(key.Value);
                        }
                        else
                            key.Value.SetActive(false);
                    }
                    foreach (var item in toRemoveVisualize)
                        Visual.visualizePool.Remove(item);

                    List<string> toRemoveLabel = new List<string>();
                    foreach (KeyValuePair<string, GameObject> label in Visual.labelDictionary)
                    {
                        if (!label.Value.activeSelf)
                        {
                            toRemoveLabel.Add(label.Key);
                            Destroy(label.Value);
                        }
                        else
                            label.Value.SetActive(false);
                    }

                    foreach (string item in toRemoveLabel)
                        Visual.labelDictionary.Remove(item);
                }
                catch { }
                #endregion

                return true;
            }
            catch (Exception exc)
            {
                Logs.LogError($"Error with prefix at {exc.StackTrace}: {exc.Message}");
                return false;
            }
        }

        private static TMP_SpriteAsset buttonSpriteSheet;
        public static TMP_SpriteAsset ButtonSpriteSheet
        {
            get
            {
                if (buttonSpriteSheet != null) return buttonSpriteSheet;
                buttonSpriteSheet = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                buttonSpriteSheet.name = "Qubit_SpriteSheet";

                var textureList = new List<Texture2D>();
                var spriteDataList = new List<(string name, int index)>();

                void AddSprite(string name, Texture2D tex)
                {
                    spriteDataList.Add((name, textureList.Count));
                    textureList.Add(tex);
                }

                AddSprite("Favorite", LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.favorite.png"));
                AddSprite("Folder", LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.folder.png"));

                for (int i = 1; i <= 3; i++)
                {
                    AddSprite("Left" + i, LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.left{i}.png"));
                    AddSprite("Right" + i, LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.right{i}.png"));
                }

                int maxSize = 512;
                Texture2D spriteSheet = new Texture2D(maxSize, maxSize);
                Rect[] rects = spriteSheet.PackTextures(textureList.ToArray(), 2, maxSize);

                buttonSpriteSheet.spriteSheet = spriteSheet;
                buttonSpriteSheet.material = new Material(Shader.Find("TextMeshPro/Sprite"))
                {
                    mainTexture = spriteSheet
                };

                buttonSpriteSheet.spriteInfoList = new List<TMP_Sprite>();
                Traverse.Create(buttonSpriteSheet).Field("m_Version").SetValue("1.1.0");

                buttonSpriteSheet.spriteGlyphTable.Clear();
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
                    buttonSpriteSheet.spriteGlyphTable.Add(glyph);
                }

                buttonSpriteSheet.spriteCharacterTable.Clear();
                for (int i = 0; i < spriteDataList.Count; i++)
                {
                    var (name, _) = spriteDataList[i];

                    var character = new TMP_SpriteCharacter(0xFFFE, buttonSpriteSheet.spriteGlyphTable[i])
                    {
                        name = name,
                        scale = 1f,
                        glyphIndex = (uint)i
                    };
                    buttonSpriteSheet.spriteCharacterTable.Add(character);
                }

                buttonSpriteSheet.UpdateLookupTables();
                return buttonSpriteSheet;
            }
        }

        public static void CreateReference(bool? rightHandOverride = null)
        {
            reference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reference.transform.parent = rightHandOverride ?? VR.UsesRightHand() ? References.LeftHand : References.RightHand;
            reference.transform.localPosition = pointerOffset;
            reference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            buttonCollider = reference.GetComponent<SphereCollider>();

            if (hidePointer)
                reference.GetComponent<Renderer>().enabled = false;
            else
            {
                ObjectColors colorChanger = reference.AddComponent<ObjectColors>();
                colorChanger.colors = backgroundColor;
            }
        }
        public static Module[] GetModules()
        {
            IEnumerable<Module> result;

            if (isSearching)
            {
                result = nonGlobalSearch && Manager.CurrentCategoryName != "Main"
             ? Manager.modules[Manager.CurrentCategoryName]
                    : Manager.modules.SelectMany(category =>
               !isAdmin && (category.Key.Contains("Admin") || category.Key == "Mod Givers")
                               ? Array.Empty<Module>() : category.Value);
                string query = (keyboardInput ?? "").Replace(" ", "").ToLowerInvariant();
                result = result.Where(b => (!b.detected || allowDetected) && (!b.legacy || allowLegacy) &&
                           (b.aliases ?? Array.Empty<string>()).Concat(new[] { b.RenderName })
                  .Any(t => t.ClearTags().Replace(" ", "").ToLowerInvariant().Contains(query)));
            }
            else switch (Manager.CurrentCategoryName)
                {
                    case "Main": result = Manager.modules[Manager.CurrentCategoryName].Where(b => !skipButtons.Contains(b.name)); break;
                    case "Favorite Mods": result = favorites.Select(Manager.GetIndex).Where(b => b != null); break;
                    case "Enabled Mods":

                        result = new[] { Manager.GetIndex("Exit Enabled Mods") }.Concat(
               Manager.modules.SelectMany(category =>
            (hideSettings && category.Key.Contains("Settings")) ||
                   (hideMacros && category.Key.Contains("Macro"))
              ? Array.Empty<Module>() : category.Value.Where(b => b.enabled)));
                        break;
                    default: result = Manager.modules[Manager.CurrentCategoryName]; break;
                }
            result = result.Where(b => b != null);
#if LEGAL || LEGAL_DEBUG
result = result.Where(b => b.legal || b.label);
#endif
            if (isSearching || Manager.GetIndex("Alphabetize Menu")?.enabled == true)
                result = result.OrderBy(b => (b.RenderName).ClearTags());
            return result.ToArray();
        }

        private static Vector3? recenterPosition;
        private static Quaternion? recenterRotation;
        public static void RecenterMenu()
        {
            bool isKeyboardCondition = UI.IsKeyboardCondition();
            if (clickGUI)
            {
                if (recenterPosition == null || Vector3.Distance(recenterPosition.Value, References.BodyCollider.transform.TransformPoint(new Vector3(0f, 0f, 1.5f))) > 1f)
                {
                    menu.transform.position = References.BodyCollider.transform.TransformPoint(new Vector3(0f, 0f, 1.5f));
                    menu.transform.position = new Vector3(menu.transform.position.x, References.HeadCollider.transform.position.y + 0.15f, menu.transform.position.z);
                    menu.transform.LookAt(References.BodyCollider.transform);
                    menu.transform.rotation = Quaternion.Euler(0f, menu.transform.eulerAngles.y + 180f, 0f);

                    recenterPosition = menu.transform.position;
                    recenterRotation = menu.transform.rotation;
                }

                menu.transform.position = clickGUI && !XRSettings.isDeviceActive ? Vector3.zero : recenterPosition.Value;
                menu.transform.rotation = clickGUI && !XRSettings.isDeviceActive ? Quaternion.identity : (recenterRotation ?? menu.transform.rotation);
            }
            else
                VR.PositionMenu();
            VR.PositionMenuForKeyboard();
            UI.PositionMenuOnPC(isKeyboardCondition);

            VR.HoldPhysicalMenu();

            if (smoothMenuPosition)
            {
                smoothTargetPosition = smoothTargetPosition == Vector3.zero ? menu.transform.position : Vector3.Lerp(smoothTargetPosition, menu.transform.position, Time.deltaTime * 10f);

                menu.transform.position = smoothTargetPosition;
            }

            if (!smoothMenuRotation) return;
            smoothTargetRotation = smoothTargetRotation == Quaternion.identity ? menu.transform.rotation : Quaternion.Lerp(smoothTargetRotation, menu.transform.rotation, Time.deltaTime * 10f);

            menu.transform.rotation = smoothTargetRotation;
        }

        private static int menuOpenCount;

        public static event Action OnMenuOpened;
        public static void OpenMenu()
        {
            try
            {
                OnMenuOpened?.Invoke();
            }
            catch { }

            if (dynamicSounds)
                Sounds.Play(Sounds.DefaultSounds["Open"], global: true, vibrate: false);

            if (CreateMenu() == null) return;

            if (Bootstrapper.FirstLaunch && !askedOnboardingQuestions)
            {
                askedOnboardingQuestions = true;
                RunFirstLaunchOnboarding();
            }

            if (dynamicAnimations)
                Coroutines.instance.StartCoroutine(GrowCoroutine());

            if (particleSpawnEffect)
            {
                for (int i = 0; i < 25; i++)
                {
                    GameObject Particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Particle.transform.position = menu.transform.position;
                    Particle.transform.localScale = Vector3.one * (0.025f * (scaleWithPlayer ? References.Player.scale : 1f));
                    Particle.AddComponent<CustomParticle>();
                    Destroy(Particle.GetComponent<Collider>());
                }
            }

            menuOpenCount++;
            if (menuOpenCount == 100)
                Achievements.Unlock(new Achievements.Achievement
                {
                    name = "Persistent",
                    description = "Open the menu 100 times.",
                    icon = "Images/Achievements/persistent.png"

                });

            if (joystickMenu) return;
            if (reference == null)
                CreateReference();
        }

        private static bool askedOnboardingQuestions;
        private static void RunFirstLaunchOnboarding()
        {
            Prompt(
                "Would you like to consent to optional logging and crash reports? Your support helps make our mods better.",
                () => PromptSingle("You can always change your mind by toggling \"Disable Mod Telemetry\".", AskModernUIOptIn),
                () =>
                {
                    Manager.Toggle("Disable Mod Telemetry");
                    PromptSingle("You can always change your mind by toggling \"Disable Mod Telemetry\".", AskModernUIOptIn);
                });
        }

        private static void AskModernUIOptIn() =>
            Prompt(
                "Would you like to use the rewritten UI? This feature is in early alpha and can be disabled at any time.",
                () => Manager.Toggle("Modern Menu"));

        public static event Action OnMenuClosed;
        public static void CloseMenu()
        {
            try
            {
                OnMenuClosed?.Invoke();
            }
            catch { }

            GetObject("Shoulder Camera")?.transform.Find("CM vcam1").gameObject.SetActive(true);
            if (dynamicSounds)
                Sounds.Play(Sounds.DefaultSounds["Close"], global: true, vibrate: false);

            try
            {
                if ((isOnPC || keyboardWithToggleButton || isKeyboardPc) && TPC != null && TPC.transform.parent.gameObject.name.Contains("CameraTablet"))
                {
                    isOnPC = false;
                    TPC.transform.position = TPC.transform.parent.position;
                    TPC.transform.rotation = TPC.transform.parent.rotation;
                }
            }
            catch { }

            promptVideoPlayer?.Stop();

            smoothTargetPosition = Vector3.zero;
            smoothTargetRotation = Quaternion.identity;

            recenterPosition = null;

            if (!dynamicAnimations || explodeMenu)
            {
                if (!dropOnRemove)
                {
                    Destroy(menu);
                    menu = null;

                    Destroy(reference);
                    reference = null;

                    return;
                }

                if (explodeMenu)
                {
                    try
                    {
                        if (dynamicSounds)
                            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/explosion.ogg", "Audio/Menu/explosion.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));

                        foreach (GameObject gameObject in menu.transform.Children().Select(transform => transform.gameObject))
                        {
                            gameObject.transform.SetParent(null, true);
                            Rigidbody comp = gameObject.GetOrAddComponent<Rigidbody>();

                            if (zeroGravityMenu)
                                comp.useGravity = false;

                            if (menuCollisions)
                            {
                                GameObject collision = new GameObject("Collision");
                                collision.transform.SetParent(gameObject.transform, false);
                                collision.layer = 3;
                                collision.AddComponent<BoxCollider>();
                            }

                            comp.linearVelocity = RandomVector3(5f);
                            comp.angularVelocity = RandomVector3(50f);

                            Destroy(gameObject, 5f);
                        }
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        Rigidbody comp = menu.GetOrAddComponent<Rigidbody>();

                        if (zeroGravityMenu)
                            comp.useGravity = false;

                        if (menuCollisions)
                        {
                            GameObject collision = new GameObject("Collision");
                            collision.transform.SetParent(menuBackground.transform, false);
                            collision.layer = 3;
                            collision.AddComponent<BoxCollider>();
                        }

                        if (rightHand || (bothHands && openedwithright))
                        {
                            comp.linearVelocity = References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0);
                            comp.angularVelocity = GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller").GetOrAddComponent<GorillaVelocityEstimator>().angularVelocity;
                        }
                        else
                        {
                            comp.linearVelocity = References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0);
                            comp.angularVelocity = GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller").GetOrAddComponent<GorillaVelocityEstimator>().angularVelocity;
                        }

                        if (annoyingMode)
                        {
                            comp.linearVelocity = RandomVector3(5f);
                            comp.angularVelocity = RandomVector3(50f);
                        }
                    }
                    catch { }

                    if (menuTrail)
                    {
                        try
                        {
                            TrailRenderer trail = menu.AddComponent<TrailRenderer>();

                            trail.startColor = backgroundColor.GetColor(0);
                            trail.endColor = backgroundColor.GetColor(1);
                            trail.startWidth = 0.025f;
                            trail.endWidth = 0f;
                            trail.minVertexDistance = 0.05f;

                            if (smoothLines)
                            {
                                trail.numCapVertices = 10;
                                trail.numCornerVertices = 5;
                            }

                            trail.material.shader = Shader.Find("Sprites/Default");
                            trail.time = 2f;
                        }
                        catch { }
                    }
                }

                Destroy(menu, 5f);
                menu = null;

                Destroy(reference);
                reference = null;
            }
            else
            {
                Coroutines.instance.StartCoroutine(ShrinkCoroutine());

                Destroy(reference);
                reference = null;
            }
        }

        public static void OutlineMenuObject(GameObject toOut, bool shouldBeEnabled)
        {
            if (toOut.transform.parent != menu?.transform)
            {
                OutlineObject(toOut, shouldBeEnabled);
                return;
            }

            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Destroy(gameObject.GetComponent<BoxCollider>());
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localPosition = toOut.transform.localPosition;
            gameObject.transform.localScale = toOut.transform.localScale + new Vector3(-0.01f, 0.01f, 0.0075f);

            ObjectColors colorChanger = gameObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[shouldBeEnabled ? 1 : 0];

            if (roundMenu)
                RoundMenuObject(gameObject, 0.024f);
        }

        public static void OutlineObject(GameObject toOut, bool shouldBeEnabled)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Destroy(gameObject.GetComponent<BoxCollider>());
            gameObject.transform.parent = toOut.transform.parent;
            gameObject.transform.parent = toOut.transform.parent;
            gameObject.transform.rotation = toOut.transform.rotation;
            gameObject.transform.localPosition = toOut.transform.localPosition;
            gameObject.transform.localScale = toOut.transform.localScale + new Vector3(0.005f, 0.005f, -0.001f);

            ObjectColors colorChanger = gameObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[shouldBeEnabled ? 1 : 0];
        }

        public static void OutlineCanvasObject(MaskableGraphic canvasObject)
        {
            Outline outline = canvasObject.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(0.001f, 0.001f);
            outline.useGraphicAlpha = true;
        }

        public static void RoundMenuObject(GameObject toRound, float Bevel = 0.02f)
        {
            if (toRound.transform.parent != menu?.transform)
            {
                RoundObject(toRound, Bevel);
                return;
            }

            Renderer ToRoundRenderer = toRound.GetComponent<Renderer>();
            GameObject BaseA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(BaseA.GetComponent<Collider>());

            BaseA.transform.parent = menu.transform;
            BaseA.transform.rotation = Quaternion.identity;
            BaseA.transform.localPosition = toRound.transform.localPosition;
            BaseA.transform.localScale = toRound.transform.localScale + new Vector3(0f, Bevel * -2.55f, 0f);

            GameObject BaseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(BaseB.GetComponent<Collider>());

            BaseB.transform.parent = menu.transform;
            BaseB.transform.rotation = Quaternion.identity;
            BaseB.transform.localPosition = toRound.transform.localPosition;
            BaseB.transform.localScale = toRound.transform.localScale + new Vector3(0f, 0f, -Bevel * 2f);

            GameObject RoundCornerA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerA.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(RoundCornerA.GetComponent<Collider>());

            RoundCornerA.transform.parent = menu.transform;
            RoundCornerA.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerA.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, toRound.transform.localScale.y / 2f - Bevel * 1.275f, toRound.transform.localScale.z / 2f - Bevel);
            RoundCornerA.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject RoundCornerB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerB.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(RoundCornerB.GetComponent<Collider>());

            RoundCornerB.transform.parent = menu.transform;
            RoundCornerB.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerB.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, -(toRound.transform.localScale.y / 2f) + Bevel * 1.275f, toRound.transform.localScale.z / 2f - Bevel);
            RoundCornerB.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject RoundCornerC = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerC.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(RoundCornerC.GetComponent<Collider>());

            RoundCornerC.transform.parent = menu.transform;
            RoundCornerC.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerC.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, toRound.transform.localScale.y / 2f - Bevel * 1.275f, -(toRound.transform.localScale.z / 2f) + Bevel);
            RoundCornerC.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject RoundCornerD = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerD.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            Destroy(RoundCornerD.GetComponent<Collider>());

            RoundCornerD.transform.parent = menu.transform;
            RoundCornerD.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerD.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, -(toRound.transform.localScale.y / 2f) + Bevel * 1.275f, -(toRound.transform.localScale.z / 2f) + Bevel);
            RoundCornerD.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject[] ToChange = {
                BaseA,
                BaseB,
                RoundCornerA,
                RoundCornerB,
                RoundCornerC,
                RoundCornerD
            };

            foreach (GameObject Changed in ToChange)
            {
                ClampColor TargetChanger = Changed.AddComponent<ClampColor>();
                TargetChanger.targetRenderer = ToRoundRenderer;
            }

            ToRoundRenderer.enabled = false;

            ObjectColors colorChanger = ToRoundRenderer.GetComponent<ObjectColors>();
            if (colorChanger)
                colorChanger.overrideTransparency = false;
        }

        public static void RoundObject(GameObject toRound, float bevel = 0.02f)
        {
            static GameObject CreatePrimitive(PrimitiveType type, Transform parent, bool rendererEnabled)
            {
                GameObject obj = GameObject.CreatePrimitive(type);
                obj.GetComponent<Renderer>().enabled = rendererEnabled;

                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                obj.transform.SetParent(parent, false);
                return obj;
            }

            Renderer renderer = toRound.GetComponent<Renderer>();
            if (renderer == null) return;

            Transform parent = toRound.transform;
            Vector3 scale = parent.localScale;
            bool rendererEnabled = renderer.enabled;

            GameObject baseA = CreatePrimitive(PrimitiveType.Cube, parent, rendererEnabled);
            baseA.transform.localPosition = Vector3.zero;
            baseA.transform.localRotation = Quaternion.identity;
            baseA.transform.localScale = new Vector3(scale.x, scale.y - bevel * 2f, scale.z);

            GameObject baseB = CreatePrimitive(PrimitiveType.Cube, parent, rendererEnabled);
            baseB.transform.localPosition = Vector3.zero;
            baseB.transform.localRotation = Quaternion.identity;
            baseB.transform.localScale = new Vector3(scale.x, scale.y, scale.z - bevel * 2f);

            GameObject[] corners = new GameObject[4];
            Vector3[] cornerOffsets = {
                new Vector3(0f, scale.y / 2f - bevel, scale.z / 2f - bevel),
                new Vector3(0f, -scale.y / 2f + bevel, scale.z / 2f - bevel),
                new Vector3(0f, scale.y / 2f - bevel, -scale.z / 2f + bevel),
                new Vector3(0f, -scale.y / 2f + bevel, -scale.z / 2f + bevel)
            };

            for (int i = 0; i < 4; i++)
            {
                corners[i] = CreatePrimitive(PrimitiveType.Cylinder, parent, rendererEnabled);
                corners[i].transform.localPosition = cornerOffsets[i];
                corners[i].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                corners[i].transform.localScale = new Vector3(bevel * 2f, scale.x / 2f, bevel * 2f);
            }

            GameObject[] allObjects = { baseA, baseB, corners[0], corners[1], corners[2], corners[3] };
            foreach (GameObject obj in allObjects)
            {
                ClampColor clampColor = obj.AddComponent<ClampColor>();
                clampColor.targetRenderer = renderer;
            }

            renderer.enabled = false;

            ObjectColors colorChanger = renderer.GetComponent<ObjectColors>();
            if (colorChanger != null)
                colorChanger.overrideTransparency = false;
        }

        public static bool isAdmin;

        public static IEnumerator GrowCoroutine()
        {
            GameObject menuObject = menu;

            float elapsedTime = 0f;
            Vector3 target = menu.transform.localScale;
            while (elapsedTime < (slowDynamicAnimations ? 0.1f : 0.05f))
            {
                if (menuObject == null)
                    yield break;

                menuObject.transform.localScale = Vector3.Lerp(Vector3.zero, target, elapsedTime / (slowDynamicAnimations ? 0.1f : 0.05f));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (menuObject == null)
                yield break;

            menuObject.transform.localScale = target;
        }

        public static IEnumerator ShrinkCoroutine()
        {
            Transform menuTransform = menu.transform;
            menu = null;

            Vector3 before = menuTransform.localScale;
            float elapsedTime = 0f;
            while (elapsedTime < (slowDynamicAnimations ? 0.1f : 0.05f))
            {
                menuTransform.localScale = Vector3.Lerp(before, Vector3.zero, elapsedTime / (slowDynamicAnimations ? 0.1f : 0.05f));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Destroy(menuTransform.gameObject);
        }

        public static IEnumerator ButtonClick(int buttonIndex, Renderer render)
        {
            lastClickedName = "";
            float elapsedTime = 0f;
            while (elapsedTime < 0.1f)
            {
                if (render == null) yield break;

                int from = buttonIndex < 0 && swapButtonColors ? 0 : 1;
                int to = 1 - from;

                render.material.color = Color.Lerp(
                    buttonColors[from].GetCurrentColor(),
                    buttonColors[to].GetCurrentColor(),
                    elapsedTime / 0.1f
                );

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (render == null) yield break;

            ObjectColors colorChanger = render.gameObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[buttonIndex < 0 && swapButtonColors ? 1 : 0];

            if (roundMenu)
            {
                render.enabled = false;
                colorChanger.overrideTransparency = false;
            }

            if (!joystickMenu || buttonIndex != joystickButtonSelected) yield break;
            ExtGradient gradient = colorChanger.colors.Clone();
            gradient.SetColor(0, Color.red);

            colorChanger.colors = gradient;
        }

        public static void FollowMenuSettings(GameObject gameObject, bool shouldBeEnabled = true)
        {
            if (outlineMenu)
                OutlineMenuObject(gameObject, shouldBeEnabled);

            if (roundMenu)
                RoundMenuObject(gameObject);
        }

        public static void FollowMenuSettings(TMP_Text tmp, float? overlapTargetSpacing = null)
        {
            if (tmp == null)
                return;

            float targetSpacing = overlapTargetSpacing ?? (-8f + characterDistance);
            if (redactText)
                targetSpacing -= 3f;

            if (!Mathf.Approximately(tmp.characterSpacing, targetSpacing))
                tmp.characterSpacing = targetSpacing;

            Material fontMaterial = tmp.fontSharedMaterial;
            bool canOutline = fontMaterial != null
                && fontMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth)
                && fontMaterial.HasProperty(ShaderUtilities.ID_OutlineColor);

            if (outlineText && canOutline)
            {
                const float outlineWidth = 0.2f;

                if (!Mathf.Approximately(tmp.outlineWidth, outlineWidth))
                    tmp.outlineWidth = outlineWidth;

                if (tmp.outlineColor != Color.black)
                    tmp.outlineColor = Color.black;
            }
            else if (canOutline && !Mathf.Approximately(tmp.outlineWidth, 0f))
                tmp.outlineWidth = 0f;

            FontStyles targetStyle = tmp.fontStyle;

            if (underlineText)
                targetStyle |= FontStyles.Underline;

            if (smallCapsText)
                targetStyle |= FontStyles.SmallCaps;

            if (strikethroughText)
                targetStyle |= FontStyles.Strikethrough;

            if (tmp.fontStyle != targetStyle)
                tmp.fontStyle = targetStyle;
        }

        public static void FollowMenuSettings(MaskableGraphic canvasObject)
        {
            if (outlineText)
                OutlineCanvasObject(canvasObject);
        }

        public static void ReloadMenu()
        {
            if (menu != null)
            {
                menu.SetActive(false);
                Destroy(menu);
                menu = null;

                if (CurrentPrompt != null && CurrentPrompt.IsText && !inTextInput)
                    Modules.Categories.Settings.SpawnKeyboard();

                if (CreateMenu() == null) return;
            }

            if (reference == null) return;
            Destroy(reference);
            reference = null;

            CreateReference();
        }
        public static float badAppleTime;

        public static int DisplayedItemCount
        {
            get
            {
                if (UseQubit) return GetModules().Length;
                Module[] list = Manager.modules[Manager.CurrentCategoryName];
                int count = list.Length;

                if (Manager.CurrentCategoryName == "Favorite Mods")
                    count = favorites.Count;

                if (Manager.CurrentCategoryName == "Enabled Mods")
                {
                    var enabledMods = new List<string> { "Exit Enabled Mods" };

                    foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                    {
                        var buttonList = categoryEntry.Value;
                        var category = categoryEntry.Key;

                        bool skipSettings = hideSettings && category.Contains("Settings");
                        bool skipMacros = hideMacros && category.Contains("Macro");

                        if (skipSettings || skipMacros)
                            continue;

                        enabledMods.AddRange(from v in buttonList where v.enabled select v.name);
                    }

                    count = enabledMods.Count - 1;
                }

                if (Manager.CurrentCategoryName == "Main")
                {
                    foreach (var button in Manager.modules[Manager.CurrentCategoryName])
                    {
                        if (skipButtons.Contains(button.name))
                            count--;
                    }
                }

#if LEGAL || LEGAL_DEBUG
                list = list.Where(b => b.legal || b.label).ToArray();
                count = list.Length;
#endif

                if (!isSearching) return count;
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
                            Module[] buttonList = categoryEntry.Value;
                            foreach (Module v in buttonList)
                            {
                                try
                                {
                                    if (((categoryEntry.Key.Contains("Admin") ||
                                          categoryEntry.Key == "Mod Givers") &&
                                         !isAdmin)
                                        || (v.detected && !allowDetected)
                                        || (v.legacy && !allowLegacy)
                                        || (Manager.CurrentCategoryName == "Main" && skipButtons.Contains(v.name)))
                                        continue;

                                    string displayedText = v.RenderName;

                                    if (displayedText.Replace(" ", "").ToLower().Contains(keyboardInput.Replace(" ", "").ToLower()))
                                        searchedMods.Add(v);
                                }
                                catch { }
                            }
                        }
                    }

                    count = searchedMods.Count;
                }

                return count;
            }
        }
        public static float pageButtonChangeDelay;
        public static bool toggleButtonHeld;
        public static Vector3 smoothTargetPosition = Vector3.zero;
        public static Quaternion smoothTargetRotation = Quaternion.identity;
        public static float scrollDelay;

        private static readonly Dictionary<string, bool> Inputs = new Dictionary<string, bool>
        {
            { "A", false }, { "B", false }, { "X", false }, { "Y", false },
            { "LG", false }, { "RG", false }, { "LT", false }, { "RT", false },
            { "LJ", false }, { "RJ", false }
        };

        public static readonly Dictionary<string, bool> BindStates = new Dictionary<string, bool> {
            { "A", false },
            { "B", false },
            { "X", false },
            { "Y", false },
            { "LG", false },
            { "RG", false },
            { "LT", false },
            { "RT", false },
            { "LJ", false },
            { "RJ", false },
        };
        private static float fpsAvgTime;
        private static float fpsAverageNumber;
        private static float? potatoTime = 0f;
        private static float? adminTime = 0f;
        public static GameObject legacyGhostViewLeft;
        public static GameObject legacyGhostViewRight;
        public static Material GhostMaterial;

        public static bool toggleDebugEchoMode;
        public static readonly string[] facts = {
            "The honeybee is the only insect that produces food eaten by humans.",
            "Bananas are berries, but strawberries aren't.",
            "The Eiffel Tower can be 15 cm taller during the summer due to thermal expansion.",
            "A group of flamingos is called a 'flamboyance.'",
            "The shortest war in history was between Britain and Zanzibar on August 27, 1896 – Zanzibar surrendered after 38 minutes.",
            "Cows have best friends and can become stressed when they are separated.",
            "The first computer programmer was a woman named Ada Lovelace.",
            "A 'jiffy' is an actual unit of time, equivalent to 1/100th of a second.",
            "Octopuses have three hearts and blue blood.",
            "The world's largest desert is Antarctica.",
            "Honey never spoils. Archaeologists have found pots of honey in ancient Egyptian tombs that are over 3,000 years old and still perfectly edible.",
            "The smell of freshly-cut grass is actually a plant distress call.",
            "The average person spends six months of their life waiting for red lights to turn green.",
            "A group of owls is called a parliament.",
            "The longest word in the English language without a vowel is 'rhythms.'",
            "The Great Wall of China is not visible from the moon without aid.",
            "Venus rotates so slowly on its axis that a day on Venus (one full rotation) is longer than a year on Venus (orbit around the sun).",
            "The world's largest recorded snowflake was 15 inches wide.",
            "There are more possible iterations of a game of chess than there are atoms in the known universe.",
            "A newborn kangaroo is the size of a lima bean and is unable to hop until it's about 8 months old.",
            "The longest hiccuping spree lasted for 68 years!",
            "A single cloud can weigh more than 1 million pounds.",
            "Honeybees can recognize human faces.",
            "Cats have five toes on their front paws but only four on their back paws.",
            "The inventor of the frisbee was turned into a frisbee. Walter Morrison, the inventor, was cremated, and his ashes were turned into a frisbee after he passed away.",
            "Penguins give each other pebbles as a way of proposing.",
            "You need oxygen to live.",
            "You need to be nourished to live.",
            "The letter \"A\" is at the beginning of the alphabet. The letter \"T\" is at the beginning of both of these sentences. Why are you looking there? You're wasting your time. You're wasting even MORE time reading this. Ok bye. STOP READING!!!"
        };
    }
}
