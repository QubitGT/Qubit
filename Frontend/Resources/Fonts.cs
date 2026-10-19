/*
 * Qubit Menu  Frontend/Resources/Fonts.cs
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
using Qubit.Backend;
using Qubit.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.UI;
using static Qubit.Modules.Categories.Settings;
using static Qubit.Utilities.Assets;

namespace Qubit.Frontend.Resources
{
 public static class Fonts
    {
        private const string Default = "Inter";
        private static readonly Dictionary<string, TMP_FontAsset> cache = new Dictionary<string, TMP_FontAsset>();
        private static readonly HashSet<TMP_FontAsset> stereoFonts = new HashSet<TMP_FontAsset>();

        private static void PrepareStereoFont(TMP_FontAsset font)
        {
            if (font == null || stereoFonts.Contains(font)) return;
            var gameFont = LiberationSans ?? TMP_Settings.defaultFontAsset;
            var shader = gameFont != null && gameFont.material != null ? gameFont.material.shader : null;
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("The game's TMP shader is unavailable for stereo text.");
            var material = new Material(font.material) { shader = shader, name = font.name + " Stereo Material" };
            material.enableInstancing = true;
            font.material = material;
            stereoFonts.Add(font);
            foreach (var weight in font.fontWeightTable)
            {
                PrepareStereoFont(weight.regularTypeface);
                PrepareStereoFont(weight.italicTypeface);
            }
        }
        private static List<string> options;
        private static Module setting;
        private static string selected = Default;

        public static IReadOnlyList<string> Options => options;
        public static string Selected => selected;

        public static void Register()
        {
            if (options != null) return;
            options = new[] { Default }.Concat(Font.GetOSInstalledFontNames()
                .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).Select(name => "Windows: " + name)).ToList();
            setting = new Module {
                name = "Qubit Font", displayName = "Font: " + Default, isTogglable = false,
                incremental = true, isSetting = true, value = Default, legal = true,
                toolTip = "Changes the menu and keyboard font. Uses Qubit's clean default or an installed Windows font."
            };
            setting.cycleValue = Cycle;
            setting.method = () => Cycle(true);
            setting.onValueChanged = Restore;
            Manager.AddModule("Menu Settings", setting, 1);
            Restore();
        }

        private static void Cycle(bool forward)
        {
            int index = Math.Max(0, options.IndexOf(selected));
            Select(options[(index + (forward ? 1 : options.Count - 1)) % options.Count]);
        }

        private static void Restore()
        {
            string name = setting.GetValue<string>();
            selected = options.Contains(name) ? name : Default;
            setting.value = selected;
            setting.displayName = "Font: " + selected;
            if (Frontend.Settings.menu != null) Apply(Frontend.Settings.menu);
            if (Keyboard.VRKeyboard != null) Apply(Keyboard.VRKeyboard);
        }

        public static void Apply(GameObject root)
        {
            var font = Resolve(selected);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = text.name == "Indicator" || text.transform.parent.name == "Search"
                    ? Resolve(Default) : font;
                text.fontSharedMaterial = text.font.material;
                text.extraPadding = true;
                text.UpdateMeshPadding();
                text.SetAllDirty();
            }
        }

        public static void Select(string name)
        {
            if (!options.Contains(name)) return;
            setting.value = name;
            Restore();
            Preferences.SaveButton(setting);
        }

        public static TMP_FontAsset Resolve(string name)
        {
            if (cache.TryGetValue(name, out var font) && font != null) return font;
            if (name.StartsWith("Windows: ", StringComparison.Ordinal))
            {
                var source = Font.CreateDynamicFontFromOSFont(name.Substring(9), 96);
                font = TMP_FontAsset.CreateFontAsset(source, 96, 12, GlyphRenderMode.SDFAA,
                    2048, 2048, AtlasPopulationMode.Dynamic, true);
            }
            else font = QubitAssets.Load<TMP_FontAsset>("Qubit Inter Regular SDF");
            if (font == null) font = QubitAssets.Load<TMP_FontAsset>("Qubit Inter Regular SDF");
            PrepareStereoFont(font);
            cache[name] = font;
            return font;
        }
        public static void InitializeFonts()
        {
            AgencyFB ??= LoadAsset<TMP_FontAsset>("Agency");
            FreeSans ??= LoadAsset<TMP_FontAsset>("FreeSans");
            Candara ??= LoadAsset<TMP_FontAsset>("Candara");
            ComicSans ??= LoadAsset<TMP_FontAsset>("ComicSans");
            CascadiaMono ??= LoadAsset<TMP_FontAsset>("CascadiaMono");
            Anton ??= LoadAsset<TMP_FontAsset>("Anton");
            Minecraft ??= LoadAsset<TMP_FontAsset>("Minecraft");
            MSGothic ??= LoadAsset<TMP_FontAsset>("MSGothic");
            OpenDyslexic ??= LoadAsset<TMP_FontAsset>("OpenDyslexic");
            SimSun ??= LoadAsset<TMP_FontAsset>("SimSun");
            Taiko ??= LoadAsset<TMP_FontAsset>("Taiko");
            Terminal ??= LoadAsset<TMP_FontAsset>("Terminal");
            Utopium ??= LoadAsset<TMP_FontAsset>("Utopium");
            DejaVuSans ??= LoadAsset<TMP_FontAsset>("DejaVuSans");

            foreach (TMP_FontAsset font in new[] { AgencyFB, FreeSans, Candara, ComicSans,
                CascadiaMono, Anton, Minecraft, MSGothic, OpenDyslexic, SimSun, Taiko,
                Terminal, Utopium, DejaVuSans })
                font.fallbackFontAssetTable.Add(LiberationSans);
        }

        
        public static TMP_FontAsset activeFont = LiberationSans;
        public static FontStyles activeFontStyle = FontStyles.Normal;

        public static TMP_FontAsset AgencyFB;
        public static TMP_FontAsset FreeSans;
        public static TMP_FontAsset Candara;
        public static TMP_FontAsset ComicSans;
        public static TMP_FontAsset CascadiaMono;
        public static TMP_FontAsset Anton;
        public static TMP_FontAsset Minecraft;
        public static TMP_FontAsset MSGothic;
        public static TMP_FontAsset OpenDyslexic;
        public static TMP_FontAsset SimSun;
        public static TMP_FontAsset Taiko;
        public static TMP_FontAsset Terminal;
        public static TMP_FontAsset Utopium;
        public static TMP_FontAsset DejaVuSans;
        public static TMP_FontAsset LiberationSans = UnityEngine.Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        public static int fontCycle;

        public static void ApplyFontType(int index)
        {
            fontCycle = index;
            switch (index)
            {
                case 0:
                    activeFont = AgencyFB;
                    return;
                case 1:
                    activeFont = FreeSans;
                    return;
                case 2:
                    activeFont = DejaVuSans;
                    return;
                case 3:
                    activeFont = Utopium;
                    return;
                case 4:
                    activeFont = ComicSans;
                    return;
                case 5:
                    activeFont = CascadiaMono;
                    return;
                case 6:
                    activeFont = Candara;
                    return;
                case 7:
                    activeFont = MSGothic;
                    return;
                case 8:
                    activeFont = Anton;
                    return;
                case 9:
                    activeFont = SimSun;
                    return;
                case 10:
                    activeFont = Minecraft;
                    return;
                case 11:
                    activeFont = Terminal;
                    return;
                case 12:
                    activeFont = OpenDyslexic;
                    return;
                case 13:
                    activeFont = Taiko;
                    return;
                case 14:
                    activeFont = LiberationSans;
                    return;
            }
        }
        public static void ApplyFontStyleType(int index)
        {
            fontStyleType = index;
            activeFontStyle = index switch
            {
                0 => FontStyles.Normal,
                1 => FontStyles.Bold,
                2 => FontStyles.Italic,
                3 => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        [RunOnLaunch(LaunchPriority.Fonts)]
        public static void Initialize()
        {
            Register();
            InitializeFonts();
            activeFont = AgencyFB;
        }
    }
}
