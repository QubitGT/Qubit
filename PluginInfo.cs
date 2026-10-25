/*
 * Qubit Menu  PluginInfo.cs
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

namespace Qubit
{
    public class PluginInfo
    {
        public const string GUID = "org.qubit.gorillatag.qubitmenu";
        public const string Name = "Qubit";
        public const string Description = "Community powered mod menu for Gorilla Tag.";
        public const string BuildTimestamp = "2026-10-25T23:07:52Z";
        public const string Version = "5.0.2";

        public const string BaseDirectory =
#if LEGAL || LEGAL_DEBUG
            "Qubit/Legal";
#else
            "Qubit";
#endif

        public const string ServerAPI = "https://api.qubit.mom";
        public static string GitMaster = "https://github.com/QubitGT/Qubit";
        public static string DonateMaster = "https://patreon.com/QubitGT";
        public static string DiscordInvite = "https://discord.gg/qubit";
        public static string GeneratedFileFooter => $"Text file generated with Qubit || {GitMaster}";

        public const string ClientResourcePath = "Qubit.Resources.Client";
        public static string ServerResourcePath
        {
            get => GitMaster.Replace("github.com", "raw.githubusercontent.com") + "/master/Resources/Server";
        }

        public const string Logo = @"
┏┓  ┓ • 
┃┃┓┏┣┓┓╋
┗┻┗┻┗┛┗┗
";

#if DEBUG || LEGAL_DEBUG
        public static bool BetaBuild = true;
#else
        public static bool BetaBuild = false;
#endif
    }
}
