/*
 * Qubit Menu  Core/AddOnStartAttribute.cs
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

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Qubit.Core
{
    /// <summary>
    /// Attaches the MonoBehaviour to the menu object on startup.
    /// <paramref name="condition"/> must be a compile-time constant, e.g. [AddOnStart(ServerData.ServerDataEnabled)].
    /// Set <see cref="AttachTo"/> to add it to the GameObject of an existing component instead of the menu object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AddOnStartAttribute : Attribute
    {
        public bool Condition { get; }
        public Type AttachTo { get; set; }

        public AddOnStartAttribute(bool condition = true) =>
            Condition = condition;

        /// <summary>
        /// Adds every marked component. Run with <paramref name="attached"/> false first for the menu object ones,
        /// then true once the components named by <see cref="AttachTo"/> exist.
        /// </summary>
        public static void Apply(GameObject host, bool attached)
        {
            Type[] types;
            try
            {
                types = typeof(AddOnStartAttribute).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).ToArray(); // a missing game type must not stop startup
            }

            foreach (Type type in types)
            {
                AddOnStartAttribute attribute = type.GetCustomAttribute<AddOnStartAttribute>();
                if (attribute == null || !attribute.Condition || (attribute.AttachTo != null) != attached)
                    continue;

                GameObject target = attached
                    ? (UnityEngine.Object.FindObjectOfType(attribute.AttachTo) as Component)?.gameObject
                    : host;

                if (target != null && target.GetComponent(type) == null)
                    target.AddComponent(type);
            }
        }
    }
}
