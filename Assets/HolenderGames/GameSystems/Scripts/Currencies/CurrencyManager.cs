using System.Collections.Generic;
using UnityEngine.Events;

namespace HolenderGames.Currencies
{
    // Helper class to handle a mockup game currency to be used in buying tree upgrades.
    // The class basically handles currency changes and invokes events to other systems to update their UI accordingly.
    public class CurrencyManager
    {

        private Dictionary<CurrencyType, UnityEvent<int>> eventDictionary;
        private Dictionary<CurrencyType, int> currencies = new Dictionary<CurrencyType, int>();

        private static CurrencyManager currencyManager;
        public static CurrencyManager Instance
        {
            get
            {
                if (currencyManager == null)
                {
                    currencyManager = new CurrencyManager();
                    currencyManager.Init();
                }

                return currencyManager;
            }
        }

        void Init()
        {
            if (eventDictionary == null)
            {
                eventDictionary = new Dictionary<CurrencyType, UnityEvent<int>>();
            }
        }

        public void AddCurrency(CurrencyType type, int amount = 1)
        {
            if (!currencies.ContainsKey(type))
            {
                currencies[type] = 0;
            }

            currencies[type] += amount;

            TriggerEvent(type);
        }

        public int GetCurrencyCount(CurrencyType type)
        {
            if (!currencies.ContainsKey(type))
            {
                currencies[type] = 0;
            }

            return currencies[type];
        }

        public void StartListening(CurrencyType currencyType, UnityAction<int> listener)
        {
            UnityEvent<int> thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(currencyType, out thisEvent))
            {
                thisEvent.AddListener(listener);
            }
            else
            {
                thisEvent = new UnityEvent<int>();
                thisEvent.AddListener(listener);
                Instance.eventDictionary.Add(currencyType, thisEvent);
            }
        }

        public void StopListening(CurrencyType currencyType, UnityAction<int> listener)
        {
            if (currencyManager == null) return;
            UnityEvent<int> thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(currencyType, out thisEvent))
            {
                thisEvent.RemoveListener(listener);
            }
        }

        public void TriggerEvent(CurrencyType currencyType)
        {
            UnityEvent<int> thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(currencyType, out thisEvent))
            {
                thisEvent?.Invoke(currencies[currencyType]);
            }
        }


    }
}