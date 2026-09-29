/*
 * Qubit Menu  Classes/Menu/ButtonCollider.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software
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

using Qubit.Managers;
using UnityEngine;
using static Qubit.Menu.Main;

namespace Qubit.Classes.Menu
{
    public class ButtonCollider : MonoBehaviour
    {
        public string relatedText;

        public bool incremental;
        public bool positive;

        public GameObject qubitRoot;
        public Transform qubitFront;
        public System.Action qubitAction;

        public void OnTriggerEnter(Collider collider)
        {
            if (qubitFront != null)
            {
                if (qubitRoot != menu || !qubitRoot.activeInHierarchy) return;
                Vector3 source = isOnPC && TPC != null ? TPC.transform.position : collider.bounds.center;
                if (Vector3.Dot(source - qubitFront.position, -qubitFront.forward) <= 0f) return;
            }
            if (!(Time.time > buttonCooldown) ||
                (collider != buttonCollider && collider != lKeyCollider && collider != rKeyCollider) || joystickMenu ||
                menu == null) return;
            Press();
        }

        public void Press(bool fromRay = false)
        {
            if (menu == null || Time.time <= buttonCooldown) return;
            if (qubitFront != null && (qubitRoot != menu || !qubitRoot.activeInHierarchy)) return;
            buttonCooldown = Time.time + 0.2f;
            if (qubitAction != null) { qubitAction(); return; }
            if (qubitFront != null) QubitDetails.Inspect(relatedText);
            if (relatedText != "Global Return") // HARDCODED GLOBAL RETURN CHECK (im gonna forget)
                SoundManager.Play(SoundManager.DefaultSounds["Button"], buttonText: relatedText);

            if (annoyingMode)
            {
                if (Random.Range(1, 5) == 2)
                {
                    NotificationManager.SendNotification("Error");
                    return;
                }
            }

            if (incremental)
                ToggleIncremental(relatedText, positive, ignoreForce: fromRay);
            else
                Toggle(relatedText, true, fromRay);
        }
    }
}
