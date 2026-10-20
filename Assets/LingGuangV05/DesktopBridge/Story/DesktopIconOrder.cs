using System.Collections.Generic;
using System.Text;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Keeps the desktop icons in order with no gaps. The icons keep free-drag positions in DreamOS data, so when stage
    /// gates hide some (摆渡众包, 喵鱼, 游戏中心, 淘货 until their stage) or new ones appear (灵光.exe, sophon.dll, the
    /// prologue's files), holes and stragglers were left. Whenever the set of visible icons changes, they are packed again
    /// in the desktop's own order (the icons' order in the list, new ones at the end), top to bottom, column by column,
    /// and the positions are stored. A layout the player dragged by hand stays until the next icon appears or goes.
    /// </summary>
    public sealed class DesktopIconOrder : MonoBehaviour
    {
        const float CheckEvery = .5f;
        float next;
        string shown = "";

        public static DesktopIconOrder Install(GameObject host)
            => host.GetComponent<DesktopIconOrder>() ?? host.AddComponent<DesktopIconOrder>();

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + CheckEvery;
            var list = GameObject.Find("Desktop List");
            if (list == null) return;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return; // not while an icon is being dragged
            string now = Signature((RectTransform)list.transform);
            if (now == shown) return;
            shown = now;
            Arrange((RectTransform)list.transform);
        }

        static string Signature(RectTransform list)
        {
            var b = new StringBuilder();
            foreach (Transform child in list) if (child.gameObject.activeSelf) b.Append(child.GetInstanceID()).Append(',');
            return b.ToString();
        }

        /// <summary>Packs the visible icons of <paramref name="list"/> in their list order, column by column, and stores the positions.</summary>
        public static void Arrange(RectTransform list)
        {
            if (list == null) return;
            var grid = list.GetComponent<GridLayoutGroup>();
            Vector2 cell = grid != null ? grid.cellSize : new Vector2(112, 112), spacing = grid != null ? grid.spacing : new Vector2(14, 9);
            float left = grid != null ? grid.padding.left : 25, top = grid != null ? grid.padding.top : 24;
            int rows = Mathf.Max(1, Mathf.FloorToInt((list.rect.height - top) / (cell.y + spacing.y)));
            var icons = new List<RectTransform>();
            foreach (Transform child in list) if (child.gameObject.activeSelf && child is RectTransform rt) icons.Add(rt);
            for (int i = 0; i < icons.Count; i++)
            {
                int col = i / rows, row = i % rows;
                var p = new Vector2(left + cell.x / 2 + col * (cell.x + spacing.x), -(top + cell.y / 2 + row * (cell.y + spacing.y)));
                // Late-enabled shortcuts never went through GridLayoutGroup's initial pass.
                // Arrange uses top-left offsets, so normalize anchors before applying them.
                icons[i].anchorMin=icons[i].anchorMax=new Vector2(0,1);
                icons[i].pivot=new Vector2(.5f,.5f);icons[i].sizeDelta=cell;
                if ((icons[i].anchoredPosition - p).sqrMagnitude < 1) continue;
                icons[i].anchoredPosition = p;
                var dragger = icons[i].GetComponent<ItemDragger>();
                if (dragger != null && dragger.rememberPosition) dragger.UpdatePositionData();
            }
        }

        /// <summary>Re-pack now (an icon just appeared and is about to pop in where it belongs).</summary>
        public static void ArrangeNow(RectTransform icon)
        {
            if (icon == null || icon.parent == null) return;
            Arrange((RectTransform)icon.parent);
            var order = FindAnyObjectByType<DesktopIconOrder>();
            if (order != null) order.shown = Signature((RectTransform)icon.parent);
        }
    }
}
