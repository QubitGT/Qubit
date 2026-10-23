/*
 * Qubit Menu  Frontend/Resources/QubitAssets.cs
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
using System.IO;
using UnityEngine;

namespace Qubit.Frontend.Resources
{
    public static class QubitAssets
    {
        private static AssetBundle bundle;

        public static T Load<T>(string name) where T : UnityEngine.Object
        {
            if (bundle == null)
            {
                using var stream = typeof(QubitAssets).Assembly.GetManifestResourceStream($"{PluginInfo.ClientResourcePath}.qubitui");
                if (stream == null) throw new InvalidOperationException("The Qubit UI bundle is missing. Run QubitInterfaceBuilder.Build in Unity.");
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                bundle = AssetBundle.LoadFromMemory(memory.ToArray());
                if (bundle == null) throw new InvalidOperationException("The Qubit UI bundle could not be loaded.");
            }

            var asset = bundle.LoadAsset<T>(name);
            return asset ?? throw new InvalidOperationException("Missing Qubit UI asset: " + name);
        }
    }
}
