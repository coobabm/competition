using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Network/Network Container")]
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
            // Check for network manager
            if (networkManager == null)
            {
#if UNITY_2023_2_OR_NEWER
                if (FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).Length > 0) { networkManager = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None)[0]; }
#else
                if (FindObjectsOfType(typeof(NetworkManager)).Length > 0) { networkManager = (NetworkManager)FindObjectsOfType(typeof(NetworkManager))[0]; }
#endif
                else { Debug.Log("<b>[Network Container]</b> Network Manager is missing.", this); return; }
            }

            // Delete each cached objects
            foreach (Transform child in transform) { Destroy(child.gameObject); }

            // Create a new network object for each item
            networkManager.ListNetworks(transform);
        }
    }
}