using UnityEngine;
using UnityEngine.Rendering;

namespace Michsky.DreamOS
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class UIManagerBlur : MonoBehaviour
    {
        [SerializeField] private UIManager UIManagerAsset;

        void OnEnable()
        {
            if (GraphicsSettings.defaultRenderPipeline != null && gameObject.activeSelf) { gameObject.SetActive(false); }
            else if (UIManagerAsset != null && !UIManagerAsset.enableUIBlur && gameObject.activeSelf) { gameObject.SetActive(false); }
            else if (GraphicsSettings.defaultRenderPipeline == null && UIManagerAsset != null && UIManagerAsset.enableUIBlur && !gameObject.activeSelf) { gameObject.SetActive(true); }
        }
    }
}