using UnityEngine;

namespace HolenderGames.Currencies
{
    /// <summary>
    /// Hold the IconsDB to match a CurrencyType to the relevant icon
    /// </summary>
    public class CurrencyIcons : MonoBehaviour
    {
        public static CurrencyIcons Instance { get; private set; }

        [SerializeField] private IconsDB icons;

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public Sprite GetIcon(CurrencyType currencyType)
        {
            return icons.GetIcon(currencyType);        }
    }

}
