using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/User/Get User Info")]
    public class GetUserInfo : MonoBehaviour
    {
        [Header("Resources")]
        public UserManager userManager;

        [Header("Settings")]
        public Reference getInformation;
        public bool updateOnEnable = true;
        public bool addToManager = true;

        // Helpers
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
            // Find User manager in the scene
#if UNITY_2023_2_OR_NEWER
            if (userManager == null) { userManager = FindObjectsByType<UserManager>(FindObjectsSortMode.None)[0]; }
#else
            if (userManager == null) { userManager = (UserManager)FindObjectsOfType(typeof(UserManager))[0]; }
#endif

            // Add component to be updated list
            if (addToManager) { userManager.guiList.Add(this); }

            // Get the component based on reference
            if (getInformation == Reference.ProfilePicture) { imageObject = gameObject.GetComponent<Image>(); }
            else { textObject = gameObject.GetComponent<TextMeshProUGUI>(); }
        }

        void OnEnable()
        {
            // Get user info on enable
            if (updateOnEnable) { GetInformation(); }
        }

        public void GetInformation()
        {
            if (userManager == null)
                return;

            if (getInformation == Reference.FullName) { textObject.text = userManager.firstName + " " + userManager.lastName; }
            else if (getInformation == Reference.FirstName) { textObject.text = userManager.firstName; }
            else if (getInformation == Reference.LastName) { textObject.text = userManager.lastName; }
            else if (getInformation == Reference.Password) { textObject.text = userManager.password; }
            else if (getInformation == Reference.ProfilePicture) { imageObject.sprite = userManager.profilePicture; }
        }
    }
}