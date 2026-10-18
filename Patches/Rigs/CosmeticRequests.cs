/*
 * Qubit Menu  Patches/Rigs/CosmeticRequests.cs
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
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using Qubit.Libraries.Core;
using Qubit.Libraries.Networking;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Qubit.Patches.Rigs
{
    using Cosmetics = Qubit.Libraries.Entities.Cosmetics;
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.RequestCosmetics))]
    public class CosmeticRequests
    {
        public static bool enabled;
        public static bool bypassCosmeticCheck;
        public static Coroutine currentCoroutine;

        public static bool Prefix(VRRig __instance, PhotonMessageInfoWrapped info)
        {
            if (__instance.netView.IsMine && __instance.isLocal)
            {
                if (CosmeticsController.hasInstance)
                {
                    if (ManagerRegistry.CosmeticsController.isHidingCosmeticsFromRemotePlayers)
                    {
                        References.MyRigView.SendRPC("RPC_HideAllCosmetics", info.Sender);
                        return false;
                    }

                    if (enabled)
                    {
                        currentCoroutine ??= Coroutines.instance.StartCoroutine(LoadCosmetics());
                        return false;
                    }

                    if (bypassCosmeticCheck)
                    {
                        CosmeticsController.CosmeticSet items = new CosmeticsController.CosmeticSet
                        (
                            ManagerRegistry.CosmeticsController.currentWornSet.ToDisplayNameArray().Select(
                                cosmetic => Frontend.Settings.CosmeticsOwned.Contains(cosmetic)
                                    ? cosmetic
                                    : "null"
                                )
                            .ToArray(),
                            ManagerRegistry.CosmeticsController
                        );

                        References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", ManagerRegistry.NetworkSystem.GetPlayer(info.senderID), items.ToPackedIDArray(), ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
                        return false;
                    }
                }
            }
            return true;
        }

        private static string[] archiveCosmetics;
        public static IEnumerator LoadCosmetics()
        {
            if (Networking.InRoom)
            {
                Vector3 target = Frontend.Settings.TryOnRoom.transform.position;

                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = target;

                string[] cosmeticArray = { "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU." };

                archiveCosmetics = ManagerRegistry.CosmeticsController.currentWornSet.ToDisplayNameArray();
                ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(cosmeticArray, ManagerRegistry.CosmeticsController);

                while (Vector3.Distance(Serialization.ServerPos, target) > 0.2f)
                    yield return null;

                yield return new WaitForSeconds(0.1f);

                References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, Cosmetics.PackCosmetics(cosmeticArray), ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray(), false);
                References.LocalRig.enabled = true;
                yield return new WaitForSeconds(0.5f);

                ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(archiveCosmetics, ManagerRegistry.CosmeticsController);
                References.LocalRig.LocalUpdateCosmeticsWithTryon(ManagerRegistry.CosmeticsController.currentWornSet, ManagerRegistry.CosmeticsController.tryOnSet, false);

                float delay = Time.time + 30f;
                while (Time.time < delay || Networking.InRoom)
                    yield return null;

                currentCoroutine = null;
            }
        }
    }
}
