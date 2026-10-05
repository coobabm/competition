using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Michsky.DreamOS
{
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

        // Helpers
        string tempSecAnswer;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;

        void Awake()
        {
#if UNITY_2023_2_OR_NEWER
            if (userManager == null) { userManager = FindObjectsByType<UserManager>(FindObjectsSortMode.None)[0]; }
#else
            if (userManager == null) { userManager = (UserManager)FindObjectsOfType(typeof(UserManager))[0]; }
#endif
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
            if (newPassword.text.Length >= userManager.minPasswordCharacter && newPassword.text.Length <= userManager.maxPasswordCharacter
                && newPassword.text == newPasswordRetype.text && securityAnswer.text == tempSecAnswer)
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