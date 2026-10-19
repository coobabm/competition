using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Binds a label (or avatar image) to a field of the signed-in user (2026-10-19). Replaces DreamOS's
    /// GetUserInfo: same GUID, serialized fields and public API.
    ///
    /// Put it on a TextMeshProUGUI to show the full name, first name, last name or password, or on an Image for the
    /// profile picture. It registers in <see cref="UserManager.guiList"/> so a change in the manager refreshes it.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/User/Get User Info")]
    public class GetUserInfo : MonoBehaviour
    {
        [Header("Resources")]
        public UserManager userManager;

        [Header("Settings")]
        public Reference getInformation;
        public bool updateOnEnable = true;
        public bool addToManager = true;

        TextMeshProUGUI textObject;
        Image imageObject;

        public enum Reference
        {
            FullName,
            FirstName,
            LastName,
            Password,
            ProfilePicture
        }

        void Awake()
        {
            if (userManager == null) { userManager = FindObjectsByType<UserManager>(FindObjectsSortMode.None)[0]; }

            if (addToManager) { userManager.guiList.Add(this); }

            if (getInformation == Reference.ProfilePicture) { imageObject = GetComponent<Image>(); }
            else { textObject = GetComponent<TextMeshProUGUI>(); }
        }

        void OnEnable()
        {
            if (updateOnEnable) { GetInformation(); }
        }

        public void GetInformation()
        {
            if (userManager == null) { return; }

            switch (getInformation)
            {
                case Reference.FullName: textObject.text = userManager.firstName + " " + userManager.lastName; break;
                case Reference.FirstName: textObject.text = userManager.firstName; break;
                case Reference.LastName: textObject.text = userManager.lastName; break;
                case Reference.Password: textObject.text = userManager.password; break;
                case Reference.ProfilePicture: imageObject.sprite = userManager.profilePicture; break;
            }
        }
    }
}
