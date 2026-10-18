/*
 * Qubit Menu  Modules/Categories/Fun.cs
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
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using PlayFab;
using PlayFab.ClientModels;
using POpusCodec.Enums;
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
using static Qubit.Modules.Categories.Projectiles;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.Assets;
using static Qubit.Utilities.GameModes;
using static Qubit.Utilities.GameObj;
using static Qubit.Libraries.Holdables.Bugs;
using static Qubit.Libraries.Holdables.LckCamera;
using static Qubit.Libraries.Holdables.Hoverboards;
using static Qubit.Libraries.Entities.Blocks;
using static Qubit.Libraries.Entities.Cosmetics;
using Qubit.Libraries.Holdables;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories
{
    public static class Fun
    {
        #region Types
        public class MicPitchShifter : VoiceComponent
        {
            public float PitchFactor = 1.5f;
            public PitchProcessor floatProcessor;

            public void PhotonVoiceCreated(PhotonVoiceCreatedParams p)
            {
                if (p.Voice is LocalVoiceAudioFloat floatVoice)
                {
                    floatProcessor = new PitchProcessor(PitchFactor);
                    floatVoice.AddPostProcessor(floatProcessor);
                }
            }

            public class PitchProcessor : IProcessor<float>
            {
                private readonly float pitch;

                public PitchProcessor(float pitchFactor) =>
                    pitch = Mathf.Clamp(pitchFactor, 0.5f, 2f);

                public float[] Process(float[] buf)
                {
                    int inputLength = buf.Length;
                    float[] output = new float[inputLength];

                    float sampleIndex = 0f;
                    for (int i = 0; i < inputLength; i++)
                    {
                        int indexFloor = Mathf.FloorToInt(sampleIndex);
                        int indexCeil = Mathf.Min(indexFloor + 1, inputLength - 1);
                        float t = sampleIndex - indexFloor;

                        float interpolated = Mathf.Lerp(buf[indexFloor], buf[indexCeil], t);
                        output[i] = interpolated;

                        sampleIndex += pitch;
                        if (sampleIndex >= inputLength - 1)
                            break;
                    }

                    return output;
                }

                public void Dispose() { }
            }
        }

        public class LoopbackFactory : IAudioReader<float>
        {
            private readonly Queue<float> buffer = new Queue<float>();

            public void Feed(float[] data)
            {
                foreach (var sample in data)
                    buffer.Enqueue(sample);
            }

            public bool Read(float[] bufferOut)
            {
                if (buffer.Count < bufferOut.Length)
                    return false;

                for (int i = 0; i < bufferOut.Length; i++)
                    bufferOut[i] = buffer.Dequeue();

                return true;
            }

            public int SamplingRate => 16000;
            public int Channels => 1;
            public string Error => null;

            public void Dispose() =>
                buffer.Clear();
        }
        #endregion

        #region Settings
        public static float lastBangTime;
        public static readonly float BPM = 159f;

        public static float soundboardVolumeIndex = 1;

        public static float soundboardSpeedIndex = 1;

        public static int headSpinIndex;
        public static readonly float[] HeadSpinSpeedAmounts = { 2f, 7.5f, 8f, 9f, 200f };
        public static readonly string[] HeadSpinSpeedNames = { "Very Slow", "Slow", "Normal", "Fast", "Very Fast" };

        public static float headSpinSpeed = 10f;

        public static float instantPartyDelay;

        public static Coroutine waterSplashCoroutine;

        public static float splashDel;

        private static bool lastlhboop;
        private static bool lastrhboop;

        private static bool autoclickstate;

        public static readonly List<object[]> keyLogs = new List<object[]>();

        private static GameObject FreeCamObject;
        private static Vector3 CameraVelocity;

        public static float elapsedTime = Time.time;

        private static bool wasTagged;

        public static int targetFOV = 90;

        private static float muteDelay;

        private static float pressButtonDelay;

        public static readonly Dictionary<string, string> modsToSpoof = new Dictionary<string, string>();

        private static float tapDelay;

        private static float buttonDelay;

        private static float hitDelay;

        private static float moleMachineDelay;

        private static bool previousBraceletSpamState;
        private static float braceletSpamDelay;

        public static float isDirtyDelay;

        private static float lastTimeDingied;

        private static float delaybetweenscore;

        public static int targetQuestScore = 69;

        private static float spamDelay;
        private static bool returnOrTeleport;

        private static bool openOrClose;

        private static VirtualStumpAd virtualStumpAd;

        private static float purchaseDelay;

        public static float killDelay;

        private static LoopbackFactory factory;
        private static float copyVoiceGunDelay;

        public static DictationRecognizer drec;

        public static Coroutine dropBoard;

        public static float hoverboardGunDelay;

        public static float gbgd;

        public static Color projHookColor = Color.white;

        public static float hoverboardSpamDelay;

        public static bool flashColor;
        public static float flashDelay;

        private static bool lastDrawing;

        public static LineRenderer paintbrawlTriggerLine;
        public static float triggerBotDelay;

        public static float getOwnershipDelay;

        public static float bugSpamDelay;
        public static bool bugSpamToggle;

        public static float cameraSpamDelay;
        public static bool cameraSpamType;

        private static int objectIndex;
        private static float everythingSpamDelay;

        public static readonly Dictionary<string, bool> lastInAirValues = new Dictionary<string, bool>();

        public static bool grabbingCamera;
        public static bool grabbingHand;

        public static float delayer = -1f;

        public static float floatPower = 0.35f;

        public static Vector3 position = Vector3.zero;

        public static bool isFiring;

        public static float hoverboardAuraDelay;

        public static bool lastWasNull;

        public static bool previousGripDown;
        public static bool previousTriggerDown;

        public static float nameCycleDelay;
        public static int nameCycleIndex;
        public static int cycleSpeedIndex = 2;
        public static float nameCycleDebounce = 1f;

        public static string[] names = { };

        public static string name;

        public static float colorChangerDelay;

        public static int colorChangeType;
        public static bool strobeColor;

        public static float stealIdentityDelay;

        private static float stealCosmeticsDelay;

        public static int accessoryType;
        public static int hat;

        public static bool lastHitL;
        public static bool lastHitR;
        public static bool lastHitLP;
        public static bool lastHitRP;
        public static bool lastHitRS;

        public static readonly Dictionary<string[], int[]> cachePacked = new Dictionary<string[], int[]>();

        public static List<string> ownedArchive;
        public static List<string> tryOnCosmetics;


        public static float delay;

        private static float delayonhold;

        public static int[] archiveCosmetics;

        public static int rememberdirectory;

        public static float lastTimeCosmeticsChecked;

        public static float lastTimePaidCosmeticsChecked;

        public static bool lasttagged;

        public static bool hasGivenCosmetics;

        private static float idgundelay;

        private static float allNarrationDelay;

        private static float creationDateDelay;

        public static bool tinnitusSelf;

        public static Dictionary<string, int> ObjectByName { get => ManagerRegistry.GhostReactor.GameEntityManager.itemPrefabFactory.ToDictionary(prefab => prefab.Value.name, prefab => prefab.Key); }

        public static string textToRender;

        public static Dictionary<string, int> GadgetByName
        {
            get =>
                ManagerRegistry.SuperInfection.GameEntityManager.itemPrefabFactory
                .ToDictionary(prefab => prefab.Value.name, prefab => prefab.Key);
        }

        public static float lucyDelay;

        public static int red = 10;
        public static int green = 5;
        public static int blue;

        public static string targetRank = "High";
        #endregion

        #region Methods
        public static void FixHead()
        {
            References.LocalRig.head.trackingRotationOffset.x = 0f;
            References.LocalRig.head.trackingRotationOffset.y = 0f;
            References.LocalRig.head.trackingRotationOffset.z = 0f;
        }

        public static void UpsideDownHead() =>
            References.LocalRig.head.trackingRotationOffset.z = 180f;

        public static void BrokenNeck() =>
            References.LocalRig.head.trackingRotationOffset.z = 90f;

        public static void BackwardsHead() =>
            References.LocalRig.head.trackingRotationOffset.y = 180f;

        public static void SidewaysHead() =>
            References.LocalRig.head.trackingRotationOffset.y = 90f;
        public static void HeadBang()
        {
            if (Time.time > lastBangTime)
            {
                References.LocalRig.head.trackingRotationOffset.x = 50f;
                lastBangTime = Time.time + 60f / BPM;
            }
            else
                References.LocalRig.head.trackingRotationOffset.x = Mathf.Lerp(References.LocalRig.head.trackingRotationOffset.x, 0f, 0.1f);
        }
        public static void ApplySoundboardVolume(int index)
        {
            soundboardVolumeIndex = index * 0.05f;
            Voices.Get().ClipVolume = soundboardVolumeIndex;
        }
        public static void ApplySoundboardSpeed(int index)
        {
            soundboardSpeedIndex = index * 0.05f;
            Voices.Get().ClipSpeed = soundboardSpeedIndex;
        }
        public static void ApplyHeadSpinSpeed(int index)
        {
            headSpinIndex = index;
            headSpinSpeed = HeadSpinSpeedAmounts[index];
        }

        public static void SpazHead(string axis)
        {
            int offset = Random.Range(0, 360);
            switch (axis.ToLower())
            {
                case "x":
                    References.LocalRig.head.trackingRotationOffset.x = offset;
                    break;
                case "y":
                    References.LocalRig.head.trackingRotationOffset.y = offset;
                    break;
                case "z":
                    References.LocalRig.head.trackingRotationOffset.z = offset;
                    break;
                default:
                    return;
            }
        }

        public static void FlipHands()
        {
            Vector3 lh = References.LeftHand.position;
            Vector3 rh = References.RightHand.position;
            Quaternion lhr = References.LeftHand.rotation;
            Quaternion rhr = References.RightHand.rotation;

            References.Player.GetControllerTransform(false).transform.position = lh;
            References.Player.GetControllerTransform(true).transform.position = rh;

            References.Player.GetControllerTransform(false).transform.rotation = lhr;
            References.Player.GetControllerTransform(true).transform.rotation = rhr;
        }

        public static void FixHandTaps()
        {
            EffectDataPatch.enabled = false;
            EffectDataPatch.tapsEnabled = true;
            EffectDataPatch.doOverride = false;
            EffectDataPatch.overrideVolume = 0.1f;
            EffectDataPatch.tapMultiplier = 1;
            References.Tagger.handTapVolume = 0.1f;
        }

        public static void LoudHandTaps()
        {
            EffectDataPatch.enabled = true;
            EffectDataPatch.tapsEnabled = true;
            EffectDataPatch.doOverride = true;
            EffectDataPatch.overrideVolume = 99999f;
            EffectDataPatch.tapMultiplier = 10;
            References.Tagger.handTapVolume = 99999f;
        }

        public static void SilentHandTaps()
        {
            EffectDataPatch.enabled = true;
            EffectDataPatch.tapsEnabled = false;
            EffectDataPatch.doOverride = false;
            EffectDataPatch.overrideVolume = 0f;
            EffectDataPatch.tapMultiplier = 0;
            References.Tagger.handTapVolume = 0;
        }

        public static void SilentHandTapsOnTag()
        {
            if (References.LocalRig.IsTagged())
                SilentHandTaps();
            else
                FixHandTaps();
        }
        public static IEnumerator EnableRig()
        {
            yield return new WaitForSeconds(0.3f);
            References.LocalRig.enabled = true;
        }

        public static void BetaWaterSplash(Vector3 splashPosition, Quaternion splashRotation, float splashScale = 4, float boundingRadius = 100f, bool bigSplash = true, bool enteringWater = false, object general = null, bool bypassDelay = false)
        {
            if (CanCallWaterSplashNow() || bypassDelay)
            {
                try
                {
                    general ??= RpcTarget.All;

                    splashScale = Mathf.Clamp(splashScale, 1E-05f, 1f);
                    boundingRadius = Mathf.Clamp(boundingRadius, 0.0001f, 0.5f);

                    if ((References.BodyCollider.transform.position - splashPosition).sqrMagnitude >= 8.5f)
                    {
                        References.LocalRig.transform.position = splashPosition;
                        SendSerialize(References.LocalRig.GetPhotonView());
                    }

                    object[] parameters = {
                        splashPosition,
                        splashRotation,
                        splashScale,
                        boundingRadius,
                        bigSplash,
                        enteringWater
                    };

                    switch (general)
                    {
                        case NetPlayer player:
                            References.MyRigView.SendRPC(WaterVolume.WaterSplashRPC, NetPlayerToPlayer(player), parameters);
                            break;
                        case RpcTarget target:
                            {
                                if (target == RpcTarget.All)
                                {
                                    ManagerRegistry.ObjectPools.Instantiate(References.Player.waterParams.rippleEffect, splashPosition, splashRotation, References.Player.waterParams.rippleEffectScale * boundingRadius * 2f);
                                    ManagerRegistry.ObjectPools.Instantiate(References.Player.waterParams.splashEffect, splashPosition, splashRotation, splashScale).GetComponent<WaterSplashEffect>().PlayEffect(bigSplash, enteringWater, splashScale);

                                    target = RpcTarget.Others;
                                }

                                References.MyRigView.SendRPC(WaterVolume.WaterSplashRPC, target, parameters);
                                break;
                            }
                        case int[] targets:
                            {
                                if (targets.Contains(Networking.LocalPlayer.ActorNumber))
                                    ManagerRegistry.ObjectPools.Instantiate(References.Player.waterParams.rippleEffect, splashPosition, splashRotation, References.Player.waterParams.rippleEffectScale * boundingRadius * 2f);

                                References.LocalRig.GetPhotonView().RPC(WaterVolume.WaterSplashRPC, new RaiseEventOptions { TargetActors = targets }, parameters);
                                break;
                            }
                    }

                    Security.RPCProtection();
                }
                catch { }
            }

        }
        public static void WaterSplashHands()
        {
            if (Time.time > splashDel && (rightGrab || leftGrab))
            {
                BetaWaterSplash(rightGrab ? References.RightHand.position : References.LeftHand.position, rightGrab ? References.RightHand.rotation : References.LeftHand.rotation);
                splashDel = Time.time + 0.1f;
            }
        }

        public static void GiveWaterSplashHandsGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                if (lockTarget.rightMiddle.calcT > 0.5f || lockTarget.leftMiddle.calcT > 0.5f)
                {
                    if (Time.time > splashDel)
                    {
                        Vector3 splashPosition = lockTarget.rightMiddle.calcT > 0.5f ? lockTarget.rightHandTransform.position : lockTarget.leftHandTransform.position;
                        Quaternion splashRotation = lockTarget.rightMiddle.calcT > 0.5f ? lockTarget.rightHandTransform.rotation : lockTarget.leftHandTransform.rotation;

                        BetaWaterSplash(splashPosition, splashRotation);
                        splashDel = Time.time + 0.1f;
                    }
                }
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static bool CanCallWaterSplashNow()
        {
            float time = Time.time;
            int num = -1;
            float num2 = time + 10f;

            for (int i = 0; i < WaterVolume.splashRPCSendTimes.Length; i++)
            {
                if (WaterVolume.splashRPCSendTimes[i] < num2)
                {
                    num2 = WaterVolume.splashRPCSendTimes[i];
                    num = i;
                }
            }

            if (num != -1 && time - 0.5f > num2)
            {
                WaterVolume.splashRPCSendTimes[num] = time;
                return true;
            }
            return false;
        }

        public static void WaterSplashAura() =>
            BetaWaterSplash(References.LocalRig.transform.position + RandomVector3(2f), RandomQuaternion());

        public static void OrbitWaterSplash() =>
            BetaWaterSplash(References.HeadCollider.transform.position + new Vector3(MathF.Cos((float)Time.frameCount / 30), 1f, MathF.Sin((float)Time.frameCount / 30)), RandomQuaternion());

        public static void WaterSplashGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                BetaWaterSplash(NewPointer.transform.position, RandomQuaternion());
            });
        }

        public static void WaterSplashAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (CanCallWaterSplashNow())
                {
                    References.LocalRig.transform.position = rig.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { rig.Creator.ActorNumber } });
                    BetaWaterSplash(rig.head.rigTarget.position, RandomQuaternion(), bypassDelay: true);
                }
            }
        }

        public static void WaterSplashOnTouch()
        {
            foreach (VRRig rig in Players.ActiveRigs)
                if (!rig.IsLocal() && rig.IsBeingTouched())
                    BetaWaterSplash(rig.head.rigTarget.position, RandomQuaternion());
        }

        public static void WaterSplashWalk()
        {
            if (References.Player.IsHandTouching(true))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;
                BetaWaterSplash(References.LeftHand.position, Quaternion.Euler(ray.normal));
            }
            else if (References.Player.IsHandTouching(false))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;
                BetaWaterSplash(References.RightHand.position, Quaternion.Euler(ray.normal));
            }
        }
        public static void Boop(int sound = 84)
        {
            bool isBoopLeft = false;
            bool isBoopRight = false;
            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (!vrrig.isLocal)
                {
                    float D1 = Vector3.Distance(References.LeftHand.position, vrrig.headMesh.transform.position);
                    float D2 = Vector3.Distance(References.RightHand.position, vrrig.headMesh.transform.position);

                    float threshold = 0.275f;

                    if (!isBoopLeft)
                        isBoopLeft = D1 < threshold;

                    if (!isBoopRight)
                        isBoopRight = D2 < threshold;
                }
            }
            if (isBoopLeft && !lastlhboop)
            {
                if (Networking.InRoom)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, sound, true, 999999f);
                    Security.RPCProtection();
                }
                else
                    References.LocalRig.PlayHandTapLocal(sound, true, 999999f);
            }
            if (isBoopRight && !lastrhboop)
            {
                if (Networking.InRoom)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, sound, false, 999999f);
                    Security.RPCProtection();
                }
                else
                    References.LocalRig.PlayHandTapLocal(sound, false, 999999f);
            }
            lastlhboop = isBoopLeft;
            lastrhboop = isBoopRight;
        }
        public static void AutoClicker()
        {
            autoclickstate = !autoclickstate;
            if (leftTrigger > 0.5f)
            {
                References.InputPoller.leftControllerIndexFloat = autoclickstate ? 1f : 0f;

                References.LocalRig.leftHand.calcT = autoclickstate ? 1f : 0f;
                References.LocalRig.leftHand.MapMyFinger(1f);
            }
            if (rightTrigger > 0.5f)
            {
                References.InputPoller.rightControllerIndexFloat = autoclickstate ? 1f : 0f;

                References.LocalRig.rightHand.calcT = autoclickstate ? 1f : 0f;
                References.LocalRig.rightHand.MapMyFinger(1f);
            }
        }

        public static void EventReceived_KeyboardTracker(EventData data)
        {
            try
            {
                if (data.Code == Photon.Pun.PunEvent.RPC)
                {
                    string rpcName = PhotonNetwork.PhotonServerSettings.RpcList[int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                    object[] args = (object[])((Hashtable)data.CustomData)[4];
                    if (rpcName == "RPC_PlayHandTap" && (int)args[0] == 66)
                    {
                        VRRig target = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender));

                        Transform keyboardTransform = GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/GorillaComputerObject/ComputerUI/keyboard (1)").transform;
                        if (Vector3.Distance(target.transform.position, keyboardTransform.position) < 3f)
                        {
                            string handPath = (bool)args[1]
                                ? "rig/hand.L/palm.01.L/f_index.01.L/f_index.02.L/f_index.03.L/f_index.03.L_end"
                                : "rig/hand.R/palm.01.R/f_index.01.R/f_index.02.R/f_index.03.R/f_index.03.R_end";

                            Vector3 position = target.gameObject.transform.Find(handPath).position;

                            GameObject keysParent = keyboardTransform.Find("Buttons/Keys").gameObject;
                            float minimalDist = float.MaxValue;
                            string closestKey = "[Null]";

                            foreach (Transform child in keysParent.transform)
                            {
                                float dist = Vector3.Distance(child.position, position);
                                if (dist < minimalDist)
                                {
                                    minimalDist = dist;
                                    closestKey = Strings.ToTitleCase(child.name);
                                }
                            }

                            if (closestKey.Length > 1)
                                closestKey = "[" + closestKey + "]";

                            bool isKeyLogged = false;
                            for (int i = 0; i < keyLogs.Count; i++)
                            {
                                object[] keyLog = keyLogs[i];
                                if ((VRRig)keyLog[0] == target)
                                {
                                    isKeyLogged = true;

                                    string currentText = (string)keyLog[1];

                                    keyLogs[i][1] = closestKey.Contains("Delete") ? currentText.Length == 0 ? currentText : currentText[..^1] : currentText + closestKey;

                                    keyLogs[i][2] = Time.time + 5f;
                                    break;
                                }
                            }

                            if (!isKeyLogged)
                            {
                                if (!closestKey.Contains("Delete"))
                                    keyLogs.Add(new object[] { target, closestKey, Time.time + 5f });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void EnableKeyboardTracker() =>
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_KeyboardTracker;

        public static void KeyboardTracker()
        {
            if (keyLogs.Count > 0)
            {
                foreach (var keylog in keyLogs.Where(keylog => Time.time > (float)keylog[2]).ToList())
                {
                    Notifications.SendFormat("purple", "KEYLOGS", "" + (string)keylog[1], 5000);
                    keyLogs.Remove(keylog);
                }
            }
        }

        public static void DisableKeyboardTracker() =>
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_KeyboardTracker;

        public static void PreloadTagSounds()
        {
            string[] sounds = {
                "firstblood",
                "doublekill",
                "triplekill",
                "killingspree",
                "wickedsick",
                "monsterkill",
                "rampage"
            };

            foreach (string sound in sounds)
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/TagSounds/{sound}.ogg", $"Audio/Mods/Fun/TagSounds/{sound}.ogg");
        }
        public static void Freecam()
        {
            if (FreeCamObject == null)
            {
                FreeCamObject = new GameObject("Qubit_CameraObj");
                FreeCamObject.transform.position = References.HeadCollider.transform.position;
            }

            Camera FreeCamera = FreeCamObject.GetOrAddComponent<Camera>();
            FreeCamera.nearClipPlane = 0.01f;
            FreeCamera.cameraType = CameraType.Game;

            Vector3 inputDirection = new Vector3(leftJoystick.x, rightJoystick.y, leftJoystick.y);

            Vector3 playerForward = References.Player.bodyCollider.transform.forward.X_Z();
            Vector3 playerRight = References.Player.bodyCollider.transform.right.X_Z();

            Vector3 velocity = inputDirection.x * playerRight + inputDirection.y * Vector3.up + inputDirection.z * playerForward;
            velocity *= Movement.FlySpeed;

            CameraVelocity = Vector3.Lerp(CameraVelocity, velocity, 0.12875f);
            FreeCamObject.transform.position += CameraVelocity * Time.unscaledDeltaTime;
            FreeCamObject.transform.rotation = References.HeadCollider.transform.rotation;
        }

        public static void ThirdPersonCamera()
        {
            if (FreeCamObject == null)
            {
                FreeCamObject = new GameObject("Qubit_CameraObj");
                FreeCamObject.transform.position = References.HeadCollider.transform.position;
            }

            Camera FreeCamera = FreeCamObject.GetOrAddComponent<Camera>();
            FreeCamera.nearClipPlane = 0.01f;
            FreeCamera.cameraType = CameraType.Game;

            FreeCamObject.transform.position = References.BodyCollider.transform.TransformPoint(new Vector3(0f, 0.5f, -1.5f));
            FreeCamObject.transform.rotation = References.HeadCollider.transform.rotation;
        }

        public static void FlipCamera()
        {
            if (FreeCamObject == null)
            {
                FreeCamObject = new GameObject("Qubit_CameraObj");
                FreeCamObject.transform.position = References.HeadCollider.transform.position;
            }

            Camera FreeCamera = FreeCamObject.GetOrAddComponent<Camera>();
            FreeCamera.nearClipPlane = 0.01f;
            FreeCamera.cameraType = CameraType.Game;

            FreeCamObject.transform.position = References.HeadCollider.transform.position;
            FreeCamObject.transform.rotation = References.HeadCollider.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        }

        public static void Nausea()
        {
            if (FreeCamObject == null)
            {
                FreeCamObject = new GameObject("Qubit_CameraObj");
                FreeCamObject.transform.position = References.HeadCollider.transform.position;
            }

            float intensity = 15f;

            Camera FreeCamera = FreeCamObject.GetOrAddComponent<Camera>();
            FreeCamera.nearClipPlane = 0.01f;
            FreeCamera.cameraType = CameraType.Game;

            FreeCamObject.transform.position = References.HeadCollider.transform.position;
            FreeCamObject.transform.rotation = References.HeadCollider.transform.rotation * Quaternion.Euler(Mathf.Sin(Time.time) * intensity, Mathf.Cos(Time.time * 0.7f) * intensity, Mathf.Sin(Time.time * 1.3f) * intensity);
        }

        public static void HueShift(Color color) =>
            ManagerRegistry.ZoneShaderSettings.SetGroundFogValue(color, 0f, float.MaxValue, 0f);
        public static void PreloadJumpscareData()
        {
            LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Mods/Fun/jumpscare.png", "Images/Mods/Fun/jumpscare.png");
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/jumpscare.ogg", "Audio/Mods/Fun/jumpscare.ogg");
        }

        public static void JumpscareOnTag()
        {
            bool isTagged = References.LocalRig.IsTagged();

            if (isTagged && !wasTagged && Random.Range(0, 2000) == 1)
                Jumpscare();

            wasTagged = isTagged;
        }

        public static void Jumpscare() =>
            Coroutines.instance.StartCoroutine(JumpscareCoroutine());

        public static IEnumerator JumpscareCoroutine()
        {
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/jumpscare.ogg", "Audio/Mods/Fun/jumpscare.ogg", clip => clip.Play());

            HueShift(Color.black);
            GameObject jumpscareObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jumpscareObject.transform.SetParent(References.HeadCollider.transform, false);
            jumpscareObject.transform.localPosition = Vector3.forward * 1f;
            jumpscareObject.transform.localScale = new Vector3(1f, 1f, 0.01f);
            jumpscareObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Object.Destroy(jumpscareObject.GetComponent<Collider>());

            Material jumpscareMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

            jumpscareMaterial.SetFloat("_Surface", 1);
            jumpscareMaterial.SetFloat("_Blend", 0);
            jumpscareMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            jumpscareMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            jumpscareMaterial.SetFloat("_ZWrite", 0);
            jumpscareMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            jumpscareMaterial.renderQueue = (int)RenderQueue.Transparent;

            jumpscareObject.GetComponent<Renderer>().material = jumpscareMaterial;
            jumpscareObject.GetComponent<Renderer>().material.mainTexture = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Mods/Fun/jumpscare.png", "Images/Mods/Fun/jumpscare.png");

            for (int i = 0; i < 10; i++)
            {
                jumpscareObject.GetComponent<Renderer>().material.color = Color.white * ((i + 1) % 2);
                yield return new WaitForSeconds(0.05f);
            }

            Object.Destroy(jumpscareObject);

            HueShift(Color.clear);
        }

        public static void SpectateGun()
        {
            if (GetGunInput(false))
            {
                if (gunLocked && lockTarget != null)
                {
                    if (FreeCamObject == null)
                    {
                        FreeCamObject = new GameObject("Qubit_CameraObj");
                        FreeCamObject.transform.position = References.HeadCollider.transform.position;
                    }

                    Camera FreeCamera = FreeCamObject.GetOrAddComponent<Camera>();
                    FreeCamera.nearClipPlane = 0.01f;
                    FreeCamera.cameraType = CameraType.Game;

                    FreeCamObject.transform.position = lockTarget.headMesh.transform.transform.TransformPoint(new Vector3(0f, 0.25f, 0.25f));
                    FreeCamObject.transform.rotation = lockTarget.headMesh.transform.rotation;
                }
                else
                    GunLibrary.Target(lockTarget => { }, new GunLibrary.GunSettings { locks = true });
            }
            else
            {
                if (gunLocked)
                {
                    gunLocked = false;
                    DisableFreecam();
                }
            }
        }

        public static void DisableFreecam()
        {
            if (FreeCamObject != null)
            {
                Object.Destroy(FreeCamObject.GetComponent<Camera>());
                Object.Destroy(FreeCamObject);
                FreeCamObject = null;
            }
        }

        public static void ApplyTargetFOV(int level) =>
            targetFOV = 70 + (level * 10);


        public static void CameraFOV()
        {
            if (TPC != null)
                TPC.GetComponent<Camera>().fieldOfView = targetFOV;
        }

        public static void FixCameraFOV()
        {
            if (TPC != null)
                TPC.GetComponent<Camera>().fieldOfView = 60f;
        }

        public static void PrioritizeVoiceGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                foreach (VRRig rig in Players.ActiveRigs)
                    rig.voiceAudio.volume = rig != lockTarget ? 0.1f : 2f;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () => { gunLocked = false; } });
        }

        public static void DeprioritizeVoiceGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                foreach (VRRig rig in Players.ActiveRigs)
                    rig.voiceAudio.volume = rig != lockTarget ? 1f : 0.1f;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () => { gunLocked = false; } });
        }

        public static void ResetVoiceAll()
        {
            foreach (VRRig rig in Players.ActiveRigs)
                rig.voiceAudio.volume = 1f;
        }
        public static void MuteGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.linePlayer == GetPlayerFromVRRig(gunTarget)))
                {
                    muteDelay = Time.time + 0.5f;

                    line.muteButton.isOn = !line.muteButton.isOn;
                    line.PressButton(line.muteButton.isOn, GorillaPlayerLineButton.ButtonType.Mute);
                }
            }, new GunLibrary.GunSettings { when = () => Time.time > muteDelay });
        }

        public static void MuteAll()
        {
            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => !line.muteButton.isAutoOn))
            {
                line.muteButton.isOn = true;
                line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
            }
        }

        public static void UnmuteAll()
        {
            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines.Where(line => line.muteButton.isAutoOn))
            {
                line.muteButton.isOn = false;
                line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
            }
        }

        public static void ReportGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                NetPlayer player = GetPlayerFromVRRig(gunTarget);

                GorillaPlayerScoreboardLine.ReportPlayer(player.UserId, GorillaPlayerLineButton.ButtonType.Cheating, player.NickName);
                muteDelay = Time.time + 0.2f;
            }, new GunLibrary.GunSettings { when = () => Time.time > muteDelay });
        }

        public static void ReportAll()
        {
            foreach (NetPlayer player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                GorillaPlayerScoreboardLine.ReportPlayer(player.UserId, GorillaPlayerLineButton.ButtonType.Cheating, player.NickName);
        }
        public static void TriggerAntiReportGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    try
                    {
                        foreach (var report in from line in GorillaScoreboardTotalUpdater.allScoreboardLines where line.linePlayer == lockTarget.GetPlayer() && Vector3.Distance(line.reportButton.transform.position, References.BodyCollider.transform.position) < 50f select line.reportButton.gameObject.transform)
                        {
                            References.LocalRig.transform.position = report.transform.position;
                            References.LocalRig.leftHand.rigTarget.transform.position = report.transform.position;
                            References.LocalRig.rightHand.rigTarget.transform.position = report.transform.position;
                        }
                    }
                    catch { }

                    if (Time.time > pressButtonDelay)
                    {
                        pressButtonDelay = Time.time + 0.1f;
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 67, false, 999999f);
                        Security.RPCProtection();
                    }
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
                        References.LocalRig.enabled = true;
                        gunLocked = false;
                    }
                } });
        }

        public static void TriggerAntiReportAll()
        {
            References.LocalRig.enabled = false;

            try
            {
                Player triggerAntiReportTarget = GetRandomPlayer(false);
                foreach (var report in from line in GorillaScoreboardTotalUpdater.allScoreboardLines where NetPlayerToPlayer(line.linePlayer) == triggerAntiReportTarget && Vector3.Distance(line.reportButton.transform.position, References.BodyCollider.transform.position) < 50f select line.reportButton.gameObject.transform)
                {
                    References.LocalRig.transform.position = report.transform.position;
                    References.LocalRig.leftHand.rigTarget.transform.position = report.transform.position;
                    References.LocalRig.rightHand.rigTarget.transform.position = report.transform.position;
                }
            }
            catch { }

            if (Time.time > pressButtonDelay)
            {
                pressButtonDelay = Time.time + 0.1f;
                References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 67, false, 999999f);
                Security.RPCProtection();
            }
        }

        public static void BypassAntiReport()
        {
            SerializePatch.OverrideSerialization = () =>
            {

                bool isNearReportButton = false;
                List<int> people = new List<int> { };
                try
                {
                    foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                    {
                        Transform report = line.reportButton.gameObject.transform;
                        float D1 = Vector3.Distance(References.RightHand.position, report.position);
                        float D2 = Vector3.Distance(References.LeftHand.position, report.position);

                        if (D1 < 0.5f || D2 < 0.5f)
                        {
                            people.Add(line.linePlayer.ActorNumber);
                            isNearReportButton = true;
                        }
                    }
                }
                catch { }

                if (References.MyRigView != null && isNearReportButton)
                {
                    MassSerialize(true, new PhotonView[] { References.LocalRig.GetPhotonView() });

                    Vector3 positionArchiveLeft = References.LocalRig.leftHand.rigTarget.transform.position;
                    Vector3 positionArchiveRight = References.LocalRig.rightHand.rigTarget.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions() { TargetActors = PhotonNetwork.PlayerListOthers.Where(player => !people.ToArray().Contains(player.ActorNumber)).Select(player => player.ActorNumber).ToArray() });
                    References.LocalRig.leftHand.rigTarget.transform.position = References.HeadCollider.transform.position - (References.HeadCollider.transform.forward * 100f);
                    References.LocalRig.rightHand.rigTarget.transform.position = References.HeadCollider.transform.position - (References.HeadCollider.transform.forward * 100f);

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions() { TargetActors = people.ToArray() });

                    References.LocalRig.leftHand.rigTarget.transform.position = positionArchiveLeft;
                    References.LocalRig.rightHand.rigTarget.transform.position = positionArchiveRight;

                    Security.RPCProtection();

                    return false;
                }

                return true;
            };
        }

        public static void BreakModCheckers()
        {
            Hashtable props = new Hashtable();
            foreach (string mod in Visuals.modDictionary.Keys)
                props[mod] = true;

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public static void ReloadModsToSpoof(string key, string value, bool add = true)
        {
            switch (add)
            {
                case true:
                    modsToSpoof.Add(key, value);
                    break;
                case false:
                    modsToSpoof.Remove(key);
                    break;
            }
            Hashtable props = new Hashtable();

            foreach (string mod in modsToSpoof.Keys)
                props[mod] = true;

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        public static void CustomModSpoofer()
        {
            Prompt("Would you like to choose from a mod list or type the mod property?", () =>
            {
                List<Module> modList = new List<Module> { new Module { buttonText = "Exit Mod List", method = () => Manager.CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page." } };
                modList.AddRange(Visuals.modDictionary.Select((t, i) => Visuals.modDictionary.ElementAt(i))
                .Select((mod, i) => new Module
                {
                    buttonText = $"Mod{i}",
                    overlapText = mod.Value,
                    enableMethod = () => ReloadModsToSpoof(mod.Key, mod.Value),
                    disableMethod = () => ReloadModsToSpoof(mod.Key, mod.Value, false),
                    toolTip = $"Show that you are using the mod {mod.Value} to other players."
                }));


                Manager.modules["Mod List"] = modList.ToArray();
                Manager.CurrentCategoryName = "Mod List";
            }, () =>
            {
                PromptSingleText("Please enter what you would like to spoof your mods to (seperated by commas).", () =>
                {
                    string[] input = keyboardInput.Split(',').Select(s => s.Trim()).ToArray();
                    Hashtable props = new Hashtable();

                    foreach (string mod in input)
                    {
                        string foundKey = null;

                        foreach (var kv in Visuals.modDictionary)
                        {
                            if (string.Equals(kv.Value, mod, StringComparison.OrdinalIgnoreCase))
                            {
                                foundKey = kv.Key;
                                break;
                            }
                        }

                        props[foundKey ?? mod] = true;
                    }

                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                }, "Done");
            }, "Mod List", "Type");
        }

        public static void MuteDJSets()
        {
            foreach (RadioButtonGroupWearable djSet in GetAllType<RadioButtonGroupWearable>())
            {
                if (djSet.enabled)
                    djSet.enabled = false;
            }
        }

        public static void UnmuteDJSets()
        {
            foreach (RadioButtonGroupWearable djSet in GetAllType<RadioButtonGroupWearable>())
            {
                if (!djSet.enabled)
                    djSet.enabled = true;
            }
        }
        public static void TapAllClass<T>() where T : Tappable
        {
            if (rightGrab)
            {
                if (Time.time > tapDelay)
                {
                    foreach (Tappable TappableObject in GetAllType<T>())
                        TappableObject.OnTap(1f);

                    Security.RPCProtection();
                    tapDelay = Time.time + 0.1f;
                }
            }
        }

        public static void TriggerLeafPileGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : -10f));
                    SendSerialize(References.LocalRig.GetPhotonView());

                    References.LocalRig.transform.position = NewPointer.transform.position + (Vector3.up * (Time.frameCount % 2 == 1 ? 10f : 0f));
                }
                else
                    References.LocalRig.enabled = true;
            });
        }
        public static void ActivateAllDoors()
        {
            if (rightGrab)
            {
                if (Time.time > buttonDelay)
                {
                    foreach (GhostLabButton button in GetAllType<GhostLabButton>())
                    {
                        button.ButtonActivation();
                        Security.RPCProtection();
                    }
                    buttonDelay = Time.time + 0.1f;
                }
            }
        }
        public static void AutoHitMoleType(bool isHazard)
        {
            foreach (Mole mole in GetAllType<Mole>())
            {
                int state = mole.randomMolePickedIndex;
                if (mole.CanTap() && mole.moleTypes[state].isHazard == isHazard && Time.time > hitDelay)
                {
                    hitDelay = Time.time + 0.1f;

                    mole.OnTap(1f);
                    Security.RPCProtection();
                }
            }
        }
        public static void SpazMoleMachines()
        {
            if (Time.time > moleMachineDelay)
            {
                moleMachineDelay = Time.time + 0.25f;
                foreach (WhackAMole moleMachine in GetAllType<WhackAMole>())
                {
                    moleMachine.GetView.RPC("WhackAMoleButtonPressed", RpcTarget.All);
                    Security.RPCProtection();
                }
            }
        }

        public static void AutoStartMoles()
        {
            if (Time.time > moleMachineDelay)
            {
                moleMachineDelay = Time.time + 0.1f;
                foreach (WhackAMole moleMachine in GetAllType<WhackAMole>())
                {
                    if (moleMachine.currentState == WhackAMole.GameState.Off || moleMachine.currentState == WhackAMole.GameState.TimesUp)
                    {
                        moleMachine.GetView.RPC("WhackAMoleButtonPressed", RpcTarget.All);
                        Security.RPCProtection();
                    }
                }
            }
        }

        public static void SetBraceletState(bool enable, bool isLeftHand) =>
            References.MyRigView.SendRPC("EnableNonCosmeticHandItemRPC", RpcTarget.All, enable, isLeftHand);

        public static void GetBracelet(bool state)
        {
            if (leftGrab)
            {
                SetBraceletState(false, false);
                SetBraceletState(state, true);
            }

            if (rightGrab)
            {
                SetBraceletState(state, false);
                SetBraceletState(false, true);
            }

            if (leftGrab || rightGrab)
                Security.RPCProtection();
        }
        public static void BraceletSpam()
        {
            if (Time.time > braceletSpamDelay)
            {
                GetBracelet(Time.frameCount % 2 == 0);
                braceletSpamDelay = Time.time + 0.1f;

                previousBraceletSpamState = !previousBraceletSpamState;
            }
        }

        public static void RemoveBracelet()
        {
            SetBraceletState(false, true);
            SetBraceletState(false, false);
            Security.RPCProtection();
        }
        public static void RainbowBracelet()
        {
            BraceletPatch.enabled = true;
            if (!References.LocalRig.nonCosmeticRightHandItem.IsEnabled)
            {
                SetBraceletState(true, false);
                Security.RPCProtection();

                References.LocalRig.nonCosmeticRightHandItem.EnableItem(true);
            }
            List<Color> rgbColors = new List<Color>();
            for (int i = 0; i < 10; i++)
                rgbColors.Add(Color.HSVToRGB((Time.frameCount / 180f + i / 10f) % 1f, 1f, 1f));

            References.LocalRig.reliableState.isBraceletLeftHanded = false;
            References.LocalRig.reliableState.braceletSelfIndex = 99;
            References.LocalRig.reliableState.braceletBeadColors = rgbColors;
            References.LocalRig.friendshipBraceletRightHand.UpdateBeads(rgbColors, 99);

            if (Time.time > isDirtyDelay)
            {
                isDirtyDelay = Time.time + 0.1f;
                References.LocalRig.reliableState.SetIsDirty();
            }
        }

        public static void RemoveRainbowBracelet()
        {
            BraceletPatch.enabled = false;
            if (!References.LocalRig.nonCosmeticRightHandItem.IsEnabled)
            {
                SetBraceletState(false, false);
                Security.RPCProtection();

                References.LocalRig.nonCosmeticRightHandItem.EnableItem(false);
            }

            References.LocalRig.reliableState.isBraceletLeftHanded = false;
            References.LocalRig.reliableState.braceletSelfIndex = 0;
            References.LocalRig.reliableState.braceletBeadColors.Clear();
            References.LocalRig.UpdateFriendshipBracelet();

            References.LocalRig.reliableState.SetIsDirty();
        }
        public static void QuestNoises()
        {
            if (!Networking.InRoom) return;
            if (rightTrigger > 0.5f && Time.time > lastTimeDingied)
            {
                lastTimeDingied = Time.time + References.LocalRig.fxSettings.GetDelay(10);
                RoomSystem.SendMonkePointsRedeemed(50);
            }
        }
        public static void MaxQuestScore()
        {
            if (Time.time > delaybetweenscore)
            {
                delaybetweenscore = Time.time + 1f;
                References.LocalRig.SetQuestScore(int.MaxValue);
            }
        }
        public static void CustomQuestScore()
        {
            if (Time.time > delaybetweenscore)
            {
                delaybetweenscore = Time.time + 1f;
                References.LocalRig.SetQuestScore(targetQuestScore);
            }
        }
        public static void ArcadeTeleporterEffectSpam()
        {
            if (!Networking.InRoom) return;
            if (Time.time > spamDelay)
            {
                spamDelay = Time.time + 0.1f;
                returnOrTeleport = !returnOrTeleport;

                GetObject("City_Pretty/CosmeticsScoreboardAnchor/Arcade_prefab/MainRoom/VRArea/ModIOArcadeTeleporter/NetObject_VRTeleporter").GetComponent<PhotonView>().RPC("ActivateTeleportVFX", RpcTarget.All, returnOrTeleport, (short)Random.Range(0, 7));
                Security.RPCProtection();
            }
        }

        public static void StumpTeleporterEffectSpam()
        {
            if (!Networking.InRoom) return;
            if (Time.time > spamDelay)
            {
                spamDelay = Time.time + 0.1f;
                returnOrTeleport = !returnOrTeleport;

                GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/StumpVRHeadset/VirtualStump_StumpTeleporter/NetObject_VRTeleporter").GetComponent<PhotonView>().RPC("ActivateTeleportVFX", RpcTarget.All, returnOrTeleport, (short)0);
                Security.RPCProtection();
            }
        }

        public static void SetBasementDoorState(bool open)
        {
            if (!Networking.InRoom) return;
            if (Time.time > spamDelay)
            {
                spamDelay = Time.time + 0.1f;

                GetObject("Environment Objects/LocalObjects_Prefab/CityToBasement/DungeonEntrance/DungeonDoor_Prefab").GetComponent<PhotonView>().RPC("ChangeDoorState", RpcTarget.AllViaServer, open ? GTDoor.DoorState.Opening : GTDoor.DoorState.Closing);
                Security.RPCProtection();
            }
        }

        public static void SetElevatorDoorState(bool open)
        {
            if (!Networking.InRoom) return;
            if (Time.time > spamDelay)
            {
                spamDelay = Time.time + 0.1f;

                GRElevatorManager.ElevatorButtonPressed(open ? GRElevator.ButtonType.Open : GRElevator.ButtonType.Close, ManagerRegistry.GRElevatorManager.currentLocation);
                Security.RPCProtection();
            }
        }
        public static void BasementDoorSpam()
        {
            if (!Networking.InRoom) return;
            if (Time.time > spamDelay)
            {
                spamDelay = Time.time + 0.05f;
                openOrClose = !openOrClose;
                PhotonView view = GetObject("Environment Objects/LocalObjects_Prefab/CityToBasement/DungeonEntrance/DungeonDoor_Prefab").GetComponent<PhotonView>();

                view.RPC("ChangeDoorState", RpcTarget.AllViaServer, openOrClose ? GTDoor.DoorState.Opening : GTDoor.DoorState.Closing);
                view.RPC("ChangeDoorState", RpcTarget.AllViaServer, GTDoor.DoorState.HeldOpen);
                Security.RPCProtection();
            }
        }

        public static void ElevatorDoorSpam()
        {
            if (Time.time > spamDelay)
            {
                delay = Time.time + 0.1f;
                openOrClose = !openOrClose;

                GRElevatorManager.ElevatorButtonPressed(openOrClose ? GRElevator.ButtonType.Open : GRElevator.ButtonType.Close, ManagerRegistry.GRElevatorManager.currentLocation);
                Security.RPCProtection();
            }
        }
        public static void CustomVirtualStumpVideo() =>
            virtualStumpAd ??= new GameObject("Qubit_VirtualStumpAd").AddComponent<VirtualStumpAd>();

        public static void DisableCustomVirtualStumpVideo()
        {
            virtualStumpAd.enabled = false;
            Object.Destroy(virtualStumpAd.gameObject);
        }

        public static void ApplyCustomQuestScore(int index) => targetQuestScore = index;


        public static void GrabIDCard()
        {
            if (rightGrab)
            {
                foreach (var entity in ManagerRegistry.GhostReactor.Instance.employeeBadges.registeredBadges.Select(grBadge => grBadge.gameEntity).Where(entity => entity.onlyGrabActorNumber == PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = entity.transform.position;

                    ManagerRegistry.GhostReactor.GameEntityManager.RequestGrabEntity(entity.id, false, Vector3.zero, Quaternion.identity);
                }
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void SetPropDistanceLimit(float distance)
        {
            if (Networking.InRoom && ManagerRegistry.GorillaGameManager.GameType() == GameModeType.PropHunt)
            {
                GameModes.SetPropHuntHandDistance(distance);
            }
        }
        public static void PurchaseAllToolStations()
        {
            if (Time.time > purchaseDelay)
            {
                ManagerRegistry.GhostReactor.GhostReactorManager.ToolPurchaseStationRequest(Random.Range(0, ManagerRegistry.GhostReactor.GhostReactorManager.reactor.toolPurchasingStations.Count - 1), GhostReactorManager.ToolPurchaseStationAction.TryPurchase);
                purchaseDelay = Time.time + 0.1f;
            }
        }

        public static void RemoveCurrencySelf()
        {
            if (!Networking.IsMasterClient) { return; }
            GRPlayer.Get(PhotonNetwork.LocalPlayer.ActorNumber).shiftCreditCache = 0;
        }

        public static void RemoveCurrencyGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (PhotonNetwork.IsMasterClient)
                    GRPlayer.Get(GetPlayerFromVRRig(gunTarget).ActorNumber).shiftCreditCache = 0;
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            });
        }

        public static void RemoveCurrencyAll()
        {
            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

            foreach (Player target in PhotonNetwork.PlayerList)
            {
                GRPlayer plr = GRPlayer.Get(target.ActorNumber);
                plr.shiftCreditCache = 0;
            }
        }

        public static void SetPlayerState(Player Target, GRPlayer.GRPlayerState State)
        {
            GRPlayer GRPlayer = GRPlayer.Get(Target.ActorNumber);

            if (GRPlayer.State == State)
                return;

            if ((Target == PhotonNetwork.LocalPlayer && State == GRPlayer.GRPlayerState.Ghost)
                    || (Networking.IsMasterClient && State == GRPlayer.GRPlayerState.Alive)
                    )
            {
                ManagerRegistry.GhostReactor.GhostReactorManager.RequestPlayerStateChange(GRPlayer, State);
                Security.RPCProtection();
                return;
            }

            if (!Networking.IsMasterClient) { Notifications.SendFormat("red", "ERROR", "You are not master client."); return; }

            if (State == GRPlayer.GRPlayerState.Ghost)
                Coroutines.instance.StartCoroutine(KillTarget(Target));
        }

        public static void SetPlayerState(NetPlayer Target, GRPlayer.GRPlayerState State) =>
            SetPlayerState(NetPlayerToPlayer(Target), State);

        public static void SetPlayerState(VRRig Target, GRPlayer.GRPlayerState State) =>
            SetPlayerState(NetPlayerToPlayer(GetPlayerFromVRRig(Target)), State);

        public static IEnumerator KillTarget(Player Target)
        {
            GRPlayer GRPlayer = GRPlayer.Get(Target.ActorNumber);
            VRRig Rig = GetVRRigFromPlayer(Target);

            int netId = ManagerRegistry.GhostReactor.GameEntityManager.CreateTypeNetId(Fun.ObjectByName["GhostReactorEnemyChaserArmored"]);

            ManagerRegistry.GhostReactor.GameEntityManager.photonView.RPC("CreateItemRPC", Target, new[] { netId }, new[] { (int)ManagerRegistry.GhostReactor.GameEntityManager.zone }, new[] { Fun.ObjectByName["GhostReactorEnemyChaserArmored"] }, new[] { BitPackUtils.PackWorldPosForNetwork(Rig.transform.position) }, new[] { BitPackUtils.PackQuaternionForNetwork(Rig.transform.rotation) }, new[] { 0L }, new[] { 0 });

            ManagerRegistry.GhostReactor.GhostReactorManager.gameAgentManager.photonView.RPC("ApplyBehaviorRPC", Target, new[] { netId }, new byte[] { 6 });

            GRPlayer.ChangePlayerState(GRPlayer.GRPlayerState.Ghost, ManagerRegistry.GhostReactor.GhostReactorManager);

            Security.RPCProtection();

            yield return null;
            yield return null;
            yield return null;

            ManagerRegistry.GhostReactor.GameEntityManager.photonView.RPC("DestroyItemRPC", Target, new[] { netId });

            Security.RPCProtection();
        }

        public static void SpazToolStations()
        {
            if (Time.time > purchaseDelay)
            {
                ManagerRegistry.GhostReactor.GhostReactorManager.ToolPurchaseStationRequest(Random.Range(0, ManagerRegistry.GhostReactor.GhostReactorManager.reactor.toolPurchasingStations.Count - 1), (GhostReactorManager.ToolPurchaseStationAction)Random.Range(0, 2));
                purchaseDelay = Time.time + 0.1f;
            }
        }

        public static void SetMicrophoneQuality(int bitrate, int samplingRate)
        {

            if (!Networking.InRoom)
                return;

            if (!ManagerRegistry.NetworkSystem.LocalRecorder)
                return;

            Recorder mic = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;

            if (mic.SamplingRate == (SamplingRate)samplingRate && mic.Bitrate == bitrate)
                return;

            mic.SamplingRate = (SamplingRate)samplingRate;
            mic.Bitrate = bitrate;

            if (RecorderPatch.enabled)
                Voices.Get().SamplingRate = samplingRate;


            Coroutines.instance.StartCoroutine(DelayReloadMicrophone());
        }

        public static void SetMicrophoneAmplification(float gain)
        {
            if (!Networking.InRoom)
                return;

            if (RecorderPatch.enabled)
                Voices.Get().Gain = gain;
            else
            {
                Recorder mic = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;

                if (gain > 1)
                {
                    if (mic.gameObject.GetComponent<MicAmplifier>() != null)
                        return;

                    MicAmplifier microphoneAmplifier = mic.gameObject.GetOrAddComponent<MicAmplifier>();
                    microphoneAmplifier.AmplificationFactor = 16;
                    microphoneAmplifier.BoostValue = 16;
                }
                else
                {
                    if (mic.gameObject.GetComponent<MicAmplifier>())
                    {
                        if (mic.gameObject.GetComponent<MicAmplifier>() == null)
                            return;

                        MicAmplifier microphoneAmplifier = mic.gameObject.GetComponent<MicAmplifier>();
                        microphoneAmplifier.enabled = false;
                        Object.Destroy(mic.gameObject.GetComponent<MicAmplifier>());
                    }
                }

                Coroutines.instance.StartCoroutine(DelayReloadMicrophone());
            }

        }

        public static void EchoMicrophone(bool status)
        {
            Module button = Manager.GetIndex("Legacy Microphone");
            if (button.enabled)
            {
                Notifications.SendFormat("red", "ERROR", "You are using Legacy Microphone. This mod does not support using the old microphone system.");
                Manager.GetIndex("Echo Microphone").SetEnabled(false);
                return;
            }

            if (status)
            {
                int sampleRate = Voices.Get().SamplingRate;
                int samples = Mathf.Max(1, sampleRate / 4);
                float[] delayedBuffer = new float[samples];
                int index = 0;

                if (!Voices.Get().PostProcessors.ContainsKey("Echo"))
                {
                    Voices.Get().PostProcessors["Echo"] = buffer =>
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            float delayed = delayedBuffer[index];
                            float raw = buffer[i];
                            float mix = raw + delayed * 0.5f;

                            buffer[i] = Mathf.Clamp(mix, -1f, 1f);

                            delayedBuffer[index] = mix;
                            index = (index + 1) % samples;
                        }
                    };
                }
            }
            else
            {
                Voices.Get().PostProcessors.Remove("Echo");
            }
        }

        public static void GlitchyMicrophone(bool status)
        {
            Module button = Manager.GetIndex("Legacy Microphone");
            if (button.enabled)
            {
                Notifications.SendFormat("red", "ERROR", "You are using Legacy Microphone. This mod does not support using the old microphone system.");
                Manager.GetIndex("Glitchy Microphone").SetEnabled(false);
                return;
            }

            if (status)
            {
                int rate = Voices.Get().SamplingRate;
                int repeatLength = Mathf.Max(1, rate / 2);
                float[] history = new float[repeatLength];
                int historyIndex = 0;

                float[] repeatBuffer = new float[repeatLength];
                int repeatIndex = 0;
                int repeatsLeft = 0;

                int samplesUntilNext = Random.Range(rate * 1, rate * 4);

                Voices.Get().PostProcessClip = true;
                if (!Voices.Get().PostProcessors.ContainsKey("Glitch"))
                {
                    Voices.Get().PostProcessors["Glitch"] = buffer =>
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            history[historyIndex] = buffer[i];
                            historyIndex = (historyIndex + 1) % repeatLength;
                            if (repeatsLeft > 0)
                            {
                                buffer[i] = repeatBuffer[repeatIndex];
                                repeatIndex++;
                                if (repeatIndex >= repeatLength)
                                {
                                    repeatIndex = 0;
                                    repeatsLeft--;
                                }
                            }
                            else
                            {
                                samplesUntilNext--;
                                if (samplesUntilNext <= 0)
                                {
                                    for (int j = 0; j < repeatLength; j++)
                                    {
                                        int index = (historyIndex + j) % repeatLength;
                                        repeatBuffer[j] = history[index];
                                    }
                                    repeatsLeft = Random.Range(1, 2);
                                    repeatIndex = 0;
                                    samplesUntilNext = Random.Range(rate * 1, rate * 4);
                                }
                            }
                        }
                    };
                }
            }
            else
            {
                Voices.Get().PostProcessors.Remove("Glitch");
            }
        }

        public static void LaggyMicrophone(bool status)
        {
            Module button = Manager.GetIndex("Legacy Microphone");
            if (button.enabled)
            {
                Notifications.SendFormat("red", "ERROR", "You are using Legacy Microphone. This mod does not support using the old microphone system.");
                button.enabled = false;
                return;
            }

            if (status)
            {
                if (!Voices.Get().PostProcessors.ContainsKey("Lag"))
                {
                    Voices.Get().PostProcessors["Lag"] = buffer =>
                    {
                        if (Random.value < 0.25f)
                            Array.Clear(buffer, 0, buffer.Length);
                    };
                }
            }
            else
                Voices.Get().PostProcessors.Remove("Lag");
        }

        public static void MuteMicrophone(bool mute)
        {
            if (RecorderPatch.enabled)
                Voices.Get().MuteMicrophone = mute;
            else
            {
                if (!Networking.InRoom)
                    return;
                Recorder mic = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;
                if (mic.IsRecording != mute)
                    return;
                mic.IsRecording = !mute;
            }
        }

        public static void SetMicrophonePitch(float pitch)
        {
            if (!Networking.InRoom)
                return;

            if (RecorderPatch.enabled)
                Voices.Get().Pitch = pitch;
            else
            {
                Recorder mic = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;

                if (!Mathf.Approximately(pitch, 1f))
                {
                    MicPitchShifter pitchShifter = mic.gameObject.GetComponent<MicPitchShifter>();
                    if (pitchShifter != null && Mathf.Approximately(pitchShifter.PitchFactor, pitch))
                        return;

                    MicPitchShifter microphoneAmplifier = mic.gameObject.GetOrAddComponent<MicPitchShifter>();
                    microphoneAmplifier.PitchFactor = pitch;
                }
                else
                {
                    if (mic.gameObject.GetComponent<MicPitchShifter>())
                    {
                        MicPitchShifter microphoneAmplifier = mic.gameObject.GetComponent<MicPitchShifter>();
                        microphoneAmplifier.enabled = false;
                        Object.Destroy(mic.gameObject.GetComponent<MicPitchShifter>());
                    }
                    else
                        return;
                }

                Coroutines.instance.StartCoroutine(DelayReloadMicrophone());
            }

        }

        public static void SetDebugEchoMode(bool value)
        {
            if (!Networking.InRoom)
                return;
            if (ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder != null && ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.IsRecording)
                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = value;
        }

        public static void CopyVoiceGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        if (RecorderPatch.enabled)
                        {
                            SpeakerPatch.enabled = true;
                            SpeakerPatch.targetSpeaker = lockTarget.gameObject.GetComponent<GorillaSpeakerLoudness>().speaker;
                            if (!Voices.Get().PostProcessors.ContainsKey("CopyVoice"))  // this is so shit but i need to account for channels > 1
                            {
                                float readPos = 0f;

                                Voices.Get().PostProcessors["CopyVoice"] = buffer =>
                                {
                                    var vm = Voices.Get();
                                    int channels = vm.Channels;

                                    float sourceRate = (SpeakerPatch.targetSpeaker != null && SpeakerPatch.targetSpeaker.RemoteVoiceLink.Info.SamplingRate > 0)
                                                       ? SpeakerPatch.targetSpeaker.RemoteVoiceLink.Info.SamplingRate
                                                       : 24000;

                                    float ratio = sourceRate / vm.OutputRate;

                                    lock (SpeakerPatch.locked)
                                    {
                                        if (SpeakerPatch.SampleQueue.Count < (sourceRate * 0.04f))
                                        {
                                            Array.Clear(buffer, 0, buffer.Length);
                                            return;
                                        }

                                        for (int i = 0; i < buffer.Length; i += channels)
                                        {
                                            int idxA = (int)readPos;
                                            int idxB = idxA + 1;

                                            if (idxB < SpeakerPatch.SampleQueue.Count)
                                            {
                                                float t = readPos - idxA;
                                                float sample = Mathf.Lerp(SpeakerPatch.SampleQueue[idxA], SpeakerPatch.SampleQueue[idxB], t);

                                                for (int c = 0; c < channels; c++)
                                                {
                                                    int targetIndex = i + c;
                                                    if (targetIndex < buffer.Length)
                                                    {
                                                        buffer[targetIndex] = sample;
                                                    }
                                                }

                                                readPos += ratio;
                                            }
                                            else
                                            {
                                                for (int c = 0; c < channels; c++)
                                                {
                                                    if (i + c < buffer.Length) buffer[i + c] = 0f;
                                                }
                                            }
                                        }

                                        int consumed = (int)readPos;
                                        if (consumed > 0)
                                        {
                                            SpeakerPatch.SampleQueue.RemoveRange(0, Math.Min(consumed, SpeakerPatch.SampleQueue.Count));
                                            readPos -= consumed;
                                        }
                                    }
                                };
                            }
                        }
                        else
                        {
                            if (Time.time > copyVoiceGunDelay)
                            {
                                copyVoiceGunDelay = Time.time + 0.5f;

                                gunLocked = true;
                                lockTarget = gunTarget;

                                SpeakerPatch.enabled = true;

                                SpeakerPatch.targetSpeaker = lockTarget.gameObject.GetComponent<GorillaSpeakerLoudness>().speaker;

                                RecorderPatch.enabled = !Manager.GetIndex("Legacy Microphone").enabled;

                                Voices.Get().PostProcessors["CopyVoice"] = null;

                                factory?.Dispose();

                                factory = new LoopbackFactory();

                                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.Factory;
                                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.InputFactory = () =>
                                {
                                    return factory;
                                };
                                Coroutines.instance.StartCoroutine(DelayReloadMicrophone());
                            }
                        }

                        ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = true;
                    }
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
                        gunLocked = false;
                    if (factory != null || Voices.Get().PostProcessors.ContainsKey("CopyVoice"))
                        DisableCopyVoice();
                } });
        }

        public static void DisableCopyVoice()
        {
            factory?.Dispose();

            Voices.Get().PostProcessors.Remove("CopyVoice");

            SpeakerPatch.enabled = false;

            Sound.FixMicrophone();

            RecorderPatch.enabled = !Manager.GetIndex("Legacy Microphone").enabled;

            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = false;
        }

        public static void SaveNarration(string text)
        {
            string path = $"{PluginInfo.BaseDirectory}/Sounds/Narrations";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            Coroutines.instance.StartCoroutine(TranscribeText(text, (audio) =>
            {
                PromptSingleText("The narration has been saved in your Soundboard!");
            }, text, path));
        }
        public static void MaskVoice()
        {
            Module mod = Manager.GetIndex("Mask Voice");

            if (Application.platform == RuntimePlatform.WindowsPlayer && Environment.OSVersion.Version.Major < 10)
                PromptSingle("Your version of Windows is too old for this mod to run.", () => mod.SetEnabled(false));
            else if (Application.platform != RuntimePlatform.WindowsPlayer)
                PromptSingle("You must be on Windows 10 or greater for this mod to run.", () => mod.SetEnabled(false));

            drec = new DictationRecognizer();
            drec.DictationResult += (text, confidence) =>
            {
                if (Settings.debugDictation)
                    Logs.Log($"Dictation result: {text}");
                Notifications.SendFormat("green", "VOICE", $"{text}");

                if (ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder != null)
                {
                    if (Networking.InRoom)
                        SpeakText(text, true);
                    else
                        NarrateText(text);
                }
                else
                    NarrateText(text);
            };
            drec.DictationComplete += (completionCause) =>
            {
                drec.Start();
            };
            drec.DictationError += (error, hresult) =>
            {
                if (Settings.debugDictation)
                    Logs.LogError($"Dictation error: {error}; HResult = {hresult}.");
                if (error.Contains("Dictation support is not enabled on this device"))
                {
                    DisableMaskVoice();
                    Notifications.SendFormat("red", "ERROR", $"Online Speech Recognition is not enabled on this device. Either open the menu to enable it, or check your internet connection.", 3000);
                    Prompt("Online Speech Recognition is not enabled on your device. Would you like to open the Settings page to enable it?", () => { Files.Start("ms-settings:privacy-speech"); PromptSingle("Once you enable Online Speech Recognition, turn this mod back on!", () => mod.SetEnabled(false)); }, () => PromptSingle("You will not be able to use this mod until you enable Online Speech Recognition.", () => mod.SetEnabled(false)));
                }
            };
            drec.DictationHypothesis += (text) =>
            {
                if (Settings.debugDictation)
                    Logs.Log($"Hypothesis: {text}");
                Notifications.Clear();
                Notifications.SendFormat("green", "VOICE", $"{text}");
            };
            drec.Start();
        }

        public static void DisableMaskVoice()
        {
            drec?.Stop();
            drec?.Dispose();
        }

        public static void ProcessFrameBuffer(float[] data) =>
            factory.Feed(data);

        public static void ReloadMicrophone()
        {
            Recorder recorder = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;
            if (!RecorderPatch.enabled && recorder.SourceType != Recorder.InputSourceType.Microphone)
                recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.RestartRecording(true);
            if (RecorderPatch.enabled)
                Voices.Get().RestartMicrophone();
        }

        public static IEnumerator DelayReloadMicrophone()
        {
            yield return new WaitForSeconds(0.25f);
            ReloadMicrophone();
        }

        public static void ProjectileRange()
        {
            LoopingArray<ProjectileTracker.ProjectileInfo> projectileArray = ProjectileTracker.m_localProjectiles;
            if (projectileArray == null || projectileArray.Length <= 0) return;
            for (int index = 0; index < projectileArray.Length; index++)
            {
                SlingshotProjectile projectileInstance = projectileArray[index].projectileInstance;
                if (projectileInstance == null || !projectileInstance.gameObject.activeSelf) continue;

                foreach (var rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()).Where(rig => rig.Distance(projectileInstance.transform.position) < 0.5f))
                    projectileInstance.transform.position = rig.headMesh.transform.position;
            }
        }

        public static void StartAllRaces()
        {
            foreach (RacingManager.Race race in ManagerRegistry.RacingManager.races)
            {
                if (race.racingState == RacingManager.RacingState.Inactive)
                    race.Button_StartRace(5);
            }
        }

        public static void RopeGrabReach()
        {
            foreach (GorillaHandClimber climber in new[] { ManagerRegistry.EquipmentInteractor.LeftClimber, ManagerRegistry.EquipmentInteractor.rightClimber })
                (climber.col as SphereCollider).radius = 0.5f;
        }
        public static void AngryBirdsSounds()
        {
            if (!dynamicSounds) return;
            Slingshot slingshot = References.LocalRig.GetSlingshot() as Slingshot;

            if (!slingshot) return;

            if (slingshot.InDrawingState() && !lastDrawing)
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/AngryBirds/drawing.ogg", "Audio/Mods/Fun/AngryBirds/drawing.ogg", clip => clip.Play(buttonClickVolume / 10f));

            lastDrawing = slingshot.InDrawingState();
        }

        public static void SlingshotSelf(bool active = true)
        {
            try
            {
                References.LocalRig.SetActiveTransferrableObjectIndex(0, active ? References.LocalRig.GetSlingshot().objectIndex : -1);
                References.LocalRig.myBodyDockPositions.RefreshTransferrableItems();
            }
            catch { }
        }
        public static IEnumerator ReturnRig()
        {
            yield return new WaitForSeconds(0.2f);
            References.LocalRig.enabled = true;
            BugCoroutine = null;
        }
        public static void EverythingSpam()
        {
            ThrowableBug bugObject = GetBugObject("Floating Bug Holdable");

            if ((!bugObject.IsMyItem() || (bugObject.currentState != TransferrableObject.PositionState.Dropped && bugObject.currentState != TransferrableObject.PositionState.None)) && bugObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(bugObject.GetComponent<ClampPosition>());

            ThrowableBug fireflyObject = GetBugObject("Firefly");

            if ((!fireflyObject.IsMyItem() || (fireflyObject.currentState != TransferrableObject.PositionState.Dropped && fireflyObject.currentState != TransferrableObject.PositionState.None)) && fireflyObject.GetComponent<ClampPosition>() != null)
                Object.Destroy(fireflyObject.GetComponent<ClampPosition>());

            ThrowableBug bug = GetBug("Floating Bug Holdable");
            ThrowableBug firefly = bug != null ? GetBug("Firefly") : bug;

            if (rightGrab && Time.time > everythingSpamDelay)
            {

                everythingSpamDelay = Time.time + 0.0625f;

                objectIndex++;
                objectIndex %= 8;

                switch (objectIndex)
                {
                    case 0:
                        {
                            ThrowableBug targetBug = bug;
                            GameObject bugSpamObject = new GameObject("Qubit_BugSpamObject");
                            bugSpamObject.transform.localScale = Vector3.one * 0.2f;
                            bugSpamObject.layer = 3;

                            if (Manager.GetIndex("Bug Colliders").enabled)
                            {
                                SphereCollider collider = bugSpamObject.AddComponent<SphereCollider>();

                                if (Manager.GetIndex("Bouncy Bug").enabled)
                                {
                                    collider.material.bounciness = 1f;
                                    collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                                    collider.material.dynamicFriction = 0f;
                                }
                            }

                            bugSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                            bugSpamObject.transform.rotation = References.RightHand.rotation;

                            Rigidbody rigidbody = bugSpamObject.AddComponent<Rigidbody>();
                            rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                            rigidbody.angularVelocity = RandomVector3(100f);

                            rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                            targetBug.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = bugSpamObject.transform;

                            bugSpamObject.AddComponent<DestroyOnRest>();
                            Object.Destroy(bugSpamObject, 30f);
                            break;
                        }
                    case 1:
                        {
                            ThrowableBug targetBug = firefly;
                            GameObject bugSpamObject = new GameObject("Qubit_FireflySpamObject");
                            bugSpamObject.transform.localScale = Vector3.one * 0.2f;
                            bugSpamObject.layer = 3;

                            if (Manager.GetIndex("Bug Colliders").enabled)
                            {
                                SphereCollider collider = bugSpamObject.AddComponent<SphereCollider>();

                                if (Manager.GetIndex("Bouncy Bug").enabled)
                                {
                                    collider.material.bounciness = 1f;
                                    collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                                    collider.material.dynamicFriction = 0f;
                                }
                            }

                            bugSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                            bugSpamObject.transform.rotation = References.RightHand.rotation;

                            Rigidbody rigidbody = bugSpamObject.AddComponent<Rigidbody>();
                            rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                            rigidbody.angularVelocity = RandomVector3(100f);

                            rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                            targetBug.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = bugSpamObject.transform;

                            bugSpamObject.AddComponent<DestroyOnRest>();
                            Object.Destroy(bugSpamObject, 30f);
                            break;
                        }
                    case 2:
                        {
                            if (!Networking.InRoom)
                                break;

                            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;

                            GameObject cameraSpamObject = new GameObject("Qubit_CameraSpamObject");
                            cameraSpamObject.transform.localScale = Vector3.one * 0.2f;
                            cameraSpamObject.layer = 3;

                            if (Manager.GetIndex("Bug Colliders").enabled)
                            {
                                SphereCollider collider = cameraSpamObject.AddComponent<SphereCollider>();

                                if (Manager.GetIndex("Bouncy Bug").enabled)
                                {
                                    collider.material.bounciness = 1f;
                                    collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                                    collider.material.dynamicFriction = 0f;
                                }
                            }

                            cameraSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                            cameraSpamObject.transform.rotation = References.RightHand.rotation;

                            Rigidbody rigidbody = cameraSpamObject.AddComponent<Rigidbody>();
                            rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                            rigidbody.angularVelocity = RandomVector3(100f);

                            rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                            camera.visible = true;
                            camera.recording = true;

                            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                            camera.m_CameraVisuals.SetRecordingState(true);

                            camera.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = cameraSpamObject.transform;

                            cameraSpamObject.AddComponent<DestroyOnRest>();
                            Object.Destroy(cameraSpamObject, 30f);
                            break;
                        }
                    case 3:
                        {
                            if (!Networking.InRoom)
                                break;

                            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;

                            GameObject cameraSpamObject = new GameObject("Qubit_CameraSpamObject");
                            cameraSpamObject.transform.localScale = Vector3.one * 0.2f;
                            cameraSpamObject.layer = 3;

                            if (Manager.GetIndex("Bug Colliders").enabled)
                            {
                                SphereCollider collider = cameraSpamObject.AddComponent<SphereCollider>();

                                if (Manager.GetIndex("Bouncy Bug").enabled)
                                {
                                    collider.material.bounciness = 1f;
                                    collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                                    collider.material.dynamicFriction = 0f;
                                }
                            }

                            cameraSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                            cameraSpamObject.transform.rotation = References.RightHand.rotation;

                            Rigidbody rigidbody = cameraSpamObject.AddComponent<Rigidbody>();
                            rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                            rigidbody.angularVelocity = RandomVector3(100f);

                            rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                            camera.visible = true;
                            camera.recording = true;

                            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                            camera.m_CameraVisuals.SetRecordingState(true);

                            camera.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = cameraSpamObject.transform;

                            cameraSpamObject.AddComponent<DestroyOnRest>();
                            Object.Destroy(cameraSpamObject, 30f);
                            break;
                        }
                    case 4:
                    case 5:
                        BetaDropBoard(References.RightHand.position, References.RightHand.rotation, GetGunDirection(References.RightHand) * ShootStrength, Vector3.zero, RandomColor());
                        break;
                    case 6:
                        Projectiles.SendProjectile(Projectiles.GetAll()[Random.Range(0, Projectiles.GetAll().Count)], References.RightHand.position, GetGunDirection(References.RightHand) * ShootStrength, RandomColor());
                        break;
                    case 7:
                        Projectiles.SendProjectile(Projectiles.GetGrowingSnowballProjectileEntry(), References.RightHand.position, GetGunDirection(References.RightHand) * ShootStrength);
                        break;
                }
            }
        }

        public static void DisableEverythingSpam()
        {
            DisableBugSpam();
            DisableCameraSpam();
        }

        public static void FireSoundSpam()
        {
            if (rightTrigger > 0.5f)
                EquipCosmetic("LBALH.");
        }

        public static void BubblerGun(int index, Quaternion handRotation, float handOffset = 0.5f)
        {
            GunLibrary.Point(NewPointer =>
            {
                DisableThrowableCoroutine = Coroutines.instance.StartCoroutine(DisableThrowable(index));
                TransferrableObject transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];

                if (!CosmeticsOwned.Contains(transferrableObject.gameObject.name))
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = TryOnRoom.transform.position;
                }

                if (!transferrableObject.gameObject.activeSelf)
                {
                    References.LocalRig.SetActiveTransferrableObjectIndex(1, index);
                    transferrableObject.gameObject.SetActive(true);
                }

                transferrableObject.storedZone = BodyDockPositions.DropPositions.RightArm;
                transferrableObject.currentState = TransferrableObject.PositionState.InRightHand;

                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = NewPointer.transform.position - Vector3.up * 0.5f;

                References.LocalRig.rightHand.rigTarget.transform.position = NewPointer.transform.position + Vector3.up * handOffset;
                References.LocalRig.rightHand.rigTarget.transform.rotation = handRotation;

                References.LocalRig.rightIndex.calcT = 1f;
                References.LocalRig.rightMiddle.calcT = 1f;

                References.LocalRig.rightIndex.LerpFinger(1f, false);
                References.LocalRig.rightMiddle.LerpFinger(1f, false);
            }, new GunLibrary.GunSettings { onIdle = () => { References.LocalRig.enabled = true; }, layerMask = References.Player.locomotionEnabledLayers });
        }

        public static void WhiteColorTarget(VRRig rig)
        {
            int index = 629;
            DisableThrowableCoroutine = Coroutines.instance.StartCoroutine(DisableThrowable(index));
            TransferrableObject transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];

            if (!CosmeticsOwned.Contains(transferrableObject.gameObject.name))
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = TryOnRoom.transform.position;
            }

            if (!transferrableObject.gameObject.activeSelf)
            {
                References.LocalRig.SetActiveTransferrableObjectIndex(1, index);
                transferrableObject.gameObject.SetActive(true);
            }

            transferrableObject.storedZone = BodyDockPositions.DropPositions.RightArm;
            transferrableObject.currentState = TransferrableObject.PositionState.InRightHand;

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = rig.transform.position - Vector3.up;

            References.LocalRig.rightHand.rigTarget.transform.position = rig.transform.position;
            References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

            References.LocalRig.rightIndex.calcT = 1f;
            References.LocalRig.rightMiddle.calcT = 1f;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
        }

        public static void WhiteColorGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    WhiteColorTarget(lockTarget);

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

        public static void BlackColorTarget(VRRig rig) =>
            SendThrowableProjectile(600, rig.transform.position, Vector3.zero, Quaternion.identity);

        public static void BlackColorGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    BlackColorTarget(lockTarget);

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

        public static void ChickenTarget(VRRig rig)
        {
            int index = 651;
            DisableThrowableCoroutine = Coroutines.instance.StartCoroutine(DisableThrowable(index));
            TransferrableObject transferrableObject = References.LocalRig.myBodyDockPositions.allObjects[index];

            if (!CosmeticsOwned.Contains(transferrableObject.gameObject.name))
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = TryOnRoom.transform.position;
            }

            if (!transferrableObject.gameObject.activeSelf)
                EquipCosmetic("LMAQL.");

            transferrableObject.storedZone = BodyDockPositions.DropPositions.RightBack;
            transferrableObject.currentState = TransferrableObject.PositionState.InRightHand;

            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = rig.transform.position;

            References.LocalRig.rightHand.rigTarget.transform.position = rig.transform.position + Vector3.up * (Time.time * 5f % 1f - 0.5f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.identity;

            References.LocalRig.rightIndex.calcT = 1f;
            References.LocalRig.rightMiddle.calcT = 1f;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
        }

        public static void ChickenGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    ChickenTarget(lockTarget);

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

        public static IEnumerator DrawSmallDelay(Vector3 position)
        {
            GameObject Temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Temporary.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Temporary.transform.position = position;
            Object.Destroy(Temporary.GetComponent<Collider>());
            yield return new WaitForSeconds(0.5f);
            CreatePiece(pieceIdSet, Temporary.transform.position, RandomQuaternion(), 0, null, true);
            Object.Destroy(Temporary);
            Security.RPCProtection();
        }
        public static IEnumerator FireShotgun()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                yield break;
            }

            isFiring = true;

            if (!File.Exists($"{PluginInfo.BaseDirectory}/shotgun.wav"))
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/shotgun.ogg", "Audio/Mods/Fun/shotgun.ogg");

            Fun.PlayAudio("shotgun.wav");

            BuilderPiece bullet = null;

            yield return CreateGetPiece(1925587737, piece => bullet = piece);
            while (bullet == null)
                yield return null;

            GrabPiece(bullet, true, Vector3.zero, Quaternion.identity);
            yield return null;
            DropPiece(bullet, Controllers.GetTrueRightHand().position + Controllers.GetTrueRightHand().forward * 0.65f + Controllers.GetTrueRightHand().right * 0.03f + Controllers.GetTrueRightHand().up * 0.05f, Controllers.GetTrueRightHand().rotation, Controllers.GetTrueRightHand().forward * 19.9f, Vector3.zero);
            yield return null;
        }

        public static void AtticSizeToggle()
        {
            if (rightTrigger > 0.5f)
                References.LocalRig.sizeManager.currentSizeLayerMaskValue = 13;

            if (rightGrab)
                References.LocalRig.sizeManager.currentSizeLayerMaskValue = 2;
        }

        public static void ApplyCycleDelay(int index)
        {
            cycleSpeedIndex = index;
            nameCycleDebounce = index / 2f;
        }

        public static void GoldenNameTag(bool isGolden)
        {
            References.LocalRig.ShowGoldNameTag = isGolden;
            References.LocalRig.playerText1.color = References.LocalRig.ShowGoldNameTag ? SubscriptionManager.SUBSCRIBER_NAME_COLOR : Color.white;
        }

        public static void FlashNameTag() =>
            GoldenNameTag((Time.time % 0.2f) > 0.1f);

        public static void DisableMiningCooldown(bool enabled)
        {
            Voxel_Pickaxe pickaxe = GetObject("Player Objects/Local VRRig/Local Gorilla Player(Clone)/rig/hand.R/VIMVoxelPickaxe").GetComponent<Voxel_Pickaxe>();
            if (pickaxe)
                pickaxe.hitCooldown = enabled ? 0 : 0.5f;
        }

        public static void StationaryMining(bool enabled)
        {
            Voxel_Pickaxe pickaxe = GetObject("Player Objects/Local VRRig/Local Gorilla Player(Clone)/rig/hand.R/VIMVoxelPickaxe").GetComponent<Voxel_Pickaxe>();
            if (pickaxe)
            {
                pickaxe.minHitSpeed = enabled ? 0 : 1;
                pickaxe.minMineSpeed = enabled ? 0 : 5;
            }
        }

        public static void NameCycle(string[] names)
        {
            if (Time.time > nameCycleDelay)
            {
                nameCycleIndex++;
                if (nameCycleIndex > names.Length - 1)
                    nameCycleIndex = 0;

                ChangeName(names[nameCycleIndex]);
                nameCycleDelay = Time.time + nameCycleDebounce;
            }
        }

        public static void RandomNameCycle() =>
            NameCycle(new[] { RandomString(8) });
        public static void EnableCustomNameCycle() // Thanks to xynz_ for some fixes
        {
            var path = $"{PluginInfo.BaseDirectory}/Qubit_CustomNameCycle.txt";
            if (File.Exists(path))
            {
                names = File.ReadAllText(path)
                    .Split('\n')
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();
            }
            else
            {
                File.WriteAllText(path, "YOUR\nTEXT\nHERE");
            }
        }
        public static void AnimatedName()
        {
            if (!Networking.InRoom)
            {
                ChangeName(name);
                return;
            }
            if (string.IsNullOrEmpty(name))
                name = PhotonNetwork.LocalPlayer.NickName;
            int length = Mathf.Clamp((int)Mathf.PingPong(Time.time / 0.25f, name.Length) + 1, 1, name.Length);

            ChangeName(name[..length]);
        }

        public static void FlashColor()
        {
            if (Time.time > colorChangerDelay)
            {
                colorChangerDelay = Time.time + 0.05f;
                strobeColor = !strobeColor;
                ChangeColor(strobeColor ? Color.white : Color.black);
            }
        }

        public static void StrobeColor()
        {
            if (Time.time > colorChangerDelay)
            {
                colorChangerDelay = Time.time + 0.05f;
                ChangeColor(RandomColor());
            }
        }

        public static void RainbowColor()
        {
            if (Time.time > colorChangerDelay)
            {
                colorChangerDelay = Time.time + 0.05f;
                float h = Time.frameCount / 180f % 1f;
                ChangeColor(Color.HSVToRGB(h, 1f, 1f));
            }
        }

        public static void HardRainbowColor()
        {
            if (Time.time > colorChangerDelay)
            {
                colorChangerDelay = Time.time + 0.5f;
                colorChangeType++;
                if (colorChangeType > 3)
                    colorChangeType = 0;

                Color[] colors = {
                    Color.red,
                    Color.green,
                    Color.blue,
                    Color.magenta
                };

                ChangeColor(colors[colorChangeType]);
            }
        }

        public static void BecomePlayer(string name, Color color)
        {
            ChangeName(name);
            ChangeColor(color);
        }

        public static void BecomeMinigamesKid()
        {
            string[] names = {
                "MINIGAMES",
                "MINIGAMESKID",
                "LITTLETIMMY",
                "TIMMY",
                "SILLYBILLY"
            };
            Color[] colors = {
                Color.cyan,
                Color.green,
                Color.red,
                Color.magenta
            };

            BecomePlayer(names[Random.Range(0, names.Length)], colors[Random.Range(0, colors.Length)]);
        }
        public static void CopyIdentityGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                ChangeName(GetPlayerFromVRRig(gunTarget).NickName);
                ChangeColor(gunTarget.playerColor);
                stealIdentityDelay = Time.time + 0.5f;
            }, new GunLibrary.GunSettings { when = () => Time.time > stealIdentityDelay });
        }
        public static void CopyCosmeticsGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                References.MyRigView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, gunTarget.cosmeticSet.ToPackedIDArray(), gunTarget.tryOnSet.ToPackedIDArray(), false);
                stealCosmeticsDelay = Time.time + 0.5f;
            }, new GunLibrary.GunSettings { when = () => Time.time > stealCosmeticsDelay });
        }

        public static void ChangeAccessories()
        {
            if (leftGrab && !lastHitL)
            {
                hat--;
                if (hat < 1)
                    hat = 3;

                switch (hat)
                {
                    case 1:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton")
                            .GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 2:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (1)")
                            .GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 3:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (2)")
                            .GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                }
            }
            lastHitL = leftGrab;
            if (rightGrab && !lastHitR)
            {
                hat++;
                if (hat > 3)
                    hat = 1;

                switch (hat)
                {
                    case 1:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 2:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (1)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 3:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (2)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                }

            }
            lastHitR = rightGrab;
            if (leftPrimary && !lastHitLP)
            {
                GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeLeftButton").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                switch (hat)
                {
                    case 1:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 2:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (1)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 3:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (2)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                }
            }
            lastHitLP = leftPrimary;

            if (rightPrimary && !lastHitRP)
            {
                GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeRightItem").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                switch (hat)
                {
                    case 1:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 2:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (1)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                    case 3:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeItemButton (2)").GetComponent<GorillaPressableButton>().ButtonActivationWithHand(false);
                        break;
                }
            }
            lastHitRP = rightPrimary;

            if (rightSecondary && !lastHitRS)
            {
                accessoryType++;
                if (accessoryType > 4)
                    accessoryType = 1;

                switch (accessoryType)
                {
                    case 1:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardobeHatButton").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                        break;
                    case 2:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeFaceButton").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                        break;
                    case 3:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeBadgeButton").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                        break;
                    case 4:
                        GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/Wardrobe/WardrobeHoldableButton").GetComponent<WardrobeFunctionButton>().ButtonActivation();
                        break;
                }
            }
            lastHitRS = rightSecondary;
        }
        public static void StickyHoldables()
        {
            if (Time.time > delayonhold)
            {
                delayonhold = Time.time + 0.1f;
                foreach (TransferrableObject cosmet in GetAllType<TransferrableObject>())
                {
                    if (cosmet.IsMyItem())
                    {
                        if (cosmet.currentState == TransferrableObject.PositionState.OnLeftArm || cosmet.currentState == TransferrableObject.PositionState.OnLeftShoulder)
                            cosmet.currentState = TransferrableObject.PositionState.InLeftHand;
                        if (cosmet.currentState == TransferrableObject.PositionState.OnRightArm || cosmet.currentState == TransferrableObject.PositionState.OnRightShoulder || cosmet.currentState == TransferrableObject.PositionState.OnChest)
                            cosmet.currentState = TransferrableObject.PositionState.InRightHand;
                    }
                }
            }
        }

        public static void SpazHoldables()
        {
            if (Time.time > delayonhold)
            {
                delayonhold = Time.time + 0.1f;
                foreach (TransferrableObject cosmet in GetAllType<TransferrableObject>())
                {
                    if (cosmet.IsMyItem())
                    {
                        cosmet.currentState = (TransferrableObject.PositionState)((int)cosmet.currentState * 2);
                        if ((int)cosmet.currentState > 128)
                            cosmet.currentState = TransferrableObject.PositionState.OnLeftArm;
                    }
                }
            }
        }

        public static void RemoveCosmeticBrowser()
        {
            pageNumber = rememberdirectory;
            Manager.CurrentCategoryName = "Fun Mods";
        }

        public static void AutoLoadCosmetics()
        {
            RequestPatch.enabled = true;
            RequestPatch.currentCoroutine ??= Coroutines.instance.StartCoroutine(RequestPatch.LoadCosmetics());
            TryOnRoom.GetComponent<CosmeticBoundaryTrigger>().enabled = false;
        }

        public static void NoAutoLoadCosmetics()
        {
            TryOnRoom.GetComponent<CosmeticBoundaryTrigger>().enabled = true;
            RequestPatch.enabled = false;
        }
        public static void CopyIDGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                idgundelay = Time.time + 0.5f;
                string id = GetPlayerFromVRRig(gunTarget).UserId;
                Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
                GUIUtility.systemCopyBuffer = id;
            }, new GunLibrary.GunSettings { when = () => Time.time > idgundelay });
        }

        public static void CopyIDAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0)
            {
                foreach (var id in nearbyPlayers.Select(nearbyPlayer => GetPlayerFromVRRig(nearbyPlayer).UserId))
                {
                    Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
                    GUIUtility.systemCopyBuffer = id;
                }
            }
        }

        public static void CopyIDOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0)
            {
                foreach (var id in touchedPlayers.Select(rig => rig.GetPlayer().UserId))
                {
                    Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
                    GUIUtility.systemCopyBuffer = id;
                }
            }
        }

        public static void CopyIDAll()
        {
            foreach (var id in Players.ActiveRigs.Select(vrrig => GetPlayerFromVRRig(vrrig).UserId))
            {
                Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
                GUIUtility.systemCopyBuffer = id;
            }
        }

        public static void CopySelfID()
        {
            string id = PhotonNetwork.LocalPlayer.UserId;
            Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
            GUIUtility.systemCopyBuffer = id;
        }

        public static void NarrateIDGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                idgundelay = Time.time + 0.5f;
                SpeakText("Name: " + GetPlayerFromVRRig(gunTarget).NickName + ". I D: " + string.Join(" ", GetPlayerFromVRRig(gunTarget).UserId));
            }, new GunLibrary.GunSettings { when = () => Time.time > idgundelay });
        }

        public static void NarrateIDAll()
        {
            string ids = "";
            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (!vrrig.isLocal)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I D: " + string.Join(" ", GetPlayerFromVRRig(vrrig).UserId) + ". ";
            }
            SpeakText(ids);
        }
        public static void NarrateIDAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0 && Time.time > allNarrationDelay)
            {
                allNarrationDelay = Time.time + 10f;

                string ids = "";
                foreach (VRRig vrrig in nearbyPlayers)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I D: " + string.Join(" ", GetPlayerFromVRRig(vrrig).UserId) + ". ";
                SpeakText(ids);
            }
        }

        public static void NarrateIDOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0 && Time.time > allNarrationDelay)
            {
                allNarrationDelay = Time.time + 10f;

                string ids = "";
                foreach (VRRig vrrig in touchedPlayers)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I D: " + string.Join(" ", GetPlayerFromVRRig(vrrig).UserId) + ". ";
                SpeakText(ids);
            }
        }

        public static void NarrateSelfID() =>
            SpeakText("Name: " + PhotonNetwork.LocalPlayer.NickName + ". I D: " + string.Join(" ", PhotonNetwork.LocalPlayer.UserId));

        public static void NarrateFakeDoxxGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                idgundelay = Time.time + 0.5f;
                SpeakText("Name: " + GetPlayerFromVRRig(gunTarget).NickName + ". I P  ADD DRESS: " + string.Join(" ", $"{Random.Range(1, 255)}.{Random.Range(1, 255)}.{Random.Range(1, 255)}"));
            }, new GunLibrary.GunSettings { when = () => Time.time > idgundelay });
        }

        public static void NarrateFakeDoxxAll()
        {
            string ids = "";
            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (!vrrig.isLocal)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I P  ADD DRESS: " + string.Join(" ", $"{Random.Range(1, 255)}.{Random.Range(1, 255)}.{Random.Range(1, 255)}") + ". ";
            }
            SpeakText(ids);
        }

        public static void NarrateFakeDoxxAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count > 0 && Time.time > allNarrationDelay)
            {
                allNarrationDelay = Time.time + 10f;

                string ids = "";
                foreach (VRRig vrrig in nearbyPlayers)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I P  ADD DRESS: " + string.Join(" ", $"{Random.Range(1, 255)}.{Random.Range(1, 255)}.{Random.Range(1, 255)}") + ". ";
                SpeakText(ids);
            }
        }

        public static void NarrateFakeDoxxOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0 && Time.time > allNarrationDelay)
            {
                allNarrationDelay = Time.time + 10f;

                string ids = "";
                foreach (VRRig vrrig in touchedPlayers)
                    ids += "Name: " + GetPlayerFromVRRig(vrrig).NickName + ". I P  ADD DRESS: " + string.Join(" ", $"{Random.Range(1, 255)}.{Random.Range(1, 255)}.{Random.Range(1, 255)}") + ". ";
                SpeakText(ids);
            }
        }

        public static void NarrateFakeDoxxSelf() =>
            SpeakText("Name: " + PhotonNetwork.LocalPlayer.NickName + ". I P  ADD DRESS: " + string.Join(" ", $"{Random.Range(1, 255)}.{Random.Range(1, 255)}.{Random.Range(1, 255)}"));
        public static void CopyCreationDateSelf()
        {
            string date = GetCreationDate(PhotonNetwork.LocalPlayer.UserId, CopyCreationDate);
            if (date != "Loading...")
                CopyCreationDate(date);
        }

        public static void CopyCreationDateGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                creationDateDelay = Time.time + 0.5f;

                string date = GetCreationDate(GetPlayerFromVRRig(gunTarget).UserId, CopyCreationDate);
                if (date != "Loading...")
                    CopyCreationDate(date);
            }, new GunLibrary.GunSettings { when = () => Time.time > creationDateDelay });
        }

        public static void CopyCreationDateAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count <= 0) return;
            foreach (var date in nearbyPlayers.Select(nearbyPlayer => GetCreationDate(GetPlayerFromVRRig(nearbyPlayer).UserId, CopyCreationDate)).Where(date => date != "Loading..."))
                CopyCreationDate(date);
        }

        public static void CopyCreationDateOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = new List<VRRig>();

            foreach (VRRig rig in Players.ActiveRigs)
            {
                if (!rig.IsLocal())
                {
                    if (rig.IsBeingTouched())
                    {
                        touchedPlayers.Add(rig);
                    }
                }
            }

            if (touchedPlayers.Count > 0)
            {
                foreach (var date in touchedPlayers.Select(rig => GetCreationDate(rig.GetPlayer().UserId, CopyCreationDate)).Where(date => date != "Loading..."))
                    CopyCreationDate(date);
            }
        }

        public static void CopyCreationDateAll()
        {
            foreach (var date in Players.ActiveRigs.Select(vrrig => GetCreationDate(GetPlayerFromVRRig(vrrig).UserId, CopyCreationDate)).Where(date => date != "Loading..."))
            {
                CopyCreationDate(date);
            }
        }

        public static void CopyCreationDate(string date)
        {
            Notifications.SendFormat("green", "SUCCESS", "" + date, 5000);
            GUIUtility.systemCopyBuffer = date;
        }

        public static void NarrateCreationDateSelf()
        {
            string date = GetCreationDate(PhotonNetwork.LocalPlayer.UserId, SpeakText);
            if (date != "Loading...")
                SpeakText(date);
        }

        public static void NarrateCreationDateAll()
        {
            foreach (var date in Players.ActiveRigs.Select(vrrig => GetCreationDate(GetPlayerFromVRRig(vrrig).UserId, SpeakText)).Where(date => date != "Loading..."))
                SpeakText(date);
        }

        public static void NarrateCreationDateAura()
        {
            if (!Networking.InRoom) return;
            List<VRRig> nearbyPlayers = new List<VRRig>();

            foreach (VRRig vrrig in Players.ActiveRigs)
            {
                if (Vector3.Distance(vrrig.transform.position, References.LocalRig.transform.position) < 4 && !vrrig.IsLocal())
                    nearbyPlayers.Add(vrrig);
                else if (nearbyPlayers.Contains(vrrig))
                    nearbyPlayers.Remove(vrrig);
            }

            if (nearbyPlayers.Count <= 0 || Time.time < allNarrationDelay) return;
            allNarrationDelay = Time.time + 10f;

            foreach (var date in nearbyPlayers.Select(nearbyPlayer => GetCreationDate(GetPlayerFromVRRig(nearbyPlayer).UserId, date => SpeakText(date))).Where(date => date != "Loading..."))
                SpeakText(date);
        }

        public static void NarrateCreationDateOnTouch()
        {
            if (!Networking.InRoom) return;

            List<VRRig> touchedPlayers = Players.ActiveRigs.Where(rig => !rig.IsLocal()).Where(rig => rig.IsBeingTouched()).ToList();

            if (touchedPlayers.Count <= 0 || Time.time < allNarrationDelay) return;
            allNarrationDelay = Time.time + 10f;

            foreach (var date in touchedPlayers.Select(rig => GetCreationDate(rig.GetPlayer().UserId, date => SpeakText(date))).Where(date => date != "Loading..."))
                SpeakText(date);
        }

        public static void NarrateCreationDateGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                creationDateDelay = Time.time + 0.5f;

                string date = GetCreationDate(GetPlayerFromVRRig(gunTarget).UserId, date => SpeakText(date));
                if (date != "Loading...")
                    SpeakText(date);
            }, new GunLibrary.GunSettings { when = () => Time.time > creationDateDelay });
        }

        public static void GrabPlayerInfo()
        {
            string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                float r = 0f;
                float g = 0f;
                float b = 0f;
                string cosmetics = "";
                try
                {
                    VRRig plr = GetVRRigFromPlayer(player);
                    r = plr.playerColor.r * 255;
                    g = plr.playerColor.g * 255;
                    b = plr.playerColor.b * 255;
                    cosmetics = plr.Cosmetics();
                }
                catch { Logs.Log("Failed to log colors, rig most likely nonexistent"); }
                try
                {
                    text += "\n====================================\n";
                    text += string.Concat("Player Name: \"", player.NickName, "\", Player ID: \"", player.UserId, "\", Player Color: (R: ", r.ToString(), ", G: ", g.ToString(), ", B: ", b.ToString(), "), Cosmetics: ", cosmetics);
                }
                catch { Logs.Log("Failed to log player"); }
            }
            text += "\n====================================\n";
            text += "Text file generated with Qubit Menu";
            string fileName = $"{PluginInfo.BaseDirectory}/PlayerInfo/" + PhotonNetwork.CurrentRoom.Name + ".txt";

            File.WriteAllText(fileName, text);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static void DisablePitchScaling()
        {
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
            {
                vrrig.voicePitchForRelativeScale = new AnimationCurve(
                    new Keyframe(0f, 1f, 0f, 0f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            }
        }

        public static void EnablePitchScaling()
        {
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
                vrrig.voicePitchForRelativeScale = References.LocalRig.voicePitchForRelativeScale;
        }

        public static void DisableMouthMovement()
        {
            References.LocalRig.shouldSendSpeakingLoudness = false;
            LoudnessPatch.enabled = true;
        }

        public static void EnableMouthMovement()
        {
            References.LocalRig.shouldSendSpeakingLoudness = true;
            LoudnessPatch.enabled = false;
        }

        public static void ConfusePlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = lockTarget.transform.position - new Vector3(0f, 2f, 0f);

                    if (Time.time > Fun.splashDel)
                    {
                        References.MyRigView.SendRPC("RPC_PlaySplashEffect", lockTarget.GetPlayer(), lockTarget.transform.position + RandomVector3(0.5f), RandomQuaternion(), 4f, 100f, true, false);
                        Security.RPCProtection();
                        Fun.splashDel = Time.time + 0.1f;
                    }
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

        public static void ConfuseAllPlayers()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up * 2f;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };
        }

        public static void ConfuseAllPlayersSplash()
        {
            if (Time.time > Fun.splashDel)
            {
                Fun.splashDel = Time.time + 0.05f;
                VRRig rig = GetRandomVRRig();
                References.MyRigView.SendRPC("RPC_PlaySplashEffect", rig.GetPlayer(), rig.transform.position + RandomVector3(0.5f), RandomQuaternion(), 4f, 100f, true, false);
            }
        }
        public static void TinnitusGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!Networking.InRoom) return;
                gunLocked = true;
                lockTarget = gunTarget;

                if (Movement.tinnitus.Count == 0)
                {
                    AudioClip audioClip = Movement.CreateTinnitusSound(Movement.targetHz);
                    Voices.Clip clip = Voices.Get().AudioClip(audioClip);
                    clip.Looping = true;
                    Movement.tinnitus[clip.Id] = clip;
                }
                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = tinnitusSelf;

                SerializePatch.OverrideSerialization = () =>
                {
                    NetPlayer target = lockTarget.GetPlayer();
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 positionArchive = References.LocalRig.transform.position;
                    References.LocalRig.transform.position = lockTarget.transform.position - (lockTarget.headMesh.transform.forward * 0.2f);
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } });

                    References.LocalRig.transform.position = new Vector3(Random.Range(-99999f, 99999f), 99999f, Random.Range(-99999f, 99999f));
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != target.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                    Security.RPCProtection();
                    References.LocalRig.transform.position = positionArchive;

                    return false;
                };
            }, new GunLibrary.GunSettings { when = () => !gunLocked, onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        Sound.FixMicrophone();
                        SerializePatch.OverrideSerialization = null;
                    }
                } });
        }

        public static void TinnitusAll()
        {
            if (!Networking.InRoom) return;
            if (Movement.tinnitus.Count == 0)
            {
                AudioClip audioClip = Movement.CreateTinnitusSound(Movement.targetHz);
                Voices.Clip clip = Voices.Get().AudioClip(audioClip);
                clip.Looping = true;
                Movement.tinnitus[clip.Id] = clip;
            }
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = tinnitusSelf;

            SerializePatch.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - (targetRig.headMesh.transform.forward * 0.2f);
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };
        }

        public static void DisableTinnitus()
        {
            SerializePatch.OverrideSerialization = null;
            foreach (Voices.Clip clip in Movement.tinnitus.Values)
                Voices.Get().StopAudioClip(clip);
            Movement.tinnitus.Clear();
        }
        public static void OverstimulateGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > Fun.splashDel)
                    {
                        References.MyRigView.SendRPC("RPC_PlaySplashEffect", lockTarget.GetPlayer(), lockTarget.headMesh.transform.TransformPoint(Vector3.forward * 0.2f), RandomQuaternion(), 4f, 100f, true, false);
                        Fun.splashDel = Time.time + 0.1f;
                    }

                    if (Random.Range(0f, 1f) > 0.5f)
                    {
                        if (Time.time > Movement.soundSpamDelay)
                        {
                            Movement.soundSpamDelay = Time.time + 0.1f;
                            References.MyRigView.SendRPC("RPC_PlayHandTap", lockTarget.GetPhotonPlayer(), Random.Range(336, 338), false, 999999f);
                        }
                    }
                    else
                    {
                        if (Time.time > Movement.soundSpamDelay)
                        {
                            int[] sounds = {
                                Random.Range(40,54),
                                Random.Range(214,221)
                            };

                            Movement.soundSpamDelay = Time.time + 0.1f;
                            References.MyRigView.SendRPC("RPC_PlayHandTap", lockTarget.GetPhotonPlayer(), sounds[Random.Range(0, 1)], false, 999999f);
                        }
                    }

                    if (Time.time > Hoverboards.hoverboardSpawnDelay)
                    {
                        Hoverboards.hoverboardSpawnDelay = Time.time + 0.25f;

                        FreeHoverboardManager.DataPerPlayer orCreatePlayerData = ManagerRegistry.FreeHoverboardManager.GetOrCreatePlayerData(Networking.LocalPlayer.ActorNumber);
                        int boardIndex = orCreatePlayerData.board0.gameObject.activeSelf ? ((!orCreatePlayerData.board1.gameObject.activeSelf) ? 1 : (1 - ManagerRegistry.FreeHoverboardManager.localPlayerLastSpawnedBoardIndex)) : 0;

                        ManagerRegistry.FreeHoverboardManager.photonView.RPC("DropBoard_RPC", lockTarget.GetPhotonPlayer(), boardIndex == 1,
                            BitPackUtils.PackWorldPosForNetwork(lockTarget.headMesh.transform.TransformPoint(Vector3.forward * 0.2f)),
                            BitPackUtils.PackQuaternionForNetwork(RandomQuaternion()),
                            BitPackUtils.PackWorldPosForNetwork(RandomVector3(3)), BitPackUtils.PackWorldPosForNetwork(RandomVector3(3)),
                            BitPackUtils.PackColorForNetwork(RandomColor()));

                        ManagerRegistry.FreeHoverboardManager.localPlayerLastSpawnedBoardIndex = boardIndex;
                    }

                    Security.RPCProtection();
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal() && !gunLocked)
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;

                        SerializePatch.OverrideSerialization = () =>
                        {
                            NetPlayer target = lockTarget.GetPlayer();
                            MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                            Vector3 positionArchive = References.LocalRig.transform.position;
                            References.LocalRig.transform.position = lockTarget.transform.position + RandomVector3();
                            SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } });

                            Security.RPCProtection();
                            References.LocalRig.transform.position = positionArchive;
                            SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != target.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                            return false;
                        };
                    }
                }
            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        SerializePatch.OverrideSerialization = null;
                    }
                } });
        }

        public static void OverstimulateAll()
        {
            SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });
                Vector3 archivePos = References.LocalRig.transform.position;
                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);
                    References.LocalRig.transform.position = targetRig.transform.position + RandomVector3();
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }
                Security.RPCProtection();
                References.LocalRig.transform.position = archivePos;
                return false;
            };

            VRRig randomRig = GetRandomVRRig();

            if (Time.time > Fun.splashDel)
            {
                References.MyRigView.SendRPC("RPC_PlaySplashEffect", GetPlayerFromVRRig(randomRig), randomRig.headMesh.transform.TransformPoint(Vector3.forward * 0.2f), RandomQuaternion(), 4f, 100f, true, false);
                Fun.splashDel = Time.time + 0.1f;
            }

            if (Random.Range(0f, 1f) > 0.5f)
            {
                if (Time.time > Movement.soundSpamDelay)
                {
                    Movement.soundSpamDelay = Time.time + 0.1f;
                    References.MyRigView.SendRPC("RPC_PlayHandTap", randomRig.GetPhotonPlayer(), Random.Range(336, 338), false, 999999f);
                }
            }
            else
            {
                if (Time.time > Movement.soundSpamDelay)
                {
                    int[] sounds = {
                        Random.Range(40,54),
                        Random.Range(214,221)
                    };

                    Movement.soundSpamDelay = Time.time + 0.1f;
                    References.MyRigView.SendRPC("RPC_PlayHandTap", randomRig.GetPhotonPlayer(), sounds[Random.Range(0, 1)], false, 999999f);
                }
            }

            if (Time.time > Hoverboards.hoverboardSpawnDelay)
            {
                Hoverboards.hoverboardSpawnDelay = Time.time + 0.25f;

                FreeHoverboardManager.DataPerPlayer orCreatePlayerData = ManagerRegistry.FreeHoverboardManager.GetOrCreatePlayerData(Networking.LocalPlayer.ActorNumber);
                int boardIndex = orCreatePlayerData.board0.gameObject.activeSelf ? ((!orCreatePlayerData.board1.gameObject.activeSelf) ? 1 : (1 - ManagerRegistry.FreeHoverboardManager.localPlayerLastSpawnedBoardIndex)) : 0;

                ManagerRegistry.FreeHoverboardManager.photonView.RPC("DropBoard_RPC", randomRig.GetPhotonPlayer(), boardIndex == 1,
                    BitPackUtils.PackWorldPosForNetwork(randomRig.headMesh.transform.TransformPoint(Vector3.forward * 0.2f)),
                    BitPackUtils.PackQuaternionForNetwork(RandomQuaternion()),
                    BitPackUtils.PackWorldPosForNetwork(RandomVector3(3)), BitPackUtils.PackWorldPosForNetwork(RandomVector3(3)),
                    BitPackUtils.PackColorForNetwork(RandomColor()));

                ManagerRegistry.FreeHoverboardManager.localPlayerLastSpawnedBoardIndex = boardIndex;
            }
        }

        public static void ShutdownHeadsetGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    Hoverboards.HoverboardScreenTarget(lockTarget, Color.black);

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;

                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/shutdown.ogg", "Audio/Mods/Fun/shutdown.ogg", clip => Fun.PlayAudio(clip));
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

        public static void ShutdownHeadsetAll() =>
            Hoverboards.HoverboardScreenAll(Color.black);

        public static void ReverseSchizophrenicGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                gunLocked = true;
                lockTarget = gunTarget;

                SerializePatch.OverrideSerialization = () =>
                {
                    NetPlayer target = lockTarget.GetPlayer();
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 positionArchive = References.LocalRig.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != target.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                    References.LocalRig.transform.position = new Vector3(Random.Range(-99999f, 99999f), 99999f, Random.Range(-99999f, 99999f));
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } });

                    Security.RPCProtection();
                    References.LocalRig.transform.position = positionArchive;

                    return false;
                };
            }, new GunLibrary.GunSettings { when = () => !gunLocked, onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        SerializePatch.OverrideSerialization = null;
                    }
                } });
        }

        public static void SchizophrenicGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                gunLocked = true;
                lockTarget = gunTarget;

                SerializePatch.OverrideSerialization = () =>
                {
                    NetPlayer target = lockTarget.GetPlayer();
                    MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                    Vector3 positionArchive = References.LocalRig.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { target.ActorNumber } });

                    References.LocalRig.transform.position = new Vector3(Random.Range(-99999f, 99999f), 99999f, Random.Range(-99999f, 99999f));
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = PhotonNetwork.PlayerList.Where(plr => plr.ActorNumber != target.ActorNumber).Select(plr => plr.ActorNumber).ToArray() });

                    Security.RPCProtection();
                    References.LocalRig.transform.position = positionArchive;

                    return false;
                };
            }, new GunLibrary.GunSettings { when = () => !gunLocked, onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        SerializePatch.OverrideSerialization = null;
                    }
                } });
        }

        public static void SpamObjectGrip(int objectId)
        {
            if (rightGrab)
                Overpowered.CreateItem(RpcTarget.All, objectId, References.RightHand.position, RandomQuaternion(), References.RightHand.forward * ShootStrength, Vector3.zero);
        }

        public static void SpamEntityGrip()
        {
            int[] objectIds = ObjectByName.Select(x => x.Value).ToArray();
            SpamObjectGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void ToolSpamGrip()
        {
            int[] objectIds = ObjectByName.Where(x => x.Key.Contains("Tool")).Select(x => x.Value).ToArray();
            SpamObjectGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void ToolSpamGun()
        {
            int[] objectIds = ObjectByName.Where(x => x.Key.Contains("Tool")).Select(x => x.Value).ToArray();
            SpamObjectGun(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void SpamObjectGun(int objectId)
        {
            GunLibrary.Point(NewPointer =>
            {
                Overpowered.CreateItem(RpcTarget.All, objectId, NewPointer.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero);
            });
        }

        public static void SpamEntityGun()
        {
            int[] objectIds = ObjectByName.Select(x => x.Value).ToArray();
            SpamObjectGun(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void RainEntities()
        {
            int[] objectIds = ObjectByName.Select(x => x.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + new Vector3(Random.Range(-3f, 3f), 4f, Random.Range(-3f, 3f)), Quaternion.identity, Vector3.down, Vector3.zero);
        }

        public static void EntityAura()
        {
            int[] objectIds = ObjectByName.Select(x => x.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + RandomVector3().normalized * 2f, Quaternion.identity, Vector3.down, Vector3.zero);
        }

        public static void EntityFountain()
        {
            int[] objectIds = ObjectByName.Select(x => x.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + Vector3.up * 3f, Quaternion.identity, RandomVector3(15f), Vector3.zero);
        }

        public static void GhostReactorTextGun()
        {
            TextGun.Render(textToRender, position =>
                Overpowered.CreateItems(RpcTarget.All, Enumerable.Repeat(ObjectByName["GhostReactorCollectibleFlower"], position.Count).ToArray(), position.ToArray(), Enumerable.Repeat(Quaternion.identity, position.Count).ToArray()));
        }

        public static void SuperInfectionTextGun()
        {
            TextGun.Render(textToRender, position =>
                Overpowered.CreateItems(RpcTarget.All, Enumerable.Repeat(GadgetByName["SIGadgetDashYoyo"], position.Count).ToArray(), position.ToArray(), Enumerable.Repeat(Quaternion.identity, position.Count).ToArray(), null, ManagerRegistry.SuperInfection.GameEntityManager));
        }

        public static void GhostReactorDrawGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                Coroutines.instance.StartCoroutine(Overpowered.DrawSmallDelay(NewPointer.transform.position, ObjectByName["GhostReactorCollectibleCore"], ManagerRegistry.GhostReactor.GameEntityManager));
            });
        }

        public static void SuperInfectionDrawGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
                Coroutines.instance.StartCoroutine(Overpowered.DrawSmallDelay(NewPointer.transform.position, objectIds[Random.Range(0, objectIds.Length)], ManagerRegistry.SuperInfection.GameEntityManager));
            });
        }
        public static void DestroyEntityGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > Overpowered.destroyDelay)
                {
                    GameEntity gameEntity = null;
                    float closestDist = float.MaxValue;

                    foreach (GameEntity entity in ManagerRegistry.GhostReactor.GameEntityManager.entities)
                    {
                        if (entity != null)
                        {
                            float distance = Vector3.Distance(NewPointer.transform.position, entity.transform.position);
                            if (distance < 0.75f && distance < closestDist)
                            {
                                gameEntity = entity;
                                closestDist = distance;
                            }
                        }
                    }

                    if (gameEntity != null)
                    {
                        Overpowered.destroyDelay = Time.time + 0.02f;
                        if (Networking.IsMasterClient)
                        {
                            ManagerRegistry.GhostReactor.GameEntityManager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new[] { gameEntity.GetNetId() });
                            Security.RPCProtection();
                        }
                        else
                        {
                            gameEntity.RequestGrab(true, Vector3.zero, Quaternion.identity);
                            gameEntity.RequestThrow(true, References.BodyCollider.transform.position - (Vector3.up * 14f), Quaternion.identity, Vector3.zero, Vector3.zero);
                        }
                    }
                }
            });
        }
        public static void InfiniteResources()
        {
            var player = SIPlayer.Get(Networking.LocalPlayer.ActorNumber);
            for (int i = 0; i < player.CurrentProgression.resourceArray.Length; i++)
                player.CurrentProgression.resourceArray[i] = int.MaxValue;

            if (Time.time > Overpowered.resourceIncrementDelay)
            {
                Overpowered.resourceIncrementDelay = Time.time + 1f;
                for (int i = 0; i < (int)SIResource.ResourceType.Count; i++)
                {
                    var resourceType = (SIResource.ResourceType)i;
                    ManagerRegistry.ProgressionManager.IncrementSIResource(resourceType.ToString(), OnFailure: (error) => Overpowered.resourceIncrementDelay = Time.time + 10f);
                }
            }
        }

        public static void CompleteAllQuests()
        {
            for (int i = 0; i < ManagerRegistry.SIProgression.activeQuestIds.Length; i++)
            {
                RotatingQuest quest = ManagerRegistry.SIProgression.questSourceList.GetQuestById(ManagerRegistry.SIProgression.activeQuestIds[i]);
                quest.SetProgress(quest.requiredOccurenceCount);
            }
        }

        public static void ClaimAllTerminals()
        {
            foreach (var terminal in ManagerRegistry.SuperInfection.ZoneSuperInfection.siTerminals)
                terminal?.PlayerHandScanned(Networking.LocalPlayer.ActorNumber);
        }

        public static void UnlockAllGadgets()
        {
            foreach (var page in ManagerRegistry.SIProgression.unlockedTechTreeData)
            {
                for (int i = 0; i < page.Length; i++)
                    page[i] = true;
            }
        }

        public static void DebugBlasterAimbot()
        {
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
                .OrderBy(x => Vector3.Angle(head.forward, x.ToRig)) // only angle matters
                .ThenBy(x => x.Distance) // tiebreaker if multiple at same angle
                .Select(x => x.Rig)
                .FirstOrDefault();

            if (targetRig == null)
                return;

            Visuals.Visualize(PrimitiveType.Sphere, targetRig.headMesh.transform.position, Quaternion.identity, new Vector3(0.1f, 0.1f, 0.1f), Color.green, -91752, 0.1f);
        }

        public static void BlasterLaserSpam()
        {
            if (rightGrab)
                Overpowered.BetaFireBlaster(References.RightHand.position, References.RightHand.forward);
        }

        public static void BlasterFlingGun(Vector3 direction)
        {
            GunLibrary.Target(lockTarget =>
            {
                Overpowered.BetaFireBlaster(lockTarget.transform.position, direction.normalized);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BlasterFlingAll(Vector3 direction)
        {
            if (Overpowered.GetBlaster() == null)
                SerializePatch.OverrideSerialization = null;
            else SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Overpowered.BetaFireBlaster(targetRig.transform.position, direction, Player, true);
            }
        }

        public static void BlasterFlingTowardsGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Overpowered.BetaFireBlaster(lockTarget.transform.position, References.BodyCollider.transform.position - lockTarget.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BlasterFlingTowardsAll()
        {
            if (Overpowered.GetBlaster() == null)
                SerializePatch.OverrideSerialization = null;
            else SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Overpowered.BetaFireBlaster(targetRig.transform.position, References.BodyCollider.transform.position - targetRig.transform.position, Player, true);
            }
        }

        public static void BlasterFlingAwayGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Overpowered.BetaFireBlaster(lockTarget.transform.position, lockTarget.transform.position - References.BodyCollider.transform.position);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BlasterFlingAwayAll()
        {
            if (Overpowered.GetBlaster() == null)
                SerializePatch.OverrideSerialization = null;
            else SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Overpowered.BetaFireBlaster(targetRig.transform.position, targetRig.transform.position - References.BodyCollider.transform.position, Player, true);
            }
        }

        public static void BlasterKickGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Overpowered.BetaFireBlaster(lockTarget.transform.position, lockTarget.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - lockTarget.transform.position).normalized : lockTarget.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BlasterKickAll()
        {
            if (Overpowered.GetBlaster() == null)
                SerializePatch.OverrideSerialization = null;
            else SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Overpowered.BetaFireBlaster(targetRig.transform.position, targetRig.transform.position.z < -28.5f ? (new Vector3(-47.82025f, 6.460508f, -29.04836f) - targetRig.transform.position).normalized : targetRig.transform.position.z < -23f ? new Vector3(-50f, 0f, 50f) : Vector3.left, Player, true);
            }
        }

        public static void BlasterCrashGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                Overpowered.BetaFireBlaster(lockTarget.transform.position, lockTarget.transform.position.y > 55f ? Vector3.right : Vector3.up);
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void BlasterCrashAll()
        {
            if (Overpowered.GetBlaster() == null)
                SerializePatch.OverrideSerialization = null;
            else SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position - Vector3.up;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                Security.RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };

            foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
            {
                VRRig targetRig = GetVRRigFromPlayer(Player);
                Overpowered.BetaFireBlaster(targetRig.transform.position, targetRig.transform.position.y > 55f ? Vector3.right : Vector3.up, Player, true);
            }
        }

        public static void BlasterControlGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null && lockTarget.Distance(References.BodyCollider.transform.position) > 0.5f)
                    Overpowered.BetaFireBlaster(lockTarget.transform.position, References.BodyCollider.transform.position - lockTarget.transform.position);

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
                        gunLocked = false;
                } });
        }

        public static void SpamGadgetGrip(int objectId)
        {
            if (rightGrab)
                Overpowered.CreateItem(RpcTarget.All, objectId, References.RightHand.position, RandomQuaternion(), References.RightHand.forward * ShootStrength, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }

        public static void SpamGadgetGun(int objectId)
        {
            GunLibrary.Point(NewPointer =>
            {
                Overpowered.CreateItem(RpcTarget.All, objectId, NewPointer.transform.position, RandomQuaternion(), Vector3.zero, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
            });
        }

        public static void GadgetSpamGrip()
        {
            int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
            SpamGadgetGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void GadgetSpamGun()
        {
            int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
            SpamGadgetGun(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void RainGadgets()
        {
            int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + new Vector3(Random.Range(-3f, 3f), 4f, Random.Range(-3f, 3f)), Quaternion.identity, Vector3.down, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }

        public static void GadgetAura()
        {
            int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + RandomVector3().normalized * 2f, Quaternion.identity, Vector3.down, Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }

        public static void GadgetFountain()
        {
            int[] objectIds = GadgetByName.Select(element => element.Value).ToArray();
            Overpowered.CreateItem(RpcTarget.All, objectIds[Random.Range(0, objectIds.Length)], References.LocalRig.transform.position + Vector3.up * 3f, Quaternion.identity, RandomVector3(15f), Vector3.zero, 0L, ManagerRegistry.SuperInfection.GameEntityManager);
        }

        public static void ResourceSpamGrip()
        {
            int[] objectIds = GadgetByName.Where(x => x.Key.Contains("Resource")).Select(x => x.Value).ToArray();
            SpamGadgetGrip(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void ResourceSpamGun()
        {
            int[] objectIds = GadgetByName.Where(x => x.Key.Contains("Resource")).Select(x => x.Value).ToArray();
            SpamGadgetGun(objectIds[Random.Range(0, objectIds.Length)]);
        }

        public static void DestroyGadgetGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Time.time > Overpowered.destroyDelay)
                {
                    GameEntity gameEntity = null;
                    float closestDist = float.MaxValue;

                    foreach (GameEntity entity in ManagerRegistry.SuperInfection.GameEntityManager.entities)
                    {
                        if (entity != null)
                        {
                            float distance = Vector3.Distance(NewPointer.transform.position, entity.transform.position);
                            if (distance < 0.75f && distance < closestDist)
                            {
                                gameEntity = entity;
                                closestDist = distance;
                            }
                        }
                    }

                    if (gameEntity != null)
                    {
                        Overpowered.destroyDelay = Time.time + 0.02f;
                        if (Networking.IsMasterClient)
                        {
                            ManagerRegistry.SuperInfection.GameEntityManager.photonView.RPC("DestroyItemRPC", RpcTarget.All, new[] { gameEntity.GetNetId() });
                            Security.RPCProtection();
                        }
                        else
                        {
                            gameEntity.RequestGrab(true, Vector3.zero, Quaternion.identity, ManagerRegistry.SuperInfection.GameEntityManager);
                            gameEntity.RequestThrow(true, References.BodyCollider.transform.position - (Vector3.up * 14f), Quaternion.identity, Vector3.zero, Vector3.zero, ManagerRegistry.SuperInfection.GameEntityManager);
                        }
                    }
                }
            });
        }

        

        public static void SpawnBlueLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                hgc.timeGongStarted = Time.time;
                hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
                hgc.isSummoned = false;
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void SpawnRedLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                hgc.timeGongStarted = Time.time;
                hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
                hgc.isSummoned = true;
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void DespawnLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                hgc.currentState = HalloweenGhostChaser.ChaseState.Dormant;
                hgc.isSummoned = false;
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void LucyChase(NetPlayer player)
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                hgc.targetPlayer = player;
                hgc.followTarget = References.Tagger.offlineVRRig.transform;
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void LucyChaseGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                LucyChase(gunTarget.GetPlayer());
            });
        }

        public static void LucyAttack(NetPlayer player)
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                if (Time.time > hgc.grabTime + hgc.grabDuration + 0.1f)
                {
                    if (hgc.targetPlayer != player)
                    {
                        hgc.currentState = HalloweenGhostChaser.ChaseState.Dormant;
                        SendSerialize(hgc.GetView);
                    }
                    hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
                    hgc.grabTime = Time.time;
                    hgc.targetPlayer = player;
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void LucyAttackGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                LucyAttack(lockTarget.GetPlayer());
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void LucyHarassGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                HalloweenGhostChaser hgc = Overpowered.Lucy;
                if (hgc.IsMine)
                {
                    if (Time.time > lucyDelay)
                    {
                        hgc.currentState = hgc.currentState == HalloweenGhostChaser.ChaseState.Grabbing ? HalloweenGhostChaser.ChaseState.Chasing : HalloweenGhostChaser.ChaseState.Grabbing;
                        hgc.transform.position = lockTarget.transform.position + Vector3.up;
                        hgc.currentSpeed = 0f;
                        hgc.targetPlayer = lockTarget.GetPlayer();
                        hgc.followTarget = lockTarget.transform;
                        lucyDelay = Time.time + 0.1f;
                    }
                }
                else
                    Notifications.SendFormat("red", "ERROR", "You are not master client.");
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void LucyAttackAll()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (SerializePatch.OverrideSerialization != null)
            {
                SerializePatch.OverrideSerialization = () =>
                {
                    MassSerialize(true, new[] { hgc.GetView });
                    return false;
                };
            }

            if (hgc.IsMine)
            {
                if (Time.time > hgc.grabTime + hgc.grabDuration + 0.1f)
                {
                    foreach (NetPlayer player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                    {
                        hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
                        hgc.grabTime = Time.time;
                        hgc.targetPlayer = player;
                        SendSerialize(Overpowered.Lucy.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
                    }
                }
            }
            else
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }
        public static void SpazLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                if (Time.time > lucyDelay)
                {
                    hgc.timeGongStarted = hgc.timeGongStarted == 0f ? Time.time : 0f;
                    hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
                    hgc.isSummoned = true;
                    lucyDelay = Time.time + 0.1f;
                }
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void AnnoyingLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
            {
                if (Time.time > lucyDelay)
                {
                    hgc.timeGongStarted = Time.time;
                    hgc.grabTime = Time.time;
                    hgc.currentState = hgc.currentState == HalloweenGhostChaser.ChaseState.Gong ? HalloweenGhostChaser.ChaseState.Grabbing : HalloweenGhostChaser.ChaseState.Gong;
                    hgc.targetPlayer = GetRandomPlayer(true);
                    lucyDelay = Time.time + 0.1f;
                }
            }
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void BecomeLucy()
        {
            if (!Networking.IsMasterClient)
            {
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
                return;
            }

            if (Overpowered.Lucy != null)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

                Overpowered.Lucy.transform.position = References.BodyCollider.transform.position;
                Overpowered.Lucy.transform.rotation = References.HeadCollider.transform.rotation;

                Overpowered.Lucy.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                Overpowered.Lucy.targetPlayer = null;
            }
        }

        public static void MoveLucyGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (Overpowered.Lucy.IsMine)
                    Overpowered.Lucy.transform.position = NewPointer.transform.position + Vector3.up;
                else Notifications.SendFormat("red", "ERROR", "You are not master client.");
            });
        }

        public static void FastLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
                hgc.currentSpeed = 10f;
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void SlowLucy()
        {
            HalloweenGhostChaser hgc = Overpowered.Lucy;
            if (hgc.IsMine)
                hgc.currentSpeed = 1f;
            else Notifications.SendFormat("red", "ERROR", "You are not master client.");
        }

        public static void BarrelGun()
        {
            GunLibrary.Aim(GunData =>
            {
                if (GetGunInput(true))
                Projectiles.SendBarrelProjectile(GunData.NewPointer.transform.position + Vector3.up, Vector3.zero, Quaternion.identity);
            });
        }

        public static void SpoofRank(bool enabled, string tier = null)
        {
            RankedPatch.enabled = enabled;
            RankedPatch.targetTier = tier;
        }

        /// <summary>
        /// Spoofs your platform for the Ranked gamemode.
        /// </summary>
        /// <param name="enabled">Spoof Enabled</param>
        /// <param name="target">Target Platform</param>
        public static void SpoofPlatform(bool enabled, string target = null)
        {
            RankedPatch.enabled = enabled;
            RankedPatch.targetPlatform = target;
        }
        public static void SpoofBadge()
        {
            SetRankedPatch.enabled = true;
            if (!Mathf.Approximately(References.LocalRig.currentRankedELO, Safety.targetElo) || References.LocalRig.currentRankedSubTierQuest != Safety.targetBadge || References.LocalRig.currentRankedSubTierPC != Safety.targetBadge)
                References.LocalRig.SetRankedInfo(Safety.targetElo, Safety.targetBadge, Safety.targetBadge);
        }
        public static void PlayAudio(AudioClip sound, bool disableMicrophone = false)
        {
            if (sound == null)
            {
                Logs.LogError("AudioClip is null");
                return;
            }
            if (!Networking.InRoom)
            {
                if (Sound.soundboardAudioManager == null)
                {
                    Sound.soundboardAudioManager = new GameObject("2DAudioMgr");
                    AudioSource temp = Sound.soundboardAudioManager.AddComponent<AudioSource>();
                    temp.spatialBlend = 0f;
                }

                AudioSource ausrc = Sound.soundboardAudioManager.GetComponent<AudioSource>();
                ausrc.volume = 1f;
                ausrc.clip = sound;
                ausrc.loop = false;
                ausrc.Play();

                Sound.AudioIsPlaying = true;
                Sound.RecoverTime = Time.time + sound.length;

                return;
            }

            if (!RecorderPatch.enabled)
            {
                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.AudioClip;
                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.AudioClip = sound;
                ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.RestartRecording(true);
            }
            else if (RecorderPatch.enabled)
            {
                if (Soundboard.LegacySoundboard && Voices.Get().AudioClips.Count == 1)
                    Voices.Get().StopAudioClips();
                Voices.Get().AudioClip(sound, disableMicrophone);
            }

            if (!Soundboard.LoopAudio)
            {
                Sound.AudioIsPlaying = true;
                Sound.RecoverTime = Time.time + sound.length + 0.4f;
            }
        }


        public static void PlayAudio(string file)
        {
            if (Networking.InRoom)
            {
                LoadSoundFromFile(file, clip =>
                {
                    PlayAudio(clip);
                });
            }
        }
        #endregion


        /*
        public static string consoleTyped = "";
        public static int currentModIndex = 0;
        public static int pageNumber = 0;
        public static void ConsoleFrame()
        {
            int halfPoint = Mathf.FloorToInt((System.Console.WindowWidth - 1) / 2f);
            string logoPrefix = "";
            for (int i = 0; i < halfPoint - math.floor(PluginInfo.Logo.Split("\n")[0].Length * 0.5); i++)
                logoPrefix += " ";
            string logo = logoPrefix + PluginInfo.Logo.Replace("\n", "\n" + logoPrefix);
            
            int pageSize = System.Console.WindowHeight - 9 - (logo.Split("\n").Length - 1);
            if (Time.frameCount % 1000 == 0)
                System.Console.Clear();

            string largeNewLine = "";
            for (int i = 0; i < 100; i++)
                largeNewLine += Environment.NewLine;
            
            string modList = "";

            ButtonInfo[] categoryButtonsPre = Buttons.buttons[currentCategoryIndex];
            ButtonInfo[] categoryButtons = new ButtonInfo[categoryButtonsPre.Length + 2];
            int pageCount = (int)Math.Ceiling((double)categoryButtons.Length / pageSize) - 1;
            categoryButtons[0] = new ButtonInfo { buttonText = "Previous Page", method =() => { pageNumber--; if (pageNumber < 0) { pageNumber = pageCount; } }, isTogglable = false, toolTip = "Takes you to the previous page."};
            categoryButtons[1] = new ButtonInfo { buttonText = "Next Page", method =() => { pageNumber++; pageNumber %= pageCount + 1; }, isTogglable = false, toolTip = "Takes you to the previous page."};
            Array.Copy(categoryButtonsPre, 0, categoryButtons, 2, categoryButtonsPre.Length);
            for (int index = 0; index < categoryButtons.Length; index++)
            {
                ButtonInfo mod = categoryButtons[index];
                if (index != 0 && index != 1)
                {
                    int start = 2 + pageNumber * pageSize;
                    int end = start + pageSize;

                    if (index < start || index >= end)
                        continue;
                }
                string modName = NoRichtextTags(mod.overlapText ?? mod.buttonText);
                if (index == currentModIndex + (pageNumber * pageSize) || (index < 2 && index == currentModIndex))
                    modList += Environment.NewLine + "> " + (mod.enabled ? "[E] " : "" ) + modName;
                else
                    modList += Environment.NewLine + "  " + (mod.enabled ? "[E] " : "" ) + modName;
            }

            if (System.Console.KeyAvailable)
            {
                int buttonCount = categoryButtonsPre.Length % pageSize;
                if (pageCount != pageNumber)
                    buttonCount = pageSize;
                
                var key = System.Console.ReadKey(true);
                string stringKey = key.KeyChar.ToString();
                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        consoleTyped = consoleTyped.Length != 0 ? consoleTyped[..^1] : consoleTyped;
                        break;
                    case ConsoleKey.Enter:
                        if (consoleTyped != "")
                        {

                            ButtonInfo selButton = Buttons.GetIndex(consoleTyped);
                            if (selButton == null)
                            {
                                for (int i = 0; i < Buttons.buttons.Length; i++)
                                {
                                    ButtonInfo[] buttonList = Buttons.buttons[i];
                                    foreach (ButtonInfo buttonInfo in buttonList)
                                    {
                                        string text = (buttonInfo.overlapText ?? buttonInfo.buttonText).ToLower();
                                        if (text.Contains(consoleTyped.ToLower()) &&
                                            (selButton == null || i == currentCategoryIndex))
                                            selButton = buttonInfo;
                                    }
                                }
                            }

                            if (selButton != null)
                                Toggle(selButton.buttonText, true);
                            else
                                Notifications.SendFormat("red", "ERROR", $"Mod \"{consoleTyped}\" does not exist.");
                            consoleTyped = "";
                            break;
                        }
                        goto case ConsoleKey.RightArrow;
                    case ConsoleKey.UpArrow:
                        currentModIndex--;
                        if (currentModIndex < 0)
                            currentModIndex = buttonCount + 1;
                        break;
                    case ConsoleKey.DownArrow:
                        currentModIndex++;
                        currentModIndex %= buttonCount + 2;
                        break;
                    case ConsoleKey.RightArrow:
                        ButtonInfo button = categoryButtons[currentModIndex + (pageNumber * pageSize)];
                        if (currentModIndex < 2)
                        {
                            button = categoryButtons[currentModIndex];
                            button.method();
                            break;
                        }
                        int prevCategory = currentCategoryIndex;
                        Toggle(button.buttonText, true);
                        if (prevCategory != currentCategoryIndex)
                        {
                            pageNumber = 0;
                            currentModIndex = 0;
                        }
                        break;
                    default:
                        if (stringKey == " ")
                        {
                            if (key.Key != ConsoleKey.Spacebar)
                                break;
                        }
                        consoleTyped += stringKey;
                        break;
                }
            }
            
            string screenLine = "";
            for (int i = 0; i < System.Console.WindowWidth - 1; i++)
                screenLine += "-";
            
            string infoPrefix = "";
            for (int i = 0; i < halfPoint - 16; i++)
                infoPrefix += " ";
            
            System.Console.WriteLine(
$@"{largeNewLine}
{logo}
{infoPrefix}> Use the arrow keys to navigate.
{screenLine}{modList}
{screenLine}

> {consoleTyped}");
        }*/
    }
}
