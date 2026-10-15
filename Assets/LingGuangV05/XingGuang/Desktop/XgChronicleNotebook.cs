using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 「灵光记.txt」 on the desktop once the ending has played (名著喂养与结局_设计.md §三): a read-only notepad window with
    /// 《灵光记》, rebuilt from the save (XgSim.Chronicle.cs) every time it opens, so it needs nothing saved of its own.
    /// </summary>
    public sealed class XgChronicleNotebook : MonoBehaviour
    {
        XingGuangController controller;
        ChapterOneRuntime runtime;
        XgSim bound;
        RectTransform icon, window;
        TMP_Text body;
        bool english;

        public void Bind(XingGuangController owner)
        {
            controller = owner;
            runtime = owner != null ? owner.runtime : null;
        }

        void OnDestroy()
        {
            Close();
            if (icon != null) Destroy(icon.gameObject);
        }

        void Update()
        {
            if (controller == null || controller.Sim == null || runtime == null || runtime.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) { bound = controller.Sim; Close(); }
            var desk = PrologueDirector.Desk;
            if (desk == null || desk.icons == null) return;
            bool show = bound.S.ending.Length > 0 && runtime.Sim.S.endingPlayed && !runtime.Sim.InPrologue;
            if (show && icon == null) MakeIcon(desk);
            else if (!show && icon != null) { Destroy(icon.gameObject); icon = null; Close(); }
            if (icon != null && english != GameText.IsEnglish) { english = GameText.IsEnglish; PrologueDesk.SetIconLabel(icon, Label()); }
        }

        static string Label() => GameText.T("灵光记.txt", "Lingguang.txt");

        void MakeIcon(PrologueDesk desk)
        {
            english = GameText.IsEnglish;
            icon = desk.notepadIcon != null ? desk.Icon("Lingguang Chronicle", Label(), desk.notepadIcon) : desk.Icon("Lingguang Chronicle", Label(), null, desk.DrawBlankFile);
            icon.GetComponent<PrologueClick>().Open = () => Open(desk);
            UiTip.Add(icon, () => GameText.T("它把这一年写成了一本章回小说。只读。", "It wrote the year as a novel in chapters. Read-only."));
        }

        void Open(PrologueDesk desk)
        {
            if (window != null) { window.SetAsLastSibling(); return; }
            var client = desk.Window("Lingguang Chronicle Window", Label() + Lang.T(" - 记事本（只读）"), new Vector2(-40, 30), new Vector2(760, 520), out window, () => { window = null; body = null; });
            var menu = PrologueDesk.Rect("Menu", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -30), Vector2.zero);
            PrologueDesk.Fill(menu, new Color32(245, 246, 248, 255));
            desk.Text(PrologueDesk.Rect("Items", menu, Vector2.zero, Vector2.one, new Vector2(10, 0), Vector2.zero), Lang.T("文件(F)  编辑(E)  格式(O)  查看(V)  帮助(H)"), 15, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            var viewport = PrologueDesk.Rect("Viewport", client, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -38));
            PrologueDesk.Fill(viewport, Color.white);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = PrologueDesk.Rect("Text", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            body = desk.Text(content, "", 17, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            body.richText = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            bound.English = GameText.IsEnglish;
            body.text = bound.ChronicleText();
        }

        void Close()
        {
            if (window != null) Destroy(window.gameObject);
            window = null; body = null;
        }
    }
}
