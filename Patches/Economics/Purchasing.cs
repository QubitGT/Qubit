/*
 * Qubit Menu  Patches/Economics/Purchasing.cs
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
using HarmonyLib;
using Qubit.Core;

namespace Qubit.Patches.Economics
{
    [HarmonyPatch(typeof(CosmeticsController), nameof(CosmeticsController.PurchaseItem))]
    public class Purchasing
    {
        public static bool enabled;

        private static bool Prefix()
        {
            if (enabled)
            {
                CosmeticsController.CosmeticItem itemFromDict = ManagerRegistry.CosmeticsController.GetItemFromDict(ManagerRegistry.CosmeticsController.itemToBuy.itemName);
                if (itemFromDict.itemCategory == CosmeticsController.CosmeticCategory.Set)
                {
                    ManagerRegistry.CosmeticsController.UnlockItem(ManagerRegistry.CosmeticsController.itemToBuy.itemName);
                    foreach (string item in itemFromDict.bundledItems)
                        ManagerRegistry.CosmeticsController.UnlockItem(item);
                }
                else
                    ManagerRegistry.CosmeticsController.UnlockItem(ManagerRegistry.CosmeticsController.itemToBuy.itemName);

                ManagerRegistry.CosmeticsController.UpdateMyCosmetics();
                ManagerRegistry.CosmeticsController.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Success;

                ManagerRegistry.CosmeticsController.UpdateShoppingCart();
                ManagerRegistry.CosmeticsController.ProcessPurchaseItemState(null, ManagerRegistry.CosmeticsController.isLastHandTouchedLeft);

                return false;
            }

            return true;
        }
    }
}
