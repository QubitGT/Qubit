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

        private bool isOpen = true;
        private GameObject uiPrefab;
        private QubitDesktopOverlay desktopOverlay;

        private void Awake()
        {
            Instance = this;
            isOpen = !File.Exists(hideGUIPath);
            uiPrefab = new GameObject("Qubit Desktop UI");
            desktopOverlay = QubitDesktopOverlay.Attach(uiPrefab.transform);
            if (desktopOverlay == null)
                Logs.LogError("The Qubit desktop interface could not load.");
            uiPrefab.SetActive(isOpen);
        }

        private void Update()
        {
            if (uiPrefab == null) return;
            if (UnityEngine.InputSystem.Keyboard.current?.backslashKey.wasPressedThisFrame == true)
                ToggleGUI();
            uiPrefab.SetActive(isOpen);
            if (isOpen && desktopOverlay != null) desktopOverlay.Refresh();
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

        public void DebugPrint(string text) => Logs.Log(text);

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

        private void OnGUI() 
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
