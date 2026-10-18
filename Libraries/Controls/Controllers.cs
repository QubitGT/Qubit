/*
 * Qubit Menu  Libraries/Controls/Controllers.cs
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
using GorillaLocomotion;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Valve.VR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Qubit.Libraries.Controls
{
    public static class Controllers
    {
        public static bool leftPrimary;
        public static bool leftSecondary;
        public static bool rightPrimary;
        public static bool rightSecondary;
        public static bool leftGrab;
        public static bool rightGrab;
        public static float leftTrigger;
        public static float rightTrigger;
        public static bool leftTriggerPressed;
        public static bool rightTriggerPressed;

        public static Vector2 leftJoystick = Vector2.zero;
        public static Vector2 rightJoystick = Vector2.zero;
        public static bool leftJoystickClick;
        public static bool rightJoystickClick;

        public static void Poll()
        {
            rightPrimary = References.InputPoller.rightControllerPrimaryButton || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.RightPrimaryButton]);
            rightSecondary = References.InputPoller.rightControllerSecondaryButton || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.RightSecondaryButton]);
            leftPrimary = References.InputPoller.leftControllerPrimaryButton || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.LeftPrimaryButton]);
            leftSecondary = References.InputPoller.leftControllerSecondaryButton || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.LeftSecondaryButton]);
            leftGrab = References.InputPoller.leftGrab || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.LeftGrip]);
            rightGrab = References.InputPoller.rightGrab || UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.RightGrip]);
            leftTrigger = ControllerInputPoller.TriggerFloat(XRNode.LeftHand);
            rightTrigger = ControllerInputPoller.TriggerFloat(XRNode.RightHand);
            leftTriggerPressed = leftTrigger >= 0.5f;
            rightTriggerPressed = rightTrigger >= 0.5f;

            if (UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.LeftTrigger]))
                leftTrigger = 1f;

            if (UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.RightTrigger]))
                rightTrigger = 1f;

            if (Frontend.Settings.IsSteam)
            {
                leftJoystick = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
                rightJoystick = SteamVR_Actions.gorillaTag_RightJoystick2DAxis.GetAxis(SteamVR_Input_Sources.RightHand);

                leftJoystickClick = SteamVR_Actions.gorillaTag_LeftJoystickClick.GetState(SteamVR_Input_Sources.LeftHand);
                rightJoystickClick = SteamVR_Actions.gorillaTag_RightJoystickClick.GetState(SteamVR_Input_Sources.RightHand);
            }
            else
            {
                References.InputPoller.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystick);
                References.InputPoller.rightControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightJoystick);

                References.InputPoller.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out leftJoystickClick);
                References.InputPoller.rightControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out rightJoystickClick);
            }

            bool arrowKeysPressed = UnityInput.GetKey(Key.UpArrow) || UnityInput.GetKey(Key.DownArrow) || UnityInput.GetKey(Key.LeftArrow) || UnityInput.GetKey(Key.RightArrow);
            bool leftOverride = UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.LeftOverride]);

            if (arrowKeysPressed)
            {
                Vector2 direction = new Vector2((UnityInput.GetKey(Key.RightArrow) ? 1f : 0f) + (UnityInput.GetKey(Key.LeftArrow) ? -1f : 0f), (UnityInput.GetKey(Key.UpArrow) ? 1f : 0f) + (UnityInput.GetKey(Key.DownArrow) ? -1f : 0f));
                if (leftOverride)
                    rightJoystick = direction;
                else
                    leftJoystick = direction;
            }

            if (UnityInput.GetKey(Modules.Categories.Settings.pcBindings[Modules.Categories.Settings.ControllerBinding.JoystickClick]))
            {
                if (leftOverride)
                    rightJoystickClick = true;
                else
                    leftJoystickClick = true;
            }
        }

        public enum ControllerType
        {
            Unknown,
            Quest2,
            Quest3,
            ValveIndex,
            VIVE
        }

        private static readonly Dictionary<bool, ControllerInfo> controllerInfo = new Dictionary<bool, ControllerInfo>
        {
            { true, new ControllerInfo { type = ControllerType.Unknown, dataCacheTime = -1f } },
            { false, new ControllerInfo { type = ControllerType.Unknown, dataCacheTime = -1f } }
        };

        private static readonly Dictionary<string, ControllerType> controllerNames = new Dictionary<string, ControllerType>
        {
            { "quest2", ControllerType.Quest2 },
            { "quest3", ControllerType.Quest3 },
            { "knuckles", ControllerType.ValveIndex },
            { "vive", ControllerType.VIVE }
        };

        private struct ControllerInfo
        {
            public ControllerType type;
            public float dataCacheTime;
        }

        /// <summary>
        /// Determines the type of VR controller currently in use for the specified hand.
        /// The result is derived from the XR device name and cached briefly to avoid
        /// repeated string comparisons.
        /// </summary>
        /// <param name="left">
        /// If true, queries the left controller; otherwise queries the right controller.
        /// </param>
        /// <returns>
        /// The detected <see cref="ControllerType"/> for the specified hand,
        /// or <see cref="ControllerType.Unknown"/> if XR is inactive, the controller
        /// is unavailable, or detection fails.
        /// </returns>
        public static ControllerType GetControllerType(bool left)
        {
            try
            {
                if (!XRSettings.isDeviceActive)
                    return ControllerType.Unknown;

                var controller = left
                    ? References.InputPoller.leftControllerDevice
                    : References.InputPoller.rightControllerDevice;

                if (controllerInfo.TryGetValue(left, out var info) && !(Time.time > info.dataCacheTime + 1f))
                    return info.type;
                var controllerName = controller.name;
                var controllerType = ControllerType.Unknown;

                foreach (var kvp in controllerNames)
                {
                    if (controllerName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    controllerType = kvp.Value;
                    break;
                }

                info = new ControllerInfo
                {
                    type = controllerType,
                    dataCacheTime = Time.time
                };
                controllerInfo[left] = info;

                return info.type;
            }
            catch
            {
                return ControllerType.Unknown;
            }
        }

        /// <summary>
        /// Determines the type of VR controller currently in use for the left hand.
        /// </summary>
        /// <returns>
        /// The detected left-hand <see cref="ControllerType"/>,
        /// or <see cref="ControllerType.Unknown"/> if detection fails.
        /// </returns>
        public static ControllerType GetLeftControllerType() => GetControllerType(true);

        /// <summary>
        /// Determines the type of VR controller currently in use for the right hand.
        /// </summary>
        /// <returns>
        /// The detected right-hand <see cref="ControllerType"/>,
        /// or <see cref="ControllerType.Unknown"/> if detection fails.
        /// </returns>
        public static ControllerType GetRightControllerType() => GetControllerType(false);

        /// <summary>
        /// Calculates the true world-space pose of a player's hand by combining the
        /// controller transform with the hand-specific positional and rotational offsets.
        /// </summary>
        /// <param name="left">
        /// If true, uses the left hand/controller; otherwise uses the right hand/controller.
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description><c>position</c> � The final world-space position of the hand.</description></item>
        /// <item><description><c>rotation</c> � The final world-space rotation of the hand.</description></item>
        /// <item><description><c>up</c> � The hand�s up direction in world space.</description></item>
        /// <item><description><c>forward</c> � The hand�s forward direction in world space.</description></item>
        /// <item><description><c>right</c> � The hand�s right direction in world space.</description></item>
        /// </list>
        /// </returns>
        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right) GetTrueHandPosition(bool left)
        {
            Transform controllerTransform = left ? References.LeftHand : References.RightHand;
            GTPlayer.HandState handState = left ? References.Player.LeftHand : References.Player.RightHand;

            Quaternion rot = controllerTransform.rotation * handState.handRotOffset;
            return (controllerTransform.position + controllerTransform.rotation * (handState.handOffset * References.Player.scale), rot, rot * Vector3.up, rot * Vector3.forward, rot * Vector3.right);
        }

        /// <summary>
        /// Gets the true world-space pose of the player's left hand,
        /// including position, rotation, and orientation vectors.
        /// </summary>
        /// <returns>
        /// A tuple containing the left hand�s world-space position, rotation,
        /// and its up, forward, and right direction vectors.
        /// </returns>
        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right)
            GetTrueLeftHand() => GetTrueHandPosition(true);

        /// <summary>
        /// Gets the true world-space pose of the player's right hand,
        /// including position, rotation, and orientation vectors.
        /// </summary>
        /// <returns>
        /// A tuple containing the right hand�s world-space position, rotation,
        /// and its up, forward, and right direction vectors.
        /// </returns>
        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right)
            GetTrueRightHand() => GetTrueHandPosition(false);
    
    }
}