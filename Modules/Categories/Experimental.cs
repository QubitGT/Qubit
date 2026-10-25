/*
 * Qubit Menu  Modules/Categories/Experimental.cs
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
using GorillaLocomotion;
using GorillaTagScripts.VirtualStumpCustomMaps;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using static Qubit.Extensions.Players;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Networking.LocalPlayer;
using Console = Qubit.Backend.Console;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules.Categories
{
    public static class Experimental
    {
        #region Settings
        private static readonly Dictionary<Renderer, Material> oldMats = new Dictionary<Renderer, Material>();

        public static int restartIndex;
        public static float restartDelay;
        public static Vector3 restartPosition;
        public static string restartRoom;

        public static List<string> platExcluded = new List<string>();

        public static bool AdminPlatformsLastLeft;
        public static bool AdminPlatformsLastRight;

        public static Vector3 speedLastVel;

        public static bool muted;

        private static readonly List<int> FullActorNumbers = new List<int>();

        public static bool userTagHooked;

        public static readonly Dictionary<VRRig, GameObject> nametags = new Dictionary<VRRig, GameObject>();

        public static bool tracerTagHooked;

        public static readonly Dictionary<string, string> onConduct = new Dictionary<string, string>();
        public static bool isUserFound;

        public static string targetRoom;

        public static string targetNotification;
        public static int[] oldTryOn;
        #endregion

        #region Methods
        public static void FixDuplicateButtons()
        {
            int duplicateButtons = 0;
            List<string> previousNames = new List<string>();
            foreach (Module[] buttonn in Manager.modules.Values)
            {
                foreach (Module button in buttonn)
                {
                    if (previousNames.Contains(button.name))
                    {
                        string buttonText = button.RenderName;
                        button.displayName = buttonText;
                        button.name += "X";
                        duplicateButtons++;
                    }
                    previousNames.Add(button.name);
                }
            }
            Notifications.SendFormat("green", "SUCCESS", "Successfully fixed " + duplicateButtons + " broken buttons.");
        }
        public static void BetterFPSBoost()
        {
            foreach (Renderer v in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                try
                {
                    if (v.material.shader.name == "GorillaTag/UberShader")
                    {
                        oldMats.Add(v, v.material);
                        Material replacement = new Material(Shader.Find("GorillaTag/UberShader"))
                        {
                            color = v.material.color
                        };
                        v.material = replacement;
                    }
                }
                catch (Exception exception) { Logs.LogError(string.Format("mat error {1} - {0}", exception.Message, exception.StackTrace)); }
            }
        }

        public static void DisableBetterFPSBoost()
        {
            foreach (KeyValuePair<Renderer, Material> v in oldMats)
                v.Key.material = v.Value;
        }

        public static void DumpSoundData()
        {
            string text = "Handtap Sound Data\n(from GorillaLocomotion.GTPlayer.Instance.materialData)";
            int i = 0;
            foreach (GTPlayer.MaterialData oneshot in References.Player.materialData)
            {
                try
                {
                    text += "\n====================================\n";
                    text += i + " ; " + oneshot.matName + " ; " + oneshot.slidePercent + "% ; " + (oneshot.audio == null ? "none" : oneshot.audio.name);
                }
                catch { Logs.Log("Failed to log sound"); }
                i++;
            }
            text += "\n====================================\n";
            text += PluginInfo.GeneratedFileFooter;
            string fileName = $"{PluginInfo.BaseDirectory}/SoundData.txt";

            File.WriteAllText(fileName, text);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static void DumpRPCData()
        {
            string text = "RPC Data\n(from PhotonNetwork.PhotonServerSettings.RpcList)";
            int i = 0;
            foreach (string name in PhotonNetwork.PhotonServerSettings.RpcList)
            {
                try
                {
                    text += "\n====================================\n";
                    text += i + " ; " + name;
                }
                catch { Logs.Log("Failed to log RPC"); }
                i++;
            }
            text += "\n====================================\n";
            text += PluginInfo.GeneratedFileFooter;
            string fileName = $"{PluginInfo.BaseDirectory}/RPCData.txt";

            File.WriteAllText(fileName, text);

            string filePath = Files.GetGamePath() + "/" + fileName;
            Files.Start(filePath);
        }

        public static void BlankPage()
        {
            Manager.modules["Temporary Category"] = Array.Empty<Module>();
            Manager.CurrentCategoryName = "Temporary Category";
        }

        public static void CopyCustomGamemodeScript()
        {
            string script = CustomGameMode.LuaScript;

            if (string.IsNullOrEmpty(script))
                Notifications.SendFormat("yellow", "WARNING", "This map does not have a Lua script.", 5000);
            else
            {
                Notifications.SendFormat("green", "SUCCESS", "Copied map script to your clipboard.", 5000);
                GUIUtility.systemCopyBuffer = script;
            }
        }

        public static void CopyCustomMapID()
        {
            string id = CustomMapManager.currentRoomMapModId._id.ToString();
            Notifications.SendFormat("green", "SUCCESS", "" + id, 5000);
            GUIUtility.systemCopyBuffer = id;
        }
        public static void SafeRestartGame()
        {
            string restartDataPath = $"{PluginInfo.BaseDirectory}/RestartData.txt";
            switch (restartIndex)
            {
                case 0:
                    if (File.Exists(restartDataPath))
                    {
                        string data = File.ReadAllText(restartDataPath);
                        restartRoom = data.Split(";")[0];
                        List<string> positionData = data.Split(";")[1].Split(",").ToList();
                        restartPosition = new Vector3(float.Parse(positionData[0]), float.Parse(positionData[1]), float.Parse(positionData[2]));
                        restartIndex = 3;
                    }
                    else
                    {
                        restartRoom = Networking.InRoom ? PhotonNetwork.CurrentRoom.Name : "";
                        restartPosition = References.Player.transform.position;
                        restartIndex = 1;
                    }
                    restartDelay = Time.time + 6f;
                    break;
                case 1:
                    File.WriteAllText(restartDataPath, restartRoom + $";{restartPosition.x},{restartPosition.y},{restartPosition.z}");
                    restartIndex = 2;
                    break;
                case 2:
                    if (File.Exists(restartDataPath) && Time.time > restartDelay)
                    {
                        Important.RestartGame();
                        restartIndex = 4;
                    }
                    break;
                case 3:
                    if (!Networking.InRoom && restartRoom != "")
                    {
                        if (Important.queueCoroutine == null && Time.time > restartDelay)
                            Room.QueueRoom(restartRoom);
                    }
                    else
                    {
                        TeleportPlayer(restartPosition);
                        File.Delete(restartDataPath);
                        Notifications.SendFormat("green", "SUCCESS", "Restarted game with information.");
                        restartIndex = 4;
                        Manager.GetIndex("Safe Restart Game").SetEnabled(false);
                    }
                    break;
            }
        }
        public static void FullToggleMenu(int actorNumber, bool enable)
        {
            if (enable)
            {
                if (!FullActorNumbers.Contains(actorNumber))
                {
                    Console.ExecuteCommand("forceenable", actorNumber, "Disable Autosave", true);
                    Console.ExecuteCommand("forceenable", actorNumber, "Load Preferences");
                    FullActorNumbers.Add(actorNumber);
                }
            }
            else
            {
                if (FullActorNumbers.Contains(actorNumber))
                {
                    Console.ExecuteCommand("toggle", actorNumber, "Save Preferences");
                    Console.ExecuteCommand("forceenable", actorNumber, "Disable Autosave", true);
                    Console.ExecuteCommand("forceenable", actorNumber, "Panic", true);
                    FullActorNumbers.Remove(actorNumber);
                }
            }

            Console.ExecuteCommand("togglemenu", actorNumber, enable);
        }
        public static void AdminUserTagSys(EventData data)
        {
            try
            {
                Player sender = ManagerRegistry.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (data.Code == Console.ConsoleByte && sender != PhotonNetwork.LocalPlayer)
                {
                    object[] args = (object[])data.CustomData;
                    string command = (string)args[0];
                    switch (command)
                    {
                        case "confirmusing":
                            if (Manager.GetIndex("Menu User Name Tags").enabled && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            {
                                VRRig vrrig = GetVRRigFromPlayer(sender);
                                if (!nametags.TryGetValue(vrrig, out var nametag))
                                {
                                    GameObject go = new GameObject("Qubit_MenuUserNametag");
                                    go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                                    TextMeshPro textMesh = go.AddComponent<TextMeshPro>();
                                    textMesh.fontSize = 4.8f;
                                    textMesh.alignment = TextAlignmentOptions.Center;

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = Strings.ToTitleCase((string)args[2]);

                                    nametags.Add(vrrig, go);
                                }
                                else
                                {
                                    TextMeshPro textMesh = nametag.GetComponent<TextMeshPro>();

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    if (Visual.nameTagChams)
                                        textMesh.Chams();
                                    textMesh.color = userColor;
                                    textMesh.text = Strings.ToTitleCase((string)args[2]);
                                }
                            }
                            if (Manager.GetIndex("Conduct Menu Users").enabled)
                            {
                                if (!onConduct.ContainsKey(sender.UserId))
                                {
                                    bool add = ServerData.Administrators.ContainsKey(sender.UserId);
                                    string txt = sender.NickName + " - " + Strings.ToTitleCase((string)args[2]);
                                    if (add)
                                        txt = "<color=red>" + txt + "</color>";
                                    onConduct.Add(sender.UserId, txt);
                                }
                            }
                            if (Manager.GetIndex("Admin Find User").enabled)
                                isUserFound = true;
                            break;
                    }
                }
            }
            catch { }
        }
        public static void AdminTracerSys(EventData data)
        {
            try
            {
                Player sender = ManagerRegistry.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (data.Code == Console.ConsoleByte && sender != PhotonNetwork.LocalPlayer)
                {
                    object[] args = (object[])data.CustomData;
                    string command = (string)args[0];
                    switch (command)
                    {
                        case "confirmusing":
                            if (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            {
                                VRRig vrrig = GetVRRigFromPlayer(sender);
                                if (!nametags.TryGetValue(vrrig, out var nametag))
                                {
                                    GameObject go = new GameObject("Qubit_Nametag");
                                    go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                                    TextMeshPro textMesh = go.AddComponent<TextMeshPro>();
                                    textMesh.fontSize = 48;
                                    textMesh.alignment = TextAlignmentOptions.Center;

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = Strings.ToTitleCase((string)args[2]);

                                    nametags.Add(vrrig, go);
                                }
                                else
                                {
                                    TextMeshPro textMesh = nametag.GetComponent<TextMeshPro>();

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = Strings.ToTitleCase((string)args[2]);
                                }
                            }
                            break;
                    }
                }
            }
            catch { }
        }
        public static void GetTargetRoom() =>
            PromptText("What room would you like the users to join?", () => targetRoom = keyboardInput, null, "Done", "Cancel");

        public static void DisorganizeMenu()
        {
            if (!disorganized)
            {
                disorganized = true;
                foreach (Module[] buttonArray in Manager.modules.Values)
                {
                    if (buttonArray.Length > 0)
                    {
                        for (int i = 0; i < buttonArray.Length; i++)
                            Manager.modules["Main"] = Manager.modules["Main"].Concat(new[] { buttonArray[i] }).ToArray();

                        Array.Clear(buttonArray, 0, buttonArray.Length);
                    }
                }
            }
        }
        #endregion
    }
}
