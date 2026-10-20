using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Desktop software appears stage by stage (名词简化与训练页减负策划 §1.6). Until its stage an app has no desktop icon
    /// and its window cannot stay open. The moment a stage unlocks it, the icon pops in on the first free grid cell
    /// (0 → 1.15 → 1.0 with a gold ring, 动效策划 D3) and a tray balloon says what it is for. Loading a save shows the
    /// icons it has reached without replaying anything. Apps still initialize normally, so unlocking never loses state.
    /// </summary>
    public sealed class StudyDesktopGates : MonoBehaviour
    {
        readonly Dictionary<WindowManager, string> windows = new Dictionary<WindowManager, string>();
        /// <summary>Last seen open/closed state per feature; reset when another save is bound.</summary>
        readonly Dictionary<string, bool> seen = new Dictionary<string, bool>();
        readonly Dictionary<string, bool> scanned = new Dictionary<string, bool>();
        readonly HashSet<string> announced = new HashSet<string>();
        XingGuangController lab;
        LingGuangV05.XingGuang.XgSim bound;
        float scanAt;
        Transform cachedDesktopList;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static void Attach(XingGuangController owner)
        {
            var gates = owner.GetComponent<StudyDesktopGates>() ?? owner.gameObject.AddComponent<StudyDesktopGates>();
            gates.lab = owner;
        }

        public static string Feature(string app)
        {
            switch (app)
            {
                case "taohuo": case "xunbao": return "shop";
                case "home": return "power";
                case "zhongbao": return "crowd";
                case "miaoyu": return "data";
                default: return null;
            }
        }

        /// <summary>Native windows that host a gated app (the router has no binding for some of them).</summary>
        static string WindowFeature(string window)
        {
            switch (window)
            {
                case "Calculator": return "crowd";   // 摆渡众包
                case "Commander": return "data";     // 喵鱼
                case "Game Hub": return "games";     // 游戏中心
                default: return null;
            }
        }

        /// <summary>The balloon when a feature's app first appears: sender and one line. Null = no balloon.</summary>
        static string[] Balloon(string feature)
        {
            switch (feature)
            {
                case "shop": return new[] { T(LingGuangV05.Core.AppNames.ShopZh, LingGuangV05.Core.AppNames.ShopEn), T("显卡、数据包，今天下单明天到。", "Graphics cards and data packs. Order today, here tomorrow.") };
                case "data": return new[] { T(LingGuangV05.Core.AppNames.UsedZh, LingGuangV05.Core.AppNames.UsedEn), T("旧卡在这儿卖，也能淘到便宜货。", "Sell old cards here, or find a bargain.") };
                case "games": return new[] { T("游戏中心", "Game Hub"), T("累了就下盘棋，它也会下。", "Tired? Play a game of chess. It plays too.") };
                case "crowd": return new[] { T("摆渡众包", "Bodu Crowd"), T("先看验收要求，再签约接单。", "Read acceptance requirements before signing a contract.") };
                default: return null;
            }
        }

        public static bool Allows(string app)
        {
            string feature = Feature(app);
            if (feature == null) return true;
            var owner = XingGuangController.Instance;
            if(owner==null||owner.Sim==null||owner.runtime==null)return true;
            string id=LingGuangV05.XingGuang.XgSim.DesktopAppId(app);
            if(!string.IsNullOrEmpty(id)&&!owner.Sim.DesktopAppInstalled(id))return false;
            return owner.runtime.Sim.InPrologue||!owner.runtime.Sim.AppInstalled||owner.Sim.SystemUnlocked(feature);
        }

        public static bool AllowsWindow(WindowManager window)
        {
            if(window==null)return false;
            string route=window.name=="Calculator"?"zhongbao":window.name=="Commander"?"miaoyu":"";
            var owner=XingGuangController.Instance;
            if(route.Length==0&&owner?.runtime!=null)
                foreach(var binding in DesktopWindowIndex.GetBindings(owner.runtime))if(binding.Window==window){route=binding.ApplicationId;break;}
            return route.Length==0||Allows(route);
        }
        bool FeatureOpen(string feature)
        {
            string id=feature=="shop"?"taohuo":feature=="data"?"miaoyu":feature=="crowd"?"zhongbao":"";
            return lab.Sim.SystemUnlocked(feature)&&(id.Length==0||lab.Sim.DesktopAppInstalled(id));
        }
        void LateUpdate()
        {
            if(XgStageCurtain.Busy&&XgStageCurtain.Step<3)return;
            if (lab == null || lab.Sim == null || lab.runtime == null || lab.runtime.Sim.InPrologue || !lab.runtime.Sim.AppInstalled) return;
            if (!ReferenceEquals(bound, lab.Sim)) { bound = lab.Sim; seen.Clear(); announced.Clear(); scanAt = 0; }
            if (Time.unscaledTime >= scanAt)
            {
                scanAt = Time.unscaledTime + 1;
                foreach (var binding in DesktopWindowIndex.GetBindings(lab.runtime))
                {
                    string feature = Feature(binding.ApplicationId);
                    if (feature == null) continue;
                    if (binding.DesktopShortcut != null) Gate(feature, binding.DesktopShortcut.transform as RectTransform);
                    if (binding.Window != null) windows[binding.Window] = feature;
                    if(binding.TaskbarShortcut!=null)binding.TaskbarShortcut.gameObject.SetActive(FeatureOpen(feature));
                }
                if(cachedDesktopList==null){var list=GameObject.Find("Desktop List");cachedDesktopList=list!=null?list.transform:null;}
                foreach (var window in DesktopWindowIndex.Get(lab.runtime))
                {
                    string feature = WindowFeature(window.name);
                    if (feature == null) continue;
                    windows[window] = feature;
                    if(window.taskbarButton!=null)window.taskbarButton.gameObject.SetActive(FeatureOpen(feature));
                    var icon = cachedDesktopList != null ? cachedDesktopList.Find(window.name) as RectTransform : null;
                    if (icon != null) Gate(feature, icon);
                }
                // Applied after the whole scan, so every icon of one feature (some apps have several) sees the same change.
                foreach (var pair in scanned) seen[pair.Key] = pair.Value;
                scanned.Clear();
            }
            foreach (var pair in windows)
            {
                if (pair.Key == null || FeatureOpen(pair.Value) || !pair.Key.isOn) continue;
                pair.Key.CloseWindow();
                lab.View?.ShowToast(T("从群友的链接下载并安装后开放。", "Download and install it from the link in the group."), 3);
            }
        }

        /// <summary>Shows or hides one icon. A feature that turns open while this save is bound pops in and is announced once.</summary>
        void Gate(string feature, RectTransform icon)
        {
            if (icon == null) return;
            bool open = FeatureOpen(feature);
            scanned[feature] = open;
            bool fresh = open && seen.TryGetValue(feature, out bool was) && !was;
            if (fresh && !icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
                DesktopIconOrder.ArrangeNow(icon); // it takes its place in the desktop's order, no gaps
                StartCoroutine(PopIn(icon));
            }
            else if (icon.gameObject.activeSelf != open) icon.gameObject.SetActive(open);
            if (fresh && announced.Add(feature)) Announce(feature);
        }

        static void Announce(string feature)
        {
            var line = Balloon(feature);
            var desk = PrologueDirector.Desk;
            if (line == null || desk == null) return;
            desk.StoryPopup(line[0], line[1], 5);
        }

        /// <summary>0 → 1.15 → 1.0 over about half a second, and a gold ring that widens and fades behind it.</summary>
        IEnumerator PopIn(RectTransform icon)
        {
            var ring = new GameObject("Unlock Ring", typeof(RectTransform)).GetComponent<RectTransform>();
            ring.SetParent(icon, false);
            ring.SetAsFirstSibling();
            ring.anchorMin = ring.anchorMax = new Vector2(.5f, .5f);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var g = ring.gameObject.AddComponent<XgRingGraphic>();
            g.Width = 4; g.raycastTarget = false;
            var gold = new Color(1f, .77f, .42f, 1f);
            float size = Mathf.Max(48, Mathf.Min(icon.rect.width, icon.rect.height));
            const float Pop = .5f, Ring = .9f;
            for (float t = 0; t < Ring; t += Time.unscaledDeltaTime)
            {
                if (icon == null) yield break;
                float p = Mathf.Clamp01(t / Pop);
                // Overshoot to 1.15 at 60%, settle to 1.0.
                float s = p < .6f ? Mathf.Lerp(0, 1.15f, Ease(p / .6f)) : Mathf.Lerp(1.15f, 1f, Ease((p - .6f) / .4f));
                icon.localScale = new Vector3(s, s, 1);
                float r = t / Ring;
                ring.sizeDelta = Vector2.one * size * Mathf.Lerp(.7f, 1.6f, Ease(r));
                g.color = new Color(gold.r, gold.g, gold.b, 1 - r);
                yield return null;
            }
            if (icon != null) icon.localScale = Vector3.one;
            if (ring != null) Destroy(ring.gameObject);
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return 1 - (1 - x) * (1 - x); }
    }
}
