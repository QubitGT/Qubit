/*
 * Qubit Menu  Libraries/Holdables/Gliders.cs
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

using Settings = Qubit.Modules.Categories.Settings;
using Fun = Qubit.Modules.Categories.Fun;
using Overpowered = Qubit.Modules.Categories.Overpowered;
using Manager = Qubit.Modules.Manager;
using static Qubit.Modules.Manager;
using Module = Qubit.Modules.Module;
using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using GorillaTag.Cosmetics;
using GorillaTag.Rendering;
using GorillaTagScripts;
using GorillaTagScripts.Builder;
using Ionic.Zlib;
using POpusCodec.Enums;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using PlayFab;
using PlayFab.ClientModels;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Menu;
using Qubit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows.Speech;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Transcription;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Categories.Fun;
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameModes;
using static Qubit.Utilities.GameObj;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Libraries.Holdables
{
    public static class Gliders
    {

        public static void GliderGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                {
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                        glider.gameObject.transform.position = NewPointer.transform.position + Vector3.up;
                    else
                        glider.OnHover(null, null);
                }
            });
        }

        public static void ModifyGliderSpeed(float pullUpLiftBonus, float dragVsSpeedDragFactor)
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                glider.pullUpLiftBonus = pullUpLiftBonus;
                glider.dragVsSpeedDragFactor = dragVsSpeedDragFactor;
            }
        }

        public static void FixGliderSpeed()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                glider.pullUpLiftBonus = 0.1f;
                glider.dragVsSpeedDragFactor = 0.2f;
            }
        }

        public static void GrabGliders()
        {
            if (rightGrab)
            {
                foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                {
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    {
                        glider.gameObject.transform.position = References.RightHand.position;
                        glider.gameObject.transform.rotation = References.RightHand.rotation;
                    }
                    else
                        glider.OnHover(null, null);
                }
            }
        }

        public static void RespawnGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.Respawn();
                else
                    glider.OnHover(null, null);
            }
        }

        public static void SpazGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.gameObject.transform.rotation = RandomQuaternion();
                else
                    glider.OnHover(null, null);
            }
        }

        public static void GliderAura()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                {
                    glider.gameObject.transform.position = References.HeadCollider.transform.position + RandomVector3();
                    glider.gameObject.transform.rotation = RandomQuaternion();
                }
                else
                    glider.OnHover(null, null);
            }
        }

        public static void OrbitGliders()
        {
            GliderHoldable[] them = GetAllType<GliderHoldable>();
            int index = 0;
            foreach (GliderHoldable glider in them)
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                {
                    float offset = 360f / them.Length * index;
                    glider.gameObject.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 5f, 2, MathF.Sin(offset + (float)Time.frameCount / 30) * 5f);
                }
                else
                    glider.OnHover(null, null);

                index++;
            }
        }

        public static void DestroyGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.gameObject.transform.position = new Vector3(99999f, 99999f, 99999f);
                else
                    glider.OnHover(null, null);
            }
        }

        public static void GliderBlindGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }

                if (gunLocked)
                {
                    foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                    {
                        if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                        {
                            glider.gameObject.transform.position = lockTarget.headMesh.transform.position;
                            glider.gameObject.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        }
                        else
                            glider.OnHover(null, null);
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                } });
        }

        public static void GliderBlindAll()
        {
            GliderHoldable[] those = GetAllType<GliderHoldable>();
            int index = 0;
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
            {
                try
                {
                    GliderHoldable glider = those[index];
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    {
                        glider.gameObject.transform.position = vrrig.headMesh.transform.position;
                        glider.gameObject.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                    }
                    else
                        glider.OnHover(null, null);
                }
                catch { }
                index++;
            }
        }
    }
}
