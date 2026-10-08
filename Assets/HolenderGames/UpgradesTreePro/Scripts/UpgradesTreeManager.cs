using HolenderGames.Currencies;
using HolenderGames.StatSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    // The manager class to handle the Tree of upgrades.
    // The main purpose of the class is the handle the purchasing of upgrades and unlocking of child upgrades.
    // Use this class as the main point of handling the logic of your game when and upgrade is bought.
    public class UpgradesTreeManager : MonoBehaviour
    {
        public static event Action OnUpgradeApplied;
        private List<Upgrade> upgrades;
        private Dictionary<Upgrade, UpgradeView> views;


        private void Start()
        {
            upgrades = new List<Upgrade>();
            views = new Dictionary<Upgrade, UpgradeView>();
            ExtractTree();

        }

        private void ExtractTree()
        {
            var upgradeViews = GetComponentsInChildren<UpgradeView>(true);
            foreach (UpgradeView upgradeView in upgradeViews)
            {

                UpgradeData data = upgradeView.Data;
                if (data.LevelsData.Length == 0)
                {
                    Debug.LogError("Upgrade has no levels details " + data.name);
                    continue;
                }

                upgradeView.OnBuyUpgrade += OnBuyUpgrade;

                UpgradeState upgradeState =new UpgradeState();
                Upgrade upgrade = new Upgrade(data, upgradeState);
                upgrades.Add(upgrade);
                views.Add(upgrade, upgradeView);

                upgradeView.SetUpgradeHandler(upgrade);

            }
        }

        private void OnBuyUpgrade(Upgrade upgrade)
        {
            int cost = -upgrade.Cost; // caching the cost for this level before upgrading. 
            CurrencyType currencyType = upgrade.CurrencyType;
            ApplyUpgrade(upgrade);
            upgrade.LevelUp();
            CurrencyManager.Instance.AddCurrency(currencyType, cost); // applying cost after upgrading to refresh the tree UI
            //check if any children upgrades should be unlocked
            UnlockUpgrades(upgrade);

            OnUpgradeApplied?.Invoke();
        }

        private void UnlockUpgrades(Upgrade parentUpgrade)
        {
            bool isMaxed = parentUpgrade.IsMaxLevel;
            int current = parentUpgrade.CurrentLevel;

            foreach (var upgrade in upgrades)
            {
                var requiredParent = upgrade.Data.ParentUpgrade;
                if (requiredParent != null && requiredParent == parentUpgrade.Data)
                {
                    if (upgrade.Data.UnlockWhenParentMaxed && isMaxed)
                    {
                        upgrade.Unlock();
                        views[upgrade].Unlock();
                    }
                    else if (!upgrade.Data.UnlockWhenParentMaxed && upgrade.Data.UnlockAtParentLevel <= current)
                    {
                        upgrade.Unlock();
                        views[upgrade].Unlock();
                    }
                }
            }
        }

        public void ApplyUpgrade(Upgrade upgrade)
        {
            foreach (var effect in upgrade.Effects)
            {
                if (effect.operation == StatOperation.Unlock)
                {
                    GameData.Instance.UnlocksStats.Add(effect.statType);
                    continue;
                }

                if (GameData.Instance.NumericalStats.TryGetValue(effect.statType, out var stat))
                {
                    stat.ApplyEffect(effect.operation, effect.value);
                }
            }
        }
    }
}