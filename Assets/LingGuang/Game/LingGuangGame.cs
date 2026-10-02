using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuang.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LingGuang.Game
{
    /// <summary>
    /// Presenter root for 《灵光一闪》. Owns the RunState, undo stack and input; plays simulator events through FxPlayer.
    /// Builds everything at runtime so the scene only needs this one component.
    /// </summary>
    public sealed class LingGuangGame : MonoBehaviour
    {
        public int seed = 0;               // 0 = random
        public bool showNoticeOnStart = true;

        GameConfig cfg;
        RunState state;
        readonly List<RunState> undo = new List<RunState>();
        BoardView board;
        FxPlayer fx;
        CameraRig rig;
        SynthAudio synth;
        Hud hud;
        Camera cam;
        TMP_FontAsset font;

        int tutorialIndex = -1;
        List<TutorialLevel> tutorial;
        float idleTimer;
        int lastHintPhase = -1;
        int selectedStart = -1;
        int placingCandidate = -1;
        int ghostDir = 0;
        bool busy;
        bool reduceFlash;
        bool debugOpen;
        Volume volume;
        Bloom bloom;

        // mouse state
        Vector2 pressPos;
        int pressCell = -1;
        bool pressing, dragging, panning, draggingSpark;
        float lastClickTime;
        Vector3 lastPanWorld;
        float rotateCooldown;
        int hoverCell = -1;

        string ConfigPath => Path.Combine(Application.streamingAssetsPath, "LingGuang", "GameConfig.json");
        System.DateTime configStamp;
        float configPoll;

        // ------------------------------------------------------------------ automation hooks (debug / MCP testing)

        public RunState State => state;
        public Hud HudRef => hud;
        public bool Busy => busy;
        public void AutoCloseModal() { if (hud.ModalOpen) hud.modalButton.onClick.Invoke(); }
        public void AutoSelect(int sparkId) { selectedStart = sparkId; Refresh(); }
        public void AutoConfirm() => TryConfirm();
        public void AutoEndRound() => hud.onEndRound?.Invoke();
        public bool AutoPlace(int candidate, int cell, int dir) => Do(() => state.Place(candidate, cell, dir));
        public void AutoSkipAnim() => fx.skipRequested = true;
        public void AutoNewRun(int sd) => NewRun(sd);
        public bool autoPlay;

        IEnumerator AutoPlayLoop()
        {
            while (autoPlay)
            {
                if (hud.ModalOpen) { yield return new WaitForSeconds(0.6f); AutoCloseModal(); yield return null; continue; }
                if (busy || state.phase != Phase.Prepare) { yield return null; continue; }
                var (cand, cell, dir) = Bot.ChoosePlacement(state);
                if (cand >= 0) { AutoPlace(cand, cell, dir); yield return new WaitForSeconds(0.3f); }
                else if (state.candidates.Count > 0) Do(() => state.SkipCandidates());
                for (int i = 0; i < state.shop.Count; i++) if (!state.shop[i].sold && state.coins >= state.shop[i].price) Do(() => state.Buy(i));
                if (state.IsBossRound && state.drugs.Count > 0 && !EffectDef.Get(state.drugs[0]).now.Any(e => e.op == Op.StartsAdd && e.value < 0)) Do(() => state.UseDrug(0));
                while (autoPlay && state.phase == Phase.Prepare && state.startsLeft > 0)
                {
                    int st = Bot.ChooseStart(state);
                    if (st < 0) break;
                    AutoSelect(st);
                    yield return new WaitForSeconds(0.2f);
                    TryConfirm();
                    yield return null;
                    while (busy && !hud.ModalOpen) yield return null;
                }
                if (autoPlay && state.phase == Phase.Prepare && !busy) StartCoroutine(SettleFlow());
                yield return null;
                while (busy && !hud.ModalOpen) yield return null;
            }
        }

        public void ToggleAutoPlay()
        {
            autoPlay = !autoPlay;
            hud.Toast(autoPlay ? "自动演示开启（F3 关闭）" : "自动演示关闭");
            if (autoPlay) StartCoroutine(AutoPlayLoop());
        }

        // ------------------------------------------------------------------ setup

        void Start()
        {
            Application.targetFrameRate = 120;
            font = LoadFont();
            SetupCamera();
            SetupEventSystem();
            cfg = LoadConfig();

            var bgo = new GameObject("Board");
            board = bgo.AddComponent<BoardView>();
            var fgo = new GameObject("Fx");
            fx = fgo.AddComponent<FxPlayer>();
            synth = gameObject.AddComponent<SynthAudio>();
            var hgo = new GameObject("HudRoot");
            hud = hgo.AddComponent<Hud>();
            hud.Build(font);
            WireHud();
            fx.Init(board, rig, synth, font);
            fx.lightBarWorld = () => UiWorld(hud.lightAnchor);
            fx.multBarWorld = () => UiWorld(hud.multText.rectTransform);
            fx.onEvent = OnPlayEvent;

            reduceFlash = PlayerPrefs.GetInt("lg_reduceFlash", 0) == 1;
            NewRun(seed != 0 ? seed : Random.Range(1, 999999));
            if (showNoticeOnStart) ShowNotice();
            hud.Log("tutorialDone=" + PlayerPrefs.GetInt("lg_tutorialDone", 0));
        }

        TMP_FontAsset LoadFont()
        {
            var f = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            TMP_FontAsset fa = null;
            if (f != null)
            {
                fa = TMP_FontAsset.CreateFontAsset(f, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fa != null) fa.name = "LingGuang_CJK_Dynamic";
            }
            if (fa == null) fa = TMP_Settings.defaultFontAsset;
            if (fa != null && TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset != fa)
            {
                if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
                fa.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
            }
            return fa;
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
                cgo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.016f, 0.02f, 0.04f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            cam.allowMSAA = false;
            rig = cam.GetComponent<CameraRig>() ?? cam.gameObject.AddComponent<CameraRig>();
            rig.Init(cam);

            var vgo = new GameObject("PostFX");
            volume = vgo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.4f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.72f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            volume.sharedProfile = profile;
            if (cam.GetComponent<PixelBoardPresentation>() == null)
                cam.gameObject.AddComponent<PixelBoardPresentation>();
        }

        void SetupEventSystem()
        {
            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                es = go.GetComponent<EventSystem>();
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                foreach (var m in es.GetComponents<BaseInputModule>()) Destroy(m);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        GameConfig LoadConfig()
        {
            var c = new GameConfig();
            try
            {
                if (File.Exists(ConfigPath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), c);
                    configStamp = File.GetLastWriteTimeUtc(ConfigPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                    File.WriteAllText(ConfigPath, JsonUtility.ToJson(c, true));
                    configStamp = File.GetLastWriteTimeUtc(ConfigPath);
                }
            }
            catch (System.Exception ex) { Debug.LogWarning("[LingGuang] config load failed: " + ex.Message); }
            return c;
        }

        void PollConfig(bool force = false)
        {
            configPoll -= Time.unscaledDeltaTime;
            if (!force && configPoll > 0f) return;
            configPoll = 1f;
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var stamp = File.GetLastWriteTimeUtc(ConfigPath);
                if (!force && stamp == configStamp) return;
                configStamp = stamp;
                JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), cfg);
                hud.Toast("配置已热重载（棋盘形状改动需新开一局）");
                if (!busy) { board.Sync(); hud.Refresh(state, false); }
            }
            catch (System.Exception ex) { hud.Toast("配置读取失败：" + ex.Message); }
        }

        void WireHud()
        {
            hud.onCandidate = i => { if (busy) return; placingCandidate = placingCandidate == i ? -1 : i; hud.selectedCandidate = placingCandidate; ghostDir = 0; Refresh(); };
            hud.onSkipCandidates = () => Do(() => state.SkipCandidates());
            hud.onConfirm = TryConfirm;
            hud.onEndRound = () =>
            {
                if (busy || state.phase != Phase.Prepare) return;
                if (tutorialIndex >= 0) { StartTutorial(tutorialIndex); return; }
                StartCoroutine(SettleFlow());
            };
            hud.onUndo = Undo;
            hud.onTrial = TryTrial;
            hud.onBuy = i => Do(() => state.Buy(i));
            hud.onUseDrug = i => Do(() => state.UseDrug(i));
            hud.onRefreshShop = () => Do(() => state.RerollShop());
            hud.onReview = () => Do(() => state.BuyReview());
            hud.onReroll = () => Do(() => state.RerollCandidates());
            hud.onNewRun = () =>
            {
                int.TryParse(hud.seedInput.text, out int sd);
                NewRun(sd != 0 ? sd : Random.Range(1, 999999));
            };
            hud.onToggleStep = () => { fx.stepMode = !fx.stepMode; hud.Toast(fx.stepMode ? "单拍模式：按“下一拍”或 N 推进" : "单拍模式关闭"); };
            hud.onStep = () => fx.stepRequested = true;
            hud.onSkipAnim = () => fx.skipRequested = true;
            hud.onToggleInternal = () => { board.showInternal = !board.showInternal; board.Sync(); };
            hud.onToggleReduceFlash = ToggleReduceFlash;
        }

        void ToggleReduceFlash()
        {
            reduceFlash = !reduceFlash;
            PlayerPrefs.SetInt("lg_reduceFlash", reduceFlash ? 1 : 0);
            ApplyFlashSetting();
            hud.Toast(reduceFlash ? "已开启“降低闪烁”" : "已关闭“降低闪烁”");
        }

        void ApplyFlashSetting()
        {
            fx.reduceFlash = reduceFlash;
            var pixels = cam != null ? cam.GetComponent<PixelBoardPresentation>() : null;
            if (pixels != null) pixels.ReduceFlash = reduceFlash;
            if (bloom != null) bloom.intensity.Override(reduceFlash ? 0.7f : 1.4f);
        }

        void ShowNotice()
        {
            hud.ShowModal("灵光一闪",
                "<color=#ffd9a0>光敏提示</color>：本作包含闪光与快速明暗变化。如果你对闪光敏感，请开启“降低闪烁”（随时按 F2 切换）。\n\n" +
                "三条规则：\n一、电点亮灵光，到达阈值闪烁\n二、闪烁的灵光通过光丝把电传给其他灵光\n三、不传电的光丝变细，传电的光丝变粗\n\n" +
                "<size=20>左键：选择/放置/选为起点    滚轮：指向灵光时旋转，指向背景时缩放\n拖拽灵光：移动    拖拽背景：平移    双击背景：复位视角\nEnter：发动    长按空格：快进    Tab：跳过动画    Esc：撤销/暂停\nF1：调试面板    F2：降低闪烁    F3：自动演示    F5：重载配置</size>",
                "开始", null, "胎儿期教学", () => StartTutorial(0));
        }

        // ------------------------------------------------------------------ tutorial (§15 lite)

        public void StartTutorial(int index)
        {
            StopAllCoroutines();
            busy = false;
            fx.ClearAll();
            tutorial ??= Tutorial.Levels(cfg);
            if (index >= tutorial.Count)
            {
                tutorialIndex = -1;
                PlayerPrefs.SetInt("lg_tutorialDone", 1);
                hud.ShowModal("出生", "心跳声转为了灯塔。\n从现在起，你的灵光要自己长出一张网。", "开始一局", () => NewRun(Random.Range(1, 999999)));
                return;
            }
            tutorialIndex = index;
            var lv = tutorial[index];
            state = lv.build(cfg);
            undo.Clear();
            selectedStart = -1; placingCandidate = -1; hud.selectedCandidate = -1;
            board.Init(state, font);
            rig.limits = board.FullBounds();
            rig.Fit(board.FullBounds(), true);
            idleTimer = 0f;
            Refresh();
            hud.ShowModal($"胎儿期 {index + 1}/5 · {lv.title}", lv.instruction + $"\n\n<color=#ffd9a0>目标：{lv.goal}</color>", "好", null,
                PlayerPrefs.GetInt("lg_tutorialDone", 0) == 1 ? "跳过教学" : null, () => { tutorialIndex = -1; NewRun(Random.Range(1, 999999)); });
        }

        void CheckTutorial(ConfirmResult res)
        {
            var lv = tutorial[tutorialIndex];
            if (res != null && lv.check(state, res))
            {
                fx.ShowBanner("完成", new Color(0.8f, 1f, 0.85f), 0.9f);
                synth.PlayChord(true);
                int next = tutorialIndex + 1;
                hud.ShowModal($"{lv.title} · 完成", NextLessonText(next), "下一关", () => StartTutorial(next));
            }
            else if (state.startsLeft <= 0)
            {
                hud.ShowModal("再试一次", "起点次数用完了。\n" + lv.instruction, "重来", () => StartTutorial(tutorialIndex));
            }
        }

        string NextLessonText(int next) => next < tutorial.Count ? $"下一关：{tutorial[next].title}" : "教学结束。";

        void TutorialHint()
        {
            if (tutorialIndex < 0 || busy || hud.ModalOpen) { idleTimer = 0f; return; }
            idleTimer += Time.deltaTime;
            int hc = tutorial[tutorialIndex].hintCell;
            if (idleTimer > 10f && hc >= 0)
            {
                int phase = Mathf.FloorToInt(idleTimer * 2f) % 3;
                if (phase != lastHintPhase)
                {
                    lastHintPhase = phase;
                    board.ShowPreview(new Dictionary<int, List<int>> { { phase, new List<int> { hc } } });
                }
            }
            else lastHintPhase = -1;
        }

        // ------------------------------------------------------------------ run

        void NewRun(int sd)
        {
            tutorialIndex = -1;
            StopAllCoroutines();
            busy = false;
            fx.ClearAll();
            state = RunState.NewRun(cfg, sd, PlayerPrefs.GetInt("lg_separation", 0) == 1);
            undo.Clear();
            selectedStart = -1; placingCandidate = -1;
            hud.selectedCandidate = -1;
            hud.seedInput.text = sd.ToString();
            hud.eventLog.Clear();
            board.Init(state, font);
            ApplyFlashSetting();
            rig.limits = board.FullBounds();
            rig.Fit(board.FullBounds(), true);
            rig.minSize = 3.2f;
            Refresh();
            hud.Toast($"新的一局（种子 {sd}）。选一个灵光作为起点，按 Enter 发动。");
            if (state.board.GenerationFailed) { Debug.LogError("[LingGuang] board is disconnected; check mapRows / outerGapPairs in GameConfig.json"); hud.Toast("棋盘不连通：请检查配置里的 mapRows / outerGapPairs", 6f); }
            if (autoPlay) StartCoroutine(AutoPlayLoop());
        }

        void Refresh()
        {
            if (state == null) return;
            if (selectedStart >= 0 && !state.sparks.ContainsKey(selectedStart)) selectedStart = -1;
            board.SetSelected(selectedStart >= 0 ? state.sparks[selectedStart].cell : -1);
            board.Sync();
            hud.selectedCandidate = placingCandidate < state.candidates.Count ? placingCandidate : -1;
            if (hud.selectedCandidate < 0) placingCandidate = -1;
            hud.Refresh(state, false);
            UpdatePreview();
            hud.hintText.text = Hint();
            if (tutorialIndex >= 0 && tutorial != null)
            {
                var lv = tutorial[tutorialIndex];
                hud.stageText.text = $"胎儿期 {tutorialIndex + 1}/5 · {lv.title}";
                hud.targetText.text = "目标：" + lv.goal;
                hud.gateText.text = "“结束本轮”= 重来这一关";
                hud.itemsPanel.gameObject.SetActive(false);
                hud.drugsPanel.gameObject.SetActive(false);
                hud.shopPanel.gameObject.SetActive(false);
                hud.coinsText.text = "";
            }
            UpdateDebugInfo();
        }

        string Hint()
        {
            if (state.phase != Phase.Prepare) return "";
            if (placingCandidate >= 0) return "点击空格放置，滚轮旋转朝向，右键取消";
            if (selectedStart < 0) return "点击一个灵光作为起点";
            return "按 Enter 发动；起点次数用完或点“结束本轮”时结算";
        }

        void UpdatePreview()
        {
            if (busy || selectedStart < 0 || !(state.items.Contains("lens") || state.reviewActive) || state.phase != Phase.Prepare) { board.ShowPreview(null); return; }
            var sim = state.Preview(selectedStart, 2);
            board.ShowPreview(sim?.firedCellsByBeat);
        }

        void UpdateDebugInfo()
        {
            if (!debugOpen) return;
            var s = state;
            hud.debugInfo.text = $"seed {s.seed}  stage {s.stage} round {s.roundInStage}  sparks {s.sparks.Count}  edges {s.edges.Count}  weak {s.weakLinks.Count}\n" +
                                 $"light {s.roundLight} add {s.roundAddMult:0.##}  topPattern {s.roundTopPattern ?? "-"} last {s.lastRoundTopPattern ?? "-"}  freshK {s.freshK}  step {(fx.stepMode ? "on" : "off")}";
        }

        bool Do(System.Func<bool> op)
        {
            if (busy || state.phase != Phase.Prepare) return false;
            var snap = state.Clone();
            if (op())
            {
                undo.Add(snap);
                if (undo.Count > 200) undo.RemoveAt(0);
                idleTimer = 0f;
                Refresh();
                synth.PlayTick();
                return true;
            }
            hud.Toast(state.lastError ?? "不能这样做");
            return false;
        }

        void Undo()
        {
            if (busy) return;
            if (undo.Count == 0) { ShowPause(); return; }
            state = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            board.state = state;
            placingCandidate = -1;
            Refresh();
            hud.Toast("已撤销");
        }

        void ShowPause()
        {
            hud.ShowModal("暂停", $"种子 {state.seed}\n\n降低闪烁：{(reduceFlash ? "开" : "关")}（F2）\n显示内部电量：{(board.showInternal ? "开" : "关")}（调试面板）",
                "继续", null, "重新开始", () => NewRun(Random.Range(1, 999999)));
        }

        // ------------------------------------------------------------------ confirm / playback

        void TryConfirm()
        {
            if (busy || hud.ModalOpen) return;
            if (state.phase != Phase.Prepare) return;
            if (!state.CanConfirm(selectedStart, out var why)) { hud.Toast(why); return; }
            StartCoroutine(ConfirmFlow());
        }

        IEnumerator ConfirmFlow()
        {
            busy = true;
            board.ShowPreview(null);
            board.SnapshotDisplay();
            hud.shownLight = state.roundLight; hud.shownMult = state.roundAddMult;
            var res = state.Confirm(selectedStart);
            undo.Clear();
            if (res == null) { busy = false; board.EndDisplay(); yield break; }
            hud.Log($"—— 发动 #{state.stats.confirms}（{res.events.Count} 个事件）");
            yield return fx.Play(res.events);
            board.EndDisplay();
            hud.Refresh(state, false);
            busy = false;
            Refresh();
            idleTimer = 0f;
            if (tutorialIndex >= 0) { CheckTutorial(res); yield break; }
            if (state.startsLeft <= 0) StartCoroutine(SettleFlow());
        }

        void TryTrial()
        {
            if (busy || state.phase != Phase.Prepare) return;
            if (!state.items.Contains("computer")) { hud.Toast("需要物件“电脑”"); return; }
            if (state.trialUsed) { hud.Toast("本轮已经试运行过"); return; }
            if (!state.CanConfirm(selectedStart, out var why)) { hud.Toast(why); return; }
            StartCoroutine(TrialFlow());
        }

        IEnumerator TrialFlow()
        {
            busy = true;
            var real = state;
            var clone = state.Clone();
            board.state = clone;
            board.SnapshotDisplay();
            hud.Toast("试运行：结束后自动撤回");
            var res = clone.Confirm(selectedStart);
            if (res != null) yield return fx.Play(res.events);
            board.state = real;
            real.trialUsed = true;
            board.EndDisplay();
            busy = false;
            Refresh();
        }

        void OnPlayEvent(SimEvent e)
        {
            var s = board.state;
            switch (e.type)
            {
                case SimEventType.Deliver:
                case SimEventType.RippleApply:
                case SimEventType.WeakLinkDeliver:
                case SimEventType.Leak:
                    if (e.nodeId >= 0) { board.dispCharge[e.nodeId] = e.charge; board.dispThreshold[e.nodeId] = e.threshold; }
                    break;
                case SimEventType.Fire:
                    if (e.nodeId >= 0)
                    {
                        board.dispCharge[e.nodeId] = 0;
                        board.dispThreshold[e.nodeId] = e.threshold + cfg.refireThresholdStep;
                    }
                    break;
                case SimEventType.Conduct:
                    board.SetEdgeDisplayCount(e.cell, e.cellB, e.amount);
                    break;
                case SimEventType.ScoreDelta:
                    hud.SetScore(hud.shownLight + e.light, hud.shownMult + e.mult, s);
                    hud.Pump(hud.lightText);
                    if (e.mult > 0) hud.Pump(hud.multText);
                    hud.Pump(hud.estimateText);
                    break;
            }
            if (debugOpen && e.type != SimEventType.Leak && e.type != SimEventType.BeatEnd) hud.Log(e.ToString());
        }

        // ------------------------------------------------------------------ settlement

        IEnumerator SettleFlow()
        {
            if (busy) yield break;
            busy = true;
            int oldStage = state.stage;
            int preLight = state.roundLight;
            float preMult = state.roundAddMult;
            var rep = state.Settle();
            var sc = rep.score;
            if (sc == null) { busy = false; yield break; }
            yield return MultiplyAnimation(preLight, preMult, sc.score);
            synth.PlayChord(sc.passed);
            if (state.separationUnlocked) PlayerPrefs.SetInt("lg_separation", 1);
            var lines = new List<string>
            {
                $"本轮得分 <b>{sc.score}</b> / 目标 {sc.target}    {(sc.passed ? "<color=#a8ffb8>达标</color>" : "<color=#ff9a8a>未达标</color>")}",
                $"最高回路型：{PatternName(sc.topPattern)}{(rep.patternVoided ? "（模仿关口：加成作废）" : "")}",
                $"余光 +{rep.coinsGained}" + (rep.insightGained > 0 ? $"    领悟 +{rep.insightGained}" : "")
            };
            lines.AddRange(rep.log);
            if (rep.decayedEdges > 0) lines.Add($"{rep.decayedEdges} 条光丝因废退降级");
            if (rep.stageEnded && !rep.victory) lines.Add($"\n<color=#ffd9a0>{cfg.stageNames[oldStage]}结束 → {cfg.stageNames[state.stage]}</color>");

            if (rep.gameOver || rep.victory)
            {
                board.Sync();
                hud.Refresh(state, false);
                bool done = false;
                yield return new WaitForSeconds(0.4f);
                var ending = EndingNamer.Describe(state);
                // freeze the network into a light map: full view, background tinted with the ending colour
                rig.Fit(board.FullBounds(), false);
                if (ColorUtility.TryParseHtmlString(EndingNamer.ColorHex.TryGetValue(EndingNamer.LastColorName ?? "", out var hex) ? hex : "#202030", out var endC))
                {
                    var bg = endC * (rep.victory ? 0.22f : 0.12f);
                    float t0 = 0f; var from = cam.backgroundColor;
                    while (t0 < 1.2f) { t0 += Time.deltaTime; cam.backgroundColor = Color.Lerp(from, new Color(bg.r, bg.g, bg.b), t0 / 1.2f); yield return null; }
                }
                yield return new WaitForSeconds(0.6f);
                hud.canvas.enabled = false;
                yield return null;
                string shot = SaveGallery(ending.title, rep.victory);
                yield return null;
                yield return null;
                hud.canvas.enabled = true;
                hud.ShowModal(rep.victory ? "光图 · " + ending.title : "光图 · " + ending.title + "（未完）",
                    string.Join("\n", lines.Take(2)) + "\n\n" + ending.body + (shot != null ? $"\n<size=16><color=#8090a0>光图已保存：{shot}</color></size>" : ""),
                    "再来一局", () => { done = true; cam.backgroundColor = new Color(0.016f, 0.02f, 0.04f); NewRun(Random.Range(1, 999999)); });
                busy = false;
                while (!done) yield return null;
                yield break;
            }

            bool cont = false;
            hud.ShowModal(sc.passed ? "达标" : "未达标", string.Join("\n", lines), "继续", () => cont = true);
            while (!cont) yield return null;

            if (rep.stageEnded)
            {
                // stage transition: memory fold -> lighthouse shrinks -> camera pulls out -> new region lights up -> name
                board.Sync();
                if (rep.memoryCells != null)
                {
                    foreach (int c in rep.memoryCells) board.ViewAt(c)?.Flash(0.8f);
                    fx.ShowBanner("记忆", new Color(1f, 0.9f, 0.7f), 0.8f);
                    yield return new WaitForSeconds(0.9f);
                }
                state.NextRound();
                board.RebuildGullies();
                board.Sync();
                board.ShowLighthouseCover(true);
                yield return new WaitForSeconds(0.5f);
                board.ShowLighthouseCover(false);
                rig.Fit(board.FullBounds(), false);
                yield return new WaitForSeconds(0.6f);
                var newRegion = HexBoard.StageRegion[state.stage];
                foreach (int c in state.board.AllCells().Where(c => state.board.regionOf[c] == newRegion)) board.ViewAt(c)?.Flash(0.6f);
                fx.ShowBanner(cfg.stageNames[state.stage], new Color(1f, 0.88f, 0.62f), 1.4f);
                synth.PlayPattern();
                yield return new WaitForSeconds(1.0f);
            }
            else state.NextRound();

            undo.Clear();
            selectedStart = -1;
            placingCandidate = -1;
            busy = false;
            Refresh();
            var gate = state.StageGate;
            if (state.roundInStage == 0 && gate != null) hud.Toast($"本阶段关口「{gate.name}」：{gate.desc}", 4f);
            else if (state.IsBossRound) hud.Toast("关口轮：目标为基准 2 倍" + (gate != null ? $"，「{gate.name}」生效" : ""), 3f);
        }

        /// <summary>Local gallery (图鉴, minimal): one PNG per run plus an index line of collected endings.</summary>
        string SaveGallery(string title, bool victory)
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "LingGuang", "Gallery");
                Directory.CreateDirectory(dir);
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string png = Path.Combine(dir, $"lightmap_{stamp}.png");
                ScreenCapture.CaptureScreenshot(png);
                File.AppendAllText(Path.Combine(dir, "endings.txt"), $"{stamp}\t{(victory ? "终局" : "未完")}\t{title}\tseed={state.seed}\n");
                return png;
            }
            catch (System.Exception ex) { Debug.LogWarning("[LingGuang] gallery save failed: " + ex.Message); return null; }
        }

        IEnumerator MultiplyAnimation(int light, float mult, int score)
        {
            // light and multiplier fly together, the estimate counts up to the final score
            hud.lightText.text = $"光量 {light}";
            hud.multText.text = $"× (1 + {mult:0.##})";
            float t = 0f;
            const float dur = 0.8f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0, 1, t / dur);
                hud.estimateText.text = $"= {Mathf.RoundToInt(score * u)}";
                hud.lightText.transform.localScale = hud.multText.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(u * Mathf.PI));
                yield return null;
            }
            hud.estimateText.text = $"= {score}";
            hud.Pump(hud.estimateText);
            yield return new WaitForSeconds(0.25f);
        }

        string PatternName(string id) => id == null ? "无" : cfg.patterns.FirstOrDefault(p => p.id == id)?.name ?? id;

        // ------------------------------------------------------------------ input

        Vector3 MouseWorld()
        {
            var mp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            var w = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
            w.z = 0;
            return w;
        }

        Vector3 UiWorld(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var screen = (corners[0] + corners[2]) / 2f; // overlay canvas: world == screen
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            w.z = 0;
            return w;
        }

        void Update()
        {
            if (state == null || Mouse.current == null || Keyboard.current == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            PollConfig();
            TutorialHint();
            if (kb.f5Key.wasPressedThisFrame) PollConfig(true);
            if (kb.f1Key.wasPressedThisFrame) { debugOpen = !debugOpen; hud.debugPanel.gameObject.SetActive(debugOpen); UpdateDebugInfo(); }
            if (kb.f2Key.wasPressedThisFrame) ToggleReduceFlash();
            if (kb.f3Key.wasPressedThisFrame) ToggleAutoPlay();
            fx.fastForward = kb.spaceKey.isPressed;
            if (kb.tabKey.wasPressedThisFrame && fx.playing) fx.skipRequested = true;
            if (kb.nKey.wasPressedThisFrame) fx.stepRequested = true;

            if (autoPlay) return;
            if (hud.ModalOpen)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) hud.modalButton.onClick.Invoke();
                return;
            }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) TryConfirm();
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (placingCandidate >= 0) { placingCandidate = -1; Refresh(); }
                else Undo();
            }
            if (kb.tKey.wasPressedThisFrame) TryTrial();

            var world = MouseWorld();
            bool overUI = Hud.PointerOverUI();
            int cell = overUI ? -1 : board.CellAt(world);
            var sparkUnder = cell >= 0 ? state.SparkAt(cell) : null;

            if (kb.hKey.wasPressedThisFrame && sparkUnder != null) Do(() => state.SetConch(sparkUnder.id));
            if (kb.eKey.wasPressedThisFrame && cell >= 0) Do(() => state.SetEarphoneRegion(state.board.regionOf[cell]));

            // hover + ghost + tooltip
            if (cell != hoverCell) hoverCell = cell;
            bool placing = placingCandidate >= 0 && placingCandidate < state.candidates.Count;
            if (placing)
            {
                bool ok = cell >= 0 && state.CanPlaceAt(cell);
                board.SetHover(cell, ok);
                board.SetGhost(cell >= 0, cell, state.candidates[placingCandidate], ghostDir);
            }
            else
            {
                board.SetGhost(false, -1, Shape.Conduct, 0);
                board.SetHover(draggingSpark ? cell : sparkUnder != null ? cell : -1, !draggingSpark || (cell >= 0 && state.CanPlaceAt(cell)));
            }
            hud.ShowTooltip(overUI || dragging ? null : Tooltip(cell), mouse.position.ReadValue());

            // wheel
            float scroll = mouse.scroll.ReadValue().y;
            rotateCooldown -= Time.deltaTime;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
            {
                bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed || kb.leftCommandKey.isPressed;
                if (!ctrl && placing && cell >= 0)
                {
                    if (rotateCooldown <= 0f) { ghostDir = (ghostDir + (scroll > 0 ? 5 : 1)) % 6; rotateCooldown = 0.12f; }
                }
                else if (!ctrl && sparkUnder != null && !busy)
                {
                    if (rotateCooldown <= 0f)
                    {
                        int id = sparkUnder.id;
                        Do(() => state.Rotate(id, scroll > 0 ? -1 : 1));
                        rotateCooldown = 0.12f;
                    }
                }
                else rig.Zoom(Mathf.Clamp(scroll / (Mathf.Abs(scroll) >= 50f ? 120f : 12f), -1f, 1f) * 0.12f, world);
            }

            // buttons
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (placingCandidate >= 0) placingCandidate = -1;
                else selectedStart = -1;
                Refresh();
            }
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                pressing = true; dragging = false; panning = false; draggingSpark = false;
                pressPos = mouse.position.ReadValue();
                pressCell = cell;
                lastPanWorld = world;
            }
            if (pressing && mouse.leftButton.isPressed)
            {
                if (!dragging && (mouse.position.ReadValue() - pressPos).magnitude > 8f)
                {
                    dragging = true;
                    var ps = pressCell >= 0 ? state.SparkAt(pressCell) : null;
                    if (ps != null && !busy && !placing && state.phase == Phase.Prepare && state.CanAdjust(ps, out _) && !state.nodes[ps.nodeId].isMemory)
                        draggingSpark = true;
                    else panning = true;
                }
                if (draggingSpark)
                {
                    var v = board.ViewAt(pressCell);
                    if (v != null) v.target = world;
                }
                else if (panning)
                {
                    rig.Pan(world - lastPanWorld);
                    lastPanWorld = MouseWorld();
                }
            }
            if (pressing && mouse.leftButton.wasReleasedThisFrame)
            {
                pressing = false;
                if (draggingSpark)
                {
                    var ps = state.SparkAt(pressCell);
                    if (ps != null && cell >= 0 && cell != pressCell)
                    {
                        int id = ps.id, target = cell;
                        if (!Do(() => state.Move(id, target))) board.Sync();
                    }
                    else board.Sync();
                    draggingSpark = false;
                }
                else if (!dragging) Click(cell, sparkUnder);
                dragging = false; panning = false;
            }
        }

        void Click(int cell, Spark sparkUnder)
        {
            bool dbl = Time.unscaledTime - lastClickTime < 0.3f;
            lastClickTime = Time.unscaledTime;
            if (busy) return;
            if (placingCandidate >= 0 && placingCandidate < state.candidates.Count)
            {
                if (cell >= 0 && state.CanPlaceAt(cell))
                {
                    int idx = placingCandidate, dir = ghostDir;
                    if (Do(() => state.Place(idx, cell, dir)))
                    {
                        placingCandidate = -1;
                        selectedStart = state.SparkAt(cell)?.id ?? selectedStart;
                        Refresh();
                    }
                }
                return;
            }
            if (sparkUnder != null)
            {
                var node = state.nodes[sparkUnder.nodeId];
                if (node.isMemory && selectedStart >= 0 && state.sparks.ContainsKey(selectedStart) && state.sparks[selectedStart].nodeId == node.id)
                {
                    if (!board.collapsedMemories.Add(node.id)) board.collapsedMemories.Remove(node.id);
                }
                selectedStart = sparkUnder.id;
                Refresh();
                return;
            }
            if (cell < 0 && dbl) rig.Fit(board.FullBounds(), false);
            else if (cell >= 0 && dbl) rig.Fit(board.FullBounds(), false);
        }

        string Tooltip(int cell)
        {
            if (cell < 0) return null;
            var s = state;
            var b = s.board;
            var reg = b.regionOf[cell];
            var rs = s.RegionStateOf(reg);
            var sp = s.SparkAt(cell);
            var mods = new Mods(s);
            var lines = new List<string>();
            string regionLine = $"<color=#9fb6d8>{BoardView.RegionNames[(int)reg]}</color>";
            if (!s.IsUnlocked(cell)) regionLine += $"（{cfg.stageNames[RunState.RegionStage(reg)]}解锁）";
            if (s.disabledCells.Contains(cell)) regionLine += "（已失效）";
            if (rs != null && rs.sedimented) regionLine += $"  陈光 可塑度 {rs.plasticity}";
            var pk = s.pickups.FirstOrDefault(p => p.cell == cell);
            if (sp == null)
            {
                lines.Add(regionLine);
                if (pk != null) lines.Add($"拾物点：{EffectDef.Get(pk.itemId)?.name}（还剩 {pk.roundsLeft} 轮；电信号投递到这里即领取）");
                if (s.LighthouseCells().Contains(cell)) lines.Add("灯塔照射范围");
                return string.Join("\n", lines);
            }
            var node = s.nodes[sp.nodeId];
            string name = sp.isCharacter ? $"流光 · {RunState.PersonalityNames[Mathf.Clamp(sp.personality, 0, 4)]}" : cfg.Shape(sp.shape).name;
            if (node.isMemory) name = $"{node.title}（{node.sparks.Count} 个灵光）· 成员 {name}";
            lines.Add($"<b>{name}</b>  {regionLine}");
            int thr = mods.Threshold(node);
            int lightSum = node.sparks.Sum(id => s.sparks[id].light);
            lines.Add($"电量 {node.charge / 2f:0.#} / 阈值 {thr / 2f:0.#}    光量 {(node.isMemory ? lightSum : sp.light)}    输出 {mods.Output(sp) / 2f:0.#}");
            if (node.firedThisRound) lines.Add($"本轮已闪 {node.fireCountRound} 次（疲劳：阈值 +{node.thresholdRaise / 2f:0.#}）");
            if (node.isMemory) lines.Add($"可塑度 {node.plasticity}    被触发 {node.triggerCount} 次");
            if (sp.isCharacter) lines.Add($"关系 {s.CharacterRelation(sp)}（≥{cfg.characterFamiliarAt} 熟悉）  停留 {sp.stayRounds} 轮");
            if (sp.placedByPlayer && !sp.isCharacter && !node.isMemory)
            {
                int age = s.RoundIndex - sp.placedRound;
                lines.Add(age < cfg.rootRounds ? $"扎根期：还剩 {cfg.rootRounds - age} 轮" : (s.IsAnchored(sp) ? "已扎根" : $"无依 {sp.noAnchorStreak}/{cfg.driftAfterRounds}"));
            }
            foreach (var e in s.edges.Values.Where(e => e.from == sp.id))
            {
                var to = s.sparks[e.to];
                int lvl = mods.EdgeLevel(e);
                string lv = e.pruned ? "已剪除" : lvl == 2 ? "髓鞘化" : lvl == 1 ? "加粗" : "新生";
                lines.Add($"→ 光丝 {lv}：导通 {e.count}，送 {mods.DeliveryAmount(e, false) / 2f:0.#}，延迟 {mods.Delay(e)} 拍{(e.stp > 0 ? $"，扰动 +{e.stp / 2f:0.#}" : "")}");
            }
            if (sp.shape == Shape.Instinct) lines.Add("<size=15>本能：不可移动、不可旋转</size>");
            if (sp.id == s.conchSpark) lines.Add("<color=#7fffe0>海螺指定</color>");
            return string.Join("\n", lines);
        }
    }
}
