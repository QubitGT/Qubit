/*
 * Qubit Menu  Core/References.cs
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
using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;

namespace Qubit.Core
{
    public class References
    {
        public static VRRig LocalRig
        {
            get => VRRig.LocalRig;
        }

        public static GorillaTagger Tagger
        {
            get => GorillaTagger.Instance;
        }

        public static GTPlayer Player
        {
            get => GTPlayer.Instance;
        }

        public static NetworkSystem Network
        {
            get => NetworkSystem.Instance;
        }

        public static ControllerInputPoller InputPoller
        {
            get => ControllerInputPoller.instance;
        }

        public static GorillaComputer Computer
        {
            get => GorillaComputer.instance;
        }

        public static SphereCollider HeadCollider
        {
            get => GorillaTagger.Instance.headCollider;
        }

        public static CapsuleCollider BodyCollider
        {
            get => GorillaTagger.Instance.bodyCollider;
        }

        public static Transform LeftHand
        {
            get => GorillaTagger.Instance.leftHandTransform;
        }

        public static Transform RightHand
        {
            get => GorillaTagger.Instance.rightHandTransform;
        }

        public static Rigidbody Rigidbody
        {
            get => GorillaTagger.Instance.rigidbody;
        }

        public static NetworkView MyRigView
        {
            get => GorillaTagger.Instance.myVRRig;
        }

        public static NetPlayer LocalPlayer
        {
            get => NetworkSystem.Instance.LocalPlayer;
        }

        public static bool InRoom
        {
            get => NetworkSystem.Instance.InRoom;
        }

        public static bool IsMasterClient
        {
            get => NetworkSystem.Instance.IsMasterClient;
        }
    }
}
