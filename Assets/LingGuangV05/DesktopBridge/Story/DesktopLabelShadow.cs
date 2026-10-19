using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Gives the desktop icon names a dark halo (2026-10-19, owner: 「桌面的字要有阴影，字有一圈黑圈」), so white labels
    /// stay readable on any wallpaper, like the XP/7 desktop. Uses the SDF underlay (dilated, soft, nudged down-right),
    /// which grows outside the glyph instead of eating into thin CJK strokes the way an outline would. Fonts change with
    /// the language and shortcuts appear later, so the labels are rechecked every half second; each source material
    /// gets one shadowed copy, shared by every label using it.
    /// </summary>
    public sealed class DesktopLabelShadow : MonoBehaviour
    {
        const float CheckEvery = .5f;
        static readonly Color Halo = new Color(0f, 0f, 0f, .95f);

        static readonly Dictionary<Material, Material> Shadowed = new Dictionary<Material, Material>();
        static readonly HashSet<Material> Made = new HashSet<Material>();

        readonly List<TMP_Text> labels = new List<TMP_Text>();
        float next;
        Transform list;

        public static DesktopLabelShadow Install(GameObject host)
            => host.GetComponent<DesktopLabelShadow>() ?? host.AddComponent<DesktopLabelShadow>();

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + CheckEvery;
            if (list == null) { var found = GameObject.Find("Desktop List"); list = found != null ? found.transform : null; }
            if (list == null) return;

            list.GetComponentsInChildren(true, labels);
            foreach (var label in labels) Apply(label);
        }

        static void Apply(TMP_Text label)
        {
            Material source = label.fontSharedMaterial;
            if (source == null || Made.Contains(source) || !source.HasProperty(ShaderUtilities.ID_UnderlayColor)) return;

            if (!Shadowed.TryGetValue(source, out Material shadow) || shadow == null)
            {
                shadow = new Material(source) { name = source.name + " (desktop halo)" };
                shadow.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                shadow.SetColor(ShaderUtilities.ID_UnderlayColor, Halo);
                shadow.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, .15f);
                shadow.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.15f);
                shadow.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1f);
                shadow.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .25f);
                // The ring is as wide as the font atlas padding allows; recompute the clamp for the new underlay
                ShaderUtilities.UpdateShaderRatios(shadow);
                Shadowed[source] = shadow;
                Made.Add(shadow);
            }

            label.fontSharedMaterial = shadow;
        }
    }
}
