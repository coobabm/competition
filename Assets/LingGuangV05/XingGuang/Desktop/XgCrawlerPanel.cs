using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 摆渡百科爬虫, inside the 数据 page of 灵光.exe: a browser view over a generated 摆渡百科 article
    /// (<see cref="XgCrawlArticle"/>) with a spider (<see cref="XgSpiderWalker"/>) walking down it. Every word a foot
    /// lands on is grabbed (<see cref="XgSim.CrawlGrab"/>): it glitches for a moment (highlight, box, colour, size jump,
    /// jitter, a ghost copy in another style), stays dimmed, and a token flies to the sample counter in the corner. The
    /// page scrolls with the spider; at the bottom the crawler follows a link to the next article. The run ends when the
    /// time is up or the cap is full; 收工 ends it early and keeps what was grabbed. 减少特效 keeps only the highlight and
    /// the colour change, without jitter, ghosts or knocks. The idea of a spider crawling a text page comes from
    /// @rybinfx's web crawler; this is an independent implementation.
    /// </summary>
    public sealed class XgCrawlerPanel
    {
        /// <summary>The page is read closer than the desktop: the resident's size times this.</summary>
        const float Pad = 28, PageScale = 1.5f;
        const int MaxGlitchStartsPerFrame = 4, MaxActiveGlitches = 24, TokenPool = 24;
        const float GlitchSeconds = .6f, ReducedGlitchSeconds = .35f, ChaseRadius = 150;

        sealed class Word
        {
            public int text, start, length;
            public string value;
            public UnityEngine.Rect rect;
            public bool taken;
            public float glitch = -1, touch = -1;
            /// <summary>Glitch effects, one bit each: 1 highlight, 2 box, 4 colour, 8 size jump, 16 jitter.</summary>
            public int kinds;
            public Color accent;
        }

        sealed class Token { public TMP_Text text; public Vector2 from, to, ctrl; public float t; }
        sealed class GhostCopy { public TMP_Text text; public Vector2 at, drift; public float t, life; public Color color; }

        readonly IXgPageHost host;
        readonly XgUi ui;
        readonly RectTransform parent;
        readonly Action closed;
        RectTransform root, viewport, content, tokenLayer, counterBox, summary;
        TMP_Text titleText, statusText, urlText, counterText, counterSub, hintText, summaryText;
        RectTransform counterFill;
        XgBtn closeButton, againButton, backButton;
        XgCrawlerLayer marks, spiderLayer;
        readonly List<TMP_Text> texts = new List<TMP_Text>();
        readonly List<TMP_MeshInfo[]> cached = new List<TMP_MeshInfo[]>();
        readonly List<bool> dirty = new List<bool>();
        readonly List<Word> words = new List<Word>();
        readonly List<Token> tokens = new List<Token>();
        readonly Stack<TMP_Text> tokenPool = new Stack<TMP_Text>();
        readonly List<GhostCopy> ghosts = new List<GhostCopy>();
        readonly Stack<TMP_Text> ghostPool = new Stack<TMP_Text>();
        XgSpiderWalker spider, sophon;
        XgCrawlArticle article;
        Vector2 waypoint;
        Vector2? pointer, clicked;
        float scroll, contentHeight, timeLeft, sophonAt = -1, sophonLeft, lastTick;
        int pageNo, grabbed, startsThisFrame, glitching;
        bool running, built, marksDirty;
        System.Random rng = new System.Random();

        public bool IsOpen => root != null && root.gameObject.activeSelf;
        /// <summary>The first-appearance scene: nothing is paid or credited; it simply reads.</summary>
        public bool Intro => intro;

        bool intro;
        float thinkTimer = 10, lookLeft;
        readonly XgSpiderFx fx = new XgSpiderFx();

        XgSim Sim => host.Sim;
        XgJuice Fx => host.Juice;
        bool Reduced => Sim != null && Sim.S.reduceFx;

        static readonly Color[] Neon = { XgDark.Good, XgDark.Data, XgDark.Params, XgDark.Hot, XgDark.Link, XgDark.Gold };

        public XgCrawlerPanel(IXgPageHost host, XgUi ui, RectTransform parent, Action closed)
        {
            this.host = host; this.ui = ui; this.parent = parent; this.closed = closed;
        }

        // ───────────── build ─────────────

        void Build()
        {
            built = true;
            root = Rect("Crawler", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(root, XgDark.Page);

            // Header: name, live status, 收工.
            var head = Strip("Header", root, 0, 30, 2, 2);
            titleText = ui.Text(Rect("Title", head, Vector2.zero, new Vector2(.45f, 1), Vector2.zero, Vector2.zero), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            statusText = ui.Text(Rect("Status", head, new Vector2(.45f, 0), Vector2.one, Vector2.zero, new Vector2(-120, 0)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            closeButton = ui.Button(head, "", Close, 13);
            closeButton.rt.anchorMin = closeButton.rt.anchorMax = new Vector2(1, .5f);
            closeButton.rt.offsetMin = new Vector2(-110, -13); closeButton.rt.offsetMax = new Vector2(0, 13);

            // The embedded browser: an address bar over the page.
            var frame = ui.Card(root, "Browser", Vector2.zero, Vector2.one, new Vector2(2, 26), new Vector2(-2, -36));
            var bar = Strip("Address", frame, 1, 26, 1, 1);
            Panel(bar, XgDark.Panel3).raycastTarget = false;
            var field = Rect("Field", bar, Vector2.zero, Vector2.one, new Vector2(64, 4), new Vector2(-8, -4));
            Panel(field, XgDark.Page).raycastTarget = false;
            ui.Text(Rect("Nav", bar, Vector2.zero, new Vector2(0, 1), new Vector2(8, 0), new Vector2(60, 0)), "◀ ▶", 12, XgDark.Dim, TextAlignmentOptions.MidlineLeft).textWrappingMode = TextWrappingModes.NoWrap;
            urlText = ui.Text(Rect("Url", field, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            urlText.textWrappingMode = TextWrappingModes.NoWrap; urlText.overflowMode = TextOverflowModes.Ellipsis;

            viewport = Rect("Viewport", frame, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -28));
            viewport.gameObject.AddComponent<RectMask2D>();
            Panel(viewport, XgDark.Paper);
            var input = viewport.gameObject.AddComponent<XgCrawlerInput>();
            input.Move = OnPointerMove; input.Exit = () => pointer = null; input.Click = OnPointerClick;

            content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0, 1);

            // Footer: how to steer it, and the credit for the idea.
            hintText = ui.Text(Rect("Hint", root, Vector2.zero, new Vector2(1, 0), new Vector2(4, 2), new Vector2(-4, 24)), "", 11, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            hintText.textWrappingMode = TextWrappingModes.NoWrap; hintText.overflowMode = TextOverflowModes.Ellipsis;

            // The sample counter in the page's lower-right corner, above the page.
            counterBox = ui.Card(viewport, "Counter", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-196, 12), new Vector2(-14, 82));
            counterText = ui.Text(Rect("Count", counterBox, Vector2.zero, Vector2.one, new Vector2(12, 26), new Vector2(-12, -4)), "", 24, XgDark.Data, TextAlignmentOptions.MidlineLeft);
            counterText.textWrappingMode = TextWrappingModes.NoWrap;
            counterSub = ui.Text(Rect("Sub", counterBox, Vector2.zero, new Vector2(1, 0), new Vector2(12, 10), new Vector2(-12, 28)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            counterSub.textWrappingMode = TextWrappingModes.NoWrap; counterSub.overflowMode = TextOverflowModes.Ellipsis;
            var track = Rect("Track", counterBox, Vector2.zero, new Vector2(1, 0), new Vector2(12, 6), new Vector2(-12, 9));
            counterFill = Bar(track, "Fill", XgDark.Track, XgDark.Data);

            tokenLayer = Rect("Tokens", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // End-of-run card.
            summary = ui.Card(root, "Summary", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-220, -90), new Vector2(220, 90));
            summaryText = ui.Text(Rect("Text", summary, Vector2.zero, Vector2.one, new Vector2(20, 56), new Vector2(-20, -14)), "", 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            againButton = ui.Button(summary, "", Again, 14);
            againButton.rt.anchorMin = againButton.rt.anchorMax = new Vector2(.5f, 0);
            againButton.rt.offsetMin = new Vector2(-200, 14); againButton.rt.offsetMax = new Vector2(-8, 46);
            backButton = ui.Button(summary, "", Close, 14);
            backButton.rt.anchorMin = backButton.rt.anchorMax = new Vector2(.5f, 0);
            backButton.rt.offsetMin = new Vector2(8, 14); backButton.rt.offsetMax = new Vector2(200, 46);
            summary.gameObject.SetActive(false);

            root.gameObject.SetActive(false);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        // ───────────── open, close ─────────────

        /// <summary>Shows the crawler for the run the sim just started (or the one still going).</summary>
        public void Open() => Open(false);

        /// <summary>
        /// Opens the page for a paid run (<paramref name="introScene"/> false) or for the spider's first appearance,
        /// where nothing is charged or credited and it reads until the player closes the page.
        /// </summary>
        public void Open(bool introScene)
        {
            if (!built) Build();
            if (introScene && !running) intro = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            summary.gameObject.SetActive(false);
            if (running) return;
            running = intro || Sim.CrawlActive;
            grabbed = intro ? 0 : Sim.CrawlTaken;
            fx.Clear();
            timeLeft = (float)XgSim.CrawlSeconds;
            pageNo = 0;
            // The sophon: very rarely another crawler's legs reach into the page for a few seconds.
            sophonAt = rng.NextDouble() < .04 ? (float)XgSim.CrawlSeconds * (.35f + .2f * (float)rng.NextDouble()) : -1;
            sophon = null;
            LoadPage();
            Refresh();
        }

        /// <summary>It notices the player: stops, turns toward the cursor with its eyes lit, then goes back to reading.</summary>
        public void LookAtPlayer(float seconds)
        {
            lookLeft = seconds;
            Fx.Play(XgJuice.Sfx.Id.Tick, .6f, .5f);
        }

        void Close()
        {
            if (Sim != null && Sim.CrawlActive && !intro) Sim.EndCrawl();
            running = false; intro = false; lookLeft = 0;
            ClearTransient();
            if (root != null) root.gameObject.SetActive(false);
            Fx.Play(XgJuice.Sfx.Id.Click);
            closed?.Invoke();
        }

        void Again()
        {
            if (!Sim.StartCrawl(host.Host)) { Fx.Play(XgJuice.Sfx.Id.Thud); Fx.Knock(againButton.rt, .05f, new Vector2(8, 0)); return; }
            Fx.Play(XgJuice.Sfx.Id.Coin);
            running = false;
            Open();
            host.Refresh(true);
        }

        void Finish()
        {
            if (!running || intro) return;
            running = false;
            int n = Sim.CrawlActive ? Sim.EndCrawl() : grabbed;
            var ds = XgCatalog.Dataset(XgSim.CrawlDataset);
            summaryText.text = "<b>" + T("爬虫收工", "Crawler done") + "</b>\n\n"
                + T("抓回 ", "Grabbed ") + "<color=#5DA8E8><b>" + n + "</b></color>" + T(" 个词，每个算一条语料，进了「" + ds.name + "」。", " words, one row each, into " + ds.nameEn + ".")
                + "\n<size=12><color=#6F95A5>" + T("约 " + N(XgSim.CrawlNoise * 100, "0") + "% 是广告和错字；研究树的「数据清洗」能减半。", "About " + N(XgSim.CrawlNoise * 100, "0") + "% is adverts and typos; 数据清洗 in the research tree halves that.") + "</color></size>";
            summary.gameObject.SetActive(true);
            summary.SetAsLastSibling();
            if (!Reduced) Fx.Knock(summary, .1f, Vector2.zero, .3f);
            Fx.Play(XgJuice.Sfx.Id.Ding);
            host.Refresh(true);
        }

        void ClearTransient()
        {
            foreach (var t in tokens) { t.text.gameObject.SetActive(false); tokenPool.Push(t.text); }
            tokens.Clear();
            foreach (var g in ghosts) { g.text.gameObject.SetActive(false); ghostPool.Push(g.text); }
            ghosts.Clear();
        }

        // ───────────── the page ─────────────

        void LoadPage()
        {
            ClearTransient();
            foreach (var t in texts) if (t != null) UnityEngine.Object.Destroy(t.gameObject);
            texts.Clear(); cached.Clear(); dirty.Clear(); words.Clear();
            if (marks != null) UnityEngine.Object.Destroy(marks.gameObject);
            if (spiderLayer != null) UnityEngine.Object.Destroy(spiderLayer.gameObject);

            bool en = En;
            article = XgCrawlArticle.Make(rng.Next(), Sim.Today, en, Headlines(Sim.Today));
            pageNo++;
            urlText.text = "http://" + article.Url;

            marks = Layer("Marks");
            Canvas.ForceUpdateCanvases();
            float width = Mathf.Max(200, viewport.rect.width - Pad * 2 - 4);
            float y = 18;
            var mast = MakeText(T("摆渡百科 · 网友共同编辑的百科全书", "Bodu Encyclopedia · the encyclopedia anyone can edit"), 12, XgDark.Dim, width, ref y, false);
            mast.fontStyle = FontStyles.Italic;
            y += 4;
            foreach (var b in article.blocks)
            {
                float size; Color color; float before = 0, after = 6;
                switch (b.kind)
                {
                    case XgCrawlArticle.Kind.Title: size = 28; color = XgDark.Ink; after = 10; break;
                    case XgCrawlArticle.Kind.Lead: size = 16; color = XgDark.Ink; after = 12; break;
                    case XgCrawlArticle.Kind.Heading: size = 18; color = XgDark.Good; before = 10; after = 6; break;
                    case XgCrawlArticle.Kind.News: size = 14; color = XgDark.Link; after = 4; break;
                    case XgCrawlArticle.Kind.Reference: size = 12; color = XgDark.Muted; after = 3; break;
                    default: size = 16; color = new Color(XgDark.Ink.r, XgDark.Ink.g, XgDark.Ink.b, .86f); after = 8; break;
                }
                y += before;
                string value = b.Text(en);
                var t = MakeText(value, size, color, width, ref y, true);
                if (b.kind == XgCrawlArticle.Kind.Title) t.fontStyle = FontStyles.Bold;
                y += after;
                if (b.kind == XgCrawlArticle.Kind.Heading) y += 2;
                int index = texts.Count - 1;
                var starts = b.Starts(en);
                for (int i = 0; i < b.words.Count; i++)
                    if (XgCrawlArticle.Grabbable(b.words[i])) words.Add(new Word { text = index, start = starts[i], length = b.words[i].Length, value = b.words[i] });
            }
            contentHeight = y + 140;
            content.sizeDelta = new Vector2(0, contentHeight);
            spiderLayer = Layer("Spider");
            spiderLayer.draw = DrawSpiders;
            marks.draw = DrawMarks;

            // Word rects in content space (first line of a word that wraps).
            foreach (var t in texts) t.ForceMeshUpdate(true);
            for (int i = 0; i < texts.Count; i++) { cached.Add(texts[i].textInfo.CopyMeshInfoVertexData()); dirty.Add(false); }
            for (int i = words.Count - 1; i >= 0; i--)
            {
                var w = words[i];
                if (!WordRect(w, out w.rect)) words.RemoveAt(i);
            }

            scroll = 0;
            content.anchoredPosition = Vector2.zero;
            var start = new Vector2(width * .5f + Pad, -40);
            // The resident spider itself, grown as far as the abilities have taken it.
            int abilities = Sim.AbilitiesCount;
            spider = new XgSpiderWalker(start, (float)XgResident.Size(abilities) * PageScale, XgResident.LegCount(abilities)) { FindFoothold = Foothold, Planted = OnPlanted, heading = -Mathf.PI / 2 };
            fx.Clear();
            sweepRight = rng.Next(2) == 0;
            waypoint = NextWaypoint(start);
            if (sophon != null) sophon = null;
            marksDirty = true;
        }

        XgCrawlerLayer Layer(string name)
        {
            var rt = Rect(name, content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Pivot top-left, like the content: local points are content points.
            rt.pivot = new Vector2(0, 1);
            var layer = rt.gameObject.AddComponent<XgCrawlerLayer>();
            layer.raycastTarget = false;
            layer.color = Color.white;
            return layer;
        }

        TMP_Text MakeText(string value, float size, Color color, float width, ref float y, bool article)
        {
            var rt = Rect("Text", content, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0, 1);
            var t = ui.Text(rt, value, size, color, TextAlignmentOptions.TopLeft);
            t.richText = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            float h = t.GetPreferredValues(value, width, 0).y;
            rt.sizeDelta = new Vector2(width, h);
            rt.anchoredPosition = new Vector2(Pad, -y);
            y += h;
            if (article) texts.Add(t);
            return t;
        }

        static List<string[]> Headlines(int today)
        {
            var list = new List<string[]>();
            try
            {
                var day = new DateTime(today / 10000, Mathf.Clamp(today / 100 % 100, 1, 12), Mathf.Clamp(today % 100, 1, 28));
                var news = EraContent.Events.Visible(EraEvents.News, day);
                // Visible is newest first; the generator wants date order.
                for (int i = news.Count - 1; i >= 0; i--) if (!news[i].brief && news[i].title.Length > 0) list.Add(new[] { news[i].title, news[i].titleEn });
            }
            catch (ArgumentException) { }
            return list;
        }

        bool WordRect(Word w, out UnityEngine.Rect rect)
        {
            rect = default;
            var t = texts[w.text];
            var info = t.textInfo;
            if (w.start >= info.characterCount) return false;
            var first = info.characterInfo[w.start];
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            for (int c = w.start; c < w.start + w.length && c < info.characterCount; c++)
            {
                var ci = info.characterInfo[c];
                if (!ci.isVisible || ci.lineNumber != first.lineNumber) continue;
                xMin = Mathf.Min(xMin, ci.bottomLeft.x); xMax = Mathf.Max(xMax, ci.topRight.x);
                var line = info.lineInfo[ci.lineNumber];
                yMin = Mathf.Min(yMin, line.descender); yMax = Mathf.Max(yMax, line.ascender);
            }
            if (xMin > xMax) return false;
            Vector2 a = content.InverseTransformPoint(t.transform.TransformPoint(new Vector3(xMin, yMin)));
            Vector2 b = content.InverseTransformPoint(t.transform.TransformPoint(new Vector3(xMax, yMax)));
            rect = UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        void OnTextChanged(UnityEngine.Object obj)
        {
            for (int i = 0; i < texts.Count && i < cached.Count; i++)
                if (texts[i] == obj) { cached[i] = texts[i].textInfo.CopyMeshInfoVertexData(); dirty[i] = true; }
        }

        // ───────────── walking ─────────────

        int Foothold(Vector2 near, float radius, out Vector2 at)
        {
            at = near;
            int best = -1; float bestScore = float.MaxValue;
            for (int i = 0; i < words.Count; i++)
            {
                var r = words[i].rect;
                float dx = Mathf.Max(0, Mathf.Max(r.xMin - near.x, near.x - r.xMax));
                float dy = Mathf.Max(0, Mathf.Max(r.yMin - near.y, near.y - r.yMax));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;
                float score = d + (words[i].taken ? 14 : 0);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0)
            {
                var r = words[best].rect;
                at = new Vector2(Mathf.Clamp(near.x, r.xMin + r.width * .25f, r.xMax - r.width * .25f), r.center.y);
            }
            return best;
        }

        bool sweepRight = true;

        Vector2 NextWaypoint(Vector2 from)
        {
            // Sweep like a reader: keep going the same way along the lines, dropping a little each hop, and turn back
            // near the page edge. Untaken words are preferred.
            float width = viewport.rect.width;
            if (sweepRight && from.x > width - Pad - 90) sweepRight = false;
            else if (!sweepRight && from.x < Pad + 90) sweepRight = true;
            float dir = sweepRight ? 1 : -1;
            int best = -1; float bestScore = float.MaxValue;
            for (int i = 0; i < words.Count; i++)
            {
                var c = words[i].rect.center;
                float drop = from.y - c.y, ahead = (c.x - from.x) * dir;
                if (drop < -4 || drop > 70 || ahead < 50 || ahead > 230) continue;
                float score = Mathf.Abs(drop - 22) * 1.5f + Mathf.Abs(ahead - 130) * .4f + (words[i].taken ? 70 : 0) + (float)rng.NextDouble() * 30;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) return words[best].rect.center;
            // Nothing ahead on these lines: drop down at the edge.
            return new Vector2(Mathf.Clamp(from.x + dir * 120, Pad + 30, width - Pad - 30), from.y - 50);
        }

        void OnPointerMove(PointerEventData e)
        {
            var cam = e.enterEventCamera != null ? e.enterEventCamera : e.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, cam, out var p)) pointer = p;
        }

        void OnPointerClick(PointerEventData e)
        {
            if (!running) return;
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(content, e.position, cam, out var p)) { clicked = p; waypoint = p; Fx.Play(XgJuice.Sfx.Id.Tick, 1.4f, .4f); }
        }

        void OnPlanted(XgSpiderWalker who, XgSpiderWalker.Leg leg)
        {
            if (leg.word < 0 || leg.word >= words.Count) return;
            var w = words[leg.word];
            if (who == sophon)
            {
                // The other crawler takes nothing; its touch just darkens the word for a moment.
                w.touch = 0; w.accent = new Color32(120, 20, 30, 255); marksDirty = true;
                return;
            }
            if (w.taken)
            {
                if (!Reduced) { w.touch = 0; w.accent = XgDark.Dim; marksDirty = true; }
                return;
            }
            if (!running || lookLeft > 0) return;
            if (!intro && Sim.CrawlGrab(1) <= 0) return;
            w.taken = true;
            grabbed = intro ? grabbed + 1 : Sim.CrawlTaken;
            fx.reduced = Reduced;
            fx.Read(spider, leg, w.rect, XgSpiderFx.Accent(rng), w.length);
            if (!intro) fx.Credit();
            dirty[w.text] = true; marksDirty = true;
            if (startsThisFrame < MaxGlitchStartsPerFrame && glitching < MaxActiveGlitches)
            {
                startsThisFrame++;
                w.glitch = 0;
                w.accent = Neon[rng.Next(Neon.Length)];
                if (Reduced) w.kinds = 1 | 4;
                else
                {
                    w.kinds = 1 << rng.Next(2);                 // highlight or box
                    w.kinds |= 4;                                // colour
                    if (rng.NextDouble() < .5) w.kinds |= 8;    // size jump
                    if (rng.NextDouble() < .45) w.kinds |= 16;  // jitter
                    if (rng.NextDouble() < .55) Ghost(w);       // ghost copy in another style
                }
            }
            Fly(w);
        }

        // ───────────── frame ─────────────

        public void Tick(float dt)
        {
            if (!IsOpen || article == null) return;
            dt = Mathf.Min(dt, .05f);
            startsThisFrame = 0;
            if (running && !intro && !Sim.CrawlActive) Finish();
            if (running && !intro)
            {
                timeLeft -= dt;
                if (timeLeft <= 0 || Sim.CrawlLeft <= 0) Finish();
            }

            float viewH = viewport.rect.height, width = viewport.rect.width;
            if (spider != null)
            {
                // Chase the pointer when it comes close, otherwise follow the route (or the clicked spot).
                Vector2 target = waypoint;
                float speedScale = running ? 1 : .35f;
                // Thinking: now and then it stops and its threads light up.
                thinkTimer -= dt;
                if (running && thinkTimer <= 0 && fx.Threads >= 4 && lookLeft <= 0) { thinkTimer = 9 + 6 * (float)rng.NextDouble(); fx.reduced = Reduced; fx.Think(); }
                spider.Settled = fx.Thinking || lookLeft > 0;
                if (spider.Settled) { target = spider.pos; speedScale = 0; }
                else if (pointer.HasValue && Vector2.Distance(pointer.Value, spider.pos) < ChaseRadius) { target = pointer.Value; speedScale = 1.25f; }
                else if (Vector2.Distance(spider.pos, waypoint) < 16)
                {
                    clicked = null;
                    waypoint = NextWaypoint(spider.pos);
                }
                spider.Tick(dt, target, speedScale, Reduced);
                if (lookLeft > 0)
                {
                    // It noticed the player: turn toward the cursor (or straight out of the screen) and hold still.
                    lookLeft -= dt;
                    var at = pointer.HasValue ? pointer.Value : spider.pos + Vector2.down * 100;
                    float face = Mathf.Atan2(at.y - spider.pos.y, at.x - spider.pos.x);
                    spider.heading = Mathf.LerpAngle(spider.heading * Mathf.Rad2Deg, face * Mathf.Rad2Deg, 1 - Mathf.Exp(-5 * dt)) * Mathf.Deg2Rad;
                }
                spider.eyeBoost = Mathf.MoveTowards(spider.eyeBoost, lookLeft > 0 ? 1 : 0, dt * 3);
                spider.pos.x = Mathf.Clamp(spider.pos.x, 10, width - 10);
                // Reached the bottom: follow a link to the next article.
                if (running && -spider.pos.y > contentHeight - 150) { LoadPage(); return; }
            }
            TickSophon(dt, width, viewH);

            // Camera: keep the spider about 40% down the view.
            float want = Mathf.Clamp(-spider.pos.y - viewH * .4f, 0, Mathf.Max(0, contentHeight - viewH));
            scroll = Mathf.Lerp(scroll, want, 1 - Mathf.Exp(-2.5f * dt));
            content.anchoredPosition = new Vector2(0, scroll);

            fx.reduced = Reduced;
            fx.Tick(dt, spider);
            TickWords(dt);
            TickTokens(dt);
            TickGhosts(dt);
            spiderLayer.Redraw();
            if (marksDirty || glitching > 0) { marks.Redraw(); marksDirty = false; }
            Refresh();
        }

        void TickSophon(float dt, float width, float viewH)
        {
            if (sophonAt < 0 || !running) { if (sophon != null && !running) sophon = null; return; }
            float elapsed = (float)XgSim.CrawlSeconds - timeLeft;
            if (sophon == null && elapsed >= sophonAt)
            {
                sophon = new XgSpiderWalker(new Vector2(width + 40, -scroll - viewH * .2f), 2.2f) { hideBody = true, FindFoothold = Foothold, Planted = OnPlanted, heading = -Mathf.PI / 2, maxSpeed = 30 };
                sophonLeft = 7;
                sophonAt = -1;
            }
            if (sophon == null) return;
            sophonLeft -= dt;
            // Its body stays just past the right edge; only the legs reach in.
            var target = new Vector2(width + 40, -scroll - viewH * (sophonLeft > 1.5f ? .75f : .1f));
            sophon.Tick(dt, target, 1, Reduced);
            sophon.pos.x = width + 40;
            if (sophonLeft <= 0) sophon = null;
        }

        void TickWords(float dt)
        {
            glitching = 0;
            float life = Reduced ? ReducedGlitchSeconds : GlitchSeconds;
            foreach (var w in words)
            {
                if (w.glitch >= 0)
                {
                    w.glitch += dt;
                    dirty[w.text] = true;
                    if (w.glitch >= life) { w.glitch = -1; marksDirty = true; }
                    else glitching++;
                }
                if (w.touch >= 0) { w.touch += dt; if (w.touch > .3f) { w.touch = -1; } marksDirty = true; }
            }
            for (int i = 0; i < texts.Count; i++) if (dirty[i]) { Rewrite(i, life); dirty[i] = false; }
        }

        /// <summary>Restores a text's vertices and applies the taken and glitching words of that text.</summary>
        void Rewrite(int index, float life)
        {
            var t = texts[index];
            var info = t.textInfo;
            var source = cached[index];
            if (source == null || info.meshInfo == null) return;
            for (int m = 0; m < info.meshInfo.Length && m < source.Length; m++)
            {
                var dst = info.meshInfo[m]; var src = source[m];
                if (dst.vertices == null || src.vertices == null) continue;
                Array.Copy(src.vertices, dst.vertices, Mathf.Min(src.vertices.Length, dst.vertices.Length));
                if (src.colors32 != null && dst.colors32 != null) Array.Copy(src.colors32, dst.colors32, Mathf.Min(src.colors32.Length, dst.colors32.Length));
            }
            Color32 dim = new Color(XgDark.Muted.r, XgDark.Muted.g, XgDark.Muted.b, .42f);
            foreach (var w in words)
            {
                if (w.text != index || !w.taken) continue;
                float k = w.glitch >= 0 ? Mathf.Clamp01(w.glitch / life) : 1;
                float pulse = w.glitch >= 0 ? Mathf.Sin(k * Mathf.PI) : 0;
                Color32 col = w.glitch >= 0 && (w.kinds & 4) != 0 ? (Color32)Color.Lerp(w.accent, (Color)dim, k * k) : dim;
                float scale = (w.kinds & 8) != 0 && w.glitch >= 0 ? 1 + .45f * pulse : 1;
                var center = Vector3.zero; int n = 0;
                for (int c = w.start; c < w.start + w.length && c < info.characterCount; c++)
                {
                    var ci = info.characterInfo[c];
                    if (!ci.isVisible) continue;
                    var v = info.meshInfo[ci.materialReferenceIndex].vertices;
                    center += (v[ci.vertexIndex] + v[ci.vertexIndex + 2]) * .5f; n++;
                }
                if (n > 0) center /= n;
                for (int c = w.start; c < w.start + w.length && c < info.characterCount; c++)
                {
                    var ci = info.characterInfo[c];
                    if (!ci.isVisible) continue;
                    var mesh = info.meshInfo[ci.materialReferenceIndex];
                    Vector3 jitter = Vector3.zero;
                    if ((w.kinds & 16) != 0 && w.glitch >= 0) jitter = new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * 5 * pulse;
                    for (int q = 0; q < 4; q++)
                    {
                        int vi = ci.vertexIndex + q;
                        if (scale != 1) mesh.vertices[vi] = center + (mesh.vertices[vi] - center) * scale;
                        mesh.vertices[vi] += jitter;
                        mesh.colors32[vi] = col;
                    }
                }
            }
            t.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        // ───────────── tokens, ghosts ─────────────

        Vector2 ToLayer(RectTransform layer, Vector2 contentPoint) => layer.InverseTransformPoint(content.TransformPoint(contentPoint));

        void Fly(Word w)
        {
            if (tokens.Count >= TokenPool) { Arrive(); return; }
            var t = tokenPool.Count > 0 ? tokenPool.Pop() : NewTokenText();
            t.gameObject.SetActive(true);
            t.text = w.value;
            t.color = w.accent.a > 0 ? w.accent : XgDark.Data;
            var from = ToLayer(tokenLayer, w.rect.center);
            var to = (Vector2)tokenLayer.InverseTransformPoint(counterBox.TransformPoint(counterBox.rect.center));
            var mid = (from + to) * .5f + new Vector2(0, 60 + 40 * (float)rng.NextDouble());
            tokens.Add(new Token { text = t, from = from, to = to, ctrl = mid, t = 0 });
            t.rectTransform.anchoredPosition = from;
        }

        TMP_Text NewTokenText()
        {
            var rt = Rect("Token", tokenLayer, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-60, -10), new Vector2(60, 10));
            var t = ui.Text(rt, "", 14, XgDark.Data, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.richText = false; t.fontStyle = FontStyles.Bold;
            return t;
        }

        void TickTokens(float dt)
        {
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                var k = tokens[i];
                k.t += dt / .65f;
                if (k.t >= 1)
                {
                    k.text.gameObject.SetActive(false); tokenPool.Push(k.text); tokens.RemoveAt(i);
                    Arrive();
                    continue;
                }
                float u = k.t * k.t;
                var p = (1 - u) * (1 - u) * k.from + 2 * (1 - u) * u * k.ctrl + u * u * k.to;
                k.text.rectTransform.anchoredPosition = p;
                k.text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, .55f, k.t);
                var c = k.text.color; c.a = Mathf.Lerp(1, .5f, k.t); k.text.color = c;
            }
        }

        void Arrive()
        {
            if (!Reduced) Fx.Knock(counterBox, .04f, Vector2.zero, .12f);
            if (Time.unscaledTime - lastTick > .07f) { lastTick = Time.unscaledTime; Fx.Play(XgJuice.Sfx.Id.Tick, .9f + .4f * (float)rng.NextDouble(), .25f); }
        }

        void Ghost(Word w)
        {
            var t = ghostPool.Count > 0 ? ghostPool.Pop() : NewGhostText();
            t.gameObject.SetActive(true);
            bool latin = true;
            foreach (char ch in w.value) if (ch > 0x2000) latin = false;
            // Another style for the copy: bold italic, monospace, much larger, struck through or (Latin) another font.
            int style = rng.Next(latin ? 5 : 4);
            float size = texts[w.text].fontSize;
            t.font = ui.font; t.fontStyle = FontStyles.Normal;
            switch (style)
            {
                case 0: t.text = "<b><i>" + w.value + "</i></b>"; break;
                case 1: t.text = "<mspace=0.9em>" + w.value + "</mspace>"; break;
                case 2: t.text = w.value; size *= 1.7f; break;
                case 3: t.text = "<s>" + w.value + "</s>"; t.fontStyle = FontStyles.UpperCase; break;
                default: t.text = w.value; if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset; t.fontStyle = FontStyles.Bold | FontStyles.UpperCase; break;
            }
            t.fontSize = size;
            var at = w.rect.center;
            t.rectTransform.anchoredPosition = at;
            t.rectTransform.SetAsLastSibling();
            spiderLayer.rectTransform.SetAsLastSibling();
            var accent = Neon[rng.Next(Neon.Length)];
            ghosts.Add(new GhostCopy { text = t, at = at, drift = new Vector2(((float)rng.NextDouble() - .5f) * 30, 14 + 12 * (float)rng.NextDouble()), t = 0, life = .5f, color = accent });
        }

        TMP_Text NewGhostText()
        {
            var rt = Rect("Ghost", content, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-80, -14), new Vector2(80, 14));
            var t = ui.Text(rt, "", 15, XgDark.Good, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.richText = true;
            return t;
        }

        void TickGhosts(float dt)
        {
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i];
                g.t += dt;
                if (g.t >= g.life) { g.text.gameObject.SetActive(false); ghostPool.Push(g.text); ghosts.RemoveAt(i); continue; }
                float k = g.t / g.life;
                g.text.rectTransform.anchoredPosition = g.at + g.drift * k + (k < .2f ? new Vector2(((float)rng.NextDouble() - .5f) * 6, 0) : Vector2.zero);
                var c = g.color; c.a = (1 - k) * .85f; g.text.color = c;
            }
        }

        // ───────────── drawing ─────────────

        void DrawMarks(VertexHelper vh)
        {
            float life = Reduced ? ReducedGlitchSeconds : GlitchSeconds;
            var underline = XgDark.Hairline;
            foreach (var w in words)
            {
                var r = w.rect;
                if (w.taken) XgDraw.Box(vh, new Vector2(r.xMin, r.yMin - 1), new Vector2(r.xMax, r.yMin), underline);
                if (w.glitch >= 0)
                {
                    float k = Mathf.Clamp01(w.glitch / life);
                    var c = w.accent; c.a = (1 - k) * .38f;
                    var pad = new Vector2(3, 2) * (1 + .4f * Mathf.Sin(k * Mathf.PI));
                    if ((w.kinds & 1) != 0) XgDraw.Box(vh, r.min - pad, r.max + pad, c);
                    if ((w.kinds & 2) != 0)
                    {
                        c.a = (1 - k) * .9f;
                        Outline(vh, r.min - pad, r.max + pad, 1.2f, c);
                    }
                }
                if (w.touch >= 0)
                {
                    var c = w.accent; c.a = (1 - w.touch / .3f) * .5f;
                    Outline(vh, r.min - new Vector2(2, 1), r.max + new Vector2(2, 1), 1, c);
                }
            }
        }

        static void Outline(VertexHelper vh, Vector2 min, Vector2 max, float w, Color c)
        {
            XgDraw.Box(vh, min, new Vector2(max.x, min.y + w), c);
            XgDraw.Box(vh, new Vector2(min.x, max.y - w), max, c);
            XgDraw.Box(vh, min, new Vector2(min.x + w, max.y), c);
            XgDraw.Box(vh, new Vector2(max.x - w, min.y), max, c);
        }

        void DrawSpiders(VertexHelper vh)
        {
            if (spider != null)
            {
                fx.DrawBehind(vh, spider);
                spider.Draw(vh, new Color(18 / 255f, 34 / 255f, 44 / 255f, .95f), new Color32(20, 48, 52, 255), XgDark.Good, 0, new Color(214 / 255f, 238 / 255f, 245 / 255f, .75f));
                fx.DrawFront(vh, spider);
            }
            if (sophon != null)
            {
                float a = Mathf.Clamp01(Mathf.Min(sophonLeft, 7 - sophonLeft) / .8f);
                sophon.Draw(vh, new Color(.02f, .03f, .05f, .95f * a), Color.clear, new Color(.5f, .05f, .08f, a), 0);
            }
        }

        // ───────────── labels ─────────────

        void Refresh()
        {
            if (Sim == null) return;
            var ds = XgCatalog.Dataset(XgSim.CrawlDataset);
            titleText.text = T("摆渡百科爬虫", "Bodu Encyclopedia crawler") + "  <size=12><color=#3A5566>bodu-spider 0.3 · " + T("第 " + pageNo + " 页", "page " + pageNo) + "</color></size>";
            if (intro)
                statusText.text = "<color=#E07B4F>" + T("没有人启动过它", "Nobody started it") + "</color>  ·  " + T("已读 ", "read ") + grabbed + T(" 个词", " words");
            else if (running)
                statusText.text = (sophon != null ? "<color=#E24B4A>" + T("检测到另一只爬虫……", "Another crawler detected…") + "</color>   " : "")
                    + T("剩 ", "") + N(Mathf.Max(0, timeLeft), "0") + T(" 秒", " s left") + "  ·  " + grabbed + " / " + XgSim.CrawlCap;
            else statusText.text = T("已收工", "Finished") + "  ·  " + grabbed + " / " + XgSim.CrawlCap;
            closeButton.Set(intro ? T("关掉", "Close") : running ? T("收工", "Stop") : T("返回数据页", "Back to Data"), true, null, running && !intro ? XgDark.Hot : XgDark.Ink);
            counterText.text = intro ? grabbed + "<size=13><color=#6F95A5> " + T("词", "words") + "</color></size>"
                : "+" + grabbed + "<size=13><color=#6F95A5> / " + XgSim.CrawlCap + "</color></size>";
            counterSub.text = intro ? T("→ 它自己", "→ itself") : T("→ " + ds.name + " · 每词一条", "→ " + ds.nameEn + " · a row a word");
            SetBar(counterFill, intro ? Mathf.Repeat(grabbed / 60f, 1) : grabbed / (float)XgSim.CrawlCap);
            hintText.text = T("鼠标靠近它会追过来；点一下给它指路。收工也留下已经抓到的。", "It chases the pointer when close; click to send it somewhere. Stopping keeps what it has grabbed.")
                + "   <color=#3A5566>" + T("灵感来自 @rybinfx 的网页蜘蛛", "Inspired by @rybinfx's web crawler") + "</color>";
            if (summary.gameObject.activeSelf)
            {
                var o = Sim.Offer(XgSim.CrawlOfferId);
                bool can = o != null && o.available && host.Host.Money + 1e-9 >= XgSim.CrawlPrice;
                againButton.Set(T("再爬一次 ¥", "Crawl again ¥") + Money(XgSim.CrawlPrice), can, null, XgDark.Money);
                backButton.Set(T("返回数据页", "Back to Data"), true);
            }
        }
    }
}
