using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    public class NotepadPreset : MonoBehaviour
    {
        public ButtonManager presetButton;
        public TextMeshProUGUI titleText;
        [HideInInspector] public NotepadManager manager;
        [HideInInspector] public int noteIndex;
        [HideInInspector] public string noteID;
        [HideInInspector] public bool isCustom;
    }
}