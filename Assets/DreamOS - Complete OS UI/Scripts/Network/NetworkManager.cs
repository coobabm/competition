using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
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

        // Helpers
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Network;

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
            // We don't need to list network if dynamic network is disabled
            if (!dynamicNetwork)
            {
                isConnected = true;
                return;
            }

            // Check if there's a saved data for connection
            if (DreamOSDataManager.ContainsJsonKey(dataCat, "IsConnected")) { isConnected = DreamOSDataManager.ReadBooleanData(dataCat, "IsConnected"); }

            // Check if there's a saved data for network
            if (isConnected && DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentNetwork")) { currentNetworkIndex = GetNetworkIndex(DreamOSDataManager.ReadStringData(dataCat, "CurrentNetwork")); }

            // Update all available indicators
            UpdateIndicators(true);
        }

        public void ListNetworks(Transform parent)
        {
            // We don't need to list network if dynamic network is disabled
            if (!dynamicNetwork) 
            { 
                isConnected = true;
                return; 
            }
           
            // Delete each cached items before creating presets
            foreach (Transform child in parent) { Destroy(child.gameObject); }
           
            // Process each network item
            for (int i = 0; i < networkItems.Count; i++)
            {
                // Spawn network preset
                GameObject go = Instantiate(networkPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(parent, false);
                go.gameObject.name = networkItems[i].networkID;

                // Get and set the preset component
                NetworkPreset preset = go.GetComponent<NetworkPreset>();
                preset.manager = this;
                preset.networkID = networkItems[i].networkID;
                preset.password = networkItems[i].password;
                preset.signalImage.sprite = GetSignalPowerSprite(networkItems[i].signalPower);
                preset.titleText.text = preset.networkID;
                networkItems[i].preset = preset;

                // Add button events
                preset.connectButton.onClick.AddListener(delegate { preset.Connect(); });
                preset.disconnectButton.onClick.AddListener(delegate { preset.Disconnect(); });

                // Set preset state
                if (i == currentNetworkIndex && isConnected) { preset.Connect(true); }
                else { preset.SetNotConnected(); }
            }
        }

        public void ConnectToNetwork(string networkID, string password = null)
        {
            for (int i = 0; i < networkItems.Count; i++)
            {
                if (networkItems[i].preset == null) { continue; }
                else if (networkID == networkItems[i].networkID && password == networkItems[i].password)
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

        public void UpdateIndicators(bool checkForConnection = false)
        {
            foreach (Image img in networkIndicators) 
            {
                if (img == null) { continue; }
                img.sprite = GetSignalPowerSprite(networkItems[currentNetworkIndex].signalPower, checkForConnection);
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
            NetworkItem nitem = new NetworkItem();
            nitem.networkID = networkID;
            nitem.signalPower = signalPower;
            nitem.password = password;
            networkItems.Add(nitem);
        }

        public Sprite GetSignalPowerSprite(SignalPower power, bool checkForConnection = false)
        {
            Sprite value = null;

            if (!isConnected && checkForConnection) { value = signalDisconnected; }
            else if (power == SignalPower.Weak) { value = signalWeak; }
            else if (power == SignalPower.Normal) { value = signalNormal; }
            else if (power == SignalPower.Strong) { value = signalStrong; }
            else if (power == SignalPower.Best) { value = signalBest; }

            return value;
        }

        public int GetNetworkIndex(string networkID)
        {
            int index = -1;

            for (int i = 0; i < networkItems.Count; i++)
            {
                if (networkItems[i].networkID == networkID)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        public bool IsConnectedToNetwork(int index)
        {
            if (isConnected && currentNetworkIndex == index) { return true; }
            else { return false; }
        }

        public bool IsConnectedToNetwork(string networkID)
        {
            int index = GetNetworkIndex(networkID);

            if (isConnected && currentNetworkIndex == index) { return true; }
            else { return false; }
        }
    }
}