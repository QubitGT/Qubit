/*
 * Qubit Menu  Modules/Categories/Advantages.cs
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

using GorillaGameModes;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using Qubit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.GameModes;
using static Qubit.Utilities.GameObj;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Advantages
    {
        public static bool instantTag = true;

        public static void TagSelf()
        {
            static void TurnOff()
            {
                Manager.GetIndex("Tag Self").SetEnabled(false);
                ReloadMenu();
            }
            if (PhotonNetwork.IsMasterClient)
            {
                AddInfected(PhotonNetwork.LocalPlayer);
                Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> You have been tagged.");
                TurnOff();
            }
            else
            {
                if (InfectedList().Contains(PhotonNetwork.LocalPlayer))
                {
                    Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> You have been tagged.");
                    VRRig.LocalRig.enabled = true;
                    TurnOff();

                    if (instantTag)
                        SerializePatch.OverrideSerialization = null;
                }
                else
                {
                    VRRig rig = Players.ActiveRigs
                        .Where(r => !r.IsLocal() && r.IsTagged())
                        .OrderBy(r => r.Distance(VRRig.LocalRig)
                                    + r.LatestVelocity().magnitude)
                        .FirstOrDefault();

                    if (instantTag)
                    {
                        SerializePatch.OverrideSerialization = () =>
                        {
                            if (VRRig.LocalRig.IsTagged())
                                return true;

                            MassSerialize(true, new[] { VRRig.LocalRig.GetPhotonView() });

                            Vector3 positionArchive = VRRig.LocalRig.transform.position;
                            VRRig.LocalRig.transform.position = rig.rightHandTransform.transform.position;
                            SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber, rig.GetPlayer().ActorNumber } });

                            RPCProtection();
                            VRRig.LocalRig.transform.position = positionArchive;

                            return false;
                        };
                    }
                    else
                    {
                        if (!rig.IsTagged()) return;
                        VRRig.LocalRig.enabled = false;
                        if (rig != null) VRRig.LocalRig.transform.position = rig.rightHandTransform.position;

                        if (!Manager.GetIndex("Obnoxious Tag").enabled) return;
                        Quaternion rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
                        VRRig.LocalRig.transform.rotation = rotation;

                        VRRig.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                        VRRig.LocalRig.leftHand.rigTarget.transform.position = VRRig.LocalRig.transform.position + RandomVector3();
                        VRRig.LocalRig.rightHand.rigTarget.transform.position = VRRig.LocalRig.transform.position + RandomVector3();

                        VRRig.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                        VRRig.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();
                    }
                }
            }
        }

        public static void UntagSelf()
        {
            if (NetworkSystem.Instance.IsMasterClient)
                RemoveInfected(PhotonNetwork.LocalPlayer);
            else if (!NetworkSystem.Instance.IsMasterClient)
            {
                Room.Reconnect();
                NoTagOnJoin();
            }

            GTPlayer.Instance.disableMovement = false;
        }

        public static void AntiTag()
        {
            if (NetworkSystem.Instance.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (!ReportTagPatch.invinciblePlayers.Contains(NetworkSystem.Instance.LocalPlayer))
                        ReportTagPatch.invinciblePlayers.Add(NetworkSystem.Instance.LocalPlayer);
                }
                else
                {
                    if (VRRig.LocalRig.IsTagged())
                        UntagSelf();
                }
            }
            else
            {
                TagOnJoin();
                ReportTagPatch.invinciblePlayers.Clear();
            }
        }

        public static void DisableAntiTag()
        {
            if (NetworkSystem.Instance.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (ReportTagPatch.invinciblePlayers.Contains(NetworkSystem.Instance.LocalPlayer))
                        ReportTagPatch.invinciblePlayers.Remove(NetworkSystem.Instance.LocalPlayer);
                }
                NoTagOnJoin();
            }
        }

        public static float spamTagDelay;

        public static void GiveTagLagGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (lockTarget != null)
                        ReportTagPatch.invinciblePlayers.Remove(lockTarget.GetPlayer());

                    gunLocked = true;
                    lockTarget = gunTarget;

                    ReportTagPatch.invinciblePlayers.Add(GetPlayerFromVRRig(gunTarget));

                }
                else
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        ReportTagPatch.invinciblePlayers.Remove(lockTarget.GetPlayer());
                    }
                } });
        }

        public static float tagAuraDistance = 1.666f;
        public static int tagAuraIndex = 1;
        public static readonly string[] TagAuraRangeNames = { "Short", "Normal", "Far", "Maximum" };
        public static readonly float[] TagAuraRangeDistances = { 0.777f, 1.666f, 3f, 5.5f };
        public static void ApplyTagAuraRange(int index) { tagAuraIndex = index; tagAuraDistance = TagAuraRangeDistances[index]; }

        public static int tagRangeIndex;
        private static float tagReachDistance = 0.3f;
        public static readonly string[] TagReachDistanceNames = { "Unnoticable", "Normal", "Far", "Maximum" };
        public static readonly float[] TagReachDistances = { 0.3f, 0.5f, 1f, 3f };
        public static void ApplyTagReachDistance(int index) { tagRangeIndex = index; tagReachDistance = TagReachDistances[index]; }

        public static void TagAura()
        {
            Color color = Color.red;
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => VRRig.LocalRig.IsTagged() && !vrrig.IsTagged() && !GTPlayer.Instance.disableMovement && vrrig.Distance(VRRig.LocalRig) < tagAuraDistance))
            {
                color = Color.green;
                ReportTag(vrrig);
            }
            if (Manager.GetIndex("Visualize Tag Aura").enabled)
                Visuals.Visualize(PrimitiveType.Cylinder, VRRig.LocalRig.bodyTransform.position, Quaternion.identity, new Vector3(tagAuraDistance, 0.01f, tagAuraDistance), Manager.GetIndex("Prettier Visualize").enabled ? color : backgroundColor.GetCurrentColor(), -20170121181, 0.1f);
        }

        public static void GripTagAura()
        {
            if (rightGrab)
                TagAura();
        }

        public static void TagAuraPlayer(VRRig giving)
        {
            foreach (var vrrig in from vrrig in Players.ActiveRigs let distance = Vector3.Distance(vrrig.headMesh.transform.position, giving.transform.position) where giving.IsTagged() && !vrrig.IsTagged() && !GTPlayer.Instance.disableMovement && distance < tagAuraDistance && !VRRig.LocalRig.IsLocal() && VRRig.LocalRig.IsTagged() select vrrig)
                TagPlayer(GetPlayerFromVRRig(vrrig));
        }

        public static void TagAuraGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                TagAuraPlayer(lockTarget);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void TagAuraAll()
        {
            foreach (VRRig vrrig in Players.ActiveRigs)
                TagAuraPlayer(vrrig);
        }

        public static void TagReach()
        {
            if (!VRRig.LocalRig.IsTagged()) return;
            GorillaTagger.Instance.maxTagDistance = float.MaxValue;

            GorillaTagger.Instance.tagRadiusOverride = tagReachDistance;
            GorillaTagger.Instance.tagRadiusOverrideFrame = Time.frameCount + 16;

            if (Manager.GetIndex("Visualize Tag Reach").enabled)
            {
                Visuals.Visualize(PrimitiveType.Sphere, GorillaTagger.Instance.leftHandTransform.position, Quaternion.identity, new Vector3(tagReachDistance, 1, tagReachDistance), backgroundColor.GetCurrentColor(), -149286, 0.1f);
                Visuals.Visualize(PrimitiveType.Sphere, GorillaTagger.Instance.rightHandTransform.position, Quaternion.identity, new Vector3(tagReachDistance, 1, tagReachDistance), backgroundColor.GetCurrentColor(), -149285, 0.1f);
            }
        }

        public static bool ValidateTag(VRRig Rig) =>
            Vector3.Distance(ServerSyncPos, Rig.transform.position) < 6f;

        public static void TagGun()
        {
            if (!NetworkSystem.Instance.InRoom) return;
            if (instantTag)
            {
                InstantTagGun();
                return;
            }

            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (!lockTarget.IsTagged())
                    {
                        VRRig.LocalRig.enabled = false;

                        if (!Manager.GetIndex("Obnoxious Tag").enabled)
                            VRRig.LocalRig.transform.position = lockTarget.transform.position - new Vector3(0f, 3f, 0f);
                        else
                        {
                            Vector3 position = lockTarget.transform.position + RandomVector3();

                            VRRig.LocalRig.transform.position = position;

                            VRRig.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                            VRRig.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                            VRRig.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                            VRRig.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                            VRRig.LocalRig.leftIndex.calcT = 0f;
                            VRRig.LocalRig.leftMiddle.calcT = 0f;
                            VRRig.LocalRig.leftThumb.calcT = 0f;

                            VRRig.LocalRig.leftIndex.LerpFinger(1f, false);
                            VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);
                            VRRig.LocalRig.leftThumb.LerpFinger(1f, false);

                            VRRig.LocalRig.rightIndex.calcT = 0f;
                            VRRig.LocalRig.rightMiddle.calcT = 0f;
                            VRRig.LocalRig.rightThumb.calcT = 0f;

                            VRRig.LocalRig.rightIndex.LerpFinger(1f, false);
                            VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);
                            VRRig.LocalRig.rightThumb.LerpFinger(1f, false);
                        }

                        if (ValidateTag(lockTarget))
                            ReportTag(lockTarget);
                    }
                    else
                    {
                        gunLocked = false;
                        VRRig.LocalRig.enabled = true;
                    }
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        if (PhotonNetwork.IsMasterClient)
                            AddInfected(GetPlayerFromVRRig(gunTarget));
                        else
                        {
                            if (!VRRig.LocalRig.IsTagged()) return;
                            gunLocked = true;
                            lockTarget = gunTarget;
                        }
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        VRRig.LocalRig.enabled = true;
                    }
                } });
        }

        private static float reportTagDelay;
        public static void ReportTag(VRRig rig)
        {
            if (Time.time > reportTagDelay)
            {
                reportTagDelay = Time.time + 0.1f;
                GameMode.ReportTag(rig.GetPlayer());
            }
        }

        public static void TagPlayer(NetPlayer player)
        {
            if (!NetworkSystem.Instance.InRoom) return;
            static void TurnOff()
            {
                Manager.GetIndex("Tag Player").SetEnabled(false);
                ReloadMenu();
            }
            if (PhotonNetwork.IsMasterClient)
            {
                AddInfected(player);
                return;
            }

            if (!VRRig.LocalRig.IsTagged())
            {
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be tagged.");
                TurnOff();
                return;
            }

            if (instantTag)
            {
                InstantTagPlayer(player);
                TurnOff();
                return;
            }

            VRRig targetRig = GetVRRigFromPlayer(player);
            if (!targetRig.IsTagged())
            {
                VRRig.LocalRig.enabled = false;

                if (!Manager.GetIndex("Obnoxious Tag").enabled)
                    VRRig.LocalRig.transform.position = targetRig.transform.position - new Vector3(0f, 3f, 0f);
                else
                {
                    Vector3 position = targetRig.transform.position + RandomVector3();

                    VRRig.LocalRig.transform.position = position;

                    VRRig.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    VRRig.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                    VRRig.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                    VRRig.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    VRRig.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    VRRig.LocalRig.leftIndex.calcT = 0f;
                    VRRig.LocalRig.leftMiddle.calcT = 0f;
                    VRRig.LocalRig.leftThumb.calcT = 0f;

                    VRRig.LocalRig.leftIndex.LerpFinger(1f, false);
                    VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);
                    VRRig.LocalRig.leftThumb.LerpFinger(1f, false);

                    VRRig.LocalRig.rightIndex.calcT = 0f;
                    VRRig.LocalRig.rightMiddle.calcT = 0f;
                    VRRig.LocalRig.rightThumb.calcT = 0f;

                    VRRig.LocalRig.rightIndex.LerpFinger(1f, false);
                    VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);
                    VRRig.LocalRig.rightThumb.LerpFinger(1f, false);
                }

                if (ValidateTag(targetRig))
                    ReportTag(targetRig);
            }
            else
                TurnOff();
        }

        public static void FlickTagGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                GTPlayer.Instance.GetControllerTransform(false).position = NewPointer.transform.position;

                if (Vector3.Distance(GTPlayer.Instance.GetControllerTransform(false).position, GorillaTagger.Instance.bodyCollider.transform.position) > 4f)
                    GTPlayer.Instance.GetControllerTransform(false).position = GorillaTagger.Instance.bodyCollider.transform.position + (GTPlayer.Instance.GetControllerTransform(false).position - GorillaTagger.Instance.bodyCollider.transform.position) * 4f;
            });
        }

        public static void TagAll()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            static void TurnOff()
            {
                Manager.GetIndex("Tag All").SetEnabled(false);
                ReloadMenu();
            }

            if (GorillaGameManager.instance.GameType() == GameModeType.HuntDown)
            {
                HuntTagAll();
                return;
            }

            if (NetworkSystem.Instance.IsMasterClient)
            {
                foreach (Player v in PhotonNetwork.PlayerList)
                    AddInfected(v);

                TurnOff();
                Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Everyone is tagged!");
            }
            else
            {
                if (instantTag)
                {
                    InstantTagAll();
                    Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Everyone is tagged!");
                    TurnOff();
                    return;
                }

                if (!VRRig.LocalRig.IsTagged())
                {
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be tagged.");
                    TurnOff();
                }
                else
                {
                    bool isInfectedPlayers = Players.ActiveRigs.Any(vrrig => !vrrig.IsTagged());
                    if (isInfectedPlayers)
                    {
                        foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.IsTagged()))
                        {
                            VRRig.LocalRig.enabled = false;

                            if (!Manager.GetIndex("Obnoxious Tag").enabled)
                                VRRig.LocalRig.transform.position = vrrig.transform.position - new Vector3(0f, 3f, 0f);
                            else
                            {
                                Vector3 position = vrrig.transform.position + RandomVector3();

                                VRRig.LocalRig.transform.position = position;
                                VRRig.LocalRig.transform.rotation = RandomQuaternion();

                                VRRig.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                                VRRig.LocalRig.leftHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();
                                VRRig.LocalRig.rightHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();

                                VRRig.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                                VRRig.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                                VRRig.LocalRig.leftIndex.calcT = 0f;
                                VRRig.LocalRig.leftMiddle.calcT = 0f;
                                VRRig.LocalRig.leftThumb.calcT = 0f;

                                VRRig.LocalRig.leftIndex.LerpFinger(1f, false);
                                VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);
                                VRRig.LocalRig.leftThumb.LerpFinger(1f, false);

                                VRRig.LocalRig.rightIndex.calcT = 0f;
                                VRRig.LocalRig.rightMiddle.calcT = 0f;
                                VRRig.LocalRig.rightThumb.calcT = 0f;

                                VRRig.LocalRig.rightIndex.LerpFinger(1f, false);
                                VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);
                                VRRig.LocalRig.rightThumb.LerpFinger(1f, false);
                            }

                            if (ValidateTag(vrrig))
                                ReportTag(vrrig);
                        }
                    }
                    else
                    {
                        Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Everyone is tagged!");
                        VRRig.LocalRig.enabled = true;
                        TurnOff();
                    }
                }
            }
        }

        public static void InstantTagPlayer(NetPlayer Target)
        {
            if (!VRRig.LocalRig.IsTagged() || Target.VRRig().IsTagged())
                return;

            Vector3 archiveRigPosition = VRRig.LocalRig.transform.position;
            VRRig.LocalRig.transform.position = GetVRRigFromPlayer(Target).transform.position;

            SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
            GameMode.ReportTag(Target);

            VRRig.LocalRig.transform.position = archiveRigPosition;

            RPCProtection();
        }

        private static float tagGunDelay;
        public static void InstantTagGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > tagGunDelay)
                {
                    try
                    {
                        VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                        if (gunTarget && !gunTarget.IsLocal())
                        {
                            tagGunDelay = Time.time + 0.2f;
                            InstantTagPlayer(NetPlayerToPlayer(GetPlayerFromVRRig(gunTarget)));
                        }
                    }
                    catch { }
                }
            });
        }

        public static void InstantTagAll()
        {
            if (!VRRig.LocalRig.IsTagged())
            {
                Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You must be tagged.");
                return;
            }

            Vector3 archiveRigPosition = VRRig.LocalRig.transform.position;

            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.IsTagged()))
            {
                VRRig.LocalRig.transform.position = vrrig.transform.position;
                SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
                GameMode.ReportTag(GetPlayerFromVRRig(vrrig));
            }

            VRRig.LocalRig.transform.position = archiveRigPosition;

            SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
            RPCProtection();
        }

        public static void HuntTagAll()
        {
            GorillaHuntManager huntComputer = ManagerRegistry.GorillaHuntManager;
            NetPlayer target = huntComputer.GetTargetOf(PhotonNetwork.LocalPlayer);
            if (!GTPlayer.Instance.disableMovement)
            {
                VRRig vrrig = GetVRRigFromPlayer(target);
                VRRig.LocalRig.enabled = false;

                if (!Manager.GetIndex("Obnoxious Tag").enabled)
                    VRRig.LocalRig.transform.position = vrrig.transform.position - new Vector3(0f, 3f, 0f);
                else
                {
                    Vector3 position = vrrig.transform.position + RandomVector3();

                    VRRig.LocalRig.transform.position = position;

                    VRRig.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    VRRig.LocalRig.leftHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();
                    VRRig.LocalRig.rightHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();

                    VRRig.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    VRRig.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    VRRig.LocalRig.leftIndex.calcT = 0f;
                    VRRig.LocalRig.leftMiddle.calcT = 0f;
                    VRRig.LocalRig.leftThumb.calcT = 0f;

                    VRRig.LocalRig.leftIndex.LerpFinger(1f, false);
                    VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);
                    VRRig.LocalRig.leftThumb.LerpFinger(1f, false);

                    VRRig.LocalRig.rightIndex.calcT = 0f;
                    VRRig.LocalRig.rightMiddle.calcT = 0f;
                    VRRig.LocalRig.rightThumb.calcT = 0f;

                    VRRig.LocalRig.rightIndex.LerpFinger(1f, false);
                    VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);
                    VRRig.LocalRig.rightThumb.LerpFinger(1f, false);
                }

                if (ValidateTag(vrrig))
                    ReportTag(vrrig);
            }
            else
            {
                Notifications.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Everyone is tagged!");
                VRRig.LocalRig.enabled = true;
                Manager.GetIndex("Tag All").enabled = false;
                ReloadMenu();
            }
        }

        public static void TagBot()
        {
            if (NetworkSystem.Instance.InRoom)
            {
                if (!VRRig.LocalRig.IsTagged())
                {
                    if (InfectedList().Count > 0)
                        TagSelf();
                }
                else
                {
                    if (InfectedList().Count != PhotonNetwork.PlayerList.Length)
                        TagAll();
                }
            }
            else
                VRRig.LocalRig.enabled = true;
        }

        public static void NoTagOnJoin()
        {
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerConfig.Player_HasDoneTutorial, out object obj);
            if (obj is bool b && b)
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
                {
                    { PlayerConfig.Player_HasDoneTutorial, false }
                });
            }
        }

        public static void TagOnJoin() =>
            NetworkSystem.Instance.SetMyTutorialComplete();

        public static void ReportAntiTag()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                if (VRRig.LocalRig.IsTagged())
                    return true;

                MassSerialize(true, new[] { VRRig.LocalRig.GetPhotonView() });

                Vector3 positionArchive = VRRig.LocalRig.transform.position;
                SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != PhotonNetwork.MasterClient.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                VRRig.LocalRig.transform.position = new Vector3(99999f, 99999f, 99999f);
                SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });

                RPCProtection();
                VRRig.LocalRig.transform.position = positionArchive;

                return false;
            };
        }

        public static float paintbrawlSpamDelay;

        public static void PaintbrawlKillGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                if (!NetworkSystem.Instance.IsMasterClient)
                    Notifications.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                else
                    GameModes.SetPaintbrawlHealth(owner, 0);
            });
        }

        public static void PaintbrawlKillSelf()
        {
            GameModes.KillPaintbrawlPlayer(NetworkSystem.Instance.LocalPlayer);
        }

        public static void PaintbrawlKillAll()
        {
            if (!NetworkSystem.Instance.IsMasterClient)
                GameModes.KillPaintbrawlPlayer(GetRandomPlayer(false));
            else
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                    GameModes.SetPaintbrawlHealth(player.ActorNumber, 0);
            }
        }

        public static void DebugSlingshotAimbot()
        {
            if (VRRig.LocalRig.GetSlingshot() == null)
                return;

            if (VRRig.LocalRig.GetSlingshot().InLeftHand() ? leftTrigger > 0.5f : rightTrigger > 0.5f)
                return;

            List<NetPlayer> infected = InfectedList();
            List<VRRig> rigs = Players.ActiveRigs
                .Where(rig => !rig.isLocal)
                .Where(rig => !infected.Contains(rig.GetPlayer()))
                .ToList();

            Transform head = GorillaTagger.Instance.headCollider.transform;
            VRRig targetRig = rigs
                .Where(rig => rig != null)
                .Select(rig => new
                {
                    Rig = rig,
                    ToRig = (rig.transform.position - head.position).normalized,
                    Distance = Vector3.Distance(head.position, rig.transform.position)
                })
                .OrderBy(x => Vector3.Angle(head.forward, x.ToRig)) // only angle matters
                .ThenBy(x => x.Distance) // tiebreaker if multiple at same angle
                .Select(x => x.Rig)
                .FirstOrDefault();

            if (targetRig == null)
                return;

            Visuals.Visualize(PrimitiveType.Sphere, targetRig.headMesh.transform.position, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), Color.green, -91752, 0.1f);
        }
        public static void SlingshotHelper()
        {
            Slingshot slingshot = VRRig.LocalRig.GetSlingshot() as Slingshot;
            if (slingshot == null)
                return;

            if (slingshot.ForLeftHandSlingshot() ? rightGrab : leftGrab)
                slingshot.itemState = slingshot.ForLeftHandSlingshot() ? TransferrableObject.ItemStates.State2 : TransferrableObject.ItemStates.State3;
        }
        public static void SlingshotTriggerBot()
        {
            if (Fun.paintbrawlTriggerLine != null)
            {
                if (!Fun.paintbrawlTriggerLine.gameObject.activeSelf)
                {
                    Fun.paintbrawlTriggerLine = null;
                    Object.Destroy(Fun.paintbrawlTriggerLine.gameObject);
                }
                else
                    Fun.paintbrawlTriggerLine.gameObject.SetActive(false);
            }

            Slingshot localSlingshot = VRRig.LocalRig.GetSlingshot() as Slingshot;
            if (localSlingshot == null || !localSlingshot.InDrawingState())
                return;

            if (Fun.paintbrawlTriggerLine == null)
            {
                GameObject lineObject = new GameObject("LineObject");
                Fun.paintbrawlTriggerLine = lineObject.AddComponent<LineRenderer>();
                Fun.paintbrawlTriggerLine.positionCount = 25;
            }

            Fun.paintbrawlTriggerLine.gameObject.SetActive(true);

            Fun.paintbrawlTriggerLine.startColor = Color.black;
            Fun.paintbrawlTriggerLine.endColor = Color.black;

            Vector3 localPosition = localSlingshot.drawingHand.transform.position + (localSlingshot.centerOrigin.position - localSlingshot.drawingHand.transform.position).normalized * ((EquipmentInteractor.instance.grabRadius - localSlingshot.dummyProjectileColliderRadius) * (localSlingshot.dummyProjectileInitialScale * Mathf.Abs(localSlingshot.transform.lossyScale.x)));
            Vector3 localVelocity = localSlingshot.GetLaunchVelocity();

            Visuals.DrawTrajectory(localPosition, localVelocity, Fun.paintbrawlTriggerLine, NoInvisLayerMask(), Vector3.down * 10.79f);

            Fun.paintbrawlTriggerLine.enabled = false;

            if (Fun.paintbrawlTriggerLine.startColor == Color.green && Time.time > Fun.triggerBotDelay)
                Fun.triggerBotDelay = Time.time + 0.5f;

            if (Time.time < Fun.triggerBotDelay)
            {
                if (localSlingshot.ForLeftHandSlingshot())
                {
                    ControllerInputPoller.instance.rightControllerGripFloat = 0f;
                    ControllerInputPoller.instance.rightGrab = false;
                }
                else
                {
                    ControllerInputPoller.instance.leftControllerGripFloat = 0f;
                    ControllerInputPoller.instance.leftGrab = false;
                }
            }
        }
        public static void TagLagDetector()
        {
            if (NetworkSystem.Instance.InRoom && !NetworkSystem.Instance.IsMasterClient)
            {
                VRRig masterRig = PhotonNetwork.MasterClient.VRRig();
                bool thereIsTagLag = masterRig.GetTruePing() > 1000;

                switch (thereIsTagLag)
                {
                    case true when !Important.lastTagLag:
                        Notifications.SendNotification("<color=grey>[</color><color=red>TAG LAG</color><color=grey>]</color> There is currently tag lag.");
                        break;
                    case false when Important.lastTagLag:
                        Notifications.SendNotification("<color=grey>[</color><color=green>TAG LAG</color><color=grey>]</color> There is no longer tag lag.");
                        break;
                }

                Important.lastTagLag = thereIsTagLag;
            }
            else
            {
                if (Important.lastTagLag)
                    Notifications.SendNotification("<color=grey>[</color><color=green>TAG LAG</color><color=grey>]</color> There is no longer tag lag.");
                Important.lastTagLag = false;
            }
        }

        public static void FakeLag()
        {
            PlayerSerializePatch.delay = Manager.GetIndex("Fake Lag Others").enabled ? Movement.fakeLagDelay : (float?)null;

            SerializePatch.OverrideSerialization = !Manager.GetIndex("Disable Fake Lag Self").enabled
                ? (() =>
                {
                    MassSerialize(true, delay: Movement.fakeLagDelay);
                    return false;
                })
                : (Func<bool>)null;
        }

        public static void LagRange()
        {
            bool isTagged = VRRig.LocalRig.IsTagged();

            VRRig closestRig = Players.ActiveRigs
                .Where(rig => rig != null && !rig.isLocal &&
                                  (isTagged ? !rig.IsTagged() : rig.IsTagged()))
                .OrderBy(rig => Vector3.Distance(rig.transform.position, GorillaTagger.Instance.bodyCollider.transform.position))
                .FirstOrDefault();

            float rigDistance = closestRig == null ? float.MaxValue :
                          Vector3.Distance(GorillaTagger.Instance.bodyCollider.transform.position, closestRig.transform.position);

            if (rigDistance < 15f)
            {
                float lagPower = Mathf.Clamp(rigDistance, 1f, 15f) / 15f;
                PhotonNetwork.SerializationRate = 4 + (int)Math.Ceiling(lagPower * 6f);
            }
        }
        public static void Blink()
        {
            SerializePatch.OverrideSerialization = () => false;
            PlayerSerializePatch.stopSerialization = true;
            Movement.isBlinking = true;
        }

        public static void DisableBlink()
        {
            SerializePatch.OverrideSerialization = null;
            PlayerSerializePatch.stopSerialization = false;
            Movement.isBlinking = false;
        }
    }
}
