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
using Qubit.Backend;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules.Categories;
using Qubit.Patches;
using Qubit.Patches.Terms;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;
using static Qubit.Backend.Preferences;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Settings;
using Console = Qubit.Backend.Console;

namespace Qubit.Core
{
    public enum LaunchPriority
    {
        Settings,
        Fonts,
        Details,
        DonationState,
        Rooms,
        Serialization,
        Visuals,
        Scene,
        ModuleTags,
        Plugins,
        Sound,
        Macros,
        Preferences,
        PCControls,
        VanillaProps,
        Achievements,
        PatchReport,
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class RunOnLaunchAttribute : Attribute
    {
        public LaunchPriority? Priority { get; }

        public RunOnLaunchAttribute() { }

        public RunOnLaunchAttribute(LaunchPriority priority) =>
            Priority = priority;
    }

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
            TermsOfService.enabled = Preferences.IsSavedEnabled(TermsOfService.ButtonName);
            if (File.Exists($"{PluginInfo.BaseDirectory}/Qubit_DisableTelemetry.txt"))
                ServerData.DisableTelemetry = true;
            GorillaTagger.OnPlayerSpawned(LoadMenu);
        }

        private void LoadMenu()
        {
            PatchHandler.PatchAll();
            Coroutines coroutineManager = gameObject.AddComponent<Coroutines>();
            AddOnStartAttribute.Apply(gameObject, false);
            Console.Initialize();
            AddOnStartAttribute.Apply(gameObject, true);
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

            RunLaunchMethods();
        }

        private static void RunLaunchMethods()
        {
            Type[] types;
            try
            {
                types = typeof(Instance).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).ToArray();
            }

            var launchMethods = types
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Select(method => (method, attribute: method.GetCustomAttribute<RunOnLaunchAttribute>()))
                .Where(entry => entry.attribute != null)
                .OrderBy(entry => (int?)entry.attribute.Priority ?? int.MaxValue);

            foreach (var (method, _) in launchMethods)
            {
                if (method.GetParameters().Length > 0)
                {
                    Logs.LogError($"[RunOnLaunch] {method.DeclaringType?.Name}.{method.Name} takes parameters and was skipped.");
                    continue;
                }

                try
                {
                    method.Invoke(null, null);
                }
                catch (TargetInvocationException exception)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                }
            }
        }
    }
}