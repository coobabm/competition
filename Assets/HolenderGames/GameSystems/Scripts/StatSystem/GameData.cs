using System;
using System.Collections.Generic;

namespace HolenderGames.StatSystem
{
    /// <summary>
    /// Top level singleton class to manage and expose the main parameters of the gameplay
    /// and allow upgrading and notifying by events of new stats.
    /// </summary>
    [Serializable]
    public class GameData
    {
        // Singleton instance
        private static GameData _instance;

        public Dictionary<StatType, StatValue> NumericalStats = new();
        public HashSet<StatType> UnlocksStats = new();

        public static GameData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameData();
                }
                return _instance;
            }
        }

        // Private constructor to prevent external instantiation
        private GameData() { }
        public void Reset(StatDB statDB)
        {
            NumericalStats = new();
            UnlocksStats = new();

            foreach (var stat in statDB.BaseStats)
            {
                NumericalStats.Add(stat.type, new StatValue(stat.baseValue));
            }
        }

        public float GetStat(StatType statType)
        {
            if (NumericalStats.TryGetValue(statType, out var stat))
                return stat.FinalValue;
            return 0;
        }

        public bool IsUnlocked(StatType statType) => UnlocksStats.Contains(statType);

    }


}

