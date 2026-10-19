/*
 * Qubit Menu  Libraries/Entities/ThrowableProjectiles.cs
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
using GorillaLocomotion;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Modules;
using Qubit.Modules.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Frontend.Settings;
using static Qubit.Modules.Categories.Overpowered;

namespace Qubit.Libraries.Entities
{
    /// <summary>
    /// The projectile cache and lookup system shared by the projectile mods, plus the barrel-projectile sender.
    /// </summary>
    public static class ThrowableProjectiles
    {
        public class ProjectileEntry
        {
            public string Name;
            public SnowballThrowable ThrowableLeft;
            public SnowballThrowable ThrowableRight;
            public int ThrowableIndex;
            public string CosmeticId;
            private float nextResolve;

            public SnowballThrowable Throwable
            {
                get
                {
                    Resolve();
                    return ThrowableRight ?? ThrowableLeft;
                }
            }

            public void Resolve()
            {
                if (ThrowableLeft == null) ThrowableLeft = FindByThrowableIndex(SnowballMaker.leftHandInstance, ThrowableIndex);
                if (ThrowableRight == null) ThrowableRight = FindByThrowableIndex(SnowballMaker.rightHandInstance, ThrowableIndex);
                if (ThrowableLeft != null) ThrowableLeft.velocityEstimator = SnowballMaker.leftHandInstance?.velocityEstimator;
                if (ThrowableRight != null) ThrowableRight.velocityEstimator = SnowballMaker.rightHandInstance?.velocityEstimator;
                if ((ThrowableLeft != null && ThrowableRight != null) || Time.unscaledTime < nextResolve || !EnsureProjectileSystem()) return;
                var registry = References.LocalRig?.cosmeticsObjectRegistry;
                if (registry == null || string.IsNullOrEmpty(CosmeticId)) return;
                nextResolve = Time.unscaledTime + 5f;
                try
                {
                    bool hasLeft = CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(true, ThrowableIndex, out string leftId);
                    bool hasRight = CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(false, ThrowableIndex, out string rightId);
                    string requested = null;
                    if (hasLeft && ThrowableLeft == null) { registry.Cosmetic(leftId); requested = leftId; }
                    if (hasRight && ThrowableRight == null && rightId != requested) registry.Cosmetic(rightId);
                    if (!hasLeft && !hasRight) registry.Cosmetic(CosmeticId);
                }
                catch (Exception ex) { Logs.LogError($"Loading projectile {Name} failed: {ex}"); }
            }
        }

        public enum ThrowableHand
        {
            Left,
            Right,
            Both,
            Dynamic
        }

        internal static List<ProjectileEntry> _cachedProjectileEntries;
        private static float _nextCacheAttempt;
        private static float nextPrepareAttempt;

        private static bool EnsureProjectileSystem()
        {
            if (CosmeticsV2Spawner_Dirty.isPrepared) return true;
            if (ManagerRegistry.CosmeticsController == null || !ManagerRegistry.CosmeticsController.v2_allCosmeticsInfoAssetRef_isLoaded ||
                !GTPlayer.hasInstance || References.LocalRig == null || VRRigCache.Instance == null ||
                CosmeticsV2Spawner_Dirty._gVRRigDatas.Count > 0 || Time.unscaledTime < nextPrepareAttempt) return false;
            nextPrepareAttempt = Time.unscaledTime + 5f;
            try
            {
                Logs.LogWarning("Projectile cosmetics loader has not started; preparing it now");
                CosmeticsV2Spawner_Dirty.PrepareLoadOpInfos();
            }
            catch (Exception ex) { Logs.LogError($"Preparing projectile cosmetics failed: {ex}"); }
            return CosmeticsV2Spawner_Dirty.isPrepared;
        }

        public static void BuildProjectileCache(Action onComplete = null)
        {
            if (_cachedProjectileEntries != null)
            {
                onComplete?.Invoke();
                return;
            }
            if (Time.unscaledTime < _nextCacheAttempt || ManagerRegistry.CosmeticsController == null)
                return;

            try
            {
                var entries = new List<ProjectileEntry>();
                var asset = ManagerRegistry.CosmeticsController.v2_allCosmeticsInfoAssetRef.Asset as AllCosmeticsArraySO;
                IEnumerable<CosmeticInfoV2> cosmetics = asset != null
                    ? asset.sturdyAssetRefs.Where(item => item.obj != null).Select(item => item.obj.info)
                    : ManagerRegistry.CosmeticsController.v2_allCosmetics;
                if (cosmetics == null) return;
                foreach (CosmeticInfoV2 info in cosmetics)
                {
                    if (!info.isThrowable || entries.Exists(entry => entry.ThrowableIndex == info.throwableIndex))
                        continue;
                    string name = CleanProjectileName(info.displayName);
                    if (name == "Firework Mortar")
                        continue;
                    entries.Add(new ProjectileEntry { Name = name, ThrowableIndex = info.throwableIndex, CosmeticId = info.playFabID });
                }
                if (entries.Count == 0)
                {
                    _nextCacheAttempt = Time.unscaledTime + 1f;
                    return;
                }
                entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                _cachedProjectileEntries = entries;
                Logs.Log($"Loaded {entries.Count} projectile choices");
                if (Manager.modules != null) Manager.GetIndex("Change Projectile")?.onValueChanged?.Invoke();
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Logs.LogError($"BuildProjectileCache failed: {ex}");
                _nextCacheAttempt = Time.unscaledTime + 5f;
            }
        }

        public static void LoadProjectileCache()
        {
            if (_cachedProjectileEntries == null)
                BuildProjectileCache();
        }

        private static SnowballThrowable FindByThrowableIndex(SnowballMaker maker, int throwableIndex)
        {
            if (maker == null || maker.snowballs == null)
                return null;
            foreach (var sb in maker.snowballs)
            {
                if (sb != null && sb.throwableMakerIndex == throwableIndex)
                    return sb;
            }
            return null;
        }

        public static IReadOnlyList<ProjectileEntry> GetAll()
        {
            if (_cachedProjectileEntries == null)
                BuildProjectileCache();
            return (IReadOnlyList<ProjectileEntry>)_cachedProjectileEntries ?? Array.Empty<ProjectileEntry>();
        }

        public static ProjectileEntry GetPreferredProjectileEntry()
        {
            IReadOnlyList<ProjectileEntry> all = GetAll();
            if (all.Count == 0)
                return null;
            int index = Projectiles.ProjectileMode;
            if (index < 0 || index >= all.Count)
            {
                index = 0;
                Projectiles.ProjectileMode = index;
            }
            all[index].Resolve();
            return all[index];
        }

        public static ProjectileEntry GetGrowingSnowballProjectileEntry()
        {
            ProjectileEntry entry = GetPreferredProjectileEntry();
            return entry?.Throwable is GrowingSnowballThrowable ? entry : FindProjectile("Growing Snowball");
        }

        public static ProjectileEntry FindProjectile(string name) =>
            GetAll().FirstOrDefault(e => e.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        public static string CleanProjectileName(string name)
        {
            name = name.Trim().TrimEnd('.');

            name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            name = Regex.Replace(name, @"^Throwable\s+", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(
                name,
                @"(\s+(Right Hand|Left Hand|Right|Left|Projectile|Throwable|R|L))+$",
                "",
                RegexOptions.IgnoreCase);

            name = name.Trim();
            name = Regex.Replace(name, @"\s+", " ");
            name = Strings.ToTitleCase(name);

            switch (name)
            {
                case "Layerdip":
                    return "Layer Dip";
                case "Icecreamscoop":
                    return "Ice Cream Scoop";
                case "Portable Bonfire Stick":
                    return "Bonfire Stick";
                case "Tricktreat Piece":
                    return "Trick or Treat";
                default:
                    break;
            }
            return name;
        }
        public static void SendBarrelProjectile(Vector3 pos, Vector3 vel, Quaternion rot, RaiseEventOptions options = null, bool disableCooldown = false)
        {
            options ??= new RaiseEventOptions { Receivers = ReceiverGroup.Others };

            int index = BarrelIndex;

            if (Projectiles.DisableThrowableCoroutine != null)
                Coroutines.instance.StopCoroutine(Projectiles.DisableThrowableCoroutine);

            Projectiles.DisableThrowableCoroutine = Coroutines.instance.StartCoroutine(Projectiles.DisableThrowable(index));
            TransferrableObject transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];

            if (transferrableObject == null)
            {
                if (!CosmeticsOwned.Contains("Lucky Smash Barrel"))
                {
                    References.LocalRig.transform.position = TryOnRoom.transform.position;
                    ManagerRegistry.CosmeticsController.PressWardrobeItemButton(ManagerRegistry.CosmeticsController.allCosmetics.FirstOrDefault(c => string.Equals(c.overrideDisplayName, "Lucky Smash Barrel", StringComparison.OrdinalIgnoreCase)), false, false);
                }
                References.LocalRig.SetActiveTransferrableObjectIndex(1, index);
                transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];
            }

            if (transferrableObject == null)
            {
                Logs.LogError("Lucky smash barrel not found, cannot send barrel projectile.");
                return;
            }

            DeployableObject barrel = transferrableObject.GetComponent<DeployableObject>();
            if (!disableCooldown && barrel.m_spamChecker.CanCallNow(Time.unscaledTime))
            {
                transferrableObject.currentState = TransferrableObject.PositionState.InRightHand;

                object[] data = {
                    barrel._deploySignal._signalID,
                    ManagerRegistry.NetworkSystem.ServerTimestamp,
                    BitPackUtils.PackWorldPosForNetwork(pos),
                    BitPackUtils.PackQuaternionForNetwork(rot),
                    BitPackUtils.PackWorldPosForNetwork(vel)
                };
                PhotonNetwork.RaiseEvent(177, data, options, SendOptions.SendReliable);
                barrel._child.Deploy(barrel, pos, rot, vel);
                RPCProtection();
            }
        }
    }
}
