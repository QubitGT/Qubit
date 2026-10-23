/*
 * Qubit Menu  Extensions/GameObj.cs
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
using System;
using System.Collections.Generic;
using UnityEngine;
using static Qubit.Extensions.Vectors;
using static Qubit.Frontend.Settings;
using Object = UnityEngine.Object;

namespace Qubit.Extensions
{
    public static class GameObj
    {
        public static readonly Dictionary<Type, object[]> typePool = new Dictionary<Type, object[]>();
        private static readonly Dictionary<Type, float> receiveTypeDelay = new Dictionary<Type, float>();

        public static T[] GetAllType<T>(float decayTime = 5f) where T : Object
        {
            Type type = typeof(T);

            float lastReceivedTime = receiveTypeDelay.GetValueOrDefault(type, -1f);

            if (Time.time > lastReceivedTime)
            {
                typePool.Remove(type);
                receiveTypeDelay[type] = Time.time + decayTime;
            }

            if (!typePool.ContainsKey(type))
                typePool.Add(type, UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            return (T[])typePool[type];
        }

        private static float randomIndex;
        private static float randomDecayTime;

        public static T GetRandomType<T>(float decayTime = 0f) where T : Object
        {
            T[] allOfType = GetAllType<T>();

            if (!(Time.time > randomDecayTime)) return allOfType[(int)(randomIndex * allOfType.Length)];
            randomIndex = UnityEngine.Random.Range(0f, 1f);
            randomDecayTime = Time.time + decayTime;

            return allOfType[(int)(randomIndex * allOfType.Length)];
        }

        public static void ClearType<T>() where T : Object
        {
            Type type = typeof(T);

            typePool.Remove(type);
        }

        private static readonly Dictionary<string, GameObject> objectPool = new Dictionary<string, GameObject>();

        public static GameObject GetObject(string find)
        {
            if (objectPool.TryGetValue(find, out GameObject go))
                return go;

            GameObject tgo = GameObject.Find(find);
            if (!tgo && find.Contains("/"))
            {
                var split = find.Split('/');
                var rootName = split[0];

                var root = GameObject.Find(rootName);

                if (root != null)
                {
                    var path = find[(rootName.Length + 1)..];
                    var tr = root.transform.Find(path);

                    if (tr != null)
                        tgo = tr.gameObject;
                }
            }
            if (tgo != null)
                objectPool.Add(find, tgo);

            return tgo;
        }

        public static void FixStickyColliders(GameObject platform)
        {
            Vector3[] localPositions = {
                new Vector3(0, 1f, 0),
                new Vector3(0, -1f, 0),
                new Vector3(1f, 0, 0),
                new Vector3(-1f, 0, 0),
                new Vector3(0, 0, 1f),
                new Vector3(0, 0, -1f)
            };
            Quaternion[] localRotations = {
                Quaternion.Euler(90, 0, 0),
                Quaternion.Euler(-90, 0, 0),
                Quaternion.Euler(0, -90, 0),
                Quaternion.Euler(0, 90, 0),
                Quaternion.identity,
                Quaternion.Euler(0, 180, 0)
            };
            for (int i = 0; i < localPositions.Length; i++)
            {
                GameObject side = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float size = 0.025f * (scaleWithPlayer ? References.Player.scale : 1f);
                side.transform.SetParent(platform.transform);
                side.transform.position = localPositions[i] * (size / 2);
                side.transform.rotation = localRotations[i];
                WorldScale(side, new Vector3(size, size, 0.01f * (scaleWithPlayer ? References.Player.scale : 1f)));
                side.GetComponent<Renderer>().enabled = false;
            }
        }

        private static int? noInvisLayerMask;
        public static int NoInvisLayerMask()
        {
            noInvisLayerMask ??= ~(
                1 << LayerMask.NameToLayer("TransparentFX") |
                1 << LayerMask.NameToLayer("Ignore Raycast") |
                1 << LayerMask.NameToLayer("Zone") |
                1 << LayerMask.NameToLayer("Gorilla Trigger") |
                1 << LayerMask.NameToLayer("Gorilla Boundary") |
                1 << LayerMask.NameToLayer("GorillaCosmetics") |
                1 << LayerMask.NameToLayer("GorillaParticle"));

            return noInvisLayerMask ?? References.Player.locomotionEnabledLayers;
        }
    }
}
