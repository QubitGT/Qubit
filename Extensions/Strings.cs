/*
 * Qubit Menu  Extensions/Strings.cs
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
using Qubit.Libraries.Language;
using System;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Settings;

namespace Qubit.Extensions
{
    public static class Strings
    {
        public static string ClearTags(this string input) =>
            NoRichtextTags(input);

        public static string Hash(this string input) =>
            GetSHA256(input);

        public static string EnforceLength(this string str, int maxLength) =>
            str.Length > maxLength ? str[..maxLength] : str;

        public static string Random(this string _, int length) =>
            RandomString(length);

        public static string ToTitleCase(this string text) =>
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());

        /// <summary>
        /// Applies configured menu text transformations to the specified input string, including optional translation
        /// and case adjustments.
        /// </summary>
        /// <remarks>The transformations applied depend on the current values of the menu settings, such
        /// as translation, lowercase, and uppercase modes. If both lowercase and uppercase modes are enabled, the
        /// uppercase transformation takes precedence.</remarks>
        /// <param name="input">The input string to be processed and transformed according to the current menu settings.</param>
        /// <param name="translateText">true to translate the input string before applying case transformations; otherwise, false. The default is
        /// true.</param>
        /// <returns>A string containing the transformed input, with translation and case modifications applied as specified by
        /// the current settings.</returns>
        public static string FollowMenuSettings(string input, bool translateText = true, bool reloadOnTranslate = false)
        {
            Action<string> onTranslate = null;
            if (reloadOnTranslate)
                onTranslate = (_) => TranslationReloadDelay();

            if (translateText && translate)
                input = Translations.TranslateText(input, onTranslate);

            if (lowercaseMode)
                input = input.ToLower();

            if (uppercaseMode)
                input = input.ToUpper();

            if (!redactText) return input;
            var result = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
                result[i] = input[i] == ' ' ? ' ' : '█';

            input = new string(result);

            return input;
        }

        private static Coroutine translationCoroutine;
        public static void TranslationReloadDelay()
        {
            if (translationCoroutine != null)
            {
                Coroutines.instance.StopCoroutine(translationCoroutine);
                translationCoroutine = null;
            }

            translationCoroutine = Coroutines.instance.StartCoroutine(TranslationReloadCoroutine());
        }

        public static IEnumerator TranslationReloadCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            ReloadMenu();
        }

        private static readonly Regex notagsRegex = new Regex("<.*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string NoRichtextTags(string input, string replace = "") =>
            input.IndexOf('<') < 0 ? input : notagsRegex.Replace(input, replace);

        public static bool vibrantColors;
        public static string FixTMProTags(string input)
        {
            if (vibrantColors)
                return input;

            input = input.Replace("<color=green>", "<color=#008000>");
            input = input.Replace("<color=purple>", "<color=#800080>");

            return input;
        }

        public static string NoColorTags(string input, string replace = "")
        {
            Regex notags = new Regex(@"<color=.*?>|</color>", RegexOptions.IgnoreCase);
            return notags.Replace(input, replace);
        }

        private static Gradient richtextGradientGradient = new Gradient();
        public static string RichtextGradient(string input, GradientColorKey[] Colors)
        {
            richtextGradientGradient ??= new Gradient();
            richtextGradientGradient.colorKeys = Colors;

            var sb = new System.Text.StringBuilder(input.Length * 24);
            for (int i = 0; i < input.Length; i++)
            {
                char character = input[i];
                Color characterColor = richtextGradientGradient.Evaluate((Time.time / 2f + i / 25f) % 1f);
                sb.Append("<color=#").Append(ColorToHex(characterColor)).Append('>').Append(character).Append("</color>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Hashes the input string using SHA256 and returns the hexadecimal representation.
        /// </summary>
        /// <param name="input">Input string to be hashed.</param>
        /// <returns>Output hashed string.</returns>
        public static string GetSHA256(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder stringBuilder = new StringBuilder();

            foreach (byte b in bytes)
                stringBuilder.Append(b.ToString("x2"));

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Wraps the text in the standard grey brackets with a colored tag, for example "[SUCCESS]".
        /// </summary>
        /// <param name="tag">The tag text.</param>
        /// <param name="color">The color of the tag text.</param>
        public static string FormatTag(this string tag, string color) =>
            Bracket(color, tag);

        /// <summary>
        /// Builds the grey-bracketed colored text used in button labels and notifications, for example "[G]" with a green G.
        /// </summary>
        /// <param name="color">The color of the text between the brackets.</param>
        /// <param name="text">The text between the brackets.</param>
        public static string Bracket(string color, string text) =>
            $"<color=grey>[</color><color={color}>{text}</color><color=grey>]</color>";
    }
}
