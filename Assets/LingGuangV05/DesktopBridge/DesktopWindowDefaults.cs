using System.Collections.Generic;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>First-open sizes in desktop coordinates. Later moves, resizes and maximize/restore belong to the player.</summary>
    public sealed class DesktopWindowDefaults : MonoBehaviour
    {
        readonly HashSet<WindowManager> registered = new HashSet<WindowManager>();
        readonly HashSet<WindowManager> opened = new HashSet<WindowManager>();
        readonly HashSet<WindowManager> placed = new HashSet<WindowManager>();
        readonly Dictionary<WindowManager, Vector2> areas = new Dictionary<WindowManager, Vector2>();
        float next;
        ChapterOneRuntime runtimeOwner;
        void Awake(){runtimeOwner=GetComponent<ChapterOneRuntime>();}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            Attach(runtime);
        }

        internal static void Attach(ChapterOneRuntime runtime)
        {
            if (runtime != null && runtime.GetComponent<DesktopWindowDefaults>() == null)
                runtime.gameObject.AddComponent<DesktopWindowDefaults>();
        }

        public static Vector2 PreferredSize(string name)
        {
            if (name.Contains("YY 2010")) return new Vector2(340, 700);
            switch (name)
            {
                case "Messaging": return new Vector2(1020, 740);
                case "Web Browser": return new Vector2(1440, 850);
                case "灵光.exe": return new Vector2(1440, 850);
                case "Mail": return new Vector2(1280, 800);
                case "Settings": return new Vector2(1080, 760);
                case "Notepad": return new Vector2(900, 680);
                case "AI Files": return new Vector2(1000, 720);
                case "Commander": return new Vector2(1280, 800); // second-hand shop
                case "Calculator": return new Vector2(1380, 840); // crowd workbench
                case "Reminder": return new Vector2(1280, 800); // forum
                case "Music Player": return new Vector2(1100, 740);
                default: return new Vector2(1280, 800);
            }
        }

        public static Vector2 SizeFor(string name, Vector2 area)
        {
            float scale = Mathf.Min(area.x / 1920f, area.y / 1030f);
            var desired = PreferredSize(name) * scale;
            return new Vector2(Mathf.Min(desired.x, area.x - 40 * scale), Mathf.Min(desired.y, area.y - 40 * scale));
        }

        static Vector2 OffsetFor(string name)
        {
            if (name.Contains("YY 2010")) return new Vector2(690, 36);
            switch (name)
            {
                case "Messaging": return new Vector2(-180, -24);
                case "Web Browser": return new Vector2(72, 28);
                case "灵光.exe": return new Vector2(112, 28);
                case "Calculator": return new Vector2(-48, -12);
                case "Settings": return new Vector2(130, -36);
                case "Notepad": return new Vector2(-160, 64);
                default: return new Vector2(-48, 12);
            }
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f;
            foreach (var window in DesktopWindowIndex.Get(runtimeOwner))
            {
                if (window.windowContainer == null) continue;
                if (registered.Add(window)) window.onOpen.AddListener(() => Prepare(window, true));
                Prepare(window);
            }
        }

        void Prepare(WindowManager window, bool opening = false)
        {
            if (window == null || window.windowContainer == null) return;
            var frame = window.windowContainer;
            var parent = frame.parent as RectTransform;
            if (parent == null || parent.rect.width < 100 || parent.rect.height < 100) return;
            var area = parent.rect.size;
            if (!opened.Contains(window) && areas.TryGetValue(window, out var oldArea) && oldArea != area) placed.Remove(window);
            if (!placed.Contains(window) && !window.isFullscreen)
            {
                var size = SizeFor(window.name, area);
                var margin = (area - size) * .5f;
                var offset = OffsetFor(window.name) * Mathf.Min(area.x / 1920f, area.y / 1030f);
                offset.x = Mathf.Clamp(offset.x, -margin.x + 16, margin.x - 16);
                offset.y = Mathf.Clamp(offset.y, -margin.y + 16, margin.y - 16);
                frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
                frame.offsetMin = margin + offset; frame.offsetMax = -margin + offset;
                placed.Add(window);
            }
            if (opening || !areas.TryGetValue(window, out var previous) || previous != area)
            {
                // Native DreamOS assumes 1920x1080; match resize bounds to this desktop's actual work area.
                foreach (var grip in frame.GetComponentsInChildren<WindowResizeAnchor>(true))
                {
                    grip.minSize = SizeFor(window.name, area) * .78f - area;
                    grip.maxSize = Vector2.zero;
                }
                areas[window] = area;
                if (placed.Contains(window) && !window.isFullscreen) window.windowDragger?.ClampToArea();
            }
            if (opening) opened.Add(window);
        }
    }
}
