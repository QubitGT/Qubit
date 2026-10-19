/*
 * Qubit Menu  Backend/VanillaPropsBaseline.cs
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
using Qubit.Libraries.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Qubit.Backend
{
    /// <summary>
    /// The custom player properties an unmodified game sets on a player.
    /// Anything else on a player is reported by the mod name tags as an unknown property, which usually means another mod.
    /// </summary>
    public static class VanillaPropsBaseline
    {
        private const string FileName = "VanillaProps.json";

        private static readonly string[] DefaultKeys =
        {
            "didtutorial",
            "platform",
            "mothershipid",
            "gtfcexpirationtime"
        };

        private static HashSet<string> vanillaKeys = new HashSet<string>();

        public static bool IsLoaded => vanillaKeys.Count > 0;

        private static string FilePath => Path.Combine(PluginInfo.BaseDirectory, FileName);

        private static string NormalizeKey(object key) =>
            (key?.ToString() ?? "").Trim().ToLowerInvariant();

        /// <summary>
        /// Saves the local player's current custom properties as the baseline. Run it on a clean session.
        /// </summary>
        public static bool Capture()
        {
            try
            {
                Hashtable properties = Photon.Pun.PhotonNetwork.LocalPlayer.CustomProperties;
                if (properties == null || properties.Count == 0)
                {
                    Logs.LogError("Cannot capture vanilla props: LocalPlayer.CustomProperties is empty. Join a room first.");
                    return false;
                }

                var keys = new List<string>();
                foreach (DictionaryEntry entry in properties)
                {
                    string key = NormalizeKey(entry.Key);
                    if (key.Length > 0)
                        keys.Add(key);
                }

                vanillaKeys = new HashSet<string>(keys);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(keys, Formatting.Indented));
                Logs.Log($"Captured {vanillaKeys.Count} vanilla custom property keys to {FilePath}.");
                return true;
            }
            catch (Exception exception)
            {
                Logs.LogError($"Failed to capture vanilla props: {exception.Message}");
                return false;
            }
        }

        [RunOnLaunch(LaunchPriority.VanillaProps)]
        public static void Load()
        {
            vanillaKeys = new HashSet<string>(DefaultKeys);

            try
            {
                if (!File.Exists(FilePath))
                    return;

                var savedKeys = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(FilePath));
                if (savedKeys != null)
                    vanillaKeys.UnionWith(savedKeys);
            }
            catch (Exception exception)
            {
                Logs.LogError($"Failed to load vanilla props baseline: {exception.Message}");
            }

            Logs.Log($"Loaded {vanillaKeys.Count} vanilla custom property keys.");
        }

        public static IEnumerable<string> FindUnknownKeys(Hashtable properties)
        {
            if (!IsLoaded || properties == null)
                yield break;

            foreach (DictionaryEntry entry in properties)
            {
                string key = NormalizeKey(entry.Key);
                if (key.Length > 0 && !vanillaKeys.Contains(key))
                    yield return key;
            }
        }
    }
}
