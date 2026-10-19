using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>One row in the Notepad list (replaces DreamOS's NotepadPreset; same GUID and fields).</summary>
    public class NoteListEntry : MonoBehaviour
    {
        public ButtonManager presetButton;
        public TextMeshProUGUI titleText;
        [HideInInspector] public NoteApp manager;
        [HideInInspector] public int noteIndex;
        [HideInInspector] public string noteID;
        [HideInInspector] public bool isCustom;
    }
}
