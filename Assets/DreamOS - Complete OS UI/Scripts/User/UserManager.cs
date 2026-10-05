using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Michsky.DreamOS
{
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
        public string systemSecurityQuestion = "Answer: DreamOS";
        public string systemSecurityAnswer = "DreamOS";

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

        // Helpers
        string noSecQuestionIndicator = "No security question set.";
        float cachedDesktopLength = 0.5f;
        float cachedLockScreenInLength = 0.5f;
        float cachedLockScreenOutLength = 0.5f;
        public bool isLockScreenOpen;
        bool isLoginScreenOpen;
        public bool userCreated;
        public bool hasPassword;
        [HideInInspector] public bool nameOK;
        [HideInInspector] public bool lastNameOK;
        [HideInInspector] public bool passwordOK;
        [HideInInspector] public bool passwordRetypeOK;
        public List<GetUserInfo> guiList = new List<GetUserInfo>();
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.User;

        void Awake()
        {
            // Get animator state lengths
            if (desktopScreen != null) { cachedDesktopLength = DreamOSInternalTools.GetAnimatorClipLength(desktopScreen, "Desktop_In") + 0.1f; }
            if (lockScreen != null)
            {
                cachedLockScreenOutLength = DreamOSInternalTools.GetAnimatorClipLength(lockScreen, "LockScreen_PasswordIn") + 0.1f;
                cachedLockScreenOutLength = DreamOSInternalTools.GetAnimatorClipLength(lockScreen, "LockScreen_PasswordOut") + 0.1f;
            }

            // Find required components if missing
#if UNITY_2023_2_OR_NEWER
            if (bootManager == null) { bootManager = FindObjectsByType<BootManager>(FindObjectsSortMode.None)[0]; }
            if (setupScreen == null) { setupScreen = FindObjectsByType<SetupManager>(FindObjectsSortMode.None)[0]; }
#else
            if (bootManager == null) { bootManager = (BootManager)FindObjectsOfType(typeof(BootManager))[0]; }
            if (setupScreen == null) { setupScreen = (SetupManager)FindObjectsOfType(typeof(SetupManager))[0]; }
#endif
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

                if (!DreamOSDataManager.ContainsJsonKey(dataCat, "UserCreated")) { userCreated = false; }
                else if (DreamOSDataManager.ReadBooleanData(dataCat, "UserCreated")) { userCreated = true; }
                else { userCreated = false; }

                if (userCreated)
                {
                    // Read user data
                    firstName = DreamOSDataManager.ReadStringData(dataCat, "UserFirstName");
                    lastName = DreamOSDataManager.ReadStringData(dataCat, "UserLastName");
                    password = DreamOSDataManager.ReadStringData(dataCat, "UserPassword");
                    secQuestion = DreamOSDataManager.ReadStringData(dataCat, "UserSecQuestion");
                    secAnswer = DreamOSDataManager.ReadStringData(dataCat, "UserSecAnswer");

                    // Check for profile picture data
                    if (!DreamOSDataManager.ContainsJsonKey(dataCat, "UserProfilePicture")) { ppIndex = 0; DreamOSDataManager.WriteIntData(dataCat, "UserProfilePicture", ppIndex); }
                    else { ppIndex = DreamOSDataManager.ReadIntData(dataCat, "UserProfilePicture"); }

                    // Check for password
                    if (string.IsNullOrEmpty(password)) { hasPassword = false; }
                    else { hasPassword = true; }
                }

                profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;
            }

            else
            {
                userCreated = true;

                // If password is null, change boolean
                if (string.IsNullOrEmpty(systemPassword)) { hasPassword = false; }
                else { hasPassword = true; }

                // Setting up the user details
                firstName = systemUsername;
                lastName = systemLastname;
                password = systemPassword;
                profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;
            }
        }

        public void InitializeProfilePictures()
        {
            if (ppParent == null || ppItem == null)
                return;

            foreach (Transform child in ppParent) { Destroy(child.gameObject); }
            for (int i = 0; i < ppLibrary.pictures.Count; ++i)
            {
                int index = i;

                GameObject go = Instantiate(ppItem, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(ppParent, false);
                go.name = ppLibrary.pictures[i].pictureID;

                ButtonManager wpButton = go.GetComponent<ButtonManager>();
                wpButton.SetIcon(ppLibrary.pictures[i].pictureSprite);
                wpButton.onClick.AddListener(delegate 
                { 
                    SetProfilePicture(index); 
                    wpButton.gameObject.GetComponentInParent<ModalWindowManager>().CloseWindow();
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
            // Set first name
            firstName = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserFirstName", firstName); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.FirstName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetFirstName(TMP_InputField tmpVar)
        {
            // Set first name
            firstName = tmpVar.text;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserFirstName", firstName); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.FirstName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetLastName(string textVar)
        {
            // Set last name
            lastName = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserLastName", lastName); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.LastName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetLastName(TMP_InputField tmpVar)
        {
            // Set last name
            lastName = tmpVar.text;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserLastName", lastName); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.LastName);
            UpdateUserInfoUI(GetUserInfo.Reference.FullName);
        }

        public void SetPassword(string textVar)
        {
            // Set pass
            password = textVar;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserPassword", password); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.Password);
        }

        public void SetPassword(TMP_InputField tmpVar)
        {
            // Set pass
            password = tmpVar.text;
            if (!disableUserCreating) { DreamOSDataManager.WriteStringData(dataCat, "UserPassword", password); }

            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.Password);
        }

        public void SetSecurityQuestion(string textVar)
        {
            if (string.IsNullOrEmpty(textVar)) { DreamOSDataManager.WriteStringData(dataCat, "UserSecQuestion", noSecQuestionIndicator); }
            else { DreamOSDataManager.WriteStringData(dataCat, "UserSecQuestion", textVar); }
        }
      
        public void SetSecurityQuestion(TMP_InputField tmpVar) 
        {
            if (string.IsNullOrEmpty(tmpVar.text)) { DreamOSDataManager.WriteStringData(dataCat, "UserSecQuestion", noSecQuestionIndicator); }
            else { DreamOSDataManager.WriteStringData(dataCat, "UserSecQuestion", tmpVar.text); }
        }
      
        public void SetSecurityAnswer(string textVar)
        { 
            DreamOSDataManager.WriteStringData(dataCat, "UserSecAnswer", textVar);
        }

        public void SetSecurityAnswer(TMP_InputField tmpVar)
        {
            DreamOSDataManager.WriteStringData(dataCat, "UserSecAnswer", tmpVar.text); 
        }

        public void SetProfilePicture(int pictureIndex)
        {
            // Set index and sprite
            ppIndex = pictureIndex;
            profilePicture = ppLibrary.pictures[ppIndex].pictureSprite;
            
            // Write data
            if (saveProfilePicture) { DreamOSDataManager.WriteIntData(dataCat, "UserProfilePicture", ppIndex); }
         
            // Update UI
            UpdateUserInfoUI(GetUserInfo.Reference.ProfilePicture);
        }

        public void UpdateUserInfoUI(GetUserInfo.Reference reference)
        {
            foreach (GetUserInfo gui in guiList) 
            {
                if (gui == null || gui.getInformation != reference) { continue; }
                else { gui.GetInformation(); }
            }
        }

        public void CreateUser()
        {
            userCreated = true;
            DreamOSDataManager.WriteBooleanData(dataCat, "UserCreated", userCreated);
            if (DreamOSDataManager.ContainsJsonKey(dataCat, "UserPassword")) { password = DreamOSDataManager.ReadStringData(dataCat, "UserPassword"); }

            if (string.IsNullOrEmpty(password)) { hasPassword = false; }
            else { hasPassword = true; }
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

            if (disableLockScreen && !hasPassword)
            {
                lockScreen.enabled = true;
                isLockScreenOpen = false;
                isLoginScreenOpen = false;
                lockScreen.gameObject.SetActive(false);
                onLogin.Invoke();
                ShowDesktop();
            }

            StopCoroutine("DisableLockScreenAnimator");
            StartCoroutine("DisableLockScreenAnimator");
        }

        public void OpenLockScreenPassword()
        {
            if (lockScreen == null) { return; }
            if (lockScreenBlur != null) { lockScreenBlur.BlurInAnim(); }

            isLoginScreenOpen = true;
            lockScreen.gameObject.SetActive(true);
            lockScreen.enabled = true;
            lockScreen.Play("Password In");

            StopCoroutine("DisableLockScreenAnimator");
            StartCoroutine("DisableLockScreenAnimator");
        }

        public void CloseLockScreen()
        {
            if (!isLockScreenOpen || lockScreen == null) { return; }
            if (lockScreenBlur != null) { lockScreenBlur.BlurOutAnim(); }

            lockScreen.enabled = true;
            isLockScreenOpen = false;
            isLoginScreenOpen = false;

            if (hasPassword) { lockScreen.Play("Password Out"); }
            else { lockScreen.Play("Out"); }

            StopCoroutine("DisableLockScreenAnimator");
            StartCoroutine("DisableLockScreen");
        }

        public void ShowDesktop()
        {
            desktopScreen.gameObject.SetActive(true);
            desktopScreen.enabled = true;
            desktopScreen.Play("In");

            StopCoroutine("DisableDesktopAnimator");
            StartCoroutine("DisableDesktopAnimator");
        }

        public void HideDesktop()
        {
            desktopScreen.enabled = true;
            desktopScreen.Play("Out");

            StopCoroutine("DisableDesktopAnimator");
            StartCoroutine("DisableDesktopAnimator");
        }

        public void AnimateLockScreen()
        {
            if (isLoginScreenOpen)
                return;

            StopCoroutine("DisableDesktopAnimator");

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
            else if (lockScreenPassword.text == password)
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

        IEnumerator DisableDesktopAnimator()
        {
            yield return new WaitForSeconds(cachedDesktopLength);
            desktopScreen.enabled = false;
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