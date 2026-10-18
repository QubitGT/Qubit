/*
 * Qubit Menu  Extensions/Colors.cs
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

using System;
using System.Collections.Generic;
using UnityEngine;
using static Qubit.Frontend.Settings;

namespace Qubit.Extensions
{
    public static class Colors
    {

        private const int MaxGradientCacheSize = 64;
        private static readonly LinkedList<(Color, Color)> gradientUsage = new LinkedList<(Color, Color)>();
        public static readonly Dictionary<(Color, Color), Texture2D> cacheGradients = new Dictionary<(Color, Color), Texture2D>();

        private static Color Quantize(Color c, float step = 1f / 64f)
        {
            return new Color(
                Mathf.Round(c.r / step) * step,
                Mathf.Round(c.g / step) * step,
                Mathf.Round(c.b / step) * step,
                Mathf.Round(c.a / step) * step
            );
        }

        public static Texture2D GetGradientTexture(Color colorA, Color colorB)
        {
            var key = (Quantize(colorA), Quantize(colorB));

            if (cacheGradients.TryGetValue(key, out Texture2D cachedTexture))
            {
                gradientUsage.Remove(key);
                gradientUsage.AddLast(key);
                return cachedTexture;
            }

            Texture2D txt2d = new Texture2D(128, 128);
            Color[] pixels = new Color[128 * 128];

            if (horizontalGradients)
            {
                for (int y = 0; y < 128; y++)
                {
                    Color rowColor = Color.Lerp(colorA, colorB, y / 128f);
                    for (int x = 0; x < 128; x++)
                        pixels[y * 128 + x] = rowColor;
                }
            }
            else
            {
                for (int i = 0; i < 128; i++)
                {
                    Color rowColor = Color.Lerp(colorA, colorB, i / 128f);
                    for (int j = 0; j < 128; j++)
                        pixels[j * 128 + i] = rowColor;
                }
            }

            txt2d.SetPixels(pixels);
            txt2d.wrapMode = TextureWrapMode.Mirror;
            txt2d.Apply();

            cacheGradients.Add(key, txt2d);
            gradientUsage.AddLast(key);

            if (cacheGradients.Count > MaxGradientCacheSize)
            {
                var oldestKey = gradientUsage.First.Value;
                gradientUsage.RemoveFirst();

                if (cacheGradients.TryGetValue(oldestKey, out Texture2D oldTex))
                {
                    UnityEngine.Object.Destroy(oldTex);
                    cacheGradients.Remove(oldestKey);
                }
            }

            return txt2d;
        }

        public static string ColorToHex(Color color) =>
            ColorUtility.ToHtmlStringRGB(color);

        public static Color HexToColor(string hex)
        {
            return !ColorUtility.TryParseHtmlString(hex, out var color) ? Color.black : color;
        }

        public static Color BrightenColor(Color color, float intensity = 0.5f)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + (1f - v) * intensity * 1.2f);
            s = Mathf.Clamp01(s * (1f - intensity * 0.3f));
            return Color.HSVToRGB(h, s, v);
        }

        public static Color DarkenColor(Color color, float intensity = 0.5f) =>
            new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a);

        public static string ToHex(this Color input) =>
            ColorToHex(input);

        public static string ToRichRGBString(this Color input, int roundAmount = 255) =>
            $"<color=red>{Math.Round(input.r * roundAmount)}</color> <color=green>{Math.Round(input.g * roundAmount)}</color> <color=blue>{Math.Round(input.b * roundAmount)}</color>";

        public static string ToRGBString(this Color input, int roundAmount = 255) =>
            $"{Math.Round(input.r * roundAmount)} {Math.Round(input.g * roundAmount)} {Math.Round(input.b * roundAmount)}";
    }
}
