/*
 * Qubit Menu  Core/Instance.cs
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
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Frontend;
using Qubit.Frontend.Resources;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using Qubit.Modules.Categories;
using Qubit.Patches;
using Qubit.Patches.Menu;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using static Qubit.Backend.Preferences;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Settings;
using Console = Qubit.Backend.Console;

namespace Qubit.Core
{
    public class Instance : MonoBehaviour
    {
        private const float IntegrityCheckIntervalSeconds = 120f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Bootstrapper.FirstLaunch = !Directory.Exists(PluginInfo.BaseDirectory);
            string[] existingDirectories =
            {
                "",
                "/Sounds",
                "/Plugins",
                "/Backups",
                "/Macros",
                "/TTS",
                "/PlayerInfo",
                "/CustomScripts",
                "/Friends",
                "/Friends/Messages",
                "/Achievements"
            };
            foreach (string dir in existingDirectories)
            {
                string target = $"{PluginInfo.BaseDirectory}{dir}";
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);
            }
            PatchHandler.PatchAll(true);
            if (File.Exists($"{PluginInfo.BaseDirectory}/Qubit_Preferences.txt"))
            {
                if (File.ReadAllLines($"{PluginInfo.BaseDirectory}/Qubit_Preferences.txt")[0]
                    .Split(";;")
                    .Contains("Accept TOS"))
                {
                    TOSPatches.enabled = true;
                }
            }
            if (File.Exists($"{PluginInfo.BaseDirectory}/Qubit_DisableTelemetry.txt"))
                ServerData.DisableTelemetry = true;
            GorillaTagger.OnPlayerSpawned(LoadMenu);
        }

        private void LoadMenu()
        {
            PatchHandler.PatchAll();
            Coroutines coroutineManager = gameObject.AddComponent<Coroutines>();
            gameObject.AddComponent<Notifications>();
            gameObject.AddComponent<CustomBoards>();
            gameObject.AddComponent<UI>();
            coroutineManager.StartCoroutine(PatchIntegrityLoop());
        }

        private static IEnumerator PatchIntegrityLoop()
        {
            var wait = new WaitForSeconds(IntegrityCheckIntervalSeconds);

            while (true)
            {
                if (PatchHandler.instance != null)
                {
                    try
                    {
                        PatchHandler.PatchIntegrityCheck();
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"PatchIntegrityCheck threw: {ex}");
                    }
                }

                yield return wait;
            }
        }

        public static void UnloadMenu()
        {
            Preferences.RunWithoutSaving(Modules.Categories.Settings.Panic);

            CustomBoards.CustomBoardsEnabled = false;
            CustomBoards.CustomBoardFonts = false;

            Rooms.Shutdown();

            if (Console.instance != null)
                Destroy(Console.instance.gameObject);

            if (Notifications.Instance != null)
            {
                Destroy(Notifications.Instance.canvas);
                Destroy(Notifications.arraylistText);
                Destroy(Notifications.notificationText);
                Destroy(Notifications.Instance.gameObject);
            }

            if (VRKeyboard != null)
            {
                Destroy(VRKeyboard);
                VRKeyboard = null;
            }

            if (CustomBoards.instance.motdTitle != null)
            {
                Destroy(CustomBoards.instance.motdTitle);
                CustomBoards.instance.motdTitle = null;
            }

            if (CustomBoards.instance.motdText != null)
            {
                Destroy(CustomBoards.instance.motdText);
                CustomBoards.instance.motdText = null;
            }

            if (menuBackground != null)
            {
                Destroy(menuBackground);
                menuBackground = null;
            }

            if (menu != null)
            {
                Destroy(menu);
                menu = null;
            }

            if (reference != null)
            {
                Destroy(reference);
                reference = null;
            }

            if (lKeyReference != null)
            {
                Destroy(lKeyReference);
                lKeyReference = null;
            }

            if (rKeyReference != null)
            {
                Destroy(rKeyReference);
                rKeyReference = null;
            }

            try
            {
                Visuals.ClearLinePool();
                Visuals.ClearNameTagPool();
            }
            catch { }

            HasLoaded = false;
            hasLoadedPreferences = false;
            loadPreferencesTime = -1;

            Lockdown = true;

            PatchHandler.UnpatchAll();

            if (Bootstrapper.Loader)
                Destroy(Bootstrapper.Loader);

        }

        public static void Launch()
        {
            if (Coroutines.instance == null)
                Logs.LogError("CoroutineManager instance is null on menu launch. Features may not function properly.");

            if (Notifications.Instance == null)
                Logs.LogError("NotificationManager instance is null on menu launch. Features may not function properly.");

            Frontend.Settings.Initialize();

            Fonts.Initialize();
            QubitDetails.Register();

            Preferences.LoadDonationState();

            Rooms.Initialize();
            Serialization.Initialize();

            Visuals.Initialize();
            Frontend.Settings.InitializeScene();

            Console.Initialize();
            Manager.TagNewModules();

            Plugins.Initialize();
            Sound.Initialize();
            Movement.Initialize();
            Preferences.LoadWithRetry();
            Modules.Categories.Settings.InitializePCControls();

            Achievements.CheckVeteran();
            PatchHandler.ReportErrors();
        }
    }
}