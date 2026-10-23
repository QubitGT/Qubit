/*
 * Qubit Menu  Modules/Categories/Presets.cs
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

using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using System;
using System.IO;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Modules.Manager;

namespace Qubit.Modules.Categories
{
    public static class Presets
    {
        #region Methods
        public static void LegitimatePreset()
        {
            string[] presetMods = {
                "Joystick Menu",
                "Thin Menu",
                "Disable Enabled GUI",
                "Disable Board Colors",
                "Disable Disconnect Button",
                "Disable Page Buttons",
                "Disable Search Button",
                "Disable Return Button",
                "Hidden on Camera",
                "Hide Notifications on Camera",
                "Hide Text on Camera",
                "Disable FPS Counter",
                "Fix Rig Colors",
            };

            themeType = 28;
            pageButtonType = 1;
            fontCycle = -1;

            Qubit.Modules.Manager.GetIndex("Change Menu Theme").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Page Type").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Font Type").cycleValue(true);

            Settings.Panic();

            foreach (string mod in presetMods)
                Toggle(mod);

            Notifications.SendFormat("purple", "PRESET", "Legitimate preset enabled successfully.");
        }

        public static void GhostPreset()
        {
            string[] presetMods = {
                $"Ghost {Qubit.Extensions.Strings.Bracket("green", "A")}",
                $"Invisible {Qubit.Extensions.Strings.Bracket("green", "B")}",
                $"Noclip {Qubit.Extensions.Strings.Bracket("green", "T")}",
                "Steam Long Arms",
                "Break Audio Gun",
                "No Finger Movement",
                "Platforms",
                "Change Arm Length"
            };

            Movement.longarmCycle = 2;

            Settings.Panic();

            foreach (string mod in presetMods)
                Toggle(mod);

            Notifications.SendFormat("purple", "PRESET", "Ghost preset enabled successfully.");
        }

        public static void SaveCustomPreset(int id)
        {
            try
            {
                string directory = Path.Combine(PluginInfo.BaseDirectory, "SavedPresets");
                Directory.CreateDirectory(directory);

                string file = Path.Combine(directory, $"Preset_{id}.txt");
                File.WriteAllText(file, Settings.SavePreferencesToText());

                Notifications.SendFormat("purple", "PRESET", "Custom preset saved successfully.");
            }
            catch (Exception ex)
            {
                Logs.LogError($"Failed to save custom preset {id}: {ex}");
                Notifications.SendFormat("red", "ERROR", "Failed to save custom preset.");
            }
        }

        public static void LoadCustomPreset(int id)
        {
            try
            {
                string directory = Path.Combine(PluginInfo.BaseDirectory, "SavedPresets");
                string file = Path.Combine(directory, $"Preset_{id}.txt");

                if (!File.Exists(file))
                {
                    Notifications.SendFormat("red", "ERROR", "Custom preset not found.");
                    return;
                }

                string text = File.ReadAllText(file);
                Logs.Log(text);
                Settings.LoadPreferencesFromText(text);

                Notifications.SendFormat("purple", "PRESET", "Custom preset loaded successfully.");
            }
            catch (Exception ex)
            {
                Logs.LogError($"Failed to load custom preset {id}: {ex}");
                Notifications.SendFormat("red", "ERROR", "Failed to load custom preset.");
            }
        }

        public static void PerformancePreset()
        {
            string[] presetMods = {
                "Disable Enabled GUI",
                "Disable Board Colors",
                "Disable FPS Counter",
                "FPS Boost",
                "Disable Ghostview"
            };

            themeType = 30;
            pageButtonType = 1;
            fontCycle = 0;

            Qubit.Modules.Manager.GetIndex("Change Menu Theme").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Page Type").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Font Type").cycleValue(true);

            Settings.Panic();

            foreach (string mod in presetMods)
                Toggle(mod);

            Notifications.SendFormat("purple", "PRESET", "Performance preset enabled successfully.");
        }

        public static void SafetyPreset()
        {
            string[] presetMods = {
                "No Finger Movement",
                $"Fake Oculus Menu {Qubit.Extensions.Strings.Bracket("green", "X")}",
                "Disable Gamemode Buttons",
                "Anti Crash",
                "Anti Moderator",
                $"Anti Report {Qubit.Extensions.Strings.Bracket("green", "Disconnect")}",
                $"Show Anti Cheat Reports {Qubit.Extensions.Strings.Bracket("green", "Self")}"
            };

            themeType = 33;
            pageButtonType = 1;
            fontCycle = 0;

            Qubit.Modules.Manager.GetIndex("Change Menu Theme").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Page Type").cycleValue(true);
            Qubit.Modules.Manager.GetIndex("Change Font Type").cycleValue(true);

            Settings.Panic();

            foreach (string mod in presetMods)
                Toggle(mod);

            Notifications.SendFormat("purple", "PRESET", "Safety preset enabled successfully.");
        }

        public static void SimplePreset()
        {
            string[] presetMods = {
                "Disable Enabled GUI",
                "Accept TOS",
                "Player Scale Menu",
                "PC Button Click"
            };

            pageButtonType = 2;
            Qubit.Modules.Manager.GetIndex("Change Page Type").cycleValue(true);

            foreach (string mod in presetMods)
                Toggle(mod);

            Notifications.SendFormat("purple", "PRESET", "Simple preset enabled successfully.");
        }
        #endregion
    }
}
