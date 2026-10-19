using System.IO;
using System.Text;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// Editor hooks for the live origin film trial (<see cref="OriginLive"/>): play it in Play mode, or render single frames to PNG
    /// without Play mode (offscreen camera, like the other edit-mode smoke tools) for comparison with the Remotion stills.
    /// Menu: Tools / LingGuang / Origin live – preview, Origin live – render stills.
    /// </summary>
    public static class OriginLiveTools
    {
        const string PreviewFlag = "OriginLivePreview";
        public static readonly float[] StillSeconds = { 1f, 4f, 8f, 12f, 15.5f, 16.8f };

        [MenuItem("Tools/LingGuang/Origin live – preview")]
        public static void Preview()
        {
            if (EditorApplication.isPlaying) { OriginLivePreview.Open(); return; }
            SessionState.SetBool(PreviewFlag, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PreviewFlag, false)) return;
                SessionState.SetBool(PreviewFlag, false);
                EditorApplication.delayCall += () => { if (EditorApplication.isPlaying) OriginLivePreview.Open(); };
            };
        }

        [MenuItem("Tools/LingGuang/Origin live – render stills")]
        public static void RenderStillsMenu() { Debug.Log(RenderStills()); }

        /// <summary>Renders the six comparison frames (1, 4, 8, 12, 15.5, 16.8 s) to /tmp/origin_live_&lt;sec&gt;.png at 1920×1080.</summary>
        public static string RenderStills()
        {
            return RenderFrames(StillSeconds, "/tmp/origin_live_", 1920, 1080);
        }

        /// <summary>The still sweep: one still every 5 s of the cut (→ /tmp/origin_sweep_&lt;sec&gt;.png), in Chinese or English.</summary>
        public static string RenderSweep(bool english = false)
        {
            var secs = new System.Collections.Generic.List<float>();
            for (int t = 0; t <= OriginLive.Duration; t += 5) secs.Add(t);
            return RenderFramesIn(secs.ToArray(), english ? "/tmp/origin_sweep_en_" : "/tmp/origin_sweep_", 1920, 1080, english);
        }

        [MenuItem("Tools/LingGuang/Origin live – render sweep")]
        public static void RenderSweepMenu() { Debug.Log(RenderSweep()); }

        /// <summary>
        /// Every frame of the cut at fps to dir/f_00000.png… (for an mp4 preview with the music: ffmpeg -framerate fps -i f_%05d.png).
        /// Frames from..to (exclusive) so a long render can be split across calls.
        /// </summary>
        public static string RenderSequence(string dir, int fps, int width, int height, int from = 0, int to = int.MaxValue)
        {
            Directory.CreateDirectory(dir);
            int count = Mathf.CeilToInt(OriginLive.Duration * fps);
            to = Mathf.Min(to, count);
            var secs = new System.Collections.Generic.List<float>();
            for (int i = from; i < to; i++) secs.Add(i / (float)fps);
            string log = RenderFrames(secs.ToArray(), Path.Combine(dir, "t_"), width, height);
            // RenderFrames names by seconds; rename to a frame index for ffmpeg.
            for (int i = from; i < to; i++)
            {
                string src = Path.Combine(dir, "t_" + (i / (float)fps).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                string dst = Path.Combine(dir, "f_" + i.ToString("00000") + ".png");
                if (File.Exists(dst)) File.Delete(dst);
                if (File.Exists(src)) File.Move(src, dst);
            }
            return (to - from) + " frames → " + dir + (log.StartsWith("skipped") ? " (" + log.Trim() + ")" : "");
        }

        /// <summary>RenderFrames with the game language forced to English or Chinese for the duration.</summary>
        public static string RenderFramesIn(float[] seconds, string prefix, int width, int height, bool english)
        {
            using (LingGuangV05.Runtime.GameText.Bind(() => english ? LingGuangV05.Runtime.GameText.English : LingGuangV05.Runtime.GameText.Chinese)) return RenderFrames(seconds, prefix, width, height);
        }

        /// <summary>Renders the given film times to prefix + seconds + ".png" with an offscreen camera. No Play mode, no audio, nothing saved.</summary>
        public static string RenderFrames(float[] seconds, string prefix, int width, int height)
        {
            if (Application.isPlaying) return "skipped: Unity is in Play mode";
            var log = new StringBuilder();
            var canvasGo = new GameObject("OriginLiveStills", typeof(Canvas));
            GameObject camGo = null; RenderTexture rt = null;
            try
            {
                canvasGo.hideFlags = HideFlags.HideAndDontSave;
                var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var canvasRt = (RectTransform)canvasGo.transform; canvasRt.sizeDelta = new Vector2(width, height);
                var live = OriginLive.Create(canvasRt);
                camGo = new GameObject("OriginLiveCam", typeof(Camera)); camGo.hideFlags = HideFlags.HideAndDontSave;
                var cam = camGo.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = height / 2f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                cam.transform.position = canvasRt.position + new Vector3(0, 0, -10); cam.nearClipPlane = .1f; cam.farClipPlane = 100;
                canvas.worldCamera = cam;
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); cam.targetTexture = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                foreach (float t in seconds)
                {
                    live.Render(t);
                    Canvas.ForceUpdateCanvases();
                    foreach (var text in canvasGo.GetComponentsInChildren<TMPro.TMP_Text>(false)) text.ForceMeshUpdate();
                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
                    RenderTexture.active = prev;
                    string path = prefix + t.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ".png";
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    log.AppendLine(path);
                }
                Object.DestroyImmediate(tex);
            }
            finally
            {
                if (camGo != null) Object.DestroyImmediate(camGo);
                if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                Object.DestroyImmediate(canvasGo);
            }
            return log.ToString();
        }
    }
}
