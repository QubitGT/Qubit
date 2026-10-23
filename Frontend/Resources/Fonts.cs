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
using TMPro;
using UnityEngine;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Modules.Categories.Settings;

namespace Qubit.Frontend.Resources
{
    public static class Fonts
    {
        public static void Apply(GameObject root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = activeFont;
                text.fontSharedMaterial = text.font.material;
                text.fontStyle = activeFontStyle;
                text.extraPadding = true;
                text.UpdateMeshPadding();
                text.SetAllDirty();
            }
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
                    break;
                case 1:
                    activeFont = FreeSans;
                    break;
                case 2:
                    activeFont = DejaVuSans;
                    break;
                case 3:
                    activeFont = Utopium;
                    break;
                case 4:
                    activeFont = ComicSans;
                    break;
                case 5:
                    activeFont = CascadiaMono;
                    break;
                case 6:
                    activeFont = Candara;
                    break;
                case 7:
                    activeFont = MSGothic;
                    break;
                case 8:
                    activeFont = Anton;
                    break;
                case 9:
                    activeFont = SimSun;
                    break;
                case 10:
                    activeFont = Minecraft;
                    break;
                case 11:
                    activeFont = Terminal;
                    break;
                case 12:
                    activeFont = OpenDyslexic;
                    break;
                case 13:
                    activeFont = Taiko;
                    break;
                case 14:
                    activeFont = LiberationSans;
                    break;
            }
            if (Frontend.Settings.menu != null) Apply(Frontend.Settings.menu);
            if (Keyboard.VRKeyboard != null) Apply(Keyboard.VRKeyboard);
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
            InitializeFonts();
            ApplyFontType(9);
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
    }
}
