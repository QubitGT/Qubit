/*
 * Qubit Menu  Core/ManagerRegistry.cs
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

using GorillaTagScripts;
using Qubit.Utilities;

namespace Qubit.Core
{
    public class ManagerRegistry
    {
        public class GhostReactor
        {
            public static GhostReactorManager GhostReactorManager
            {
                get => global::GhostReactor.instance.grManager;
            }

            public static GameEntityManager GameEntityManager
            {
                get => GameEntityManager.GetManagerForZone(global::GhostReactor.instance.zone);
            }
        }

        public class SuperInfection
        {
            public static SuperInfectionManager SuperInfectionManager
            {
                get => SuperInfectionManager.activeSuperInfectionManager;
            }

            public static global::SuperInfection ZoneSuperInfection
            {
                get => SuperInfectionManager.zoneSuperInfection;
            }

            public static GameEntityManager GameEntityManager
            {
                get => SuperInfectionManager.gameEntityManager;
            }
        }

        public class CustomMaps
        {
            public static CustomMapsGameManager CustomMapsGameManager
            {
                get => CustomMapsGameManager.instance;
            }

            public static GameEntityManager GameEntityManager
            {
                get => CustomMapsGameManager.gameEntityManager;
            }
        }

        public static GorillaGameManager GorillaGameManager
        {
            get => GorillaGameManager.instance;
        }

        public static GorillaTagManager GorillaTagManager
        {
            get => (GorillaTagManager)GorillaGameManager.instance;
        }

        public static GorillaAmbushManager GorillaAmbushManager
        {
            get => (GorillaAmbushManager)GorillaGameManager.instance;
        }

        public static GorillaPaintbrawlManager GorillaPaintbrawlManager
        {
            get => (GorillaPaintbrawlManager)GorillaGameManager.instance;
        }

        public static GorillaHuntManager GorillaHuntManager
        {
            get => (GorillaHuntManager)GorillaGameManager.instance;
        }

        public static GorillaGuardianManager GorillaGuardianManager
        {
            get => (GorillaGuardianManager)GorillaGameManager.instance;
        }

        public static GorillaPropHuntGameManager GorillaPropHuntGameManager
        {
            get => (GorillaPropHuntGameManager)GorillaGameManager.instance;
        }

        public static BuilderTable BuilderTable
        {
            get => GetBuilderTable();
        }

        public static LightningManager _lightningManager;
        public static LightningManager LightningManager
        {
            get
            {
                if (_lightningManager == null)
                    _lightningManager = GameObj.GetObject("Environment Objects/05Maze_PersistentObjects/2025_Halloween1_PersistentObjects/LightningManager").GetComponent<LightningManager>();

                return _lightningManager;
            }
            set => _lightningManager = value;
        }

        public static BuilderTable GetBuilderTable()
        {
            BuilderTable.TryGetBuilderTableForZone(References.LocalRig.zoneEntity.currentZone, out BuilderTable table);
            return table;
        }
    }
}
