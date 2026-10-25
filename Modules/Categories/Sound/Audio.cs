/*
 * Qubit Menu  Modules/Categories/Sound/Audio.cs
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
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using Qubit.Core;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Patches.Voice;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Qubit.Libraries.Controls.Controllers;
using Random = UnityEngine.Random;
using Security = Qubit.Core.Security;

namespace Qubit.Modules.Categories.Sound
{
    public static class Audio
    {
        #region Types
        public class SoundData
        {
            public Voices.Clip Clip;
            public AudioClip AudioClip;
        }
        #endregion

        #region Settings

        public static string Subdirectory = "";

        public static bool AudioIsPlaying;
        public static float RecoverTime = -1f;

        public static GameObject soundboardAudioManager;

        public static readonly Dictionary<string, SoundData> activeSounds = new Dictionary<string, SoundData>();





        public static float sendEffectDelay;

        private static float soundSpamDelay;

        private static bool squeakToggle;

        private static bool sirenToggle;

        public static int soundId;
        #endregion

        #region Methods




        public static string librarySearchQuery = "";











        public static void StopAllSounds()
        {
            if (soundboardAudioManager != null)
            {
                soundboardAudioManager.GetComponent<AudioSource>().Stop();
                AudioIsPlaying = false;
                RecoverTime = -1f;
            }

            foreach (Module[] buttonArray in Soundboard.CachedButtons.Values)
            {
                foreach (Module button in buttonArray)
                {
                    if (button != null && button.enabled)
                        button.SetEnabled(false);
                }
            }

            if (Networking.InRoom)
            {
                if (VoiceRecorder.enabled)
                {
                    if (activeSounds != null)
                    {
                        var keys = new HashSet<string>(activeSounds.Keys);

                        foreach (var row in Manager.modules.Values)
                        {
                            foreach (var button in row)
                            {
                                if (keys.Contains(button.name))
                                    button.SetEnabled(false);
                            }
                        }

                        activeSounds.Clear();
                    }
                    Voices.Get().StopAudioClips();

                    ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = false;
                }
                else
                {
                    ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.Microphone;
                    ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.AudioClip = null;
                    ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.RestartRecording(true);
                    ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = false;
                }
            }

            AudioIsPlaying = false;
            RecoverTime = -1f;
        }













        public static void PlayTag(int id, float volume)
        {
            if (!Networking.IsMasterClient)
                Notifications.SendFormat("red", "ERROR", "You are not master client.");
            else
            {
                if (Time.time > sendEffectDelay)
                {
                    object[] soundSendData = { id, volume, false };
                    object[] sendEventData = { PhotonNetwork.ServerTimestamp, Constants.Network.ROOM_SYSTEM, soundSendData };

                    try
                    {
                        PhotonNetwork.RaiseEvent(3, sendEventData, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendUnreliable);
                    }
                    catch { }
                    Security.RPCProtection();

                    sendEffectDelay = Time.time + 0.2f;
                }
            }
        }
        public static void SoundSpam(int soundId, bool constant = false)
        {
            if (rightGrab || constant)
            {
                if (Time.time > soundSpamDelay)
                    soundSpamDelay = Time.time + 0.1f;
                else
                    return;

                if (Networking.InRoom)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, false, 999999f);
                    Security.RPCProtection();
                }
                else
                    References.LocalRig.PlayHandTapLocal(soundId, false, 999999f);
            }
        }

        public static void JmancurlySoundSpam() =>
            SoundSpam(Random.Range(336, 338));

        public static void RandomSoundSpam() =>
            SoundSpam(Random.Range(0, References.Player.materialData.Count));

        public static void CrystalSoundSpam()
        {
            int[] sounds = {
                Random.Range(40,54),
                Random.Range(214,221)
            };
            SoundSpam(sounds[Random.Range(0, 1)]);
        }
        public static void SqueakSoundSpam()
        {
            if (Time.time > soundSpamDelay)
                squeakToggle = !squeakToggle;

            SoundSpam(squeakToggle ? 75 : 76);
        }
        public static void SirenSoundSpam()
        {
            if (Time.time > soundSpamDelay)
                sirenToggle = !sirenToggle;

            SoundSpam(sirenToggle ? 48 : 50);
        }

        public static void CustomSoundSpam() => SoundSpam(soundId);

        [RunOnLaunch(LaunchPriority.Sound)]
        public static void Initialize()
        {
            try
            {
                Soundboard.LoadSoundboard(false);
            }
            catch (Exception exc)
            {
                Logs.LogError(
                $"Error with Sound.LoadSoundboard() at {exc.StackTrace}: {exc.Message}");
            }
        }
        #endregion
    }
}
