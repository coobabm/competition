using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One row of the Wi-Fi list: signal icon, name, password box and a Connect / Disconnect button.
    /// Replaces DreamOS's NetworkPreset (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// Connecting and disconnecting writes "IsConnected" and "CurrentNetwork" to DreamOS's Network data, which
    /// <see cref="NetworkManager"/> restores on its next Awake.
    /// </summary>
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

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

        void OnEnable()
        {
            if (manager == null)
                return;

            // The window may have been closed while the connection changed: resync the row
            bool connected = manager.IsConnectedToNetwork(networkID);
            if (connected && !disconnectButton.gameObject.activeInHierarchy) { SetConnected(); }
            else if (!connected && disconnectButton.gameObject.activeInHierarchy) { SetNotConnected(); }
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

            // Only password-protected networks show the lock and the password box
            bool locked = !string.IsNullOrEmpty(password);
            passwordInput.gameObject.SetActive(locked);
            lockedImage.gameObject.SetActive(locked);
        }

        public void Connect(bool bypassPasswordCheck = false)
        {
            if (bypassPasswordCheck || passwordInput.text == password)
            {
                // Leave the network we were on first
                if (manager.isConnected) { manager.networkItems[manager.currentNetworkIndex].preset.SetNotConnected(); }
                passwordInput.text = "";

                SetConnected();

                manager.isConnected = true;
                manager.currentNetworkIndex = manager.GetNetworkIndex(networkID);
                manager.UpdateIndicators();

                DreamOSDataManager.WriteBooleanData(dataCat, "IsConnected", true);
                DreamOSDataManager.WriteStringData(dataCat, "CurrentNetwork", networkID);
            }
            else
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
