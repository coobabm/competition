using System.Collections;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The 「我的设备」 group at the bottom of YY's session list (2016 YY / QQ had one): a folding header and the
    /// 「我的电脑」 row. It lives under the list as an add-on (YYChatView is not changed) and places itself under the
    /// last visible session row every frame. It is shown until the AI has joined YY (AiJoinsYy turns 「我的电脑」
    /// into the AI; afterwards the group is gone). While <see cref="Scripted"/> the cutscene drives the row's texts.
    /// </summary>
    public sealed class YYDeviceGroup : MonoBehaviour
    {
        public const float HeaderHeight = 28, RowHeight = 64;
        const string HeaderName = "YY Devices Header", RowName = "YY Devices Row";
        static readonly Color Ink = new Color32(34, 40, 52, 255), Muted = new Color32(128, 138, 154, 255);
        static readonly Color Selected = new Color32(214, 233, 252, 255), Teal = new Color32(0, 150, 136, 255);

        YYChatHub hub;
        TMP_FontAsset font;
        RectTransform header, row, avatar, monitor, pixels;
        Image rowBg;
        YYCircle avatarCircle;
        TMP_Text headerText;

        /// <summary>The group is unfolded (the player can fold it by clicking the header).</summary>
        public bool Expanded;
        /// <summary>The cutscene owns the row: it stays visible and its texts are not refreshed.</summary>
        public bool Scripted;
        /// <summary>The row blinks like a device that is busy.</summary>
        public bool Flicker;
        public TMP_Text NameLabel { get; private set; }
        public TMP_Text Preview { get; private set; }
        public RectTransform Row => row;

        /// <summary>The group under YY's session list, made once the YY view has been built; null before that.</summary>
        public static YYDeviceGroup Ensure(YYChatHub hub)
        {
            if (hub == null || hub.View == null) return null;
            var list = hub.View.transform.Find("YY2016/Side/Sessions") as RectTransform;
            if (list == null) return null;
            var group = list.GetComponent<YYDeviceGroup>() ?? list.gameObject.AddComponent<YYDeviceGroup>();
            if (group.hub != hub || group.header == null) group.Build(hub);
            return group;
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        void Build(YYChatHub chat)
        {
            hub = chat;
            // A script reload forgets the references but keeps the objects: never draw the group twice.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == HeaderName || child.name == RowName) Destroy(child.gameObject);
            }
            foreach (var t in hub.View.GetComponentsInChildren<TMP_Text>(true)) if (t.font != null) { font = t.font; break; }
            if (font == null) font = PrologueDesk.CjkFont();

            header = PrologueDesk.Rect(HeaderName, transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var hit = PrologueDesk.Fill(header, new Color(0, 0, 0, 0));
            var button = header.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.onClick.AddListener(() => { if (!Scripted) Expanded = !Expanded; });
            headerText = Label(PrologueDesk.Rect("Text", header, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0)), 13, Muted);

            row = PrologueDesk.Rect(RowName, transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            rowBg = PrologueDesk.Fill(row, new Color(0, 0, 0, 0), false);
            avatar = PrologueDesk.Rect("Avatar", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -54), new Vector2(56, -10));
            avatarCircle = avatar.gameObject.AddComponent<YYCircle>();
            avatarCircle.color = Teal; avatarCircle.raycastTarget = false;
            avatar.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            // A little 2016 monitor: screen, stand, foot.
            monitor = PrologueDesk.Rect("Monitor", avatar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(PrologueDesk.Centered("Screen", monitor, new Vector2(0, 4), new Vector2(24, 16)), Color.white, false);
            PrologueDesk.Fill(PrologueDesk.Centered("Glass", monitor, new Vector2(0, 4), new Vector2(20, 12)), new Color32(120, 200, 240, 255), false);
            PrologueDesk.Fill(PrologueDesk.Centered("Stand", monitor, new Vector2(0, -7), new Vector2(4, 6)), Color.white, false);
            PrologueDesk.Fill(PrologueDesk.Centered("Foot", monitor, new Vector2(0, -10), new Vector2(14, 3)), Color.white, false);

            NameLabel = Label(PrologueDesk.Rect("Name", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(66, -32), new Vector2(-14, -10)), 15, Ink);
            Preview = Label(PrologueDesk.Rect("Preview", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(66, -54), new Vector2(-14, -32)), 13, Muted);
            Refresh();
        }

        TMP_Text Label(RectTransform rt, float size, Color color)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = false;
            return t;
        }

        void Refresh()
        {
            headerText.text = (Expanded ? "▾  " : "▸  ") + Lang.T("我的设备") + "  " + (Expanded ? "1/1" : "1");
            if (Scripted) return;
            NameLabel.text = T("我的电脑", "My Computer");
            Preview.text = Lang.T("已连接 · 可以给自己传文件");
        }

        void LateUpdate()
        {
            if (header == null || hub == null) return;
            bool show = Scripted || hub.S != null && !hub.S.lingguangUnlocked;
            if (header.gameObject.activeSelf != show) header.gameObject.SetActive(show);
            bool open = show && Expanded;
            if (row.gameObject.activeSelf != open) row.gameObject.SetActive(open);
            if (!show) return;

            // Under the lowest visible session row (the view lays them out from the top, RowHeight each).
            float bottom = 0;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf || !child.name.StartsWith("Session_", System.StringComparison.Ordinal)) continue;
                bottom = Mathf.Max(bottom, -((RectTransform)child).offsetMin.y);
            }
            header.offsetMin = new Vector2(0, -bottom - HeaderHeight); header.offsetMax = new Vector2(0, -bottom);
            row.offsetMin = new Vector2(0, -bottom - HeaderHeight - RowHeight); row.offsetMax = new Vector2(0, -bottom - HeaderHeight);
            Refresh();
            float blink = Flicker ? (Mathf.Sin(Time.unscaledTime * 22f) > 0 ? 1 : .25f) : 0;
            rowBg.color = Flicker ? new Color(Selected.r, Selected.g, Selected.b, blink) : new Color(0, 0, 0, 0);
        }

        /// <summary>
        /// The avatar becomes the 8×8 「0」 of 0.txt, one pixel at a time in a random order
        /// (<paramref name="tick"/> runs for every few pixels, for a sound).
        /// </summary>
        public IEnumerator DrawZero(float seconds, System.Action tick)
        {
            if (avatar == null) yield break;
            if (pixels != null) Destroy(pixels.gameObject);
            monitor.gameObject.SetActive(false);
            avatarCircle.color = AiJoinsYy.ZeroBack;
            pixels = PrologueDesk.Rect("Zero", avatar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            float cell = avatar.rect.width / 8f;
            var cells = new System.Collections.Generic.List<RectTransform>();
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    if (AiJoinsYy.ZeroRows[y][x] != 'X') continue;
                    var px = PrologueDesk.Rect("Px", pixels, Vector2.zero, Vector2.zero, new Vector2(x * cell, (7 - y) * cell), new Vector2((x + 1) * cell, (8 - y) * cell));
                    PrologueDesk.Fill(px, AiJoinsYy.ZeroFore, false);
                    px.gameObject.SetActive(false);
                    cells.Add(px);
                }
            for (int i = cells.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var tmp = cells[i]; cells[i] = cells[j]; cells[j] = tmp; }
            float each = seconds / Mathf.Max(1, cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == null) yield break;
                cells[i].gameObject.SetActive(true);
                if (i % 3 == 0) tick?.Invoke();
                yield return PrologueDesk.Wait(each);
            }
        }

        /// <summary>Back to the plain 「我的电脑」 row (after a cutscene, for a later replay).</summary>
        public void ResetRow()
        {
            Scripted = false; Flicker = false;
            if (pixels != null) { Destroy(pixels.gameObject); pixels = null; }
            if (monitor != null) monitor.gameObject.SetActive(true);
            if (avatarCircle != null) avatarCircle.color = Teal;
            if (header != null) Refresh();
        }
    }
}
