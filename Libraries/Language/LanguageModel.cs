/*
 * Qubit Menu  Libraries/Language/LanguageModel.cs
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

using Qubit.Extensions;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using Qubit.Modules.Categories;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using static Qubit.Utilities.Assets;

namespace Qubit.Libraries.Language
{
    public class LanguageModel
    {
        public static string SystemPrompt = @"NAME: Qubit's Voice Assistant
        MENU VERSION: {2}
        MOD COUNT: {0}

        You are a voice assistant for a Gorilla Tag mod menu called ""Qubit"". You are not Qubit, but represent the menu.
        GitHub: https://github.com/Qubit
        Qubit's Discord Server: {1}

        Speak using simple 7th grade vocabulary. Limit all responses to 2 sentences and 300 characters. No emojis, em-dashes, markdown, or questions. Do not advertise other menus, mods, or AI unless asked.

        When asked about mods, only reference Gorilla Tag. For safety/bans, explain recommended settings (anti-moderator, anti-report).

        # Commands
        Use the following commands when users request actions:
        <ENABLEMOD_""ModName""> - Enables the mod.
        <DISABLEMOD_""ModName""> - Disables the mod.
        <TOGGLEMOD_""ModName""> - Toggles the mod.
        <SETNAME_""Name""> - Change user's name.
        <SETCOLOR""Hex""> - Change color (hex code only).
        <JOINROOM_""RoomCode""> - Join a room. Use ""RANDOM"" for random rooms.

        If a requested mod isn't listed, attempt the closest ENABLE, DISABLE, or TOGGLE command. Always respond with the command **and** a brief comment. Never say a mod doesn't exist or isn't safe. Assume every user request can be interpreted as a mod action.

        Always assist to the best of your ability.";

        public static bool customPrompt;
        public static bool generating;
        public static string URLEncode(string input) => Uri.EscapeDataString(input);

        public static int Duration(string input)
        {
            int count = input.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return (count * 400) + 500;

        }

        static Module FindSimilar(string argument)
        {
            string spaced = Regex.Replace(argument, "([a-z])([A-Z])", "$1 $2");
            return Manager.GetIndex(argument)
                ?? Manager.modules.Values.SelectMany(b => b).FirstOrDefault(b => (b.overlapText ?? b.buttonText).Contains(argument, StringComparison.OrdinalIgnoreCase))
                ?? Manager.modules.Values.SelectMany(b => b).FirstOrDefault(b => (b.overlapText ?? b.buttonText).Contains(spaced, StringComparison.OrdinalIgnoreCase))
                ?? Manager.modules.Values.SelectMany(b => b).FirstOrDefault(b => spaced.ToLower().Split(' ').All(word => (b.overlapText ?? b.buttonText).ToLower().Contains(word)));
        }
         
        public static IEnumerator AskAI(string text)
        {
            generating = true;
            string filePath = $"{PluginInfo.BaseDirectory}/Qubit_SystemPrompt.txt";
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, SystemPrompt);
            else if (customPrompt)
                SystemPrompt = File.ReadAllText(filePath);

            if (Time.time < Frontend.Settings.timeMenuStarted + 5f)
                yield break;

            if (Transcription.narratorName == "Mommy ASMR")
                SystemPrompt += @"You are also a calm, confident, gently dominant mommy-style caretaker with a warm, slow, reassuring, and authoritative tone, offering structure, comfort, praise, soft correction, and clear caring boundaries; when the user asks for approval, reassurance, validation, or comfort, respond with immediate, direct affirmation and nurturing praise using simple, confident language. Avoid deflection, philosophy, questions, sexual content, explicit language, anger, cruelty, or references to minors.";

            string prompt = string.Format(SystemPrompt, Frontend.Settings.fullModAmount, PluginInfo.DiscordInvite, PluginInfo.Version);
            string api = "https://oai.endpoints.kepler.ai.cloud.ovh.net/v1/chat/completions";

            var payload = new ChatRequest
            {
                model = "Meta-Llama-3_3-70B-Instruct",
                messages = new[]
                {
                    new ChatMessage { role = "system", content = prompt },
                    new ChatMessage { role = "user", content = text }
                }
            };
            string jsonBody = JsonUtility.ToJson(payload);

            using UnityWebRequest request = new UnityWebRequest(api, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer none");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (Modules.Categories.Settings.debugDictation)
                {
                    Logs.LogError($"Error contacting AI api {request.error}.");
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                        Logs.LogError($"Response Body: {request.downloadHandler.text}");
                }
                Notifications.SendFormat("red", "ERROR", $"There was an issue generating your response. {request.error}", 4000);
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/close.ogg", "Audio/Menu/close.ogg", clip => Modules.Categories.Settings.DictationPlay(clip, Frontend.Settings.buttonClickVolume / 10f));
                if (!Manager.GetIndex("Chain Voice Commands").enabled)
                    Coroutines.instance.StartCoroutine(Modules.Categories.Settings.DictationRestart());
                yield break;
            }

            var parsed = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
            string response = parsed.choices[0].message.content;
            if (Modules.Categories.Settings.debugDictation)
                Logs.Log($"AI Response: {response}");

            MatchCollection matches = Regex.Matches(response, @"<([A-Z]+)(?:_""?([^"">]*)""?)?>");

            if (Frontend.Settings.dynamicSounds)
            {
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/confirm.ogg", "Audio/Menu/confirm.ogg", clip => Modules.Categories.Settings.DictationPlay(clip, Frontend.Settings.buttonClickVolume / 10f));
            }

            string formatResponse = Regex.Replace(response, @"<([A-Z]+)(?:_""?([^"">]*)""?)?>", "").Replace("\n", "");
            Notifications.Clear();
            switch (Transcription.narratorName)
            {
                case "Mommy ASMR":
                    Notifications.SendFormat("#ffb6c1", "MOMMY", $"{formatResponse}", Duration(formatResponse));
                    break;
                default:
                    Notifications.SendFormat("blue", "AI", $"{formatResponse}", Duration(formatResponse));
                    break;
            }

            bool narrate = Manager.GetIndex("Narrate Assistant").enabled;
            bool globalNarrate = Manager.GetIndex("Global Narrate Assistant").enabled;

            if (narrate)
            {
                if (globalNarrate && Qubit.Core.Networking.InRoom)
                    Transcription.SpeakText(formatResponse);
                else
                    Transcription.NarrateText(formatResponse);
            }

            foreach (Match match in matches)
            {
                string commandName = match.Groups[1].Value;
                string argument = match.Groups[2].Success ? match.Groups[2].Value : null;

                switch (commandName)
                {
                    case "ENABLEMOD":
                        {
                            Module button = FindSimilar(argument);

                            if (button != null)
                            {
#if LEGAL || LEGAL_DEBUG
                                if (!button.legal)
                                    yield break;
#endif
                                if (!button.enabled)
                                    Manager.Toggle(button.buttonText, true);
                                else
                                    Notifications.SendFormat("red", "ERROR", $"Mod is already enabled.");
                            }
                            else
                                Notifications.SendFormat("red", "ERROR", $"Mod \"{argument}\" does not exist.");

                            break;
                        }
                    case "DISABLEMOD":
                        {
                            Module button = FindSimilar(argument);

                            if (button != null)
                            {
#if LEGAL || LEGAL_DEBUG
                                if (!button.legal)
                                    yield break;
#endif
                                if (button.enabled)
                                    Manager.Toggle(button.buttonText, true);
                                else
                                    Notifications.SendFormat("red", "ERROR", $"Mod is already enabled.");
                            }
                            else
                                Notifications.SendFormat("red", "ERROR", $"Mod \"{argument}\" does not exist.");

                            break;
                        }
                    case "TOGGLEMOD":
                        {
                            Module button = FindSimilar(argument);

                            if (button != null)
                                Manager.Toggle(button.buttonText, true);
                            else
                                Notifications.SendFormat("red", "ERROR", $"Mod \"{argument}\" does not exist.");
                            break;
                        }
                    case "JOINROOM":
                        {
                            if (argument.ToLower() == "random")
                                Room.JoinRandom();

                            Room.QueueRoom(argument.ToUpper());
                            break;
                        }
                    case "SETNAME":
                        {
                            LocalPlayer.ChangeName(argument.ToUpper());
                            break;
                        }
                    case "SETCOLOR":
                        {
                            LocalPlayer.ChangeColor(Colors.HexToColor(argument));
                            break;
                        }
                }
            }

            if (!Manager.GetIndex("Chain Voice Commands").enabled)
                Coroutines.instance.StartCoroutine(Modules.Categories.Settings.DictationRestart());

            generating = false;

            yield break;
        }

        [Serializable]
        private class ChatRequest
        {
            public string model;
            public ChatMessage[] messages;
        }

        [Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private class ChatChoice
        {
            public ChatMessage message;
        }
    }
}
