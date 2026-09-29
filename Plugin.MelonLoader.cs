/*
 * Qubit Menu  Plugin.MelonLoader.cs
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


using MelonLoader;
using Qubit.Managers;

[assembly: MelonInfo(typeof(Qubit.PluginMelonLoader), Qubit.PluginInfo.Name, Qubit.PluginInfo.Version, "Qubit")]
[assembly: MelonOptionalDependencies("BepInEx")]
namespace Qubit
{
    public class PluginMelonLoader : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LogManager.SetLogger((level, msg) =>
            {
                switch (level)
                {
                    case Level.Error:
                        LoggerInstance.Error(msg);
                        break;
                    case Level.Warning:
                        LoggerInstance.Warning(msg);
                        break;
                    default:
                        LoggerInstance.Msg(msg);
                        break;
                }
            });

            Bootstrapper.Initialize();
        }

        public override void OnDeinitializeMelon() =>
            Menu.Main.UnloadMenu();
    }
}
