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

using GorillaGameModes;
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Categories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Qubit.Extensions.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Networking
{
    public static class Rooms
    {
        public static void JoinRoom(string name, JoinType joinType = JoinType.Solo) =>
            ManagerRegistry.PhotonNetworkController.AttemptToJoinSpecificRoom(name, joinType);

        public static void JoinRandom()
        {
            if (Qubit.Core.Networking.InRoom)
            {
                ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                Coroutines.instance.StartCoroutine(JoinRandomDelay());
                return;
            }

            GorillaNetworkJoinTrigger trigger = ManagerRegistry.PhotonNetworkController.currentJoinTrigger ?? References.Computer.GetJoinTriggerForZone("forest");
            ManagerRegistry.PhotonNetworkController.AttemptToJoinPublicRoom(trigger);
        }

        public static IEnumerator JoinRandomDelay()
        {
            yield return new WaitForSeconds(1.5f);
            JoinRandom();
        }

        public static void CreateRoom(string roomName, bool isPublic, byte roomSize = 0, JoinType roomJoinType = JoinType.Solo)
        {
            if (roomSize > 10 && !roomName.StartsWith("@"))
                roomName = $"@{roomName}";
            var netTrigger = ManagerRegistry.PhotonNetworkController.currentJoinTrigger ?? References.Computer.GetJoinTriggerForZone("forest");
            RoomConfig roomConfig = new RoomConfig
            {
                createIfMissing = true,
                isJoinable = true,
                isPublic = isPublic,
                MaxPlayers = roomSize == 0 ? RoomSystem.GetRoomSizeForCreate(netTrigger.zone, Enum.Parse<GameModeType>(References.Computer.currentGameMode.Value, true), !isPublic, SubscriptionManager.IsLocalSubscribed()) : roomSize,
                CustomProps = new Hashtable
                {
                    { "platform", ManagerRegistry.PhotonNetworkController.platformTag },
                    { "gameMode", netTrigger.GetFullDesiredGameModeString() },
                    { "language", LocalisationManager.CurrentLanguage.ToString() },
                    { "fan_club", SubscriptionManager.IsLocalSubscribed() ? "true" : "false" },
                    { "queueName", References.Computer.currentQueue },
                }
            };

            ManagerRegistry.PhotonNetworkController.currentJoinType = roomJoinType;

            if (roomJoinType == JoinType.JoinWithParty || roomJoinType == JoinType.ForceJoinWithParty)
                Task.Run(ManagerRegistry.PhotonNetworkController.SendPartyFollowCommands);

            switch (roomJoinType)
            {
                case JoinType.JoinWithNearby:
                case JoinType.JoinWithElevator:
                    roomConfig.SetFriendIDs(ManagerRegistry.PhotonNetworkController.FriendIDList);
                    break;
                case JoinType.JoinWithParty:
                case JoinType.ForceJoinWithParty:
                    roomConfig.SetFriendIDs(ManagerRegistry.FriendshipGroupDetection.PartyMemberIDs.ToList());
                    break;
            }

            if (Overpowered.instantCreate)
            {
                (ManagerRegistry.NetworkSystem as NetworkSystemPUN).internalState = NetworkSystemPUN.InternalState.Searching_Creating;
                _ = ForceCreateRoom(roomName, roomConfig);
            }
            else
                ManagerRegistry.NetworkSystem.ConnectToRoom(roomName, roomConfig);
        }

        public static void BroadcastRoom(string roomName, bool create, string key, string shuffler)
        {
            string text = NetworkSystem.ShuffleRoomName(roomName, shuffler.Substring(2, 8), true) + "|" + NetworkSystem.ShuffleRoomName("ABCDEFGHIJKLMNPQRSTUVWXYZ123456789".Substring(ManagerRegistry.NetworkSystem.currentRegionIndex, 1), shuffler[..2], true);

            BroadcastMyRoomRequest broadcastMyRoomRequest = new BroadcastMyRoomRequest
            {
                KeyToFollow = key,
                RoomToJoin = text,
                Set = create
            };

            ManagerRegistry.GorillaServer.BroadcastMyRoom(broadcastMyRoomRequest, delegate { }, delegate { });
        }

        public static async Task ForceCreateRoom(string name, RoomConfig options)
        {
            if (Qubit.Core.Networking.InRoom)
                await ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();

            await (ManagerRegistry.NetworkSystem as NetworkSystemPUN).TryCreateRoom(name, options);
        }

        public static string RandomRoomName()
        {
            while (true)
            {
                string text = RandomString();
                if (References.Computer.CheckAutoBanListForName(text))
                    return text;
            }
        }

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
            Overpowered.UpdateModdedLabel();
            Security.RPCProtection();
        }

        public static void OnLeaveRoom()
        {
            if (!inRoomStatus)
                return;

            inRoomStatus = false;

            Notifications.RoomLeft(lastRoom);
            Visual.CleanVisualsOnLeave();
            Overpowered.UpdateModdedLabel();
            Security.RPCProtection();
        }

        public static bool rapidMasterSwitchProtection;
        private static readonly List<float> masterSwitchTimestamps = new List<float>();

        public static void OnMasterClientSwitch(NetPlayer masterClient)
        {
            if (rapidMasterSwitchProtection)
            {
                masterSwitchTimestamps.Add(Time.time);
                masterSwitchTimestamps.RemoveAll(t => Time.time - t > 3f);

                if (masterSwitchTimestamps.Count >= 5)
                {
                    masterSwitchTimestamps.Clear();
                    ManagerRegistry.NetworkSystem.ReturnToSinglePlayer();
                }
            }

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
                Visual.CleanVisualsOnLeave(rig);
        }

        [RunOnLaunch(LaunchPriority.Rooms)]
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
