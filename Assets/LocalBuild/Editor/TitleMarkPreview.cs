using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Temporary Editor QA helper; copy into Assets/LocalBuild/Editor only for the parent's verification.
// Does not call FontBootPreview, build fonts, save assets, read/write player saves or access PlayerPrefs.
public static class TitleMarkPreview
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void CaptureAndExit()
    {
        int code = 0;
        try { Capture(); }
        catch (Exception error) { Debug.LogException(error); code = 1; }
        EditorApplication.Exit(code);
    }

    [MenuItem("Tools/LingGuang/QA/Title Mark Preview")]
    public static void Capture()
    {
        string output = Argument("-titleMarkOutput");
        if (string.IsNullOrWhiteSpace(output)) output = Path.Combine("LocalVerification", "TitleMark", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        output = Path.GetFullPath(output); Directory.CreateDirectory(output);
        var report = new StringBuilder("Actual CaseMenuHud Chrome + title actions on a neutral dark background. Not the 3D menu or a Player run.\n");
        report.AppendLine("Full Build/BuildSettings deliberately omitted because they read PlayerPrefs through GamePerformance.");
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject cameraObject = null, host = null;
        try
        {
            using (GameText.Bind(() => GameText.Chinese))
            {
                cameraObject = new GameObject("TitleMark QA camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene; camera.enabled = false; camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .043f, .041f, 1);
                camera.cullingMask = 1 << 30;

                host = new GameObject("Temporary actual CaseMenuHud");
                SceneManager.MoveGameObjectToScene(host, scene);
                var hud = host.AddComponent<CaseMenuHud>();
                var rootObject = new GameObject("CaseMenuOverlay", typeof(RectTransform), typeof(Canvas));
                rootObject.transform.SetParent(host.transform, false);
                var canvas = rootObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = 1; canvas.sortingOrder = 32000;
                // Explicit reference scaling avoids Editor GameView dimensions affecting offscreen capture.
                var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
                var primary = XgUiV4Fonts.Sans != null ? XgUiV4Fonts.Sans : fallback;
                Need(primary != null, "existing primary font available");
                typeof(CaseMenuHud).GetField("font", Private).SetValue(hud, primary);
                typeof(CaseMenuHud).GetField("canvasRoot", Private).SetValue(hud, rootObject.GetComponent<RectTransform>());
                Invoke(hud, "BuildChrome"); Invoke(hud, "BuildTitleActions");
                hud.SetChromeAlpha(1);
                foreach (var t in host.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                var title = host.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "Title");
                var expected = XgUiV4Fonts.SansSemibold != null ? XgUiV4Fonts.SansSemibold : primary;
                Need(title.text == LingGuangV05.Core.AppNames.GameZh, "title text unchanged");
                Need(title.font == expected, "original title face unchanged");
                Need(title.fontStyle == FontStyles.Normal && title.fontWeight == FontWeight.Regular, "original title style unchanged");
                Need(title.enableAutoSizing && title.fontSizeMin == 42 && title.fontSizeMax == 66 && title.characterSpacing == 3, "original size and tracking unchanged");
                Need(title.rectTransform.sizeDelta == new Vector2(610,124) && title.rectTransform.anchoredPosition == new Vector2(64,-98), "original title rectangle unchanged");
                var mark = host.GetComponentsInChildren<Graphic>(true).Single(g => g.name == "TitleSignal");
                Need(!host.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.name == "AiContentNotice"), "title menu has no separate AI disclosure heading");
                var disclosure = host.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "AiContentNoticeBody");
                Need(disclosure.text == AiContentNotice.Description(false), "title disclosure contains the current model description");
                Need(disclosure.fontSize == 14 && disclosure.color == host.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "KeyboardHint").color, "disclosure uses the existing muted small-text style");
                Need(disclosure.rectTransform.anchorMin == Vector2.zero && disclosure.rectTransform.anchoredPosition == new Vector2(70, 22), "disclosure sits at bottom left");
                Need(!mark.raycastTarget, "decoration cannot intercept input");
                Need(mark.GetComponent<Selectable>() == null, "decoration adds no control");
                foreach (int width in new[] { 1920, 1280 })
                {
                    int height = width * 9 / 16;
                    var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                    Texture2D pixels = null;
                    try
                    {
                        texture.Create(); camera.targetTexture = texture; camera.aspect = (float)width / height;
                        canvas.scaleFactor = width / 1920f;
                        Canvas.ForceUpdateCanvases();
                        foreach (var label in host.GetComponentsInChildren<TextMeshProUGUI>(true)) label.ForceMeshUpdate(true, true);
                        mark.SetVerticesDirty(); Canvas.ForceUpdateCanvases();
                        camera.Render();
                        var mesh = mark.canvasRenderer.GetMesh();
                        Need(mesh != null && mesh.vertexCount > 0 && mesh.triangles.Length > 0, "CanvasRenderer has actual decoration mesh");
                        report.AppendLine(width + "x" + height + ": title=" + title.text + ", font=" + title.font.name + ", size=" + title.fontSize + ", vertices=" + mesh.vertexCount + ", raycast=" + mark.raycastTarget);
                        Need(!title.isTextOverflowing, "title remains fully readable");
                        foreach (bool english in new[] { false, true })
                        {
                            disclosure.text = AiContentNotice.Description(english);
                            disclosure.ForceMeshUpdate(true, true);
                            Need(!disclosure.isTextOverflowing, "disclosure fits in " + (english ? "English" : "Chinese"));
                        }
                        disclosure.text = AiContentNotice.Description(false);
                        disclosure.ForceMeshUpdate(true, true);
                        camera.Render();
                        pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                        var previous = RenderTexture.active;
                        try { RenderTexture.active = texture; pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply(); }
                        finally { RenderTexture.active = previous; }
                        File.WriteAllBytes(Path.Combine(output, "title-hud-" + width + "x" + height + ".png"), pixels.EncodeToPNG());
                    }
                    finally
                    {
                        camera.targetTexture = null;
                        if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                        texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
            report.AppendLine("PASS: assertions completed. No settings or runtime archive callbacks invoked.");
        }
        catch (Exception error) { report.AppendLine("FAIL: " + error); throw; }
        finally
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllText(Path.Combine(output, "report.txt"), report.ToString());
            Debug.Log("[TitleMarkPreview] " + output);
        }
    }

    static void Need(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
    static void Invoke(CaseMenuHud hud, string method) { typeof(CaseMenuHud).GetMethod(method, Private).Invoke(hud, null); }
    static string Argument(string name)
    {
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == name) return arguments[i + 1];
        return null;
    }
}
