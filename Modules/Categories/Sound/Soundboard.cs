/*
 * Qubit Menu  Modules/Categories/Sound/Soundboard.cs
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
using Valve.Newtonsoft.Json.Linq;
using static Qubit.Core.Security;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Libraries.Core.Files;

namespace Qubit.Modules.Categories.Sound
{
    public static class Soundboard
    {
        #region Settings
        public static bool LegacySoundboard = false;
        public static bool LoopAudio = false;
        public static bool OverlapAudio = false;
        public static readonly Dictionary<string, Module[]> CachedButtons = new Dictionary<string, Module[]>();

        public static bool disableLocalSoundboard;
        #endregion

        public static int BindMode;
        private static bool lastBindPressed;
        public static readonly string[] BindModeNames = {
            "None", "A", "B", "X", "Y", "Left Grip", "Right Grip",
            "Left Trigger", "Right Trigger", "Left Joystick", "Right Joystick"
        };
        public static void LoadSoundboard(bool openCategory = true)
        {
            string key = Audio.Subdirectory ?? "";

            if (Soundboard.CachedButtons.TryGetValue(key, out Module[] buttons))
            {
                Manager.modules["Soundboard"] = buttons;

                if (openCategory)
                    Manager.CurrentCategoryName = "Soundboard";

                return;
            }

            string path = Path.Combine(PluginInfo.BaseDirectory, "Sounds", Audio.Subdirectory.TrimStart('/'));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            List<Module> soundButtons = new List<Module>();

            if (Audio.Subdirectory != "")
            {
                soundButtons.Add(new Module
                {
                    name = "Exit Audio.Subdirectory",
                    displayName = "Exit " + Audio.Subdirectory.Split("/")[^1],
                    method = () =>
                    {
                        Audio.Subdirectory = RemoveLastDirectory(Audio.Subdirectory);
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
                            disableMethod = Audio.StopAllSounds,
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
                                disableMethod = Audio.StopAllSounds,
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
                method = Audio.StopAllSounds,
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
                method = () => LoadSoundLibrary(),
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
            Audio.Subdirectory = string.IsNullOrEmpty(Audio.Subdirectory) ? "/" + folder : Audio.Subdirectory.TrimEnd('/') + "/" + folder;

            LoadSoundboard();
        }
        public static int libraryPage = 1;
        public static void LoadSoundLibrary(int page = 1)
        {
            libraryPage = page;

            List<Module> soundbuttons = new List<Module>
            {
                new Module { name = "Exit Sound Library", method = () => LoadSoundboard(), isTogglable = false, toolTip = "Returns you back to the soundboard." },
                new Module
                {
                    name = "Search Sound Library",
                    displayName = string.IsNullOrEmpty(Audio.librarySearchQuery) ? "Search" : $"Search: {Audio.librarySearchQuery}",
                    method = () => PromptSingleText("Search for a sound on MyInstants.", () =>
                    {
                        Audio.librarySearchQuery = keyboardInput;
                        LoadSoundLibrary();
                    }),
                    isTogglable = false,
                    toolTip = "Searches MyInstants for a sound to download."
                }
            };

            try
            {
                string endpoint = string.IsNullOrEmpty(Audio.librarySearchQuery) ? "trending" : "search";
                string url = $"https://myinstants-api.page.gd/{endpoint}?page={page}" + (string.IsNullOrEmpty(Audio.librarySearchQuery) ? "" : $"&q={Uri.EscapeDataString(Audio.librarySearchQuery)}");

                JObject json = JObject.Parse(GetHttp(url));
                if ((int)json["status"] != 200)
                    throw new Exception(json["message"]?.ToString() ?? "Unknown error");

                int index = 0;
                foreach (JObject sound in json["data"].Cast<JObject>())
                {
                    index++;
                    string title = sound["title"].ToString();
                    string mp3 = sound["mp3"].ToString();
                    soundbuttons.Add(new Module { name = "SoundboardDownload" + index, displayName = title, method = () => DownloadSound(title, mp3), isTogglable = false, toolTip = "Downloads " + title + " to your sound library." });
                }

                if (page > 1)
                    soundbuttons.Add(new Module { name = "Previous Sound Library Page", method = () => LoadSoundLibrary(page - 1), isTogglable = false, toolTip = "Loads the previous page of sounds." });

                if ((bool?)json["has_next"] == true)
                    soundbuttons.Add(new Module { name = "Next Sound Library Page", method = () => LoadSoundLibrary(page + 1), isTogglable = false, toolTip = "Loads the next page of sounds." });
            }
            catch (Exception ex)
            {
                Notifications.SendFormat("red", "ERROR", "Failed to load the sound library: " + ex.Message);
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

            bool isActive = Audio.activeSounds.ContainsKey(hash);
            string hashedName = soundName.Hash();

            void Play()
            {
                if (Soundboard.OverlapAudio)
                {
                    Fun.PlayAudio(soundPath);
                    playButton.SetEnabled(false);
                    return;
                }

                if (!Audio.activeSounds.ContainsKey(hash))
                {
                    playButton.displayName = "Stop";
                    PlaySoundboardSound(soundPath, hash, Soundboard.LoopAudio, BindMode > 0);

                    System.Collections.IEnumerator Reload()
                    {
                        while (!Audio.activeSounds.ContainsKey(hash))
                            yield return null;

                        LoadSoundProperties(soundName, soundPath, hash);
                        ReloadMenu();
                    }

                    Coroutines.instance.StartCoroutine(Reload());
                }
            }

            void Stop()
            {
                if (Audio.activeSounds.ContainsKey(hash))
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
                        durationButton.displayName = Audio.activeSounds.TryGetValue(hash, out var s) && s.Clip != null
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

            if (Audio.activeSounds.TryGetValue(hash, out var sound) && sound.Clip != null)
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
                    displayName = $"Skip {Strings.Bracket("green", $"{skipAmount:0.0}s")}",
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
                        skipButton.displayName = $"Skip Amount {Strings.Bracket("green", $"{skipAmount:0.0}s")}";
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

            string filename = Path.Combine("Sounds", Audio.Subdirectory.TrimStart('/'), $"{name}.{GetFileExtension(url)}");
            if (File.Exists($"{PluginInfo.BaseDirectory}/{filename}"))
                File.Delete($"{PluginInfo.BaseDirectory}/{filename}");

            audioFilePool.Remove(name);

            LoadSoundFromURL(url, filename, clip =>
            {
                if (clip.length < 20f)
                    Play2DAudio(clip);
            });

            Soundboard.CachedButtons.Remove(Audio.Subdirectory ?? "");

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

                if (!Audio.activeSounds.ContainsKey(hash))
                {
                    if (VoiceRecorder.enabled && Networking.InRoom)
                    {
                        var id = Voices.Get().AudioClip(clip, false);
                        Audio.activeSounds[hash] = new Audio.SoundData { Clip = id, AudioClip = clip };
                    }
                }

                var clips = Voices.Get().AudioClips;
                var keys = Audio.activeSounds.Keys.ToList();
            }

            if (file is string filePath)
                LoadSoundFromFile(filePath, Play);
            else if (file is AudioClip audioClip)
                Play(audioClip);
        }
        public static void StopSoundboardSound(string hash)
        {
            if (Audio.activeSounds.Any())
            {
                if (Audio.activeSounds.ContainsKey(hash))
                {
                    if (VoiceRecorder.enabled)
                        Voices.Get().StopAudioClip(Audio.activeSounds[hash].Clip);
                    Audio.activeSounds.Remove(hash);
                }
            }
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
                    Microphone.FixMicrophone();
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
    }
}
