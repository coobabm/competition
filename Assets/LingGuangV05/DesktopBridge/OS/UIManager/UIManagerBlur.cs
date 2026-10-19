using UnityEngine;
using UnityEngine.Rendering;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Switches a blur object on or off to match the UIManager's blur setting. The blur only exists in the built-in
    /// render pipeline, so with any scriptable pipeline assigned it is always turned off. Replaces DreamOS's
    /// UIManagerBlur (2026-10-19): same GUID and serialized fields.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class UIManagerBlur : MonoBehaviour
    {
        [SerializeField] private UIManager UIManagerAsset;

        void OnEnable()
        {
            bool usesScriptablePipeline = GraphicsSettings.defaultRenderPipeline != null;

            if (usesScriptablePipeline && gameObject.activeSelf) { gameObject.SetActive(false); }
            else if (UIManagerAsset != null && !UIManagerAsset.enableUIBlur && gameObject.activeSelf) { gameObject.SetActive(false); }
            else if (!usesScriptablePipeline && UIManagerAsset != null && UIManagerAsset.enableUIBlur && !gameObject.activeSelf) { gameObject.SetActive(true); }
        }
    }
}
