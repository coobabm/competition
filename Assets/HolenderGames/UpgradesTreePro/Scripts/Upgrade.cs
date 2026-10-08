using HolenderGames.Currencies;
using HolenderGames.StatSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    public class Upgrade
    {
        public UpgradeData Data { get; }
        public UpgradeState State { get; }

        public Upgrade(UpgradeData data, UpgradeState state)
        {
            Data = data;
            State = state;

            if(data.ParentUpgrade == null)
            {
                Unlock();
            }
        }

        public void LevelUp()
        {
            if (IsMaxLevel)
                return;

            State.Level++;
        }

        internal void Unlock()
        {
            State.IsUnlocked = true;
        }

        public int CurrentLevel => State.Level;
        public bool IsUnlocked => State.IsUnlocked;
        public bool IsMaxLevel { get { return CurrentLevel >= Data.LevelsData.Length; } }
        public int Cost { get { return Data.LevelsData[IsMaxLevel ? CurrentLevel - 1 : CurrentLevel].Cost; } }
        public string Description { get { return Data.LevelsData[IsMaxLevel ? CurrentLevel - 1 : CurrentLevel].Description.ToString(); } }
        public UpgradeEffect[] Effects { get { return IsMaxLevel ? null :Data.LevelsData[CurrentLevel].Effects; } }

        public CurrencyType CurrencyType => Data.CurrencyType;
        public Sprite Icon => Data.Icon;

        public List<string> GetEffectsLabels()
        {
            List<string> lables = new List<string>();

            if (IsMaxLevel)
                return lables;

            foreach (var effect in Effects)
            {
                if (!effect.showOnTooltip)
                    continue;

                if (GameData.Instance.NumericalStats.TryGetValue(effect.statType, out var stat))
                {
                    float current = stat.FinalValue;
                    float after = stat.EvaluateEffect(effect.operation, effect.value);

                    string label = effect.tooltipLabel + " " + current + " -> " + after;
                    lables.Add(label);
                }
            }

            return lables;
        }
    }
}

