/*
 * Qubit Menu  Libraries/Networking/LocalPlayer.cs
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

using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Qubit.Modules.Categories;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Settings;

namespace Qubit.Libraries.Networking
{
    public static class LocalPlayer
    {

        public static void TeleportPlayer(Vector3 pos, bool keepVelocity = false) // Prevents your hands from getting stuck on trees
        {
            GTPlayer.Instance.TeleportTo(World2Player(pos), GTPlayer.Instance.transform.rotation, keepVelocity);
            VRRig.LocalRig.transform.position = pos;

            closePosition = Vector3.zero;
            Movement.lastPosition = Vector3.zero;
            if (VRKeyboard == null) return;
            VRKeyboard.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
            VRKeyboard.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation;
        }

        public static void TeleportPlayer(Transform transform, bool matchDestinationRotation = true, bool maintainVelocity = true)
        {
            GTPlayer.Instance.TeleportTo(transform, matchDestinationRotation, maintainVelocity);
            closePosition = Vector3.zero;
            Movement.lastPosition = Vector3.zero;
            if (VRKeyboard == null) return;
            VRKeyboard.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
            VRKeyboard.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation;
        }

        public static void SetRotation(Quaternion rotation) =>
            SetRotation(rotation.y);

        public static void SetRotation(float rotation) =>
           GTPlayer.Instance.Turn(rotation - GTPlayer.Instance.mainCamera.transform.eulerAngles.y);

        public static void ChangeName(string PlayerName, bool noColor = false)
        {
            if (!noColor)
            {
                try
                {
                    if (!GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(PhotonNetwork
                            .LocalPlayer.UserId) &&
                        !CosmeticWardrobeProximityDetector.IsUserNearWardrobe(PhotonNetwork.LocalPlayer.ActorNumber)) return;
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, VRRig.LocalRig.playerColor.r, VRRig.LocalRig.playerColor.g, VRRig.LocalRig.playerColor.b);
                }
                catch { }
            }
            GorillaComputer.instance.currentName = PlayerName;

            GorillaComputer.instance.SetLocalNameTagText(GorillaComputer.instance.currentName);
            GorillaComputer.instance.savedName = GorillaComputer.instance.currentName;
            PlayerPrefs.SetString("playerName", GorillaComputer.instance.currentName);
            PlayerPrefs.Save();

            PhotonNetwork.LocalPlayer.NickName = PlayerName;
            RPCProtection();


        }

        public static void ChangeColor(Color color, object target = null)
        {
            PlayerPrefs.SetFloat("redValue", Mathf.Clamp(color.r, 0f, 1f));
            PlayerPrefs.SetFloat("greenValue", Mathf.Clamp(color.g, 0f, 1f));
            PlayerPrefs.SetFloat("blueValue", Mathf.Clamp(color.b, 0f, 1f));

            GorillaTagger.Instance.UpdateColor(color.r, color.g, color.b);
            PlayerPrefs.Save();

            try
            {
                switch (target)
                {
                    case null:
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, color.r, color.g, color.b);
                        break;
                    case NetPlayer player:
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", player, color.r, color.g, color.b);
                        break;
                    case RpcTarget targets:
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", targets, color.r, color.g, color.b);
                        break;
                }

                RPCProtection();
            }
            catch { }
        }
    }
}
