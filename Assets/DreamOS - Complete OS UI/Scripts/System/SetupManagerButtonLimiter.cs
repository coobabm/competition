using UnityEngine;

namespace Michsky.DreamOS
{
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

        bool isNameOK = false;
        bool isLastNameOK = false;
        bool isPasswordOK = false;
        bool isPasswordRetypeOK = false;

        public enum VariableType { UserInformation, Privacy }

        void OnEnable()
        {
            if (setupManager == null || setupManager.userManager == null) { return; }
            if (disableButtonOnEnable) { targetButton.Interactable(false); }
        }

        void Update()
        {
            if (setupManager == null || setupManager.userManager == null)
                return;

            if (checkFor == VariableType.UserInformation && setupManager.userManager.nameOK && !isNameOK) { SetNameState(true); }
            else if (checkFor == VariableType.UserInformation && !setupManager.userManager.nameOK && isNameOK) { SetNameState(false); }
            else if (checkFor == VariableType.UserInformation && setupManager.userManager.lastNameOK && !isLastNameOK) { SetLastNameState(true); }
            else if (checkFor == VariableType.UserInformation && !setupManager.userManager.lastNameOK && isLastNameOK) { SetLastNameState(false); }
            else if (checkFor == VariableType.UserInformation && isNameOK && isLastNameOK && !targetButton.isInteractable) { AllowInformation(); }

            if (checkFor == VariableType.Privacy && setupManager.userManager.passwordOK && !isPasswordOK) { SetPasswordState(true); }
            else if (checkFor == VariableType.Privacy && !setupManager.userManager.passwordOK && isPasswordOK) { SetPasswordState(false); }
            else if (checkFor == VariableType.Privacy && setupManager.userManager.passwordRetypeOK && !isPasswordRetypeOK) { SetRetypePasswordState(true); }
            else if (checkFor == VariableType.Privacy && !setupManager.userManager.passwordRetypeOK && isPasswordRetypeOK) { SetRetypePasswordState(false); }
            else if (checkFor == VariableType.Privacy && isPasswordOK && isPasswordRetypeOK && !targetButton.isInteractable) { AllowPrivacy(); }
        }

        void SetNameState(bool value)
        {
            isNameOK = value;
            lastNameError.Hide();

            if (value == true) { nameError.Hide(); }
            else { targetButton.Interactable(false); nameError.Show(); }
        }

        void SetLastNameState(bool value)
        {
            isLastNameOK = value;

            if (value == true) { lastNameError.Hide(); }
            else { targetButton.Interactable(false); lastNameError.Show(); }
        }

        void SetPasswordState(bool value)
        {
            isPasswordOK = value;
            passCheckError.Hide();

            if (value == true) { passError.Hide(); }
            else { targetButton.Interactable(false); passError.Show(); }

        }
        void SetRetypePasswordState(bool value)
        {
            isPasswordRetypeOK = value;
            passError.Hide();

            if (value == true) { passCheckError.Hide(); }
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