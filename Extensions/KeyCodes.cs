/*
 * Qubit Menu  Extensions/KeyCodes.cs
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

using System.Collections.Generic;
using UnityEngine;

namespace Qubit.Extensions
{
    public static class KeyCodes
    {

        private static readonly Dictionary<KeyCode, string> normalMap = new Dictionary<KeyCode, string>
        {
            { KeyCode.Space, " " },
            { KeyCode.Comma, "," },
            { KeyCode.Period, "." },
            { KeyCode.Slash, "/" },
            { KeyCode.Backslash, "\\" },
            { KeyCode.Minus, "-" },
            { KeyCode.Equals, "=" },
            { KeyCode.Semicolon, ";" },
            { KeyCode.Quote, "'" },
            { KeyCode.LeftBracket, "[" },
            { KeyCode.RightBracket, "]" }
        };

        private static readonly Dictionary<KeyCode, string> shiftMap = new Dictionary<KeyCode, string>
        {
            { KeyCode.Alpha1, "!" },
            { KeyCode.Alpha2, "@" },
            { KeyCode.Alpha3, "#" },
            { KeyCode.Alpha4, "$" },
            { KeyCode.Alpha5, "%" },
            { KeyCode.Alpha6, "^" },
            { KeyCode.Alpha7, "&" },
            { KeyCode.Alpha8, "*" },
            { KeyCode.Alpha9, "(" },
            { KeyCode.Alpha0, ")" },

            { KeyCode.Minus, "_" },
            { KeyCode.Equals, "+" },
            { KeyCode.LeftBracket, "{" },
            { KeyCode.RightBracket, "}" },
            { KeyCode.Backslash, "|" },
            { KeyCode.Semicolon, ":" },
            { KeyCode.Quote, "\"" },
            { KeyCode.Comma, "<" },
            { KeyCode.Period, ">" },
            { KeyCode.Slash, "?" }
        };

        public static string Key(this KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
                return key.ToString().ToLower();

            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return ((char)('0' + (key - KeyCode.Alpha0))).ToString();

            if (normalMap.TryGetValue(key, out string value))
                return value;

            switch (key)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return "\n";
                case KeyCode.Tab: return "\t";
                default: return "";
            }
        }

        public static string ShiftedKey(this KeyCode key)
        {
            return key >= KeyCode.A && key <= KeyCode.Z ? key.ToString() : shiftMap.TryGetValue(key, out string shifted) ? shifted : key.Key();
        }
    }
}
