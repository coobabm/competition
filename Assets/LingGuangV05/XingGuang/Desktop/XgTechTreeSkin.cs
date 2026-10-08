using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The Upgrade Tree PRO art the 科技 page draws with (Assets/HolenderGames): the square fill, the state frame,
    /// the hover ring and the connector line. The page is built in code at runtime, so it reaches the package's
    /// sprites through this one asset in Resources (LingGuangV05/TechTree/XgTechTreeSkin). Without it the page
    /// still works with plain rectangles and outlines.
    /// </summary>
    [CreateAssetMenu(menuName = "LingGuang/Tech tree skin")]
    public sealed class XgTechTreeSkin : ScriptableObject
    {
        public const string ResourcePath = "LingGuangV05/TechTree/XgTechTreeSkin";

        [Tooltip("Square fill (UpgradesTreePro/Textures/box).")] public Sprite fill;
        [Tooltip("Thick state frame (UpgradesTreePro/Textures/box_outline_0).")] public Sprite frame;
        [Tooltip("Thin hover ring (UpgradesTreePro/Textures/box_outline_1).")] public Sprite ring;
        [Tooltip("Connector and divider lines (Demo_Examples/Textures/button_square_border, as UpgradeButton's line uses).")] public Sprite line;

        static XgTechTreeSkin loaded;
        static bool tried;

        /// <summary>The skin, or an empty one (plain rectangles) when the asset is missing.</summary>
        public static XgTechTreeSkin Load()
        {
            if (loaded != null) return loaded;
            if (!tried) { tried = true; loaded = Resources.Load<XgTechTreeSkin>(ResourcePath); }
            if (loaded == null) { loaded = CreateInstance<XgTechTreeSkin>(); loaded.hideFlags = HideFlags.DontSave; }
            return loaded;
        }
    }
}
