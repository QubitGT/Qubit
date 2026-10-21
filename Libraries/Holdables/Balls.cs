/*
 * Qubit Menu  Libraries/Holdables/Balls.cs
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
using Qubit.Modules.Classes;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Extensions.Random;
using static Qubit.Libraries.Controls.Controllers;
using Random = UnityEngine.Random;

namespace Qubit.Libraries.Holdables
{
    public static class Balls
    {
        public static float ballSizeScale = 1f;

        public static void BallGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                    ball.transform.position = NewPointer.transform.position + Vector3.up;
            });
        }

        public static void GrabBalls()
        {
            if (rightGrab)
            {
                foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                {
                    ball.transform.position = References.RightHand.position;
                    ball.transform.rotation = References.RightHand.rotation;
                }
            }
        }

        public static void StealBalls()
        {
            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                if (!ball.IsMyItem())
                    ball.WorldShareableRequestOwnership();
        }

        public static void SpazBalls()
        {
            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                ball.transform.rotation = RandomQuaternion();
        }

        public static void OrbitBalls()
        {
            TransferrableBall[] them = GetAllType<TransferrableBall>();
            int index = 0;
            foreach (TransferrableBall ball in them)
            {
                float offset = 360f / them.Length * index;
                ball.transform.position = References.HeadCollider.transform.position + new Vector3(Mathf.Cos(offset + (float)Time.frameCount / 30) * 2f, 1, Mathf.Sin(offset + (float)Time.frameCount / 30) * 2f);
                index++;
            }
        }

        public static void BallAura()
        {
            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
            {
                ball.transform.position = References.HeadCollider.transform.position + RandomVector3();
                ball.transform.rotation = RandomQuaternion();
            }
        }

        public static void DestroyBalls()
        {
            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                ball.transform.position = new Vector3(99999f, 99999f, 99999f);
        }

        public static void BallSizeChanger()
        {
            if (rightTrigger > 0.5f)
                ballSizeScale += 0.1f;
            if (rightGrab)
                ballSizeScale -= 0.1f;

            ballSizeScale = Mathf.Max(ballSizeScale, 0.05f);

            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                ball.transform.localScale = Vector3.one * ballSizeScale;
        }

        public static void BecomeBall()
        {
            References.LocalRig.enabled = false;
            References.LocalRig.transform.position = References.BodyCollider.transform.position - Vector3.up * 99999f;

            foreach (TransferrableBall ball in GetAllType<TransferrableBall>())
                ball.transform.position = References.BodyCollider.transform.position;
        }
    }
}
