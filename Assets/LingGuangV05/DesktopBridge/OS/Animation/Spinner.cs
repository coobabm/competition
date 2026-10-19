using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Spins a loading icon on unscaled time (browser tab spinners, boot screen). Replaces DreamOS's Spinner
    /// (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/Animation/Spinner")]
    public class Spinner : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private RectTransform objectToSpin;

        [Header("Settings")]
        [SerializeField] [Range(0.1f, 50)] private float speed = 10;
        [SerializeField] private Direction direction;

        float currentPos;

        public enum Direction { Clockwise, CounterClockwise }

        void Awake()
        {
            if (objectToSpin == null) { enabled = false; }
        }

        void Update()
        {
            float step = speed * 100 * Time.unscaledDeltaTime;

            // The angle is never wrapped on the clockwise side (it only decreases); the full turns are harmless
            // because the rotation is an Euler angle.
            if (direction == Direction.Clockwise)
            {
                currentPos -= step;
                if (currentPos >= 360) { currentPos = 0; }
            }

            else if (direction == Direction.CounterClockwise)
            {
                currentPos += step;
                if (currentPos <= -360) { currentPos = 0; }
            }

            objectToSpin.localRotation = Quaternion.Euler(0, 0, currentPos);
        }
    }
}
