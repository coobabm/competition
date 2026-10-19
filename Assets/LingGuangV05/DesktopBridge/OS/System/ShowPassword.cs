using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Show/hide toggle for a password field (2026-10-19). Replaces DreamOS's ShowPassword: same GUID, serialized
    /// fields and public API.
    ///
    /// The field is masked whenever the object is enabled. <see cref="TogglePassword"/> flips between masked and plain
    /// text and shows whichever of the two icon objects offers the opposite action.
    /// </summary>
    public class ShowPassword : MonoBehaviour
    {
        [Header("Resources")]
        public TMP_InputField inputField;
        public GameObject showObject;
        public GameObject hideObject;

        void OnEnable()
        {
            HidePassword();
        }

        public void TogglePassword()
        {
            if (inputField == null) { return; }

            if (inputField.contentType == TMP_InputField.ContentType.Standard) { SetMasked(true); }
            else if (inputField.contentType == TMP_InputField.ContentType.Password) { SetMasked(false); }
        }

        public void HidePassword()
        {
            if (inputField != null && inputField.contentType == TMP_InputField.ContentType.Standard) { SetMasked(true); }
        }

        void SetMasked(bool masked)
        {
            inputField.contentType = masked ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            inputField.ForceLabelUpdate();

            showObject.SetActive(masked);
            hideObject.SetActive(!masked);
        }
    }
}
