using LingGuangV05.Desktop.Story;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Games
{
    public sealed class ChessPieceView : MonoBehaviour
    {
        Image face, shadow, glow;
        char piece = '.';
        bool check, selected, suppressed;
        public RectTransform Visual => (RectTransform)transform;
        public Image Face => face;
        public bool InCheck => check;

        public void Build()
        {
            var root = (RectTransform)transform;
            shadow = PrologueDesk.Fill(PrologueDesk.Centered("Piece Shadow", root, new Vector2(0,-26), new Vector2(41,10)), new Color(0,0,0,.16f), false);
            shadow.sprite = PrologueDesk.Circle();
            glow = PrologueDesk.Fill(PrologueDesk.Centered("Piece Glow", root, Vector2.zero, new Vector2(65,65)), Color.clear, false);
            glow.sprite = PrologueDesk.Circle();
            face = PrologueDesk.Fill(PrologueDesk.Centered("Piece Sprite", root, Vector2.zero, new Vector2(67,67)), Color.white, false);
            face.preserveAspect = true;
            SetPiece('.', null, false, false);
        }
        public void SetPiece(char value, Sprite sprite, bool inCheck, bool picked)
        {
            if (face == null) return;
            piece = value; check = inCheck; selected = picked;
            face.sprite = sprite;
            float size = char.ToLowerInvariant(piece) == 'p' ? 56 : char.ToLowerInvariant(piece) == 'r' ? 61 : 67;
            face.rectTransform.sizeDelta = Vector2.one * size;
            SetSuppressed(suppressed);
            if (!check) face.rectTransform.anchoredPosition = Vector2.zero;
        }
        public void SetSuppressed(bool value)
        {
            suppressed = value;
            bool show = piece != '.' && face != null && face.sprite != null && !value;
            if (face != null) face.enabled = show;
            if (shadow != null) shadow.enabled = show;
            if (glow != null) glow.enabled = show && (selected || check);
        }
        public static Vector2 CheckOffset(float seconds, bool reduced)
        {
            return reduced ? Vector2.zero : new Vector2(Mathf.Sin(seconds * 29f) * 1.65f, Mathf.Sin(seconds * 23f) * .6f);
        }
        void Update()
        {
            if (face == null || piece == '.' || suppressed) return;
            bool reduced = PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0) != 0;
            face.rectTransform.anchoredPosition = check ? CheckOffset(Time.unscaledTime, reduced) : Vector2.zero;
            if (glow != null && glow.enabled)
            {
                Color color = check ? new Color(1f,.27f,.21f) : new Color(.39f,.85f,.25f);
                color.a = reduced ? .16f : .13f + .045f * Mathf.Sin(Time.unscaledTime * 4f);
                glow.color = color;
            }
        }
        void OnDisable() { if (face != null) face.rectTransform.anchoredPosition = Vector2.zero; }
    }
}
