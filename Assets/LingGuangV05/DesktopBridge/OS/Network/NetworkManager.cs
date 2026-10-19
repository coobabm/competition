using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's Wi-Fi state: the list of networks, which one is joined, and the signal icons shown in the
    /// taskbar. The browser reads <c>isConnected</c>, <c>dynamicNetwork</c>, <c>networkItems[].networkSpeed</c>,
    /// <c>currentNetworkIndex</c> and <c>defaultSpeed</c> to decide how fast pages and downloads load.
    /// Replaces DreamOS's NetworkManager (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// The connection is saved in DreamOS's Network data as "IsConnected" and "CurrentNetwork" (the network ID).
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        // List
        public List<NetworkItem> networkItems = new List<NetworkItem>();

        // Resources
        public GameObject networkPreset;
        public List<Image> networkIndicators = new List<Image>();

        // Settings
        public bool dynamicNetwork = true;
        [Range(0.1f, 100)] public float defaultSpeed = 20;
        public bool isConnected;
        public int currentNetworkIndex = 0;
        [SerializeField] private Sprite signalDisconnected;
        [SerializeField] private Sprite signalWeak;
        [SerializeField] private Sprite signalNormal;
        [SerializeField] private Sprite signalStrong;
        [SerializeField] private Sprite signalBest;

        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

        [System.Serializable]
        public class NetworkItem
        {
            public string networkID = "My Network";
            public string password = "password";
            public SignalPower signalPower;
            [Range(0.1f, 100)] public float networkSpeed = 20;
            [HideInInspector] public NetworkPreset preset;
        }

        public enum SignalPower { Weak, Normal, Strong, Best }

        void Awake()
        {
            // With a static network there is nothing to restore: we are simply online
            if (!dynamicNetwork)
            {
                isConnected = true;
                return;
            }

            if (DreamOSDataManager.ContainsJsonKey(dataCat, "IsConnected")) { isConnected = DreamOSDataManager.ReadBooleanData(dataCat, "IsConnected"); }

            if (isConnected && DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentNetwork")) { currentNetworkIndex = GetNetworkIndex(DreamOSDataManager.ReadStringData(dataCat, "CurrentNetwork")); }

            UpdateIndicators(true);
        }

        /// <summary>Spawns one preset row per network under <paramref name="parent"/>, replacing any old rows.</summary>
        public void ListNetworks(Transform parent)
        {
            if (!dynamicNetwork)
            {
                isConnected = true;
                return;
            }

            foreach (Transform child in parent) { Destroy(child.gameObject); }

            for (int i = 0; i < networkItems.Count; i++)
            {
                GameObject go = Instantiate(networkPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(parent, false);
                go.gameObject.name = networkItems[i].networkID;

                NetworkPreset preset = go.GetComponent<NetworkPreset>();
                preset.manager = this;
                preset.networkID = networkItems[i].networkID;
                preset.password = networkItems[i].password;
                preset.signalImage.sprite = GetSignalPowerSprite(networkItems[i].signalPower);
                preset.titleText.text = preset.networkID;
                networkItems[i].preset = preset;

                preset.connectButton.onClick.AddListener(delegate { preset.Connect(); });
                preset.disconnectButton.onClick.AddListener(delegate { preset.Disconnect(); });

                if (i == currentNetworkIndex && isConnected) { preset.Connect(true); }
                else { preset.SetNotConnected(); }
            }
        }

        public void ConnectToNetwork(string networkID, string password = null)
        {
            for (int i = 0; i < networkItems.Count; i++)
            {
                if (networkItems[i].preset == null) { continue; }
                if (networkID == networkItems[i].networkID && password == networkItems[i].password)
                {
                    networkItems[i].preset.Connect(true);
                    break;
                }
            }
        }

        public void DisconnectFromNetwork()
        {
            if (!isConnected)
                return;

            if (networkItems[currentNetworkIndex].preset != null) { networkItems[currentNetworkIndex].preset.Disconnect(); }
            else
            {
                DreamOSDataManager.WriteBooleanData(dataCat, "IsConnected", false);
                UpdateIndicators();
            }
        }

        /// <summary>
        /// Paints every taskbar indicator with the current network's signal icon. A current index outside the list
        /// (nothing selected, or a saved network that no longer exists) counts as offline.
        /// </summary>
        public void UpdateIndicators(bool checkForConnection = false)
        {
            bool hasNetwork = currentNetworkIndex >= 0 && currentNetworkIndex < networkItems.Count;
            if (!hasNetwork) { isConnected = false; }

            Sprite signal = hasNetwork
                ? GetSignalPowerSprite(networkItems[currentNetworkIndex].signalPower, checkForConnection)
                : signalDisconnected;

            foreach (Image indicator in networkIndicators)
            {
                if (indicator == null) { continue; }
                indicator.sprite = signal;
            }
        }

        public void PlayWrongPassword()
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.errorSound);
            }
        }

        public void CreateNetwork(string networkID, string password, SignalPower signalPower)
        {
            NetworkItem item = new NetworkItem();
            item.networkID = networkID;
            item.signalPower = signalPower;
            item.password = password;
            networkItems.Add(item);
        }

        public Sprite GetSignalPowerSprite(SignalPower power, bool checkForConnection = false)
        {
            if (!isConnected && checkForConnection) { return signalDisconnected; }

            switch (power)
            {
                case SignalPower.Weak: return signalWeak;
                case SignalPower.Normal: return signalNormal;
                case SignalPower.Strong: return signalStrong;
                case SignalPower.Best: return signalBest;
                default: return null;
            }
        }

        /// <summary>Index of the network with this ID, or -1.</summary>
        public int GetNetworkIndex(string networkID)
        {
            for (int i = 0; i < networkItems.Count; i++)
            {
                if (networkItems[i].networkID == networkID) { return i; }
            }

            return -1;
        }

        public bool IsConnectedToNetwork(int index)
        {
            return isConnected && currentNetworkIndex == index;
        }

        public bool IsConnectedToNetwork(string networkID)
        {
            return isConnected && currentNetworkIndex == GetNetworkIndex(networkID);
        }
    }
}
