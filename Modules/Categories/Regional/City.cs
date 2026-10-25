/*
 * Qubit Menu  Modules/Categories/Regional/City.cs
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

using Photon.Pun;
using Qubit.Core;
using Qubit.Libraries.Entities;
using Qubit.Modules.Classes;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Modules.Classes.GunLibrary;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories.Regional
{
    public static class City
    {
        private static bool openOrClose;

        public static void SetBasementDoorState(bool open)
        {
            if (!Networking.InRoom) return;
            if (Time.time > Fun.spamDelay)
            {
                Fun.spamDelay = Time.time + 0.1f;

                GetObject("Environment Objects/LocalObjects_Prefab/CityToBasement/DungeonEntrance/DungeonDoor_Prefab").GetComponent<PhotonView>().RPC("ChangeDoorState", RpcTarget.AllViaServer, open ? GTDoor.DoorState.Opening : GTDoor.DoorState.Closing);
                Security.RPCProtection();
            }
        }
        public static void SetElevatorDoorState(bool open)
        {
            if (!Networking.InRoom) return;
            if (Time.time > Fun.spamDelay)
            {
                Fun.spamDelay = Time.time + 0.1f;

                GRElevatorManager.ElevatorButtonPressed(open ? GRElevator.ButtonType.Open : GRElevator.ButtonType.Close, ManagerRegistry.GRElevatorManager.currentLocation);
                Security.RPCProtection();
            }
        }
        public static void ElevatorProximitySensor()
        {
            if (ManagerRegistry.GRElevatorManager == null) return;

            bool inRange = Vector3.Distance(References.LocalRig.transform.position, ManagerRegistry.GRElevatorManager.transform.position) <= 3f;
            if (inRange == Fun.elevatorSensorOpen) return;

            Fun.elevatorSensorOpen = inRange;
            SetElevatorDoorState(inRange);
        }
        public static void BasementDoorSpam()
        {
            if (!Networking.InRoom) return;
            if (Time.time > Fun.spamDelay)
            {
                Fun.spamDelay = Time.time + 0.05f;
                openOrClose = !openOrClose;
                PhotonView view = GetObject("Environment Objects/LocalObjects_Prefab/CityToBasement/DungeonEntrance/DungeonDoor_Prefab").GetComponent<PhotonView>();

                view.RPC("ChangeDoorState", RpcTarget.AllViaServer, openOrClose ? GTDoor.DoorState.Opening : GTDoor.DoorState.Closing);
                view.RPC("ChangeDoorState", RpcTarget.AllViaServer, GTDoor.DoorState.HeldOpen);
                Security.RPCProtection();
            }
        }
        public static void ElevatorDoorSpam()
        {
            if (Time.time > Fun.spamDelay)
            {
                Fun.delay = Time.time + 0.1f;
                openOrClose = !openOrClose;

                GRElevatorManager.ElevatorButtonPressed(openOrClose ? GRElevator.ButtonType.Open : GRElevator.ButtonType.Close, ManagerRegistry.GRElevatorManager.currentLocation);
                Security.RPCProtection();
            }
        }
        public static void BarrelGun()
        {
            if (Manager.GetIndex("Weak Barrel Fling").enabled)
            {
                GunLibrary.Target(lockTarget =>
                {
                    if (!GetGunInput(true)) return;

                    Vector3 targetPosition = lockTarget.transform.position;
                    Vector3 spawnPosition = targetPosition + Vector3.down * 3.47f;
                    Vector3 velocity = (targetPosition - spawnPosition).normalized * 1200f;
                    ThrowableProjectiles.SendBarrelProjectile(spawnPosition, velocity, Quaternion.identity);
                }, new GunLibrary.GunSettings { locks = true });
                return;
            }

            GunLibrary.Aim(GunData =>
            {
                if (GetGunInput(true))
                    ThrowableProjectiles.SendBarrelProjectile(GunData.NewPointer.transform.position + Vector3.up, Vector3.zero, Quaternion.identity);
            });
        }
    }
}
