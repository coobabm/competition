using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Builds the 训练 page in Edit mode, on an in-memory lab and household (no disk save, no scene change, nothing
    /// persisted), and renders it to a PNG so the layout can be looked at without entering Play mode.
    /// Invoke <see cref="Render"/> through Editor tooling: scenario 0 = an early lab (stage 2), 1 = stage 6; pageName "train" or "items".
    /// </summary>
    public static class XgTrainPageLayoutQa
    {
        sealed class Stub : IXgPageHost
        {
            public XgSim sim; public IXgHost host; public string tab = "train";
            public XingGuangController Controller => null;
            public XgSim Sim => sim;
            public IXgHost Host => host;
            public XgJuice Juice => null;
            public int BuyAmount { get; set; } = 1;
            public bool Visible => true;
            public string Tab => tab;
            public RectTransform ComboChip => null;
            public void Refresh(bool force) { }
            public void ShowToast(string text, float seconds) { }
            public void ShowTab(string id) { tab = id; }
            public bool BuyNode(XgNode node, RectTransform from) => sim.BuyNode(node.id, host);
        }

        public static string Render(string path, int scenario = 0, bool dataView = false, float probeX = -1, float probeY = -1, bool english = false, string pageName = "train")
        {
            if (EditorApplication.isPlaying) return "refused: Unity is in Play mode";
            var temp = new GameObject("XgTrainPageLayoutQa") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture target = null;
            TMP_FontAsset font = null;
            var languageScope = english ? GameText.Bind(() => GameText.English) : null;
            var previousLang = Lang.IsEnglish;
            if (english) Lang.IsEnglish = () => true;
            try
            {
                var cameraGo = new GameObject("Camera"); cameraGo.transform.SetParent(temp.transform);
                var cam = cameraGo.AddComponent<Camera>();
                cam.orthographic = true; cam.cullingMask = 1 << LayerMask.NameToLayer("UI"); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(7, 12, 18, 255);
                target = new RenderTexture(1236, 693, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = target;
                var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") }; canvasGo.transform.SetParent(temp.transform);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                var canvasRect = (RectTransform)canvasGo.transform;
                // A dynamic font made from the project's CJK face, so the Chinese text draws (kept in memory only).
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Emergence/Resources/Fonts/NotoSansCJKsc-Regular.otf");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.hideFlags = HideFlags.HideAndDontSave;

                // The page area: the window minus the 52 px top bar, the 148 px nav and the 12 px margins.
                var back = XgUi.Rect("Back", canvasRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                XgUi.Panel(back, XgDark.Page);
                var area = XgUi.Rect("Pages", back, Vector2.zero, Vector2.one, new Vector2(148 + 12, 12), new Vector2(-12, -52 - 12));

                // An in-memory household: rent is being billed, a few cards, some money.
                var runtimeGo = new GameObject("Runtime"); runtimeGo.transform.SetParent(temp.transform);
                var runtime = runtimeGo.AddComponent<ChapterOneRuntime>();
                runtime.useDiskSave = false;
                runtime.EnsureInitialized();
                var house = runtime.Sim;
                house.S.prologue = 2; house.S.appInstallState = 2;
                GameCalendar.Advance(house.S, GameCalendar.DayIndex(new DateTime(2016, scenario == 0 ? 6 : 11, 20)));
                house.SettleCalendar();
                house.S.money = scenario == 0 ? 1180 : 41200;
                house.S.monthKwh = scenario == 0 ? 38 : 312; house.S.kwhMonth = (scenario == 0 ? 201606 : 201611);
                house.S.billsToday.power = scenario == 0 ? 22.4 : 186;
                var host = new XingGuangHost(runtime);

                var sim = new XgSim { English = english };
                sim.S.desksOpen.Add("mnist");
                sim.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 400 });
                foreach (string id in new[] { "mlp", "lenet", "dropout", "relu" }) if (!sim.Has(id)) sim.S.unlocked.Add(id);
                if (scenario == 1)
                {
                    foreach (string id in new[] { "transformer", "warmup", "resnet", "cudnn", "augment" }) if (!sim.Has(id)) sim.S.unlocked.Add(id);
                    sim.S.stage = 6; sim.S.trainedParamsK = 310;
                    sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300000 });
                }
                sim.S.consumables.Add(new XgStack { id = "offpeak", count = 2 });
                sim.S.consumables.Add(new XgStack { id = "redbull", count = 4, cooldown = 9 });
                sim.S.consumables.Add(new XgStack { id = "ckpt", count = 1 });
                sim.S.consumables.Add(new XgStack { id = "clean", count = 1 });
                sim.S.offpeakRounds = 7; sim.S.redbullSeconds = 42; sim.S.rollbackGuards = 1;
                for (int i = 0; i < 24; i++) sim.TrainEpoch(XgTrack.Vision, host);

                var stub = new Stub { sim = sim, host = host };
                var ui = new XgUi(font, null, true);
                XgPage page = pageName == "items" ? (XgPage)new XgItemsPage() : new XgTrainPage();
                stub.tab = pageName;
                page.Init(stub, ui);
                page.Build(area);
                page.root.gameObject.name = pageName;
                if (pageName == "train") typeof(XgTrainPage).GetField("dataView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(page, dataView);
                page.Refresh();
                Canvas.ForceUpdateCanvases();
                page.Refresh();
                Canvas.ForceUpdateCanvases();
                // A long page (the items shelves) is shown from its bottom.
                if (pageName == "items") foreach (var scroll in page.root.GetComponentsInChildren<ScrollRect>()) scroll.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                string probe = "";
                if (probeX >= 0)
                {
                    // What is drawn under a pixel (from the top-left of the image): the graphics whose rectangle contains it, top-most last.
                    var found = new System.Collections.Generic.List<string>();
                    foreach (var g in page.root.GetComponentsInChildren<Graphic>(false))
                    {
                        var corners = new Vector3[4];
                        g.rectTransform.GetWorldCorners(corners);
                        var a = cam.WorldToScreenPoint(corners[0]); var b = cam.WorldToScreenPoint(corners[2]);
                        if (probeX >= a.x && probeX <= b.x && target.height - probeY >= a.y && target.height - probeY <= b.y)
                        {
                            string name = g.name; var t = g.transform.parent;
                            for (int i = 0; i < 3 && t != null; i++, t = t.parent) name = t.name + "/" + name;
                            found.Add(name + " " + g.GetType().Name + " " + Mathf.RoundToInt(b.x - a.x) + "x" + Mathf.RoundToInt(b.y - a.y) + " " + g.color);
                        }
                    }
                    probe = "\n" + string.Join("\n", found);
                }
                return "ok " + path + probe;
            }
            catch (Exception e) { return "error: " + e; }
            finally
            {
                Lang.IsEnglish = previousLang; languageScope?.Dispose();
                if (font != null) { UnityEngine.Object.DestroyImmediate(font.material); UnityEngine.Object.DestroyImmediate(font); }
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                UnityEngine.Object.DestroyImmediate(temp);
            }
        }
    }
}
