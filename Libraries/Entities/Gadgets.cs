/*
 * Qubit Menu  Libraries/Entities/Gadgets.cs
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
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion.Gameplay;
using GorillaTag;
using GorillaTagScripts;
using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Locomotion;
using Qubit.Patches.Netcode;
using Qubit.Patches.Safety.RPC;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameObj;
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Libraries.Entities.Critters;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using JoinType = GorillaNetworking.JoinType;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Qubit.Modules.Categories;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// Super Infection gadget helpers: grabbing/spawning the charge blaster and firing it at a target.
    /// </summary>
    public static class Gadgets
    {
        public static Dictionary<string, int> GadgetByName =>
            GameEntityManager.itemPrefabFactory.ToDictionary(prefab => prefab.Value.name, prefab => prefab.Key);

        public static int? spawnedNetId;
        public static int spawnedFrame;

        public static float blasterDelay;
        public static Coroutine BlasterCoroutine;

        public static readonly Dictionary<NetPlayer, float> perPlayerDictionary = new Dictionary<NetPlayer, float>();

        public static SIGadgetChargeBlaster GetBlaster()
        {
            References.InputPoller.leftGrab = true;
            References.InputPoller.leftControllerGripFloat = 1f;

            int hash = GadgetByName["MegaChargeBlasterGadget"];

            GameEntityManager gameEntityManager = ManagerRegistry.SuperInfection.GameEntityManager;
            GamePlayer gamePlayer = GamePlayer.GetGamePlayer(PhotonNetwork.LocalPlayer);
            if (gamePlayer.IsHoldingEntity(gameEntityManager, true))
            {
                GameEntity entity = gameEntityManager.GetGameEntity(gamePlayer.GetGrabbedGameEntityId(GamePlayer.GetHandIndex(true)));
                if (entity.gameObject.TryGetComponent<SIGadgetChargeBlaster>(out var blaster))
                    return blaster;
                else
                    entity.RequestThrow(true, entity.transform.position, entity.transform.rotation, Vector3.zero, Vector3.zero, gameEntityManager);
            }
            else
            {
                if (Qubit.Core.Networking.IsMasterClient)
                {
                    if (spawnedNetId != null && spawnedFrame > Time.frameCount - 10)
                    {
                        GameEntity entity = gameEntityManager.GetGameEntity(spawnedNetId.Value);
                        entity.RequestGrab(true, Vector3.zero, Quaternion.identity, gameEntityManager);
                        if (entity.gameObject.TryGetComponent<SIGadgetChargeBlaster>(out var blaster))
                            return blaster;
                    }

                    if (!gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                        return null;

                    int netId = gameEntityManager.CreateTypeNetId(hash);

                    object[] createData = {
                        new[] { netId },
                        new[] { hash },
                        new[] { BitPackUtils.PackWorldPosForNetwork(References.LeftHand.position) },
                        new[] { BitPackUtils.PackQuaternionForNetwork(References.LeftHand.rotation) },
                        new[] { 0L },
                        new[] { gameEntityManager.GetInvalidNetId() }
                    };

                    gameEntityManager.photonView.RPC("CreateItemRPC", RpcTarget.All, createData);
                    spawnedNetId = netId;
                    spawnedFrame = Time.frameCount;

                    RPCProtection();
                }
                else
                {
                    Vector3 position = References.LeftHand.position;

                    float maxDistance = 12f;
                    if (Vector3.Distance(ServerLeftHandPos, position) > maxDistance)
                        position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * maxDistance;

                    List<GameEntity> entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                        Vector3.Distance(References.BodyCollider.transform.position, e.transform.position) > 3f &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0)
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        Vector3.Distance(ServerLeftHandPos, e.transform.position) < maxDistance &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0)
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash &&
                        gameEntityManager.ValidateGrab(e, PhotonNetwork.LocalPlayer.actorNumber, true)).ToList();

                    if (entities.Count <= 0) // Desperate measures
                        entities = gameEntityManager.entities.Where(e =>
                        e != null &&
                        e.typeId == hash).ToList();

                    if (entities.Count <= 0)
                        return null;

                    GameEntity entity = entities.OrderByDescending(entity => entity.transform.position.Distance(References.BodyCollider.transform.position)).FirstOrDefault();

                    if (Vector3.Distance(entity.transform.position, References.BodyCollider.transform.position) > maxDistance)
                    {
                        References.LocalRig.enabled = false;
                        References.LocalRig.transform.position = entity.transform.position - Vector3.one * 5f;

                        if (CritterCoroutine != null)
                            Coroutines.instance.StopCoroutine(CritterCoroutine);

                        CritterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
                    }

                    if (Vector3.Distance(entity.transform.position, ServerPos) < maxDistance && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                    {
                        entity.transform.position = References.RightHand.position;
                        entity.transform.rotation = RandomQuaternion();

                        entity.RequestGrab(true, Vector3.zero, Quaternion.identity, gameEntityManager);
                        RPCProtection();
                    }
                }
            }

            return null;
        }

        public static void FireBlaster(Vector3 position, Vector3 direction)
        {
            SIGadgetChargeBlaster blaster = GetBlaster();
            if (blaster == null)
                return;

            Quaternion rotation = Quaternion.LookRotation(direction);
            if (Time.time < blasterDelay)
                return;

            blasterDelay = Time.time + SIPlayer.LocalPlayer.clientToClientRPCLimiter.GetDelay();

            if ((position - References.LeftHand.position).magnitude > blaster.blaster.maxLagDistance)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = position - Vector3.one;

                if (BlasterCoroutine != null)
                    Coroutines.instance.StopCoroutine(BlasterCoroutine);

                BlasterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
            }

            blaster.blaster.lastFired = 0f;
            blaster.FireProjectile(blaster.maxChargeDiff, blaster.blaster.NextFireId(), position, rotation);

            RPCProtection();
        }

        public static void FireBlaster(Vector3 position, Vector3 direction, NetPlayer target, bool ignoreDistnace = false)
        {
            perPlayerDictionary.TryGetValue(target, out float perPlayerDelay);

            Quaternion rotation = Quaternion.LookRotation(direction);
            if (Time.time < perPlayerDelay)
                return;

            SIGadgetChargeBlaster blaster = GetBlaster();
            if (blaster == null)
                return;

            perPlayerDictionary[target] = Time.time + SIPlayer.LocalPlayer.clientToClientRPCLimiter.GetDelay();

            if (!ignoreDistnace && (position - References.LeftHand.position).magnitude > blaster.blaster.maxLagDistance)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = position - Vector3.one;

                if (BlasterCoroutine != null)
                    Coroutines.instance.StopCoroutine(BlasterCoroutine);

                BlasterCoroutine = Coroutines.instance.StartCoroutine(Overpowered.RopeEnableRig());
            }

            blaster.blaster.lastFired = 0f;
            SuperInfectionManager.GetSIManagerForZone(blaster.blaster.gameEntity.manager.zone)?.photonView.RPC("SIClientToClientRPC", target.GetPlayer(), new object[]
            {
                (int)SuperInfectionManager.ClientToClientRPC.CallEntityRPCData,
                new object[]
                {
                    blaster.blaster.gameEntity.GetNetId(),
                    0,
                    new object[]
                    {
                        blaster.maxChargeDiff,
                        blaster.blaster.NextFireId(),
                        position,
                        rotation
                    }
                }
            });

            RPCProtection();
        }
    }
}
