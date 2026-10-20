using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Private phone-backup classification and review. Compatibility component name is retained.</summary>
    public sealed class AjiePhoneSort : MonoBehaviour
    {
        const float CheckEvery = .5f;
        static readonly Color Yellow = new Color32(247, 200, 82, 255), Tab = new Color32(232, 176, 52, 255), Pick = new Color32(59, 120, 219, 255);

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        float nextCheck;
        bool leadIn;
        RectTransform window, now, foldersBox, bottom;
        TMP_Text header, nowName, nowPick, status;
        readonly Dictionary<string, RectTransform> tiles = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, TMP_Text> counts = new Dictionary<string, TMP_Text>();
        readonly List<GameObject> buttons = new List<GameObject>();
        string selected;
        Coroutine playing;

        public static AjiePhoneSort Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var p = host.GetComponent<AjiePhoneSort>() ?? host.gameObject.AddComponent<AjiePhoneSort>();
            p.host = host;
            return p;
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        static XgSim Sim { get { var lab = XingGuangController.Instance; return lab != null ? lab.Sim : null; } }

        bool Busy()
        {
            if (AiJoinsYy.Playing || AutoLabelEpiphany.Playing || StoryFocus.Active) return true;
            return host is StoryDesktopPresenter presenter && (presenter.CutscenePlaying || presenter.NotificationsHeld);
        }

        void OnDisable() { StopAllCoroutines(); if (window != null) Destroy(window.gameObject); window = null; playing = null; morning = false; leadIn = false; toastShown = false; }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckEvery;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var sim = Sim;
            if (runtime == null || runtime.Sim == null || runtime.TestMode || runtime.Sim.InPrologue || sim == null || !sim.DesktopHosted) return;
            if (sim.S.psStep == 0)
            {
                if (!sim.PhoneSortDue || Busy() || InnerVoice.Busy || leadIn) return;
                StartCoroutine(LeadIn(sim));
                return;
            }
            if (sim.S.psStep >= 4 || leadIn) return;
            if (sim.S.psStep == 3)
            {
                if (!morning && !Busy() && !InnerVoice.Busy) StartCoroutine(Morning(sim));
                return;
            }
            if (window != null || Busy()) return;
            // The scene is on a step but its window is not up (a reload, or the player closed it): bring it back once
            // it is quiet, from a toast so it never jumps over what the player is doing.
            if (!toastShown && host is StoryDesktopPresenter p)
            {
                toastShown = true;
                p.ShowToast(T("我的备份 U 盘还插着。", "My backup stick is still plugged in."), () => { toastShown = false; Open(Sim); });
            }
        }

        bool toastShown;

        IEnumerator LeadIn(XgSim sim)
        {
            leadIn = true; sim.PhoneSortStart(); runtime.MarkDirty();
            InnerVoice.Say("翻到一个旧 U 盘，里面是我旧手机四年的备份。", "I found an old USB stick: four years of my old phone's backups.", 3f);
            while (InnerVoice.Busy) yield return null;
            InnerVoice.Say("四千多张，表情包、截图、自拍混在一起。八个文件夹……让它试着挑。", "Over four thousand pictures: memes, screenshots, selfies mixed together. Eight folders… let it try picking.", 3f);
            while (InnerVoice.Busy) yield return null;
            leadIn = false; Open(Sim);
        }

        // ───────────── the window ─────────────

        void Open(XgSim sim)
        {
            var desk = PrologueDirector.Desk;
            if (sim == null || desk == null || window != null) return;
            var client = desk.Window("Private Phone Backup", T("G:\\我的旧手机备份", "G:\\My old phone backup"), Vector2.zero, new Vector2(1060, 680), out window, () => { window = null; StopPlaying(); });
            client.GetComponent<Image>().color = new Color32(246, 248, 252, 255);
            header = desk.Text(PrologueDesk.Rect("Header", client, new Vector2(0, 1), Vector2.one, new Vector2(18, -40), new Vector2(-18, -8)), "", 16, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
            now = PrologueDesk.Rect("Now", client, new Vector2(0, 0), new Vector2(0, 1), new Vector2(18, 110), new Vector2(378, -50));
            PrologueDesk.Fill(now, Color.white).raycastTarget = false;
            now.gameObject.AddComponent<Outline>().effectColor = new Color32(210, 216, 228, 255);
            nowName = desk.Text(PrologueDesk.Rect("Name", client, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 76), new Vector2(378, 104)), "", 13, PrologueDesk.Muted, TextAlignmentOptions.MidlineLeft);
            nowPick = desk.Text(PrologueDesk.Rect("Pick", client, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 50), new Vector2(378, 76)), "", 16, Pick, TextAlignmentOptions.MidlineLeft);
            foldersBox = PrologueDesk.Rect("Folders", client, new Vector2(0, 0), Vector2.one, new Vector2(398, 110), new Vector2(-18, -50));
            tiles.Clear(); counts.Clear();
            for (int i = 0; i < XgSim.PhoneFolders.Length; i++)
            {
                var f = XgSim.PhoneFolders[i];
                int col = i % 4, row = i / 4;
                var tile = PrologueDesk.Rect("Folder " + f.id, foldersBox, new Vector2(col / 4f, 1 - (row + 1) / 2f), new Vector2((col + 1) / 4f, 1 - row / 2f), new Vector2(8, 8), new Vector2(-8, -8));
                var bg = PrologueDesk.Fill(tile, new Color32(255, 255, 255, 255));
                var button = tile.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
                string id = f.id;
                button.onClick.AddListener(() => FolderClicked(id));
                var tab = PrologueDesk.Rect("Tab", tile, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-62, 34), new Vector2(-10, 50));
                PrologueDesk.Fill(tab, Tab, false);
                var body = PrologueDesk.Rect("Body", tile, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-62, -30), new Vector2(62, 40));
                PrologueDesk.Fill(body, Yellow, false);
                desk.Text(PrologueDesk.Rect("Name", tile, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 30), new Vector2(-4, 62)), T(f.zh, f.en), 17, PrologueDesk.Ink, TextAlignmentOptions.Center);
                counts[f.id] = desk.Text(PrologueDesk.Rect("Count", tile, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 6), new Vector2(-4, 30)), "0", 14, PrologueDesk.Muted, TextAlignmentOptions.Center);
                tiles[f.id] = tile;
            }
            bottom = PrologueDesk.Rect("Bottom", client, Vector2.zero, new Vector2(1, 0), new Vector2(18, 8), new Vector2(-18, 46));
            status = desk.Text(PrologueDesk.Rect("Status", client, Vector2.zero, new Vector2(1, 0), new Vector2(398, 50), new Vector2(-18, 104)), "", 15, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            Show(sim);
        }

        void StopPlaying() { if (playing != null) StopCoroutine(playing); playing = null; }

        void ClearButtons() { foreach (var b in buttons) if (b != null) Destroy(b); buttons.Clear(); }

        void AddButton(string label, int index, int of, System.Action click, bool accent = false)
        {
            var desk = PrologueDirector.Desk;
            float w = 260, gap = 16, total = of * w + (of - 1) * gap;
            var b = desk.Button(bottom, label, new Vector2(-total / 2 + w / 2 + index * (w + gap), 0), new Vector2(w, 36), click, accent ? new Color32(214, 230, 255, 255) : (Color?)null);
            buttons.Add(b.gameObject);
        }

        /// <summary>The window for the step the save is on.</summary>
        void Show(XgSim sim)
        {
            if (window == null || sim == null) return;
            ClearButtons();
            selected = null;
            header.text = T("G:\\我的旧手机备份 · " + XgSim.PhoneTotal.ToString("#,0") + " 个项目", "G:\\My old phone backup · " + XgSim.PhoneTotal.ToString("#,0") + " items");
            RefreshCounts(sim);
            switch (sim.S.psStep)
            {
                case 1:
                    ShowShot(XgSim.PhoneShot("panda"));
                    nowPick.text = "";
                    status.text = T("八个文件夹，四千多张图。", "Eight folders, four thousand pictures.");
                    AddButton(T("让它来分", "Let it sort"), 0, 1, () => { if (playing == null) playing = StartCoroutine(Sort(Sim)); }, true);
                    break;
                case 2:
                    if (sim.S.psIds == 0) { ShowIds(sim); break; }
                    ShowFixes(sim);
                    break;
                case 3:
                    CloseWindow(); // the private review follows
                    break;
            }
        }

        void RefreshCounts(XgSim sim)
        {
            foreach (var f in XgSim.PhoneFolders) if (counts.TryGetValue(f.id, out var c)) c.text = sim.S.psStep >= 2 ? sim.PhoneCount(f.id).ToString("#,0") : "0";
        }

        // ───────────── sorting ─────────────

        IEnumerator Sort(XgSim sim)
        {
            ClearButtons();
            var picks = sim.PhoneSortRun();
            runtime.MarkDirty();
            var shown = new Dictionary<string, int>();
            foreach (var f in XgSim.PhoneFolders) shown[f.id] = 0;
            foreach (var c in counts.Values) c.text = "0";
            int done = 0;
            for (int i = 0; i < XgSim.PhoneShots.Length; i++)
            {
                var shot = XgSim.PhoneShots[i];
                string pick = picks[i];
                float look = Mathf.Lerp(.75f, .22f, i / (float)XgSim.PhoneShots.Length);
                ShowShot(shot);
                nowPick.text = "";
                status.text = T("它在看第 " + (done + 1) + " 张……", "Looking at picture " + (done + 1) + "…");
                yield return new WaitForSecondsRealtime(look);
                var folder = XgSim.PhoneFolder(pick);
                nowPick.text = T("→ " + folder.zh, "→ " + folder.en);
                yield return Fly(shot, pick, .26f);
                shown[pick]++; done++;
                counts[pick].text = shown[pick].ToString("#,0");
                StartCoroutine(Pop(tiles[pick]));
                XgJuice.Sfx.Play(XgJuice.Sfx.Id.Tick, 1 + done * .01f, .35f);
                // The moments the story stops on.
                if (shot.id == "catear") { InnerVoice.Say("……我那张猫耳自拍，进了表情包。", "…My cat-ear selfie went into memes.", 2.4f); InnerVoice.Say("也不能说全错。", "Can't say it's all wrong.", 2.2f); yield return new WaitForSecondsRealtime(1.6f); }
                if (shot.id == "beach") { InnerVoice.Say("海边那张是横着拍的……它扔进了「其他」。", "The beach one was taken sideways… it threw it in Other.", 2.8f); InnerVoice.Say("它认的是那几个格子。图一转，就不认识了。", "It knows those squares. Turn the picture and it doesn't know it.", 3.2f); yield return new WaitForSecondsRealtime(2f); }
            }
            // The rest go by as a blur while the counts run up.
            nowPick.text = "";
            float blur = 3.6f;
            for (float t = 0; t < blur; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / blur);
                foreach (var f in XgSim.PhoneFolders) counts[f.id].text = Mathf.RoundToInt(Mathf.Lerp(shown[f.id], sim.PhoneCount(f.id), k)).ToString("#,0");
                status.text = T("已分 " + Mathf.RoundToInt(Mathf.Lerp(done, XgSim.PhoneTotal, k)).ToString("#,0") + " / " + XgSim.PhoneTotal.ToString("#,0"),
                    "Sorted " + Mathf.RoundToInt(Mathf.Lerp(done, XgSim.PhoneTotal, k)).ToString("#,0") + " / " + XgSim.PhoneTotal.ToString("#,0"));
                if (Random.value < .5f) StartCoroutine(Blur());
                yield return null;
            }
            RefreshCounts(sim);
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Ding);
            playing = null;
            while (InnerVoice.Busy) yield return null;
            InnerVoice.Say("分完了。……「其他」里怎么这么多卡片？", "Done. …Why is Other full of cards?", 2.8f);
            Show(sim);
        }

        IEnumerator Fly(XgPhotoShot shot, string folder, float seconds)
        {
            if (!tiles.TryGetValue(folder, out var tile) || window == null) yield break;
            var fly = PrologueDesk.Centered("Flying", window, Vector2.zero, new Vector2(120, 120));
            Paint(fly, shot);
            Vector3 from = now.position, to = tile.position;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (fly == null) yield break;
                float k = t / seconds, e = 1 - (1 - k) * (1 - k);
                fly.position = Vector3.Lerp(from, to, e);
                float s = Mathf.Lerp(2.2f, .35f, e);
                fly.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            if (fly != null) Destroy(fly.gameObject);
        }

        IEnumerator Blur()
        {
            if (window == null) yield break;
            var f = XgSim.PhoneFolders[Random.Range(0, XgSim.PhoneFolders.Length)];
            var fly = PrologueDesk.Centered("Blur", window, Vector2.zero, new Vector2(40, 40));
            var img = PrologueDesk.Fill(fly, Color.HSVToRGB(Random.value, .25f, .95f), false);
            Vector3 from = now.position + (Vector3)(Random.insideUnitCircle * 60), to = tiles[f.id].position;
            for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
            {
                if (fly == null) yield break;
                fly.position = Vector3.Lerp(from, to, t / .35f);
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1 - t / .35f);
                yield return null;
            }
            if (fly != null) Destroy(fly.gameObject);
        }

        static IEnumerator Pop(RectTransform rt)
        {
            for (float t = 0; t < .18f; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                float s = 1 + .1f * Mathf.Sin(t / .18f * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        // ───────────── checking ─────────────

        void ShowIds(XgSim sim)
        {
            ShowShot(XgSim.PhoneShot("idcard"));
            nowPick.text = T("其他 · " + XgSim.PhoneIdCards + " 张", "Other · " + XgSim.PhoneIdCards + " of these");
            status.text = T(XgSim.PhoneIdCards + " 张旧证件照片。是我以前办手续时留下的备份，已经不再需要。",
                XgSim.PhoneIdCards + " old ID-document photos left from my own paperwork. They are no longer needed.");
            AddButton(T("从我的备份删掉", "Delete from my backup"), 0, 2, () => { sim.PhoneIds(true); runtime.MarkDirty(); Show(sim); });
            AddButton(T("暂时保留", "Keep for now"), 1, 2, () => { sim.PhoneIds(false); runtime.MarkDirty(); Show(sim); });
        }

        void ShowFixes(XgSim sim)
        {
            var wrong = sim.PhoneWrong();
            string summary = T(XgSim.PhoneTotal.ToString("#,0") + " 张，它分对了 " + sim.PhoneRightPct + "%。", XgSim.PhoneTotal.ToString("#,0") + " pictures, " + sim.PhoneRightPct + "% where they belong.");
            if (wrong.Count == 0)
            {
                ShowShot(null);
                status.text = summary + T(" 翻了一遍，没看出放错的。", " Looked through: none in the wrong place that I can see.");
            }
            else
            {
                var shot = selected != null ? XgSim.PhoneShot(selected) : wrong[0];
                selected = shot.id;
                ShowShot(shot);
                var at = XgSim.PhoneFolder(sim.PhonePick(shot.id));
                nowPick.text = T("它放进了「" + at.zh + "」", "It put this in " + at.en);
                status.text = summary + T(" 这几张不对（还有 " + wrong.Count + " 张）：点它该去的文件夹。", " These are wrong (" + wrong.Count + " left): click the folder it belongs in.");
                if (wrong.Count > 1) AddButton(T("下一张", "Next one"), 0, 2, () => { var w = Sim.PhoneWrong(); int i = w.FindIndex(x => x.id == selected); selected = w[(i + 1) % w.Count].id; Show(Sim); });
            }
            AddButton(T("检查完成，保存备份", "Checked. Save the backup"), wrong.Count > 1 ? 1 : 0, wrong.Count > 1 ? 2 : 1, () => { if (Sim.PhoneHandBack()) { runtime.MarkDirty(); CloseWindow(); } }, true);
        }

        void FolderClicked(string folder)
        {
            var sim = Sim;
            if (sim == null || sim.S.psStep != 2 || sim.S.psIds == 0 || selected == null) return;
            if (sim.PhoneMove(selected, folder))
            {
                runtime.MarkDirty();
                XgJuice.Sfx.Play(XgJuice.Sfx.Id.Swoosh);
                StartCoroutine(Pop(tiles[folder]));
                selected = null;
                RefreshCounts(sim);
                Show(sim);
            }
            else XgJuice.Sfx.Play(XgJuice.Sfx.Id.Thud, 1, .5f);
        }

        // The original psStep / psAlibi values remain valid; the final choice is a private review.
        bool morning;
        void CloseWindow() { StopPlaying(); if (window != null) Destroy(window.gameObject); window = null; }
        IEnumerator Morning(XgSim sim)
        {
            morning = true; CloseWindow();
            InnerVoice.Say("备份整理好了。检查过错分的图，再留一句复盘。", "The backup is sorted. I checked its mistakes; now a note for next time.", 2.6f);
            while (InnerVoice.Busy) yield return null;
            var desk = PrologueDirector.Desk;
            if (desk == null) { morning = false; yield break; }
            var client = desk.Window("Phone Backup Review", T("旧手机备份 · 复盘", "Old phone backup · review"), Vector2.zero, new Vector2(620,280), out window, () => { window = null; morning = false; });
            desk.Text(PrologueDesk.Rect("Summary", client, Vector2.zero, Vector2.one, new Vector2(20,90), new Vector2(-20,-20)),
                T("分对了 " + sim.PhoneRightPct + "%；猫耳自拍进了表情包，横过来的照片就认不出来。", "It sorted " + sim.PhoneRightPct + "% correctly; the cat-ear selfie became a meme, and a rotated photo confused it."), 18, PrologueDesk.Ink, TMPro.TextAlignmentOptions.MidlineLeft);
            desk.Button(client, T("先让它分，我来检查", "Let it sort; I check"), new Vector2(-145,-85), new Vector2(270,42), () => Answer(true));
            desk.Button(client, T("换一批再验证", "Test another batch"), new Vector2(145,-85), new Vector2(270,42), () => Answer(false));
        }
        void Answer(bool script)
        {
            var sim = Sim; if (sim == null) return;
            var reply = sim.PhoneAlibi(script); if (reply.Length == 0) return;
            runtime.MarkDirty(); CloseWindow(); StartCoroutine(After(sim, reply));
        }
        IEnumerator After(XgSim sim, string[] reply)
        {
            InnerVoice.Say(reply[0], reply[1], 2.6f);
            while (InnerVoice.Busy) yield return null;
            InnerVoice.Say("它能挑了。可它不是真的看见，歪一点就不认识。得让它真的看见。", "It can pick now, but tilt a picture and it is lost. It needs to really see.", 3.2f);
            while (InnerVoice.Busy) yield return null;
            morning = false;
        }

        // ───────────── pictures ─────────────

        void ShowShot(XgPhotoShot shot)
        {
            for (int i = now.childCount - 1; i >= 0; i--) Destroy(now.GetChild(i).gameObject);
            nowName.text = shot != null ? shot.file : "";
            if (shot != null) Paint(now, shot);
        }

        /// <summary>A licensed photo, or a drawn one (chat, game, selfie, cat ears, meme, panda, ID card).</summary>
        static void Paint(RectTransform box, XgPhotoShot shot)
        {
            var desk = PrologueDirector.Desk;
            if (box.GetComponent<RectMask2D>() == null) box.gameObject.AddComponent<RectMask2D>();
            var art = PrologueDesk.Rect("Art", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (shot.photo.Length > 0)
            {
                var sprite = DesktopMedia.Picture(shot.photo);
                var img = art.gameObject.AddComponent<Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = false;
                if (sprite != null)
                {
                    var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = sprite.rect.width / sprite.rect.height;
                }
                if (shot.sideways) art.localRotation = Quaternion.Euler(0, 0, 90);
                return;
            }
            var bg = PrologueDesk.Fill(art, Color.white, false);
            switch (shot.draw)
            {
                case "chat":
                    bg.color = new Color32(235, 235, 235, 255);
                    Block(art, new Vector2(0, .9f), new Vector2(1, 1), new Color32(57, 57, 64, 255));
                    Block(art, new Vector2(.06f, .66f), new Vector2(.62f, .8f), Color.white);
                    Block(art, new Vector2(.38f, .46f), new Vector2(.94f, .6f), new Color32(158, 234, 106, 255));
                    Block(art, new Vector2(.06f, .26f), new Vector2(.5f, .4f), Color.white);
                    Block(art, new Vector2(.5f, .08f), new Vector2(.94f, .2f), new Color32(158, 234, 106, 255));
                    break;
                case "game":
                    bg.color = new Color32(32, 46, 84, 255);
                    Block(art, new Vector2(0, 0), new Vector2(1, .35f), new Color32(70, 92, 120, 255));
                    Block(art, new Vector2(.05f, .06f), new Vector2(.4f, .11f), new Color32(250, 160, 40, 255));
                    Block(art, new Vector2(.74f, .74f), new Vector2(.95f, .95f), new Color32(20, 28, 50, 220));
                    desk.Text(PrologueDesk.Rect("Cross", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "+", 64, Color.white, TextAlignmentOptions.Center);
                    break;
                case "selfie":
                case "catear":
                    bg.color = shot.draw == "catear" ? new Color32(255, 214, 230, 255) : new Color32(206, 226, 246, 255);
                    Block(art, new Vector2(.22f, 0), new Vector2(.78f, .3f), new Color32(60, 70, 96, 255));
                    desk.Text(PrologueDesk.Rect("Head", art, new Vector2(0, .22f), new Vector2(1, .82f), Vector2.zero, Vector2.zero), "●", 190, new Color32(241, 200, 160, 255), TextAlignmentOptions.Center);
                    if (shot.draw == "catear")
                        desk.Text(PrologueDesk.Rect("Ears", art, new Vector2(0, .66f), new Vector2(1, .96f), Vector2.zero, Vector2.zero), "▲　　▲", 54, new Color32(250, 120, 170, 255), TextAlignmentOptions.Center);
                    break;
                case "panda":
                    bg.color = new Color32(250, 250, 250, 255);
                    desk.Text(PrologueDesk.Rect("Eyes", art, new Vector2(0, .4f), new Vector2(1, .8f), Vector2.zero, Vector2.zero), "●　●", 80, new Color32(30, 30, 30, 255), TextAlignmentOptions.Center);
                    desk.Text(PrologueDesk.Rect("Caption", art, new Vector2(0, .05f), new Vector2(1, .3f), Vector2.zero, Vector2.zero), T("你瞅啥", "Whatcha lookin' at"), 40, new Color32(30, 30, 30, 255), TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
                    break;
                case "meme":
                    bg.color = new Color32(255, 226, 90, 255);
                    desk.Text(PrologueDesk.Rect("Words", art, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10)), T("稳住\n我们能赢", "Steady,\nwe can win"), 52, new Color32(30, 30, 30, 255), TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
                    break;
                case "idcard":
                    bg.color = new Color32(60, 66, 72, 255);
                    var card = PrologueDesk.Rect("Card", art, new Vector2(.08f, .22f), new Vector2(.92f, .78f), Vector2.zero, Vector2.zero);
                    PrologueDesk.Fill(card, new Color32(206, 232, 240, 255), false);
                    Block(card, new Vector2(.06f, .15f), new Vector2(.34f, .85f), new Color32(170, 190, 200, 255));
                    for (int i = 0; i < 3; i++) Block(card, new Vector2(.42f, .62f - i * .2f), new Vector2(.92f - i * .1f, .7f - i * .2f), new Color32(150, 170, 182, 255));
                    break;
            }
        }

        static void Block(RectTransform parent, Vector2 min, Vector2 max, Color color)
            => PrologueDesk.Fill(PrologueDesk.Rect("Block", parent, min, max, Vector2.zero, Vector2.zero), color, false);
    }
}
