using System;
using System.IO;
using System.Text;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The memory book made visible (stage 5+). On the desktop: 「笔记.txt」, opening a read-only notepad window with
    /// one note per line. On disk: 「{AI}的笔记.txt」 beside the save in persistentDataPath/LingGuangV05, rewritten
    /// when the runtime writes a real disk save and the book has changed. Tests and QA (TestMode, or disk saves off)
    /// never write it. The book itself lives in XgState (XgSim.MemoryBook.cs); this is only a mirror.
    /// </summary>
    public sealed class XgMemoryNotebook : MonoBehaviour
    {
        XingGuangController controller;
        ChapterOneRuntime runtime;
        XgSim bound;
        RectTransform icon, window;
        TMP_Text body;
        int shownRevision = -1, writtenRevision = -1;
        string writtenPath = "";
        bool english;

        static string T(string zh, string en) => GameText.T(zh, en);

        public void Bind(XingGuangController owner)
        {
            if (runtime != null) runtime.Saving -= WriteFile;
            controller = owner;
            runtime = owner != null ? owner.runtime : null;
            if (runtime != null) runtime.Saving += WriteFile;
        }

        void OnDestroy()
        {
            if (runtime != null) runtime.Saving -= WriteFile;
            Close();
            if (icon != null) Destroy(icon.gameObject);
        }

        void Update()
        {
            if (controller == null || controller.Sim == null || runtime == null || runtime.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim))
            {
                bound = controller.Sim;
                writtenRevision = shownRevision = -1;
                Close();
            }
            var desk = PrologueDirector.Desk;
            if (desk == null || desk.icons == null) return;
            bool show = bound.MemoryOpen && !runtime.Sim.InPrologue;
            if (show && icon == null) MakeIcon(desk);
            else if (!show && icon != null) { Destroy(icon.gameObject); icon = null; Close(); }
            if (icon != null && english != GameText.IsEnglish) { english = GameText.IsEnglish; PrologueDesk.SetIconLabel(icon, Label()); }
            if (window != null && body != null && shownRevision != bound.S.memoryRevision) Fill();
        }

        static string Label() => T("笔记.txt", "notes.txt");

        void MakeIcon(PrologueDesk desk)
        {
            english = GameText.IsEnglish;
            icon = desk.notepadIcon != null ? desk.Icon("Memory Notebook", Label(), desk.notepadIcon) : desk.Icon("Memory Notebook", Label(), null, desk.DrawBlankFile);
            icon.GetComponent<PrologueClick>().Open = () => Open(desk);
            UiTip.Add(icon, () => T("它自己记的笔记：你们聊过的重要的事。只读。", "Its own notes: the important things you talked about. Read-only."));
        }

        void Open(PrologueDesk desk)
        {
            if (window != null) { window.SetAsLastSibling(); return; }
            var client = desk.Window("Memory Notebook Window", Label() + T(" - 记事本（只读）", " - Notepad (read-only)"), new Vector2(-60, 40), new Vector2(720, 460), out window, () => { window = null; body = null; });
            var menu = PrologueDesk.Rect("Menu", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -30), Vector2.zero);
            PrologueDesk.Fill(menu, new Color32(245, 246, 248, 255));
            desk.Text(PrologueDesk.Rect("Items", menu, Vector2.zero, Vector2.one, new Vector2(10, 0), Vector2.zero), T("文件(F)  编辑(E)  格式(O)  查看(V)  帮助(H)", "File  Edit  Format  View  Help"), 15, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);

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
            Fill();
        }

        void Fill()
        {
            if (body == null || bound == null) return;
            bound.English = GameText.IsEnglish;
            body.text = bound.NotebookText();
            shownRevision = bound.S.memoryRevision;
        }

        void Close()
        {
            if (window != null) Destroy(window.gameObject);
            window = null; body = null;
        }

        /// <summary>Runs right before the runtime serializes its save (ChapterOneRuntime.Saving).</summary>
        void WriteFile()
        {
            try
            {
                if (runtime == null || !runtime.useDiskSave || runtime.TestMode || bound == null || !ReferenceEquals(bound, controller.Sim)) return;
                if (!bound.MemoryOpen && bound.MemoryBook.Count == 0) return;
                string folder = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "LingGuangV05"));
                // A redirected save folder (a persistence test) is not the player's folder: write nothing.
                string saveFolder = Path.GetDirectoryName(runtime.SavePath ?? "");
                if (string.IsNullOrEmpty(saveFolder) || !string.Equals(Path.GetFullPath(saveFolder), folder, StringComparison.Ordinal)) return;
                string path = Path.Combine(folder, bound.NotebookFileName());
                if (writtenRevision == bound.S.memoryRevision && path == writtenPath && File.Exists(path)) return;
                Directory.CreateDirectory(folder);
                // A mirror of the save, not the save: a plain overwrite is enough.
                File.WriteAllText(path, bound.NotebookText().Replace("\n", Environment.NewLine), new UTF8Encoding(true));
                writtenRevision = bound.S.memoryRevision; writtenPath = path;
            }
            catch (Exception error) { Debug.LogWarning("[灵光] 笔记写入失败：" + error.Message); }
        }
    }
}
