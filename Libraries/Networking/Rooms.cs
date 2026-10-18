/*
 * Qubit Menu  Libraries/Networking/Rooms.cs
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
using Photon.Pun;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Categories;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Networking
{
    public static class Rooms
    {
        public static bool inRoomStatus;
        public static string lastRoom;

        public static void OnJoinRoom()
        {
            if (inRoomStatus)
                return;

            inRoomStatus = true;
            lastRoom = PhotonNetwork.CurrentRoom.Name;

            Notifications.RoomJoined(lastRoom);
            OnMasterClientSwitch(ManagerRegistry.NetworkSystem.MasterClient);
            Security.RPCProtection();
        }

        public static void OnLeaveRoom()
        {
            if (!inRoomStatus)
                return;

            inRoomStatus = false;

            Notifications.RoomLeft(lastRoom);
            Visuals.CleanVisualsOnLeave();
            Security.RPCProtection();
        }

        public static void OnMasterClientSwitch(NetPlayer masterClient)
        {
            if (Frontend.Settings.disableMasterClientNotifications)
                return;

            Master.UpdateMasterLabel();
            Notifications.MasterClientSwitched();
        }

        public static void OnPlayerJoin(NetPlayer player) =>
            Notifications.PlayerJoined(player);

        public static void OnPlayerLeave(NetPlayer player)
        {
            Notifications.PlayerLeft(player);

            var rig = player.VRRig();
            if (rig != null)
                Visuals.CleanVisualsOnLeave(rig);
        }

        public static void Initialize()
        {
            ManagerRegistry.NetworkSystem.OnJoinedRoomEvent += OnJoinRoom;
            ManagerRegistry.NetworkSystem.OnReturnedToSinglePlayer += OnLeaveRoom;
            ManagerRegistry.NetworkSystem.OnMasterClientSwitchedEvent += OnMasterClientSwitch;

            ManagerRegistry.NetworkSystem.OnPlayerJoined += OnPlayerJoin;
            ManagerRegistry.NetworkSystem.OnPlayerLeft += OnPlayerLeave;
        }

        public static void Shutdown()
        {
            ManagerRegistry.NetworkSystem.OnJoinedRoomEvent -= OnJoinRoom;
            ManagerRegistry.NetworkSystem.OnReturnedToSinglePlayer -= OnLeaveRoom;

            ManagerRegistry.NetworkSystem.OnPlayerJoined -= OnPlayerJoin;
            ManagerRegistry.NetworkSystem.OnPlayerLeft -= OnPlayerLeave;
        }
    }
}
