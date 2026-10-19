using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// A picture bubble (moved from DreamOS 2026-10-19; same GUID and fields); its view button opens the picture in
    /// the photo app, or is greyed out without one.
    /// </summary>
    public class ImageMessage : MonoBehaviour
    {
        public Image imageObject;
        public ButtonManager viewButton;
        public TextMeshProUGUI timeText;

        [HideInInspector] public PhotoApp pgm;
        [HideInInspector] public Sprite spriteVar;
        [HideInInspector] public string title;
        [HideInInspector] public string description;

        void Start()
        {
            if (pgm == null)
            {
                viewButton.Interactable(false);
                return;
            }

            var window = pgm.GetComponent<WindowManager>();
            viewButton.onClick.AddListener(() =>
            {
                window.OpenWindow();
                pgm.OpenPhoto(spriteVar, title, description);
            });
        }
    }
}
