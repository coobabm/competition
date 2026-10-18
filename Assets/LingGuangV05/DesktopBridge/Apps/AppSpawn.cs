using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    static class AppSpawn
    {
        /// <summary>
        /// Instantiates a list row at the scene root and then parents it (local position zero), the order the original
        /// DreamOS apps used: the row's Awake runs before it is in the list, so its widgets set up exactly as before.
        /// </summary>
        public static GameObject Row(GameObject preset, Transform parent, string name)
        {
            var go = Object.Instantiate(preset, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(parent, false);
            go.name = name;
            return go;
        }
    }
}
