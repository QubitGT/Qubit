/*
 * Qubit Menu  Modules/Categories/Regional/MiscMap.cs
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

using GorillaTag.Gravity;
using Photon.Pun;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Modules.Classes;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories.Regional
{
    public static class MiscMap
    {
        private static float tapDelay;
        private static float buttonDelay;

        public static void ToggleSpaceGravity(bool on) =>
            GetObject("SpaceMap/Space_Landing_V3/GravityZone_Spherical").SetActive(on);
        public static void TogglePlanetZones(bool on)
        {
            foreach (var zone in GetAllType<PlanetZone>())
                zone.enabled = on;
        }
        public static void RemoveForestColliders()
        {
            Transform ForestCollisions = GetObject("Environment Objects/LocalObjects_Prefab/ForestToHoverboard/TurnOnInForestAndHoverboard/ForestDome_CollisionOnly").transform;

            if (ForestCollisions == null)
                return;

            for (int i = 2; i < 4; i++)
            {
                GameObject c = ForestCollisions.transform.GetChild(i).gameObject;
                c.SetActive(false);
                Movement.forestColliders.Add(c);
            }
        }
        public static void RestoreForestColliders()
        {
            foreach (GameObject c in Movement.forestColliders)
                c.SetActive(true);

            Movement.forestColliders.Clear();
        }
        public static void TapAllClass<T>() where T : Tappable
        {
            if (rightGrab)
            {
                if (Time.time > tapDelay)
                {
                    foreach (Tappable TappableObject in GetAllType<T>())
                        TappableObject.OnTap(1f);

                    Security.RPCProtection();
                    tapDelay = Time.time + 0.1f;
                }
            }
        }
        public static void TriggerLeafPileGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : -10f));
                    SendSerialize(References.LocalRig.GetPhotonView());

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : 0f));
                }
                else
                    References.LocalRig.enabled = true;
            });
        }
        public static void ActivateAllDoors()
        {
            if (rightGrab)
            {
                if (Time.time > buttonDelay)
                {
                    foreach (GhostLabButton button in GetAllType<GhostLabButton>())
                    {
                        button.ButtonActivation();
                        Security.RPCProtection();
                    }
                    buttonDelay = Time.time + 0.1f;
                }
            }
        }
    }
}
