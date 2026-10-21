/*
 * Qubit Menu  Modules/Categories/Movement.cs
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
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Frontend.Colors;
using Qubit.Libraries.Audio;
using Qubit.Libraries.Controls;
using Qubit.Libraries.Core;
using Qubit.Libraries.Entities;
using Qubit.Libraries.Interface;
using Qubit.Libraries.Networking;
using Qubit.Modules.Classes;
using Qubit.Patches.Locomotion;
using Qubit.Patches.Netcode;
using Qubit.Patches.Rigs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Valve.Newtonsoft.Json.Linq;
using WebSocketSharp;
using static Qubit.Core.Security;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Keyboard;
using static Qubit.Frontend.Prompts;
using static Qubit.Frontend.Resources.Fonts;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Audio.Sounds;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Libraries.Core.Assets;
using static Qubit.Libraries.Networking.LocalPlayer;
using static Qubit.Libraries.Networking.Serialization;
using static Qubit.Modules.Classes.GunLibrary;
using static Qubit.Modules.Manager;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using TextMeshPro = TMPro.TextMeshPro;

namespace Qubit.Modules.Categories
{
    public static class Movement
    {
        #region Types
        public struct PlayerPosition
        {
            public Vector3 position;
            public Vector3 velocity;

            public (Vector3 position, Quaternion rotation) leftHand;
            public (Vector3 position, Quaternion rotation) rightHand;

            public bool leftGrip;
            public bool rightGrip;

            public bool leftTrigger;
            public bool rightTrigger;

            public readonly void MoveTo()
            {
                TeleportPlayer(position);
                References.Rigidbody.linearVelocity = velocity;

                References.LeftHand.position = leftHand.position;
                References.LeftHand.rotation = leftHand.rotation;

                References.RightHand.position = rightHand.position;
                References.RightHand.rotation = rightHand.rotation;

                References.InputPoller.leftControllerGripFloat = leftGrip ? 1f : 0f;
                References.InputPoller.rightControllerGripFloat = rightGrip ? 1f : 0f;

                References.InputPoller.leftControllerIndexFloat = leftTrigger ? 1f : 0f;
                References.InputPoller.rightControllerIndexFloat = rightTrigger ? 1f : 0f;
            }

            public static PlayerPosition CurrentPosition() =>
                new PlayerPosition
                {
                    position = References.BodyCollider.transform.position,
                    velocity = References.Rigidbody.linearVelocity,

                    leftHand = (References.LeftHand.position, References.LeftHand.rotation),
                    rightHand = (References.RightHand.position, References.RightHand.rotation),

                    leftGrip = leftGrab,
                    rightGrip = rightGrab,

                    leftTrigger = false,
                    rightTrigger = Controllers.rightTrigger > 0.5f
                };

            public readonly JObject ToJObject() => new JObject
            {
                ["position"] = Vec3ToJObject(position),
                ["velocity"] = Vec3ToJObject(velocity),

                ["leftHand"] = new JObject
                {
                    ["position"] = Vec3ToJObject(leftHand.position),
                    ["rotation"] = QuatToJObject(leftHand.rotation)
                },
                ["rightHand"] = new JObject
                {
                    ["position"] = Vec3ToJObject(rightHand.position),
                    ["rotation"] = QuatToJObject(rightHand.rotation)
                },
                ["leftGrip"] = leftGrip,
                ["rightGrip"] = rightGrip,
                ["leftTrigger"] = leftTrigger,
                ["rightTrigger"] = rightTrigger
            };

            public static PlayerPosition FromJObject(JObject obj)
            {
                var position = new PlayerPosition
                {
                    position = JObjectToVec3((JObject)obj["position"]),
                    velocity = JObjectToVec3((JObject)obj["velocity"]),

                    leftHand = (
                        JObjectToVec3((JObject)obj["leftHand"]["position"]),
                        JObjectToQuat((JObject)obj["leftHand"]["rotation"])
                    ),
                    rightHand = (
                        JObjectToVec3((JObject)obj["rightHand"]["position"]),
                        JObjectToQuat((JObject)obj["rightHand"]["rotation"])
                    )
                };

                if (obj["leftGrip"] != null)
                    position.leftGrip = (bool)obj["leftGrip"];

                if (obj["rightGrip"] != null)
                    position.rightGrip = (bool)obj["rightGrip"];

                if (obj["leftTrigger"] != null)
                    position.leftTrigger = (bool)obj["leftTrigger"];

                if (obj["rightTrigger"] != null)
                    position.rightTrigger = (bool)obj["rightTrigger"];

                return position;
            }

            private static JObject Vec3ToJObject(Vector3 v) =>
                new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };

            private static Vector3 JObjectToVec3(JObject obj) =>
                new Vector3((float)obj["x"], (float)obj["y"], (float)obj["z"]);

            private static JObject QuatToJObject(Quaternion q) =>
                new JObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };

            private static Quaternion JObjectToQuat(JObject obj) =>
                new Quaternion((float)obj["x"], (float)obj["y"], (float)obj["z"], (float)obj["w"]);
        }

        public class Macro
        {
            public List<PlayerPosition> positions;
            public float macroStepDuration;
            public string name;
            public bool enabled;

            public string DumpJSON()
            {
                var obj = new JObject
                {
                    ["name"] = name,
                    ["enabled"] = enabled,
                    ["positions"] = new JArray(positions.ConvertAll(p => p.ToJObject())),
                    ["step-time"] = macroStepDuration
                };
                return obj.ToString();
            }

            public static Macro LoadJSON(string json)
            {
                var obj = JObject.Parse(json);

                var macro = new Macro
                {
                    name = (string)obj["name"],
                    enabled = (bool)obj["enabled"],
                    positions = new List<PlayerPosition>()
                };

                foreach (var token in (JArray)obj["positions"])
                    macro.positions.Add(PlayerPosition.FromJObject((JObject)token));

                macro.macroStepDuration = obj.TryGetValue("step-time", out JToken stepTimeToken) ? (float)stepTimeToken : 0.1f;

                return macro;
            }
        }
        #endregion

        #region Settings
        public static int platformMode;
        public static readonly string[] PlatformTypeNames = {
            "Normal", "Invisible", "Rainbow", "Random Color", "Noclip", "Glass", "Projectile"
        };

        public static int platformShape;
        public static readonly string[] PlatformShapeNames = {
            "Sphere", "Cube", "Cylinder", "Legacy", "Small", "Long", "1x1", "Massive"
        };

        public static int flySpeedCycle = 1;
        public static float _flySpeed = 10f;

        public static float FlySpeed
        {
            get => _flySpeed * (scaleWithPlayer ? References.Player.scale : 1f);
            set => _flySpeed = value;
        }

        public static int speedboostCycle = 1;
        public static float jspeed = 7.5f;
        public static float jmulti = 1.1f;

        public static int longarmCycle = 2;
        public static float armlength = 1.25f;

        public static GameObject leftplat;
        public static GameObject rightplat;

        private static readonly Dictionary<bool, List<GameObject>> frozonicPlatforms = new Dictionary<bool, List<GameObject>>();
        private static readonly Dictionary<bool, int> platformIndex = new Dictionary<bool, int>();

        public static readonly float[] SpeedBoostAmounts = { 2f, 7.5f, 8f, 9f, 200f };
        public static readonly float[] SpeedBoostMultipliers = { 0.5f, 1.1f, 1.5f, 2f, 10f };
        public static readonly string[] SpeedBoostNames = { "Slow", "Normal", "Middle", "Fast", "Ultra Fast" };

        public static readonly float[] FlySpeedAmounts = { 5f, 10f, 30f, 60f, 0.5f };
        public static readonly string[] FlySpeedNames = { "Slow", "Normal", "Fast", "Extra Fast", "Extra Slow" };

        public static int playspaceAbuseIndex;
        public static readonly float[] PlayspaceAbuseAmounts = { 0.004f, 0.01f, 0.1f, 0.001f, 0.002f };
        public static readonly string[] PlayspaceAbuseNames = { "Normal", "Fast", "Extra Fast", "Extra Slow", "Slow" };

        public static readonly float[] ArmLengthAmounts = { 0.75f, 1.1f, 1.25f, 1.5f, 2f };
        public static readonly string[] ArmLengthNames = { "Shorter", "Unnoticable", "Normal", "Long", "Extreme" };

        public static bool noclip;

        public static float startX = -1f;
        public static float startY = -1f;

        public static float subThingy;
        public static float subThingyZ;

        public static Vector3 lastPosition = Vector3.zero;

        private static float driveSpeed;
        public static int driveInt;
        public static readonly float[] DriveSpeedAmounts = { 10f, 30f, 50f, 100f, 3f };
        public static readonly string[] DriveSpeedNames = { "Normal", "Fast", "Ultra Fast", "The Flash", "Slow" };

        public static int fastRopesInt;
        public static readonly float[] FastRopesSpeedAmounts = { 5f, 10f, 30f };
        public static readonly string[] FastRopesSpeedNames = { "Normal", "Fast", "Ultra Fast" };

        public static Vector2 driveLerpDirection = Vector2.zero;

        private static bool previousDash;

        private static readonly float revCooldown = 0.5f;
        private static float nextrevTime = 0f;

        private static float flapTime;

        private static float loaoalsode;

        public static Vector3 rightgrapplePoint;
        public static Vector3 leftgrapplePoint;
        public static SpringJoint rightjoint;
        public static SpringJoint leftjoint;
        public static bool isLeftGrappling;
        public static bool isRightGrappling;







        public static GameObject portalGun;
        public static GameObject bluePortal;
        public static GameObject orangePortal;
        public static GameObject crosshair;

        public static bool playedOpen;
        public static bool flipped;
        public static bool inPortal;

        public static float portalDelay;
        public static float flipDelay;

        private static List<Vector3> posArchive;

        public static Vector3 leftPos = Vector3.zero;
        public static Vector3 rightPos = Vector3.zero;
        public static bool lastOnBranch;

        private static readonly List<object[]> playerPositions = new List<object[]>();

        public static float macroPlaybackRange = 1f;
        public static int macroPlaybackRangeIndex = 1;
        public static readonly float[] MacroPlaybackRangeAmounts = { 0.5f, 1f, 2f, 3f, 0.25f };
        public static readonly string[] MacroPlaybackRangeNames = { "Small", "Normal", "Large", "Extra Large", "Extra Small" };

        public static Dictionary<string, Macro> macros = new Dictionary<string, Macro>();

        public static bool recordingMacro;
        public static float positionDelay;
        public const float defaultMacroStep = 0.05f;
        public static List<PlayerPosition> recordingData = new List<PlayerPosition>();

        public static Coroutine activeMacro;

        public static bool frameStepper;
        public static bool midpointMacros;
        public static bool didMacro;
        public static bool directionBased;

        private static bool frameCompleted;
        private static bool frameStepperNotified;

        private static Vector3 walkPos;
        private static Vector3 walkNormal;

        public static int wallWalkStrengthIndex = 2;
        private static float wallWalkStrength = 9.81f;

        public static bool leftWallWalk;
        public static bool bothWallWalk;

        public static readonly float[] WallWalkStrengthAmounts = { 2f, 5f, 9.81f, 15f, 50f };
        public static readonly string[] WallWalkStrengthNames = { "Very Weak", "Weak", "Normal", "Strong", "Very Strong" };

        static Quaternion spiderRot;
        static Quaternion tappedRot;

        private static int rememberPageNumber;
        public static readonly Dictionary<string, GTZone> mapData = new Dictionary<string, GTZone>()
        {
            { "Forest", GTZone.forest },
            { "Ranked Forest", GTZone.ranked },
            { "City", GTZone.city },
            { "Canyons", GTZone.canyon },
            { "Clouds", GTZone.skyJungle },
            { "Caves", GTZone.cave },
            { "Beach", GTZone.beach },
            { "Mountains", GTZone.mountain },
            { "Basement", GTZone.basement },
            { "Metropolis", GTZone.Metropolis },
            { "Arcade", GTZone.arcade },
            { "Critters", GTZone.critters },
            { "Skate Park", GTZone.hoverboard },
            { "Monke Blocks", GTZone.monkeBlocks },
            { "Ghost Reactor", GTZone.ghostReactor },
            { "Rotating", GTZone.rotating },
            { "Bayou", GTZone.bayou },
            { "Virtual Stump", GTZone.customMaps },
            { "GTFC", GTZone.GTFC },
            { "Lava Forest", GTZone.VIMExperience1 },
            { "Space Map", GTZone.spaceMap },
        };

        public static bool previousTeleportTrigger;

        public static GameObject CheckPoint;

        public static int selectedCheckpoint = -1;
        public static float selectedCheckpointDelay;
        public static readonly List<GameObject> checkpoints = new List<GameObject>();

        public static GameObject BombObject;

        private static GameObject pearl;
        private static Texture2D pearltxt;
        private static Material pearlmat;
        private static bool isrighthandedpearl;

        public static Playspace playspace;

        public static readonly List<GameObject> forestColliders = new List<GameObject>();

        public static bool wasDisabledAlready;
        public static bool invisMonke;

        public static bool lastHit;
        public static bool lastHit2;

        private static bool ghostMonke;

        public static Vector3 offsetLH = Vector3.zero;
        public static Vector3 offsetRH = Vector3.zero;
        public static Vector3 offsetH = Vector3.zero;

        public static Vector3? startPosition;

        public static GameObject recBodyRotary;

        public static Vector3 stillBeybladeStartPos = Vector3.zero;

        private static Vector3 headPos = Vector3.zero;
        private static Vector3 headRot = Vector3.zero;

        private static Vector3 handPos_L = Vector3.zero;
        private static Vector3 handRot_L = Vector3.zero;

        private static Vector3 handPos_R = Vector3.zero;
        private static Vector3 handRot_R = Vector3.zero;

        private static bool hasAdded;

        public static float beesDelay;

        public static float sizeScale = 1f;

        private static Quaternion? vrrigJoystickRot;

        private static Quaternion? bodyJoystickRot;

        static bool flipping;
        static float flipStart;
        static Quaternion flipFrom;
        static Vector3 flipAxis;
        const float flipDuration = 1f;

        private static readonly Dictionary<GorillaSurfaceOverride, float> velocityArchive = new Dictionary<GorillaSurfaceOverride, float>();

        public static GameObject stickpart;

        private static bool leftisclimbing;
        private static bool rightisclimbing;
        private static GameObject climb;

        public static float oldSlide;

        public static readonly Vector3[] lastLeft = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
        public static readonly Vector3[] lastRight = { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        private static VRRig sithlord;
        private static bool sithright;
        private static float sithdist = 1f;

        public static readonly Dictionary<VRRig, List<GameObject>> RigColliders = new Dictionary<VRRig, List<GameObject>>();

        public static int pullPowerInt;
        public static readonly float[] PullModPowers = { 0.05f, 0.1f, 0.2f, 0.4f };
        public static readonly string[] PullModPowerNames = { "Normal", "Medium", "Strong", "Powerful" };

        private static float pullPower = 0.05f;
        private static readonly Dictionary<bool, bool> previousTouchingGround = new Dictionary<bool, bool>();

        public static GameObject leftThrow;
        public static GameObject rightThrow;

        public static GameObject flickLeft;
        public static GameObject flickRight;

        private static Quaternion initialRotationLeft = Quaternion.identity;
        private static Quaternion initialRotationRight = Quaternion.identity;

        public static bool passWorldScaleCheck;
        private static Vector3? lastFramePosition;
        private static bool wasHandsNearHead;

        public static float extendingTime;

        public static GameObject lvT;
        public static GameObject rvT;

        public static float predCount = 0.0125f;
        public static int predInt = 1;
        public static readonly float[] PredictionAmounts = { 0.00625f, 0.0125f, 0.025f, 0.05f };
        public static readonly string[] PredictionAmountNames = { "Low", "Normal", "High", "Extreme" };

        public static int fakeLagDelayIndex = 10;
        public static float fakeLagDelay = 1f;

        public static bool isBlinking;

        public static int timerPowerIndex = 15;

        private static float timerPower = 1.5f;

        public static Vector3? longJumpPower;
        public static float? keepVelocityUntil;
        public static Vector3? velocity;

        public static float playspaceAbusePower = 0.004f;

        private static float preBounciness;
        private static PhysicsMaterialCombine whateverthisis = PhysicsMaterialCombine.Maximum;
        private static float preFrictiness;

        public static GameObject airSwimPart;

        private static List<float> waterSurfaceJumpAmount;
        private static List<float> waterSurfaceJumpMaxSpeed;

        public static readonly Dictionary<VRRig, Vector3> followPositions = new Dictionary<VRRig, Vector3>();

        public static int targetHz = 4000;

        public static Dictionary<Guid, Voices.Clip> tinnitus = new Dictionary<Guid, Voices.Clip>();
        public static float soundSpamDelay;

        public static float headspazDelay;
        public static bool headspazType;

        private static Vector3 headoffs = Vector3.zero;

        public static float laggyRigDelay;

        public static bool wasRightPrimaryPressed;

        public static int multiplicationAmount = 15;

        public static Material glass;
        #endregion

        #region Methods
        public static void ApplyPlatformType(int index) => platformMode = index;
        public static void ApplyPlatformShape(int index) => platformShape = index;

        public static PrimitiveType GetPlatformPrimitiveType()
        {
            return platformShape switch
            {
                0 => PrimitiveType.Sphere,
                1 => PrimitiveType.Cube,
                2 => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube,
            };
        }

        public static Vector3 GetPlatformScale()
        {
            return new[]
            {
                new Vector3(0.333f, 0.333f, 0.333f),
                new Vector3(0.333f, 0.333f, 0.333f),
                new Vector3(0.333f, 0.333f, 0.333f),
                new Vector3(0.025f, 0.3f, 0.4f),
                new Vector3(0.025f, 0.15f, 0.2f),
                new Vector3(0.025f, 0.3f, 0.8f),
                new Vector3(0.1f, 0.1f, 0.1f),
                new Vector3(0.025f, 1f, 1f)
            }[platformShape] * (scaleWithPlayer ? References.Player.scale : 1f);
        }

        public static GameObject CreatePlatform()
        {
            GameObject platform = GameObject.CreatePrimitive(GetPlatformPrimitiveType());
            platform.transform.localScale = GetPlatformScale();

            Renderer platformRenderer = platform.GetComponent<Renderer>();

            switch (platformMode)
            {
                case 1:
                case 6:
                    platformRenderer.enabled = false;
                    break;
                case 4:
                    UpdateClipColliders(false);
                    break;
                case 5:
                    platform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 29;
                    if (glass == null)
                        glass = new Material(Shader.Find("GUI/Text Shader"));

                    platformRenderer.material = glass;
                    break;
            }

            if (!Manager.GetIndex("Non-Sticky Platforms").enabled)
                FixStickyColliders(platform);

            if (platformRenderer.enabled && platform)
            {
                ObjectColors colorChanger = platform.AddComponent<ObjectColors>();
                colorChanger.colors = backgroundColor;

                if (platformMode == 2 || platformMode == 3)
                {
                    colorChanger.colors = colorChanger.colors.Clone();
                    colorChanger.colors.rainbow |= platformMode == 2;
                    colorChanger.colors.epileptic |= platformMode == 3;
                }
            }

            if (Manager.GetIndex("Platform Outlines").enabled)
            {
                GameObject gameObject = GameObject.CreatePrimitive(GetPlatformPrimitiveType());
                Object.Destroy(gameObject.GetComponent<Collider>());
                gameObject.transform.parent = platform.transform;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = new Vector3(0.95f, 1.05f, 1.05f);

                ObjectColors outlineColorChanger = gameObject.AddComponent<ObjectColors>();
                outlineColorChanger.colors = buttonColors[0];
            }

            foreach (Collider collider in platform.GetComponentsInChildren<Collider>(true))
                collider.gameObject.layer = PlatformLayer;

            return platform;
        }

        public static void SetPlatformPosition(GameObject platform, bool left)
        {
            var (position, rotation, _, _, right) = left ? Controllers.GetTrueLeftHand() : Controllers.GetTrueRightHand();

            platform.transform.position = position;
            platform.transform.rotation = rotation;

            if (Manager.GetIndex("Non-Sticky Platforms").enabled)
                platform.transform.position += right * ((left ? 1f : -1f) * ((0.025f + platform.transform.localScale.x / 2f) * (scaleWithPlayer ? References.Player.scale : 1f)));

            Friends.PlatformSpawned(true, platform.transform.position, platform.transform.rotation, platform.transform.localScale, GetPlatformPrimitiveType());
        }
        public static void ProcessPlatform(bool left, bool value)
        {
            GameObject platform = left ? leftplat : rightplat;
            switch (value)
            {
                case true when platform == null:
                    {
                        GameObject newPlatform = CreatePlatform();
                        SetPlatformPosition(newPlatform, left);
                        if (left)
                            leftplat = newPlatform;
                        else
                            rightplat = newPlatform;

                        break;
                    }
                case false when platform != null:
                    {
                        if (Manager.GetIndex("Platform Gravity").enabled)
                        {
                            platform.AddComponent(typeof(Rigidbody));
                            Object.Destroy(platform.GetComponent<Collider>());
                            Object.Destroy(platform, 2f);
                        }
                        else
                            Object.Destroy(platform);

                        if (left)
                            leftplat = null;
                        else
                            rightplat = null;
                        if (platformMode == 4 && rightplat == null)
                            UpdateClipColliders(true);

                        Friends.PlatformDespawned(true);
                        break;
                    }
            }
        }

        public static void Platforms(bool? left = null, bool? right = null)
        {
            if (platformMode == 6)
                Projectiles.GrabProjectile();

            ProcessPlatform(true, left ?? leftGrab);
            ProcessPlatform(false, right ?? rightGrab);
        }
        public static void HandleFrozone(bool left)
        {
            bool grip = left ? leftGrab : rightGrab;

            frozonicPlatforms.TryGetValue(left, out List<GameObject> frozonicPlatformList);
            if (frozonicPlatformList == null)
            {
                frozonicPlatformList = new List<GameObject>();
                frozonicPlatforms.Add(left, frozonicPlatformList);
            }

            platformIndex.TryGetValue(left, out int index);

            if (grip)
            {
                GameObject platform;
                if (frozonicPlatformList.Count >= 72)
                    platform = frozonicPlatformList[index];
                else
                {
                    platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    platform.layer = PlatformLayer;
                    platform.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();
                    platform.transform.localScale = new Vector3(0.025f, 0.3f, 0.4f) * (scaleWithPlayer ? References.Player.scale : 1f);
                    platform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 61;
                    frozonicPlatformList.Add(platform);
                }

                var (position, rotation, _, _, right) = left ? Controllers.GetTrueLeftHand() : Controllers.GetTrueRightHand();

                platform.transform.position = position + (right * ((left ? 1f : -1f) * ((0.025f + platform.transform.localScale.x / 2f) * (scaleWithPlayer ? References.Player.scale : 1f))));
                platform.transform.rotation = rotation;

                index = (index + 1) % 72;
            }

            platformIndex[left] = index;

            if (!grip && frozonicPlatformList.Count > 0)
            {
                int platformIndex = frozonicPlatformList.Count - 1;

                Object.Destroy(frozonicPlatformList[platformIndex]);
                frozonicPlatformList.RemoveAt(platformIndex);
            }
        }

        public static void Frozone()
        {
            HandleFrozone(true);
            HandleFrozone(false);

            References.BodyCollider.enabled = !(leftGrab || rightGrab);
        }
        public static void ApplySpeedBoostAmount(int index) { jspeed = SpeedBoostAmounts[index]; jmulti = SpeedBoostMultipliers[index]; }

        public static void PlatformSpam()
        {
            if (rightGrab)
            {
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(platform.GetComponent<BoxCollider>());
                platform.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();
                platform.GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
                platform.transform.localScale = new Vector3(0.025f, 0.3f, 0.4f);
                platform.transform.position = References.RightHand.position;
                platform.transform.rotation = References.RightHand.rotation;
                Object.Destroy(platform, 1f);
                PhotonNetwork.RaiseEvent(69, new object[] { platform.transform.position, platform.transform.rotation }, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            }
        }

        public static void PlatformGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(platform.GetComponent<BoxCollider>());
                platform.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();
                platform.GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
                platform.transform.localScale = new Vector3(0.025f, 0.3f, 0.4f);
                platform.transform.position = NewPointer.transform.position;
                platform.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
                Object.Destroy(platform, 1f);
                PhotonNetwork.RaiseEvent(69, new object[] { platform.transform.position, platform.transform.rotation }, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            });
        }
        public static void ApplyFlySpeed(int index) => FlySpeed = FlySpeedAmounts[index];
        public static void ApplyPlayspaceAbuseSpeed(int index) => playspaceAbusePower = PlayspaceAbuseAmounts[index];
        public static void ApplyArmLength(int index) => armlength = ArmLengthAmounts[index];

        public static void Fly()
        {
            if (rightPrimary)
            {
                References.Player.transform.position += References.HeadCollider.transform.forward * (Time.deltaTime * FlySpeed);
                References.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public static void TriggerFly()
        {
            if (rightTrigger > 0.5f)
            {
                References.Player.transform.position += References.HeadCollider.transform.forward * (Time.deltaTime * FlySpeed);
                References.Rigidbody.linearVelocity = Vector3.zero;
            }
        }
        public static void NoclipFly()
        {
            if (rightPrimary)
            {
                References.Player.transform.position += References.HeadCollider.transform.forward * (Time.deltaTime * FlySpeed);
                References.Rigidbody.linearVelocity = Vector3.zero;
                if (!noclip)
                {
                    noclip = true;
                    UpdateClipColliders(false);
                }
            }
            else
            {
                if (noclip)
                {
                    noclip = false;
                    UpdateClipColliders(true);
                }
            }
        }

        public static void JoystickFly()
        {
            Vector2 joy = leftJoystick;

            if (Mathf.Abs(joy.x) > 0.3 || Mathf.Abs(joy.y) > 0.3)
            {
                References.Player.transform.position += References.HeadCollider.transform.forward * (Time.deltaTime * (joy.y * FlySpeed)) + References.HeadCollider.transform.right * (Time.deltaTime * (joy.x * FlySpeed));
                References.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public static void BarkFly()
        {
            Vector3 inputDirection = new Vector3(leftJoystick.x, rightJoystick.y, leftJoystick.y);

            Vector3 playerForward = References.Player.bodyCollider.transform.forward.X_Z();
            Vector3 playerRight = References.Player.bodyCollider.transform.right.X_Z();

            Vector3 velocity = inputDirection.x * playerRight + inputDirection.y * Vector3.up + inputDirection.z * playerForward;
            velocity *= FlySpeed;
            References.Rigidbody.linearVelocity = Vector3.Lerp(References.Rigidbody.linearVelocity, velocity, 0.12875f);


            ZeroGravity();
        }

        public static void VelocityBarkFly()
        {
            if (Mathf.Abs(leftJoystick.x) > 0.3 || Mathf.Abs(leftJoystick.y) > 0.3 || Mathf.Abs(rightJoystick.x) > 0.3 || Mathf.Abs(rightJoystick.y) > 0.3)
                BarkFly();
        }

        public static void HandFly()
        {
            if (rightPrimary)
            {
                References.Player.transform.position += Controllers.GetTrueRightHand().forward * (Time.deltaTime * FlySpeed);
                References.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public static void FlyTowardsGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                References.Player.transform.position += (lockTarget.transform.position - References.BodyCollider.transform.position) * (Time.deltaTime * FlySpeed);
                References.Rigidbody.linearVelocity = Vector3.zero;
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void SlingshotFly()
        {
            if (rightPrimary)
                References.Rigidbody.linearVelocity += References.Player.headCollider.transform.forward * (Time.deltaTime * (FlySpeed * 2));
        }

        public static void ZeroGravitySlingshotFly()
        {
            if (rightPrimary)
            {
                ZeroGravity();
                References.Rigidbody.linearVelocity += References.Player.headCollider.transform.forward * (Time.deltaTime * FlySpeed);
            }
        }
        public static void EnableWASDFly()
        {
            lastPosition = References.Rigidbody.transform.position;
            if (XRSettings.isDeviceActive && Time.time < timeMenuStarted + 1f)
                Toggle("WASD Fly");
        }

        public static void WASDFly()
        {
            bool stationary = !Manager.GetIndex("Disable Stationary WASD Fly").enabled;

            bool W = UnityInput.GetKey(Key.W);
            bool A = UnityInput.GetKey(Key.A);
            bool S = UnityInput.GetKey(Key.S);
            bool D = UnityInput.GetKey(Key.D);
            bool Space = UnityInput.GetKey(Key.Space);
            bool Ctrl = UnityInput.GetKey(Key.LeftCtrl);
            bool Shift = UnityInput.GetKey(Key.LeftShift);
            bool Alt = UnityInput.GetKey(Key.LeftAlt);

            bool LeftArrow = UnityInput.GetKey(Key.LeftArrow);
            bool RightArrow = UnityInput.GetKey(Key.RightArrow);
            bool UpArrow = UnityInput.GetKey(Key.UpArrow);
            bool DownArrow = UnityInput.GetKey(Key.DownArrow);

            if (stationary || W || A || S || D || Space || Ctrl)
                References.Rigidbody.linearVelocity = Vector3.zero;

            if (!menu)
            {
                Transform parentTransform = References.Player.GetControllerTransform(false).parent;

                float turnSpeed = 250f;

                if (LeftArrow)
                    parentTransform.eulerAngles += new Vector3(0, -turnSpeed, 0) * Time.deltaTime;
                if (RightArrow)
                    parentTransform.eulerAngles += new Vector3(0, turnSpeed, 0) * Time.deltaTime;
                if (UpArrow)
                    parentTransform.eulerAngles += new Vector3(-turnSpeed, 0, 0) * Time.deltaTime;
                if (DownArrow)
                    parentTransform.eulerAngles += new Vector3(turnSpeed, 0, 0) * Time.deltaTime;

                if (Mouse.current.rightButton.isPressed)
                {
                    Quaternion currentRotation = parentTransform.rotation;
                    Vector3 euler = currentRotation.eulerAngles;

                    if (startX < 0)
                    {
                        startX = euler.y;
                        subThingy = Mouse.current.position.value.x / Screen.width;
                    }
                    if (startY < 0)
                    {
                        startY = euler.x;
                        subThingyZ = Mouse.current.position.value.y / Screen.height;
                    }

                    float newX = startY - (Mouse.current.position.value.y / Screen.height - subThingyZ) * 360 * 1.33f;
                    float newY = startX + (Mouse.current.position.value.x / Screen.width - subThingy) * 360 * 1.33f;

                    newX = newX > 180f ? newX - 360f : newX;
                    newX = Mathf.Clamp(newX, -90f, 90f);

                    parentTransform.rotation = Quaternion.Euler(newX, newY, euler.z);
                }
                else
                {
                    startX = -1;
                    startY = -1;
                }

                float speed = FlySpeed;
                if (Shift)
                    speed *= 2f;
                else if (Alt)
                    speed /= 2;

                if (W)
                    References.Rigidbody.transform.position += References.Player.GetControllerTransform(false).parent.forward * (Time.deltaTime * speed);

                if (S)
                    References.Rigidbody.transform.position += References.Player.GetControllerTransform(false).parent.forward * (Time.deltaTime * -speed);

                if (A)
                    References.Rigidbody.transform.position += References.Player.GetControllerTransform(false).parent.right * (Time.deltaTime * -speed);

                if (D)
                    References.Rigidbody.transform.position += References.Player.GetControllerTransform(false).parent.right * (Time.deltaTime * speed);

                if (Space)
                    References.Rigidbody.transform.position += new Vector3(0f, Time.deltaTime * speed, 0f);

                if (Ctrl)
                    References.Rigidbody.transform.position += new Vector3(0f, Time.deltaTime * -speed, 0f);

                References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;
            }

            if (!W && !A && !S && !D && !Space && !Ctrl && lastPosition != Vector3.zero && stationary)
                References.Rigidbody.transform.position = lastPosition;
            else
                lastPosition = References.Rigidbody.transform.position;
        }
        public static void ApplyDriveSpeed(int index) => driveSpeed = DriveSpeedAmounts[index];


        public static void ApplyFastRopesSpeed(int index) => RopeAttach.amplifier = FastRopesSpeedAmounts[index];
        public static void Drive()
        {
            Vector2 joy = leftJoystick;
            driveLerpDirection = Vector2.Lerp(driveLerpDirection, joy, 0.05f);

            Vector3 addition = References.BodyCollider.transform.forward * driveLerpDirection.y + References.BodyCollider.transform.right * driveLerpDirection.x;
            Physics.Raycast(References.BodyCollider.transform.position - new Vector3(0f, 0.2f, 0f), Vector3.down, out var Ray, 512f, References.Player.locomotionEnabledLayers);
            Vector3 targetVelocity = addition * driveSpeed;

            if (Ray.distance < 0.2f && (Mathf.Abs(driveLerpDirection.x) > 0.05f || Mathf.Abs(driveLerpDirection.y) > 0.05f))
                References.Rigidbody.linearVelocity = new Vector3(targetVelocity.x, References.Rigidbody.linearVelocity.y, targetVelocity.z);
        }

        public static void HardDrive()
        {
            bool elevatedStickyDrive = Manager.GetIndex("Elevated Sticky Drive").enabled;
            if ((Mathf.Abs(leftJoystick.x) > 0.05f || Mathf.Abs(leftJoystick.y) > 0.05f) && closePosition == Vector3.zero)
            {
                Vector3 direction = References.BodyCollider.transform.forward * leftJoystick.y
                                  + References.BodyCollider.transform.right * leftJoystick.x;

                Vector3 raycastPosition = References.BodyCollider.transform.position
                    + Vector3.up * 5f
                    + direction * (Time.deltaTime * driveSpeed);
                Physics.Raycast(raycastPosition, Vector3.down, out var Ray, 50f, References.Player.locomotionEnabledLayers);

                Vector3 targetPosition = Ray.point == Vector3.zero ? raycastPosition : Ray.point;

                TeleportPlayer(targetPosition + Vector3.up * (elevatedStickyDrive ? 0.7f : 0.2f));
                References.Rigidbody.linearVelocity = Vector3.zero;
            }
        }
        public static void Dash()
        {
            if (rightPrimary && !previousDash)
                References.Rigidbody.linearVelocity += References.Player.headCollider.transform.forward * FlySpeed;

            previousDash = rightPrimary;
        }
        private static bool previousDashBack, previousDashLeft, previousDashRight;
        public static void DashBack()
        {
            if (rightPrimary && !previousDashBack)
                References.Rigidbody.linearVelocity -= References.Player.headCollider.transform.forward * FlySpeed;

            previousDashBack = rightPrimary;
        }
        public static void DashLeft()
        {
            if (rightPrimary && !previousDashLeft)
                References.Rigidbody.linearVelocity -= References.Player.headCollider.transform.right * FlySpeed;

            previousDashLeft = rightPrimary;
        }
        public static void DashRight()
        {
            if (rightPrimary && !previousDashRight)
                References.Rigidbody.linearVelocity += References.Player.headCollider.transform.right * FlySpeed;

            previousDashRight = rightPrimary;
        }

        private static Vector3 lastLeftHandDragPos, lastRightHandDragPos;
        public static void PlaySpaceDrag()
        {
            if (leftGrab)
                References.Rigidbody.linearVelocity -= (References.LeftHand.position - lastLeftHandDragPos) * (scaleWithPlayer ? References.Player.scale : 1f) * 10f;
            lastLeftHandDragPos = References.LeftHand.position;

            if (rightGrab)
                References.Rigidbody.linearVelocity -= (References.RightHand.position - lastRightHandDragPos) * (scaleWithPlayer ? References.Player.scale : 1f) * 10f;
            lastRightHandDragPos = References.RightHand.position;
        }

        public static void ResetRig()
        {
            References.LocalRig.enabled = true;
            References.LocalRig.transform.localRotation = Quaternion.identity;
            References.HeadCollider.transform.localRotation = Quaternion.identity;
        }

        public static void BecomePlayerGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = lockTarget.transform.position;
                References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                References.LocalRig.head.rigTarget.position = lockTarget.head.rigTarget.position;
                References.LocalRig.head.rigTarget.rotation = lockTarget.head.rigTarget.rotation;
                References.LocalRig.leftHand.rigTarget.position = lockTarget.leftHand.rigTarget.position;
                References.LocalRig.leftHand.rigTarget.rotation = lockTarget.leftHand.rigTarget.rotation;
                References.LocalRig.rightHand.rigTarget.position = lockTarget.rightHand.rigTarget.position;
                References.LocalRig.rightHand.rigTarget.rotation = lockTarget.rightHand.rigTarget.rotation;

                References.LocalRig.bodyTransform.rotation = lockTarget.bodyTransform.rotation;
                References.LocalRig.headMesh.transform.rotation = lockTarget.headMesh.transform.rotation;
            }, new GunLibrary.GunSettings { locks = true, onIdle = () => { if (gunLocked) { gunLocked = false; References.LocalRig.enabled = true; } } });
        }
        public static void ReverseVelocity()
        {
            if (Time.time < nextrevTime)
                return;

            if (rightPrimary)
            {
                References.Rigidbody.linearVelocity = -References.Rigidbody.linearVelocity;

                nextrevTime = Time.time + revCooldown;
            }
        }
        public static void BirdFly()
        {
            if (Vector3.Distance(References.LeftHand.position, References.HeadCollider.transform.position) < 0.63f || Vector3.Distance(References.RightHand.position, References.HeadCollider.transform.position) < 0.63f)
                return;

            if (Vector3.Distance(References.LeftHand.position, References.RightHand.position) < 1f)
                return;
            if (Physics.Raycast(References.BodyCollider.attachedRigidbody.position, Vector3.down, hitInfo: out _, Physics.AllLayers))
                return;

            UnityEngine.XR.InputDevice LeftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            UnityEngine.XR.InputDevice RightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (LeftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 leftVel) && RightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 rightVel))
            {
                if (Time.time - flapTime < 0.4f) return;

                if (leftVel.y < -1.2f && rightVel.y < -1.2f)
                {
                    float force = Mathf.Min(6f * ((Mathf.Abs(leftVel.y) + Mathf.Abs(rightVel.y)) / 2f) / 1.2f, 9f);
                    References.BodyCollider.attachedRigidbody.AddForce(Vector3.up * force, ForceMode.VelocityChange);

                    flapTime = Time.time;
                }
            }
        }

        public static void IronMan()
        {
            Rigidbody rb = References.Rigidbody;

            if (leftPrimary)
            {
                Vector3 leftForce = FlySpeed * -References.LeftHand.right;
                rb.AddForce(leftForce * Time.deltaTime, ForceMode.VelocityChange);

                float hapticStrength = References.Tagger.tapHapticStrength / 50f * rb.linearVelocity.magnitude;
                References.Tagger.StartVibration(true, hapticStrength, References.Tagger.tapHapticDuration);
            }

            if (rightPrimary)
            {
                Vector3 rightForce = FlySpeed * References.RightHand.right;
                rb.AddForce(rightForce * Time.deltaTime, ForceMode.VelocityChange);

                float hapticStrength = References.Tagger.tapHapticStrength / 50f * rb.linearVelocity.magnitude;
                References.Tagger.StartVibration(false, hapticStrength, References.Tagger.tapHapticDuration);
            }
        }
        private static BalloonHoldable GetTargetBalloon()
        {
            foreach (BalloonHoldable balloo in GetAllType<BalloonHoldable>())
            {
                if (balloo.IsMyItem())
                    return balloo;
            }
            if (Time.time > loaoalsode)
            {
                Notifications.SendFormat("red", "ERROR", "You must equip a balloon.");
                loaoalsode = Time.time + 1f;
            }
            return null;
        }

        public static void SpiderMan()
        {
            if (leftGrab)
            {
                if (!isLeftGrappling)
                {
                    isLeftGrappling = true;
                    References.Rigidbody.linearVelocity += References.LeftHand.forward * 5f;
                    if (Networking.InRoom)
                    {
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                    }
                    else
                        References.LocalRig.PlayHandTapLocal(89, true, 999999f);

                    RPCProtection();

                    leftgrapplePoint = References.LeftHand.position + References.LeftHand.forward * 16f;

                    leftjoint = References.Tagger.gameObject.AddComponent<SpringJoint>();
                    leftjoint.autoConfigureConnectedAnchor = false;
                    leftjoint.connectedAnchor = leftgrapplePoint;

                    float leftdistanceFromPoint = Vector3.Distance(References.Rigidbody.position, leftgrapplePoint);

                    leftjoint.maxDistance = leftdistanceFromPoint * 0.8f;
                    leftjoint.minDistance = leftdistanceFromPoint * 0.25f;

                    leftjoint.spring = 10f;
                    leftjoint.damper = 50f;
                    leftjoint.massScale = 12f;
                }

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                Color thecolor = Color.red;
                liner.startColor = thecolor; liner.endColor = thecolor; liner.startWidth = 0.025f; liner.endWidth = 0.025f; liner.positionCount = 2; liner.useWorldSpace = true;
                liner.SetPosition(0, References.LeftHand.position);
                liner.SetPosition(1, leftgrapplePoint);
                liner.material.shader = Shader.Find("GorillaTag/UberShader");
                Object.Destroy(line, Time.deltaTime);
            }
            else
            {
                Vector3 EndPosition = References.LeftHand.position + References.LeftHand.forward * 16f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                liner.material.shader = Shader.Find("Sprites/Default");
                liner.startColor = backgroundColor.GetCurrentColor() - new Color32(0, 0, 0, 128);
                liner.endColor = backgroundColor.GetCurrentColor(0.5f) - new Color32(0, 0, 0, 128);
                liner.startWidth = 0.025f;
                liner.endWidth = 0.025f;
                liner.positionCount = 2;
                liner.useWorldSpace = true;
                liner.SetPosition(0, References.LeftHand.position);
                liner.SetPosition(1, EndPosition);
                Object.Destroy(line, Time.deltaTime);

                isLeftGrappling = false;
                Object.Destroy(leftjoint);
            }

            if (rightGrab)
            {
                if (!isRightGrappling)
                {
                    isRightGrappling = true;
                    References.Rigidbody.linearVelocity += References.RightHand.forward * 5f;
                    if (Networking.InRoom)
                    {
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                        RPCProtection();
                    }
                    else
                        References.LocalRig.PlayHandTapLocal(89, false, 999999f);

                    rightgrapplePoint = References.RightHand.position + References.RightHand.forward * 16f;

                    if (rightgrapplePoint == Vector3.zero)
                        rightgrapplePoint = References.RightHand.position + References.RightHand.forward * 512f;

                    rightjoint = References.Tagger.gameObject.AddComponent<SpringJoint>();
                    rightjoint.autoConfigureConnectedAnchor = false;
                    rightjoint.connectedAnchor = rightgrapplePoint;

                    float rightdistanceFromPoint = Vector3.Distance(References.Rigidbody.position, rightgrapplePoint);

                    rightjoint.maxDistance = rightdistanceFromPoint * 0.8f;
                    rightjoint.minDistance = rightdistanceFromPoint * 0.25f;

                    rightjoint.spring = 10f;
                    rightjoint.damper = 50f;
                    rightjoint.massScale = 12f;
                }

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                Color thecolor = Color.red;
                liner.startColor = thecolor; liner.endColor = thecolor; liner.startWidth = 0.025f; liner.endWidth = 0.025f; liner.positionCount = 2; liner.useWorldSpace = true;
                liner.SetPosition(0, References.RightHand.position);
                liner.SetPosition(1, rightgrapplePoint);
                liner.material.shader = Shader.Find("GorillaTag/UberShader");
                Object.Destroy(line, Time.deltaTime);
            }
            else
            {
                Vector3 EndPosition = References.RightHand.position + References.RightHand.forward * 16f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                liner.material.shader = Shader.Find("Sprites/Default");
                liner.startColor = backgroundColor.GetCurrentColor() - new Color32(0, 0, 0, 128);
                liner.endColor = backgroundColor.GetCurrentColor(0.5f) - new Color32(0, 0, 0, 128);
                liner.startWidth = 0.025f;
                liner.endWidth = 0.025f;
                liner.positionCount = 2;
                liner.useWorldSpace = true;
                liner.SetPosition(0, References.RightHand.position);
                liner.SetPosition(1, EndPosition);
                Object.Destroy(line, Time.deltaTime);

                isRightGrappling = false;
                Object.Destroy(rightjoint);
            }
        }

        public static void GrapplingHooks()
        {
            if (leftGrab)
            {
                if (!isLeftGrappling)
                {
                    isLeftGrappling = true;
                    References.Rigidbody.linearVelocity += References.LeftHand.forward * 5f;
                    if (Networking.InRoom)
                    {
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                    }
                    else
                        References.LocalRig.PlayHandTapLocal(89, true, 999999f);

                    RPCProtection();
                    leftgrapplePoint = References.LeftHand.position + References.LeftHand.forward * 16f;
                }
                else
                    References.Rigidbody.linearVelocity += Vector3.Normalize(leftgrapplePoint - References.LeftHand.position) * 0.5f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                Color thecolor = Color.red;
                liner.startColor = thecolor; liner.endColor = thecolor; liner.startWidth = 0.025f; liner.endWidth = 0.025f; liner.positionCount = 2; liner.useWorldSpace = true;
                liner.SetPosition(0, References.LeftHand.position);
                liner.SetPosition(1, leftgrapplePoint);
                liner.material.shader = Shader.Find("GorillaTag/UberShader");
                Object.Destroy(line, Time.deltaTime);
            }
            else
            {
                Vector3 EndPosition = References.LeftHand.position + References.LeftHand.forward * 16f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                liner.material.shader = Shader.Find("Sprites/Default");
                liner.startColor = backgroundColor.GetCurrentColor() - new Color32(0, 0, 0, 128);
                liner.endColor = backgroundColor.GetCurrentColor(0.5f) - new Color32(0, 0, 0, 128);
                liner.startWidth = 0.025f;
                liner.endWidth = 0.025f;
                liner.positionCount = 2;
                liner.useWorldSpace = true;
                liner.SetPosition(0, References.LeftHand.position);
                liner.SetPosition(1, EndPosition);
                Object.Destroy(line, Time.deltaTime);

                isLeftGrappling = false;
                Object.Destroy(leftjoint);
            }

            if (rightGrab)
            {
                if (!isRightGrappling)
                {
                    isRightGrappling = true;
                    References.Rigidbody.linearVelocity += References.RightHand.forward * 5f;
                    if (Networking.InRoom)
                    {
                        References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                        RPCProtection();
                    }
                    else
                        References.LocalRig.PlayHandTapLocal(89, false, 999999f);

                    rightgrapplePoint = References.RightHand.position + References.RightHand.forward * 16f;
                }
                else
                    References.Rigidbody.linearVelocity += Vector3.Normalize(rightgrapplePoint - References.RightHand.position) * 0.5f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                Color thecolor = Color.red;
                liner.startColor = thecolor; liner.endColor = thecolor; liner.startWidth = 0.025f; liner.endWidth = 0.025f; liner.positionCount = 2; liner.useWorldSpace = true;
                liner.SetPosition(0, References.RightHand.position);
                liner.SetPosition(1, rightgrapplePoint);
                liner.material.shader = Shader.Find("GorillaTag/UberShader");
                Object.Destroy(line, Time.deltaTime);
            }
            else
            {
                Vector3 EndPosition = References.RightHand.position + References.RightHand.forward * 16f;

                GameObject line = new GameObject("Line");
                LineRenderer liner = line.AddComponent<LineRenderer>();
                if (smoothLines)
                {
                    liner.numCapVertices = 10;
                    liner.numCornerVertices = 5;
                }
                liner.material.shader = Shader.Find("Sprites/Default");
                liner.startColor = backgroundColor.GetCurrentColor() - new Color32(0, 0, 0, 128);
                liner.endColor = backgroundColor.GetCurrentColor(0.5f) - new Color32(0, 0, 0, 128);
                liner.startWidth = 0.025f;
                liner.endWidth = 0.025f;
                liner.positionCount = 2;
                liner.useWorldSpace = true;
                liner.SetPosition(0, References.RightHand.position);
                liner.SetPosition(1, EndPosition);
                Object.Destroy(line, Time.deltaTime);

                isRightGrappling = false;
            }
        }

        public static void DisableSpiderMan()
        {
            isLeftGrappling = false;
            Object.Destroy(leftjoint);
            isRightGrappling = false;
            Object.Destroy(rightjoint);
        }

        public static void NetworkedGrappleMods()
        {
            if (Manager.GetIndex("Spider Man").enabled || Manager.GetIndex("Grappling Hooks").enabled)
            {
                if (isLeftGrappling || isRightGrappling)
                {
                    BalloonHoldable tb = GetTargetBalloon();

                    tb.balloonState = BalloonHoldable.BalloonStates.Normal;
                    tb.maxDistanceFromOwner = float.MaxValue;

                    tb.rigidbodyInstance.isKinematic = true;
                    tb.gameObject.GetComponent<BalloonDynamics>().stringLength = 512f;
                    tb.gameObject.GetComponent<BalloonDynamics>().stringStrength = 512f;
                    tb.gameObject.GetComponent<BalloonDynamics>().enableDynamics = false;

                    if (tb != null)
                    {
                        if (isLeftGrappling || isRightGrappling)
                        {
                            if (!tb.lineRenderer.enabled)
                                tb.currentState = TransferrableObject.PositionState.InLeftHand;
                        }

                        if (isLeftGrappling)
                        {
                            tb.transform.position = leftgrapplePoint;
                            tb.transform.LookAt(References.LeftHand.position);
                        }
                        else
                        {
                            tb.transform.position = rightgrapplePoint;
                            tb.transform.LookAt(References.RightHand.position);
                            tb.transform.Rotate(Vector3.left, 90f, Space.Self);
                        }
                    }
                }
                else
                {
                    BalloonHoldable tb = GetTargetBalloon();
                    BalloonHoldable.BalloonStates balloonState = tb.balloonState;

                    if (balloonState != BalloonHoldable.BalloonStates.Pop && balloonState != BalloonHoldable.BalloonStates.Waiting && balloonState != BalloonHoldable.BalloonStates.Refilling && balloonState != BalloonHoldable.BalloonStates.Returning)
                        tb.balloonState = BalloonHoldable.BalloonStates.Normal;

                    tb.rigidbodyInstance.isKinematic = false;
                    tb.gameObject.GetComponent<BalloonDynamics>().stringLength = 0.5f;
                    tb.gameObject.GetComponent<BalloonDynamics>().stringStrength = 0.9f;
                    tb.gameObject.GetComponent<BalloonDynamics>().enableDynamics = true;

                    if (tb != null)
                        tb.currentState = TransferrableObject.PositionState.Dropped;
                }
            }
        }

        public static bool IsOverlapping(Collider collider, Vector3 center, float radius)
        {
            Vector3 closest = collider.ClosestPoint(center);
            bool centerInside = closest == center;

            float distSqr = (closest - center).sqrMagnitude;
            bool overlaps = distSqr <= radius * radius;

            return centerInside || overlaps;
        }

        public static bool IsOverlapping(Collider colliderA, Collider colliderB, float radius)
        {
            Vector3 center = colliderB.ClosestPoint(colliderA.transform.position);

            Vector3 closest = colliderA.ClosestPoint(colliderB.transform.position);
            bool centerInside = closest == center;

            float distSqr = (closest - center).sqrMagnitude;
            bool overlaps = distSqr <= radius * radius;

            return centerInside || overlaps;
        }

        public static void PortalGun()
        {
            if (portalGun == null)
            {
                portalGun = LoadObject<GameObject>("PortalGun");
                portalGun.transform.SetParent(References.LocalRig.rightHandTransform.parent, false);
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portalgun_powerup.ogg", "Audio/Mods/Movement/PortalGun/portalgun_powerup.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
            }

            if (portalGun)
            {
                Transform RayPoint = portalGun.transform.Find("PortalGun/Ray");
                Physics.Raycast(RayPoint.position, RayPoint.forward, out var ray, 512f, References.Player.locomotionEnabledLayers);

                if (crosshair == null)
                {
                    crosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crosshair.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                    crosshair.GetComponent<Renderer>().material.color = Color.white;
                    Object.Destroy(crosshair.GetComponent<Collider>());
                }

                if (crosshair)
                    crosshair.transform.position = ray.point == Vector3.zero ? (RayPoint.transform.position + (RayPoint.transform.forward * 20f)) : ray.point;

                if (rightTrigger > 0.5f && Time.time > portalDelay)
                {
                    var portalNotToUse = flipped ? bluePortal : orangePortal;
                    if (portalNotToUse && (Vector3.Distance(ray.point, portalNotToUse.transform.position) < 1f || ray.point == Vector3.zero))
                    {
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portal_invalid.ogg", "Audio/Mods/Movement/PortalGun/portal_invalid.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                        portalDelay = Time.time + 0.5f;
                        return;
                    }
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portalgun_blue.ogg", "Audio/Mods/Movement/PortalGun/portalgun_blue.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                    GameObject portalToUse;
                    if (flipped)
                    {
                        if (orangePortal == null)
                            orangePortal = GetPortal(true);
                        portalToUse = orangePortal;
                    }
                    else
                    {
                        if (bluePortal == null)
                            bluePortal = GetPortal(false);
                        portalToUse = bluePortal;
                    }
                    portalToUse.transform.position = ray.point + ray.normal * 0.075f;
                    portalToUse.transform.rotation = Quaternion.LookRotation(ray.normal, Vector3.up) * Quaternion.Euler(90, 0, 0);
                    Coroutines.instance.StartCoroutine(AnimatePortalScale(portalToUse, 0.3f));
                    portalDelay = Time.time + 0.5f;
                }
                if (rightGrab && Time.time > flipDelay)
                {
                    flipped = !flipped;
                    Color32 color = flipped ? new Color32(255, 150, 0, 1) : new Color32(0, 183, 255, 1);
                    portalGun.transform.Find("PortalGun/Light").GetComponent<Renderer>().material.color = color;
                    portalGun.transform.Find("PortalGun/Light/Tube").GetComponent<Renderer>().material.color = color;

                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portalgun_fizzle.ogg", "Audio/Mods/Movement/PortalGun/portalgun_fizzle.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                    flipDelay = Time.time + 0.5f;
                }
                if (bluePortal && orangePortal)
                {
                    GameObject blueView = bluePortal.transform.Find("Rim/View").gameObject;
                    GameObject orangeView = orangePortal.transform.Find("Rim/View").gameObject;

                    if (!blueView.activeSelf)
                    {
                        blueView.SetActive(true);
                        bluePortal.transform.Find("Rim/Static").gameObject.SetActive(false);
                        if (Manager.GetIndex("High Quality Portals").enabled)
                            UpdateCameraRes(bluePortal.transform.Find("Rim/Camera/Eye").GetComponent<Camera>());
                    }

                    if (!orangeView.activeSelf)
                    {
                        orangeView.SetActive(true);
                        orangePortal.transform.Find("Rim/Static").gameObject.SetActive(false);
                        if (Manager.GetIndex("High Quality Portals").enabled)
                            UpdateCameraRes(orangePortal.transform.Find("Rim/Camera/Eye").GetComponent<Camera>());
                    }

                    if (blueView.activeSelf && orangeView.activeSelf && !playedOpen)
                    {
                        string sound = flipped ? "portal_open1.ogg" : "portal_open2.ogg";
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/{sound}", $"Audio/Mods/Movement/PortalGun/{sound}", clip => Play2DAudio(clip, buttonClickVolume / 10f));

                        PortalTrigger blueTrigger = blueView.GetOrAddComponent<PortalTrigger>();
                        PortalTrigger orangeTrigger = orangeView.GetOrAddComponent<PortalTrigger>();

                        blueTrigger.destination = orangePortal;
                        orangeTrigger.destination = bluePortal;

                        playedOpen = true;
                    }
                }
                if (rightPrimary && (bluePortal || orangePortal))
                {
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portal_close.ogg", "Audio/Mods/Movement/PortalGun/portal_close.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                    Object.Destroy(bluePortal);
                    Object.Destroy(orangePortal);
                    playedOpen = false;
                }
            }

            if (bluePortal && orangePortal)
            {
                bool goingThrough = false;
                foreach (GameObject portal in new[] { bluePortal, orangePortal })
                {
                    if (Vector3.Distance(portal.transform.position, References.BodyCollider.transform.position) < 0.6f)
                    {
                        goingThrough = true;
                        break;
                    }
                }

                switch (goingThrough)
                {
                    case true when !inPortal:
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portal_enter1.ogg", "Audio/Mods/Movement/PortalGun/portal_enter1.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                        break;
                    case false when inPortal:
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portal_exit1.ogg", "Audio/Mods/Movement/PortalGun/portal_exit1.ogg", clip => Play2DAudio(clip, buttonClickVolume / 10f));
                        break;
                }

                inPortal = goingThrough;

                References.Player.leftHand.isHolding = false;
                foreach (GameObject _ in new[] { bluePortal, orangePortal })
                {
                    if (IsOverlapping(_.transform.Find("Rim/View").GetComponent<Collider>(), Controllers.GetTrueLeftHand().position, 0.1f))
                    {
                        References.Player.leftHand.isHolding = true;
                        break;
                    }
                }

                References.Player.rightHand.isHolding = false;
                foreach (GameObject _ in new[] { bluePortal, orangePortal })
                {
                    if (IsOverlapping(_.transform.Find("Rim/View").GetComponent<Collider>(), Controllers.GetTrueRightHand().position, 0.1f))
                    {
                        References.Player.rightHand.isHolding = true;
                        break;
                    }
                }

                References.BodyCollider.enabled = true;
                foreach (GameObject _ in new[] { bluePortal, orangePortal })
                {
                    if (IsOverlapping(_.transform.Find("Rim/View").GetComponent<Collider>(), References.Player.bodyCollider.transform.position, 0.05f))
                    {
                        References.Player.bodyCollider.enabled = false;
                        break;
                    }
                }
            }
        }

        public static GameObject GetPortal(bool orange)
        {
            GameObject mainObject = LoadObject<GameObject>(orange ? "OrangePortal" : "BluePortal").transform.Find("Portal").gameObject;
            return mainObject;
        }

        public static IEnumerator TeleportPortal(GameObject portal)
        {
            Vector3 velocity = portal.transform.up * References.Rigidbody.linearVelocity.magnitude * 1.2f;
            TeleportPlayer(portal.transform.position + portal.transform.up);
            SetRotation(portal.transform.rotation.eulerAngles.y);
            References.Rigidbody.linearVelocity = velocity;

            float timer = 0f;
            while (timer < 0.1f)
            {
                References.BodyCollider.enabled = false;
                References.HeadCollider.enabled = false;

                References.Player.leftHand.isHolding = true;
                References.Player.rightHand.isHolding = true;

                timer += Time.deltaTime;
                yield return null;
            }

            References.BodyCollider.enabled = true;
            References.HeadCollider.enabled = true;

            References.Player.leftHand.isHolding = false;
            References.Player.rightHand.isHolding = false;

            yield break;
        }

        public static IEnumerator TeleportObject(GameObject obj, GameObject portal)
        {
            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Movement/PortalGun/portal_exit1.ogg", "Audio/Mods/Movement/PortalGun/portal_exit1.ogg", clip => clip.PlayAt(obj.transform.position, buttonClickSound / 20f));

            if (obj.TryGetComponent(out Rigidbody rigidbody) || obj.TryGetComponentInParent(out rigidbody))
                rigidbody.linearVelocity = portal.transform.up * rigidbody.linearVelocity.magnitude * 1.2f;
            obj.transform.position = portal.transform.position;
            obj.transform.rotation = Quaternion.LookRotation(portal.transform.up);

            if (!obj.TryGetComponent<Collider>(out var collider))
                yield break;

            float timer = 0f;
            while (timer < 0.1f)
            {
                collider.enabled = false;

                timer += Time.deltaTime;
                yield return null;
            }

            collider.enabled = true;

            yield break;
        }

        public static IEnumerator AnimatePortalScale(GameObject portal, float duration)
        {
            Vector3 originalScale = portal.transform.localScale;
            portal.transform.localScale = Vector3.zero;

            float timer = 0f;

            while (timer < duration)
            {
                float t = timer / duration;
                float sinT = Mathf.Sin(t * Mathf.PI * 0.5f);

                portal.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, originalScale, sinT);

                timer += Time.deltaTime;
                yield return null;
            }

            portal.transform.localScale = originalScale;
        }

        public static void UpdateCameraRes(Camera cam)
        {
            cam.targetTexture.width = cam.targetTexture.width * 5;
            cam.targetTexture.height = cam.targetTexture.height * 5;
        }

        public static void DisablePortalGun()
        {
            Object.Destroy(portalGun);
            Object.Destroy(bluePortal);
            Object.Destroy(orangePortal);
            Object.Destroy(crosshair);
            playedOpen = false;
        }
        public static void UpAndDown()
        {
            if (rightTrigger > 0.5f || rightGrab)
                ZeroGravity();

            if (rightTrigger > 0.5f)
                References.Rigidbody.linearVelocity += Vector3.up * (Time.deltaTime * FlySpeed * 3f);

            if (rightGrab)
                References.Rigidbody.linearVelocity += Vector3.up * (Time.deltaTime * FlySpeed * -3f);
        }

        public static void LeftAndRight()
        {
            if (rightTrigger > 0.5f || rightGrab)
                ZeroGravity();

            if (rightTrigger > 0.5f)
                References.Rigidbody.linearVelocity += References.BodyCollider.transform.right * (Time.deltaTime * FlySpeed * -3f);

            if (rightGrab)
                References.Rigidbody.linearVelocity += References.BodyCollider.transform.right * (Time.deltaTime * FlySpeed * 3f);
        }

        public static void ForwardsAndBackwards()
        {
            if (rightTrigger > 0.5f || rightGrab)
                ZeroGravity();

            if (rightTrigger > 0.5f)
                References.Rigidbody.linearVelocity += References.BodyCollider.transform.forward * (Time.deltaTime * FlySpeed * 3f);

            if (rightGrab)
                References.Rigidbody.linearVelocity += References.BodyCollider.transform.forward * (Time.deltaTime * FlySpeed * -3f);
        }

        public static void AutoWalk()
        {
            Vector2 joy = leftJoystick;

            float armLength = 0.45f;
            float animSpeed = 9f;

            if (leftJoystickClick)
                animSpeed *= 1.5f;

            if (Mathf.Abs(joy.y) > 0.05f || Mathf.Abs(joy.x) > 0.05f)
            {
                References.LeftHand.position = References.BodyCollider.transform.position + References.BodyCollider.transform.forward * (Mathf.Sin(Time.time * animSpeed) * (joy.y * armLength)) + References.BodyCollider.transform.right * (Mathf.Sin(Time.time * animSpeed) * (joy.x * armLength) - 0.2f) + new Vector3(0f, -0.3f + Mathf.Cos(Time.time * animSpeed) * 0.2f, 0f);
                References.RightHand.position = References.BodyCollider.transform.position + References.BodyCollider.transform.forward * (-Mathf.Sin(Time.time * animSpeed) * (joy.y * armLength)) + References.BodyCollider.transform.right * (-Mathf.Sin(Time.time * animSpeed) * (joy.x * armLength) + 0.2f) + new Vector3(0f, -0.3f + Mathf.Cos(Time.time * animSpeed) * -0.2f, 0f);
            }
        }

        public static void AutoFunnyRun()
        {
            if (rightGrab)
            {
                if (bothHands)
                {
                    float time = Time.frameCount;
                    References.RightHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.forward * MathF.Cos(time) / 10 + new Vector3(0, -0.5f - MathF.Sin(time) / 7, 0) + References.HeadCollider.transform.right * -0.05f;
                    References.LeftHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.forward * MathF.Cos(time + 180) / 10 + new Vector3(0, -0.5f - MathF.Sin(time + 180) / 7, 0) + References.HeadCollider.transform.right * 0.05f;
                }
                else
                {
                    float time = Time.frameCount;
                    References.RightHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.forward * MathF.Cos(time) / 10 + new Vector3(0, -0.5f - MathF.Sin(time) / 7, 0);
                }
            }
        }

        public static void AutoPinchClimb()
        {
            if (rightGrab)
            {
                float time = Time.frameCount / 3f;
                References.RightHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.right * (0.4f + MathF.Cos(time) * 0.4f) + References.HeadCollider.transform.up * (MathF.Sin(time) * 0.6f) + References.HeadCollider.transform.forward * 0.75f;
                References.LeftHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.right * -(0.4f + MathF.Cos(time) * 0.4f) + References.HeadCollider.transform.up * (MathF.Sin(time) * 0.6f) + References.HeadCollider.transform.forward * 0.75f;
            }
        }

        public static void AutoElevatorClimb()
        {
            if (rightGrab)
            {
                float time = Time.frameCount / 3f;
                References.RightHand.position = References.HeadCollider.transform.position + References.HeadCollider.transform.right * (0.4f + MathF.Cos(time) * 0.4f) + References.HeadCollider.transform.up * (MathF.Sin(time) * 0.6f) + References.HeadCollider.transform.forward * 0.75f;
            }
        }
        public static Vector3[] GetAllTreeBranchPositions()
        {
            if (posArchive != null)
                return posArchive.ToArray();

            posArchive = new List<Vector3>();

            Vector3[] TreeBranchOffsets = {
                new Vector3(-2.383f, 3.784f, 0.738f),
                new Vector3(1.55f, 5.559f, -1.56f),
                new Vector3(-2.225f, 7.214f, 0.063f),
                new Vector3(1.365f, 6.62f, 0.82f),
                new Vector3(0.405f, 8.865f, -2.759f),
                new Vector3(-2.227f, 9.763f, 2.071f),
                new Vector3(2.421f, 10.91f, 1.313f),
                new Vector3(1.618f, 13.169f, -1.216f),
                new Vector3(2.175f, 12.959f, -0.229f),
                new Vector3(1.855f, 13.837f, 1.215f),
                new Vector3(-0.265f, 14.953f, 2.935f),
                new Vector3(-2.049f, 14.962f, -1.708f),
                new Vector3(-1.249f, 18.93f, -1.62f),
            };

            string[] SmallTreeTargets = {
                "Environment Objects/LocalObjects_Prefab/Forest/Terrain/SmallTrees/Group1",
                "Environment Objects/LocalObjects_Prefab/Forest/Terrain/SmallTrees/Group2"
            };

            foreach (string SmallTreeTarget in SmallTreeTargets)
            {
                GameObject TreeGroupGO = GetObject(SmallTreeTarget);

                for (int i = 0; i < TreeGroupGO.transform.childCount; i++)
                {
                    GameObject v = TreeGroupGO.transform.GetChild(i).gameObject;

                    Vector3 oldlocalscale = v.transform.localScale;
                    v.transform.localScale *= 5;

                    foreach (Vector3 TreeBranchOffset in TreeBranchOffsets)
                        posArchive.Add(v.transform.TransformPoint(TreeBranchOffset));

                    v.transform.localScale = oldlocalscale;
                }
            }

            return posArchive.ToArray();
        }
        public static void AutoBranch()
        {
            bool isOnBranch = false;
            float branchDist = 5f;
            float lerpTime = 0.3f;

            if (rightGrab)
            {
                float dist = float.MaxValue;
                Vector3 closeDist = Vector3.zero;
                Vector3 compareDist = References.BodyCollider.transform.position;

                foreach (Vector3 treeBranchPos in GetAllTreeBranchPositions())
                {
                    float foundDist = Vector3.Distance(compareDist, treeBranchPos);
                    float dot = Vector3.Dot(treeBranchPos - compareDist, References.BodyCollider.transform.right);

                    if (foundDist < dist && dot > 0)
                    {
                        dist = foundDist;
                        closeDist = treeBranchPos;
                    }
                }

                if (dist < branchDist)
                {
                    isOnBranch = true;
                    rightPos = Vector3.Lerp(rightPos, closeDist, lerpTime);
                }
                else
                    rightPos = Vector3.Lerp(rightPos, References.RightHand.position, lerpTime);

                References.RightHand.position = rightPos;

                Vector3 lastFoundDist = closeDist;
                dist = float.MaxValue;
                closeDist = Vector3.zero;
                compareDist = References.BodyCollider.transform.position;

                foreach (Vector3 treeBranchPos in GetAllTreeBranchPositions())
                {
                    float foundDist = Vector3.Distance(compareDist, treeBranchPos);
                    float dot = Vector3.Dot(treeBranchPos - compareDist, References.BodyCollider.transform.right);

                    if (foundDist < dist && treeBranchPos != lastFoundDist && dot < 0)
                    {
                        dist = foundDist;
                        closeDist = treeBranchPos;
                    }
                }

                if (dist < branchDist)
                {
                    isOnBranch = true;
                    leftPos = Vector3.Lerp(leftPos, closeDist, lerpTime);
                }
                else
                    leftPos = Vector3.Lerp(leftPos, References.LeftHand.position, lerpTime);

                References.LeftHand.position = leftPos;
            }
            else
            {
                leftPos = References.LeftHand.position;
                rightPos = References.RightHand.position;
            }

            if (isOnBranch)
                References.Rigidbody.linearVelocity = References.Player.headCollider.transform.forward * 10f;

            switch (isOnBranch)
            {
                case true when !lastOnBranch:
                    UpdateClipColliders(false);
                    break;
                case false when lastOnBranch:
                    UpdateClipColliders(true);
                    break;
            }

            lastOnBranch = isOnBranch;
        }

        public static void ForceTagFreeze() =>
            References.Player.disableMovement = true;

        public static void NoTagFreeze() =>
            References.Player.disableMovement = false;
        public static void FeatherFalling()
        {
            Rigidbody rb = References.Rigidbody;

            if (rb.linearVelocity.magnitude > 0.1f || !References.Tagger.IsGrounded(0.2f))
            {
                Vector3 velocityAdjustment = rb.linearVelocity * 4;
                rb.linearVelocity -= velocityAdjustment * Time.fixedDeltaTime;

                if (References.Tagger.IsGrounded() && rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                                                Mathf.Max(rb.linearVelocity.y, -2f),
                                                rb.linearVelocity.z);
                }
            }
        }
        public static void LowGravity() =>
            References.Rigidbody.AddForce(Vector3.up * 6.66f, ForceMode.Acceleration);

        public static void ZeroGravity() =>
            References.Rigidbody.AddForce(-Physics.gravity, ForceMode.Acceleration);

        public static void HighGravity() =>
            References.Rigidbody.AddForce(Vector3.down * 7.77f, ForceMode.Acceleration);

        public static void ReverseGravity()
        {
            References.Player.SetGravityOverride(References.Player, p => p.AddForce(-Physics.gravity, ForceMode.Acceleration));
            GTPlayerTransform.ApplyRotationOverride(Quaternion.Euler(180f, 0f, 0f), Time.frameCount);
        }

        public static void UnflipCharacter()
        {
            References.Player.UnsetGravityOverride(References.Player);
            GTPlayerTransform.ApplyRotationOverride(Quaternion.identity, Time.frameCount);

        }
        public static void Rewind()
        {
            if (rightTrigger > 0.5f)
            {
                if (playerPositions.Count > 0)
                {
                    object[] targetPos = playerPositions[^1];

                    TeleportPlayer((Vector3)targetPos[0]);

                    References.LeftHand.position = (Vector3)targetPos[1];
                    References.LeftHand.rotation = (Quaternion)targetPos[2];

                    References.RightHand.position = (Vector3)targetPos[3];
                    References.RightHand.rotation = (Quaternion)targetPos[4];

                    References.Rigidbody.linearVelocity = (Vector3)targetPos[5] * -1f;

                    playerPositions.RemoveAt(playerPositions.Count - 1);
                }
            }
            else
            {
                playerPositions.Add(new object[] {
                    References.BodyCollider.transform.position,

                    References.LeftHand.position,
                    References.LeftHand.rotation,

                    References.RightHand.position,
                    References.RightHand.rotation,

                    References.Rigidbody.linearVelocity
                });

                if (playerPositions.Count > 8640)
                    playerPositions.RemoveAt(0);
            }
        }

        public static void ClearRewind() => playerPositions.Clear();
        public static void ApplyMacroPlaybackRange(int index) => macroPlaybackRange = MacroPlaybackRangeAmounts[index];

        public static string FormatMacroName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder();
            foreach (char c in input)
                result.Append(char.IsLetterOrDigit(c) ? c : '-');

            return result.ToString();
        }
        public static void LoadMacros()
        {
            macros.Clear();

            string[] files = Directory.GetFiles($"{PluginInfo.BaseDirectory}/Macros");
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if (!file.EndsWith(".json")) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                Macro macro = Macro.LoadJSON(File.ReadAllText(file));

                macros[fileName] = macro;
            }

            List<Module> buttons = new List<Module>
            {
                new Module { name = "Exit Macros", method =() => Manager.CurrentCategoryName = "Movement Mods", isTogglable = false, toolTip = "Returns you back to the movement mods."},
            };

            int index = 0;
            foreach (KeyValuePair<string, Macro> macroData in macros)
            {
                Macro macro = macroData.Value;
                string macroName = macroData.Key;

                buttons.Add(new Module { name = $"Macro{macroName}", displayName = macro.name, enabled = macro.enabled, enableMethod = () => ToggleMacro(macroName, true), method = () => ExecuteMacroButton(macro), disableMethod = () => ToggleMacro(macroName, false), toolTip = $"Toggles on and off the {macro.name} macro." });
                index++;
            }

            buttons.AddRange(new[]
            {
                new Module { name = "Record <color=grey>[</color><color=green>LT</color><color=grey>]</color>", method = RecordMacro, toolTip = "Record your macros with your <color=green>left trigger</color>." },
                new Module { name = "Open Macros Folder", method = OpenMacrosFolder, isTogglable = false, toolTip = "Opens the folder in which your plugins are located." },
                new Module { name = "Reload Macros", method = LoadMacros, isTogglable = false, toolTip = "Reloads your macros." },
                new Module
                {
                    name = "Enable All Macros",
                    method = () =>
                    {
                        foreach (var kvp in macros)
                        {
                            string macroName = kvp.Key;
                            kvp.Value.enabled = true;
                            ToggleMacro(macroName, true);
                        }

                        LoadMacros();
                    },
                    isTogglable = false,
                    toolTip = "Enables all macros."
                },
                new Module
                {
                    name = "Disable All Macro",
                    method = () =>
                    {
                        foreach (var kvp in macros)
                        {
                            string macroName = kvp.Key;
                            kvp.Value.enabled = false;
                            ToggleMacro(macroName, false);
                        }

                        StopMacro();
                        LoadMacros();
                    },
                    isTogglable = false,
                    toolTip = "Disables all macros."
                }

            });
            Manager.modules["Macros"] = buttons.ToArray();
        }

        public static void OpenMacrosFolder()
        {
            string filePath = Files.GetGamePath() + $"/{PluginInfo.BaseDirectory}/Macros";
            Files.Start(filePath);
        }

        public static void ToggleMacro(string macroName, bool enabled)
        {
            string filePath = $"{PluginInfo.BaseDirectory}/Macros/{macroName}.json";
            if (!File.Exists(filePath))
                return;

            Macro macro = macros[macroName];
            macro.enabled = enabled;

            File.WriteAllText(filePath, macro.DumpJSON());
        }

        public static void RecordMacro()
        {
            if (leftTrigger > 0.5f)
            {
                if (!recordingMacro)
                {
                    recordingData.Clear();
                    recordingMacro = true;

                    Notifications.SendFormat("green", "RECORDING", "Started recording...");
                }

                References.InputPoller.leftControllerIndexFloat = 0f;

                if (recordingMacro && Time.time > positionDelay)
                {
                    positionDelay = Time.time + defaultMacroStep;
                    recordingData.Add(PlayerPosition.CurrentPosition());
                }

                if (recordingMacro && recordingData.Count > 0)
                    VisualizePlayerPosition(recordingData[0], Color.green, indexIdOffset: 2000);
            }
            else
            {
                if (recordingMacro)
                {
                    recordingMacro = false;

                    Notifications.SendFormat("green", "RECORDING", "Stopped recording.");
                    FinalizeRecording();
                }
            }
        }

        public static void FinalizeRecording()
        {
            List<PlayerPosition> savedRecordingData = recordingData;
            Prompt("Would you like to save your macro?", () =>
            {
                PromptText("Please name your macro:", () =>
                {
                    string name = keyboardInput;
                    if (name.IsNullOrEmpty())
                        name = $"Macro #{macros.Count + 1}";

                    Macro macro = new Macro
                    {
                        name = name,
                        positions = savedRecordingData,
                        enabled = true,
                        macroStepDuration = defaultMacroStep
                    };

                    string filePath = Path.Combine(PluginInfo.BaseDirectory, "Macros", FormatMacroName(name) + ".json");

                    File.WriteAllText(filePath, macro.DumpJSON());
                    LoadMacros();
                }, null, "Done", "Cancel");
            });
        }
        public static IEnumerator PlayMacro(Macro macro, int startFromPosition = 0)
        {
            List<PlayerPosition> positions = macro.positions;
            PlayerPosition startPosition = PlayerPosition.CurrentPosition();

            if (startFromPosition > 0 && startFromPosition < positions.Count)
                positions = positions.GetRange(startFromPosition, positions.Count - startFromPosition);

            float macroStartTime = Time.time;
            float macroEndTime = positions.Count * macro.macroStepDuration;

            while (Time.time < macroStartTime + macroEndTime)
            {
                if (rightTrigger < 0.5f)
                {
                    StopMacro();
                    yield break;
                }

                float elapsed = Time.time - macroStartTime;
                float stepElapsed = elapsed % macro.macroStepDuration;

                int currentMacroPosition = Mathf.FloorToInt(elapsed / macro.macroStepDuration);
                currentMacroPosition = Mathf.Clamp(currentMacroPosition, 0, positions.Count);

                PlayerPosition lastPosition = currentMacroPosition - 1 < 0 ? startPosition : positions[currentMacroPosition - 1];
                PlayerPosition currentPosition = positions[currentMacroPosition];

                float t = stepElapsed / macro.macroStepDuration;
                Vector3 position = lastPosition.position.Lerp(currentPosition.position, t);
                TeleportPlayer(position);
                References.LocalRig.transform.position = position;

                References.Rigidbody.linearVelocity = lastPosition.velocity.Lerp(currentPosition.velocity, t);

                References.LeftHand.position = lastPosition.leftHand.position.Lerp(currentPosition.leftHand.position, t);
                References.LeftHand.rotation = lastPosition.leftHand.rotation.Lerp(currentPosition.leftHand.rotation, t);

                References.RightHand.position = lastPosition.rightHand.position.Lerp(currentPosition.rightHand.position, t);
                References.RightHand.rotation = lastPosition.rightHand.rotation.Lerp(currentPosition.rightHand.rotation, t);

                References.InputPoller.leftControllerGripFloat = Mathf.Lerp(lastPosition.leftGrip ? 1f : 0f, currentPosition.leftGrip ? 1f : 0f, t);
                References.InputPoller.rightControllerGripFloat = Mathf.Lerp(lastPosition.rightGrip ? 1f : 0f, currentPosition.rightGrip ? 1f : 0f, t);
                References.InputPoller.leftControllerIndexFloat = Mathf.Lerp(lastPosition.leftTrigger ? 1f : 0f, currentPosition.leftTrigger ? 1f : 0f, t);


                Notifications.information["Macro Time"] = $"{macroEndTime - elapsed:F1}s";
                Notifications.information["Macro Name"] = macro.name;

                yield return null;

                if (currentMacroPosition + (int)(1 / macro.macroStepDuration) < positions.Count)
                {
                    PlayerPosition futurePosition = positions[currentMacroPosition + (int)(1 / macro.macroStepDuration)];
                    VisualizePlayerPosition(futurePosition, Color.cyan, indexIdOffset: 0);
                }

                VisualizePlayerPosition(positions[^1], Color.red, indexIdOffset: 1000);
            }

            StopMacro();
            yield break;
        }

        public static void StopMacro()
        {
            if (activeMacro != null)
            {
                Coroutines.instance.StopCoroutine(activeMacro);
                activeMacro = null;
            }

            Notifications.information.Remove("Macro Time");
            Notifications.information.Remove("Macro Name");
        }

        public static void VisualizePlayerPosition(PlayerPosition position, Color color, float alpha = 0.15f, long indexIdOffset = 0)
        {
            Visuals.Visualize(PrimitiveType.Cube, position.position, Quaternion.LookRotation(position.velocity), new Vector3(0.1f, 0.1f, 0.25f), color, -39228393 + indexIdOffset, alpha);
            Visuals.Visualize(PrimitiveType.Cube, position.position + position.velocity.normalized * 0.125f, Quaternion.LookRotation(position.velocity), new Vector3(0.15f, 0.15f, 0.05f), color, -48492012 + indexIdOffset, alpha);
            Visuals.Visualize(PrimitiveType.Sphere, position.leftHand.position, Quaternion.identity, new Vector3(0.15f, 0.15f, 0.15f), color, -58692092 + indexIdOffset, alpha);
            Visuals.Visualize(PrimitiveType.Sphere, position.rightHand.position, Quaternion.identity, new Vector3(0.15f, 0.15f, 0.15f), color, -65843922 + indexIdOffset, alpha);
        }
        public static void ExecuteMacroButton(Macro macro)
        {
            didMacro = midpointMacros && (didMacro
                ? rightTrigger >= 0.5f
                : activeMacro != null);

            if (rightTrigger < 0.5f || activeMacro != null || didMacro)
                return;

            int position = 0;
            if (midpointMacros)
            {
                position = macro.positions
                    .Select((position, index) => new { position, index, distance = Vector3.Distance(References.BodyCollider.transform.position, position.position) })
                    .OrderBy(x => x.distance)
                    .FirstOrDefault()
                    .index;
            }

            PlayerPosition startPosition = macro.positions[position];

            if (frameStepper)
            {
                if (rightTriggerPressed && !frameStepperNotified)
                {
                    Notifications.SendFormat("green", "MACRO", "Frame Stepper is on. Hit the A button to progress instead.");
                    frameStepperNotified = true;
                }
                if (!frameCompleted && rightPrimary)
                    frameCompleted = true;

                if (!frameCompleted)
                {
                    VisualizePlayerPosition(startPosition, Color.white, 0.05f);
                    return;
                }

                frameCompleted = false;
            }

            bool doMacro = !directionBased || (References.Rigidbody.linearVelocity.magnitude > 2f && Vector3.Angle(startPosition.velocity.normalized, References.Rigidbody.linearVelocity.normalized) < 70f);

            VisualizePlayerPosition(startPosition, doMacro ? buttonColors[1].GetCurrentColor() : Color.white, doMacro ? 0.15f : 0.05f);
            if (doMacro)
                Visuals.Visualize(PrimitiveType.Sphere, startPosition.position, Quaternion.identity, new Vector3(1f, 1f, 1f), buttonColors[1].GetCurrentColor(), -1, 0.05f);
            else
                return;

            if (Vector3.Distance(References.BodyCollider.transform.position, startPosition.position) < 1f)
                activeMacro = Coroutines.instance.StartCoroutine(PlayMacro(macro, position));
        }
        public static void ApplyWallWalkStrength(int index) => wallWalkStrength = WallWalkStrengthAmounts[index];

        public static void WallWalk()
        {
            if (References.Player.IsHandTouching(true) || References.Player.IsHandTouching(false))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;
                walkPos = ray.point;
                walkNormal = ray.normal;
            }

            bool wallWalkKey =
                bothWallWalk ? leftGrab || rightGrab :
                leftWallWalk ? leftGrab : rightGrab;

            if (walkPos != Vector3.zero && wallWalkKey)
            {
                References.Rigidbody.AddForce(walkNormal * -wallWalkStrength, ForceMode.Acceleration);
                ZeroGravity();
            }
        }


        public static void LegitimateWallWalk()
        {
            float range = 0.2f;
            float power = -2f;

            if (leftGrab && (leftWallWalk || bothHands))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;

                if (Physics.Raycast(Controllers.GetTrueLeftHand().position, -ray.normal, out var Ray, range, References.Player.locomotionEnabledLayers))
                    References.Rigidbody.AddForce(Ray.normal * power, ForceMode.Acceleration);
            }

            if (rightGrab && (!leftWallWalk || bothHands))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;

                if (Physics.Raycast(Controllers.GetTrueRightHand().position, -ray.normal, out var Ray, range, References.Player.locomotionEnabledLayers))
                    References.Rigidbody.AddForce(Ray.normal * power, ForceMode.Acceleration);
            }
        }
        public static void SpiderWalk()
        {
            if (References.Player.IsHandTouching(true) || References.Player.IsHandTouching(false))
            {
                RaycastHit ray = References.Player.lastHitInfoHand;
                Vector3 up = ray.normal.normalized;
                Vector3 forward = Vector3.Cross(Vector3.right, up);
                tappedRot = Quaternion.LookRotation(forward, up);
            }

            float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
            spiderRot = Quaternion.Slerp(spiderRot, tappedRot, t);
            GTPlayerTransform.ApplyRotationOverride(spiderRot, Time.frameCount);
            References.Player.SetGravityOverride(References.Player, p => p.AddForce(spiderRot * Physics.gravity, ForceMode.Acceleration));
        }

        public static void TeleportToRandom()
        {
            closePosition = Vector3.zero;
            TeleportPlayer(GetRandomVRRig().transform.position);
            References.Rigidbody.linearVelocity = Vector3.zero;
        }

        public static void TeleportToPlayer(NetPlayer plr) => TeleportPlayer(GetVRRigFromPlayer(plr).headMesh.transform.position);

        public static void ExitTeleportToMap()
        {
            Manager.CurrentCategoryName = "Movement Mods";
            pageNumber = rememberPageNumber;
        }

        public static void EnterTeleportToMap()
        {
            rememberPageNumber = pageNumber;
            List<Module> tpbuttons = new List<Module>
            {
                new Module
                {
                    name = "Exit Teleport to Map",
                    method = ExitTeleportToMap,
                    isTogglable = false,
                    toolTip = "Returns you back to the movement mods."
                }
            };

            foreach (KeyValuePair<string, GTZone> entry in mapData)
            {
                tpbuttons.Add(new Module
                {
                    name = "TeleportMap" + tpbuttons.Count,
                    displayName = entry.Key,
                    method = () => TeleportToMap(entry.Value),
                    isTogglable = false,
                    toolTip = "Teleports you to the " + entry.Key + " map."
                });
            }

            Manager.modules["Temporary Category"] = tpbuttons.ToArray();
            Manager.CurrentCategoryName = "Temporary Category";
        }

        public static void TeleportToMap(GTZone zone)
        {
            GorillaNetworkJoinTrigger trigger = ManagerRegistry.PhotonNetworkController.allJoinTriggers.FirstOrDefault(t => t.zone == zone);
            ZoneManagement.SetActiveZone(zone);
            trigger?.OnBoxTriggered();
            TeleportPlayer(trigger?.transform.position ?? References.LocalRig.transform.position);
        }
        public static void TeleportGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                {
                    closePosition = Vector3.zero;
                    TeleportPlayer(NewPointer.transform.position + Vector3.up);
                    References.Rigidbody.linearVelocity = Vector3.zero;
                }

                previousTeleportTrigger = GetGunInput(true);
            }, new GunLibrary.GunSettings { when = () => !previousTeleportTrigger });
        }

        public static void Airstrike()
        {
            GunLibrary.Point(NewPointer =>
            {
                {
                    References.Rigidbody.linearVelocity = new Vector3(0f, -20f, 0f);
                    TeleportPlayer(NewPointer.transform.position + new Vector3(0f, 30f, 0f));
                    References.Rigidbody.linearVelocity = new Vector3(0f, -20f, 0f);
                }

                previousTeleportTrigger = GetGunInput(true);
            }, new GunLibrary.GunSettings { when = () => !previousTeleportTrigger });
        }
        public static void Checkpoint()
        {
            if (rightGrab)
            {
                if (CheckPoint == null)
                {
                    CheckPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(CheckPoint.GetComponent<SphereCollider>());
                    CheckPoint.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                }
                CheckPoint.transform.position = References.RightHand.position;
            }
            if (CheckPoint != null)
            {
                if (rightPrimary)
                {
                    CheckPoint.GetComponent<Renderer>().material.color = backgroundColor.GetColor(0);
                    TeleportPlayer(CheckPoint.transform.position);
                    References.Rigidbody.linearVelocity = Vector3.zero;
                }
                else
                    CheckPoint.GetComponent<Renderer>().material.color = buttonColors[0].GetColor(0);
            }
        }

        public static void DisableCheckpoint()
        {
            if (CheckPoint != null)
            {
                Object.Destroy(CheckPoint);
                CheckPoint = null;
            }
        }
        public static void AdvancedCheckpoints()
        {
            if (rightGrab)
            {
                bool isNearCheckpoint = false;
                foreach (var checkpoint in checkpoints.Where(checkpoint => Vector3.Distance(References.RightHand.position, checkpoint.transform.position) < 0.2f))
                {
                    isNearCheckpoint = true;
                    checkpoint.transform.position = References.RightHand.transform.position;
                    break;
                }

                if (!isNearCheckpoint)
                {
                    GameObject newCheckpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(newCheckpoint.GetComponent<SphereCollider>());
                    newCheckpoint.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    newCheckpoint.transform.position = References.RightHand.position;
                    newCheckpoint.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                    newCheckpoint.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();

                    GameObject MeshHolder = new GameObject("Label");
                    MeshHolder.transform.parent = newCheckpoint.transform;
                    MeshHolder.transform.localPosition = Vector3.zero;
                    TextMeshPro newMesh = MeshHolder.AddComponent<TextMeshPro>();

                    Renderer MeshRender = newMesh.GetComponent<Renderer>();
                    MeshRender.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                    newMesh.fontSize = 1.2f;
                    newMesh.SafeSetFont(activeFont);
                    newMesh.SafeSetFontStyle(activeFontStyle);
                    newMesh.alignment = TextAlignmentOptions.Center;
                    newMesh.Chams();
                    newMesh.color = Color.white;
                    newMesh.text = (checkpoints.Count + 1).ToString();

                    MeshRender.material.renderQueue = newCheckpoint.GetComponent<Renderer>().material.renderQueue + 2;
                    checkpoints.Add(newCheckpoint);
                }
            }

            if (rightTrigger > 0.5f)
            {
                foreach (var checkpoint in checkpoints.ToList().Where(checkpoint => Vector3.Distance(References.RightHand.position, checkpoint.transform.position) < 0.2f))
                {
                    checkpoints.Remove(checkpoint);
                    Object.Destroy(checkpoint);
                }
            }

            if (rightPrimary)
                TeleportPlayer(checkpoints[selectedCheckpoint].transform.position);

            foreach (GameObject checkpoint in checkpoints)
            {
                checkpoint.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();

                GameObject textObject = checkpoint.transform.Find("Label").gameObject;
                textObject.transform.LookAt(Camera.main.transform.position);
                textObject.transform.Rotate(0f, 180f, 0f);
            }

            if (Mathf.Abs(rightJoystick.y) > 0.5f && Time.time > selectedCheckpointDelay)
            {
                selectedCheckpointDelay = Time.time + 0.2f;
                selectedCheckpoint += rightJoystick.y > 0 ? 1 : -1;

                if (selectedCheckpoint < 0)
                    selectedCheckpoint = checkpoints.Count - 1;
            }

            if (selectedCheckpoint < 0 && checkpoints.Count > 0)
                selectedCheckpoint = 0;

            if (selectedCheckpoint > checkpoints.Count - 1)
                selectedCheckpoint = 0;

            Visuals.GetLabel("CheckpointLabel", false, (selectedCheckpoint + 1).ToString(), Color.white);
        }

        public static void DisableAdvancedCheckpoints()
        {
            selectedCheckpoint = 0;

            foreach (GameObject checkpoint in checkpoints)
                Object.Destroy(checkpoint);

            checkpoints.Clear();
        }
        public static void Bomb()
        {
            if (rightGrab)
            {
                if (BombObject == null)
                {
                    BombObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Object.Destroy(BombObject.GetComponent<SphereCollider>());
                    BombObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                }
                BombObject.transform.position = References.RightHand.position;
            }
            if (BombObject != null)
            {
                if (rightPrimary)
                {
                    Vector3 dir = References.BodyCollider.transform.position - BombObject.transform.position;
                    dir.Normalize();
                    References.Rigidbody.linearVelocity += 25f * dir;
                    Object.Destroy(BombObject);
                    BombObject = null;
                }
                else
                    BombObject.GetComponent<Renderer>().material.color = buttonColors[0].GetColor(0);
            }
        }

        public static void DisableBomb()
        {
            if (BombObject != null)
            {
                Object.Destroy(BombObject);
                BombObject = null;
            }
        }
        public static void EnderPearl()
        {
            if (rightGrab || leftGrab)
            {
                if (pearl == null)
                {
                    pearl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(pearl.GetComponent<Collider>());

                    pearl.transform.localScale = new Vector3(0.1f, 0.1f, 0.01f);
                    if (pearlmat == null)
                    {
                        pearlmat = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                        {
                            color = Color.white
                        };
                        if (pearltxt == null)
                        {
                            pearltxt = LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/Mods/Movement/pearl.png", "Images/Mods/Movement/pearl.png");
                            pearltxt.filterMode = FilterMode.Point;
                            pearltxt.wrapMode = TextureWrapMode.Clamp;
                        }
                        pearlmat.mainTexture = pearltxt;

                        pearlmat.SetFloat("_Surface", 1);
                        pearlmat.SetFloat("_Blend", 0);
                        pearlmat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        pearlmat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        pearlmat.SetFloat("_ZWrite", 0);
                        pearlmat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        pearlmat.renderQueue = (int)RenderQueue.Transparent;
                    }
                    pearl.GetComponent<Renderer>().material = pearlmat;
                }

                if (pearl.GetComponent<Rigidbody>() != null)
                    Object.Destroy(pearl.GetComponent<Rigidbody>());

                isrighthandedpearl = rightGrab;
                pearl.transform.position = rightGrab ? References.RightHand.position : References.LeftHand.position;
            }
            else
            {
                if (pearl != null)
                {
                    if (pearl.GetComponent<Rigidbody>() == null)
                    {
                        Rigidbody comp = pearl.AddComponent(typeof(Rigidbody)) as Rigidbody;
                        comp.linearVelocity = isrighthandedpearl ? References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0) : References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0);
                    }
                    Physics.Raycast(pearl.transform.position, pearl.GetComponent<Rigidbody>().linearVelocity, out var Ray, 0.25f, References.Player.locomotionEnabledLayers);
                    if (Ray.collider != null)
                    {
                        TeleportPlayer(pearl.transform.position);
                        if (Networking.InRoom)
                            References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 84, true, 999999f);
                        else
                            References.LocalRig.PlayHandTapLocal(84, true, 999999f);

                        RPCProtection();
                        Object.Destroy(pearl);
                    }
                }
            }
            if (pearl != null)
            {
                pearl.transform.LookAt(References.HeadCollider.transform.position);
                pearl.GetComponent<Rigidbody>()?.AddForce(Vector3.up * (Time.deltaTime * (6.66f / Time.deltaTime)), ForceMode.Acceleration);
            }
        }

        public static void DestroyEnderPearl()
        {
            if (pearl != null)
                Object.Destroy(pearl);
        }

        public static void SpeedBoost()
        {
            float jspt = jspeed;
            float jmpt = jmulti;

            if (Manager.GetIndex("Factored Speed Boost").enabled)
            {
                jspt = jspt / 6.5f * References.Player.maxJumpSpeed;
                jmpt = jmpt / 1.1f * References.Player.jumpMultiplier;
            }

            if (!Manager.GetIndex("Disable Max Speed Modification").enabled)
                References.Player.maxJumpSpeed = jspt;

            References.Player.jumpMultiplier = jmpt;
        }

        public static void FunMove() =>
            References.Rigidbody.linearVelocity += References.Rigidbody.linearVelocity * Time.deltaTime;

        public static void DynamicSpeedBoost()
        {
            bool isTagged = References.LocalRig.IsTagged();

            VRRig closestRig = Players.ActiveRigs
                .Where(rig => rig != null && !rig.isLocal &&
                                  (isTagged ? !rig.IsTagged() : rig.IsTagged()))
                .OrderBy(rig => Vector3.Distance(rig.transform.position, References.BodyCollider.transform.position))
                .FirstOrDefault();

            float rigDistance = closestRig == null ? float.MaxValue :
                          Vector3.Distance(References.BodyCollider.transform.position, closestRig.transform.position);

            if (rigDistance < 15f)
            {
                float jspt = jspeed;
                float jmpt = jmulti;

                if (Manager.GetIndex("Factored Speed Boost").enabled)
                {
                    jspt = jspt / 6.5f * References.Player.maxJumpSpeed;
                    jmpt = jmpt / 1.1f * References.Player.jumpMultiplier;
                }

                jspt = Mathf.Lerp(References.Player.maxJumpSpeed, jspt, Mathf.Clamp(rigDistance, 1f, 15f) / 15f);
                jmpt = Mathf.Lerp(References.Player.jumpMultiplier, jmpt, Mathf.Clamp(rigDistance, 1f, 15f) / 15f);

                if (!Manager.GetIndex("Disable Max Speed Modification").enabled)
                    References.Player.maxJumpSpeed = jspt;

                References.Player.jumpMultiplier = jmpt;
            }
        }

        public static void AlwaysMaxVelocity()
        {
            if (Manager.GetIndex("Uncap Max Velocity").enabled)
                Toggle("Uncap Max Velocity");
            else
                References.Player.jumpMultiplier = 99999f;
        }
        public static void DisableVelocityCap()
        {
            playspace = GetAllType<Playspace>().FirstOrDefault();
            playspace.enabled = false;
        }

        private static int PlatformLayer => LayerMask.NameToLayer("BuilderProp");

        public static void UpdateClipColliders(bool enabled)
        {
            foreach (MeshCollider v in Resources.FindObjectsOfTypeAll<MeshCollider>())
            {
                if (v.gameObject.layer == PlatformLayer)
                    continue;

                v.enabled = enabled;
            }
        }

        public static void Noclip()
        {
            bool gripNoclip = Manager.GetIndex("Grip Noclip").enabled;
            if (gripNoclip ? rightGrab : rightTrigger > 0.5f || Manager.GetIndex("Constant Noclip").enabled)
            {
                if (!noclip)
                {
                    noclip = true;
                    UpdateClipColliders(false);
                }
            }
            else
            {
                if (noclip)
                {
                    noclip = false;
                    UpdateClipColliders(true);
                }
            }
        }
        public static void RemoveForestColliders()
        {
            Transform ForestCollisions = GetObject("Environment Objects/LocalObjects_Prefab/ForestToHoverboard/TurnOnInForestAndHoverboard/ForestDome_CollisionOnly").transform;

            if (ForestCollisions == null)
                return;

            for (int i = 2; i < 4; i++)
            {
                GameObject c = ForestCollisions.transform.GetChild(i).gameObject;
                c.SetActive(false);
                forestColliders.Add(c);
            }
        }

        public static void RestoreForestColliders()
        {
            foreach (GameObject c in forestColliders)
                c.SetActive(true);

            forestColliders.Clear();
        }

        public static void Invisible()
        {
            bool hit = rightSecondary;
            if (Manager.GetIndex("Non-Togglable Invisible").enabled)
                invisMonke = hit;
            if (invisMonke)
            {
                References.LocalRig.enabled = false;
                References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;
            }
            if (hit && !lastHit2)
            {
                invisMonke = !invisMonke;
                if (invisMonke)
                    wasDisabledAlready = References.LocalRig.enabled;
                else
                    References.LocalRig.enabled = wasDisabledAlready;
            }
            lastHit2 = hit;
        }
        public static void Ghost()
        {
            bool hit = rightPrimary;
            if (Manager.GetIndex("Non-Togglable Ghost").enabled)
                ghostMonke = hit;

            References.LocalRig.enabled = !ghostMonke;
            if (hit && !lastHit)
                ghostMonke = !ghostMonke;

            lastHit = hit;
        }

        public static void EnableRig()
        {
            References.LocalRig.enabled = true;
            ghostException = false;
        }

        public static void RigGun()
        {
            GunLibrary.Aim(GunData =>
            {
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true))
                {
                    References.LocalRig.enabled = false;
                    References.LocalRig.transform.position = NewPointer.transform.position + new Vector3(0, 1, 0);
                }
                else
                    References.LocalRig.enabled = true;
            });
        }

        public static void LookAtGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    References.LocalRig.head.rigTarget.LookAt(lockTarget.head.rigTarget.transform);

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
                }
            });
        }

        public static void ShadowCloneGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;
                    Vector3 offset = lockTarget.transform.right * 0.9f;

                    References.LocalRig.transform.position = lockTarget.transform.position + offset;
                    References.LocalRig.transform.rotation = lockTarget.transform.rotation;
                    References.LocalRig.head.rigTarget.rotation = lockTarget.head.rigTarget.rotation;

                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.leftHand.rigTarget.transform.position + offset;
                    References.LocalRig.leftHand.rigTarget.transform.rotation = lockTarget.leftHand.rigTarget.transform.rotation;

                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.rightHand.rigTarget.transform.position + offset;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = lockTarget.rightHand.rigTarget.transform.rotation;
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void PCRigPose()
        {
            if (XRSettings.isDeviceActive || !References.LocalRig.enabled) return;

            Transform bodyTransform = References.LocalRig.bodyRenderer.transform;

            References.Player.leftHand.controllerTransform.position = bodyTransform.position + bodyTransform.right * -0.7f;
            References.Player.leftHand.controllerTransform.rotation = bodyTransform.rotation;

            References.Player.rightHand.controllerTransform.position = bodyTransform.position + bodyTransform.right * 0.7f;
            References.Player.rightHand.controllerTransform.rotation = bodyTransform.rotation;
        }

        public static void JerkOffAnimation()
        {
            Transform hand = References.Player.rightHand.controllerTransform;
            Transform body = References.BodyCollider.transform;

            float bob = Mathf.Sin(Time.time * 6f) * 0.1f;
            hand.position = body.position + Vector3.down * 0.2f + body.rotation * new Vector3(0f, 0f, bob + 0.1f);
            hand.rotation = Quaternion.Euler(0f, 10f, 0f) * body.rotation;

            References.InputPoller.rightControllerGripFloat = 1f;
            References.InputPoller.rightControllerIndexFloat = 1f;
            References.InputPoller.rightControllerPrimaryButton = true;
        }

        public static void GrabRig()
        {
            if (rightGrab)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.RightHand.position;
                References.LocalRig.transform.rotation = References.RightHand.rotation;
            }
            else
            {
                References.LocalRig.enabled = true;
            }
        }
        public static void EnableSpazRig()
        {
            ghostException = true;
            offsetLH = References.LocalRig.leftHand.trackingPositionOffset;
            offsetRH = References.LocalRig.rightHand.trackingPositionOffset;
            offsetH = References.LocalRig.head.trackingPositionOffset;
        }

        public static void SpazRig()
        {
            if (rightPrimary)
            {
                float spazAmount = 0.1f;
                ghostException = true;
                References.LocalRig.leftHand.trackingPositionOffset = offsetLH + RandomVector3(spazAmount);
                References.LocalRig.rightHand.trackingPositionOffset = offsetRH + RandomVector3(spazAmount);
                References.LocalRig.head.trackingPositionOffset = offsetH + RandomVector3(spazAmount);
            }
            else
            {
                ghostException = false;
                References.LocalRig.leftHand.trackingPositionOffset = offsetLH;
                References.LocalRig.rightHand.trackingPositionOffset = offsetRH;
                References.LocalRig.head.trackingPositionOffset = offsetH;
            }
        }

        public static void DisableSpazRig()
        {
            ghostException = false;
            References.LocalRig.leftHand.trackingPositionOffset = offsetLH;
            References.LocalRig.rightHand.trackingPositionOffset = offsetRH;
            References.LocalRig.head.trackingPositionOffset = offsetH;
        }

        public static void SpazHands()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                References.LocalRig.leftHand.rigTarget.transform.position = References.LeftHand.position + References.LocalRig.leftHand.rigTarget.transform.forward * 3f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.RightHand.position + References.LocalRig.rightHand.rigTarget.transform.forward * 3f;
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void SpazRealHands()
        {
            if (rightPrimary)
            {
                References.Player.GetControllerTransform(true).rotation = RandomQuaternion();
                References.Player.GetControllerTransform(true).position = References.LeftHand.position + References.Player.GetControllerTransform(true).forward * 3f;

                References.Player.GetControllerTransform(false).rotation = RandomQuaternion();
                References.Player.GetControllerTransform(false).position = References.RightHand.position + References.Player.GetControllerTransform(false).forward * 3f;
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void FreezeRigLimbs()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
        }

        public static void FixRigHandRotation()
        {
            References.LocalRig.leftHand.rigTarget.transform.rotation *= Quaternion.Euler(References.LocalRig.leftHand.trackingRotationOffset);
            References.LocalRig.rightHand.rigTarget.transform.rotation *= Quaternion.Euler(References.LocalRig.rightHand.trackingRotationOffset);
        }

        public static void FreezeRigBody()
        {
            References.LocalRig.enabled = false;

            var (leftPosition, leftRotation, _, _, _) = Controllers.GetTrueLeftHand();
            var (rightPosition, rightRotation, _, _, _) = Controllers.GetTrueRightHand();

            References.LocalRig.leftHand.rigTarget.transform.position = leftPosition;
            References.LocalRig.rightHand.rigTarget.transform.position = rightPosition;

            References.LocalRig.leftHand.rigTarget.transform.rotation = leftRotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = rightRotation;

            FixRigHandRotation();

            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;
        }
        public static void FreezeRig()
        {
            if (startPosition == null)
                startPosition = References.LocalRig.transform.position;

            References.LocalRig.enabled = true;
            References.LocalRig.PostTick();
            References.LocalRig.transform.position = startPosition.Value;
            References.LocalRig.enabled = false;
        }

        public static void ParalyzeRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.08f + References.BodyCollider.transform.up * 0.12f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.08f + References.BodyCollider.transform.up * 0.12f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
        }

        public static void ChickenRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.HeadCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.2f + References.BodyCollider.transform.up * -0.2f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.2f + References.BodyCollider.transform.up * -0.2f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
        }

        public static void AmputateRig()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(160f, 90f, 0f);

            References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.08f + References.BodyCollider.transform.up * 0.12f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.08f + References.BodyCollider.transform.up * 0.12f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 180f);
        }

        public static void DecapitateRigUpdate() =>
            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation * Quaternion.Euler(160f, 90f, 0f);

        public static void SetBodyPatch(bool enabled, int mode = 0)
        {
            Torso.enabled = enabled;
            Torso.mode = mode;

            if (!enabled && recBodyRotary != null)
                Object.Destroy(recBodyRotary);
        }
        public static void RecRoomBody()
        {
            SetBodyPatch(true, 3);

            if (recBodyRotary == null)
                recBodyRotary = new GameObject("ii_recBodyRotary");

            recBodyRotary.transform.rotation = Quaternion.Lerp(recBodyRotary.transform.rotation, Quaternion.Euler(0f, References.HeadCollider.transform.rotation.eulerAngles.y, 0f), Time.deltaTime * 6.5f);
        }

        public static void FreezeBodyRotation()
        {
            SetBodyPatch(true, 3);

            if (recBodyRotary == null)
                recBodyRotary = new GameObject("ii_recBodyRotary");

            recBodyRotary.transform.rotation = rightGrab ? recBodyRotary.transform.rotation : Quaternion.Euler(0f, References.HeadCollider.transform.rotation.eulerAngles.y, 0f);
        }

        public static void AutoDance()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                Vector3 bodyOffset = References.BodyCollider.transform.right * (Mathf.Cos(Time.frameCount / 20f) * 0.3f) + new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.frameCount / 20f) * 0.2f), 0f);
                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f) + bodyOffset;
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.forward * 0.2f + References.LocalRig.transform.right * -0.4f + References.LocalRig.transform.up * (0.3f + Mathf.Sin(Time.frameCount / 20f) * 0.2f);
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.forward * 0.2f + References.LocalRig.transform.right * 0.4f + References.LocalRig.transform.up * (0.3f + Mathf.Sin(Time.frameCount / 20f) * -0.2f);

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void AutoGriddy()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                Vector3 bodyOffset = References.LocalRig.transform.forward * (5f * Time.deltaTime);
                References.LocalRig.transform.position = References.LocalRig.transform.position + bodyOffset;
                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -0.33f + References.LocalRig.transform.forward * (0.5f * Mathf.Cos(Time.frameCount / 10f)) + References.LocalRig.transform.up * (-0.5f * Mathf.Abs(Mathf.Sin(Time.frameCount / 10f)));
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 0.33f + References.LocalRig.transform.forward * (0.5f * Mathf.Cos(Time.frameCount / 10f)) + References.LocalRig.transform.up * (-0.5f * Mathf.Abs(Mathf.Sin(Time.frameCount / 10f)));

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void AutoTPose()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void Helicopter()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position += new Vector3(0f, 0.05f, 0f);
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void Beyblade()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }
        public static void StillBeyblade()
        {
            if (rightPrimary)
            {
                if (stillBeybladeStartPos == Vector3.zero)
                    stillBeybladeStartPos = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);

                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = stillBeybladeStartPos;
                References.LocalRig.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 10f, 0f));

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
            {
                stillBeybladeStartPos = Vector3.zero;
                References.LocalRig.enabled = true;
            }
        }

        public static void TorsoPatch_VRRigLateUpdate() =>
            References.LocalRig.transform.rotation *= Quaternion.Euler(0f, Time.time * 180f % 360f, 0f);

        public static void Fan()
        {
            if (rightPrimary)
            {
                References.LocalRig.enabled = false;

                References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
                References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

                References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + (References.LocalRig.transform.up * (Mathf.Cos(Time.time * 15f) * 2f) + References.LocalRig.transform.right * (Mathf.Sin(Time.time * 15f) * 2f));
                References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position - (References.LocalRig.transform.up * (Mathf.Cos(Time.time * 15f) * 2f) + References.LocalRig.transform.right * (Mathf.Sin(Time.time * 15f) * 2f));

                References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                FixRigHandRotation();
            }
            else
                References.LocalRig.enabled = true;
        }

        public static void GhostAnimations()
        {
            References.LocalRig.enabled = false;

            if (headPos == Vector3.zero)
                headPos = References.HeadCollider.transform.position;
            if (headRot == Vector3.zero)
                headRot = References.HeadCollider.transform.rotation.eulerAngles;

            if (handPos_L == Vector3.zero)
                handPos_L = References.LeftHand.transform.position;
            if (handRot_L == Vector3.zero)
                handRot_L = References.LeftHand.transform.rotation.eulerAngles;

            if (handPos_R == Vector3.zero)
                handPos_R = References.RightHand.transform.position;
            if (handRot_R == Vector3.zero)
                handRot_R = References.RightHand.transform.rotation.eulerAngles;

            float positionSpeed = 0.01f;
            float rotationSpeed = 2.0f;
            float positionThreshold = 0.05f;
            float rotationThreshold = 11.5f;
            if (Vector3.Distance(headPos, References.HeadCollider.transform.position) > positionThreshold)
                headPos += Vector3.Normalize(References.HeadCollider.transform.position - headPos) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(headRot), References.HeadCollider.transform.rotation) > rotationThreshold)
                headRot = Quaternion.RotateTowards(Quaternion.Euler(headRot), References.HeadCollider.transform.rotation, rotationSpeed).eulerAngles;

            if (Vector3.Distance(handPos_L, References.LeftHand.transform.position) > positionThreshold)
                handPos_L += Vector3.Normalize(References.LeftHand.transform.position - handPos_L) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(handRot_L), References.LeftHand.transform.rotation) > rotationThreshold)
                handRot_L = Quaternion.RotateTowards(Quaternion.Euler(handRot_L), References.LeftHand.transform.rotation, rotationSpeed).eulerAngles;

            if (Vector3.Distance(handPos_R, References.RightHand.transform.position) > positionThreshold)
                handPos_R += Vector3.Normalize(References.RightHand.transform.position - handPos_R) * positionSpeed;

            if (Quaternion.Angle(Quaternion.Euler(handRot_R), References.RightHand.transform.rotation) > rotationThreshold)
                handRot_R = Quaternion.RotateTowards(Quaternion.Euler(handRot_R), References.RightHand.transform.rotation, rotationSpeed).eulerAngles;

            References.LocalRig.transform.position = headPos - new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = Quaternion.Euler(new Vector3(0f, headRot.y, 0f));

            References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(headRot);

            References.LocalRig.leftHand.rigTarget.transform.position = handPos_L;
            References.LocalRig.rightHand.rigTarget.transform.position = handPos_R;

            References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(handRot_L);
            References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(handRot_R);

            References.LocalRig.leftIndex.calcT = leftTrigger;
            References.LocalRig.leftMiddle.calcT = leftGrab ? 1 : 0;
            References.LocalRig.leftThumb.calcT = leftPrimary || leftSecondary ? 1 : 0;

            References.LocalRig.leftIndex.LerpFinger(1f, false);
            References.LocalRig.leftMiddle.LerpFinger(1f, false);
            References.LocalRig.leftThumb.LerpFinger(1f, false);

            References.LocalRig.rightIndex.calcT = rightTrigger;
            References.LocalRig.rightMiddle.calcT = rightGrab ? 1 : 0;
            References.LocalRig.rightThumb.calcT = rightPrimary || rightSecondary ? 1 : 0;

            References.LocalRig.rightIndex.LerpFinger(1f, false);
            References.LocalRig.rightMiddle.LerpFinger(1f, false);
            References.LocalRig.rightThumb.LerpFinger(1f, false);

            FixRigHandRotation();
        }

        public static void DisableGhostAnimations()
        {
            headPos = Vector3.zero;
            headRot = Vector3.zero;

            handPos_L = Vector3.zero;
            handRot_L = Vector3.zero;

            handPos_R = Vector3.zero;
            handRot_R = Vector3.zero;

            References.LocalRig.enabled = true;
        }

        public static void MinecraftAnimations()
        {
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = References.BodyCollider.transform.position + new Vector3(0f, 0.15f, 0f);
            References.LocalRig.transform.rotation = References.BodyCollider.transform.rotation;

            References.LocalRig.head.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.BodyCollider.transform.rotation;

            if (rightPrimary)
            {
                References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.25f + References.BodyCollider.transform.up * -1f + References.BodyCollider.transform.forward * Mathf.Sin(Time.frameCount / 10f);
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + References.BodyCollider.transform.up * -1f + -(References.BodyCollider.transform.forward * Mathf.Sin(Time.frameCount / 10f));
            }
            else
            {
                References.LocalRig.leftHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * -0.25f + References.BodyCollider.transform.up * -1f;
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + References.BodyCollider.transform.up * -1f;
            }

            if (rightSecondary)
            {
                References.LocalRig.rightHand.rigTarget.transform.position = References.BodyCollider.transform.position + References.BodyCollider.transform.right * 0.25f + Vector3.Lerp(References.RightHand.forward, -References.RightHand.up, 0.5f) * 2f;
                References.LocalRig.rightHand.rigTarget.transform.rotation = References.RightHand.rotation;
            }

            FixRigHandRotation();
        }

        public static void StareAtNearby() =>
            References.LocalRig.head.rigTarget.LookAt(GetClosestVRRig().headMesh.transform.position);

        public static void StareAtTarget() =>
            References.LocalRig.head.rigTarget.LookAt(lockTarget.headMesh.transform.position);
        public static void StareAtGun()
        {
            GunLibrary.Target(gunTarget =>
            {
                if (!hasAdded)
                {
                    hasAdded = true;
                    Torso.VRRigLateUpdate += StareAtTarget;
                }

                gunLocked = true;
                lockTarget = gunTarget;
            }, new GunLibrary.GunSettings
            {
                onIdle = () =>
                {
                    if (gunLocked)
                    {
                        gunLocked = false;
                        if (hasAdded)
                        {
                            hasAdded = false;
                            Torso.VRRigLateUpdate -= StareAtTarget;
                        }
                    }
                }
            });
        }

        public static void StareAtAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Quaternion headRotArchive = References.LocalRig.head.rigTarget.transform.rotation;
                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    References.LocalRig.head.rigTarget.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(GetVRRigFromPlayer(Player).headMesh.transform.position));
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();
                References.LocalRig.head.rigTarget.transform.rotation = headRotArchive;

                return false;
            };
        }

        public static void EyeContact()
        {
            foreach (VRRig rig in Players.ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                if (Physics.SphereCast(rig.headMesh.transform.position + (rig.headMesh.transform.forward * 0.25f), 0.25f, rig.headMesh.transform.forward, out _, 512f, NoInvisLayerMask()))
                {
                    References.LocalRig.head.rigTarget.LookAt(rig.headMesh.transform.position);
                    break;
                }
            }
        }

        public static void EnableFloatingRig() =>
            offsetH = References.LocalRig.head.trackingPositionOffset;

        public static void FloatingRig() =>
            References.LocalRig.head.trackingPositionOffset = offsetH + new Vector3(0f, 0.65f + Mathf.Sin(Time.frameCount / 40f) * 0.2f, 0f);

        public static void DisableFloatingRig() =>
            References.LocalRig.head.trackingPositionOffset = offsetH;
        public static void Bees()
        {
            References.LocalRig.enabled = false;
            if (Time.time > beesDelay)
            {
                VRRig target = GetRandomVRRig();

                References.LocalRig.transform.position = target.transform.position + Vector3.up;

                References.LocalRig.leftHand.rigTarget.transform.position = target.transform.position;
                References.LocalRig.rightHand.rigTarget.transform.position = target.transform.position;

                beesDelay = Time.time + 0.777f;
            }
        }
        public static void SizeChanger()
        {
            float increment = 0.05f;
            if (!Manager.GetIndex("Disable Size Changer Buttons").enabled)
            {
                if (leftTrigger > 0.5f)
                    increment = 0.2f;

                if (leftGrab)
                    increment = 0.01f;

                if (rightTrigger > 0.5f)
                    sizeScale += increment;

                if (rightGrab)
                    sizeScale -= increment;

                if (rightPrimary)
                    sizeScale = 1f;
            }

            if (sizeScale < 0.05f)
                sizeScale = 0.05f;

            References.LocalRig.transform.localScale = Vector3.one * sizeScale;
            References.LocalRig.NativeScale = sizeScale;
            References.Player.nativeScale = sizeScale;
        }
        public static void DisableSizeChanger()
        {
            sizeScale = 1f;

            References.LocalRig.transform.localScale = Vector3.one * sizeScale;
            References.LocalRig.NativeScale = sizeScale;
            References.Player.nativeScale = sizeScale;
        }

        public static void SilentRotate(bool enable, Quaternion rot)
        {
            if (enable)
            {
                ViewSerialization.OverrideSerialization ??= () =>
                {
                    if (!Networking.InRoom)
                        return true;

                    Quaternion archive = References.LocalRig.transform.rotation;
                    References.LocalRig.transform.rotation = rot;
                    SendSerialize(References.LocalRig.GetPhotonView());

                    References.LocalRig.transform.rotation = archive;
                    return false;
                };
            }
            else if (!enable)
                ViewSerialization.OverrideSerialization = null;
        }

        public static void Rotate(Quaternion rot) =>
            References.LocalRig.transform.rotation = rot;

        public static void VRRigLateUpdate_Dinnerbone() =>
            References.LocalRig.transform.RotateAround(References.LocalRig.bodyTransform.position, References.LocalRig.bodyTransform.forward, 180f);

        public static void VRRigLateUpdate_SpazBody() => Rotate(Random.rotation);

        public static void VRRigLateUpdate_FakeFBT()
        {
            Rotate(Camera.main.transform.rotation);
            References.LocalRig.head.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
            References.LocalRig.leftHand.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
            References.LocalRig.rightHand.MapMine(References.LocalRig.lastScaleFactor, References.LocalRig.playerOffsetTransform);
        }

        public static void VRRigLateUpdate_Control()
        {
            if (rightJoystickClick || leftJoystickClick || UnityInput.GetKeyDown(Key.Enter))
                vrrigJoystickRot = null;

            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;
            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            Vector2 keys = Vector2.zero;
            if (UnityInput.GetKey(Key.LeftArrow)) keys.x -= 1f;
            if (UnityInput.GetKey(Key.RightArrow)) keys.x += 1f;
            if (UnityInput.GetKey(Key.UpArrow)) keys.y += 1f;
            if (UnityInput.GetKey(Key.DownArrow)) keys.y -= 1f;

            if (keys.sqrMagnitude > 0f)
                keys.Normalize();

            if (keys.sqrMagnitude > stick.sqrMagnitude)
                stick = keys;

            if (stick.sqrMagnitude > 0.0025)
            {
                if (!vrrigJoystickRot.HasValue)
                    vrrigJoystickRot = References.LocalRig.transform.rotation;

                float yaw = stick.x * 200f * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;
                float pitch = -stick.y * 200f * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;

                Quaternion rot = vrrigJoystickRot.Value;
                rot = Quaternion.AngleAxis(yaw, Vector3.up) * rot;
                Vector3 localRight = rot * Vector3.right;
                rot = Quaternion.AngleAxis(pitch, localRight) * rot;
                rot.Normalize();

                vrrigJoystickRot = rot;
            }

            if (vrrigJoystickRot != null)
                References.LocalRig.transform.rotation = vrrigJoystickRot.Value;
        }

        public static void ControlBodyRotation()
        {
            if (rightJoystickClick || leftJoystickClick || UnityInput.GetKeyDown(Key.Enter))
                bodyJoystickRot = null;

            Vector2 r = rightJoystick;
            Vector2 l = leftJoystick;
            Vector2 stick = (l.sqrMagnitude > r.sqrMagnitude) ? l : r;

            Vector2 keys = Vector2.zero;
            if (UnityInput.GetKey(Key.LeftArrow)) keys.x -= 1f;
            if (UnityInput.GetKey(Key.RightArrow)) keys.x += 1f;
            if (UnityInput.GetKey(Key.UpArrow)) keys.y += 1f;
            if (UnityInput.GetKey(Key.DownArrow)) keys.y -= 1f;

            if (keys.sqrMagnitude > 0f)
                keys.Normalize();

            if (keys.sqrMagnitude > stick.sqrMagnitude)
                stick = keys;

            float yawSpeed = 200f;
            float pitchSpeed = 200f;

            if (stick.sqrMagnitude > 0.0025)
            {
                if (!bodyJoystickRot.HasValue)
                    bodyJoystickRot = References.LocalRig.transform.rotation;

                float yaw = stick.x * yawSpeed * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;
                float pitch = -stick.y * pitchSpeed * Manager.GetIndex("Joystick Rotation Speed").GetValue<int>() * Time.deltaTime;

                Quaternion rot = bodyJoystickRot.Value;
                rot = Quaternion.AngleAxis(yaw, Vector3.up) * rot;
                Vector3 localRight = rot * Vector3.right;
                rot = Quaternion.AngleAxis(pitch, localRight) * rot;
                rot.Normalize();

                bodyJoystickRot = rot;
            }

            if (bodyJoystickRot != null)
                References.LocalRig.transform.rotation = bodyJoystickRot.Value;
        }

        public static void Flip()
        {
            bool silentFlip = Manager.GetIndex("Silent Flip").enabled;
            if (!flipping && (rightPrimary || rightSecondary) && References.LocalRig.enabled)
            {
                if (References.Player.playerRigidBody)
                {
                    flipping = true;
                    flipStart = Time.time;
                    flipAxis = rightPrimary ? References.LocalRig.transform.right : -References.LocalRig.transform.right;
                    flipFrom = References.Player?.playerRigidBody?.rotation ?? Quaternion.identity;
                }
            }
            if (!flipping) return;

            float t = (Time.time - flipStart) / flipDuration;
            if (t >= 1f)
            {
                flipping = false;
                if (silentFlip)
                    References.LocalRig.transform.rotation = flipFrom;
                else
                    GTPlayerTransform.ApplyRotationOverride(flipFrom, Time.frameCount);

                return;
            }
            var rot = Quaternion.AngleAxis(-360f * t, flipAxis) * flipFrom;
            if (silentFlip)
                References.LocalRig.transform.rotation = Quaternion.Euler(0f, References.BodyCollider.transform.eulerAngles.y, 0f) * rot;
            else
                GTPlayerTransform.ApplyRotationOverride(rot, Time.frameCount);
        }

        public static void SpiderCrawl() =>
            References.HeadCollider.transform.rotation = Quaternion.Euler(-270, References.HeadCollider.transform.rotation.eulerAngles.y, 0);

        public static void RigPose(float pitch, float roll) =>
            References.HeadCollider.transform.rotation = Quaternion.Euler(pitch, References.HeadCollider.transform.rotation.eulerAngles.y, roll);

        public static void ButtHead() => RigPose(270, 0);
        public static void LayDown() => RigPose(90, 0);
        public static void FlatOnBack() => RigPose(270, 0);
        public static void Sideways() => RigPose(90, 90);
        public static void TiltLeft() => RigPose(0, 45);
        public static void TiltRight() => RigPose(0, -45);
        public static void LeanForward() => RigPose(45, 0);
        public static void LeanBack() => RigPose(-45, 0);

        public static void UpwardsBody() =>
             References.HeadCollider.transform.rotation = Quaternion.Euler(-180, References.HeadCollider.transform.rotation.eulerAngles.y, References.HeadCollider.transform.rotation.eulerAngles.z);
        public static void SlipSlap()
        {
            foreach (var surface in GetAllType<GorillaSurfaceOverride>())
            {
                float slidePercent = surface.slidePercentageOverride > 0f ? surface.slidePercentageOverride : References.Player.materialData[surface.overrideIndex].slidePercent;
                if (slidePercent > 0f)
                {
                    velocityArchive[surface] = surface.extraVelMultiplier;
                    surface.extraVelMultiplier += slidePercent;
                }
            }
        }

        public static void DisableSlipSlap()
        {
            foreach (var vel in velocityArchive)
                vel.Key.extraVelMultiplier = vel.Value;

            velocityArchive.Clear();
        }
        public static void StickyHands()
        {
            if (stickpart == null)
            {
                stickpart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                FixStickyColliders(stickpart);
                stickpart.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                stickpart.GetComponent<Renderer>().enabled = false;
            }
            if (References.Player.IsHandTouching(true))
                stickpart.transform.position = Controllers.GetTrueLeftHand().position;

            if (References.Player.IsHandTouching(false))
                stickpart.transform.position = Controllers.GetTrueRightHand().position;

            if (References.Player.IsHandTouching(true) && References.Player.IsHandTouching(false))
                stickpart.transform.position = Vector3.zero;
        }

        public static void DisableStickyHands()
        {
            if (stickpart != null)
            {
                Object.Destroy(stickpart);
                stickpart = null;
            }
        }
        public static void ClimbyHands()
        {

            if (climb == null)
            {
                climb = new GameObject("GR");
                climb.AddComponent<GorillaClimbable>();
            }
            if (leftGrab)
            {
                if (References.Player.IsHandTouching(true) && !leftisclimbing)
                {
                    climb.transform.position = References.LeftHand.position;
                    leftisclimbing = true;
                    References.Player.BeginClimbing(climb.AddComponent<GorillaClimbable>(), GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller/GorillaHandClimber").GetComponent<GorillaHandClimber>());
                }
            }
            else
                leftisclimbing = false;

            if (rightGrab)
            {
                if (References.Player.IsHandTouching(false) && !rightisclimbing)
                {
                    climb.transform.position = References.RightHand.position;
                    rightisclimbing = true;
                    References.Player.BeginClimbing(climb.AddComponent<GorillaClimbable>(), GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller/GorillaHandClimber").GetComponent<GorillaHandClimber>());
                }
            }
            else
                rightisclimbing = false;
        }

        public static void DisableClimbyHands()
        {
            if (climb != null)
            {
                Object.Destroy(climb);
                climb = null;
            }
        }

        public static void SetHandEnabled(bool value)
        {
            References.Player.leftHand.isHolding = !value;
            References.Player.rightHand.isHolding = !value;
        }
        public static void EnableSlideControl()
        {
            oldSlide = References.Player.slideControl;
            References.Player.slideControl = 1f;
        }

        public static void EnableWeakSlideControl()
        {
            oldSlide = References.Player.slideControl;
            References.Player.slideControl = oldSlide * 2f;
        }

        public static void DisableSlideControl() =>
            References.Player.slideControl = oldSlide;

        public static void PunchMod()
        {
            int index = -1;
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
            {
                index++;
                Vector3 localBodyPos = References.LocalRig.bodyTransform.position;

                HandlePunch(vrrig.rightHandTransform, ref lastRight[index], localBodyPos);
                HandlePunch(vrrig.leftHandTransform, ref lastLeft[index], localBodyPos);

                static void HandlePunch(Transform handTransform, ref Vector3 lastPos, Vector3 localBodyPos)
                {
                    Vector3 handPos = handTransform.position;
                    float distance = Vector3.Distance(handPos, localBodyPos);

                    if (distance < 0.25f)
                    {
                        if (Manager.GetIndex("Graphic Punch Mod").enabled)
                        {
                            Projectiles.SendProjectile(
                                ThrowableProjectiles.FindProjectile("Apple"),
                                References.LocalRig.headMesh.transform.position,
                                Vector3.down * 600f,
                                new Color32(100, 0, 0, 255));
                        }

                        References.Rigidbody.linearVelocity +=
                            Vector3.Normalize(handPos - lastPos) * 10f;
                    }

                    lastPos = handPos;
                }
            }
        }
        public static void Telekinesis()
        {
            if (sithlord == null)
            {
                foreach (VRRig vrrig in Players.ActiveRigs)
                {
                    try
                    {
                        if (!vrrig.isLocal)
                        {
                            if (vrrig.rightIndex.calcT < 0.5f && vrrig.rightMiddle.calcT > 0.5f)
                            {
                                Vector3 dir = vrrig.transform.Find("rig/hand.R").up;
                                Physics.SphereCast(vrrig.rightHandTransform.position + dir * 0.1f, 0.3f, dir, out var Ray, 512f, NoInvisLayerMask());
                                {
                                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                                    if (gunTarget && gunTarget.isLocal)
                                    {
                                        sithlord = vrrig;
                                        sithright = true;
                                        sithdist = Ray.distance;
                                    }
                                }
                            }
                            if (vrrig.leftIndex.calcT < 0.5f && vrrig.leftMiddle.calcT > 0.5f)
                            {
                                Vector3 dir = vrrig.transform.Find("rig/hand.L").up;
                                Physics.SphereCast(vrrig.leftHandTransform.position + dir * 0.1f, 0.3f, dir, out var Ray, 512f, NoInvisLayerMask());
                                {
                                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                                    if (gunTarget && gunTarget.isLocal)
                                    {
                                        sithlord = vrrig;
                                        sithright = false;
                                        sithdist = Ray.distance;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            else
            {
                if (sithright ? sithlord.rightIndex.calcT < 0.5f && sithlord.rightMiddle.calcT > 0.5f : sithlord.leftMiddle.calcT < 0.5f && sithlord.leftMiddle.calcT > 0.5f)
                {
                    Transform hand = sithright ? sithlord.rightHandTransform : sithlord.leftHandTransform;
                    Vector3 dir = sithright ? sithlord.transform.Find("rig/hand.R").up : sithlord.transform.Find("rig/hand.L").up;
                    TeleportPlayer(Vector3.Lerp(References.BodyCollider.transform.position, hand.position + dir * sithdist, 0.1f));
                    References.Rigidbody.linearVelocity = Vector3.zero;
                    ZeroGravity();
                }
                else
                    sithlord = null;
            }
        }

        public static void SafetyBubble()
        {
            foreach (VRRig rig in
                Players.ActiveRigs
                    .Where(rig => rig != null && !rig.isLocal)
                    .OrderBy(rig => Vector3.Distance(rig.transform.position, References.BodyCollider.transform.position)))
            {
                if (Vector3.Distance(rig.transform.position, References.BodyCollider.transform.position) < 2f)
                {
                    Vector3 direction = References.BodyCollider.transform.position - rig.transform.position;
                    direction = new Vector3(direction.x, 0f, direction.z).normalized;

                    TeleportPlayer(References.BodyCollider.transform.position + direction * 2f);
                }
            }
        }
        public static void SolidPlayers()
        {
            List<VRRig> toRemove = new List<VRRig>();
            foreach (VRRig rig in RigColliders.Keys)
            {
                if (!Players.ActiveRigs.Contains(rig))
                    toRemove.Add(rig);
            }

            foreach (GameObject gameObject in toRemove.SelectMany(removeRig => RigColliders[removeRig]))
                Object.Destroy(gameObject);

            foreach (VRRig rig in toRemove)
                RigColliders.Remove(rig);

            toRemove.Clear();

            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
            {
                if (!RigColliders.TryGetValue(vrrig, out List<GameObject> colliders))
                {
                    colliders = new List<GameObject>();

                    GameObject bodyCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    colliders.Add(bodyCollider);
                    var bodyRenderer = bodyCollider.GetComponent<Renderer>().enabled = false;
                    bodyCollider.transform.localScale = new Vector3(0.3f, 0.55f, 0.3f);

                    for (int i = 0; i < 19; i++)
                    {
                        GameObject boneCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        colliders.Add(boneCollider);
                        var boneRenderer = boneCollider.GetComponent<Renderer>().enabled = false;
                        boneCollider.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    }

                    RigColliders[vrrig] = colliders;
                }

                colliders[0].transform.position = vrrig.head.rigTarget.transform.position + new Vector3(0f, -0.12f, 0f);
                colliders[0].transform.rotation = vrrig.transform.rotation;

                for (int i = 0; i < 19; i++)
                {
                    GameObject boneCollider = colliders[i + 1];
                    Vector3 pointA = vrrig.mainSkin.bones[Visuals.bones[i * 2]].position;
                    Vector3 pointB = vrrig.mainSkin.bones[Visuals.bones[i * 2 + 1]].position;

                    boneCollider.transform.position = Vector3.Lerp(pointA, pointB, 0.5f);
                    boneCollider.transform.LookAt(pointB);
                    boneCollider.transform.localScale = new Vector3(0.2f, 0.2f, Vector3.Distance(pointA, pointB));
                }
            }
        }

        public static void DisableSolidPlayers()
        {
            foreach (var gameObject in RigColliders.Values.SelectMany(gameObjects => gameObjects))
                Object.Destroy(gameObject);

            RigColliders.Clear();
        }
        public static void ApplyPullModPower(int index) => pullPower = PullModPowers[index];
        public static void ProcessPullHand(bool left)
        {
            if ((left ? !leftGrab : !rightGrab))
                return;

            bool touchingGround = References.Player.IsHandTouching(left);
            previousTouchingGround.TryGetValue(left, out bool wasTouchingGround);

            if (!touchingGround && wasTouchingGround)
            {
                Vector3 normal = References.Player.lastHitInfoHand.normal;
                Vector3 direction = References.Rigidbody.linearVelocity.X_Z();
                References.Player.transform.position += (direction - normal * Vector3.Dot(direction, normal)).normalized * (direction.magnitude / References.Player.maxJumpSpeed * (pullPower * 5f)) * (scaleWithPlayer ? References.Player.scale : 1f);
            }

            previousTouchingGround[left] = touchingGround;
        }

        public static void PullMod()
        {
            ProcessPullHand(false);
            ProcessPullHand(true);
        }
        public static void ThrowControllers()
        {
            if (leftPrimary)
            {
                if (leftThrow != null)
                {
                    References.Player.GetControllerTransform(true).position = leftThrow.transform.position;
                    References.Player.GetControllerTransform(true).rotation = leftThrow.transform.rotation;
                }
                else
                {
                    leftThrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leftThrow.GetComponent<Renderer>().enabled = false;
                    Object.Destroy(leftThrow.GetComponent<BoxCollider>());

                    leftThrow.transform.position = References.Player.GetControllerTransform(true).position;
                    leftThrow.transform.rotation = References.Player.GetControllerTransform(true).rotation;
                    Rigidbody comp = leftThrow.AddComponent(typeof(Rigidbody)) as Rigidbody;
                    comp.linearVelocity = References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0);
                    try
                    {
                        if (GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller").GetComponent<GorillaVelocityEstimator>() == null)
                            GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller").AddComponent<GorillaVelocityEstimator>();

                        comp.angularVelocity = GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/LeftHand Controller").GetComponent<GorillaVelocityEstimator>().angularVelocity;
                    }
                    catch { }
                }
            }
            else
            {
                if (leftThrow != null)
                {
                    Object.Destroy(leftThrow);
                    leftThrow = null;
                }
            }

            if (rightPrimary)
            {
                if (rightThrow != null)
                {
                    References.Player.GetControllerTransform(false).position = rightThrow.transform.position;
                    References.Player.GetControllerTransform(false).rotation = rightThrow.transform.rotation;
                }
                else
                {
                    rightThrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rightThrow.GetComponent<Renderer>().enabled = false;
                    Object.Destroy(rightThrow.GetComponent<BoxCollider>());

                    rightThrow.transform.position = References.Player.GetControllerTransform(false).position;
                    rightThrow.transform.rotation = References.Player.GetControllerTransform(false).rotation;
                    Rigidbody comp = rightThrow.AddComponent(typeof(Rigidbody)) as Rigidbody;
                    comp.linearVelocity = References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0);
                    try
                    {
                        if (GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller").GetComponent<GorillaVelocityEstimator>() == null)
                            GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller").AddComponent<GorillaVelocityEstimator>();

                        comp.angularVelocity = GetObject("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/RightHand Controller").GetComponent<GorillaVelocityEstimator>().angularVelocity;
                    }
                    catch { }
                }
            }
            else
            {
                if (rightThrow != null)
                {
                    Object.Destroy(rightThrow);
                    rightThrow = null;
                }
            }
        }
        public static void EnableControllerFlick()
        {
            flickLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(flickLeft.GetComponent<BoxCollider>());
            flickLeft.GetComponent<Renderer>().enabled = false;
            flickLeft.AddComponent<GorillaVelocityTracker>();
            Rigidbody leftRigid = flickLeft.AddComponent<Rigidbody>();
            leftRigid.isKinematic = true;
            leftRigid.useGravity = false;

            flickRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(flickRight.GetComponent<BoxCollider>());
            flickRight.GetComponent<Renderer>().enabled = false;
            flickRight.AddComponent<GorillaVelocityTracker>();
            Rigidbody rightRigid = flickRight.AddComponent<Rigidbody>();
            rightRigid.isKinematic = true;
            rightRigid.useGravity = false;
        }

        public static void DisableControllerFlick()
        {
            Object.Destroy(flickLeft);
            Object.Destroy(flickRight);
        }
        public static void ControllerFlick()
        {
            if (leftPrimary)
            {
                if (initialRotationLeft == Quaternion.identity)
                    initialRotationLeft = References.LeftHand.rotation;

                Rigidbody rigidbody = flickLeft.GetComponent<Rigidbody>();
                if (rigidbody.isKinematic)
                {
                    rigidbody.isKinematic = false;
                    rigidbody.linearVelocity = rigidbody.GetComponent<GorillaVelocityTracker>().GetAverageVelocity(true, 0);
                }

                References.LeftHand.position = References.HeadCollider.transform.position - flickLeft.transform.position;
                References.LeftHand.rotation = initialRotationLeft;
            }
            else
            {
                Rigidbody rigidbody = flickLeft.GetComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                flickLeft.transform.position = References.HeadCollider.transform.position - References.LeftHand.position;
                initialRotationLeft = Quaternion.identity;
            }

            if (rightPrimary)
            {
                if (initialRotationRight == Quaternion.identity)
                    initialRotationRight = References.RightHand.rotation;

                Rigidbody rigidbody = flickRight.GetComponent<Rigidbody>();
                if (rigidbody.isKinematic)
                {
                    rigidbody.isKinematic = false;
                    rigidbody.linearVelocity = rigidbody.GetComponent<GorillaVelocityTracker>().GetAverageVelocity(true, 0);
                }

                References.RightHand.position = References.HeadCollider.transform.position - flickRight.transform.position;
                References.RightHand.rotation = initialRotationRight;
            }
            else
            {
                Rigidbody rigidbody = flickRight.GetComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                flickRight.transform.position = References.HeadCollider.transform.position - References.RightHand.position;
                initialRotationRight = Quaternion.identity;
            }
        }

        public static void StickLongArms()
        {
            References.Player.GetControllerTransform(true).transform.position = References.LeftHand.position + References.LeftHand.forward * ((armlength - 0.917f) * References.Player.scale);
            References.Player.GetControllerTransform(false).transform.position = References.RightHand.position + References.RightHand.forward * ((armlength - 0.917f) * References.Player.scale);
        }
        public static void EnableSteamLongArms()
        {
            if (passWorldScaleCheck)
            {
                Vector3 headPosition = ControllerInputPoller.DevicePosition(XRNode.Head);
                Vector3 leftHandPosition = ControllerInputPoller.DevicePosition(XRNode.LeftHand);
                Vector3 rightHandPosition = ControllerInputPoller.DevicePosition(XRNode.RightHand);

                bool bothHandsNotMoving =
                    References.Player.RightHand.velocityTracker.GetAverageVelocity(true, 0).magnitude < 2f &&
                    References.Player.LeftHand.velocityTracker.GetAverageVelocity(true, 0).magnitude < 2f;

                bool handsNearHead =
                    headPosition.Distance(leftHandPosition) < 0.2f &&
                    headPosition.Distance(rightHandPosition) < 0.2f &&
                    bothHandsNotMoving;

                if (handsNearHead && !wasHandsNearHead)
                {
                    lastFramePosition = References.BodyCollider.transform.position;
                    DisableSteamLongArms();
                }

                if (!handsNearHead && wasHandsNearHead)
                {
                    if (lastFramePosition.HasValue)
                    {
                        TeleportPlayer(lastFramePosition.Value);
                        lastFramePosition = null;
                    }
                }

                wasHandsNearHead = handsNearHead;
            }

            References.Player.transform.localScale = Vector3.one * (References.LocalRig.NativeScale * armlength);
        }

        public static void DisableSteamLongArms() =>
            References.Player.transform.localScale = Vector3.one * References.LocalRig.NativeScale;
        public static void Extenders()
        {
            extendingTime += rightJoystickClick ? 0 - Time.unscaledDeltaTime : Time.unscaledDeltaTime;
            if (extendingTime > 1) extendingTime = 1;
            if (extendingTime < 0) extendingTime = 0;

            float delayedLength = (armlength - 1f) * extendingTime + 1f;
            References.Player.transform.localScale = new Vector3(delayedLength, delayedLength, delayedLength);
        }

        public static void MultipliedLongArms()
        {
            References.Player.GetControllerTransform(true).transform.position = References.HeadCollider.transform.position - (References.HeadCollider.transform.position - References.LeftHand.position) * armlength;
            References.Player.GetControllerTransform(false).transform.position = References.HeadCollider.transform.position - (References.HeadCollider.transform.position - References.RightHand.position) * armlength;
        }

        public static void VerticalLongArms()
        {
            Vector3 lefty = References.HeadCollider.transform.position - References.LeftHand.position;
            lefty.y *= armlength;
            Vector3 righty = References.HeadCollider.transform.position - References.RightHand.position;
            righty.y *= armlength;
            References.Player.GetControllerTransform(true).transform.position = References.HeadCollider.transform.position - lefty;
            References.Player.GetControllerTransform(false).transform.position = References.HeadCollider.transform.position - righty;
        }

        public static void HorizontalLongArms()
        {
            Vector3 lefty = References.HeadCollider.transform.position - References.LeftHand.position;
            lefty.x *= armlength;
            lefty.z *= armlength;
            Vector3 righty = References.HeadCollider.transform.position - References.RightHand.position;
            righty.x *= armlength;
            righty.z *= armlength;
            References.Player.GetControllerTransform(true).position = References.HeadCollider.transform.position - lefty;
            References.Player.GetControllerTransform(false).position = References.HeadCollider.transform.position - righty;
        }
        public static void CreateVelocityTrackers()
        {
            lvT = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(lvT.GetComponent<BoxCollider>());
            lvT.GetComponent<Renderer>().enabled = false;
            lvT.AddComponent<GorillaVelocityTracker>();

            rvT = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(rvT.GetComponent<BoxCollider>());
            rvT.GetComponent<Renderer>().enabled = false;
            rvT.AddComponent<GorillaVelocityTracker>();
        }

        public static void DestroyVelocityTrackers()
        {
            Object.Destroy(lvT);
            Object.Destroy(rvT);
        }
        public static void ApplyPredictionAmount(int index) => predCount = PredictionAmounts[index];
        public static void VelocityLongArms()
        {
            lvT.transform.position = References.HeadCollider.transform.position - References.LeftHand.position;
            rvT.transform.position = References.HeadCollider.transform.position - References.RightHand.position;
            References.Player.GetControllerTransform(true).transform.position -= lvT.GetComponent<GorillaVelocityTracker>().GetAverageVelocity(true, 0) * predCount;
            References.Player.GetControllerTransform(false).transform.position -= rvT.GetComponent<GorillaVelocityTracker>().GetAverageVelocity(true, 0) * predCount;
        }

        public static void ApplyFakeLagStrength(int index) => fakeLagDelay = index / 10f;
        public static void ApplyTimerSpeed(int index) => timerPower = index / 10f;
        public static void Timer()
        {
            References.Player.debugMovement = true;
            Time.timeScale = timerPower;
        }

        public static void FlickJump()
        {
            if (rightPrimary)
                References.Player.GetControllerTransform(false).transform.position = References.RightHand.position + new Vector3(0f, -1.5f, 0f);
        }
        public static void PlayspaceAbuse()
        {
            if (rightPrimary)
            {
                keepVelocityUntil ??= Time.time + 0.5f;
                velocity ??= References.Rigidbody.linearVelocity;

                if (Time.time < keepVelocityUntil)
                    References.Rigidbody.linearVelocity = velocity.Value;

                longJumpPower ??= (References.Rigidbody.linearVelocity * playspaceAbusePower).X_Z();
                References.Player.transform.position += longJumpPower.Value;
            }
            else
            {
                longJumpPower = null;
                velocity = null;
            }
        }

        public static void BunnyHop()
        {
            Physics.Raycast(References.BodyCollider.transform.position - new Vector3(0f, 0.2f, 0f), Vector3.down, out var Ray, 512f, References.Player.locomotionEnabledLayers);

            if (Ray.distance < 0.15f)
                References.Rigidbody.linearVelocity = new Vector3(References.Rigidbody.linearVelocity.x, References.Player.jumpMultiplier * 2.727272727f, References.Rigidbody.linearVelocity.z);
        }

        public static void Strafe()
        {
            Vector3 direction = Manager.GetIndex("Hand Oriented Strafe").enabled
                ? new Vector3(-References.RightHand.up.x, 0f, -References.RightHand.up.z).normalized
                : References.BodyCollider.transform.forward;

            float power = References.Player.maxJumpSpeed;
            Vector3 velocity = direction * power;

            References.Rigidbody.linearVelocity = new Vector3(velocity.x, References.Rigidbody.linearVelocity.y, velocity.z);
        }

        public static void DynamicStrafe()
        {
            Vector3 direction = Manager.GetIndex("Hand Oriented Strafe").enabled
                ? new Vector3(-References.RightHand.up.x, 0f, -References.RightHand.up.z).normalized
                : References.BodyCollider.transform.forward;

            float power = new Vector3(References.Rigidbody.linearVelocity.x, 0, References.Rigidbody.linearVelocity.z).magnitude;
            Vector3 velocity = direction * power;

            References.Rigidbody.linearVelocity = new Vector3(velocity.x, References.Rigidbody.linearVelocity.y, velocity.z);
        }

        public static void GroundHelper()
        {
            if (rightGrab)
            {
                Vector3 x3 = References.Rigidbody.linearVelocity;
                if (x3.y > 0f)
                    References.Rigidbody.linearVelocity = new Vector3(x3.x, 0f, x3.z);
            }
        }

        public static void LowFPSMovement()
        {
            References.Player.velocityHistorySize = 12;
            References.Player.InitializeValues();
        }

        public static void PreBouncy()
        {
            preBounciness = References.BodyCollider.material.bounciness;
            whateverthisis = References.BodyCollider.material.bounceCombine;
            preFrictiness = References.BodyCollider.material.dynamicFriction;
        }

        public static void Bouncy()
        {
            References.BodyCollider.material.bounciness = 1f;
            References.BodyCollider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
            References.BodyCollider.material.dynamicFriction = 0f;
        }

        public static void PostBouncy()
        {
            References.BodyCollider.material.bounciness = preBounciness;
            References.BodyCollider.material.bounceCombine = whateverthisis;
            References.BodyCollider.material.dynamicFriction = preFrictiness;
        }

        public static void DisableWater()
        {
            foreach (WaterVolume waterVolume in GetAllType<WaterVolume>())
            {
                GameObject v = waterVolume.gameObject;
                v.layer = LayerMask.NameToLayer("TransparentFX");
            }
        }

        public static void SolidWater()
        {
            foreach (WaterVolume waterVolume in GetAllType<WaterVolume>())
            {
                GameObject v = waterVolume.gameObject;
                v.layer = LayerMask.NameToLayer("Default");
            }
        }

        public static void FixWater()
        {
            foreach (WaterVolume waterVolume in GetAllType<WaterVolume>())
            {
                GameObject v = waterVolume.gameObject;
                v.layer = LayerMask.NameToLayer("Water");
            }
        }
        public static void AirSwim()
        {
            if (airSwimPart == null)
            {
                airSwimPart = Object.Instantiate(GetObject("Environment Objects/LocalObjects_Prefab/ForestToBeach/ForestToBeach_Prefab_V4/ForestToBeach_Geo/CaveWaterVolume"));
                airSwimPart.transform.localScale = new Vector3(5f, 5f, 5f);
                airSwimPart.GetComponent<Renderer>().enabled = false;
            }
            else
            {
                References.Player.audioManager.UnsetMixerSnapshot();
                airSwimPart.transform.position = References.HeadCollider.transform.position + new Vector3(0f, 2.5f, 0f);
            }
        }

        public static void DisableAirSwim()
        {
            if (airSwimPart != null)
                Object.Destroy(airSwimPart);
        }

        public static void SetSwimSpeed(float speed = 3f) =>
            References.Player.swimmingParamsList.ForEach(s => s.swimmingVelocityOutOfWaterDrainRate = speed);
        public static void LiquidRunHelper(bool enable)
        {
            if (enable)
            {
                waterSurfaceJumpAmount = References.Player.swimmingParamsList.Select(s => s.waterSurfaceJumpAmount).ToList();
                waterSurfaceJumpMaxSpeed = References.Player.swimmingParamsList.Select(s => s.waterSurfaceJumpMaxSpeed).ToList();

                References.Player.swimmingParamsList.ForEach(s =>
                {
                    s.waterSurfaceJumpAmount = 1.25f;
                    s.waterSurfaceJumpMaxSpeed = 4.333f;
                });
            }
            else
            {
                for (int i = 0; i < References.Player.swimmingParamsList.Count; i++)
                {
                    References.Player.swimmingParamsList[i].waterSurfaceJumpAmount = waterSurfaceJumpAmount[i];
                    References.Player.swimmingParamsList[i].waterSurfaceJumpMaxSpeed = waterSurfaceJumpMaxSpeed[i];
                }
            }
        }

        public static void PiggybackGun()
        {
            GunLibrary.Target(lockTarget =>
            {
                TeleportPlayer(lockTarget.transform.position + new Vector3(0f, 0.5f, 0f));
                References.Rigidbody.linearVelocity = Vector3.zero;
            }, new GunLibrary.GunSettings { locks = true });
        }

        public static void PiggybackAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    References.LocalRig.transform.position = GetVRRigFromPlayer(Player).headMesh.transform.position;
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;

                return false;
            };
        }

        public static void CopyMovementGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    CopyMovementPlayer(lockTarget.GetPlayer());

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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void CopyMovementPlayer(NetPlayer player, bool fingers = true)
        {
            VRRig targetRig = GetVRRigFromPlayer(player);
            References.LocalRig.enabled = false;

            References.LocalRig.transform.position = targetRig.syncPos;
            References.LocalRig.transform.rotation = targetRig.syncRotation;

            References.LocalRig.leftHand.rigTarget.transform.position = targetRig.leftHand.rigTarget.transform.position;
            References.LocalRig.rightHand.rigTarget.transform.position = targetRig.rightHand.rigTarget.transform.position;

            References.LocalRig.leftHand.rigTarget.transform.rotation = targetRig.leftHand.rigTarget.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = targetRig.rightHand.rigTarget.transform.rotation;

            if (fingers)
            {
                References.LocalRig.leftIndex.calcT = targetRig.leftIndex.calcT;
                References.LocalRig.leftMiddle.calcT = targetRig.leftMiddle.calcT;
                References.LocalRig.leftThumb.calcT = targetRig.leftThumb.calcT;

                References.LocalRig.leftIndex.LerpFinger(1f, false);
                References.LocalRig.leftMiddle.LerpFinger(1f, false);
                References.LocalRig.leftThumb.LerpFinger(1f, false);

                References.LocalRig.rightIndex.calcT = targetRig.rightIndex.calcT;
                References.LocalRig.rightMiddle.calcT = targetRig.rightMiddle.calcT;
                References.LocalRig.rightThumb.calcT = targetRig.rightThumb.calcT;

                References.LocalRig.rightIndex.LerpFinger(1f, false);
                References.LocalRig.rightMiddle.LerpFinger(1f, false);
                References.LocalRig.rightThumb.LerpFinger(1f, false);
            }

            References.LocalRig.head.rigTarget.transform.rotation = targetRig.head.rigTarget.transform.rotation;
        }

        public static void CopyMovementAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    CopyMovementPlayer(Player, false);
                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.enabled = true;

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void FollowPlayer(NetPlayer player, bool fingers = true)
        {
            VRRig targetRig = GetVRRigFromPlayer(player);
            References.LocalRig.enabled = false;

            Vector3 look = targetRig.transform.position - References.LocalRig.transform.position;
            look.Normalize();

            Vector3 position = References.LocalRig.transform.position + look * (FlySpeed / 2f * Time.deltaTime);

            References.LocalRig.transform.position = position;
            References.LocalRig.transform.LookAt(targetRig.transform.position);

            References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
            References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
            References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

            References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
            References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

            FixRigHandRotation();

            if (fingers)
            {
                References.LocalRig.leftIndex.calcT = 0f;
                References.LocalRig.leftMiddle.calcT = 0f;
                References.LocalRig.leftThumb.calcT = 0f;

                References.LocalRig.leftIndex.LerpFinger(1f, false);
                References.LocalRig.leftMiddle.LerpFinger(1f, false);
                References.LocalRig.leftThumb.LerpFinger(1f, false);

                References.LocalRig.rightIndex.calcT = 0f;
                References.LocalRig.rightMiddle.calcT = 0f;
                References.LocalRig.rightThumb.calcT = 0f;

                References.LocalRig.rightIndex.LerpFinger(1f, false);
                References.LocalRig.rightMiddle.LerpFinger(1f, false);
                References.LocalRig.rightThumb.LerpFinger(1f, false);
            }
        }

        public static void FollowPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                    FollowPlayer(lockTarget.GetPlayer());

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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }
        public static void FollowAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    Vector3 position = followPositions.GetValueOrDefault(targetRig, archivePos);

                    Vector3 look = targetRig.transform.position - position;
                    look.Normalize();

                    position += look * (FlySpeed / 2f * Time.deltaTime);

                    followPositions.Remove(targetRig);

                    followPositions.Add(targetRig, position);

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void OrbitPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = lockTarget.transform.position + new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f));
                    References.LocalRig.transform.LookAt(lockTarget.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    FixRigHandRotation();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void OrbitAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.transform.position + new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f));
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.leftHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * -1f;
                    References.LocalRig.rightHand.rigTarget.transform.position = References.LocalRig.transform.position + References.LocalRig.transform.right * 1f;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;
                    References.LocalRig.rightHand.rigTarget.transform.rotation = References.LocalRig.transform.rotation;

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void JumpscareGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    References.LocalRig.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.forward * Random.Range(0.1f, 0.5f);
                    References.LocalRig.head.rigTarget.transform.LookAt(lockTarget.headMesh.transform.position);
                    Quaternion dirLook = References.LocalRig.head.rigTarget.transform.rotation;

                    References.LocalRig.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.right * 0.2f;
                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.headMesh.transform.position + lockTarget.headMesh.transform.right * -0.2f;

                    References.LocalRig.head.rigTarget.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                    FixRigHandRotation();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void JumpscareAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    References.LocalRig.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.forward * Random.Range(0.1f, 0.5f);
                    References.LocalRig.head.rigTarget.transform.LookAt(targetRig.headMesh.transform.position);
                    Quaternion dirLook = References.LocalRig.head.rigTarget.transform.rotation;

                    References.LocalRig.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.right * 0.2f;
                    References.LocalRig.rightHand.rigTarget.transform.position = targetRig.headMesh.transform.position + targetRig.headMesh.transform.right * -0.2f;

                    References.LocalRig.head.rigTarget.transform.rotation = dirLook;

                    References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void AnnoyPlayerGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    Vector3 position = lockTarget.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(lockTarget.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    Sound.SoundSpam(337, true);
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void AnnoyAllPlayers()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    Vector3 position = targetRig.transform.position + RandomVector3();

                    References.LocalRig.transform.position = position;
                    References.LocalRig.transform.LookAt(targetRig.transform.position);

                    References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + RandomVector3();
                    References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + RandomVector3();

                    References.LocalRig.leftHand.rigTarget.transform.rotation = RandomQuaternion();
                    References.LocalRig.rightHand.rigTarget.transform.rotation = RandomQuaternion();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }
        public static void ApplyTinnitusHz(int index) => targetHz = 4000 + index * 500;
        public static AudioClip CreateTinnitusSound(int targetHz, float seconds = 180f)
        {
            var vm = Voices.Get();
            int sampleRate = vm.OutputRate;

            int hz = Mathf.Clamp(targetHz, 1, (sampleRate / 2) - 1);
            int samples = Mathf.CeilToInt(sampleRate * seconds);

            var clip = AudioClip.Create("tinnitus", samples, 1, sampleRate, false);
            var data = new float[samples];

            double inc = (double)hz / sampleRate;

            for (int i = 0; i < samples; i++)
                data[i] = (float)Math.Sin(2.0 * Math.PI * hz * i / sampleRate);

            clip.SetData(data, 0);
            return clip;
        }

        public static void IntercourseGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * -(0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = lockTarget.transform.rotation;
                        References.LocalRig.rightHand.rigTarget.transform.rotation = lockTarget.transform.rotation;
                    }
                    else
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = lockTarget.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = lockTarget.transform.rotation;
                    }

                    FixRigHandRotation();
                    IntercourseNoises();

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    References.LocalRig.head.rigTarget.transform.rotation = lockTarget.transform.rotation;
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void IntercourseNoises()
        {
            if (Time.frameCount % 45 == 0)
            {
                if (Networking.InRoom)
                {
                    References.MyRigView.SendRPC("RPC_PlayHandTap", RpcTarget.All, 64, true, 999999f);

                    if (Manager.GetIndex("Splash Intercourse").enabled)
                        Fun.WaterSplash(References.LocalRig.transform.position, References.LocalRig.transform.rotation);

                    RPCProtection();
                }
                else
                    References.LocalRig.PlayHandTapLocal(64, true, 999999f);
            }
        }

        public static void IntercourseAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * -(0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = targetRig.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = targetRig.transform.rotation;
                        References.LocalRig.rightHand.rigTarget.transform.rotation = targetRig.transform.rotation;
                    }
                    else
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f);
                        References.LocalRig.transform.rotation = targetRig.transform.rotation;

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = targetRig.transform.rotation;
                    }

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void PromptForSex()
        {
            Prompt("You have to be age verified to use this mod. Would you like to proceed to the age verification process?", () =>
            {
                Notifications.SendFormat("red", "SEX", $"A browser tab has been opened on your computer.");
                PromptSingle("A browser tab has been opened on your computer. Please go and verify your age.", null, "Ok frick off buddy");
                Files.OpenURL("https://qubit.mom/age_verification");

                Coroutines.instance.StartCoroutine(Sex());
            });
        }

        public static IEnumerator Sex()
        {
            while (Application.isFocused)
                yield return null;
            float time = Time.time + 5f;
            while (!Application.isFocused && time > Time.time)
                yield return null;

            GameObject sex = LoadObject<GameObject>("sex");
            sex.layer = 8;
            Rigidbody rb = sex.GetComponent<Rigidbody>();

            rb.useGravity = false;

            sex.transform.position = References.LocalRig.headMesh.transform.position + References.LocalRig.headMesh.transform.forward * 1.5f;
            sex.transform.LookAt(References.LocalRig.headMesh.transform);

            AudioClip clip = null;
            bool loaded = false;

            LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Mods/Fun/sex.ogg", "Audio/Mods/Fun/sex.ogg", c =>
            {
                clip = c;
                loaded = true;
            });

            while (!loaded)
                yield return null;

            AudioSource ausrc = audioManager.GetComponent<AudioSource>();
            ausrc.volume = 1f;
            ausrc.PlayOneShot(clip);

            yield return new WaitForSeconds(clip.length);

            rb.useGravity = true;

            float grabRadius = 0.3f;

            Transform activeHand = null;
            Vector3 velocity = Vector3.zero;

            while (true)
            {
                Transform rightHand = References.Player.rightHand.controllerTransform;
                Transform leftHand = References.Player.leftHand.controllerTransform;

                bool rightHolding = activeHand == rightHand && rightGrab;
                bool leftHolding = activeHand == leftHand && leftGrab;

                if (activeHand != null)
                {
                    if (rightHolding || leftHolding)
                    {
                        velocity = activeHand.position;
                        sex.transform.SetPositionAndRotation(activeHand.position, activeHand.rotation);

                        if (rb != null)
                            rb.isKinematic = true;
                    }
                    else
                    {
                        if (rb != null)
                        {
                            rb.isKinematic = false;
                            rb.linearVelocity = (activeHand.position - velocity) / Time.deltaTime;
                        }
                        activeHand = null;
                    }
                }
                else
                {
                    if (rightGrab && rightHand != null && Vector3.Distance(sex.transform.position, rightHand.position) <= grabRadius)
                        activeHand = rightHand;
                    else if (leftGrab && leftHand != null && Vector3.Distance(sex.transform.position, leftHand.position) <= grabRadius)
                        activeHand = leftHand;
                }

                yield return null;
            }
        }

        public static void HeadGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    References.LocalRig.enabled = false;

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + lockTarget.transform.up * -0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }
                    else
                    {
                        References.LocalRig.transform.position = lockTarget.transform.position + lockTarget.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + lockTarget.transform.up * 0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * 0.2f + lockTarget.transform.up * 0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = lockTarget.transform.position + lockTarget.transform.right * -0.2f + lockTarget.transform.up * 0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(lockTarget.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }

                    References.LocalRig.leftIndex.calcT = 0f;
                    References.LocalRig.leftMiddle.calcT = 0f;
                    References.LocalRig.leftThumb.calcT = 0f;

                    References.LocalRig.leftIndex.LerpFinger(1f, false);
                    References.LocalRig.leftMiddle.LerpFinger(1f, false);
                    References.LocalRig.leftThumb.LerpFinger(1f, false);

                    References.LocalRig.rightIndex.calcT = 0f;
                    References.LocalRig.rightMiddle.calcT = 0f;
                    References.LocalRig.rightThumb.calcT = 0f;

                    References.LocalRig.rightIndex.LerpFinger(1f, false);
                    References.LocalRig.rightMiddle.LerpFinger(1f, false);
                    References.LocalRig.rightThumb.LerpFinger(1f, false);

                    FixRigHandRotation();
                    IntercourseNoises();
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
                    {
                        gunLocked = false;
                        References.LocalRig.enabled = true;
                    }
                }
            });
        }

        public static void HeadAll()
        {
            ViewSerialization.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { References.LocalRig.GetPhotonView() });

                Vector3 archivePos = References.LocalRig.transform.position;
                Quaternion archiveRot = References.LocalRig.transform.rotation;

                Vector3 archivePosLeft = References.LocalRig.leftHand.rigTarget.position;
                Quaternion archiveRotLeft = References.LocalRig.leftHand.rigTarget.rotation;

                Vector3 archivePosRight = References.LocalRig.rightHand.rigTarget.position;
                Quaternion archiveRotRight = References.LocalRig.rightHand.rigTarget.rotation;

                Quaternion archiveHeadRot = References.LocalRig.head.rigTarget.transform.rotation;

                foreach (NetPlayer Player in ManagerRegistry.NetworkSystem.PlayerListOthers)
                {
                    VRRig targetRig = GetVRRigFromPlayer(Player);

                    if (!Manager.GetIndex("Reverse Intercourse").enabled)
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + targetRig.transform.up * -0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * -0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * -0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }
                    else
                    {
                        References.LocalRig.transform.position = targetRig.transform.position + targetRig.transform.forward * (0.2f + Mathf.Sin(Time.frameCount / 8f) * 0.1f) + targetRig.transform.up * 0.4f;
                        References.LocalRig.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.leftHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * 0.2f + targetRig.transform.up * 0.4f;
                        References.LocalRig.rightHand.rigTarget.transform.position = targetRig.transform.position + targetRig.transform.right * -0.2f + targetRig.transform.up * 0.4f;

                        References.LocalRig.leftHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                        References.LocalRig.rightHand.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));

                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(targetRig.transform.rotation.eulerAngles + new Vector3(0f, 180f, 0f));
                    }

                    FixRigHandRotation();

                    SendSerialize(References.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = new[] { Player.ActorNumber } });
                }

                RPCProtection();

                References.LocalRig.transform.position = archivePos;
                References.LocalRig.transform.rotation = archiveRot;

                References.LocalRig.leftHand.rigTarget.position = archivePosLeft;
                References.LocalRig.leftHand.rigTarget.rotation = archiveRotLeft;

                References.LocalRig.rightHand.rigTarget.position = archivePosRight;
                References.LocalRig.rightHand.rigTarget.rotation = archiveRotRight;

                References.LocalRig.head.rigTarget.transform.rotation = archiveHeadRot;

                return false;
            };
        }

        public static void RemoveCopy()
        {
            gunLocked = false;
            lockTarget = null;
            References.LocalRig.enabled = true;
        }

        public static void SpazHead()
        {
            if (References.LocalRig.enabled)
            {
                References.LocalRig.head.trackingRotationOffset.x = Random.Range(0f, 360f);
                References.LocalRig.head.trackingRotationOffset.y = Random.Range(0f, 360f);
                References.LocalRig.head.trackingRotationOffset.z = Random.Range(0f, 360f);
            }
            else
                References.LocalRig.head.rigTarget.transform.rotation = RandomQuaternion();
        }

        public static void RandomSpazHead()
        {
            if (headspazType)
            {
                SpazHead();
                if (Time.time > headspazDelay)
                {
                    headspazType = false;
                    headspazDelay = Time.time + Random.Range(1000f, 4000f) / 1000f;
                }
            }
            else
            {
                Fun.FixHead();
                if (Time.time > headspazDelay)
                {
                    headspazType = true;
                    headspazDelay = Time.time + Random.Range(200f, 1000f) / 1000f;
                }
            }
        }
        public static void EnableSpazHead() =>
            headoffs = References.LocalRig.head.trackingPositionOffset;

        public static void SpazHeadPosition() =>
            References.LocalRig.head.trackingPositionOffset = headoffs + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));

        public static void FixHeadPosition() =>
            References.LocalRig.head.trackingPositionOffset = headoffs;

        public static void RandomSpazHeadPosition()
        {
            if (headspazType)
            {
                SpazHeadPosition();
                if (Time.time > headspazDelay)
                {
                    headspazType = false;
                    headspazDelay = Time.time + Random.Range(1000f, 4000f) / 1000f;
                }
            }
            else
            {
                FixHeadPosition();
                if (Time.time > headspazDelay)
                {
                    headspazType = true;
                    headspazDelay = Time.time + Random.Range(200f, 1000f) / 1000f;
                }
            }
        }
        public static void LaggyRig()
        {
            References.LocalRig.enabled = false;
            ghostException = true;
            if (Time.time > laggyRigDelay)
            {
                References.LocalRig.enabled = true;
                References.LocalRig.PostTick();
                References.LocalRig.enabled = false;

                laggyRigDelay = Time.time + 0.5f;
            }
        }
        public static void UpdateRig()
        {
            References.LocalRig.enabled = false;
            ghostException = true;
            if (rightPrimary && !wasRightPrimaryPressed)
            {
                References.LocalRig.enabled = true;
                References.LocalRig.PostTick();
                References.LocalRig.enabled = false;
            }

            wasRightPrimaryPressed = rightPrimary;
        }
        public static void ApplyMultiplicationAmount(int index) => multiplicationAmount = index;
        public static void SpinHead(string axis)
        {
            if (References.LocalRig.enabled)
            {
                switch (axis.ToLower())
                {
                    case "x":
                        References.LocalRig.head.trackingRotationOffset.x += Fun.headSpinSpeed;
                        break;
                    case "y":
                        References.LocalRig.head.trackingRotationOffset.y += Fun.headSpinSpeed;
                        break;
                    case "z":
                        References.LocalRig.head.trackingRotationOffset.z += Fun.headSpinSpeed;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (axis.ToLower())
                {
                    case "x":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(Fun.headSpinSpeed, 0f, 0f));
                        break;
                    case "y":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(0f, Fun.headSpinSpeed, 0f));
                        break;
                    case "z":
                        References.LocalRig.head.rigTarget.transform.rotation = Quaternion.Euler(References.LocalRig.head.rigTarget.transform.rotation.eulerAngles + new Vector3(0f, 0f, Fun.headSpinSpeed));
                        break;
                    default:
                        return;
                }
            }
        }

        [RunOnLaunch(LaunchPriority.Macros)]
        public static void Initialize()
        {
            try
            {
                LoadMacros();
            }
            catch (Exception exc)
            {
                Logs.LogError(
                $"Error with Movement.LoadMacros() at {exc.StackTrace}: {exc.Message}");
            }
        }
        #endregion
    }
}
