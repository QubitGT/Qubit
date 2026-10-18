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
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Netcode;
using Qubit.Patches.Tagging;
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
        #region Settings
        public static bool instantTag = true;

        public static float spamTagDelay;

        public static float tagAuraDistance = 1.666f;
        public static int tagAuraIndex = 1;
        public static readonly string[] TagAuraRangeNames = { "Short", "Normal", "Far", "Maximum" };
        public static readonly float[] TagAuraRangeDistances = { 0.777f, 1.666f, 3f, 5.5f };

        public static int tagRangeIndex;
        private static float tagReachDistance = 0.3f;
        public static readonly string[] TagReachDistanceNames = { "Unnoticable", "Normal", "Far", "Maximum" };
        public static readonly float[] TagReachDistances = { 0.3f, 0.5f, 1f, 3f };

        private static float reportTagDelay;

        private static float tagGunDelay;

        public static float paintbrawlSpamDelay;
        #endregion

        #region Methods
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
                Notifications.SendFormat("green", "SUCCESS", "You have been tagged.");
                TurnOff();
            }
            else
            {
                if (InfectedList().Contains(PhotonNetwork.LocalPlayer))
                {
                    Notifications.SendFormat("green", "SUCCESS", "You have been tagged.");
                    References.LocalRig.enabled = true;
                    TurnOff();

                    if (instantTag)
                        ViewSerialization.OverrideSerialization = null;
                }
                else
                {
                    VRRig rig = Players.ActiveRigs
                        .Where(r => !r.IsLocal() && r.IsTagged())
                        .OrderBy(r => r.Distance(References.LocalRig)
                                    + r.LatestVelocity().magnitude)
                        .FirstOrDefault();

                    if (instantTag)
                    {
                        ViewSerialization.OverrideSerialization = () =>
                        {
                            if (References.LocalRig.IsTagged())
                                return true;

                            MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                            Vector3 positionArchive = References.LocalRig.transform.position;
                            References.LocalRig.transform.position = rig.rightHandTransform.transform.position;
                            SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber, rig.GetPlayer().ActorNumber } });

                            RPCProtection();
                            References.LocalRig.transform.position = positionArchive;

                            return false;
                        };
                    }
                    else
                    {
                        if (!rig.IsTagged()) return;
                        References.LocalRig.enabled = false;
                        if (rig != null) References.LocalRig.transform.position = rig.rightHandTransform.position;

                        if (!Manager.GetIndex("Obnoxious Tag").enabled) return;
                        Quaternion rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
                        References.LocalRig.transform.rotation = rotation;

                        References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                        References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + RandomVector3();
                        References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + RandomVector3();

                        References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                        References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();
                    }
                }
            }
        }

        public static void UntagSelf()
        {
            if (Networking.IsMasterClient)
                RemoveInfected(PhotonNetwork.LocalPlayer);
            else if (!Networking.IsMasterClient)
            {
                Room.Reconnect();
                NoTagOnJoin();
            }

            References.Player.disableMovement = false;
        }

        public static void AntiTag()
        {
            if (Networking.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (!TagReports.invinciblePlayers.Contains(Networking.LocalPlayer))
                        TagReports.invinciblePlayers.Add(Networking.LocalPlayer);
                }
                else
                {
                    if (References.LocalRig.IsTagged())
                        UntagSelf();
                }
            }
            else
            {
                TagOnJoin();
                TagReports.invinciblePlayers.Clear();
            }
        }

        public static void DisableAntiTag()
        {
            if (Networking.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (TagReports.invinciblePlayers.Contains(Networking.LocalPlayer))
                        TagReports.invinciblePlayers.Remove(Networking.LocalPlayer);
                }
                NoTagOnJoin();
            }
        }

        public static void GiveTagLagGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (lockTarget != null)
                        TagReports.invinciblePlayers.Remove(lockTarget.GetPlayer());

                    gunLocked = true;
                    lockTarget = gunTarget;

                    TagReports.invinciblePlayers.Add(GetPlayerFromVRRig(gunTarget));

                }
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        TagReports.invinciblePlayers.Remove(lockTarget.GetPlayer());
                    }
                } });
        }
        public static void ApplyTagAuraRange(int index) { tagAuraIndex = index; tagAuraDistance = TagAuraRangeDistances[index]; }
        public static void ApplyTagReachDistance(int index) { tagRangeIndex = index; tagReachDistance = TagReachDistances[index]; }

        public static void TagAura()
        {
            Color color = Color.red;
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => References.LocalRig.IsTagged() && !vrrig.IsTagged() && !References.Player.disableMovement && vrrig.Distance(References.LocalRig) < tagAuraDistance))
            {
                color = Color.green;
                ReportTag(vrrig);
            }
            if (Manager.GetIndex("Visualize Tag Aura").enabled)
                Visuals.Visualize(PrimitiveType.Cylinder, References.LocalRig.bodyTransform.position, Quaternion.identity, new Vector3(tagAuraDistance, 0.01f, tagAuraDistance), Manager.GetIndex("Prettier Visualize").enabled ? color : backgroundColor.GetCurrentColor(), -20170121181, 0.1f);
        }

        public static void GripTagAura()
        {
            if (rightGrab)
                TagAura();
        }

        public static void TagAuraPlayer(VRRig giving)
        {
            foreach (var vrrig in from vrrig in Players.ActiveRigs let distance = Vector3.Distance(vrrig.headMesh.transform.position, giving.transform.position) where giving.IsTagged() && !vrrig.IsTagged() && !References.Player.disableMovement && distance < tagAuraDistance && !References.LocalRig.IsLocal() && References.LocalRig.IsTagged() select vrrig)
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
            if (!References.LocalRig.IsTagged()) return;
            References.Tagger.maxTagDistance = float.MaxValue;

            References.Tagger.tagRadiusOverride = tagReachDistance;
            References.Tagger.tagRadiusOverrideFrame = Time.frameCount + 16;

            if (Manager.GetIndex("Visualize Tag Reach").enabled)
            {
                Visuals.Visualize(PrimitiveType.Sphere, References.LeftHand.position, Quaternion.identity, new Vector3(tagReachDistance, 1, tagReachDistance), backgroundColor.GetCurrentColor(), -149286, 0.1f);
                Visuals.Visualize(PrimitiveType.Sphere, References.RightHand.position, Quaternion.identity, new Vector3(tagReachDistance, 1, tagReachDistance), backgroundColor.GetCurrentColor(), -149285, 0.1f);
            }
        }

        public static bool ValidateTag(VRRig Rig) =>
            Vector3.Distance(ServerSyncPos, Rig.transform.position) < 6f;

        public static void TagGun()
        {
            if (!Networking.InRoom) return;
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
                        References.LocalRig.enabled = false;

                        if (!Manager.GetIndex("Obnoxious Tag").enabled)
                            References.LocalRig.transform.position = lockTarget.transform.position - new Vector3(0f, 3f, 0f);
                        else
                        {
                            Vector3 position = lockTarget.transform.position + RandomVector3();

                            References.LocalRig.transform.position = position;

                            References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                            References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                            References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                            References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                            References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                            References.LocalRig.leftIndex.calcT = 0f;
                            References.LocalRig.leftMiddle.calcT = 0f;
                            References.LocalRig.leftThumb.calcT = 0f;

                            References.LocalRig.leftIndex.LerpFinger(1f, false);
                            References.LocalRig.leftMiddle.LerpFinger(1f, false);
                            References.LocalRig.leftThumb.LerpFinger(1f, false);

                            References.LocalRig.rightIndex.calcT = 0f;
                            References.LocalRig.rightMiddle.calcT = 0f;
                            References.LocalRig.rightThumb.calcT = 0f;

                            References.LocalRig.rightIndex.LerpFinger(1f, false);
                            References.LocalRig.rightMiddle.LerpFinger(1f, false);
                            References.LocalRig.rightThumb.LerpFinger(1f, false);
                        }

                        if (ValidateTag(lockTarget))
                            ReportTag(lockTarget);
                    }
                    else
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
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
                            if (!References.LocalRig.IsTagged()) return;
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
                        References.LocalRig.enabled = true;
                    }
                } });
        }
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
            if (!Networking.InRoom) return;
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

            if (!References.LocalRig.IsTagged())
            {
                Notifications.SendFormat("red", "ERROR", "You must be tagged.");
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
                References.LocalRig.enabled = false;

                if (!Manager.GetIndex("Obnoxious Tag").enabled)
                    References.LocalRig.transform.position = targetRig.transform.position - new Vector3(0f, 3f, 0f);
                else
                {
                    Vector3 position = targetRig.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
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
                References.Player.GetControllerTransform(false).position = NewPointer.transform.position;

                if (Vector3.Distance(References.Player.GetControllerTransform(false).position, References.BodyCollider.transform.position) > 4f)
                    References.Player.GetControllerTransform(false).position = References.BodyCollider.transform.position + (References.Player.GetControllerTransform(false).position - References.BodyCollider.transform.position) * 4f;
            });
        }

        public static void TagAll()
        {
            if (!Networking.InRoom) return;

            static void TurnOff()
            {
                Manager.GetIndex("Tag All").SetEnabled(false);
                ReloadMenu();
            }

            if (ManagerRegistry.GorillaGameManager.GameType() == GameModeType.HuntDown)
            {
                HuntTagAll();
                return;
            }

            if (Networking.IsMasterClient)
            {
                foreach (Player v in PhotonNetwork.PlayerList)
                    AddInfected(v);

                TurnOff();
                Notifications.SendFormat("green", "SUCCESS", "Everyone is tagged!");
            }
            else
            {
                if (instantTag)
                {
                    InstantTagAll();
                    Notifications.SendFormat("green", "SUCCESS", "Everyone is tagged!");
                    TurnOff();
                    return;
                }

                if (!References.LocalRig.IsTagged())
                {
                    Notifications.SendFormat("red", "ERROR", "You must be tagged.");
                    TurnOff();
                }
                else
                {
                    bool isInfectedPlayers = Players.ActiveRigs.Any(vrrig => !vrrig.IsTagged());
                    if (isInfectedPlayers)
                    {
                        foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.IsTagged()))
                        {
                            References.LocalRig.enabled = false;

                            if (!Manager.GetIndex("Obnoxious Tag").enabled)
                                References.LocalRig.transform.position = vrrig.transform.position - new Vector3(0f, 3f, 0f);
                            else
                            {
                                Vector3 position = vrrig.transform.position + RandomVector3();

                                References.LocalRig.transform.position = position;
                                References.LocalRig.transform.rotation = RandomQuaternion();

                                References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                                References.LocalRig.leftHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();
                                References.LocalRig.rightHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();

                                References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                                References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                                References.LocalRig.leftIndex.calcT = 0f;
                                References.LocalRig.leftMiddle.calcT = 0f;
                                References.LocalRig.leftThumb.calcT = 0f;

                                References.LocalRig.leftIndex.LerpFinger(1f, false);
                                References.LocalRig.leftMiddle.LerpFinger(1f, false);
                                References.LocalRig.leftThumb.LerpFinger(1f, false);

                                References.LocalRig.rightIndex.calcT = 0f;
                                References.LocalRig.rightMiddle.calcT = 0f;
                                References.LocalRig.rightThumb.calcT = 0f;

                                References.LocalRig.rightIndex.LerpFinger(1f, false);
                                References.LocalRig.rightMiddle.LerpFinger(1f, false);
                                References.LocalRig.rightThumb.LerpFinger(1f, false);
                            }

                            if (ValidateTag(vrrig))
                                ReportTag(vrrig);
                        }
                    }
                    else
                    {
                        Notifications.SendFormat("green", "SUCCESS", "Everyone is tagged!");
                        References.LocalRig.enabled = true;
                        TurnOff();
                    }
                }
            }
        }

        public static void InstantTagPlayer(NetPlayer Target)
        {
            if (!References.LocalRig.IsTagged() || Target.VRRig().IsTagged())
                return;

            Vector3 archiveRigPosition = References.LocalRig.transform.position;
            References.LocalRig.transform.position = GetVRRigFromPlayer(Target).transform.position;

            SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
            GameMode.ReportTag(Target);

            References.LocalRig.transform.position = archiveRigPosition;

            RPCProtection();
        }
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
            if (!References.LocalRig.IsTagged())
            {
                Notifications.SendFormat("red", "ERROR", "You must be tagged.");
                return;
            }

            Vector3 archiveRigPosition = References.LocalRig.transform.position;

            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.IsTagged()))
            {
                References.LocalRig.transform.position = vrrig.transform.position;
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
                GameMode.ReportTag(GetPlayerFromVRRig(vrrig));
            }

            References.LocalRig.transform.position = archiveRigPosition;

            SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });
            RPCProtection();
        }

        public static void HuntTagAll()
        {
            GorillaHuntManager huntComputer = ManagerRegistry.GorillaHuntManager;
            NetPlayer target = huntComputer.GetTargetOf(PhotonNetwork.LocalPlayer);
            if (!References.Player.disableMovement)
            {
                VRRig vrrig = GetVRRigFromPlayer(target);
                References.LocalRig.enabled = false;

                if (!Manager.GetIndex("Obnoxious Tag").enabled)
                    References.LocalRig.transform.position = vrrig.transform.position - new Vector3(0f, 3f, 0f);
                else
                {
                    Vector3 position = vrrig.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = vrrig.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
                }

                if (ValidateTag(vrrig))
                    ReportTag(vrrig);
            }
            else
            {
                Notifications.SendFormat("green", "SUCCESS", "Everyone is tagged!");
                References.LocalRig.enabled = true;
                Manager.GetIndex("Tag All").enabled = false;
                ReloadMenu();
            }
        }

        public static void TagBot()
        {
            if (Networking.InRoom)
            {
                if (!References.LocalRig.IsTagged())
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
                References.LocalRig.enabled = true;
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
            ManagerRegistry.NetworkSystem.SetMyTutorialComplete();

        public static void ReportAntiTag()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                if (References.LocalRig.IsTagged())
                    return true;

                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 positionArchive = References.LocalRig.transform.position;
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != PhotonNetwork.MasterClient.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                References.LocalRig.transform.position = new Vector3(99999f, 99999f, 99999f);
                SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } });

                RPCProtection();
                References.LocalRig.transform.position = positionArchive;

                return false;
            };
        }

        public static void PaintbrawlKillGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                if (!Networking.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                    GameModes.SetPaintbrawlHealth(owner, 0);
            });
        }

        public static void PaintbrawlKillSelf()
        {
            GameModes.KillPaintbrawlPlayer(Networking.LocalPlayer);
        }

        public static void PaintbrawlKillAll()
        {
            if (!Networking.IsMasterClient)
                GameModes.KillPaintbrawlPlayer(GetRandomPlayer(false));
            else
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                    GameModes.SetPaintbrawlHealth(player.ActorNumber, 0);
            }
        }

        public static void DebugSlingshotAimbot()
        {
            if (References.LocalRig.GetSlingshot() == null)
                return;

            if (References.LocalRig.GetSlingshot().InLeftHand() ? leftTrigger > 0.5f : rightTrigger > 0.5f)
                return;

            List<NetPlayer> infected = InfectedList();
            List<VRRig> rigs = Players.ActiveRigs
                .Where(rig => !rig.isLocal)
                .Where(rig => !infected.Contains(rig.GetPlayer()))
                .ToList();

            Transform head = References.HeadCollider.transform;
            VRRig targetRig = rigs
                .Where(rig => rig != null)
                .Select(rig => new
                {
                    Rig = rig,
                    ToRig = (rig.transform.position - head.position).normalized,
                    Distance = Vector3.Distance(head.position, rig.transform.position)
                })
                .OrderBy(x => Vector3.Angle(head.forward, x.ToRig)) 
                .ThenBy(x => x.Distance) 
                .Select(x => x.Rig)
                .FirstOrDefault();

            if (targetRig == null)
                return;

            Visuals.Visualize(PrimitiveType.Sphere, targetRig.headMesh.transform.position, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), Color.green, -91752, 0.1f);
        }
        public static void SlingshotHelper()
        {
            Slingshot slingshot = References.LocalRig.GetSlingshot() as Slingshot;
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

            Slingshot localSlingshot = References.LocalRig.GetSlingshot() as Slingshot;
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

            Vector3 localPosition = localSlingshot.drawingHand.transform.position + (localSlingshot.centerOrigin.position - localSlingshot.drawingHand.transform.position).normalized * ((ManagerRegistry.EquipmentInteractor.grabRadius - localSlingshot.dummyProjectileColliderRadius) * (localSlingshot.dummyProjectileInitialScale * Mathf.Abs(localSlingshot.transform.lossyScale.x)));
            Vector3 localVelocity = localSlingshot.GetLaunchVelocity();

            Visuals.DrawTrajectory(localPosition, localVelocity, Fun.paintbrawlTriggerLine, NoInvisLayerMask(), Vector3.down * 10.79f);

            Fun.paintbrawlTriggerLine.enabled = false;

            if (Fun.paintbrawlTriggerLine.startColor == Color.green && Time.time > Fun.triggerBotDelay)
                Fun.triggerBotDelay = Time.time + 0.5f;

            if (Time.time < Fun.triggerBotDelay)
            {
                if (localSlingshot.ForLeftHandSlingshot())
                {
                    References.InputPoller.rightControllerGripFloat = 0f;
                    References.InputPoller.rightGrab = false;
                }
                else
                {
                    References.InputPoller.leftControllerGripFloat = 0f;
                    References.InputPoller.leftGrab = false;
                }
            }
        }
        public static void TagLagDetector()
        {
            if (Networking.InRoom && !Networking.IsMasterClient)
            {
                VRRig masterRig = PhotonNetwork.MasterClient.VRRig();
                bool thereIsTagLag = masterRig.GetTruePing() > 1000;

                switch (thereIsTagLag)
                {
                    case true when !Important.lastTagLag:
                        Notifications.SendFormat("red", "TAG LAG", "There is currently tag lag.");
                        break;
                    case false when Important.lastTagLag:
                        Notifications.SendFormat("green", "TAG LAG", "There is no longer tag lag.");
                        break;
                }

                Important.lastTagLag = thereIsTagLag;
            }
            else
            {
                if (Important.lastTagLag)
                    Notifications.SendFormat("green", "TAG LAG", "There is no longer tag lag.");
                Important.lastTagLag = false;
            }
        }

        public static void FakeLag()
        {
            RigSerialization.delay = Manager.GetIndex("Fake Lag Others").enabled ? Movement.fakeLagDelay : (float?)null;

            ViewSerialization.OverrideSerialization = !Manager.GetIndex("Disable Fake Lag Self").enabled
                ? (() =>
                {
                    MassSerialize(true, delay: Movement.fakeLagDelay);
                    return false;
                })
                : (Func<bool>)null;
        }

        public static void LagRange()
        {
            bool isTagged = References.LocalRig.IsTagged();

            VRRig closestRig = Players.ActiveRigs
                .Where(rig => rig != null && !rig.isLocal &&
                                  (isTagged ? !rig.IsTagged() : rig.IsTagged()))
                .OrderBy(rig => Vector3.Distance(rig.transform.position, References.BodyCollider.transform.position))
                .FirstOrDefault();

            float rigDistance = closestRig == null ? float.MaxValue :
                          Vector3.Distance(References.BodyCollider.transform.position, closestRig.transform.position);

            if (rigDistance < 15f)
            {
                float lagPower = Mathf.Clamp(rigDistance, 1f, 15f) / 15f;
                PhotonNetwork.SerializationRate = 4 + (int)Math.Ceiling(lagPower * 6f);
            }
        }
        public static void Blink()
        {
            ViewSerialization.OverrideSerialization = () => false;
            RigSerialization.stopSerialization = true;
            Movement.isBlinking = true;
        }

        public static void DisableBlink()
        {
            ViewSerialization.OverrideSerialization = null;
            RigSerialization.stopSerialization = false;
            Movement.isBlinking = false;
        }
        #endregion
    }
}
