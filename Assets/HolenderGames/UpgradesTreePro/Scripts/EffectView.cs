using DG.Tweening;
using HolenderGames.Currencies;
using HolenderGames.Sound;
using HolenderGames.StatSystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HolenderGames.UpgradesTreePro
{
    // A class to handle the view of each effect.
    // The view shows the current stat value and the evaluated value after the effect is applied
    // for example: Damage 20 -> 25
    public class EffectView : MonoBehaviour
    {
      

        [SerializeField] private TextMeshProUGUI txtEffect;

      
        public void SetEffect(UpgradeEffect effect)
        {
           

            if (GameData.Instance.NumericalStats.TryGetValue(effect.statType, out var stat))
            {
                float current = stat.FinalValue;
                float after = stat.EvaluateEffect(effect.operation, effect.value);

                string label = effect.tooltipLabel + " " + current.ToString("0.##") + " -> " + after.ToString("0.##");
                txtEffect.SetText(label);
            }
        }
       
    }
}