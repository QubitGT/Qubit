/*
 * Qubit Menu  Utilities/GameModes.cs
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
using GorillaTagScripts;
using Photon.Pun;
using Qubit.Core;
using Qubit.Libraries.Interface;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.Players;
using static Qubit.Utilities.GameObj;

namespace Qubit.Utilities
{
    public static class GameModes
    {
        public static List<NetPlayer> InfectedList()
        {
            List<NetPlayer> infected = new List<NetPlayer>();

            if (!Networking.InRoom || ManagerRegistry.GorillaGameManager == null)
                return infected;

            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.SuperInfect:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    if (ManagerRegistry.GorillaGameManager is GorillaTagManager tagManager)
                    {
                        if (tagManager.isCurrentlyTag)
                        {
                            if (tagManager.currentIt != null)
                                infected.Add(tagManager.currentIt);
                        }
                        else if (tagManager.currentInfected != null)
                        {
                            infected.AddRange(tagManager.currentInfected.Where(element => element != null));
                        }
                    }
                    break;

                case GameModeType.Ghost:
                case GameModeType.Ambush:
                    if (ManagerRegistry.GorillaGameManager is GorillaAmbushManager ghostManager)
                    {
                        if (ghostManager.isCurrentlyTag)
                        {
                            if (ghostManager.currentIt != null)
                                infected.Add(ghostManager.currentIt);
                        }
                        else if (ghostManager.currentInfected != null)
                        {
                            infected.AddRange(ghostManager.currentInfected.Where(element => element != null));
                        }
                    }
                    break;

                case GameModeType.Paintbrawl:
                    if (ManagerRegistry.GorillaGameManager is GorillaPaintbrawlManager paintbrawlManager && paintbrawlManager.playerLives != null)
                    {
                        infected.AddRange(
                            paintbrawlManager.playerLives
                                .Where(element => element.Value <= 0)
                                .Select(element => PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(element.Key))
                                .Where(dummy => dummy != null)
                                .Select(dummy => (NetPlayer)dummy)
                        );

                        if (ManagerRegistry.NetworkSystem?.LocalPlayer != null && !infected.Contains(Networking.LocalPlayer))
                            infected.Add(Networking.LocalPlayer);
                    }
                    break;
            }

            return infected;
        }

        public static void AddInfected(NetPlayer plr)
        {
            if (!Networking.InRoom || ManagerRegistry.GorillaGameManager == null || plr == null)
                return;

            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.SuperInfect:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    if (ManagerRegistry.GorillaGameManager is GorillaTagManager tagManager)
                    {
                        if (tagManager.isCurrentlyTag)
                            tagManager.ChangeCurrentIt(plr);
                        else if (tagManager.currentInfected != null && !tagManager.currentInfected.Contains(plr))
                            tagManager.AddInfectedPlayer(plr);
                    }
                    break;

                case GameModeType.Ghost:
                case GameModeType.Ambush:
                    if (ManagerRegistry.GorillaGameManager is GorillaAmbushManager ghostManager)
                    {
                        if (ghostManager.isCurrentlyTag)
                            ghostManager.ChangeCurrentIt(plr);
                        else if (ghostManager.currentInfected != null && !ghostManager.currentInfected.Contains(plr))
                            ghostManager.AddInfectedPlayer(plr);
                    }
                    break;

                case GameModeType.Paintbrawl:
                    if (ManagerRegistry.GorillaGameManager is GorillaPaintbrawlManager paintbrawlManager && paintbrawlManager.playerLives != null)
                        SetPaintbrawlHealth(plr.ActorNumber, 0);

                    break;
            }
        }

        public static void RemoveInfected(NetPlayer plr)
        {
            if (!Networking.InRoom || ManagerRegistry.GorillaGameManager == null || plr == null)
                return;

            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.SuperInfect:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    if (ManagerRegistry.GorillaGameManager is GorillaTagManager tagManager)
                    {
                        switch (tagManager.isCurrentlyTag)
                        {
                            case true when tagManager.currentIt == plr:
                                tagManager.currentIt = null;
                                break;
                            case false when tagManager.currentInfected != null && tagManager.currentInfected.Contains(plr):
                                tagManager.currentInfected.Remove(plr);
                                break;
                        }
                    }
                    break;

                case GameModeType.Ghost:
                case GameModeType.Ambush:
                    if (ManagerRegistry.GorillaGameManager is GorillaAmbushManager ghostManager)
                    {
                        switch (ghostManager.isCurrentlyTag)
                        {
                            case true when ghostManager.currentIt == plr:
                                ghostManager.currentIt = null;
                                break;
                            case false when ghostManager.currentInfected != null && ghostManager.currentInfected.Contains(plr):
                                ghostManager.currentInfected.Remove(plr);
                                break;
                        }
                    }
                    break;

                case GameModeType.Paintbrawl:
                    if (ManagerRegistry.GorillaGameManager is GorillaPaintbrawlManager paintbrawlManager && paintbrawlManager.playerLives != null)
                        SetPaintbrawlHealth(plr.ActorNumber, 3);

                    break;
            }
        }

        public static void AddRock(NetPlayer plr)
        {
            if (!Networking.InRoom || ManagerRegistry.GorillaGameManager == null || plr == null)
                return;

            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.SuperInfect:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    if (ManagerRegistry.GorillaGameManager is GorillaTagManager tagManager)
                        tagManager.ChangeCurrentIt(plr);

                    break;

                case GameModeType.Ghost:
                case GameModeType.Ambush:
                    if (ManagerRegistry.GorillaGameManager is GorillaAmbushManager ghostManager)
                        ghostManager.ChangeCurrentIt(plr);

                    break;

                case GameModeType.Paintbrawl:
                    if (ManagerRegistry.GorillaGameManager is GorillaPaintbrawlManager paintbrawlManager && paintbrawlManager.playerLives != null)
                        SetPaintbrawlHealth(plr.ActorNumber, 0);

                    break;
            }
        }

        public static void RemoveRock(NetPlayer plr)
        {
            if (!Networking.InRoom || ManagerRegistry.GorillaGameManager == null || plr == null)
                return;

            switch (ManagerRegistry.GorillaGameManager.GameType())
            {
                case GameModeType.Infection:
                case GameModeType.InfectionCompetitive:
                case GameModeType.SuperInfect:
                case GameModeType.FreezeTag:
                case GameModeType.PropHunt:
                    if (ManagerRegistry.GorillaGameManager is GorillaTagManager tagManager && tagManager.currentIt == plr)
                        tagManager.ChangeCurrentIt(null);

                    break;

                case GameModeType.Ghost:
                case GameModeType.Ambush:
                    if (ManagerRegistry.GorillaGameManager is GorillaAmbushManager ghostManager && ghostManager.currentIt == plr)
                        ghostManager.ChangeCurrentIt(null);

                    break;

                case GameModeType.Paintbrawl:
                    if (ManagerRegistry.GorillaGameManager is GorillaPaintbrawlManager paintbrawlManager && paintbrawlManager.playerLives != null)
                        SetPaintbrawlHealth(plr.ActorNumber, 3);

                    break;
            }
        }

        #region Paintbrawl
        private static int paintbrawlHitIndex;
        private static readonly Dictionary<int, float> paintbrawlHitDelays = new Dictionary<int, float>();

        public static void SetPaintbrawlHealth(int actorNumber, int health) =>
            ManagerRegistry.GorillaPaintbrawlManager.playerLives[actorNumber] = health;

        public static void SetPaintbrawlHealth(NetPlayer target, int health)
        {
            if (Networking.IsMasterClient)
            {
                SetPaintbrawlHealth(target.ActorNumber, health);
                return;
            }

            if (ManagerRegistry.GorillaPaintbrawlManager.playerLives.TryGetValue(target.ActorNumber, out int current) && health < current)
                ReportPaintbrawlHit(target);
        }

        public static void ReportPaintbrawlHit(NetPlayer target)
        {
            if (paintbrawlHitDelays.TryGetValue(target.ActorNumber, out float lastTime))
            {
                if (Time.time > lastTime)
                    return;
            }

            paintbrawlHitDelays[target.ActorNumber] = Time.time + 3.1f;

            VRRig rig = GetVRRigFromPlayer(target);
            GameMode.ActiveNetworkHandler.SendRPC("RPC_ReportSlingshotHit", false, NetPlayerToPlayer(target), rig.transform.position, paintbrawlHitIndex);
            RPCProtection();

            paintbrawlHitIndex++;
        }

        public static void KillPaintbrawlPlayer(NetPlayer target)
        {
            if (!Networking.IsMasterClient)
                ReportPaintbrawlHit(target);
            else
                SetPaintbrawlHealth(target.ActorNumber, 0);
        }

        public static void StartPaintbrawl() =>
            ManagerRegistry.GorillaPaintbrawlManager.StartBattle();

        public static void EndPaintbrawl() =>
            ManagerRegistry.GorillaPaintbrawlManager.BattleEnd();

        public static void SetPaintbrawlCooldowns(float hitCooldown, float tagCooldown, float stunGracePeriod)
        {
            GorillaPaintbrawlManager brawlManager = ManagerRegistry.GorillaPaintbrawlManager;
            brawlManager.hitCooldown = hitCooldown;
            brawlManager.tagCoolDown = tagCooldown;
            brawlManager.stunGracePeriod = stunGracePeriod;
        }
        #endregion

        #region Guardian
        public static bool IsGuardian(NetPlayer player) =>
            ManagerRegistry.GorillaGuardianManager.IsPlayerGuardian(player);

        public static void SetGuardian(NetPlayer target)
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }
            if (IsGuardian(target))
                return;

            foreach (TappableGuardianIdol tgi in GetAllType<TappableGuardianIdol>())
            {
                if (tgi.manager && tgi.manager.photonView && !tgi.isChangingPositions)
                {
                    GorillaGuardianZoneManager zoneManager = tgi.zoneManager;
                    if (zoneManager.IsZoneValid() && tgi.manager && zoneManager.CurrentGuardian == null)
                    {
                        zoneManager.SetGuardian(target);
                        return;
                    }
                }
            }
        }

        private static float guardianTapDelay;
        public static void BecomeGuardian()
        {
            if (Networking.InRoom)
            {
                if (ManagerRegistry.GorillaGameManager.GameType() != GameModeType.Guardian)
                    return;

                if (Networking.IsMasterClient)
                {
                    if (!References.LocalRig.enabled)
                        References.LocalRig.enabled = true;
                    if (!IsGuardian(PhotonNetwork.LocalPlayer))
                        SetGuardian(PhotonNetwork.LocalPlayer);
                }
                else
                {
                    foreach (TappableGuardianIdol tgi in GetAllType<TappableGuardianIdol>())
                    {
                        if (tgi.manager && tgi.manager.photonView && !tgi.isChangingPositions)
                        {
                            GorillaGuardianZoneManager zoneManager = tgi.zoneManager;
                            if (!IsGuardian(Networking.LocalPlayer) && zoneManager.IsZoneValid() && tgi.manager)
                            {
                                References.LocalRig.enabled = false;
                                References.LocalRig.transform.position = tgi.transform.position;
                                References.LocalRig.leftHand.rigTarget.transform.position = tgi.transform.position;
                                References.LocalRig.rightHand.rigTarget.transform.position = tgi.transform.position;

                                if (Time.time > guardianTapDelay)
                                {
                                    guardianTapDelay = Time.time + (zoneManager._currentActivationTime >= zoneManager.requiredActivationTime - 1f ? 0f : 0.2f);
                                    tgi.OnTap(1);
                                    RPCProtection();
                                }
                            }
                        }
                        else
                            References.LocalRig.enabled = true;
                    }
                }
            }
        }
        #endregion

        #region Prop Hunt
        public static void SetPropHuntHandDistance(float distance)
        {
            if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
                ManagerRegistry.GorillaPropHuntGameManager.m_ph_hand_follow_distance = distance;
        }

        public static void SetPropHuntRoundStart(int millis)
        {
            if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
                ManagerRegistry.GorillaPropHuntGameManager._ph_timeRoundStartedMillis = millis;
        }

        public static void RandomizePropHuntSeed()
        {
            if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
                ManagerRegistry.GorillaPropHuntGameManager._ph_randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
        }
        #endregion

        #region Monke Ball
        public static void SetMonkeBallTeams(Dictionary<int, int> colors) 
        {
            var filteredPlayers = ManagerRegistry.NetworkSystem.AllNetPlayers
                .Where(p => colors.ContainsKey(p.ActorNumber));
            ManagerRegistry.MonkeBallGame.photonView.RPC(
                "RequestSetGameStateRPC",
                RpcTarget.All,
                (int)MonkeBallGame.GameState.Playing,
                PhotonNetwork.Time + (ManagerRegistry.MonkeBallGame.gameDuration - 1f),
                filteredPlayers.Select(p => p.ActorNumber).ToArray(),
                filteredPlayers.Select(p => colors[p.ActorNumber]).ToArray(),
                new int[ManagerRegistry.MonkeBallGame.team.Count],
                ManagerRegistry.MonkeBallGame.startingBalls
                    .Select(ball => BitPackUtils.PackHandPosRotForNetwork(ball.transform.position, ball.transform.rotation))
                    .ToArray(),
                ManagerRegistry.MonkeBallGame.startingBalls
                    .Select(ball => BitPackUtils.PackWorldPosForNetwork(ball.gameBall.GetVelocity()))
                    .ToArray()
            );
        }
        #endregion

        public static void RequestGrab(this GameEntity gameEntity, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (gameEntityManager.IsAuthority())
                (manager ?? ManagerRegistry.GhostReactor.GameEntityManager).photonView.RPC("GrabEntityRPC", RpcTarget.All, new object[] { gameEntity.id, isLeftHand, BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation), Networking.LocalPlayer.GetPlayer() });
            else
                (manager ?? ManagerRegistry.GhostReactor.GameEntityManager).RequestGrabEntity(gameEntity.id, isLeftHand, localPosition, localRotation);
        }

        public static void RequestThrow(this GameEntity gameEntity, bool isLeftHand, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, GameEntityManager manager = null)
        {
            GameEntityManager gameEntityManager = manager ?? ManagerRegistry.GhostReactor.GameEntityManager;
            if (gameEntityManager.IsAuthority())
                gameEntityManager.photonView.RPC("ThrowEntityRPC", RpcTarget.All, new object[] { gameEntity.id, isLeftHand, position, rotation, velocity, angVelocity, Networking.LocalPlayer.GetPlayer(), PhotonNetwork.Time });
            else
                gameEntityManager.photonView.RPC("RequestThrowEntityRPC", RpcTarget.MasterClient, new object[]
                {
                    gameEntityManager.GetNetIdFromEntityId(gameEntity.id),
                    isLeftHand,
                    position,
                    rotation,
                    velocity,
                    angVelocity
                });
        }

        public static int CreateTypeNetId(this GameEntityManager manager, int typeId) =>
            manager.CreateNetId(1 + manager.FactoryGetBuiltInEntityCountById(typeId));

        public static int GetInvalidNetId(this GameEntityManager manager) =>
            manager.GetNetIdFromEntityId(GameEntityId.Invalid);}
}
