using DG.Tweening;
using HolenderGames.Currencies;
using HolenderGames.Sound;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HolenderGames.UpgradesTreePro
{
    // A class to handle the view of each upgrade.
    // It hold the scriptable object which contains the data of the upgrade and updates the UI accordingly.
    // This class also handles clicking (buying) the upgrade, updating the view when currency changes and update becomes affordable or not.
    public class UpgradeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<Upgrade> OnBuyUpgrade;

        [SerializeField] private UpgradeData upgradeData;
        [SerializeField] private Image bg;
        [SerializeField] private Image icon;
        [SerializeField] private bool isVisibleLocked = false;
        public UpgradeTooltip tooltip;
        private Button btnUpgrade;
        public UpgradeData Data { get { return upgradeData; } }
        private Upgrade upgrade;
        private Color iconColor;
        private void Awake()
        {
            btnUpgrade = GetComponent<Button>();
            btnUpgrade.onClick.AddListener(OnUpgradeClick);
            iconColor = icon.color;
        }

        void Start()
        {
            if (upgradeData == null)
            {
                Debug.LogError("Assign an UpgradeData scriptable object to prefab " + name);
                return;
            }
        }

        public void SetUpgradeHandler(Upgrade upgrade)
        {
            this.upgrade = upgrade;
            UpdateUI();
        }

        private void OnEnable()
        {
            CurrencyManager.Instance.StartListening(Data.CurrencyType, OnCurrencyChange);
            OnCurrencyChange(CurrencyManager.Instance.GetCurrencyCount(Data.CurrencyType));
        }

        private void OnDisable()
        {
            CurrencyManager.Instance.StopListening(Data.CurrencyType, OnCurrencyChange);
        }

        private void OnCurrencyChange()
        {
            OnCurrencyChange(CurrencyManager.Instance.GetCurrencyCount(Data.CurrencyType));
        }
        private void OnCurrencyChange(int currentCurrency)
        {
            if (upgrade == null)
            {
                return;
            }
            if (upgrade.IsMaxLevel || !upgrade.IsUnlocked)
                return;

            bg.color = currentCurrency < upgrade.Cost ? Color.red : Color.green;
            btnUpgrade.interactable = currentCurrency >= upgrade.Cost;
        }

        public void OnUpgradeClick()
        {
            if (upgradeData == null)
            {
                Debug.LogError("Assign an UpgradeData scriptable object to prefab " + name);
                return;
            }
            OnBuyUpgrade?.Invoke(upgrade);


            if (upgrade.IsMaxLevel)
            {
                btnUpgrade.interactable = false;
                bg.color = Color.yellow;
            }

            tooltip.UpdateTooltip(upgrade.Description, upgrade.CurrentLevel, upgrade.Data.LevelsData.Length, upgrade.Cost, upgrade.Effects);

            //animation
            bg.transform.DOKill();
            bg.transform.localScale = Vector3.one;
            bg.transform.DOShakeScale(0.3f, strength: 0.2f);

            SoundManager.Instance.PlaySound(GameSound.BuyUpgrade);
        }

        public void Unlock()
        {
            UpdateUI();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            SoundManager.Instance.PlaySound(GameSound.HoverOn);
            tooltip.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip.gameObject.SetActive(false);
        }
        public void UpdateUI()
        {
            tooltip.InitTooltip(upgrade.Data.UpgradeName, upgrade.Data.Icon, CurrencyIcons.Instance.GetIcon(upgrade.Data.CurrencyType));
            tooltip.UpdateTooltip(upgrade.Description, upgrade.CurrentLevel, upgrade.Data.LevelsData.Length, upgrade.Cost, upgrade.Effects);
            icon.sprite = upgrade.Icon;

            gameObject.SetActive(upgrade.IsUnlocked || isVisibleLocked);
            btnUpgrade.interactable = upgrade.IsUnlocked;

            if (upgrade.IsMaxLevel)
            {
                btnUpgrade.interactable = false;
                bg.color = Color.yellow;
            }
            else if (!upgrade.IsUnlocked)
            {
                bg.color = Color.grey;
            }

            icon.color = upgrade.IsUnlocked ? iconColor : Color.grey;

            
            // Update ui to show if upgrade is affordable
            OnCurrencyChange();
        }
    }
}