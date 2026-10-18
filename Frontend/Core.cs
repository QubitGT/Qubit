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

using Qubit.Core;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts;
using HarmonyLib;
using Photon.Pun;
using Qubit.Backend;
using Qubit.Extensions;
using Qubit.Frontend.Colors;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules;
using Qubit.Modules.Categories;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Classes.QubitMenuBody;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Overpowered;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using ButtonCollider = Qubit.Frontend.Classes.ButtonCollider;
using Console = Qubit.Backend.Console;
using JoinType = GorillaNetworking.JoinType;
using Random = UnityEngine.Random;

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
                    if (hidetitle && !noPageNumber) textToSet += "      ";
                    if (disableFpsCounter) textToSet = "";
                    if (hidetitle && !noPageNumber) textToSet += "Page " + (pageNumber + 1);

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

                if (adminTime != null && References.InRoom)
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
                    string targetString = doCustomName ? NoRichtextTags(customMenuName) : "Qubit Menu";
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
                // Update watches
                Watches.ForEach(watch => watch.Update());

                // Load projectiles
                Projectiles.LoadProjectileCache();

                // Fix for disorganized menu
                if (disorganized && Manager.CurrentCategoryName != "Main")
                {
                    Manager.CurrentCategoryName = "Main";
                    ReloadMenu();
                }

                // Fix for long menu
                if (longmenu && pageNumber != 0)
                {
                    pageNumber = 0;
                    ReloadMenu();
                }

                VR.UpdatePhysicalReference();

                // Bad apple theme
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

                // Gun sounds
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

                // Gun Vibrations
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
                        Notifications.SendNotification("<color=grey>[</color><color=#FF00FF>FUN FACT</color><color=grey>]</color> " + facts[Random.Range(0, facts.Length - 1)] + "");
                    }
                }

                // Party kick code (to return back to the main lobby when you're done)
                if (References.InRoom)
                {
                    if (partyKickReconnecting)
                    {
                        partyLastCode = null;
                        partyKickReconnecting = false;
                        Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Successfully " + (waitForPlayerJoin ? "banned" : "kicked") + " " + amountPartying + " party member.");
                        FriendshipGroupDetection.Instance.LeaveParty();
                    }
                    else
                    {
                        if (partyLastCode != null && Time.time > partyTime && (!waitForPlayerJoin || PhotonNetwork.PlayerListOthers.Length > 0))
                        {
                            if (Manager.GetIndex("Rejoin on Kick").enabled)
                            {
                                Logs.Log("Attempting rejoin");
                                References.Network.ReturnToSinglePlayer();
                                partyKickReconnecting = true;
                            }
                            else
                            {
                                Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Successfully " + (waitForPlayerJoin ? "banned" : "kicked") + " " + amountPartying + " party member.");
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
                            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(partyLastCode, JoinType.Solo);
                            partyTime = Time.time + Important.reconnectDelay;
                        }
                    }
                }

                // Recover from playing sound on soundboard code
                try
                {
                    if (!RecorderPatch.enabled)
                    {
                        if (Sound.AudioIsPlaying)
                        {
                            if (Time.time > Sound.RecoverTime)
                            {
                                Sound.StopAllSounds();
                            }
                        }
                    }
                    else if (RecorderPatch.enabled)
                    {
                        bool enabled = (Voices.Get().AudioClips.Any() && !Soundboard.disableLocalSoundboard)
                         || Voices.Get().PostProcessors.Any()
                         || Voices.Get().Pitch != 1f;

                        if (toggleDebugEchoMode != enabled)
                        {
                            References.Network.VoiceConnection.PrimaryRecorder.DebugEchoMode = enabled;
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

                        Visuals.FixRigMaterialESPColors(GhostRig);

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
                    // Custom mod binds
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
                    Visuals.ClearLinePool();
                    Visuals.ClearNameTagPool();

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
                    foreach (KeyValuePair<(long, float, PrimitiveType), GameObject> key in Visuals.visualizePool)
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
                        Visuals.visualizePool.Remove(item);

                    List<string> toRemoveLabel = new List<string>();
                    foreach (KeyValuePair<string, GameObject> label in Visuals.labelDictionary)
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
                        Visuals.labelDictionary.Remove(item);
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
        private static void AddButton(float offset, int buttonIndex, Module method)
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

                if (longmenu && buttonIndex >= PageSize)
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
                Button.relatedText = method.buttonText;

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
                            Destroy(Button);

                            RenderIncrementalButton(false, offset, buttonIndex, method);
                            RenderIncrementalButton(true, offset, buttonIndex, method);
                        }
                    }
                }

                bool shouldSwap = swapButtonColors && buttonIndex < 0;

                if (lastClickedName != method.buttonText)
                {
                    ObjectColors colorChanger = buttonObject.AddComponent<ObjectColors>();
                    colorChanger.colors = buttonColors[shouldSwap ^ method.enabled ? 1 : 0];

                    if (joystickMenu && buttonIndex == joystickButtonSelected)
                    {
                        joystickSelectedButton = method.buttonText;

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
                Traverse.Create(buttonSpriteSheet).Field("m_Version").SetValue("1.1.0"); // TextMeshPro kills itself unless this is set.

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

        private static void AddDebugButton()
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

        private static void AddDonateButton()
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

        private static void AddUpdateButton()
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

        private static void AddReturnButton(bool offcenteredPosition)
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

        private static void RenderIncrementalButton(bool increment, float offset, int buttonIndex, Module method)
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
                button.relatedText = method.buttonText;
                button.incremental = true;
                button.positive = increment;

                if (increment)
                    buttonObject.transform.localPosition = new Vector3(buttonObject.transform.localPosition.x, -buttonObject.transform.localPosition.y, buttonObject.transform.localPosition.z);

                if (lastClickedName != method.buttonText + (increment ? "+" : "-"))
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
            if (NoAutoSizeText)
                textTransform.sizeDelta = new Vector2(9f, 0.015f);

            if (hideTextOnCamera)
                textTransform.gameObject.layer = 19;

            textTransform.localPosition = thinMenu ? new Vector3(.064f, increment ? -0.12f : 0.12f, .111f - offset / 2.6f) : new Vector3(.064f, increment ? -0.18f : 0.18f, .111f - offset / 2.6f);
            textTransform.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(buttonText);
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
 result = result.Where(b => (!b.detected || allowDetected) &&
            (b.aliases ?? Array.Empty<string>()).Concat(new[] { b.overlapText ?? b.buttonText })
   .Any(t => t.ClearTags().Replace(" ", "").ToLowerInvariant().Contains(query)));
}
       else switch (Manager.CurrentCategoryName)
  {
                  case "Main": result = Manager.modules[Manager.CurrentCategoryName].Where(b => !skipButtons.Contains(b.buttonText)); break;
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
                  result = result.OrderBy(b => (b.overlapText ?? b.buttonText).ClearTags());
    return result.ToArray();
 }
        public static GameObject CreateLegacyMenu()
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

            Destroy(menu.GetComponent<BoxCollider>());
            Destroy(menu.GetComponent<Renderer>());

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
            Destroy(menuBackground.GetComponent<BoxCollider>());

            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.localPosition = new Vector3(0.50f, 0f, 0f);
            menuBackground.transform.rotation = Quaternion.identity;

            // Size is calculated in depth, width, height
            menuBackground.transform.localScale = thinMenu ? new Vector3(0.1f, 1f, 1f) : new Vector3(0.1f, 1.5f, 1f);

            if (innerOutline || themeType == 32)
            {
                GameObject innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, -0.4840625f, 0f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.0065f, 0.98f);

                ObjectColors colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, 0.4840625f, 0f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.0065f, 0.98f);

                colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
                innerOutlineSegment.transform.parent = menuBackground.transform;
                innerOutlineSegment.transform.rotation = Quaternion.identity;
                innerOutlineSegment.transform.localPosition = new Vector3(0f, 0f, -0.4875f);
                innerOutlineSegment.transform.localScale = new Vector3(1.025f, 0.968125f, 0.005f);

                colorChanger = innerOutlineSegment.AddComponent<ObjectColors>();
                colorChanger.colors = buttonColors[1];

                innerOutlineSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(innerOutlineSegment.GetComponent<BoxCollider>());
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
                if (doCustomMenuBackground)
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

            if (!hidetitle)
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
                    title.text += $" <color=grey>[</color><color=white>{(pageScrolling ? pageOffset : pageNumber) + 1}</color><color=grey>]</color>";

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
                    string targetString = doCustomName ? NoRichtextTags(customMenuName) : "Qubit Menu";
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
                if (NoAutoSizeText)
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
                if (hidetitle && !noPageNumber) textToSet += "      ";
                if (hidetitle && !noPageNumber) textToSet += "Page " + (pageNumber + 1);

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
                fpsTransform.sizeDelta = NoAutoSizeText ? new Vector2(9f, 0.015f) : new Vector2(0.28f, 0.02f);
                fpsTransform.localPosition = new Vector3(0.06f, 0f, hidetitle ? 0.175f : 0.135f);
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
                if (NoAutoSizeText)
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
                                    texts.Add(v.overlapText ?? v.buttonText);

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
                                            || (v.detected && !allowDetected))
                                            continue;

                                        List<string> texts = v.aliases == null ? new List<string>() : v.aliases.ToList();
                                        texts.Add(v.overlapText ?? v.buttonText);

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
                                        if (!skipButtons.Contains(button.buttonText))
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
                                    enabledMods = enabledMods.OrderBy(v => v.buttonText).ToList();
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
                                buttonText = "This category seems to be empty.",
                                legal = true,
                                label = true
                            }
                        );
                    }


                    if (Manager.GetIndex("Alphabetize Menu").enabled || isSearching)
                        renderButtons = StringsToInfos(Alphabetize(InfosToStrings(renderButtons)));

                    if (!longmenu)
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

                    Destroy(particle.GetComponent<BoxCollider>());
                    Destroy(particle, 2f);

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
                Sounds.Play(Sounds.DefaultSounds["Open"], global: true);

            CreateMenu();

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
                Sounds.Play(Sounds.DefaultSounds["Close"], global: true);

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

        private static void AddPageButtons()
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
                        new Vector3(0.09f, hidetitle ? 0.1f : 0.3f, 0.05f),
                        new Vector3(0.56f, (thinMenu ? 0.299f : 0.499f) + (hidetitle ? 0.1f : 0f), 0.355f + (hidetitle ? 0.1f : 0f)),
                        new Vector3(0.56f, (thinMenu ? -0.299f : -0.499f) - (hidetitle ? 0.1f : 0f), 0.355f + (hidetitle ? 0.1f : 0f)),
                        new Vector3(0.064f, (thinMenu ? 0.09f : 0.15f) + (hidetitle ? 0.035f : 0f), 0.135f + (hidetitle ? 0.0375f : 0f)),
                        new Vector3(0.064f, (thinMenu ? -0.09f : -0.15f) - (hidetitle ? 0.035f : 0f), 0.135f + (hidetitle ? 0.0375f : 0f)),
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

        private static void CreatePageButtonPair(string prevButtonName, string nextButtonName, Vector3 buttonScale, Vector3 prevButtonPos, Vector3 nextButtonPos, Vector3 prevTextPos, Vector3 nextTextPos, ExtGradient color, Vector2? textSize = null)
        {
            AdvancedAddButton(prevButtonName, buttonScale, prevButtonPos, prevTextPos, color, textSize, 0);
            AdvancedAddButton(nextButtonName, buttonScale, nextButtonPos, nextTextPos, color, textSize, 1);
        }

        private static GameObject AdvancedAddButton(string buttonName, Vector3 scale, Vector3 position, Vector3 textPosition, ExtGradient color, Vector2? textSize, int arrowIndex)
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

            if (NoAutoSizeText)
                textRect.sizeDelta = new Vector2(9f, 0.015f);

            if (hideTextOnCamera)
                textRect.gameObject.layer = 19;

            textRect.localPosition = textPosition;
            textRect.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            FollowMenuSettings(text);
            FollowMenuSettings(button, !swapButtonColors);

            return button;
        }

        /// <summary>
        /// Applies or removes an outline effect to the specified menu object, adjusting its appearance based on the
        /// provided state.
        /// </summary>
        /// <remarks>If the specified object is not a direct child of the menu, the outline effect is
        /// applied using a general method. For menu child objects, a new primitive is created and styled to visually
        /// represent the outline. The appearance of the outline may vary depending on the value of shouldBeEnabled and
        /// the configuration of the menu. This method does not modify the original object's geometry or
        /// components.</remarks>
        /// <param name="toOut">The GameObject to which the outline effect will be applied or removed. Must not be null.</param>
        /// <param name="shouldBeEnabled">A value indicating whether the outline effect should be enabled (<see langword="true"/>) or removed (<see
        /// langword="false"/>).</param>
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

            if (shouldRound)
                RoundMenuObject(gameObject, 0.024f);
        }

        /// <summary>
        /// Creates an outline effect around the specified GameObject to visually indicate its enabled or disabled
        /// state.
        /// </summary>
        /// <remarks>The outline is created as a separate GameObject, matching the position, rotation, and
        /// scale of the target object. The outline color reflects the enabled or disabled state as determined by the
        /// application's color settings. This method does not modify the original GameObject.</remarks>
        /// <param name="toOut">The GameObject to outline. Cannot be null.</param>
        /// <param name="shouldBeEnabled">A value indicating whether the outline should represent the enabled (<see langword="true"/>) or disabled
        /// (<see langword="false"/>) state.</param>
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

        /// <summary>
        /// Adds a black outline effect to the specified UI canvas object.
        /// </summary>
        /// <remarks>This method adds a Unity UI Outline component to the target object's GameObject,
        /// configuring it with a thin black outline. If the object already has an Outline component, an additional one
        /// will be added, which may result in multiple outlines. The outline uses the graphic's alpha
        /// channel.</remarks>
        /// <param name="canvasObject">The UI element to which the outline effect will be applied. Must not be null.</param>
        public static void OutlineCanvasObject(MaskableGraphic canvasObject)
        {
            // Creds: libyyyreal for optimization tech
            Outline outline = canvasObject.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(0.001f, 0.001f);
            outline.useGraphicAlpha = true;
        }

        /// <summary>
        /// Replaces a menu object's appearance with a rounded version by constructing beveled edges and rounded corners
        /// around the specified GameObject.
        /// </summary>
        /// <remarks>If the specified GameObject is not a direct child of the menu, only a basic rounding
        /// operation is performed. When applied to a menu child, the method disables the original renderer and
        /// constructs new primitives to visually represent a rounded, beveled menu item. The method also ensures that
        /// color and transparency settings are preserved by copying relevant components.</remarks>
        /// <param name="toRound">The GameObject to be visually rounded. Must be a child of the menu GameObject to receive the full rounded
        /// treatment; otherwise, a standard rounding is applied.</param>
        /// <param name="Bevel">The width of the bevel applied to the edges and corners, in Unity units. Must be a positive value. The
        /// default is 0.02.</param>
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

        /// <summary>
        /// Replaces the specified GameObject with a visually rounded version by constructing a composite of primitive
        /// shapes with beveled edges.
        /// </summary>
        /// <remarks>This method disables the original object's Renderer and creates new child primitives
        /// to approximate a rounded appearance. The original object's ColorChanger component, if present, will have its
        /// overrideTransparency property set to false. The method does not modify the original object's collider or
        /// mesh, and is intended for visual effects only.</remarks>
        /// <param name="toRound">The GameObject to be visually rounded. Must have a Renderer component attached.</param>
        /// <param name="bevel">The width, in world units, of the bevel applied to the object's edges. Must be non-negative. The default
        /// value is 0.02.</param>
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

            ObjectColors colorChanger = render.gameObject.AddComponent<ObjectColors>();
            colorChanger.colors = buttonColors[buttonIndex < 0 && swapButtonColors ? 1 : 0];

            if (shouldRound)
            {
                render.enabled = false;
                colorChanger.overrideTransparency = false;
            }

            if (!joystickMenu || buttonIndex != joystickButtonSelected) yield break;
            ExtGradient gradient = colorChanger.colors.Clone();
            gradient.SetColor(0, Color.red);

            colorChanger.colors = gradient;
        }

        /// <summary>
        /// Applies menu appearance settings to the specified game object, such as outlining and rounding, based on
        /// current configuration.
        /// </summary>
        /// <param name="gameObject">The game object to which the menu appearance settings will be applied.</param>
        /// <param name="shouldBeEnabled">A value indicating whether outlining should be enabled for the game object. The default is <see
        /// langword="true"/>.</param>
        public static void FollowMenuSettings(GameObject gameObject, bool shouldBeEnabled = true)
        {
            if (shouldOutline)
                OutlineMenuObject(gameObject, shouldBeEnabled);

            if (shouldRound)
                RoundMenuObject(gameObject);
        }

        /// <summary>
        /// Applies menu appearance settings to the specified canvas object, such as outlining, based on
        /// current configuration.
        /// </summary>
        /// <param name="tmp">The canvas object to which the menu appearance settings will be applied.</param>
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

        /// <summary>
        /// Applies menu appearance settings to the specified canvas object, such as outlining, based on
        /// current configuration.
        /// </summary>
        /// <param name="canvasObject">The canvas object to which the menu appearance settings will be applied.</param>
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

                CreateMenu();
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

                        enabledMods.AddRange(from v in buttonList where v.enabled select v.buttonText);
                    }

                    count = enabledMods.Count - 1;
                }

                if (Manager.CurrentCategoryName == "Main")
                {
                    foreach (var button in Manager.modules[Manager.CurrentCategoryName])
                    {
                        if (skipButtons.Contains(button.buttonText))
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
                                texts.Add(v.overlapText ?? v.buttonText);

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
                                        || (Manager.CurrentCategoryName == "Main" && skipButtons.Contains(v.buttonText)))
                                        continue;

                                    string displayedText = v.overlapText ?? v.buttonText;

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

        public static bool toggleDebugEchoMode; // Build with this enabled for a surprise
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

    public static class Settings
    {
        // The variable warehouse

        public static bool isOnPC;
        public static bool IsSteam = true;
        public static bool Lockdown;

        public static bool HasLoaded;
        public static bool allowDetected;
        public static float playTime;
        public static int frameCount;

        public static bool thinMenu = true;
        public static bool longmenu;
        public static bool hidetitle;
        public static bool disorganized;
        public static bool flipMenu;
        public static bool shinyMenu;
        public static bool transparentMenu;
        public static bool crystallizeMenu;
        public static bool zeroGravityMenu;
        public static bool menuCollisions;
        public static bool dropOnRemove = true;
        public static bool shouldOutline;
        public static bool outlineText;
        public static bool underlineText;
        public static bool strikethroughText;
        public static bool smallCapsText;
        public static bool innerOutline;
        public static bool smoothLines;
        public static bool shouldRound;
        public static bool openedwithright;
        public static bool oneHand;
        public static bool clickGUI;

        public static int _pageSize = 8;
        public static int PageSize
        {
            get => UseQubit ? QubitPageCapacity : _pageSize - buttonOffset;
            set => _pageSize = value;
        }

        public static float ButtonDistance
        {
            get => 0.8f / (PageSize + buttonOffset);
        }

        public static int pageOffset;
        public static int pageNumber;
        public static bool pageScrolling;
        public static bool noPageNumber;
        public static bool disablePageButtons;
        public static bool swapButtonColors;
        public static int pageButtonType = 1;

        public static int buttonClickSound = 8;
        public static int buttonClickVolume = 4;
        public static int buttonOffset = 0;
        public static int menuButtonIndex = 1;
        public static bool toggleButton;
        public static bool toggleButtonActive;

        public static bool doButtonsVibrate = true;
        public static bool serversidedButtonSounds;

        public static bool joystickMenu;

        public static bool physicalMenu;

        public static bool barkMenu;
        public static bool barkMenuOpen;

        public static Vector3 physicalOpenPosition = Vector3.zero;
        public static bool joystickOpen;
        public static bool smoothMenuPosition;
        public static bool smoothMenuRotation;
        public static int joystickButtonSelected;
        public static string joystickSelectedButton = "";

        public static int joystickMenuPosition;
        public static Vector3[] joystickMenuPositions = {
            new Vector3(0.3f, 0.2f, 1f),
            new Vector3(-0.3f, 0.2f, 1f),
            new Vector3(0.3f, -0.2f, 1f),
            new Vector3(-0.3f, -0.2f, 1f),
            new Vector3(0f, -0.1f, 0.5f),
            new Vector3(0.3f, -0.1f, 1f),
            new Vector3(-0.3f, -0.1f, 1f),
            new Vector3(0f, 0.2f, 1f),
            new Vector3(0f, -0.2f, 1f),
        };

        public static bool rightHand;
        public static bool bothHands;
        public static bool wristMenu;
        public static bool explodeMenu;
        public static bool watchMenu;

        public static bool stackNotifications;
        public static bool hideBrackets;
        public static bool disableNotifications;
        public static bool disableMasterClientNotifications;
        public static bool disableRoomNotifications;
        public static bool disablePlayerNotifications;
        public static bool clearNotificationsOnDisconnect;
        public static bool showEnabledModsVR = true;
        public static bool advancedArraylist;
        public static bool flipArraylist;
        public static bool hideSettings;
        public static bool hideMacros;
        public static bool hideTextOnCamera;
        public static bool hidePointer;
        public static bool incrementalButtons = true;
        public static bool disableDisconnectButton;
        public static bool disableFpsCounter;
        public static bool disableReturnButton;
        public static bool enableDebugButton;

        public static bool ghostException;
        public static bool disableGhostview;
        public static bool legacyGhostview;
        public static bool checkMode;
        public static bool rockWatermark = false;
        public static bool disableWatermark;
        public static string CosmeticsOwned;
        public static bool NoAutoSizeText;

        public static bool doCustomName;
        public static string customMenuName = "Your Text Here";
        public static readonly string menuName =
#if LEGAL
            "<b>Qubit</b> Legal";
#else
            "<b>Qubit</b> Menu";
#endif
        public static bool doCustomMenuBackground;
        public static bool menuTrail;
        public static bool adaptiveButtons = true;
        public static int pcbg;

        public static int? fullModAmount;
        public static bool scaleWithPlayer;
        public static float menuScale = 1f;
        public static int notificationScale = 30;
        public static int overlayScale = 30;
        public static int arraylistScale = 20;

        public static bool dynamicSounds;
        public static bool exclusivePageSounds;
        public static bool dynamicAnimations;
        public static bool slowDynamicAnimations;
        public static bool dynamicGradients;
        public static bool horizontalGradients;
        public static bool scrollingGradients;
        public static bool particleSpawnEffect;
        public static bool animatedTitle;
        public static bool gradientTitle;
        public static string lastClickedName = "";

        public static bool isMenuButtonHeld;
        public static bool shouldBePC;


        public static bool ToggleBindings = true;
        public static bool IsBinding;
        public static string BindInput = "";
        public static bool IsRebinding;

        public static readonly Dictionary<string, List<string>> ModBindings = new Dictionary<string, List<string>> {
            { "A", new List<string>() },
            { "B", new List<string>() },
            { "X", new List<string>() },
            { "Y", new List<string>() },
            { "LG", new List<string>() },
            { "RG", new List<string>() },
            { "LT", new List<string>() },
            { "RT", new List<string>() },
            { "LJ", new List<string>() },
            { "RJ", new List<string>() },
        };

        public static readonly List<string> quickActions = new List<string>();

        public static Camera TPC;
        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static VideoPlayer videoPlayer;
        public static SphereCollider buttonCollider;
        public static GameObject canvasObj;
        public static TextMeshPro fpsCount;
        public static Image watermarkImage;
        public static bool fpsCountTimed;
        public static bool fpsCountAverage;
        public static bool ftCount;
        public static bool acceptedDonations;
        public static float lastDeltaTime = 1f;
        public static TextMeshPro title;
        public static VRRig GhostRig;
        public static Material CrystalMaterial;
        public static Material watermarkMat;
        public static GameObject menuSpawnPosition;

        public static GameObject TryOnRoom;

        public class WatchInfo
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
            public GameObject shell;
            public Image indicator;

            public void Update()
            {
                Renderer renderer = shell.GetComponent<Renderer>();
                if (text.font != activeFont)
                    text.SafeSetFont(activeFont);
                if (text.fontStyle != activeFontStyle)
                    text.SafeSetFontStyle(activeFontStyle);
                if (renderer.sharedMaterial != CustomBoards.BoardMaterial)
                    renderer.sharedMaterial = CustomBoards.BoardMaterial;
            }
        }

        public static List<WatchInfo> Watches = new List<WatchInfo>();
        public static Texture2D customMenuBackgroundImage;
        public static Texture2D customWatermark;

        public static readonly List<string> favorites = new List<string> { "Exit Favorite Mods" };
        public static readonly List<string> skipButtons = new List<string> { };
        public static bool translate;

        public static string serverLink = "https://discord.gg/FXja6thKZC";

        public static int arrowType;
        public static readonly string[][] arrowTypes = {
            new[] {"<", ">"},
            new[] {"←", "→"},
            new[] { "    <sprite name=\"Left1\">", "    <sprite name=\"Right1\">"},
            new[] {"◄", "►"},
            new[] { "    <sprite name=\"Left2\">", "    <sprite name=\"Right2\">"},
            new[] {"‹", "›"},
            new[] {"«", "»"},
            new[] { "    <sprite name=\"Left3\">", "    <sprite name=\"Right3\">"},
            new[] {"-", "+"},
            new[] {"", ""},
            new[] {"v", "ʌ"},
            new[] { "v\nv\nv\nv\nv\nv", "ʌ\nʌ\nʌ\nʌ\nʌ\nʌ" }
        };

        public static int themeType = 0;
        public static bool slowFadeColors;

        public static ExtGradient backgroundColor = new ExtGradient
        {
            colors = ExtGradient.GetSolidGradient(
                new Color32(118, 6, 252, 128)
            )
        };

        public static ExtGradient menuBackgroundColor = new ExtGradient
        {
            colors = ExtGradient.GetSolidGradient(
                new Color32(22, 22, 22, 128)
            )
        };

        public static ExtGradient[] buttonColors = {
            new ExtGradient // Released
            {
                colors = ExtGradient.GetSolidGradient(
                    new Color32(118, 6, 252, 255)
                )
            },
            new ExtGradient // Pressed
            {
                colors = ExtGradient.GetSolidGradient(
                    new Color32(88, 6, 186, 255)
                )
            }
        };

        public static ExtGradient[] textColors = {
            new ExtGradient // Title
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            },
            new ExtGradient // Button Released
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            },
            new ExtGradient // Button Clicked
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            }
        };

        public static Vector3 closePosition;

        public static Vector3 pointerOffset = new Vector3(0f, -0.1f, 0f);

        public static float timeMenuStarted = -1f;
        public static float buttonCooldown;

        public static int notificationDecayTime = 1000;
        public static bool redactText;

        public static bool annoyingMode;

        public static void Initialize()
        {
            timeMenuStarted = Time.time;
            IsSteam = PlayFabAuthenticator.instance.platform;
        }

        public static void InitializeScene()
        {
            CrystalMaterial = GameObj.GetObject("Environment Objects/LocalObjects_Prefab/ForestToCave/C_Crystal_Chunk")?.GetComponent<Renderer>()?.material;
            TryOnRoom = GameObj.GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Cosmetics Room Triggers/TryOnRoom");

            fullModAmount ??= Manager.modules.Values.SelectMany(list => list).ToArray().Length;
        }
    }
}
