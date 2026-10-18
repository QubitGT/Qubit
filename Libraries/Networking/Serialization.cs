/*
 * Qubit Menu  Libraries/Networking/Serialization.cs
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
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Patches.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Qubit.Libraries.Networking
{
    public static class Serialization
    {

        public static Vector3 ServerSyncPos;
        public static Vector3 ServerSyncLeftHandPos;
        public static Vector3 ServerSyncRightHandPos;

        public static Vector3 ServerPos;
        public static Vector3 ServerLeftHandPos;
        public static Vector3 ServerRightHandPos;

        public static void OnSerialize()
        {
            ServerSyncPos = References.LocalRig?.transform.position ?? ServerSyncPos;
            ServerSyncLeftHandPos = References.LocalRig?.leftHand?.rigTarget?.transform.position ?? ServerSyncLeftHandPos;
            ServerSyncRightHandPos = References.LocalRig?.rightHand?.rigTarget?.transform.position ?? ServerSyncRightHandPos;
        }

        public static readonly Dictionary<VRRig, int> playerPing = new Dictionary<VRRig, int>();
        public static void OnPlayerSerialize(VRRig rig) =>
            playerPing[rig] = rig.GetTruePing();

        /// <summary>
        /// Serializes multiple PhotonViews owned by the local player, with optional filtering and timing adjustments.
        /// </summary>
        /// <param name="exclude">If true, serializes all owned PhotonViews except those in the viewFilter; if false, serializes only those in
        /// the viewFilter.</param>
        /// <param name="viewFilter">An array of PhotonViews to include or exclude from serialization, depending on the exclude parameter.</param>
        /// <param name="timeOffset">An integer offset to apply to the serialization time.</param>
        /// <param name="delay">A delay in seconds before serialization is sent.</param>
        public static void MassSerialize(bool exclude = false, PhotonView[] viewFilter = null, int timeOffset = 0, float delay = 0f)
        {
            if (!Qubit.Core.Networking.InRoom)
                return;

            viewFilter ??= Array.Empty<PhotonView>();

            NonAllocDictionary<int, PhotonView> photonViewList = PhotonNetwork.photonViewList;
            List<PhotonView> viewsToSerialize = new List<PhotonView>();

            List<int> filteredViewIDs = viewFilter.Select(view => view.ViewID).ToList();

            foreach (PhotonView photonView in photonViewList.Values)
            {
                if (!photonView.IsMine || photonView.Synchronization == ViewSynchronization.Off || !photonView.isActiveAndEnabled || PhotonNetwork.blockedSendingGroups.Contains(photonView.Group))
                    continue;

                if (exclude)
                {
                    if (!filteredViewIDs.Contains(photonView.ViewID))
                        viewsToSerialize.Add(photonView);
                }
                else
                {
                    if (filteredViewIDs.Contains(photonView.ViewID))
                        viewsToSerialize.Add(photonView);
                }
            }

            foreach (PhotonView view in viewsToSerialize)
                SendSerialize(view, null, timeOffset, delay);
        }

        /// <summary>
        /// Serializes and sends the state of a PhotonView to other clients in the room, with optional event options,
        /// time offset, and delay.
        /// </summary>
        /// <param name="pv">The PhotonView to serialize and send.</param>
        /// <param name="options">Optional RaiseEventOptions to customize event sending.</param>
        /// <param name="timeOffset">Optional time offset to apply to the event timestamp.</param>
        /// <param name="delay">Optional delay in seconds before sending the event.</param>
        public static void SendSerialize(PhotonView pv, RaiseEventOptions options = null, int timeOffset = 0, float delay = 0f)
        {
            if (!Qubit.Core.Networking.InRoom)
                return;

            if (pv == null)
            {
                Logs.LogError("PhotonView is null. Cannot serialize.");
                return;
            }

            List<object> serializedData = PhotonNetwork.OnSerializeWrite(pv);
            if (serializedData == null || serializedData.Count == 0)
                return;

            PhotonNetwork.RaiseEventBatch raiseEventBatch = new PhotonNetwork.RaiseEventBatch();

            bool mixedReliable = pv.mixedModeIsReliable;
            raiseEventBatch.Reliable = pv.Synchronization == ViewSynchronization.ReliableDeltaCompressed || mixedReliable;
            raiseEventBatch.Group = pv.Group;

            IDictionary dictionary = PhotonNetwork.serializeViewBatches;

            PhotonNetwork.SerializeViewBatch serializeViewBatch = new PhotonNetwork.SerializeViewBatch(raiseEventBatch, 2);

            if (!dictionary.Contains(raiseEventBatch))
                dictionary[raiseEventBatch] = serializeViewBatch;

            serializeViewBatch.Add(serializedData);

            RaiseEventOptions sendOptions = PhotonNetwork.serializeRaiseEvOptions;
            RaiseEventOptions finalOptions = options != null ? new RaiseEventOptions
            {
                CachingOption = sendOptions.CachingOption,
                Flags = sendOptions.Flags,
                InterestGroup = sendOptions.InterestGroup,
                TargetActors = options.TargetActors,
                Receivers = options.Receivers
            } : sendOptions;

            bool reliable = serializeViewBatch.Batch.Reliable;
            List<object> objectUpdate = serializeViewBatch.ObjectUpdates;
            byte currentLevelPrefix = PhotonNetwork.currentLevelPrefix;

            objectUpdate[0] = PhotonNetwork.ServerTimestamp + timeOffset;
            objectUpdate[1] = currentLevelPrefix != 0 ? (object)currentLevelPrefix : null;

            if (delay <= 0f)
                PhotonNetwork.NetworkingClient.OpRaiseEvent(reliable ? Photon.Pun.PunEvent.SendSerializeReliable : Photon.Pun.PunEvent.SendSerialize, objectUpdate, finalOptions,
                    reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
            else
            {
                objectUpdate = new List<object>(objectUpdate);
                Coroutines.instance.StartCoroutine(SerializationDelay(() =>
                    PhotonNetwork.NetworkingClient.OpRaiseEvent(reliable ? Photon.Pun.PunEvent.SendSerializeReliable : Photon.Pun.PunEvent.SendSerialize, objectUpdate, finalOptions,
                        reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable), delay));
            }

            serializeViewBatch.Clear();
        }

        public static IEnumerator SerializationDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        [RunOnLaunch(LaunchPriority.Serialization)]
        public static void Initialize()
        {
            ViewSerialization.OnSerialize += OnSerialize;
            RigSerialization.OnPlayerSerialize += OnPlayerSerialize;
        }
    }
}
