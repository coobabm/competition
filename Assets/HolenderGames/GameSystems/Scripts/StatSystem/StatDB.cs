using System.Collections.Generic;
using UnityEngine;

namespace HolenderGames.StatSystem
{
    /// <summary>
    /// A db to hold all the initial base stats of the game
    /// </summary>
    [CreateAssetMenu(menuName = "Game Data/Stat Database")]
    public class StatDB : ScriptableObject
    {
        [System.Serializable]
        public class StatEntry
        {
            public StatType type;
            public float baseValue;
        }

        [Header("Base Stats")]
        public List<StatEntry> BaseStats = new();
    }

}

