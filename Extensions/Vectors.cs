/*
 * Qubit Menu  Extensions/Vectors.cs
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
using UnityEngine;
using static Qubit.Extensions.Random;

namespace Qubit.Extensions
{
    public static class Vectors
    {
        public static float Distance(this Vector3 point, Vector3 to) =>
            Vector3.Distance(point, to);

        public static Vector3 Lerp(this Vector3 a, Vector3 b, float t) =>
            Vector3.Lerp(a, b, t);

        public static Vector3 XyZ(this Vector3 a) =>
            new Vector3(a.x, Mathf.Max(a.y, 0f), a.z);

        public static long Pack(this Vector3 vec) =>
            BitPackUtils.PackWorldPosForNetwork(vec);

        public static Vector3 Random(this Vector3 _, float power = 1) =>
            RandomVector3(power);

        public static Vector3 ClampMagnitude(this Vector3 vec, float magnitude) =>
            Vector3.ClampMagnitude(vec, magnitude);

        public static Vector3 ClampSqrMagnitude(this Vector3 vec, float sqrMagnitude)
        {
            float currentSqrMag = vec.sqrMagnitude;

            if (!(currentSqrMag > sqrMagnitude) || !(currentSqrMag > 0f)) return vec;
            float scale = MathF.Sqrt(sqrMagnitude / currentSqrMag);
            vec *= scale;

            return vec;
        }

        /// <summary>
        /// Converts a position from world coordinates to the player's local coordinate space.
        /// </summary>
        /// <remarks>This method is useful for determining the position of an object or point in relation
        /// to the player's current location and orientation within the game world.</remarks>
        /// <param name="world">The position in world coordinates to convert.</param>
        /// <returns>A <see cref="Vector3"/> representing the input position relative to the player's local coordinate system.</returns>
        public static Vector3 World2Player(Vector3 world) =>
            world - References.BodyCollider.transform.position + References.Tagger.transform.position;

        /// <summary>
        /// Sets the world-scale of a GameObject to a specified value,
        /// taking into account the scale of its parent.
        /// </summary>
        /// <param name="obj">The GameObject whose scale will be adjusted.</param>
        /// <param name="targetWorldScale">
        /// The desired scale of the GameObject in world space.
        /// Each component (x, y, z) represents the target size along that axis.
        /// </param>
        /// <remarks>
        /// This method adjusts the localScale of the GameObject so that its
        /// effective world scale matches the targetWorldScale, regardless of
        /// the parent's scale. It uses the parent's lossyScale to calculate
        /// the required localScale.
        /// </remarks>
        public static void WorldScale(GameObject obj, Vector3 targetWorldScale)
        {
            Vector3 parentScale = obj.transform.parent.lossyScale;
            obj.transform.localScale = new Vector3(targetWorldScale.x / parentScale.x, targetWorldScale.y / parentScale.y, targetWorldScale.z / parentScale.z);
        }
    }
}
