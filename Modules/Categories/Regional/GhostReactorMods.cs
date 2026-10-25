/*
 * Qubit Menu  Modules/Categories/Regional/GhostReactorMods.cs
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
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Networking;
using Qubit.Modules.Classes;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using static Qubit.Modules.Categories.Projectile;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories.Regional
{
    public static class GhostReactorMods
    {
        public static void SpamObjectGrip(int objectId)
        {
            if (rightGrab)
                GameEntities.CreateItem(RpcTarget.All, objectId, References.RightHand.position, RandomQuaternion(), References.RightHand.forward * ShootStrength, Vector3.zero);
        }
        public static void SpamEntityGrip()
        {
            int[] objectIds = Fun.ObjectByName.Select(x => x.Value).ToArray();
            SpamObjectGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void ToolSpamGrip()
        {
            int[] objectIds = Fun.ObjectByName.Where(x => x.Key.Contains("Tool")).Select(x => x.Value).ToArray();
            SpamObjectGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void ToolSpamGun()
        {
            int[] objectIds = Fun.ObjectByName.Where(x => x.Key.Contains("Tool")).Select(x => x.Value).ToArray();
            SpamObjectGun(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void SpamObjectGun(int objectId)
        {
            GunLibrary.Point(NewPointer =>
            {
                GameEntities.CreateItem(RpcTarget.All, objectId, NewPointer.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero);
            });
        }
        public static void SpamEntityGun()
        {
            int[] objectIds = Fun.ObjectByName.Select(x => x.Value).ToArray();
            SpamObjectGun(objectIds[Random.Range(0, objectIds.Length)]);
        }
        public static void RainEntities()
        {
            int[] objectIds = Fun.ObjectByName.Select(x => x.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + new Vector3(Random.Range(-3f, 3f), 4f, Random.Range(-3f, 3f)), Quaternion.identity, Vector3.down, Vector3.zero);
        }
        public static void EntityAura()
        {
            int[] objectIds = Fun.ObjectByName.Select(x => x.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + RandomVector3().normalized * 2f, Quaternion.identity, Vector3.down, Vector3.zero);
        }
        public static void EntityFountain()
        {
            int[] objectIds = Fun.ObjectByName.Select(x => x.Value).ToArray();
            GameEntities.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + Vector3.up * 3f, Quaternion.identity, RandomVector3(15f), Vector3.zero);
        }
        public static void GhostReactorTextGun()
        {
            TextGun.Render(Fun.textToRender, position =>
                GameEntities.CreateItems(RpcTarget.All, Enumerable.Repeat(Fun.ObjectByName["GhostReactorCollectibleFlower"], position.Count).ToArray(), position.ToArray(), Enumerable.Repeat(Quaternion.identity, position.Count).ToArray()));
        }
        public static void GhostReactorDrawGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Coroutines.instance.StartCoroutine(GameEntities.DrawSmallDelay(NewPointer.transform.position, Fun.ObjectByName["GhostReactorCollectibleCore"], ManagerRegistry.GhostReactor.GameEntityManager));
            });
        }
        public static void DestroyEntityGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > Overpowered.destroyDelay)
                {
                    GameEntity gameEntity = null;
                    float closestDist = float.MaxValue;

                    foreach (GameEntity entity in ManagerRegistry.GhostReactor.GameEntityManager.entities)
                    {
                        if (entity != null)
                        {
                            float distance = Vector3.Distance(NewPointer.transform.position, entity.transform.position);
                            if (distance < 0.75f && distance < closestDist)
                            {
                                gameEntity = entity;
                                closestDist = distance;
                            }
                        }
                    }

                    if (gameEntity != null)
                    {
                        Overpowered.destroyDelay = Time.time + 0.02f;
                        if (Networking.IsMasterClient)
                        {
                            ManagerRegistry.GhostReactor.GameEntityManager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new[] { gameEntity.GetNetId() });
                            Security.RPCProtection();
                        }
                        else
                        {
                            gameEntity.RequestGrab(true, Vector3.zero, Quaternion.identity);
                            gameEntity.RequestThrow(true, References.BodyCollider.transform.position - (Vector3.up * 14f), Quaternion.identity, Vector3.zero, Vector3.zero);
                        }
                    }
                }
            });
        }
    }
}
