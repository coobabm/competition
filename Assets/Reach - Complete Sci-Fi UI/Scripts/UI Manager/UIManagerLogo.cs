using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Reach
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Reach UI/UI Manager/UI Manager Logo")]
    public class UIManagerLogo : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private UIManager UIManagerAsset;
        private Image objImage;

        [Header("Settings")]
        [SerializeField] private LogoType logoType = LogoType.GameLogo;

        public enum LogoType { GameLogo, BrandLogo }

        void Awake()
        {
            this.enabled = true;

            if (UIManagerAsset == null) { UIManagerAsset = Resources.Load<UIManager>("Reach UI Manager"); }
            if (objImage == null) { objImage = GetComponent<Image>(); }
            if (UIManagerAsset.enableDynamicUpdate == false) { UpdateImage(); this.enabled = false; }
        }

        void Update()
        {
            if (UIManagerAsset == null) { return; }
            if (UIManagerAsset.enableDynamicUpdate == true) { UpdateImage(); }
        }


        void UpdateImage()
        {
            if (objImage == null)
                return;

            if (logoType == LogoType.GameLogo) { objImage.sprite = UIManagerAsset.gameLogo; }
            else if (logoType == LogoType.BrandLogo) { objImage.sprite = UIManagerAsset.brandLogo; }
        }
    }
}