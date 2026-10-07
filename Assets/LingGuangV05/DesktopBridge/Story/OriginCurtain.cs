using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using Michsky.DreamOS;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The curtain call after the ending: the inner voice wonders how it got here, the 对话 page offers
    /// 「你是怎么被训练出来的？」, it answers 「好，我来给你解释」, and the desktop's own video player (restyled as 2016's
    /// 暴风影音) plays the evolution video (StreamingAssets/LingGuangV05/Video/evolution.mp4) full screen; Esc drops
    /// to the normal window, 全屏 goes back. The video has Chinese captions burned in; in English an English line
    /// covers them. Afterwards 「我从哪儿来.mp4」 stays on the desktop to watch again, and it asks
    /// 「你觉得呢？」 back. The rules and the once-only bookkeeping live in XgSim.Origin.cs.
    /// </summary>
    public sealed class OriginCurtain : MonoBehaviour
    {
        const string VideoPath = "LingGuangV05/Video/evolution.mp4";
        const float ReplyToVideo = 1.8f, HintAfterEnding = 60f;

        StoryDesktopPresenter presenter;
        ChapterOneRuntime runtime;
        XingGuangController lab;
        XgSim bound;
        float playAt = -1, readySince = -1;
        bool hintQueued, promiseKept;

        RectTransform fileIcon, overlay, overlayBand, windowBand, fullscreenButton;
        TMP_Text overlaySubtitle, windowSubtitle, overlayClock;
        VideoPlayerManager native;
        WindowManager nativeWindow;
        bool watching, styled;
        string url;
        readonly List<(double start, double end, string text)> english = new List<(double, double, string)>();

        public static OriginCurtain Install(StoryDesktopPresenter host)
        {
            var c = host.GetComponent<OriginCurtain>() ?? host.gameObject.AddComponent<OriginCurtain>();
            c.presenter = host;
            return c;
        }

        static string T(string zh, string en) => GameText.T(zh, en);
        /// <summary>The origin video is playing in the native player (full screen or windowed).</summary>
        public bool Playing => watching;

        void Update()
        {
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            if (lab == null || lab.Sim == null) return;
            if (!ReferenceEquals(bound, lab.Sim)) Rebind(lab.Sim);
            var desk = PrologueDirector.Desk;
            if (desk == null) return;

            if (bound.OriginReady && runtime != null && runtime.Sim != null && runtime.Sim.S.endingPlayed && !EndingOnScreen())
            {
                if (readySince < 0) readySince = Time.unscaledTime;
                // The inner voice wonders once the ending has fully played; it is marked only when actually seen.
                if (!hintQueued && !bound.S.originHinted && !bound.S.originExplained && Time.unscaledTime - readySince >= HintAfterEnding)
                {
                    hintQueued = true;
                    var sim = bound;
                    InnerVoice.Say("半年。", "Half a year.", 2.2f, () => { if (ReferenceEquals(sim, bound)) sim.TakeOriginHint(); });
                    InnerVoice.Say("它是怎么从「是。否。」走到今天的？", "How did it get from \"yes. no.\" to here?");
                    InnerVoice.Say("……问问它。", "…Ask it.", 2f);
                }
            }
            else readySince = -1;
            if (hintQueued && !InnerVoice.Busy) hintQueued = false;

            if (bound.S.originVideoSeen && fileIcon == null) MakeFileIcon(desk);
            // It promised to explain but the video never ran (the reply came during a save switch or before a reload): run it now, once.
            if (!promiseKept && bound.S.originExplained && !bound.S.originVideoSeen && !watching && playAt < 0) playAt = Time.unscaledTime + ReplyToVideo;
            if (playAt >= 0 && Time.unscaledTime >= playAt && !watching && !EndingOnScreen()) { playAt = -1; promiseKept = true; Play(); }
            if (watching) TickPlayer();
        }

        bool EndingOnScreen()
        {
            var director = presenter != null ? presenter.GetComponent<PrologueDirector>() : null;
            return director != null && director.EndingPlaying || presenter != null && presenter.CutscenePlaying || AutoLabelEpiphany.Playing;
        }

        void Rebind(XgSim sim)
        {
            if (bound != null) bound.OriginVideoRequested -= OnRequested;
            bound = sim;
            bound.OriginVideoRequested += OnRequested;
            playAt = -1; readySince = -1; hintQueued = false; promiseKept = false;
            if (fileIcon != null) { Destroy(fileIcon.gameObject); fileIcon = null; }
            Stop(false);
        }

        void OnDestroy()
        {
            if (bound != null) bound.OriginVideoRequested -= OnRequested;
            Stop(false);
        }

        // Let the reply 「好，我来给你解释」 be read first.
        void OnRequested() { if (!watching) playAt = Time.unscaledTime + ReplyToVideo; }

        void MakeFileIcon(PrologueDesk desk)
        {
            fileIcon = desk.Icon("Origin Video File", Lang.T("我从哪儿来.mp4"), null, DrawFilm);
            fileIcon.GetComponent<PrologueClick>().Open = () => { if (!watching) Play(); };
        }

        static void DrawFilm(RectTransform art)
        {
            PrologueDesk.Fill(PrologueDesk.Centered("Film", art, Vector2.zero, new Vector2(46, 50)), new Color32(40, 44, 52, 255), false);
            for (int i = 0; i < 4; i++)
            {
                PrologueDesk.Fill(PrologueDesk.Centered("Hole", art, new Vector2(-17, 18 - i * 12), new Vector2(6, 6)), new Color32(220, 220, 220, 255), false);
                PrologueDesk.Fill(PrologueDesk.Centered("Hole", art, new Vector2(17, 18 - i * 12), new Vector2(6, 6)), new Color32(220, 220, 220, 255), false);
            }
            PrologueDesk.Fill(PrologueDesk.Centered("Frame", art, Vector2.zero, new Vector2(24, 34)), new Color32(200, 230, 90, 255), false);
        }

        // ───────────── the native player, restyled as 暴风影音 ─────────────

        bool FindNative()
        {
            if (native == null) native = FindAnyObjectByType<VideoPlayerManager>(FindObjectsInactive.Include);
            if (native == null || native.videoPlayer == null) return false;
            if (nativeWindow == null) nativeWindow = native.GetComponentInParent<WindowManager>(true);
            if (!styled) Restyle();
            return true;
        }

        /// <summary>2016's player: the window says 暴风影音, the demo clips go, and this video sits in the library.</summary>
        void Restyle()
        {
            styled = true;
            LoadEnglish();
            foreach (var text in native.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "Aero Window Title" || text.text == "视频播放器" || text.text == "Video Player") titles.Add(text);
            Retitle();
            foreach (var item in native.videoItems)
                if (item != null && item.preset != null) item.preset.gameObject.SetActive(false);
            url = Path.Combine(Application.streamingAssetsPath, VideoPath);
            var cover = LingGuangV05.Desktop.Media.DesktopMedia.Picture("origin_video");
            native.CreateVideo(cover, Lang.T("我从哪儿来"), Lang.T("灵光 · 2:41"), url);
            native.videoPlayer.loopPointReached += OnNativeEnded;
            // The window's own video gets the English line and a 全屏 button.
            var screen = FindVideoImage();
            if (screen != null)
            {
                windowBand = Band(screen, out windowSubtitle, 18);
                var desk = PrologueDirector.Desk;
                if (desk != null)
                {
                    var b = desk.Button(screen, Lang.T("全屏"), Vector2.zero, new Vector2(110, 34), () => SetFullscreen(true), new Color32(40, 44, 52, 230));
                    fullscreenButton = (RectTransform)b.transform;
                    fullscreenButton.anchorMin = fullscreenButton.anchorMax = new Vector2(1, 1);
                    fullscreenButton.anchoredPosition = new Vector2(-70, -26);
                    b.GetComponentInChildren<TMP_Text>().color = Color.white;
                    fullscreenButton.gameObject.SetActive(false);
                }
            }
        }

        readonly List<TMP_Text> titles = new List<TMP_Text>();
        float nextRetitle;

        /// <summary>The window's names say 暴风影音; the native localisation may write them back, so this repeats.</summary>
        void Retitle()
        {
            string name = Lang.T("暴风影音");
            foreach (var t in titles) if (t != null && t.text != name) t.text = name;
        }

        RectTransform FindVideoImage()
        {
            foreach (var raw in native.GetComponentsInChildren<RawImage>(true))
                if (raw.transform.parent != null && raw.transform.parent.name == "Now Playing") return (RectTransform)raw.transform;
            return null;
        }

        RectTransform Band(RectTransform parent, out TMP_Text text, float size)
        {
            var band = PrologueDesk.Rect("Origin Subtitle Band", parent, new Vector2(0, .04f), new Vector2(1, .19f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(band, new Color(.04f, .05f, .05f, 1f), false);
            text = PrologueDirector.Desk.Text(PrologueDesk.Rect("Subtitle", band, Vector2.zero, Vector2.one, new Vector2(30, 0), new Vector2(-30, 0)), "", size, new Color(.93f, .94f, .9f), TextAlignmentOptions.Center);
            text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = size + 8;
            band.gameObject.SetActive(false);
            return band;
        }

        void Play()
        {
            var desk = PrologueDirector.Desk;
            if (desk == null) return;
            if (!FindNative())
            {
                desk.Popup(Lang.T("暴风影音"), Lang.T("播放器没找到。"), 5);
                Finished();
                return;
            }
            // On desktop builds StreamingAssets is a plain folder; a missing file still counts as watched, so the story goes on.
            if (!url.Contains("://") && !File.Exists(url))
            {
                Debug.LogWarning("OriginCurtain: missing " + url);
                desk.Popup(Lang.T("暴风影音"), Lang.T("找不到文件：我从哪儿来.mp4"), 5);
                Finished();
                return;
            }
            if (nativeWindow != null) { nativeWindow.OpenWindow(); nativeWindow.FocusToWindow(); }
            native.OpenVideo(url);
            watching = true;
            SetFullscreen(true);
        }

        /// <summary>Full screen is a black stage over the whole desktop showing the player's own picture.</summary>
        void SetFullscreen(bool on)
        {
            var desk = PrologueDirector.Desk;
            if (on && overlay == null && desk != null && native != null)
            {
                overlay = PrologueDesk.Rect("Origin Fullscreen", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                PrologueDesk.Fill(overlay, Color.black);
                overlay.SetAsLastSibling();
                var picture = PrologueDesk.Rect("Picture", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var raw = picture.gameObject.AddComponent<RawImage>();
                raw.texture = native.videoPlayer.targetTexture; raw.raycastTarget = false;
                var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
                overlayBand = Band(picture, out overlaySubtitle, 28);
                overlayClock = desk.Text(PrologueDesk.Rect("Hint", overlay, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-420, -40), new Vector2(-16, -8)), "", 15, new Color(1, 1, 1, .45f), TextAlignmentOptions.MidlineRight);
                // A click toggles pause, like a 2016 player.
                var button = overlay.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { if (native.videoPlayer.isPlaying) native.Pause(); else native.Play(); });
            }
            if (overlay != null) overlay.gameObject.SetActive(on);
            if (fullscreenButton != null) fullscreenButton.gameObject.SetActive(!on && watching);
        }

        void OnNativeEnded(VideoPlayer p) { if (watching && p.url == url) Stop(true); }

        void TickPlayer()
        {
            if (native == null || native.videoPlayer == null) { Stop(true); return; }
            var p = native.videoPlayer;
            // The player was closed, or something else is playing in it now.
            bool windowShut = nativeWindow != null && !nativeWindow.gameObject.activeInHierarchy;
            if (p.url != url || windowShut && (overlay == null || !overlay.gameObject.activeSelf)) { Stop(true); return; }
            var keyboard = Keyboard.current;
            if (keyboard != null && overlay != null && overlay.gameObject.activeSelf)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) SetFullscreen(false);
                else if (keyboard.spaceKey.wasPressedThisFrame) { if (p.isPlaying) native.Pause(); else native.Play(); }
            }
            if (Time.unscaledTime >= nextRetitle) { nextRetitle = Time.unscaledTime + .5f; Retitle(); }
            if (overlayClock != null) overlayClock.text = Clock(p.time) + " / " + Clock(p.length > 0 ? p.length : 161) + Lang.T("    Esc 退出全屏");
            string line = GameText.IsEnglish ? EnglishAt(p.time) : null;
            Show(overlayBand, overlaySubtitle, line);
            Show(windowBand, windowSubtitle, line);
        }

        static void Show(RectTransform band, TMP_Text text, string line)
        {
            if (band == null) return;
            band.gameObject.SetActive(line != null);
            if (line != null && text.text != line) text.text = line;
        }

        static string Clock(double seconds)
        {
            int s = Mathf.Max(0, (int)seconds);
            return (s / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        void LoadEnglish()
        {
            english.Clear();
            var asset = Resources.Load<TextAsset>("LingGuangV05/Video/evolution_en");
            if (asset == null) return;
            foreach (var block in asset.text.Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var lines = block.Split('\n');
                if (lines.Length < 3) continue;
                var times = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
                if (times.Length != 2 || !TryTime(times[0], out double a) || !TryTime(times[1], out double b)) continue;
                english.Add((a, b, string.Join("\n", lines, 2, lines.Length - 2)));
            }
        }

        static bool TryTime(string s, out double seconds)
        {
            seconds = 0;
            var parts = s.Trim().Replace(',', '.').Split(':');
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m)
                || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double sec)) return false;
            seconds = h * 3600 + m * 60 + sec;
            return true;
        }

        string EnglishAt(double t)
        {
            foreach (var cue in english) if (t >= cue.start && t < cue.end) return cue.text;
            return null;
        }

        /// <summary>Leaves the video. When it ended or the player was closed, it asks 「你觉得呢？」 back the first time.</summary>
        void Stop(bool finished)
        {
            if (!watching && !finished) return;
            watching = false;
            if (overlay != null) { Destroy(overlay.gameObject); overlay = null; overlayBand = null; }
            if (fullscreenButton != null) fullscreenButton.gameObject.SetActive(false);
            if (windowBand != null) windowBand.gameObject.SetActive(false);
            if (finished) Finished();
        }

        void Finished()
        {
            if (bound != null && bound.OriginVideoFinished())
            {
                // It asks back on the 对话 page; open it so the question is not missed.
                if (lab != null) { lab.Open(); lab.View?.Open("chat"); lab.View?.Refresh(true); }
            }
        }
    }
}
