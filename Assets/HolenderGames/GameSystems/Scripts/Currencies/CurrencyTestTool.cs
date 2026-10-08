using UnityEngine;
using UnityEngine.UI;

namespace HolenderGames.Currencies
{
    /// <summary>
    /// For testing: allows adding currencies to the players inventory with a UI button.
    /// </summary>
    public class CurrencyTestTool : MonoBehaviour
    {

        [SerializeField] private CurrencyType type;
        [SerializeField] private int amount;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(AddCurrency);
        }

        public void AddCurrency()
        {
            CurrencyManager.Instance.AddCurrency(type, amount);
        }
    }
}
