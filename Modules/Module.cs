/*
 * Qubit Menu  Modules/Module.cs
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

using Qubit.Backend;
using Qubit.Libraries.Core;
using System;

namespace Qubit.Modules
{
    public class Module
    {
        public string name = "-";
        public string displayName;

        public string RenderName => displayName ?? name;

        public string[] aliases;

        public string toolTip = "This button doesn't have a tooltip/tutorial.";

        public Action method;
        public Action postMethod;

        public Action enableMethod;
        public Action disableMethod;

        public bool enabled;
        public bool isTogglable = true;

        public bool hideFromArraylist;
        public bool label;
        public bool incremental;
        public bool detected;

        public bool legal;

        public string customBind;
        public string rebindKey;

        public bool isSetting;
        public object value;

        public Action onValueChanged;
        public Action<bool> cycleValue;

        public bool excludeFromSave;

        internal bool firstFrame = true;

        public T GetValue<T>()
        {
            if (value == null)
                return default;

            Type targetType = typeof(T);

            try
            {
                return targetType.IsEnum
                    ? value is string s
                        ? (T)Enum.Parse(targetType, s, ignoreCase: true)
                        : (T)Enum.ToObject(targetType, value)
                    : (T)Convert.ChangeType(value, targetType);
            }
            catch (Exception e)
            {
                Logs.Log($"GetValue<{targetType.Name}> failed: value was '{value}' (actual type: {value.GetType().Name}). {e.Message}");
                throw;
            }
        }
        public void SetValue<T>(T v) => value = v;

        public void SetEnabled(bool value, bool save = true)
        {
            enabled = value;

            if (save && !excludeFromSave)
                Preferences.SaveButton(this);
        }
    }
}