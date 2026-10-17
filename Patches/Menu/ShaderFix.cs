/*
 * Qubit Menu  Patches/Menu/ShaderFix.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
<<<<<<< HEAD
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
=======
 * Copyright (C) 2026  Qubit Software
>>>>>>> da96021c (Fixes)
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

using HarmonyLib;
<<<<<<< HEAD
=======
using Qubit.Classes.Menu;
>>>>>>> da96021c (Fixes)
using UnityEngine;
using UnityEngine.Rendering;
using static Qubit.Menu.Main;

namespace Qubit.Patches.Menu
{
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.CreatePrimitive))]
    public class ShaderFix
    {
        private static void Postfix(GameObject __result)
        {
<<<<<<< HEAD
            if (crystallizeMenu && CrystalMaterial != null)
                __result.GetComponent<Renderer>().material = CrystalMaterial;
            else if (transparentMenu)
            {
                Material material = __result.GetComponent<Renderer>().material;
                material.shader = Shader.Find(shinyMenu ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
=======
            Renderer rend = __result?.GetComponent<Renderer>();
            if (rend == null || rend.material == null) return;
            // Only restyle primitives created by the menu. The game itself (ground chunks,
            // map props, texture-pack placeholders) also uses CreatePrimitive — recoloring those
            // is what turned the ground solid/magenta. Menu primitives live under the menu
            // transform or carry menu components; leave everything else untouched.
            bool isMenuPrimitive = (__result.transform.parent != null && (__result.transform.IsChildOf(menu?.transform) || __result.GetComponent<ColorChanger>() != null || __result.GetComponent<ButtonCollider>() != null))
                || __result.name.Contains("Qubit") || __result.name.Contains("Menu Background") || __result.name.Contains("Menu");
            if (!isMenuPrimitive && !crystallizeMenu && !transparentMenu)
            {
                // Default path still runs below, but guard the shader lookup so a missing
                // shader can never turn game geometry magenta.
            }
            if (crystallizeMenu && CrystalMaterial != null)
            {
                if (isMenuPrimitive) rend.material = CrystalMaterial;
                return;
            }
            else if (transparentMenu)
            {
                if (!isMenuPrimitive) return;
                Material material = rend.material;
                Shader transparentShader = Shader.Find(shinyMenu ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
                if (transparentShader != null) material.shader = transparentShader;
>>>>>>> da96021c (Fixes)

                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
<<<<<<< HEAD
                __result.GetComponent<Renderer>().material.shader = Shader.Find(shinyMenu ? "Universal Render Pipeline/Lit" : "GorillaTag/UberShader");

            __result.GetComponent<Renderer>().material.color = backgroundColor.GetColor(0);
=======
            {
                if (!isMenuPrimitive) return;
                Shader target = Shader.Find(shinyMenu ? "Universal Render Pipeline/Lit" : "GorillaTag/UberShader");
                if (target == null) return; // keep original shader instead of magenta
                rend.material.shader = target;
            }

            if (isMenuPrimitive && backgroundColor != null)
                rend.material.color = backgroundColor.GetColor(0);
>>>>>>> da96021c (Fixes)
        }
    }
}