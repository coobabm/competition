using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Games
{
    /// <summary>
    /// What the lab's AI does on the computer after a 游戏中心 game, one reaction per game and never the same twice in
    /// a row (the choice is <see cref="XgGames.PickReaction"/>). It grows with the stage: a blinking 灵光.exe button
    /// and a 是 / 否 popup, then one word as a popup or a short-lived desktop icon, then sentences in the chat and a
    /// 复盘.txt window, then the score, a 棋谱 file on the desktop and a teasing 五子棋 title, and at stage 6 taunts and
    /// praise in its own voice and a recycle bin renamed 你的棋 for ten seconds. Everything stays inside the fake
    /// desktop. Lives on the desktop layer so its timers survive the Game Hub window closing.
    /// </summary>
    public sealed class GameReactions : MonoBehaviour
    {
        const string RecycleBinIcon = "Prologue Recycle Bin";
        const float RecycleSeconds = 10, TeaseSeconds = 120, WordIconSeconds = 8;

        PrologueDesk desk;
        RectTransform review, wordIcon;
        string recycleOld;
        readonly Dictionary<int, List<string>> records = new Dictionary<int, List<string>>();
        readonly Dictionary<int, RectTransform> recordIcons = new Dictionary<int, RectTransform>();

        static (string zh, string en) tease;
        static float teaseUntil = -1;

        static string T(string zh, string en) => GameText.T(zh, en);
        static string T((string zh, string en) line) => GameText.T(line.zh, line.en);

        /// <summary>The teasing title of the 五子棋 page while it lasts (null otherwise).</summary>
        public static string GomokuTitle => teaseUntil > 0 && Time.unscaledTime < teaseUntil ? T(tease) : null;

        static GameReactions Host()
        {
            var desk = PrologueDirector.Desk;
            if (desk == null || desk.layer == null) return null;
            var host = desk.layer.GetComponent<GameReactions>();
            if (host == null) host = desk.layer.gameObject.AddComponent<GameReactions>();
            host.desk = desk;
            return host;
        }

        /// <summary>
        /// A hub game ended: the lab records it and learns from it, then one reaction shows on the desktop. Returns
        /// the samples added (0 without a lab).
        /// </summary>
        public static int Finish(XgSim lab, string game, XgGameOutcome outcome, int moves, IList<string> log)
        {
            if (lab == null) return 0;
            int samples = lab.GamePlayed(game, outcome, moves);
            var host = Host();
            if (host != null) host.React(lab, game, outcome, moves, log);
            return samples;
        }

        /// <summary>Who signs a popup: 灵光 until it can talk (stage 4), then its own name.</summary>
        static string Who(XgSim lab)
        {
            string name = lab.Profile != null ? lab.Profile.name : "";
            return lab.S.stage >= 4 && !string.IsNullOrEmpty(name) ? name : T(AppNames.AppZh, AppNames.AppEn);
        }

        void React(XgSim lab, string game, XgGameOutcome outcome, int moves, IList<string> log)
        {
            var c = lab.GameContext(game, outcome, moves);
            var kind = lab.NextGameReaction(UnityEngine.Random.value);
            float roll = UnityEngine.Random.value;
            if (Play(kind, lab, c, roll, log)) return;
            // The desktop piece it wanted is not there (no taskbar button, no recycle bin): say it in a popup instead.
            desk.Popup(Who(lab), T(lab.S.stage <= 2 ? XgGames.YesNo(outcome) : lab.S.stage == 3 ? XgGames.Word(outcome, c.aiPlayed, roll) : XgGames.Sentence(c, roll)), 5);
        }

        bool Play(XgReaction kind, XgSim lab, XgGameContext c, float roll, IList<string> log)
        {
            string who = Who(lab);
            switch (kind)
            {
                case XgReaction.Blink:
                    return Blink(4);
                case XgReaction.YesNo:
                    desk.Popup(who, T(XgGames.YesNo(c.outcome)), 4);
                    return true;
                case XgReaction.WordPopup:
                    desk.Popup(who, T(XgGames.Word(c.outcome, c.aiPlayed, roll)), 4);
                    return true;
                case XgReaction.WordIcon:
                    return WordIcon(T(XgGames.Word(c.outcome, c.aiPlayed, roll)));
                case XgReaction.ChatLine:
                    lab.AddLine("ai", T(XgGames.Sentence(c, roll)));
                    Blink(2.5f);
                    return true;
                case XgReaction.SentencePopup:
                    desk.Popup(who, T(XgGames.Sentence(c, roll)), 6);
                    return true;
                case XgReaction.ReviewNote:
                    return Review(XgGames.Review(c, roll));
                case XgReaction.ScoreChat:
                    lab.AddLine("ai", T(XgGames.Score(c, roll)));
                    Blink(2.5f);
                    return true;
                case XgReaction.RecordFile:
                    return Record(lab, c, log, who);
                case XgReaction.TitleTease:
                    tease = XgGames.Title(c, roll);
                    teaseUntil = Time.unscaledTime + TeaseSeconds;
                    if (c.game != XgGames.Gomoku) desk.Popup(who, T("五子棋那边，" + c.self + "留了句话。", c.self + " left a note on the gomoku page."), 5);
                    return true;
                case XgReaction.Taunt:
                {
                    string line = T(XgGames.Taunt(c, roll));
                    lab.AddLine("ai", line);
                    desk.Popup(who, line, 7);
                    return true;
                }
                case XgReaction.RecycleBin:
                    if (!RenameRecycleBin(T(XgGames.RecycleName(c)))) return false;
                    desk.Popup(who, Lang.T("看看回收站。"), 5);
                    return true;
                default:
                    return false;
            }
        }

        // ───────────── stage 1–3: the taskbar button and one word ─────────────

        /// <summary>Flashes an orange glow over the 灵光.exe taskbar button, the way Windows asks for attention.</summary>
        bool Blink(float seconds)
        {
            var router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
            var bridge = router != null ? router.Get(LingGuangInstallFlow.AppId) : null;
            var button = bridge != null && bridge.TaskbarShortcut != null ? bridge.TaskbarShortcut.transform as RectTransform : null;
            if (button == null || !button.gameObject.activeInHierarchy) return false;
            StartCoroutine(Flash(button, seconds));
            return true;
        }

        static IEnumerator Flash(RectTransform button, float seconds)
        {
            var old = button.Find("Game Blink");
            if (old != null) Destroy(old.gameObject);
            var glow = PrologueDesk.Rect("Game Blink", button, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var img = PrologueDesk.Fill(glow, Color.clear, false);
            for (float t = 0; t < seconds && img != null; t += Time.unscaledDeltaTime)
            {
                img.color = new Color(1f, .62f, .15f, Mathf.PingPong(t * 2.4f, 1) * .7f);
                yield return null;
            }
            if (glow != null) Destroy(glow.gameObject);
        }

        /// <summary>A desktop file named after the word appears for a few seconds, then is gone.</summary>
        bool WordIcon(string word)
        {
            if (desk.icons == null) return false;
            if (wordIcon != null) Destroy(wordIcon.gameObject);
            wordIcon = desk.Icon("Game Word Icon", word, null, art =>
            {
                desk.DrawBlankFile(art);
                desk.Text(PrologueDesk.Rect("Word", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), word, 20, PrologueDesk.Ink, TextAlignmentOptions.Center);
            });
            StartCoroutine(RemoveLater(wordIcon, WordIconSeconds));
            return true;
        }

        static IEnumerator RemoveLater(RectTransform item, float seconds)
        {
            yield return PrologueDesk.Wait(seconds);
            if (item != null) Destroy(item.gameObject);
        }

        // ───────────── stage 4–5: notes and files ─────────────

        /// <summary>A small Notepad window, 复盘.txt, with two or three lines about the game.</summary>
        bool Review(List<(string zh, string en)> lines)
        {
            if (review != null) Destroy(review.gameObject);
            var text = new List<string>();
            foreach (var l in lines) text.Add(T(l));
            var pos = new Vector2(UnityEngine.Random.Range(-300f, 100f), UnityEngine.Random.Range(-80f, 160f));
            var client = desk.Window("Game Review", Lang.T("复盘.txt - 记事本"), pos, new Vector2(460, 210), out review);
            desk.Text(PrologueDesk.Rect("Text", client, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10)), string.Join("\n", text), 18, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            return true;
        }

        /// <summary>棋谱_YYYYMMDD.txt on the desktop: the day's last few games, newest first, each with its moves.</summary>
        bool Record(XgSim lab, XgGameContext c, IList<string> log, string who)
        {
            if (desk.icons == null || log == null || log.Count == 0) return false;
            int day = lab.Today;
            string file = Lang.T("棋谱_") + day + ".txt";
            var g = XgGames.Name(c.game);
            string result = c.outcome == XgGameOutcome.Draw ? Lang.T("和棋") : c.outcome == XgGameOutcome.PlayerWon ? Lang.T("你赢") : Lang.T("你输");
            var body = new System.Text.StringBuilder();
            body.Append(T(g.zh, g.en)).Append(" · ").Append(result).Append(" · ").Append(c.moves).Append(Lang.T(" 手")).Append('\n');
            for (int i = 0; i < log.Count; i += 2)
            {
                body.Append(i / 2 + 1).Append(". ").Append(log[i]);
                if (i + 1 < log.Count) body.Append(' ').Append(log[i + 1]);
                body.Append((i / 2) % 5 == 4 ? "\n" : "   ");
            }
            if (!records.TryGetValue(day, out var games)) records[day] = games = new List<string>();
            games.Insert(0, body.ToString().TrimEnd());
            if (games.Count > 3) games.RemoveRange(3, games.Count - 3);

            if (!recordIcons.TryGetValue(day, out var icon) || icon == null)
            {
                icon = desk.notepadIcon != null ? desk.Icon("Game Record " + day, file, desk.notepadIcon) : desk.Icon("Game Record " + day, file, null, desk.DrawBlankFile);
                icon.GetComponent<PrologueClick>().Open = () => OpenRecord(day, file);
                recordIcons[day] = icon;
            }
            desk.Popup(who, Lang.T("棋谱存到桌面了：") + file, 6);
            return true;
        }

        void OpenRecord(int day, string file)
        {
            if (!records.TryGetValue(day, out var games)) return;
            var client = desk.Window("Game Record Window", file + Lang.T(" - 记事本"), new Vector2(-120, 60), new Vector2(620, 460), out _);
            var t = desk.Text(PrologueDesk.Rect("Text", client, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10)), string.Join("\n\n", games), 15, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            t.overflowMode = TextOverflowModes.Truncate;
        }

        // ───────────── stage 6: the recycle bin ─────────────

        bool RenameRecycleBin(string name)
        {
            var icon = desk.icons != null ? desk.icons.Find(RecycleBinIcon) : null;
            var title = icon != null ? icon.Find("Title") : null;
            var label = title != null ? title.GetComponent<TMP_Text>() : null;
            if (label == null) return false;
            StartCoroutine(Rename(label, name));
            return true;
        }

        IEnumerator Rename(TMP_Text label, string name)
        {
            if (recycleOld == null) recycleOld = label.text;
            label.text = name;
            yield return PrologueDesk.Wait(RecycleSeconds);
            // A language switch in between already put the right name back; only undo our own.
            if (label != null && label.text == name && recycleOld != null) label.text = recycleOld;
            recycleOld = null;
        }
    }
}
