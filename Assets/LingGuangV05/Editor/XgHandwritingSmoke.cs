using System;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// An edit-mode smoke test of the writing view on the 手写数字 desk (no Play mode, nothing saved): builds the 标注台 on a
    /// throw-away canvas with a stub host, writes a digit through the page's own pen handlers, lets the live guess run, hands
    /// the drawing in, and checks the pay, the sample, the empty-drawing hint, the yes/no toggle and the English texts.
    /// Menu: Tools / LingGuang / Smoke test handwriting view.
    /// </summary>
    public static class XgHandwritingSmoke
    {
        sealed class Wallet : IXgHost
        {
            public double money = 2000;
            public double Compute => 1;
            public double VramMB => 16000;
            public double Money => money;
            public bool Spend(double a) { if (a < 0 || money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
        }

        sealed class Stub : IXgPageHost
        {
            public XgSim sim; public Wallet wallet = new Wallet(); public XgJuice juice;
            public XingGuangController Controller => null;
            public XgSim Sim => sim;
            public IXgHost Host => wallet;
            public XgJuice Juice => juice;
            public int BuyAmount { get; set; } = 1;
            public bool Visible => true;
            public string Tab => "label";
            public RectTransform ComboChip => null;
            public void Refresh(bool force) { }
            public void ShowToast(string text, float seconds) { }
            public void ShowTab(string id) { }
            public bool BuyNode(XgNode node, RectTransform from) { return false; }
        }

        static readonly StringBuilder Log = new StringBuilder();
        static int failures;
        static void Note(string s) { Log.AppendLine(s); }
        static void Check(bool ok, string what) { if (!ok) { failures++; Note("FAIL " + what); } else Note("ok   " + what); }

        const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, All).GetValue(o);
        static void Call(object o, string name, params object[] args) { o.GetType().GetMethod(name, All).Invoke(o, args); }

        [MenuItem("Tools/LingGuang/Smoke test handwriting view")]
        public static void Menu() { Debug.Log(Run()); }

        public static string Run()
        {
            Log.Length = 0; failures = 0;
            if (Application.isPlaying) return "skipped: Unity is in Play mode";
            var canvasGo = new GameObject("XgHandwritingSmoke", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                canvasGo.hideFlags = HideFlags.DontSave;
                var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var canvasRt = (RectTransform)canvasGo.transform; canvasRt.sizeDelta = new Vector2(1266, 763);
                var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(canvasRt, false);
                area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(144, 36); area.offsetMax = new Vector2(-12, -70);
                var jgo = new GameObject("Juice", typeof(RectTransform)); jgo.transform.SetParent(canvasRt, false);
                var stub = new Stub { sim = new XgSim { GoldChance = 0 } };
                stub.juice = jgo.AddComponent<XgJuice>(); stub.juice.Init(canvasRt, canvasRt, null); stub.juice.Muted = true;
                var s = stub.sim;
                s.S.stage = s.S.stageVision = 2;
                s.S.desksOpen.Add("mnist");
                s.SelectDesk("mnist");

                var label = new XgLabelPage();
                label.Init(stub, new XgUi(null, null));
                label.Build(area);
                Canvas.ForceUpdateCanvases();
                label.Refresh(); label.Tick(.1f);

                var drawRoot = Field<RectTransform>(label, "drawRoot");
                var body = (RectTransform)drawRoot.parent;
                Check(!drawRoot.gameObject.activeSelf, "the desk opens on the yes/no cards");
                Check(Field<XgBtn>(label, "classicToggle").rt.parent.gameObject.activeSelf, "with a toggle to the writing view");
                Field<XgBtn>(label, "classicToggle").button.onClick.Invoke();
                Check(drawRoot.gameObject.activeSelf, "the toggle opens the writing view");
                Note("body " + body.rect.width.ToString("0") + " x " + body.rect.height.ToString("0") + ", writing view " + drawRoot.rect.width.ToString("0") + " x " + drawRoot.rect.height.ToString("0"));
                Check(drawRoot.rect.height >= 540 && drawRoot.rect.width >= 572, "writing view is big enough for its layout (needs 572 x 540)");
                var asked = Field<int>(label, "asked");
                Check(asked >= 0 && asked <= 9, "a digit is asked: " + asked);
                Check(Field<TMP_Text>(label, "drawPrompt").text.Contains(asked.ToString()), "prompt names the digit: " + Field<TMP_Text>(label, "drawPrompt").text);
                Check(!Field<XgBtn>(label, "classicToggle").rt.parent.gameObject.activeSelf, "the way back is hidden while writing");
                Note("header rule: " + Field<TMP_Text>(label, "wkRule").text);
                Note("bubble idle: " + Field<TMP_Text>(label, "guessLine").text + " | " + Field<TMP_Text>(label, "memoryTitle").text);

                // An empty page is not handed in.
                double money = stub.wallet.money;
                Field<XgBtn>(label, "drawDone").button.onClick.Invoke();
                Check(stub.wallet.money == money && s.Labels("mnist") == 0, "empty page: no pay, no sample (" + Field<TMP_Text>(label, "drawFeedback").text + ")");

                // Write the asked digit as a bold stroke through the page's pen handlers (a "1" for any digit; the reading is what is tested).
                Call(label, "PenDown", new Vector2(.5f, .1f));
                for (int i = 1; i <= 20; i++) Call(label, "PenMove", new Vector2(.5f + .05f * Mathf.Sin(i), .1f + .8f * i / 20f));
                Call(label, "PenUp");
                label.Tick(.3f);
                var ink = Field<XgHandwritingInk>(label, "ink");
                Check(ink.Count > 0, "ink segments drawn: " + ink.Count);
                Check(Field<XgHandwritingGuess>(label, "liveGuess").valid, "live guess came up");
                Note("bubble: " + Field<TMP_Text>(label, "guessLine").text + " " + Field<TMP_Text>(label, "guessPct").text + " | " + Field<TMP_Text>(label, "timeLine").text);
                Note("top candidates: " + Field<TMP_Text[]>(label, "oddsDigit")[0].text + " " + Field<TMP_Text[]>(label, "oddsPct")[0].text + ", " + Field<TMP_Text[]>(label, "oddsDigit")[1].text + " " + Field<TMP_Text[]>(label, "oddsPct")[1].text);

                // Clear and write again, then hand it in.
                Field<XgBtn>(label, "drawClear").button.onClick.Invoke();
                Check(ink.Count == 0 && !Field<bool>(label, "hasInk"), "clear empties the sheet");
                Call(label, "PenDown", new Vector2(.5f, .1f));
                for (int i = 1; i <= 20; i++) Call(label, "PenMove", new Vector2(.5f, .1f + .8f * i / 20f));
                Call(label, "PenUp");
                label.Tick(.3f);
                double pay = s.ManualPayFor("mnist", s.LevelOf("mnist"));
                money = stub.wallet.money;
                Field<XgBtn>(label, "drawDone").button.onClick.Invoke();
                Check(s.Labels("mnist") == 1 && s.HandwritingMemory == 1, "one sample credited and remembered");
                Check(stub.wallet.money >= money + pay - 1e-9, "paid at least the hand pay (" + (stub.wallet.money - money).ToString("0.00") + " of " + pay.ToString("0.00") + ")");
                Check(s.S.handFines == 0, "no fine");
                Note("feedback: " + Field<TMP_Text>(label, "drawFeedback").text.Replace("\n", " | "));
                Note("stats: " + Field<TMP_Text>(label, "memoryTitle").text + " | " + Field<TMP_Text>(label, "accuracyLabel").text + " " + Field<TMP_Text>(label, "accuracyBig").text + " | " + Field<TMP_Text>(label, "sessionCaption").text);
                Note("next digit asked: " + Field<int>(label, "asked"));
                Check(Field<XgHandwritingInk>(label, "ink").Count == 0, "sheet cleared for the next digit");

                // Yes/no cards stay reachable and back.
                Field<XgBtn>(label, "drawToggle").button.onClick.Invoke();
                Check(!drawRoot.gameObject.activeSelf, "toggle shows the yes/no cards");
                Check(Field<XgBtn>(label, "classicToggle").rt.parent.gameObject.activeSelf, "and a way back");
                Check(Field<XgBtn>(label, "yes").rt.parent.gameObject.activeInHierarchy, "yes button is on screen");
                Field<XgBtn>(label, "classicToggle").button.onClick.Invoke();
                Check(drawRoot.gameObject.activeSelf, "back to writing");

                // Other desks keep their cards.
                s.SelectDesk("spam"); label.Refresh();
                Check(!drawRoot.gameObject.activeSelf, "no writing view on other desks");
                s.SelectDesk("mnist"); label.Refresh();

                // English: no Chinese left in the writing view.
                using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                {
                    stub.sim.English = true;
                    label.RebuildTabs(); label.Refresh();
                    int leaks = 0;
                    foreach (var t in drawRoot.GetComponentsInChildren<TMP_Text>(false))
                        foreach (char c in t.text ?? "") if (c >= 0x4E00 && c <= 0x9FFF) { leaks++; Note("  Chinese left: " + t.name + " '" + t.text + "'"); break; }
                    Check(leaks == 0, "English writing view has no Chinese");
                    Note("English prompt: " + Field<TMP_Text>(label, "drawPrompt").text + " | " + Field<TMP_Text>(label, "drawPay").text);
                }
                for (int i = 0; i < 20; i++) label.Tick(.1f);
            }
            catch (Exception e) { failures++; Note("EXCEPTION " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace); }
            finally { UnityEngine.Object.DestroyImmediate(canvasGo); }
            return (failures == 0 ? "HANDWRITING SMOKE OK" : "HANDWRITING SMOKE FAILURES " + failures) + "\n" + Log;
        }

        /// <summary>Renders the writing view mid-drawing to a PNG (edit mode, throw-away canvas). Layout check only; the game's own font and skin are used.</summary>
        public static string Shot(string path, int taught, bool afterHandIn)
        {
            if (Application.isPlaying) return "skipped: Unity is in Play mode";
            var canvasGo = new GameObject("XgHandwritingShot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject camGo = null; RenderTexture rt = null;
            try
            {
                canvasGo.hideFlags = HideFlags.DontSave;
                var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var canvasRt = (RectTransform)canvasGo.transform; canvasRt.sizeDelta = new Vector2(1266, 763);
                var back = new GameObject("Back", typeof(RectTransform), typeof(Image)); back.transform.SetParent(canvasRt, false);
                var backRt = (RectTransform)back.transform; backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one; backRt.offsetMin = backRt.offsetMax = Vector2.zero;
                back.GetComponent<Image>().color = new Color(.93f, .94f, .96f);
                var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(canvasRt, false);
                area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(144, 36); area.offsetMax = new Vector2(-12, -70);
                var jgo = new GameObject("Juice", typeof(RectTransform)); jgo.transform.SetParent(canvasRt, false);
                var stub = new Stub { sim = new XgSim { GoldChance = 0 } };
                var font = PrologueDesk.CjkFont();
                stub.juice = jgo.AddComponent<XgJuice>(); stub.juice.Init(canvasRt, canvasRt, font); stub.juice.Muted = true;
                var s = stub.sim;
                s.S.stage = s.S.stageVision = 2; s.S.desksOpen.Add("mnist"); s.SelectDesk("mnist");
                var label = new XgLabelPage();
                label.Init(stub, new XgUi(font, null));
                label.Build(area);
                label.Refresh(); label.Tick(.1f);
                for (int i = 0; i < taught; i++) s.SubmitHandwriting(i % 10, XgHandwriting.Template(i % 10, (i / 10) % 2), 3 + i % 4, stub.wallet);
                label.Refresh(); label.Tick(.1f);
                int asked = Field<int>(label, "asked");
                // A rough "asked" digit drawn through the pen handlers: a loop for 0/6/8/9, a bar and a stroke otherwise.
                var pts = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i <= 24; i++) { float a = i / 24f * Mathf.PI * 2; pts.Add(new Vector2(.5f + .27f * Mathf.Cos(a), .5f + .38f * Mathf.Sin(a))); }
                if (asked == 1) { pts.Clear(); pts.Add(new Vector2(.5f, .1f)); pts.Add(new Vector2(.5f, .9f)); }
                Call(label, "PenDown", pts[0]);
                for (int i = 1; i < pts.Count; i++) { Call(label, "PenMove", pts[i]); label.Tick(.05f); }
                Call(label, "PenUp"); label.Tick(.3f);
                if (afterHandIn) { Field<XgBtn>(label, "drawDone").button.onClick.Invoke(); label.Tick(.1f); }
                Canvas.ForceUpdateCanvases();
                foreach (var t in canvasGo.GetComponentsInChildren<TMP_Text>(false)) t.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                camGo = new GameObject("ShotCam", typeof(Camera)); camGo.hideFlags = HideFlags.DontSave;
                var cam = camGo.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 763 / 2f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.white;
                camGo.transform.position = canvasRt.position + new Vector3(0, 0, -10); cam.nearClipPlane = .1f; cam.farClipPlane = 100;
                canvas.worldCamera = cam;
                rt = new RenderTexture(1266, 763, 24); cam.targetTexture = rt; cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(1266, 763, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1266, 763), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                var diag = new StringBuilder();
                foreach (var name in new[] { "drawPrompt", "drawPay", "memoryTitle", "guessLine" })
                { var t = Field<TMP_Text>(label, name); diag.Append(name + ": '" + t.text + "' color " + t.color + " rect " + t.rectTransform.rect + " verts " + t.textInfo.characterCount + "/" + (t.mesh != null ? t.mesh.vertexCount : -1) + " active " + t.gameObject.activeInHierarchy + " font " + (t.font != null ? t.font.name : "null") + " size " + t.fontSize + "\n"); }
                var od = Field<TMP_Text[]>(label, "oddsDigit")[0]; diag.Append("oddsDigit0: '" + od.text + "' rect " + od.rectTransform.rect + " verts " + (od.mesh != null ? od.mesh.vertexCount : -1) + " color " + od.color + "\n");
                diag.Append("ink graphic verts " + (Field<XgHandwritingInk>(label, "ink").GetComponent<CanvasRenderer>() != null ? "has CR" : "NO CR") + " count " + Field<XgHandwritingInk>(label, "ink").Count + " rect " + Field<XgHandwritingInk>(label, "ink").rectTransform.rect + "\n");
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return "saved " + path + " asked " + asked + "\n" + diag;
            }
            catch (Exception e) { return "EXCEPTION " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace; }
            finally
            {
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }
    }
}
