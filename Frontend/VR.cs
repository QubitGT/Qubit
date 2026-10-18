/*
 * Qubit Menu  Frontend/VR.cs
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
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using static Qubit.Extensions.Strings;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Classes.QubitMenuBody;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Modules.Manager;

namespace Qubit.Frontend
{
    public static class VR
    {
        private static bool GetLeftInput(int i) => i switch
        {
            0 => leftPrimary,
            1 => leftSecondary,
            2 => leftGrab,
            3 => leftTrigger > 0.5f,
            4 => leftJoystickClick,
            _ => false
        };

        private static bool GetRightInput(int i) => i switch
        {
            0 => rightPrimary,
            1 => rightSecondary,
            2 => rightGrab,
            3 => rightTrigger > 0.5f,
            4 => rightJoystickClick,
            _ => false
        };

        public static bool GetMenuButtonCondition()
        {
            bool buttonCondition = rightHand ? GetRightInput(menuButtonIndex) : GetLeftInput(menuButtonIndex);

            if (oneHand)
                buttonCondition = rightHand ? GetLeftInput(menuButtonIndex) : GetRightInput(menuButtonIndex);

            if (bothHands)
            {
                buttonCondition = GetRightInput(menuButtonIndex) || GetLeftInput(menuButtonIndex);
                if (buttonCondition)
                    openedwithright = GetRightInput(menuButtonIndex);
            }

            if (!XRSettings.isDeviceActive)
                buttonCondition = false;

            if (wristMenu)
            {
                bool shouldOpen = Vector3.Distance(References.LeftHand.position + References.LeftHand.forward * 0.1f, Controllers.GetTrueRightHand().position) < 0.1f;
                if (rightHand)
                    shouldOpen = Vector3.Distance(Controllers.GetTrueLeftHand().position, References.RightHand.position + References.RightHand.forward * 0.1f) < 0.1f;

                if (shouldOpen && !lastChecker)
                    wristOpen = !wristOpen;

                lastChecker = shouldOpen;

                buttonCondition = wristOpen;
            }

            if (joystickMenu)
            {
                bool shouldOpen = rightJoystickClick;

                if (shouldOpen && !lastChecker)
                {
                    joystickOpen = !joystickOpen;
                    joystickDelay = Time.time + 0.2f;
                }
                lastChecker = shouldOpen;

                buttonCondition = joystickOpen;
            }
            else
                joystickButtonSelected = 0;

            if (physicalMenu)
            {
                if (buttonCondition)
                    physicalOpenPosition = Vector3.zero;

                buttonCondition = true;
            }

            return buttonCondition;
        }

        public static void UpdatePhysicalReference()
        {
            
            if (physicalMenu && menu != null)
            {
                try
                {
                    if (Vector3.Distance(References.BodyCollider.transform.position, menu.transform.position) < 1.5f)
                    {
                        if (reference == null)
                            CreateReference();
                    }
                    else
                    {
                        if (reference != null)
                        {
                            UnityEngine.Object.Destroy(reference);
                            reference = null;
                        }
                    }
                }
                catch { }
            }
        }

        public static void UpdateJoystickMenu()
        {
            bool joystickSelectPressed = leftJoystickClick && !joystickSelectHeld;
            joystickSelectHeld = leftJoystickClick;
            if (joystickMenu && joystickOpen && menu != null)
            {
                Vector2 js = leftJoystick;
                if (Time.time > joystickDelay || joystickSelectPressed)
                {
                    int lastButton = qubitFront != null ? qubitJoystickTargets.Count : PageSize;

                    if (qubitFront == null && joystickMenuSearching)
                        lastButton++;
                    lastButton = Math.Max(1, lastButton);

                    if (joystickSelectPressed)
                    {
                        if (dynamicSounds)
                            Sounds.Play("Select");

                        Module button = Manager.GetIndex(joystickSelectedButton);
                        if (button != null && button.incremental)
                            ToggleIncremental(joystickSelectedButton, leftTrigger < 0.5f);
                        else if (button != null)
                            Toggle(joystickSelectedButton, true);
                        joystickDelay = Time.time + 0.2f;
                    }
                    else if (js.x > 0.5f)
                    {
                        if (dynamicSounds)
                            Sounds.Play("Next");

                        Toggle("NextPage");
                        joystickDelay = Time.time + 0.2f;
                    }
                    else if (js.x < -0.5f)
                    {
                        if (dynamicSounds)
                            Sounds.Play("Previous");

                        Toggle("PreviousPage");
                        joystickDelay = Time.time + 0.2f;
                    }

                    else if (js.y > 0.5f)
                    {
                        if (dynamicSounds)
                            Sounds.Play("Up");

                        joystickButtonSelected--;
                        if (joystickButtonSelected < 0)
                            joystickButtonSelected = lastButton - 1;

                        if (qubitFront != null) UpdateJoystickSelection();
                        else ReloadMenu();
                        joystickDelay = Time.time + 0.2f;
                    }
                    else if (js.y < -0.5f)
                    {
                        if (dynamicSounds)
                            Sounds.Play("Down");

                        joystickButtonSelected++;
                        joystickButtonSelected %= lastButton;

                        if (qubitFront != null) UpdateJoystickSelection();
                        else ReloadMenu();
                        joystickDelay = Time.time + 0.2f;
                    }
                }
            }
        }

        public static void UpdateBarkMenu()
        {
            if (barkMenu)
            {
                if (barkMenuOpen)
                {
                    if (barkMenuGrabbed == null)
                    {
                        if (menuBackground != null)
                        {
                            bool leftGrabbing = Vector3.Distance(References.LeftHand.position, menuBackground.transform.position) < 0.4f && leftGrab;
                            bool rightGrabbing = Vector3.Distance(References.RightHand.position, menuBackground.transform.position) < 0.4f && rightGrab;

                            if (leftGrabbing || rightGrabbing)
                            {
                                barkMenuGrabbed = leftGrabbing;
                                rightHand = rightGrabbing;

                                if (reference != null)
                                {
                                    UnityEngine.Object.Destroy(reference);
                                    reference = null;

                                    CreateReference();
                                }
                            }
                        }
                    }
                    else
                    {
                        bool gripping = barkMenuGrabbed.Value ? leftGrab : rightGrab;

                        if (!gripping)
                        {
                            barkMenuGrabbed = null;
                            barkMenuOpen = false;
                        }
                    }
                }
                else
                {
                    static bool IsBangingPosition(Vector3 position)
                    {
                        Vector3 closestPoint = References.BodyCollider.ClosestPoint(position);
                        return closestPoint.Distance(position) <= 0.1f;
                    }

                    bool bangingChest = IsBangingPosition(Controllers.GetTrueLeftHand().position) || IsBangingPosition(Controllers.GetTrueRightHand().position);

                    if (bangingChest && !previousBarkBangState)
                    {
                        if (Time.time > barkBangDelay)
                            barkBangCount = 0;

                        barkBangDelay = Time.time + 0.5f;
                        barkBangCount++;

                        if (barkBangCount >= 3)
                            barkMenuOpen = true;
                    }

                    previousBarkBangState = bangingChest;
                }
            }
        }

        public static void UpdateWatchMenu()
        {
            try
            {
                if (watchMenu)
                {
                    Module[] toSortOf = Manager.modules[Manager.CurrentCategoryName];

                    if (Manager.CurrentCategoryName == "Favorite Mods")
                        toSortOf = StringsToInfos(favorites.ToArray());

                    if (Manager.CurrentCategoryName == "Enabled Mods")
                    {
                        List<Module> enabledMods = new List<Module>();
                        foreach (KeyValuePair<string, Module[]> categoryEntry in Manager.modules)
                        {
                            Module[] buttonList = categoryEntry.Value;
                            enabledMods.AddRange(buttonList.Where(v => v.enabled && (!hideSettings || !categoryEntry.Key.Contains("Settings")) && (!hideMacros || !categoryEntry.Key.Contains("Macro"))));
                        }
                        enabledMods = enabledMods.OrderBy(v => v.overlapText ?? v.buttonText).ToList();
                        enabledMods.Insert(0, Manager.GetIndex("Exit Enabled Mods"));
                        toSortOf = enabledMods.ToArray();
                    }

                    TextMeshProUGUI watchText = Watches[0].text;

                    string text = toSortOf[watchMenuIndex].buttonText;

                    if (toSortOf[watchMenuIndex].overlapText != null)
                        text = toSortOf[watchMenuIndex].overlapText;

                    text += $"\n<color=grey>[{watchMenuIndex + 1}/{toSortOf.Length}]\n{DateTime.Now:hh:mm tt}</color>";

                    text = FollowMenuSettings(text, false);

                    watchText.SafeSetText(text);

                    if (watchIndicatorMat == null)
                        watchIndicatorMat = new Material(Shader.Find("GorillaTag/UberShader"));

                    watchIndicatorMat.color = toSortOf[watchMenuIndex].enabled ? buttonColors[1].GetCurrentColor() : buttonColors[0].GetCurrentColor();
                    Watches[0].indicator.material = watchIndicatorMat;

                    Vector2 js = rightHand ? rightJoystick : leftJoystick;
                    if (Time.time > wristMenuDelay)
                    {
                        if (js.x > 0.5f || (rightHand ? js.y < -0.5f : js.y > 0.5f))
                        {
                            watchMenuIndex++;
                            if (watchMenuIndex > toSortOf.Length - 1)
                                watchMenuIndex = 0;

                            wristMenuDelay = Time.time + 0.2f;
                        }
                        if (js.x < -0.5f || (rightHand ? js.y > 0.5f : js.y < -0.5f))
                        {
                            watchMenuIndex--;
                            if (watchMenuIndex < 0)
                                watchMenuIndex = toSortOf.Length - 1;

                            wristMenuDelay = Time.time + 0.2f;
                        }
                        if (rightHand ? rightJoystickClick : leftJoystickClick)
                        {
                            string archive = Manager.CurrentCategoryName;
                            Toggle(toSortOf[watchMenuIndex].buttonText, true);
                            if (Manager.CurrentCategoryName != archive)
                                watchMenuIndex = 0;

                            wristMenuDelay = Time.time + 0.2f;
                        }
                    }
                }
            }
            catch { }
        }

        public static void PositionMenu()
        {
            if (joystickMenu)
            {
                menu.transform.position = References.HeadCollider.transform.TransformPoint(joystickMenuPositions[joystickMenuPosition]);
                if (qubitFront != null)
                {
                    var head = References.HeadCollider.transform;
                    menu.transform.rotation = Quaternion.LookRotation(menu.transform.position - head.position, head.up)
                        * Quaternion.Inverse(QubitFaceRotation);
                }
                else
                {
                    menu.transform.LookAt(References.HeadCollider.transform);
                    Vector3 rotModify = menu.transform.rotation.eulerAngles;
                    rotModify += new Vector3(-90f, 0f, -90f);
                    menu.transform.rotation = Quaternion.Euler(rotModify);
                }
            }
            else
            {
                if (!wristMenu)
                {
                    if (oneHand)
                    {
                        menu.transform.position = References.HeadCollider.transform.TransformPoint(new Vector3(0f, -0.1f, 0.5f));
                        menu.transform.LookAt(References.HeadCollider.transform);
                        Vector3 rotModify = menu.transform.rotation.eulerAngles;
                        rotModify += new Vector3(-90f, 0f, -90f);
                        menu.transform.rotation = Quaternion.Euler(rotModify);
                    }
                    else if (barkMenu && barkMenuOpen)
                    {
                        if (barkMenuGrabbed != null)
                        {
                            if (barkMenuGrabbed.Value)
                            {
                                menu.transform.position = References.LeftHand.position;
                                menu.transform.rotation = References.LeftHand.rotation;
                            }
                            else
                            {
                                menu.transform.position = References.RightHand.position;
                                Vector3 rotation = References.RightHand.rotation.eulerAngles;
                                rotation += new Vector3(0f, 0f, 180f);
                                menu.transform.rotation = Quaternion.Euler(rotation);
                            }
                        }
                        else
                        {
                            menu.transform.position = References.BodyCollider.transform.TransformPoint(new Vector3(0f, 0f, 0.5f));
                            menu.transform.position = new Vector3(menu.transform.position.x, References.HeadCollider.transform.position.y, menu.transform.position.z);
                            menu.transform.LookAt(References.BodyCollider.transform);
                            menu.transform.rotation = Quaternion.Euler(0f, menu.transform.eulerAngles.y, 0f);
                            Vector3 rotModify = menu.transform.rotation.eulerAngles;
                            rotModify += new Vector3(-90f, 0f, -90f);
                            menu.transform.rotation = Quaternion.Euler(rotModify);
                        }
                    }
                    else
                    {
                        if (VR.UsesRightHand())
                        {
                            menu.transform.position = References.RightHand.position;
                            Vector3 rotation = References.RightHand.rotation.eulerAngles;
                            rotation += new Vector3(0f, 0f, 180f);
                            menu.transform.rotation = Quaternion.Euler(rotation);
                        }
                        else
                        {
                            menu.transform.position = References.LeftHand.position;
                            menu.transform.rotation = References.LeftHand.rotation;
                        }
                        if (flipMenu)
                        {
                            Vector3 rotation = menu.transform.rotation.eulerAngles;
                            rotation += new Vector3(0f, 0f, 180f);
                            menu.transform.rotation = Quaternion.Euler(rotation);
                        }
                    }
                }
                else
                {
                    menu.transform.localPosition = Vector3.zero;
                    menu.transform.localRotation = Quaternion.identity;
                    menu.transform.position = rightHand
                        ? References.RightHand.position + new Vector3(0f, 0.3f, 0f)
                        : References.LeftHand.position + new Vector3(0f, 0.3f, 0f);

                    menu.transform.LookAt(References.HeadCollider.transform.position);
                    Vector3 rotModify = menu.transform.rotation.eulerAngles;
                    rotModify += new Vector3(-90f, 0f, -90f);
                    menu.transform.rotation = Quaternion.Euler(rotModify);
                }
            }
        }

        public static void PositionMenuForKeyboard()
        {
            if (inTextInput && !isKeyboardPc && !clickGUI)
            {
                menu.transform.position = menuSpawnPosition.transform.position;
                if (VRKeyboard != null && VRKeyboard.GetComponent<Keyboard.View>() != null)
                    menu.transform.rotation = menuSpawnPosition.transform.rotation * Quaternion.Euler(-90f, 90f, 0f);
                else
                {
                    menu.transform.rotation = menuSpawnPosition.transform.rotation;
                    Vector3 rotModify = menu.transform.rotation.eulerAngles;
                    rotModify += new Vector3(-90f, 90f, -90f);
                    menu.transform.rotation = Quaternion.Euler(rotModify);
                }
            }
        }

        public static void HoldPhysicalMenu()
        {
            if (physicalMenu)
            {
                if (physicalOpenPosition == Vector3.zero)
                {
                    physicalOpenPosition = menu.transform.position;
                    physicalOpenRotation = menu.transform.rotation;
                }

                menu.transform.position = physicalOpenPosition;
                menu.transform.rotation = physicalOpenRotation;
            }
        }

        public static bool UsesRightHand() =>
            rightHand || (bothHands && References.InputPoller.rightControllerSecondaryButton);

        public static Ray GetHandRay(Vector3 position, Vector3 forward) =>
            new Ray(position, forward);

        public static bool IsTriggerPressed(bool left) =>
            (left ? Controllers.leftTrigger : Controllers.rightTrigger) > .5f;
        public static bool? barkMenuGrabbed;

        public static float barkBangDelay;
        public static int barkBangCount;

        public static bool previousBarkBangState;
        public static Quaternion physicalOpenRotation = Quaternion.identity;
        public static float joystickDelay;
        public static bool wristOpen;
        public static float wristMenuDelay;
        public static bool lastChecker;

        public static Material watchIndicatorMat;
        public static int watchMenuIndex;
    }
}
