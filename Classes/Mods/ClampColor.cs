/*
 * Qubit Menu  Classes/Mods/ClampColor.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software
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

using Qubit.Classes.Menu;
using Qubit.Managers;
using UnityEngine;

namespace Qubit.Classes.Mods
{
    public class ClampColor : MonoBehaviour
    {
        public void Start()
        {
            targetRenderer.gameObject.GetComponent<ColorChanger>()?.Start();

            gameObjectRenderer = GetComponent<Renderer>();
            Update();
        }

        public void Update()
        {
            if (gameObjectRenderer.sharedMaterial.shader != targetRenderer.sharedMaterial.shader)
            {
                LogManager.Log("Creating new material");
                gameObjectRenderer.material = new Material(targetRenderer.sharedMaterial.shader);
            }

            if (targetRenderer.sharedMaterial.mainTexture != null && gameObjectRenderer.sharedMaterial.mainTexture != targetRenderer.sharedMaterial.mainTexture)
                gameObjectRenderer.material.mainTexture = targetRenderer.sharedMaterial.mainTexture;

            gameObjectRenderer.material.color = targetRenderer.sharedMaterial.color;
        }

        public Renderer gameObjectRenderer;
        public Renderer targetRenderer;
    }
}
