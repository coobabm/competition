using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The "forgot password" dialog (2026-10-19). Replaces DreamOS's ResetPassword: same GUID, serialized fields and
    /// public API.
    ///
    /// Shows the user's security question (from the saved "User" data, or the inspector's system question when user
    /// creation is off). <see cref="ChangePassword"/> accepts a new password only if it fits the manager's length
    /// limits, matches its retype, and the security answer is right; otherwise it fires <see cref="onError"/>.
    /// </summary>
    public class ResetPassword : MonoBehaviour
    {
        [Header("Resources")]
        public UserManager userManager;
        public TextMeshProUGUI securityQuestion;
        public TMP_InputField securityAnswer;
        public TMP_InputField newPassword;
        public TMP_InputField newPasswordRetype;
        public ModalWindowManager modalManager;

        [Header("Events")]
        public UnityEvent onError = new UnityEvent();

        string tempSecAnswer;
        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;

        void Awake()
        {
            if (userManager == null) { userManager = FindObjectsByType<UserManager>(FindObjectsSortMode.None)[0]; }
        }

        void OnEnable()
        {
            if (!userManager.disableUserCreating && DreamOSDataManager.ContainsJsonKey(dataCat, "UserSecQuestion"))
            {
                securityQuestion.text = DreamOSDataManager.ReadStringData(dataCat, "UserSecQuestion");
                tempSecAnswer = DreamOSDataManager.ReadStringData(dataCat, "UserSecAnswer");
            }
            else
            {
                securityQuestion.text = userManager.systemSecurityQuestion;
                tempSecAnswer = userManager.systemSecurityAnswer;
            }
        }

        public void ChangePassword()
        {
            bool lengthOK = newPassword.text.Length >= userManager.minPasswordCharacter && newPassword.text.Length <= userManager.maxPasswordCharacter;

            if (lengthOK && newPassword.text == newPasswordRetype.text && securityAnswer.text == tempSecAnswer)
            {
                DreamOSDataManager.WriteStringData(dataCat, "UserPassword", newPassword.text);

                userManager.password = newPassword.text;
                userManager.hasPassword = true;

                modalManager.CloseWindow();

                newPassword.text = "";
                newPasswordRetype.text = "";
                securityAnswer.text = "";
            }
            else { onError.Invoke(); }
        }
    }
}
