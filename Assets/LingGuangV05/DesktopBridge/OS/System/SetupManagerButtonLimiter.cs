using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Keeps a setup step's Next button locked until its fields are valid (2026-10-19). Replaces DreamOS's
    /// SetupManagerButtonLimiter: same GUID, serialized fields and public API.
    ///
    /// Watches the nameOK/lastNameOK (information step) or passwordOK/passwordRetypeOK (privacy step) flags that
    /// <see cref="SetupManager"/> maintains, shows or hides the matching <see cref="SystemErrorPopup"/> when a flag
    /// flips, and re-enables the button once both flags of the step are good. Like the original it handles at most one
    /// change per frame (the checks are an else-if ladder).
    /// </summary>
    public class SetupManagerButtonLimiter : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private SetupManager setupManager;
        [SerializeField] private ButtonManager targetButton;
        [SerializeField] private SystemErrorPopup nameError;
        [SerializeField] private SystemErrorPopup lastNameError;
        [SerializeField] private SystemErrorPopup passError;
        [SerializeField] private SystemErrorPopup passCheckError;

        [Header("Settings")]
        [SerializeField] private bool disableButtonOnEnable = true;
        [SerializeField] private VariableType checkFor = VariableType.UserInformation;

        bool isNameOK;
        bool isLastNameOK;
        bool isPasswordOK;
        bool isPasswordRetypeOK;

        public enum VariableType { UserInformation, Privacy }

        void OnEnable()
        {
            if (setupManager == null || setupManager.userManager == null) { return; }
            if (disableButtonOnEnable) { targetButton.Interactable(false); }
        }

        void Update()
        {
            if (setupManager == null || setupManager.userManager == null) { return; }
            UserManager user = setupManager.userManager;

            if (checkFor == VariableType.UserInformation)
            {
                if (user.nameOK && !isNameOK) { SetNameState(true); }
                else if (!user.nameOK && isNameOK) { SetNameState(false); }
                else if (user.lastNameOK && !isLastNameOK) { SetLastNameState(true); }
                else if (!user.lastNameOK && isLastNameOK) { SetLastNameState(false); }
                else if (isNameOK && isLastNameOK && !targetButton.isInteractable) { AllowInformation(); }
            }

            if (checkFor == VariableType.Privacy)
            {
                if (user.passwordOK && !isPasswordOK) { SetPasswordState(true); }
                else if (!user.passwordOK && isPasswordOK) { SetPasswordState(false); }
                else if (user.passwordRetypeOK && !isPasswordRetypeOK) { SetRetypePasswordState(true); }
                else if (!user.passwordRetypeOK && isPasswordRetypeOK) { SetRetypePasswordState(false); }
                else if (isPasswordOK && isPasswordRetypeOK && !targetButton.isInteractable) { AllowPrivacy(); }
            }
        }

        void SetNameState(bool value)
        {
            isNameOK = value;
            lastNameError.Hide();

            if (value) { nameError.Hide(); }
            else { targetButton.Interactable(false); nameError.Show(); }
        }

        void SetLastNameState(bool value)
        {
            isLastNameOK = value;

            if (value) { lastNameError.Hide(); }
            else { targetButton.Interactable(false); lastNameError.Show(); }
        }

        void SetPasswordState(bool value)
        {
            isPasswordOK = value;
            passCheckError.Hide();

            if (value) { passError.Hide(); }
            else { targetButton.Interactable(false); passError.Show(); }
        }

        void SetRetypePasswordState(bool value)
        {
            isPasswordRetypeOK = value;
            passError.Hide();

            if (value) { passCheckError.Hide(); }
            else { targetButton.Interactable(false); passCheckError.Show(); }
        }

        void AllowInformation()
        {
            targetButton.Interactable(true);
            nameError.Hide();
            lastNameError.Hide();
        }

        void AllowPrivacy()
        {
            targetButton.Interactable(true);
            passError.Hide();
            passCheckError.Hide();
        }
    }
}
