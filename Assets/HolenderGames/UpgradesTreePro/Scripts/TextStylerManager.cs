using UnityEngine;
using System.Text.RegularExpressions;

namespace HolenderGames.UpgradesTreePro
{
    /// <summary>
    /// Singletone to format texts with colored tags
    /// </summary>
    public class TextStylerManager : MonoBehaviour
    {
        public static TextStylerManager Instance { get; private set; }

        [Header("Reference to the color definitions")]
        [SerializeField] private TextStyleDB styleDB;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (styleDB != null)
                styleDB.BuildCache();
        }

        private static readonly Regex TagRegex = new Regex(@"<(\w+)>(.*?)</\1>", RegexOptions.IgnoreCase);

        /// <summary>
        /// Converts custom inline markup into rich text with colors.
        /// Example: "Deal <gold>100</gold> damage." → "Deal <color=#FFD700>100</color> damage."
        /// </summary>
        public string ApplyStyles(string input)
        {
            if (string.IsNullOrEmpty(input) || styleDB == null)
                return input;

            return TagRegex.Replace(input, match =>
            {
                string tag = match.Groups[1].Value;
                string content = match.Groups[2].Value;

                if (styleDB.TryGetColor(tag, out Color color))
                {
                    string hex = ColorUtility.ToHtmlStringRGB(color);
                    return $"<color=#{hex}>{content}</color>";
                }

                return content; // fallback: remove unknown tag
            });
        }
    }

}