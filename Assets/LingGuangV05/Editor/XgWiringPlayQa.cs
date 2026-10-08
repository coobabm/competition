using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Play QA for the 接线 page: binds a memory-only stage-5 lab (refuses while the scene runtime has useDiskSave on),
    /// opens the page, and saves what the desktop LCD shows. The LCD render texture is read directly, because
    /// ScreenCapture lags while the editor window is unfocused.
    /// </summary>
    public static class XgWiringPlayQa
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static string Setup()
        {
            var rt = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (!EditorApplication.isPlaying || rt == null || rt.useDiskSave) return "refused: needs Play with useDiskSave off";
            var hub = LingGuangV05.Desktop.YY.YYChatHub.Instance;
            if (hub != null && hub.S != null) hub.S.lingguangUnlocked = true; // skip the YY join cutscene
            var lab = new XgSim();
            var S = lab.S;
            S.stage = 5;
            string[] ds = { "mnist", "logic", "spam", "danmu", "cifar", "translate", "headline", "review" };
            string[] ar = { "lenet", "mlp", "mlp", "lstm", "alexnet", "attention", "mlp", "lstm" };
            double[] acc = { .975, .9, .93, .88, .76, .66, .87, .86 };
            int[] dep = { 2, 3, 3, 2, 6, 4, 3, 2 }; int[] wid = { 3, 4, 4, 4, 5, 5, 4, 4 };
            for (int i = 0; i < ds.Length; i++)
            {
                var d = XgCatalog.Dataset(ds[i]);
                var e = new XgModelEntry { id = S.nextModelId++, track = (int)d.track, dataset = ds[i], arch = ar[i], depth = dep[i], width = wid[i], acc = acc[i], record = true };
                e.name = XgSim.FileName(e);
                S.models.Add(e);
                S.best.Add(new XgBest { dataset = ds[i], arch = ar[i], acc = acc[i], modelId = e.id });
                if (!S.owned.Contains(ds[i])) S.owned.Add(ds[i]);
                if (!S.desksOpen.Contains(ds[i])) S.desksOpen.Add(ds[i]);
                S.labels.Add(new XgLabelCount { dataset = ds[i], count = 300 });
                if (!S.unlocked.Contains(ar[i])) S.unlocked.Add(ar[i]);
            }
            foreach (string c in new[] { "cheque", "civilexam", "homework", "antifraud", "captcha", "danmaku", "crossborder", "clickbait", "fakereview" }) S.contracts.Add(c);
            S.autoLevel = 3; S.epochs = 200; S.assessments = 50;
            S.sequence.arch = "attention"; S.sequence.depth = 4; S.sequence.width = 5; S.sequence.dataset = "translate";
            var house = new LingGuangV05.Core.ChapterOneSim();
            house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 30000;
            house.S.gpuModels.Add("gtx1080"); house.S.gpuCount = house.S.gpuModels.Count;
            house.S.labState = JsonUtility.ToJson(S);
            rt.GetType().GetMethod("Attach", Flags).Invoke(rt, new object[] { house });
            var ctl = rt.GetComponent<XingGuangController>();
            ctl.GetType().GetMethod("BindSave", Flags).Invoke(ctl, new object[] { house });
            ctl.Open();
            ctl.View.ShowTab("wiring");
            var sim = ctl.Sim;
            sim.Connect("cafe", "cafe7", ctl.Host);
            sim.Connect("cafe7", "run:0", ctl.Host);
            sim.S.vision.running = true;
            Show(ctl);
            return "bound stage " + sim.S.stage + ", tab " + ctl.View.Tab;
        }

        /// <summary>The 灵光 window in front and opaque (DreamOS can swallow the opening animation), cards closed.</summary>
        public static void Show(XingGuangController ctl)
        {
            var holo = ctl.View.Holo;
            if (holo != null)
            {
                var pending = holo.GetType().GetField("pending", Flags)?.GetValue(holo) as System.Collections.IList;
                pending?.Clear();
                (holo.GetType().GetField("overlay", Flags)?.GetValue(holo) as RectTransform)?.gameObject.SetActive(false);
            }
            var cg = ctl.Window.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }
            ctl.Window.transform.SetAsLastSibling();
            ctl.Window.FocusToWindow();
        }

        /// <summary>
        /// The real mouse path: a desktop point is turned into the Game-view pixel on the physical LCD panel, and the
        /// LCD's own input bridge (AeroLcdInputSurface) maps it back, raycasts the desktop and sends the events, as it
        /// does for the player's mouse.
        /// </summary>
        static HongmengOS.Aero2010.AeroLcdInputSurface Surface(out Vector2 physical, Vector3 world)
        {
            physical = Vector2.zero;
            var input = Object.FindAnyObjectByType<HongmengOS.Aero2010.AeroLcdInputSurface>();
            if (input == null || input.sourceCamera == null || input.displayRect == null) return null;
            var tex = input.sourceCamera.targetTexture;
            Vector2 src = RectTransformUtility.WorldToScreenPoint(input.sourceCamera, world);
            var uv = new Vector2(src.x / tex.width, src.y / tex.height);
            if (input.flipY) uv.y = 1 - uv.y;
            var rect = input.displayRect.rect;
            var local = new Vector2(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height);
            var canvas = input.displayRect.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            physical = RectTransformUtility.WorldToScreenPoint(cam, input.displayRect.TransformPoint(local));
            return input;
        }

        static UnityEngine.EventSystems.PointerEventData Data(Vector2 physical, float scrollY = 0) =>
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = physical, pointerId = -1, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, scrollDelta = new Vector2(0, scrollY) };

        public static string Click(Vector3 world)
        {
            var input = Surface(out var physical, world);
            if (input == null) return "no LCD input surface";
            input.OnPointerMove(Data(physical));
            input.OnPointerDown(Data(physical));
            input.OnPointerUp(Data(physical));
            return "clicked at " + physical;
        }

        public static string Scroll(Vector3 world, float dy)
        {
            var input = Surface(out var physical, world);
            if (input == null) return "no LCD input surface";
            input.OnPointerMove(Data(physical));
            input.OnScroll(Data(physical, dy));
            return "scrolled at " + physical;
        }

        /// <summary>Moves the pointer over a point (hover).</summary>
        public static string Move(Vector3 world)
        {
            var input = Surface(out var physical, world);
            if (input == null) return "no LCD input surface";
            input.OnPointerEnter(Data(physical));
            input.OnPointerMove(Data(physical));
            return "moved to " + physical;
        }

        public static string Capture(string file)
        {
            var rt = Object.FindAnyObjectByType<ChapterOneRuntime>();
            var ctl = rt != null ? rt.GetComponent<XingGuangController>() : null;
            if (ctl != null) Show(ctl);
            var cam = GameObject.Find("LCD Source Camera").GetComponent<Camera>();
            var target = cam.targetTexture;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return file;
        }
    }
}
