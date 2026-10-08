using HolenderGames.StatSystem;
using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    /// <summary>
    /// Attached to a scene object to be unlocked by the relevant unlockable stat upgrade.
    /// </summary>
    public class Unlockable : Upgradable
    {
        [SerializeField] private StatType unlockStat;
        [SerializeField] private GameObject unlockObject;

        protected override void UpdateStats()
        {
            unlockObject.SetActive(GameData.Instance.IsUnlocked(unlockStat));
        }
    }
}