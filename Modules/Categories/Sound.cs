/*
 * Qubit Menu  Modules/Categories/Sound.cs
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
using Qubit.Extensions;
using Qubit.Frontend.Classes;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Patches.Voice;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Qubit.Core.Security;
using static Qubit.Frontend.Core;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Libraries.Core.Files;
using static Qubit.Modules.Manager;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Categories
{
    public static class Sound
    {
        #region Types
        private class SoundData
        {
            public Voices.Clip Clip;
            public AudioClip AudioClip;
        }
        #endregion

        #region Settings
        public static int BindMode;
        public static string Subdirectory = "";

        public static bool AudioIsPlaying;
        public static float RecoverTime = -1f;

        public static GameObject soundboardAudioManager;

        private static readonly Dictionary<string, SoundData> activeSounds = new Dictionary<string, SoundData>();

        private static bool lastBindPressed;

        public static readonly string[] BindModeNames = {
            "None", "A", "B", "X", "Y", "Left Grip", "Right Grip",
            "Left Trigger", "Right Trigger", "Left Joystick", "Right Joystick"
        };

        public static float sendEffectDelay;

        private static float soundSpamDelay;

        private static bool squeakToggle;

        private static bool sirenToggle;

        public static int soundId;
        #endregion

        #region Methods
        public static void LoadSoundboard(bool openCategory = true)
        {
            string key = Subdirectory ?? "";

            if (Soundboard.CachedButtons.TryGetValue(key, out Module[] buttons))
            {
                Manager.modules["Soundboard"] = buttons;

                if (openCategory)
                    Manager.CurrentCategoryName = "Soundboard";

                return;
            }

            string path = Path.Combine(PluginInfo.BaseDirectory, "Sounds", Subdirectory.TrimStart('/'));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            List<Module> soundButtons = new List<Module>();

            if (Subdirectory != "")
            {
                soundButtons.Add(new Module
                {
                    name = "Exit Subdirectory",
                    displayName = "Exit " + Subdirectory.Split("/")[^1],
                    method = () =>
                    {
                        Subdirectory = RemoveLastDirectory(Subdirectory);
                        LoadSoundboard();
                    },
                    isTogglable = false,
                    toolTip = "Returns you back to the last folder."
                });
            }
            else
            {
                soundButtons.Add(new Module
                {
                    name = "Exit Soundboard",
                    method = () => Manager.CurrentCategoryName = "Sound Mods",
                    isTogglable = false,
                    toolTip = "Returns you back to the sound mods."
                });
            }

            string[] folders = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);

            soundButtons.AddRange(
                from folder in folders
                let relativePath = Path.GetRelativePath(path, folder)
                select new Module
                {
                    name = "SoundboardFolder" + relativePath.Hash(),
                    displayName = $"<sprite name=\"Folder\">  {relativePath}  ",
                    method = () => OpenFolder(relativePath),
                    isTogglable = false,
                    toolTip = "Opens the " + relativePath + " folder."
                });

            if (!VoiceRecorder.enabled)
                Notifications.SendFormat("red", "WARNING", $"You are using the legacy microphone system. Modern soundboard features will not be implemented.");

            foreach (string file in files)
            {
                string fileName = Path.GetRelativePath(path, file);
                string soundName = RemoveFileExtension(fileName).Replace("_", " ");
                string soundPath = Path.GetRelativePath(PluginInfo.BaseDirectory, file).Replace("\\", "/");

                string soundKey = "SoundboardSound" + soundName.Hash();

                if (!VoiceRecorder.enabled || Soundboard.LegacySoundboard)
                {
                    if (BindMode > 0)
                    {
                        soundButtons.Add(new Module
                        {
                            name = soundKey,
                            displayName = soundName,
                            method = () => PrepareBindAudio(soundPath),
                            disableMethod = StopAllSounds,
                            toolTip = $"Plays {soundName} through your microphone."
                        });
                    }
                    else
                    {
                        if (Soundboard.LoopAudio)
                        {
                            soundButtons.Add(new Module
                            {
                                name = soundKey,
                                displayName = soundName,
                                enableMethod = () => Fun.PlayAudio(soundPath),
                                disableMethod = StopAllSounds,
                                toolTip = $"Plays {soundName} through your microphone."
                            });
                        }
                        else
                        {
                            soundButtons.Add(new Module
                            {
                                name = soundKey,
                                displayName = soundName,
                                method = () => Fun.PlayAudio(soundPath),
                                isTogglable = false,
                                toolTip = $"Plays {soundName} through your microphone."
                            });
                        }
                    }
                }
                else if (VoiceRecorder.enabled)
                {
                    Module buttonInfo = null;
                    buttonInfo = new Module
                    {
                        name = soundKey,
                        displayName = soundName,
                        toolTip = $"Allows you to view {soundName}'s properties.",
                        method = () => LoadSoundProperties(soundName, soundPath, soundName.Hash()),
                        isTogglable = false
                    };

                    soundButtons.Add(buttonInfo);
                }
            }

            soundButtons.Add(new Module
            {
                name = "Stop All Sounds",
                method = StopAllSounds,
                isTogglable = false,
                toolTip = "Stops all currently playing sounds."
            });

            soundButtons.Add(new Module
            {
                name = "Open Sound Folder",
                aliases = new[] { "Open Soundboard Folder" },
                method = OpenSoundFolder,
                isTogglable = false,
                toolTip = "Opens a folder containing all of your sounds."
            });

            soundButtons.Add(new Module
            {
                name = "Reload Sounds",
                method = () =>
                {
                    Soundboard.CachedButtons.Clear();
                    LoadSoundboard();
                },
                isTogglable = false,
                toolTip = "Reloads all of your sounds."
            });

            soundButtons.Add(new Module
            {
                name = "Get More Sounds",
                method = LoadSoundLibrary,
                isTogglable = false,
                toolTip = "Opens a public audio library, where you can download your own sounds."
            });

            Soundboard.CachedButtons[key] = soundButtons.ToArray();
            Manager.modules["Soundboard"] = Soundboard.CachedButtons[key];

            if (openCategory)
                Manager.CurrentCategoryName = "Soundboard";
        }

        public static void OpenFolder(string folder)
        {
            Subdirectory = string.IsNullOrEmpty(Subdirectory) ? "/" + folder : Subdirectory.TrimEnd('/') + "/" + folder;

            LoadSoundboard();
        }

        public static void LoadSoundLibrary()
        {
            string library = GetHttp($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/Soundboard/SoundLibrary.txt");
            string[] audios = AlphabetizeNoSkip(library.Split("\n"));
            List<Module> soundbuttons = new List<Module> { new Module { name = "Exit Sound Library", method = () => LoadSoundboard(), isTogglable = false, toolTip = "Returns you back to the soundboard." } };
            int index = 0;
            foreach (string audio in audios)
            {
                if (audio.Length > 2)
                {
                    index++;
                    string[] Data = audio.Split(";");
                    soundbuttons.Add(new Module { name = "SoundboardDownload" + index, displayName = Data[0], method = () => DownloadSound(Data[0], $"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/Soundboard/Sounds/{Data[1]}"), isTogglable = false, toolTip = "Downloads " + Data[0] + " to your sound library." });
                }
            }
            Manager.modules["Sound Library"] = soundbuttons.ToArray();
            Manager.CurrentCategoryName = "Sound Library";
        }

        public static void LoadSoundProperties(string soundName, string soundPath, string hash)
        {
            Module playButton = null;
            Module pauseButton = null;
            Module durationButton = null;
            Module volumeButton = null;
            Module speedButton = null;

            float skipAmount = 1f;
            Module skipAmountButton = null;
            Module skipButton = null;

            bool isActive = activeSounds.ContainsKey(hash);
            string hashedName = soundName.Hash();

            void Play()
            {
                if (Soundboard.OverlapAudio)
                {
                    Fun.PlayAudio(soundPath);
                    playButton.SetEnabled(false);
                    return;
                }

                if (!activeSounds.ContainsKey(hash))
                {
                    playButton.displayName = "Stop";
                    PlaySoundboardSound(soundPath, hash, Soundboard.LoopAudio, BindMode > 0);

                    System.Collections.IEnumerator Reload()
                    {
                        while (!activeSounds.ContainsKey(hash))
                            yield return null;

                        LoadSoundProperties(soundName, soundPath, hash);
                        ReloadMenu();
                    }

                    Coroutines.instance.StartCoroutine(Reload());
                }
            }

            void Stop()
            {
                if (activeSounds.ContainsKey(hash))
                {
                    playButton.displayName = "Play";
                    StopSoundboardSound(hash);
                    LoadSoundProperties(soundName, soundPath, hash);
                    ReloadMenu();
                }
            }

            string FormatDuration(double totalSeconds)
            {
                if (totalSeconds < 0) totalSeconds = 0;

                int days = (int)(totalSeconds / 86400);
                int hours = (int)((totalSeconds % 86400) / 3600);
                int minutes = (int)((totalSeconds % 3600) / 60);
                int seconds = (int)(totalSeconds % 60);

                List<string> parts = new List<string>();

                if (days > 0) parts.Add($"{days} day{(days == 1 ? "" : "s")}");
                if (hours > 0) parts.Add($"{hours} hour{(hours == 1 ? "" : "s")}");
                if (minutes > 0) parts.Add($"{minutes} minute{(minutes == 1 ? "" : "s")}");
                if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds} second{(seconds == 1 ? "" : "s")}");

                return string.Join(" ", parts);
            }

            playButton = new Module
            {
                name = $"Play or Pause SoundboardSound {hashedName}",
                toolTip = $"Plays or pauses the sound {soundName}.",
                displayName = isActive ? "Stop" : "Play",
                enableMethod = Play,
                disableMethod = Stop,
                enabled = isActive
            };

            durationButton = new Module
            {
                label = true,
                name = $"SoundboardSound {hashedName}'s Duration",
                displayName = $"Duration: Loading..",
                method = () =>
                {
                    try
                    {
                        durationButton.displayName = activeSounds.TryGetValue(hash, out var s) && s.Clip != null
                            ? $"Duration: {FormatDuration(s.Clip.CurrentTime)}"
                            : "Duration: N/A";
                    }
                    catch { }
                }
            };

            var list = new List<Module>
            {
                new Module
                {
                    name = $"Exit {soundName}'s Properties",
                    method = () => LoadSoundboard(),
                    isTogglable = false,
                    toolTip = "Returns you back to the soundboard."
                },
                playButton
            };

            if (activeSounds.TryGetValue(hash, out var sound) && sound.Clip != null)
            {
                pauseButton = new Module
                {
                    name = $"Pause SoundboardSound {hashedName}",
                    displayName = "Pause",
                    enableMethod = () =>
                    {
                        sound.Clip.Pause();
                        pauseButton.displayName = "Resume";
                    },
                    disableMethod = () =>
                    {
                        sound.Clip.Resume();
                        pauseButton.displayName = "Pause";
                    },
                    toolTip = $"Pauses or resumes the sound."
                };
                list.Add(pauseButton);
                list.Add(new Module
                {
                    name = $"Loop SoundboardSound {hashedName}",
                    displayName = "Loop",
                    enableMethod = () => sound.Clip.Looping = true,
                    disableMethod = () => sound.Clip.Looping = false,
                    toolTip = "Makes the song loop when it ends."
                });

                skipButton = new Module
                {
                    name = $"Skip SoundboardSound {hashedName}",
                    displayName = $"Skip <color=grey>[</color><color=green>{skipAmount:0.0}s</color><color=grey>]</color>",
                    enableMethod = () => sound.Clip.CurrentTime = Mathf.Clamp(sound.Clip.CurrentTime + skipAmount, 0, sound.Clip.Length),
                    disableMethod = () => sound.Clip.CurrentTime = Mathf.Clamp(sound.Clip.CurrentTime - skipAmount, 0, sound.Clip.Length),
                    incremental = true,
                    isTogglable = false,
                    toolTip = "Skips forward or backward in the sound by the skip amount."
                };

                list.Add(skipButton);
                list.Add(durationButton);
                list.Add(new Module
                {
                    label = true,
                    name = $"SoundboardSound {hashedName}'s Length",
                    displayName = $"Length: {FormatDuration(sound.Clip.Length)}"
                });

                volumeButton = ButtonHelper.CreateNumeric(
                    $"Change SoundboardSound {hashedName}'s Volume",
                    min: 0,
                    max: 200,
                    defaultValue: Mathf.RoundToInt(Mathf.Clamp(sound.Clip.Volume, 0, 10) / 0.05f),
                    apply: v => sound.Clip.Volume = v * 0.05f,
                    display: v => (v * 0.05f).ToString("F2"),
                    toolTip: "Changes the volume of the sound. Higher volumes will make the sound louder, while lower volumes will make it quieter.",
                    overlapText: "Change Volume");

                speedButton = ButtonHelper.CreateNumeric(
                    $"Change SoundboardSound {hashedName}'s Speed",
                    min: 0,
                    max: 100,
                    defaultValue: Mathf.RoundToInt(Mathf.Clamp(sound.Clip.Speed, 0, 5) / 0.05f),
                    apply: v => sound.Clip.Speed = v * 0.05f,
                    display: v => (v * 0.05f).ToString("F2"),
                    toolTip: "Changes the speed of the sound. Higher speeds will make the pitch higher, while lower speeds will make it lower.",
                    overlapText: "Change Speed");

                skipAmountButton = ButtonHelper.CreateNumeric(
                    $"Change Skip Amount SoundboardSound {hashedName}",
                    min: 0,
                    max: 60,
                    defaultValue: Mathf.RoundToInt(skipAmount / 0.5f),
                    apply: v =>
                    {
                        skipAmount = v * 0.5f;
                        skipButton.displayName = $"Skip Amount <color=grey>[</color><color=green>{skipAmount:0.0}s</color><color=grey>]</color>";
                    },
                    display: v => $"{v * 0.5f:0.0}s",
                    persist: false,
                    toolTip: "Changes how much time is skipped when you press the skip button.",
                    overlapText: "Skip Amount");

                list.Add(skipAmountButton);
                list.Add(volumeButton);
                list.Add(speedButton);
            }

            Manager.modules["Sound Properties"] = list.ToArray();
            Manager.CurrentCategoryName = "Sound Properties";
        }

        public static void DownloadSound(string name, string url)
        {
            if (name.Contains(".."))
                name = name.Replace("..", "");

            if (name.Contains(":"))
                return;

            string filename = Path.Combine("Sounds", Subdirectory.TrimStart('/'), $"{name}.{GetFileExtension(url)}");
            if (File.Exists($"{PluginInfo.BaseDirectory}/{filename}"))
                File.Delete($"{PluginInfo.BaseDirectory}/{filename}");

            audioFilePool.Remove(name);

            LoadSoundFromURL(url, filename, clip =>
            {
                if (clip.length < 20f)
                    Play2DAudio(clip);
            });

            Soundboard.CachedButtons.Remove(Subdirectory ?? "");

            Notifications.SendFormat("green", "SUCCESS", "Successfully downloaded " + name + " to the soundboard.");
        }

        public static void PlaySoundboardSound(object file, string hash, bool loopAudio, bool bind)
        {
            bool[] bindings = {
                rightPrimary,
                rightSecondary,
                leftPrimary,
                leftSecondary,
                leftGrab,
                rightGrab,
                leftTrigger > 0.5f,
                rightTrigger > 0.5f,
                leftJoystickClick,
                rightJoystickClick
            };

            bool shouldPlay = true;
            if (bind && BindMode > 0)
            {
                bool bindPressed = bindings[BindMode - 1];
                shouldPlay = bindPressed && !lastBindPressed;
                lastBindPressed = bindPressed;
            }

            if (!shouldPlay)
                return;

            void Play(AudioClip clip)
            {
                if (clip == null)
                    return;

                if (!activeSounds.ContainsKey(hash))
                {
                    if (VoiceRecorder.enabled && Networking.InRoom)
                    {
                        var id = Voices.Get().AudioClip(clip, false);
                        activeSounds[hash] = new SoundData { Clip = id, AudioClip = clip };
                    }
                }

                var clips = Voices.Get().AudioClips;
                var keys = activeSounds.Keys.ToList();
            }

            if (file is string filePath)
                LoadSoundFromFile(filePath, Play);
            else if (file is AudioClip audioClip)
                Play(audioClip);
        }
        public static void StopSoundboardSound(string hash)
        {
            if (activeSounds.Any())
            {
                if (activeSounds.ContainsKey(hash))
                {
                    if (VoiceRecorder.enabled)
                        Voices.Get().StopAudioClip(activeSounds[hash].Clip);
                    activeSounds.Remove(hash);
                }
            }
        }

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

        public static void FixMicrophone()
        {
            if (!Networking.InRoom)
                return;
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
            }
            else
            {

            }
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.Microphone;
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.AudioClip = null;
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.RestartRecording(true);
            ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.DebugEchoMode = false;
        }
        public static void PrepareBindAudio(string file)
        {
            bool[] bindings = {
                rightPrimary,
                rightSecondary,
                leftPrimary,
                leftSecondary,
                leftGrab,
                rightGrab,
                leftTrigger > 0.5f,
                rightTrigger > 0.5f,
                leftJoystickClick,
                rightJoystickClick
            };

            bool bindPressed = bindings[BindMode - 1];
            if (bindPressed && !lastBindPressed)
            {
                if (ManagerRegistry.NetworkSystem.VoiceConnection.PrimaryRecorder.SourceType == Recorder.InputSourceType.AudioClip)
                    FixMicrophone();
                else
                    Fun.PlayAudio(file);
            }
            lastBindPressed = bindPressed;
        }

        public static void OpenSoundFolder()
        {
            string filePath = GetGamePath() + $"/{PluginInfo.BaseDirectory}/Sounds";
            Files.Start(filePath);
        }
        public static void ApplySoundBindings(int index) => BindMode = index;
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
                    RPCProtection();

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
                    RPCProtection();
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
                LoadSoundboard(false);
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
