using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HolenderGames.Currencies
{
    /// <summary>
    /// Visualising and updating the collectible current amount in the UI.
    /// </summary>
    public class CurrencyViewer : MonoBehaviour
    {

        [SerializeField] private CurrencyType type;
        [SerializeField] private Image icon;
        private TextMeshProUGUI txtAmount;
        private void Awake()
        {
            txtAmount = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Start()
        {
            icon.sprite = CurrencyIcons.Instance.GetIcon(type);
        }


        private void OnEnable()
        {
            CurrencyManager.Instance.StartListening(type, onCollect);
            onCollect(CurrencyManager.Instance.GetCurrencyCount(type));
        }
        private void OnDisable()
        {
            CurrencyManager.Instance.StopListening(type, onCollect);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }


        private void onCollect(int amount)
        {
            txtAmount.text = amount.ToString();

            //animation
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOShakeScale(0.2f, strength: 0.2f).onComplete = () => transform.localScale = Vector3.one;
        }
       
    }

}
