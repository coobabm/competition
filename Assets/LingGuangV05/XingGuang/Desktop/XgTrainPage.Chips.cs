using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>The training page's small framed labels (one factor of the effective scale) and the left column's layout.</summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>A small framed label: one factor of the effective scale (a part of the TrainScaleModule prefab).</summary>
        sealed class Chip
        {
            public RectTransform rt;
            public XgFrameGraphic frame;
            public TMP_Text text;
        }

        /// <summary>Puts the train row at the bottom of the left column and the curve card above it.</summary>
        void LayoutLeft()
        {
            trainRow.offsetMin = Vector2.zero; trainRow.offsetMax = new Vector2(0, TrainRowHeight);
            curveCard.offsetMin = new Vector2(0, TrainRowHeight + RowGap);
        }
    }
}
