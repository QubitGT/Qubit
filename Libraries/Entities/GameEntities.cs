/*
 * Qubit Menu  Libraries/Entities/GameEntities.cs
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

using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Vectors;
using static Qubit.Libraries.Entities.Critters;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Libraries.Networking.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// Low level helpers for spawning and crashing GameEntityManager entities (Ghost Reactor, Super Infection, custom maps).
    /// </summary>
    public static class GameEntities
    {
        public const int ItemCrashCount = 500;

        public static void GameEntityCrash(GameEntityManager manager, object target, Vector3? targetPosition = null)
        {
            if (manager == null)
                return;

            targetPosition ??= References.BodyCollider.transform.position;

            int[] objectIds = GameEntityManager.itemPrefabFactory.Keys.ToArray();
            int[] ids = new int[ItemCrashCount];
            Vector3[] positions = new Vector3[ItemCrashCount];
            Quaternion[] rotations = new Quaternion[ItemCrashCount];

            for (int i = 0; i < ItemCrashCount; i++)
            {
                ids[i] = objectIds[Random.Range(0, objectIds.Length)];
                positions[i] = targetPosition.Value;
                rotations[i] = Quaternion.identity;
            }

            CreateItems(target, ids, positions, rotations, manager: manager);
        }

        public static void CreateItem(object target, int hash, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, long sendData = 0L, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                if (gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                {
                    int netId = gameEntityManager.CreateTypeNetId(hash);

                    if (target is NetPlayer netPlayer)
                        target = NetPlayerToPlayer(netPlayer);

                    object[] createData = {
                        new[] { netId },
                        new[] { hash },
                        new[] { BitPackUtils.PackWorldPosForNetwork(position) },
                        new[] { BitPackUtils.PackQuaternionForNetwork(rotation) },
                        new[] { sendData },
                        new[] { gameEntityManager.GetInvalidNetId() }
                    };

                    switch (target)
                    {
                        case RpcTarget rpcTarget:
                            gameEntityManager.photonView.RPC("CreateItemRPC", rpcTarget, createData);
                            break;
                        case Player player:
                            gameEntityManager.photonView.RPC("CreateItemRPC", player, createData);
                            break;
                    }

                    if ((velocity != Vector3.zero || angVelocity != Vector3.zero || Manager.GetIndex("Entity Gravity").enabled) && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.ThrowEntity].CanCallNow())
                    {

                        velocity = velocity.ClampSqrMagnitude(1600f);

                        object[] dropData = {
                            netId,
                            true,
                            position,
                            rotation,
                            velocity,
                            angVelocity,
                            PhotonNetwork.LocalPlayer,
                            PhotonNetwork.Time
                        };

                        switch (target)
                        {
                            case RpcTarget rpcTarget:
                                gameEntityManager.photonView.RPC("ThrowEntityRPC", rpcTarget, dropData);
                                break;
                            case Player player:
                                gameEntityManager.photonView.RPC("ThrowEntityRPC", player, dropData);
                                break;
                        }
                    }
                }

                RPCProtection();
            }
            else
            {
                float maxDistance = 12f;
                if (Vector3.Distance(ServerLeftHandPos, position) > maxDistance)
                    position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * maxDistance;

                GamePlayer gamePlayer = GamePlayer.GetGamePlayer(PhotonNetwork.LocalPlayer);
                if (gamePlayer.IsHoldingEntity(gameEntityManager, true) && gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItem].CanCallNow())
                {
                    References.LocalRig.enabled = true;
                    if (ServerLeftHandPos.Distance(position) < maxDistance)
                        gameEntityManager.GetGameEntity(gamePlayer.GetGrabbedGameEntityId(GamePlayer.GetHandIndex(true))).RequestThrow(true, position, rotation, velocity, angVelocity, gameEntityManager);
                    else
                        return;
                }

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
                    return;

                GameEntity entity = entities.OrderByDescending(entity => entity.transform.position.Distance(References.BodyCollider.transform.position)).FirstOrDefault();

                if (Vector3.Distance(entity.transform.position, References.BodyCollider.transform.position) > maxDistance)
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = entity.transform.position - Vector3.one * 5f;

                    if (CritterCoroutine != null)
                        Coroutines.instance.StopCoroutine(CritterCoroutine);

                    CritterCoroutine = Coroutines.instance.StartCoroutine(Qubit.Modules.Categories.Overpowered.RopeEnableRig());
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

        public static void CreateItems(object target, int[] hashes, Vector3[] positions, Quaternion[] rotations, long[] sendData = null, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (Qubit.Core.Networking.IsMasterClient)
            {
                if (gameEntityManager.m_RpcSpamChecks.m_callLimiters[(int)GameEntityManager.RPC.CreateItems].CanCallNow())
                {
                    if (target is NetPlayer netPlayer)
                        target = NetPlayerToPlayer(netPlayer);

                    sendData ??= Enumerable.Repeat(0L, hashes.Length).ToArray();

                    byte[] data = new byte[15360];
                    MemoryStream memoryStream = new MemoryStream(data);
                    BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                    binaryWriter.Write(hashes.Length);

                    for (int i = 0; i < hashes.Length; i++)
                    {
                        binaryWriter.Write(manager.CreateTypeNetId(hashes[i]));
                        binaryWriter.Write(hashes[i]);
                        binaryWriter.Write(BitPackUtils.PackWorldPosForNetwork(positions[i]));
                        binaryWriter.Write(BitPackUtils.PackQuaternionForNetwork(rotations[i]));
                        binaryWriter.Write(sendData[i]);
                        binaryWriter.Write(gameEntityManager.GetInvalidNetId());
                    }

                    byte[] array = GZipStream.CompressBuffer(data);

                    object[] createData = {
                        (int)manager.zone,
                        array
                    };

                    switch (target)
                    {
                        case RpcTarget rpcTarget:
                            gameEntityManager.photonView.RPC("CreateItemsRPC", rpcTarget, createData);
                            break;
                        case Player player:
                            gameEntityManager.photonView.RPC("CreateItemsRPC", player, createData);
                            break;
                    }

                    RPCProtection();
                }

            }
            else
                CreateItem(target, hashes[0], positions[0], rotations[0], Vector3.zero, Vector3.zero, sendData.Length > 0 ? sendData[1] : 0L, manager);
        }

        public static IEnumerator DrawSmallDelay(Vector3 position, int id, GameEntityManager manager)
        {
            GameObject Temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Temporary.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Temporary.transform.position = position;
            Object.Destroy(Temporary.GetComponent<Collider>());
            yield return new WaitForSeconds(0.5f);
            CreateItem(RpcTarget.All, id, Temporary.transform.position + new Vector3(0f, 0.1f, 0f), RandomQuaternion(), Vector3.zero, Vector3.zero, manager: manager);
            Object.Destroy(Temporary);
            RPCProtection();
        }
    }
}
