using DG.Tweening;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HolenderGames.UpgradesTreePro
{
    // A class that handles viewing of the tooltip above each upgrade.
    public class UpgradeTooltip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtCost;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtCurrentLevel;
        [SerializeField] private TextMeshProUGUI txtMaxLevel;
        [SerializeField] private Image icon;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private RectTransform headerLayoutRoot;
        [SerializeField] private EffectView[] effectViews;

        private void OnEnable()
        {
            // Animation
            transform.DOKill();
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            transform.DOShakeRotation(0.3f,strength:20,vibrato:20);    
            transform.DOShakeScale(0.2f,strength:0.5f,vibrato:5);
        }

        // first time initializing of the constant parameters of the class: name, icon.
        public void InitTooltip(string upgradeName, Sprite icon, Sprite currencyIcon)
        {
            // Init the base values of the upgrade (doesn't change)
            txtName.text = upgradeName;
            this.icon.sprite = icon;
            this.currencyIcon.sprite = currencyIcon;
        }
        // Updating the tooltip whenever a new level of the upgrade is purchased.
        public void UpdateTooltip(string description, int currentLevel, int maxLevel, int cost, UpgradeEffect[] effects)
        {
            // Init and update the variables of the upgrade (changes every level)
            txtCost.text = cost.ToString();
            txtDescription.text = TextStylerManager.Instance.ApplyStyles(description);
            txtCurrentLevel.text = currentLevel.ToString();
            txtMaxLevel.text = maxLevel.ToString();

            if (currentLevel >= maxLevel)
            {
                txtCost.text = "MAX";
            }

            // Init effect views
            for (int i = 0; i < effectViews.Length; i++)
            {
                effectViews[i].gameObject.SetActive(false);
            }

            // On max level
            if (effects == null)
                return;

            int effectIdx = 0;
            foreach (UpgradeEffect effect in effects)
            {
                if (!effect.showOnTooltip)
                    continue;

                effectViews[effectIdx].gameObject.SetActive(true);
                effectViews[effectIdx].SetEffect(effect);
                effectIdx++;
                if (effectIdx >= effectViews.Length)
                    break;
            }


        }
    }
}