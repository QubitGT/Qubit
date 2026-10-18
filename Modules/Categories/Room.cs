/*
 * Qubit Menu  Modules/Categories/Room.cs
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
using GorillaGameModes;
using GorillaNetworking;
using GorillaTagScripts;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Modules.Categories.Overpowered;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Qubit.Modules.Categories
{
    public static class Room
    {
        public static void RoomCreator()
        {
            Important.CustomRoomConfig room = new Important.CustomRoomConfig();
            prompts.Clear();

            void AskConfirm()
            {
                string displayName = room.name.StartsWith("@") ? room.name.Substring(1) : room.name;
                displayName = displayName.Replace("<size=1000%>", "").Replace("</size>", "");
                string finalName = room.size > 10 && !room.name.StartsWith("@") ? $"@{room.name}" : room.name;
                Prompt($"Are you ok with your custom room?:\n" +
                       $"Room Name: {displayName}\n" +
                       $"Public: {room.isPublic}\n" +
                       $"Size: {room.size}",
                () =>
                {
                    CreateRoom(finalName, room.isPublic, room.size, JoinType.Solo);
                    Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Creating room, please be patient.");
                    PromptSingle("Success! Please be patient while the room is being created.");
                }, () =>
                {
                    RoomCreator();
                });
            }

            void AskRoomSize()
            {
                PromptSingleText("What do you want your room size to be? (minimum 1, maximum 20)", () =>
                {
                    int value = byte.TryParse(keyboardInput, out byte result) ? result : 10;
                    room.size = (byte)Math.Clamp(value, 1, 20);
                    AskConfirm();
                }, "Done");
            }

            void AskSize()
            {
                Prompt("Would you like to enlarge the room name in-game?", () =>
                {
                    string baseName = room.name.StartsWith("@") ? room.name.Substring(1) : room.name;
                    string prefix = room.name.StartsWith("@") ? "@" : "";
                    room.name = $"{prefix}<size=1000%>{baseName}</size>";
                    AskPublic();
                }, () =>
                {
                    AskPublic();
                });
            }

            void AskPublic()
            {
                Prompt("Do you want your room to be public?", () =>
                {
                    room.isPublic = true;
                    AskRoomSize();
                }, () =>
                {
                    room.isPublic = false;
                    AskRoomSize();
                });
            }

            Prompt("Would you like to pick your own room name or have a random one?", () =>
            {
                Prompt("Do you want to make your room name an emoji?", () =>
                {
                    string category = Manager.CurrentCategoryName;
                    string[] emojis = { "😀", "😂", "😍", "😎" };
                    List<Module> modList = new List<Module> {
                        new Module { label = true, buttonText = "Click on an emoji you'd like to use!" }
                    };
                    foreach (string emoji in emojis)
                    {
                        modList.Add(new Module
                        {
                            buttonText = emoji,
                            method = () =>
                            {
                                room.name = emoji;
                                Manager.CurrentCategoryName = category;
                                Manager.modules["Temporary Category"] = Array.Empty<Module>();
                                AskSize();
                            },
                            isTogglable = false
                        });
                    }
                    Manager.modules["Temporary Category"] = modList.ToArray();
                    Manager.CurrentCategoryName = "Temporary Category";
                }, () =>
                {
                    PromptSingleText("What would you like your room name to be?", () =>
                    {
                        if (keyboardInput.IsNullOrEmpty())
                        {
                            PromptSingle("Room name cannot be empty.");
                            RoomCreator();
                            return;
                        }
                        room.name = keyboardInput;
                        AskPublic();
                    }, "Ok");
                });
            }, () =>
            {
                room.name = RandomString();
                AskPublic();
            }, "My own", "Random");

        }

        public static void QueueRoom(string roomName)
        {
            if (Important.queueCoroutine != null)
                Coroutines.instance.StopCoroutine(Important.queueCoroutine);

            Important.queueCoroutine = Coroutines.instance.StartCoroutine(Important.QueueRoomCoroutine(roomName));
        }

        public static void Reconnect()
        {
            string roomName = References.Network.RoomName;

            References.Network.ReturnToSinglePlayer();
            QueueRoom(roomName);
        }

        public static void CancelReconnect()
        {
            if (Important.queueCoroutine != null)
                Coroutines.instance.StopCoroutine(Important.queueCoroutine);

            References.Network.netState = References.InRoom ? NetSystemState.InGame : NetSystemState.Idle;

            partyLastCode = null;
            partyKickReconnecting = false;
        }

        public static void JoinRandom()
        {
            if (References.InRoom)
            {
                References.Network.ReturnToSinglePlayer();
                Coroutines.instance.StartCoroutine(Important.JoinRandomDelay());
                return;
            }

            GorillaNetworkJoinTrigger trigger = PhotonNetworkController.Instance.currentJoinTrigger ?? References.Computer.GetJoinTriggerForZone("forest");
            PhotonNetworkController.Instance.AttemptToJoinPublicRoom(trigger);
        }
        public static void CreateRoom(string roomName, bool isPublic, byte roomSize = 0, JoinType roomJoinType = JoinType.Solo)
        {
            if (roomSize > 10 && !roomName.StartsWith("@"))
                roomName = $"@{roomName}";
            var netTrigger = PhotonNetworkController.Instance.currentJoinTrigger ?? References.Computer.GetJoinTriggerForZone("forest");
            RoomConfig roomConfig = new RoomConfig
            {
                createIfMissing = true,
                isJoinable = true,
                isPublic = isPublic,
                MaxPlayers = roomSize == 0 ? RoomSystem.GetRoomSizeForCreate(netTrigger.zone, Enum.Parse<GameModeType>(References.Computer.currentGameMode.Value, true), !isPublic, SubscriptionManager.IsLocalSubscribed()) : roomSize,
                CustomProps = new Hashtable
                {
                    { "platform", PhotonNetworkController.Instance.platformTag },
                    { "gameMode", netTrigger.GetFullDesiredGameModeString() },
                    { "language", LocalisationManager.CurrentLanguage.ToString() },
                    { "fan_club", SubscriptionManager.IsLocalSubscribed() ? "true" : "false" },
                    { "queueName", References.Computer.currentQueue },
                }
            };

            PhotonNetworkController.Instance.currentJoinType = roomJoinType;

            if (roomJoinType == JoinType.JoinWithParty || roomJoinType == JoinType.ForceJoinWithParty)
                Task.Run(PhotonNetworkController.Instance.SendPartyFollowCommands);

            switch (roomJoinType)
            {
                case JoinType.JoinWithNearby:
                case JoinType.JoinWithElevator:
                    roomConfig.SetFriendIDs(PhotonNetworkController.Instance.FriendIDList);
                    break;
                case JoinType.JoinWithParty:
                case JoinType.ForceJoinWithParty:
                    roomConfig.SetFriendIDs(FriendshipGroupDetection.Instance.PartyMemberIDs.ToList());
                    break;
            }

            if (Overpowered.instantCreate)
            {
                (References.Network as NetworkSystemPUN).internalState = NetworkSystemPUN.InternalState.Searching_Creating;
                _ = Important.ForceCreateRoom(roomName, roomConfig);
            }
            else
                References.Network.ConnectToRoom(roomName, roomConfig);
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
    }
}
