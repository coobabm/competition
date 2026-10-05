using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class ImageMessage : MonoBehaviour
    {
        // Resources
        public Image imageObject;
        public ButtonManager viewButton;
        public TextMeshProUGUI timeText;

        // Helpers
        [HideInInspector] public PhotoGalleryManager pgm;
        WindowManager pgmwm;
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

            pgmwm = pgm.gameObject.GetComponent<WindowManager>();

            viewButton.onClick.AddListener(delegate 
            {
                pgmwm.OpenWindow();
                pgm.OpenPhoto(spriteVar, title, description); 
            });
        }
    }
}