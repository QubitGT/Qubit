/*
 * Qubit Menu  Libraries/Holdables/Gliders.cs
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

using Photon.Pun;
using Qubit.Core;
using Qubit.Extensions;
using Qubit.Modules.Classes;
using System;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Players;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using static Qubit.Modules.Classes.GunLibrary;
using Random = UnityEngine.Random;

namespace Qubit.Libraries.Holdables
{
    public static class Gliders
    {

        public static void GliderGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                {
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                        glider.gameObject.transform.position = NewPointer.transform.position + Vector3.up;
                    else
                        glider.OnHover(null, null);
                }
            });
        }

        public static void ModifyGliderSpeed(float pullUpLiftBonus, float dragVsSpeedDragFactor)
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                glider.pullUpLiftBonus = pullUpLiftBonus;
                glider.dragVsSpeedDragFactor = dragVsSpeedDragFactor;
            }
        }

        public static void FixGliderSpeed()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                glider.pullUpLiftBonus = 0.1f;
                glider.dragVsSpeedDragFactor = 0.2f;
            }
        }

        public static void GrabGliders()
        {
            if (rightGrab)
            {
                foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                {
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    {
                        glider.gameObject.transform.position = References.RightHand.position;
                        glider.gameObject.transform.rotation = References.RightHand.rotation;
                    }
                    else
                        glider.OnHover(null, null);
                }
            }
        }

        public static void RespawnGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.Respawn();
                else
                    glider.OnHover(null, null);
            }
        }

        public static void SpazGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.gameObject.transform.rotation = RandomQuaternion();
                else
                    glider.OnHover(null, null);
            }
        }

        public static void GliderAura()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                {
                    glider.gameObject.transform.position = References.HeadCollider.transform.position + RandomVector3();
                    glider.gameObject.transform.rotation = RandomQuaternion();
                }
                else
                    glider.OnHover(null, null);
            }
        }

        public static void OrbitGliders()
        {
            GliderHoldable[] them = GetAllType<GliderHoldable>();
            int index = 0;
            foreach (GliderHoldable glider in them)
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                {
                    float offset = 360f / them.Length * index;
                    glider.gameObject.transform.position = References.HeadCollider.transform.position + new Vector3(MathF.Cos(offset + (float)Time.frameCount / 30) * 5f, 2, MathF.Sin(offset + (float)Time.frameCount / 30) * 5f);
                }
                else
                    glider.OnHover(null, null);

                index++;
            }
        }

        public static float[] smoothedGliderHeights;

        public static void GliderVisualizer()
        {
            GliderHoldable[] them = GetAllType<GliderHoldable>();
            if (smoothedGliderHeights == null || smoothedGliderHeights.Length != them.Length)
                smoothedGliderHeights = new float[them.Length];

            float loudness = 0f;
            GorillaSpeakerLoudness recorder = References.LocalRig.GetComponent<GorillaSpeakerLoudness>();
            if (recorder != null)
                loudness = recorder.Loudness;

            for (int index = 0; index < them.Length; index++)
            {
                GliderHoldable glider = them[index];
                if (glider.GetView.Owner != PhotonNetwork.LocalPlayer)
                {
                    glider.OnHover(null, null);
                    continue;
                }

                smoothedGliderHeights[index] = Mathf.Lerp(smoothedGliderHeights[index], loudness * 10f, Time.deltaTime * 8f);

                float angle = (360f / them.Length * index + Time.time * 20f) * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                glider.gameObject.transform.position = References.HeadCollider.transform.position + outward * 6f + Vector3.up * (smoothedGliderHeights[index] - 0.1f);
                glider.gameObject.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
            }
        }

        public static void DestroyGliders()
        {
            foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
            {
                if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    glider.gameObject.transform.position = new Vector3(99999f, 99999f, 99999f);
                else
                    glider.OnHover(null, null);
            }
        }

        public static void GliderBlindGun()
        {
            GunLibrary.Aim(GunData =>
            {
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }

                if (gunLocked)
                {
                    foreach (GliderHoldable glider in GetAllType<GliderHoldable>())
                    {
                        if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                        {
                            glider.gameObject.transform.position = lockTarget.headMesh.transform.position;
                            glider.gameObject.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                        }
                        else
                            glider.OnHover(null, null);
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

        public static void GliderBlindAll()
        {
            GliderHoldable[] those = GetAllType<GliderHoldable>();
            int index = 0;
            foreach (var vrrig in Players.ActiveRigs.Where(vrrig => !vrrig.isLocal))
            {
                try
                {
                    GliderHoldable glider = those[index];
                    if (glider.GetView.Owner == PhotonNetwork.LocalPlayer)
                    {
                        glider.gameObject.transform.position = vrrig.headMesh.transform.position;
                        glider.gameObject.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                    }
                    else
                        glider.OnHover(null, null);
                }
                catch { }
                index++;
            }
        }
    }
}
