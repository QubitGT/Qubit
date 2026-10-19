/*
 * Qubit Menu  Libraries/Audio/Transcription.cs
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

using Qubit.Libraries.Core;
using Qubit.Modules.Categories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Libraries.Core.Files;

namespace Qubit.Libraries.Audio
{
    public static class Transcription
    {










        public static IEnumerator TranscribeText(string text, Action<AudioClip> onComplete, string customFileName = null, string customPath = null)
        {
            if (Time.time < timeMenuStarted + 5f)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string fileName = !string.IsNullOrEmpty(customPath) ? SanitizeFileName(customFileName) : $"{GetSHA256(text)}{(narratorIndex == 0 ? ".wav" : ".mp3")}";
            string directoryPath = !string.IsNullOrEmpty(customPath) ? customPath : $"{PluginInfo.BaseDirectory}/TTS{(narratorName == "Default" ? "" : narratorName)}";
            string filePath = directoryPath + "/" + fileName;

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(filePath))
            {
                switch (narratorIndex)
                {

                    case 0:
                        {
                            string postData = JsonConvert.SerializeObject(new { text });

                            using UnityWebRequest request = new UnityWebRequest($"{PluginInfo.ServerAPI}/tts", "POST");
                            byte[] raw = Encoding.UTF8.GetBytes(postData);

                            request.uploadHandler = new UploadHandlerRaw(raw);
                            request.SetRequestHeader("Content-Type", "application/json");
                            request.downloadHandler = new DownloadHandlerBuffer();

                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Logs.LogError("Error downloading TTS: " + request.error);
                                onComplete?.Invoke(null);
                                yield break;
                            }

                            byte[] response = request.downloadHandler.data;
                            File.WriteAllBytes(filePath, response);

                            break;
                        }


                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        {
                            if (text.Length > 550)
                                text = text[..550];

                            using UnityWebRequest request = new UnityWebRequest("https://lazypy.ro/tts/request_tts.php?service=Streamlabs&voice=" + narratorName + "&text=" + UnityWebRequest.EscapeURL(text), "POST");
                            request.downloadHandler = new DownloadHandlerBuffer();
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Logs.LogError("Error getting TTS: " + request.error);
                                onComplete?.Invoke(null);
                                yield break;
                            }

                            string jsonResponse = request.downloadHandler.text;
                            var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                            using UnityWebRequest dataRequest = UnityWebRequest.Get(responseData["audio_url"].ToString().Replace("\\", ""));
                            yield return dataRequest.SendWebRequest();

                            if (dataRequest.result != UnityWebRequest.Result.Success)
                                Logs.LogError("Error downloading TTS: " + responseData["audio_url"]);
                            else
                                File.WriteAllBytes(filePath, dataRequest.downloadHandler.data);

                            break;
                        }


                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                        {
                            Dictionary<int, string> voiceCodenames = new Dictionary<int, string>
                            {
                                { 9, "en_us_001" },
                                { 10, "en_female_grandma" },
                                { 11, "en_male_grinch" },
                                { 12, "en_male_ukneighbor" },
                                { 13, "en_us_ghostface" },
                                { 14, "en_female_zombie" },
                                { 15, "en_male_narration" },
                                { 16, "en_male_pirate" },
                                { 17, "en_male_m03_sunshine_soon" },
                                { 18, "en_us_006" },
                                { 19, "en_male_david_gingerman" },
                                { 20, "en_male_chris" },
                                { 21, "en_male_sing_funny_thanksgiving" },
                                { 22, "en_male_santa_effect" }
                            };

                            if (text.Length > 300)
                                text = text[..300];

                            using UnityWebRequest request = new UnityWebRequest("https://lazypy.ro/tts/request_tts.php?service=TikTok&voice=" + voiceCodenames[narratorIndex] + "&text=" + UnityWebRequest.EscapeURL(text), "POST");
                            request.downloadHandler = new DownloadHandlerBuffer();
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Logs.LogError("Error getting TTS: " + request.error);
                                onComplete?.Invoke(null);
                                yield break;
                            }

                            string jsonResponse = request.downloadHandler.text;
                            var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                            using UnityWebRequest dataRequest = UnityWebRequest.Get(responseData["audio_url"].ToString().Replace("\\", ""));
                            yield return dataRequest.SendWebRequest();

                            if (dataRequest.result != UnityWebRequest.Result.Success)
                                Logs.LogError("Error downloading TTS: " + responseData["audio_url"]);
                            else
                                File.WriteAllBytes(filePath, dataRequest.downloadHandler.data);

                            break;
                        }

                    case 23:
                    case 24:
                        {
                            Dictionary<int, string> voiceCodenames = new Dictionary<int, string>
                            {
                                { 23, "en-us" },
                                { 24, "en-gb" }
                            };

                            if (text.Length > 300)
                                text = text[..300];

                            using UnityWebRequest request = new UnityWebRequest("https://lazypy.ro/tts/request_tts.php?service=Google%20Translate&voice=" + voiceCodenames[narratorIndex] + "&text=" + UnityWebRequest.EscapeURL(text), "POST");
                            request.downloadHandler = new DownloadHandlerBuffer();
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Logs.LogError("Error getting TTS: " + request.error);
                                onComplete?.Invoke(null);
                                yield break;
                            }

                            string jsonResponse = request.downloadHandler.text;
                            var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                            using UnityWebRequest dataRequest = UnityWebRequest.Get(responseData["audio_url"].ToString().Replace("\\", ""));
                            yield return dataRequest.SendWebRequest();

                            if (dataRequest.result != UnityWebRequest.Result.Success)
                                Logs.LogError("Error downloading TTS: " + responseData["audio_url"]);
                            else
                                File.WriteAllBytes(filePath, dataRequest.downloadHandler.data);

                            break;
                        }

                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                        {
                            Dictionary<int, string> voiceCodenames = new Dictionary<int, string>
                            {
                                { 25, "Dog" },
                                { 26, "Jerkface" },
                                { 27, "Robot" },
                                { 28, "Vlad" },
                                { 29, "Obama" }
                            };

                            if (text.Length > 300)
                                text = text[..300];

                            using UnityWebRequest request = new UnityWebRequest("https://lazypy.ro/tts/request_tts.php?service=VoiceForge&voice=" + voiceCodenames[narratorIndex] + "&text=" + UnityWebRequest.EscapeURL(text), "POST");
                            request.downloadHandler = new DownloadHandlerBuffer();
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Logs.LogError("Error getting TTS: " + request.error);
                                onComplete?.Invoke(null);
                                yield break;
                            }

                            string jsonResponse = request.downloadHandler.text;
                            var responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);

                            using UnityWebRequest dataRequest = UnityWebRequest.Get(responseData["audio_url"].ToString().Replace("\\", ""));
                            yield return dataRequest.SendWebRequest();

                            if (dataRequest.result != UnityWebRequest.Result.Success)
                                Logs.LogError("Error downloading TTS: " + responseData["audio_url"]);
                            else
                                File.WriteAllBytes(filePath, dataRequest.downloadHandler.data);

                            break;
                        }

                }
            }

            LoadSoundFromFile($"{directoryPath[$"{PluginInfo.BaseDirectory}/".Length..]}/{fileName}", onComplete);
        }

        public static void NarrateText(string text) =>
            Coroutines.instance.StartCoroutine(TranscribeText(text, (audio) => Play2DAudio(audio, buttonClickSound / 10f)));

        /// <summary>
        /// Initiates narration of the specified text by transcribing it to audio and playing it through your microphone.
        /// </summary>
        /// <param name="text">The text to be narrated.</param>
        public static void SpeakText(string text)
        {
            SpeakText(text, false);
        }






        public static void SpeakText(string text, bool disableMicrophone = false) =>
            Coroutines.instance.StartCoroutine(TranscribeText(text, t => Fun.PlayAudio(t, disableMicrophone)));
        public static string narratorName = "Default";
        public static int narratorIndex;
    }
}
