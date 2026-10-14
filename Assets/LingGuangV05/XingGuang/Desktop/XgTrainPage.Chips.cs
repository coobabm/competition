using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>The training page's small framed labels (one factor of the effective scale) and the left column's layout.</summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>A small framed label: one factor of the effective scale.</summary>
        sealed class Chip
        {
            public RectTransform rt;
            public XgFrameGraphic frame;
            public TMP_Text text;
        }

        Chip MakeChip(RectTransform parent, string name, float size)
        {
            var rt = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0, 1);
            var frame = rt.gameObject.AddComponent<XgFrameGraphic>();
            frame.raycastTarget = false;
            var text = ui.Text(Rect("Text", rt, Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-5, 0)), "", size, XgDark.Ink, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return new Chip { rt = rt, frame = frame, text = text };
        }

        /// <summary>Sizes a chip to its text and puts it at (x, y) from the top-left of its parent; returns its width.</summary>
        static float PlaceChip(Chip c, float x, float y, float height, float minWidth = 0)
        {
            float w = Mathf.Max(minWidth, c.text.GetPreferredValues(c.text.text).x + 12);
            c.rt.sizeDelta = new Vector2(w, height);
            c.rt.anchoredPosition = new Vector2(x, -y);
            return w;
        }

        /// <summary>Puts the train row at the bottom of the left column and the curve card above it.</summary>
        void LayoutLeft()
        {
            trainRow.offsetMin = Vector2.zero; trainRow.offsetMax = new Vector2(0, TrainRowHeight);
            curveCard.offsetMin = new Vector2(0, TrainRowHeight + RowGap);
        }
    }
}
