using System;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// An edit-mode smoke test of the desk games (no Play mode, nothing saved): for every desk that has a native game it builds
    /// the 标注台 on a throw-away canvas with a stub host, checks that the game is the desk's main view, lets it run, settles one
    /// answer through the game's own context (checking the sample lands on that desk's dataset), flips to the yes/no cards and
    /// back, and renders the page to /tmp/desk_&lt;id&gt;.png. Never touches Time.timeScale.
    /// Menu: Tools / LingGuang / Smoke test desk games.
    /// </summary>
    public static class XgDeskGameSmoke
    {
        sealed class Wallet : IXgHost
        {
            public double money = 100000;
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

        [MenuItem("Tools/LingGuang/Smoke test desk games")]
        public static void Menu() { Debug.Log(Run()); }

        public static string Run()
        {
            Log.Length = 0; failures = 0;
            if (Application.isPlaying) return "skipped: Unity is in Play mode";
            float scale = Time.timeScale;
            foreach (var desk in XgCatalog.Desks)
            {
                string game = XgMiniGames.ForDesk(desk.id);
                try { OneDesk(desk.id, game); }
                catch (Exception e) { failures++; Note("EXCEPTION on " + desk.id + ": " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace); }
            }
            Check(Mathf.Approximately(Time.timeScale, scale) && Time.timeScale > 0, "Time.timeScale untouched (" + Time.timeScale + ")");
            return (failures == 0 ? "DESK GAME SMOKE OK" : "DESK GAME SMOKE FAILURES " + failures) + "\n" + Log;
        }

        static void OneDesk(string deskId, string game)
        {
            var canvasGo = new GameObject("XgDeskGameSmoke", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            GameObject camGo = null; RenderTexture rt = null; Texture2D tex = null;
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
                s.S.stage = s.S.stageVision = 6;
                foreach (var d in XgCatalog.Desks) if (!s.S.desksOpen.Contains(d.id)) s.S.desksOpen.Add(d.id);
                s.SelectDesk(deskId);

                var label = new XgLabelPage();
                label.Init(stub, new XgUi(font, null));
                label.Build(area);
                Canvas.ForceUpdateCanvases();
                label.Refresh();
                for (int i = 0; i < 12; i++) label.Tick(.05f);

                var page = Field<XgDeskGamePage>(label, "deskGame");
                bool handwriting = deskId == XgSim.HandwritingDesk;
                if (game == null)
                {
                    Check(!page.root.gameObject.activeSelf, deskId + ": no game, the " + (handwriting ? "writing view / " : "") + "cards stay");
                    Note(deskId + ": no native game (" + (handwriting ? "own writing view" : "classic cards") + ")");
                    return;
                }
                Check(page.root.gameObject.activeSelf && page.Playing && page.SelectedGame == game, deskId + ": " + game + " is the desk's main view");
                var def = XgMiniGames.Find(game);
                Check(def.Desk == deskId, deskId + ": the game labels the desk's own dataset");
                var toggle = Field<XgBtn>(label, "gameToggle");
                Check(!ZhongbaoSkin.Frame(toggle).gameObject.activeSelf, deskId + ": the way back is hidden while playing");

                // One answer through the game's own context: one sample of this desk's dataset, paid.
                var context = Field<XgMiniContext>(page, "context");
                double rows = s.Labels(deskId), total = s.TotalLabels, money = stub.wallet.money;
                context.Report(true);
                Check(s.Labels(deskId) == rows + 1 && s.TotalLabels == total + 1, deskId + ": one answer = one sample of " + deskId);
                Check(stub.wallet.money > money, deskId + ": and it is paid (+¥" + (stub.wallet.money - money).ToString("0.00") + ")");

                for (int i = 0; i < 40; i++) label.Tick(.05f);
                Canvas.ForceUpdateCanvases();

                // Render before flipping.
                void Shoot(string path)
                {
                    foreach (var t in canvasGo.GetComponentsInChildren<TMP_Text>(false)) t.ForceMeshUpdate();
                    Canvas.ForceUpdateCanvases();
                    if (camGo == null)
                    {
                        camGo = new GameObject("ShotCam", typeof(Camera)); camGo.hideFlags = HideFlags.DontSave;
                        var cam = camGo.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 763 / 2f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.white;
                        camGo.transform.position = canvasRt.position + new Vector3(0, 0, -10); cam.nearClipPlane = .1f; cam.farClipPlane = 100;
                        canvas.worldCamera = cam;
                        rt = new RenderTexture(1266, 763, 24); cam.targetTexture = rt;
                        tex = new Texture2D(1266, 763, TextureFormat.RGB24, false);
                    }
                    camGo.GetComponent<Camera>().Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 1266, 763), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                    Note("shot " + path);
                }
                Shoot("/tmp/desk_" + deskId + ".png");

                // The edit-mode juice never ticks, so floating "+¥" texts from earlier answers would sit on top of the next one: clear them.
                void ClearFloaters()
                {
                    var list = Field<System.Collections.IList>(stub.juice, "floaters");
                    foreach (var f in list) { var t = f.GetType().GetField("text", All).GetValue(f) as TMP_Text; if (t != null) t.gameObject.SetActive(false); }
                    list.Clear();
                }
                ClearFloaters();
                // Mid-game states worth a look: a whack with a hat on show, and the quiz after asking for help and after an answer.
                if (deskId == "arith")
                {
                    var moles = Field<XgMiniGame>(page, "game");
                    var holes = Field<XgMoleGraphic[]>(moles, "holes"); var expires = Field<float[]>(moles, "expires");
                    int hit = -1; for (int i = 0; i < holes.Length; i++) if (expires[i] > 0) { hit = i; break; }
                    Check(hit >= 0, "arith: a mole is up after two seconds");
                    if (hit >= 0)
                    {
                        // Put a purple hat on another mole that is really up (its sum stays on the sign); an empty hole is never decorated.
                        for (int i = 0; i < holes.Length; i++) if (i != hit && expires[i] > 0) { holes[i].Ai = true; holes[i].Redraw(); break; }
                        holes[hit].Down.Invoke();
                        label.Tick(.1f);
                        Shoot("/tmp/desk_arith_hit.png");
                    }
                }
                if (deskId == "sense")
                {
                    var quiz = Field<XgMiniGame>(page, "game"); var quizType = quiz.GetType();
                    quizType.GetMethod("UseAi", All).Invoke(quiz, null);
                    label.Tick(.05f);
                    Shoot("/tmp/desk_sense_help.png");
                    var statement = Field<XgPhrase>(quiz, "statement");
                    quizType.GetMethod("Answer", All).Invoke(quiz, new object[] { statement.yes });
                    label.Tick(.05f);
                    Shoot("/tmp/desk_sense_right.png");
                }

                // The yes/no cards stay one toggle away, and back.
                Field<XgBtn>(page, "classic").button.onClick.Invoke();
                label.Tick(.05f);
                Check(!page.root.gameObject.activeSelf, deskId + ": the toggle shows the yes/no cards");
                Check(ZhongbaoSkin.Frame(toggle).gameObject.activeSelf, deskId + ": and a way back to the game");
                toggle.button.onClick.Invoke();
                label.Tick(.05f);
                Check(page.root.gameObject.activeSelf && page.Playing, deskId + ": back in the game");

                // English: no Chinese in the chrome (the games' own bodies are checked separately).
                using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                {
                    stub.sim.English = true;
                    Field<TMP_Text>(page, "status").text = Field<TMP_Text>(page, "speech").text = ""; // event lines are written in the language of their moment
                    label.RebuildTabs(); label.Refresh(); label.Tick(.05f);
                    foreach (string n in new[] { "heading", "status", "payLine", "speech" })
                    {
                        var t = Field<TMP_Text>(page, n);
                        bool leak = false; foreach (char c in t.text ?? "") if (c >= 0x4E00 && c <= 0x9FFF) leak = true;
                        Check(!leak, deskId + ": English " + n + " has no Chinese ('" + t.text + "')");
                    }
                }
                // English game body: reopen the game under the English language and look for Chinese on its stage (poems stay Chinese: they are the content).
                using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                {
                    stub.sim.English = true;
                    page.Stop(); label.Refresh();
                    for (int i = 0; i < 30; i++) label.Tick(.1f);
                    Check(page.Playing, deskId + ": reopened in English");
                    int leaks = 0;
                    foreach (var t in page.root.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (t.transform.parent.name.StartsWith("Poem") || t.name == "Chain" || t.name == "Theme") continue;
                        foreach (char c in t.text ?? "") if (c >= 0x4E00 && c <= 0x9FFF) { leaks++; Note("  Chinese left in " + deskId + ": " + t.name + " '" + t.text.Replace("\n", " | ") + "'"); break; }
                    }
                    Check(leaks == 0, deskId + ": English game body has no Chinese");
                }
                for (int i = 0; i < 10; i++) label.Tick(.1f);
            }
            finally
            {
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }
    }
}
