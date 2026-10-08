using UnityEngine;
using UnityEngine.UI;

namespace HolenderGames.Sound
{
    /// <summary>
    /// Simple MonoBehaviour to easily set a sound effect to a button or unlockable object in the inspector
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class Soundable : MonoBehaviour
    {
        [SerializeField] GameSound Clip;
        [SerializeField] bool PlayOnAwake = true;
        [SerializeField] bool PlayOnClick = false;

        private Button btn;

        private void Awake()
        {
            if (!PlayOnClick)
                return;

            btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            if (PlayOnAwake)
                Play();
        }

        private void OnClick()
        {
            Play();
        }

        public void Play()
        {
            SoundManager.Instance.PlaySound(Clip);
        }

    }
}