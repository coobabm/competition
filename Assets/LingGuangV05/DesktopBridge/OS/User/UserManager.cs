using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's user account, lock screen and sign-in (2026-10-19). Replaces DreamOS's UserManager: same GUID,
    /// serialized fields and public API, so the boot/setup flow, the settings and lock-screen widgets, the
    /// <see cref="GetUserInfo"/> labels and the apps that read the user's name keep binding unchanged.
    ///
    /// The account lives in DreamOS's "User" data (UserCreated, UserFirstName, UserLastName, UserPassword,
    /// UserSecQuestion, UserSecAnswer, UserProfilePicture). With <see cref="disableUserCreating"/> set there is no
    /// setup: the fixed "system" user from the inspector is used and nothing is written. The lock screen has an idle
    /// state and a password state; <see cref="Login"/> compares the typed text with the password, and
    /// <see cref="WipeUserData"/> clears every data category and reboots.
    /// </summary>
    public class UserManager : MonoBehaviour
    {
        // Resources
        public BootManager bootManager;
        public SetupManager setupScreen;
        public Animator desktopScreen;
        public Animator lockScreen;
        public TMP_InputField lockScreenPassword;
        public ProfilePictureLibrary ppLibrary;
        [SerializeField] private GameObject ppItem;
        [SerializeField] private Transform ppParent;
        [SerializeField] private SystemErrorPopup wrongPassError;
        [SerializeField] private UIBlur lockScreenBlur;

        // Content
        [Range(1, 20)] public int minNameCharacter = 1;
        [Range(1, 20)] public int maxNameCharacter = 14;

        [Range(1, 20)] public int minPasswordCharacter = 4;
        [Range(1, 20)] public int maxPasswordCharacter = 16;

        // Events
        public UnityEvent onLogin = new UnityEvent();
        public UnityEvent onLock = new UnityEvent();
        public UnityEvent onWrongPassword = new UnityEvent();

        // Custom User Info
        public string systemUsername = "Admin";
        public string systemLastname = "";
        public string systemPassword = "1234";
        public string systemSecurityQuestion = "Answer: hongmengos";
        public string systemSecurityAnswer = "hongmengos";

        // Settings
        public bool disableUserCreating = false;
        public bool disableLockScreen = false;
        public bool saveProfilePicture = true;
        public int ppIndex;

        // User variables
        public string firstName;
        public string lastName;
        public string password;
        public string secQuestion;
        public string secAnswer;
        public Sprite profilePicture;

        // State
        public bool isLockScreenOpen;
        public bool userCreated;
        public bool hasPassword;
        [HideInInspector] public bool nameOK;
        [HideInInspector] public bool lastNameOK;
        [HideInInspector] public bool passwordOK;
        [HideInInspector] public bool passwordRetypeOK;
        public List<GetUserInfo> guiList = new List<GetUserInfo>();

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;
        const string NoSecQuestionIndicator = "No security question set.";

        // DreamOS read the lock screen's "In" length into the Out field first and then overwrote it, so the
        // animator-off delay after "In" has always been this 0.5 s default; only the Out length is measured.
        float cachedLockScreenInLength = 0.5f;
        float cachedLockScreenOutLength = 0.5f;
        bool isLoginScreenOpen;

        void Awake()
        {
            if (lockScreen != null)
            {
                cachedLockScreenOutLength = DreamOSInternalTools.GetAnimatorClipLength(lockScreen, "LockScreen_PasswordOut") + 0.1f;
            }

            if (bootManager == null) { bootManager = FindObjectsByType<BootManager>(FindObjectsSortMode.None)[0]; }
            if (setupScreen == null) { setupScreen = FindObjectsByType<SetupManager>(FindObjectsSortMode.None)[0]; }
        }

        void OnEnable()
        {
            Initialize();
            InitializeProfilePictures();
        }

        public void Initialize()
        {
            if (!disableUserCreating)
            {
                nameOK = false;
                lastNameOK = false;
                passwordOK = false;
                passwordRetypeOK = false;

                userCreated = DreamOSDataManager.ContainsJsonKey(dataCat, "UserCreated") && DreamOSDataManager.ReadBooleanData(dataCat, "UserCreated");

                if (userCreated)
                {
                    firstName = DreamOSDataManager.ReadStringData(dataCat, "UserFirstName");
                    lastName = DreamOSDataManager.ReadStringData(dataCat, "UserLastName");
                    password = DreamOSDataManager.ReadStringData(dataCat, "UserPassword");
                    secQuestion = DreamOSDataManager.ReadStringData(dataCat, "UserSecQuestion");
                    secAnswer = DreamOSDataManager.ReadStringData(dataCat, "UserSecAnswer");

                    if (!DreamOSDataManager.ContainsJsonKey(dataCat, "UserProfilePicture")) { ppIndex = 0; DreamOSDataManager.WriteIntData(dataCat, "UserProfilePicture", ppIndex); }
                    else { ppIndex = DreamOSDataManager.ReadIntData(dataCat, "UserProfilePicture"); }

                    hasPassword = !string.IsNullOrEmpty(password);
                }

                profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;
            }
            else
            {
                userCreated = true;
                hasPassword = !string.IsNullOrEmpty(systemPassword);

                firstName = systemUsername;
                lastName = systemLastname;
                password = systemPassword;
                profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;
            }
        }

        public void InitializeProfilePictures()
        {
            if (ppParent == null || ppItem == null) { return; }

            foreach (Transform child in ppParent) { Destroy(child.gameObject); }

            for (int i = 0; i < ppLibrary.pictures.Count; ++i)
            {
                int index = i;

                GameObject entry = Instantiate(ppItem, Vector3.zero, Quaternion.identity);
                entry.transform.SetParent(ppParent, false);
                entry.name = ppLibrary.pictures[i].pictureID;

                ButtonManager pictureButton = entry.GetComponent<ButtonManager>();
                pictureButton.SetIcon(ppLibrary.pictures[i].pictureSprite);
                pictureButton.onClick.AddListener(delegate
                {
                    SetProfilePicture(index);
                    pictureButton.gameObject.GetComponentInParent<ModalWindowManager>().CloseWindow();
                });
            }
        }

        public void SetUserCreated(bool value)
        {
            userCreated = value;
            DreamOSDataManager.WriteBooleanData(dataCat, "UserCreated", value);
        }

        public void SetFirstName(string textVar)
        {
            firstName = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserFirstName", firstName); }

            UpdateUserInfoUI(GetUserInfo.Reference.FirstName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetFirstName(TMP_InputField tmpVar) { SetFirstName(tmpVar.text); }

        public void SetLastName(string textVar)
        {
            lastName = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserLastName", lastName); }

            UpdateUserInfoUI(GetUserInfo.Reference.LastName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetLastName(TMP_InputField tmpVar) { SetLastName(tmpVar.text); }

        public void SetPassword(string textVar)
        {
            password = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserPassword", password); }

            UpdateUserInfoUI(GetUserInfo.Reference.Password);
        }

        public void SetPassword(TMP_InputField tmpVar) { SetPassword(tmpVar.text); }

        public void SetSecurityQuestion(string textVar)
        {
            DreamOSDataManager.WriteStringData(dataCat, "UserSecQuestion", string.IsNullOrEmpty(textVar) ? NoSecQuestionIndicator : textVar);
        }

        public void SetSecurityQuestion(TMP_InputField tmpVar) { SetSecurityQuestion(tmpVar.text); }

        public void SetSecurityAnswer(string textVar)
        {
            DreamOSDataManager.WriteStringData(dataCat, "UserSecAnswer", textVar);
        }

        public void SetSecurityAnswer(TMP_InputField tmpVar) { SetSecurityAnswer(tmpVar.text); }

        public void SetProfilePicture(int pictureIndex)
        {
            ppIndex = pictureIndex;
            profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;

            if (saveProfilePicture) { DreamOSDataManager.WriteIntData(dataCat, "UserProfilePicture", ppIndex); }

            UpdateUserInfoUI(GetUserInfo.Reference.ProfilePicture);
        }

        public void UpdateUserInfoUI(GetUserInfo.Reference reference)
        {
            foreach (GetUserInfo label in guiList)
            {
                if (label == null || label.getInformation != reference) { continue; }
                label.GetInformation();
            }
        }

        public void CreateUser()
        {
            userCreated = true;
            DreamOSDataManager.WriteBooleanData(dataCat, "UserCreated", userCreated);
            if (DreamOSDataManager.ContainsJsonKey(dataCat, "UserPassword")) { password = DreamOSDataManager.ReadStringData(dataCat, "UserPassword"); }

            hasPassword = !string.IsNullOrEmpty(password);
        }

        public void LockSystem()
        {
            if (lockScreenBlur != null) { lockScreenBlur.BlurOutAnim(); }
            if (lockScreen != null) { OpenLockScreen(); }

            HideDesktop();
            onLock.Invoke();
        }

        public void OpenLockScreen()
        {
            if (isLockScreenOpen || lockScreen == null) { return; }
            if (lockScreenBlur != null) { lockScreenBlur.BlurOutAnim(); }

            lockScreen.gameObject.SetActive(true);
            lockScreen.enabled = true;
            lockScreen.Play("In");

            isLockScreenOpen = true;

            // No lock screen wanted and nothing to type: go straight to the desktop.
            if (disableLockScreen && !hasPassword)
            {
                lockScreen.enabled = true;
                isLockScreenOpen = false;
                isLoginScreenOpen = false;
                lockScreen.gameObject.SetActive(false);
                onLogin.Invoke();
                ShowDesktop();
            }

            RestartLockScreenAnimatorTimer();
        }

        public void OpenLockScreenPassword()
        {
            if (lockScreen == null) { return; }
            if (lockScreenBlur != null) { lockScreenBlur.BlurInAnim(); }

            isLoginScreenOpen = true;
            lockScreen.gameObject.SetActive(true);
            lockScreen.enabled = true;
            lockScreen.Play("Password In");

            RestartLockScreenAnimatorTimer();
        }

        public void CloseLockScreen()
        {
            if (!isLockScreenOpen || lockScreen == null) { return; }
            if (lockScreenBlur != null) { lockScreenBlur.BlurOutAnim(); }

            lockScreen.enabled = true;
            isLockScreenOpen = false;
            isLoginScreenOpen = false;

            lockScreen.Play(hasPassword ? "Password Out" : "Out");

            StopCoroutine(nameof(DisableLockScreenAnimator));
            StartCoroutine(nameof(DisableLockScreen));
        }

        public void ShowDesktop()
        {
            desktopScreen.gameObject.SetActive(true);
            desktopScreen.enabled = true;
            desktopScreen.Play("In", 0, 0);

            StopCoroutine(nameof(DisableDesktopAnimator));
            StartCoroutine(nameof(DisableDesktopAnimator), "In");
        }

        public void HideDesktop()
        {
            desktopScreen.enabled = true;
            desktopScreen.Play("Out", 0, 0);

            StopCoroutine(nameof(DisableDesktopAnimator));
            StartCoroutine(nameof(DisableDesktopAnimator), "Out");
        }

        public void AnimateLockScreen()
        {
            if (isLoginScreenOpen) { return; }

            StopCoroutine(nameof(DisableDesktopAnimator));

            if (hasPassword) { OpenLockScreenPassword(); }
            else
            {
                CloseLockScreen();
                ShowDesktop();
                onLogin.Invoke();
            }
        }

        public void Login()
        {
            if (lockScreenPassword.text != password) { onWrongPassword.Invoke(); wrongPassError.Show(); }
            else
            {
                CloseLockScreen();
                ShowDesktop();
                onLogin.Invoke();
            }
        }

        public void WipeUserData()
        {
            nameOK = false;
            lastNameOK = false;
            passwordOK = false;
            passwordRetypeOK = false;

            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.Apps);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.User);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.System);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.DateAndTime);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.Network);
            DreamOSDataManager.DeleteDataCategory(DreamOSDataManager.DataCategory.Widgets);

            bootManager.Reboot();
        }

        void RestartLockScreenAnimatorTimer()
        {
            StopCoroutine(nameof(DisableLockScreenAnimator));
            StartCoroutine(nameof(DisableLockScreenAnimator));
        }

        IEnumerator DisableDesktopAnimator(string expectedState)
        {
            // A wall-clock delay can resume before the Animator has evaluated its
            // last frame after a slow startup. That leaves the whole desktop faint.
            // Let the requested In/Out state actually finish before freezing it.
            yield return null;
            // Requests made at EndOfFrame must pass a fresh Animator evaluation;
            // otherwise the previous completed state can be mistaken for this request.
            yield return new WaitForEndOfFrame();
            while (desktopScreen != null && desktopScreen.isActiveAndEnabled)
            {
                var state = desktopScreen.GetCurrentAnimatorStateInfo(0);
                if (!desktopScreen.IsInTransition(0) && state.IsName(expectedState) && state.normalizedTime >= 1)
                {
                    desktopScreen.enabled = false;
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator DisableLockScreenAnimator()
        {
            yield return new WaitForSeconds(cachedLockScreenInLength);
            lockScreen.enabled = false;
        }

        IEnumerator DisableLockScreen()
        {
            yield return new WaitForSeconds(cachedLockScreenOutLength);
            lockScreen.gameObject.SetActive(false);
        }
    }
}
