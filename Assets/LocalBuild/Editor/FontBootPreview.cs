using System;
using System.IO;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Read-only visual check for the UiV4 font weights, the title HUD font and the LCD boot screen.
/// Builds everything in a throwaway scene object, renders through an off-screen camera and writes PNGs to
/// LocalVerification/FontBootPreview/&lt;stamp&gt;/. Touches no save data and no scene files.
/// Batch: -executeMethod FontBootPreview.CaptureAndExit
/// </summary>
public static class FontBootPreview
{
    const int W = 1920, H = 1080;

    public static void CaptureAndExit()
    {
        int code = 0;
        try { Capture(); }
        catch (Exception e) { Debug.LogException(e); code = 1; }
        EditorApplication.Exit(code);
    }

    [MenuItem("Tools/LingGuang/QA/Font + Boot Preview")]
    public static void Capture()
    {
        LingGuangV05.EditorTools.UiV4FontWeights.Build();
        string dir = Path.Combine("LocalVerification", "FontBootPreview", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(dir);
        var desktopCjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
        var report = new System.Text.StringBuilder();
        report.AppendLine("UiSans=" + Name(XgUiV4Fonts.Sans) + " Medium=" + Name(XgUiV4Fonts.SansMedium) + " Semibold=" + Name(XgUiV4Fonts.SansSemibold));
        if (XgUiV4Fonts.Sans != null)
            report.AppendLine("UiSans weight[5]=" + Name(XgUiV4Fonts.Sans.fontWeightTable[5].regularTypeface) + " [7]=" + Name(XgUiV4Fonts.Sans.fontWeightTable[7].regularTypeface));

        var camGo = new GameObject("FontBootPreview Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.02f, .03f, .04f, 1);
        cam.orthographic = true; cam.enabled = false;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        try
        {
            // 1) LCD boot screen at four stages.
            var host = NewCanvas("Boot Host", cam);
            var display = new GameObject("LCD Display", typeof(RectTransform), typeof(RawImage));
            var displayRect = (RectTransform)display.transform;
            displayRect.SetParent(host.transform, false);
            displayRect.anchorMin = Vector2.zero; displayRect.anchorMax = Vector2.one; displayRect.offsetMin = displayRect.offsetMax = Vector2.zero;
            display.GetComponent<RawImage>().color = Color.black;
            Canvas.ForceUpdateCanvases();
            var boot = host.AddComponent<CaseMenuBootScreen>();
            boot.Build(displayRect, desktopCjk);
            boot.Show();
            foreach (var p in new[] { .10f, .36f, .66f, .90f })
            {
                boot.SetProgress(p, 1f);
                Shoot(cam, rt, Path.Combine(dir, $"boot-{Mathf.RoundToInt(p * 100):00}.png"));
            }
            UnityEngine.Object.DestroyImmediate(host);

            // 2) Title HUD over a neutral backdrop.
            var hudGo = new GameObject("CaseMenuHud Preview");
            var hud = hudGo.AddComponent<CaseMenuHud>();
            hud.Build(desktopCjk, null, null, null, null, null);
            foreach (var canvas in hudGo.GetComponentsInChildren<Canvas>(true))
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1; }
            cam.backgroundColor = new Color(.16f, .12f, .09f, 1);
            Shoot(cam, rt, Path.Combine(dir, "title-hud.png"));
            foreach (var label in hudGo.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == "Title") report.AppendLine($"Title font={Name(label.font)} size={label.fontSize:0.#} overflowing={label.isTextOverflowing}");
            UnityEngine.Object.DestroyImmediate(hudGo);

            // 3) .exe type specimen: regular, <b> (now real Semibold through the weight table), Medium.
            cam.backgroundColor = new Color(.05f, .086f, .086f, 1);
            var spec = NewCanvas("Specimen", cam);
            string[] lines =
            {
                "灵光.exe · 深度学习工作站",
                "<b>常识判断</b>  判断题目对错；快答有奖励，慢慢想也没关系。",
                "<b>刚从冰箱拿出来的冰棍是热的。</b>",
                "<b>下一项能力 · 多选一</b>   参数 0·?   样本 0·?",
                "<font-weight=500>任务大厅  按酬劳排序  ¥1.25/题</font-weight>",
                "<b>训练一轮</b>  第 4 轮 · 电费 ¥1.6",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                var t = Text(spec.transform, lines[i], XgUiV4Fonts.Sans ?? desktopCjk, i == 0 ? 26 : 34, new Vector2(80, -70 - i * 92));
                t.color = i == 0 ? new Color(.55f, .70f, .72f) : new Color(.92f, .95f, .93f);
            }
            var faux = Text(spec.transform, "仿粗体对照（旧）：<b>常识判断 · 训练一轮</b>", desktopCjk, 34, new Vector2(80, -70 - lines.Length * 92 - 40));
            faux.color = new Color(.85f, .55f, .45f);
            Shoot(cam, rt, Path.Combine(dir, "exe-specimen.png"));
            UnityEngine.Object.DestroyImmediate(spec);

            // 4) Small-size sharpness study at the 720p QA scale (canvas scale 2/3): material copies only, assets untouched.
            cam.backgroundColor = new Color(.05f, .086f, .086f, 1);
            var study = NewCanvas("Sharpness", cam);
            var ss = study.GetComponent<CanvasScaler>(); ss.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; ss.scaleFactor = 1f;
            float[] sharp = { 0f, .25f, .5f, .75f };
            var temp = new System.Collections.Generic.List<Material>();
            for (int k = 0; k < sharp.Length; k++)
            {
                var col = new GameObject("col" + k, typeof(RectTransform)); col.transform.SetParent(study.transform, false);
                col.transform.localScale = Vector3.one * (2f / 3f);
                var cr = (RectTransform)col.transform; cr.anchorMin = cr.anchorMax = new Vector2(0, 1); cr.pivot = new Vector2(0, 1);
                cr.anchoredPosition = new Vector2(40 + (k % 2) * 950, -40 - (k / 2) * 520); cr.sizeDelta = new Vector2(1400, 760);
                Material regular = new Material(XgUiV4Fonts.Sans.material); regular.SetFloat("_Sharpness", sharp[k]); temp.Add(regular);
                Material semi = new Material(XgUiV4Fonts.SansSemibold.material); semi.SetFloat("_Sharpness", sharp[k]); temp.Add(semi);
                string[] specimen =
                {
                    "_Sharpness = " + sharp[k],
                    "灵光.exe · 深度学习工作站  下一项能力 · 长记忆·翻译",
                    "已有 4 · 没有拔掉 · 全部算数   参数量 0·?  样本 4·?",
                    "还考不了「长记忆·翻译」：接着练，接着喂数据",
                    "常识判断  新题 62.5%  练过的 83.9%  最佳 D 260",
                    "一年有 13 个月。  长颈鹿的脖子很长。  它答 是",
                };
                for (int i = 0; i < specimen.Length; i++)
                {
                    bool bold = i == 4 || i == 0;
                    var t = Text(col.transform, specimen[i], bold ? XgUiV4Fonts.SansSemibold : XgUiV4Fonts.Sans, i == 0 ? 22 : (i % 2 == 0 ? 13 : 14) * 1.5f, new Vector2(0, -i * 64));
                    t.fontSharedMaterial = bold ? semi : regular;
                    t.color = i == 0 ? new Color(.95f, .75f, .4f) : new Color(.86f, .9f, .88f);
                }
            }
            Shoot(cam, rt, Path.Combine(dir, "sharpness-study.png"));
            UnityEngine.Object.DestroyImmediate(study);
            foreach (var m in temp) UnityEngine.Object.DestroyImmediate(m);
        }
        finally
        {
            cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(camGo);
        }
        File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString());
        Debug.Log("[FontBootPreview] wrote " + dir + "\n" + report);
    }

    static string Name(UnityEngine.Object o) => o == null ? "null" : o.name;

    static GameObject NewCanvas(string name, Camera cam)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(W, H);
        return go;
    }

    static TextMeshProUGUI Text(Transform parent, string value, TMP_FontAsset font, float size, Vector2 topLeft)
    {
        var go = new GameObject("Line", typeof(RectTransform));
        var r = (RectTransform)go.transform; r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchoredPosition = topLeft; r.sizeDelta = new Vector2(1760, 80);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font; t.fontSize = size; t.text = value; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return t;
    }

    static void Shoot(Camera cam, RenderTexture rt, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) t.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
        cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }
}
