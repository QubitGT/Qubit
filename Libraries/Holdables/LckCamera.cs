/*
 * Qubit Menu  Libraries/Holdables/LckCamera.cs
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

using GorillaExtensions;
using GorillaLocomotion.Climbing;
using Qubit.Core;
using Qubit.Modules.Classes;
using System;
using UnityEngine;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Modules.Categories.Fun;
using static Qubit.Modules.Categories.Projectile;
using static Qubit.Modules.Classes.GunLibrary;
using Manager = Qubit.Modules.Manager;
using Object = UnityEngine.Object;

namespace Qubit.Libraries.Holdables
{
    public static class LckCamera
    {
        public static void CameraGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);

                camera.transform.position = NewPointer.transform.position + Vector3.up;
            });
        }

        public static void TabletGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);

                camera.transform.position = NewPointer.transform.position + Vector3.up;
            });
        }
        public static void CameraSpam()
        {
            if (rightGrab && Time.time > cameraSpamDelay)
            {
                cameraSpamDelay = Time.time + 0.25f;
                cameraSpamType = !cameraSpamType;

                LckSocialCamera camera = cameraSpamType ? ManagerRegistry.LckSocialCameraManager._networkedCococam : ManagerRegistry.LckSocialCameraManager._networkedTablet;

                GameObject cameraSpamObject = new GameObject("Qubit_CameraSpamObject");
                cameraSpamObject.transform.localScale = Vector3.one * 0.2f;
                cameraSpamObject.layer = 3;

                if (Manager.GetIndex("Bug Colliders").enabled)
                {
                    SphereCollider collider = cameraSpamObject.AddComponent<SphereCollider>();

                    if (Manager.GetIndex("Bouncy Bug").enabled)
                    {
                        collider.material.bounciness = 1f;
                        collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                        collider.material.dynamicFriction = 0f;
                    }
                }

                cameraSpamObject.transform.position = References.RightHand.position + GetGunDirection(References.RightHand) * 0.5f;
                cameraSpamObject.transform.rotation = References.RightHand.rotation;

                Rigidbody rigidbody = cameraSpamObject.AddComponent<Rigidbody>();
                rigidbody.linearVelocity = GetGunDirection(References.RightHand) * ShootStrength;
                rigidbody.angularVelocity = RandomVector3(100f);

                rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);

                camera.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = cameraSpamObject.transform;

                cameraSpamObject.AddComponent<DestroyOnRest>();
                Object.Destroy(cameraSpamObject, 30f);
            }
        }

        public static void DisableCameraSpam()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;

            if (camera.GetComponent<ClampPosition>() != null)
                Object.Destroy(camera.GetComponent<ClampPosition>());
        }

        public static void PhysicalCamera()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;

            if (!camera.visible)
            {
                camera.transform.position = References.RightHand.position;
                camera.transform.rotation = References.RightHand.rotation;

                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);
            }

            if (!grabbingCamera)
            {
                bool canGrabLeft = Vector3.Distance(References.LeftHand.position, camera.transform.position) < 0.3f && leftGrab;
                bool canGrabRight = Vector3.Distance(References.RightHand.position, camera.transform.position) < 0.3f && rightGrab;

                if (canGrabLeft || canGrabRight)
                {
                    grabbingCamera = true;
                    grabbingHand = canGrabLeft;
                }
            }
            else
            {
                Transform targetTransform = grabbingHand ?
                    References.LeftHand :
                    References.RightHand;

                camera.transform.position = targetTransform.position;
                camera.transform.rotation = targetTransform.rotation;

                GorillaVelocityTracker tracker = camera.gameObject.GetOrAddComponent<GorillaVelocityTracker>();
                GorillaVelocityEstimator estimator = camera.gameObject.GetOrAddComponent<GorillaVelocityEstimator>();

                if (grabbingHand ? !leftGrab : !rightGrab)
                {
                    grabbingCamera = false;

                    GameObject bugSpamObject = new GameObject("Qubit_BugSpamObject");
                    bugSpamObject.transform.localScale = Vector3.one * 0.2f;
                    bugSpamObject.layer = 3;

                    if (Manager.GetIndex("Bug Colliders").enabled)
                    {
                        SphereCollider collider = bugSpamObject.AddComponent<SphereCollider>();

                        if (Manager.GetIndex("Bouncy Bug").enabled)
                        {
                            collider.material.bounciness = 1f;
                            collider.material.bounceCombine = PhysicsMaterialCombine.Maximum;
                            collider.material.dynamicFriction = 0f;
                        }
                    }

                    bugSpamObject.transform.position = camera.transform.position;
                    bugSpamObject.transform.rotation = camera.transform.rotation;

                    Rigidbody rigidbody = bugSpamObject.AddComponent<Rigidbody>();
                    rigidbody.linearVelocity = tracker.GetAverageVelocity(true, 0);
                    rigidbody.angularVelocity = estimator.angularVelocity;

                    rigidbody.useGravity = !Manager.GetIndex("Zero Gravity Bugs").enabled;

                    camera.gameObject.GetOrAddComponent<ClampPosition>().targetTransform = bugSpamObject.transform;

                    bugSpamObject.AddComponent<DestroyOnRest>();
                    Object.Destroy(bugSpamObject, 30f);
                }
            }
        }

        public static void GrabCamera()
        {
            if (rightGrab)
            {
                LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);

                camera.transform.position = References.RightHand.position;
                camera.transform.rotation = References.RightHand.rotation;
            }
        }

        public static void GrabTablet()
        {
            if (rightGrab)
            {
                LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
                camera.visible = true;
                camera.recording = true;

                camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
                camera.m_CameraVisuals.SetRecordingState(true);

                camera.transform.position = References.RightHand.position;
                camera.transform.rotation = References.RightHand.rotation;
            }
        }

        public static void DestroyCamera()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = false;
            camera.recording = false;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(false);
            camera.m_CameraVisuals.SetRecordingState(false);
        }

        public static void DestroyTablet()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
            camera.visible = false;
            camera.recording = false;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(false);
            camera.m_CameraVisuals.SetRecordingState(false);
        }

        public static void SpazCamera()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.rotation = RandomQuaternion();
        }

        public static void SpazTablet()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.rotation = RandomQuaternion();
        }

        public static void OrbitCamera()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(240f + (float)Time.frameCount / 30), 1, MathF.Sin(240f + (float)Time.frameCount / 30));
        }

        public static void OrbitTablet()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(60f + (float)Time.frameCount / 30), 1, MathF.Sin(240f + (float)Time.frameCount / 30));
        }

        public static void CameraAura()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.HeadCollider.transform.position + RandomVector3();
            camera.transform.rotation = RandomQuaternion();
        }

        public static void TabletAura()
        {
            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.HeadCollider.transform.position + RandomVector3();
            camera.transform.rotation = RandomQuaternion();
        }

        public static void BecomeCamera()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedCococam;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.BodyCollider.transform.position;
            camera.transform.rotation = References.HeadCollider.transform.rotation;
        }

        public static void BecomeTablet()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

            LckSocialCamera camera = ManagerRegistry.LckSocialCameraManager._networkedTablet;
            camera.visible = true;
            camera.recording = true;

            camera.m_CameraVisuals.SetNetworkedVisualsActive(true);
            camera.m_CameraVisuals.SetRecordingState(true);

            camera.transform.position = References.BodyCollider.transform.position;
            camera.transform.rotation = References.HeadCollider.transform.rotation;
        }
    }
}
