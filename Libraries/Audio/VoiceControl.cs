/*
 * Qubit Menu  Libraries/Audio/VoiceControl.cs
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
using Photon.Realtime;
using Photon.Voice;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Modules.Classes;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Extensions.Players;
using static Qubit.Modules.Classes.GunLibrary;

namespace Qubit.Libraries.Audio
{
    /// <summary>
    /// Spoofs Photon Voice RPC events to mute or deafen other players.
    /// </summary>
    public static class VoiceControl
    {
        public static void MuteTarget(object target)
        {
            RaiseEventOptions raiseOptions = new RaiseEventOptions();

            if (target is ReceiverGroup group)
                raiseOptions.Receivers = group;
            else if (target is int[] actors)
                raiseOptions.TargetActors = actors;
            else if (target is RaiseEventOptions options)
                raiseOptions = options;
            else
                return;

            SendOptions sendOptions = new SendOptions
            {
                Reliability = false,
                Channel = 0
            };

            Dictionary<byte, object> voiceData = new Dictionary<byte, object>
            {
                { 1, 255 },
                { 2, Voices.Get().SamplingRate },
                { 3, 2 },
                { 4, 20000 },
                { 5, 30000 },
                { 10, null },
                { 11, (byte)0 },
                { 12, Codec.AudioOpus }
            };

            object[] eventData =
            {
                (byte)0,
                (byte)1,
                new object[] { voiceData }
            };

            ManagerRegistry.PhotonVoiceNetwork.Client.OpRaiseEvent(
                202,
                eventData,
                raiseOptions,
                sendOptions
            );
        }

        public static void ServerMuteAll()
        {
            for (int i = 0; i < 2; i++)
                MuteTarget(ReceiverGroup.All);
        }

        public static void DeafenGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    for (int i = 0; i < 2; i++)
                        MuteTarget(new int[] { lockTarget.GetPlayer().ActorNumber });
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void DeafenAll()
        {
            for (int i = 0; i < 2; i++)
                MuteTarget(ReceiverGroup.Others);
        }
    }
}
