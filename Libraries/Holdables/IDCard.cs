/*
 * Qubit Menu  Libraries/Holdables/IDCard.cs
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
using Qubit.Libraries.Interface;
using Qubit.Modules.Classes;
using System.Linq;
using UnityEngine;
using static Qubit.Extensions.GameObj;
using static Qubit.Libraries.Controls.Controllers;

namespace Qubit.Libraries.Holdables
{
    /// <summary>
    /// Mods that control the beach ID card holdable.
    /// </summary>
    public static class IDCard
    {
        private static TransferrableObject _card;

        public static TransferrableObject Card
        {
            get
            {
                if (_card == null || !_card.gameObject.activeInHierarchy)
                    _card = GetAllType<TransferrableObject>().FirstOrDefault(card => card.gameObject.name == "ID Card Holdable");

                return _card;
            }
        }

        /// <summary>
        /// Requests ownership of the card if we don't control it yet.
        /// </summary>
        private static bool EnsureOwnership()
        {
            TransferrableObject card = Card;
            if (card == null)
                return false;

            if (!card.IsMyItem() && (card.Dropped() || (int)card.currentState == 0))
            {
                if (!Qubit.Core.Networking.IsMasterClient)
                {
                    Notifications.SendFormat("yellow", "ID CARD", "Trying to get ownership of the card, please be patient.");
                    return false;
                }

                card.WorldShareableRequestOwnership();
                return false;
            }

            if (card.IsMyItem() || !Qubit.Core.Networking.InRoom)
            {
                References.LocalRig.enabled = true;
                card.maxDistanceFromOriginBeforeRespawn = float.MaxValue;
                card.maxDistanceFromTargetPlayerBeforeRespawn = float.MaxValue;
            }

            return true;
        }

        public static void IDCardGun()
        {
            GunLibrary.Point(NewPointer =>
            {
                if (!EnsureOwnership()) return;
                if (Card != null)
                    Card.transform.position = NewPointer.transform.position + new Vector3(0f, 0.38f, 0f);
            });
        }

        public static void GrabIDCard()
        {
            if (rightGrab)
            {
                if (!EnsureOwnership()) return;
                if (Card != null)
                    Card.transform.position = References.RightHand.position;
            }
            else if (leftGrab)
            {
                if (!EnsureOwnership()) return;
                if (Card != null)
                    Card.transform.position = References.LeftHand.position;
            }
        }

        public static void OrbitIDCard()
        {
            if (!EnsureOwnership()) return;

            TransferrableObject card = Card;
            if (card == null)
                return;

            float angle = Time.time * 3f;
            card.transform.position = References.LocalRig.transform.position + new Vector3(Mathf.Sin(angle), 1f, Mathf.Cos(angle));
        }
    }
}
