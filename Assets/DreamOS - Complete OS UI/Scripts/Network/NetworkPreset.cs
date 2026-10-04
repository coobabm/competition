using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class NetworkPreset : MonoBehaviour
    {
        // Resources
        public ButtonManager connectButton;
        public ButtonManager disconnectButton;
        public Image signalImage;
        public Image lockedImage;
        public TextMeshProUGUI titleText;
        public TMP_InputField passwordInput;
        public GameObject indicatorObject;
        [HideInInspector] public NetworkManager manager;
        [HideInInspector] public string networkID;
        [HideInInspector] public string password;

        // Helpers
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

        void OnEnable()
        {
            if (manager == null)
                return;

            if (manager.IsConnectedToNetwork(networkID) && !disconnectButton.gameObject.activeInHierarchy) { SetConnected(); }
            else if (!manager.IsConnectedToNetwork(networkID) && disconnectButton.gameObject.activeInHierarchy) { SetNotConnected(); }
        }

        public void SetConnected()
        {
            connectButton.gameObject.SetActive(false);
            disconnectButton.gameObject.SetActive(true);

            passwordInput.gameObject.SetActive(false);
            lockedImage.gameObject.SetActive(false);
            indicatorObject.gameObject.SetActive(true);
        }

        public void SetNotConnected()
        {
            connectButton.gameObject.SetActive(true);
            disconnectButton.gameObject.SetActive(false);
            indicatorObject.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(password)) { passwordInput.gameObject.SetActive(true); lockedImage.gameObject.SetActive(true); }
            else { passwordInput.gameObject.SetActive(false); lockedImage.gameObject.SetActive(false); }
        }

        public void Connect(bool bypassPasswordCheck = false)
        {
            if (bypassPasswordCheck || passwordInput.text == password)
            {
                if (manager.isConnected) { manager.networkItems[manager.currentNetworkIndex].preset.SetNotConnected(); }
                passwordInput.text = "";           
               
                SetConnected();

                manager.isConnected = true;
                manager.currentNetworkIndex = manager.GetNetworkIndex(networkID);
                manager.UpdateIndicators();

                DreamOSDataManager.WriteBooleanData(dataCat, "IsConnected", true);
                DreamOSDataManager.WriteStringData(dataCat, "CurrentNetwork", networkID);
            }

            else if (passwordInput.text != password) 
            {
                passwordInput.text = "";
                manager.PlayWrongPassword();
            }
        }

        public void Disconnect()
        {
            SetNotConnected();

            manager.isConnected = false;
            manager.UpdateIndicators(true);

            DreamOSDataManager.WriteBooleanData(dataCat, "IsConnected", false);
            DreamOSDataManager.WriteStringData(dataCat, "CurrentNetwork", null);
        }
    }
}