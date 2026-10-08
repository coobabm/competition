using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    /// <summary>
    /// abstract class that defines the base for any upgradable object
    /// </summary>
    public abstract class Upgradable : MonoBehaviour
    {
        protected virtual void Awake()
        {
            UpgradesTreeManager.OnUpgradeApplied += UpdateStats;
        }
        protected virtual void Start()
        {
            UpdateStats();
        }

        private void OnDestroy()
        {
            UpgradesTreeManager.OnUpgradeApplied -= UpdateStats;
        }

        protected abstract void UpdateStats();
    }
}