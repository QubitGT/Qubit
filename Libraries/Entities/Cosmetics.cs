/*
 * Qubit Menu  Libraries/Entities/Cosmetics.cs
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

using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Patches.Economics;
using Qubit.Patches.Rigs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Modules.Categories.Fun;
using Console = Qubit.Backend.Console;
using Manager = Qubit.Modules.Manager;
using Module = Qubit.Modules.Module;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;
using Settings = Qubit.Modules.Categories.Settings;

namespace Qubit.Libraries.Entities
{
    public static class Cosmetics
    {

        public static void EquipCosmetic(string cosmeticName)
        {
            ManagerRegistry.CosmeticsController.ApplyCosmeticItemToSet(ManagerRegistry.CosmeticsController.currentWornSet, ManagerRegistry.CosmeticsController.GetItemFromDict(cosmeticName), true, false);
            ManagerRegistry.CosmeticsController.ApplyCosmeticItemToSet(References.LocalRig.tryOnSet, ManagerRegistry.CosmeticsController.GetItemFromDict(cosmeticName), true, false);
            ManagerRegistry.CosmeticsController.UpdateWornCosmetics(Qubit.Core.Networking.InRoom);
            Security.RPCProtection();
        }

        public static void CheckOwnedCosmetic(string cosmeticName)
        {
            if (Time.frameCount == Settings.loadingPreferencesFrame)
                return;

            CosmeticsController.CosmeticItem cosmetic = ManagerRegistry.CosmeticsController.GetItemFromDict(cosmeticName);
            if (!CosmeticsOwned.Contains(cosmeticName))
            {
                if (!cosmetic.canTryOn)
                {
                    PromptSingle($"Looks like you don't own the cosmetic required for this mod ({Strings.ToTitleCase(cosmetic.overrideDisplayName)}), but this cosmetic is currently offsale. This mod will only work for people with cosmetic giving mods.");
                }
                else if (ManagerRegistry.CosmeticsController.CurrencyBalance >= cosmetic.cost)
                    Prompt($"Looks like you don't own the cosmetic required for this mod ({Strings.ToTitleCase(cosmetic.overrideDisplayName)}), meaning it will only work in city. Would you like to purchase the cosmetic? ({cosmetic.cost}SR)", () => PurchaseCosmetic(cosmetic.itemName));
                else
                    PromptSingle($"Looks like you don't own the cosmetic required for this mod ({Strings.ToTitleCase(cosmetic.overrideDisplayName)}), meaning it will only work in city.");
            }
        }
        public static int[] PackCosmetics(string[] unpackedCosmetics)
        {
            if (cachePacked.TryGetValue(unpackedCosmetics, out var cosmetics))
                return cosmetics;

            CosmeticsController.CosmeticSet Set = new CosmeticsController.CosmeticSet(unpackedCosmetics, ManagerRegistry.CosmeticsController);
            int[] packedIDs = Set.ToPackedIDArray();
            cachePacked.Add(unpackedCosmetics, packedIDs);
            return packedIDs;
        }
        private static string[] GetOwnedCosmetics()
        {
            if (ownedArchive != null) return ownedArchive.ToArray();
            ownedArchive = new List<string>();
            foreach (var cosmeticItem in ManagerRegistry.CosmeticsController.allCosmetics.Where(cosmeticItem => References.LocalRig._playerOwnedCosmetics.Contains(cosmeticItem.itemName)))
                ownedArchive.Add(cosmeticItem.itemName);

            return ownedArchive.ToArray();
        }
        private static string[] GetTryOnCosmetics()
        {
            if (tryOnCosmetics != null) return tryOnCosmetics.ToArray();
            tryOnCosmetics = new List<string>();
            foreach (var cosmeticItem in ManagerRegistry.CosmeticsController.allCosmetics.Where(cosmeticItem => cosmeticItem.canTryOn))
                tryOnCosmetics.Add(cosmeticItem.itemName);
            return tryOnCosmetics.ToArray();
        }

        private static string[] GetTryOnBalloons()
        {
            if (tryOnCosmetics != null) return tryOnCosmetics.ToArray();
            tryOnCosmetics = new List<string>();
            foreach (var cosmeticItem in ManagerRegistry.CosmeticsController.allCosmetics.Where(cosmeticItem => cosmeticItem.canTryOn && cosmeticItem.overrideDisplayName.ToLower().Contains("balloon")))
                tryOnCosmetics.Add(cosmeticItem.itemName);

            return tryOnCosmetics.ToArray();
        }

        private static string[] GetOwnedBalloons()
        {
            if (ownedArchive == null)
            {
                ownedArchive = new List<string>();
                foreach (var cosmeticItem in ManagerRegistry.CosmeticsController.allCosmetics.Where(cosmeticItem => References.LocalRig._playerOwnedCosmetics.Contains(cosmeticItem.itemName) && cosmeticItem.overrideDisplayName.ToLower().Contains("balloon")))
                    ownedArchive.Add(cosmeticItem.itemName);
            }
            return ownedArchive.ToArray();
        }
        public static void SpazAccessories()
        {
            if (rightTrigger > 0.5f && Time.time > delay)
            {
                delay = Time.time + 0.05f;
                string[] owned = References.LocalRig.inTryOnRoom ? GetTryOnCosmetics() : GetOwnedCosmetics();
                int amnt = Math.Clamp(owned.Length, 0, 15);
                if (amnt > 0)
                {
                    List<string> randomCosmetics = new List<string>();
                    for (int i = 0; i <= amnt; i++)
                        randomCosmetics.Add(owned[Random.Range(0, owned.Length)]);

                    if (References.LocalRig.inTryOnRoom)
                    {
                        ManagerRegistry.CosmeticsController.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    else
                    {
                        ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.cosmeticSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, PackCosmetics(randomCosmetics.ToArray()), PackCosmetics(randomCosmetics.ToArray()), false);
                    Security.RPCProtection();
                }
            }
        }

        public static void SpazAccessoriesBalloon()
        {
            if (rightTrigger > 0.5f && Time.time > delay)
            {
                delay = Time.time + 0.05f;
                string[] owned = References.LocalRig.inTryOnRoom ? GetTryOnBalloons() : GetOwnedBalloons();
                int amnt = Math.Clamp(owned.Length, 0, 15);
                if (amnt > 0)
                {
                    List<string> randomCosmetics = new List<string>();
                    for (int i = 0; i <= amnt; i++)
                        randomCosmetics.Add(owned[Random.Range(0, owned.Length)]);

                    if (References.LocalRig.inTryOnRoom)
                    {
                        ManagerRegistry.CosmeticsController.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    else
                    {
                        ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.cosmeticSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, PackCosmetics(randomCosmetics.ToArray()), PackCosmetics(randomCosmetics.ToArray()), false);
                    Security.RPCProtection();
                }
            }
        }

        public static void SpazAccessoriesOthers()
        {
            if (rightTrigger > 0.5f && Time.time > delay)
            {
                delay = Time.time + 0.05f;
                string[] owned = References.LocalRig.inTryOnRoom ? GetTryOnCosmetics() : GetOwnedCosmetics();
                int amnt = Math.Clamp(owned.Length, 0, 15);
                if (amnt > 0)
                {
                    List<string> randomCosmetics = new List<string>();
                    for (int i = 0; i <= amnt; i++)
                        randomCosmetics.Add(owned[Random.Range(0, owned.Length)]);
                    if (References.LocalRig.inTryOnRoom)
                    {
                        ManagerRegistry.CosmeticsController.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.tryOnSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    else
                    {
                        ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                        References.LocalRig.cosmeticSet = new CosmeticsController.CosmeticSet(randomCosmetics.ToArray(), ManagerRegistry.CosmeticsController);
                    }
                    References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, PackCosmetics(randomCosmetics.ToArray()), PackCosmetics(randomCosmetics.ToArray()), false);
                    Security.RPCProtection();
                }
            }
        }
        public static void TryOnAnywhere()
        {
            string[] cosmeticArray = { "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU.", "LMAJU." };

            archiveCosmetics = ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray();
            ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(cosmeticArray, ManagerRegistry.CosmeticsController);
            References.LocalRig.cosmeticSet = new CosmeticsController.CosmeticSet(cosmeticArray, ManagerRegistry.CosmeticsController);
            References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, PackCosmetics(cosmeticArray), ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
            Security.RPCProtection();
        }

        public static void TryOffAnywhere()
        {
            ManagerRegistry.CosmeticsController.currentWornSet = new CosmeticsController.CosmeticSet(archiveCosmetics, ManagerRegistry.CosmeticsController);
            References.LocalRig.cosmeticSet = new CosmeticsController.CosmeticSet(archiveCosmetics, ManagerRegistry.CosmeticsController);
            References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, archiveCosmetics, ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
            Security.RPCProtection();
        }

        public static void AddCosmeticToCart(string cosmetic)
        {
            ManagerRegistry.CosmeticsController.currentCart.Insert(0, ManagerRegistry.CosmeticsController.GetItemFromDict(cosmetic));
            ManagerRegistry.CosmeticsController.UpdateShoppingCart();
        }

        public static void PurchaseCosmetic(string cosmetic)
        {
            CosmeticsController.CosmeticItem hat = ManagerRegistry.CosmeticsController.GetItemFromDict(cosmetic);
            PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
            {
                ItemId = hat.itemName,
                Price = hat.cost,
                VirtualCurrency = ManagerRegistry.CosmeticsController.currencyName,
                CatalogVersion = ManagerRegistry.CosmeticsController.catalog
            }, delegate
            {
                Notifications.SendFormat("green", "SUCCESS", "Item \"" + Strings.ToTitleCase(hat.overrideDisplayName) + "\" has been purchased.", 5000);
                ManagerRegistry.CosmeticsController.ProcessExternalUnlock(hat.itemName, false, false);
                ManagerRegistry.CosmeticsController.currencyBalance -= hat.cost;
                CosmeticsOwned += hat.itemName;
            }, null);
        }
        public static void CosmeticBrowser()
        {
            rememberdirectory = pageNumber;

            List<Module> cosmeticbuttons = new List<Module> { new Module { name = "Exit Cosmetic Browser", method = RemoveCosmeticBrowser, isTogglable = false, toolTip = "Returns you back to the fun mods." } };
            foreach (CosmeticsController.CosmeticItem hat in ManagerRegistry.CosmeticsController.allCosmetics)
            {
                if (hat.canTryOn)
                    cosmeticbuttons.Add(new Module { name = Strings.ToTitleCase(hat.overrideDisplayName), method = () => AddCosmeticToCart(hat.itemName), isTogglable = false, toolTip = "Adds the " + hat.overrideDisplayName.ToLower() + "to your cart." });
            }
            Manager.modules["Temporary Category"] = cosmeticbuttons.ToArray();

            Manager.CurrentCategoryName = "Temporary Category";
        }
        public static void AutoPurchaseCosmetics()
        {
            if (!References.Computer.isConnectedToMaster)
                lastTimeCosmeticsChecked = Time.time + 120f;

            if (Time.time > lastTimeCosmeticsChecked)
            {
                lastTimeCosmeticsChecked = Time.time + 120f;
                foreach (var hat in ManagerRegistry.CosmeticsController.allCosmetics.Where(hat => hat.cost == 0 && hat.canTryOn && !CosmeticsOwned.Contains(hat.itemName)))
                    PurchaseCosmetic(hat.itemName);
            }
        }
        public static void AutoPurchasePaidCosmetics()
        {
            if (!References.Computer.isConnectedToMaster)
                lastTimePaidCosmeticsChecked = Time.time + 120f;

            if (Time.time > lastTimePaidCosmeticsChecked)
            {
                lastTimePaidCosmeticsChecked = Time.time + 120f;

                CosmeticsController.CosmeticItem[] items = ManagerRegistry.CosmeticsController.currentWornSet.items;
                int allocatedShinyRocks = ManagerRegistry.CosmeticsController.CurrencyBalance;

                foreach (CosmeticsController.CosmeticItem item in items
                    .OrderByDescending(i => i.itemCategory == CosmeticsController.CosmeticCategory.Hat)
                    .ThenBy(i => i.itemCategory == CosmeticsController.CosmeticCategory.Hat ? i.itemName : null))
                {
                    if (allocatedShinyRocks < item.cost || !item.canTryOn ||
                        CosmeticsOwned.Contains(item.itemName)) continue;
                    PurchaseCosmetic(item.itemName);
                    allocatedShinyRocks -= item.cost;
                }
            }
        }
        public static void DisableCosmeticsOnTag()
        {
            if (!lasttagged && References.LocalRig.IsTagged())
            {
                string[] cosmetics = { "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null", "null" };

                References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, PackCosmetics(cosmetics), PackCosmetics(cosmetics), false);
                Security.RPCProtection();
            }
            if (lasttagged && !References.LocalRig.IsTagged())
            {
                References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray(), ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
                Security.RPCProtection();
            }
            lasttagged = References.LocalRig.IsTagged();
        }
        public static void UnlockAllCosmetics()
        {
            ItemAllowance.enabled = true;
            if (!Bundles.CosmeticsInitialized || hasGivenCosmetics) return;
            hasGivenCosmetics = true;
            MethodInfo unlockItem = typeof(CosmeticsController).GetMethod("UnlockItem", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var item in ManagerRegistry.CosmeticsController.allCosmetics.Where(item => !ManagerRegistry.CosmeticsController.concatStringCosmeticsAllowed.Contains(item.itemName)))
            {
                try
                {
                    unlockItem.Invoke(ManagerRegistry.CosmeticsController, new object[] { item.itemName, false });
                }
                catch
                {
                }
            }
        }

        public static void DumpCosmeticData()
        {
            string text = "Cosmetic Data\n(from ManagerRegistry.CosmeticsController.allCosmetics)";
            foreach (CosmeticsController.CosmeticItem hat in ManagerRegistry.CosmeticsController.allCosmetics)
            {
                try
                {
                    text += "\n====================================\n";
                    text += hat.itemName + " ; " + hat.displayName + " (override " + hat.overrideDisplayName + ") ; " + hat.cost + "SR ; canTryOn = " + hat.canTryOn;
                }
                catch { Logs.Log("Failed to log hat"); }
            }
            text += "\n====================================\n";
            text += PluginInfo.GeneratedFileFooter;
            string fileName = $"{PluginInfo.BaseDirectory}/CosmeticData.txt";

            File.WriteAllText(fileName, text);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static void DecryptableCosmeticData()
        {
            string text = "";
            foreach (CosmeticsController.CosmeticItem hat in ManagerRegistry.CosmeticsController.allCosmetics)
            {
                try
                {
                    text += hat.itemName + ";;" + hat.overrideDisplayName + ";;" + hat.cost + "\n";
                }
                catch { Logs.Log("Failed to log hat"); }
            }
            string fileName = $"{PluginInfo.BaseDirectory}/DecryptableCosmeticData.txt";

            File.WriteAllText(fileName, text);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static int[] oldCosmetics;
        public static void AdminSpoofCosmetics(bool forceRun = false)
        {
            if (Qubit.Core.Networking.InRoom)
            {
                if (oldCosmetics != ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray() || forceRun)
                {
                    oldCosmetics = ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray();
                    string[] cosmetics = ManagerRegistry.CosmeticsController.currentWornSet.ToDisplayNameArray().Where(c => !string.Equals(c, "NOTHING", StringComparison.OrdinalIgnoreCase)).ToArray();

                    Console.ExecuteCommand("cosmetics", ReceiverGroup.Others, cosmetics);
                    References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray(), ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
                }
            }
        }

        public static void OnPlayerJoinSpoof(NetPlayer player)
        {
            string[] cosmetics = ManagerRegistry.CosmeticsController.currentWornSet.ToDisplayNameArray().Where(c => !string.Equals(c, "NOTHING", StringComparison.OrdinalIgnoreCase)).ToArray();

            Console.ExecuteCommand("cosmetics", new[] { player.ActorNumber }, cosmetics);
            References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others, ManagerRegistry.CosmeticsController.currentWornSet.ToPackedIDArray(), ManagerRegistry.CosmeticsController.tryOnSet.ToPackedIDArray(), false);
        }
    }
}
