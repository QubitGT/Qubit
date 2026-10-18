/*
 * Qubit Menu  Modules/Classes/GunLibrary.cs
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
using Qubit.Extensions;
using Qubit.Frontend;
using Qubit.Frontend.Classes;
using Qubit.Libraries.Controls;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Qubit.Frontend.Settings;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Utilities.GameObj;
using Random = UnityEngine.Random;

namespace Qubit.Modules.Classes
{
    public static class GunLibrary
    {
        /// <summary>
        /// Optional behavior for the gun methods in <see cref="GunLibrary"/>.
        /// </summary>
        public sealed class GunSettings
        {
            /// <summary>Locks onto the rig that was shot and keeps running the action while the grip is held.</summary>
            public bool locks;

            /// <summary>Extra condition that must be true before the trigger is allowed to fire (delays, cooldowns).</summary>
            public Func<bool> when;

            /// <summary>Runs every frame the grip is not held.</summary>
            public Action onIdle;

            /// <summary>Overrides the layer mask the gun ray is cast with.</summary>
            public int? layerMask;
        }

        /// <summary>
        /// Runs the action with the raw gun ray and pointer while the grip is held.
        /// </summary>
        public static void Aim(Action<(RaycastHit Ray, GameObject NewPointer)> action, GunSettings settings = null)
        {
            if (GetGunInput(false))
                action(RenderGun(settings?.layerMask));
            else
                settings?.onIdle?.Invoke();
        }

        /// <summary>
        /// Runs the action on the rig the gun points at when the trigger is pressed.
        /// With <see cref="GunSettings.locks"/> the rig is locked on and the action runs every frame while the grip is held.
        /// </summary>
        public static void Target(Action<VRRig> action, GunSettings settings = null)
        {
            bool locks = settings != null && settings.locks;

            GunLibrary.Aim(gunData =>
            {
                if (locks && gunLocked && lockTarget != null)
                                    action(lockTarget);

                                if (GetGunInput(true) && (locks || settings?.when == null || settings.when()))
                                {
                                    VRRig gunTarget = gunData.Ray.collider.GetComponentInParent<VRRig>();
                                    if (gunTarget && !gunTarget.IsLocal())
                                    {
                                        if (locks)
                                        {
                                            gunLocked = true;
                                            lockTarget = gunTarget;
                                        }
                                        else
                                            action(gunTarget);
                                    }
                                }

            }, new GunLibrary.GunSettings { onIdle = () =>
                {
                    if (locks)
                    {
                        if (gunLocked)
                            gunLocked = false;
                    }
                    else
                        settings?.onIdle?.Invoke();

                }, layerMask = settings?.layerMask });
        }

        /// <summary>
        /// Runs the action with the pointer when the trigger is pressed.
        /// </summary>
        public static void Point(Action<GameObject> action, GunSettings settings = null)
        {
            GunLibrary.Aim(gunData =>
            {
                if (GetGunInput(true) && (settings?.when == null || settings.when()))
                                    action(gunData.NewPointer);

            }, new GunLibrary.GunSettings { onIdle = () => { settings?.onIdle?.Invoke(); }, layerMask = settings?.layerMask });
        }

        private static readonly List<float> volumeArchive = new List<float>();
        private static Vector3 GunPositionSmoothed = Vector3.zero;

        public static GameObject GunPointer;
        public static LineRenderer GunLine;

        /// <summary>
        /// Renders a gun pointer and line from the player's target gun position.
        /// </summary>
        /// <param name="overrideLayerMask">Layer Mask</param>
        /// <returns>RaycastHit and Pointer GameObject</returns>
        internal static (RaycastHit Ray, GameObject NewPointer) RenderGun(int? overrideLayerMask = null)
        {
            GunSpawned = true;
            Transform GunTransform = SwapGunHand ? References.LeftHand : References.RightHand;

            Vector3 StartPosition = GunTransform.position;
            Vector3 Direction = GunTransform.forward;

            Vector3 Up = -GunTransform.up;
            Vector3 Right = GunTransform.right;

            switch (GunDirection)
            {
                case 1:
                    Up = GunTransform.forward;
                    Direction = -GunTransform.up;
                    break;
                case 2:
                    Up = GunTransform.forward;
                    Right = -GunTransform.up;
                    Direction = GunTransform.right * (SwapGunHand ? 1f : -1f);
                    break;
                case 3:
                    var (_, _, up, forward, right) = SwapGunHand ? Controllers.GetTrueLeftHand() : Controllers.GetTrueRightHand();

                    Up = up;
                    Right = right;
                    Direction = forward;
                    break;
                case 4:
                    Up = References.HeadCollider.transform.up;
                    Right = References.HeadCollider.transform.right;
                    Direction = References.HeadCollider.transform.forward;
                    StartPosition = References.HeadCollider.transform.position + Up * 0.1f;
                    break;
            }

            if (GiveGunTarget != null)
            {
                GunTransform = SwapGunHand ? GiveGunTarget.leftHandTransform : GiveGunTarget.rightHandTransform;

                StartPosition = GunTransform.position;
                Direction = GunTransform.up;

                Up = GunTransform.forward;
                Right = GunTransform.right;
            }

            Physics.Raycast(StartPosition + Direction / 4f * (scaleWithPlayer ? References.Player.scale : 1f), Direction, out var Ray, 512f, overrideLayerMask ?? NoInvisLayerMask());

            if (shouldBePC)
                UI.AimGunAtMouse(out Ray, ref Direction);
            Vector3 EndPosition = gunLocked ? lockTarget.transform.position : Ray.point;

            if (EndPosition == Vector3.zero)
                EndPosition = StartPosition + Direction * 512f;

            if (SmoothGunPointer)
            {
                GunPositionSmoothed = Vector3.Lerp(GunPositionSmoothed, EndPosition, Time.deltaTime * 6f);
                EndPosition = GunPositionSmoothed;
            }

            if (GunPointer == null)
                GunPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            GunPointer.SetActive(true);
            GunPointer.transform.localScale = (smallGunPointer ? new Vector3(0.1f, 0.1f, 0.1f) : new Vector3(0.2f, 0.2f, 0.2f)) * (scaleWithPlayer ? References.Player.scale : 1f);
            GunPointer.transform.position = EndPosition;

            Renderer PointerRenderer = GunPointer.GetComponent<Renderer>();

            if (PointerRenderer.sharedMaterial.shader.name != "GUI/Text Shader")
                PointerRenderer.material.shader = Shader.Find("GUI/Text Shader");

            PointerRenderer.material.color = gunLocked || GetGunInput(true) ? buttonColors[1].GetCurrentColor() : buttonColors[0].GetCurrentColor();

            if (disableGunPointer)
                PointerRenderer.enabled = false;

            if (GunParticles && (GetGunInput(true) || gunLocked))
            {
                GameObject Particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Particle.transform.position = EndPosition;
                Particle.transform.localScale = Vector3.one * (0.025f * (scaleWithPlayer ? References.Player.scale : 1f));
                Particle.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                Particle.AddComponent<CustomParticle>();
                UnityEngine.Object.Destroy(Particle.GetComponent<Collider>());
            }

            Collider gunPointerCollider = GunPointer.GetComponent<Collider>();
            if (gunPointerCollider != null)
                UnityEngine.Object.Destroy(gunPointerCollider);

            if (disableGunLine) return (Ray, GunPointer);
            if (GunLine == null)
            {
                GameObject line = new GameObject("Qubit_GunLine");
                GunLine = line.AddComponent<LineRenderer>();
            }

            GunLine.gameObject.SetActive(true);
            if (GunLine.material.shader.name != "GUI/Text Shader")
                GunLine.material.shader = Shader.Find("GUI/Text Shader");
            GunLine.startColor = backgroundColor.GetCurrentColor();
            GunLine.endColor = backgroundColor.GetCurrentColor(0.5f);
            GunLine.startWidth = 0.025f * (scaleWithPlayer ? References.Player.scale : 1f);
            GunLine.endWidth = 0.025f * (scaleWithPlayer ? References.Player.scale : 1f);
            GunLine.useWorldSpace = true;
            if (smoothLines)
            {
                GunLine.numCapVertices = 10;
                GunLine.numCornerVertices = 5;
            }
            if (gunVariation != 9)
            {
                GunLine.positionCount = 2;
                GunLine.SetPosition(0, StartPosition);
                GunLine.SetPosition(1, EndPosition);
            }
            int Step = GunLineQuality;
            switch (gunVariation)
            {
                case 1: // Lightning
                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position + (Random.Range(0f, 1f) > 0.75f ? new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)) : Vector3.zero));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 2: // Wavy
                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            float value = i / (float)Step * 50f;

                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position + Up * (Mathf.Sin(Time.time * -10f + value) * 0.1f));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 3: // Blocky
                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, new Vector3(Mathf.Round(Position.x * 25f) / 25f, Mathf.Round(Position.y * 25f) / 25f, Mathf.Round(Position.z * 25f) / 25f));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 4: // Sinewave
                    Step = GunLineQuality / 2;

                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position + Up * (Mathf.Sin(Time.time * 10f) * (i % 2 == 0 ? 0.1f : -0.1f)));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 5: // Spring
                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            float value = i / (float)Step * 50f;

                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position + Right * (Mathf.Cos(Time.time * -10f + value) * 0.1f) + Up * (Mathf.Sin(Time.time * -10f + value) * 0.1f));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 6: // Bouncy
                    if (GetGunInput(true) || gunLocked)
                    {
                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            float value = i / (float)Step * 15f;
                            GunLine.SetPosition(i, Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f)) + Up * (Mathf.Abs(Mathf.Sin(Time.time * -10f + value)) * 0.3f));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 7: // Audio
                    if (GetGunInput(true) || gunLocked)
                    {
                        float audioSize = 0f;

                        if (gunLocked)
                        {
                            GorillaSpeakerLoudness targetRecorder = lockTarget.GetComponent<GorillaSpeakerLoudness>();
                            if (targetRecorder != null)
                                audioSize += targetRecorder.Loudness * 3f;
                        }

                        GorillaSpeakerLoudness localRecorder = References.LocalRig.GetComponent<GorillaSpeakerLoudness>();
                        if (localRecorder != null)
                            audioSize += localRecorder.Loudness * 3f;

                        if (volumeArchive.Count > Step)
                            volumeArchive.RemoveAt(volumeArchive.Count - 1);

                        GunLine.positionCount = Step;
                        GunLine.SetPosition(0, StartPosition);

                        for (int i = 1; i < Step - 1; i++)
                        {
                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position + Up * ((i >= volumeArchive.Count ? 0 : volumeArchive[i]) * (i % 2 == 0 ? 1f : -1f)));
                        }

                        GunLine.SetPosition(Step - 1, EndPosition);
                    }
                    break;
                case 8: // Bezier, credits to Crisp / Kman / Steal / Untitled One of those 4 I don't really know who
                    Vector3 BaseMid = Vector3.Lerp(StartPosition, EndPosition, 0.5f);

                    float angle = Time.time * 3f;
                    Vector3 wobbleOffset = Up * (Mathf.Sin(angle) * 0.15f) + Right * (Mathf.Cos(angle * 1.3f) * 0.15f);
                    Vector3 targetMid = BaseMid + wobbleOffset;

                    if (MidPosition == Vector3.zero) MidPosition = targetMid;

                    Vector3 force = (targetMid - MidPosition) * 40f;
                    MidVelocity += force * Time.deltaTime;
                    MidVelocity *= Mathf.Exp(-6f * Time.deltaTime);
                    MidPosition += MidVelocity * Time.deltaTime;

                    GunLine.positionCount = Step;
                    GunLine.SetPosition(0, StartPosition);

                    Vector3[] points = new Vector3[Step];
                    for (int i = 0; i < Step; i++)
                    {
                        float t = (float)i / (Step - 1);
                        points[i] = Mathf.Pow(1 - t, 2) * StartPosition +
                                    2 * (1 - t) * t * MidPosition +
                                    Mathf.Pow(t, 2) * EndPosition;
                    }

                    GunLine.positionCount = Step;
                    GunLine.SetPositions(points);
                    break;
                case 9: // Rope
                    GunLine.positionCount = Step;

                    RopePhysics physics = GunLine.gameObject.GetComponent<RopePhysics>();
                    if (physics == null)
                    {
                        for (int i = 0; i < Step; i++)
                        {
                            Vector3 Position = Vector3.Lerp(StartPosition, EndPosition, i / (Step - 1f));
                            GunLine.SetPosition(i, Position);
                        }

                        physics = GunLine.gameObject.AddComponent<RopePhysics>();
                    }

                    physics.segmentLength = Vector3.Distance(StartPosition, EndPosition) / (Step - 1) * (GetGunInput(true) || gunLocked ? 1.1f : 1.2f);
                    physics.SetStartPosition(StartPosition);
                    physics.SetEndPosition(EndPosition);
                    break;
            }

            return (Ray, GunPointer);
        }

        private static VRRig _giveGunTarget;
        public static VRRig GiveGunTarget
        {
            get
            {
                if (!Players.ActiveRigs.Contains(_giveGunTarget))
                    _giveGunTarget = null;

                return _giveGunTarget;
            }
            set => _giveGunTarget = value;
        }

        /// <summary>
        /// Returns the gun input state.
        /// </summary>
        /// <param name="isShooting">Trigger</param>
        /// <returns>Holding Button</returns>
        public static bool GetGunInput(bool isShooting)
        {
            return GiveGunTarget != null
                ? isShooting
                    ? TriggerlessGuns || (SwapGunHand ? GiveGunTarget.leftIndex.calcT > 0.5f : GiveGunTarget.rightIndex.calcT > 0.5f)
                    : GriplessGuns || (SwapGunHand ? GiveGunTarget.leftMiddle.calcT > 0.5f : GiveGunTarget.rightMiddle.calcT > 0.5f)
                : isShooting
                ? TriggerlessGuns || (SwapGunHand ? leftTrigger > 0.5f : rightTrigger > 0.5f) || UI.IsMouseShooting()
                : GriplessGuns || (SwapGunHand ? leftGrab : rightGrab) || (HardGunLocks && gunLocked && !rightSecondary) || UI.IsMouseGripping();
        }

        /// <summary>
        /// Returns the gun direction vector based on the specified transform and the current GunDirection setting.
        /// </summary>
        /// <param name="transform">The transform used to determine the gun's orientation.</param>
        /// <returns>A Vector3 representing the selected gun direction.</returns>
        public static Vector3 GetGunDirection(Transform transform)
        {
            return GunDirection switch
            {
                1 => transform.forward,

                2 => -transform.up,

                3 => Vector3.Distance(transform.position, Controllers.GetTrueRightHand().position) <
                     Vector3.Distance(transform.position, Controllers.GetTrueLeftHand().position)
                    ? Controllers.GetTrueRightHand().forward
                    : Controllers.GetTrueLeftHand().forward,

                4 => References.HeadCollider.transform.forward,

                _ => transform.forward
            };
        }

        public static Vector3 MidPosition;
        public static Vector3 MidVelocity;

        public static bool SmoothGunPointer;
        public static bool smallGunPointer;
        public static bool disableGunPointer;
        public static bool disableGunLine;
        public static bool SwapGunHand;
        public static bool GriplessGuns;
        public static bool TriggerlessGuns;
        public static bool HardGunLocks;
        public static bool GunSounds;
        public static bool GunVibrations;
        public static bool GunParticles;
        public static int gunVariation;
        public static int GunDirection;
        public static int GunLineQuality = 50;

        public static bool GunSpawned;
        public static bool gunLocked;
        public static VRRig lockTarget;

        public static bool lastGunSpawned;
        public static bool lastGunTrigger;

        public static bool lastGunSpawnedVibration;
    }
}
