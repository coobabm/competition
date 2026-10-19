using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Games
{
    /// <summary>One game inside the 游戏中心: it builds its own board into the area it is given and keeps its state while hidden.</summary>
    public abstract partial class HubGame : MonoBehaviour
    {
        protected TMP_FontAsset font;
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Blurb { get; }
        /// <summary>Big glyph on the home card and in the sidebar.</summary>
        public abstract string Glyph { get; }
        public abstract Color Accent { get; }
        /// <summary>Why the game cannot be played yet (null = playable).</summary>
        public virtual string Locked => null;
        public abstract void Build(RectTransform area);
        /// <summary>Called when the game is shown again (texts may have changed language).</summary>
        public virtual void Refresh() { }
        public void Init(TMP_FontAsset fontAsset) { font = fontAsset; }

        protected static string T(string zh, string en) => GameText.T(zh, en);

        protected TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            return t;
        }

        protected Button Button(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color fill, Action click, out TMP_Text label)
        {
            var rt = PrologueDesk.Rect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var img = PrologueDesk.Fill(rt, fill);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => click());
            label = Text(PrologueDesk.Rect("T", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 18, Color.white, TextAlignmentOptions.Center);
            return b;
        }

        /// <summary>The lab's stage (1 before 灵光 exists).</summary>
        protected static int Stage => TiebaHub.Lab()?.S.stage ?? 1;

        // ───────────── the lab's side of a game (design v1.1 §3) ─────────────

        protected static readonly System.Random Rng = new System.Random();
        /// <summary>The last "+N 样本 · dataset" line and the last stage-6 commentary line, shown under the status.</summary>
        protected string sampleNote = "", commentary = "";
        string commentaryEn = "";
        XgSim sampleNoteOwner;
        XgMoveSituation lastSituation;
        int lastComment = -1;

        protected static XgSim Lab => TiebaHub.Lab();

        /// <summary>True once the lab's AI is the opponent (gomoku stage 3, go 4, chess 6); before that a built-in program plays.</summary>
        protected bool AiPlays => privateAiOpponent && Lab?.BoardAiAvailable==true;

        /// <summary>Use the same saved/default AI name policy as the lab.</summary>
        protected static string AiName
        {
            get
            {
                var lab = Lab;
                return lab != null ? lab.AiDisplayName : Lang.T("它");
            }
        }

        /// <summary>Chance the AI plays its best move (it misreads the board otherwise).</summary>
        protected double Skill => !AiPlays?1:Lab.BoardLogicLevel==0?0:Lab.BoardLogicLevel>=3 ? .98 : .85;

        /// <summary>Every few moves of a game in progress feed the lab.</summary>
        protected void Moved(int plies) { if(plies==1)BeginRoundRecord(); }
        protected void Finished(XgGameOutcome outcome,int moves,IList<string> log)
        {
            if(!ReferenceEquals(roundOwner,Lab)||roundSession==null)return;
            bool ai=AiPlays;
            if(roundOwner.FinishBoardSession(roundSession,outcome,moves,log))
            {
                GameReactions.Finish(roundOwner,Id,outcome,moves,log,ai);
                // Keep a receipt marker, not a sentence frozen in the completion language.
                sampleNote="completed";
                sampleNoteOwner=roundOwner;
            }
            roundSession=null;
        }

        string SampleText(XgSim lab, int n)
        {
            var d = XgCatalog.Dataset(lab.GameDataset(Id));
            return "+" + n + Lang.T(" 样本 · ") + (d == null ? "" : T(d.name, d.nameEn));
        }

        /// <summary>Stage 6: a canned commentary line after the AI's move (never the same line twice in a row for a situation).</summary>
        protected void Comment(XgMoveSituation situation, int moves)
        {
            var lab = Lab;
            if (lab == null || lab.S.stage < 6 || !AiPlays) { commentary = ""; return; }
            if (!XgGames.ShouldComment(situation, Rng.NextDouble())) return;
            int i = XgGames.NoRepeat(XgGames.CommentCount(situation), situation == lastSituation ? lastComment : -1, Rng.NextDouble());
            lastSituation = situation; lastComment = i;
            var line = XgGames.Comment(situation, i, lab.GameContext(Id, XgGameOutcome.Draw, moves));
            commentary = line.zh; commentaryEn = line.en;
        }

        /// <summary>A new game clears the commentary; the last sample note stays as a receipt.</summary>
        protected void ClearComment() { commentary = ""; }

        /// <summary>The commentary and sample lines for the status text (empty when there is nothing to say).</summary>
        protected string LabLines()
        {
            string s = "";
            if (commentary.Length > 0) s += "\n\n<color=#7A4FA0>" + AiName + Lang.T("：「") + T(commentary, commentaryEn) + T("」", "”") + "</color>\n<size=12><color=#696E7A>" + AiContentNotice.Footer(GameText.IsEnglish) + "</color></size>";
            if (sampleNote.Length > 0 && ReferenceEquals(sampleNoteOwner,Lab)) s += "\n\n<size=15><color=#2E8B57>" + T("对局已结束。样本需在资料夹安装对局记录包。","Game complete. Samples require installing a game-record pack from Files.") + "</color></size>";
            return s;
        }
    }

    /// <summary>
    /// 游戏中心 (design v1.1 §3): the desktop's native Game Hub window, its content replaced by a small 2016 game
    /// launcher with 五子棋, 围棋 and 国际象棋. The window keeps its own name and icon. A sidebar switches between the
    /// home page (one card per game) and each game; games keep their position while you switch away.
    /// </summary>
    public sealed partial class GameHubView : MonoBehaviour
    {
        public const string NativeWindow = "Game Hub";
        static readonly Color Side = new Color32(32, 38, 56, 255), SideHover = new Color32(52, 62, 90, 255), Page = new Color32(240, 242, 246, 255);
        WindowManager window;
        TMP_FontAsset font;
        RectTransform main, home;
        readonly List<HubGame> games = new List<HubGame>();
        readonly Dictionary<string, RectTransform> areas = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Image> navFills = new Dictionary<string, Image>();
        readonly Dictionary<string, TMP_Text> navLabels = new Dictionary<string, TMP_Text>();
        readonly List<(HubGame game, TMP_Text title, TMP_Text blurb, TMP_Text state, TMP_Text button)> cards = new List<(HubGame, TMP_Text, TMP_Text, TMP_Text, TMP_Text)>();
        TMP_Text homeTitle, homeLine, sideTitle;
        string showing = "home";
        bool boardLanguageDirty;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static GameHubView Install()
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == NativeWindow);
            var holder = window != null ? window.transform.Find("Container/Content") : null;
            if (holder == null) return null;
            var view = holder.gameObject.GetComponent<GameHubView>() ?? holder.gameObject.AddComponent<GameHubView>();
            view.window = window;
            GameText.Changed += view.OnLanguage;
            return view;
        }

        void OnDestroy() { GameText.Changed -= OnLanguage; composerSprites?.Dispose(); }

        void OnLanguage() { Relabel(); boardLanguageDirty=true; }
        void LateUpdate()
        {
            if(!boardLanguageDirty||window==null||!window.isOn)return;
            // The controller updates its display locale during Update. Redraw once afterwards,
            // including completed boards that no longer receive move-driven redraws.
            foreach(var game in games)if(game.Id==showing){game.SyncBoardOwner();game.Refresh();break;}
            boardLanguageDirty=false;
        }

        void Start()
        {
            font = PrologueDesk.CjkFont();
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            var root = PrologueDesk.Rect("Game Hub 2016", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Page);
            UiFitScale.Attach(root, window, new Vector2(1266, 763));

            games.Add(Add<GomokuPanel>()); games.Add(Add<GoPanel>()); games.Add(Add<ChessPanel>()); games.Add(Add<LudoPanel>()); games.Add(Add<ChineseCheckersPanel>()); games.Add(Add<MonopolyPanel>());

            var side = PrologueDesk.Rect("Sidebar", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(210, 0));
            PrologueDesk.Fill(side, Side);
            sideTitle = Text(PrologueDesk.Rect("Title", side, new Vector2(0, 1), Vector2.one, new Vector2(20, -70), new Vector2(-10, -18)), "", 24, Color.white, TextAlignmentOptions.MidlineLeft);
            sideTitle.fontStyle = FontStyles.Bold;
            Nav(side, "home", "⌂", 0);
            for (int i = 0; i < games.Count; i++) Nav(side, games[i].Id, games[i].Glyph, i + 1);
            Nav(side,"board.reader","▦",games.Count+1);
            Nav(side,"board.composer","♛",games.Count+2);
            Nav(side,"board.xiangqi","车",games.Count+3);

            main = PrologueDesk.Rect("Main", root, Vector2.zero, Vector2.one, new Vector2(210, 24), Vector2.zero);
            var notice = AiContentNotice.CreateFooter(root, font, new Color32(105, 110, 122, 255));
            notice.rectTransform.parent.GetComponent<RectTransform>().offsetMin = new Vector2(210, 0);
            BuildHome();
            BuildBoardReader();
            BuildBoardComposer();
            BuildXiangqiComposer();
            foreach (var g in games)
            {
                var area = PrologueDesk.Rect(g.Id, main, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                g.Init(font); g.Build(area); g.AttachBoardControls(area);
                area.gameObject.SetActive(false);
                areas[g.Id] = area;
            }
            Relabel();
            Show("home");
        }

        T2 Add<T2>() where T2 : HubGame => gameObject.AddComponent<T2>();

        void Nav(RectTransform side, string id, string glyph, int index)
        {
            var rt = PrologueDesk.Rect("Nav " + id, side, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -140 - index * 62), new Vector2(-8, -84 - index * 62));
            var img = PrologueDesk.Fill(rt, Side);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var colors = b.colors; colors.highlightedColor = new Color(1.25f, 1.25f, 1.35f, 1); b.colors = colors;
            b.onClick.AddListener(() => Show(id));
            // Resolve the drawn icon first, so unused fallback glyphs never reach TMP.
            if (LingGuangV05.Desktop.XingGuang.XgIcons.Place(rt, "hub", id, 30, 28, 40) == null)
                Text(PrologueDesk.Rect("Glyph", rt, Vector2.zero, new Vector2(0, 1), new Vector2(10, 0), new Vector2(50, 0)), glyph, 26, Color.white, TextAlignmentOptions.Center);
            navLabels[id] = Text(PrologueDesk.Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(58, 0), new Vector2(-6, 0)), "", 20, Color.white, TextAlignmentOptions.MidlineLeft);
            navFills[id] = img;
        }

        void BuildHome()
        {
            home = PrologueDesk.Rect("Home", main, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            homeTitle = Text(PrologueDesk.Rect("Title", home, new Vector2(0, 1), Vector2.one, new Vector2(40, -86), new Vector2(-40, -30)), "", 34, new Color32(30, 36, 52, 255), TextAlignmentOptions.MidlineLeft);
            homeTitle.fontStyle = FontStyles.Bold;
            homeLine = Text(PrologueDesk.Rect("Line", home, new Vector2(0, 1), Vector2.one, new Vector2(40, -124), new Vector2(-40, -86)), "", 18, new Color32(100, 108, 124, 255), TextAlignmentOptions.MidlineLeft);
            for (int i = 0; i < games.Count; i++)
            {
                var g = games[i];
                float x = 30 + (i%3)*330, top = 145+(i/3)*280;
                var card = PrologueDesk.Rect("Card " + g.Id, home, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x,-top-260),new Vector2(x+300,-top));
                PrologueDesk.Fill(card, Color.white);
                card.gameObject.AddComponent<Outline>().effectColor = new Color32(214, 218, 228, 255);
                var art = PrologueDesk.Rect("Art", card, new Vector2(0, 1), Vector2.one, new Vector2(0, -72), Vector2.zero);
                PrologueDesk.Fill(art, g.Accent, false);
                if (LingGuangV05.Desktop.XingGuang.XgIcons.Place(art, "hub", g.Id, 150, 36, 58) == null)
                    Text(PrologueDesk.Rect("Glyph", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), g.Glyph, 48, Color.white, TextAlignmentOptions.Center);
                var title = Text(PrologueDesk.Rect("Name", card, new Vector2(0, 1), Vector2.one, new Vector2(15,-111),new Vector2(-15,-76)), "", 26, new Color32(30, 36, 52, 255), TextAlignmentOptions.MidlineLeft);
                title.fontStyle = FontStyles.Bold;
                var blurb = Text(PrologueDesk.Rect("Blurb", card, Vector2.zero, Vector2.one, new Vector2(15,78),new Vector2(-15,-114)), "", 16, new Color32(90, 98, 114, 255));
                var state = Text(PrologueDesk.Rect("State", card, Vector2.zero, new Vector2(1, 0), new Vector2(20, 66), new Vector2(-20, 92)), "", 14, new Color32(200, 90, 40, 255), TextAlignmentOptions.MidlineLeft);
                var play = PrologueDesk.Rect("Play", card, Vector2.zero, new Vector2(1, 0), new Vector2(20, 14), new Vector2(-20, 58));
                var playImg = PrologueDesk.Fill(play, g.Accent);
                var pb = play.gameObject.AddComponent<Button>(); pb.targetGraphic = playImg; pb.onClick.AddListener(() => Show(g.Id));
                var label = Text(PrologueDesk.Rect("T", play, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 20, Color.white, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
                cards.Add((g, title, blurb, state, label));
            }
        }

        TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            return t;
        }

        void Show(string id)
        {
            if(id=="board.reader"&&!ReaderBought||(id=="board.composer"||id=="board.xiangqi")&&!ComposerBought)id="home";
            showing = id;
            SyncBoardReader();
            SyncBoardComposer();SyncXiangqiComposer();
            if (home != null) home.gameObject.SetActive(id == "home");
            foreach (var kv in areas) kv.Value.gameObject.SetActive(kv.Key == id);
            foreach (var g in games) if (g.Id == id) { g.SyncBoardOwner(); g.OnBoardShown(); g.Refresh(); }
            foreach (var kv in navFills) kv.Value.color = kv.Key == id ? SideHover : Side;
            Relabel();
        }

        void Relabel()
        {
            if (sideTitle == null) return;
            sideTitle.text = Lang.T("游戏中心");
            navLabels["home"].text = Lang.T("首页");
            RelabelBoardReader();
            RelabelBoardComposer();RelabelXiangqiComposer();
            foreach (var g in games) navLabels[g.Id].text = g.Title;
            homeTitle.text = Lang.T("游戏中心");
            homeLine.text = Lang.T("2016 · 本地对战 · 不用联网");
            foreach (var c in cards)
            {
                c.title.text = c.game.Title;
                c.blurb.text = c.game.Blurb;
                c.state.text = c.game.Locked ?? "";
                c.button.text = Lang.T("开始");
            }
        }

        void Update()
        {
            SyncBoardReader();
            SyncBoardComposer();TickBoardComposer();SyncXiangqiComposer();TickXiangqiComposer();
            if (sideTitle == null || Time.frameCount % 30 != 0) return;
            // Titles can be teased by the lab's AI for a while (五子棋 at stage 5–6), so they are re-read here.
            foreach (var g in games) { g.SyncBoardOwner(); navLabels[g.Id].text = g.Title; }
            // Lock text, titles and blurbs follow the lab's stage while the home page is open.
            if (showing == "home") foreach (var c in cards) { c.state.text = c.game.Locked ?? ""; c.title.text = c.game.Title; c.blurb.text = c.game.Blurb; }
        }
    }
}
