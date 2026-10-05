using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class MailPreset : MonoBehaviour
    {
        public Animator animator;
        public Image coverImage;
        public TextMeshProUGUI letterText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI fromText;
        public TextMeshProUGUI subjectText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI dateText;
        public TextMeshProUGUI contentText;
        public GameObject contentList;
        public Transform customParent;
    }
}