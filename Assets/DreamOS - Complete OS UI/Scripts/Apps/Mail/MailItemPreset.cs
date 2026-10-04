using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class MailItemPreset : MonoBehaviour
    {
        public Image coverImage;
        public TextMeshProUGUI letterText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI subjectText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI dateText;
        [HideInInspector] public MailItem mailItem;
    }
}