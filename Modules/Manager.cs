/*
 * Qubit Menu  Modules/Manager.cs
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

using Qubit.Patches.Building;
using Qubit.Patches.Economics;
using Qubit.Patches.Events;
using Qubit.Patches.Gadgets;
using Qubit.Patches.Interface;
using Qubit.Patches.Locomotion;
using Qubit.Patches.Netcode;
using Qubit.Patches.Safety.AntiBan;
using Qubit.Patches.Safety.AntiCheat;
using Qubit.Patches.Safety.AntiCrash;
using Qubit.Patches.Safety.Telemetry;
using Qubit.Patches.Weapons;
using Qubit.Patches.Rigs;
using Qubit.Patches.Tagging;
using Qubit.Patches.Terms;
using Qubit.Patches.Voice;
using Qubit.Utilities;
using GorillaLocomotion;
using GorillaTagScripts.ObstacleCourse;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Backend;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend;
using Qubit.Frontend.Classes;
using Qubit.Frontend.Colors;
using Qubit.Frontend.Resources;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Language;
using Qubit.Libraries.Networking;
using Qubit.Modules;
using Qubit.Modules.Categories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using static Qubit.Backend.Preferences;
using static Qubit.Core.Instance;
using static Qubit.Extensions.Colors;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Strings;
using static Qubit.Frontend.Classes.QubitMenuBody;
using static Qubit.Frontend.Core;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Audio.Transcription;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Rooms;
using static Qubit.Modules.Categories.Settings;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Utilities.GameObj;
using Qubit.Libraries.Holdables;
using Qubit.Libraries.Entities;
using Console = Qubit.Backend.Console;
using JoinType = GorillaNetworking.JoinType;
using Random = UnityEngine.Random;
using Room = Qubit.Modules.Categories.Room;
using Security = Qubit.Core.Security;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules
{
    using Critters = Qubit.Libraries.Entities.Critters;
    using Cosmetics = Qubit.Libraries.Entities.Cosmetics;
    [HarmonyPatch(typeof(GTPlayer), nameof(GTPlayer.LateUpdate))]
    public static class Manager
    {
        public static Dictionary<string, Module[]> modules = new Dictionary<string, Module[]>
        {
            ["Main"] = new[]
            {
                new Module { buttonText = "Join Discord", method = Important.JoinDiscord, isTogglable = false, toolTip = "Invites you to join the <b>Qubit</b> Discord server.", legal = true},

                new Module { buttonText = "Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Opens the settings tab.", legal = true},
                new Module { buttonText = "Friends", method =() => CurrentCategoryName = "Friends", isTogglable = false, toolTip = "Opens the friends tab.", legal = true},
                new Module { buttonText = "Players", method = Modules.Categories.Settings.PlayersTab, isTogglable = false, toolTip = "Opens the players tab.", legal = true},
                new Module { buttonText = "Favorite Mods", method =() => CurrentCategoryName = "Favorite Mods", isTogglable = false, toolTip = "Opens your favorite mods. Favorite mods with right grip.", legal = true},
                new Module { buttonText = "Enabled Mods", method =() => CurrentCategoryName = "Enabled Mods", isTogglable = false, toolTip = "Shows all mods you have enabled.", legal = true},
                new Module { buttonText = "Room Mods", method =() => CurrentCategoryName = "Room Mods", isTogglable = false, toolTip = "Opens the room mods.", legal = true},
                new Module { buttonText = "Important Mods", method =() => CurrentCategoryName = "Important Mods", isTogglable = false, toolTip = "Opens the important mods.", legal = true},
                new Module { buttonText = "Safety Mods", method =() => CurrentCategoryName = "Safety Mods", isTogglable = false, toolTip = "Opens the safety mods.", legal = true},
                new Module { buttonText = "Movement Mods", method =() => CurrentCategoryName = "Movement Mods", isTogglable = false, toolTip = "Opens the movement mods."},
                new Module { buttonText = "Advantage Mods", method =() => CurrentCategoryName = "Advantage Mods", isTogglable = false, toolTip = "Opens the advantage mods."},
                new Module { buttonText = "Visual Mods", method =() => CurrentCategoryName = "Visual Mods", isTogglable = false, toolTip = "Opens the visual mods.", legal = true},
                new Module { buttonText = "Fun Mods", method =() => CurrentCategoryName = "Fun Mods", isTogglable = false, toolTip = "Opens the fun mods.", legal = true},
                new Module { buttonText = "Sound Mods", method =() => CurrentCategoryName = "Sound Mods", isTogglable = false, toolTip = "Opens the sound mods."},
                new Module { buttonText = "Projectile Mods", method =() => CurrentCategoryName = "Projectile Mods", isTogglable = false, toolTip = "Opens the projectile mods."},
                new Module { buttonText = "Master Mods", method =() => CurrentCategoryName = "Master Mods", isTogglable = false, toolTip = "Opens the master mods."},
                new Module { buttonText = "Overpowered Mods", method =() => CurrentCategoryName = "Overpowered Mods", isTogglable = false, toolTip = "Opens the overpowered mods."},
                new Module { buttonText = "Experimental Mods", method =() => CurrentCategoryName = "Experimental Mods", isTogglable = false, toolTip = "Opens the experimental mods.", legal = true},
                new Module { buttonText = "Detected Mods", overlapText = "<color=red>Detected Mods</color>", method = Detected.EnterDetectedTab, isTogglable = false, toolTip = "Opens the detected mods."},

                new Module { buttonText = "Achievements", method = Achievements.LoadTab, isTogglable = false, toolTip = "Opens the achievements page.", legal = true},
                new Module { buttonText = "Credits", method =() => CurrentCategoryName = "Credits", isTogglable = false, toolTip = "Opens the credits page.", legal = true}
            },

            ["Settings"] = new[]
            {
                new Module { buttonText = "Exit Settings", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Menu Settings", method =() => CurrentCategoryName = "Menu Settings", isTogglable = false, toolTip = "Opens the settings for the menu.", legal = true},
                new Module { buttonText = "Category Settings", method = Modules.Categories.Settings.CategorySettings, isTogglable = false, toolTip = "Opens the settings for the categories.", legal = true},

                new Module { buttonText = "Keybind Settings", method =() => CurrentCategoryName = "Keybind Settings", isTogglable = false, toolTip = "Opens the settings for the keybinds.", legal = true},
                new Module { buttonText = "Rebind Settings", method =() => CurrentCategoryName = "Rebind Settings", isTogglable = false, toolTip = "Opens the settings for rebinds.", legal = true},
                new Module { buttonText = "Friend Settings", method =() => CurrentCategoryName = "Friend Settings", isTogglable = false, toolTip = "Opens the settings for the friend system.", legal = true},
                new Module { buttonText = "Plugin Settings", method =() => CurrentCategoryName = "Plugin Settings", isTogglable = false, toolTip = "Opens the settings for the plugins.", legal = true},

                new Module { buttonText = "Soundboard Settings", method =() => CurrentCategoryName = "Soundboard Settings", isTogglable = false, toolTip = "Opens the settings for the soundboard."},
                new Module { buttonText = "Room Settings", method =() => CurrentCategoryName = "Room Settings", isTogglable = false, toolTip = "Opens the settings for the room mods.", legal = true},
                new Module { buttonText = "Safety Settings", method =() => CurrentCategoryName = "Safety Settings", isTogglable = false, toolTip = "Opens the settings for the safety mods."},
                new Module { buttonText = "Movement Settings", method =() => CurrentCategoryName = "Movement Settings", isTogglable = false, toolTip = "Opens the settings for the movement mods."},
                new Module { buttonText = "Advantage Settings", method =() => CurrentCategoryName = "Advantage Settings", isTogglable = false, toolTip = "Opens the settings for the advantage mods.", legal = true},
                new Module { buttonText = "Visual Settings", method =() => CurrentCategoryName = "Visual Settings", isTogglable = false, toolTip = "Opens the settings for the visual mods.", legal = true},
                new Module { buttonText = "Fun Settings", method =() => CurrentCategoryName = "Fun Settings", isTogglable = false, toolTip = "Opens the settings for the fun mods.", legal = true},
                new Module { buttonText = "Overpowered Settings", method =() => CurrentCategoryName = "Overpowered Settings", isTogglable = false, toolTip = "Opens the settings for the overpowered mods."},
                new Module { buttonText = "Detected Settings", method =() => CurrentCategoryName = "Detected Settings", isTogglable = false, toolTip = "Opens the settings for the detected mods."},
                new Module { buttonText = "Projectile Settings", method =() => CurrentCategoryName = "Projectile Settings", isTogglable = false, toolTip = "Opens the settings for the projectiles."},
                new Module { buttonText = "Patreon Settings", method =() => CurrentCategoryName = "Patreon Settings", isTogglable = false, toolTip = "Opens the settings for the Patreon mods."},
            },

            ["Menu Settings"] = new[]
            {
                new Module { buttonText = "Exit Menu Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Right Hand", enableMethod = Modules.Categories.Settings.RightHand, disableMethod = Modules.Categories.Settings.LeftHand, toolTip = "Puts the menu on your right hand.", legal = true},
                new Module { buttonText = "Both Hands", enableMethod =() => bothHands = true, disableMethod =() => bothHands = false, toolTip = "Puts the menu on your both of your hands.", legal = true},

                new Module { buttonText = "One Handed Menu", enableMethod =() => oneHand = true, disableMethod =() => oneHand = false, toolTip = "Makes the menu open in front of you, so you can use it with one hand.", legal = true},
                new Module { buttonText = "Joystick Menu", enableMethod =() => joystickMenu = true, disableMethod = Modules.Categories.Settings.JoystickMenuOff, toolTip = "Makes the menu into something like Colossal, click your joysticks to open, joysticks to move between mods and pages, and click your left joystick to toggle a mod.", legal = true},
                new Module { buttonText = "Physical Menu", enableMethod = Modules.Categories.Settings.PhysicalMenuOn, disableMethod = Modules.Categories.Settings.PhysicalMenuOff, toolTip = "Freezes the menu in world space.", legal = true},
                new Module { buttonText = "Bark Menu", enableMethod =() => barkMenu = true, disableMethod =() => barkMenu = false, toolTip = "Allows you to spawn the menu similar to bark by banging on your chest.", legal = true},
                new Module { buttonText = "Wrist Menu", enableMethod =() => wristMenu = true, disableMethod =() => wristMenu = false, toolTip = "Turns the menu into a weird wrist watch, click your hand to open it.", legal = true},
                new Module { buttonText = "Watch Menu", enableMethod = Modules.Categories.Settings.WatchMenuOn, method = Modules.Categories.Settings.CheckWatchMenu, disableMethod = Modules.Categories.Settings.WatchMenuOff, toolTip = "Turns the menu into a watch, click your joystick to toggle, and move your joystick to select a mod."},
                new Module { buttonText = "Shiny Menu", enableMethod =() => shinyMenu = true, disableMethod =() => shinyMenu = false, toolTip = "Makes the menu's textures use the old shader.", legal = true},
                new Module { buttonText = "Transparent Menu", enableMethod =() => transparentMenu = true, disableMethod =() => transparentMenu = false, toolTip = "Makes the menu transparent.", legal = true},
                new Module { buttonText = "Crystallize Menu", enableMethod =() => { crystallizeMenu = true; CustomBoards.BoardMaterial = CrystalMaterial; }, disableMethod =() => { crystallizeMenu = false ; CustomBoards.BoardMaterial = null; }, toolTip = "Turns the menu into crystals.", legal = true},
                new Module { buttonText = "Explode Menu", enableMethod =() => explodeMenu = true, disableMethod =() => explodeMenu = false, toolTip = "Makes the menu explode when closing it.", legal = true},
                new Module { buttonText = "Thick Menu", enableMethod =() => thinMenu = false, disableMethod =() => thinMenu = true, toolTip = "Makes the menu thin.", legal = true},
                new Module { buttonText = "Long Menu", enableMethod =() => longMenu = true, disableMethod =() => longMenu = false, toolTip = "Makes the menu long.", legal = true},
                new Module { buttonText = "Flip Menu", enableMethod =() => flipMenu = true, disableMethod =() => flipMenu = false, toolTip = "Flips the menu to the back of your hand.", legal = true},


                new Module { buttonText = "Round Menu", enableMethod =() => roundMenu = true, disableMethod =() => roundMenu = false, toolTip = "Makes the menu objects round.", legal = true},
                new Module { buttonText = "Outline Menu", enableMethod =() => outlineMenu = true, disableMethod =() => outlineMenu = false, toolTip = "Gives the menu objects an outline.", legal = true},
                new Module { buttonText = "Outline Text", enableMethod =() => outlineText = true, disableMethod =() => outlineText = false, toolTip = "Gives the text objects an outline.", legal = true},
                new Module { buttonText = "Strikethrough Text", enableMethod =() => strikethroughText = true, disableMethod =() => strikethroughText = false, toolTip = "Strikes out all text on the menu.", legal = true},
                new Module { buttonText = "Underline Text", enableMethod =() => underlineText = true, disableMethod =() => underlineText = false, toolTip = "Underlines all text on the menu.", legal = true},
                new Module { buttonText = "Small-Caps Text", enableMethod =() => smallCapsText = true, disableMethod =() => smallCapsText = false, toolTip = "Turns all text into a small capital version.", legal = true},
                new Module { buttonText = "Redact Text", enableMethod =() => redactText = true, disableMethod =() => redactText = false, toolTip = "Redacts all text on the menu.", legal = true},
                new Module { buttonText = "Inner Outline Menu", enableMethod =() => innerOutline = true, disableMethod =() => innerOutline = false, toolTip = "Gives the menu an outline on the inside.", legal = true},
                new Module { buttonText = "Smooth Menu Position", enableMethod =() => smoothMenuPosition = true, disableMethod =() => smoothMenuPosition = false, toolTip = "Smoothes the menu's position.", legal = true},
                new Module { buttonText = "Smooth Menu Rotation", enableMethod =() => smoothMenuRotation = true, disableMethod =() => smoothMenuRotation = false, toolTip = "Smoothes the menu's rotation.", legal = true},

                new Module { buttonText = "Disable Favorite Keybind", enableMethod =() => Manager.disableFavoritingKeybind = true, disableMethod =() => Manager.disableFavoritingKeybind = false, toolTip = "Makes it so toggling a button while holding left grip does nothing.", legal = true},
                new Module { buttonText = "Freeze Player in Menu", method = Modules.Categories.Settings.FreezePlayerInMenu, enableMethod =() => closePosition = References.Rigidbody.transform.position, toolTip = "Freezes your character when inside the menu.", legal = true},
                new Module { buttonText = "Freeze Rig in Menu", overlapText = "Ghost Rig in Menu", method = Modules.Categories.Settings.FreezeRigInMenu, disableMethod = Movement.EnableRig, toolTip = "Freezes your rig when inside the menu.", legal = true},
                new Module { buttonText = "Zero Gravity Menu", enableMethod =() => zeroGravityMenu = true, disableMethod =() => zeroGravityMenu = false, toolTip = "Disables gravity on the menu when dropping it.", legal = true},
                new Module { buttonText = "Menu Collisions", enableMethod =() => menuCollisions = true, disableMethod =() => menuCollisions = false, toolTip = "Gives the menu collisions when dropping it.", legal = true},
                new Module { buttonText = "Player Scale Menu", enableMethod =() => scaleWithPlayer = true, disableMethod =() => scaleWithPlayer = false, toolTip = "Scales the menu with your player scale.", legal = true},
                new Module { buttonText = "Alphabetize Menu", toolTip = "Alphabetizes the entire menu.", legal = true},
                new Module { buttonText = "Custom Menu Name", enableMethod = Modules.Categories.Settings.CustomMenuName, disableMethod =() => doCustomName = false, toolTip = $"Changes the name of the menu to whatever. You can change the text inside of your Gorilla Tag files ({PluginInfo.BaseDirectory}/Qubit_CustomMenuName.txt).", legal = true},
                new Module { buttonText = "Menu Trail", enableMethod =() => menuTrail = true, disableMethod =() => menuTrail = false, toolTip = "Gives the menu a trail when you drop.", legal = true},

                new Module { buttonText = "Dynamic Animations", enableMethod =() => dynamicAnimations = true, disableMethod =() => dynamicAnimations = false, toolTip = "Adds more animations to the menu, giving you a better sense of control.", legal = true},
                new Module { buttonText = "Slow Dynamic Animations", enableMethod =() => slowDynamicAnimations = true, disableMethod =() => slowDynamicAnimations = false, toolTip = "Makes Dynamic Animations slower.", legal = true},
                new Module { buttonText = "Dynamic Gradients", enableMethod =() => dynamicGradients = true, disableMethod =() => dynamicGradients = false, toolTip = "Makes gradients dynamic, showing you the full gradient instead of a pulsing color.", legal = true},
                new Module { buttonText = "Horizontal Gradients", enableMethod =() => { horizontalGradients = true; cacheGradients.Clear(); }, disableMethod =() => { horizontalGradients = false; cacheGradients.Clear(); }, toolTip = "Rotates the dynamic gradients by 90 degrees.", legal = true},
                new Module { buttonText = "Scrolling Gradients", enableMethod =() => scrollingGradients = true, disableMethod =() => scrollingGradients = false, toolTip = "Scrolls the dynamic gradients over time.", legal = true},
                new Module { buttonText = "Dynamic Sounds", enableMethod =() => dynamicSounds = true, disableMethod =() => dynamicSounds = false, toolTip = "Adds more sounds to the menu, giving you a better sense of control.", legal = true},
                new Module { buttonText = "Disable Adaptive Buttons", enableMethod =() => adaptiveButtons = false, disableMethod =() => adaptiveButtons = true, toolTip = "Disables the rebinding of buttons to make your experience better based on what controllers you're using.", legal = true},
                new Module { buttonText = "Incremental Boost", enableMethod =() => incrementalBoost = true, disableMethod =() => incrementalBoost = false, toolTip = "Allows you to increment faster by holding down your <color=green>grip</color>.", legal = true},
                new Module { buttonText = "Page Scrolling", enableMethod =() => pageScrolling = true, disableMethod =() => { pageScrolling = false; pageOffset = 0; }, toolTip = "Allows you to scroll through the mods with your joystick instead of flipping through pages.", legal = true},
                new Module { buttonText = "Exclusive Page Sounds", enableMethod =() => exclusivePageSounds = true, disableMethod =() => exclusivePageSounds = false, toolTip = "Makes the sound that joystick menu makes when switching pages using the menu.", legal = true},
                new Module { buttonText = "Particle Spawn Effect", enableMethod =() => particleSpawnEffect = true, disableMethod =() => particleSpawnEffect = false, toolTip = "Spawns particles when opening the menu.", legal = true},
                new Module { buttonText = "Gradient Title", enableMethod =() => gradientTitle = true, disableMethod =() => gradientTitle = false, toolTip = "Gives a gradient to the title of the menu depending on your theme.", legal = true},
                new Module { buttonText = "Animated Title", enableMethod =() => animatedTitle = true, disableMethod =() => animatedTitle = false, toolTip = "Animates the title of the menu.", legal = true},
                new Module { buttonText = "Voice Commands", enableMethod = Modules.Categories.Settings.VoiceRecognitionOn, method = Modules.Categories.Settings.CheckFocus, disableMethod = Modules.Categories.Settings.VoiceRecognitionOff, toolTip = "Enable and disable mods using your voice. Activate it like how you would any other voice assistant, such as \"Jarvis, Platforms\".", legal = true},
                new Module { buttonText = "Chain Voice Commands", toolTip = "Makes voice commands chain together, so you don't have to repeatedly ask it to listen to you.", legal = true},
                new Module { buttonText = "AI Assistant", enableMethod =() => Coroutines.instance.StartCoroutine(Modules.Categories.Settings.DictationOn()), method = Modules.Categories.Settings.CheckFocus, disableMethod = Modules.Categories.Settings.DictationOff, toolTip = "A voice assistant with artificial intelligence capabilities.", legal = true},
                

                new Module { buttonText = "Narrate Assistant", toolTip = "Narrates what the voice assistant says locally.", legal = true},
                new Module { buttonText = "Global Narrate Assistant", toolTip = "Narrates what the voice assistant says globally.", legal = true},
                new Module { buttonText = "Global Dynamic Sounds", toolTip = "Plays the dynamic sounds through your microphone.", legal = true},

                new Module { buttonText = "Debug Dictation", enableMethod =() => Modules.Categories.Settings.debugDictation = true, disableMethod =() => Modules.Categories.Settings.debugDictation = false, toolTip = "Debug what you say to the AI Assistant in your Unity console.", legal = true},

                new Module { buttonText = "Custom System Prompt", enableMethod =() => LanguageModel.customPrompt = true, disableMethod =() => LanguageModel.customPrompt = false, toolTip = "Never resets the system prompt, allowing you to edit the file.", legal = true},

                new Module { buttonText = "Reset Voice Commands Keywords", method = Modules.Categories.Settings.ResetVoiceCommandsKeywords, isTogglable = false, toolTip = "Resets the keywords for all the voice command related mods.", legal = true},
                new Module { buttonText = "Reset System Prompt", method = Modules.Categories.Settings.ResetSystemPrompt, isTogglable = false, toolTip = "Resets the system prompt for the AI Assistant.", legal = true},

                new Module { buttonText = "Player Select", method = Modules.Categories.Settings.PlayerSelect, toolTip = "Spawns a line in your hand when moving your hand away from the menu that you can select players with.", legal = true},
                

                new Module { buttonText = "Annoying Mode", enableMethod =() => annoyingMode = true, disableMethod = Modules.Categories.Settings.AnnoyingModeOff, toolTip = "Turns on the April Fools 2024 settings.", legal = true},
                new Module { buttonText = "Lowercase Mode", enableMethod =() => lowercaseMode = true, disableMethod =() => lowercaseMode = false, toolTip = "Makes the entire menu's text lowercase.", legal = true},
                new Module { buttonText = "Uppercase Mode", enableMethod =() => uppercaseMode = true, disableMethod =() => uppercaseMode = false, toolTip = "Makes the entire menu's text uppercase.", legal = true},
                new Module { buttonText = "Overflow Mode", enableMethod =() => noAutoSizeText = true, disableMethod =() => noAutoSizeText = false, toolTip = "Makes the entire menu's text overflow.", legal = true},

                ButtonHelper.Create("Change Menu Language", () => Modules.Categories.Settings.LanguageNames, 0, Modules.Categories.Settings.ApplyMenuLanguage, "Changes the language of the menu.", legal: true),
                ButtonHelper.Create("Change Menu Button", () => Modules.Categories.Settings.MenuButtonNames, menuButtonIndex, Modules.Categories.Settings.ApplyMenuButton, "Changes the button used to open menu.", legal: true),
                new Module { buttonText = "Menu Toggle Button", enableMethod =() => toggleButton = true, disableMethod =() => toggleButton = false, toolTip = "Allows the menu to be toggled on and off with the menu button.", legal = true},
                ButtonHelper.Create("Change Menu Theme", () => Modules.Categories.Settings.Themes.Select(t => t.Name).ToArray(), themeType, v => Modules.Categories.Settings.ApplyMenuTheme(v), toolTip: "Changes the theme of the menu.", legal: true),
                new Module { buttonText = "Slow Gradient Fade", enableMethod =() => slowFadeColors = true, disableMethod =() => slowFadeColors = false, toolTip = "Makes gradient themes fade slower on the background.", legal = true},
                ButtonHelper.CreateNumeric("Change Menu Scale", 2, 30, 10, Modules.Categories.Settings.ApplyMenuScale, v => (v / 10f).ToString(), "Changes the scale of the menu.", legal: true),
                ButtonHelper.CreateNumeric("Change Notification Scale", 1, 20, 6, Modules.Categories.Settings.ApplyNotificationScale, v => v.ToString(), "Changes the scale of the notifications.", legal: true),
                ButtonHelper.CreateNumeric("Change Arraylist Scale", 1, 20, 4, Modules.Categories.Settings.ApplyArraylistScale, v => v.ToString(), "Changes the scale of the arraylist.", legal: true),
                ButtonHelper.CreateNumeric("Change Overlay Scale", 1, 20, 6, Modules.Categories.Settings.ApplyOverlayScale, v => v.ToString(), "Changes the scale of the overlay.", legal: true),
                ButtonHelper.CreateNumeric("Change Page Size", 4, QubitMaxRows, _pageSize, Modules.Categories.Settings.ApplyPageSize, v => v.ToString(), "Changes the amount of rows per page, from 4 to 8. Search uses one row for the query.", legal: true),
                ButtonHelper.CreateNumeric("Change Character Distance", 0, 15, characterDistance, Modules.Categories.Settings.ApplyCharacterDistance, v => (v + 1).ToString(), "Changes the distance between characters on the menu.", legal: true),
                new Module { buttonText = "Custom Menu Theme", enableMethod = Modules.Categories.Settings.CustomMenuTheme, disableMethod = Modules.Categories.Settings.FixTheme, toolTip = "Changes the theme of the menu to a custom one.", legal = true},
                new Module { buttonText = "Change Custom Menu Theme", method = Modules.Categories.Settings.ChangeCustomMenuTheme, isTogglable = false, toolTip = "Changes the theme of custom the menu.", legal = true},
                new Module { buttonText = "Custom Menu Background", enableMethod = Modules.Categories.Settings.CustomMenuBackground, disableMethod = Modules.Categories.Settings.FixMenuBackground, toolTip = $"Changes the background of the menu to a custom image. You can change the photo inside of your Gorilla Tag files ({PluginInfo.BaseDirectory}/CustomBackground.png).", legal = true},
                new Module { buttonText = "Custom Watermark", enableMethod = Modules.Categories.Settings.CustomWatermark, disableMethod =() => customWatermark = null, toolTip = $"Changes the watermark on the UI and the back of the menu to a custom image. You can change the photo inside of your Gorilla Tag files ({PluginInfo.BaseDirectory}/CustomWatermark.png).", legal = true},
                new Module { buttonText = "Disable Watermark", enableMethod =() => disableWatermark = true, disableMethod =() => disableWatermark = false, toolTip = "Disables the watermark on the UI and the back of the menu.", legal = true},
                ButtonHelper.CreateNumeric("Change Page Type", 1, 6, pageButtonType, Modules.Categories.Settings.ApplyPageType, toolTip: "Changes the type of page buttons.", legal: true),
                ButtonHelper.CreateNumeric("Change Arrow Type", 0, arrowTypes.Length - 1, arrowType, Modules.Categories.Settings.ApplyArrowType, toolTip: "Changes the type of arrows on the page buttons.", legal: true),
                ButtonHelper.CreateNumeric("Change Font Type", 0, 14, fontCycle, Fonts.ApplyFontType, toolTip: "Changes the type of font.", legal: true),
                new Module { buttonText = "Rapid Font Changer", method = Modules.Categories.Settings.ChangeFontRapid, toolTip = "Changes the type of font every menu refresh.", legal = true},
                new Module { buttonText = "Custom Font Type", enableMethod = Modules.Categories.Settings.CustomFontType, method = Modules.Categories.Settings.PersistCustomFont, disableMethod = Modules.Categories.Settings.DisableCustomFont, toolTip = $"Changes the font type on the menu to a custom font. You can change the photo inside of your Gorilla Tag files ({PluginInfo.BaseDirectory}/Qubit_CustomWatermark.txt).", legal = true},
                ButtonHelper.CreateNumeric("Change Font Style Type", 0, 3, Modules.Categories.Settings.fontStyleType, Fonts.ApplyFontStyleType, toolTip: "Changes the style of the font.", legal: true),
                ButtonHelper.Create("Change Input Text Color", () => Modules.Categories.Settings.InputColorNames, Modules.Categories.Settings.inputTextColorInt, Modules.Categories.Settings.ApplyInputTextColor, "Changes the color of the input indicator next to the buttons.", legal: true),
                new Module { buttonText = "Vibrant Text Colors", enableMethod =() => vibrantColors = true, disableMethod =() => vibrantColors = false, toolTip = "Makes certain green and purple colors more vibrant.", legal = true},
                ButtonHelper.CreateNumeric("Change PC Menu Background", 0, 5, pcBackgroundIndex, Modules.Categories.Settings.ApplyPCUI, toolTip: "Changes the background of the PC ui.", legal: true),
                ButtonHelper.CreateNumeric("Change Joystick Menu Position", 0, joystickMenuPositions.Length - 1, joystickMenuPosition, Modules.Categories.Settings.ApplyJoystickMenuPosition, toolTip: "Changes the position of the joystick menu.", legal: true),
                ButtonHelper.CreateNumeric("Change Notification Time", 0, 5, notificationDecayTime / 1000, Modules.Categories.Settings.ApplyNotificationTime, v => v.ToString(), "Changes the time before a notification is removed.", legal: true),
                ButtonHelper.Create("Change Notification Sound", () => Sounds.SoundList["Notifications"].Keys.ToArray(), 0, v => Modules.Categories.Settings.ApplyNotificationSound(Sounds.SoundList["Notifications"].Keys.ToArray()[v]), toolTip: "Changes the sound that plays when receiving a notification.", legal: true, onCycle: _ => { var src = audioManager?.GetComponent<AudioSource>(); src?.Stop(); Sounds.Play(Sounds.DefaultSounds["Notification"]); }),
                new Module { buttonText = "Notification Sound on Error", enableMethod =() => Notifications.soundOnError = true, disableMethod =() => Notifications.soundOnError = false, toolTip = "Plays your target notification sound when an error happens.", legal = true},
                ButtonHelper.Create("Change Narration Voice", () => Modules.Categories.Settings.NarratorNames, narratorIndex, Modules.Categories.Settings.ApplyNarrationVoice, "Changes the voice of the narrator.", legal: true),
                ButtonHelper.CreateNumeric("Change Pointer Position", 0, Modules.Categories.Settings.PointerPositions.Length - 1, pointerIndex, Modules.Categories.Settings.ApplyPointerPosition, toolTip: "Changes the position of the pointer.", legal: true),

                new Module { buttonText = "Swap GUI Colors", toolTip = "Swaps the GUI's colors to the enabled color, for darker themes.", legal = true},
                new Module { buttonText = "Swap Button Colors", enableMethod =() => swapButtonColors = true, disableMethod =() => swapButtonColors = false, toolTip = "Swaps the colors of the page buttons, disconnect button, search button, and return button to be the opposite color.", legal = true},
                new Module { buttonText = "Swap Ghostview Colors", toolTip = "Swaps the ghostview's colors to the enabled color, for darker themes.", legal = true},

                ButtonHelper.Create("Change Gun Line Quality", () => Modules.Categories.Settings.GunQualityNames, 2, Modules.Categories.Settings.ApplyGunLineQuality, "Changes the amount of points on your gun.", legal: true),
                ButtonHelper.Create("Change Gun Variation", () => Modules.Categories.Settings.GunVariationNames, gunVariation, Modules.Categories.Settings.ApplyGunVariation, "Changes the look of the gun.", legal: true),
                ButtonHelper.Create("Change Gun Direction", () => Modules.Categories.Settings.GunDirectionNames, GunDirection, Modules.Categories.Settings.ApplyGunDirection, "Changes the direction of the gun.", legal: true),

                new Module { buttonText = "Gun Sounds", enableMethod =() => GunSounds = true, disableMethod =() => GunSounds = false, toolTip = "Plays laser sounds when interacting with the gun.", legal = true},
                new Module { buttonText = "Gun Vibrations", enableMethod =() => GunVibrations = true, disableMethod =() => GunVibrations = false, toolTip = "Vibrates your controller when interacting with the gun.", legal = true},
                new Module { buttonText = "Gun Particles", enableMethod =() => GunParticles = true, disableMethod =() => GunParticles = false, toolTip = "Gives the gun particles when you shoot it.", legal = true},
                new Module { buttonText = "Swap Gun Hand", enableMethod =() => SwapGunHand = true, disableMethod =() => SwapGunHand = false, toolTip = "Swaps the hand gun mods work with.", legal = true},
                new Module { buttonText = "Gripless Guns", enableMethod =() => GriplessGuns = true, disableMethod =() => GriplessGuns = false, toolTip = "Forces your grip to be held for guns.", legal = true},
                new Module { buttonText = "Triggerless Guns", enableMethod =() => TriggerlessGuns = true, disableMethod =() => TriggerlessGuns = false, toolTip = "Forces your trigger to be held for guns.", legal = true},
                new Module { buttonText = "Hard Gun Lock", enableMethod =() => HardGunLocks = true, disableMethod =() => HardGunLocks = false, toolTip = "Locks the guns even when letting go of grip until you press <color=green>B</color>.", legal = true},
                new Module { buttonText = "Small Gun Pointer", enableMethod =() => smallGunPointer = true, disableMethod =() => smallGunPointer = false, toolTip = "Makes the ball at the end of every gun mod smaller.", legal = true},
                new Module { buttonText = "Smooth Gun Pointer", enableMethod =() => SmoothGunPointer = true, disableMethod =() => SmoothGunPointer = false, toolTip = "Makes the ball at the end of every gun mod smoother.", legal = true},
                new Module { buttonText = "Disable Gun Pointer", enableMethod =() => disableGunPointer = true, disableMethod =() => disableGunPointer = false, toolTip = "Disables the ball at the end of every gun mod.", legal = true},
                new Module { buttonText = "Disable Gun Line", enableMethod =() => disableGunLine = true, disableMethod =() => disableGunLine = false, toolTip = "Disables the gun from your hand to the end of every gun mod.", legal = true},

                new Module { buttonText = "Checkbox Buttons", enableMethod =() => checkMode = true, disableMethod =() => checkMode = false, toolTip = "Turns the buttons into checkboxes.", legal = true},
                ButtonHelper.Create("Change Button Sound", () => Sounds.SoundList["Buttons"].Keys.ToArray(), 0, v => Modules.Categories.Settings.ApplyButtonSound(Sounds.SoundList["Buttons"].Keys.ToArray()[v]), toolTip: "Changes the button click sound.", legal: true,
                    onCycle: _ =>
                    {
                        if (References.LocalRig == null) return;
                        References.LocalRig.leftHandPlayer?.Stop();
                        References.LocalRig.rightHandPlayer?.Stop();
                        Sounds.Play(Sounds.DefaultSounds["Button"]);
                    }),
                ButtonHelper.CreateNumeric("Change Button Volume", 0, 10, buttonClickVolume, Modules.Categories.Settings.ApplyButtonVolume, v => v.ToString(), "Changes the volume of the buttons.", legal: true, onCycle: Modules.Categories.Settings.PreviewButtonVolume),
                new Module { buttonText = "Serversided Button Sounds", enableMethod =() => serversidedButtonSounds = true, disableMethod =() => serversidedButtonSounds = false, toolTip = "Lets everyone in the the room hear the buttons."},
                new Module { buttonText = "Disable Button Vibration", enableMethod =() => doButtonsVibrate = false, disableMethod =() => doButtonsVibrate = true, toolTip = "Disables the slight vibration that happens when you click a button."},

                ButtonHelper.Create("Change Menu Soundpack", () => Sounds.Soundpacks.Keys.ToArray(), Sounds.DefaultSoundpack, v => Modules.Categories.Settings.ApplyMenuSoundpack(Sounds.Soundpacks.Keys.ToArray()[v]), toolTip: "Changes the sound pack used for the menu.", legal: true,
                    onCycle: _ =>
                    {
                        if (References.LocalRig == null) return;
                        References.LocalRig.leftHandPlayer?.Stop();
                        References.LocalRig.rightHandPlayer?.Stop();
                        Sounds.Play("Default");
                    }
                ),

                new Module { buttonText = "Clear Notifications on Disconnect", enableMethod =() => clearNotificationsOnDisconnect = true, disableMethod =() => clearNotificationsOnDisconnect = false, toolTip = "Clears all notifications on disconnect.", legal = true},
                new Module { buttonText = "Hide Notifications on Camera", overlapText = "Streamer Mode Notifications", toolTip = "Makes notifications only render in VR.", legal = true},
                new Module { buttonText = "Stack Notifications", enableMethod =() => stackNotifications = true, disableMethod =() => stackNotifications = false, toolTip = "Stacks repeated notifications into one notification.", legal = true},
                new Module { buttonText = "Narrate Notifications", enableMethod =() => Notifications.narrateNotifications = true, disableMethod =() => Notifications.narrateNotifications = false, toolTip = "Narrates all notifications with text to speech.", legal = true},
                new Module { buttonText = "No Prefix Narration", enableMethod =() => Notifications.noPrefix = true, disableMethod =() => Notifications.noPrefix = false, toolTip = "Stops the prefix on notifications from narrating itself.", legal = true},
                new Module { buttonText = "Hide Notification Brackets", enableMethod =() => hideBrackets = true, disableMethod =() => hideBrackets = false, toolTip = "Hides brackets on all notifications.", legal = true},

                new Module { buttonText = "Conduct Notifications", enableMethod =() => { GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText").GetComponent<TextMeshPro>().text = "Qubit"; GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData").GetComponent<TextMeshPro>().richText = true; }, method =() => GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData").GetComponent<TextMeshPro>().text = Notifications.notificationText.text, toolTip = "Shows notifications on the code of conduct instead.", legal = true},
                new Module { buttonText = "Disable Notification Rich Text", enableMethod =() => Notifications.noRichText = true, disableMethod =() => Notifications.noRichText = false, toolTip = "Removes rich text from notifications.", legal = true},

                new Module { buttonText = "Disable RPC Protection", enableMethod =() => Qubit.Patches.Safety.RPC.OpRaiseEventPatch.enabled = false, disableMethod =() => Qubit.Patches.Safety.RPC.OpRaiseEventPatch.enabled = true, toolTip = "Disables the client-side protection limiting you to sending 500 RPCs a second.", legal = true},
                new Module { buttonText = "Disable Notifications", enableMethod =() => disableNotifications = true, disableMethod =() => disableNotifications = false, toolTip = "Disables all notifications.", legal = true},
                new Module { buttonText = "Disable Master Client Notifications", enableMethod =() => disableMasterClientNotifications = true, disableMethod =() => disableMasterClientNotifications = false, toolTip = "Disables all notifications regarding master client.", legal = true},
                new Module { buttonText = "Disable Room Notifications", enableMethod =() => disableRoomNotifications = true, disableMethod =() => disableRoomNotifications = false, toolTip = "Disables all notifications regarding the room.", legal = true},
                new Module { buttonText = "Disable Player Notifications", enableMethod =() => disablePlayerNotifications = true, disableMethod =() => disablePlayerNotifications = false, toolTip = "Disables all notifications regarding players.", legal = true},
                new Module { buttonText = "Disable Enabled GUI", overlapText = "Disable Arraylist GUI", enableMethod =() => showEnabledModsVR = false, disableMethod =() => showEnabledModsVR = true, toolTip = "Disables the GUI that shows the enabled mods.", legal = true},
                new Module { buttonText = "Disable Incremental Buttons", enableMethod =() => incrementalButtons = false, disableMethod =() => incrementalButtons = true, toolTip = "Disables the buttons with the increment and decrement buttons next to it.", legal = true},
                new Module { buttonText = "Disable Disconnect Button", enableMethod =() => disableDisconnectButton = true, disableMethod =() => disableDisconnectButton = false, toolTip = "Disables the disconnect button at the top of the menu.", legal = true},
                new Module { buttonText = "Disable Menu Title", enableMethod =() => { buttonOffset = pageButtonType == 2 ? 1 : -1; hideTitle = true; }, method =() => { buttonOffset = pageButtonType == 2 ? 1 : -1; hideTitle = true; }, disableMethod =() => { buttonOffset = pageButtonType == 2 ? 2 : 0; hideTitle = false; }, toolTip = "Hides the menu title, allowing for more buttons per page.", legal = true},
                new Module { buttonText = "Disable Search Button", enableMethod =() => disableSearchButton = true, disableMethod =() => disableSearchButton = false, toolTip = "Disables the search button at the bottom of the menu.", legal = true},
                new Module { buttonText = "Disable Return Button", enableMethod =() => disableReturnButton = true, disableMethod =() => disableReturnButton = false, toolTip = "Disables the return button at the bottom of the menu.", legal = true},
                new Module { buttonText = "Disable Page Buttons", enableMethod = Modules.Categories.Settings.DisablePageButtons, disableMethod =() => disablePageButtons = false, toolTip = "Disables the page buttons. Recommended with Joystick Menu.", legal = true},
                new Module { buttonText = "Disable Page Number", enableMethod =() => noPageNumber = true, disableMethod =() => noPageNumber = false, toolTip = "Disables the current page number in the title text.", legal = true},
                new Module { buttonText = "Disable FPS Counter", enableMethod =() => disableFpsCounter = true, disableMethod =() => disableFpsCounter = false, toolTip = "Disables the FPS counter.", legal = true},
                new Module { buttonText = "Disable Drop Menu", enableMethod =() => dropOnRemove = false, disableMethod =() => dropOnRemove = true, toolTip = "Makes the menu despawn instead of falling.", legal = true},
                new Module { buttonText = "Disable Board Colors", overlapText = "Disable Custom Boards", enableMethod =() => CustomBoards.CustomBoardsEnabled = false, disableMethod =() => CustomBoards.CustomBoardsEnabled = true, toolTip = "Disables the board colors to look legitimate on screen share.", legal = true},
                new Module { buttonText = "Disable Custom Text Colors", enableMethod =() => CustomBoards.CustomBoardTextEnabled = false, disableMethod =() => CustomBoards.CustomBoardTextEnabled = true, toolTip = "Disables the text colors on the boards to make them match their original theme.", legal = true},
                new Module { buttonText = "Custom Board Fonts", enableMethod =() => CustomBoards.CustomBoardFonts = true, disableMethod =() => CustomBoards.CustomBoardFonts = false, toolTip = "Applies the menu's font to the boards.", legal = true},

                new Module { buttonText = "Disable Keyboard Delay", toolTip = "Disables the delay between pressing keys on the keyboard.", legal = true},
                new Module { buttonText = "Disable PC Keyboard Sounds", enableMethod =() => pcKeyboardSounds = false, disableMethod =() => pcKeyboardSounds = true, toolTip = "Disables the sound for pressing keys on PC.", legal = true},

                new Module { buttonText = "Info Hide ID", enableMethod =() => Modules.Categories.Settings.hideId = true, disableMethod =() => Modules.Categories.Settings.hideId = false, toolTip = "Hides your ID in the information page."},
                new Module { buttonText = "Conduct Info", method = Modules.Categories.Settings.ConductDebug, toolTip = "Shows debug information on the code of conduct."},
                new Module { buttonText = "Info Button", enableMethod =() => enableDebugButton = true, disableMethod =() => enableDebugButton = false, toolTip = "Shows an information button at the bottom of the menu."},

                new Module { buttonText = "Hide Text on Camera", enableMethod =() => hideTextOnCamera = true, disableMethod =() => hideTextOnCamera = false, overlapText = "Streamer Mode Menu Text", toolTip = "Makes the menu's text only render on VR."},
                new Module { buttonText = "Hide Pointer", enableMethod =() => hidePointer = true, disableMethod =() => hidePointer = false, toolTip = "Hides the pointer above your hand.", legal = true},
                new Module { buttonText = "Hide Settings", enableMethod =() => hideSettings = true, disableMethod =() => hideSettings = false, toolTip = "Hides all settings from the Enabled Mods tab, and all arraylists.", legal = true},
                new Module { buttonText = "Hide Macros", enableMethod =() => hideMacros = true, disableMethod =() => hideMacros = false, toolTip = "Hides all macros from the Enabled Mods tab." },

                new Module { buttonText = "Advanced Arraylist", enableMethod =() => advancedArraylist = true, disableMethod =() => advancedArraylist = false, toolTip = "Updates the FPS Counter less, making it easier to read.", legal = true},
                new Module { buttonText = "Flip Arraylist", enableMethod =() => flipArraylist = true, disableMethod =() => flipArraylist = false, toolTip = "Flips the arraylist at the top of the screen.", legal = true},

                new Module { buttonText = "Slow FPS Counter", enableMethod =() => fpsCountTimed = true, disableMethod =() => fpsCountTimed = false, toolTip = "Updates the FPS Counter less, making it easier to read.", legal = true},
                new Module { buttonText = "Average FPS Counter", enableMethod =() => fpsCountAverage = true, disableMethod =() => fpsCountAverage = false, toolTip = "Smooths out the FPS Counter, making it easier to read.", legal = true},
                new Module { buttonText = "Frametime Counter", enableMethod =() => ftCount = true, disableMethod =() => ftCount = false, toolTip = "Replace the FPS Counter to show frametime in ms instead.", legal = true},

                new Module { buttonText = "Disable Ghostview", enableMethod =() => disableGhostview = true, disableMethod =() => disableGhostview = false, toolTip = "Disables the transparent rig when you're in ghost."},
                new Module { buttonText = "Legacy Ghostview", enableMethod =() => legacyGhostview = true, disableMethod =() => legacyGhostview = false, toolTip = "Reverts the transparent rig to the two balls when you're in ghost."},

                new Module { buttonText = "No Global Search", enableMethod =() => nonGlobalSearch = true, disableMethod =() => nonGlobalSearch = false, toolTip = "Makes the search button only search for mods in the current subcategory, unless on the main page.", legal = true},
                new Module { buttonText = "Joystick Menu Search", enableMethod =() => joystickMenuSearching = true, disableMethod =() => joystickMenuSearching = false, toolTip = "Allows you to move your selected item down to the search button with joystick menu.", legal = true},

                new Module { buttonText = "Menu Presets", method =() => CurrentCategoryName = "Menu Presets", isTogglable = false, toolTip = "Opens the page of presets."},
                new Module { buttonText = "Backup Preferences", enableMethod =() => backupPreferences = true, disableMethod =() => backupPreferences = false, toolTip = "Automatically saves a copy of your preferences every minute.", legal = true},
                new Module { buttonText = "Save Preferences", method = Preferences.Save, isTogglable = false, toolTip = "Saves your preferences to a file.", legal = true},
                new Module { buttonText = "Load Preferences", method = Preferences.Load, isTogglable = false, toolTip = "Loads your preferences from a file.", legal = true},
                new Module { buttonText = "Disable Autosave", enableMethod = () => Preferences.DisableAutoSave = true, disableMethod = () => Preferences.DisableAutoSave = false, toolTip = "Disables the auto save mechanism.", legal = true},
                new Module { buttonText = "Panic", method = Modules.Categories.Settings.Panic, isTogglable = false, toolTip = "Disables every single active mod.", legal = true},
            },

            ["Room Settings"] = new[]
            {
                new Module { buttonText = "Exit Room Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "20 Player Rooms", toolTip = "Changes Create Public and Create Private to support 20 players." },
                ButtonHelper.CreateNumeric("Change Reconnect Time", 1, 5, Important.reconnectDelay, Modules.Categories.Settings.ApplyReconnectTime, v => v.ToString(), "Changes the amount of time waited before attempting to reconnect again.", legal: true),
            },

            ["Movement Settings"] = new[]
            {
                new Module { buttonText = "Exit Movement Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                ButtonHelper.Create("Change Platform Type", () => Movement.PlatformTypeNames, Movement.platformMode, Movement.ApplyPlatformType, "Changes the type of the platforms."),
                ButtonHelper.Create("Change Platform Shape", () => Movement.PlatformShapeNames, Movement.platformShape, Movement.ApplyPlatformShape, "Changes the shape of the platforms."),

                new Module { buttonText = "Platform Gravity", toolTip = "Makes platforms fall instead of instantly deleting them."},
                new Module { buttonText = "Platform Outlines", toolTip = "Makes platforms have outlines."},
                new Module { buttonText = "Non-Sticky Platforms", toolTip = "Makes your platforms no longer sticky."},

                new Module { buttonText = "Grip Noclip", toolTip = "Activates noclip with your <color=green>grip</color> instead."},
                new Module { buttonText = "Constant Noclip", toolTip = "Keeps your noclip activated even when not holding any buttons."},
                new Module { buttonText = "Left Hand Wall Walk", method =() => Movement.leftWallWalk = true, disableMethod =() => Movement.leftWallWalk = false, toolTip = "Swaps the wall walk mod to your left hand."},
                new Module { buttonText = "Both Hands Wall Walk", method =() => Movement.bothWallWalk = true, disableMethod =() => Movement.bothWallWalk = false, toolTip = "Allows you to use wall walk with both of your hands."},

                ButtonHelper.Create("Change Fly Speed", () => Movement.FlySpeedNames, Movement.flySpeedCycle, Movement.ApplyFlySpeed, "Changes the speed of the fly mods, such as Iron Man."),
                ButtonHelper.Create("Change Playspace Abuse Speed", () => Movement.PlayspaceAbuseNames, Movement.playspaceAbuseIndex, Movement.ApplyPlayspaceAbuseSpeed, "Changes the speed of the playspace abuse mods."),
                ButtonHelper.Create("Change Arm Length", () => Movement.ArmLengthNames, Movement.longarmCycle, Movement.ApplyArmLength, "Changes the length of the long arm mods."),
                ButtonHelper.Create("Change Speed Boost Amount", () => Movement.SpeedBoostNames, Movement.speedboostCycle, Movement.ApplySpeedBoostAmount, "Changes the speed of the speed boost mod."),
                ButtonHelper.Create("Change Wall Walk Strength", () => Movement.WallWalkStrengthNames, Movement.wallWalkStrengthIndex, Movement.ApplyWallWalkStrength, "Changes the strength of the wall walk mod."),
                ButtonHelper.Create("Change Pull Mod Power", () => Movement.PullModPowerNames, Movement.pullPowerInt, Movement.ApplyPullModPower, "Changes the power of the pull mod."),
                ButtonHelper.Create("Change Prediction Amount", () => Movement.PredictionAmountNames, Movement.predInt, Movement.ApplyPredictionAmount, "Changes the power of the predictions."),
                ButtonHelper.CreateNumeric("Change Timer Speed", 1, 50, Movement.timerPowerIndex, Movement.ApplyTimerSpeed, v => (v / 10f).ToString(), "Changes the speed of the timer mod."),
                ButtonHelper.Create("cdSpeed", () => Movement.DriveSpeedNames, Movement.driveInt, Movement.ApplyDriveSpeed, "Changes the speed of the drive mod.", overlapText: "Change Drive Speed"),
                ButtonHelper.Create("Change Fast Ropes Speed", () => Movement.FastRopesSpeedNames, Movement.fastRopesInt, Movement.ApplyFastRopesSpeed, "Changes the speed of the Fast Ropes mod."),
                new Module { buttonText = "Factored Speed Boost", toolTip = "Factors your current speed into the speed boost, giving you a positive effect even if you're tagged."},
                new Module { buttonText = "Disable Max Speed Modification", toolTip = "Makes your max speed not change, so you can't be detected of using a speed boost."},
                new Module { buttonText = "Disable Size Changer Buttons", toolTip = "Disables the size changer's buttons, so hitting grip or trigger or whatever won't do anything."},
                new Module { buttonText = "Pass World Scale Checks", enableMethod =() => Movement.passWorldScaleCheck = true, disableMethod =() => Movement.passWorldScaleCheck = false, toolTip = "Disables the Steam Long Arms mod when your hands are close to your head."},

                new Module { buttonText = "Macro Framestepper", enableMethod =() => Movement.frameStepper = true, disableMethod =() => Movement.frameStepper = false, toolTip = "Allows you to progress the macro frame by frame."},
                new Module { buttonText = "Midpoint Macros", enableMethod =() => Movement.midpointMacros = true, disableMethod =() => Movement.midpointMacros = false, toolTip = "Allows for macros to be played from their middles." },
                new Module { buttonText = "Direction Based Macros", enableMethod =() => Movement.directionBased = true, disableMethod =() => Movement.directionBased = false, toolTip = "Only plays macros if you match their velocity direction." },
                ButtonHelper.Create("Change Macro Playback Range", () => Movement.MacroPlaybackRangeNames, Movement.macroPlaybackRangeIndex, Movement.ApplyMacroPlaybackRange, "Changes the range where macros can play."),

                new Module { buttonText = "Hand Oriented Strafe", toolTip = "Makes the strafe mods move you in the forward direction of your hand."},
                new Module { buttonText = "Disable Stationary WASD Fly", toolTip = "Disables WASD Fly keeping you in-place when not moving."},

                new Module { buttonText = "Networked Grapple Mods", method = Movement.NetworkedGrappleMods, toolTip = "Makes the spider man and grappling hook mods networked, showing the line for everyone. This requires a balloon."},

                new Module { buttonText = "Non-Togglable Ghost", toolTip = "Makes the ghost mod only activate when holding down the button."},
                new Module { buttonText = "Non-Togglable Invisible", toolTip = "Makes the invisible mod only activate when holding down the button"},

                new Module { buttonText = "Splash Intercourse", toolTip = "Splashes water when \"impacting\" another player with the intercourse gun."},
                new Module { buttonText = "Reverse Intercourse", toolTip = "Turns you into the receiver when using the intercourse gun."},

                new Module { buttonText = "Elevated Sticky Drive", toolTip = "Makes you float higher in the air whenever you use Sticky Drive."},
                new Module { buttonText = "High Quality Portals", toolTip = "Makes the view through the portals higher quality." },

                new Module { buttonText = "Silent Flip", aliases = new[] { "Silent Backflip" }, toolTip = "Makes you backflip without rotating your camera" },
                ButtonHelper.CreateNumeric("Joystick Rotation Speed", 1, 5, 1, null, v => v.ToString(), "Changes the speed of the joystick rotation."),
            },

            ["Projectile Settings"] = new[]
            {
                new Module { buttonText = "Exit Projectile Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                ButtonHelper.Create("Change Projectile", () => Projectiles.GetAll()?.Select(e => e.Name).ToArray() ?? Array.Empty<string>(), Projectiles.ProjectileMode, v => Projectiles.ProjectileMode = v, toolTip: "Changes the projectile of the projectile mods."),
                new Module { buttonText = "Random Projectile", toolTip = "Makes the projectiles random." },
                new Module { buttonText = "Random Direction", toolTip = "Makes the projectiles go everywhere." },
                new Module { buttonText = "Random Color", toolTip = "Makes the projectiles random colors." },

                new Module { buttonText = "Alternate Projectile Hand", toolTip = "Constantly swaps the hand that the projectile mods shoot from." },
                new Module { buttonText = "Left Handed Projectiles", toolTip = "Allows you to shoot projectiles from your left hand as well." },

                new Module { buttonText = "Both Handed Projectiles", toolTip = "Allows you to shoot projectiles from your both hands." },


                ButtonHelper.Create("Change Shoot Speed", () => Projectiles.ShootStrengthNames, Projectiles.shootCycle, Projectiles.ApplyShootSpeed, "Changes the speed of shooting projectiles."),
                new Module { buttonText = "Shoot Projectiles", toolTip = "Shoots projectiles like a gun." },

                new Module { buttonText = "Include Hand Velocity", toolTip = "Adds the hand velocity to the projectile velocity." },

                new Module { buttonText = "Rainbow Projectiles", toolTip = "Makes the projectiles' color be rainbow." },
                new Module { buttonText = "Hard Rainbow Projectiles", toolTip = "Makes the projectiles' color be a harsher form of rainbow." },

                new Module { buttonText = "Custom Colored Projectiles", toolTip = "Makes the projectile color the custom color (buttons above)." },

                ButtonHelper.CreateNumeric("RedProj", 0, 10, Fun.red, Projectiles.ApplyRed, v => v.ToString(), "Makes projectiles more red.", overlapText: "Red"),
                ButtonHelper.CreateNumeric("GreenProj", 0, 10, Fun.green, Projectiles.ApplyGreen, v => v.ToString(), "Makes projectiles more green.", overlapText: "Green"),
                ButtonHelper.CreateNumeric("BlueProj", 0, 10, Fun.blue, Projectiles.ApplyBlue, v => v.ToString(), "Makes projectiles more blue.", overlapText: "Blue"),

                new Module { buttonText = "Friend Sided Projectiles", enableMethod =() => Projectiles.friendSided = true, disableMethod =() => Projectiles.friendSided = false, toolTip = "Makes projectiles only appear for you and your friends on the menu." },
                ButtonHelper.CreateNumeric("Friend Projectile Scale", 1, 5, Projectiles.friendProjectileScale, Projectiles.ApplyFriendProjectileScale, v => v.ToString(), "Changes the scale of projectiles sent by the Friend system."),
                new Module { buttonText = "Client Sided Projectiles", enableMethod =() => Projectiles.clientSided = true, disableMethod =() => Projectiles.clientSided = false, toolTip = "Makes projectiles only appear for you." },

                new Module { buttonText = "Override Projectile Index", method =() => SnowballModels.enabled = true, disableMethod =() => SnowballModels.enabled = false, toolTip = "Forces a specific projectile index on random projectiles." },
                ButtonHelper.CreateNumeric("Change Projectile Index", 0, 15, Projectiles.targetProjectileIndex, Projectiles.ApplyProjectileIndex, v => (v + 1).ToString(), "Changes the targetted projectile index on the \"Override Projectile Index\" mod."),

                ButtonHelper.CreateNumeric("Change Projectile Delay", -1, 20, Projectiles.projDebounceIndex, Projectiles.ApplyProjectileDelay, Projectiles.DisplayProjectileDelay, "Gives the projectiles a delay before spawning another.", onCycle: Projectiles.ProjectileDelayWarning),

                ButtonHelper.CreateNumeric("Change Snowball Size", 0, Projectiles.MaximumSnowballSize, Projectiles._snowballSize, Projectiles.ApplySnowballSize, v => (v + 1).ToString(), "Changes the size of the growing snowballs.", overlapText: "Change Growing Snowball Size"),
                new Module { buttonText = "Random Growing Snowball Size", isTogglable = false, toolTip = "Randomizes the size of the growing snowballs." },
                ButtonHelper.CreateNumeric("Change Snowball Multiplication Factor", 1, 5, Projectiles.snowballMultiplicationFactor, Projectiles.ApplySnowballMultiplicationFactor, v => v.ToString(), "Changes the multiplication factor of the snowballs."),

                new Module { buttonText = "Disable Snowball Impact Effect", method = Projectiles.DisableSnowballImpactEffect, toolTip = "Disables the impact effect that people get when hit with snowballs."},
            },

            ["Room Mods"] = new[]
            {
                new Module { buttonText = "Exit Room Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Room Creator", method = Room.RoomCreator, isTogglable = false, toolTip = "Create your own custom room!"},

                new Module { buttonText = "Disconnect", method =() => ManagerRegistry.NetworkSystem.ReturnToSinglePlayer(), isTogglable = false, toolTip = "Disconnects you from the the room.", legal = true}, 
                new Module { buttonText = "Reconnect", aliases = new[] { "Rejoin" }, method = Room.Reconnect, isTogglable = false, toolTip = "Reconnects you from and to the the room.", legal = true},

                new Module { buttonText = "Cancel Reconnect", method = Room.CancelReconnect, isTogglable = false, toolTip = "Cancels the reconnection loop.", legal = true},

                new Module { buttonText = "Join Last Room", method =() => ManagerRegistry.PhotonNetworkController.AttemptToJoinSpecificRoom(lastRoom, JoinType.Solo), isTogglable = false, toolTip = "Joins the last room you left.", legal = true},
                new Module { buttonText = "Join Random", method = Room.JoinRandom, isTogglable = false, toolTip = "Joins a random public room.", legal = true},

                new Module { buttonText = "Create Public", method =() => Room.CreateRoom(Room.RandomRoomName(), true), isTogglable = false, toolTip = "Creates a public room."},
                new Module { buttonText = "Create Private", method =() => Room.CreateRoom(Room.RandomRoomName(), false), isTogglable = false, toolTip = "Creates a private room.", legal = true},

                new Module { buttonText = "Fast Disconnect", method =() => SinglePlayer.enabled = true, disableMethod =() =>  SinglePlayer.enabled = false, toolTip = "Uses the fastest method of disconnecting possible.", legal = true},
                new Module { buttonText = "Join Menu Room", method =() => ManagerRegistry.PhotonNetworkController.AttemptToJoinSpecificRoom($"<$Qubit_{PluginInfo.Version}>", JoinType.Solo), isTogglable = false, toolTip = "Connects you to a room that is exclusive to <b>Qubit</b> users.", legal = true},

                new Module { buttonText = "Bypass Join Room Type", enableMethod =() => JoinedRoom.enabled = true, disableMethod =() => JoinedRoom.enabled = false, toolTip = "Bypasses the immediate disconnection when trying to join a room that is in another map."},

                new Module { buttonText = "Auto Join Room", method =() => PromptText("What room would you like to join?", () => Room.QueueRoom(keyboardInput), null, "Done", "Cancel"), isTogglable = false, toolTip = "Automatically attempts to connect to whatever room you desire every couple of seconds until connected.", legal = true},

                new Module { buttonText = "Auto Join Room \"RUN\"", method =() => Room.QueueRoom("RUN"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"RUN\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"HIDE\"", method =() => Room.QueueRoom("HIDE"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"HIDE\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"DAISY\"", method =() => Room.QueueRoom("DAISY"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"DAISY\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"DAISY01\"", method =() => Room.QueueRoom("DAISY01"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"DAISY01\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"DAISY09\"", method =() => Room.QueueRoom("DAISY09"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"DAISY09\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"JV3U\"", method =() => Room.QueueRoom("JV3U"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"JV3U\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"J3VU\"", method =() => Room.QueueRoom("J3VU"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"J3VU\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"PBBV\"", method =() => Room.QueueRoom("PBBV"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"PBBV\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"BOT\"", method =() => Room.QueueRoom("BOT"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"BOT\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"LUCIO\"", method =() => Room.QueueRoom("LUCIO"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"LUCIO\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"VEN1\"", method =() => Room.QueueRoom("VEN1"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"VEN1\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"SREN16\"", method =() => Room.QueueRoom("SREN16"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"SREN16\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"SREN17\"", method =() => Room.QueueRoom("SREN17"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"SREN17\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"SREN18\"", method =() => Room.QueueRoom("SREN18"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"SREN18\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"HELP\"", method =() => Room.QueueRoom("HELP"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"HELP\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"STATUE\"", method =() => Room.QueueRoom("STATUE"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"STATUE\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"ECHO\"", method =() => Room.QueueRoom("ECHO"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"ECHO\" every couple of seconds until connected." },

                new Module { buttonText = "Auto Join Room \"MOD\"", method =() => Room.QueueRoom("MOD"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"MOD\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"MODS\"", method =() => Room.QueueRoom("MODS"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"MODS\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"HACK\"", method =() => Room.QueueRoom("HACK"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"HACK\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"HACKER\"", method =() => Room.QueueRoom("HACKER"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"HACKER\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"LEMUR\"", method =() => Room.QueueRoom("LEMUR"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"LEMUR\" every couple of seconds until connected. Lemming joins this code very often." },
                new Module { buttonText = "Auto Join Room \"JMANCURLY\"", method =() => Room.QueueRoom("JMANCURLY"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"JMANCURLY\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"JMAN\"", method =() => Room.QueueRoom("JMAN"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"JMAN\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"ELLIOT\"", method =() => Room.QueueRoom("ELLIOT"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"ELLIOT\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"TYLERVR\"", method =() => Room.QueueRoom("TYLERVR"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"TYLERVR\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"JUANGTAG\"", method =() => Room.QueueRoom("JUANGTAG"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"JUANGTAG\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"GHOST\"", method =() => Room.QueueRoom("GHOST"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"GHOST\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"GULLIBLE\"", method =() => Room.QueueRoom("GULLIBLE"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"GULLIBLE\" every couple of seconds until connected." },

                new Module { buttonText = "Auto Join Room \"GAY\"", method =() => Room.QueueRoom("GAY"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"GAY\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"FURRY\"", method =() => Room.QueueRoom("FURRY"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"FURRY\" every couple of seconds until connected." },
                new Module { buttonText = "Auto Join Room \"FORSAKEN\"", method =() => Room.QueueRoom("FORSAKEN"), isTogglable = false, toolTip = "Automatically attempts to connect to room \"FORSAKEN\" every couple of seconds until connected." },
            },

            ["Important Mods"] = new[]
            {
                new Module { buttonText = "Exit Important Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Exit Gorilla Tag", aliases = new[] { "Quit Gorilla Tag", "Exit Game", "Quit Game", "Exit App", "Quit App" }, method = () => Prompt("Are you sure you want to exit Gorilla Tag?", () => { Preferences.Save(); Application.Quit(); }), isTogglable = false, toolTip = "Closes Gorilla Tag.", legal = true},
                new Module { buttonText = "Restart Gorilla Tag", aliases = new[] { "Restart Game", "Restart App" }, method = () => Prompt("Are you sure you want to restart Gorilla Tag?", Important.RestartGame), isTogglable = false, toolTip = "Restarts Gorilla Tag.", legal = true},
                new Module { buttonText = "Open Gorilla Tag Folder", method = Important.OpenGorillaTagFolder, isTogglable = false, toolTip = "Opens the folder in which your game is located.", legal = true},

                new Module { buttonText = "Discord RPC", aliases = new[] { "Self Tracker" }, method = Important.DiscordRPC, disableMethod = Important.DisableDiscordRPC, toolTip = "Gives you a indicator on Discord that you are using Qubit.", legal = true},
                new Module { buttonText = "Media Integration", aliases = new[] { "Spotify" }, enableMethod = Important.EnsureIntegrationProgram, method = Important.MediaIntegration, disableMethod = Important.DisableMediaIntegration, toolTip = "Shows you what media you are watching/listening to in the top left. To switch media, open the menu and use your left joystick.", legal = true},

                new Module { buttonText = "Anti Hand Tap", enableMethod =() => HandTaps.enabled = true, disableMethod =() => HandTaps.enabled = false, toolTip = "Stops all hand tap sounds from being played."},
                new Module { buttonText = "First Person Camera", enableMethod = Important.EnableFPC, postMethod = Important.MoveFPC, disableMethod = Important.DisableFPC, toolTip = "Makes your camera output what you see in VR.", legal = true},
                new Module { buttonText = "Force Enable Hands", enableMethod =() => Important.ForceEnableHands(), disableMethod =() => Important.ForceEnableHands(false), toolTip = "Prevents your hands from disconnecting.", legal = true},

                new Module { buttonText = $"Oculus Report Menu {Bracket("green", "X")}", method = Important.OculusReportMenu, toolTip = "Opens the Oculus report menu when holding <color=green>X</color>."},

                new Module { buttonText = "Accept TOS", enableMethod =() => TermsOfService.enabled = true, method = Important.AcceptTOS, disableMethod =() => TermsOfService.enabled = false, toolTip = "Accepts the Terms of Service for you."},
                new Module { buttonText = "Bypass K-ID Restrictions", overlapText = "Bypass k-ID Restrictions", method =() => Permissions.enabled = true, disableMethod =() => Permissions.enabled = false, toolTip = "Bypasses the permission restrictions held by k-ID for underage users."},

                new Module { buttonText = "Copy Player Position", method = Important.CopyPlayerPosition, isTogglable = false, toolTip = "Copies the current player position to the clipboard.", legal = true },

                new Module { buttonText = "Clear Notifications", method = Notifications.Clear, isTogglable = false, toolTip = "Clears your notifications.", legal = true},

                new Module { buttonText = "Anti AFK", enableMethod =() => ManagerRegistry.PhotonNetworkController.disableAFKKick = true, disableMethod =() => ManagerRegistry.PhotonNetworkController.disableAFKKick = false, toolTip = "Doesn't let you get kicked for being AFK."},
                new Module { buttonText = "Disable Network Triggers", enableMethod =() => JoinTriggers.enabled = true, disableMethod =() => JoinTriggers.enabled = false, toolTip = "Disables the network triggers, so you can change maps without disconnecting."},
                new Module { buttonText = "Disable Map Triggers", enableMethod =() => GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab").SetActive(false), disableMethod =() => GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab").SetActive(true), toolTip = "Disables the map triggers, so you can change maps without loading them."},
                new Module { buttonText = "Disable Quit Box", enableMethod =() => QuitBoxes.enabled = false, disableMethod =() => QuitBoxes.enabled = true, toolTip = "Disables the box under the map that closes your game."},
                new Module { buttonText = "Physical Quit Box", enableMethod = Important.PhysicalQuitbox, disableMethod = Important.DisablePhysicalQuitbox, toolTip = "Makes the quitbox physical, letting you see and walk on it."},
                new Module { buttonText = "Stump Quit Box", enableMethod =() => QuitBoxes.teleportToStump = true, disableMethod =() => QuitBoxes.teleportToStump = false, toolTip = "Disables the box under the map that closes your game."},
                new Module { buttonText = "Block on Mute", method = Important.BlockOnMute, toolTip = "Disables any muted players' rig unless you need to see them."},

                new Module { buttonText = "Steam Refund Timer", method =() => { if (playTime > 6000f) { Notifications.information["REFUND"] = "Refund soon"; } else { Notifications.information.Remove("REFUND"); } }, enableMethod = Important.CheckNewAcc, disableMethod =() => Notifications.information.Remove("REFUND"), toolTip = "Alerts you when you are nearby the steam refund time."},
                new Module { buttonText = "Advanced Ban Message", enableMethod =() => ErrorMessages.enabled = true, disableMethod =() => ErrorMessages.enabled = false, toolTip = "Shows more information, such as remaining time and unban date, when banned.", legal = true},

                new Module { buttonText = "120 FPS", method =() => Important.CapFPS(120), toolTip = "Caps your FPS at 120 frames per second."},
                new Module { buttonText = "90 FPS", method =() => Important.CapFPS(90), toolTip = "Caps your FPS at 90 frames per second."},
                new Module { buttonText = "72 FPS", method =() => Important.CapFPS(72), toolTip = "Caps your FPS at 72 frames per second."},
                new Module { buttonText = "60 FPS", method =() => Important.CapFPS(60), toolTip = "Caps your FPS at 60 frames per second."},
                new Module { buttonText = "45 FPS", method =() => Important.CapFPS(45), toolTip = "Caps your FPS at 45 frames per second."},
                new Module { buttonText = "30 FPS", method =() => Important.CapFPS(30), toolTip = "Caps your FPS at 30 frames per second."},
                new Module { buttonText = "15 FPS", method =() => Important.CapFPS(15), toolTip = "Caps your FPS at 15 frames per second."},
                new Module { buttonText = "Unlock FPS", method = Important.UncapFPS, disableMethod =() => Application.targetFrameRate = 144, toolTip = "Unlocks your FPS."},

                new Module { buttonText = "PC Button Click", aliases = new[] { "PC Click" }, method = Important.PCButtonClick, disableMethod = Important.DisablePCButtonClick, toolTip = "Lets you click in-game buttons with your mouse."},
                new Module { buttonText = "PC Controller Emulation", method = Important.PCControllerEmulation, toolTip = "Allows you to press buttons on your in-game controllers using your keyboard."},
                new Module { buttonText = "Unlock Competitive Queue", method =() => References.Computer.CompQueueUnlockButtonPress(), isTogglable = false, toolTip = "Permanently unlocks the competitive queue."},
                new Module { buttonText = "Change Queue to Default", overlapText = $"Change Queue {Bracket("green", "Default")}", method =() => References.Computer.currentQueue = "DEFAULT", isTogglable = false, toolTip = "Changes your queue to default."},
                new Module { buttonText = "Change Queue to Minigames", overlapText = $"Change Queue {Bracket("green", "Minigames")}", method =() => References.Computer.currentQueue = "MINIGAMES", isTogglable = false, toolTip = "Changes your queue to minigames."},
                new Module { buttonText = "Change Queue to Competitive", overlapText = $"Change Queue {Bracket("green", "Competitive")}", method =() => References.Computer.currentQueue = "COMPETITIVE", isTogglable = false, toolTip = "Changes your queue to competitive."},

                new Module { buttonText = "Disable April Fools FX", enableMethod = Important.DisableAprilFoolsFX, disableMethod = Important.EnableAprilFoolsFX, toolTip = "Disables the April Fools effect in Gorilla Tag 2026."},

                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Casual")}", method =() => References.Computer.SetGameModeWithoutButton("Casual"), isTogglable = false, toolTip = "Changes your target gamemode to casual."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Infection")}", method =() => References.Computer.SetGameModeWithoutButton("Infection"), isTogglable = false, toolTip = "Changes your target gamemode to infection."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Hunt")}", method =() => References.Computer.SetGameModeWithoutButton("HuntDown"), isTogglable = false, toolTip = "Changes your target gamemode to hunt."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Paintbrawl")}", method =() => References.Computer.SetGameModeWithoutButton("Paintbrawl"), isTogglable = false, toolTip = "Changes your target gamemode to paintbrawl."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Ambush")}", method =() => References.Computer.SetGameModeWithoutButton("Ambush"), isTogglable = false, toolTip = "Changes your target gamemode to ambush."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Freeze Tag")}", method =() => References.Computer.SetGameModeWithoutButton("FreezeTag"), isTogglable = false, toolTip = "Changes your target gamemode to freeze tag."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Ghost Tag")}", method =() => References.Computer.SetGameModeWithoutButton("Ghost"), isTogglable = false, toolTip = "Changes your target gamemode to ghost tag."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Custom")}", method =() => References.Computer.SetGameModeWithoutButton("Custom"), isTogglable = false, toolTip = "Changes your target gamemode to custom."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Guardian")}", method =() => References.Computer.SetGameModeWithoutButton("Guardian"), isTogglable = false, toolTip = "Changes your target gamemode to guardian."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Prop Hunt")}", method =() => References.Computer.SetGameModeWithoutButton("PropHunt"), isTogglable = false, toolTip = "Changes your target gamemode to prop hunt."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Super Infection")}", method =() => References.Computer.SetGameModeWithoutButton("SuperInfect"), isTogglable = false, toolTip = "Changes your target gamemode to super infection."},
                new Module { buttonText = $"Change Target Gamemode {Bracket("green", "Error")}", method =() => References.Computer.SetGameModeWithoutButton("None"), isTogglable = false, toolTip = "Changes your target gamemode to none."},

                new Module { buttonText = "Connect to US", aliases = new[] { "US VPN", "Connect to United States", "United States VPN" }, method =() => Important.ConnectToRegion("us"), toolTip = "Connects you to the United States servers."},
                new Module { buttonText = "Connect to US West", aliases = new[] { "Connect to USW", "USW VPN", "US West VPN", "Connect to United States West", "United States West VPN" }, method =() => Important.ConnectToRegion("usw"), toolTip = "Connects you to the western United States servers."},
                new Module { buttonText = "Connect to EU", aliases = new[] { "EU VPN", "Connect to Europe", "Europe VPN" }, method =() => Important.ConnectToRegion("eu"), toolTip = "Connects you to the Europe servers."},

                new Module { buttonText = "Reauthenticate", method = ManagerRegistry.MothershipAuthenticator.BeginLoginFlow, isTogglable = false, toolTip = "Restarts the login flow that happens at the beginning of the game.", legal = true},
            },

            ["Safety Mods"] = new[]
            {
                new Module { buttonText = "Exit Safety Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "No Finger Movement", aliases = new[] { "Disable Fingers" }, postMethod = Safety.NoFinger, toolTip = "Makes your fingers not move, so you can use wall walk without getting called out." },

                new Module { buttonText = $"Fake Oculus Menu {Bracket("green", "X")}", method = Safety.FakeOculusMenu, toolTip = "Imitates opening your Oculus menu when holding <color=green>X</color>."},
                new Module { buttonText = $"Fake Report Menu {Bracket("green", "Y")}", method = Safety.FakeReportMenu, toolTip = "Imitates opening the report menu when holding <color=green>Y</color>."},
                new Module { buttonText = $"Fake Broken Controller {Bracket("green", "X")}", enableMethod =() => GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHandTriggerCollider").GetComponent<Collider>().enabled = false, method = Safety.FakeBrokenController, disableMethod =() => GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHandTriggerCollider").GetComponent<Collider>().enabled = true, toolTip = "Makes you look like your left controller is broken, hold <color=green>X</color> to move your right hand with your left hand."},
                new Module { buttonText = $"Fake Power Off {Bracket("green", "J")}", method = Safety.FakePowerOff, toolTip = "Imitates turning off your headset when holding down your <color=green>joystick</color>."},
                new Module { buttonText = $"Fake Valve Tracking {Bracket("green", "J")}", enableMethod =() => Torso.VRRigLateUpdate += Safety.FakeValveTracking, disableMethod =() => Torso.VRRigLateUpdate -= Safety.FakeValveTracking, toolTip = "Imitates what happens when your headset disconnects on a Valve Index when holding your <color=green>right joystick</color>."},

                new Module { buttonText = "Disable Gamemode Buttons", enableMethod =() => Safety.SetGamemodeButtonActive(false), disableMethod =() => Safety.SetGamemodeButtonActive(), toolTip = "Disables the gamemode buttons."},
                new Module { buttonText = "Support Page Spoof", method = Safety.SpoofSupportPage, toolTip = "Makes the support page appear as if you are on Oculus."},

                new Module { buttonText = "Flush RPCs", method = Security.RPCProtection, isTogglable = false, toolTip = "Flushes all RPC calls, good after you stop spamming." },
                new Module { buttonText = "Anti Crash", overlapText = "Exploit Guard", aliases = new[] { "Anti Crash" }, enableMethod =() => AntiCrashPatches.enabled = true, disableMethod =() => AntiCrashPatches.enabled = false, toolTip = "Prevents crashers from completely annihilating your computer.", legal = true},
                new Module { buttonText = "Anti Ban Crash", enableMethod =() => AntiBanCrash1.enabled = true, disableMethod =() => AntiBanCrash1.enabled = false, toolTip = "Prevents your game from crashing when you are banned."},
                new Module { buttonText = "Anti Kick", enableMethod = Safety.OnlySerializeNecessary, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Only networks the necessities to prevent getting kicked."},
                new Module { buttonText = "Anti Name Ban", enableMethod =() => BanPatches.enabled = true, method = Safety.AntiNameBan, disableMethod =() => BanPatches.enabled = false, toolTip = "Prevents you from getting banned for setting your name to bad things."},
                new Module { buttonText = "Anti Stump Kick", enableMethod =() => GroupSearch.enabled = true, disableMethod =() => GroupSearch.enabled = false, toolTip = "Stops people from group kicking you.", legal = true},
                new Module { buttonText = "Auto Clear Cache", method = Safety.AutoClearCache, toolTip = "Automatically clears your game's cache (garbage collector) every minute to prevent memory leaks.", legal = true},
                new Module { buttonText = "Anti Moderator", method = Safety.AntiModerator, toolTip = "When someone with the stick joins, you get disconnected and their player ID and room code gets saved to a file."},
                new Module { buttonText = "Anti Content Creator", method = Safety.AntiContentCreator, toolTip = "When a content creator joins, you get disconnected and their player ID and room code gets saved to a file."},
                new Module { buttonText = "Cosmetic Notifications", method = Safety.CosmeticNotifications, toolTip = "Sends you a notification if there is a Finger Painter, Illustrator, Administrator, Stick, Forest Guide, or Another Axiom Creator in your room."},

                new Module { buttonText = "Steam Detector", method = Safety.SteamDetector, toolTip = "Detects when a player in your room is on Steam.", legal = true},

                new Module { buttonText = "Bypass Automod", method = Safety.BypassAutomod, toolTip = "Attempts to bypass automod muting yourself and others."},
                new Module { buttonText = "Bypass Mod Checkers", enableMethod =() => PropertyOverrides.enabled = true, method = Safety.BypassModCheckers, disableMethod =() => PropertyOverrides.enabled = false, toolTip = "Tells players using mod checkers that you have no mods."},
                new Module { buttonText = "Bypass Cosmetic Check", method =() => CosmeticRequests.bypassCosmeticCheck = true, disableMethod =() => CosmeticRequests.bypassCosmeticCheck = false, toolTip = "Turns off the networking for any cosmetic mods, stopping people from seeing if you're using one."},

                new Module { buttonText = "Anti Predictions", enableMethod = Safety.AntiPredictions, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Prevents people from checking if your predictions are too high."},

                new Module { buttonText = $"Anti Report {Bracket("green", "Disconnect")}", method = Safety.AntiReportDisconnect, toolTip = "Disconnects you from the room when anyone comes near your report button."},
                new Module { buttonText = $"Anti Report {Bracket("green", "Reconnect")}", method = Safety.AntiReportReconnect, toolTip = "Disconnects and reconnects you from the room when anyone comes near your report button."},
                new Module { buttonText = $"Anti Report {Bracket("green", "Join Random")}", method = Safety.AntiReportJoinRandom, toolTip = "Connects you to a random the room when anyone comes near your report button."},

                new Module { buttonText = $"Anti Report {Bracket("green", "Oculus")}", enableMethod = Safety.EnableAntiOculusReport, disableMethod = Safety.DisableAntiOculusReport, toolTip = "Disconnects you from the room when you get reported with the Oculus report menu."},
                new Module { buttonText = $"Anti Report {Bracket("green", "Anti Cheat")}", enableMethod =() => SendReport.AntiACReport = true, disableMethod =() => SendReport.AntiACReport = false, toolTip = "Disconnects you from the room when you get reported by the anti cheat."},
                new Module { buttonText = $"Anti Report {Bracket("green", "Notify")}", method = Safety.AntiReportNotify, toolTip = "Tells you when people come near your report button, but doesn't do anything."},
                new Module { buttonText = $"Anti Report {Bracket("green", "Overlay")}", method = Safety.AntiReportOverlay, toolTip = "Shows you an overlay when people come near your report button, but doesn't do anything."},

                new Module { buttonText = $"Show Anti Cheat Reports {Bracket("green", "Self")}", enableMethod =() => SendReport.AntiCheatSelf = true, disableMethod =() => SendReport.AntiCheatSelf = false, toolTip = "Gives you a notification every time you have been reported by the anti cheat."},
                new Module { buttonText = $"Show Anti Cheat Reports {Bracket("green", "All")}", enableMethod =() => SendReport.AntiCheatAll = true, disableMethod =() => SendReport.AntiCheatAll = false, toolTip = "Gives you a notification every time anyone has been reported by the anti cheat."},

                new Module { buttonText = "Change Identity", method = Safety.ChangeIdentity, isTogglable = false, toolTip = "Changes your name and color to something a new player would have."},
                new Module { buttonText = $"Change Identity {Bracket("green", "Normal")}", method = Safety.ChangeIdentityRegular, isTogglable = false, toolTip = "Changes your name and color to something a regular player would have."},
                new Module { buttonText = $"Change Identity {Bracket("green", "Custom")}", method = Safety.ChangeIdentityCustom, isTogglable = false, toolTip = "Changes your name and color to whatever you desire."},

                new Module { buttonText = "Change Identity on Disconnect", method =() => Safety.ChangeIdentityOnDisconnect(Safety.ChangeIdentity), toolTip = "When you leave, your name and color will be set to something a new player would have."},
                new Module { buttonText = $"Change Identity on Disconnect {Bracket("green", "Normal")}", method =() => Safety.ChangeIdentityOnDisconnect(Safety.ChangeIdentityRegular), toolTip = "When you leave, your name and color will be set to something a regular player would have."},
                new Module { buttonText = $"Change Identity on Disconnect {Bracket("green", "Child")}", method =() => Safety.ChangeIdentityOnDisconnect(Safety.ChangeIdentityCustom), toolTip = "When you leave, your name and color will be set to whatever you desire."},

                new Module { buttonText = "FPS Spoof", enableMethod =() => PlayerStats.SpoofFPS = true, disableMethod =() => PlayerStats.SpoofFPS = false, toolTip = "Makes your FPS appear different for other players."},
                new Module { buttonText = "Target FPS Spoof", enableMethod =() => PlayerStats.SpoofTargetFPS = true, disableMethod =() => PlayerStats.SpoofTargetFPS = false, toolTip = "Makes your FPS appear different for other players."},
                new Module { buttonText = "Ping Spoof", enableMethod =() => PlayerStats.SpoofPing = true, disableMethod =() => PlayerStats.SpoofPing = false, toolTip = "Makes your ping appear different for other players."},

                new Module { buttonText = "Name Spoof", method = Safety.NameSpoof, toolTip = "Changes your name on the leaderboard to something random, but not on your rig."},
                new Module { buttonText = "Color Spoof", method = Safety.ColorSpoof, toolTip = "Makes your color appear different to every player."},

                new Module { buttonText = "Unload Menu", method = () => Prompt("Are you sure you want unload the menu?", UnloadMenu), isTogglable = false, toolTip = "Unloads the menu from your game."},
                new Module { buttonText = "Disable Anti Telemetry", enableMethod =() => TelemetryPatches.enabled = false, disableMethod =() => TelemetryPatches.enabled = true, toolTip = "Allows the game to send log data to Gorilla Tag's servers." }
            },

            ["Movement Mods"] = new[]
            {
                new Module { buttonText = "Exit Movement Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Platforms", overlapText = $"Platforms {Bracket("green", "G")}", postMethod =() => Movement.Platforms(), toolTip = "Spawns platforms on your hands when holding <color=green>grip</color>."},
                new Module { buttonText = "Trigger Platforms", overlapText = $"Trigger Platforms {Bracket("green", "T")}", postMethod =() => Movement.Platforms(leftTrigger > 0.5f, rightTrigger > 0.5f), toolTip = "Spawns platforms on your hands when holding <color=green>trigger</color>."},
                new Module { buttonText = "Frozone", overlapText = $"Frozone {Bracket("green", "G")}", method = Movement.Frozone, toolTip = "Spawns slippery blocks under your hands using <color=green>grip</color>."},
                new Module { buttonText = $"Platform Spam {Bracket("green", "G")}", method = Movement.PlatformSpam, toolTip = "Spawns legacy platforms rapidly at your hand for those who have networked platforms."},
                new Module { buttonText = "Platform Gun", method = Movement.PlatformGun, toolTip = "Spawns legacy platforms rapidly wherever your hand desires for those who have networked platforms."},

                new Module { buttonText = $"Fly {Bracket("green", "A")}", aliases = new[] { "Super Monke" }, method = Movement.Fly, toolTip = "Sends your character forwards when holding <color=green>A</color>."},
                new Module { buttonText = $"Trigger Fly {Bracket("green", "T")}", method = Movement.TriggerFly, toolTip = "Sends your character forwards when holding <color=green>trigger</color>."},
                new Module { buttonText = $"Noclip Fly {Bracket("green", "A")}", method = Movement.NoclipFly, toolTip = "Sends your character forwards and makes you go through objects when holding <color=green>A</color>."},
                new Module { buttonText = $"Joystick Fly {Bracket("green", "J")}", method = Movement.JoystickFly, toolTip = "Sends your character in whatever direction you are pointing your <color=green>joystick</color> in."},
                new Module { buttonText = $"Bark Fly {Bracket("green", "J")}", postMethod = Movement.BarkFly, toolTip = "Acts like the fly that Bark has. Credits to KyleTheScientist."},
                new Module { buttonText = $"Hand Fly {Bracket("green", "A")}", method = Movement.HandFly, toolTip = "Sends your character in your hand's direction when holding <color=green>A</color>."},
                new Module { buttonText = "Fly Towards Gun", method = Movement.FlyTowardsGun, toolTip = "Sends your character towards whoever your hand desires."},
                new Module { buttonText = $"Slingshot Fly {Bracket("green", "A")}", method = Movement.SlingshotFly, toolTip = "Sends your character forwards, in a more elastic manner, when holding <color=green>A</color>."},
                new Module { buttonText = $"Zero Gravity Slingshot Fly {Bracket("green", "A")}", method = Movement.ZeroGravitySlingshotFly, toolTip = "Sends your character forwards, in a more elastic manner without gravity, when holding <color=green>A</color>."},
                new Module { buttonText = $"Slingshot Bark Fly {Bracket("green", "J")}", method = Movement.VelocityBarkFly, toolTip = "Acts like the fly that Bark has, mixed with slingshot fly. Credits to KyleTheScientist."},
                new Module { buttonText = "WASD Fly", aliases = new[] { "PC Fly" }, enableMethod = Movement.EnableWASDFly, method = Movement.WASDFly, disableMethod =() => References.Player.GetControllerTransform(false).parent.rotation = Quaternion.Euler(0, 0, 0), toolTip = "Moves your rig with <color=green>WASD</color>."},

                new Module { buttonText = $"Dash {Bracket("green", "A")}", method = Movement.Dash, toolTip = "Flings your character forwards when pressing <color=green>A</color>."},
                new Module { buttonText = $"Reverse Velocity {Bracket("green", "A")}", method = Movement.ReverseVelocity, toolTip = "Reverses your current velocity when you press <color=green>A</color>."},
                new Module { buttonText = "Bird Fly", method = Movement.BirdFly, toolTip = "Makes you fly like a bird when you flap your wings."},
                new Module { buttonText = "Iron Man", aliases = new[] { "Iron Monke" }, method = Movement.IronMan, toolTip = "Turns you into iron man, rotate your hands around to change direction."},
                new Module { buttonText = "Spider Man", aliases = new[] { "Spider Monke" }, method = Movement.SpiderMan, disableMethod = Movement.DisableSpiderMan, toolTip = "Turns you into spider man, use your <color=green>grips</color> to shoot webs."},
                new Module { buttonText = "Grappling Hooks", method = Movement.GrapplingHooks, disableMethod = Movement.DisableSpiderMan, toolTip = "Gives you grappling hooks, use your <color=green>grips</color> to shoot them."},
                new Module { buttonText = "Portal Gun", aliases = new[] { "Portals" }, method = Movement.PortalGun, disableMethod = Movement.DisablePortalGun, toolTip = "Gives you a gun to spawn portals that can be seen and walked through."},

                new Module { buttonText = $"Drive {Bracket("green", "J")}", aliases = new[] { "Car Monke" }, method = Movement.Drive, toolTip = "Lets you drive around in your invisible car. Use the <color=green>joystick</color> to move."},
                new Module { buttonText = $"Hard Drive {Bracket("green", "J")}", overlapText = $"Sticky Drive {Bracket("green", "J")}", method = Movement.HardDrive, toolTip = "Similar to drive, but locks you to the ground."},

                new Module { buttonText = $"Noclip {Bracket("green", "T")}", method = Movement.Noclip, toolTip = "Makes you go through objects when holding <color=green>trigger</color>."},
                new Module { buttonText = "Up And Down", method = Movement.UpAndDown, toolTip = "Makes you go up when holding your <color=green>trigger</color>, and down when holding your <color=green>grip</color>."},
                new Module { buttonText = "Left And Right", method = Movement.LeftAndRight, toolTip = "Makes you go left when holding your <color=green>trigger</color>, and right when holding your <color=green>grip</color>."},
                new Module { buttonText = "Forwards And Backwards", method = Movement.ForwardsAndBackwards, toolTip = "Makes you go forwards when holding your <color=green>trigger</color>, and backwards when holding your <color=green>grip</color>."},

                new Module { buttonText = "Size Changer", method = Movement.SizeChanger, enableMethod = Movement.DisableSizeChanger, disableMethod = Movement.DisableSizeChanger, toolTip = "Increase your size by holding <color=green>trigger</color>, and decrease your size by holding <color=green>grip</color>."},

                new Module { buttonText = $"Auto Walk {Bracket("green", "J")}", aliases = new[] { "Ghost Walk", "Walk Simulator" }, method = Movement.AutoWalk, toolTip = "Makes your character automatically walk when using the <color=green>joystick</color>."},
                new Module { buttonText = $"Auto Funny Run {Bracket("green", "G")}", method = Movement.AutoFunnyRun, toolTip = "Makes your character automatically funny run when holding <color=green>grip</color>."},
                new Module { buttonText = $"Auto Pinch Climb {Bracket("green", "G")}", method = Movement.AutoPinchClimb, toolTip = "Makes your character automatically pinch climb when holding <color=green>grip</color>."},
                new Module { buttonText = $"Auto Elevator Climb {Bracket("green", "G")}", method = Movement.AutoElevatorClimb, toolTip = "Makes your character automatically elevator climb when holding <color=green>grip</color>."},
                new Module { buttonText = $"Auto Branch {Bracket("green", "G")}", method = Movement.AutoBranch, toolTip = "Makes your character automatically branch when holding <color=green>grip</color>."},

                new Module { buttonText = "Force Tag Freeze", method = Movement.ForceTagFreeze, disableMethod = Movement.NoTagFreeze, toolTip = "Forces tag freeze on your character."},
                new Module { buttonText = "No Tag Freeze", method = Movement.NoTagFreeze, toolTip = "Disables tag freeze on your character."},
                new Module { buttonText = "Feather Falling", postMethod = Movement.FeatherFalling, toolTip = "Makes you fall like a feather."},
                new Module { buttonText = "Low Gravity", aliases = new[] { "Moon Gravity" }, postMethod = Movement.LowGravity, toolTip = "Makes gravity lower on your character."},
                new Module { buttonText = "Zero Gravity", aliases = new[] { "No Gravity" }, postMethod = Movement.ZeroGravity, toolTip = "Disables gravity on your character."},
                new Module { buttonText = "High Gravity", aliases = new[] { "Mars Gravity" }, postMethod = Movement.HighGravity, toolTip = "Makes gravity higher on your character."},
                new Module { buttonText = "Reverse Gravity", aliases = new[] { "Flip Gravity" }, enableMethod = Movement.ReverseGravity, disableMethod = Movement.UnflipCharacter, toolTip = "Reverses gravity on your character."},

                new Module { buttonText = $"Rewind {Bracket("green", "T")}", aliases = new[] { "Reverse" }, method = Movement.Rewind, disableMethod = Movement.ClearRewind, toolTip = "Brings you back in time when holding <color=green>trigger</color>."},
                new Module { buttonText = "Macros", method =() => CurrentCategoryName = "Macros", isTogglable = false, toolTip = "Opens a category to manage your macros."},

                new Module { buttonText = $"Wall Walk {Bracket("green", "G")}", postMethod = Movement.WallWalk, toolTip = "Makes you get brought towards any wall you touch when holding <color=green>grip</color>."},
                new Module { buttonText = $"Legitimate Wall Walk {Bracket("green", "G")}", method = Movement.LegitimateWallWalk, toolTip = "Makes you get brought towards any wall you touch when holding <color=green>grip</color>, but less noticable."},
                new Module { buttonText = "Spider Walk", aliases = new[] { "Spider Monke", "Spider Monkey" }, method = Movement.SpiderWalk, disableMethod = Movement.UnflipCharacter, toolTip = "Makes your gravity and character towards any wall you touch. This may cause motion sickness."},

                new Module { buttonText = "Teleport to Random", method = Movement.TeleportToRandom, isTogglable = false, toolTip = "Teleports you to a random player."},
                new Module { buttonText = "Teleport to Map", method = Movement.EnterTeleportToMap, isTogglable = false, toolTip = "Teleports you to a map of your choosing."},
                new Module { buttonText = "Teleport Gun", method = Movement.TeleportGun, toolTip = "Teleports to wherever your hand desires."},
                new Module { buttonText = "Airstrike", method = Movement.Airstrike, toolTip = "Teleports to wherever your hand desires, except farther up, then launches you back down."},

                new Module { buttonText = $"Checkpoint {Bracket("green", "G")}", method = Movement.Checkpoint, disableMethod = Movement.DisableCheckpoint, toolTip = "Place a checkpoint with <color=green>grip</color> and teleport to it with <color=green>A</color>."},
                new Module { buttonText = $"Advanced Checkpoints {Bracket("green", "G")}", method = Movement.AdvancedCheckpoints, disableMethod = Movement.DisableAdvancedCheckpoints, toolTip = "Place checkpoints with <color=green>grip</color>, use your joystick to swap between checkpoints, and teleport to your selected checkpoint with <color=green>A</color>."},
                new Module { buttonText = $"Ender Pearl {Bracket("green", "G")}", method = Movement.EnderPearl, disableMethod = Movement.DestroyEnderPearl, toolTip = "Gives you a throwable ender pearl when holding <color=green>grip</color>."},
                new Module { buttonText = $"C4 {Bracket("green", "G")}", aliases = new[] { "Bomb" }, method = Movement.Bomb, disableMethod = Movement.DisableBomb, toolTip = "Place a C4 with <color=green>grip</color> and detonate it with <color=green>A</color>."},

                new Module { buttonText = "Punch Mod", method = Movement.PunchMod, toolTip = "Lets people punch you across the map."},
                new Module { buttonText = "Telekinesis", method = Movement.Telekinesis, toolTip = "Lets people control you with nothing but the power of their finger."},
                new Module { buttonText = "Safety Bubble", method = Movement.SafetyBubble, toolTip = "Moves you away from players if they get too close to you."},
                new Module { buttonText = "Solid Players", aliases = new[] { "Solid Monke" }, method = Movement.SolidPlayers, disableMethod = Movement.DisableSolidPlayers, toolTip = "Lets you walk on top of other players."},
                new Module { buttonText = $"Pull Mod {Bracket("green", "G")}", method = Movement.PullMod, toolTip = "Pulls you more whenever you walk to simulate speed without modifying your velocity."},
                new Module { buttonText = $"Long Jump {Bracket("green", "A")}", overlapText = $"Playspace Abuse {Bracket("green", "A")}", aliases = new[] { "Long Jump" }, method = Movement.PlayspaceAbuse, toolTip = "Makes you look like you're legitimately long jumping when holding <color=green>A</color>."},
                new Module { buttonText = "Velocity Long Arms", overlapText = "Predictions", aliases = new[] { "Velocity Long Arms" }, enableMethod = Movement.CreateVelocityTrackers, method = Movement.VelocityLongArms, disableMethod = Movement.DestroyVelocityTrackers, toolTip = "Moves your arms farther depending on how fast you move them."},

                new Module { buttonText = "Timer", enableMethod =() => TaggerTimer.enabled = true, method = Movement.Timer, disableMethod =() => { TaggerTimer.enabled = false; Time.timeScale = 1f; References.Player.debugMovement = false;  }, toolTip = "Speeds up or slows down the time of your game."},

                new Module { buttonText = "Speed Boost", method = Movement.SpeedBoost, toolTip = "Changes your speed to whatever you set it to."},
                new Module { buttonText = "Grip Speed Boost", method =() => { if (rightGrab) { Movement.SpeedBoost(); } }, toolTip = "Changes your speed to whatever you set it to, if you're holding right grip."},
                new Module { buttonText = "Dynamic Speed Boost", method = Movement.DynamicSpeedBoost, toolTip = "Dynamically changes your speed to whatever you set it to when tagged players get closer to you."},
                new Module { buttonText = "Uncap Max Velocity", aliases = new[] { "Velmax" }, method =() => References.Player.maxJumpSpeed = float.MaxValue, toolTip = "Removes the velocity limit of walking."},
                new Module { buttonText = "Always Max Velocity", method = Movement.AlwaysMaxVelocity, toolTip = "Always makes you go as fast as the velocity limit."},
                new Module { buttonText = "Disable Velocity Cap", enableMethod = Movement.DisableVelocityCap, disableMethod =() => Movement.playspace.enabled = true, toolTip = "Lets you go as fast as you want without hitting the velocity limit."},

                new Module { buttonText = "Funny Movement", overlapText = "Exponential Movement", aliases = new[] { "Funny Movement" }, method = Movement.FunMove, toolTip = "Multiplies your velocity every frame, making you exponential."},

                new Module { buttonText = "Slip Slap", enableMethod = Movement.SlipSlap, disableMethod = Movement.DisableSlipSlap, toolTip = "Allows you to slip slap again."},
                new Module { buttonText = "Slippery Hands", overlapText = "Slippery Surfaces", aliases = new[] { "Slippery Hands" }, enableMethod =() => SlidePercentage.everythingSlippery = true, disableMethod =() => SlidePercentage.everythingSlippery = false, toolTip = "Makes everything ice, as in extremely slippery."},
                new Module { buttonText = "Grippy Hands", overlapText = "No Slippery Surfaces", aliases = new[] { "Grippy Hands" }, enableMethod =() => SlidePercentage.everythingGrippy = true, disableMethod =() => SlidePercentage.everythingGrippy = false, toolTip = "Disables any slipperiness of any surfaces."},
                new Module { buttonText = "Slippery Surface Helper", aliases = new[] { "Slippery Wall Helper" }, enableMethod =() => SlidePercentage.minimalSlip = true, disableMethod =() => SlidePercentage.minimalSlip = false, toolTip = "Helps you stick to slippery walls more, but doesn't remove the slipperiness."},
                new Module { buttonText = "Remove Forest Colliders", enableMethod = Movement.RemoveForestColliders, disableMethod = Movement.RestoreForestColliders, toolTip = "Removes the colliders on the roof and the entrance to tutorial inside Forest."},
                new Module { buttonText = "Sticky Hands", method = Movement.StickyHands, disableMethod = Movement.DisableStickyHands, toolTip = "Makes your hands really sticky."},
                new Module { buttonText = "Climby Hands", method = Movement.ClimbyHands, disableMethod = Movement.DisableClimbyHands, toolTip = "Lets you climb everything like a rope."},
                new Module { buttonText = "Disable Hands", method =() => Movement.SetHandEnabled(false), disableMethod =() => Movement.SetHandEnabled(true), toolTip = "Disables your hand colliders."},
                new Module { buttonText = "Disable Body Collider", method =() => References.BodyCollider.enabled = false, disableMethod =() => References.BodyCollider.enabled = true, toolTip = "Disables your body's collider."},
                new Module { buttonText = "Disable Head Collider", method =() => References.HeadCollider.enabled = false, disableMethod =() => References.HeadCollider.enabled = true, toolTip = "Disables your head's collider."},

                new Module { buttonText = "Slide Control", enableMethod = Movement.EnableSlideControl, disableMethod = Movement.DisableSlideControl, toolTip = "Lets you control yourself on ice perfectly."},
                new Module { buttonText = "Weak Slide Control", enableMethod = Movement.EnableWeakSlideControl, disableMethod = Movement.DisableSlideControl, toolTip = "Lets you control yourself on ice a little more perfect than before."},

                new Module { buttonText = "Throw Controllers", method = Movement.ThrowControllers, toolTip = "Lets you throw your controllers with <color=green>X</color> or <color=green>A</color>."},
                new Module { buttonText = "Controller Flick", aliases = new[] { "DC Flick", "Disconnect Flick" }, enableMethod = Movement.EnableControllerFlick, method = Movement.ControllerFlick, disableMethod = Movement.DisableControllerFlick, toolTip = "Flicks your controllers in a similar way to disconnecting them with <color=green>X</color> or <color=green>A</color>."},

                new Module { buttonText = "Uncap Arm Length", aliases = new[] { "Armmax" }, method =() => { References.Player.leftHand.maxArmLength = float.MaxValue; References.Player.rightHand.maxArmLength = float.MaxValue; }, disableMethod =() => { References.Player.leftHand.maxArmLength = 1; References.Player.rightHand.maxArmLength = 1; }, toolTip = "Removes the arm distance limit."},
                new Module { buttonText = "Steam Long Arms", method = Movement.EnableSteamLongArms, disableMethod = Movement.DisableSteamLongArms, toolTip = "Gives you long arms similar to override world scale."},
                new Module { buttonText = "Stick Long Arms", method = Movement.StickLongArms, toolTip = "Makes you look like you're using sticks."},
                new Module { buttonText = "Multiplied Long Arms", method = Movement.MultipliedLongArms, toolTip = "Gives you a weird version of long arms."},
                new Module { buttonText = "Vertical Long Arms", method = Movement.VerticalLongArms, toolTip = "Gives you a version of long arms to help you vertically."},
                new Module { buttonText = "Horizontal Long Arms", method = Movement.HorizontalLongArms, toolTip = "Gives you a version of long arms to help you horizontally."},

                new Module { buttonText = $"Extenders {Bracket("green", "J")}", method = Movement.Extenders, enableMethod =() => Movement.extendingTime = 0f, disableMethod = Movement.DisableSteamLongArms, toolTip = "Steam long arms, but it slowly disables when holding the right trigger."},

                new Module { buttonText = $"Flick Jump {Bracket("green", "A")}", method = Movement.FlickJump, toolTip = "Makes your hand go down really fast when holding <color=green>A</color>."},

                new Module { buttonText = "Bunny Hop", aliases = new[] { "Bhop" }, method = Movement.BunnyHop, toolTip = "Makes you automatically jump when on the ground."},
                new Module { buttonText = "Strafe", method = Movement.Strafe, toolTip = "Makes you strafe when in the air."},
                new Module { buttonText = "Dynamic Strafe", method = Movement.DynamicStrafe, toolTip = "Makes you dynamically strafe when in the air."},
                new Module { buttonText = $"Ground Helper {Bracket("green", "G")}", method = Movement.GroundHelper, toolTip = "Helps you run on ground when holding <color=green>grip</color>."},

                new Module { buttonText = "Low FPS Movement", enableMethod = Movement.LowFPSMovement, disableMethod =() => References.Tagger._forceFramerateCheck = true, toolTip = "Simulates movement at low FPS by changing how your velocity is stored."},

                new Module { buttonText = "Bouncy", enableMethod = Movement.PreBouncy, method = Movement.Bouncy, disableMethod = Movement.PostBouncy, toolTip = "Makes you really bouncy when on the ground."},
                new Module { buttonText = "Solid Water", aliases = new[] { "Jesus" }, enableMethod = Movement.SolidWater, disableMethod = Movement.FixWater, toolTip = "Makes the water solid in the beach map." },
                new Module { buttonText = "Disable Water", enableMethod = Movement.DisableWater, disableMethod = Movement.FixWater, toolTip = "Disables the water in the beach map." },
                new Module { buttonText = "Air Swim", aliases = new[] { "Fish" }, method = Movement.AirSwim, disableMethod = Movement.DisableAirSwim, toolTip = "Puts you in a block of water, letting you swim in the air." },
                new Module { buttonText = "Fast Swim", method =() => Movement.SetSwimSpeed(10f), disableMethod =() => Movement.SetSwimSpeed(), toolTip = "Lets you swim faster in water." },
                new Module { buttonText = "Liquid Run Helper", enableMethod =() => Movement.LiquidRunHelper(true), disableMethod =() => Movement.LiquidRunHelper(false), toolTip = "Adds back the nostalgic water running to the game." },
                new Module { buttonText = "Disable Air", overlapText = "Disable Wind Barriers", aliases = new[] { "Disable Air" }, enableMethod =() => { ForceVolumes.enabled = true; GetObject("Environment Objects/LocalObjects_Prefab/Forest/Environment/Forest_ForceVolumes/").SetActive(false); GetObject("Environment Objects/LocalObjects_Prefab/ForestToHoverboard/TurnOnInForestAndHoverboard/ForestDome_CollisionOnly").SetActive(false); }, disableMethod =() => { ForceVolumes.enabled = false; GetObject("Environment Objects/LocalObjects_Prefab/Forest/Environment/Forest_ForceVolumes/").SetActive(true); GetObject("Environment Objects/LocalObjects_Prefab/ForestToHoverboard/TurnOnInForestAndHoverboard/ForestDome_CollisionOnly").SetActive(true); }, toolTip = "Disables the wind barriers in every map." },

                new Module { buttonText = $"Ghost {Bracket("green", "A")}", method = Movement.Ghost, disableMethod = Movement.EnableRig, toolTip = "Keeps your rig still when holding <color=green>A</color>."},
                new Module { buttonText = $"Invisible {Bracket("green", "B")}", method = Movement.Invisible, disableMethod = Movement.EnableRig, toolTip = "Makes you go invisible when holding <color=green>B</color>."},

                new Module { buttonText = "Rig Gun", method = Movement.RigGun, toolTip = "Moves your rig to wherever your hand desires."},
                new Module { buttonText = $"Grab Rig {Bracket("green", "G")}", method = Movement.GrabRig, toolTip = "Lets you grab your rig when holding <color=green>grip</color>."},

                new Module { buttonText = "Spin Head X", method =() => Movement.SpinHead("x"), disableMethod = Fun.FixHead, toolTip = "Spins your head on the X axis."},
                new Module { buttonText = "Spin Head Y", method =() => Movement.SpinHead("y"), disableMethod = Fun.FixHead, toolTip = "Spins your head on the Y axis."},
                new Module { buttonText = "Spin Head Z", method =() => Movement.SpinHead("z"), disableMethod = Fun.FixHead, toolTip = "Spins your head on the Z axis."},

                new Module { buttonText = $"Spaz Rig {Bracket("green", "A")}", enableMethod = Movement.EnableSpazRig, method = Movement.SpazRig, disableMethod = Movement.DisableSpazRig, toolTip = "Makes every part of your rig spaz out a little bit when holding <color=green>A</color>."},
                new Module { buttonText = "Spaz Full Body", enableMethod =() => Torso.VRRigLateUpdate += Movement.VRRigLateUpdate_SpazBody, method =() => ghostException = true, disableMethod =() => { Torso.VRRigLateUpdate -= Movement.VRRigLateUpdate_SpazBody; ghostException = false; }, toolTip = "Makes your body spaz in a bunch of different directions."},
                new Module { buttonText = $"Spaz Rig Hands {Bracket("green", "A")}", method = Movement.SpazHands, toolTip = "Makes your rig's hands spaz out everywhere when holding <color=green>A</color>."},
                new Module { buttonText = $"Spaz Hands {Bracket("green", "A")}", method = Movement.SpazRealHands, toolTip = "Makes your hands spaz out everywhere when holding <color=green>A</color>."},
                new Module { buttonText = "Random Spaz Head Position", enableMethod = Movement.EnableSpazHead, method = Movement.RandomSpazHeadPosition, disableMethod = Movement.FixHeadPosition, toolTip = "Makes your head position spaz out for 0 to 1 seconds every 1 to 4 seconds."},
                new Module { buttonText = "Random Spaz Head", overlapText = "Random Spaz Head Rotation", method = Movement.RandomSpazHead, disableMethod = Fun.FixHead, toolTip = "Makes your head rotation spaz out for 0 to 1 seconds every 1 to 4 seconds."},
                new Module { buttonText = "Spaz Head Position", enableMethod = Movement.EnableSpazHead, method = Movement.SpazHeadPosition, disableMethod = Movement.FixHeadPosition, toolTip = "Makes your head position spaz out."},
                new Module { buttonText = "Spaz Head", overlapText = "Spaz Head Rotation", method = Movement.SpazHead, disableMethod = Fun.FixHead, toolTip = "Makes your head rotation spaz out."},
                new Module { buttonText = "Spaz Head X", method =() => Fun.SpazHead("x"), disableMethod = Fun.FixHead, toolTip = "Spaz your head on the X axis."},
                new Module { buttonText = "Spaz Head Y", method =() => Fun.SpazHead("y"), disableMethod = Fun.FixHead, toolTip = "Spaz your head on the Y axis."},
                new Module { buttonText = "Spaz Head Z", method =() => Fun.SpazHead("z"), disableMethod = Fun.FixHead, toolTip = "Spaz your head on the Z axis."},

                new Module { buttonText = "Laggy Rig", method = Movement.LaggyRig, disableMethod = Movement.EnableRig, toolTip = "Makes your rig laggy."},
                new Module { buttonText = "Smooth Rig", method =() => PhotonNetwork.SerializationRate = 30, disableMethod =() => PhotonNetwork.SerializationRate = 10, toolTip = "Makes your rig really smooth."},
                new Module { buttonText = $"Update Rig {Bracket("green", "A")}", method = Movement.UpdateRig, disableMethod = Movement.EnableRig, toolTip = "Freezes your rig in place. Whenever you click <color=green>A</color>, your rig will update."},

                new Module { buttonText = "Freeze Rig Limbs", method = Movement.FreezeRigLimbs, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Makes your hands and head freeze on your rig, but not your body."},
                new Module { buttonText = "Freeze Rig Body", method = Movement.FreezeRigBody, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Makes your body freeze on your rig, but not your hands and head."},
                new Module { buttonText = "Freeze Rig", method = Movement.FreezeRig, disableMethod =() => { References.LocalRig.enabled = true; Movement.startPosition = null; }, toolTip = "Makes your body freeze on your rig, but not your hands and head."},

                new Module { buttonText = "Paralyze Rig", method = Movement.ParalyzeRig, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Removes your arms from your rig."},
                new Module { buttonText = "Chicken Rig", method = Movement.ChickenRig, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Makes your rig look like a chicken."},
                new Module { buttonText = "Amputate Rig", method = Movement.AmputateRig, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Removes all of your limbs from your rig."},
                new Module { buttonText = "Decapitate Rig", enableMethod =() => Torso.VRRigLateUpdate += Movement.DecapitateRigUpdate, disableMethod =() => Torso.VRRigLateUpdate -= Movement.DecapitateRigUpdate, toolTip = "Removes the head from your rig."},

                new Module { buttonText = "Fake Full Body Tracking", enableMethod =() => Torso.VRRigLateUpdate += Movement.VRRigLateUpdate_FakeFBT, disableMethod =() => Torso.VRRigLateUpdate -= Movement.VRRigLateUpdate_FakeFBT, toolTip = "Fakes full body tracking by making your rig follow your camera orientation."},
                new Module { buttonText = "Control Rig Rotation", enableMethod =() => Torso.VRRigLateUpdate += Movement.VRRigLateUpdate_Control, disableMethod =() => Torso.VRRigLateUpdate -= Movement.VRRigLateUpdate_Control, toolTip = "Control your rig rotation with your joysticks or arrow keys. Click either joystick or enter to reset rotation."},

                new Module { buttonText = "Control Body Rotation", postMethod = Movement.ControlBodyRotation, toolTip = "Control your body rotation with your joysticks or arrow keys. Click either joystick or enter to reset rotation."},
                new Module { buttonText = $"Flip {Bracket("green", "A")} {Bracket("green", "B")}", enableMethod =() => Torso.VRRigLateUpdate += Movement.Flip, disableMethod =() => { Torso.VRRigLateUpdate -= Movement.Flip; Movement.UnflipCharacter(); }, toolTip = "Makes you flip back when you click your A button, flip forwards when you click your B button."},
                new Module { buttonText = "Spider Crawl", postMethod = Movement.SpiderCrawl, toolTip = "Makes you look like a spider."},
                new Module { buttonText = "Upwards Body", postMethod = Movement.UpwardsBody, toolTip = "Makes your body point upwards."},

                new Module { buttonText = "Spin Rig Body", method =() => Movement.SetBodyPatch(true), disableMethod =() => Movement.SetBodyPatch(false), toolTip = "Makes your body spin around, but not your head."},
                new Module { buttonText = "Spaz Rig Body", method =() => Movement.SetBodyPatch(true, 1), disableMethod =() => Movement.SetBodyPatch(false), toolTip = "Gives your body a seizure, randomizing its rotation."},
                new Module { buttonText = "Reverse Rig Body", method =() => Movement.SetBodyPatch(true, 2), disableMethod =() => Movement.SetBodyPatch(false), toolTip = "Flips your body around backwards, but not your head."},
                new Module { buttonText = "Rec Room Body", method = Movement.RecRoomBody, disableMethod =() => Movement.SetBodyPatch(false), toolTip = "Makes your rig like how the Rec Room bodies are."},
                new Module { buttonText = "Dinnerbone", enableMethod =() => Torso.VRRigLateUpdate += Movement.VRRigLateUpdate_Dinnerbone, method =() => ghostException = true, disableMethod =() => { Torso.VRRigLateUpdate -= Movement.VRRigLateUpdate_Dinnerbone; ghostException = false;  }, toolTip = "Flips you upside down, inspired by the easter egg in Minecraft."},
                new Module { buttonText = $"Freeze Body Rotation {Bracket("green", "G")}", method = Movement.FreezeBodyRotation, disableMethod =() => Movement.SetBodyPatch(false), toolTip = "Freezes your body rotation in place, but not your head, when holding <color=green>grip</color>."},

                new Module { buttonText = $"Auto Dance {Bracket("green", "A")}", aliases = new[] { "Emote" }, method = Movement.AutoDance, toolTip = "Makes you dance when holding <color=green>A</color>."},
                new Module { buttonText = $"Auto Griddy {Bracket("green", "A")}", method = Movement.AutoGriddy, toolTip = "Makes you griddy when holding <color=green>A</color>."},
                new Module { buttonText = $"Auto T Pose {Bracket("green", "A")}", overlapText = $"T Pose {Bracket("green", "A")}", method = Movement.AutoTPose, toolTip = "Makes you t pose when holding <color=green>A</color>. Good for fly trolling."},
                new Module { buttonText = $"Helicopter {Bracket("green", "A")}", method = Movement.Helicopter, toolTip = "Turns you into a helicopter when holding <color=green>A</color>."},
                new Module { buttonText = $"Beyblade {Bracket("green", "A")}", method = Movement.Beyblade, toolTip = "Turns you into a beyblade when holding <color=green>A</color>."},
                new Module { buttonText = $"Still Beyblade {Bracket("green", "A")}", method = Movement.StillBeyblade, disableMethod =() => Movement.stillBeybladeStartPos = Vector3.zero, toolTip = "Turns you into a beyblade when holding <color=green>A</color>. Doesn't move you."},
                new Module { buttonText = "Spin Bot", enableMethod =() => Torso.VRRigLateUpdate += Movement.TorsoPatch_VRRigLateUpdate, method =() => ghostException = true, disableMethod =() => { Torso.VRRigLateUpdate -= Movement.TorsoPatch_VRRigLateUpdate; ghostException = false; }, toolTip = "Makes your body spin around, but not your head."},
                new Module { buttonText = $"Fan {Bracket("green", "A")}", method = Movement.Fan, toolTip = "Turns you into a fan when holding <color=green>A</color>."},
                new Module { buttonText = "Ghost Animations", method = Movement.GhostAnimations, disableMethod = Movement.DisableGhostAnimations, toolTip = "Makes you look like a ghost, making your movement snappy and slow."},
                new Module { buttonText = "Minecraft Animations", method = Movement.MinecraftAnimations, disableMethod = Movement.EnableRig, toolTip = "Puts your hands down, and makes you walk when holding <color=green>A</color>. You can also point with <color=green>B</color>."},

                new Module { buttonText = "Stare at Nearby", overlapText = "Stare At Player Nearby", enableMethod =() => Torso.VRRigLateUpdate += Movement.StareAtNearby, disableMethod =() => Torso.VRRigLateUpdate -= Movement.StareAtNearby, toolTip = "Makes you stare at the nearest player."},
                new Module { buttonText = "Stare at Player Gun", method = Movement.StareAtGun, disableMethod =() => Torso.VRRigLateUpdate -= Movement.StareAtTarget, toolTip = "Makes you stare at whoever your hand desires."},
                new Module { buttonText = "Stare at All Players", aliases = new[] { "Owl" }, enableMethod = Movement.StareAtAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes you stare at everyone in the room."},
                new Module { buttonText = "Eye Contact", enableMethod =() => Torso.VRRigLateUpdate += Movement.EyeContact, disableMethod =() => Torso.VRRigLateUpdate -= Movement.EyeContact, toolTip = "Makes you stare at anyone who is looking at you."},
                new Module { buttonText = "Floating Rig", enableMethod = Movement.EnableFloatingRig, method = Movement.FloatingRig, disableMethod = Movement.DisableFloatingRig, toolTip = "Makes your rig float."},


                new Module { buttonText = "Bees", method = Movement.Bees, disableMethod = Movement.EnableRig, toolTip = "Makes your rig teleport to random players, imitating the bees ghost."},

                new Module { buttonText = "Piggyback Gun", aliases = new[] { "Ride Gun" }, method = Movement.PiggybackGun, toolTip = "Teleports you on top of whoever your hand desires repeatedly."},
                new Module { buttonText = "Piggyback All", aliases = new[] { "Ride All" }, enableMethod = Movement.PiggybackAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes you appear on top of everyone in the room"},

                new Module { buttonText = "Copy Movement Gun", method = Movement.CopyMovementGun, toolTip = "Makes your rig copy the movement of whoever your hand desires."},
                new Module { buttonText = "Copy Movement All", enableMethod = Movement.CopyMovementAll, disableMethod =() => { ViewSerialization.OverrideSerialization = null; Movement.followPositions.Clear(); }, toolTip = "Makes your rig copy the movement of every player in the room."},

                new Module { buttonText = "Follow Player Gun", method = Movement.FollowPlayerGun, toolTip = "Flies your rig towards whoever your hand desires."},
                new Module { buttonText = "Follow All Players", enableMethod = Movement.FollowAllPlayers, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Flies your rig towards everyone in the room."},

                new Module { buttonText = "Orbit Player Gun", method = Movement.OrbitPlayerGun, toolTip = "Orbits your rig around whoever your hand desires."},
                new Module { buttonText = "Orbit All Players", enableMethod = Movement.OrbitAllPlayers, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Orbits your rig around everyone in the room."},

                new Module { buttonText = "Jumpscare Gun", method = Movement.JumpscareGun, toolTip = "Makes you jumpscare whoever your hand desires."},
                new Module { buttonText = "Jumpscare All", enableMethod = Movement.JumpscareAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes you jumpscare everyone in the room."},

                new Module { buttonText = "Annoy Player Gun", method = Movement.AnnoyPlayerGun, toolTip = "Spazzes your body around whoever your hand desires, with sounds."},
                new Module { buttonText = "Annoy All Players", enableMethod = Movement.AnnoyAllPlayers, method =() => Sound.SoundSpam(337, true), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Spazzes your body around everyone in the room, with sounds."},

                new Module { buttonText = "Intercourse Gun", aliases = new[] { "Sex Gun" }, method = Movement.IntercourseGun, toolTip = "Makes you thrust whoever your hand desires, with sounds."},
                new Module { buttonText = "Intercourse All", aliases = new[] { "Sex All" }, enableMethod = Movement.IntercourseAll, method = Movement.IntercourseNoises, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes you thrust everyone in the room, with sounds."},

                new Module { buttonText = "Head Gun", aliases = new[] { "Blowjob Gun" }, method = Movement.HeadGun, toolTip = "Makes you thrust whoever your hand desires, but lower, with sounds."},
                new Module { buttonText = "Head All", aliases = new[] { "Blowjob All" }, enableMethod = Movement.HeadAll, method = Movement.IntercourseNoises, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes you thrust everyone in the room, but lower, with sounds."}
            },

            ["Advantage Mods"] = new[]
            {
                new Module { buttonText = "Exit Advantage Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Tag Self", method = Advantages.TagSelf, disableMethod = Movement.EnableRig, toolTip = "Attempts to tags yourself."},
                new Module { buttonText = "Tag Gun", method = Advantages.TagGun, toolTip = "Tags whoever your hand desires."},
                new Module { buttonText = "Tag All", method = Advantages.TagAll, disableMethod = Movement.EnableRig, toolTip = "Attempts to tag everyone in the the room."},

                new Module { buttonText = "Tag Aura", aliases = new[] { "Tag Range" }, method = Advantages.TagAura, toolTip = "Moves your hand into nearby players when tagged."},
                new Module { buttonText = $"Grip Tag Aura {Bracket("green", "G")}", aliases = new[] { "Grip Tag Range" }, method = Advantages.GripTagAura, toolTip = "Moves your hand into nearby players when tagged and when holding <color=green>grip</color>."},

                new Module { buttonText = "Tag Aura Gun", method = Advantages.TagAuraGun, toolTip = "Gives a player tag aura."},
                new Module { buttonText = "Tag Aura All", method = Advantages.TagAuraAll, toolTip = "Gives all players tag aura."},

                new Module { buttonText = "Tag Reach", method = Advantages.TagReach, disableMethod =() => References.Tagger.maxTagDistance = 1.2f, toolTip = "Makes your hand tag hitbox larger."},

                new Module { buttonText = "Flick Tag Gun", method = Advantages.FlickTagGun, toolTip = "Moves your hand to wherever your hand desires in an attempt to tag whoever your hand desires."},

                new Module { buttonText = "Tag Bot", method = Advantages.TagBot, disableMethod = Movement.EnableRig, toolTip = "Automatically tags yourself and everyone else on a loop, use <color=green>B</color> to turn it off."},

                new Module { buttonText = "No Tag on Join", method = Advantages.NoTagOnJoin, disableMethod = Advantages.TagOnJoin, toolTip = "When you join a the room, you won't be tagged when you join."},
                new Module { buttonText = "Untag Self", method = Advantages.UntagSelf, isTogglable = false, toolTip = "Removes you from the list of tagged players."},

                new Module { buttonText = "Anti Tag", method = Advantages.AntiTag, disableMethod = Advantages.DisableAntiTag, toolTip = "Removes you from the list of tagged players when tagged."},
                new Module { buttonText = "Report Anti Tag", enableMethod = Advantages.ReportAntiTag, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Prevents you from getting tagged, and whoever tries to tag you will just get reported."},

                new Module { buttonText = "No Tag Limit", aliases = new[] { "Tagmax" }, method =() => References.Tagger.maxTagDistance = float.MaxValue, disableMethod =() => References.Tagger.maxTagDistance = 1.2f, toolTip = "Removes the distance check when tagging players."},
                new Module { buttonText = "Tag Lag Detector", method = Advantages.TagLagDetector, toolTip = "Detects when the master client is not currently allowing tag requests."},

                new Module { buttonText = "Fake Lag", method = Advantages.FakeLag, disableMethod =() => { ViewSerialization.OverrideSerialization = null; RigSerialization.delay = null; }, toolTip = "Forces your ping to be high."},
                new Module { buttonText = "Lag Range", method = Advantages.LagRange, toolTip = "Dynamically changes how much your rig updates depending on how close you are to others."},
                new Module { buttonText = "Blink", method = Advantages.Blink, disableMethod = Advantages.DisableBlink, toolTip = "Stops your client from sending and receiving player update packets."},

                new Module { buttonText = "Paintbrawl Aimbot", overlapText = "Slingshot Aimbot", enableMethod =() => SlingshotLaunch.enabled = true, method = Advantages.DebugSlingshotAimbot, disableMethod =() => SlingshotLaunch.enabled = false, toolTip = "Redirects your slingshot to the closest nearby players."},
                new Module { buttonText = "Slingshot Helper", method = Advantages.SlingshotHelper, toolTip = "Helps you grab the small paintball on your slingshot."},
                new Module { buttonText = "Slingshot Trigger Bot", method = Advantages.SlingshotTriggerBot, toolTip = "Releases the small paintball on your slingshot when hovering over another player."},

                new Module { buttonText = "Paintbrawl Kill Self", method = Advantages.PaintbrawlKillSelf, toolTip = "Kills yourself in paintbrawl." },
                new Module { buttonText = "Paintbrawl Kill Gun", method = Advantages.PaintbrawlKillGun, toolTip = "Kills whoever your hand desires in paintbrawl." },
                new Module { buttonText = "Paintbrawl Kill All", method = Advantages.PaintbrawlKillAll, toolTip = "Kills everyone in the room in paintbrawl." }
            },

            ["Visual Mods"] = new[]
            {
                new Module { buttonText = "Exit Visual Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Morning Time", method =() => ManagerRegistry.BetterDayNightManager.SetTimeOfDay(1), toolTip = "Sets your time of day to morning."},
                new Module { buttonText = "Day Time", method =() => ManagerRegistry.BetterDayNightManager.SetTimeOfDay(3), toolTip = "Sets your time of day to daytime."},
                new Module { buttonText = "Evening Time", method =() => ManagerRegistry.BetterDayNightManager.SetTimeOfDay(7), toolTip = "Sets your time of day to evening."},
                new Module { buttonText = "Night Time", method =() => ManagerRegistry.BetterDayNightManager.SetTimeOfDay(0), toolTip = "Sets your time of day to night."},
                new Module { buttonText = "Fullbright", method =() => Visuals.SetFullbrightStatus(true), disableMethod =() => Visuals.SetFullbrightStatus(false), toolTip = "Disables the dynamic lighting in maps that use it."},

                new Module { buttonText = "Remove Blindfold", method = Visuals.RemoveBlindfold, toolTip = "Disables the blindfold in the prop hunt map."},

                new Module { buttonText = "Core ESP", method = Visuals.CoreESP, toolTip = "Puts dots on your screen at where all of the cores in the ghost reactor map are."},
                new Module { buttonText = "Critter ESP", method = Visuals.CritterESP, toolTip = "Puts dots on your screen at where all of the critters in the critter map are."},
                new Module { buttonText = "Creature ESP", method = Visuals.CreatureESP, toolTip = "Puts dots on your screen at where all of the creatures are in forest and caves."},
                new Module { buttonText = "Enemy ESP", method = Visuals.EnemyESP, toolTip = "Puts dots on your screen at where all of the cores in the ghost reactor map are."},
                new Module { buttonText = "Resource ESP", method = Visuals.ResourceESP, toolTip = "Puts dots on your screen at where all of the resources are in the Super Infection gamemode."},

                new Module { buttonText = "Enable Snow", aliases = new[] { "Winter" }, enableMethod =() => Visuals.ToggleSnow(true), disableMethod =() => Visuals.ToggleSnow(false), toolTip = "Forcibly enables the snow."},
                new Module { buttonText = "Rainy Weather", aliases = new[] { "Enable Rain" }, method =() => Visuals.WeatherChange(true), toolTip = "Forces the weather to rain."},
                new Module { buttonText = "Clear Weather", aliases = new[] { "Disable Rain" }, method =() => Visuals.WeatherChange(false), toolTip = "Forces the weather to sunny skies all day."},
                new Module { buttonText = "Disable Fog", method = Visuals.DisableFog, disableMethod = Visuals.ResetFog, toolTip = "Disables the fog."},
                new Module { buttonText = "Enable Fog", method = Visuals.EnableFog, disableMethod = Visuals.ResetFog, toolTip = "Enables the fog."},

                new Module { buttonText = "Disable Ambience", enableMethod = Visuals.DisableAmbience, disableMethod = Visuals.EnableAmbience, toolTip = "Disables all ambient effects.", legal = true},

                new Module { buttonText = "Custom Skybox Color", aliases = new[] { "Custom Sky Color" }, enableMethod = Visuals.DoCustomSkyboxColor, method = Visuals.CustomSkyboxColor, disableMethod = Visuals.UnCustomSkyboxColor, toolTip = "Changes the skybox color to match the menu.", legal = true},
                new Module { buttonText = "Draw Gun", method = Visuals.DrawGun, disableMethod = Visuals.DisableDrawGun, toolTip = "Lets you draw on whatever your hand desires.", legal = true},
                new Module { buttonText = "Gamesense Ring", aliases = new[] { "Fortnite Ring" }, enableMethod =() => HandTaps.OnHandTap += Visuals.OnHandTapGamesenseRing, method = Visuals.GamesenseRing, disableMethod = Visuals.DisableGamesenseRing, toolTip = "Shows the direction of where people walk around you." },

                new Module { buttonText = "Velocity Label", method = Visuals.VelocityLabel, toolTip = "Puts text on your right hand, showing your velocity.", legal = true},
                new Module { buttonText = "Nearby Label", method = Visuals.NearbyTaggerLabel, toolTip = "Puts text on your left hand, showing you the distance of the nearest tagger."},
                new Module { buttonText = "Last Label", method = Visuals.LastLabel, toolTip = "Puts text on your left hand, showing you how many untagged people are left.", legal = true},
                new Module { buttonText = "Time Label", method = Visuals.TimeLabel, toolTip = "Puts text on your right hand, showing how long you've been playing for without getting tagged.", legal = true},

                new Module { buttonText = "FPS Overlay", method =() => Notifications.information["FPS"] = lastDeltaTime.ToString(), disableMethod =() => Notifications.information.Remove("FPS"), toolTip = "Displays your FPS on your screen.", legal = true},
                new Module { buttonText = "Ping Overlay", method = Visuals.PingOverlay, disableMethod =() => Notifications.information.Remove("Ping"), toolTip = "Displays the server's ping on your screen.", legal = true},
                new Module { buttonText = "Time Overlay", method =() => Notifications.information["Time"] = DateTime.Now.ToString("hh:mm tt"), disableMethod =() => Notifications.information.Remove("Time"), toolTip = "Displays your current time on your screen.", legal = true},
                new Module { buttonText = "Playtime Overlay", method =() => { Notifications.information["Playtime"] = Visuals.OverallPlaytime; Visuals.UpdatePlaytime(); }, disableMethod =() => Notifications.information.Remove("Playtime"), toolTip = "Displays your play time from when the mod was enabled on your screen.", legal = true},
                new Module { buttonText = "Room Information Overlay", overlapText = "Important Room Info Overlay", method =() => { if (Networking.InRoom) { Notifications.information["Room Code"] = PhotonNetwork.CurrentRoom.Name; Notifications.information["Players"] = PhotonNetwork.PlayerList.Length.ToString(); } else { Notifications.information.Remove("Room Code"); Notifications.information.Remove("Players"); } }, disableMethod =() => { Notifications.information.Remove("Room Code"); Notifications.information.Remove("Players"); }, toolTip = "Displays basic information about the room on your screen.", legal = true},
                new Module { buttonText = "Extra Room Info Overlay", method =() => Visuals.ExtraRoomInfo(), disableMethod =() => Visuals.ExtraRoomInfo(false), toolTip = "Displays extra information about the room on your screen."},
                new Module { buttonText = "Networking Overlay", method =() => { Notifications.information["Ping"] = PhotonNetwork.GetPing().ToString(); Notifications.information["Region"] = ManagerRegistry.NetworkSystem.regionNames[ManagerRegistry.NetworkSystem.currentRegionIndex].ToUpper(); }, disableMethod =() => { Notifications.information.Remove("Ping"); Notifications.information.Remove("Region"); }, toolTip = "Displays information about networking on your screen.", legal = true},
                new Module { buttonText = "Clipboard Overlay", method =() => Notifications.information["Clip"] = GUIUtility.systemCopyBuffer.Length > 20 ? GUIUtility.systemCopyBuffer[..20] : GUIUtility.systemCopyBuffer, disableMethod =() => Notifications.information.Remove("Clip"), toolTip = "Displays your current clipboard on your screen.", legal = true},
                new Module { buttonText = "Velocity Overlay", method =() => Notifications.information["Velocity"] = $"{References.Rigidbody.linearVelocity.magnitude:F1}m/s", disableMethod =() => Notifications.information.Remove("Velocity"), toolTip = "Displays your velocity on your screen.", legal = true},
                new Module { buttonText = "Nearby Overlay", method = Visuals.NearbyTaggerOverlay, disableMethod =() => Notifications.information.Remove("Nearby"), toolTip = "Displays the distance to the nearest tagger/target on your screen."},
                new Module { buttonText = "Info Overlay Gun", method = Visuals.InfoOverlayGun, toolTip = "Displays an overlay, showing the information of whoever your hand desires."},

                new Module { buttonText = "Debug HUD", aliases = new[] { "Developer HUD", "Debug UI", "Developer UI" }, enableMethod = Visuals.EnableDebugHUD, disableMethod = Visuals.DisableDebugHUD, toolTip = "Displays the developer debug HUD."},

                new Module { buttonText = "Info Watch", enableMethod = Visuals.WatchOn, method = Visuals.WatchStep, disableMethod = Visuals.WatchOff, toolTip = "Puts a watch on your hand that tells you the time and your FPS."},
                new Module { buttonText = "Leaderboard Info", enableMethod =() => ScoreboardLines.enabled = true, method = Visuals.LeaderboardInfo, disableMethod =() => ScoreboardLines.enabled = false, toolTip = "Shows info next to players' names on the leaderboard."},

                new Module { buttonText = "FPS Boost", aliases = new[] { "Low Quality" }, enableMethod =() => QualitySettings.globalTextureMipmapLimit = int.MaxValue, disableMethod =() => QualitySettings.globalTextureMipmapLimit = 1, toolTip = "Makes everything low quality in an attempt to boost your FPS.", },
                new Module { buttonText = "Freeze In Background", enableMethod =() => Application.runInBackground = false, disableMethod =() => Application.runInBackground = true, toolTip = "Freezes the game when the application is not focused.", legal = true},

                new Module { buttonText = "Fake Unban Self", method = Visuals.FakeUnbanSelf, isTogglable = false, toolTip = "Makes it appear as if you're not banned." },

                new Module { buttonText = "Jump Predictions", overlapText = "Jump Trajectories", method = Visuals.JumpPredictions, disableMethod = Visuals.DisableJumpPredictions, toolTip = "Shows a visualizer of where the other players will jump."},
                new Module { buttonText = "Hitbox Predictions", method = Visuals.HitboxPredictions, disableMethod = Visuals.DisableHitboxPredictions, toolTip = "Shows capsules where other players' hitboxes are."},
                new Module { buttonText = "Paintbrawl Trajectories", overlapText = "Projectile Trajectories", method = Visuals.PaintbrawlTrajectories, disableMethod = Visuals.DisablePaintbrawlTrajectories, toolTip = "Shows a visualizer of where all projectiles and your slingshot will hit."},

                new Module { buttonText = "Audio Visualizer", enableMethod = Visuals.CreateAudioVisualizer, method = Visuals.AudioVisualizer, disableMethod = Visuals.DestroyAudioVisualizer, toolTip = "Shows a visualizer of your microphone loudness below your player.", legal = true},
                new Module { buttonText = "Show Server Position", method = Visuals.ShowServerPosition, disableMethod = Visuals.DisableShowServerPosition, toolTip = "Shows your current syncronized position on the server.", legal = true},
                new Module { buttonText = "Show Scheduled Objects", enableMethod = Visuals.ShowScheduledObjects, toolTip = "Shows all scheduled and planned objects before their target date."},

                new Module { buttonText = "Visualize Network Triggers", method = Visuals.VisualizeNetworkTriggers, toolTip = "Visualizes the network joining and leaving triggers."},
                new Module { buttonText = "Visualize Wind Barriers", aliases = new[] { "Visualize Air" }, method = Visuals.VisualizeWindBarriers, toolTip = "Visualizes the wind barriers."},
                new Module { buttonText = "Visualize Map Triggers", method = Visuals.VisualizeMapTriggers, toolTip = "Visualizes the map loading and unloading triggers."},

                new Module { buttonText = "Name Tags", method = Visuals.NameTags, disableMethod = Visuals.DisableNameTags, toolTip = "Gives players name tags above their heads that show their nickname.", legal = true},
                new Module { buttonText = "Velocity Name Tags", method = Visuals.VelocityTags, disableMethod = Visuals.DisableVelocityTags, toolTip = "Gives players name tags above their heads that show their velocity."},
                new Module { buttonText = "FPS Name Tags", method = Visuals.FPSTags, disableMethod = Visuals.DisableFPSTags, toolTip = "Gives players name tags above their heads that show their FPS."},
                new Module { buttonText = "Target FPS Name Tags", method = Visuals.TargetFPSTags, disableMethod = Visuals.DisableTargetFPSTags, toolTip = "Gives players name tags above their heads that show their maximum FPS."},
                new Module { buttonText = "ID Name Tags", method = Visuals.IDTags, disableMethod = Visuals.DisableIDTags, toolTip = "Gives players name tags above their heads that show their ID."},
                new Module { buttonText = "Platform Name Tags", method = Visuals.PlatformTags, disableMethod = Visuals.DisablePlatformTags, toolTip = "Gives players name tags above their heads that show what platform they're playing on.", legal = true},
                new Module { buttonText = "k-ID Name Tags", method = Visuals.KIDNameTags, disableMethod = Visuals.DisableKIDNameTags, toolTip = "Gives players name tags above their heads that show if they have k-ID restrictions."},
                new Module { buttonText = "Subscriber Name Tags", method = Visuals.SubscriberNameTags, disableMethod = Visuals.DisableSubscriberNameTags, toolTip = "Gives players name tags above their heads that show if they're subscribed to the fan club."},
                new Module { buttonText = "Creation Date Name Tags", method = Visuals.CreationDateTags, disableMethod = Visuals.DisableCreationDateTags, toolTip = "Gives players name tags above their heads that show their creation date."},
                new Module { buttonText = "Ping Name Tags", method = Visuals.PingTags, disableMethod = Visuals.DisablePingTags, toolTip = "Gives players name tags above their heads that show their ping."},
                new Module { buttonText = "Turn Name Tags", method = Visuals.TurnTags, disableMethod = Visuals.DisableTurnTags, toolTip = "Gives players name tags above their heads that show their turn settings."},
                new Module { buttonText = "Tagged Name Tags", method = Visuals.TaggedTags, disableMethod = Visuals.DisableTaggedTags, toolTip = "Gives players name tags above their heads that show who tagged them."},
                new Module { buttonText = "Mod Name Tags", method = Visuals.ModTags, disableMethod = Visuals.DisableModTags, toolTip = "Gives players name tags above their heads that show what mods they have."},
                new Module { buttonText = "Cosmetic Name Tags", method = Visuals.CosmeticTags, disableMethod = Visuals.DisableCosmeticTags, toolTip = "Gives players name tags above their heads that show what special cosmetics they have."},
                new Module { buttonText = "Verified Name Tags", method = Visuals.VerifiedTags, disableMethod = Visuals.DisableVerifiedTags, toolTip = "Gives players name tags above their heads if they are a verified player."},
                new Module { buttonText = "Lag Name Tags", method = Visuals.CrashedTags, disableMethod = Visuals.DisableCrashedTags, toolTip = "Gives players name tags above their heads if they are lagging."},
                new Module { buttonText = "Compact Name Tags", overlapText = "VRChat Name Tags", aliases = new[] { "Compact Name Tags" }, method = Visuals.CompactTags, disableMethod = Visuals.DisableCompactTags, toolTip = "Gives players name tags above their heads that show a lot of information compactly. Credits to snake for the mod idea."},
                new Module { buttonText = "Minecraft Name Tags", method = Visuals.MinecraftTags, disableMethod = Visuals.DisableMinecraftTags, toolTip = "Gives players the same nametags that are shown in Minecraft.", legal = true},
                new Module { buttonText = "Casting Name Tags", method = Visuals.CastingTags, disableMethod = Visuals.DisableCastingTags, toolTip = "Gives players a very simplistic nametag, only showing platform and name. Inspired by BingusNameTags++.", legal = true},

                new Module { buttonText = "Fix Rig Colors", method = Visuals.FixRigColors, toolTip = "Fixes a Steam bug where other players' color would be wrong between servers.", legal = true},
                new Module { buttonText = "Disable Rig Lerping", overlapText = "Disable Rig Smoothing", method = Visuals.NoSmoothRigs, disableMethod = Visuals.ReSmoothRigs, toolTip = "Disable the smoothing on the other player's rigs."},
                new Module { buttonText = "Better Rig Lerping", overlapText = "Better Rig Smoothing", enableMethod =() => RigSerialization.OnPlayerSerialize += Visuals.BetterRigLerping, disableMethod =() => RigSerialization.OnPlayerSerialize -= Visuals.BetterRigLerping, toolTip = "Estimates the inbetween positions using a real velocity emulator on the other player's rigs."},
                new Module { buttonText = "Remove Leaves", enableMethod = Visuals.EnableRemoveLeaves, disableMethod = Visuals.DisableRemoveLeaves, toolTip = "Removes leaves on trees, good for branching."},
                new Module { buttonText = "Streamer Remove Leaves", enableMethod = Visuals.EnableStreamerRemoveLeaves, disableMethod = Visuals.DisableStreamerRemoveLeaves, toolTip = "Removes leaves on trees in VR, but not on the camera. Good for streaming."},
                new Module { buttonText = "Remove Cosmetics", enableMethod = Visuals.DisableCosmetics, disableMethod = Visuals.EnableCosmetics, toolTip = "Locally toggles off your cosmetics, so you can wear sight-blocking cosmetics such as the eyepatch."},
                new Module { buttonText = $"X-Ray {Bracket("green", "T")}", aliases = new[] { "See Through" }, method = Visuals.Xray, toolTip = "Lets you see through objects when holding <color=green>trigger</color>."},

                new Module { buttonText = "Special Cosmetic Indicators", aliases = new[] { "Cosmetic ESP" }, method = Visuals.CosmeticIndicators, disableMethod = Visuals.DisableCosmeticIndicators, toolTip = "Shows icons above people's heads if they are a Finger Painter, Illustrator, Administrator, Stick, Forest Guide, or Another Axiom Creator."},

                new Module { buttonText = "Voice ESP", method = Visuals.VoiceESP, disableMethod = Visuals.DisableVoiceIndicators, toolTip = "Puts voice indicators above people's heads when they're talking.", legal = true},

                new Module { buttonText = "Grabbable Indicators", method = Visuals.GripESP, disableMethod = Visuals.DisableGripESP, toolTip = "Puts indicators above people's heads when they can be grabbed."},

                new Module { buttonText = "Platform Indicators", method = Visuals.PlatformIndicators, disableMethod = Visuals.DisablePlatformIndicators, toolTip = "Puts indicators above people's heads that show what platform they are playing on."},
                new Module { buttonText = "Platform ESP", method = Visuals.PlatformESP, disableMethod = Visuals.DisablePlatformIndicators, toolTip = "Puts indicators above people's heads that show what platform they are playing on, but now they go through walls."},

                new Module { buttonText = "No Limb Mode", enableMethod = Visuals.StartNoLimb, method = Visuals.NoLimbMode, disableMethod = Visuals.EndNoLimb, toolTip = "Makes your regular rig invisible, and puts balls on your hands."},

                new Module { buttonText = "Casual Tracers", method = Visuals.CasualTracers, disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand. Shows everyone."},
                new Module { buttonText = "Infection Tracers", method = Visuals.InfectionTracers, disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Tracers", method = Visuals.HuntTracers, disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Tracers", method =() => Visuals.AutomaticESP(Visuals.InfectionTracers, Visuals.HuntTracers, Visuals.CasualTracers), disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand. Shows targets for the current gamemode."},
                new Module { buttonText = "Nearest Tracer", method = Visuals.NearestTracer, disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand. Shows the nearest player."},

                new Module { buttonText = "Casual Box ESP", method = Visuals.CasualBoxESP, disableMethod = Visuals.DisableBoxESP, toolTip = "Puts boxes over players. Shows everyone."},
                new Module { buttonText = "Infection Box ESP", method = Visuals.InfectionBoxESP, disableMethod = Visuals.DisableBoxESP, toolTip = "Puts boxes over players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Box ESP", method = Visuals.HuntBoxESP, disableMethod = Visuals.DisableBoxESP, toolTip = "Puts boxes over players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Box ESP", method =() => Visuals.AutomaticESP(Visuals.InfectionBoxESP, Visuals.HuntBoxESP, Visuals.CasualBoxESP), disableMethod = Visuals.DisableBoxESP, toolTip = "Puts boxes over players. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Hollow Box ESP", method = Visuals.CasualHollowBoxESP, disableMethod = Visuals.DisableHollowBoxESP, toolTip = "Puts hollow boxes over players. Shows everyone."},
                new Module { buttonText = "Infection Hollow Box ESP", method = Visuals.HollowInfectionBoxESP, disableMethod = Visuals.DisableHollowBoxESP, toolTip = "Puts hollow boxes over players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Hollow Box ESP", method = Visuals.HollowHuntBoxESP, disableMethod = Visuals.DisableHollowBoxESP, toolTip = "Puts hollow boxes over players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Hollow Box ESP", method =() => Visuals.AutomaticESP(Visuals.HollowInfectionBoxESP, Visuals.HollowHuntBoxESP, Visuals.CasualHollowBoxESP), disableMethod = Visuals.DisableHollowBoxESP, toolTip = "Puts hollow boxes over players. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Breadcrumbs", method = Visuals.CasualBreadcrumbs, disableMethod = Visuals.DisableBreadcrumbs, toolTip = "Puts breadcrumb trails over players. Shows everyone."},
                new Module { buttonText = "Infection Breadcrumbs", method = Visuals.InfectionBreadcrumbs, disableMethod = Visuals.DisableBreadcrumbs, toolTip = "Puts breadcrumb trails over players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Breadcrumbs", method = Visuals.HuntBreadcrumbs, disableMethod = Visuals.DisableBreadcrumbs, toolTip = "Puts breadcrumb trails over players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Breadcrumbs", method =() => Visuals.AutomaticESP(Visuals.InfectionBreadcrumbs, Visuals.HuntBreadcrumbs, Visuals.CasualBreadcrumbs), disableMethod = Visuals.DisableBreadcrumbs, toolTip = "Puts breadcrumb trails over players. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Bone ESP", method = Visuals.CasualBoneESP, disableMethod = Visuals.DisableBoneESP, toolTip = "Puts bones over players. Shows everyone."},
                new Module { buttonText = "Infection Bone ESP", method = Visuals.InfectionBoneESP, disableMethod = Visuals.DisableBoneESP, toolTip = "Puts bones over players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Bone ESP", method = Visuals.HuntBoneESP, disableMethod = Visuals.DisableBoneESP, toolTip = "Puts bones over players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Bone ESP", method =() => Visuals.AutomaticESP(Visuals.InfectionBoneESP, Visuals.HuntBoneESP, Visuals.CasualBoneESP), disableMethod = Visuals.DisableBoneESP, toolTip = "Puts bones over players. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Skeleton ESP", method = Visuals.CasualSkeletonESP, disableMethod = Visuals.DisableSkeletonESP, toolTip = "Lets you see players skeletons through walls. Shows everyone."},
                new Module { buttonText = "Infection Skeleton ESP", method = Visuals.InfectionSkeletonESP, disableMethod = Visuals.DisableChams, toolTip = "Lets you see players skeletons through walls. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Skeleton ESP", method = Visuals.HuntSkeletonESP, disableMethod = Visuals.DisableChams, toolTip = "Lets you see players skeletons through walls. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Skeleton ESP", method =() => Visuals.AutomaticESP(Visuals.InfectionSkeletonESP, Visuals.HuntSkeletonESP, Visuals.CasualSkeletonESP), disableMethod = Visuals.DisableSkeletonESP, toolTip = "Lets you see players skeletons through walls. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Wireframe ESP", method = Visuals.CasualWireframeESP, disableMethod = Visuals.DisableWireframeESP, toolTip = "Puts wireframes over players. Shows everyone."},
                new Module { buttonText = "Infection Wireframe ESP", method = Visuals.InfectionWireframeESP, disableMethod = Visuals.DisableWireframeESP, toolTip = "Puts wireframes over players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Wireframe ESP", method = Visuals.HuntWireframeESP, disableMethod = Visuals.DisableWireframeESP, toolTip = "Puts wireframes over players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Wireframe ESP", method =() => Visuals.AutomaticESP(Visuals.InfectionWireframeESP, Visuals.HuntWireframeESP, Visuals.CasualWireframeESP), disableMethod = Visuals.DisableWireframeESP, toolTip = "Puts wireframes over players. Shows targets for the current gamemode."},

                new Module { buttonText = "Chams", method = Visuals.Chams, disableMethod = Visuals.DisableShaderChams, toolTip = "Lets you see players through walls."},

                new Module { buttonText = "Casual Chams", method = Visuals.CasualChams, disableMethod = Visuals.DisableChams, toolTip = "Lets you see players fur through walls. Shows everyone."},
                new Module { buttonText = "Infection Chams", method = Visuals.InfectionChams, disableMethod = Visuals.DisableChams, toolTip = "Lets you see players fur through walls. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Chams", method = Visuals.HuntChams, disableMethod = Visuals.DisableChams, toolTip = "Lets you see players fur through walls. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Chams", method =() => Visuals.AutomaticESP(Visuals.InfectionChams, Visuals.HuntChams, Visuals.CasualChams), disableMethod = Visuals.DisableChams, toolTip = "Lets you see players fur through walls. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Beacons", method = Visuals.CasualBeacons, disableMethod =() => Visuals.isLineRenderQueued = true, toolTip = "Puts a beacon above players. Shows everyone."},
                new Module { buttonText = "Infection Beacons", method = Visuals.InfectionBeacons, disableMethod =() => Visuals.isLineRenderQueued = true, toolTip = "Puts a beacon above players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Beacons", method = Visuals.HuntBeacons, disableMethod =() => Visuals.isLineRenderQueued = true, toolTip = "Puts a beacon above players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Beacons", method =() => Visuals.AutomaticESP(Visuals.InfectionBeacons, Visuals.HuntBeacons, Visuals.CasualBeacons), disableMethod =() => Visuals.isLineRenderQueued = true, toolTip = "Puts a beacon above players. Shows targets for the current gamemode."},

                new Module { buttonText = "Casual Distance ESP", method = Visuals.CasualDistanceESP, disableMethod =() => Visuals.isNameTagQueued = true, toolTip = "Shows your distance from players. Shows everyone."},
                new Module { buttonText = "Infection Distance ESP", method = Visuals.InfectionDistanceESP, disableMethod =() => Visuals.isNameTagQueued = true, toolTip = "Shows your distance from players. Shows untagged when tagged, vice versa."},
                new Module { buttonText = "Hunt Distance ESP", method = Visuals.HuntDistanceESP, disableMethod =() => Visuals.isNameTagQueued = true, toolTip = "Shows your distance from players. Shows your target and who is hunting you."},
                new Module { buttonText = "Automatic Distance ESP", method =() => Visuals.AutomaticESP(Visuals.InfectionDistanceESP, Visuals.HuntDistanceESP, Visuals.CasualDistanceESP), disableMethod =() => Visuals.isNameTagQueued = true, toolTip = "Shows your distance from players. Shows targets for the current gamemode."},

                new Module { buttonText = "Show Pointers", method = Visuals.ShowButtonColliders, disableMethod = Visuals.HideButtonColliders, toolTip = "Shows dots near your hands, such as when you open the menu."},

                new Module { buttonText = "Info Watch Menu Name", enableMethod =() => Visuals.infoWatchMenuName = true, disableMethod =() => Visuals.infoWatchMenuName = false, toolTip = "Shows the menu name on the Info Watch mod."},
                new Module { buttonText = "Info Watch FPS", enableMethod =() => Visuals.infoWatchFPS = true, disableMethod =() => Visuals.infoWatchFPS = false, toolTip = "Shows your framerate on the Info Watch mod."},
                new Module { buttonText = "Info Watch Time", enableMethod =() => Visuals.infoWatchTime = true, disableMethod =() => Visuals.infoWatchTime = false, toolTip = "Shows the current time on the Info Watch mod."},
                new Module { buttonText = "Info Watch Clipboard", enableMethod =() => Visuals.infoWatchClip = true, disableMethod =() => Visuals.infoWatchClip = false, toolTip = "Shows your clipboard on the Info Watch mod."},
                new Module { buttonText = "Info Watch Code", enableMethod =() => Visuals.infoWatchCode = true, disableMethod =() => Visuals.infoWatchCode = false, toolTip = "Shows the lobby code on the Info Watch mod."},
            },

            ["Fun Mods"] = new[]
            {
                new Module { buttonText = "Exit Fun Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Custom Map Mods", aliases = new[] { "Virtual Stump Mods" }, method =() => CurrentCategoryName = "Custom Map Mods", isTogglable = false, toolTip = "Opens the custom map mods tab."},

                new Module { buttonText = "Upside Down Head", method = Fun.UpsideDownHead, disableMethod = Fun.FixHead, toolTip = "Flips your head upside down on the Z axis."},
                new Module { buttonText = "Backwards Head", method = Fun.BackwardsHead, disableMethod = Fun.FixHead, toolTip = "Rotates your head 180 degrees on the Y axis."},
                new Module { buttonText = "Sideways Head", method = Fun.SidewaysHead, disableMethod = Fun.FixHead, toolTip = "Rotates your head 90 degrees on the Y axis."},

                new Module { buttonText = "Broken Neck", method = Fun.BrokenNeck, disableMethod = Fun.FixHead, toolTip = "Rotates your head 90 degrees on the Z axis."},

                new Module { buttonText = "Head Bang", method = Fun.HeadBang, disableMethod = Fun.FixHead, toolTip = "Bangs your head at the BPM of Paint it Black (159)."},

                new Module { buttonText = "Flip Hands", aliases = new[] { "Fish Arms" }, method = Fun.FlipHands, toolTip = "Swaps your hands, left is right and right is left."},
                new Module { buttonText = "Loud Hand Taps", method = Fun.LoudHandTaps, disableMethod = Fun.FixHandTaps, toolTip = "Makes your hand taps really loud."},
                new Module { buttonText = "Silent Hand Taps", aliases = new[] { "No Hand Taps" }, method = Fun.SilentHandTaps, disableMethod = Fun.FixHandTaps, toolTip = "Makes your hand taps really quiet."},
                new Module { buttonText = "Instant Hand Taps", method =() => References.Tagger.tapCoolDown = 0f, disableMethod =() => References.Tagger.tapCoolDown = 0.33f, toolTip = "Removes the hand tap cooldown."},
                new Module { buttonText = "Silent Hand Taps on Tag", aliases = new[] { "No Hand Taps on Tag" }, method = Fun.SilentHandTapsOnTag, disableMethod = Fun.FixHandTaps, toolTip = "Makes your hand taps really quiet when you're tagged, good for ambush."},

                new Module { buttonText = $"Water Splash Hands {Bracket("green", "G")}", method = Fun.WaterSplashHands, toolTip = "Splashes water when holding <color=green>grip</color>."},
                new Module { buttonText = "Give Water Splash Hands Gun", method = Fun.GiveWaterSplashHandsGun, toolTip = "Gives whoever your hand desires the water splash hands mod." },

                new Module { buttonText = "Water Splash Walk", method = Fun.WaterSplashWalk, toolTip = "Splashes water whenever you take a step."},
                new Module { buttonText = "Water Splash Aura", method = Fun.WaterSplashAura, toolTip = "Splashes water around you at random positions."},
                new Module { buttonText = "Orbit Water Splash", method = Fun.OrbitWaterSplash, toolTip = "Splashes water orbitally around you."},
                new Module { buttonText = "Water Splash Gun", method = Fun.WaterSplashGun, toolTip = "Splashes water wherever your hand desires."},
                new Module { buttonText = "Water Splash On Touch", method = Fun.WaterSplashOnTouch, toolTip = "Splashes water whoever your hand touches."},

                new Module { buttonText = "Confuse Player Gun", method = Fun.ConfusePlayerGun, toolTip = "Makes whoever your hand desires look like they're going crazy by splashing water on their screen."},
                new Module { buttonText = "Confuse All Players", enableMethod = Fun.ConfuseAllPlayers, method = Fun.ConfuseAllPlayersSplash, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Splashes water on everyone's screens, making them look like they're going crazy."},

                new Module { buttonText = "Tinnitus Gun", aliases = new[] { "Ear Rape All", "Earrape All" }, method = Fun.TinnitusGun, disableMethod = Fun.DisableTinnitus, toolTip = "Plays high pitched noises for whoever your hand desires."},
                new Module { buttonText = "Tinnitus All", aliases = new[] { "Ear Rape All", "Earrape All" }, enableMethod = Fun.TinnitusAll, disableMethod = Fun.DisableTinnitus, toolTip = "Plays high pitched noises for everyone in the room."},

                new Module { buttonText = "Spawn Red Lucy", method = Fun.SpawnRedLucy, isTogglable = false, toolTip = "Summons the red lucy in forest." },
                new Module { buttonText = "Spawn Blue Lucy", method = Fun.SpawnBlueLucy, isTogglable = false, toolTip = "Summons the blue lucy in forest." },
                new Module { buttonText = "Despawn Lucy", method = Fun.DespawnLucy, isTogglable = false, toolTip = "Despawns lucy in forest." },

                new Module { buttonText = "Lucy Chase Self", method =() => Fun.LucyChase(Networking.LocalPlayer), isTogglable = false, toolTip = "Makes lucy chase you." },
                new Module { buttonText = "Lucy Chase Gun", method = Fun.LucyChaseGun, toolTip = "Makes lucy chase whoever your hand desires." },

                new Module { buttonText = "Lucy Attack Self", method =() => Fun.LucyAttack(Networking.LocalPlayer), isTogglable = false, toolTip = "Makes lucy attack you." },
                new Module { buttonText = "Lucy Attack Gun", method = Fun.LucyAttackGun, toolTip = "Makes lucy attack whoever your hand desires." },
                new Module { buttonText = "Lucy Attack All", method = Fun.LucyAttackAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes lucy attack everyone in the room." },

                new Module { buttonText = "Lucy Harass Gun", method = Fun.LucyHarassGun, toolTip = "Makes lucy attack harass your hand desires." },
                new Module { buttonText = "Move Lucy Gun", method = Fun.MoveLucyGun, toolTip = "Moves lucy to wherever your hand desires." },

                new Module { buttonText = "Spaz Lucy", method = Fun.SpazLucy, toolTip = "Gives lucy a seizure." },
                new Module { buttonText = "Break Lucy", method =() => { Fun.SpazLucy(); Fun.lucyDelay = 0f; }, toolTip = "Breaks lucy." },
                new Module { buttonText = "Annoying Lucy", method = Fun.AnnoyingLucy, toolTip = "Makes lucy really annoying, by attacking everyone and making sounds of the bells." },

                new Module { buttonText = "Become Lucy", method = Fun.BecomeLucy, disableMethod = Movement.EnableRig, toolTip = "Turns you into the bug." },

                new Module { buttonText = "Fast Lucy", method = Fun.FastLucy, toolTip = "Makes lucy become really fast." },
                new Module { buttonText = "Slow Lucy", method = Fun.SlowLucy, toolTip = "Makes lucy become really slow." },

                new Module { buttonText = "Overstimulate Gun", method = Fun.OverstimulateGun, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Overstimulates whoever your hand desires."},
                new Module { buttonText = "Overstimulate All", method = Fun.OverstimulateAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Overstimulates everyone in the room."},

                new Module { buttonText = "Shutdown Headset Gun", method = Fun.ShutdownHeadsetGun, disableMethod = Fun.DisableTinnitus, toolTip = "Pretends to shut down the headset of whoever your hand desires."},
                new Module { buttonText = "Shutdown Headset All", enableMethod =() => Qubit.Utilities.Assets.LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/shutdown.ogg", "Audio/Mods/Fun/shutdown.ogg", clip => Fun.PlayAudio(clip)), method = Fun.ShutdownHeadsetAll, disableMethod = Fun.DisableTinnitus, toolTip = "Pretends to shut down the headset of everyone in the room."},

                new Module { buttonText = "Schizophrenic Gun", method = Fun.SchizophrenicGun, toolTip = "Makes you disappear for whoever your hand desires."},
                new Module { buttonText = "Reverse Schizophrenic Gun", method = Fun.ReverseSchizophrenicGun, toolTip = "Makes you only appear for whoever your hand desires."},

                new Module { buttonText = "Boop", method =() => Fun.Boop(), toolTip = "Makes a pop sound when you touch someone's nose."},
                new Module { buttonText = "Gong", method =() => Fun.Boop(248), toolTip = "Makes a gong sound when you hit someone's face."},
                new Module { buttonText = "Slap", method =() => Fun.Boop(338), toolTip = "Makes a slap sound when you hit someone's face."},

                new Module { buttonText = $"Auto Clicker {Bracket("green", "T")}", method = Fun.AutoClicker, toolTip = "Automatically presses  trigger for you when holding <color=green>trigger</color>."},

                new Module { buttonText = "Keyboard Tracker", enableMethod = Fun.EnableKeyboardTracker, method = Fun.KeyboardTracker, disableMethod = Fun.DisableKeyboardTracker, toolTip = "Tracks everyone's keyboard inputs in the lobby."},

                new Module { buttonText = "Tag Sounds", enableMethod = Fun.PreloadTagSounds, method =() => GameModeTagging.enabled = true, disableMethod =() => GameModeTagging.enabled = false, toolTip = "Plays a selection of dramatic sound effects when tagging players. Credits to Wyndigo for the idea."},

                new Module { buttonText = $"Free Camera {Bracket("green", "J")}", postMethod = Fun.Freecam, disableMethod = Fun.DisableFreecam, toolTip = "Exit your own body and fly around to your free will.", legal = true},
                new Module { buttonText = "Third Person Camera", postMethod = Fun.ThirdPersonCamera, disableMethod = Fun.DisableFreecam, toolTip = "Moves your camera to behind your head."},
                new Module { buttonText = "Flip Camera", postMethod = Fun.FlipCamera, disableMethod = Fun.DisableFreecam, toolTip = "Flips your camera 180 degrees."},
                new Module { buttonText = "Camera FOV", postMethod = Fun.CameraFOV, disableMethod = Fun.FixCameraFOV, toolTip = "Changes the FOV of your PC camera.", legal = true},
                new Module { buttonText = "Spectate Gun", postMethod = Fun.SpectateGun, disableMethod = Fun.DisableFreecam, toolTip = "Lets you see through the eyes of whoever your hand desires."},

                

                new Module { buttonText = "Nausea", aliases = new[] { "Sick", "Nautious", "Drunk" }, postMethod = Fun.Nausea, disableMethod = Fun.DisableFreecam, toolTip = "Gives you the Nausea effect from Minecraft."},
                new Module { buttonText = "LSD", aliases = new[] { "Drugs", "High" }, method =() => { Color rgb = Color.HSVToRGB(Time.frameCount / 180f % 1f, 1f, 1f); Fun.HueShift(new Color(rgb.r, rgb.g, rgb.b, 0.1f)); }, disableMethod =() => Fun.HueShift(Color.clear), toolTip = "Hue shifts your game to a rainbow color.", legal = true},
                new Module { buttonText = "Jumpscare on Tag", enableMethod = Fun.PreloadJumpscareData, method = Fun.JumpscareOnTag, toolTip = "Gives a 1/2000 chance of a jumpscare happening when getting tagged."},
                new Module { buttonText = "Spam Jumpscare", method = Fun.Jumpscare, toolTip = "Repeatedly jumpscares you.", legal = true},
                new Module { buttonText = "Jumpscare", method = Fun.Jumpscare, isTogglable = false, toolTip = "Jumpscares you.", legal = true},

                new Module { buttonText = "Prioritize Voice Gun", method = Fun.PrioritizeVoiceGun, toolTip = "Prioritizes whoever your hand desires' voice.", legal = true},
                new Module { buttonText = "Deprioritize Voice Gun", method = Fun.DeprioritizeVoiceGun, toolTip = "Deprioritizes whoever your hand desires' voice.", legal = true},
                new Module { buttonText = "Reset Voice", isTogglable = false, method = Fun.ResetVoiceAll, toolTip = "Resets everyones voice back to normal.", legal = true},

                new Module { buttonText = "Mute Gun", method = Fun.MuteGun, toolTip = "Mutes or unmutes whoever your hand desires.", legal = true},
                new Module { buttonText = "Mute All", method = Fun.MuteAll, disableMethod = Fun.UnmuteAll, toolTip = "Mutes everyone in the room.", legal = true},

                new Module { buttonText = "Report Gun", method = Fun.ReportGun, toolTip = "Reports whoever your hand desires for cheating."},
                new Module { buttonText = "Report All", method = Fun.ReportAll, isTogglable = false, toolTip = "Reports everyone in the room for cheating."},

                new Module { buttonText = "Trigger Anti Report Gun", method = Fun.TriggerAntiReportGun, toolTip = "Triggers whoever your hand desires' anti report if enabled."},
                new Module { buttonText = "Trigger Anti Report All", method = Fun.TriggerAntiReportAll, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Triggers everyone in the room's anti report if enabled."},
                new Module { buttonText = "Bypass Anti Report", method = Fun.BypassAntiReport, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Bypasses anti report mods when reporting players."},

                new Module { buttonText = "Break Mod Checkers", enableMethod = Fun.BreakModCheckers, disableMethod = Safety.BypassModCheckers, toolTip = "Tells players using mod checkers that you have every mod possible."},
                new Module { buttonText = "Custom Mod Spoofer", method = Fun.CustomModSpoofer, isTogglable = false, toolTip = "Make mod checkers see only what you allow."},

                new Module { buttonText = "Mute DJ Sets", method = Fun.MuteDJSets, disableMethod = Fun.UnmuteDJSets, toolTip = "Mutes every DJ set so you don't have to hear the worst music known to man."},
                new Module { buttonText = "Infinite Dreidel", method =() => Dreidels.enabled = true, disableMethod =() => Dreidels.enabled = false, toolTip = "Makes the dreidel cosmetic spin forever."},

                new Module { buttonText = "Barrel Gun", method = Fun.BarrelGun, toolTip = "Spawns a barrel at wherever your hand desires." },

                new Module { buttonText = "Legacy Microphone", enableMethod =() => { VoiceRecorder.enabled = false; Fun.ReloadMicrophone();  }, disableMethod =() => { VoiceRecorder.enabled = true; Fun.ReloadMicrophone(); }, toolTip = "Reverts the microphone system into using the legacy input switcher. This is generally not recommended unless you know what you are doing.", legal = true },
                new Module { buttonText = "Low Quality Microphone", enableMethod =() => Fun.SetMicrophoneQuality(6000, 4000), disableMethod =() => Fun.SetMicrophoneQuality(20000, 16000), toolTip = "Makes your microphone have really bad quality."},
                new Module { buttonText = "High Quality Microphone", enableMethod =() => Fun.SetMicrophoneQuality(30000, 24000), disableMethod =() => Fun.SetMicrophoneQuality(20000, 16000), toolTip = "Makes your microphone have really high quality."},
                new Module { buttonText = "Loud Microphone", method =() => Fun.SetMicrophoneAmplification(VoiceRecorder.enabled ? 5f : 16f), disableMethod =() => Fun.SetMicrophoneAmplification(1), toolTip = "Makes your microphone really loud."},

                new Module { buttonText = "Mute Microphone", method =() => Fun.MuteMicrophone(true), disableMethod =() => Fun.MuteMicrophone(false), toolTip = "Disables your microphone.", legal = true},
                new Module { buttonText = "Reload Microphone", aliases = new[] { "Restart Microphone" }, method = Fun.ReloadMicrophone, isTogglable = false,  toolTip = "Restarts / fixes your microphone.", legal = true},

                new Module { buttonText = "Voice Changers", method =() => CurrentCategoryName = "Voice Changers", isTogglable = false, toolTip = "Brings you into a category with voice changers."},

                new Module { buttonText = "Microphone Feedback", method =() => Fun.SetDebugEchoMode(true), disableMethod =() => Fun.SetDebugEchoMode(false), toolTip = "Plays sound coming through your microphone back to your speakers.", legal = true},
                new Module { buttonText = "Copy Voice Gun", method = Fun.CopyVoiceGun, disableMethod = Fun.DisableCopyVoice, toolTip = "Copies the voice of whoever your hand desires."},

                new Module { buttonText = "Narrate Text", aliases = new[] { "Text to Speech", "Speech Text" }, method =() => PromptText("What would you like to be narrated?", () => SpeakText(keyboardInput), null, "Done", "Cancel"), isTogglable = false, toolTip = "Narrates the text of your desire."},
                new Module { buttonText = "Save Narration", method =() => PromptText("What would you like the narration to say?", () => Fun.SaveNarration(keyboardInput)), isTogglable = false, toolTip = "Saves whatever you want to narrate to your soundboard."},
                new Module { buttonText = "Mask Voice", enableMethod = Fun.MaskVoice, disableMethod = Fun.DisableMaskVoice, toolTip = "Masks your voice with a TTS voice."},

                new Module { buttonText = "Disable Pitch Scaling", method = Fun.DisablePitchScaling, disableMethod = Fun.EnablePitchScaling, toolTip = "Disables the pitch effects on players' voices when they are a different scale."},
                new Module { buttonText = "Disable Mouth Movement", method = Fun.DisableMouthMovement, disableMethod = Fun.EnableMouthMovement, toolTip = "Disables your mouth from moving."},

                new Module { buttonText = $"Activate All Doors {Bracket("green", "G")}", method = Fun.ActivateAllDoors, toolTip = "Activates all doors when holding <color=green>grip</color>."},
                new Module { buttonText = $"Tap All Crystals {Bracket("green", "G")}", method = Fun.TapAllClass<GorillaCaveCrystal>, toolTip = "Taps all crystals when holding <color=green>grip</color>."},
                new Module { buttonText = $"Tap All Bells {Bracket("green", "G")}", method = Fun.TapAllClass<TappableBell>, toolTip = "Taps all bells when holding <color=green>grip</color>."},
                new Module { buttonText = "Trigger Leaf Pile Gun", method = Fun.TriggerLeafPileGun, toolTip = "Shows the effects on whatever leaf pile you desire."},

                new Module { buttonText = $"Get Bracelet {Bracket("green", "G")}", method =() => Fun.GetBracelet(true), toolTip = "Gives you a party bracelet without needing to be in a party."},
                new Module { buttonText = $"Spam Bracelet {Bracket("green", "G")}", method = Fun.BraceletSpam, toolTip = "Spams the party bracelet on and off."},
                new Module { buttonText = "Remove Bracelet", method = Fun.RemoveBracelet, isTogglable = false, toolTip = "Disables the party bracelet. This does not kick you from the party."},

                new Module { buttonText = "Rainbow Bracelet", method = Fun.RainbowBracelet, disableMethod = Fun.RemoveRainbowBracelet, toolTip = "Gives you a rainbow party bracelet."},

                new Module { buttonText = $"Quest Noises {Bracket("green", "T")}", method = Fun.QuestNoises, toolTip = "Makes noises at the quest machine in city when holding <color=green>trigger</color>."},
                new Module { buttonText = "Max Quest Score", method = Fun.MaxQuestScore, toolTip = "Gives you the maximum quest score in the game (99999)."},
                new Module { buttonText = "Custom Quest Score", method = Fun.CustomQuestScore, toolTip = "Gives you a custom quest score. You can change this in the settings."},

                new Module { buttonText = "Matchmaking Tier Spoof", method =() => Fun.SpoofRank(true, Fun.targetRank), disableMethod =() => Fun.SpoofRank(false), toolTip = "Spoofs your rank for competitive lobbies, letting you join higher or lower lobbies."},
                new Module { buttonText = "Matchmaking Platform Spoof", method =() => Fun.SpoofPlatform(true, "Quest"), disableMethod =() => Fun.SpoofPlatform(false), toolTip = "Spoofs your platform for competitive lobbies, letting you join quest lobbies."},

                new Module { buttonText = "Badge Tier Spoof", method = Fun.SpoofBadge, disableMethod =() => RankedProgression.enabled = false, toolTip = "Spoofs your competitive badge, showing that you have a higher rank than you really do."},
                new Module { buttonText = "Ignore Friend Privacy", method =() => FriendCards.enabled = true, disableMethod =() => FriendCards.enabled = false, toolTip = "Allows you to join friends that have their privacy setting set to private."},

                new Module { buttonText = "Arcade Teleporter Effect Spam", method = Fun.ArcadeTeleporterEffectSpam, toolTip = "Spams the effects on the virtual stump teleporters in the arcade when holding <color=green>trigger</color>."},
                new Module { buttonText = "Stump Teleporter Effect Spam", method = Fun.StumpTeleporterEffectSpam, toolTip = "Spams the effects on the virtual stump teleporter in forest when holding <color=green>trigger</color>."},

                new Module { buttonText = "Open Basement Door", method =() => Fun.SetBasementDoorState(true), isTogglable = false, toolTip = "Opens the basement door."},
                new Module { buttonText = "Open Elevator Door", method =() => Fun.SetElevatorDoorState(true), isTogglable = false, toolTip = "Opens the elevator door."},

                new Module { buttonText = "Close Basement Door", method =() => Fun.SetBasementDoorState(false), isTogglable = false, toolTip = "Closes the basement door."},
                new Module { buttonText = "Close Elevator Door", method =() => Fun.SetElevatorDoorState(false), isTogglable = false, toolTip = "Closes the elevator door."},

                new Module { buttonText = "Spam Open Basement Door", method =() => Fun.SetBasementDoorState(true), toolTip = "Repeatedly opens the basement door."},
                new Module { buttonText = "Spam Open Elevator Door", method =() => Fun.SetElevatorDoorState(true), toolTip = "Repeatedly opens the elevator door."},

                new Module { buttonText = "Spam Close Basement Door", method =() => Fun.SetBasementDoorState(false), toolTip = "Repeatedly closes the basement door."},
                new Module { buttonText = "Spam Close Elevator Door", method =() => Fun.SetElevatorDoorState(false), toolTip = "Repeatedly closes the elevator door."},

                new Module { buttonText = "Basement Door Spam", method = Fun.BasementDoorSpam, toolTip = "Repeatedly opens and closes the basement door."},
                new Module { buttonText = "Elevator Door Spam", method = Fun.ElevatorDoorSpam, toolTip = "Repeatedly opens and closes the elevator door."},

                new Module { buttonText = "Custom Virtual Stump Video", enableMethod = Fun.CustomVirtualStumpVideo, disableMethod = Fun.DisableCustomVirtualStumpVideo, toolTip = "Plays a video by the virtual stump VR headset in stump."},

                new Module { buttonText = "Get Builder Watch", method = Blocks.GiveBuilderWatch, isTogglable = false, toolTip = "Gives you the builder watch without needing to be in attic."},
                new Module { buttonText = "Remove Builder Watch", method = Blocks.RemoveBuilderWatch, isTogglable = false, toolTip = "Disables the builder watch."},

                

                
                
                

                
                

                new Module { buttonText = "Fast Gliders", enableMethod =() => Gliders.ModifyGliderSpeed(0.5f, 0.5f), disableMethod =() => Gliders.ModifyGliderSpeed(0.1f, 0.2f), toolTip = "Makes the gliders fast."},
                new Module { buttonText = "Slow Gliders", enableMethod =() => Gliders.ModifyGliderSpeed(0.05f, 0.05f), disableMethod =() => Gliders.ModifyGliderSpeed(0.1f, 0.2f), toolTip = "Makes the gliders slow."},

                new Module { buttonText = "Glider Blind Gun", method = Gliders.GliderBlindGun, toolTip = "Moves all of the gliders to whoever your hand desires' faces." },
                new Module { buttonText = "Glider Blind All", method = Gliders.GliderBlindAll, toolTip = "Moves all of the gliders to everyone's faces." },

                new Module { buttonText = "Fast Ropes", enableMethod =() => RopeAttach.enabled = true, disableMethod =() => RopeAttach.enabled = false, toolTip = "Makes ropes go five times faster when jumping on them."},
                new Module { buttonText = "Rope Grab Reach", method = Fun.RopeGrabReach, disableMethod =() => Climbing.enabled = false, toolTip = "Allows you to grab ropes from farther away."},

                new Module { buttonText = "No Respawn Gliders", enableMethod =() => GliderRespawn.enabled = true, disableMethod =() => GliderRespawn.enabled = false, toolTip = "Doesn't respawn gliders that go too far outside the bounds of clouds."},

                new Module { buttonText = "Anti Grab", enableMethod =() => GuardianGrabs.enabled = true, disableMethod =() => GuardianGrabs.enabled = false, toolTip = "Prevents players from picking you up in guardian."},
                new Module { buttonText = "Anti Knockback", enableMethod =() => KnockbackOverride.enabled = true, disableMethod =() => KnockbackOverride.enabled = false, toolTip = "Prevents any force from knocking you back."},
                new Module { buttonText = "Multiply Knockback", enableMethod =() => KnockbackMultiplier.enabled = true, disableMethod =() => KnockbackMultiplier.enabled = false, toolTip = "Multiplies your knockback by an amount set in settings."},
                new Module { buttonText = "Multiply Self Knockback", enableMethod =() => SelfKnockback.enabled = true, disableMethod =() => SelfKnockback.enabled = false, toolTip = "Multiplies your projectile knockback by an amount set in settings."},

                new Module { buttonText = "Fast Throw", method =() => { VelocityTracking.enabled = true; VelocityTracking.multipleFactor = 10f; }, disableMethod =() => VelocityTracking.enabled = false, toolTip = "Multiplies your throw factor by 10."},
                new Module { buttonText = "Slow Throw", method =() => { VelocityTracking.enabled = true; VelocityTracking.multipleFactor = 0.1f; }, disableMethod =() => VelocityTracking.enabled = false, toolTip = "Multiplies your throw factor by 0.1."},

                new Module { buttonText = "Slingshot Self", enableMethod =() => Fun.SlingshotSelf(), disableMethod =() => Fun.SlingshotSelf(false), toolTip = "Gives you a client sided slingshot."},
                new Module { buttonText = "Angry Birds", enableMethod =() => ProjectileLaunch.enabled = true, method = Fun.AngryBirdsSounds, disableMethod =() => ProjectileLaunch.enabled = false, toolTip = "Flings you in whatever direction your slingshot's projectiles are heading."},

                new Module { buttonText = "Large Snowballs", enableMethod =() => SnowballGrowth.enabled = true, disableMethod =() => SnowballGrowth.enabled = false, toolTip = "Makes snowballs by default the largest size."},

                new Module { buttonText = "Fast Snowballs", overlapText = "Fast Projectiles", method = Projectiles.FastSnowballs, disableMethod = Projectiles.FixSnowballs, toolTip = "Makes projectiles go really fast when thrown."},
                new Module { buttonText = "Slow Snowballs", overlapText = "Slow Projectiles", method = Projectiles.SlowSnowballs, disableMethod = Projectiles.FixSnowballs, toolTip = "Makes projectiles go really slow when thrown."},

                new Module { buttonText = "Projectile Range", method = Fun.ProjectileRange, toolTip = "Increases the hitbox scale of your projectiles."},

                new Module { buttonText = "Rainbow Held Projectiles", enableMethod = Projectiles.HookProjectileColors, method =() => Fun.projHookColor = Color.HSVToRGB(Time.frameCount / 180f % 1f, 1f, 1f), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Changes your projectile's color to be rainbow"},
                new Module { buttonText = "Flash Held Projectiles", enableMethod = Projectiles.HookProjectileColors, method =() => Fun.projHookColor = Time.time % 0.2f > 0.1f ? Color.white : Color.black, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Changes your projectile's color to be rainbow"},
                new Module { buttonText = "Strobe Held Projectiles", enableMethod = Projectiles.HookProjectileColors, method =() => Fun.projHookColor = RandomColor(), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Changes your projectile's color to be rainbow"},
                new Module { buttonText = "Custom Held Projectiles", enableMethod = Projectiles.HookProjectileColors, method =() => Fun.projHookColor = new Color(Fun.red / 10f, Fun.green / 10f, Fun.blue / 10f), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Changes your projectile's color to be whatever your custom projectile color is set to in the projectile settings."},

                new Module { buttonText = "Fast Hoverboard", method = Hoverboards.FastHoverboard, disableMethod = Hoverboards.FixHoverboard, toolTip = "Makes your hoverboard go really fast."},
                new Module { buttonText = "Slow Hoverboard", method = Hoverboards.SlowHoverboard, disableMethod = Hoverboards.FixHoverboard, toolTip = "Makes your hoverboard go really slow."},

                new Module { buttonText = "Rainbow Hoverboard", method = Hoverboards.RainbowHoverboard, toolTip = "Changes your hoverboard's color to be rainbow."},
                new Module { buttonText = "Strobe Hoverboard", overlapText = "Flash Hoverboard", method = Hoverboards.StrobeHoverboard, toolTip = "Changes your hoverboard's color to flash between black and white."},
                new Module { buttonText = "Random Hoverboard", overlapText = "Strobe Hoverboard", method = Hoverboards.RandomHoverboard, toolTip = "Changes your hoverboard's color to flash random colors."},

                new Module { buttonText = "Black Screen Gun", method =() => Hoverboards.HoverboardScreenGun(Color.black), toolTip = "Uses the hoverboards to blind whoever your hand desires."},
                new Module { buttonText = "Black Screen All", method =() => Hoverboards.HoverboardScreenAll(Color.black), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the hoverboards to blind everyone in the room."},

                new Module { buttonText = "White Screen Gun", method =() => Hoverboards.HoverboardScreenGun(Color.white), toolTip = "Uses the hoverboards to make whoever your hand desires' screen white."},
                new Module { buttonText = "White Screen All", method =() => Hoverboards.HoverboardScreenAll(Color.white), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the hoverboards to flash the screen of everyone in the room."},

                new Module { buttonText = "Flash Screen Gun", method =() => Hoverboards.HoverboardScreenGun(Time.time % 0.2f > 0.1f ? Color.white : Color.black), toolTip = "Uses the hoverboards to flash the screen of whoever your hand desires."},
                new Module { buttonText = "Flash Screen All", method =() => Hoverboards.HoverboardScreenAll(Time.time % 0.2f > 0.1f ? Color.white : Color.black), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the hoverboards to blind everyone in the room."},

                new Module { buttonText = "Strobe Screen Gun", method =() => Hoverboards.HoverboardScreenGun(RandomColor()), toolTip = "Uses the hoverboards to flash the screen of whoever your hand desires."},
                new Module { buttonText = "Strobe Screen All", method =() => Hoverboards.HoverboardScreenAll(RandomColor()), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the hoverboards to blind everyone in the room."},

                new Module { buttonText = "Rainbow Screen Gun", method =() => Hoverboards.HoverboardScreenGun(Color.HSVToRGB(Time.frameCount / 180f % 1f, 1f, 1f)), toolTip = "Uses the hoverboards to make the screen of whoever your hand desires rainbow."},
                new Module { buttonText = "Rainbow Screen All", method =() => Hoverboards.HoverboardScreenAll(Color.HSVToRGB(Time.frameCount / 180f % 1f, 1f, 1f)), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the hoverboards to make the screen of everyone in the room rainbow."},

                new Module { buttonText = $"Hoverboard Spam {Bracket("green", "G")}", method = Hoverboards.HoverboardSpam, toolTip = "Spams hoverboards from your hand when holding <color=green>grip</color>."},
                new Module { buttonText = "Spam Spawn Hoverboards", method = Hoverboards.SpawnHoverboard, toolTip = "Spam spawns hoverboards at your player position."},
                new Module { buttonText = "Spawn Hoverboard", method = Hoverboards.SpawnHoverboard, isTogglable = false, toolTip = "Spawns a hoverboard at your player position."},

                new Module { buttonText = "Start All Races", method = Fun.StartAllRaces, isTogglable = false, toolTip = "Starts every race in the hoverboard map."},

                new Module { buttonText = "Override Hand Link", method =() => HandLinkGrounded.enabled = true, disableMethod =() => HandLinkGrounded.enabled = false, toolTip = "Prioritizes you when you or others grab onto you."},
                new Module { buttonText = "Disable Hand Link", method =() => { References.LocalRig.leftHandLink.BreakLink(); References.LocalRig.rightHandLink.BreakLink(); }, toolTip = "Disables you from grabbing onto other players and them from grabbing onto you."},
                new Module { buttonText = "Anti Hand Link", method =() => HandLink.enabled = true, disableMethod =() => HandLink.enabled = false, toolTip = "Disables you from moving when grabbing onto other players."},

                new Module { buttonText = "Fast Throw Players", method =() => HandLinkRelease.enabled = true, disableMethod =() => HandLinkRelease.enabled = false, toolTip = "Makes players go really fast when you throw them."},

                new Module { buttonText = "Noclip Building", method = Blocks.NoclipBuilding, disableMethod = Blocks.DisableNoclipBuilding, toolTip = "Disables the colliders of every block in the block map."},
                new Module { buttonText = "Overlap Building", enableMethod =() => PieceOverlap.enabled = true, disableMethod =() => PieceOverlap.enabled = false, toolTip = "Lets you place pieces inside of each other in the attic."},
                new Module { buttonText = "Small Building", enableMethod =() => PieceInteraction.enabled = true, disableMethod =() => PieceInteraction.enabled = false, toolTip = "Lets you build in the block map while small."},
                new Module { buttonText = "Multi Grab", method = Blocks.MultiGrab, toolTip = "Lets you grab multiple objects."},

                new Module { buttonText = "Block Size Toggle", method = Fun.AtticSizeToggle, toolTip = "Toggles your scale when pressing <color=green>grip</color> or <color=green>trigger</color>."},
                new Module { buttonText = $"Grab All Nearby Blocks {Bracket("green", "G")}", method = Blocks.GrabAllBlocksNearby, toolTip = "Grabs every nearby building block when holding <color=green>G</color>."},
                new Module { buttonText = $"Grab All Selected Blocks {Bracket("green", "G")}", method = Blocks.GrabAllSelectedNearby, toolTip = "Grabs every nearby building block that matches your selection when holding <color=green>G</color>."},

                new Module { buttonText = $"Massive Block {Bracket("green", "G")}", method = Blocks.MassiveBlock, toolTip = "Spawns you a massive block when you press <color=green>grip</color>."},
                new Module { buttonText = "Select Block Gun", method = Blocks.SelectBlockGun, toolTip = "Selects whatever building block your hand desires to be used for the building mods." },
                new Module { buttonText = "Copy Block Info Gun", method = Blocks.CopyBlockInfoGun, toolTip = "Copies whatever building block your hand desires to be used for the building mods to your clipboard." },
                new Module { buttonText = "Building Block Browser", method = Blocks.BlockBrowser, isTogglable = false, toolTip = "Browse through every block that you can spawn and select it." },

                new Module { buttonText = $"Grab Building Blocks {Bracket("green", "G")}", method = Blocks.SpamGrabBlocks, toolTip = "Forces the building block into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Building Block Minigun {Bracket("green", "G")}", method = Blocks.BuildingBlockMinigun, toolTip = "Spams building blocks out of your hand when holding <color=green>grip</color>." },
                new Module { buttonText = "Building Block Gun", method = Blocks.BlocksGun, toolTip = "Moves the building blocks to wherever your hand desires." },

                new Module { buttonText = "Orbit Building Blocks", method = Blocks.OrbitBlocks, toolTip = "Orbits the building blocks around you." },
                new Module { buttonText = "Rain Building Blocks", method = Blocks.RainBuildingBlocks, toolTip = "Makes the building blocks fall around you like rain." },
                new Module { buttonText = "Building Block Fountain", method = Blocks.BuildingBlockFountain, toolTip = "Spurts building blocks out of your head like a fountain." },
                new Module { buttonText = "Building Block Aura", method = Blocks.BuildingBlockAura, toolTip = "Moves the building blocks around you at random positions." },

                new Module { buttonText = "Building Block Text Gun", enableMethod =() => PromptText("What text would you like to show?", () => Fun.textToRender = keyboardInput.ToUpper(), null, "Done", "Cancel"), method = Blocks.BuildingBlockTextGun, toolTip = "Spawns entities in the shape of the text you desire in the ghost reactor."},

                new Module { buttonText = "Place Building Block Gun", method = Blocks.PlaceBlockGun, toolTip = "Places whatever building block your hand desires on the last grid space you have placed blocks on." },

                new Module { buttonText = "Destroy Building Block Gun", method = Blocks.DestroyBlockGun, toolTip = "Shreds whatever building block your hand desires." },
                new Module { buttonText = "Destroy Building Blocks", overlapText = "Destroy All Building Blocks", method = Blocks.DestroyBlocks, toolTip = "Shreds every building block." },

                new Module { buttonText = "Save Builder Table Data", method = Blocks.SaveBuilderTableData, isTogglable = false, toolTip = "Dumps the data of your current build to a JSON file." },
                new Module { buttonText = "Load Builder Table Data", method = Blocks.LoadBuilderTableData, isTogglable = false, toolTip = "Loads the data of the dumped JSON files in your game directory and saves it to your current slot." },

                new Module { buttonText = "Disable Critters Dome", enableMethod =() => GetObject("Critters/Critters_Environment/Landscape/Critters_Landscape_Dome").SetActive(false), disableMethod =() => GetObject("Critters/Critters_Environment/Landscape/Critters_Landscape_Dome").SetActive(true), toolTip = "Disables the critters dome." },
                new Module { buttonText = "Enable Forest Dome", enableMethod =() => GetObject("Environment Objects/LocalObjects_Prefab/Forest/Super Infection Zone - Forest Variant/ForestDome_Prefab").SetActive(true), disableMethod =() => GetObject("Environment Objects/LocalObjects_Prefab/Forest/Super Infection Zone - Forest Variant/ForestDome_Prefab").SetActive(false), toolTip = "Enables the dome in forest." },

                new Module { buttonText = $"Critter Spam {Bracket("green", "G")}", method = Critters.CritterSpam, toolTip = "Spawns critters on your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Minigun {Bracket("green", "G")}", method = Critters.CritterMinigun, toolTip = "Shoots critters out of your hand when holding <color=green>grip</color>"},
                new Module { buttonText = "Critter Gun", method = Critters.CritterGun, toolTip = "Spawns critters at wherever your hand desires."},

                new Module { buttonText = $"Critter Sticky Goo Spam {Bracket("green", "G")}", method =() => Critters.ObjectSpam(CrittersActor.CrittersActorType.StickyGoo), toolTip = "Spams sticky goo in your hand when holding <color=green>grip</color>"},

                new Module { buttonText = $"Critter Food Spam {Bracket("green", "G")}", method =() => Critters.ObjectSpam(CrittersActor.CrittersActorType.Food), toolTip = "Spams food in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Noise Maker Spam {Bracket("green", "G")}", method =() => Critters.ObjectSpam(CrittersActor.CrittersActorType.NoiseMaker), toolTip = "Spams noise makers in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Stun Bomb Spam {Bracket("green", "G")}", method =() => Critters.ObjectSpam(CrittersActor.CrittersActorType.StunBomb), toolTip = "Spams stun bombs in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Sticky Trap Spam {Bracket("green", "G")}", method =() => Critters.ObjectSpam(CrittersActor.CrittersActorType.StickyTrap), toolTip = "Spams sticky traps in your hand when holding <color=green>grip</color>"},

                new Module { buttonText = "Critter Sticky Goo Gun", method =() => Critters.ObjectGun(CrittersActor.CrittersActorType.StickyGoo), toolTip = "Spams sticky goo at wherever your hand desires."},

                new Module { buttonText = "Critter Food Gun", method =() => Critters.ObjectGun(CrittersActor.CrittersActorType.Food), toolTip = "Spams food at wherever your hand desires."},
                new Module { buttonText = "Critter Noise Maker Gun", method =() => Critters.ObjectGun(CrittersActor.CrittersActorType.NoiseMaker), toolTip = "Spams noise makers at wherever your hand desires."},
                new Module { buttonText = "Critter Stun Bomb Gun", method =() => Critters.ObjectGun(CrittersActor.CrittersActorType.StunBomb), toolTip = "Spams stun bombs at wherever your hand desires."},
                new Module { buttonText = "Critter Sticky Trap Gun", method =() => Critters.ObjectGun(CrittersActor.CrittersActorType.StickyTrap), toolTip = "Spams sticky traps at wherever your hand desires."},

                new Module { buttonText = $"Critter Shockwave Effect Spam {Bracket("green", "G")}", method =() => Critters.EffectSpam(CrittersManager.CritterEvent.StunExplosion), toolTip = "Spams the shockwave particles in your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Critter Sticky Effect Spam {Bracket("green", "G")}", method =() => Critters.EffectSpam(CrittersManager.CritterEvent.StickyDeployed), toolTip = "Spams the sticky particles in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Eating Effect Spam {Bracket("green", "G")}", method =() => Critters.EffectSpam(CrittersManager.CritterEvent.StickyTriggered), toolTip = "Spams the sticky eating particles in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Noise Effect Spam {Bracket("green", "G")}", method =() => Critters.EffectSpam(CrittersManager.CritterEvent.NoiseMakerTriggered), toolTip = "Spams the noise particles in your hand when holding <color=green>grip</color>"},
                new Module { buttonText = $"Critter Particle Effect Spam {Bracket("green", "G")}", method =() => Critters.EffectSpam((CrittersManager.CritterEvent)Random.Range(0, 4)), toolTip = "Spams every particle in your hand when holding <color=green>grip</color>"},

                new Module { buttonText = "Critter Shockwave Effect Gun", method =() => Critters.EffectGun(CrittersManager.CritterEvent.StunExplosion), toolTip = "Spams the shockwave particles at wherever your hand desires."},
                new Module { buttonText = "Critter Sticky Effect Gun", method =() => Critters.EffectGun(CrittersManager.CritterEvent.StickyDeployed), toolTip = "Spams the sticky particles at wherever your hand desires."},
                new Module { buttonText = "Critter Eating Effect Gun", method =() => Critters.EffectGun(CrittersManager.CritterEvent.StickyTriggered), toolTip = "Spams the sticky eating particles at wherever your hand desires."},
                new Module { buttonText = "Critter Noise Effect Gun", method =() => Critters.EffectGun(CrittersManager.CritterEvent.NoiseMakerTriggered), toolTip = "Spams the noise particles at wherever your hand desires."},
                new Module { buttonText = "Critter Particle Effect Gun", method =() => Critters.EffectGun((CrittersManager.CritterEvent)Random.Range(0, 4)), toolTip = "Spams every particle at wherever your hand desires."},

                new Module { buttonText = $"Grab ID Card {Bracket("green", "G")}", method = Fun.GrabIDCard, toolTip = "Puts the ID card in your hand." },
                new Module { buttonText = "Entity Reach", method =() => EntityGrabbing.enabled = true, disableMethod =() => EntityGrabbing.enabled = false, toolTip = "Gives you the ability to grab entities from farther away in the horror map." },

                new Module { buttonText = "Infinite Prop Distance", method =() => Fun.SetPropDistanceLimit(float.MaxValue), disableMethod =() => Fun.SetPropDistanceLimit(0.35f), toolTip = "Removes the distance limit of props in the prop hunt map." },
                new Module { buttonText = "Prop Noclip", method =() => PropHuntFollower.enabled = true, disableMethod =() => PropHuntFollower.enabled = false, toolTip = "Allows you to put props in walls in the prop hunt map." },

                new Module { buttonText = "Spaz Tool Stations", method = Fun.SpazToolStations, toolTip = "Spazzes out the tool purchase stations in the horror map." },
                new Module { buttonText = "Purchase All Tool Stations", method = Fun.PurchaseAllToolStations, toolTip = "Makes every tool purchase station force purchase in the horror map." },

                new Module { buttonText = $"Ghost Reactor Gate Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnergyCostGate"]), toolTip = "Spawns gates out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Core Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorCollectibleCore"]), toolTip = "Spawns collectible cores out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Slime Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorCollectibleSentientCore"]), toolTip = "Spawns sentient collectible cores out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Tool Spam {Bracket("green", "G")}", method = Fun.ToolSpamGrip, toolTip = "Spawns random tools out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Flower Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorCollectibleFlower"]), toolTip = "Spawns flowers out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Crate Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorBreakableCrate"]), toolTip = "Spawns barrels out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Barrel Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorBreakableBarrel"]), toolTip = "Spawns barrels out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Bug Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyPest"]), toolTip = "Spawns the annoying bug enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Large Bug Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyPestBig"]), toolTip = "Spawns the large annoying bug enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Ranged Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyRanged"]), toolTip = "Spawns ranged enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Chaser Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyChaser"]), toolTip = "Spawns chasing enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Armored Ranged Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyRangedArmored"]), toolTip = "Spawns armored ranged enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Armored Chaser Enemy Spam {Bracket("green", "G")}", method =() => Fun.SpamObjectGrip(Fun.ObjectByName["GhostReactorEnemyChaserArmored"]), toolTip = "Spawns armored chasing enemies out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Ghost Reactor Entity Spam {Bracket("green", "G")}", method = Fun.SpamEntityGrip, toolTip = "Spawns a random entity out of your hand when holding <color=green>grip</color>."},

                new Module { buttonText = "Ghost Reactor Gate Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnergyCostGate"]), toolTip = "Spawns gates at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Core Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorCollectibleCore"]), toolTip = "Spawns collectible cores at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Slime Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorCollectibleSentientCore"]), toolTip = "Spawns sentient collectible cores at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Tool Gun", method = Fun.ToolSpamGun, toolTip = "Spawns random tools at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Flower Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorCollectibleFlower"]), toolTip = "Spawns flowers at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Crate Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorBreakableCrate"]), toolTip = "Spawns barrels at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Barrel Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorBreakableBarrel"]), toolTip = "Spawns barrels at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Bug Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyPest"]), toolTip = "Spawns the annoying bug enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Large Bug Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyPestBig"]), toolTip = "Spawns the large annoying bug enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Ranged Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyRanged"]), toolTip = "Spawns ranged enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Chaser Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyChaser"]), toolTip = "Spawns chasing enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Armored Ranged Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyRangedArmored"]), toolTip = "Spawns armored ranged enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Armored Chaser Enemy Gun", method =() => Fun.SpamObjectGun(Fun.ObjectByName["GhostReactorEnemyChaserArmored"]), toolTip = "Spawns armored chasing enemies at wherever your hand desires."},
                new Module { buttonText = "Ghost Reactor Entity Gun", method = Fun.SpamEntityGun, toolTip = "Spawns a random entity at wherever your hand desires."},

                new Module { buttonText = "Ghost Reactor Rain Entities", method = Fun.RainEntities, toolTip = "Makes random entities fall around you like rain."},
                new Module { buttonText = "Ghost Reactor Entity Aura", method = Fun.EntityAura, toolTip = "Creates a ball of random entities around you."},
                new Module { buttonText = "Ghost Reactor Entity Fountain", method = Fun.EntityFountain, toolTip = "Spurts random entities out of your head like a fountain."},

                new Module { buttonText = "Ghost Reactor Text Gun", enableMethod =() => PromptText("What text would you like to show?", () => Fun.textToRender = keyboardInput.ToUpper(), null, "Done", "Cancel"), method = Fun.GhostReactorTextGun, toolTip = "Spawns entities in the shape of the text you desire in the ghost reactor."},
                new Module { buttonText = "Ghost Reactor Draw Gun", method = Fun.GhostReactorDrawGun, toolTip = "Allows you to draw with entities in ghost reactor."},

                new Module { buttonText = "Destroy Entity Gun", method = Fun.DestroyEntityGun, toolTip = "Destroys any entity which your hand desires."},

                new Module { buttonText = "Infinite Jet Fuel", method =() => WristJetFuel.enabled = true, disableMethod =() => WristJetFuel.enabled = false, toolTip = "Gives the jet gadgets in Super Infection infinite fuel."},
                new Module { buttonText = "Infinite Platforms", method =() => PlatformDeployer.enabled = true, disableMethod =() => PlatformDeployer.enabled = false, toolTip = "Gives the platform spawner gadgets in Super Infection infinite platforms."},
                new Module { buttonText = "Infinite Resources", method = Fun.InfiniteResources, toolTip = "Gives you infinite resources in the Super Infection gamemode."},
                new Module { buttonText = "Complete All Quests", method = Fun.CompleteAllQuests, isTogglable = false, toolTip = "Completes every quest in the Super Infection gamemode."},
                new Module { buttonText = "Claim All Terminals", method = Fun.ClaimAllTerminals, isTogglable = false, toolTip = "Claims every terminal in the Super Infection gamemode."},
                new Module { buttonText = "Unlock All Gadgets", method = Fun.UnlockAllGadgets, toolTip = "Unlocks every gadget in the Super Infection gamemode."},

                new Module { buttonText = "No Blaster Cooldown", method =() => ChargeBlasterCooldown.enabled = true, disableMethod =() => ChargeBlasterCooldown.enabled = false, toolTip = "Removes the cooldown on the blaster."},
                new Module { buttonText = "Blaster Aimbot", enableMethod =() => ChargeBlasterFire.enabled = true, method = Fun.DebugBlasterAimbot, disableMethod =() => ChargeBlasterFire.enabled = false, toolTip = "Automatically aims the blaster towards players."},

                new Module { buttonText = $"Blaster Laser Spam {Bracket("green", "G")}", method = Fun.BlasterLaserSpam, toolTip = "Spams lasers out of your hand when holding <color=green>grip</color>."},

                new Module { buttonText = "Blaster Float Gun", method =() => Fun.BlasterFlingGun(Vector3.up), toolTip = "Uses the blasters to fling whoever your hand desires vertically."},
                new Module { buttonText = "Blaster Float All", method =() => Fun.BlasterFlingAll(Vector3.up), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the blasters to fling everyone in the room vertically."},

                new Module { buttonText = "Blaster Control Gun", method = Fun.BlasterControlGun, toolTip = "Uses the blasters to control whoever your hand desires."},

                new Module { buttonText = "Blaster Fling Gun", method =() => Fun.BlasterFlingGun(RandomVector3()), toolTip = "Uses the blasters to fling whoever your hand desires."},
                new Module { buttonText = "Blaster Fling All", method =() => Fun.BlasterFlingAll(RandomVector3()), disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the blasters to fling everyone in the room."},

                new Module { buttonText = "Blaster Fling Towards Gun", overlapText = "Blaster Bring Gun", aliases = new[] { "Blaster Fling Towards Gun" }, method = Fun.BlasterFlingTowardsGun, toolTip = "Uses the blasters to fling whoever your hand desires towards you."},
                new Module { buttonText = "Blaster Fling Towards All", overlapText = "Blaster Bring All", aliases = new[] { "Blaster Fling Towards All" }, method = Fun.BlasterFlingTowardsAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the blasters to fling everyone in the room towards you."},

                new Module { buttonText = "Blaster Fling Away Gun", overlapText = "Blaster Push Gun", aliases = new[] { "Blaster Fling Towards Gun" }, method = Fun.BlasterFlingAwayGun, toolTip = "Uses the blasters to fling whoever your hand desires away from you."},
                new Module { buttonText = "Blaster Fling Away All", overlapText = "Blaster Push All", aliases = new[] { "Blaster Fling Towards All" }, method = Fun.BlasterFlingAwayAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Uses the blasters to fling everyone in the room away from you."},

                new Module { buttonText = "Blaster Kick Gun", method = Fun.BlasterKickGun, toolTip = "Kicks whoever your hand desires using the blasters." },
                new Module { buttonText = $"Blaster Kick All {Bracket("green", "T")}", method = Fun.BlasterKickAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks everyone in the room when holding <color=green>trigger</color> using the blasters." },

                new Module { buttonText = "Blaster Crash Gun", method = Fun.BlasterCrashGun, toolTip = "Crashes whoever your hand desires using the blasters." },
                new Module { buttonText = $"Blaster Crash All {Bracket("green", "T")}", method = Fun.BlasterCrashAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Crashes everyone in the room when holding <color=green>trigger</color> using the blasters." },

                new Module { buttonText = $"Stilt Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["StiltGadget FixedScaledLong"]), toolTip = "Spawns stilts out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Turkey Stilt Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["StiltGadget Turkey"]), toolTip = "Spawns stilts out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Motorized Stilt Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["StiltGadget Motorized3"]), toolTip = "Spawns motorized stilts out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Thruster Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["WristJetGadgetPropellor"]), toolTip = "Spawns thrusters out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Yoyo Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["SIGadgetDashYoyo"]), toolTip = "Spawns yoyos out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Blaster Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["MegaChargeBlasterGadget"]), toolTip = "Spawns blasters out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Lobber Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["BlastLobberGadget"]), toolTip = "Spawns lobbers out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Strider Tentacle Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["TentacleArmGadget_Strider"]), toolTip = "Spawns tentacles out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Crawler Tentacle Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["TentacleArmGadget_Crawler"]), toolTip = "Spawns tentacles out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Platform Deployer Spam {Bracket("green", "G")}", method =() => Fun.SpamGadgetGrip(Fun.GadgetByName["PlatformDeployerGadget"]), toolTip = "Spawns platform deployers out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Gadget Spam {Bracket("green", "G")}", method = Fun.GadgetSpamGrip, toolTip = "Spawns random gadgets out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Resource Spam {Bracket("green", "G")}", method = Fun.ResourceSpamGrip, toolTip = "Spawns random resources out of your hand when holding <color=green>grip</color>."},

                new Module { buttonText = "Stilt Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["StiltGadget FixedScaledLong"]), toolTip = "Spawns stilts at wherever your hand desires."},
                new Module { buttonText = "Turkey Stilt Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["StiltGadget Turkey"]), toolTip = "Spawns stilts at wherever your hand desires."},
                new Module { buttonText = "Motorized Stilt Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["StiltGadget Motorized3"]), toolTip = "Spawns motorized stilts at wherever your hand desires."},
                new Module { buttonText = "Thruster Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["WristJetGadgetPropellor"]), toolTip = "Spawns thrusters at wherever your hand desires."},
                new Module { buttonText = "Yoyo Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["SIGadgetDashYoyo"]), toolTip = "Spawns yoyos at wherever your hand desires."},
                new Module { buttonText = "Blaster Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["MegaChargeBlasterGadget"]), toolTip = "Spawns blasters at wherever your hand desires."},
                new Module { buttonText = "Lobber Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["BlastLobberGadget"]), toolTip = "Spawns lobbers at wherever your hand desires."},
                new Module { buttonText = "Strider Tentacle Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["TentacleArmGadget_Strider"]), toolTip = "Spawns tentacles at wherever your hand desires."},
                new Module { buttonText = "Crawler Tentacle Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["TentacleArmGadget_Crawler"]), toolTip = "Spawns tentacles at wherever your hand desires."},
                new Module { buttonText = "Platform Deployer Gun", method =() => Fun.SpamGadgetGun(Fun.GadgetByName["PlatformDeployerGadget"]), toolTip = "Spawns platform deployers at wherever your hand desires."},
                new Module { buttonText = "Gadget Gun", method = Fun.GadgetSpamGun, toolTip = "Spawns random gadgets at wherever your hand desires."},
                new Module { buttonText = "Resource Gun", method = Fun.ResourceSpamGun, toolTip = "Spawns random resources at wherever your hand desires."},

                new Module { buttonText = "Rain Gadgets", method = Fun.RainGadgets, toolTip = "Makes random gadgets fall around you like rain."},
                new Module { buttonText = "Gadget Aura", method = Fun.GadgetAura, toolTip = "Creates a ball of random gadgets around you."},
                new Module { buttonText = "Gadget Fountain", method = Fun.GadgetFountain, toolTip = "Spurts random gadgets out of your head like a fountain."},

                new Module { buttonText = "Super Infection Text Gun", enableMethod =() => PromptText("What text would you like to show?", () => Fun.textToRender = keyboardInput.ToUpper(), null, "Done", "Cancel"), method = Fun.SuperInfectionTextGun, toolTip = "Spawns entities in the shape of the text you desire in the Super Infection gamemode."},
                new Module { buttonText = "Super Infection Draw Gun", method = Fun.SuperInfectionDrawGun, toolTip = "Allows you to draw with entities in Super Infection."},

                new Module { buttonText = "Destroy Gadget Gun", method = Fun.DestroyGadgetGun, toolTip = "Destroys any gadget which your hand desires."},

                new Module { buttonText = $"Fire Sound Spam {Bracket("green", "T")}", enableMethod =() => Cosmetics.CheckOwnedCosmetic("LBALH."), method = Fun.FireSoundSpam, toolTip = "Spams fire sounds when holding <color=green>trigger</color>."},

                new Module { buttonText = "Bubble Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(33), method =() => Fun.BubblerGun(33, Quaternion.identity, 0.1f), toolTip = "Uses the bubbler to spawn bubbles at wherever your hand desires."},
                new Module { buttonText = "Fire Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(175), method =() => Fun.BubblerGun(175, Quaternion.Euler(180f, 0f, 0f)), toolTip = "Uses the bubbler to spawn bubbles at wherever your hand desires."},

                new Module { buttonText = "White Color Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(629), method = Fun.WhiteColorGun, toolTip = "Sprays whoever your hand desires with the sunblock spray cosmetic."},
                new Module { buttonText = "White Color All", enableMethod =() => Projectiles.CheckOwnedThrowable(629), method =() => Fun.WhiteColorTarget(GetTargetPlayer()), disableMethod =() => References.LocalRig.enabled = false, toolTip = "Sprays everyone in the room with the sunblock spray cosmetic."},

                new Module { buttonText = "Black Color Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(600), method = Fun.BlackColorGun, toolTip = "Uses the smoke bomb to make whoever your hand desires black."},
                new Module { buttonText = "Black Color All", enableMethod =() => Projectiles.CheckOwnedThrowable(600), method =() => Fun.BlackColorTarget(GetTargetPlayer()), disableMethod =() => References.LocalRig.enabled = false, toolTip = "Uses the smoke bomb to make everyone in the room black."},

                new Module { buttonText = "Chicken Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(651), method = Fun.ChickenGun, toolTip = "Uses the smoke bomb to make whoever your hand desires black."},
                new Module { buttonText = "Chicken All", enableMethod =() => Projectiles.CheckOwnedThrowable(651), method =() => Fun.ChickenTarget(GetTargetPlayer()), disableMethod =() => References.LocalRig.enabled = false, toolTip = "Uses the smoke bomb to make everyone in the room black."},

                new Module { buttonText = $"Whoopee Cushion Spam {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(626), method =() => Projectiles.ThrowableProjectileSpam(626), toolTip = "Spawns whoopee cushions on your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Whoopee Cushion Minigun {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(626), method =() => Projectiles.ThrowableProjectileMinigun(626), toolTip = "Shoots whoopee cushions out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = "Whoopee Cushion Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(626), method =() => Projectiles.ThrowableProjectileGun(626), toolTip = "Spawns whoopee cushions at wherever your hand desires."},

                new Module { buttonText = $"Smoke Bomb Spam {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(600), method =() => Projectiles.ThrowableProjectileSpam(600), toolTip = "Spawns smoke bombs on your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Smoke Bomb Minigun {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(600), method =() => Projectiles.ThrowableProjectileMinigun(600), toolTip = "Shoots smoke bombs out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = "Smoke Bomb Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(600), method =() => Projectiles.ThrowableProjectileGun(600), toolTip = "Spawns smoke bombs at wherever your hand desires."},

                new Module { buttonText = $"Firecracker Spam {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(587), method =() => Projectiles.ThrowableProjectileSpam(587), toolTip = "Spawns firecrackers on your hand when holding <color=green>grip</color>."},
                new Module { buttonText = $"Firecracker Minigun {Bracket("green", "G")}", enableMethod =() => Projectiles.CheckOwnedThrowable(587), method =() => Projectiles.ThrowableProjectileMinigun(587), toolTip = "Shoots firecrackers out of your hand when holding <color=green>grip</color>."},
                new Module { buttonText = "Firecracker Gun", enableMethod =() => Projectiles.CheckOwnedThrowable(587), method =() => Projectiles.ThrowableProjectileGun(587), toolTip = "Spawns firecrackers at wherever your hand desires."},

                new Module { buttonText = "Spaz All Moles", method = Fun.SpazMoleMachines, toolTip = "Gives the moles a seizure."},
                new Module { buttonText = "Auto Start Moles", method = Fun.AutoStartMoles, toolTip = "Automatically starts the mole games."},
                new Module { buttonText = "Auto Hit Moles", method =() => Fun.AutoHitMoleType(false), toolTip = "Hits all of the moles automatically."},
                new Module { buttonText = "Auto Hit Hazards", method =() => Fun.AutoHitMoleType(true), toolTip = "Hits all of the hazards automatically."},

                new Module { buttonText = "No Respawn Bug", enableMethod =() => Bugs.SetRespawnDistance("Floating Bug Holdable"), disableMethod =() => Bugs.SetRespawnDistance("Floating Bug Holdable", 50f), toolTip = "Doesn't respawn the bug if it goes too far outside the bounds of forest."},
                new Module { buttonText = "No Respawn Bat", enableMethod =() => Bugs.SetRespawnDistance("Cave Bat Holdable"), disableMethod =() => Bugs.SetRespawnDistance("Cave Bat Holdable", 50f), toolTip = "Doesn't respawn the bat if it goes too far outside the bounds of caves."},
                new Module { buttonText = "No Respawn Firefly", enableMethod =() => Bugs.SetRespawnDistance("Firefly"), disableMethod =() => Bugs.SetRespawnDistance("Firefly", 50f), toolTip = "Doesn't respawn the firefly if it goes too far outside the bounds of forest."},

                new Module { buttonText = "Permanent Bug", method =() => Bugs.PermanentOwnership("Floating Bug Holdable"), disableMethod =() => Ownership.blacklistedGuards.Clear(), toolTip = "Disables other players from grabbing the bug."},
                new Module { buttonText = "Permanent Bat", method =() => Bugs.PermanentOwnership("Cave Bat Holdable"), disableMethod =() => Ownership.blacklistedGuards.Clear(), toolTip = "Disables other players from grabbing the bat."},
                new Module { buttonText = "Permanent Firefly", method =() => Bugs.PermanentOwnership("Firefly"), disableMethod =() => Ownership.blacklistedGuards.Clear(), toolTip = "Disables other players from grabbing the firefly."},

                new Module { buttonText = $"Bug Spam {Bracket("green", "G")}", method = Bugs.BugSpam, disableMethod = Bugs.DisableBugSpam, toolTip = "Shoots the bug and firefly out of your hand repeatedly." },
                new Module { buttonText = $"Camera Spam {Bracket("green", "G")}", method = LckCamera.CameraSpam, disableMethod = LckCamera.DisableCameraSpam, toolTip = "Shoots the camera out of your hand repeatedly." },
                new Module { buttonText = $"Everything Spam {Bracket("green", "G")}", method = Fun.EverythingSpam, disableMethod = Fun.DisableEverythingSpam, toolTip = "Shoots everything out of your hand repeatedly." },

                new Module { buttonText = "Bug Phallus", method = Bugs.BugPhallus, toolTip = "Gives you a phallus in the form of the bugs." },
                new Module { buttonText = "Bug Phallus Gun", method = Bugs.BugPhallusGun, toolTip = "Gives whoever your hand desires a phallus in the form of the bugs." },

                new Module { buttonText = "Bug Vibrate Gun", method = Bugs.BugVibrateGun, toolTip = "Vibrates the controllers of whoever your hand desires using the bugs." },
                new Module { buttonText = "Bug Vibrate All", enableMethod = Bugs.EnableBugVibrateAll, method = Bugs.BugVibrateAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Vibrates the controllers of everyone in the room using the bugs." },

                new Module { buttonText = "Holster Bug", method =() => Bugs.HolsterObject("Floating Bug Holdable", TransferrableObject.PositionState.OnLeftArm), toolTip = "Holsters the bug on your left arm." },
                new Module { buttonText = "Holster Bat", method =() => Bugs.HolsterObject("Cave Bat Holdable", TransferrableObject.PositionState.OnChest), toolTip = "Holsters the bat on your chest." },
                new Module { buttonText = "Holster Firefly", method =() => Bugs.HolsterObject("Firefly", TransferrableObject.PositionState.OnRightArm), toolTip = "Holsters the firefly on your right arm." },

                new Module { buttonText = "Freeze Bug", method =() => Bugs.FreezeObject("Floating Bug Holdable"), toolTip = "Freezes the bug in place." },
                new Module { buttonText = "Freeze Bat", method =() => Bugs.FreezeObject("Cave Bat Holdable"), toolTip = "Freezes the bat in place." },
                new Module { buttonText = "Freeze Firefly", method =() => Bugs.FreezeObject("Firefly"), toolTip = "Freezes the firefly in place." },

                new Module { buttonText = "Fast Bug", method =() => Bugs.SetObjectSpeed("Floating Bug Holdable", 5f), disableMethod =() => Bugs.SetObjectSpeed("Floating Bug Holdable"), toolTip = "Speeds up the bug." },
                new Module { buttonText = "Fast Bat", method =() => Bugs.SetObjectSpeed("Cave Bat Holdable", 5f), disableMethod =() => Bugs.SetObjectSpeed("Cave Bat Holdable"), toolTip = "Speeds up the bat." },
                new Module { buttonText = "Fast Firefly", method =() => Bugs.SetObjectSpeed("Firefly", 5f), disableMethod =() => Bugs.SetObjectSpeed("Firefly"), toolTip = "Speeds up the firefly." },

                new Module { buttonText = "Slow Bug", method =() => Bugs.SetObjectSpeed("Floating Bug Holdable", 0.1f), disableMethod =() => Bugs.SetObjectSpeed("Floating Bug Holdable"), toolTip = "Slows down the bug." },
                new Module { buttonText = "Slow Bat", method =() => Bugs.SetObjectSpeed("Cave Bat Holdable", 0.1f), disableMethod =() => Bugs.SetObjectSpeed("Cave Bat Holdable"), toolTip = "Slows down the bat." },
                new Module { buttonText = "Slow Firefly", method =() => Bugs.SetObjectSpeed("Firefly", 0.1f), disableMethod =() => Bugs.SetObjectSpeed("Firefly"), toolTip = "Slows down the firefly." },

                new Module { buttonText = "Physical Bug", method =() => Bugs.PhysicalObject("Floating Bug Holdable"), toolTip = "Gives the bug physics, letting you grab onto it and throw it." },
                new Module { buttonText = "Physical Bat", method =() => Bugs.PhysicalObject("Cave Bat Holdable"), toolTip = "Gives the bat physics, letting you grab onto it and throw it." },
                new Module { buttonText = "Physical Firefly", method =() => Bugs.PhysicalObject("Firefly"), toolTip = "Gives the firefly physics, letting you grab onto it and throw it." },
                new Module { buttonText = "Physical Camera", method = LckCamera.PhysicalCamera, toolTip = "Gives the camera physics, letting you grab onto it and throw it." },

                new Module { buttonText = $"Grab Bug {Bracket("green", "G")}", method =() => Bugs.ObjectToHand("Floating Bug Holdable"), toolTip = "Forces the bug into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Bat {Bracket("green", "G")}", method =() => Bugs.ObjectToHand("Cave Bat Holdable"), toolTip = "Forces the bat into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Firefly {Bracket("green", "G")}", method =() => Bugs.ObjectToHand("Firefly"), toolTip = "Forces the firefly into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Camera {Bracket("green", "G")}", method = LckCamera.GrabCamera, toolTip = "Forces the camera into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Tablet {Bracket("green", "G")}", method = LckCamera.GrabTablet, toolTip = "Forces the tablet into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Balloons {Bracket("green", "G")}", method = Balloons.GrabBalloons, toolTip = "Forces every single balloon cosmetic into your hand when holding <color=green>grip</color>." },
                new Module { buttonText = $"Grab Gliders {Bracket("green", "G")}", method = Gliders.GrabGliders, toolTip = "Forces the bug into your hand when holding <color=green>grip</color>." },

                new Module { buttonText = "Bug Gun", method =() => Bugs.ObjectToPointGun("Floating Bug Holdable"), toolTip = "Moves the bug to wherever your hand desires." },
                new Module { buttonText = "Bat Gun", method =() => Bugs.ObjectToPointGun("Cave Bat Holdable"), toolTip = "Moves the bat to wherever your hand desires." },
                new Module { buttonText = "Firefly Gun", method =() => Bugs.ObjectToPointGun("Firefly"), toolTip = "Moves the firefly to wherever your hand desires." },
                new Module { buttonText = "Camera Gun", method = LckCamera.CameraGun, toolTip = "Moves the camera to wherever your hand desires." },
                new Module { buttonText = "Tablet Gun", method = LckCamera.TabletGun, toolTip = "Moves the tablet to wherever your hand desires." },
                new Module { buttonText = "Balloon Gun", method = Balloons.BalloonGun, toolTip = "Moves every single balloon cosmetic to wherever your hand desires." },
                new Module { buttonText = "Glider Gun", method = Gliders.GliderGun, toolTip = "Moves the gliders to wherever your hand desires." },
                new Module { buttonText = "Hoverboard Gun", method = Hoverboards.HoverboardGun, toolTip = "Spawns hoverboards at wherever your hand desires."},

                new Module { buttonText = "Spaz Bug", method =() => Bugs.SpazObject("Floating Bug Holdable"), toolTip = "Gives the bug a seizure." },
                new Module { buttonText = "Spaz Bat", method =() => Bugs.SpazObject("Cave Bat Holdable"), toolTip = "Gives the bat a seizure." },
                new Module { buttonText = "Spaz Firefly", method =() => Bugs.SpazObject("Firefly"), toolTip = "Gives the firefly a seizure." },
                new Module { buttonText = "Spaz Camera", method = LckCamera.SpazCamera, toolTip = "Gives the camera a seizure." },
                new Module { buttonText = "Spaz Tablet", method = LckCamera.SpazTablet, toolTip = "Gives the tablet a seizure." },
                new Module { buttonText = "Spaz Balloons", method = Balloons.SpazBalloons, toolTip = "Gives the balloons a seizure." },
                new Module { buttonText = "Spaz Gliders", method = Gliders.SpazGliders, toolTip = "Gives the gliders a seizure." },
                new Module { buttonText = "Spaz Hoverboard", method = Hoverboards.SpazHoverboard, toolTip = "Gives your hoverboard a seizure while holding it."},

                new Module { buttonText = "Orbit Bug", method =() => Bugs.OrbitObject("Floating Bug Holdable"), toolTip = "Orbits the bug around you." },
                new Module { buttonText = "Orbit Bat", method =() => Bugs.OrbitObject("Cave Bat Holdable", 240f), toolTip = "Orbits the bat around you." },
                new Module { buttonText = "Orbit Firefly", method =() => Bugs.OrbitObject("Firefly", 120f), toolTip = "Orbits the firefly around you." },
                new Module { buttonText = "Orbit Camera", method = LckCamera.OrbitCamera, toolTip = "Orbits the camera around you." },
                new Module { buttonText = "Orbit Tablet", method = LckCamera.OrbitTablet, toolTip = "Orbits the tablet around you." },
                new Module { buttonText = "Orbit Balloons", method = Balloons.OrbitBalloons, toolTip = "Orbits the balloons around you." },
                new Module { buttonText = "Orbit Gliders", method = Gliders.OrbitGliders, toolTip = "Orbits the gliders around you." },
                new Module { buttonText = "Orbit Hoverboards", method = Hoverboards.OrbitHoverboards, toolTip = "Orbits the hoverboards around you."},

                new Module { buttonText = "Bug Aura", method =() => Bugs.ObjectAura("Floating Bug Holdable"), toolTip = "Teleports the bug around you in random positions." },
                new Module { buttonText = "Bat Aura", method =() => Bugs.ObjectAura("Cave Bat Holdable"), toolTip = "Teleports the bat around you in random positions." },
                new Module { buttonText = "Firefly Aura", method =() => Bugs.ObjectAura("Firefly"), toolTip = "Teleports the firefly around you in random positions." },
                new Module { buttonText = "Camera Aura", method = LckCamera.CameraAura, toolTip = "Teleports the camera around you in random positions." },
                new Module { buttonText = "Tablet Aura", method = LckCamera.TabletAura, toolTip = "Teleports the tablet around you in random positions." },
                new Module { buttonText = "Balloon Aura", method = Balloons.BalloonAura, toolTip = "Teleports the balloons around you in random positions." },
                new Module { buttonText = "Glider Aura", method = Gliders.GliderAura, toolTip = "Teleports the camera around you in random positions." },
                new Module { buttonText = "Hoverboard Aura", method = Hoverboards.HoverboardAura, toolTip = "Teleports the hoverboards around you in random positions."},

                new Module { buttonText = "Ride Bug", method =() => Bugs.RideObject("Floating Bug Holdable"), toolTip = "Repeatedly teleports you on top of the bug." },
                new Module { buttonText = "Ride Bat", method =() => Bugs.RideObject("Cave Bat Holdable"), toolTip = "Repeatedly teleports you on top of the bat." },
                new Module { buttonText = "Ride Firefly", method =() => Bugs.RideObject("Firefly"), toolTip = "Repeatedly teleports you on top of the firefly." },

                new Module { buttonText = "Become Bug", method =() => Bugs.BecomeObject("Floating Bug Holdable"), disableMethod = Movement.EnableRig, toolTip = "Turns you into the bug." },
                new Module { buttonText = "Become Bat", method =() => Bugs.BecomeObject("Cave Bat Holdable"), disableMethod = Movement.EnableRig, toolTip = "Turns you into the bat." },
                new Module { buttonText = "Become Firefly", method =() => Bugs.BecomeObject("Firefly"), disableMethod = Movement.EnableRig, toolTip = "Turns you into the firefly." },
                new Module { buttonText = "Become Camera", method = LckCamera.BecomeCamera, disableMethod = Movement.EnableRig, toolTip = "Turns you into the camera." },
                new Module { buttonText = "Become Tablet", method = LckCamera.BecomeTablet, disableMethod = Movement.EnableRig, toolTip = "Turns you into the tablet." },
                new Module { buttonText = "Become Balloon", method = Balloons.BecomeBalloon, disableMethod = Movement.EnableRig, toolTip = "Turns you into a balloon when holding <color=green>trigger</color>." },
                new Module { buttonText = "Become Hoverboard", method = Hoverboards.BecomeHoverboard, disableMethod = Movement.EnableRig, toolTip = "Turns you into a hoverboard when holding <color=green>trigger</color>." },

                new Module { buttonText = "Destroy Bug", method =() => Bugs.DestroyObject("Floating Bug Holdable"), toolTip = "Sends the bug to hell." },
                new Module { buttonText = "Destroy Firefly", method =() => Bugs.DestroyObject("Firefly"), toolTip = "Sends the bug to hell." },
                new Module { buttonText = "Destroy Bat", method =() => Bugs.DestroyObject("Cave Bat Holdable"), toolTip = "Sends the bat to hell." },
                new Module { buttonText = "Destroy Camera", method = LckCamera.DestroyCamera, toolTip = "Sends the camera to hell." },
                new Module { buttonText = "Destroy Tablet", method = LckCamera.DestroyTablet, toolTip = "Sends the tablet to hell." },
                new Module { buttonText = "Destroy Balloons", method = Balloons.DestroyBalloons, isTogglable = false, toolTip = "Sends every single balloon cosmetic to hell." },
                new Module { buttonText = "Destroy Gliders", method = Gliders.DestroyGliders, isTogglable = false, toolTip = "Sends every single glider to hell." },

                new Module { buttonText = "Respawn Gliders", method = Gliders.RespawnGliders, isTogglable = false, toolTip = "Respawns all the gliders." },
                new Module { buttonText = "Pop All Balloons", method = Balloons.PopAllBalloons, isTogglable = false, toolTip = "Pops every single balloon cosmetic." },

                new Module { buttonText = "Golden Name Tag", method =() => Fun.GoldenNameTag(true), disableMethod =() => Fun.GoldenNameTag(false), toolTip = "Changes your name tag to a golden color. This mod only works if you are subscribed to the fan club." },
                new Module { buttonText = "Flash Name Tag", method = Fun.FlashNameTag, toolTip = "Flashes your name tag to between golden and white. This mod only works if you are subscribed to the fan club." },

                new Module { buttonText = "Disable Mining Cooldown", enableMethod = ()=> Fun.DisableMiningCooldown(false), disableMethod =() => Fun.DisableMiningCooldown(true), toolTip = "Remove the cooldown on your pickaxe for mining."},
                new Module { buttonText = "Instant Mining", enableMethod = ()=> Fun.StationaryMining(true), disableMethod =() => Fun.StationaryMining(false), toolTip = "Be able to mine by swinging your pickaxe at any speed."},

                new Module { buttonText = "Set Name to \"STATUE\"", method =() => ChangeName("STATUE"), isTogglable = false, toolTip = "Sets your name to \"STATUE\"." },
                new Module { buttonText = "Set Name to \"HIDE\"", method =() => ChangeName("HIDE"), isTogglable = false, toolTip = "Sets your name to \"HIDE\"." },
                new Module { buttonText = "Set Name to \"RUN\"", method =() => ChangeName("RUN"), isTogglable = false, toolTip = "Sets your name to \"RUN\"." },
                new Module { buttonText = "Set Name to \"BEHINDYOU\"", method =() => ChangeName("BEHINDYOU"), isTogglable = false, toolTip = "Sets your name to \"BEHINDYOU\"." },
                new Module { buttonText = "Set Name to \"iiOnTop\"", method =() => ChangeName("iiOnTop"), isTogglable = false, toolTip = "Sets your name to \"iiOnTop\"." },

                new Module { buttonText = "PBBV Name Cycle", method =() => Fun.NameCycle(new[] { "PBBV", "IS", "HERE" }), toolTip = "Sets your name on a loop to \"PBBV\", \"IS\", and \"HERE\"." },
                new Module { buttonText = "J3VU Name Cycle", method =() => Fun.NameCycle(new[] { "J3VU", "HAS", "BECOME", "HOSTILE" }), toolTip = "Sets your name on a loop to \"J3VU\", \"HAS\", \"BECOME\", and \"HOSTILE\"" },
                new Module { buttonText = "H1D3 Name Cycle", method =() => Fun.NameCycle(new[] { "H1D3", "HE", "HAS", "AWOKEN" }), toolTip = "Sets your name on a loop to \"H1D3\", \"HE\", \"HAS\", and \"AWOKEN\"" },
                new Module { buttonText = "No Escape Name Cycle", method =() => Fun.NameCycle(new[] { "THERE", "IS", "NO", "ESCAPE" }), toolTip = "Sets your name on a loop to \"THERE\", \"IS\", \"NO\", and \"ESCAPE\"" },
                new Module { buttonText = "Run Rabbit Name Cycle", method =() => Fun.NameCycle(new[] { "RUN", "RABBIT" }), toolTip = "Sets your name on a loop to \"RUN\" and \"RABBIT\"." },
                new Module { buttonText = "Random Name Cycle", method = Fun.RandomNameCycle, toolTip = "Sets your name on a loop to a bunch of random characters." },
                new Module { buttonText = "Custom Name Cycle", enableMethod = Fun.EnableCustomNameCycle, method =() => Fun.NameCycle(Fun.names), toolTip = "Sets your name on a loop to whatever's in the file." },
                new Module { buttonText = "Animated Name", method = Fun.AnimatedName, disableMethod =() => { ChangeName(Fun.name); Fun.name = null;  }, toolTip = "Animates your current username." },

                new Module { buttonText = "Flash Color", method = Fun.FlashColor, toolTip = "Makes your character flash." },
                new Module { buttonText = "Strobe Color", method = Fun.StrobeColor, toolTip = "Makes your character random colors." },
                new Module { buttonText = "Rainbow Color", method = Fun.RainbowColor, toolTip = "Makes your character rainbow." },
                new Module { buttonText = "Hard Rainbow Color", method = Fun.HardRainbowColor, toolTip = "Makes your character flash from red, green, blue, and magenta." },

                new Module { buttonText = "Become \"NOESCAPE\"", method =() => Fun.BecomePlayer("NOESCAPE", Color.black), isTogglable = false, toolTip = "Sets your name to \"NOESCAPE\" and color to black." },
                new Module { buttonText = "Become \"H1D3\"", method =() => Fun.BecomePlayer("H1D3", Color.black), isTogglable = false, toolTip = "Sets your name to \"H1D3\" and color to black." },
                new Module { buttonText = "Become \"PBBV\"", method =() => Fun.BecomePlayer("PBBV", new Color32(230, 127, 102, 255)), isTogglable = false, toolTip = "Sets your name to \"PBBV\" and color to salmon." },
                new Module { buttonText = "Become \"J3VU\"", method =() => Fun.BecomePlayer("J3VU", Color.green), isTogglable = false, toolTip = "Sets your name to \"J3VU\" and color to green." },
                new Module { buttonText = "Become \"ECHO\"", method =() => Fun.BecomePlayer("ECHO", new Color32(0, 150, 255, 255)), isTogglable = false, toolTip = "Sets your name to \"ECHO\" and color to sky blue." },
                new Module { buttonText = "Become \"DAISY09\"", method =() => Fun.BecomePlayer("DAISY09", new Color32(255, 81, 231, 255)), isTogglable = false, toolTip = "Sets your name to \"DAISY09\" and color to a light pink." },
                new Module { buttonText = "Become \"STATUE\"", method =() => Fun.BecomePlayer("STATUE", Color.black), isTogglable = false, toolTip = "Sets your name to \"STATUE\" and color to black." },
                new Module { buttonText = "Become Child", method = Fun.BecomeMinigamesKid, isTogglable = false, toolTip = "Sets your name and color to something a child would pick." },

                new Module { buttonText = "Become Hidden on Leaderboard", method =() => Fun.BecomePlayer("I", new Color32(0, 53, 2, 255)), isTogglable = false, toolTip = "Sets your name to \"I\" and your color to a dark green, matching the leaderboard." },
                new Module { buttonText = "Copy Identity Gun", method = Fun.CopyIdentityGun, toolTip = "Steals the identity of whoever your hand desires." },
                new Module { buttonText = "Copy Cosmetics Gun", method = Fun.CopyCosmeticsGun, toolTip = "Steals the cosmetics of whoever your hand desires." },

                new Module { buttonText = "Change Accessories", overlapText = "Change Cosmetics", method = Fun.ChangeAccessories, toolTip = "Use your grips to change what hat you're wearing." },
                new Module { buttonText = "Spaz Accessories", overlapText = $"Spaz Cosmetics {Bracket("green", "All")}", method = Cosmetics.SpazAccessories, toolTip = "Spazzes your hats out for everyone when holding <color=green>trigger</color>." },
                new Module { buttonText = $"Spaz Cosmetics {Bracket("green", "Others")}", method = Cosmetics.SpazAccessoriesOthers, toolTip = "Spazzes your hats out for everyone except you when holding <color=green>trigger</color>." },

                new Module { buttonText = $"Spaz Balloon Cosmetics {Bracket("green", "T")}", method = Cosmetics.SpazAccessoriesBalloon, toolTip = "Spazzes your balloons out for everyone when holding <color=green>trigger</color>." },

                new Module { buttonText = "Cosmetic Spoof", enableMethod = Cosmetics.TryOnAnywhere, disableMethod = Cosmetics.TryOffAnywhere, toolTip = "Lets you try on cosmetics from anywhere. Enable this mod after wearing the cosmetics." },
                new Module { buttonText = "Cosmetic Browser", method = Cosmetics.CosmeticBrowser, isTogglable = false, toolTip = "Browse through every cosmetic that you can try on and add it to your cart." },
                new Module { buttonText = "Auto Spoof Cosmetics", enableMethod = Fun.AutoLoadCosmetics, disableMethod = Fun.NoAutoLoadCosmetics, toolTip = "Automatically spoofs your cosmetics, making you appear with anything you're able to try-on." },

                new Module { buttonText = "Auto Purchase Cosmetics", overlapText = "Auto Purchase Free Cosmetics", method = Cosmetics.AutoPurchaseCosmetics, toolTip = "Automatically purchases any free cosmetics." },
                new Module { buttonText = "Auto Purchase Current Cosmetics", method = Cosmetics.AutoPurchasePaidCosmetics, toolTip = "Automatically purchases all cosmetics on your outfit until you own everything. This does use shiny rocks." },
                new Module { buttonText = "Disable Cosmetics on Tag", method = Cosmetics.DisableCosmeticsOnTag, toolTip = "Disables your cosmetics when you get tagged, good for ambush." },

                new Module { buttonText = "Unlock Fan Club Subscription", aliases = new[] { "Unlock VIM", "Unlock Very Cool Monke", "Free VIM" }, enableMethod =() => SubscriptionStatus.enabled = true, disableMethod =() => SubscriptionStatus.enabled = false, toolTip = "Unlocks the Gorilla Tag fan club subscription." },
                new Module { buttonText = "Unlock All Cosmetics", method = Cosmetics.UnlockAllCosmetics, toolTip = "Unlocks every cosmetic in the game. This mod is client-sided." },
                new Module { buttonText = "Unlimited Shiny Rocks", enableMethod =() => Purchasing.enabled = true, method =() => ManagerRegistry.CosmeticsController.currencyBalance = int.MaxValue, disableMethod =() => Purchasing.enabled = false, toolTip = "Gives you 2 billion shiny rocks. This mod is client sided." },

                new Module { buttonText = "Sticky Holdables", method = Fun.StickyHoldables, toolTip = "Makes your holdables sticky.", legal = true},
                new Module { buttonText = "Spaz Holdables", method = Fun.SpazHoldables, toolTip = "Spazzes out the positions of your holdables." },

                new Module { buttonText = "Get ID Self", method = Fun.CopySelfID, isTogglable = false, toolTip = "Gets your player ID and copies it to the clipboard.", legal = true},
                new Module { buttonText = "Get ID Gun", method = Fun.CopyIDGun, toolTip = "Gets the player ID of whoever your hand desires and copies it to the clipboard." },
                new Module { buttonText = "Get ID All", method = Fun.CopyIDAll, isTogglable = false, toolTip = "Gets the player IDs of everyone and copies them to the clipboard." },
                new Module { buttonText = "Get ID Aura", method = Fun.CopyIDAura, toolTip = "Gets the player ID of players nearby you and copies it to the clipboard." },
                new Module { buttonText = "Get ID On Touch", method = Fun.CopyIDOnTouch, toolTip = "Gets the player ID of players you touch and copies it to the clipboard." },

                new Module { buttonText = "Narrate ID Self", method = Fun.NarrateSelfID, isTogglable = false, toolTip = "Gets your player ID and speaks it through your microphone."},
                new Module { buttonText = "Narrate ID Gun", method = Fun.NarrateIDGun, toolTip = "Gets the player ID of whoever your hand desires and speaks it through your microphone." },
                new Module { buttonText = "Narrate ID All", method = Fun.NarrateIDAll, isTogglable = false, toolTip = "Gets the player IDs of everyone and speaks them through your microphone." },
                new Module { buttonText = "Narrate ID Aura", method = Fun.NarrateIDAura, toolTip = "Gets the player ID of players nearby you and speaks it through your microphone." },
                new Module { buttonText = "Narrate ID On Touch", method = Fun.NarrateIDOnTouch, toolTip = "Gets the player ID of players you touch and speaks it through your microphone." },

                new Module { buttonText = "Narrate Fake IP Self", method = Fun.NarrateFakeDoxxSelf, isTogglable = false, toolTip = "Gets random numbers that look like an IP address and speaks it through your microphone."},
                new Module { buttonText = "Narrate Fake IP Gun", method = Fun.NarrateFakeDoxxGun, toolTip = "Gets random numbers that look like an IP address and speaks it through your microphone towards whoever your hand desires." },
                new Module { buttonText = "Narrate Fake IP All", method = Fun.NarrateFakeDoxxAll, isTogglable = false, toolTip = "Gets random numbers that look like an IP address for everyone and speaks it through your microphone." },
                new Module { buttonText = "Narrate Fake IP Aura", method = Fun.NarrateFakeDoxxAura, toolTip = "Gets random numbers that look like an IP address and speaks it through your microphone." },
                new Module { buttonText = "Narrate Fake IP On Touch", method = Fun.NarrateFakeDoxxOnTouch, toolTip = "Gets random numbers that look like an IP address and speaks it through your microphone." },

                new Module { buttonText = "Get Creation Date Self", method = Fun.CopyCreationDateSelf, isTogglable = false, toolTip = "Gets the creation date of your account and copies it to the clipboard."},
                new Module { buttonText = "Get Creation Date Gun", method = Fun.CopyCreationDateGun, toolTip = "Gets the creation date of whoever your hand desires' account and copies it to the clipboard." },
                new Module { buttonText = "Get Creation Date All", method = Fun.CopyCreationDateAll, isTogglable = false, toolTip = "Gets the creation date of everyones account and copies it to the clipboard." },
                new Module { buttonText = "Get Creation Date Aura", method = Fun.CopyCreationDateAura, toolTip = "Gets the creation date of nearby players accounts and copies it to the clipboard." },
                new Module { buttonText = "Get Creation Date On Touch", method = Fun.CopyCreationDateOnTouch, toolTip = "Gets the creation date of players you touch accounts and copies it to the clipboard." },

                new Module { buttonText = "Narrate Creation Date Self", method = Fun.NarrateCreationDateSelf, isTogglable = false, toolTip = "Gets the creation date of your account and speaks it through your microphone." },
                new Module { buttonText = "Narrate Creation Date Gun", method = Fun.NarrateCreationDateGun, toolTip = "Gets the creation date of whoever your hand desires' account and speaks it through your microphone." },
                new Module { buttonText = "Narrate Creation Date All", method = Fun.NarrateCreationDateAll, isTogglable = false, toolTip = "Gets the creation date of everyones account and speaks it through your microphone." },
                new Module { buttonText = "Narrate Creation Date Aura", method = Fun.NarrateCreationDateAura, toolTip = "Gets the creation date of nearby players accounts and speaks it through your microphone." },
                new Module { buttonText = "Narrate Creation Date On Touch", method = Fun.NarrateCreationDateOnTouch, toolTip = "Gets the creation date of players you touch accounts and speaks it through your microphone." },

                new Module { buttonText = "Grab Player Info", method = Fun.GrabPlayerInfo, isTogglable = false, toolTip = "Saves every player's name, color, and player ID as a text file and opens it." },
            },

            ["Rebind Settings"] = new[]
            {
                new Module { buttonText = "Exit Rebind Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Rebind A", enableMethod =() => Modules.Categories.Settings.StartRebind("A"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind B", enableMethod =() => Modules.Categories.Settings.StartRebind("B"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind X", enableMethod =() => Modules.Categories.Settings.StartRebind("X"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Y", enableMethod =() => Modules.Categories.Settings.StartRebind("Y"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Left Grip", enableMethod =() => Modules.Categories.Settings.StartRebind("LG"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Right Grip", enableMethod =() => Modules.Categories.Settings.StartRebind("RG"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Left Trigger", enableMethod =() => Modules.Categories.Settings.StartRebind("LT"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Right Trigger", enableMethod =() => Modules.Categories.Settings.StartRebind("RT"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Left Joystick", enableMethod =() => Modules.Categories.Settings.StartRebind("LJ"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},
                new Module { buttonText = "Rebind Right Joystick", enableMethod =() => Modules.Categories.Settings.StartRebind("RJ"), disableMethod =() => IsRebinding = false, toolTip = "Enables rebinding mode, letting you change a mod's button.", legal = true},

                new Module { buttonText = "Clear Rebinds", method = Modules.Categories.Settings.RemoveRebinds, isTogglable = false, toolTip = "Removes all rebinds.", legal = true},
            },

            ["Sound Mods"] = new[]
            {
                new Module { buttonText = "Exit Sound Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Soundboard", method =() => Sound.LoadSoundboard(), isTogglable = false, toolTip = "A working, customizable soundboard that lets you play audios through your microphone."},

                new Module { buttonText = $"Bass Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(68), toolTip = "Plays the loud drum sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Metal Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(18), toolTip = "Plays the metal sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Wolf Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(195), toolTip = "Plays the wolf howl when holding <color=green>grip</color>." },
                new Module { buttonText = $"Cat Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(236), toolTip = "Plays the cat meow when holding <color=green>grip</color>." },
                new Module { buttonText = $"Turkey Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(83), toolTip = "Plays the turkey sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Frog Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(91), toolTip = "Plays the frog creak when holding <color=green>grip</color>." },
                new Module { buttonText = $"Bee Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(191), toolTip = "Plays the bee buzz when holding <color=green>grip</color>." },
                new Module { buttonText = $"Squeak Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(215), toolTip = "Plays the squeak sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Okay Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(336), toolTip = "Plays the sound of jmancurly saying \"okay\" with autotune when holding <color=green>grip</color>." },
                new Module { buttonText = $"Scream Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(337), toolTip = "Plays the sound of jmancurly screaming when holding <color=green>grip</color>." },
                new Module { buttonText = $"Slap Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(338), toolTip = "Plays the slap sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Jmancurly Sound Spam {Bracket("green", "G")}", method = Sound.JmancurlySoundSpam, toolTip = "Plays the sounds from the jmancurly statue when holding <color=green>grip</color>." },
                new Module { buttonText = $"Random Sound Spam {Bracket("green", "G")}", method = Sound.RandomSoundSpam, toolTip = "Plays random sounds when holding <color=green>grip</color>." },
                new Module { buttonText = $"Earrape Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(215), toolTip = "Plays a high-pitched sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Ding Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(244), toolTip = "Plays a ding sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Crystal Sound Spam {Bracket("green", "G")}", method = Sound.CrystalSoundSpam, toolTip = "Plays some crystal noises when holding <color=green>grip</color>." },
                new Module { buttonText = $"Piano Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(Random.Range(295, 307)), toolTip = "Plays some terrible piano when holding <color=green>grip</color>." },
                new Module { buttonText = $"Big Crystal Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(213), toolTip = "Plays a long crystal sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Pan Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(248), toolTip = "Plays a pan sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"AK-47 Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(203), toolTip = "Plays a sound that sounds like an AK-47 when holding <color=green>grip</color>." },
                new Module { buttonText = $"Siren Sound Spam {Bracket("green", "G")}", method = Sound.SirenSoundSpam, toolTip = "Plays a siren sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Tick Sound Spam {Bracket("green", "G")}", method =() => Sound.SoundSpam(148), toolTip = "Plays a tick sound when holding <color=green>grip</color>." },

                ButtonHelper.Create("Custom Sound ID",
                    () => Enumerable.Range(0, References.Player?.materialData?.Count ?? 0).Select(i => i.ToString()).ToArray(),
                    Sound.soundId,
                    v => Sound.soundId = v,
                    toolTip: "Changes the Sound ID of the Custom Sound Spam."
                ),
                new Module { buttonText = "Custom Sound Spam", overlapText = $"Custom Sound Spam {Bracket("green", "0")}", method = Sound.CustomSoundSpam, toolTip = "Plays the selected sound when holding <color=green>grip</color>." },

                new Module { buttonText = "Override Tap Sound", method =() => { HandEffects.enabled = true; HandEffects.material = Sound.soundId; }, disableMethod =() => {  HandEffects.enabled = false;  HandEffects.material = -1; }, toolTip = "Plays the selected sound when holding <color=green>grip</color>." },
            },

            ["Projectile Mods"] = new[]
            {
                new Module { buttonText = "Exit Projectile Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = $"Grab Projectile {Bracket("green", "G")}", method = Projectiles.GrabProjectile, disableMethod = () => Projectiles.previousGripHeld.Clear(), toolTip = "Grabs your selected projectile holding <color=green>grip</color>." },
                new Module { buttonText = $"Projectile Spam {Bracket("green", "G")}", method = Projectiles.ProjectileSpam, toolTip = "Spams your selected projectile when holding <color=green>grip</color>." },
                new Module { buttonText = "Projectile Gun", method = Projectiles.ProjectileGun, toolTip = "Spams your selected projectile at wherever your hand desires." },
                new Module { buttonText = $"Laser Spam {Bracket("green", "G")}", method = Projectiles.LazerSpam, toolTip = "Spams your selected projectile out of your eyes like lasers when holding <color=green>grip</color>." },
                new Module { buttonText = "Give Projectile Spam Gun", method = Projectiles.GiveProjectileSpamGun, toolTip = "Acts like the projectile spam, but you can give it to whoever your hand desires when they hold <color=green>grip</color>." },
                new Module { buttonText = $"Impact Spam {Bracket("green", "G")}", method = Projectiles.ImpactSpam, toolTip = "Orbits impact effects around you" },
                new Module { buttonText = $"Impact Orbit {Bracket("green", "G")}", method = Projectiles.ImpactSpam, toolTip = "Acts like the projectile spam, but uses the impacts instead." },
                new Module { buttonText = $"Laser Eyes {Bracket("green", "G")}", method = Projectiles.LazerEyes, toolTip = "Makes you shoot lasers out of your eyes when holding <color=green>grip</color>." },
                new Module { buttonText = $"Urine {Bracket("green", "G")}", aliases = new[] { "Pee", "Piss" }, method = Projectiles.Urine, toolTip = "Makes you pee when holding <color=green>grip</color>." },
                new Module { buttonText = $"Feces {Bracket("green", "G")}", aliases = new[] { "Poop", "Shit" }, method = Projectiles.Feces, toolTip = "Makes you poo when holding <color=green>grip</color>." },
                new Module { buttonText = $"Period {Bracket("green", "G")}", method = Projectiles.Period, toolTip = "Makes you have your period when holding <color=green>grip</color>." },
                new Module { buttonText = $"Semen {Bracket("green", "G")}", aliases = new[] { "Cum", "Ejaculate" }, method = Projectiles.Semen, toolTip = "Makes you ejaculate when holding <color=green>grip</color>." },
                new Module { buttonText = $"Vomit {Bracket("green", "G")}", method = Projectiles.Vomit, toolTip = "Makes you throw up when holding <color=green>grip</color>." },
                new Module { buttonText = $"Spit {Bracket("green", "G")}", method = Projectiles.Spit, toolTip = "Makes you spit when holding <color=green>grip</color>." },

                new Module { buttonText = "Laser Eyes Gun", method = Projectiles.LazerEyesGun, toolTip = "Makes whoever your hand desires shoot lasers out of their eyes." },
                new Module { buttonText = "Urine Gun", aliases = new[] { "Pee Gun", "Piss Gun" }, method = Projectiles.UrineGun, toolTip = "Makes whoever your hand desires pee." },
                new Module { buttonText = "Feces Gun", aliases = new[] { "Poop Gun", "Shit Gun" }, method = Projectiles.FecesGun, toolTip = "Makes whoever your hand desires poo." },
                new Module { buttonText = "Period Gun", method = Projectiles.PeriodGun, toolTip = "Makes whoever your hand desires have their period." },
                new Module { buttonText = "Semen Gun", aliases = new[] { "Cum Gun", "Ejaculate Gun" }, method = Projectiles.SemenGun, toolTip = "Makes whoever your hand desires ejaculate." },
                new Module { buttonText = "Vomit Gun", method = Projectiles.VomitGun, toolTip = "Makes whoever your hand desires throw up." },
                new Module { buttonText = "Spit Gun", method = Projectiles.SpitGun, toolTip = "Makes whoever your hand desires spit." },

                new Module { buttonText = "Projectile Blind Gun", method = Projectiles.ProjectileBlindGun, toolTip = "Blinds whoever your hand desires using the egg projectiles."},
                new Module { buttonText = "Projectile Blind All", enableMethod = Projectiles.ProjectileBlindAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Blinds everybody in the room using the egg projectiles."},

                new Module { buttonText = "Projectile Lag Gun", method = Projectiles.ProjectileLagGun, toolTip = "Lags whoever your hand desires using the firework projectiles."},
                new Module { buttonText = "Projectile Lag All", enableMethod = Projectiles.ProjectileLagAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Lags everybody in the room using the firework projectiles."},

                new Module { buttonText = "Projectile Nuke Gun", method = Projectiles.ProjectileNukeGun, toolTip = "Spawns a lot of projectiles airstriking from the sky at wherever your hand desires."},

                new Module { buttonText = $"Projectile Rain {Bracket("green", "T")}", method = Projectiles.ProjectileRain, toolTip = "Rains projectiles around you when holding <color=green>trigger</color>."},
                new Module { buttonText = $"Projectile Hail {Bracket("green", "T")}", method = Projectiles.ProjectileHail, toolTip = "Hails projectiles around you when holding <color=green>trigger</color>."},
                new Module { buttonText = $"Projectile Fountain {Bracket("green", "T")}", method = Projectiles.ProjectileFountain, toolTip = "Fountains projectiles above you when holding <color=green>trigger</color>."},
                new Module { buttonText = $"Projectile Positional Fountain {Bracket("green", "G")}", method = Projectiles.ProjectilePositionalFountain, disableMethod = Projectiles.DisableProjectilePositionalFountain, toolTip = "Place a fountain <color=green>grip</color> and use it with <color=green>trigger</color>. It will fountain projectiles at wherever the fountain is."},

                new Module { buttonText = $"Projectile Orbit {Bracket("green", "T")}", method = Projectiles.ProjectileOrbit, toolTip = "Orbits projectiles around you when holding <color=green>trigger</color>."},
                new Module { buttonText = $"Projectile Aura {Bracket("green", "T")}", method = Projectiles.ProjectileAura, toolTip = "Randomly spawns projectiles around you when holding <color=green>trigger</color>."},

                new Module { buttonText = "Snowball Punch Mod", method = Projectiles.SnowballPunchMod, toolTip = "Flings people when you punch them."},
                new Module { buttonText = "Snowball Boxing", method = Projectiles.SnowballBoxing, toolTip = "Gives everyone the punch mod by using snowballs."},
                new Module { buttonText = $"Snowball Dash {Bracket("green", "A")}", method = Projectiles.SnowballDash, toolTip = "Allows other players to dash themself into the air with the snowballs."},
                new Module { buttonText = "Snowball High Jump", method = Projectiles.SnowballHighJump, toolTip = "Allow everyone to jump higher using snowballs."},
                new Module { buttonText = "Snowball Particle Gun", method = Projectiles.SnowballParticleGun, toolTip = "Spawns snowball particles wherever your hand desires."},
                new Module { buttonText = "Snowball Impact Effect Gun", method = Projectiles.SnowballImpactEffectGun, toolTip = "Spawns snowball impact events on whoever your hand desires."},

                new Module { buttonText = $"Snowball Fling Zone {Bracket("green", "G")}", method = Projectiles.SnowballFlingZone, disableMethod = Projectiles.DisableSnowballFlingZone, toolTip = "Spawn and move fling zones with your <color=green>right grip</color>. Press <color=green>trigger</color> to remove fling zones."},

                new Module { buttonText = "Snowball Fling Gun", method = Projectiles.SnowballFlingGun, toolTip = "Flings whoever your hand desires."},
                new Module { buttonText = $"Snowball Fling All {Bracket("green", "T")}", method = Projectiles.SnowballFlingAll, toolTip = "Flings everybody when holding <color=green>trigger</color>."},
                new Module { buttonText = "Snowball Safety Bubble", aliases = new[] { "Snowball Fling Aura" }, method = Projectiles.SnowballSafetyBubble, toolTip = "Anyone who gets too close to you will be launched away."},

                new Module { buttonText = "Snowball Protect Gun", method = Projectiles.SnowballProtectGun, toolTip = "Protects whoever your hand desires by flinging anyone who gets too close. "},
                new Module { buttonText = "Snowball Protect All", method = Projectiles.SnowballProtectAll, toolTip = "Protects everyone by flinging anyone who gets too close. "},

                new Module { buttonText = "Snowball Fling Vertical Gun", method = Projectiles.SnowballFlingVerticalGun, toolTip = "Flings whoever your hand desires vertically."},
                new Module { buttonText = $"Snowball Fling Vertical All {Bracket("green", "T")}", method = Projectiles.SnowballFlingVerticalAll, toolTip = "Flings everybody vertically when holding <color=green>trigger</color>."},
                new Module { buttonText = "Snowball Bring Gun", method = Projectiles.SnowballBringGun, toolTip = "Flings everybody towards wherever your hand desires."},
                new Module { buttonText = "Snowball Push Gun", method = Projectiles.SnowballPushGun, aliases = new[] { "Snowball Fling Away Gun" }, toolTip = "Flings everybody away from wherever your hand desires."},

                new Module { buttonText = "Snowball Bring Player Gun", method = Projectiles.SnowballBringPlayerGun, toolTip = "Flings whoever your hand desires towards you."},
                new Module { buttonText = "Snowball Bring All Players", method = Projectiles.SnowballBringAllPlayers, toolTip = "Flings everyone towards you."},

                new Module { buttonText = "Snowball Push Player Gun", method = Projectiles.SnowballPushPlayerGun, toolTip = "Flings whoever your hand desires away from you."},
                new Module { buttonText = "Snowball Push All Players", method = Projectiles.SnowballPushAllPlayers, toolTip = "Flings everyone away from you."},



                new Module { buttonText = "Snowball Buttocks", method = Projectiles.SnowballButtocks, disableMethod = Projectiles.DisableSnowballGenitals, toolTip = "Gives you fake buttocks using the snowballs." },
                new Module { buttonText = "Snowball Breasts", method = Projectiles.SnowballBreasts, disableMethod = Projectiles.DisableSnowballGenitals, toolTip = "Gives you fake breasts using the snowballs." },

                new Module { buttonText = $"Anti Report {Bracket("green", "Snowball Fling")}", method = Projectiles.AntiReportSnowballFling, toolTip = "Flings whoever tries to report you with the snowballs."}
            },

            ["Master Mods"] = new[]
            {
                new Module { buttonText = "Exit Master Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "MasterLabel", overlapText = "You are not master client.", label = true},

                new Module { buttonText = "VIM Kick Gun", method = Master.VIMKickGun, toolTip = "Kicks whoever your hand desires if you created the room as a fan club subscriber." },
                new Module { buttonText = "VIM Kick All", method = Master.VIMKickAll, isTogglable = false, toolTip = "Kicks everyone if you created the room as a fan club subscriber." },

                new Module { buttonText = "VIM Block Gun", method = Master.VIMBlockGun, toolTip = "Blocks whoever your hand desires if you created the room as a fan club subscriber." },
                new Module { buttonText = "VIM Block All", method = Master.VIMBlockAll, isTogglable = false, toolTip = "Blocks everyone if you created the room as a fan club subscriber." },

                new Module { buttonText = "VIM Mute Gun", method = Master.VIMMuteGun, toolTip = "Mutes whoever your hand desires if you created the room as a fan club subscriber." },
                new Module { buttonText = "VIM Mute All", enableMethod = Master.VIMMuteAll, disableMethod = Master.VIMUnmuteAll, toolTip = "Mutes everyone if you created the room as a fan club subscriber." },

                new Module { buttonText = "Guardian Self", method = Master.GuardianSelf, isTogglable = false, toolTip = "Makes you red."},
                new Module { buttonText = "Guardian Gun", method = Master.GuardianGun, toolTip = "Makes whoever your hand desires the guardian."},
                new Module { buttonText = "Guardian All", method = Master.GuardianAll, isTogglable = false, toolTip = "Makes everyone in the room the guardian."},

                new Module { buttonText = "Unguardian Self", method = Master.UnguardianSelf, isTogglable = false, toolTip = "Removes you from the guardian position."},
                new Module { buttonText = "Unguardian Gun", method = Master.UnguardianGun, toolTip = "Removes whoever your hand desires from the guardian position."},
                new Module { buttonText = "Unguardian All", method = Master.UnguardianAll, isTogglable = false, toolTip = "Removes everyone in the room from the guardian position."},

                new Module { buttonText = "Guardian Spaz", method = Master.GuardianSpaz, toolTip = "Spams the guardian position for everyone in the room."},

                
                
                

                
                
                

                
                
                

                
                
                

                new Module { buttonText = "Material Self", method =() => { if (!(Time.time > Master.materialDelay)) return; Master.MaterialTarget(References.LocalRig); Master.materialDelay = Time.time + 0.1f; }, toolTip = "Flashes the materials of yourself."},
                new Module { buttonText = "Material Gun", method = Master.MaterialGun, toolTip = "Flashes the materials of whoever your hand desires."},
                new Module { buttonText = "Material All", method = Master.MaterialAll, toolTip = "Flashes the materials of everyone in the room."},

                new Module { buttonText = "Grey Screen Gun", method = ()=> Master.ActivateGreyZoneGun(true), disableMethod =() => ViewSerialization.OverrideSerialization = null,  toolTip = "Makes whoever your hand desires' screen grey." },
                new Module { buttonText = "Fix Screen Gun", aliases = new[] { "Ungrey Screen Gun" }, method = ()=> Master.ActivateGreyZoneGun(false), toolTip = "Makes whoever your hand desires' screen normal again." },
                new Module { buttonText = "Grey Screen All", enableMethod = ()=> Master.ActivateGreyZone(true), disableMethod =() => Master.ActivateGreyZone(false), toolTip = "Makes everyone's screen grey." },

                new Module { buttonText = "Spaz Grey Screen Gun", method = Master.SpazGreyZoneGun, disableMethod = () => ViewSerialization.OverrideSerialization = null, toolTip = "Makes whoever your hand desires' screen flash grey." },
                new Module { buttonText = "Spaz Grey Screen All", method = Master.SpazGreyZone, disableMethod =() => Master.ActivateGreyZone(false), toolTip = "Makes everyone's screen flash grey." },

                new Module { buttonText = "Spaz Prop Hunt", method = Master.SpazPropHunt, toolTip = "Repeatedly starts and ends the prop hunt gamemode."},
                new Module { buttonText = "Spaz Prop Hunt Objects", method = Master.SpazPropHuntObjects, toolTip = "Repeatedly randomizes everyone's selected object in the prop hunt gamemode."},

                new Module { buttonText = "Max Currency Self", method =() => Master.SetCurrencySelf(int.MaxValue), isTogglable = false, toolTip = "Gives you the maximum amount of currency in the ghost reactor (2 billion)."},
                new Module { buttonText = "Max Currency Gun", method =() => Master.SetCurrencyGun(int.MaxValue), toolTip = "Gives whoever your hand desires the maximum amount of currency in the ghost reactor (2 billion)."},
                new Module { buttonText = "Max Currency All", method =() => Master.SetCurrencyAll(int.MaxValue), isTogglable = false, toolTip = "Gives everyone in the room the maximum amount of currency in the ghost reactor (2 billion)."},

                new Module { buttonText = "Add Currency Self", method =() => Master.AddCurrencySelf(100), isTogglable = false, toolTip = "Gives you 100 more currency in the ghost reactor."},
                new Module { buttonText = "Add Currency Gun", method =() => Master.AddCurrencyGun(100), toolTip = "Gives whoever your hand desires 100 more currency in the ghost reactor."},
                new Module { buttonText = "Add Currency All", method =() => Master.AddCurrencyAll(100), isTogglable = false, toolTip = "Gives everyone in the room 100 more currency in the ghost reactor."},

                new Module { buttonText = "Remove Currency Self", method =() => Master.SetCurrencySelf(), isTogglable = false, toolTip = "Removes all currency in the ghost reactor from yourself."},
                new Module { buttonText = "Remove Currency Gun", method =() => Master.SetCurrencyGun(), toolTip = "Removes all currency in the ghost reactor from whoever your hand desires."},
                new Module { buttonText = "Remove Currency All", method =() => Master.SetCurrencyAll(), isTogglable = false, toolTip = "Removes all currency in the ghost reactor from everyone in the room."},

                new Module { buttonText = "Invincibility", method = Master.Invincibility, toolTip = "Makes you unable to die in the ghost reactor."},

                new Module { buttonText = "Start Shift", method = Master.StartShift, isTogglable = false, toolTip = "Starts a new ghost reactor shift."},
                new Module { buttonText = "End Shift", method = Master.EndShift, isTogglable = false, toolTip = "Ends the current ghost reactor shift."},
                new Module { buttonText = "Set Quota", method = Master.SetQuota, toolTip = "Meets the quota for you."},

                new Module { buttonText = "Virtual Stump Kick Gun", method = Master.VirtualStumpKickGun, toolTip = "Kicks whoever your hand desires in the virtual stump."},
                new Module { buttonText = "Virtual Stump Kick All", method = Master.VirtualStumpKickAll, toolTip = "Kicks everyone in the virtual stump."},

                new Module { buttonText = "Virtual Stump Crash Gun", method = Master.MasterVirtualStumpCrashGun, toolTip = "Crashes whoever your hand desires in the virtual stump."},
                new Module { buttonText = "Virtual Stump Crash All", method = Master.MasterVirtualStumpCrashAll, toolTip = "Crashes everyone in the virtual stump."},

                new Module { buttonText = "Ghost Reactor Freeze Gun", method = Master.GhostReactorFreezeGun, toolTip = "Freezes whoever your hand desires in the ghost reactor."},
                new Module { buttonText = "Ghost Reactor Freeze All", method = Master.GhostReactorFreezeAll, toolTip = "Freezes everyone in the ghost reactor."},

                new Module { buttonText = "Ghost Reactor Crash Gun", method = Master.GhostReactorCrashGun, toolTip = "Crashes whoever your hand desires in the ghost reactor."},
                new Module { buttonText = "Ghost Reactor Crash All", method = Master.GhostReactorCrashAll, toolTip = "Crashes everyone in the ghost reactor."},

                new Module { buttonText = "Super Infection Crash Gun", method = Master.SuperInfectionCrashGun, toolTip = "Crashes whoever your hand desires in the Super Infection gamemode."},
                new Module { buttonText = "Super Infection Crash All", method = Master.SuperInfectionCrashAll, toolTip = "Crashes everyone in the Super Infection gamemode."},

                new Module { buttonText = "Super Infection Break Audio Gun", method = Master.SuperInfectionBreakAudioGun, toolTip = "Breaks the audio of whoever your hand desires in the Super Infection gamemode."},
                new Module { buttonText = "Super Infection Break Audio All", method = Master.SuperInfectionBreakAudioAll, toolTip = "Breaks the audio of everyone in the Super Infection gamemode."},

                new Module { buttonText = "Rise Lava", method =() => Master.ChangeLavaState(InfectionLavaController.RisingLavaState.Rising), isTogglable = false, toolTip = "Raises the lava in the forest lava map."},
                new Module { buttonText = "Flood Lava", method =() => Master.ChangeLavaState(InfectionLavaController.RisingLavaState.Full), isTogglable = false, toolTip = "Makes the lava in the forest lava map at the top."},
                new Module { buttonText = "Drain Lava", method =() => Master.ChangeLavaState(InfectionLavaController.RisingLavaState.Draining), isTogglable = false, toolTip = "Drains the lava in the forest lava map."},
                new Module { buttonText = "Empty Lava", method =() => Master.ChangeLavaState(InfectionLavaController.RisingLavaState.Drained), isTogglable = false, toolTip = "Makes the lava in the forest lava map drained."},
                new Module { buttonText = "Erupt Lava", method =() => Master.ChangeLavaState(InfectionLavaController.RisingLavaState.Erupting), isTogglable = false, toolTip = "Erupts the lava in the forest lava map."},

                
                

                new Module { buttonText = "Kill Self", method =() => Master.SetStateSelf(1), isTogglable = false, toolTip = "Turns you into a ghost."},
                new Module { buttonText = "Kill Gun", method =() => Master.SetStateGun(1), toolTip = "Turns whoever your hand desires into a ghost."},
                new Module { buttonText = "Kill All", method =() => Master.SetStateAll(1), isTogglable = false, toolTip = "Turns everyone in the room into a ghost."},

                new Module { buttonText = "Revive Self", method =() => Master.SetStateSelf(0), isTogglable = false, toolTip = "Revives you from death."},
                new Module { buttonText = "Revive Gun", method =() => Master.SetStateGun(0), toolTip = "Revives whoever your hand desires from death."},
                new Module { buttonText = "Revive All", method =() => Master.SetStateAll(0), isTogglable = false, toolTip = "Revives everyone in the room from death."},

                new Module { buttonText = "Spaz Kill Self", method = Master.SpazKillSelf, toolTip = "Repeatedly kills and revives you."},
                new Module { buttonText = "Spaz Kill Gun", method = Master.SpazKillGun, toolTip = "Repeatedly kills and revives whoever your hand desires."},
                new Module { buttonText = "Spaz Kill All", method = Master.SpazKillAll, toolTip = "Repeatedly kills and revives everyone in the room."},

                new Module { buttonText = "Unlimited Building", enableMethod = Blocks.UnlimitedBuilding, disableMethod = Blocks.DisableUnlimitedBuilding, toolTip = "Unlimits building, disabling drop zones and letting you place on people's plots." },

                new Module { buttonText = $"Shotgun {Bracket("green", "G")}", method = Master.Shotgun, toolTip = "Spawns you a shotgun when you press <color=green>grip</color>."},

                new Module { buttonText = "Building Block Crash Gun", method = Blocks.BlockCrashGun, toolTip = "Crashes whoever your hand desires if they are inside of the block map."},
                new Module { buttonText = $"Building Block Crash All {Bracket("green", "T")}", method = Blocks.BlockCrashAll, toolTip = "Crashes everybody inside of the block map."},

                new Module { buttonText = "Block Anti Report", enableMethod = Master.EnableAtticAntiReport, method = Master.AtticAntiReport, toolTip = "Automatically builds blocks around your report button."},

                new Module { buttonText = "Block Draw Gun", overlapText = "Building Block Draw Gun", method = Master.AtticDrawGun, toolTip = "Draw wherever your hand desires."},
                new Module { buttonText = "Block Build Gun", overlapText = "Building Block Build Gun",method = Master.AtticBuildGun, toolTip = "Draw wherever your hand desires with no delay."},
                new Module { buttonText = "Block Tower Gun", overlapText = "Building Block Tower Gun",method = Master.AtticTowerGun, toolTip = "Builds a tower wherever your hand desires."},

                new Module { buttonText = "Block Freeze Gun", overlapText = "Building Block Freeze Gun", method = Master.AtticFreezeGun, toolTip = "Freeze whoever your hand desires."},
                new Module { buttonText = $"Block Freeze All {Bracket("green", "T")}", overlapText = $"Building Block Freeze All {Bracket("green", "T")}", method = Master.AtticFreezeAll, toolTip = "Freezes everyone in the lobby when holding <color=green>trigger</color>."},

                new Module { buttonText = "Block Float Gun", overlapText = "Building Block Float Gun", method = Master.AtticFloatGun, toolTip = "Makes whoever your hand desires float using the building blocks."},
                new Module { buttonText = "Building Block Fling Gun", method = Master.AtticFlingGun, toolTip = "Flings whoever your hand desires using the building blocks."},

                new Module { buttonText = "Building Block Bring Gun", method = Master.AtticBringGun, toolTip = "Brings whoever your hand desires using the building blocks."},
                new Module { buttonText = "Building Block Push Gun", method = Master.AtticPushGun, toolTip = "Pushes whoever your hand desires using the building blocks."},

                new Module { buttonText = "Spaz Targets", method = Master.TargetSpam, toolTip = "Gives the targets a seizure."},

                new Module { buttonText = "Slow Monsters", enableMethod = Master.SlowMonsters, disableMethod = Master.FixMonsters, toolTip = "Slows down the basement monsters." },
                new Module { buttonText = "Fast Monsters", enableMethod = Master.FastMonsters, disableMethod = Master.FixMonsters, toolTip = "Speeds up the basement monsters." },

                new Module { buttonText = $"Grab Monsters {Bracket("green", "G")}", method = Master.GrabMonsters, toolTip = "Puts the basement monsters in your hand." },
                new Module { buttonText = "Monster Gun", method = Master.MonsterGun, toolTip = "Moves the basement monsters to wherever your hand desires." },
                new Module { buttonText = "Spaz Monsters", method = Master.SpazMonsters, toolTip = "Gives the basement monsters a seizure." },
                new Module { buttonText = "Orbit Monsters", method = Master.OrbitMonsters, toolTip = "Orbits the basement monsters around you." },
                new Module { buttonText = "Destroy Monsters", method = Master.DestroyMonsters, isTogglable = false, toolTip = "Sends the basement monsters to hell." },

                new Module { buttonText = "Infection to Tag", method = Master.InfectionToTag, disableMethod = Master.FixThreshold, toolTip = "Turns the game into tag instead of infection." },
                new Module { buttonText = "Tag to Infection", method = Master.TagToInfection, disableMethod = Master.FixThreshold, toolTip = "Turns the game into infection instead of tag." },

                new Module { buttonText = "Untag Gun", method = Master.UntagGun, toolTip = "Untags whoever your hand desires."},
                new Module { buttonText = "Untag All", method = Master.UntagAll, isTogglable = false, toolTip = "Removes everyone from the list of tagged players."},

                new Module { buttonText = "Break Tag", method = Master.UntagAll, toolTip = "Constantly removes everyone from the list of tagged players."},

                new Module { buttonText = "Spam Tag Self", method = Master.SpamTagSelf, toolTip = "Adds and removes you from the list of tagged players."},
                new Module { buttonText = "Spam Tag Gun", method = Master.SpamTagGun, toolTip = "Adds and removes you from the list of tagged players."},
                new Module { buttonText = "Spam Tag All", method = Master.SpamTagAll, toolTip = "Adds and removes everyone from the list of tagged players."},

                new Module { buttonText = "Rock Self", method = Master.RockSelf, isTogglable = false, toolTip = "Sets yourself to rock."},
                new Module { buttonText = "Rock Gun", method = Master.RockGun, toolTip = "Sets whoever your hand desires to rock."},
                new Module { buttonText = "Rock All", method = Master.RockAll, toolTip = "Sets everyone in the room to rock."},
                new Module { buttonText = "Rock Aura", method = Master.RockAura, toolTip = "Sets players nearby you to rock."},
                new Module { buttonText = "Rock On Touch", method = Master.RockOnTouch, toolTip = "Sets whoever you touch to rock."},

                new Module { buttonText = "Give Tag Lag Gun", method = Master.TagLagGun, toolTip = "Forces tag lag on whoever your hand desires, making them untaggable."},
                new Module { buttonText = "Tag Lag Gun", method = Master.TagLagGun, toolTip = "Forces tag lag on whoever your hand desires, letting them not be able to tag anyone."},
                new Module { buttonText = "Tag Lag", overlapText = "Tag Lag All", method =() => Master.SetTagCooldown(float.MaxValue), disableMethod =() => Master.SetTagCooldown(5f), toolTip = "Forces tag lag in the everyone in the room, letting no one get tagged."},

                new Module { buttonText = "Unlock Driver", method =() => Master.DriverStatus(false), isTogglable = false, toolTip = "Unlocks the driver in the virtual stump."},
                new Module { buttonText = "Become Driver", method =() => Master.DriverStatus(true), isTogglable = false, toolTip = "Makes you the driver in the virtual stump."},
                new Module { buttonText = "Spaz Driver", method = Master.SpazDriver, isTogglable = true, toolTip = "Spaz makes and unmakes you the driver in the virtual stump."},

                new Module { buttonText = "Become Driver Gun", method =() => Master.DriverStatusGun(true), isTogglable = true, toolTip = "Makes whoever your hand desires the driver in the virtual stump."},
                new Module { buttonText = "Unlock Driver Gun", method =() => Master.DriverStatusGun(false), isTogglable = true, toolTip = "Unlocks the driver for whoever your hand desires in the virtual stump."},
                new Module { buttonText = "Spaz Driver Gun", method = Master.SpazDriverStatusGun, isTogglable = true, toolTip = "Spaz makes and unmakes whoever your hand desires the driver in the virtual stump."},

                new Module { buttonText = $"Bonk Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(4), toolTip = "Plays the bonk sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Count Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(1), toolTip = "Plays the count sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Brawl Count Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(6), toolTip = "Plays the brawl count sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Brawl Start Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(7), toolTip = "Plays the brawl start sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Tag Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(0), toolTip = "Plays the tag sound when holding <color=green>grip</color>." },
                new Module { buttonText = $"Round End Sound Spam {Bracket("green", "G")}", method =() => Master.BetaSoundSpam(2), toolTip = "Plays the round end sound when holding <color=green>grip</color>." },

                new Module { buttonText = "Paintbrawl Start Game", method = Master.PaintbrawlStartGame, isTogglable = false, toolTip = "Starts a game of paintbrawl." },
                new Module { buttonText = "Paintbrawl End Game", method = Master.PaintbrawlEndGame, isTogglable = false, toolTip = "Ends the current game of paintbrawl." },
                new Module { buttonText = "Paintbrawl Restart Game", method = Master.PaintbrawlRestartGame, isTogglable = false, toolTip = "Restarts the current game of paintbrawl." },
                new Module { buttonText = "Paintbrawl Restart Spam", method = Master.PaintbrawlRestartGame, toolTip = "Spam starts and ends games of paintbrawl." },

                new Module { buttonText = "Paintbrawl Balloon Spam Self", method = Master.PaintbrawlBalloonSpamSelf, toolTip = "Spam pops and unpops your balloons in paintbrawl." },
                new Module { buttonText = "Paintbrawl Balloon Spam Gun", method = Master.PaintbrawlBalloonSpamGun, toolTip = "Spam pops and unpops whoever your hand desires' balloons in paintbrawl." },
                new Module { buttonText = "Paintbrawl Balloon Spam All", method = Master.PaintbrawlBalloonSpam, toolTip = "Spam pops and unpops everyone's balloons in paintbrawl." },

                new Module { buttonText = "Paintbrawl Revive Self", method = Master.PaintbrawlReviveSelf, isTogglable = false, toolTip = "Revives yourself in paintbrawl." },
                new Module { buttonText = "Paintbrawl Revive Gun", method = Master.PaintbrawlReviveGun, toolTip = "Revives whoever your hand desires in paintbrawl." },
                new Module { buttonText = "Paintbrawl Revive All", method = Master.PaintbrawlReviveAll, isTogglable = false, toolTip = "Revives everyone in paintbrawl." },

                new Module { buttonText = "Paintbrawl No Delay", method = Master.PaintbrawlNoDelay, disableMethod = Master.DisablePaintbrawlNoDelay, toolTip = "Revives everyone in paintbrawl." },

                new Module { buttonText = "Paintbrawl God Mode", method = Master.PaintbrawlGodMode, toolTip = "Gives you god mode in paintbrawl." },

                new Module { buttonText = "Slow Self", method = Master.SlowSelf, isTogglable = false, toolTip = "Forces tag freeze on yourself." },
                new Module { buttonText = "Slow Gun", method = Master.SlowGun, toolTip = "Forces tag freeze on whoever your hand desires." },
                new Module { buttonText = "Slow All", method = Master.SlowAll, toolTip = "Forces tag freeze on everyone in the the room." },
                new Module { buttonText = "Slow Aura", method = Master.SlowAura, toolTip = "Forces tag freeze on players nearby you."},
                new Module { buttonText = "Slow On Touch", method = Master.SlowOnTouch, toolTip = "Forces tag freeze on whoever you touch."},

                new Module { buttonText = "Vibrate Self", method = Master.VibrateSelf, isTogglable = false, toolTip = "Makes your controllers vibrate." },
                new Module { buttonText = "Vibrate Gun", method = Master.VibrateGun, toolTip = "Makes whoever your hand desires' controllers vibrate." },
                new Module { buttonText = "Vibrate All", method = Master.VibrateAll, toolTip = "Makes everyone in the the room's controllers vibrate." },
                new Module { buttonText = "Vibrate Aura", method = Master.VibrateAura, toolTip = "Makes players nearby you controllers vibrate."},
                new Module { buttonText = "Vibrate On Touch", method = Master.VibrateOnTouch, toolTip = "Makes whoever you touch controllers vibrate."},
            },

            ["Overpowered Mods"] = new[]
            {
                new Module { buttonText = "Exit Overpowered Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Always Guardian", method = Overpowered.AlwaysGuardian, disableMethod = Movement.EnableRig, toolTip = "Makes you always the guardian."},
                new Module { buttonText = "Guardian Protector", method = Overpowered.GuardianProtector, toolTip = "Pushes people away from the guardian moon if they try to approach it."},

                new Module { buttonText = "Guardian Grab Gun", method = Overpowered.GuardianGrabGun, toolTip = "Grabs whoever your hand desires if you're the guardian."},
                new Module { buttonText = $"Guardian Grab All {Bracket("green", "G")}", method = Overpowered.GuardianGrabAll, toolTip = "Grabs everyone in the room if you're the guardian."},

                new Module { buttonText = "Guardian Release Gun", method = Overpowered.GuardianReleaseGun, toolTip = "Releases whoever your hand desires if you're the guardian."},
                new Module { buttonText = $"Guardian Release All {Bracket("green", "T")}", method = Overpowered.GuardianReleaseAll, toolTip = "Releases everyone in the room if you're the guardian."},

                new Module { buttonText = "Guardian Fling Gun", method = Overpowered.GuardianFlingGun, toolTip = "Flings whoever your hand desires."},
                new Module { buttonText = $"Guardian Fling All {Bracket("green", "T")}", method = Overpowered.GuardianFlingAll, toolTip = "Flings everyone in the room."},

                new Module { buttonText = "Guardian Bring Gun", method = Overpowered.GuardianBringGun, toolTip = "Brings whoever your hand desires towards you."},
                new Module { buttonText = $"Guardian Bring All {Bracket("green", "T")}", method = Overpowered.GuardianBringAll, toolTip = "Brings everyone in the room towards you."},

                new Module { buttonText = "Guardian Bring All Gun", method = Overpowered.GuardianBringAllGun, toolTip = "Brings everyone in the room towards wherever your hand desires."},

                new Module { buttonText = "Guardian Bring Away Gun", method = Overpowered.GuardianBringAwayGun, toolTip = "Brings whoever your hand desires towards you."},
                new Module { buttonText = $"Guardian Bring Away All {Bracket("green", "T")}", method = Overpowered.GuardianBringAwayAll, toolTip = "Brings everyone in the room towards you."},

                new Module { buttonText = "Bring Away All Gun", method = Overpowered.GuardianBringAwayAllGun, toolTip = "Brings everyone in the room towards wherever your hand desires."},

                new Module { buttonText = "Guardian Anti Stump", method = Overpowered.GuardianAntiStump, toolTip = "Anyone who gets too close to the stump entrance will be launched away."},

                new Module { buttonText = $"Guardian Orbit All {Bracket("green", "T")}", method = Overpowered.GuardianOrbitAll, toolTip = "Orbits everyone in the room around you."},

                new Module { buttonText = "Guardian Punch Mod", method = Overpowered.GuardianPunchMod, toolTip = "Flings people when you punch them."},
                new Module { buttonText = "Guardian Boxing", method = Overpowered.GuardianBoxing, toolTip = "Lets everyone in the room punch eachother."},

                new Module { buttonText = "Guardian Give Fly Gun", method = Overpowered.GuardianGiveFlyGun, toolTip = "Gives whoever you want fly when they hold their right thumb down."},
                new Module { buttonText = "Guardian Give Fly All", method = Overpowered.GuardianGiveFlyAll, toolTip = "Gives everyone in the room fly when they hold their right thumb down."},

                new Module { buttonText = "Guardian Spaz Player Gun", method = Overpowered.GuardianSpazPlayerGun, toolTip = "Spazzes out whoever your hand desires."},
                new Module { buttonText = $"Guardian Spaz All Players {Bracket("green", "T")}", method = Overpowered.GuardianSpazAllPlayers, toolTip = "Spazzes out everyone in the room."},

                new Module { buttonText = $"Guardian Effect Spam Hands {Bracket("green", "G")}", method = Overpowered.GuardianEffectSpamHands, toolTip = "Spawns effects when holding <color=green>grip</color>."},
                new Module { buttonText = "Effect Spam Gun", method = Overpowered.GuardianEffectSpamGun, overlapText = "Guardian Effect Spam Gun", toolTip = "Spawns effects wherever your hand desires."},

                new Module { buttonText = "Guardian Freeze Gun", method = Overpowered.GuardianPhysicalFreezeGun, toolTip = "Freezes whoever your hand desires." },
                new Module { buttonText = $"Guardian Freeze All {Bracket("green", "T")}", method = Overpowered.GuardianPhysicalFreezeAll, toolTip = "Freezes everyone in the room when holding <color=green>trigger</color>." },

                
                

                
                

                new Module { buttonText = "Delay Ban Gun", method = Overpowered.DelayBanGun, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Delay bans whoever your hand desires."},
                new Module { buttonText = "Delay Ban All", enableMethod = Overpowered.DelayBanAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Delay bans everyone in the room."},

                
                

                new Module { buttonText = "Force Grab", method =()=> Overpowered.ForceGrab(References.LocalRig.transform.position), toolTip = "Attempts to grab the hand of anyone who presses their grips." },
                new Module { buttonText = "Force Grab Gun", postMethod = Overpowered.ForceGrabGun, disableMethod =() => References.LocalRig.enabled = true, toolTip = "Forcibly grab whoever your hand desires." },
                new Module { buttonText = "Fling on Grab", method = Overpowered.FlingOnGrab, toolTip = "Flings the player when they grab you." },
                new Module { buttonText = "Give Fly on Grab", method = Overpowered.GiveFlyOnGrab, toolTip = "When someone is holding you, they can fly by pressing their trigger." },

                
                
                
                

                new Module { buttonText = $"Anti Report {Bracket("green", "Fling")}", method = Overpowered.AntiReportFling, toolTip = "Flings whoever tries to report you."},

                new Module { buttonText = "Grab Fling Gun", enableMethod =() => Overpowered.GrabStatus = true, method = Overpowered.GrabFlingGun, disableMethod =() => Overpowered.GrabStatus = false, toolTip = "Tries to fling whoever your hand desires."},
                new Module { buttonText = "Grab Fling All", enableMethod =() => Overpowered.GrabStatus = true, postMethod = Overpowered.GrabFlingAll, disableMethod =() => Overpowered.GrabStatus = false, toolTip = "Tries to fling everyone in the room."},

                
                
                

                
                


                new Module { buttonText = "Lag Gun", method = Overpowered.LagGun, toolTip = "Lags whoever your hand desires."},
                new Module { buttonText = "Lag All", method = Overpowered.LagAll, toolTip = "Lags everyone in the room."},
                new Module { buttonText = "Lag Aura", method = Overpowered.LagAura, toolTip = "Lags players nearby."},
                new Module { buttonText = "Lag On Touch", method = Overpowered.LagOnTouch, toolTip = "Lags whoever you touch." },

                new Module { buttonText = "Server Mute All", method = Overpowered.ServerMuteAll, toolTip = "Mutes everyone in the server."},
                new Module { buttonText = "Deafen Gun", method = Overpowered.DeafenGun, toolTip = "Makes whoever your hand deseries not be able to hear anyone else."},
                new Module { buttonText = "Deafen All", method = Overpowered.DeafenAll, toolTip = "Makes everyone not be able to hear anyone except you."},

                

                

                
                

                
                

                
                

                
                

                
                

                
                

                new Module { buttonText = "Lock Room", method =() => Overpowered.SetRoomStatus(false), isTogglable = false, toolTip = "Locks the room so no one else can join."},
                new Module { buttonText = "Unlock Room", method =() => Overpowered.SetRoomStatus(true), isTogglable = false, toolTip = "Unlocks the room so anyone can join."},
                new Module { buttonText = "Close Room", enableMethod =() => ViewSerialization.OverrideSerialization = () => false, method = Overpowered.CloseRoom, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks everyone in the room." },
                new Module { buttonText = "Spy Room", enableMethod =() => Overpowered.CreatePeerBase(), disableMethod =() => Overpowered.UnloadPeerBase(), toolTip = "Allows you to hear people whilst being disconnected from the room." },

                new Module { buttonText = "Destroy Gun", method = Overpowered.DestroyGun, toolTip = "Block new players from seeing whoever your hand desires."},
                new Module { buttonText = "Destroy All", method = Overpowered.DestroyAll, isTogglable = false, toolTip = "Block new players from seeing everyone."},
                new Module { buttonText = "Destroy Aura", method = Overpowered.DestroyAura, toolTip = "Block new players from seeing players nearby you."},
                new Module { buttonText = "Destroy On Touch", method = Overpowered.DestroyOnTouch, toolTip = "Block new players from seeing players you touch."},

                new Module { buttonText = "Stump Kick Gun", method = Overpowered.StumpKickGun, toolTip = "Kicks whoever your hand desires if they are in stump." },
                new Module { buttonText = "Stump Kick All", method = Overpowered.StumpKickAll, isTogglable = false, toolTip = "Kicks everyone in stump." },

                new Module { buttonText = "Elevator Kick Gun", method = Overpowered.ElevatorKickGun, toolTip = "Kicks whoever your hand desires if they are in the elevator."},
                new Module { buttonText = "Elevator Kick All", method = Overpowered.ElevatorKickAll, isTogglable = false, toolTip = "Kicks everyone in the elevator."},
                new Module { buttonText = "Elevator Kick Aura", method = Overpowered.ElevatorKickAura, toolTip = "Kicks players nearby you if they are in the elevator."},
                new Module { buttonText = "Elevator Kick On Touch", method = Overpowered.ElevatorKickOnTouch, toolTip = "Kicks players you touch if they are in the elevator."},

                new Module { buttonText = "Instant Party", method = Overpowered.InstantParty, toolTip = "Makes parties form instantly, instead of having to wait a couple of seconds." },
                new Module { buttonText = "Leave Party", method =() => ManagerRegistry.FriendshipGroupDetection.LeaveParty(), isTogglable = false, toolTip = "Leaves the party, incase you can't pull off the string." },
                new Module { buttonText = "Party Break Network Triggers", method = Overpowered.PartyBreakNetworkTriggers, toolTip = "Breaks the network triggers for anyone in your party." },

                new Module { buttonText = "Party Kick Gun", method = Overpowered.PartyKickGun, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks whoever your hand desires if they're in your party from the room."},
                new Module { buttonText = "Party Kick All", method = Overpowered.PartyKickAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks everyone in your party from the room."},
                new Module { buttonText = "Party Kick Aura", method = Overpowered.PartyKickAura, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks nearby party members from the room."},
                new Module { buttonText = "Party Kick On Touch", method = Overpowered.PartyKickOnTouch, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Kicks party members you touch from the room."},

                new Module { buttonText = "Kick All in Party", overlapText = "Party Send All", method = Overpowered.KickAllInParty, isTogglable = false, toolTip = "Sends everyone in your party to a random room." },
                new Module { buttonText = "Ban All in Party", overlapText = "Party Ban All", method = Overpowered.BanAllInParty, isTogglable = false, toolTip = "Sends everyone in your party to a bannable code." },

                new Module { buttonText = "Auto Party Kick", method = Overpowered.AutoPartyKick, toolTip = "When you party, you will automatically send everyone in your party to a random room." },
                new Module { buttonText = "Auto Party Ban", method = Overpowered.AutoPartyBan, toolTip = "When you party, you will automatically send everyone in your party to a bannable code." },

                new Module { buttonText = "Break Audio Gun", method = Overpowered.BreakAudioGun, toolTip = "Attempts to break the audio of whoever your hand desires." },
                new Module { buttonText = $"Break Audio All {Bracket("green", "T")}", method = Overpowered.BreakAudioAll, toolTip = "Attempts to break everyone's audio when holding trigger." },
            },

            ["Soundboard"] = new[]
            {
                new Module { buttonText = "Exit Soundboard", method = () => CurrentCategoryName = "Fun Mods", isTogglable = false, toolTip = "Returns you back to the fun mods.", legal = true}
            },

            ["Favorite Mods"] = new[]
            {
                new Module { buttonText = "Exit Favorite Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},
            },

            ["Menu Presets"] = new[]
            {
                new Module { buttonText = "Exit Menu Presets", method =() => CurrentCategoryName = "Menu Settings", isTogglable = false, toolTip = "Returns to the settings for the menu.", legal = true},

                new Module { buttonText = "Legitimate Preset", method = Presets.LegitimatePreset, isTogglable = false, toolTip = "Enables a bunch of mods that make it impossible to mod check you."},
                new Module { buttonText = "Performance Preset", method = Presets.PerformancePreset, isTogglable = false, toolTip = "Enables some mods that attempt to maximize your FPS as much as possible."},
                new Module { buttonText = "Safety Preset", method = Presets.SafetyPreset, isTogglable = false, toolTip = "Enables some mods that attempt to keep you as safe as possible."},
                new Module { buttonText = "Ghost Preset", method = Presets.GhostPreset, isTogglable = false, toolTip = "Enables a bunch of mods that are commonly used for ghost trolling."},

                new Module { buttonText = "Save Custom Preset 1", method =() => Presets.SaveCustomPreset(1), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 1", method =() => Presets.LoadCustomPreset(1), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 2", method =() => Presets.SaveCustomPreset(2), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 2", method =() => Presets.LoadCustomPreset(2), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 3", method =() => Presets.SaveCustomPreset(3), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 3", method =() => Presets.LoadCustomPreset(3), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 4", method =() => Presets.SaveCustomPreset(4), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 4", method =() => Presets.LoadCustomPreset(4), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 5", method =() => Presets.SaveCustomPreset(5), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 5", method =() => Presets.LoadCustomPreset(5), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 6", method =() => Presets.SaveCustomPreset(6), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 6", method =() => Presets.LoadCustomPreset(6), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 7", method =() => Presets.SaveCustomPreset(7), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 7", method =() => Presets.LoadCustomPreset(7), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 8", method =() => Presets.SaveCustomPreset(8), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 8", method =() => Presets.LoadCustomPreset(8), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 9", method =() => Presets.SaveCustomPreset(9), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 9", method =() => Presets.LoadCustomPreset(9), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Save Custom Preset 10", method =() => Presets.SaveCustomPreset(10), isTogglable = false, toolTip = "Saves a custom preset."},
                new Module { buttonText = "Load Custom Preset 10", method =() => Presets.LoadCustomPreset(10), isTogglable = false, toolTip = "Loads a custom preset."},

                new Module { buttonText = "Quick Start Mods", method = Presets.SimplePreset, isTogglable = false, toolTip = "Enables some mods that attempt to improve your experience using the menu."},
            },

            ["Advantage Settings"] = new[]
            {
                new Module { buttonText = "Exit Advantage Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Disable Instant Tag", enableMethod =() => Advantages.instantTag = false, disableMethod =() => Advantages.instantTag = true, toolTip = "Makes the tag mods visibly teleport to the player instead of instantly tagging them."},
                new Module { buttonText = "Obnoxious Tag", toolTip = "Makes the tag mods more obnoxious. Instead of hiding in the ground, you teleport around the player like crazy."},
                new Module { buttonText = "Prettier Visualize", toolTip = "Makes the visualizers look prettier."},
                new Module { buttonText = "Visualize Tag Aura", toolTip = "Visualizes the distance threshold for the tag aura."},
                new Module { buttonText = "Visualize Tag Reach", toolTip = "Visualizes the distance threshold for the tag reach."},

                ButtonHelper.Create("ctaRange", () => Advantages.TagAuraRangeNames, Advantages.tagAuraIndex, Advantages.ApplyTagAuraRange, "Changes the range of the tag aura mods.", overlapText: "Change Tag Aura Range"),
                ButtonHelper.Create("ctrRange", () => Advantages.TagReachDistanceNames, Advantages.tagRangeIndex, Advantages.ApplyTagReachDistance, "Changes the range of the tag reach mods.", overlapText: "Change Tag Reach Distance"),

                new Module { buttonText = "Fake Lag Others", toolTip = "Makes fake lag affect other players' rigs."},
                new Module { buttonText = "Disable Fake Lag Self", toolTip = "Excludes yourself from fake lag." },
                ButtonHelper.CreateNumeric("Change Fake Lag Strength", 0, 20, Movement.fakeLagDelayIndex, Movement.ApplyFakeLagStrength, v => v.ToString(), "Changes the ping of the \"Fake Lag\" mod.")
            },

            ["Visual Settings"] = new[]
            {
                new Module { buttonText = "Exit Visual Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                ButtonHelper.CreateNumeric("Change Performance Visuals Step", 0, 10, Visuals.PerformanceModeStepIndex, Visuals.ApplyPerformanceModeVisualStep, v => (v / 10f).ToString(), "Changes the time between rendering visual mods.", legal: true),
                new Module { buttonText = "Performance Visuals", enableMethod =() => Visuals.PerformanceVisuals = true, disableMethod =() => Visuals.PerformanceVisuals = false, toolTip = "Makes visual mods render less often, to increase performange and decrease memory usage.", legal = true},
                new Module { buttonText = "Short Breadcrumbs", toolTip = "Shortens the length of the breadcrumbs.", legal = true},
                new Module { buttonText = "Follow Menu Theme", toolTip = "Makes visual mods match the theme of the menu, rather than the color of the player.", legal = true},
                new Module { buttonText = "Follow Player Colors", toolTip = "Makes the infection tracers appear their normal color instead of orange for tagged players.", legal = true},
                new Module { buttonText = "Transparent Theme", overlapText = "Transparent Visuals", toolTip = "Makes visual mods transparent.", legal = true},
                new Module { buttonText = "Nametag Chams", enableMethod =() => Visuals.nameTagChams = true, disableMethod =() => Visuals.nameTagChams = false, toolTip = "Make name tags show through objects."},
                new Module { buttonText = "Anchor Name Tags", enableMethod =() => Visuals.anchorNameTag = true, disableMethod =() => Visuals.anchorNameTag = false, toolTip = "Anchors nametags to the player's rig instead of the head.", legal = true},
                new Module { buttonText = "Show Self Nametag", enableMethod =() => Visuals.selfNameTag = true, disableMethod =() => Visuals.selfNameTag = false, toolTip = "Makes all the name tag mods render for you as well.", legal = true},
                new Module { buttonText = "Hidden on Camera", overlapText = "Streamer Mode Visuals", toolTip = "Makes visual mods only render on VR."},
                new Module { buttonText = "Hidden Labels", overlapText = "Streamer Mode Labels", toolTip = "Makes label mods only render on VR."},
                new Module { buttonText = "Thin Tracers", toolTip = "Makes the tracers thinner.", legal = true},
                new Module { buttonText = "Smooth Lines", enableMethod =() => smoothLines = true, disableMethod =() => smoothLines = false, toolTip = "Makes every line generated by the menu have smooth ends.", legal = true},
                new Module { buttonText = "Show Cosmetics", overlapText = "Show Cosmetics on Chams", toolTip = "If enabled, the cosmetics will also show through walls."}
            },

            ["Admin Mods"] = new[]
            {
                new Module { buttonText = "Exit Admin Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Mod Givers", method =() => CurrentCategoryName = "Mod Givers", isTogglable = false, toolTip = "Opens the mod givers page.", legal = true},

                new Module { buttonText = "Get Menu Users", method = Admin.GetMenuUsers, isTogglable = false, toolTip = "Detects who is using the menu.", legal = true},
                new Module { buttonText = "Auto Get Menu Users", enableMethod =() => ManagerRegistry.NetworkSystem.OnJoinedRoomEvent += Admin.GetMenuUsers, disableMethod =() => ManagerRegistry.NetworkSystem.OnJoinedRoomEvent -= Admin.GetMenuUsers, isTogglable = true, toolTip = "Detects who is using the menu on room join.", legal = true},
                new Module { buttonText = "Menu User Name Tags", enableMethod = Admin.EnableAdminMenuUserTags, method = Admin.AdminMenuUserTags, disableMethod = Admin.DisableAdminMenuUserTags, toolTip = "Puts nametags on menu users.", legal = true},
                new Module { buttonText = "Conduct Menu Users", enableMethod =() => { Admin.EnableAdminMenuUserTags(); GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText").GetComponent<TextMeshPro>().text = "CONSOLE USER LIST"; GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData").GetComponent<TextMeshPro>().richText = true; }, method = Admin.ConsoleOnConduct, toolTip = "Shows menu users on the code of conduct.", legal = true},
                new Module { buttonText = "Menu User Tracers", enableMethod = Admin.EnableAdminMenuUserTracers, method = Admin.MenuUserTracers, disableMethod =() => {Visuals.isLineRenderQueued = true;}, toolTip = "Puts tracers on your right hand to menu users.", legal = true},

                new Module { buttonText = "Admin Kick Gun", method = Admin.AdminKickGun, toolTip = "Kicks whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Kick All", method = Admin.AdminKickAll, isTogglable = false, toolTip = "Kicks everyone using the menu.", legal = true},

                new Module { buttonText = "Admin Crash Gun", method = Admin.AdminCrashGun, toolTip = "Crashes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Crash All", method = Admin.AdminCrashAll, toolTip = "Crashes everyone using the menu.", legal = true},

                new Module { buttonText = "Admin Break Game Gun", method = Admin.AdminCrashBypassGun, toolTip = "Crashes menu users who attempt to bypass crashers, also breaks the game.", legal = true},

                new Module { buttonText = "Admin Flip Menu Gun", method = Admin.FlipMenuGun, toolTip = "Flips the menu of whoever your hand desires if they're using the menu.", legal = true},

                new Module { buttonText = "Admin Freeze Gun", method =() => Admin.AdminFreezeGun(true), toolTip = "Freezes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Unfreeze Gun", method =() => Admin.AdminFreezeGun(false), toolTip = "Unfreezes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Spaz Freeze Gun", method =() => { Admin.AdminFreezeGun(true); Admin.AdminFreezeGun(false); }, toolTip = "Freezes and unfreezes whoever your hand desires if they're using the menu.", legal = true},

                new Module { buttonText = "Admin Mute Gun", method =() => Admin.AdminEnableGun(true, "Mute Microphone"), toolTip = "Mutes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Unmute Gun", method =() => Admin.AdminEnableGun(false, "Mute Microphone"), toolTip = "Unmutes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Stutter Voice Gun", method =() => { Admin.AdminEnableGun(true, "Mute Microphone"); Admin.AdminEnableGun(false, "Mute Microphone"); }, toolTip = "Stutters the voice of whoever your hand desires by muting and unmuting them if they're using the menu.", legal = true},

                new Module { buttonText = "Admin Jumpscare Gun", method = Admin.AdminJumpscareGun, toolTip = "Jumpscares whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Jumpscare All", method = Admin.AdminJumpscareAll, isTogglable = false, toolTip = "Jumpscares everyone using the menu.", legal = true},

                new Module { buttonText = $"Admin Mute All {Bracket("green", "T")}", method = Admin.AdminMute, toolTip = "Mutes everyone while holding trigger.", legal = true},

                new Module { buttonText = "Admin Board Mute Gun", method =() => Admin.AdminBMuteGun(true), toolTip = "Mutes whoever your hand desires for everyone using the menu.", legal = true},
                new Module { buttonText = "Admin Board Unmute Gun", method =() => Admin.AdminBMuteGun(false), toolTip = "Unmutes whoever your hand desires for everyone using the menu.", legal = true},
                new Module { buttonText = "Admin Board Stutter Voice Gun", method =() => { Admin.AdminBMuteGun(true); Admin.AdminBMuteGun(false); }, toolTip = "Stutters the voice of whoever your hand desires for everyone using the menu by muting and unmuting them.", legal = true},

                new Module { buttonText = "Admin Board Mute All", method =() => Admin.AdminBMuteAll(true), isTogglable = false, toolTip = "Mutes everyone for players using the menu.", legal = true},
                new Module { buttonText = "Admin Board Unmute All", method =() => Admin.AdminBMuteAll(false), isTogglable = false, toolTip = "Unmutes everyone for players using the menu.", legal = true},
                new Module { buttonText = "Admin Board Stutter Voice All", method =() => { Admin.AdminBMuteAll(true); Admin.AdminBMuteAll(false); }, toolTip = "Stutters the voice of everyone for players using the menu by muting and unmuting them.", legal = true},

                new Module { buttonText = "Admin Disable Menu Gun", method =() => Admin.AdminLockdownGun(true), toolTip = "Disables the menu of whoever your hand desires if they're using one.", legal = true},
                new Module { buttonText = "Admin Enable Menu Gun", method =() => Admin.AdminLockdownGun(false), toolTip = "Enables the menu of whoever your hand desires if they're using one.", legal = true},

                new Module { buttonText = "Admin Disable Menu All", method =() => Admin.AdminLockdownAll(true), toolTip = "Disables the menu of whoever your hand desires if they're using one.", legal = true},
                new Module { buttonText = "Admin Enable Menu All", method =() => Admin.AdminLockdownAll(false), toolTip = "Enables the menu of whoever your hand desires if they're using one.", legal = true},

                new Module { buttonText = "Admin Fully Disable Menu Gun", method =() => Admin.AdminFullLockdownGun(true), toolTip = "Disables the menu of whoever your hand desires and turns off their mods if they're using one.", legal = true},
                new Module { buttonText = "Admin Fully Enable Menu Gun", method =() => Admin.AdminFullLockdownGun(false), toolTip = "Enables the menu of whoever your hand desires and turns off their mods if they're using one.", legal = true},

                new Module { buttonText = "Admin Fully Disable Menu All", method =() => Admin.AdminFullLockdownAll(true), isTogglable = false, toolTip = "Disables the menu of whoever your hand desires and turns off their mods if they're using one.", legal = true},
                new Module { buttonText = "Admin Fully Enable Menu All", method =() => Admin.AdminFullLockdownAll(false), isTogglable = false, toolTip = "Enables the menu of whoever your hand desires and turns off their mods if they're using one.", legal = true},

                new Module { buttonText = "Admin Teleport Gun", method = Admin.AdminTeleportGun, toolTip = "Teleports whoever using the menu to wherever your hand desires.", legal = true},
                new Module { buttonText = "Admin Fling Gun", method = Admin.AdminFlingGun, toolTip = "Flings whoever your hand desires upwards.", legal = true},
                new Module { buttonText = "Admin Strangle", method = Admin.AdminStrangle, toolTip = "Strangles whoever you grab if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Fake Cosmetics", overlapText = "Admin Spoof Cosmetics", method =() => Cosmetics.AdminSpoofCosmetics(), enableMethod =() => { ManagerRegistry.NetworkSystem.OnPlayerJoined += Cosmetics.OnPlayerJoinSpoof; Cosmetics.AdminSpoofCosmetics(true); }, disableMethod =() => { ManagerRegistry.NetworkSystem.OnPlayerJoined -= Cosmetics.OnPlayerJoinSpoof; Cosmetics.oldCosmetics = null; }, toolTip = "Makes everyone using the menu see whatever cosmetics you have on as if you owned them.", legal = true},

                new Module { buttonText = "Admin Lightning Gun", method = Admin.LightningGun, toolTip = "Spawns lightning wherever your hand desires.", legal = true},
                new Module { buttonText = "Admin Lightning Aura", method = Admin.LightningAura, toolTip = "Spawns lightning wherever your hand desires.", legal = true},
                new Module { buttonText = "Admin Lightning Rain", method = Admin.LightningRain, toolTip = "Rains lightning around you and strikes whoever you hit.", legal = true},

                new Module { buttonText = $"Admin Laser {Bracket("green", "A")}", method = Admin.AdminLaser, toolTip = "Shines a red laser out of your hand when holding <color=green>A</color> or <color=green>X</color>.", legal = true},
                new Module { buttonText = $"Admin Beam {Bracket("green", "T")}", method = Admin.AdminBeam, toolTip = "Shines a rainbow spinning laser out of your head when holding <color=green>trigger</color>.", legal = true},
                new Module { buttonText = $"Admin Fractals {Bracket("green", "T")}", method = Admin.AdminFractals, toolTip = "Shines white lines out of your body when holding <color=green>trigger</color>.", legal = true},

                new Module { buttonText = "Admin Fear Gun", method = Admin.AdminFearGun, toolTip = "Sends a person into pure fear and scarefulness.", legal = true},
                new Module { buttonText = "Admin Object Gun", method = Admin.AdminObjectGun, toolTip = "Spawns an object wherever your hand desires.", legal = true},
                new Module { buttonText = "Admin Random Object Gun", method = Admin.AdminRandomObjectGun, toolTip = "Spawns a random object wherever your hand desires.", legal = true},

                new Module { buttonText = "NotifLabel", overlapText = "No Notification Set", label = true, legal = true},
                new Module { buttonText = "Set Notification", isTogglable = false, method = Admin.GetTargetNotification, toolTip = "Changes the notification text. The notification text is based off of what you type into the search bar.", legal = true},
                new Module { buttonText = "Admin Notify Self", isTogglable = false, method = Admin.NotifySelf, toolTip = "Sends a notification to yourself. The notification text is based off of what you type into the search bar.", legal = true},
                new Module { buttonText = "Admin Notify Gun", method = Admin.NotifyGun, toolTip = "Sends a notification to whoever your hand desires. The notification text is based off of what you type into the search bar.", legal = true},
                new Module { buttonText = "Admin Notify All", isTogglable = false, method = Admin.NotifyAll, toolTip = "Sends a notification to everyone using the menu. The notification text is based off of what you type into the search bar.", legal = true},

                new Module { buttonText = "Admin Join Gun", enableMethod = Admin.GetTargetNotification, method = Admin.JoinGun, toolTip = "Brings whoever your hand desires to a room. The room is based off of what you type into the search bar.", legal = true},
                new Module { buttonText = "Admin Join All", isTogglable = false, method = Admin.JoinAll, toolTip = "Brings everyone using the menu to a room. The room is based off of what you type into the search bar.", legal = true},
                new Module { buttonText = "Admin Network Scale", method = Admin.AdminNetworkScale, disableMethod = Admin.UnAdminNetworkScale, toolTip = "Networks your scale to others with the menu.", legal = true},

                new Module { buttonText = "Admin Confirm Notification", method = Admin.ConfirmNotifyAllUsing, isTogglable = false, toolTip = "Sends a notification to everyone using the menu confirming that you're an admin.", legal = true},

                new Module { buttonText = "Admin Levitate All", method = Admin.FlyAllUsing, toolTip = "Sends everyone using the menu flying away upwards.", legal = true},
                new Module { buttonText = "Admin Bouncy All", method = Admin.BouncyAllUsing, toolTip = "Makes everyone using the menu bouncy.", legal = true},
                new Module { buttonText = "Admin Bring Gun", method = Admin.AdminBringGun, toolTip = "Brings whoever your hand desires to you if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Bring All", method = Admin.BringAllUsing, toolTip = "Brings everyone using the menu to you.", legal = true},
                new Module { buttonText = "Admin Organize Gun", method = Admin.AdminOrganizeGun, toolTip = "Brings every menu user into a straight line.", legal = true},
                new Module { buttonText = "Admin Bring Hand All", method = Admin.BringHandAllUsing, toolTip = "Brings everyone using the menu to your hand.", legal = true},
                new Module { buttonText = "Admin Bring Head All", method = Admin.BringHeadAllUsing, toolTip = "Brings everyone using the menu to your head.", legal = true},
                new Module { buttonText = "Admin Orbit All", method = Admin.OrbitAllUsing, toolTip = "Makes everyone using the menu orbit you.", legal = true},

                new Module { buttonText = "Admin Lag Gun", method = Admin.AdminLagGun, toolTip = "Lags whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Lag All", method = Admin.AdminLagAll, toolTip = "Lags everyone using the menu.", legal = true},
                new Module { buttonText = "Admin Lag Spike Gun", method = Admin.AdminLagSpikeGun, toolTip = "Lag spikes whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Lag Spike All", method = Admin.AdminLagSpikeAll, isTogglable = false, toolTip = "Lag spikes everyone using the menu.", legal = true},

                new Module { buttonText = "Admin Vibrate Gun", method = Admin.AdminVibrateGun, toolTip = "Vibrate whoever your hand desires if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Vibrate All", method = Admin.AdminVibrateAll, isTogglable = false, toolTip = "Vibrates everyone using the menu.", legal = true},

                new Module { buttonText = "Admin Block Gun", method = Admin.AdminBlockGun, toolTip = "Disables whoever your hand desires from joining servers for 5 minutes if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Announce Block Gun", method =() => Admin.AdminABlockGun(false), toolTip = "Block gun, but it sends a notification to everyone using the menu that the target was blocked.", legal = true},
                new Module { buttonText = "Silent Announce Block Gun", method =() => Admin.AdminABlockGun(true), toolTip = "Block gun, but it sends a notification to everyone using the menu that the target was blocked. Hides your name.", legal = true},

                new Module { buttonText = "Admin Open Menu Gun", method =() => Admin.AdminButtonPressGun("lSecondary"), toolTip = "Force a player to open their menu.", legal = true},
                new Module { buttonText = "Admin Toggle Invis Gun", method =() => Admin.AdminButtonPressGun("rSecondary"), toolTip = "Force a player to toggle invisibility mod.", legal = true},

                new Module { buttonText = "Admin Punch Mod", method = Admin.AdminPunchMod, toolTip = "Flings people when you punch them if they're using the menu.", legal = true},
                new Module { buttonText = "Admin Find User", enableMethod =() => { Admin.EnableAdminMenuUserTags(); Admin.FindUserTime = Time.time; }, method = Admin.AdminFindUser, toolTip = "Joins publics until a menu user is found.", legal = true},

                new Module { buttonText = "No Admin Indicator", enableMethod = Admin.EnableNoAdminIndicator, method = Admin.NoAdminIndicator, disableMethod = Admin.AdminIndicatorBack, toolTip = "Disables the cone that appears above your head to others with the menu.", legal = true},
                new Module { buttonText = "Allow Kick Self", enableMethod =() => Console.allowKickSelf = true, disableMethod =() => Console.allowKickSelf = false, toolTip = "Lets other admins kick you.", legal = true},
                new Module { buttonText = "Disable Fling Self", enableMethod =() => Console.disableFlingSelf = true, disableMethod =() => Console.disableFlingSelf = false, toolTip = "Other admins can't fling you.", legal = true},

                new Module { buttonText = "Admin Platform Exclude Gun", method =() => Admin.AdminPlatToggleGun(true), toolTip = "Puts a player who is included for platform networking to be excluded.", legal = true},
                new Module { buttonText = "Admin Platform Include Gun", method =() => Admin.AdminPlatToggleGun(false), toolTip = "Puts a player who is excluded for platform networking to be included.", legal = true},

                new Module { buttonText = "Admin Dark Gun", method = Admin.DarkGun, toolTip = "Makes selected player's view dark.", legal = true},
                new Module { buttonText = "Admin Dark All", method = Admin.DarkAllUsing, isTogglable = false, toolTip = "Makes all using's view dark.", legal = true},

                new Module { buttonText = "Admin Light Gun", method = Admin.LightGun, toolTip = "Fix lighting of selected player.", legal = true},
                new Module { buttonText = "Admin Light All", method = Admin.LightAllUsing, isTogglable = false, toolTip = "Fix lighting of all using.", legal = true},

                new Module { buttonText = "Admin No Gravity Gun", method = Admin.ZeroGravityGun, toolTip = "Zero gravity for selected player.", legal = true},
                new Module { buttonText = "Admin No Gravity All", method = Admin.ZeroGravityAllUsing, isTogglable = false, toolTip = "Zero gravity for all using.", legal = true},

                new Module { buttonText = "Admin Low Gravity Gun", method = Admin.LowGravityGun, toolTip = "Low gravity for selected player.", legal = true},
                new Module { buttonText = "Admin Low Gravity All", method = Admin.LowGravityAllUsing, isTogglable = false, toolTip = "Low gravity for all using.", legal = true},

                new Module { buttonText = "Admin High Gravity Gun", method = Admin.HighGravityGun, toolTip = "High gravity for selected player.", legal = true},
                new Module { buttonText = "Admin High Gravity All", method = Admin.HighGravityAllUsing, isTogglable = false, toolTip = "High gravity for all using.", legal = true},

                new Module { buttonText = "Admin Reset Gravity Gun", method = Admin.ResetGravityGun, toolTip = "Fix gravity for selected player.", legal = true},
                new Module { buttonText = "Admin Reset Gravity All", method = Admin.ResetGravityAllUsing, isTogglable = false, toolTip = "Fix gravity for all using.", legal = true},

                new Module { buttonText = "Admin Creeper Gun", method = Admin.CreeperGun, toolTip = "Swaps hands of selected player for 5 seconds.", legal = true},
                new Module { buttonText = "Admin Creeper All", method = Admin.CreeperAllUsing, isTogglable = false, toolTip = "Swaps hands of all using for 5 seconds.", legal = true},

                new Module { buttonText = "Admin Earthquake Gun", method = Admin.EarthquakeGun, toolTip = "Shakes the view of selected player.", legal = true},
                new Module { buttonText = "Admin Earthquake All", method = Admin.EarthquakeAllUsing, isTogglable = false, toolTip = "Shakes the view of all using.", legal = true},

                new Module { buttonText = "Admin Drunk Gun", method = Admin.DrunkGun, toolTip = "Spins the camera of the selected player.", legal = true},
                new Module { buttonText = "Admin Drunk All", method = Admin.DrunkAllUsing, isTogglable = false, toolTip = "Spins the camera of all using.", legal = true},

                new Module { buttonText = "Admin Rendering Gun", method = Admin.WeirdGun, toolTip = "Makes selected players rendering tweak.", legal = true},
                new Module { buttonText = "Admin Rendering All", method = Admin.WeirdAllUsing, isTogglable = false, toolTip = "Makes all using rendering tweak.", legal = true},

                new Module { buttonText = "Admin World Off Gun", method = Admin.WorldOffGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin World Off All", method = Admin.WorldOffAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin World On Gun", method = Admin.WorldOnGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin World On All", method = Admin.WorldOnAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Spin Gun", method = Admin.SpinGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Spin All", method = Admin.SpinAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Invert Gun", method = Admin.InvertGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Invert All", method = Admin.InvertAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Bounce Gun", method = Admin.BounceGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Bounce All", method = Admin.BounceAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Rainbow Gun", method = Admin.RainbowGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Rainbow All", method = Admin.RainbowAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Flip Gun", method = Admin.FlipGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Flip All", method = Admin.FlipAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Zero-G Gun", method = Admin.ZeroGGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Zero-G All", method = Admin.ZeroGAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Super Jump Gun", method = Admin.SuperjumpGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Super Jump All", method = Admin.SuperjumpAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Blur Gun", method = Admin.BlurGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Blur All", method = Admin.BlurAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},


                new Module { buttonText = "Admin Tpose Gun", method = Admin.TposeGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Tpose All", method = Admin.TposeAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Headspin Gun", method = Admin.HeadspinGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Headspin All", method = Admin.HeadspinAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},


                new Module { buttonText = "Admin F&P Gun", method = Admin.FreezeandpullGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin F&P All", method = Admin.FreezeandpullAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Magnet Gun", method = Admin.MagnetGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Magnet All", method = Admin.MagnetAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},


                new Module { buttonText = "Admin Ragdoll Gun", method = Admin.RagdollGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Ragdoll All", method = Admin.RagdollAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Blackhole Gun", method = Admin.BlackholeGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Blackhole All", method = Admin.BlackholeAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},


                new Module { buttonText = "Admin Yeet Gun", method = Admin.YeetGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Yeet All", method = Admin.YeetAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin Lastmoment Gun", method = Admin.LastmomentGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Lastmoment All", method = Admin.LastmomentAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},


                new Module { buttonText = "Admin Stun Gun", method = Admin.StunGun, toolTip = "Disables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin Stun All", method = Admin.StunAllUsing, isTogglable = false, toolTip = "Disables all the Environment Objects for all using.", legal = true},

                new Module { buttonText = "Admin LSD Gun", method = Admin.LSDGun, toolTip = "Enables all the Environment Objects for that player.", legal = true},
                new Module { buttonText = "Admin LSD All", method = Admin.LSDAllUsing, isTogglable = false, toolTip = "Enables all the Environment Objects for all using.", legal = true},
            },

            ["Enabled Mods"] = new[]
            {
                new Module { buttonText = "Exit Enabled Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},
            },

            ["Internal Mods"] = new[]
            {
                new Module { buttonText = "Search", method = Modules.Categories.Settings.Search, isTogglable = false, toolTip = "Lets you search for specific mods.", legal = true},
                new Module { buttonText = "Global Return", method = Modules.Categories.Settings.GlobalReturn, isTogglable = false, toolTip = "Returns you to the previous category.", legal = true},
                new Module { buttonText = "Info Screen", method = Modules.Categories.Settings.Debug, enableMethod = Modules.Categories.Settings.ShowDebug, disableMethod = Modules.Categories.Settings.HideDebug, toolTip = "Shows game and modding related information.", legal = true},
                new Module { buttonText = "Donate Button", method =() => { Notifications.Clear(); acceptedDonations = true; File.WriteAllText($"{PluginInfo.BaseDirectory}/Qubit_HideDonationButton.txt", "true"); Prompt($"If you like this menu and would like to support, you should join our Patreon! ", () => Files.Start("https://patreon.com/Qubit")); }, isTogglable = false, toolTip = "An advertisement for Qubit's Patreon.", legal = true},
                new Module { buttonText = "Update Button", method =() => UpdatePrompt(), isTogglable = false, toolTip = "Prompts you to update the menu.", legal = true },

                new Module { buttonText = "Accept Prompt", method =() => { Notifications.Clear(); if (inTextInput) Modules.Categories.Settings.DestroyKeyboard(); CurrentPrompt.AcceptAction?.Invoke(); Modules.Categories.Settings.StopCurrentPrompt(); }, isTogglable = false, legal = true},
                new Module { buttonText = "Decline Prompt", method =() => { Notifications.Clear(); if (inTextInput) Modules.Categories.Settings.DestroyKeyboard(); CurrentPrompt.DeclineAction?.Invoke(); Modules.Categories.Settings.StopCurrentPrompt(); }, isTogglable = false, legal = true},

                new Module { buttonText = "Hide Qubit Clock", legal = true, isSetting = true, toolTip = "Hides the clock on the Qubit menu. Disable this setting to show it."},
                new Module { buttonText = "Hide Qubit Date", legal = true, isSetting = true, toolTip = "Hides the date on the Qubit menu. Disable this setting to show it."},
                new Module { buttonText = "Qubit Latency", enabled = true, legal = true, isSetting = true, toolTip = "Shows network latency on the Qubit menu."}
            },

            ["Sound Library"] = new[]
            {
                new Module { buttonText = "Exit Sound Library", method =() => Sound.LoadSoundboard(), isTogglable = false, toolTip = "Returns you back to the soundboard.", legal = true}
            },

            ["Experimental Mods"] = new[]
            {
                new Module { buttonText = "Exit Experimental Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Safe Restart Game", enableMethod =() => { Experimental.restartDelay = Time.time; Experimental.restartIndex = 0; }, method = Experimental.SafeRestartGame, toolTip = "Restarts Gorilla Tag, saving room and position data.", legal = true},

                new Module { buttonText = "Fix Broken Buttons", method = Experimental.FixDuplicateButtons, isTogglable = false, toolTip = "Fixes any duplicate or broken buttons.", legal = true},

                new Module { buttonText = "Get Sound Data", method = Experimental.DumpSoundData, isTogglable = false, toolTip = "Dumps the hand tap sounds to a file.", legal = true},
                new Module { buttonText = "Get Cosmetic Data", method = Cosmetics.DumpCosmeticData, isTogglable = false, toolTip = "Dumps the cosmetics and their data to a file.", legal = true},
                new Module { buttonText = "Get Decryptable Cosmetic Data", method = Cosmetics.DecryptableCosmeticData, isTogglable = false, toolTip = "Dumps the cosmetics and their data to a easily decryptable file for databases.", legal = true},
                new Module { buttonText = "Get RPC Data", method = Experimental.DumpRPCData, isTogglable = false, toolTip = "Dumps the data of every RPC to a file.", legal = true},

                new Module { buttonText = "Copy Custom Gamemode Script", method = Experimental.CopyCustomGamemodeScript, isTogglable = false, toolTip = "Copies the Lua script source code of the current custom map being played."},
                new Module { buttonText = "Copy Custom Map ID", method = Experimental.CopyCustomMapID, isTogglable = false, toolTip = "Copies the map ID of the current custom map being played."},

                new Module { buttonText = "Better FPS Boost", enableMethod = Experimental.BetterFPSBoost, disableMethod = Experimental.DisableBetterFPSBoost, toolTip = "Makes everything one color, boosting your FPS."},

                
                new Module { buttonText = "Disorganize Menu", method = Experimental.DisorganizeMenu, isTogglable = false, toolTip = "Disorganizes the entire menu. This cannot be undone."},
            },

            ["Safety Settings"] = new[]
            {
                new Module { buttonText = "Exit Safety Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                ButtonHelper.Create("Change Anti Report Distance", () => Safety.AntiReportRangeNames, Safety.antiReportRangeIndex, Safety.ApplyAntiReportRange, "Changes the distance threshold for the anti report mods."),
                ButtonHelper.CreateNumeric("Change FPS Spoof Value", 1, 120, 90, v => PlayerStats.FPS = (short)v, toolTip: "Changes the target FPS for the FPS Spoof mod."),
                ButtonHelper.CreateNumeric("Change Target FPS Spoof Value", 1, 120, 90, v => PlayerStats.TargetFPS = (short)v, toolTip: "Changes the target FPS used by the FPS Spoof mod."),
                ButtonHelper.CreateNumeric("Change Ping Spoof Value", short.MinValue, short.MaxValue, 60, v => PlayerStats.Ping = (short)v, toolTip: "Changes the target ping for the Ping Spoof mod."),


                new Module { buttonText = "Hide Anti Cheat Report Reasons", enableMethod =() => SendReport.AntiCheatReasonHide = true, disableMethod =() => SendReport.AntiCheatReasonHide = false, toolTip = "Hides the reason for Show Anti Cheat Reports."},

                new Module { buttonText = "Visualize Anti Report", method = Safety.VisualizeAntiReport, toolTip = "Visualizes the distance threshold for the anti report mods."},
                new Module { buttonText = "Smart Anti Report", enableMethod = Safety.EnableSmartAntiReport, disableMethod = Safety.DisableSmartAntiReport, toolTip = "Makes the anti report mods only activate in non-modded public lobbies."},
                new Module { buttonText = "Anti Mute", enableMethod =() => Safety.antiMute = true, disableMethod =() => Safety.antiMute = false, toolTip = "Includes the mute button with the anti report mods." }
            },

            ["Temporary Category"] = new Module[] { },

            ["Soundboard Settings"] = new[]
            {
                new Module { buttonText = "Exit Soundboard Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Legacy Soundboard", enableMethod =() => { Soundboard.LegacySoundboard = true; Soundboard.CachedButtons.Clear(); } , disableMethod =() => { Soundboard.LegacySoundboard = false; Soundboard.CachedButtons.Clear(); }, toolTip = "Enables the legacy soundboard system."},
                new Module { buttonText = "Loop Sounds", enableMethod =() => Soundboard.LoopAudio = true, disableMethod =() => Soundboard.LoopAudio = false, toolTip = "Makes sounds loop forever until stopped."},
                new Module { buttonText = "Overlap Sounds", enableMethod =() => Soundboard.OverlapAudio = true, disableMethod =() => Soundboard.OverlapAudio = false, toolTip = "Makes it so you can play sounds over and over again, making them overlap eachother."},
                ButtonHelper.Create("Sound Bindings", () => Sound.BindModeNames, Sound.BindMode, Sound.ApplySoundBindings, "Changes the button used to play sounds on the soundboard."),
                ButtonHelper.CreateNumeric("Change Soundboard Volume", 0, 100, Mathf.RoundToInt(Fun.soundboardVolumeIndex / 0.05f), Fun.ApplySoundboardVolume, v => (v * 0.05f).ToString(), "Changes the volume of your sounds.", overlapText: "Change Default Soundboard Volume"),

                ButtonHelper.CreateNumeric("Change Soundboard Speed", 0, 100, Mathf.RoundToInt(Fun.soundboardSpeedIndex / 0.05f), Fun.ApplySoundboardSpeed, v => (v * 0.05f).ToString(), "Changes the speed of your sounds.", overlapText: "Change Default Soundboard Speed"),
                new Module { buttonText = "Disable Local Soundboard", enableMethod =() => Soundboard.disableLocalSoundboard = true, disableMethod =()=> Soundboard.disableLocalSoundboard = false, toolTip = "Decides if you should hear your own soundboards or not." },
            },

            ["Overpowered Settings"] = new[]
            {
                new Module { buttonText = "Exit Overpowered Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu."},

                new Module { buttonText = "Graphic Punch Mod", toolTip = "Spawns blood projectiles when hitting other players with the allowed punch mods."},

                
                

                ButtonHelper.Create("Change Lag Power", () => Overpowered.LagPowerNames, Overpowered.lagIndex, Overpowered.ApplyLagPower, "Changes the power of the lag mods."),
                ButtonHelper.Create("Change Lag Type", () => Overpowered.LagTypeNames, Overpowered.lagTypeIndex, Overpowered.ApplyLagType, "Changes the method used to lag players."),

                ButtonHelper.Create("Master Visualization Type", () => Overpowered.VisualizationTypeNames, Overpowered.masterVisualizationType, Overpowered.ApplyMasterVisualizationType, "Changes the indicator placed on the master client for mods that show one."),

                new Module { buttonText = "Unlock on Crash", toolTip = "Unlocks the room when crashing someone. This makes the mod more powerful." },
                new Module { buttonText = "Kick to Public", enableMethod =() => Overpowered.kickToPublic = true, disableMethod =() => Overpowered.kickToPublic = false, toolTip = "Makes the kick mods send the user to a public lobby. This allows for chaining of commands." },
                new Module { buttonText = "Kick to Specific Room", enableMethod = Overpowered.KickToSpecificRoom, disableMethod =() => Overpowered.specificRoom = null, toolTip = "Makes the kick mods send the user to the specific room of your choice." },
                new Module { buttonText = "Rejoin on Kick", enableMethod =() => Overpowered.rejoinOnKick = true, disableMethod =() => Overpowered.rejoinOnKick = false, toolTip = "Makes room based kick mods join the room you kicked the target in once they have been kicked." },
                new Module { buttonText = "Fast Kick", enableMethod =() => Overpowered.instantCreate = true, disableMethod =() => Overpowered.instantCreate = false, toolTip = "Instantly creates a room instead of checking if one already exists." },
                new Module { buttonText = "Kick Fix", enableMethod =() => EntityJoining.enabled = true, disableMethod =() => EntityJoining.enabled = false, toolTip = "Stops the super infection, virtual stump, and other kick mods from breaking." },

                
            },

            ["Keybind Settings"] = new[]
            {
                new Module { buttonText = "Exit Keybind Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Non-Toggle Keybinds", enableMethod =() => ToggleBindings = false, disableMethod =() => ToggleBindings = true, toolTip = "Enables mods while holding down the button, instead of toggling them.", legal = true},
                new Module { buttonText = "Overwrite Keybinds", enableMethod =() => OverwriteKeybinds = true, disableMethod =() => OverwriteKeybinds = false, toolTip = "Forces every button to be held down with keybinded mods.", legal = true},
                new Module { buttonText = "Clear All Keybinds", method = Modules.Categories.Settings.ClearAllKeybinds, isTogglable = false, toolTip = "Enables mods while holding down the button, instead of toggling them.", legal = true},

                new Module { buttonText = "Keybind A", enableMethod =() => Modules.Categories.Settings.StartBind("A"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind B", enableMethod =() => Modules.Categories.Settings.StartBind("B"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind X", enableMethod =() => Modules.Categories.Settings.StartBind("X"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Y", enableMethod =() => Modules.Categories.Settings.StartBind("Y"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Left Grip", enableMethod =() => Modules.Categories.Settings.StartBind("LG"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Right Grip", enableMethod =() => Modules.Categories.Settings.StartBind("RG"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Left Trigger", enableMethod =() => Modules.Categories.Settings.StartBind("LT"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Right Trigger", enableMethod =() => Modules.Categories.Settings.StartBind("RT"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Left Joystick", enableMethod =() => Modules.Categories.Settings.StartBind("LJ"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
                new Module { buttonText = "Keybind Right Joystick", enableMethod =() => Modules.Categories.Settings.StartBind("RJ"), disableMethod =() => IsBinding = false, toolTip = "Enables binding mode, letting you bind a mod to a button.", legal = true},
            },

            ["Plugin Settings"] = new[]
            {
                new Module { buttonText = "Exit Plugin Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},
                new Module { buttonText = "Reload Plugins", method = Plugins.ReloadPlugins, isTogglable = false, toolTip = "Reloads all of your plugins.", legal = true}
            },

            ["Friends"] = new[]
            {
                new Module { buttonText = "Exit Friends", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},
                new Module { buttonText = "Loading...", label = true, legal = true},
            },

            ["Friend Settings"] = new[]
            {
                new Module { buttonText = "Exit Friend Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                new Module { buttonText = "Disable Rig Networking", enableMethod =() => Friends.RigNetworking = false, disableMethod =() => Friends.RigNetworking = true, toolTip = "Disables the networking between friends when your rig is disabled.", legal = true},
                new Module { buttonText = "Disable Platform Networking", enableMethod =() => Friends.PlatformNetworking = false, disableMethod =() => Friends.PlatformNetworking = true, toolTip = "Disables the platform networking between friends.", legal = true},
                new Module { buttonText = "Disable Pinging", enableMethod =() => Friends.Pinging = false, disableMethod =() => Friends.Pinging = true, toolTip = "Disables the pinging feature between friends.", legal = true},
                new Module { buttonText = "Disable Messaging", enableMethod =() => Friends.Messaging = false, disableMethod =() => Friends.Messaging = true, toolTip = "Disables the message feature between friends.", legal = true},
                new Module { buttonText = "Disable Friend Sounds", enableMethod =() => Friends.SoundEffects = false, disableMethod =() => Friends.SoundEffects = true, toolTip = "Disables the sound effects in the friend system.", legal = true},

               new Module { buttonText = "Disable Invite Notifications", enableMethod =() => Friends.InviteNotifications = false, disableMethod =() => Friends.InviteNotifications = true, toolTip = "Disables the prompt and notification when getting an invite from a friend.", legal = true},
                new Module { buttonText = "Disable Preference Sharing", enableMethod =() => Friends.PreferenceSharing = false, disableMethod =() => Friends.PreferenceSharing = true, toolTip = "Disables the prompt and notification when a friend shares their preferences with you.", legal = true},
                new Module { buttonText = "Physical Platforms", enableMethod =() => Friends.PhysicalPlatforms = true, disableMethod =() => Friends.PhysicalPlatforms = false, toolTip = "Allows networked platforms to be collided with between friends.", legal = true},
            },

            ["Fun Settings"] = new[]
            {
                new Module { buttonText = "Exit Fun Settings", method =() => CurrentCategoryName = "Settings", isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true},

                ButtonHelper.Create("Change Head Spin Speed", () => Fun.HeadSpinSpeedNames, Fun.headSpinIndex, Fun.ApplyHeadSpinSpeed, "Changes the speed of the head spin mods."),
                ButtonHelper.CreateNumeric("Change Tinnitus Hertz", 0, 7, (Movement.targetHz - 4000) / 500, Movement.ApplyTinnitusHz, v => (4000 + v * 500).ToString(), "Changes the target hertz for the tinnitus mods."),

                new Module { buttonText = "Zero Gravity Bugs", toolTip = "Removes the gravity from the bugs on the Bug Spam mod."},
                new Module { buttonText = "Bug Colliders", toolTip = "Gives the bug colliders on the Bug Spam mod."},
                new Module { buttonText = "Bouncy Bug", toolTip = "Makes the bug bounce off of surfaces if using the bug colliders setting on the Bug Spam mod."},

                ButtonHelper.CreateNumeric("Change Custom Quest Score", 0, 99999, Fun.targetQuestScore, Fun.ApplyCustomQuestScore, v => v.ToString(), "Changes the value of the \"Custom Quest Score\" mod."),

                ButtonHelper.CreateNumeric("Change Ranked Tier", 0, 2, Safety.rankIndex, Safety.ApplyRankedTier, v => ((RankedProgressionManager.ERankedMatchmakingTier)v).ToString(), "Changes the target tier for the matchmaking spoof mod.", overlapText: "Change Matchmaking Tier"),
                ButtonHelper.CreateNumeric("Change ELO Value", 0, 4000, 4000, elo => Safety.targetElo = elo, v => v.ToString(), "Changes the target ELO for the badge spoof mod."),
                ButtonHelper.Create("Change Badge Tier", () => Safety.BadgeNames, Safety.targetBadge, Safety.ApplyBadgeTier, "Changes the target tier for the badge spoof mod."),

                ButtonHelper.CreateNumeric("Change Target FOV", 0, 4, 2, Fun.ApplyTargetFOV, v => (70 + (v * 10)).ToString(), "Changes the target field of view for the \"Camera FOV\" mod.", legal: true),
                ButtonHelper.CreateNumeric("Knockback Multiplication Amount", 0, 1280, Movement.multiplicationAmount, Movement.ApplyMultiplicationAmount, v => (v / 10f).ToString(), "Adjusts how much your knockback is multiplied."),

                new Module { buttonText = "Zero Gravity Blocks", toolTip = "Removes the gravity from the blocks."},
                new Module { buttonText = "Random Block Type", toolTip = "Selects a random block when using block mods."},

                new Module { buttonText = "No Random Position Grab", toolTip = "Disables the position randomization in the \"Grab All ### Blocks\" mods."},
                new Module { buttonText = "No Random Rotation Grab", toolTip = "Disables the rotation randomization in the \"Grab All ### Blocks\" mods."},

                ButtonHelper.CreateNumeric("Change Block Delay", 1, 20, Blocks.blockDebounceIndex, Blocks.ApplyBlockDelay, v => (v / 20f).ToString(), "Gives the blocks a delay before spawning."),
                ButtonHelper.CreateNumeric("Change Cycle Delay", 1, 4, Fun.cycleSpeedIndex, Fun.ApplyCycleDelay, v => (v / 2f).ToString(), "Changes the delay on name cycle mods.", overlapText: "Change Name Cycle Delay"),

                new Module { buttonText = "Entity Gravity", toolTip = "Gives gravity to any spawned entities in the ghost reactor or Super Infection gamemode."},

                new Module { buttonText = "Tinnitus Self", enableMethod =() => Fun.tinnitusSelf = true, disableMethod =() => Fun.tinnitusSelf = false, toolTip = "Be able to hear the loud beep the menu creates with this mod on. God save your ears."},
            },

            ["Players"] = new[]
            {
                new Module { buttonText = "Exit Players", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page." }
            },

            ["Credits"] = new[]
            {
                new Module { buttonText = "Exit Credits", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Twig", method =() => Files.Start("https://github.com/Twigcore"), isTogglable = false, toolTip = "Twig is the current owner of Qubit, previously a developer of Qubit." },
                new Module { buttonText = "Kingofnetflix", isTogglable = false, toolTip = "Kingofnetflix is the creator of <b>Qubit (Menu)</b>, and was a developer for ii's <b>Stupid</b> Menu. Quit modding and the modding community August 2026." },
                new Module { buttonText = "iiDk", method =() => Files.Start("https://tianjo.cc/iidk"), isTogglable = false, toolTip = "iiDk is the creator of ii's <b>Stupid</b> Menu (which this menu is forked from), and was working on it since 2023. He was also the owner of ii's Stupid Mods. Both have shut down ever since 2/24/2026.", legal = true},
                new Module { buttonText = "Twigcore", method =() => Files.Start("https://github.com/Twigcore"), isTogglable = false, toolTip = "Twigcore is one of the main owners of Console, the admin system in the menu. He helps with asset ideas, moderate users, contributed to the menu, and much more."},

                new Module { buttonText = "Cha554", method =() => Files.Start("https://github.com/Cha554"), isTogglable = false, toolTip = "Cha554 helped me make the menu work, and develop it."},
                new Module { buttonText = "Ghydric", method =() => Files.Start("https://github.com/klintevej"), isTogglable = false, toolTip = "Klintevej is the administrator of the Discord bot and admin server."},

                new Module { buttonText = "Joseph", method =() => Files.Start("https://github.com/josephabyt"), isTogglable = false, toolTip = "Joseph has contributed to ii's <b>Stupid</b> Menu. He is the creator of many mods, like the debug screen, extenders, disable menu title, steam refund timer, and many more."},
                new Module { buttonText = "DrPerky", method =() => Files.Start("https://github.com/DrPerkyLegit"), isTogglable = false, toolTip = "DrPerky has contributed to ii's <b>Stupid</b> Menu. He helped me rewrite all of the visual mods."},
                new Module { buttonText = "ShibaGT", method =() => Files.Start("https://github.com/ShibaGT"), isTogglable = false, toolTip = "ShibaGT has contributed to ii's <b>Stupid</b> Menu. He gave me a coroutine manager, and created minor things for the menu."},
                new Module { buttonText = "TestofficialXD", method =() => Files.Start("https://github.com/TestofficialXD"), isTogglable = false, toolTip = "TestofficialXD has contributed to ii's <b>Stupid</b> Menu. He wrote the initial critter mods, and gave me the idea for Tag Sounds."},
                new Module { buttonText = "Leetus", method =() => Files.Start("https://github.com/leetus"), isTogglable = false, toolTip = "Leetus has contributed to ii's <b>Stupid</b> Menu. He made minor optimizations to mods in the menu."},

                new Module { buttonText = "Graze", method =() => Files.Start("https://github.com/The-Graze"), isTogglable = false, toolTip = "Graze gave me permission to use their color detection system and Media Control buttons."},
                new Module { buttonText = "Zvbex", method =() => Files.Start("https://guns.lol/zvbexisking"), isTogglable = false, toolTip = "Zvbex gave me permission to use their initial platform detection system."},
                new Module { buttonText = "Shiny", method =() => Files.Start("https://github.com/Shiny003"), isTogglable = false, toolTip = "Shiny gave me permission to use their PlayFab display name spoof patch."},
                new Module { buttonText = "Will", method =() => Files.Start("https://github.com/64will64"), isTogglable = false, toolTip = "64Will64 gave me the idea to make body rotation mods and the Low FPS Movement mod."},
                new Module { buttonText = "KyleTheScientist", method =() => Files.Start("https://github.com/KyleTheScientist"), isTogglable = false, toolTip = "KyleTheScientist gave me the idea to add \"Bark Fly\" to the menu and helped me create and use asset bundles.", legal = true},
                new Module { buttonText = "Gorilla Dev", method =() => Files.Start("https://github.com/GorillerDev"), isTogglable = false, toolTip = $"Gorilla Dev gave me the idea to add \"Anti Report {Bracket("green", "Oculus")}\" to the menu."},
                new Module { buttonText = "EyeCantSee", method =() => Files.Start("https://github.com/charlottebutson-pixel"), isTogglable = false, toolTip = "EyeCantSee has pushed minor optimizations and features to the menu."},

                new Module { buttonText = "GPL v3", method =() => Files.Start("https://www.gnu.org/licenses/gpl-3.0.html"), isTogglable = false, toolTip = "The GNU General Public License Version 3 is the license that my menu uses. It proveides a \"free, copyleft license for software and other kinds of works.\"", legal = true},
            },

            ["Custom Map Mods"] = new[]
            {
                new Module { buttonText = "Exit Custom Map Mods", method =() => CurrentCategoryName = "Fun Mods", isTogglable = false, toolTip = "Returns you back to the fun mods.", legal = true},
                new Module { buttonText = "You have not loaded a map.", label = true }
            },

            ["Mod Givers"] = new[]
            {
                new Module { buttonText = "Exit Admin Mod Givers", method =() => CurrentCategoryName = "Admin Mods", isTogglable = false, toolTip = "Returns you back to the Admin mods.", legal = true},
                new Module { buttonText = "Give Fly Gun", method = Admin.AdminGiveFlyGun, toolTip = "Gives whoever you want fly when they hold their right thumb down if they're using console."},
                new Module { buttonText = "Give Trigger Fly Gun", method = Admin.AdminGiveTriggerFlyGun, toolTip = "Gives whoever you want fly when they hold their trigger down if they're using console."},
                new Module { buttonText = "Give Speed Boost Gun", method = Admin.AdminGiveSpeedGun, toolTip = "Gives whoever you want speed boost if they're using console."},
                new Module { buttonText = "Give Low Gravity Gun", method = Admin.AdminGiveLowGravity, toolTip = "Gives whoever you want low gravity if they're using console."},
                new Module { buttonText = "Give Platforms Gun", method = Admin.AdminGivePlatforms, toolTip = "Gives whoever you want platforms if they're using console."},
            },

            ["Chat Messages"] = new Module[] { },

            ["Macros"] = new[]
            {
                new Module { buttonText = "Exit Macros", method =() => CurrentCategoryName = "Movement Mods", isTogglable = false, toolTip = "Returns you back to the movement mods.", legal = true},
                new Module { buttonText = $"Record {Bracket("green", "LT")}", method = Movement.RecordMacro, toolTip = "Record your macros with your <color=green>left trigger</color>." },
                new Module { buttonText = "Reload Macros", method = Movement.LoadMacros, isTogglable = false, toolTip = "Reloads your macros." },
            },

            ["Detected Mods"] = new[]
            {
                new Module { buttonText = "Exit Detected Mods", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Detected Lag Gun", overlapText = "Lag Gun", method = Detected.LagGun, detected = true, toolTip = "Lags whoever your hand desires."},
                new Module { buttonText = "Detected Lag All", overlapText = "Lag All", method = Detected.LagAll, detected = true, toolTip = "Lags everyone in the room."},
                new Module { buttonText = "Detected Lag Aura", overlapText = "Lag Aura", method = Detected.LagAura, detected = true, toolTip = "Lags players nearby you."},
                new Module { buttonText = "Detected Lag On Touch", overlapText = "Lag On Touch", method = Detected.LagOnTouch, detected = true, toolTip = "Lags players that you touch."},

                new Module { buttonText = "Detected Quarantine Gun", overlapText = "Quarantine Gun", method = Detected.QuarantineGun, detected = true, toolTip = "Quarantine whoever your hand desires' game."},
                new Module { buttonText = "Detected Quarantine All", overlapText = "Quarantine All", method = Detected.QuarantineAll, detected = true, toolTip = "Quarantine everyone in the room."},
                new Module { buttonText = "Detected Quarantine Aura", overlapText = "Quarantine Aura", method = Detected.QuarantineAura, detected = true, toolTip = "Mess up everyone in the room."},
                new Module { buttonText = "Detected Quarantine On Touch", overlapText = "Quarantine On Touch", method = Detected.QuarantineOnTouch, detected = true, toolTip = "Mess up everyone in the room."},
                new Module { buttonText = "Detected Leaderboard Quarantine", overlapText = "Leaderboard Quarantine", method = Detected.LeaderboardQuarantine, disableMethod =() => Detected.RevertLeaderboard("QUARANTINE"), detected = true, toolTip = "Quarantines players when you report them on the leaderboard."},

                new Module { buttonText = "Detected Mute Gun", overlapText = "Mute Gun", method = Detected.MuteGun, detected = true, toolTip = "Mutes whoever your hand desires."},
                new Module { buttonText = "Detected Mute All", overlapText = "Mute All", method = Detected.MuteAll, detected = true, toolTip = "Mutes everyone in the room."},
                new Module { buttonText = "Detected Mute Aura", overlapText = "Mute Aura", method = Detected.MuteAura, detected = true, toolTip = "Mutes players nearby you."},
                new Module { buttonText = "Detected Mute On Touch", overlapText = "Mute On Touch", method = Detected.MuteOnTouch, detected = true, toolTip = "Mutes players that you touch."},
                new Module { buttonText = "Detected Leaderboard Mute", overlapText = "Leaderboard Mute", method = Detected.LeaderboardMute, detected = true, toolTip = "Mutes players globally when you mute them on the leaderboard."},

                new Module { buttonText = "Detected Ghost Gun", overlapText = "Ghost Gun", method = Detected.GhostGun, detected = true, toolTip = "Freezes whoever your hand desires, making them a ghost."},
                new Module { buttonText = "Detected Ghost All", overlapText = "Ghost All", method = Detected.GhostAll, isTogglable = false, detected = true, toolTip = "Freezes everyone, making them a ghost."},
                new Module { buttonText = "Detected Ghost Aura", overlapText = "Ghost Aura", method = Detected.GhostAura, isTogglable = true, detected = true, toolTip = "Freezes nearby players, making them a ghost."},
                new Module { buttonText = "Detected Ghost On Touch", overlapText = "Ghost On Touch", method = Detected.GhostOnTouch, detected = true, toolTip = "Freezes players you touch, making them a ghost."},
                new Module { buttonText = "Detected Leaderboard Ghost", overlapText = "Leaderboard Ghost", method = Detected.LeaderboardGhost, disableMethod =() => Detected.RevertLeaderboard("GHOST"), detected = true, toolTip = "Ghosts players when you report them on the leaderboard."},

                new Module { buttonText = "Detected Unghost Gun", overlapText = "Unghost Gun", method = Detected.UnghostGun, detected = true, toolTip = "Unfreezes whoever your hand desires, making them no longer a ghost."},
                new Module { buttonText = "Detected Unghost All", overlapText = "Unghost All", method = Detected.UnghostAll, isTogglable = false, detected = true, toolTip = "Unfreezes everyone, making them no longer a ghost."},
                new Module { buttonText = "Detected Unghost Aura", overlapText = "Unghost Aura", method = Detected.UnghostAura, isTogglable = true, detected = true, toolTip = "Unfreezes players nearby you, making them no longer a ghost."},
                new Module { buttonText = "Detected Unghost On Touch", overlapText = "Unghost On Touch", method = Detected.UnghostOnTouch, detected = true, toolTip = "Unfreeze players that you touch, making them no longer a ghost."},

                new Module { buttonText = "Detected Spam Ghost Gun", overlapText = "Spam Ghost Gun", method =() => { Detected.GhostGun(); Detected.UnghostGun(); }, detected = true, toolTip = "Spam makes whoever your hand desires freeze and unfreeze again. Ghost and Unghost."},
                new Module { buttonText = "Detected Spam Ghost All", overlapText = "Spam Ghost All", method =() => { Detected.GhostAll(); Detected.UnghostAll(); }, isTogglable = false, detected = true, toolTip = "Spam makes everyone freeze and unfreeze again. Ghost and Unghost."},
                new Module { buttonText = "Detected Spam Ghost Aura", overlapText = "Spam Ghost Aura", method =() => { Detected.GhostAura(); Detected.UnghostAura(); }, isTogglable = true, detected = true, toolTip = "Spam makes players nearby freeze and unfreeze again. Ghost and Unghost."},
                new Module { buttonText = "Detected Spam Ghost On Touch", overlapText = "Spam Ghost On Touch", method =() => { Detected.GhostOnTouch(); Detected.UnghostOnTouch(); }, detected = true, toolTip = "Spam makes players you touch freeze and unfreeze again. Ghost and Unghost."},


                new Module { buttonText = "Detected Isolate Gun", overlapText = "Isolate Gun", method = Detected.IsolateGun, detected = true, toolTip = "Makes whoever your hand desires only be able to see you."},
                new Module { buttonText = "Detected Isolate All", overlapText = "Isolate All", method = Detected.IsolateAll, isTogglable = false, detected = true, toolTip = "Makes everyone only be able to see you."},
                new Module { buttonText = "Detected Isolate Aura", overlapText = "Isolate Aura", method = Detected.IsolateAura, detected = true, toolTip = "Makes players nearby only be able to see you."},
                new Module { buttonText = "Detected Isolate On Touch", overlapText = "Isolate On Touch", method = Detected.IsolateOnTouch, detected = true, toolTip = "Players that you touch will only be able to see you."},

                new Module { buttonText = "Detected Change Name Gun", overlapText = "Change Name Gun", enableMethod = Detected.PromptNameChange, method = Detected.ChangeNameGun, detected = true, toolTip = "Changes the name of whoever your hand desires."},
                new Module { buttonText = "Detected Change Name All", overlapText = "Change Name All", enableMethod = Detected.PromptNameChange, method = Detected.ChangeNameAll, detected = true, toolTip = "Changes the name of everyone in the room."},
                new Module { buttonText = "Detected Change Name Aura", overlapText = "Change Name Aura", enableMethod = Detected.PromptNameChange, method = Detected.ChangeNameAura, detected = true, toolTip = "Changes the name of whoever is near you."},
                new Module { buttonText = "Detected Change Name On Touch", overlapText = "Change Name On Touch", enableMethod = Detected.PromptNameChange, method = Detected.ChangeNameOnTouch, detected = true, toolTip = "Changes the name of players that you touch."},

                new Module { buttonText = "Detected Ban Gun", overlapText = "Ban Gun", method = Detected.BanGun, detected = true, toolTip = "Changes the name of whoever your hand desires to a banned name."},
                new Module { buttonText = "Detected Ban All", overlapText = "Ban All", method = Detected.BanGun, detected = true, toolTip = "Changes the name of everyone in the room to a banned name."},

                new Module { buttonText = "Bypass Mod Checkers Gun", method = Detected.BypassModCheckersGun, detected = true, toolTip = "Tells players using mod checkers that whoever your hand desires has no mods."},
                new Module { buttonText = "Bypass Mod Checkers All", method = Detected.BypassModCheckersAll, isTogglable = false, detected = true, toolTip = "Tells players using mod checkers that no one has no mods."},
                new Module { buttonText = "Bypass Mod Checkers Aura", method = Detected.BypassModCheckersAura, detected = true, toolTip = "Tells players using mod checkers that players nearby you have no mods."},
                new Module { buttonText = "Bypass Mod Checkers On Touch", method = Detected.BypassModCheckersOnTouch, detected = true, toolTip = "Tells players using mod checkers that players you touch have no mods."},

                new Module { buttonText = "Break Mod Checkers Gun", method = Detected.BreakModCheckersGun, detected = true, toolTip = "Tells players using mod checkers that whoever your hand desires has every mod."},
                new Module { buttonText = "Break Mod Checkers All", method = Detected.BreakModCheckersAll, isTogglable = false, detected = true, toolTip = "Tells players using mod checkers that everyone has every mod."},
                new Module { buttonText = "Break Mod Checkers Aura", method = Detected.BreakModCheckersAura, detected = true, toolTip = "Tells players using mod checkers that players nearby you have every mod."},
                new Module { buttonText = "Break Mod Checkers On Touch", method = Detected.BreakModCheckersOnTouch, detected = true, toolTip = "Tells players using mod checkers that players you touch have every mod."},

                new Module { buttonText = "Gamemode Include Gun", method = Detected.GamemodeIncludeGun, detected = true, toolTip = "Includes whoever your hand desires from the current gamemode."},
                new Module { buttonText = "Gamemode Include All", method = Detected.GamemodeIncludeAll, isTogglable = false, detected = true, toolTip = "Includes everyone from the current gamemode."},
                new Module { buttonText = "Gamemode Include Aura", method = Detected.GamemodeIncludeAura, detected = true, toolTip = "Includes players nearby you from the current gamemode."},
                new Module { buttonText = "Gamemode Include On Touch", method = Detected.GamemodeIncludeOnTouch, detected = true, toolTip = "Includes players you touch from the current gamemode."},

                new Module { buttonText = "Gamemode Exclude Gun", method = Detected.GamemodeExcludeGun, detected = true, toolTip = "Excludes whoever your hand desires from the current gamemode."},
                new Module { buttonText = "Gamemode Exclude All", method = Detected.GamemodeExcludeAll, isTogglable = false, detected = true, toolTip = "Excludes everyone from the current gamemode."},
                new Module { buttonText = "Gamemode Exclude Aura", method = Detected.GamemodeExcludeAura, detected = true, toolTip = "Excludes players nearby you from the current gamemode."},
                new Module { buttonText = "Gamemode Exclude On Touch", method = Detected.GamemodeExcludeOnTouch, detected = true, toolTip = "Excludes players you touch from the current gamemode."},

                new Module { buttonText = "Break Network Triggers", method = Detected.BreakNetworkTriggers, isTogglable = false, detected = true, toolTip = "Breaks the network triggers."},
                new Module { buttonText = "Kick Network Triggers", method = Detected.KickNetworkTriggers, isTogglable = false, detected = true, toolTip = "Makes all network triggers kick you."},

                new Module { buttonText = "Spaz Gamemode", method = Detected.SpazGamemode, detected = true, toolTip = "Rapidly changes the gamemode."},
                new Module { buttonText = "Break Gamemode", enableMethod =() => Detected.BreakGamemode(true), disableMethod =() => Detected.BreakGamemode(false), detected = true, toolTip = "Breaks the current gamemode."},
                new Module { buttonText = "Change Gamemode to None", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.None), isTogglable = false, detected = true, toolTip = "Changes the gamemode to error/none."},
                new Module { buttonText = "Change Gamemode to Count", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Count), isTogglable = false, detected = true, toolTip = "Changes the gamemode to count."},
                new Module { buttonText = "Change Gamemode to Casual", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Casual), isTogglable = false, detected = true, toolTip = "Changes the gamemode to casual."},
                new Module { buttonText = "Change Gamemode to Infection", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Infection), isTogglable = false, detected = true, toolTip = "Changes the gamemode to infection."},
                new Module { buttonText = "Change Gamemode to Competitive Infection", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.InfectionCompetitive), isTogglable = false, detected = true, toolTip = "Changes the gamemode to competitive infection."},
                new Module { buttonText = "Change Gamemode to Super Infection", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.SuperInfect), isTogglable = false, detected = true, toolTip = "Changes the gamemode to super infection."},
                new Module { buttonText = "Change Gamemode to Super Casual", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.SuperCasual), isTogglable = false, detected = true, toolTip = "Changes the gamemode to super casual."},
                new Module { buttonText = "Change Gamemode to Hunt", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.HuntDown), isTogglable = false, detected = true, toolTip = "Changes the gamemode to hunt."},
                new Module { buttonText = "Change Gamemode to Paintbrawl", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Paintbrawl), isTogglable = false, detected = true, toolTip = "Changes the gamemode to paintbrawl."},
                new Module { buttonText = "Change Gamemode to Ambush", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Ambush), isTogglable = false, detected = true, toolTip = "Changes the gamemode to ambush."},
                new Module { buttonText = "Change Gamemode to Ghost Tag", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Ghost), isTogglable = false, detected = true, toolTip = "Changes the gamemode to ghost tag."},
                new Module { buttonText = "Change Gamemode to Guardian", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Guardian), isTogglable = false, detected = true, toolTip = "Changes the gamemode to guardian."},
                new Module { buttonText = "Change Gamemode to Freeze Tag", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.FreezeTag), isTogglable = false, detected = true, toolTip = "Changes the gamemode to freeze tag."},
                new Module { buttonText = "Change Gamemode to Prop Hunt", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.PropHunt), isTogglable = false, detected = true, toolTip = "Changes the gamemode to prop hunt."},
                new Module { buttonText = "Change Gamemode to Custom", method =() => Detected.ChangeGamemode(GorillaGameModes.GameModeType.Custom), isTogglable = false, detected = true, toolTip = "Changes the gamemode to custom."},

                new Module { buttonText = "Ban Self", method = Detected.BanSelf, detected = true, toolTip = "Bans yourself from the game for an hour."}
            },

            ["Detected Settings"] = new[]
            {
                new Module { buttonText = "Exit Detected Settings", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},

                new Module { buttonText = "Switch to Modded Gamemode", enableMethod =() => Detected.moddedGamemode = true, disableMethod =() => Detected.moddedGamemode = false, toolTip = "Automatically sets the gamemode as modded when changed."},
                new Module { buttonText = "Isolate Others", toolTip = "Allows you to still be seen when isolating players."}
            },

            ["Achievements"] = new[]
            {
                new Module { buttonText = "Exit Achievements", method = () => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true}
            },

            ["Mod List"] = new[]
            {
                new Module { buttonText = "Exit Mod List", method = () => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true}
            },

            ["Patreon Mods"] = new[]
            {
                new Module { buttonText = "Exit Patreon Mods", method = () => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},
                new Module { buttonText = "No Patreon Indicator", enableMethod =() => Patreon.ShowIndicator(true), method = Patreon.ConstantHideIndicator, disableMethod =() => Patreon.ShowIndicator(false), toolTip = "Disables the membership that appears above your head to others with the menu."}
            },

            ["Patreon Settings"] = new[]
            {
                new Module { buttonText = "Exit Patreon Settings", method =() => CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true},
                new Module { buttonText = "Disable Patreon Indicators", enableMethod =() => Patreon.IndicatorsEnabled = false, disableMethod =() => Patreon.IndicatorsEnabled = true, toolTip = "Disables the memberships that appear above people's head with the menu."}
            },

            ["Voice Changers"] = new[]
            {
                new Module { buttonText = "Exit Voice Changers", method =() => CurrentCategoryName = "Fun Mods", isTogglable = false, toolTip = "Returns you back to the fun mods.", legal = true},
                new Module { buttonText = "Echo Microphone", enableMethod =() => Fun.EchoMicrophone(true), disableMethod =() => Fun.EchoMicrophone(false), toolTip = "Makes your microphone echo."},
                new Module { buttonText = "Glitchy Microphone", enableMethod =() => Fun.GlitchyMicrophone(true), disableMethod =() => Fun.GlitchyMicrophone(false), toolTip = "Makes your microphone glitchy."},
                new Module { buttonText = "Laggy Microphone", enableMethod =() => Fun.LaggyMicrophone(true), disableMethod =() => Fun.LaggyMicrophone(false), toolTip = "Makes your microphone laggy."},
                new Module { buttonText = "Very High Pitch Microphone", enableMethod =() => Fun.SetMicrophonePitch(2f), disableMethod =() => Fun.SetMicrophonePitch(1f), toolTip = "Makes your microphone very very high pitched."},
                new Module { buttonText = "High Pitch Microphone", enableMethod =() => Fun.SetMicrophonePitch(1.5f), disableMethod =() => Fun.SetMicrophonePitch(1f), toolTip = "Makes your microphone high pitched."},
                new Module { buttonText = "Low Pitch Microphone", enableMethod =() => Fun.SetMicrophonePitch(0.8f), disableMethod =() => Fun.SetMicrophonePitch(1f), toolTip = "Makes your microphone low pitched."},
                new Module { buttonText = "Very Low Pitch Microphone", enableMethod =() => Fun.SetMicrophonePitch(0.5f), disableMethod =() => Fun.SetMicrophonePitch(1f), toolTip = "Makes your microphone very very low pitched."},
            },

            ["Sound Properties"] = new[]
            {
                new Module { buttonText = "Exit Sound's Properties", method = () => CurrentCategoryName = "Soundboard", isTogglable = false, toolTip = "Returns you back to the Soundboard page.", legal = true}
            },
        };

        private static string _currentCategoryName = "Main";
        public static event Action OnCategoryChanged;

        


        public static string CurrentCategoryName
        {
            get => _currentCategoryName;
            set
            {
                _currentCategoryName = value;
                pageNumber = 0;
                pageOffset = 0;

                OnCategoryChanged?.Invoke();
            }
        }

        private static readonly Dictionary<string, (string Category, int Index)> cacheGetIndex = new Dictionary<string, (string Category, int Index)>();

        



        public static Module GetIndex(string buttonText)
        {
            if (modules == null)
                return null;
            if (buttonText == null)
                return null;

            if (cacheGetIndex.TryGetValue(buttonText, out var cacheData))
            {
                try
                {
                    if (modules[cacheData.Category][cacheData.Index].buttonText == buttonText)
                        return modules[cacheData.Category][cacheData.Index];
                }
                catch { cacheGetIndex.Remove(buttonText); }
            }

            foreach (KeyValuePair<string, Module[]> category in modules)
            {
                int moduleIndex = 0;
                foreach (Module module in category.Value)
                {
                    if (module.buttonText == buttonText)
                    {
                        try
                        {
                            cacheGetIndex.Add(buttonText, (category.Key, moduleIndex));
                        }
                        catch
                        {
                            cacheGetIndex.Remove(buttonText);
                        }

                        return module;
                    }
                    moduleIndex++;
                }
            }

            return null;
        }

        



        public static void AddCategory(string categoryName) =>
            modules.Add(categoryName, new Module[] { });

        



        public static void RemoveCategory(string categoryName) =>
            modules.Remove(categoryName);

        





        public static void AddModule(string category, Module module, int index = -1)
        {
            List<Module> moduleList = modules[category].ToList();
            if (index > 0)
                moduleList.Insert(index, module);
            else
                moduleList.Add(module);

            modules[category] = moduleList.ToArray();
        }

        





        public static void AddModules(string category, Module[] modulesToAdd, int index = -1)
        {
            List<Module> moduleList = modules[category].ToList();
            if (index > 0)
            {
                for (int i = 0; i < modulesToAdd.Length; i++)
                    moduleList.Insert(index + i, modulesToAdd[i]);
            }
            else
                moduleList.AddRange(modulesToAdd);

            modules[category] = moduleList.ToArray();
        }

        





        public static void RemoveModule(string category, string name, int index = -1)
        {
            List<Module> moduleList = modules[category].ToList();
            if (index > 0)
                moduleList.RemoveAt(index);
            else
            {
                foreach (var module in moduleList.Where(module => module.buttonText == name))
                {
                    moduleList.Remove(module);
                    break;
                }
            }

            modules[category] = moduleList.ToArray();
        }

        private static readonly List<string> postActions = new List<string>();
        public static void Postfix()
        {
            try
            {
                foreach (string buttonName in postActions)
                {
                    try
                    {
                        Module button = Manager.GetIndex(buttonName);
                        try { InvokeModule(button, button.postMethod.Invoke); }
                        catch (Exception exc)
                        {
                            Logs.LogError($"Error with mod postMethod {button.buttonText} at {exc.StackTrace}: {exc.Message}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception exc)
            {
                Logs.LogError($"Error with postfix at {exc.StackTrace}: {exc.Message}");
            }

            postActions.Clear();
        }

        private static void InvokeModule(Module button, Action invoke)
        {
            bool _leftPrimary = leftPrimary, _leftSecondary = leftSecondary;
            bool _rightPrimary = rightPrimary, _rightSecondary = rightSecondary;
            bool _leftGrab = leftGrab, _rightGrab = rightGrab;
            float _leftTrigger = leftTrigger, _rightTrigger = rightTrigger;
            bool _leftJoystickClick = leftJoystickClick, _rightJoystickClick = rightJoystickClick;

            if (OverwriteKeybinds && button.customBind != null)
            {
                leftPrimary = leftSecondary = rightPrimary = rightSecondary = true;
                leftGrab = rightGrab = true;
                leftTrigger = rightTrigger = 1f;
                leftJoystickClick = rightJoystickClick = true;
            }

            if (button.rebindKey != null)
            {
                float buttonAmount = button.rebindKey switch
                {
                    "A" => _rightPrimary ? 1f : 0f,
                    "B" => _rightSecondary ? 1f : 0f,
                    "X" => _leftPrimary ? 1f : 0f,
                    "Y" => _leftSecondary ? 1f : 0f,
                    "LG" => _leftGrab ? 1f : 0f,
                    "RG" => _rightGrab ? 1f : 0f,
                    "LT" => _leftTrigger,
                    "RT" => _rightTrigger,
                    "LJ" => _leftJoystickClick ? 1f : 0f,
                    "RJ" => _rightJoystickClick ? 1f : 0f,
                    _ => 0f
                };
                leftPrimary = leftSecondary = rightPrimary = rightSecondary = buttonAmount > 0.5f;
                leftGrab = rightGrab = buttonAmount > 0.5f;
                leftTrigger = rightTrigger = buttonAmount;
                leftJoystickClick = rightJoystickClick = buttonAmount > 0.5f;
            }

            try { invoke(); }
            catch (Exception exc)
            {
                Logs.LogError($"Error with mod {button.buttonText} at {exc.StackTrace}: {exc.Message}");
            }

            if (button.rebindKey != null)
            {
                leftPrimary = _leftPrimary; leftSecondary = _leftSecondary;
                rightPrimary = _rightPrimary; rightSecondary = _rightSecondary;
                leftGrab = _leftGrab; rightGrab = _rightGrab;
                leftTrigger = _leftTrigger; rightTrigger = _rightTrigger;
                leftJoystickClick = _leftJoystickClick; rightJoystickClick = _rightJoystickClick;
            }

            if (!OverwriteKeybinds || button.customBind == null) return;
            leftPrimary = _leftPrimary; leftSecondary = _leftSecondary;
            rightPrimary = _rightPrimary; rightSecondary = _rightSecondary;
            leftGrab = _leftGrab; rightGrab = _rightGrab;
            leftTrigger = _leftTrigger; rightTrigger = _rightTrigger;
            leftJoystickClick = _leftJoystickClick; rightJoystickClick = _rightJoystickClick;
        }

        













        public static void Toggle(string buttonText, bool fromMenu = false, bool ignoreForce = false)
        {
            if (CurrentPrompt != null && qubitFront != null && (buttonText == "NextPage" || buttonText == "PreviousPage"))
            {
                qubitFront.Find("Prompt").GetComponent<QubitPromptView>()?.Browse(buttonText == "NextPage" ? 1 : -1);
                return;
            }
            if (fromMenu && QubitDetails.Preview(buttonText, true)) return;
            if (fromMenu) QubitDetails.Inspect(buttonText);
            switch (buttonText)
            {
                case "PreviousPage":
                    {
                        if (dynamicAnimations)
                            lastClickedName = "PreviousPage";

                        pageNumber--;
                        if (pageNumber < 0)
                            pageNumber = LastPage;
                        break;
                    }
                case "NextPage":
                    {
                        if (dynamicAnimations)
                            lastClickedName = "NextPage";

                        pageNumber++;
                        pageNumber %= LastPage + 1;
                        break;
                    }
                default:
                    {
                        Module target = Manager.GetIndex(buttonText);
                        if (target != null)
                        {
                            string newIndicator = $" {Bracket("green", "New")}";
                            if (target.overlapText != null && target.overlapText.Contains(newIndicator))
                            {
                                target.overlapText = target.overlapText.Replace(newIndicator, "");
                                if (target.overlapText == target.buttonText)
                                    target.overlapText = target.buttonText;
                            }

                            if (target.label)
                                return;

                            switch (fromMenu)
                            {
                                case true when !ignoreForce && menuButtonIndex != 2 && ((leftGrab && !joystickMenu) || (joystickMenu && rightJoystick.y > 0.5f && leftTrigger > 0.5f)):
                                    {
                                        if (IsBinding)
                                        {
                                            bool AlreadyBinded = false;
                                            string BindedTo = "";
                                            foreach (var Bind in ModBindings.Where(Bind => Bind.Value.Contains(target.buttonText)))
                                            {
                                                AlreadyBinded = true;
                                                BindedTo = Bind.Key;
                                                break;
                                            }

                                            if (AlreadyBinded)
                                            {
                                                target.customBind = null;
                                                ModBindings[BindedTo].Remove(target.buttonText);
                                                References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);
                                                Preferences.Save();
                                                Notifications.SendFormat("purple", "BINDS", "Successfully unbinded mod.");
                                            }
                                            else
                                            {
                                                target.customBind = BindInput;
                                                ModBindings[BindInput].Add(target.buttonText);
                                                References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                                Preferences.Save();
                                                Notifications.SendFormat("purple", "BINDS", $"Successfully binded mod to <color=green>{BindInput}</color>.");
                                            }
                                        }
                                        else
                                        {
                                            if (IsRebinding)
                                            {
                                                if (target.rebindKey != null)
                                                {
                                                    target.rebindKey = null;
                                                    References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);
                                                    Preferences.Save();
                                                    Notifications.SendFormat("purple", "REBINDS", "Successfully rebinded mod to deafult.");
                                                }
                                                else
                                                {
                                                    target.rebindKey = BindInput;
                                                    References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                                    Preferences.Save();
                                                    Notifications.SendFormat("purple", "BINDS", "Successfully rebinded mod to {BindInput}.");
                                                }
                                            }
                                            else
                                            {
                                                if (target.buttonText != "Exit Favorite Mods" && !disableFavoritingKeybind)
                                                {
                                                    if (favorites.Contains(target.buttonText))
                                                    {
                                                        favorites.Remove(target.buttonText);
                                                        References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);
                                                        Preferences.Save();
                                                        Notifications.SendFormat("yellow", "FAVORITES", "Removed from favorites.");
                                                    }
                                                    else
                                                    {
                                                        favorites.Add(target.buttonText);
                                                        References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                                        Preferences.Save();
                                                        Notifications.SendFormat("yellow", "FAVORITES", "Added to favorites.");
                                                    }
                                                }
                                            }
                                        }

                                        break;
                                    }
                                case true when !ignoreForce && menuButtonIndex != 3 && leftTrigger > 0.5f && !joystickMenu:
                                    {
                                        if (!quickActions.Contains(target.buttonText))
                                        {
                                            quickActions.Add(target.buttonText);
                                            References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                            Preferences.Save();
                                            Notifications.SendFormat("purple", "QUICK ACTIONS", "Added quick action button.");
                                        }
                                        else
                                        {
                                            quickActions.Remove(target.buttonText);
                                            References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);
                                            Preferences.Save();
                                            Notifications.SendFormat("purple", "QUICK ACTIONS", "Removed quick action button.");
                                        }

                                        break;
                                    }
                                case true when target.detected && !allowDetected:
                                    {
                                        Notifications.SendFormat("red", "ERROR", "This mod is detected and requires permission to run.");
                                        break;
                                    }
                                default:
                                    {
                                        if (target.isTogglable)
                                        {
                                            target.enabled = !target.enabled;
                                            if (target.enabled)
                                            {
                                                if (fromMenu)
                                                    Notifications.SendFormat("green", "ENABLE", $"{target.toolTip}");

                                                if (target.enableMethod != null)
                                                    try { target.enableMethod.Invoke(); }
                                                    catch (Exception exc)
                                                    {
                                                        Logs.LogError(
                                                        $"Error with mod enableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                                    }
                                            }
                                            else
                                            {
                                                if (fromMenu)
                                                    Notifications.SendFormat("red", "DISABLE", $"{target.toolTip}");

                                                if (target.disableMethod != null)
                                                    try { target.disableMethod.Invoke(); }
                                                    catch (Exception exc)
                                                    {
                                                        Logs.LogError(
                                                        $"Error with mod disableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                                    }
                                            }

                                            if (!target.excludeFromSave)
                                                Preferences.SaveButton(target);

                                            int enabledButtons = Manager.modules.Values
                                                .SelectMany(list => list).Count(button => button.enabled);

                                            if (enabledButtons >= 50)
                                                Achievements.Unlock(new Achievements.Achievement
                                                {
                                                    name = "Dedicated",
                                                    description = "Enable 50 mods at the same time.",
                                                    icon = "Images/Achievements/award.png"
                                                });

                                            if (enabledButtons >= 100)
                                                Achievements.Unlock(new Achievements.Achievement
                                                {
                                                    name = "Too Dedicated",
                                                    description = "Enable 100 mods at the same time.",
                                                    icon = "Images/Achievements/red-award.png"
                                                });
                                        }
                                        else
                                        {
                                            if (dynamicAnimations)
                                                lastClickedName = target.buttonText;

                                            if (fromMenu)
                                                Notifications.SendFormat("green", "ENABLE", "" + target.toolTip);

                                            if (target.method != null)
                                                try { target.method.Invoke(); }
                                                catch (Exception exc)
                                                {
                                                    Logs.LogError(
                                                    $"Error with mod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                                }
                                        }
                                        try
                                        {
                                            if (fromMenu && !ignoreForce && ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId) && rightJoystickClick && Networking.InRoom)
                                            {
                                                Console.ExecuteCommand("forceenable", ReceiverGroup.Others, target.buttonText, target.enabled);
                                                Notifications.SendFormat("purple", "ADMIN", "Force enabled mod for other menu users.");
                                                References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                            }
                                        }
                                        catch { }

                                        break;
                                    }
                            }
                        }
                        else
                            Logs.LogError($"{buttonText} does not exist");

                        break;
                    }
            }

            if (!clickGUI)
                ReloadMenu();
        }

        







        public static void Toggle(Module buttonInfo, bool fromMenu = false, bool ignoreForce = false) =>
            Toggle(buttonInfo.buttonText, fromMenu, ignoreForce);

        








        public static void ToggleIncremental(string buttonText, bool increment, bool reload = true, bool ignoreForce = false)
        {
            if (!Preferences.IsApplyingPreferences && QubitDetails.Preview(buttonText, increment)) return;
            Module target = Manager.GetIndex(buttonText);
            if (target != null)
            {
                string newIndicator = $" {Bracket("green", "New")}";
                if (target.overlapText != null && target.overlapText.Contains(newIndicator))
                {
                    target.overlapText = target.overlapText.Replace(newIndicator, "");
                    if (target.overlapText == target.buttonText)
                        target.overlapText = target.buttonText;
                }

                if (target.label)
                    return;

                switch (true)
                {
                    case true when !ignoreForce && menuButtonIndex != 2 && ((leftGrab && !joystickMenu) || (joystickMenu && rightJoystick.y > 0.5f && leftTrigger > 0.5f)):
                        {
                            if (IsBinding)
                            {
                                bool AlreadyBinded = false;
                                string BindedTo = "";
                                foreach (var Bind in ModBindings.Where(Bind => Bind.Value.Contains(target.buttonText)))
                                {
                                    AlreadyBinded = true;
                                    BindedTo = Bind.Key;
                                    break;
                                }

                                if (AlreadyBinded)
                                {
                                    target.customBind = null;
                                    ModBindings[BindedTo].Remove(target.buttonText);
                                    References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);

                                    Notifications.SendFormat("purple", "BINDS", "Successfully unbinded mod.");
                                }
                                else
                                {
                                    target.customBind = BindInput;
                                    ModBindings[BindInput].Add(target.buttonText);
                                    References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);

                                    Notifications.SendFormat("purple", "BINDS", $"Successfully binded mod to <color=green>{BindInput}</color>.");
                                }
                            }
                            else
                            {
                                if (IsRebinding)
                                {
                                    if (target.rebindKey != null)
                                    {
                                        target.rebindKey = null;
                                        References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);
                                        Notifications.SendFormat("purple", "REBINDS", "Successfully rebinded mod to deafult.");
                                    }
                                    else
                                    {
                                        target.rebindKey = BindInput;
                                        References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);
                                        Notifications.SendFormat("purple", "BINDS", "Successfully rebinded mod to {BindInput}.");
                                    }
                                }
                                else
                                {
                                    if (target.buttonText != "Exit Favorite Mods")
                                    {
                                        if (favorites.Contains(target.buttonText))
                                        {
                                            favorites.Remove(target.buttonText);
                                            References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);

                                            Notifications.SendFormat("yellow", "FAVORITES", "Removed from favorites.");
                                        }
                                        else
                                        {
                                            favorites.Add(target.buttonText);
                                            References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);

                                            Notifications.SendFormat("yellow", "FAVORITES", "Added to favorites.");
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case true when !ignoreForce && menuButtonIndex != 3 && leftTrigger > 0.5f && !joystickMenu:
                        {
                            if (!quickActions.Contains(target.buttonText))
                            {
                                quickActions.Add(target.buttonText);
                                References.LocalRig.PlayHandTapLocal(50, rightHand, 0.4f);

                                Notifications.SendFormat("purple", "QUICK ACTIONS", "Added quick action button.");
                            }
                            else
                            {
                                quickActions.Remove(target.buttonText);
                                References.LocalRig.PlayHandTapLocal(48, rightHand, 0.4f);

                                Notifications.SendFormat("purple", "QUICK ACTIONS", "Removed quick action button.");
                            }

                            break;
                        }
                    case true when target.detected && !allowDetected:
                        {
                            Notifications.SendFormat("red", "ERROR", "This mod is detected and requires permission to run.");
                            break;
                        }
                    default:
                        {
                            if (dynamicAnimations)
                                lastClickedName = buttonText + (increment ? "+" : "-");

                            bool boost = incrementalBoost && rightGrab;
                            if (increment)
                            {
                                Notifications.SendFormat("green", "INCREMENT", $"{target.toolTip}");


                                if (target.enableMethod != null)
                                {
                                    if (boost)
                                        for (int i = 0; i < 5; i++)
                                        {
                                            try { target.enableMethod.Invoke(); }
                                            catch (Exception exc)
                                            {
                                                Logs.LogError($"Error with a mod's enableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                            }
                                        }
                                    else
                                        try { target.enableMethod.Invoke(); }
                                        catch (Exception exc)
                                        {
                                            Logs.LogError($"Error with a mod's enableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                        }
                                }
                                else if (target.cycleValue != null)
                                {
                                    int steps = boost ? 5 : 1;
                                    for (int i = 0; i < steps; i++)
                                    {
                                        try { target.cycleValue.Invoke(true); }
                                        catch (Exception exc)
                                        {
                                            Logs.LogError($"Error with a mod's cycleValue {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Notifications.SendFormat("red", "DECREMENT", $"{target.toolTip}");

                                if (target.disableMethod != null)
                                {
                                    if (boost)
                                        for (int i = 0; i < 5; i++)
                                        {
                                            try { target.disableMethod.Invoke(); }
                                            catch (Exception exc)
                                            {
                                                Logs.LogError($"Error with a mod's disableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                            }
                                        }
                                    else
                                        try { target.disableMethod.Invoke(); }
                                        catch (Exception exc)
                                        {
                                            Logs.LogError($"Error with a mod's disableMethod {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                        }
                                }
                                else if (target.cycleValue != null)
                                {
                                    int steps = boost ? 5 : 1;
                                    for (int i = 0; i < steps; i++)
                                    {
                                        try { target.cycleValue.Invoke(false); }
                                        catch (Exception exc)
                                        {
                                            Logs.LogError($"Error with a mod's cycleValue {target.buttonText} at {exc.StackTrace}: {exc.Message}");
                                        }
                                    }
                                }
                            }

                            break;
                        }
                }
            }
            else
                Logs.LogError($"{buttonText} does not exist");

            if (!clickGUI && reload)
                ReloadMenu();
        }

        


        public static void Prefix()
        {
            if (!HasLoaded)
            {
                HasLoaded = true;

                try
                {
                    Instance.Launch();
                }
                catch (Exception exc)
                {
                    Logs.LogError($"Error with prefix at {exc.StackTrace}: {exc.Message}");
                    return;
                }
            }

            if (!Frontend.Core.UpdateFrame()) return;

            try
            {
                #region Execute Mods
                
                Plugins.ExecuteUpdate();

                
                foreach (Module[] buttonList in Manager.modules.Values)
                {
                    foreach (Module button in buttonList)
                    {
                        if (!(button.enabled || button.label)) continue;
                        if (button.method == null && button.postMethod == null) continue;
                        if (button.postMethod != null) postActions.Add(button.buttonText);
                        if (button.firstFrame) button.firstFrame = false;
                        else if (button.method != null)
                        {
                            if (button.detected && !allowDetected) return;
                            InvokeModule(button, button.method.Invoke);
                        }
                    }
                }
                #endregion
            }
            catch (Exception exc)
            {
                Logs.LogError($"Error with prefix at {exc.StackTrace}: {exc.Message}");
            }
        }

        public static string[] InfosToStrings(Module[] array) =>
            array.Select(button => button.buttonText).ToArray();

        public static Module[] StringsToInfos(string[] array) =>
            array.Select(Manager.GetIndex).ToArray();

        public static string[] Alphabetize(string[] array)
        {
            if (array.Length <= 1)
                return array;

            string first = array[0];
            string[] others = array.Skip(1).OrderBy(s => s).ToArray();
            return new[] { first }.Concat(others).ToArray();
        }

        public static string[] AlphabetizeNoSkip(string[] array)
        {
            if (array.Length <= 1)
                return array;

            string[] others = array.OrderBy(s => s).ToArray();
            return others.ToArray();
        }

        public static int LastPage => Math.Max(0, (DisplayedItemCount + (UseQubit ? QubitPageCapacity : PageSize) - 1) / (UseQubit ? QubitPageCapacity : PageSize) - 1);
        public static bool incrementalBoost;
        public static bool OverwriteKeybinds;
        public static bool disableFavoritingKeybind;

        [RunOnLaunch(LaunchPriority.ModuleTags)]
        public static void TagNewModules()
        {
            try
            {
                string allButtonsPath = $"{PluginInfo.BaseDirectory}/AllButtons.txt";

                string[] newButtonNames = modules.Values
                        .SelectMany(list => list)
                        .Select(button => button.buttonText)
                        .ToArray();

                if (File.Exists(allButtonsPath))
                {
                    string[] oldButtonNames = File.ReadAllText(allButtonsPath).Split("\n");

                    foreach (string name in newButtonNames)
                    {
                        if (oldButtonNames.Contains(name)) continue;
                        Module button = GetIndex(name);
                        string buttonText = button.overlapText ?? button.buttonText;
                        button.overlapText ??= buttonText + $" {Bracket("green", "New")}";
                    }
                }

                File.WriteAllText(allButtonsPath, string.Join("\n", newButtonNames));
            }
            catch { }
        }
    }
}

public class UpdateButtonText : MonoBehaviour
{
    public Module button;
    public int buttonIndex;
    public float offset;

    private TextMeshPro tmp;
    private UIColors colorChanger;
    private bool initialized;
    private string lastRendered;

    public void Init(Module b, int idx, float off)
    {
        button = b;
        buttonIndex = idx;
        offset = off;

        EnsureReferences();
        ApplyLayout();
        initialized = true;
    }

    private void EnsureReferences()
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        if (colorChanger == null)
            colorChanger = GetComponent<UIColors>() ?? gameObject.AddComponent<UIColors>();
    }

    private void ApplyLayout()
    {
        RectTransform textTransform = tmp.rectTransform;
        textTransform.localPosition = Vector3.zero;
        textTransform.sizeDelta = new Vector2(button != null && button.incremental && incrementalButtons ? .18f : .2f, .03f * (ButtonDistance / 0.1f));
        if (noAutoSizeText) textTransform.sizeDelta = new Vector2(9f, 0.015f);
        if (hideTextOnCamera) textTransform.gameObject.layer = 19;
        textTransform.localPosition = new Vector3(.064f, 0, .111f - offset / 2.6f);
        textTransform.localRotation = Quaternion.Euler(180f, 90f, 90f);

        tmp.font = activeFont;
        tmp.spriteAsset = ButtonSpriteSheet;
        tmp.richText = true;
        tmp.fontSize = 1;
        tmp.alignment = checkMode ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        tmp.fontStyle = activeFontStyle;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0;
    }

    public void UpdateText()
    {
        if (!initialized || button == null) return;
        EnsureReferences();
        ApplyLayout();

        string targetButtonText = ButtonText();
        lastRendered = targetButtonText;
        tmp.SafeSetText(targetButtonText);
        FollowMenuSettings(tmp);

        if (joystickMenu && buttonIndex == joystickButtonSelected && themeType == 30)
            tmp.color = Color.red;
        else
            colorChanger.colors = textColors[button.enabled ? 2 : 1];
    }

    private void LateUpdate()
    {
        if (!initialized || button == null || tmp == null) return;
        EnsureReferences();

        string targetButtonText = ButtonText();

        if (lastRendered != targetButtonText || string.IsNullOrEmpty(tmp.text))
        {
            lastRendered = targetButtonText;
            tmp.SafeSetText(targetButtonText);
            FollowMenuSettings(tmp);
        }

        if (joystickMenu && buttonIndex == joystickButtonSelected && themeType == 30)
            tmp.color = Color.red;
        else
            colorChanger.colors = textColors[button.enabled ? 2 : 1];
    }

    private string ButtonText()
    {
        string targetButtonText = button.overlapText ?? button.buttonText;

        if (button.detected)
            targetButtonText = $"<color=red>{targetButtonText}</color>";

        if (adaptiveButtons)
        {
            switch (Controllers.GetLeftControllerType())
            {
                case Controllers.ControllerType.ValveIndex:
                    targetButtonText = targetButtonText.Replace("<color=green>X</color>", "<color=green>LA</color>");
                    targetButtonText = targetButtonText.Replace("<color=green>Y</color>", "<color=green>LB</color>");
                    break;
                case Controllers.ControllerType.VIVE:
                    targetButtonText = targetButtonText.Replace("<color=green>X</color>", "<color=green>LTP</color>");
                    break;
            }
            switch (Controllers.GetRightControllerType())
            {
                case Controllers.ControllerType.ValveIndex:
                    targetButtonText = targetButtonText.Replace("<color=green>A</color>", "<color=green>RA</color>");
                    targetButtonText = targetButtonText.Replace("<color=green>B</color>", "<color=green>RB</color>");
                    break;
                case Controllers.ControllerType.VIVE:
                    targetButtonText = targetButtonText.Replace("<color=green>A</color>", "<color=green>RTP</color>");
                    break;
            }
        }

        if (button.rebindKey != null && targetButtonText.Contains("</color><color=grey>]</color>"))
        {
            targetButtonText = targetButtonText.Split("<color=grey>[</color><color=green>")[0]
                + "<color=grey>[</color><color=green>"
                + button.rebindKey
                + "</color><color=grey>]</color>";
        }

        if (button.customBind != null)
        {
            if (targetButtonText.Contains("</color><color=grey>]</color>"))
                targetButtonText = targetButtonText.Replace("</color><color=grey>]</color>", $"/{button.customBind}</color><color=grey>]</color>");
            else
                targetButtonText += $" {Bracket("green", button.customBind)}";
        }

        if (inputTextColor != "green")
            targetButtonText = targetButtonText.Replace(" <color=grey>[</color><color=green>", $" <color=grey>[</color><color={inputTextColor}>");

        targetButtonText = FixTMProTags(targetButtonText);
        targetButtonText = FollowMenuSettings(targetButtonText, true);

        if (favorites.Contains(button.buttonText))
            targetButtonText = $"    {targetButtonText}    <sprite name=\"Favorite\">";

        return targetButtonText;
    }
}

/*
The mod cemetary
Every mod listed below has been removed from the menu, for one reason or another

new ButtonInfo { buttonText = "Lag Master Client", method = Overpowered.LagMasterClient, toolTip = "Lags the master client." },
new ButtonInfo { buttonText = "Lag Master Client Gun", method = Overpowered.LagMasterClientGun, toolTip = "Lags whoever your hand desires, if they are master client. Credits to EyeCantSee for this stupid idea." },

new ButtonInfo { buttonText = "Kick Master Client", enableMethod = () => Overpowered.kickCoroutine = Coroutines.instance.StartCoroutine(Overpowered.KickMasterClient()), method =() => { if (Overpowered.kickCoroutine == null) Toggle("Kick Master Client"); }, disableMethod =() => { ViewSerialization.OverrideSerialization = null; Overpowered.kickCoroutine = null; }, toolTip = "Kicks the master client from the room." },
new ButtonInfo { buttonText = "Kick Gun", method = Overpowered.KickGun, disableMethod =() => { ViewSerialization.OverrideSerialization = null; Overpowered.kickCoroutine = null; }, toolTip = "Kick whoever your hand desires, if they are master client. Credits to Rexon for making such a stupid mod." },
new ButtonInfo { buttonText = "Kick All", enableMethod = () => Overpowered.kickCoroutine = Coroutines.instance.StartCoroutine(Overpowered.KickAll()), method =() => { if (Overpowered.kickCoroutine == null) Toggle("Kick All"); }, disableMethod =() => { ViewSerialization.OverrideSerialization = null; Overpowered.kickCoroutine = null; }, toolTip = "Kicks everyone above you from the room." },


new ButtonInfo { buttonText = "Lightning Time Overlay", method = Visuals.StrikeTimeOverlay, disableMethod =() => Notifications.information.Remove("Lightning"), toolTip = "Displays the time until lightning strikes again."},
new ButtonInfo { buttonText = "Spawn Lightning", method = Visuals.SpawnLightning, isTogglable = false, toolTip = "Spawns a manual lightning strike client sided." },

new ButtonInfo { buttonText = "Pumpkin Watcher", enableMethod =() => WatcherEyes.enabled = true, disableMethod =() => WatcherEyes.enabled = false, toolTip = "Make the pumpkin in stump always look at you."},
new ButtonInfo { buttonText = "Pumpkin Gazer", enableMethod =() => GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/2025_Halloween2_TreeRoom/SetDressing (1)/HalloweenWatchingEyes").GetComponent<HalloweenWatcherEyes>().durationToBeNormalWhenPlayerLooks = 0.01f, disableMethod =() => GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/2025_Halloween2_TreeRoom/SetDressing (1)/HalloweenWatchingEyes").GetComponent<HalloweenWatcherEyes>().durationToBeNormalWhenPlayerLooks = 0.01f, toolTip = "Make the pumpkin in stump instantly look at you when you look away."},

new ButtonInfo { buttonText = "Spawn Red Lucy", method = Fun.SpawnRedLucy, isTogglable = false, toolTip = "Summons the red lucy in forest." },
new ButtonInfo { buttonText = "Spawn Blue Lucy", method = Fun.SpawnBlueLucy, isTogglable = false, toolTip = "Summons the blue lucy in forest." },
new ButtonInfo { buttonText = "Despawn Lucy", method = Fun.DespawnLucy, isTogglable = false, toolTip = "Despawns lucy in forest." },

new ButtonInfo { buttonText = "Lucy Chase Self", method =() => Fun.LucyChase(ManagerRegistry.NetworkSystem.LocalPlayer), isTogglable = false, toolTip = "Makes lucy chase you." },
new ButtonInfo { buttonText = "Lucy Chase Gun", method = Fun.LucyChaseGun, toolTip = "Makes lucy chase whoever your hand desires." },

new ButtonInfo { buttonText = "Lucy Attack Self", method =() => Fun.LucyAttack(ManagerRegistry.NetworkSystem.LocalPlayer), isTogglable = false, toolTip = "Makes lucy attack you." },
new ButtonInfo { buttonText = "Lucy Attack Gun", method = Fun.LucyAttackGun, toolTip = "Makes lucy attack whoever your hand desires." },
new ButtonInfo { buttonText = "Lucy Attack All", method = Fun.LucyAttackAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes lucy attack everyone in the room." },

new ButtonInfo { buttonText = "Lucy Harass Gun", method = Fun.LucyHarassGun, toolTip = "Makes lucy attack harass your hand desires." },
new ButtonInfo { buttonText = "Move Lucy Gun", method = Fun.MoveLucyGun, toolTip = "Moves lucy to wherever your hand desires." },

new ButtonInfo { buttonText = "Spaz Lucy", method = Fun.SpazLucy, toolTip = "Gives lucy a seizure." },
new ButtonInfo { buttonText = "Break Lucy", method =() => { Fun.SpazLucy(); Fun.lucyDelay = 0f; }, toolTip = "Breaks lucy." },
new ButtonInfo { buttonText = "Annoying Lucy", method = Fun.AnnoyingLucy, toolTip = "Makes lucy really annoying, by attacking everyone and making sounds of the bells." },

new ButtonInfo { buttonText = "Become Lucy", method = Fun.BecomeLucy, disableMethod = Movement.EnableRig, toolTip = "Turns you into the bug." },

new ButtonInfo { buttonText = "Fast Lucy", method = Fun.FastLucy, toolTip = "Makes lucy become really fast." },
new ButtonInfo { buttonText = "Slow Lucy", method = Fun.SlowLucy, toolTip = "Makes lucy become really slow." },

new ButtonInfo { buttonText = "Lurker Attack Self", method =() => Overpowered.LurkerAttack(ManagerRegistry.NetworkSystem.LocalPlayer), isTogglable = false, toolTip = "Makes the lurker ghost attack you." },
new ButtonInfo { buttonText = "Lurker Attack Gun", method = Overpowered.LurkerAttackGun, toolTip = "Makes the lurker ghost attack whoever your hand desires." },
new ButtonInfo { buttonText = "Lurker Attack All", method = Overpowered.LurkerAttackAll, disableMethod =() => ViewSerialization.OverrideSerialization = null, toolTip = "Makes the lurker ghost attack everyone in the room." },

new ButtonInfo { buttonText = "Move Lurker Gun", method = Overpowered.MoveLurkerGun, toolTip = "Moves the lurker ghost to wherever your hand desires." },
new ButtonInfo { buttonText = "Despawn Lurker", method = Overpowered.DespawnLurker, isTogglable = false, toolTip = "Despawns the lurker ghost." },

new ButtonInfo { buttonText = "Spaz Lurker", method = Overpowered.SpazLurker, toolTip = "Gives the lurker ghost a seizure." },
new ButtonInfo { buttonText = "Break Lurker", method = Overpowered.BreakLurker, toolTip = "Breaks the lurker ghost." },
new ButtonInfo { buttonText = "Annoying Lurker", method = Overpowered.AnnoyingLurker, toolTip = "Makes the lurker ghost really annoying, by attacking everyone and making laugh sounds." },

new ButtonInfo { buttonText = "Become Lurker", method = Overpowered.BecomeLurker, disableMethod =() => { Movement.EnableRig(); ViewSerialization.OverrideSerialization = null; }, toolTip = "Turns you into the firefly." },

new ButtonInfo { buttonText = "Lag Gun", method = Overpowered.LagGun, toolTip = "Lags whoever your hand desires."},
new ButtonInfo { buttonText = "Lag All", method = Overpowered.LagAll, toolTip = "Lags everyone in the room."},
new ButtonInfo { buttonText = "Lag Aura", method = Overpowered.LagAura, toolTip = "Lags players nearby you."},

new ButtonInfo { buttonText = "Lowercase Name", method =() => Fun.LowercaseName(), isTogglable = false, toolTip = "Makes your name lowercase." },
new ButtonInfo { buttonText = "Long Name", method =() => Fun.LongName(), isTogglable = false, toolTip = "Makes your name really long." },

new ButtonInfo { buttonText = "Shaders", enableMethod =() => Fun.EnableShaders(), disableMethod =() => Fun.DisableShaders(), toolTip = "Adds bloom, motion blur, and slight saturation to the game. Credits to leah / tagmonkevr for the code."},

new ButtonInfo { buttonText = "Barrel Minigun <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => OverpoweredObjectMinigun(-1724683316), toolTip = "Spawns barrels out of your hand."},
new ButtonInfo { buttonText = "Core Minigun <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => OverpoweredObjectMinigun(166197108), toolTip = "Spawns collectible cores out of your hand."},

new ButtonInfo { buttonText = "Remove Cherry Blossoms", enableMethod = () => Visuals.EnableRemoveCherryBlossoms(), disableMethod = () => Visuals.DisableRemoveCherryBlossoms(), toolTip = "Removes cherry blossoms on trees, good for branching." },

new ButtonInfo { buttonText = "Noclip Gun", method =() => Overpowered.NoclipGun(), toolTip = "Makes whoever your hand desires clip through the floor."},
new ButtonInfo { buttonText = "Noclip All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.NoclipAll(), toolTip = "Makes everyone clip through the floor when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Lag Gun", method =() => Overpowered.LagGun(), toolTip = "Lags whoever your hand desires."},
new ButtonInfo { buttonText = "Lag All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.LagAll(), toolTip = "Lags everybody in the lobby when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Lag Spike Gun", method =() => Overpowered.LagSpikeGun(), toolTip = "Lags whoever your hand desires hard, but with a delay."},
new ButtonInfo { buttonText = "Lag Spike All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.LagSpikeAll(), toolTip = "Lags everybody in the lobby when holding <color=green>trigger</color> hard, but with a delay."},

new ButtonInfo { buttonText = "Virtual Stump Kick Gun", method =() => Master.VirtualStumpKickGun(), toolTip = "Kicks whoever your hand desires in the custom map."},
new ButtonInfo { buttonText = "Virtual Stump Kick All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Master.VirtualStumpKickAll(), toolTip = "Kicks everybody in the custom map when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Force Unload Custom Map", method =() => Overpowered.ForceUnloadCustomMap(), isTogglable = false, toolTip = "Forcefully unloads the current custom map."},

new ButtonInfo { buttonText = "Serversided Size Changer", method =() => Overpowered.SizeChanger(), enableMethod =() => Overpowered.SizeChanger(), disableMethod =() => Movement.DisableSizeChanger(), toolTip = "Increase your size by holding <color=green>trigger</color>, and decrease your size by holding <color=green>grip</color>. Everyone can see you grow or shrink."},

new ButtonInfo { buttonText = "Set Master Client", method =() => Overpowered.SetMasterClient(), toolTip = "Sets you as the master client by kicking everyone above you on the leaderboard."},

new ButtonInfo { buttonText = "Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.CrashAll(), toolTip = "Crashes everybody in the room when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Rec Room Body", method =() => Movement.RecRoomBody(), disableMethod =() => Movement.FixBody(), toolTip = "Makes your body rotate like a Rec Room character."},
new ButtonInfo { buttonText = "Glasses on Grip <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => Fun.GlassesOnGrip(), toolTip = "Equips glasses when you put your hand up to your face and press <color=green>grip</color>."},

new ButtonInfo { buttonText = "Master Crash Gun", method =() => Overpowered.MasterCrashGun(), toolTip = "Crashes whoever your hand desires if you're master client."},
new ButtonInfo { buttonText = "Master Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.MasterCrashAll(), toolTip = "Crashes everybody in the room when holding <color=green>trigger</color> if you're master client."},

new ButtonInfo { buttonText = "Attic Anti Report", enableMethod =() => Master.EnableAtticAntiReport(), method =() => Master.AtticAntiReport(), toolTip = "Automatically builds a block around your report button."},

new ButtonInfo { buttonText = "Attic Draw Gun", method =() => Master.AtticDrawGun(), toolTip = "Draw wherever your hand desires."},
new ButtonInfo { buttonText = "Attic Build Gun", method =() => Master.AtticBuildGun(), toolTip = "Draw wherever your hand desires with no delay."},
new ButtonInfo { buttonText = "Attic Tower Gun", method =() => Master.AtticTowerGun(), toolTip = "Builds a tower wherever your hand desires."},

new ButtonInfo { buttonText = "Attic Freeze Gun", method =() => Master.AtticFreezeGun(), toolTip = "Freeze whoever your hand desires."},
new ButtonInfo { buttonText = "Attic Freeze All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Master.AtticFreezeAll(), toolTip = "Freezes everyone in the room when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Attic Float Gun", method =() => Master.AtticFloatGun(), toolTip = "Makes whoever your hand desires float."},
new ButtonInfo { buttonText = "Attic Float All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Fun.AtticFloatAll(), toolTip = "Makes everyone in the room float when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Spaz Gamemode <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.SpazGamemode(), toolTip = "Spam changes the gamemode every tenth of a second when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Change Gamemode to Casual", method =() => Overpowered.ChangeGamemode("Casual"), isTogglable = false, toolTip = "Changes the gamemode to casual."},
new ButtonInfo { buttonText = "Change Gamemode to Infection", method =() => Overpowered.ChangeGamemode("Infection"), isTogglable = false, toolTip = "Changes the gamemode to infection."},
new ButtonInfo { buttonText = "Change Gamemode to Hunt", method =() => Overpowered.ChangeGamemode("Hunt"), isTogglable = false, toolTip = "Changes the gamemode to hunt."},
new ButtonInfo { buttonText = "Change Gamemode to Paintbrawl", method =() => Overpowered.ChangeGamemode("Paintbrawl"), isTogglable = false, toolTip = "Changes the gamemode to paintbrawl."},
new ButtonInfo { buttonText = "Change Gamemode to Ambush", method =() => Overpowered.ChangeGamemode("Ambush"), isTogglable = false, toolTip = "Changes the gamemode to ambush."},
new ButtonInfo { buttonText = "Change Gamemode to Ghost Tag", method =() => Overpowered.ChangeGamemode("Ghost"), isTogglable = false, toolTip = "Changes the gamemode to ghost tag."},
new ButtonInfo { buttonText = "Change Gamemode to Guardian", method =() => Overpowered.ChangeGamemode("Guardian"), isTogglable = false, toolTip = "Changes the gamemode to guardian."},
new ButtonInfo { buttonText = "Change Gamemode to Freeze Tag", method =() => Overpowered.ChangeGamemode("FreezeTag"), isTogglable = false, toolTip = "Changes the gamemode to freeze tag."},

new ButtonInfo { buttonText = "Attic Serversided Blocks", method =() => Overpowered.AtticServersidedBlocks(), toolTip = "Lets you spawn and do anything with blocks in any map."},

new ButtonInfo { buttonText = "Change Text Location", overlapText = "Change Text Location <color=grey>[</color><color=green>Forest</color><color=grey>]</color>", method =() => Overpowered.ChangeFriendStationPosition(), isTogglable = false, toolTip = "Changes the friend position of where the text spawns."},
new ButtonInfo { buttonText = "Big Emoji <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.BigEmoji(), toolTip = "Spawns a really big emoji at stump when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Black Box <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.BlackScreenAll(), toolTip = "Spawns a really big black emoji when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Transgender Flag <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.TransgenderFlag(), toolTip = "Spawns a transgender flag when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Strobe <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.Strobe(), toolTip = "Spawns a rave when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Advertisement <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.Advertisement(), toolTip = "Spawns a really big advertisement when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Silly Face <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.SillyFace(), toolTip = "Spawns a silly face when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Testicles <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.Testicles(), toolTip = "Spawns male testicles when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Firecracker Spam <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => Overpowered.FirecrackerSpam(), toolTip = "Spams firecrackers out of your hand when holding <color=green>grip</color>."},
new ButtonInfo { buttonText = "Firecracker Spray <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => Overpowered.FirecrackerSpray(), toolTip = "Sprays firecrackers out of your hand when holding <color=green>grip</color>."},

new ButtonInfo { buttonText = "Firecracker Fountain <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.FirecrackerFountain(), toolTip = "Spams a fountain of firecrackers when holding <color=green>trigger</color>."},
new ButtonInfo { buttonText = "Firecracker Rain <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.FirecrackerRain(), toolTip = "Rains firecrackers when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Firecracker Gun", method =() => Overpowered.FirecrackerGun(), toolTip = "Spams firecrackers at wherever your hand desires."},
new ButtonInfo { buttonText = "Firecracker Airstrike Gun", method =() => Overpowered.FirecrackerAirstrikeGun(), toolTip = "Spams firecrackers down from the heavens at wherever your hand desires."},

new ButtonInfo { buttonText = "Become Firecrackers <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.BecomeFirecrackers(), toolTip = "Turns you into a bunch of firecrackers when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Firecracker Crash Gun", method =() => Overpowered.FirecrackerCrashGun(), toolTip = "Crashes whoever your hand desires with the firecrackers."},
new ButtonInfo { buttonText = "Firecracker Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.FirecrackerCrashAll(), toolTip = "Crashes everybody in the room when holding <color=green>trigger</color> with the firecrackers."},

new ButtonInfo { buttonText = "Firecracker Instant Crash Gun", method =() => Overpowered.FirecrackerInstantCrashGun(), toolTip = "Crashes whoever your hand desires with the firecrackers."},
new ButtonInfo { buttonText = "Firecracker Instant Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.FirecrackerInstantCrashAll(), toolTip = "Crashes everybody in the room when holding <color=green>trigger</color> with the firecrackers."},
new ButtonInfo { buttonText = "Repair Kick", method =() => Overpowered.RepairKick(), isTogglable = false, toolTip = "Swaps the target used for kicking, to hopefully repair any kick mods."},
new ButtonInfo { buttonText = "Auto Repair Kick", method =() => Overpowered.AutoRepairKick(), toolTip = "Automatically swaps the target used for kicking, to hopefully repair any kick mods without needing to manually press that button."},

new ButtonInfo { buttonText = "Anti Report <color=grey>[</color><color=green>Kick</color><color=grey>]</color>", method =() => Overpowered.AntiReportKick(), toolTip = "Kicks whoever tries to report you."},
new ButtonInfo { buttonText = "Leaderboard Kick", method =() => Overpowered.LeaderboardKick(), disableMethod =() => Overpowered.DisableLeaderboardKick(), toolTip = "Changes the report button into a kick button."},

new ButtonInfo { buttonText = "Kick Gun", method =() => Overpowered.KickGun(), toolTip = "Kicks whoever your hand desires."},
new ButtonInfo { buttonText = "Kick All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.KickAll(), toolTip = "Kicks everybody in the lobby when holding <color=green>trigger</color>."},

new ButtonInfo { buttonText = "Crash Gun", method =() => Overpowered.CrashGun(), toolTip = "Crashes whoever your hand desires." },
new ButtonInfo { buttonText = "Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.CrashAll(), toolTip = "Crashes everyone in the room when holding <color=green>grip</color>." },

new ButtonInfo { buttonText = "Instant Crash Gun", method =() => Overpowered.InstantCrashGun(), toolTip = "Crashes whoever your hand desires instantly." },
new ButtonInfo { buttonText = "Instant Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.InstantCrashAll(), toolTip = "Crashes everyone in the room instantly when holding <color=green>grip</color>." },

new ButtonInfo { buttonText = "Instant Crank Elves", method =() => Projectiles.InstantCrankElf(), disableMethod =() => Projectiles.DisableInstantCrankElf(), toolTip = "Makes the elf launcher instantly spawn elves when barely moving the handle." },
new ButtonInfo { buttonText = "Elf Launcher Spam <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => Projectiles.ElfLauncherSpam(), toolTip = "Spams the elf launcher cosmetic when holding <color=green>grip</color>." },
new ButtonInfo { buttonText = "Elf Gun", method =() => Projectiles.ElfGun(), toolTip = "Spams elves wherever your hand desires." },
new ButtonInfo { buttonText = "Elf Annoy Gun", method =() => Projectiles.ElfAnnoyGun(), toolTip = "Spams the elf launcher cosmetic around and towards whoever your hand desires." },
new ButtonInfo { buttonText = "Elf Airstrike Gun", method =() => Projectiles.ElfAirstrikeGun(), toolTip = "Spams the elf launcher cosmetic above and down towards whoever your hand desires." },

new ButtonInfo { buttonText = "Piece Name Helper", method =() => Fun.PieceNameHelper(), toolTip = "Remove me later."},

new ButtonInfo { buttonText = "Crash Amount", overlapText = "Crash Amount <color=grey>[</color><color=green>2</color><color=grey>]</color>", method =() => Settings.CrashAmount(), isTogglable = false, toolTip = "Changes the amount of projectiles the crash mods send."},
new ButtonInfo { buttonText = "Projectile Gun", method =() => Projectiles.ProjectileGun(), toolTip = "Acts like the projectile spam, but the projectiles only show up for you and whoever your hand desires." },

new ButtonInfo { buttonText = "Anti Ban", overlapText = "Anti Ban <color=grey>[</color><color=red>Risky</color><color=grey>]</color>", method =() => Overpowered.AntiBan(), isTogglable = false, toolTip = "Prevents you from getting banned. This mod is very experimental, if you get banned, I take ZERO responsibility."},
new ButtonInfo { buttonText = "Anti Ban Check", method =() => Overpowered.AntiBanCheck(), isTogglable = false, toolTip = "Tests if the the room is modded or not."},

new ButtonInfo { buttonText = "Set Master", overlapText = "Set Master <color=grey>[</color><color=red>Risky</color><color=grey>]</color>", method =() => Overpowered.FastMaster(), isTogglable = false, toolTip = "Sets you as master client."},
new ButtonInfo { buttonText = "Set Master Gun", overlapText = "Set Master Gun <color=grey>[</color><color=red>Risky</color><color=grey>]</color>", method =() => Overpowered.SetMasterGun(), toolTip = "Sets whoever your hand desires as master client."},
new ButtonInfo { buttonText = "Auto Set Master", overlapText = "Auto Set Master <color=grey>[</color><color=red>Risky</color><color=grey>]</color>", method =() => Experimental.AutoSetMaster(), toolTip = "Sets you as master client when in modded lobbies or when using the anti ban."},

new ButtonInfo { buttonText = "Infection Gamemode", method =() => Overpowered.InfectionGamemode(), isTogglable = false, toolTip = "Sets the gamemode to infection."},
new ButtonInfo { buttonText = "Casual Gamemode", method =() => Overpowered.CasualGamemode(), isTogglable = false, toolTip = "Sets the gamemode to casual."},
new ButtonInfo { buttonText = "Hunt Gamemode", method =() => Overpowered.HuntGamemode(), isTogglable = false, toolTip = "Sets the gamemode to hunt."},
new ButtonInfo { buttonText = "Paintbrawl Gamemode", method =() => Overpowered.PaintbrawlGamemode(), isTogglable = false, toolTip = "Sets the gamemode to paintbrawl."},

new ButtonInfo { buttonText = "Break Network Triggers", method =() => Overpowered.SSDisableNetworkTriggers(), isTogglable = false, toolTip = "Disables network triggers for everyone."},
new ButtonInfo { buttonText = "Trap Stump", method =() => Overpowered.TrapStump(), isTogglable = false, toolTip = "Anyone who enters the stump will be kicked."},

new ButtonInfo { buttonText = "Make Room Private", method =() => Overpowered.MakeRoomPrivate(), isTogglable = false, toolTip = "Makes the room private."},
new ButtonInfo { buttonText = "Make Room Public", method =() => Overpowered.MakeRoomPublic(), isTogglable = false, toolTip = "Makes the room private."},

new ButtonInfo { buttonText = "Lag Gun", method =() => Experimental.LagGun(), toolTip = "Lags whoever your hand desires." },
new ButtonInfo { buttonText = "Lag All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Experimental.LagAll(), toolTip = "Lags everyone when holding <color=green>trigger</color>." },

new ButtonInfo { buttonText = "Crash Gun", method =() => Experimental.CrashGun(), toolTip = "Crashes whoever your hand desires." },
new ButtonInfo { buttonText = "Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Experimental.CrashAll(), toolTip = "Crashes everyone when holding <color=green>trigger</color>." },

new ButtonInfo { buttonText = "Change Name Gun", method =() => Experimental.ChangeNameGun(), toolTip = "Changes whoever your hand desires' name to your name. Credits to kman for creating the original method." },
new ButtonInfo { buttonText = "Change Name All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Experimental.ChangeNameAll(), toolTip = "Changes everyone's name to your name. Credits to kman for creating the original method." },

new ButtonInfo { buttonText = "Destroy Gun", method =() => Overpowered.DestroyGun(), toolTip = "Makes new players not see whoever your hand desires." },
new ButtonInfo { buttonText = "Destroy All", method =() => Overpowered.DestroyAll(), isTogglable = false, toolTip = "Every player that joins after you will not be able to see anyone." },

new ButtonInfo { buttonText = "Acid Self", method =() => Overpowered.AcidSelf(), isTogglable = false, toolTip = "Turns you into acid." },
new ButtonInfo { buttonText = "Acid Gun", method =() => Overpowered.AcidGun(), toolTip = "Turns whoever your hand desires into acid." },
new ButtonInfo { buttonText = "Acid All", method =() => Overpowered.AcidAll(), isTogglable = false, toolTip = "Turns everyone into acid." },

new ButtonInfo { buttonText = "Lag Gun <color=grey>[</color><color=purple>Experimental</color><color=grey>]</color>", method =() => Overpowered.LagGun(), toolTip = "Lags whoever your hand desires." },
new ButtonInfo { buttonText = "Lag All <color=grey>[</color><color=purple>Experimental</color><color=grey>]</color> <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.LagAll(), toolTip = "Lags everyone when holding <color=green>trigger</color>." },

new ButtonInfo { buttonText = "Crash Gun <color=grey>[</color><color=purple>Experimental</color><color=grey>]</color>", method =() => Overpowered.CrashGun(), toolTip = "Crashes whoever your hand desires." },
new ButtonInfo { buttonText = "Crash All <color=grey>[</color><color=purple>Experimental</color><color=grey>]</color> <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.CrashAll(), toolTip = "Crashes everyone when holding <color=green>trigger</color>." },

new ButtonInfo { buttonText = "Unacid Self", method =() => Fun.UnacidSelf(), isTogglable = false, toolTip = "Unturns you into acid." },
new ButtonInfo { buttonText = "Unacid Gun", method =() => Fun.UnacidGun(), toolTip = "Unturns whoever your hand desires into acid." },
new ButtonInfo { buttonText = "Unacid All", method =() => Fun.UnacidAll(), isTogglable = false, toolTip = "Unturns everyone into acid." },

new ButtonInfo { buttonText = "Anti Report <color=grey>[</color><color=green>Lag</color><color=grey>]</color>", method =() => Safety.AntiReportLag(), toolTip = "Lags whoever comes near your report button."},
new ButtonInfo { buttonText = "Anti Report <color=grey>[</color><color=green>Crash</color><color=grey>]</color>", method =() => Safety.AntiReportCrash(), toolTip = "Crashes whoever comes near your report button."}

new ButtonInfo { buttonText = "Crash Gun", method =() => Overpowered.CrashGun(), toolTip = "Crashes or lags whoever your hand desires." },
new ButtonInfo { buttonText = "Crash All <color=grey>[</color><color=green>T</color><color=grey>]</color>", method =() => Overpowered.CrashAll(), toolTip = "Crashes every quest player, and lags/crashes every steam player when holding <color=green>trigger</color>" },
new ButtonInfo { buttonText = "Random Color Snowballs", enableMethod =() => Projectiles.RandomColorSnowballs(), disableMethod =() => Projectiles.NoRandomColorSnowballs(), toolTip = "Makes your snowballs random colors." },
new ButtonInfo { buttonText = "Black Snowballs", enableMethod =() => Projectiles.BlackSnowballs(), disableMethod =() => Projectiles.FixBlackSnowballs(), toolTip = "Makes your snowballs black." },

new ButtonInfo { buttonText = "Lag Gun", method =() => Overpowered.BubbleGun(), toolTip = "Spawns a massive bubble which lags whoever your hand desires." },
new ButtonInfo { buttonText = "Lag All", method =() => Overpowered.BubbleAll(), toolTip = "Spawns a massive bubble which lags everyone." },

new ButtonInfo { buttonText = "Break Bug", method =() => Fun.BreakBug(), isTogglable = false, toolTip = "Breaks the bug." },
new ButtonInfo { buttonText = "Break Bat", method =() => Fun.BreakBat(), isTogglable = false, toolTip = "Breaks the bat." },

new ButtonInfo { buttonText = "Steal Bug", method =() => Fun.StealBug(), toolTip = "Steals the bug." },
new ButtonInfo { buttonText = "Steal Bat", method =() => Fun.StealBat(), toolTip = "Steals the bat." },

new ButtonInfo { buttonText = "Spaz Voice", method =() => Fun.SpazVoice(), disableMethod =() => Fun.UnspazVoice(), toolTip = "Spazzes your voice out. Only works with monke speak on."},

new ButtonInfo { buttonText = "Acid Self", method =() => Basement.SodaSelf(), isTogglable = false, toolTip = "Turns you into soda."},
new ButtonInfo { buttonText = "Unacid Self", method =() => Basement.UnsodaSelf(), isTogglable = false, toolTip = "Turns you not into soda."},

new ButtonInfo { buttonText = "Grab Train", method =() => Fun.GrabTrain(), toolTip = "Puts the train in your hand." },
new ButtonInfo { buttonText = "Train Gun", method =() => Fun.TrainGun(), toolTip = "Moves the train to wherever your hand desires." },
new ButtonInfo { buttonText = "Destroy Train", method =() => Fun.DestroyTrain(), isTogglable = false, toolTip = "Sends the train to hell." },
new ButtonInfo { buttonText = "Slow Train", enableMethod =() => Fun.SlowTrain(), disableMethod =() => Fun.FixTrain(), toolTip = "Makes the train slower." },
new ButtonInfo { buttonText = "Fast Train", enableMethod =() => Fun.FastTrain(), disableMethod =() => Fun.FixTrain(), toolTip = "Makes the train faster." },

new ButtonInfo { buttonText = "Lava Splash Hands <color=grey>[</color><color=green>G</color><color=grey>]</color>", method =() => Fun.LavaSplashHands(), toolTip = "Splashes lava when holding <color=green>grip</color>."},
new ButtonInfo { buttonText = "Lava Splash Aura", method =() => Fun.LavaSplashAura(), toolTip = "Splashes lava around you at random positions."},
new ButtonInfo { buttonText = "Lava Splash Gun", method =() => Fun.LavaSplashGun(), toolTip = "Splashes lava wherever your hand desires."},

new ButtonInfo { buttonText = "Force Erupt Lava", method =() => Overpowered.ForceEruptLava(), isTogglable = false, toolTip = "Forcibly rises the lava." },
new ButtonInfo { buttonText = "Force Drain Lava", method =() => Overpowered.ForceUneruptLava(), isTogglable = false, toolTip = "Forcibly drains the lava." },
new ButtonInfo { buttonText = "Instant Rise Lava", method =() => Overpowered.ForceRiseLava(), isTogglable = false, toolTip = "Instantly rises the lava." },
new ButtonInfo { buttonText = "Instant Drain Lava", method =() => Overpowered.ForceDrainLava(), isTogglable = false, toolTip = "Instantly drains the lava." },
new ButtonInfo { buttonText = "Spaz Lava", method =() => Overpowered.SpazLava(), toolTip = "Spazzes out the lava." },

new ButtonInfo { buttonText = "Kill Bees", method =() => Fun.KillBees(), isTogglable = false, toolTip = "Sends the bees to hell."},
new ButtonInfo { buttonText = "Anger Bees Self", method =() => Fun.AngerBees(), isTogglable = false, toolTip = "Angers the bees on you."},
new ButtonInfo { buttonText = "Anger Bees Gun", method =() => Fun.AngerBeesGun(), toolTip = "Angers the bees wherever your hand desires."},
new ButtonInfo { buttonText = "Anger Bees All", method =() => Fun.AngerBeesAll(), toolTip = "Angers the bees on everyone."},

new ButtonInfo { buttonText = "Sting Self", method =() => Fun.StingSelf(), isTogglable = false, toolTip = "Makes the bees attack you."},
new ButtonInfo { buttonText = "Sting Gun", method =() => Fun.StingGun(), toolTip = "Makes the bees attack whoever your hand desires."},
new ButtonInfo { buttonText = "Sting All", method =() => Fun.StingAll(), toolTip = "Makes the bees attack everyone."},

new ButtonInfo { buttonText = "Remove Christmas Lights", enableMethod =() => Advantages.EnableRemoveChristmasLights(), disableMethod =() => Advantages.DisableRemoveChristmasLights(), toolTip = "Removes lights, good for walls."},
new ButtonInfo { buttonText = "Remove Winter Decorations", enableMethod =() => Advantages.EnableRemoveChristmasDecorations(), disableMethod =() => Advantages.DisableRemoveChristmasDecorations(), toolTip = "Removes snowmen and such, good for anyone but very obvious."},
new ButtonInfo { buttonText = "Projectile Bomb <color=grey>[</color><color=green>A</color><color=grey>]</color>", method =() => Projectiles.ProjectileBomb(), disableMethod =() => Projectiles.DisableProjectileBomb(), toolTip = "Acts like C4, but instead of launching you, it spawns 5 projectiles in random directions." },

new ButtonInfo { buttonText = "Gorilla Voice <color=grey>[</color><color=green>A</color><color=grey>]</color>", method =() => Fun.GorillaVoice(), toolTip = "Turns your voice into the gorilla voice when holding <color=green>A</color>."},
new ButtonInfo { buttonText = "Spam Eat Honey Comb", method =() => Fun.HoneycombSpam(), toolTip = "Spam eats the honey comb when holding <color=green>grip</color>."},

new ButtonInfo { buttonText = "Remove Self from Leaderboard", method =() => Overpowered.RemoveSelfFromLeaderboard(), isTogglable = false, toolTip = "Removes yourself from the leaderboard." },

new ButtonInfo { buttonText = "Start Moon Event", method =() => Overpowered.StartMoonEvent(), isTogglable = false, toolTip = "Starts the moon event."},
new ButtonInfo { buttonText = "End Moon Event", method =() => Overpowered.EndMoonEvent(), isTogglable = false, toolTip = "Ends the moon event."},
new ButtonInfo { buttonText = "Spaz Moon Event", method =() => Overpowered.FlashScreen(), toolTip = "Spazzes out the moon event."},

new ButtonInfo { buttonText = "Spawn Red Lucy", method =() => Fun.SpawnRedLucy(), isTogglable = false, toolTip = "Summons the red Lucy in forest." },
new ButtonInfo { buttonText = "Spawn Blue Lucy", method =() => Fun.SpawnBlueLucy(), isTogglable = false, toolTip = "Summons the blue Lucy in forest." },
new ButtonInfo { buttonText = "Despawn Lucy", method =() => Fun.DespawnLucy(), isTogglable = false, toolTip = "Despawns lucy in forest." },
new ButtonInfo { buttonText = "Spaz Lucy", method =() => Fun.SpazLucy(), toolTip = "Gives lucy a seizure." },

new ButtonInfo { buttonText = "Lucy Chase Self", method =() => Overpowered.LucyChaseSelf(), isTogglable = false, toolTip = "Makes lucy chase you." },
new ButtonInfo { buttonText = "Lucy Chase Gun", method =() => Fun.LucyChaseGun(), toolTip = "Makes lucy chase whoever your hand desires." },

new ButtonInfo { buttonText = "Lucy Attack Self", method =() => Overpowered.LucyAttackSelf(), isTogglable = false, toolTip = "Makes lucy attack you." },
new ButtonInfo { buttonText = "Lucy Attack Gun", method =() => Fun.LucyAttackGun(), toolTip = "Makes lucy attack whoever your hand desires." },
new ButtonInfo { buttonText = "Annoying Lucy", method =() => Fun.AnnoyingLucy(), toolTip = "Makes lucy really annoying, by attacking everyone and making sounds of the bells." },

new ButtonInfo { buttonText = "Fast Lucy", method =() => Fun.FastLucy(), toolTip = "Makes lucy become really fast." },
new ButtonInfo { buttonText = "Slow Lucy", method =() => Fun.SlowLucy(), toolTip = "Makes lucy become really slow." },
new ButtonInfo { buttonText = "Anti Lucy", enableMethod =() => LucyRise.enabled = true, disableMethod =() => LucyRise.enabled = false, toolTip = "Prevents lucy from moving you."},
new ButtonInfo { buttonText = "Disable Lucy",  enableMethod =() => LucyChaser.enabled = true, disableMethod =() => LucyChaser.enabled = false, toolTip = "Prevents lucy from spawning."},
new ButtonInfo { buttonText = "Anti Lurker", enableMethod =() => LurkerStates.enabled = true, method = Safety.AntiLurker, disableMethod =() => LurkerStates.enabled = false, toolTip = "Prevents the lurker ghost from possessing you."},
new ButtonInfo { buttonText = "Kick on Grab", postMethod =() => Overpowered.TowardsPositionOnGrab(new Vector3(-71.33718f, 101.4977f, -93.09029f)), disableMethod =() => VRRig.LocalRig.enabled = true, toolTip = "Kicks the player when they grab you." },
new ButtonInfo { buttonText = "Crash on Grab", postMethod =() => Overpowered.DirectionOnGrab(new Vector3(10000, 10000, 10000)), disableMethod =() => VRRig.LocalRig.enabled = true, toolTip = "Crashes the player when they grab you." },
new ButtonInfo { buttonText = "Destroy on Grab", postMethod =() => Overpowered.DirectionOnGrab(new Vector3(1f, -1f, 1f)), disableMethod =() => VRRig.LocalRig.enabled = true, toolTip = "Destroys the player when they grab you." },
new ButtonInfo { buttonText = "Obliterate on Grab", postMethod =() => Overpowered.DirectionOnGrab(Vector3.up), disableMethod =() => VRRig.LocalRig.enabled = true, toolTip = "Obliterates the player when they grab you." },
new ButtonInfo { buttonText = "Towards Point on Grab Gun", postMethod = Overpowered.TowardsPointOnGrab, disableMethod = Overpowered.DisableTowardsPointOnGrab, toolTip = "Sends the player to your target position when they grab you." },
 */
