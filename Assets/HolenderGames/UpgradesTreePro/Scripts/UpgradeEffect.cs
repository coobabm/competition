using HolenderGames.StatSystem;

namespace HolenderGames.UpgradesTreePro
{
    /// <summary>
    /// Attached to every upgrade in the tree to handle the effect of that upgrade on a specific stat
    /// </summary>
    [System.Serializable]
    public struct UpgradeEffect
    {
        public StatType statType;
        public StatOperation operation;
        public float value;
        public bool showOnTooltip;
        public string tooltipLabel;
    }
}

