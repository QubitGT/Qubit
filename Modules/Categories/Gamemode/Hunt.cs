/*
 * Qubit Menu  Modules/Categories/Gamemode/Hunt.cs
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

using GorillaExtensions;
using GorillaGameModes;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Categories;
using Qubit.Modules.Classes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static Qubit.Extensions.Players;
using Object = UnityEngine.Object;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules.Categories.Gamemode
{
    public static class Hunt
    {
        private static bool InHunt() =>
            Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.HuntDown;

        public static void HuntTargetOverlay()
        {
            if (!InHunt())
            {
                Notifications.information.Remove("Hunt Target");
                Notifications.information.Remove("Hunted By");
                return;
            }

            GorillaHuntManager hunt = ManagerRegistry.GorillaHuntManager;
            NetPlayer target = hunt.GetTargetOf(Networking.LocalPlayer);
            NetPlayer hunter = Players.ActiveRigs
                .Where(rig => !rig.isLocal)
                .Select(GetPlayerFromVRRig)
                .FirstOrDefault(p => hunt.GetTargetOf(p) == Networking.LocalPlayer);

            Notifications.information["Hunt Target"] = target != null ? target.NickName : "None";
            Notifications.information["Hunted By"] = hunter != null ? hunter.NickName : "None";
        }

        public static void DisableHuntTargetOverlay()
        {
            Notifications.information.Remove("Hunt Target");
            Notifications.information.Remove("Hunted By");
        }

        public static void GetHuntTargetGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!InHunt())
                {
                    Notifications.SendFormat("red", "ERROR", "You are not in a hunt gamemode.");
                    return;
                }

                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                GorillaHuntManager hunt = ManagerRegistry.GorillaHuntManager;

                NetPlayer theirTarget = hunt.GetTargetOf(owner);
                NetPlayer theirHunter = Players.ActiveRigs
                    .Where(rig => !rig.isLocal && GetPlayerFromVRRig(rig) != owner)
                    .Select(GetPlayerFromVRRig)
                    .FirstOrDefault(p => hunt.GetTargetOf(p) == owner);

                Notifications.SendFormat("green", "HUNT", $"{owner.NickName}'s target: {(theirTarget != null ? theirTarget.NickName : "None")}. Hunted by: {(theirHunter != null ? theirHunter.NickName : "None")}.");
            });
        }

        private static readonly Dictionary<VRRig, GameObject> huntTags = new Dictionary<VRRig, GameObject>();
        public static void HuntTargetNameTags()
        {
            if (!InHunt())
            {
                DisableHuntTargetNameTags();
                return;
            }

            Visual.PruneRigObjects(huntTags);

            GorillaHuntManager hunt = ManagerRegistry.GorillaHuntManager;
            NetPlayer myTarget = hunt.GetTargetOf(Networking.LocalPlayer);

            var relevant = new Dictionary<VRRig, string>();
            foreach (VRRig vrrig in Players.ActiveRigs.Where(rig => !rig.isLocal))
            {
                NetPlayer owner = GetPlayerFromVRRig(vrrig);

                if (owner == myTarget)
                    relevant[vrrig] = "TARGET";
                else if (hunt.GetTargetOf(owner) == Networking.LocalPlayer)
                    relevant[vrrig] = "HUNTING YOU";
            }

            foreach (VRRig stale in huntTags.Keys.Where(rig => !relevant.ContainsKey(rig)).ToList())
            {
                Object.Destroy(huntTags[stale]);
                huntTags.Remove(stale);
            }

            foreach (var pair in relevant)
            {
                if (!huntTags.TryGetValue(pair.Key, out GameObject nameTag))
                    huntTags.Add(pair.Key, nameTag = Visual.CreateNameTag("Qubit_HuntTag"));

                TextMeshPro tmp = nameTag.GetOrAddComponent<TextMeshPro>();
                tmp.SafeSetText(pair.Value);
                tmp.color = pair.Value == "TARGET" ? Color.green : Color.red;

                Visual.PlaceNameTag(nameTag, tmp, pair.Key);
            }
        }

        public static void DisableHuntTargetNameTags()
        {
            foreach (GameObject tag in huntTags.Values)
                Object.Destroy(tag);

            huntTags.Clear();
        }
    }
}
