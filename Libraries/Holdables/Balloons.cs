/*
 * Qubit Menu  Libraries/Holdables/Balloons.cs
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
    public static class Balloons
    {

        public static void BalloonAura()
        {
            foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
            {
                if (balloon.ownerRig.isLocal)
                {
                    balloon.gameObject.transform.position = References.HeadCollider.transform.position + RandomVector3();
                    balloon.gameObject.transform.rotation = RandomQuaternion();
                }
                else
                    balloon.WorldShareableRequestOwnership();
            }
        }

        public static void PopAllBalloons()
        {
            foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
                balloon.OwnerPopBalloon();
        }

        public static void GrabBalloons()
        {
            if (rightGrab)
            {
                foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
                {
                    if (balloon.ownerRig.isLocal)
                        balloon.gameObject.transform.position = References.RightHand.position;
                    else
                        balloon.WorldShareableRequestOwnership();
                }
            }
        }

        public static void SpazBalloons()
        {
            foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
            {
                if (balloon.ownerRig.isLocal)
                    balloon.gameObject.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                else
                    balloon.WorldShareableRequestOwnership();
            }
        }

        public static void OrbitBalloons()
        {
            BalloonHoldable[] them = GetAllType<BalloonHoldable>();
            int index = 0;
            foreach (BalloonHoldable balloon in them)
            {
                if (balloon.ownerRig.isLocal)
                {
                    float offset = 360f / them.Length * index;
                    balloon.gameObject.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 5f, 2, MathF.Sin(offset + (float)Time.frameCount / 30) * 5f);
                }
                else
                    balloon.WorldShareableRequestOwnership();

                index++;
            }
        }

        public static void BalloonGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
                {
                    if (balloon.ownerRig.isLocal)
                        balloon.gameObject.transform.position = NewPointer.transform.position + Vector3.up;
                    else
                        balloon.WorldShareableRequestOwnership();
                }
            });
        }

        public static void DestroyBalloons()
        {
            foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
            {
                if (balloon.ownerRig.isLocal)
                    balloon.gameObject.transform.position = new Vector3(99999f, 99999f, 99999f);
                else
                    balloon.WorldShareableRequestOwnership();
            }
        }

        public static void BecomeBalloon()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

            foreach (BalloonHoldable balloon in GetAllType<BalloonHoldable>())
            {
                if (balloon.ownerRig.isLocal)
                {
                    balloon.gameObject.transform.position = References.BodyCollider.transform.position;
                    balloon.gameObject.transform.rotation = References.HeadCollider.transform.rotation;
                    break;
                }

                balloon.WorldShareableRequestOwnership();
            }
        }
    }
}
