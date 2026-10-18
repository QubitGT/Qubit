/*
 * Qubit Menu  Core/Networking.cs
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
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Qubit.Core
{
    public class Networking
    {
        public static bool InRoom
        {
            get => ManagerRegistry.NetworkSystem.InRoom;
        }

        public static bool IsMasterClient
        {
            get => ManagerRegistry.NetworkSystem.IsMasterClient;
        }

        public static NetPlayer LocalPlayer
        {
            get => ManagerRegistry.NetworkSystem.LocalPlayer;
        }

        public static NetPlayer MasterClient
        {
            get => ManagerRegistry.NetworkSystem.MasterClient;
        }

        public static Player PhotonLocalPlayer
        {
            get => PhotonNetwork.LocalPlayer;
        }

        public static NetPlayer[] NetPlayers
        {
            get => ManagerRegistry.NetworkSystem.AllNetPlayers;
        }

        public static NetPlayer[] OtherNetPlayers
        {
            get => ManagerRegistry.NetworkSystem.PlayerListOthers;
        }

        public static Player[] PlayerList
        {
            get => PhotonNetwork.PlayerList;
        }

        public static Player[] OtherPlayers
        {
            get => PhotonNetwork.PlayerListOthers;
        }

        public static int PlayerCount
        {
            get => PhotonNetwork.PlayerList.Length;
        }

        public static Room Room
        {
            get => PhotonNetwork.CurrentRoom;
        }

        public static string RoomName
        {
            get => PhotonNetwork.CurrentRoom?.Name;
        }

        public static Hashtable CustomProperties
        {
            get => PhotonNetwork.CurrentRoom?.CustomProperties;
        }

        public static string GameMode
        {
            get => ManagerRegistry.NetworkSystem.GameModeString;
        }

        public static string Region
        {
            get => PhotonNetwork.CloudRegion;
        }

        public static int Ping
        {
            get => PhotonNetwork.GetPing();
        }
    }
}
