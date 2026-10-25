/*
 * Qubit Menu  Modules/Categories/Gamemode/Paintbrawl.cs
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
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.GameModes;
using Object = UnityEngine.Object;

namespace Qubit.Modules.Categories.Gamemode
{
    public static class Paintbrawl
    {
        public static float paintbrawlSpamDelay;

        public static void PaintbrawlKillGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer owner = GetPlayerFromVRRig(gunTarget);
                if (!Networking.IsMasterClient)
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
                else
                    SetPaintbrawlHealth(owner, 0);
            });
        }

        public static void PaintbrawlKillSelf()
        {
            KillPaintbrawlPlayer(Networking.LocalPlayer);
        }

        public static void PaintbrawlKillAll()
        {
            if (!Networking.IsMasterClient)
                KillPaintbrawlPlayer(GetRandomPlayer(false));
            else
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                    SetPaintbrawlHealth(player.ActorNumber, 0);
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

            Visual.Visualize(PrimitiveType.Sphere, targetRig.headMesh.transform.position, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), Color.green, -91752, 0.1f);
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

            Visual.DrawTrajectory(localPosition, localVelocity, Fun.paintbrawlTriggerLine, NoInvisLayerMask(), Vector3.down * 10.79f);

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
    }
}
