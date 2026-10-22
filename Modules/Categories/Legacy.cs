/*
 * Qubit Menu  Modules/Categories/Legacy.cs
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

using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Modules.Classes;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Libraries.Networking.LocalPlayer;

namespace Qubit.Modules.Categories
{
    public static class Legacy
    {
        public static void ForceUnloadCustomMap()
        {
            VirtualStumpSerializer serializer = GetAllType<VirtualStumpSerializer>().FirstOrDefault();
            if (serializer == null)
                return;

            serializer.UnloadMapSynced();
            RPCProtection();
        }

        private static float scDelay;

        public static void SizeChanger()
        {
            Movement.sizeScale = Mathf.Clamp(Movement.sizeScale, 0.1f, 10f);
            Movement.SizeChanger();
            Movement.sizeScale = Mathf.Clamp(Movement.sizeScale, 0.1f, 10f);

            if (Time.time > scDelay)
            {
                scDelay = Time.time + 1.01f;
                References.MyRigView.SendRPC("RPC_UpdateNativeSize", RpcTarget.Others, new object[] { Movement.sizeScale });
            }
        }

        public static void DisableSizeChanger()
        {
            Movement.DisableSizeChanger();
            References.MyRigView.SendRPC("RPC_UpdateNativeSize", RpcTarget.Others, new object[] { 1f });
        }

        public static void LongName() => ChangeName("ǄǄǄǄǄǄǄǄǄǄǄǄ");

        private static AngryBeeSwarm angryBeeSwarm;
        private static AngryBeeSwarm AngryBeeSwarm => angryBeeSwarm ??= GameObject.Find("Environment Objects/PersistentObjects_Prefab/Nowruz2024_PersistentObjects/AngryBeeSwarm/FloatingChaseBeeSwarm")?.GetComponent<AngryBeeSwarm>();

        public static void KillBees()
        {
            if (AngryBeeSwarm == null)
                return;

            AngryBeeSwarm.GetComponent<PhotonView>().OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
            AngryBeeSwarm.Emerge(new Vector3(99999f, 99999f, 99999f), new Vector3(999f, 999f, 999f));
        }

        public static void AngerBees()
        {
            if (AngryBeeSwarm == null)
                return;

            AngryBeeSwarm.GetComponent<PhotonView>().OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
            Vector3 target = References.HeadCollider.transform.position + new Vector3(0f, 3f, 0f);
            AngryBeeSwarm.Emerge(target, target);
        }

        public static void AngerBeesGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (AngryBeeSwarm == null)
                    return;

                AngryBeeSwarm.GetComponent<PhotonView>().OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
                Vector3 target = lockTarget.transform.position + new Vector3(0f, 3f, 0f);
                AngryBeeSwarm.Emerge(target, target);
            });
        }

        public static void AngerBeesAll()
        {
            if (AngryBeeSwarm == null)
                return;

            AngryBeeSwarm.GetComponent<PhotonView>().OwnerActorNr = PhotonNetwork.LocalPlayer.ActorNumber;
            foreach (VRRig rig in Players.ActiveRigs)
            {
                Vector3 target = rig.transform.position + new Vector3(0f, 3f, 0f);
                AngryBeeSwarm.Emerge(target, target);
            }
        }
    }
}
