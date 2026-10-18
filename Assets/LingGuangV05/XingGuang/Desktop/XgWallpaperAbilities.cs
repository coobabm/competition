using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWallpaperAbilities : MaskableGraphic
    {
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgBrainWallpaperPresentation presentation;
        readonly bool[] learned = new bool[XgSim.AbilityCount];
        readonly TMP_Text[] names = new TMP_Text[XgSim.AbilityCount];
        readonly TMP_Text[] progress = new TMP_Text[XgSim.AbilityCount];
        float nextRefresh;int lastStage=-1,lastNext=-1,lastLearned=-1;double lastParams=-1,lastSamples=-1;bool lastEnglish;
        public int LearnedCount { get; private set; }
        public int NextAbility { get; private set; }
        public float ParameterProgress { get; private set; }
        public float DataProgress { get; private set; }
        static Color ParamsInk => new Color32(157, 150, 242, 255);
        static Color DataInk => new Color32(81, 192, 187, 255);

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Ability Scale", wallpaper.transform, new Vector2(.31f, .1f), new Vector2(.73f, .19f), Vector2.zero, Vector2.zero);
            var view = rect.gameObject.AddComponent<XgWallpaperAbilities>();
            view.controller = controller; view.wallpaper = wallpaper; view.raycastTarget = false;
            view.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>();
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                view.names[i] = view.Label("Ability " + (i + 1), i, .52f, 1, 13);
                view.progress[i] = view.Label("Progress " + (i + 1), i, -.25f, .02f, 10);
            }
        }

        TMP_Text Label(string name, int index, float bottom, float top, int size)
        {
            var text = PrologueDesk.Rect(name, transform, new Vector2(index / 6f, bottom), new Vector2((index + 1) / 6f, top),
                new Vector2(4, 0), new Vector2(-4, 0)).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = PrologueDesk.CjkFont(); text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false; text.fontSizeMin = 8; text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false;
            return text;
        }

        void Update()
        {
            if (wallpaper == null || controller?.Sim == null) return;
            float alpha = wallpaper.Reveal.Visible ? Mathf.SmoothStep(0, 1, (wallpaper.Reveal.Age - XgWallpaperReveal.CompleteSeconds) / .8f) : 0;
            alpha *= presentation != null ? presentation.LightLevel : 1;
            canvasRenderer.SetAlpha(alpha);
            for (int i = 0; i < names.Length; i++) { names[i].canvasRenderer.SetAlpha(alpha); progress[i].canvasRenderer.SetAlpha(alpha); }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .1f;
            var sim = controller.Sim;
            int mask=0;for(int i=1;i<=XgSim.AbilityCount;i++)if(sim.HasAbility(i))mask|=1<<i;
            if(lastStage==sim.S.stage&&lastNext==sim.NextAbility&&lastParams==sim.TrainedParamsK&&lastSamples==sim.TrainedSamples&&lastEnglish==GameText.IsEnglish&&lastLearned==mask)return;
            lastStage=sim.S.stage;lastNext=sim.NextAbility;lastParams=sim.TrainedParamsK;lastSamples=sim.TrainedSamples;lastEnglish=GameText.IsEnglish;lastLearned=mask;
            LearnedCount = 0; NextAbility = sim.NextAbility;
            // How much is trained, never how far to the hidden line (XgSim.Learned.cs).
            ParameterProgress = NextAbility > 0 ? (float)sim.ParamsFill : 0;
            DataProgress = NextAbility > 0 ? (float)sim.SamplesFill : 0;
            for (int i = 0; i < learned.Length; i++)
            {
                learned[i] = sim.HasAbility(i + 1); if (learned[i]) LearnedCount++;
                bool current = i + 1 == NextAbility;
                names[i].text = XgSim.AbilityName(i + 1, GameText.IsEnglish);
                names[i].color = learned[i] ? XgCortexGraphic.N.Teal : current ? XgCortexGraphic.N.Gold : new Color32(93, 113, 130, 255);
                progress[i].text = current ? "<color=#9D96F2>P " + XgSim.ParamsText(sim.TrainedParamsK) + "</color>  <color=#51C0BB>D " + XgSim.SamplesText(sim.TrainedSamples) + "</color>  ?" : "";
            }
            SetVerticesDirty();
        }

        public Vector2 LampPoint(int index)
        {
            var rect = rectTransform.rect;
            return rect.min + Vector2.Scale(rect.size, new Vector2((index + .5f) / 6, .48f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect = rectTransform.rect;
            float width = rect.width / 6, scale = rect.height / 97.2f;
            for (int i = 0; i < learned.Length; i++)
            {
                var point = LampPoint(i); float half = width * .36f;
                var ink = learned[i] ? XgCortexGraphic.N.Teal : (Color)new Color32(36, 54, 67, 255);
                if (learned[i]) { var glow = ink; glow.a = .12f; XgDraw.Seg(vh, point - Vector2.right * half, point + Vector2.right * half, 12 * scale, glow); }
                XgDraw.Seg(vh, point - Vector2.right * half, point + Vector2.right * half, 3 * scale, ink);
                if (i + 1 != NextAbility) continue;
                Bar(vh, point + Vector2.down * rect.height * .17f, half, ParameterProgress, ParamsInk, scale);
                Bar(vh, point + Vector2.down * rect.height * .32f, half, DataProgress, DataInk, scale);
            }
        }

        static void Bar(VertexHelper vh, Vector2 point, float half, float value, Color ink, float scale)
        {
            Vector2 left = point - Vector2.right * half;
            XgDraw.Seg(vh, left, point + Vector2.right * half, 4 * scale, new Color32(25, 40, 51, 255));
            if (value > 0) XgDraw.Seg(vh, left, left + Vector2.right * (2 * half * Mathf.Clamp01(value)), 4 * scale, ink);
        }
    }
}
