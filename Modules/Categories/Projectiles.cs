/*
 * Qubit Menu  Modules/Categories/Projectiles.cs
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
using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag.CosmeticSystem;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.GameObj;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Projectiles
    {
        public class ProjectileEntry
        {
            public string Name;
            public SnowballThrowable ThrowableLeft;
            public SnowballThrowable ThrowableRight;
            public SnowballThrowable Throwable => ThrowableRight;
            public int ThrowableIndex => Throwable.throwableMakerIndex;
        }

        internal static List<ProjectileEntry> _cachedProjectileEntries;
        private static bool _isBuildingCache = false;

        public static void BuildProjectileCache(Action onComplete = null)
        {
            if (_cachedProjectileEntries != null)
            {
                onComplete?.Invoke();
                return;
            }
            if (_isBuildingCache || !CosmeticsV2Spawner_Dirty.isPrepared)
                return;

            var allCosmeticsArraySO = CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.Asset as AllCosmeticsArraySO;
            if (allCosmeticsArraySO == null)
                return;

            _isBuildingCache = true;

            try
            {
                var entries = new List<ProjectileEntry>();
                var pending = new List<(CosmeticInfoV2 info, string rightId, string leftId)>();

                foreach (var cosmeticRef in allCosmeticsArraySO.sturdyAssetRefs)
                {
                    CosmeticInfoV2 info = cosmeticRef.obj.info;
                    if (!info.isThrowable)
                        continue;
                    if (pending.Exists(p => p.info.throwableIndex == info.throwableIndex))
                        continue;

                    bool hasRight = CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(false, info.throwableIndex, out string rightId);
                    bool hasLeft = CosmeticsV2Spawner_Dirty.GetPlayfabIdFromThrowableIndex(true, info.throwableIndex, out string leftId);
                    if (!hasRight && !hasLeft)
                        continue;

                    pending.Add((info, hasRight ? rightId : null, hasLeft ? leftId : null));
                }

                int remaining = pending.Count;
                if (remaining == 0)
                {
                    _cachedProjectileEntries = entries;
                    _isBuildingCache = false;
                    onComplete?.Invoke();
                    return;
                }

                void Finish()
                {
                    if (--remaining == 0)
                    {
                        _cachedProjectileEntries = entries;
                        _isBuildingCache = false;
                        Module projectileButton = Manager.GetIndex("Change Projectile");
                        projectileButton?.onValueChanged?.Invoke();
                        onComplete?.Invoke();
                    }
                }

                async void ResolveThrowable((CosmeticInfoV2 info, string rightId, string leftId) item)
                {
                    try
                    {
                        string name = CleanProjectileName(item.info.displayName);
                        if (name.Equals("Firework Mortar"))
                        {
                            Finish();
                            return;
                        }

                        var registry = References.LocalRig?.cosmeticsObjectRegistry;
                        if (registry == null)
                        {
                            Logs.LogError("CosmeticsItemRegistry on our rig is null??");
                            Finish();
                            return;
                        }

                        if (item.leftId != null) registry.Cosmetic(item.leftId);
                        if (item.rightId != null) registry.Cosmetic(item.rightId);

                        float start = Time.time;
                        SnowballThrowable left = null, right = null;

                        bool Satisfied() => (item.leftId == null || left != null) && (item.rightId == null || right != null);

                        while (!Satisfied())
                        {
                            left = FindByThrowableIndex(SnowballMaker.leftHandInstance, item.info.throwableIndex);
                            right = FindByThrowableIndex(SnowballMaker.rightHandInstance, item.info.throwableIndex);

                            if (Satisfied())
                                break;

                            if (Time.time > start + 5f)
                            {
                                Logs.LogError($"Throwable (index: {item.info.throwableIndex}, right id: {item.rightId}, left id: {item.leftId}) took too long to spawn, so I'm just gonna go");
                                break;
                            }

                            await Awaitable.EndOfFrameAsync(default);
                        }

                        if (left != null) left.velocityEstimator = SnowballMaker.leftHandInstance?.velocityEstimator;
                        if (right != null) right.velocityEstimator = SnowballMaker.rightHandInstance?.velocityEstimator;

                        entries.Add(new ProjectileEntry
                        {
                            Name = name,
                            ThrowableLeft = left,
                            ThrowableRight = right
                        });
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"Trying to resolve throwable failed (index: {item.info.throwableIndex}, right id: {item.rightId}, left id: {item.leftId}): {ex}");
                    }
                    finally
                    {
                        Finish();
                    }
                }

                foreach (var item in pending)
                    ResolveThrowable(item);
            }
            catch (Exception ex)
            {
                Logs.LogError($"BuildProjectileCache failed: {ex}");
                _isBuildingCache = false;
            }
        }

        public static void LoadProjectileCache()
        {
            if (CosmeticsV2Spawner_Dirty.isPrepared && _cachedProjectileEntries == null)
                BuildProjectileCache();
        }

        private static SnowballThrowable FindByThrowableIndex(SnowballMaker maker, int throwableIndex)
        {
            if (maker == null)
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
            if (_cachedProjectileEntries == null && !_isBuildingCache)
                BuildProjectileCache();
            return (IReadOnlyList<ProjectileEntry>)_cachedProjectileEntries ?? Array.Empty<ProjectileEntry>();
        }

        public static ProjectileEntry GetPreferredProjectileEntry() =>
            GetAll()[ProjectileMode];
        public static ProjectileEntry GetGrowingSnowballProjectileEntry()
        {
            ProjectileEntry entry = GetPreferredProjectileEntry();
            return entry.Throwable is GrowingSnowballThrowable ? entry : FindProjectile("Growing Snowball");
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

        public static bool friendSided;
        public static int friendProjectileScale = 1;
        public static void ApplyFriendProjectileScale(int index) => friendProjectileScale = index;

        public enum ThrowableHand
        {
            Left,
            Right,
            Both,
            Dynamic
        }
        private static RoomSystem.ProjectileSource ToProjectileSource(ThrowableHand hand)
        {
            switch (hand)
            {
                case ThrowableHand.Left:
                    return RoomSystem.ProjectileSource.LeftHand;
                case ThrowableHand.Right:
                    return RoomSystem.ProjectileSource.RightHand;
                default:
                    return RoomSystem.ProjectileSource.RightHand;
            }
        }
        public static void UpdateNetworkedProjectile(int index = -1, int modelIndex = -1, ThrowableHand hand = ThrowableHand.Dynamic, bool serialize = false)
        {
            if (hand == ThrowableHand.Left || hand == ThrowableHand.Both)
                References.LocalRig.LeftThrowableProjectileIndex = index;
            if (hand == ThrowableHand.Right || hand == ThrowableHand.Both)
                References.LocalRig.RightThrowableProjectileIndex = index;
            else if (hand == ThrowableHand.Dynamic)
                if (References.LocalRig.LeftThrowableProjectileIndex == -1)
                    References.LocalRig.RightThrowableProjectileIndex = index;
                else if (References.LocalRig.RightThrowableProjectileIndex == -1)
                    References.LocalRig.LeftThrowableProjectileIndex = index;
            References.LocalRig.SetRandomThrowableModelIndex(modelIndex);
            References.LocalRig.myBodyDockPositions.RefreshTransferrableItems();
            if (serialize && References.InRoom)
                SendSerialize(References.LocalRig.GetPhotonView());
        }

        public static void ClearNetworkedProjectile(ThrowableHand hand = ThrowableHand.Dynamic, bool serialize = false)
        {
            if (hand == ThrowableHand.Dynamic)
                for (int i = 0; i < 2; i++)
                    UpdateNetworkedProjectile(-1, -1, hand, serialize);
            else if (hand == ThrowableHand.Left)
                UpdateNetworkedProjectile(-1, -1, ThrowableHand.Left, serialize);
            else if (hand == ThrowableHand.Right)
                UpdateNetworkedProjectile(-1, -1, ThrowableHand.Right, serialize);
        }

        public static SnowballThrowable GetThrowableByHand(ThrowableHand hand = ThrowableHand.Dynamic)
        {
            GameObject throwable = hand switch
            {
                ThrowableHand.Left => References.LocalRig.myBodyDockPositions.GetLeftHandThrowable(),
                ThrowableHand.Right => References.LocalRig.myBodyDockPositions.GetRightHandThrowable(),
                ThrowableHand.Dynamic => References.LocalRig.myBodyDockPositions.GetRightHandThrowable() ?? References.LocalRig.myBodyDockPositions.GetLeftHandThrowable(),
                _ => References.LocalRig.myBodyDockPositions.GetRightHandThrowable()
            };

            return throwable?.GetComponent<SnowballThrowable>();
        }

        public static void SetSnowballSize(int size, ThrowableHand hand = ThrowableHand.Dynamic)
        {
            if (GetThrowableByHand(hand) is GrowingSnowballThrowable growingSnowball)
                growingSnowball.SetSizeLevelAuthority(growingSnowball.GetValidSizeLevel(size));
        }

        public static void LaunchLocalProjectile(Vector3 position, Vector3 velocity, byte projectileType, int index, bool overrideColor, Color32 color, int scale, int projectileHash, VRRig rig)
        {
            try
            {
                if (projectileType == 0)
                {
                    ProjectileWeapon weapon = rig.projectileWeapon;
                    if (weapon.IsNotNull())
                    {
                        GameObject go = ObjectPools.instance.Instantiate(weapon.projectilePrefab, true);
                        SlingshotProjectile projectile = go.GetComponent<SlingshotProjectile>();
                        projectile.Launch(position, velocity, null, false, false, index, scale, overrideColor, color);
                    }
                }
                else
                {
                    GameObject go = ObjectPools.instance.Instantiate(projectileHash, true);
                    SlingshotProjectile projectile = go.GetComponent<SlingshotProjectile>();
                    projectile.Launch(position, velocity, null, false, false, index, scale, overrideColor, color);
                }
            }
            catch (Exception e)
            {
                Logs.LogError($"Launching a Local Projectile errored: {e.Message}. Full exception:\n{e}");
            }
        }

        public static void LaunchLocalGrowingSnowball(string snowballName, Vector3 position, Vector3 velocity, float scale, int index, Color color, VRRig sender)
        {
            GrowingSnowballThrowable snowball = FindProjectile(snowballName)?.Throwable as GrowingSnowballThrowable ?? null;
            SlingshotProjectile projectile = snowball.SpawnGrowingSnowball(ref velocity, scale);
            projectile.Launch(position, velocity, sender.Creator, false, false, index, scale, true, new Color(color.r, color.g, color.b, 1f));
        }

        public static bool CanCallNow(FXType type)
        {
            if (projDebounceType == -1)
            {
                return friendSided || clientSided ? true : References.LocalRig.fxSettings.CanCallNow((int)type);
            }
            else
            {
                if (projDebounceType > 0f)
                {
                    projDebounce = Time.time + projDebounceType;
                    return true;
                }
                else
                    return false;
            }
        }
        public static bool clientSided;
        public static void SendProjectile(ProjectileEntry projectile, Vector3 position, Vector3 velocity, Color? color = null, int growingSnowballSize = -1, RaiseEventOptions options = null, ThrowableHand hand = ThrowableHand.Dynamic, bool bypassTeleport = false)
        {
            try
            {
                projectileFrameSent = Time.frameCount;
                ProjectileWatcherCoroutine ??= Coroutines.instance.StartCoroutine(ProjectileWatcher());

                options ??= friendSided && !ServerData.Administrators.ContainsKey(References.LocalPlayer.UserId)
                        ? new RaiseEventOptions
                        {
                            TargetActors = References.Network.PlayerListOthers
                                //.Where(p => FriendManager.IsPlayerFriend(p))
                                .Select(p => p.ActorNumber)
                                .Concat(new[] { References.LocalPlayer.ActorNumber })
                                .ToArray()
                        }
                        : new RaiseEventOptions { Receivers = ReceiverGroup.All };

                if (CanCallNow(FXType.Projectile) || !References.InRoom)
                {
                    velocity = Vector3.ClampMagnitude(velocity, 10000f);
                    if (!color.HasValue)
                        color = CalculateProjectileColor();

                    SnowballThrowable Throwable = projectile.Throwable ?? throw new Exception("Throwable is null");
                    References.LocalRig.SetThrowableProjectileColor(true, color.Value);
                    UpdateNetworkedProjectile(projectile.ThrowableIndex, targetProjectileIndex, hand);

                    if (Vector3.Distance(References.BodyCollider.transform.position, position) > 3.9f && References.InRoom && !bypassTeleport && !clientSided && !friendSided)
                        References.LocalRig.transform.position = position + new Vector3(0f, velocity.y > 0f ? -3f : 3f, 0f);

                    int index = GetProjectileIncrement(position, velocity, Throwable.transform.lossyScale.x);
                    if (Throwable is GrowingSnowballThrowable)
                    {
                        GrowingSnowballThrowable GrowingSnowball = GetThrowableByHand(hand) as GrowingSnowballThrowable ?? throw new Exception("GrowingSnowball Throwable is null");
                        if (growingSnowballSize == -1)
                            growingSnowballSize = GrowingSnowball.MaxSizeLevel;
                        // friendSided ? Math.Max(SnowballSize, friendProjectileScale) : SnowballSize;

                        if (References.InRoom || friendSided)
                        {
                            if (friendSided)
                            {
                                Color32 color32 = color.Value;

                                object[] projectileSendData = new object[8];
                                projectileSendData[0] = "sendSnowball";
                                projectileSendData[1] = projectile.Name;
                                projectileSendData[1] = position;
                                projectileSendData[2] = velocity;
                                projectileSendData[3] = color32.r;
                                projectileSendData[4] = color32.g;
                                projectileSendData[5] = color32.b;
                                projectileSendData[6] = GrowingSnowball.GetValidSizeLevel(SnowballSize);
                                projectileSendData[7] = index;

                                //PhotonNetwork.RaiseEvent(FriendManager.FriendByte, projectileSendData, options, SendOptions.SendReliable);
                                LaunchLocalGrowingSnowball(projectile.Name, position, velocity, GrowingSnowball.GetValidSizeLevel(SnowballSize), index, color.Value, References.LocalRig);
                            }
                            else if (References.InRoom)
                            {
                                if (GrowingSnowball.changeSizeEvent == null)
                                    throw new Exception("GrowingSnowball changeSizeEvent is null");
                                else if (GrowingSnowball.snowballThrowEvent == null)
                                    throw new Exception("GrowingSnowball snowballThrowEvent is null");

                                PhotonNetwork.RaiseEvent(PhotonEvent.PHOTON_EVENT_CODE, new object[]
                                {
                                    GrowingSnowball.changeSizeEvent._eventId,
                                    growingSnowballSize
                                }, options, SendOptions.SendReliable);

                                PhotonNetwork.RaiseEvent(PhotonEvent.PHOTON_EVENT_CODE, new object[]
                                {
                                    GrowingSnowball.snowballThrowEvent._eventId,
                                    position,
                                    velocity,
                                    index
                                }, options, SendOptions.SendReliable);

                                RPCProtection();
                            }

                        }
                        else if (!References.InRoom || clientSided)
                            LaunchLocalGrowingSnowball(projectile.Name, position, velocity, GrowingSnowball.snowballSizeLevels[growingSnowballSize].snowballScale, index, color.Value, References.LocalRig);
                    }
                    else
                    {
                        Color32 color32 = color.Value;

                        List<object> projectileSendData = new List<object>
                        {
                            position,
                            velocity,
                            (byte)ToProjectileSource(hand),
                            index,
                            true,
                            color32.r,
                            color32.g,
                            color32.b,
                            color32.a
                        };

                        List<object> sendEventData = new List<object>();
                        if (friendSided)
                        {
                            projectileSendData.Add(friendProjectileScale);
                            projectileSendData.Add(Throwable.ProjectileHash);
                            sendEventData.Add("sendProjectile");
                            sendEventData.Add(projectileSendData.ToArray());
                        }
                        else
                        {
                            sendEventData.Add(References.Network.ServerTimestamp);
                            sendEventData.Add(0);
                            sendEventData.Add(projectileSendData.ToArray());
                        }

                        if (!References.InRoom || clientSided || friendSided)
                            LaunchLocalProjectile(position, velocity, (byte)ToProjectileSource(hand), index, true, color32, friendSided ? friendProjectileScale : 1, Throwable.ProjectileHash, References.LocalRig);
                        else
                        {
                            //PhotonNetwork.RaiseEvent(friendSided ? FriendManager.FriendByte : Constants.Network.ROOM_SYSTEM, sendEventData.ToArray(), options, SendOptions.SendReliable);
                            SendSerialize(References.LocalRig.GetPhotonView());
                            RPCProtection();
                        }
                    }
                }
            }
            catch (Exception e) { Logs.LogError($"Projectile error: {e.Message}. Full exception:\n{e}"); }
        }

        public static Coroutine ProjectileWatcherCoroutine;
        static int projectileFrameSent;
        static IEnumerator ProjectileWatcher()
        {
            while (Time.frameCount < projectileFrameSent + 5)
                yield return null;

            ClearNetworkedProjectile(serialize: true);
            ProjectileWatcherCoroutine = null;
        }

        public static void BetaFireImpact(Vector3 position, Color color = default)
        {
            if (CanCallNow(FXType.Impact))
            {
                if (color == default)
                    color = CalculateProjectileColor();
                object[] impactSendData = new object[6];
                impactSendData[0] = position;
                impactSendData[1] = color.r;
                impactSendData[2] = color.g;
                impactSendData[3] = color.b;
                impactSendData[4] = 1f;
                impactSendData[5] = 1;

                object[] sendEventData = new object[3];
                sendEventData[0] = PhotonNetwork.ServerTimestamp;
                sendEventData[1] = (byte)1;
                sendEventData[2] = impactSendData;
                PhotonNetwork.RaiseEvent(Constants.Network.ROOM_SYSTEM, sendEventData, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendUnreliable);
                RPCProtection();
            }
        }

        public static void BetaSnowballImpact(NetPlayer Target)
        {
            if (RoomSystem.callbackInstance.roomSettings.PlayerEffectLimiter.CanCallNow())
            {
                object[] playerEffectData = new object[6];
                playerEffectData[0] = Target.ActorNumber;
                playerEffectData[1] = 0;

                object[] sendEventData = new object[3];
                sendEventData[0] = References.Network.ServerTimestamp;
                sendEventData[1] = (byte)6;
                sendEventData[2] = playerEffectData;

                PhotonNetwork.RaiseEvent(Constants.Network.ROOM_SYSTEM, sendEventData, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendUnreliable);
                RPCProtection();
            }

        }

        private static int _projMode;
        public static int ProjectileMode
        {
            get
            {
                Module random = Manager.GetIndex("Random Projectile");
                return random != null && random.enabled ? Random.Range(0, GetAll()?.Count ?? 1) : _projMode;
            }
            set => _projMode = value;
        }
        public static void ChangeProjectile(bool positive = true)
        {
            string[] projectileNames = GetAll()?.Select(e => e.Name).ToArray() ?? Array.Empty<string>();
            if (projectileNames.Length == 0)
                return;
            if (positive)
                ProjectileMode++;
            else
                ProjectileMode--;

            ProjectileMode %= projectileNames.Length;
            if (ProjectileMode < 0)
                ProjectileMode = projectileNames.Length - 1;

            Manager.GetIndex("Change Projectile").overlapText = "Change Projectile <color=grey>[</color><color=green>" + projectileNames[ProjectileMode] + "</color><color=grey>]</color>";
        }

        public static int targetProjectileIndex;
        public static void ApplyProjectileIndex(int index) => targetProjectileIndex = index;

        public static int shootCycle = 1;
        public static readonly float[] ShootStrengthTypes = { 9.72f, 19.44f, 38.88f, 200f, 1000000f };
        public static readonly string[] ShootStrengthNames = { "Slow", "Medium", "Fast", "Ultra Fast", "Instant" };
        public static void ApplyShootSpeed(int index) => ShootStrength = ShootStrengthTypes[index];

        public static void ApplyRed(int index) => Fun.red = index;
        public static void ApplyGreen(int index) => Fun.green = index;
        public static void ApplyBlue(int index) => Fun.blue = index;
        public static float projDebounce;
        public static float projDebounceType = -1f;

        public static int projDebounceIndex = -1;
        public static void ApplyProjectileDelay(int index)
        {
            projDebounceIndex = index;
            projDebounceType = index == -1 ? -1f : index / 20f;
        }
        public static string DisplayProjectileDelay(int index) => index == -1 ? "Default" : (index / 20f).ToString();
        public static void ProjectileDelayWarning(bool positive)
        {
            if (projDebounceType != -1f && (!Manager.GetIndex("Friend Sided Projectiles").enabled || !Manager.GetIndex("Client Sided Projectiles").enabled))
                Notifications.SendNotification($"<color=grey>[</color><color=red>WARNING</color><color=grey>]</color> Using a projectile delay thats not the default may not work and have the possibility of getting you banned. Use at your own caution.", 5000);
        }

        public static Color CalculateProjectileColor()
        {
            byte r = 255;
            byte g = 255;
            byte b = 255;

            if (Manager.GetIndex("Random Color").enabled)
            {
                r = (byte)Random.Range(0, 255);
                g = (byte)Random.Range(0, 255);
                b = (byte)Random.Range(0, 255);
            }

            else if (Manager.GetIndex("Rainbow Projectiles").enabled)
            {
                float h = Time.frameCount / 180f % 1f;
                Color rgbcolor = Color.HSVToRGB(h, 1f, 1f);
                r = (byte)(rgbcolor.r * 255);
                g = (byte)(rgbcolor.g * 255);
                b = (byte)(rgbcolor.b * 255);
            }

            else if (Manager.GetIndex("Hard Rainbow Projectiles").enabled)
            {
                float h = Time.frameCount / 180f % 1f;
                Color rgbcolor = Color.HSVToRGB(h, 1f, 1f);
                r = (byte)(Mathf.Floor(rgbcolor.r * 2f) / 2f * 255f);
                g = (byte)(Mathf.Floor(rgbcolor.g * 2f) / 2f * 255f);
                b = (byte)(Mathf.Floor(rgbcolor.b * 2f) / 2f * 255f);
            }

            else if (Manager.GetIndex("Custom Colored Projectiles").enabled)
            {
                r = (byte)(Fun.red / 10f * 255);
                g = (byte)(Fun.green / 10f * 255);
                b = (byte)(Fun.blue / 10f * 255);
            }

            return new Color32(r, g, b, 255);
        }

        private static int archiveIncrement;
        public static int GetProjectileIncrement(Vector3 Position, Vector3 Velocity, float Scale)
        {
            try
            {
                GameObject SlingshotProjectileGameObject = new GameObject("SlingshotProjectileHolder");
                SlingshotProjectile SlingshotProjectile = SlingshotProjectileGameObject.AddComponent<SlingshotProjectile>();

                int Data = ProjectileTracker.AddAndIncrementLocalProjectile(SlingshotProjectile, Velocity, Position, Scale);
                archiveIncrement = Data;

                Object.Destroy(SlingshotProjectileGameObject);
                return Data;
            }
            catch
            {
                Logs.Log("Falling back to archiveIncrement");

                archiveIncrement++;
                return archiveIncrement;
            }
        }

        public static void DisableSnowballImpactEffect()
        {
            if (References.InRoom && RoomSystem.callbackInstance.roomSettings.PlayerEffectLimiter.CanCallNow())
            {
                object[] playerEffectData = new object[6];
                playerEffectData[0] = -1;
                playerEffectData[1] = -1;

                object[] sendEventData = new object[3];
                sendEventData[0] = References.Network.ServerTimestamp;
                sendEventData[1] = (byte)6;
                sendEventData[2] = playerEffectData;

                PhotonNetwork.RaiseEvent(Constants.Network.ROOM_SYSTEM, sendEventData, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendUnreliable);

                RPCProtection();
            }
        }

        public static int MaximumSnowballSize = 7;
        public static int _snowballSize = MaximumSnowballSize;
        public static int SnowballSize
        {
            get
            {
                return Manager.GetIndex("Random Growing Snowball Size").enabled
                    ? Random.Range(0, MaximumSnowballSize)
                    : friendSided ? Math.Max(_snowballSize, friendProjectileScale) : _snowballSize;
            }
            set => _snowballSize = value;
        }
        public static void ApplySnowballSize(int index) => SnowballSize = index;

        public static int snowballMultiplicationFactor = 1;
        public static void ApplySnowballMultiplicationFactor(int index) => snowballMultiplicationFactor = index;

        public static void ProjectileSpam()
        {
            bool fireLeft = (Manager.GetIndex("Left Handed Projectiles").enabled || Manager.GetIndex("Both Handed Projectiles").enabled) && leftGrab;
            bool fireRight = rightGrab || Mouse.current.leftButton.isPressed;

            if (Manager.GetIndex("Both Handed Projectiles").enabled)
            {
                fireLeft = leftGrab;
                fireRight = rightGrab;
            }

            if (fireLeft || fireRight)
            {
                Transform[] hands = new Transform[] { References.LocalRig.leftHandTransform, References.LocalRig.rightHandTransform };
                bool[] fireHands = new bool[] { fireLeft, fireRight };

                for (int i = 0; i < 2; i++)
                {
                    if (!fireHands[i]) continue;

                    Vector3 startpos = hands[i].position;
                    Vector3 charvel = References.Player.RigidbodyVelocity;

                    if (Manager.GetIndex("Shoot Projectiles").enabled)
                    {
                        charvel += GetGunDirection(hands[i]) * ShootStrength;

                        if (Mouse.current.leftButton.isPressed)
                        {
                            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                            if (Physics.Raycast(ray, out var hit, 512f, NoInvisLayerMask()))
                            {
                                charvel = (hit.point - hands[i].position).normalized * ShootStrength * 2f;
                            }
                        }
                    }

                    if (Manager.GetIndex("Random Direction").enabled)
                        charvel = RandomVector3(100f);

                    if (Manager.GetIndex("Include Hand Velocity").enabled)
                        charvel = hands[i] == References.RightHand
                            ? References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0)
                            : References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0);

                    SendProjectile(GetPreferredProjectileEntry(), startpos, charvel, CalculateProjectileColor(), SnowballSize, null, Manager.GetIndex("Alternate Projectile Hand").enabled ? (Time.frameCount % 2 == 0) ? ThrowableHand.Left : ThrowableHand.Right : ThrowableHand.Dynamic);
                }
            }
        }

        public static void ProjectileGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    Vector3 startpos = NewPointer.transform.position + Vector3.up;
                    Vector3 charvel = Vector3.zero;

                    if (Manager.GetIndex("Shoot Projectiles").enabled)
                    {
                        charvel = References.Player.RigidbodyVelocity + GetGunDirection(References.RightHand) * ShootStrength;
                        if (Mouse.current.leftButton.isPressed)
                        {
                            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                            Physics.Raycast(ray, out var hit, 512f, NoInvisLayerMask());
                            charvel = hit.point - References.RightHand.transform.position;
                            charvel.Normalize();
                            charvel *= ShootStrength * 2f;
                        }
                    }

                    if (Manager.GetIndex("Include Hand Velocity").enabled)
                        charvel = References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0);

                    SendProjectile(GetPreferredProjectileEntry(), startpos, charvel, CalculateProjectileColor(), SnowballSize);
                }
            });
        }

        public static void LazerSpam()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.HeadCollider.transform.position;
                Vector3 charvel = References.HeadCollider.transform.forward * 30f;

                if (Manager.GetIndex("Shoot Projectiles").enabled)
                {
                    charvel = References.Player.RigidbodyVelocity + GetGunDirection(References.RightHand) * ShootStrength;
                    if (Mouse.current.leftButton.isPressed)
                    {
                        Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                        Physics.Raycast(ray, out var hit, 512f, NoInvisLayerMask());
                        charvel = hit.point - References.RightHand.transform.position;
                        charvel.Normalize();
                        charvel *= ShootStrength * 2f;
                    }
                }

                if (Manager.GetIndex("Include Hand Velocity").enabled)
                    charvel = References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0);

                SendProjectile(GetPreferredProjectileEntry(), startpos, charvel, CalculateProjectileColor());
            }
        }

        public static void GiveProjectileSpamGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Vector3 startpos = lockTarget.rightHandTransform.position;
                Vector3 charvel = Vector3.zero;

                if (Manager.GetIndex("Shoot Projectiles").enabled)
                    charvel = lockTarget.rightHandTransform.transform.forward * ShootStrength;

                SendProjectile(GetPreferredProjectileEntry(), startpos, charvel, CalculateProjectileColor());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void ImpactSpam()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
                BetaFireImpact(References.RightHand.position);
        }

        public static void ImpactOrbit()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
                BetaFireImpact(References.HeadCollider.transform.position + new Vector3(MathF.Cos(Time.frameCount / 30f), 2f, MathF.Sin(Time.frameCount / 30f)));
        }


        public static readonly Dictionary<bool, bool> previousGripHeld = new Dictionary<bool, bool>();
        public static void GrabProjectile()
        {
            foreach (bool leftHand in new[] { true, false })
            {
                bool gripHeld = leftHand ? leftGrab : rightGrab;

                bool wasHeld = previousGripHeld.TryGetValue(leftHand, out var prev) && prev;

                bool justPressed = gripHeld && !wasHeld;

                previousGripHeld[leftHand] = gripHeld;

                if (!justPressed) continue;

                var entry = GetPreferredProjectileEntry();
                SnowballThrowable throwable = leftHand ? entry?.ThrowableLeft : entry?.ThrowableRight;
                if (throwable == null)
                {
                    Logs.LogError("Throwable is null on " + (leftHand ? "left" : "right") + " hand for projectile: " + entry?.Name);
                    continue;
                }
                if (!throwable.gameObject.activeSelf)
                    throwable.SetSnowballActiveLocal(true);

                References.LocalRig.SetThrowableProjectileColor(leftHand, CalculateProjectileColor());

                if (throwable is GrowingSnowballThrowable growingSnowball && growingSnowball.sizeLevel != SnowballSize)
                    growingSnowball.SetSizeLevelAuthority(growingSnowball.GetValidSizeLevel(SnowballSize));
            }
        }

        public static void Urine()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.BodyCollider.transform.position + new Vector3(0f, -0.15f, 0f);
                Vector3 charvel = References.BodyCollider.transform.forward * 8.33f;

                SendProjectile(FindProjectile("Science Candy"), startpos, charvel, Color.yellow);
            }
        }

        public static void Feces()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.BodyCollider.transform.position + new Vector3(0f, -0.3f, 0f);
                Vector3 charvel = Vector3.zero;

                SendProjectile(FindProjectile("Fish Food"), startpos, charvel, Color.brown);
            }
        }

        public static void Period()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.BodyCollider.transform.position + new Vector3(0f, -0.3f, 0f);
                Vector3 charvel = Vector3.zero;

                SendProjectile(FindProjectile("Ice Cream Scoop"), startpos, charvel, Color.red);
            }
        }

        public static void Semen()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.BodyCollider.transform.position + new Vector3(0f, -0.15f, 0f);
                Vector3 charvel = References.BodyCollider.transform.forward * 8.33f;

                SendProjectile(FindProjectile("Science Candy"), startpos, charvel, Color.ghostWhite);
            }
        }

        public static void Vomit()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.HeadCollider.transform.position + References.HeadCollider.transform.forward * 0.1f + References.HeadCollider.transform.up * -0.15f;
                Vector3 charvel = References.HeadCollider.transform.forward * 8.33f;

                SendProjectile(FindProjectile("Fish Food"), startpos, charvel, Color.green);
            }
        }

        public static void Spit()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.HeadCollider.transform.position + References.HeadCollider.transform.forward * 0.1f + References.HeadCollider.transform.up * -0.15f;
                Vector3 charvel = References.HeadCollider.transform.forward * 8.33f;

                SendProjectile(FindProjectile("Water Balloon"), startpos, charvel, Color.cyan);
            }
        }

        public static void LazerEyes()
        {
            if (rightGrab || Mouse.current.leftButton.isPressed)
            {
                Vector3 startpos = References.HeadCollider.transform.position;
                Vector3 charvel = References.HeadCollider.transform.forward * 30f;

                SendProjectile(FindProjectile("Walnut"), startpos, charvel, Color.red);
            }
        }

        public static void UrineGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.transform.position + new Vector3(0f, -0.4f, 0f) + lockTarget.transform.forward * 0.2f;
                    Vector3 charvel = lockTarget.transform.forward * 8.33f;

                    SendProjectile(FindProjectile("Science Candy"), startpos, charvel, Color.yellow);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void FecesGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.transform.position + new Vector3(0f, -0.65f, 0f);
                    Vector3 charvel = Vector3.zero;

                    SendProjectile(FindProjectile("Fish Food"), startpos, charvel, Color.brown);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void PeriodGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.transform.position + new Vector3(0f, -0.65f, 0f);
                    Vector3 charvel = Vector3.zero;

                    SendProjectile(FindProjectile("Ice Cream"), startpos, charvel, Color.red);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void SemenGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.transform.position + new Vector3(0f, -0.4f, 0f) + lockTarget.transform.forward * 0.2f;
                    Vector3 charvel = lockTarget.transform.forward * 8.33f;

                    SendProjectile(FindProjectile("Science Candy"), startpos, charvel, Color.ghostWhite);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void VomitGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.forward * 0.4f + lockTarget.headMesh.transform.up * -0.05f;
                    Vector3 charvel = lockTarget.headMesh.transform.forward * 8.33f;

                    SendProjectile(FindProjectile("Fish Food"), startpos, charvel, Color.green);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void SpitGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.forward * 0.4f + lockTarget.headMesh.transform.up * -0.05f;
                    Vector3 charvel = lockTarget.headMesh.transform.forward * 8.33f;

                    SendProjectile(FindProjectile("Water Balloon"), startpos, charvel, Color.cyan);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void LazerEyesGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    Vector3 startpos = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.forward * 0.4f + lockTarget.headMesh.transform.up * -0.05f;
                    Vector3 charvel = lockTarget.headMesh.transform.forward * 30f;

                    SendProjectile(FindProjectile("Walnut"), startpos, charvel, Color.red);
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
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                } });
        }

        public static void ProjectileBlindGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                ProjectileBlindPlayer(lockTarget);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void ProjectileBlindAll()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                if (References.InRoom)
                {
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 archivePos = References.LocalRig.transform.position;

                    foreach (NetPlayer Player in References.Network.PlayerListOthers)
                    {
                        VRRig rig = GetVRRigFromPlayer(Player);
                        References.LocalRig.transform.position = rig.transform.position - Vector3.one * 3f;

                        SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });

                        SendProjectile(FindProjectile("Egg"), rig.headMesh.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, -15f, 0f), Color.black, -1, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });
                    }

                    RPCProtection();

                    References.LocalRig.enabled = true;

                    References.LocalRig.transform.position = archivePos;

                    return false;
                }

                return true;
            };
        }

        public static void ProjectileBlindPlayer(NetPlayer player)
        {
            VRRig rig = GetVRRigFromPlayer(player);
            SendProjectile(FindProjectile("Egg"), rig.headMesh.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, -15f, 0f), Color.black, -1, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });
        }

        public static void ProjectileBlindPlayer(VRRig player) => ProjectileBlindPlayer(GetPlayerFromVRRig(player));

        public static void ProjectileLagGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                ProjectileLagPlayer(lockTarget);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void ProjectileLagAll()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                if (References.InRoom)
                {
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 archivePos = References.LocalRig.transform.position;

                    foreach (NetPlayer Player in References.Network.PlayerListOthers)
                    {
                        VRRig rig = GetVRRigFromPlayer(Player);
                        References.LocalRig.transform.position = rig.transform.position - Vector3.one * 3f;

                        SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });

                        SendProjectile(FindProjectile("Fireworks"), rig.headMesh.transform.position + new Vector3(0f, 0.1f, 0f) + rig.headMesh.transform.forward * -0.7f, new Vector3(0f, 15f, 0f), Color.black, -1, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });
                    }

                    RPCProtection();

                    References.LocalRig.enabled = true;

                    References.LocalRig.transform.position = archivePos;

                    return false;
                }

                return true;
            };
        }

        public static void ProjectileLagPlayer(NetPlayer player)
        {
            VRRig rig = GetVRRigFromPlayer(player);
            SendProjectile(FindProjectile("Fireworks"), rig.headMesh.transform.position + new Vector3(0f, 0.1f, 0f) + rig.headMesh.transform.forward * -0.7f, new Vector3(0f, 15f, 0f), Color.black, -1, new RaiseEventOptions { TargetActors = new[] { NetPlayerToPlayer(rig.GetPlayer()).ActorNumber } });
        }

        public static void ProjectileLagPlayer(VRRig player) => ProjectileLagPlayer(GetPlayerFromVRRig(player));

        public static void ProjectileNukeGun()
        {
            GunLibrary.Point(newPointer =>
            {
                float t = Time.timeSinceLevelLoad;

                Vector3 startPos = newPointer.transform.position + Vector3.up * 50f;
                Vector3 velocity = Physics.gravity * t;
                Vector3 position = startPos + 0.5f * Physics.gravity * (t * t);

                SendProjectile(
                    GetPreferredProjectileEntry(),
                    position + RandomVector3(velocity.magnitude * 0.25f).X_Z(),
                    velocity
                );
            });
        }

        public static void ProjectileRain()
        {
            if (rightTrigger > 0.5f)
                SendProjectile(GetPreferredProjectileEntry(), References.LocalRig.transform.position + new Vector3(Random.Range(-5f, 5f), 5f, Random.Range(-5f, 5f)), Vector3.zero);
        }

        public static void ProjectileHail()
        {
            if (rightTrigger > 0.5f)
                SendProjectile(GetPreferredProjectileEntry(), References.LocalRig.transform.position + new Vector3(Random.Range(-5f, 5f), 5f, Random.Range(-5f, 5f)), new Vector3(0f, -50f, 0f));
        }

        public static void ProjectileFountain()
        {
            if (rightTrigger > 0.5f)
                SendProjectile(GetPreferredProjectileEntry(), References.LocalRig.transform.position + Vector3.up, new Vector3(Random.Range(-15f, 15f), Random.Range(20f, 25f), Random.Range(-15f, 15f)));
        }

        public static GameObject FountainObject;
        public static void ProjectilePositionalFountain()
        {
            if (rightGrab)
            {
                if (FountainObject == null)
                {
                    FountainObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(FountainObject.GetComponent<SphereCollider>());
                    FountainObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                }
                FountainObject.transform.position = References.RightHand.position;
            }
            if (FountainObject != null)
            {
                if (rightTrigger > 0.5f)
                    SendProjectile(GetPreferredProjectileEntry(), FountainObject.transform.position, new Vector3(Random.Range(-15f, 15f), Random.Range(20f, 25f), Random.Range(-15f, 15f)));
                else
                    FountainObject.GetComponent<Renderer>().material.color = buttonColors[0].GetColor(0);
            }
        }

        public static void DisableProjectilePositionalFountain()
        {
            if (FountainObject != null)
            {
                Object.Destroy(FountainObject);
                FountainObject = null;
            }
        }

        public static void ProjectileOrbit()
        {
            if (rightTrigger > 0.5f)
                SendProjectile(GetPreferredProjectileEntry(), References.HeadCollider.transform.position + new Vector3(MathF.Cos(Time.frameCount / 30f), 2f, MathF.Sin(Time.frameCount / 30f)), new Vector3(0f, 50f, 0f));
        }

        public static void ProjectileAura()
        {
            if (rightTrigger > 0.5f)
                SendProjectile(GetPreferredProjectileEntry(), References.HeadCollider.transform.position + RandomVector3(), RandomVector3() * 20f);
        }

        public static void SnowballParticleGun()
        {
            GunLibrary.Aim(GunData =>
            {
                if (GetGunInput(true))
                SendProjectile(GetGrowingSnowballProjectileEntry(), GunData.NewPointer.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0f, 0f));
            });
        }

        public static void SnowballImpactEffectGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                BetaSnowballImpact(lockTarget.GetPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SnowballPunchMod()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (!rig.IsLocal() && (Vector3.Distance(References.LeftHand.position, rig.headMesh.transform.position) < 0.25f || Vector3.Distance(References.RightHand.position, rig.headMesh.transform.position) < 0.25f))
                {
                    Vector3 targetDirection = References.HeadCollider.transform.position - rig.headMesh.transform.position;
                    SendProjectile(GetGrowingSnowballProjectileEntry(), References.HeadCollider.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z).normalized / 1.7f, new Vector3(0f, -500f, 0f), null, -1, new RaiseEventOptions() { TargetActors = new int[] { rig.GetPlayer().ActorNumber } });

                    if (Manager.GetIndex("Graphic Punch Mod").enabled)
                        SendProjectile(FindProjectile("Apple"), rig.head.rigTarget.position, Vector3.down * 600f, new Color32(100, 0, 0, 255));
                }
            }
        }

        private static readonly Dictionary<VRRig, float> boxingDelay = new Dictionary<VRRig, float> { };
        public static float GetBoxingDelay(VRRig rig) =>
            boxingDelay.GetValueOrDefault(rig, -1);

        internal static void SetBoxingDelay(VRRig rig)
        {
            boxingDelay.Remove(rig);

            boxingDelay.Add(rig, projDebounceType);
        }

        public static void SnowballBoxing()
        {
            foreach (VRRig rig1 in ActiveRigs)
            {
                if (Time.time < GetBoxingDelay(rig1))
                    continue;

                foreach (VRRig rig2 in ActiveRigs)
                {
                    if (rig2 == rig1) continue;
                    if (Vector3.Distance(rig2.leftHandTransform.position, rig1.head.rigTarget.position) < 0.25f || Vector3.Distance(rig2.rightHandTransform.position, rig1.head.rigTarget.position) < 0.25f)
                    {
                        Vector3 targetDirection = rig2.head.rigTarget.position - rig1.head.rigTarget.position;
                        SendProjectile(GetGrowingSnowballProjectileEntry(), rig1.head.rigTarget.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z).normalized / 1.7f, new Vector3(0f, -500f, 0f));
                        SetBoxingDelay(rig1);
                    }
                }
            }
        }

        public static void SnowballDash()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (Time.time < GetBoxingDelay(rig))
                    return;

                if (!rig.isOfflineVRRig && rig.rightThumb.calcT > 0.5f)
                {
                    SendProjectile(GetGrowingSnowballProjectileEntry(), rig.head.rigTarget.position + new Vector3(0f, 0.5f, 0f) + new Vector3(-rig.head.rigTarget.forward.x, 0f, -rig.head.rigTarget.forward.z) * 1.5f, new Vector3(0f, -300f, 0f));
                    SetBoxingDelay(rig);
                }
            }
        }

        public static void SnowballHighJump()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (Time.time < GetBoxingDelay(rig))
                    return;
                Physics.Raycast(rig.bodyTransform.position - new Vector3(0f, 0.2f, 0f), Vector3.down, out var Ray, 512f, References.Player.locomotionEnabledLayers);

                if (!rig.isOfflineVRRig && (Ray.distance > 0.12f && Ray.distance < 0.2f))
                {
                    SendProjectile(GetGrowingSnowballProjectileEntry(), rig.head.rigTarget.position + new Vector3(0f, -0.7f, 0f), new Vector3(0f, -500f, 0f));
                    SetBoxingDelay(rig);
                }
            }
        }

        public static void SnowballSafetyBubble()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (!rig.isLocal)
                {
                    if (rig.IsNear())
                    {
                        Vector3 targetDirection = rig.head.rigTarget.position - References.HeadCollider.transform.position;
                        SendProjectile(
                            GetGrowingSnowballProjectileEntry(),
                            References.HeadCollider.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z).normalized / 1.7f,
                            new Vector3(0f, -500f, 0f)
                        );
                        if (References.InRoom && References.LocalRig.fxSettings.CanCallNow((int)FXType.PlayHandTap))
                            References.LocalRig.GetNetView().SendRPC("RPC_PlayHandTap", RpcTarget.All, 248, false, 999999f);
                    }
                }
            }
        }

        public static void SnowballProtectAll()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                foreach (VRRig rig2 in ActiveRigs)
                {
                    if (rig != rig2 && rig.IsNear(rig2))
                    {
                        Vector3 targetDirection = rig2.head.rigTarget.position - rig.head.rigTarget.position;
                        SendProjectile(
                            GetGrowingSnowballProjectileEntry(),
                            rig.head.rigTarget.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z).normalized / 1.7f,
                            new Vector3(0f, -500f, 0f)
                        );
                    }
                }
            }
        }

        public static void SnowballProtectGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                foreach (VRRig rig in ActiveRigs)
                {
                    if (lockTarget != rig && lockTarget.IsNear(rig))
                    {
                        Vector3 targetDirection = rig.head.rigTarget.position - lockTarget.head.rigTarget.position;
                        SendProjectile(
                            GetGrowingSnowballProjectileEntry(),
                            lockTarget.head.rigTarget.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z).normalized / 1.7f,
                            new Vector3(0f, -500f, 0f)
                        );
                    }
                }
            }, new GunLibrary.GunSettings { locks = true });

        }

        public static void FlingPlayer(VRRig rig) =>
             SendProjectile(GetGrowingSnowballProjectileEntry(), rig.transform.position + Vector3.down, Vector3.down);

        public static void SnowballFlingGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                FlingPlayer(lockTarget);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static readonly List<GameObject> flingZones = new List<GameObject>();
        public static void SnowballFlingZone()
        {
            if (rightGrab)
            {
                bool isNearCheckpoint = false;
                foreach (var checkpoint in flingZones.Where(checkpoint => Vector3.Distance(References.RightHand.position, checkpoint.transform.position) < 0.5f))
                {
                    isNearCheckpoint = true;
                    checkpoint.transform.position = References.RightHand.transform.position;
                    break;
                }

                if (!isNearCheckpoint)
                {
                    GameObject newCheckpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(newCheckpoint.GetComponent<SphereCollider>());
                    newCheckpoint.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    newCheckpoint.transform.position = References.RightHand.position;
                    newCheckpoint.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                    newCheckpoint.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f, 0.3f);
                    flingZones.Add(newCheckpoint);
                }
            }

            if (rightTrigger > 0.5f)
            {
                foreach (var checkpoint in flingZones.ToList().Where(checkpoint => Vector3.Distance(References.RightHand.position, checkpoint.transform.position) < 0.5f))
                {
                    flingZones.Remove(checkpoint);
                    Object.Destroy(checkpoint);
                }
            }

            foreach (VRRig rig in ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                foreach (var checkpoint in flingZones)
                {
                    if (Vector3.Distance(rig.transform.position, checkpoint.transform.position) < 0.5f || Vector3.Distance(rig.leftHandTransform.position, checkpoint.transform.position) < 0.5f || Vector3.Distance(rig.rightHandTransform.position, checkpoint.transform.position) < 0.5f)
                        SendProjectile(GetGrowingSnowballProjectileEntry(), checkpoint.transform.position, new Vector3(0f, -500f, 0f));
                }
            }
        }

        public static void DisableSnowballFlingZone()
        {
            foreach (GameObject checkpoint in flingZones)
                Object.Destroy(checkpoint);

            flingZones.Clear();
        }

        public static void SnowballFlingAll()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (rightTriggerPressed)
                    FlingPlayer(rig);
            }
        }

        public static void SnowballFlingVerticalGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                SendProjectile(GetGrowingSnowballProjectileEntry(), lockTarget.headMesh.transform.position + new Vector3(0f, -0.7f, 0f), new Vector3(0f, -500f, 0f));
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SnowballFlingVerticalAll()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                if (rightTriggerPressed)
                    SendProjectile(GetGrowingSnowballProjectileEntry(), rig.transform.position + new Vector3(0f, -0.7f, 0f), new Vector3(0f, -500f, 0f));
            }

        }

        public static void SnowballBringGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Player plr = NetPlayerToPlayer(GetPlayerFromVRRig(GetTargetPlayer(0.5f)));
                Vector3 targetDirection = (NewPointer.transform.position - GetVRRigFromPlayer(plr).headMesh.transform.position).normalized;
                SendProjectile(GetGrowingSnowballProjectileEntry(), GetVRRigFromPlayer(plr).transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(-targetDirection.x, 0f, -targetDirection.z) / 1.7f, new Vector3(0f, -500f, 0f));
            });
        }

        public static void SnowballPushGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                SendProjectile(GetGrowingSnowballProjectileEntry(), NewPointer.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, -500f, 0f));
            });
        }

        public static void SnowballBringPlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Vector3 targetDirection = (lockTarget.headMesh.transform.position - References.HeadCollider.transform.position).normalized;
                SendProjectile(GetGrowingSnowballProjectileEntry(), lockTarget.headMesh.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z) * 1.5f, new Vector3(0f, -100f, 0f));
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SnowballBringAllPlayers()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                Vector3 targetDirection = (rig.headMesh.transform.position - References.HeadCollider.transform.position).normalized;
                SendProjectile(GetGrowingSnowballProjectileEntry(), rig.headMesh.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z) * 1.5f, new Vector3(0f, -100f, 0f));
            }
        }

        public static void SnowballPushPlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Vector3 targetDirection = (References.HeadCollider.transform.position - lockTarget.headMesh.transform.position).normalized;
                SendProjectile(GetGrowingSnowballProjectileEntry(), lockTarget.headMesh.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z) * 1.5f, new Vector3(0f, -100f, 0f));
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SnowballPushAllPlayers()
        {
            foreach (VRRig rig in ActiveRigs)
            {
                Vector3 targetDirection = (References.HeadCollider.transform.position - rig.headMesh.transform.position).normalized;
                SendProjectile(GetGrowingSnowballProjectileEntry(), rig.headMesh.transform.position + new Vector3(0f, 0.5f, 0f) + new Vector3(targetDirection.x, 0f, targetDirection.z) * 1.5f, new Vector3(0f, -100f, 0f));
            }
        }

        public static void AntiReportSnowballFling()
        {
            Safety.AntiReport((vrrig, position) =>
            {
                SendProjectile(GetGrowingSnowballProjectileEntry(), position, new Vector3(0f, -500f, 0f));
                Notifications.SendNotification("<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " + vrrig.Creator.NickName + " attempted to report you, they have been flung.");
            });
        }

        public static void SnowballButtocks()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.TransformDirection(
                new Vector3(-0.0436f, -0.3f, -0.1563f)
            );
            References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.TransformDirection(
                new Vector3(-0.0072f, -0.2964f, -0.1563f)
            );

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation * Quaternion.Euler(330f, 344.5f, 0f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation * Quaternion.Euler(340f, 165.5f, 160f);

            References.LocalRig.leftIndex.calcT = 1f;
            References.LocalRig.leftMiddle.calcT = 1f;
            References.LocalRig.leftThumb.calcT = 1f;

            References.LocalRig.leftIndex.LerpFinger(1f, false);
            References.LocalRig.leftMiddle.LerpFinger(1f, false);
            References.LocalRig.leftThumb.LerpFinger(1f, false);

            References.LocalRig.rightIndex.calcT = 1f;
            References.LocalRig.rightMiddle.calcT = 1f;
            References.LocalRig.rightThumb.calcT = 1f;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
            References.LocalRig.rightThumb.LerpFinger(1f, false);

            ProjectileEntry snowball = FindProjectile($"Growing Snowball");
            for (int i = 0; i < 2; i++)
            {
                UpdateNetworkedProjectile(snowball.ThrowableIndex, targetProjectileIndex, i == 0 ? ThrowableHand.Left : ThrowableHand.Right);
                SetSnowballSize((snowball.Throwable as GrowingSnowballThrowable)?.MaxSizeLevel ?? 0, i == 0 ? ThrowableHand.Left : ThrowableHand.Right);
            }
            References.LocalRig.SetThrowableProjectileColor(true, References.LocalRig.playerColor);
        }

        public static void SnowballBreasts()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.TransformDirection(
                new Vector3(-0.08f, -0.0691f, 0f)
            );
            References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.TransformDirection(
                new Vector3(-0.0073f, -0.2182f, 0.0164f)
            );

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation * Quaternion.Euler(350f, 140f, 62f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation * Quaternion.Euler(8f, 30f, 8f);

            References.LocalRig.leftIndex.calcT = 1f;
            References.LocalRig.leftMiddle.calcT = 1f;
            References.LocalRig.leftThumb.calcT = 1f;

            References.LocalRig.leftIndex.LerpFinger(1f, false);
            References.LocalRig.leftMiddle.LerpFinger(1f, false);
            References.LocalRig.leftThumb.LerpFinger(1f, false);

            References.LocalRig.rightIndex.calcT = 1f;
            References.LocalRig.rightMiddle.calcT = 1f;
            References.LocalRig.rightThumb.calcT = 1f;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
            References.LocalRig.rightThumb.LerpFinger(1f, false);


            ProjectileEntry snowball = FindProjectile($"Growing Snowball");
            for (int i = 0; i < 2; i++)
            {
                UpdateNetworkedProjectile(snowball.ThrowableIndex, targetProjectileIndex, i == 0 ? ThrowableHand.Left : ThrowableHand.Right);
                SetSnowballSize((snowball.Throwable as GrowingSnowballThrowable)?.MaxSizeLevel ?? 0, i == 0 ? ThrowableHand.Left : ThrowableHand.Right);
            }
            References.LocalRig.SetThrowableProjectileColor(false, References.LocalRig.playerColor);
        }

        public static void DisableSnowballGenitals()
        {
            References.LocalRig.enabled = true;
            ClearNetworkedProjectile();
            References.LocalRig.SetThrowableProjectileColor(false, Color.white);
        }

        public static Dictionary<string, SnowballThrowable> snowballDict;
        private static bool allSnowballsInitialized;

        /// <summary>
        /// Retrieves a SnowballThrowable instance by its projectile name.
        /// </summary>
        /// <param name="projectileName">The name of the projectile to retrieve.</param>
        /// <returns>The matching SnowballThrowable instance if found; otherwise, null.</returns>
        public static SnowballThrowable GetProjectile(string projectileName)
        {
            if (CosmeticsV2Spawner_Dirty.isPrepared)
            {
                var throwables = ((AllCosmeticsArraySO)CosmeticsController.instance.v2_allCosmeticsInfoAssetRef.Asset).sturdyAssetRefs.Where(x => x.obj != null && x.obj.info.isThrowable).Select(x => x.obj.info.playFabID).Distinct().ToList();
                if (snowballDict == null || snowballDict.Count != (throwables.Count - 3)) // lazy fix
                {
                    if (!CosmeticsV2Spawner_Dirty.isPrepared)
                        return null;

                    if (!References.Computer.isConnectedToMaster)
                        return null;

                    if (!allSnowballsInitialized &&
                        (CosmeticsV2Spawner_Dirty.materialIndexToSnowballThrowablePlayfabIdStringLeft.Count >= 1 &&
                         CosmeticsV2Spawner_Dirty.materialIndexToSnowballThrowablePlayfabIdStringRight.Count >= 1))
                    {
                        allSnowballsInitialized = true;

                        CosmeticsV2Spawner_Dirty.materialIndexToSnowballThrowablePlayfabIdStringLeft.ForEach(v => References.LocalRig.cosmeticsObjectRegistry.Cosmetic(v.Value));
                        CosmeticsV2Spawner_Dirty.materialIndexToSnowballThrowablePlayfabIdStringRight.ForEach(v => References.LocalRig.cosmeticsObjectRegistry.Cosmetic(v.Value));

                        return null;
                    }

                    snowballDict = new Dictionary<string, SnowballThrowable>();
                    foreach (SnowballMaker Maker in new[] { SnowballMaker.leftHandInstance, SnowballMaker.rightHandInstance })
                    {
                        foreach (SnowballThrowable Throwable in Maker.snowballs)
                        {
                            try
                            {
                                string key = Throwable.transform.parent.gameObject.name;
                                snowballDict.Add(key, Throwable);
                            }
                            catch (Exception e)
                            {
                                Logs.LogError($"Failed to add projectile to snowballDict: {e.Message}");
                            }
                        }
                    }
                }

                projectileName += "(Clone)";
                if (!snowballDict.TryGetValue(projectileName, out var projectile))
                {
                    Logs.LogWarning($"Projectile not found: {projectileName}");
                    return null;
                }

                return projectile;
            }
            return null;
        }

        public static float ShootStrength = 19.44f;
    }
}
