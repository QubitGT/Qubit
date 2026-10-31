/*
 * Qubit Menu  Frontend/Settings.cs
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

using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Colors;
using Qubit.Libraries.Interface;
using Qubit.Modules;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using static Qubit.Frontend.Classes.QubitMenuBody;
using static Qubit.Frontend.Resources.Fonts;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Frontend
{
    public static class Settings
    {
        #region Appearance

        public static ExtGradient backgroundColor = new ExtGradient
        {
            colors = ExtGradient.GetSolidGradient(
                new Color32(118, 6, 252, 128)
            )
        };

        public static ExtGradient menuBackgroundColor = new ExtGradient
        {
            colors = ExtGradient.GetSolidGradient(
                new Color32(22, 22, 22, 128)
            )
        };

        public static ExtGradient[] buttonColors = {
            new ExtGradient
            {
                colors = ExtGradient.GetSolidGradient(
                    new Color32(118, 6, 252, 255)
                )
            },
            new ExtGradient
            {
                colors = ExtGradient.GetSolidGradient(
                    new Color32(88, 6, 186, 255)
                )
            }
        };

        public static ExtGradient[] textColors = {
            new ExtGradient
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            },
            new ExtGradient
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            },
            new ExtGradient
            {
                colors = ExtGradient.GetSolidGradient(Color.white)
            }
        };

        public static int arrowType;
        public static readonly string[][] arrowTypes = {
            new[] {"<", ">"},
            new[] {"←", "→"},
            new[] { "    <sprite name=\"Left1\">", "    <sprite name=\"Right1\">"},
            new[] {"◄", "►"},
            new[] { "    <sprite name=\"Left2\">", "    <sprite name=\"Right2\">"},
            new[] {"‹", "›"},
            new[] {"«", "»"},
            new[] { "    <sprite name=\"Left3\">", "    <sprite name=\"Right3\">"},
            new[] {"-", "+"},
            new[] {"", ""},
            new[] {"v", "ʌ"},
            new[] { "v\nv\nv\nv\nv\nv", "ʌ\nʌ\nʌ\nʌ\nʌ\nʌ" }
        };

        public static int themeType = 0;
        public static bool slowFadeColors;

        public static Vector3 pointerOffset = new Vector3(0f, -0.1f, 0f);

        public static bool thinMenu = true;
        public static bool longMenu;
        public static bool hideTitle;
        public static bool flipMenu;
        public static bool shinyMenu;
        public static bool transparentMenu;
        public static bool crystallizeMenu;
        public static bool outlineMenu;
        public static bool outlineText;
        public static bool underlineText;
        public static bool strikethroughText;
        public static bool smallCapsText;
        public static bool innerOutline;
        public static bool smoothLines;
        public static bool roundMenu;
        public static bool noPageNumber;
        public static bool disablePageButtons;
        public static bool swapButtonColors;
        public static int pageButtonType = 1;
        public static bool hideBrackets;
        public static bool flipArraylist;
        public static bool hidePointer;
        public static bool rockWatermark = false;
        public static bool disableWatermark;
        public static bool noAutoSizeText;
        public static bool customMenuBackground;
        public static bool menuTrail;
        public static bool adaptiveButtons = true;
        public static int pcBackgroundIndex;
        public static bool scaleWithPlayer;
        public static float menuScale = 1f;
        public static int notificationScale = 30;
        public static int overlayScale = 30;
        public static int arraylistScale = 20;
        public static bool dynamicGradients;
        public static bool horizontalGradients;
        public static bool scrollingGradients;
        public static bool legacyGradients;
        public static bool particleSpawnEffect;
        public static bool animatedTitle;
        public static bool gradientTitle;
        public static Texture2D customMenuBackgroundImage;
        public static Texture2D customWatermark;

        #endregion

        #region State

        public static bool isOnPC;
        public static bool IsSteam = true;
        public static bool Lockdown;

        public static bool HasLoaded;
        public static bool allowDetected;
        public static bool allowLegacy;
        public static float playTime;
        public static int frameCount;
        public static bool disorganized;
        public static bool zeroGravityMenu;
        public static bool menuCollisions;
        public static bool dropOnRemove = true;
        public static bool openedwithright;
        public static bool oneHand;
        public static bool clickGUI;

        public static int _pageSize = 8;
        public static int PageSize
        {
            get => UseQubit ? QubitPageCapacity : _pageSize - buttonOffset;
            set => _pageSize = value;
        }

        public static float ButtonDistance
        {
            get => 0.8f / (PageSize + buttonOffset);
        }

        public static int pageOffset;
        public static int pageNumber;
        public static bool pageScrolling;

        public static int buttonClickSound = 8;
        public static int buttonClickVolume = 4;
        public static int buttonOffset = 0;
        public static int menuButtonIndex = 1;
        public static bool toggleButton;
        public static bool toggleButtonActive;

        public static bool doButtonsVibrate = true;
        public static bool serversidedButtonSounds;

        public static bool joystickMenu;

        public static bool physicalMenu;

        public static bool barkMenu;
        public static bool barkMenuOpen;

        public static Vector3 physicalOpenPosition = Vector3.zero;
        public static bool joystickOpen;
        public static bool smoothMenuPosition;
        public static bool smoothMenuRotation;
        public static int joystickButtonSelected;
        public static string joystickSelectedButton = "";

        public static int joystickMenuPosition;
        public static Vector3[] joystickMenuPositions = {
            new Vector3(0.3f, 0.2f, 1f),
            new Vector3(-0.3f, 0.2f, 1f),
            new Vector3(0.3f, -0.2f, 1f),
            new Vector3(-0.3f, -0.2f, 1f),
            new Vector3(0f, -0.1f, 0.5f),
            new Vector3(0.3f, -0.1f, 1f),
            new Vector3(-0.3f, -0.1f, 1f),
            new Vector3(0f, 0.2f, 1f),
            new Vector3(0f, -0.2f, 1f),
        };

        public static bool rightHand;
        public static bool bothHands;
        public static bool wristMenu;
        public static bool explodeMenu;
        public static bool watchMenu;

        public static bool stackNotifications;
        public static bool disableNotifications;
        public static bool disableMasterClientNotifications;
        public static bool disableRoomNotifications;
        public static bool disablePlayerNotifications;
        public static bool clearNotificationsOnDisconnect;
        public static bool showEnabledModsVR = true;
        public static bool advancedArraylist;
        public static bool hideSettings;
        public static bool hideMacros;
        public static bool hideTextOnCamera;
        public static bool incrementalButtons = true;
        public static bool disableDisconnectButton;
        public static bool disableFpsCounter;
        public static bool disableReturnButton;
        public static bool enableDebugButton;

        public static bool ghostException;
        public static bool disableGhostview;
        public static bool legacyGhostview;
        public static bool checkMode;
        public static string CosmeticsOwned;

        public static bool doCustomName;
        public static string customMenuName = "Your Text Here";
        public static readonly string menuName =
#if LEGAL
            "<b>Qubit</b> Legal";
#else
            "<b>Qubit</b>";
#endif

        public static int? fullModAmount;

        public static bool dynamicSounds;
        public static bool exclusivePageSounds;
        public static bool dynamicAnimations;
        public static bool slowDynamicAnimations;
        public static string lastClickedName = "";

        public static Key KeyboardBind =>
            Modules.Categories.Settings.pcBindings.TryGetValue(Modules.Categories.Settings.ControllerBinding.Keyboard, out Key key) ? key : Key.Q;

        public static bool isMenuButtonHeld;
        public static bool shouldBePC;

        public static bool ToggleBindings = true;
        public static bool IsBinding;
        public static string BindInput = "";
        public static bool IsRebinding;

        public static readonly Dictionary<string, List<string>> ModBindings = new Dictionary<string, List<string>> {
            { "A", new List<string>() },
            { "B", new List<string>() },
            { "X", new List<string>() },
            { "Y", new List<string>() },
            { "LG", new List<string>() },
            { "RG", new List<string>() },
            { "LT", new List<string>() },
            { "RT", new List<string>() },
            { "LJ", new List<string>() },
            { "RJ", new List<string>() },
        };

        public static readonly List<string> quickActions = new List<string>();

        public static Camera TPC;
        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static VideoPlayer videoPlayer;
        public static SphereCollider buttonCollider;
        public static GameObject canvasObj;
        public static TextMeshPro fpsCount;
        public static Image watermarkImage;
        public static bool fpsCountTimed;
        public static bool fpsCountAverage;
        public static bool ftCount;
        public static bool acceptedDonations;
        public static float lastDeltaTime = 1f;
        public static TextMeshPro title;
        public static VRRig GhostRig;
        public static Material CrystalMaterial;
        public static Material watermarkMat;
        public static GameObject menuSpawnPosition;

        public static GameObject TryOnRoom;

        public class WatchInfo
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
            public GameObject shell;
            public Image indicator;

            public void Update()
            {
                if (gameObject == null || shell == null || text == null) return;
                Renderer renderer = shell.GetComponent<Renderer>();
                if (text.font != activeFont)
                    text.SafeSetFont(activeFont);
                if (text.fontStyle != activeFontStyle)
                    text.SafeSetFontStyle(activeFontStyle);
                if (renderer.sharedMaterial != CustomBoards.BoardMaterial)
                    renderer.sharedMaterial = CustomBoards.BoardMaterial;
            }
        }

        public static List<WatchInfo> Watches = new List<WatchInfo>();

        public static readonly List<string> favorites = new List<string> { "Exit Favorite Mods" };
        public static readonly List<string> skipButtons = new List<string> { };
        public static bool translate;

        public static Vector3 closePosition;

        public static float timeMenuStarted = -1f;
        public static float buttonCooldown;

        public static int notificationDecayTime = 1000;
        public static bool redactText;

        public static bool annoyingMode;

        [RunOnLaunch(LaunchPriority.Settings)]
        public static void Initialize()
        {
            timeMenuStarted = Time.time;
            IsSteam = ManagerRegistry.PlayFabAuthenticator.platform;
        }

        [RunOnLaunch(LaunchPriority.Scene)]
        public static void InitializeScene()
        {
            CrystalMaterial = GameObj.GetObject("Environment Objects/LocalObjects_Prefab/ForestToCave/C_Crystal_Chunk")?.GetComponent<Renderer>()?.material;
            TryOnRoom = GameObj.GetObject("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Cosmetics Room Triggers/TryOnRoom");

            fullModAmount ??= Manager.modules.Values.SelectMany(list => list).ToArray().Length;
        }

        #endregion
    }
}
