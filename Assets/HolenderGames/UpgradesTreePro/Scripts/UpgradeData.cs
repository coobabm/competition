using HolenderGames.Currencies;
using System;
using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    // A Scriptable object to hold the data for each upgrade
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Game Data/Create UpgradeData", order = 0)]
    public class UpgradeData : ScriptableObject
    {
        [Serializable]
        public class UpgradeLevel
        {
            [TextArea(2, 5)]
            public string Description;
            public int Cost;
            public UpgradeEffect[] Effects;
        }
        public string UpgradeName;
        public Sprite Icon;
        public CurrencyType CurrencyType;

        public bool UnlockWhenParentMaxed = true;
        [Range(1, 7)]
        public int UnlockAtParentLevel = 1; // only relevant when UnlockWhenParentMaxed = false
        public UpgradeData ParentUpgrade; // The parent upgrade required in order to unlock this upgrade.

        public UpgradeLevel[] LevelsData;

        public override string ToString()
        {
            string desc;

            desc = name + ":\n";

            for (int i = 0; i < LevelsData.Length; i++)
            {
                desc += "Level" + (i + 1).ToString() + ": ";
                desc += LevelsData[i].Description.ToString();
                desc += $", (cost: {LevelsData[i].Cost} coins)";
                if (ParentUpgrade != null)
                {
                    desc += ", required upgrade: " + ParentUpgrade.name;

                }
                desc += "\n";
            }

            return desc;

        }
    }
}