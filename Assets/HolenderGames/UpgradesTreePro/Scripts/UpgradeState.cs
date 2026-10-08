
namespace HolenderGames.UpgradesTreePro
{
    [System.Serializable]
    public class UpgradeState
    {
        public int Level = 0;
        public bool IsUnlocked = false;

        public UpgradeState()
        {
                
        }
        public UpgradeState(int Level, bool IsUnlocked)
        {
            this.Level = Level;
            this.IsUnlocked = IsUnlocked;
        }
    }
}

