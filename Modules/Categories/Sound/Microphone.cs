/*
 * Qubit Menu  Modules/Categories/Sound/Microphone.cs
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

using GorillaExtensions;
using Photon.Voice;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using POpusCodec.Enums;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using Qubit.Patches.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows.Speech;
using static Qubit.Extensions.Players;
using static Qubit.Frontend.Prompts;
using static Qubit.Libraries.Audio.Transcription;
using static Qubit.Modules.Classes.GunLibrary;
using Object = UnityEngine.Object;

namespace Qubit.Modules.Categories.Sound
{
    public static class Microphone
    {
        public static void SelfMute() => References.LocalRig.voiceAudio.mute = true;
        public static void DisableSelfMute() => References.LocalRig.voiceAudio.mute = false;
        public static void FixMicrophone()
        {
            if (!Networking.InRoom)
                return;
            if (VoiceRecorder.enabled)
            {
                if (Audio.activeSounds != null)
                {
                    var keys = new HashSet<string>(Audio.activeSounds.Keys);

                    foreach (var row in Manager.modules.Values)
                    {
                        foreach (var button in row)
                        {
                            if (keys.Contains(button.name))
                                button.SetEnabled(false);
                        }
                    }

                    Audio.activeSounds.Clear();
                }
                Voices.Get().StopAudioClips();
            }
            else
            {
            }
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.Microphone;
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.AudioClip = null;
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.RestartRecording(true);
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = false;
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

            if (VoiceRecorder.enabled)
                Voices.Get().SamplingRate = samplingRate;

            Coroutines.instance.StartCoroutine(DelayReloadMicrophone());
        }
        public static void SetMicrophoneAmplification(float gain)
        {
            if (!Networking.InRoom)
                return;

            if (VoiceRecorder.enabled)
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
        public static void MuteMicrophone(bool mute)
        {
            if (VoiceRecorder.enabled)
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
                        if (VoiceRecorder.enabled)
                        {
                            VoiceSpeaker.enabled = true;
                            VoiceSpeaker.targetSpeaker = lockTarget.gameObject.GetComponent<GorillaSpeakerLoudness>().speaker;
                            if (!Voices.Get().PostProcessors.ContainsKey("CopyVoice"))
                            {
                                float readPos = 0f;

                                Voices.Get().PostProcessors["CopyVoice"] = buffer =>
                                {
                                    var vm = Voices.Get();
                                    int channels = vm.Channels;

                                    float sourceRate = (VoiceSpeaker.targetSpeaker != null && VoiceSpeaker.targetSpeaker.RemoteVoiceLink.Info.SamplingRate > 0)
                                                       ? VoiceSpeaker.targetSpeaker.RemoteVoiceLink.Info.SamplingRate
                                                       : 24000;

                                    float ratio = sourceRate / vm.OutputRate;

                                    lock (VoiceSpeaker.locked)
                                    {
                                        if (VoiceSpeaker.SampleQueue.Count < (sourceRate * 0.04f))
                                        {
                                            Array.Clear(buffer, 0, buffer.Length);
                                            return;
                                        }

                                        for (int i = 0; i < buffer.Length; i += channels)
                                        {
                                            int idxA = (int)readPos;
                                            int idxB = idxA + 1;

                                            if (idxB < VoiceSpeaker.SampleQueue.Count)
                                            {
                                                float t = readPos - idxA;
                                                float sample = Mathf.Lerp(VoiceSpeaker.SampleQueue[idxA], VoiceSpeaker.SampleQueue[idxB], t);

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
                                            VoiceSpeaker.SampleQueue.RemoveRange(0, Math.Min(consumed, VoiceSpeaker.SampleQueue.Count));
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

                                VoiceSpeaker.enabled = true;

                                VoiceSpeaker.targetSpeaker = lockTarget.gameObject.GetComponent<GorillaSpeakerLoudness>().speaker;

                                VoiceRecorder.enabled = !Manager.GetIndex("Legacy Microphone").enabled;

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
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                        gunLocked = false;
                    if (factory != null || Voices.Get().PostProcessors.ContainsKey("CopyVoice"))
                        DisableCopyVoice();
                }
            });
        }
        public static void DisableCopyVoice()
        {
            factory?.Dispose();

            Voices.Get().PostProcessors.Remove("CopyVoice");

            VoiceSpeaker.enabled = false;

            FixMicrophone();

            VoiceRecorder.enabled = !Manager.GetIndex("Legacy Microphone").enabled;

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
        public static void ReloadMicrophone()
        {
            Recorder recorder = ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder;
            if (!VoiceRecorder.enabled && recorder.SourceType != Recorder.InputSourceType.Microphone)
                recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.RestartRecording(true);
            if (VoiceRecorder.enabled)
                Voices.Get().RestartMicrophone();
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
        public static LoopbackFactory factory;
        private static float copyVoiceGunDelay;
        public static DictationRecognizer drec;
        public static IEnumerator DelayReloadMicrophone()
        {
            yield return new WaitForSeconds(0.25f);
            ReloadMicrophone();
        }

    }
}
