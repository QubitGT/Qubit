/*
 * Qubit Menu  Libraries/Core/Achievements.cs
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
using Qubit.Libraries.Interface;
using Qubit.Modules;
using Qubit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Valve.Newtonsoft.Json.Linq;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Utilities.Assets;

namespace Qubit.Libraries.Core
{
    public static class Achievements
    {
        private static List<Achievement> _achievements;
        public static List<Achievement> List
        {
            get
            {
                if (_achievements != null) return _achievements;
                _achievements = new List<Achievement>();

                string[] files = Directory.GetFiles($"{PluginInfo.BaseDirectory}/Achievements");
                foreach (string file in files)
                {
                    if (file.EndsWith(".json"))
                        _achievements.Add(Achievement.FromJObject(JObject.Parse(File.ReadAllText(file))));
                }

                return _achievements;
            }
            set => _achievements = value;
        }

        public static void LoadTab()
        {
            int achievementCount = List.Count;

            List<Module> achievementButtons = new List<Module> { new Module { buttonText = "Exit Achievements", method = () => Manager.CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page." } };

            if (achievementCount <= 0)
                achievementButtons.Add(
                    new Module
                    {
                        buttonText = "You have no achievements.",
                        label = true,
                        legal = true
                    });
            else
                for (int i = 0; i < achievementCount; i++)
                {
                    Achievement achievement = List[i];
                    achievementButtons.Add(
                        new Module
                        {
                            buttonText = $"Achievement{i}",
                            overlapText = achievement.name,
                            method = () => PromptSingle($"{achievement.description}<{PluginInfo.ServerResourcePath}/{achievement.icon}>", null, "Done"),
                            isTogglable = false,
                            toolTip = achievement.description,
                            legal = true
                        });
                }

            Manager.modules["Achievements"] = achievementButtons.ToArray();
            Manager.CurrentCategoryName = "Achievements";
        }

        public static bool Has(string name) =>
            List.Any(a => a.name == name);

        public static void Unlock(Achievement achievement)
        {
            if (Has(achievement.name))
                return;

            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/achievement.ogg", "Audio/Menu/achievement.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
            Notifications.SendFormat("purple", "ACHIEVEMENT", $"Achievement unlocked! \"{achievement.name}\"");

            List.Add(achievement);
            File.WriteAllText($"{PluginInfo.BaseDirectory}/Achievements/{achievement.name.Hash()}.json", achievement.ToJObject().ToString());
        }

        public struct Achievement
        {
            public string name;

            public string description;
            public string icon;

            public readonly JObject ToJObject() => new JObject
            {
                ["name"] = name,

                ["description"] = description,
                ["icon"] = icon
            };

            public static Achievement FromJObject(JObject obj) => new Achievement
            {
                name = (string)obj["name"],
                description = (string)obj["description"],
                icon = (string)obj["icon"]
            };
        }

        [RunOnLaunch(14)]
        public static void CheckVeteran()
        {
            if (new DirectoryInfo(Path.Combine(Files.GetGamePath(), PluginInfo.ClientResourcePath)).CreationTime >= DateTime.Now.AddYears(1))
                Unlock(new Achievement
                {
                    name = "Veteran",
                    description = "Use the menu for over a year.",
                    icon = "Images/Achievements/veteran.png"
                });
        }
    }
}
