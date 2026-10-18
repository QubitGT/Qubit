/*
 * Qubit Menu  Modules/Classes/TextGun.cs
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
using static Qubit.Modules.Classes.GunLibrary;

namespace Qubit.Modules.Classes
{
    public static class TextGun
    {

        public static Dictionary<string, bool[][]> Letters = new Dictionary<string, bool[][]> {
            { "A", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "B", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "C", new[]
            {
                new[] { false, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { false, true, true, true, true },
            } },
            { "D", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
            } },
            { "E", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { "F", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { "G", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, false, false, true, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "H", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "I", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { true, true, true, true, true },
            } },
            { "J", new[]
            {
                new[] { false, false, false, false, true },
                new[] { false, false, false, false, true },
                new[] { false, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "K", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, true, false },
                new[] { true, true, true, false, false },
                new[] { true, false, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "L", new[]
            {
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { "M", new[]
            {
                new[] { true, true, false, true, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
            } },
            { "N", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, true, false, false, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, true, true },
                new[] { true, false, false, false, true },
            } },
            { "O", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "P", new[]
            {
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, false },
                new[] { true, false, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { "Q", new[]
            {
                new[] { false, true, true, true, false },
                new[] { true, false, false, false, true },
                new[] { true, false, true, false, true },
                new[] { true, false, false, true, false },
                new[] { false, true, true, false, true },
            } },
            { "R", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, true },
                new[] { true, true, true, true, true },
                new[] { true, false, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "S", new[]
            {
                new[] { true, true, true, true, true },
                new[] { true, false, false, false, false },
                new[] { true, true, true, true, true },
                new[] { false, false, false, false, true },
                new[] { true, true, true, true, true },
            } },
            { "T", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
            } },
            { "U", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, true, true, false },
            } },
            { "V", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
            } },
            { "W", new[]
            {
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, false, false, true },
                new[] { true, false, true, false, true },
                new[] { false, true, false, true, false },
            } },
            { "X", new[]
            {
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, true, false },
                new[] { true, false, false, false, true },
            } },
            { "Y", new[]
            {
                new[] { true, false, false, false, true },
                new[] { false, true, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
            } },
            { "Z", new[]
            {
                new[] { true, true, true, true, true },
                new[] { false, false, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, false, false },
                new[] { true, true, true, true, true },
            } },
            { ".", new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, true, false, false },
            } },
            { "/", new[]
            {
                new[] { false, false, false, false, true },
                new[] { false, false, false, true, false },
                new[] { false, false, true, false, false },
                new[] { false, true, false, false, false },
                new[] { true, false, false, false, false },
            } },
            { " ", new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
                new[] { false, false, false, false, false },
            } }
        };
        private static float textDelay;
        private static int characterIndex;
        private static Vector3? basePosition;

        public static void Render(string text, Action<List<Vector3>> spawn)
        {
            GunLibrary.Aim(GunData =>
            {
                if (GetGunInput(true))
                {
                    if (basePosition == null)
                        basePosition = GunData.NewPointer.transform.position + Vector3.up;

                    if (Time.time > textDelay)
                    {
                        textDelay = Time.time + 0.1f;
                        bool[][] characterData = Letters[text[characterIndex].ToString()];

                        List<Vector3> positions = new List<Vector3>();
                        for (int i = 0; i < characterData.Length; i++)
                        {
                            bool[] column = characterData[i];

                            for (int j = 0; j < column.Length; j++)
                            {
                                if (column[j])
                                    positions.Add(basePosition.Value + new Vector3((j * 0.2f) + (characterIndex * 1.2f), i * -0.2f, 0f));
                            }
                        }

                        characterIndex++;
                        spawn(positions);
                    }
                }
                else
                {
                    characterIndex = 0;
                    basePosition = null;
                }
            });
        }
    }
}
