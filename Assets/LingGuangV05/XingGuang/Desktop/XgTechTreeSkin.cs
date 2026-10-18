using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The art the 科技 page draws with (Assets/LingGuangV05/Art/TechTree, drawn by Tools/TechTreeArt/make_tech_tree_art.py;
    /// it replaced Upgrade Tree PRO's textures on 2026-10-19): the square fill, the state frame,
    /// the hover ring and the connector line. The page is built in code at runtime, so it reaches the package's
    /// sprites through this one asset in Resources (LingGuangV05/TechTree/XgTechTreeSkin). Without it the page
    /// still works with plain rectangles and outlines.
    /// </summary>
    [CreateAssetMenu(menuName = "LingGuang/Tech tree skin")]
    public sealed class XgTechTreeSkin : ScriptableObject
    {
        public const string ResourcePath = "LingGuangV05/TechTree/XgTechTreeSkin";

        [Tooltip("Square fill (Art/TechTree/fill).")] public Sprite fill;
        [Tooltip("Thick state frame (Art/TechTree/frame).")] public Sprite frame;
        [Tooltip("Thin hover ring (Art/TechTree/ring).")] public Sprite ring;
        [Tooltip("Connector and divider lines (Art/TechTree/line).")] public Sprite line;

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
