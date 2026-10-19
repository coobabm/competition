using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The Wi-Fi list panel: asks the <see cref="NetworkManager"/> to spawn one row per network when it starts.
    /// Replaces DreamOS's NetworkContainer (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Network/Network Container")]
    public class NetworkContainer : MonoBehaviour
    {
        // Resources
        [SerializeField] private NetworkManager networkManager;

        void Start()
        {
            ListNetworks();
        }

        public void ListNetworks()
        {
            if (networkManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                NetworkManager[] found = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
#else
                NetworkManager[] found = FindObjectsOfType<NetworkManager>();
#endif
                if (found.Length == 0) { Debug.Log("<b>[Network Container]</b> Network Manager is missing.", this); return; }
                networkManager = found[0];
            }

            foreach (Transform child in transform) { Destroy(child.gameObject); }

            networkManager.ListNetworks(transform);
        }
    }
}
