using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;

namespace LingGuangV05.UI
{
    /// <summary>
    /// Chapter-one view only. The independently-owned runtime is the sole simulation clock.
    /// Builds once, reuses controls and coalesces simulation events to at most four UI refreshes/s.
    /// </summary>
    public sealed class ChapterOneApp : MonoBehaviour
    {
        private static readonly Color Background = new Color32(12, 24, 38, 255);
        private static readonly Color PanelColor = new Color32(20, 40, 59, 255);
        private static readonly Color RaisedColor = new Color32(29, 54, 76, 255);
        private static readonly Color Ink = new Color32(234, 242, 250, 255);
        private static readonly Color Muted = new Color32(157, 181, 201, 255);
        private static readonly Color Teal = new Color32(83, 211, 184, 255);
        private static readonly Color Gold = new Color32(245, 203, 112, 255);
        private static readonly Color Purple = new Color32(185, 147, 240, 255);
        private static readonly Color Danger = new Color32(240, 151, 143, 255);
        private static readonly Color Orange = new Color32(255, 171, 88, 255);
        private static readonly string[] BoardModes = { "connect", "place", "remove" };

        private ChapterOneRuntime _runtime;
        private ChapterOneSim _observedSim;
        private TMP_FontAsset _font;
        private TMP_FontAsset _ownedFont;
        private readonly Dictionary<TMP_Text, TMP_FontAsset> _externalFontUsers = new Dictionary<TMP_Text, TMP_FontAsset>();
        private RectTransform _root, _pagesRoot, _boardArea, _resetDialog;
        private readonly Dictionary<string, RectTransform> _pages = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, UnityEngine.UI.Button> _buttons = new Dictionary<string, UnityEngine.UI.Button>();
        private readonly Dictionary<TMP_Text, string> _authoredLabels = new Dictionary<TMP_Text, string>();
        private readonly Dictionary<string, TMP_Text> _labels = new Dictionary<string, TMP_Text>();
        private readonly List<CellView> _cells = new List<CellView>(35);
        private readonly List<BoardEdgesGraphic.Segment> _edgeSegments = new List<BoardEdgesGraphic.Segment>(210);
        private readonly Dictionary<int, Vector2> _nodePositions = new Dictionary<int, Vector2>(35);
        private readonly HashSet<int> _pulseNodes = new HashSet<int>();
        private readonly List<CanvasGroup> _modalBlockedGroups = new List<CanvasGroup>(2);
        private BoardEdgesGraphic _edgeGraphic;
        private TMP_InputField _nameInput;
        private string _profileId = "lingguang";
        private string[] _availableTabs = { "board", "skills", "persona", "exam" };
        private string _activeTab = "board", _boardMode = "connect", _status = "所有软件共享同一台学习机；关闭窗口不会暂停运行。";
        private string _feedback = "这是离线规则模拟，不会下载模型或连接真实 AI。标注的是正确答案，不是判断 AI 是否答对。";
        private int _selectedNode = -1, _lastSubmitFrame = -1, _displayedCardId = -1;
        private float _nextRefresh, _pulseUntil, _pulseDuration;
        private bool _dirty = true, _built, _correctionPulse, _completionAnnounced;
        private Vector2 _lastBoardSize;

        public string ActiveTab => _activeTab;
        public string ProfileId => _profileId;
        public string[] AvailableTabs => (string[])_availableTabs.Clone();
        public string ProfileTitle => _profileId == "mail" ? "邮件" : _profileId == "xunbao" ? "寻宝" : _profileId == "home" ? "家庭" : _profileId == "yy" ? "YY" : "灵光.exe";
        public string BoardMode => _boardMode;
        public RectTransform UiRoot => _root;
        public int SelectedNode => _selectedNode;
        /// <summary>Owned by this app. Consumers must not destroy or mutate it.</summary>
        public TMP_FontAsset EffectiveFont => _font;

        public void ConfigureProfile(string appId)
        {
            string[] tabs;
            switch (appId)
            {
                case "lingguang": tabs = new[] { "board", "skills", "persona", "exam" }; break;
                case "mail": tabs = new[] { "cards" }; break;
                case "xunbao": tabs = new[] { "shop" }; break;
                case "home": tabs = new[] { "jobs", "power" }; break;
                case "yy": tabs = new[] { "chat" }; break;
                default: throw new ArgumentOutOfRangeException(nameof(appId), appId, "未知的第一章软件配置。");
            }
            if (_built)
            {
                if (_profileId == appId) return;
                throw new InvalidOperationException("软件界面构建后不能切换配置，请创建独立的应用窗口。");
            }
            _profileId = appId;
            _availableTabs = tabs;
            _activeTab = tabs[0];
        }

        private bool OwnsTab(string tab) { return Array.IndexOf(_availableTabs, tab) >= 0; }

        private string TabTitle(string tab)
        {
            switch (tab)
            {
                case "cards": return "收件箱";
                case "board": return "神经网络";
                case "skills": return "技能训练";
                case "shop": return "电脑硬件";
                case "jobs": return "家庭收入";
                case "power": return "用电账单";
                case "persona": return "我的 AI";
                case "exam": return "入学考试";
                default: return "老周";
            }
        }

        public void Initialize(ChapterOneRuntime runtime, RectTransform content, TMP_FontAsset font)
        {
            if (runtime == null || content == null) throw new ArgumentNullException(runtime == null ? nameof(runtime) : nameof(content));
            if (_built && _root.parent != content) throw new InvalidOperationException("已构建的灵光窗口不能移动到另一个内容容器。");
            if (_runtime != null) _runtime.Changed -= MarkDirty;
            _runtime = runtime;
            _runtime.EnsureInitialized();
            if (!_built) _font = CreateOwnedFont(font);
            _runtime.Changed += MarkDirty;
            GameText.Changed -= OnLanguageChanged;
            GameText.Changed += OnLanguageChanged;
            if (!_built)
            {
                Build(content);
                _built = true;
            }
            BindSimulation();
            ShowTab(_activeTab);
            Refresh();
        }

        private void OnEnable() { _dirty = true; if (_built) OnLanguageChanged(); }

        private void OnLanguageChanged()
        {
            // These bindings are created together with their labels, never discovered by a scene sweep.
            foreach (KeyValuePair<TMP_Text, string> item in _authoredLabels)
                if (item.Key != null) item.Key.text = GameText.Source(item.Value);
            Refresh();
        }

        private void OnDestroy()
        {
            GameText.Changed -= OnLanguageChanged;
            if (_runtime != null) _runtime.Changed -= MarkDirty;
            UnbindSimulation();
            // Scene-local chrome registered by the adapter may outlive the app component.
            foreach (KeyValuePair<TMP_Text, TMP_FontAsset> consumer in _externalFontUsers)
                if (consumer.Key != null) consumer.Key.font = consumer.Value;
            _externalFontUsers.Clear();
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(_root.gameObject);
                else DestroyImmediate(_root.gameObject);
            }
            if (_ownedFont != null)
            {
                // Destroy view objects at frame end before releasing font resources. A short
                // deferred release also lets TMP return its generated submesh materials safely.
                float delay = Application.isPlaying ? .1f : 0;
                Texture2D[] atlases = _ownedFont.atlasTextures;
                for (int i = 0; i < atlases.Length; i++) if (atlases[i] != null) ReleaseOwned(atlases[i], delay);
                if (_ownedFont.material != null) ReleaseOwned(_ownedFont.material, delay);
                ReleaseOwned(_ownedFont, delay);
                _ownedFont = null;
            }
        }

        private static void ReleaseOwned(UnityEngine.Object resource, float delay)
        {
            if (Application.isPlaying) Destroy(resource, delay);
            else DestroyImmediate(resource);
        }

        private TMP_FontAsset CreateOwnedFont(TMP_FontAsset fallback)
        {
            Font source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null)
            {
                Debug.LogWarning("灵光：找不到本地 Noto CJK 字体，暂用桌面传入字体。", this);
                return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
            }
            _ownedFont = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            _ownedFont.name = "LingGuangV05_" + _profileId + "_Runtime_NotoCJK";
            _ownedFont.hideFlags = HideFlags.DontSave;
            _ownedFont.isMultiAtlasTexturesEnabled = true;
            _ownedFont.TryAddCharacters(" 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,:;!?+-/=_%#()[]¥°→·—…，。；：！？（）「」是垃圾邮件否正常输入灵光老周保存读取启动第一章完成");
            return _ownedFont;
        }

        /// <summary>Apply to adapter-owned chrome without modifying the original font asset.</summary>
        public void ApplyEffectiveFont(TMP_Text text)
        {
            if (text == null || _font == null) return;
            if (!_externalFontUsers.ContainsKey(text)) _externalFontUsers.Add(text, text.font);
            text.font = _font;
            if (_ownedFont != null && !string.IsNullOrEmpty(text.text)) _ownedFont.TryAddCharacters(text.text);
        }

        private void Update()
        {
            if (!_built || _runtime == null) return;
            if (Time.unscaledTime >= _nextRefresh && (_dirty || (_activeTab == "board" && Time.unscaledTime <= _pulseUntil))) Refresh();
        }

        private void MarkDirty() { _dirty = true; }

        private void BindSimulation()
        {
            if (_runtime == null || ReferenceEquals(_runtime.Sim, _observedSim)) return;
            UnbindSimulation();
            _observedSim = _runtime.Sim;
            _observedSim.CardResolved += OnCardResolved;
            _observedSim.Pulsed += OnPulse;
            _selectedNode = -1;
            _displayedCardId = -1;
            _completionAnnounced = _observedSim.S.chapterOneComplete;
        }

        private void UnbindSimulation()
        {
            if (_observedSim == null) return;
            _observedSim.CardResolved -= OnCardResolved;
            _observedSim.Pulsed -= OnPulse;
            _observedSim = null;
        }

        private void OnCardResolved(CardResolution resolution)
        {
            // Other app instances share the runtime, but must not inherit unrelated exam feedback.
            if (resolution.cardId != _displayedCardId) return;
            _feedback = (resolution.correct ? "标注正确" : "这次标注不正确") + (resolution.corrected ? " · 已纠正模型\n" : "\n") + resolution.explanation;
            _dirty = true;
        }

        private void OnPulse(TrainingPulse pulse)
        {
            if (!OwnsTab("board")) return;
            // Preserve a readable correction trail instead of overwriting it with the next heartbeat.
            if (!pulse.correction && _correctionPulse && Time.unscaledTime < _pulseUntil) return;
            _pulseNodes.Clear();
            if (pulse.nodeIds != null) for (int i = 0; i < pulse.nodeIds.Length; i++) _pulseNodes.Add(pulse.nodeIds[i]);
            _correctionPulse = pulse.correction;
            _pulseDuration = pulse.correction ? 1.8f : 1.05f;
            _pulseUntil = Time.unscaledTime + _pulseDuration;
            _dirty = true;
        }

        public void ShowTab(string tab)
        {
            if (string.IsNullOrEmpty(tab) || !_pages.ContainsKey(tab)) return;
            _activeTab = tab;
            foreach (KeyValuePair<string, RectTransform> page in _pages) page.Value.gameObject.SetActive(page.Key == tab);
            for (int i = 0; i < _availableTabs.Length; i++)
            {
                string item = _availableTabs[i];
                UnityEngine.UI.Button button = _buttons["Tab_" + item];
                button.targetGraphic.color = item == tab ? new Color32(30, 94, 109, 255) : PanelColor;
                _labels["TabLabel_" + item].color = item == tab ? (_profileId == "xunbao" ? Orange : Teal) : Ink;
            }
            _dirty = true;
            if (_built) Refresh();
        }

        public void Refresh()
        {
            if (!_built || _runtime == null) return;
            BindSimulation();
            ChapterOneSim sim = _runtime.Sim;
            GameState state = sim.S;
            _dirty = false;
            _nextRefresh = Time.unscaledTime + .25f;
            RefreshHeader(sim);
            Text("Objective", ProfileObjective(sim));
            Text("FooterStatus", GameText.Source(_status));
            Text("SaveStatus", GameText.Source(_runtime.SaveStatus ?? "尚未保存"));
            if (state.chapterOneComplete && !_completionAnnounced)
            {
                _completionAnnounced = true;
                _status = "第一章完成：你的小模型拿到了入学资格。第二章尚未实装。";
                Text("FooterStatus", GameText.Source(_status));
            }
            switch (_activeTab)
            {
                case "cards": RefreshCards(sim, false); break;
                case "board": RefreshBoard(sim); break;
                case "skills": RefreshSkills(sim); break;
                case "shop": RefreshShop(sim); break;
                case "jobs": RefreshJobs(sim); break;
                case "power": RefreshPower(sim); break;
                case "persona": RefreshPersona(sim); break;
                case "exam": RefreshExam(sim); break;
                case "chat": RefreshChat(sim); break;
            }
        }

        private void RefreshHeader(ChapterOneSim sim)
        {
            GameState state = sim.S;
            if (_profileId == "mail")
            {
                Text("MailSummary", state.examActive ? GameText.T("分拣暂停 · 请回灵光继续入学考试", "Sorting paused · Continue the entrance exam in Lingguang") : GameText.T("待分类 1 封    已核对 ", "1 awaiting review    Reviewed ") + state.cardsReviewed + GameText.T(" 封    纠错 ", "    Corrections ") + state.correctedCards + GameText.T(" 次", " times"));
                Text("HeaderDay", GameText.T("收件箱与灵光训练同步 · 本地模拟邮件", "Inbox synced with Lingguang training · Simulated local mail"));
            }
            else if (_profileId == "yy")
            {
                Text("ContactSummary", GameText.T("联系人 1 位    未接入网络服务", "1 contact    No network service connected"));
                Text("HeaderDay", GameText.T("老周的预设留言与帮助 · 不是真人在线或实时 AI", "Lao Zhou's preset messages and help · Not live chat or real AI"));
            }
            else
            {
                Text("HeaderMoney", (_profileId == "home" ? GameText.T("可用余额", "Available balance") : GameText.T("钱包", "Wallet")) + "  ¥" + Money(state.money));
                Text("HeaderSamples", _profileId == "xunbao" ? "GPU  " + state.gpuCount + GameText.T(" 张", " cards") : _profileId == "home" ? GameText.T("收入  ¥", "Income ¥") + Money(sim.IncomePerSecond) + GameText.T("/秒", "/sec") : GameText.T("样本  ", "Samples  ") + Number(state.samples));
                Text("HeaderSteps", _profileId == "xunbao" ? GameText.T("槽位  ", "Slots  ") + state.gpuCount + "/" + state.caseCount * sim.Config.gpusPerCase : _profileId == "home" ? GameText.T("待付  ¥", "Due ¥") + Money(state.billDue) : GameText.T("训练步  ", "Steps  ") + Number(state.steps));
                Text("HeaderLearning", _profileId == "xunbao" ? GameText.T("显存  ", "VRAM  ") + Number(sim.MemoryCapacity) + "M" : _profileId == "home" ? GameText.T("温度  ", "Temperature  ") + Number(state.temperature) + "°C" : GameText.T("学习点  ", "Learning points  ") + Number(state.learningPoints));
                Text("HeaderDay", GameText.T("第 ", "Day ") + state.day + GameText.T(" 天   ", "   ") + (state.chapterOneComplete ? GameText.T("第一章已完成", "Chapter One complete") : GameText.T("第一章 · 唤醒", "Chapter One · Awakening")));
            }
            Text("HeaderState", state.breakerTripped ? GameText.T("学习机已跳闸", "Learner breaker tripped") : state.unpaidPower ? GameText.T("学习机欠费停机", "Learner power suspended") : _profileId == "mail" ? GameText.T("本地收件箱", "Local inbox") : _profileId == "yy" ? GameText.T("本地会话", "Local conversation") : GameText.T("学习机运行中", "Learner running"));
            if (_labels.TryGetValue("HeaderState", out TMP_Text status)) status.color = state.breakerTripped || state.unpaidPower ? Danger : Teal;
        }

        private string ProfileObjective(ChapterOneSim sim)
        {
            if (_profileId == "mail") return sim.S.examActive ? GameText.T("正在灵光参加入学考试，请回「灵光.exe」继续。收件箱分拣已暂停，不会在这里提交试题。", "The entrance exam is in progress. Continue in Lingguang.exe. Inbox sorting is paused; this window does not submit exam answers.") : GameText.T("邮件分拣  /  读完邮件再选择真实分类；模型的模拟建议并不总是正确。训练与考试请打开「灵光.exe」。", "Mail sorting / Read each email and choose its actual category. Model suggestions can be wrong. Open Lingguang.exe for training and exams.");
            if (_profileId == "xunbao") return GameText.T("硬件集市  /  本地模拟交易，没有真实付款或网络连接。购买前检查槽位、显存与家庭供电负载。", "Hardware market / Simulated local transactions, not real payments. Check slots, VRAM and power load before buying.");
            if (_profileId == "home") return GameText.T("家庭管理  /  收入与用电共享同一笔余额。关闭所有软件仍会接单、训练和结算电费。", "Home / Income and power share one balance. Jobs, training and electricity billing continue when all windows are closed.");
            if (_profileId == "yy") return GameText.T("与老周的会话  /  选择下方问题阅读预设帮助。这里没有接入实时聊天或真实 AI。", "Chat with Lao Zhou / Select a question for preset help. No live chat or real AI is connected.");
            return Objective(sim);
        }

        private static string Objective(ChapterOneSim sim)
        {
            GameState state = sim.S;
            if (state.chapterOneComplete) return GameText.T("章节完成  /  第二章「向外连接」尚未开放；可以继续优化网络、标注和管理设备。", "Chapter complete / Chapter Two is not available. You may continue optimizing, labeling and managing hardware.");
            if (state.breakerTripped) return GameText.T("下一步  /  去「寻宝」出售多余 GPU 降低功率，再打开「家庭」的用电账单恢复断路器。", "Next / Sell spare GPUs in Xunbao to lower load, then reset the breaker in Home's electricity bills.");
            if (state.unpaidPower) return GameText.T("下一步  /  打开「家庭」缴费；钱包不足时去「邮件」继续正确标注，可偿还欠费。", "Next / Pay in Home. If funds are low, correct labels in Mail repay the overdue balance.");
            if (state.examActive) return GameText.T("考试进行中  /  已回答 ", "Exam in progress / Answered ") + state.examAnswered + " / " + sim.Config.examQuestions + GameText.T(" 封邮件；不必一次做完，保存可保留进度。", " emails. You can save and continue later.");
            if (!sim.HasOutputPath) return GameText.T("下一步  /  切到神经网络，将输入端通过相邻节点接到输出端。点击起点，再点击终点。", "Next / Open Neural network and connect the input to outputs through adjacent nodes. Select a source, then a target.");
            if (state.cardsReviewed >= 3 && !state.jobEnabled && state.totalEarned <= 0) return GameText.T("下一步  /  已能承接第一份订单！打开「家庭」的收入页启用邮件分类，赚取维护费。", "Next / Your first job is available! Enable mail sorting in Home's income page to earn upkeep money.");
            if (state.cardsReviewed < sim.Config.examRequiredCards) return GameText.T("下一步  /  打开「邮件」完成入学前标注：", "Next / Review emails in Mail before admission: ") + state.cardsReviewed + " / " + sim.Config.examRequiredCards + GameText.T("。请选择真实分类，而不是附和模型。", ". Choose the actual category, not just the model's suggestion.");
            if (state.steps < sim.Config.examRequiredSteps) return GameText.T("下一步  /  保持网络运行，积累训练步：", "Next / Keep the network running to gain training steps: ") + Number(state.steps) + " / " + sim.Config.examRequiredSteps + GameText.T("。窗口关闭后仍会运行。", ". It keeps running with windows closed.");
            if (sim.Accuracy < sim.Config.examRequiredAccuracy) return GameText.T("下一步  /  提高模拟识别率至 ", "Next / Raise simulated accuracy to ") + Percent(sim.Config.examRequiredAccuracy) + GameText.T("；积累学习点，训练技能或调整网络。", "; earn learning points, upgrade skills or adjust the network.");
            return GameText.T("下一步  /  入学条件已满足！进入「入学考试」，挑战 ", "Next / Admission requirements met! Open Entrance exam and classify ") + sim.Config.examQuestions + GameText.T(" 封邮件。", " emails.");
        }

        private void Build(RectTransform content)
        {
            _root = Rect("LingGuangAppRoot", content);
            Fill(_root);
            BackgroundImage(_root, Background);
            RectTransform header = Rect("Header", _root);
            Top(header, 0, 0, 0, 76);
            BackgroundImage(header, PanelColor);
            Label("Brand", header, ProfileTitle, new Vector2(18, 5), new Vector2(196, 42), 27, _profileId == "xunbao" ? Orange : Ink, FontStyles.Bold);
            string subtitle = _profileId == "mail" ? "2016 / 本地邮件阅读器" : _profileId == "yy" ? "2016 / 联系人与留言" : _profileId == "xunbao" ? "2016 / 电脑硬件集市" : _profileId == "home" ? "2016 / 家庭管理" : "2016 / 模型工作台";
            Label("BrandSubtitle", header, subtitle, new Vector2(20, 49), new Vector2(210, 20), 12, Muted);
            if (_profileId == "mail" || _profileId == "yy")
                Label(_profileId == "mail" ? "MailSummary" : "ContactSummary", header, "", new Vector2(237, 12), new Vector2(720, 30), 18, Ink);
            else
            {
                Label("HeaderMoney", header, "", new Vector2(236, 10), new Vector2(220, 32), 20, _profileId == "xunbao" ? Orange : Gold);
                Label("HeaderSamples", header, "", new Vector2(466, 10), new Vector2(220, 29), 18, Ink);
                Label("HeaderSteps", header, "", new Vector2(696, 10), new Vector2(220, 29), 18, Ink);
                Label("HeaderLearning", header, "", new Vector2(926, 10), new Vector2(290, 29), 18, Teal);
            }
            Label("HeaderDay", header, "", new Vector2(237, 45), new Vector2(455, 22), 13, Muted);
            Label("HeaderState", header, "", new Vector2(926, 45), new Vector2(290, 22), 13, Teal);

            RectTransform nav = Rect("Navigation", _root);
            nav.anchorMin = new Vector2(0, 0); nav.anchorMax = new Vector2(0, 1);
            nav.pivot = new Vector2(0, 1); nav.offsetMin = new Vector2(12, 12); nav.offsetMax = new Vector2(176, -88);
            BackgroundImage(nav, PanelColor);
            _modalBlockedGroups.Add(nav.gameObject.AddComponent<CanvasGroup>());
            Label("NavigationTitle", nav, _profileId == "mail" ? "文件夹" : _profileId == "yy" ? "我的联系人" : _profileId == "xunbao" ? "商品分类" : _profileId == "home" ? "家庭事务" : "模型工作台", new Vector2(12, 11), new Vector2(145, 26), 15, Muted);
            for (int i = 0; i < _availableTabs.Length; i++)
            {
                string tab = _availableTabs[i];
                UnityEngine.UI.Button button = Button("Tab_" + tab, nav, TabTitle(tab), () => ShowTab(tab));
                Top(button.GetComponent<RectTransform>(), 7, 49 + i * 42, 7, _profileId == "yy" ? 60 : 36);
                _labels["TabLabel_" + tab] = button.GetComponentInChildren<TMP_Text>();
                button.GetComponentInChildren<TMP_Text>().fontSize = 15;
            }
            string navNotes = _profileId == "mail" ? "收件箱内为训练邮件\n\n分类结果会同步至\n灵光.exe\n\n入学考试期间\n本收件箱暂停分拣" : _profileId == "yy" ? "老周\n训练顾问 · 本地留言\n\n没有真人在线\n没有实时 AI\n\n点击问题读取回复" : _profileId == "xunbao" ? "电脑配件 · 本地商家\n\n模拟购买与回收\n不涉及真实付款\n\n电费请打开「家庭」" : _profileId == "home" ? "这台学习机也要养家\n\n接单赚取维护费\n每天自动结算电费\n\n硬件采购请去「寻宝」" : "数据来自「邮件」\n设备来自「寻宝」\n收入与电费在「家庭」\n帮助消息在「YY」";
            Label("NavigationNotes", nav, navNotes, new Vector2(12, _availableTabs.Length > 2 ? 244 : 154), new Vector2(140, 185), 12, Muted);
            TMP_Text saves = Label("SaveStatus", nav, "", new Vector2(9, 371), new Vector2(146, 36), 11, Muted);
            saves.overflowMode = TextOverflowModes.Ellipsis;
            UnityEngine.UI.Button save = Button("SaveNow", nav, "保存", () => PersistenceAction("save"));
            Bottom(save.GetComponent<RectTransform>(), 8, 47, 8, 29);
            UnityEngine.UI.Button reload = Button("ReloadSave", nav, "读取存档", () => PersistenceAction("reload"));
            Bottom(reload.GetComponent<RectTransform>(), 8, 12, 8, 29);

            RectTransform main = Rect("Main", _root);
            Fill(main, 188, 88, 12, 12);
            _modalBlockedGroups.Add(main.gameObject.AddComponent<CanvasGroup>());
            RectTransform objective = Rect("ObjectivePanel", main);
            Top(objective, 0, 0, 0, 52);
            BackgroundImage(objective, new Color32(26, 62, 77, 255));
            TMP_Text objectiveLabel = FillLabel("Objective", objective, "", 14, Teal, 13, 6, 13, 6);
            objectiveLabel.enableAutoSizing = true; objectiveLabel.fontSizeMin = 12; objectiveLabel.fontSizeMax = 14;
            _pagesRoot = Rect("Pages", main);
            Fill(_pagesRoot, 0, 62, 0, 41);
            for (int i = 0; i < _availableTabs.Length; i++)
            {
                string tab = _availableTabs[i];
                RectTransform page = Rect("Page_" + tab, _pagesRoot);
                Fill(page); _pages.Add(tab, page);
            }
            RectTransform footer = Rect("StatusPanel", main);
            Bottom(footer, 0, 0, 0, 34);
            BackgroundImage(footer, PanelColor);
            TMP_Text status = FillLabel("FooterStatus", footer, "", 12, Gold, 10, 4, 10, 4);
            status.overflowMode = TextOverflowModes.Ellipsis;
            // Build only this app's real pages. No hidden nine-page clone per window.
            foreach (string tab in _availableTabs)
            {
                switch (tab)
                {
                    case "cards": BuildMailCards(_pages[tab]); break;
                    case "board": BuildBoard(_pages[tab]); break;
                    case "skills": BuildSkills(_pages[tab]); break;
                    case "shop": BuildShop(_pages[tab]); break;
                    case "jobs": BuildJobs(_pages[tab]); break;
                    case "power": BuildPower(_pages[tab]); break;
                    case "persona": BuildPersona(_pages[tab]); BuildResetDialog(); break;
                    case "exam": BuildExam(_pages[tab]); break;
                    case "chat": BuildChat(_pages[tab]); break;
                }
            }
            // Only the finite UI corpus is prewarmed. Player names / card content are added on demand.
            if (_ownedFont != null)
            {
                var initialText = new System.Text.StringBuilder(8192);
                foreach (TMP_Text label in _labels.Values)
                    if (initialText.Length < 16384) initialText.Append(label.text);
                _ownedFont.TryAddCharacters(initialText.ToString());
            }
        }

        private void BuildMailCards(RectTransform page)
        {
            RectTransform list = Panel("MailMessageList", page, 0, 0, .285f, 1);
            RectTransform reader = Panel("MailMessageReader", page, .30f, 0, 1, 1);
            Label("MailListTitle", list, "收件箱", new Vector2(16, 15), new Vector2(240, 34), 22, Ink, FontStyles.Bold);
            Label("MailListSubtitle", list, "每次处理一封训练邮件", new Vector2(16, 55), new Vector2(246, 25), 13, Muted);
            RectTransform current = Panel("MailCurrentEnvelope", list, .04f, .20f, .96f, .60f);
            current.GetComponent<UnityEngine.UI.Image>().color = new Color32(31, 70, 94, 255);
            RegionLabel("MailPreviewSender", current, "", 0, .05f, 1, .26f, 14, Teal, 13);
            RegionLabel("MailPreviewSubject", current, "", 0, .28f, 1, .70f, 16, Ink, 13);
            UnityEngine.UI.Button open = Button("MailOpenCurrent", current, "阅读当前邮件", () => { _status = "当前邮件已在右侧打开。请选择真实分类。"; Refresh(); });
            Region(open.GetComponent<RectTransform>(), .05f, .74f, .95f, .94f, 0);
            RegionLabel("CardStats", list, "", 0, .65f, 1, .98f, 13, Muted, 16);
            TMP_Text subject = Label("CardTitle", reader, "待分类邮件", new Vector2(20, 13), new Vector2(640, 35), 22, Ink, FontStyles.Bold);
            // Mail subjects use a single header row; the complete content remains in CardPrompt.
            subject.textWrappingMode = TextWrappingModes.NoWrap;
            subject.overflowMode = TextOverflowModes.Ellipsis;
            Label("CardCategory", reader, "", new Vector2(20, 55), new Vector2(640, 24), 13, Teal);
            RegionLabel("MailSender", reader, "", 0, .17f, 1, .25f, 13, Muted, 20);
            TMP_Text body = RegionLabel("CardPrompt", reader, "", 0, .28f, 1, .54f, 21, Ink, 20);
            body.enableAutoSizing = true; body.fontSizeMin = 17; body.fontSizeMax = 21;
            RegionLabel("CardSuggestion", reader, "", 0, .55f, 1, .635f, 13, Gold, 20);
            UnityEngine.UI.Button yes = Button("CardAnswerYes", reader, "是 · 垃圾邮件", () => SubmitAnswer(true));
            Region(yes.GetComponent<RectTransform>(), .03f, .66f, .485f, .765f, 0);
            yes.targetGraphic.color = new Color32(28, 109, 113, 255);
            UnityEngine.UI.Button no = Button("CardAnswerNo", reader, "否 · 正常邮件", () => SubmitAnswer(false));
            Region(no.GetComponent<RectTransform>(), .515f, .66f, .97f, .765f, 0);
            RegionLabel("CardFeedback", reader, "", 0, .80f, 1, .99f, 13, Muted, 20);
        }

        private void BuildCards(RectTransform page, string prefix, bool exam)
        {
            RectTransform left = Panel(prefix + "Surface", page, 0, 0, .65f, 1);
            RectTransform right = Panel(prefix + "FeedbackPanel", page, .665f, 0, 1, 1);
            Label(prefix + "Title", left, exam ? "入学考试 / 邮件分类" : "给第一束灵光一份好数据", new Vector2(17, 14), new Vector2(545, 29), 22, Ink, FontStyles.Bold);
            Label(prefix + "Category", left, "", new Vector2(18, 51), new Vector2(530, 24), 13, Teal);
            TMP_Text prompt = RegionLabel(prefix + "Prompt", left, "", 0, .25f, 1, .66f, 24, Ink, 18);
            prompt.enableAutoSizing = true; prompt.fontSizeMin = 17; prompt.fontSizeMax = 24;
            TMP_Text suggestion = RegionLabel(prefix + "Suggestion", left, "", 0, .69f, 1, .8f, 14, Gold, 18);
            suggestion.overflowMode = TextOverflowModes.Ellipsis;
            UnityEngine.UI.Button yes = Button(prefix + "AnswerYes", left, "是 · 垃圾邮件", () => SubmitAnswer(true));
            Region(yes.GetComponent<RectTransform>(), .035f, .825f, .485f, .97f, 0);
            yes.targetGraphic.color = new Color32(28, 109, 113, 255);
            UnityEngine.UI.Button no = Button(prefix + "AnswerNo", left, "否 · 正常邮件", () => SubmitAnswer(false));
            Region(no.GetComponent<RectTransform>(), .515f, .825f, .965f, .97f, 0);
            Label(prefix + "FeedbackTitle", right, "复盘记录", new Vector2(15, 16), new Vector2(220, 29), 21, Teal, FontStyles.Bold);
            RegionLabel(prefix + "Feedback", right, "", 0, .16f, 1, .65f, 16, Ink, 16);
            RegionLabel(prefix + "Stats", right, "", 0, .66f, 1, .98f, 14, Muted, 16);
        }

        private void RefreshCards(ChapterOneSim sim, bool exam)
        {
            string prefix = exam ? "ExamCard" : "Card";
            bool mailPaused = !exam && _profileId == "mail" && sim.S.examActive;
            CardData card = (exam && !sim.S.examActive) || mailPaused ? null : sim.CurrentCard;
            _displayedCardId = card != null ? card.id : -1;
            Text(prefix + "Category", card == null ? mailPaused ? GameText.T("收件箱分拣已暂停", "Inbox sorting paused") : GameText.T("暂无待标注内容", "Nothing to label") : (sim.S.examActive ? GameText.T("考试题 ", "Exam question ") + (sim.S.examAnswered + 1) + " / " + sim.Config.examQuestions : GameText.T("本地训练邮件 #", "Local training email #") + card.id) + "   /   " + GameText.Source(card.category));
            Text(prefix + "Prompt", mailPaused ? GameText.T("正在灵光参加入学考试。\n请回「灵光.exe」的入学考试继续；这里不会提交试题。", "An exam is in progress.\nContinue in Lingguang.exe; this window cannot submit exam answers.") : card != null ? GameText.Source(card.prompt) : sim.S.chapterOneComplete ? GameText.T("第一章已完成。\n第二章尚未开放；可以打开「邮件」继续标注。", "Chapter One complete.\nChapter Two is not available. Keep labeling in Mail if you wish.") : GameText.T("准备好后点击上方「开始入学考试」。\n失败后可训练并重新挑战。", "Click Start entrance exam when ready.\nIf you fail, train further and try again.") );
            Text(prefix + "Suggestion", card != null ? GameText.T("模拟模型判断：", "Simulated prediction: ") + (card.predictedYes ? GameText.T("垃圾邮件", "Spam") : GameText.T("正常邮件", "Not spam")) + GameText.T("    模拟置信度 ", "    Simulated confidence ") + Percent(card.confidence) : "");
            Text(prefix + "Feedback", mailPaused ? GameText.T("考试期间分拣按钮已锁定。考试结束后，本收件箱会自动恢复训练邮件。", "Sorting is locked during the exam. Training emails resume automatically afterward.") : GameText.Source(_feedback));
            Text(prefix + "Stats", GameText.T("已核对  ", "Reviewed  ") + sim.S.cardsReviewed + GameText.T(" 封\n纠正模型  ", " emails\nModel corrections  ") + sim.S.correctedCards + GameText.T(" 次\n模拟识别率  ", "\nSimulated accuracy  ") + Percent(sim.Accuracy) + GameText.T("\n\n模型建议不是标准答案。\n正确分类积累样本和学习点。", "\n\nThe model's suggestion is not the answer key.\nCorrect labels earn samples and learning points."));
            _buttons[prefix + "AnswerYes"].interactable = card != null && (!exam || sim.S.examActive);
            _buttons[prefix + "AnswerNo"].interactable = card != null && (!exam || sim.S.examActive);
            if (!exam && _profileId == "mail")
            {
                // CurrentCard.prompt also contains the classification question on a later line.
                // Explicit CR/LF survives TMP NoWrap, so derive envelope metadata from line one.
                string prompt = card == null ? "" : FirstLine(GameText.Source(card.prompt));
                int separator = prompt.IndexOf('：');
                if (separator < 0) separator = prompt.IndexOf(':');
                string sender = separator > 0 && separator < 28 ? prompt.Substring(0, separator) : GameText.T("训练收件箱", "Training inbox");
                string subject = separator >= 0 ? prompt.Substring(separator + 1) : prompt;
                Text("CardTitle", mailPaused ? GameText.T("分拣暂停", "Sorting paused") : GameText.T("主题：", "Subject: ") + ShortText(subject, GameText.IsEnglish ? 48 : 23));
                Text("MailSender", mailPaused ? GameText.T("此窗口不会代替灵光提交入学试题。", "This window cannot submit exam answers for Lingguang.") : GameText.T("发件人：", "From: ") + sender + GameText.T("    收件人：", "    To: ") + sim.S.aiName + GameText.T("    [模拟邮件]", "    [Simulated email]"));
                Text("MailPreviewSender", mailPaused ? GameText.T("入学考试进行中", "Entrance exam in progress") : sender);
                Text("MailPreviewSubject", mailPaused ? GameText.T("回到灵光继续考试\n训练收件箱稍后恢复", "Continue the exam in Lingguang.\nTraining mail will resume afterward.") : ShortText(subject, GameText.IsEnglish ? 68 : 36));
                _buttons["MailOpenCurrent"].interactable = !mailPaused;
            }
        }

        public void SubmitAnswer(bool answer)
        {
            if (!_built || _runtime == null || _lastSubmitFrame == Time.frameCount) return;
            // Enforce app ownership synchronously: another window may have started the exam
            // after this mail view's last throttled refresh.
            if (_profileId == "mail" && _runtime.Sim.S.examActive) { _status = "请回「灵光.exe」继续入学考试，邮件分拣已暂停。"; Refresh(); return; }
            if (_profileId == "lingguang" && (_activeTab != "exam" || !_runtime.Sim.S.examActive)) return;
            if (_profileId != "mail" && _profileId != "lingguang") return;
            CardData card = _runtime.Sim.CurrentCard;
            if (card == null || card.id != _displayedCardId) { Refresh(); return; }
            _lastSubmitFrame = Time.frameCount;
            Run(() => _runtime.Sim.SubmitCard(card.id, answer));
        }

        private void BuildBoard(RectTransform page)
        {
            RectTransform toolbar = Panel("BoardToolbar", page, 0, 0, 1, .14f);
            string[] titles = { "连线", "放置节点", "移除节点" };
            for (int i = 0; i < BoardModes.Length; i++)
            {
                string mode = BoardModes[i];
                UnityEngine.UI.Button b = Button("BoardMode_" + mode, toolbar, titles[i], () => SetBoardMode(mode));
                Region(b.GetComponent<RectTransform>(), .01f + i * .16f, .13f, .15f + i * .16f, .87f, 0);
            }
            RegionLabel("BoardModeHint", toolbar, "", .5f, 0, 1, 1, 12, Muted, 8);
            _boardArea = Panel("HexBoard", page, 0, .16f, .745f, 1);
            RectTransform edgeRect = Rect("WeightedEdges", _boardArea);
            Fill(edgeRect);
            edgeRect.gameObject.AddComponent<CanvasRenderer>();
            _edgeGraphic = edgeRect.gameObject.AddComponent<BoardEdgesGraphic>();
            _edgeGraphic.raycastTarget = false;
            for (int r = 0; r < 5; r++) for (int q = 0; q < 7; q++)
            {
                int cq = q, cr = r;
                RectTransform cell = Rect("BoardCell_" + q + "_" + r, _boardArea);
                cell.gameObject.AddComponent<CanvasRenderer>();
                HexCellGraphic graphic = cell.gameObject.AddComponent<HexCellGraphic>();
                graphic.color = RaisedColor;
                UnityEngine.UI.Button button = cell.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = graphic;
                button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                SetButtonColors(button);
                button.onClick.AddListener(() => ClickBoardCell(cq, cr));
                TMP_Text label = FillLabel("CellText_" + q + "_" + r, cell, "", 11, Ink, 0, 0, 0, 0);
                label.alignment = TextAlignmentOptions.Center;
                _cells.Add(new CellView { q = q, r = r, rect = cell, graphic = graphic, label = label });
            }
            // Weighted arrows sit above cell fills; their center cutouts leave node labels readable.
            _edgeGraphic.transform.SetAsLastSibling();
            RectTransform detail = Panel("BoardDetails", page, .76f, .16f, 1, 1);
            Label("BoardDetailsTitle", detail, "网络仪表", new Vector2(13, 15), new Vector2(210, 28), 21, Teal, FontStyles.Bold);
            RegionLabel("BoardStats", detail, "", 0, .16f, 1, .59f, 14, Ink, 14);
            RegionLabel("BoardSelection", detail, "", 0, .59f, 1, .83f, 13, Gold, 14);
            RegionLabel("BoardLegend", detail, "青：正常脉冲\n紫：纠错反馈 · 线宽表示权重\n点起点→终点；重复点击撤线", 0, .83f, 1, 1, 11, Muted, 14);
        }

        public void SetBoardMode(string mode)
        {
            if (!OwnsTab("board")) return;
            if (mode != "connect" && mode != "place" && mode != "remove") return;
            _boardMode = mode;
            _selectedNode = -1;
            _status = mode == "connect" ? "连接模式：先点起点，再点相邻终点；重复连接同一条边会移除它。" : mode == "place" ? "放置模式：点击空白六边格。端口所在列不能放置隐藏节点。" : "移除模式：点击普通节点。输入与输出端口受到保护。";
            Refresh();
        }

        public void ClickBoardCell(int q, int r)
        {
            if (!OwnsTab("board") || _runtime == null || q < 0 || q > 6 || r < 0 || r > 4) return;
            ChapterOneSim sim = _runtime.Sim;
            NodeState node = FindNode(sim.S, q, r);
            if (_boardMode == "place") { Run(() => sim.AddNode(q, r)); return; }
            if (_boardMode == "remove")
            {
                if (node == null) { _status = "这里是空格，没有可移除的节点。"; Refresh(); return; }
                Run(() => sim.RemoveNode(node.id)); return;
            }
            if (node == null) { _status = "连接需要两个节点；先切换「放置节点」填充这个格子。"; Refresh(); return; }
            if (_selectedNode < 0) { _selectedNode = node.id; _status = "已选择起点 #" + node.id + "；请点击相邻终点。"; Refresh(); return; }
            if (_selectedNode == node.id) { _selectedNode = -1; _status = "已取消起点。"; Refresh(); return; }
            int from = _selectedNode;
            bool accepted = sim.ToggleEdge(from, node.id);
            _status = sim.LastMessage;
            if (accepted) _selectedNode = -1;
            Refresh();
        }

        private void RefreshBoard(ChapterOneSim sim)
        {
            if (_boardArea.rect.width <= 0 || _boardArea.rect.height <= 0) return;
            float radius = Mathf.Min((_boardArea.rect.width - 24) / 15.8564f, (_boardArea.rect.height - 26) / 8f);
            radius = Mathf.Max(radius, 10);
            float width = 15.8564f * radius, height = 8 * radius;
            Vector2 origin = new Vector2((_boardArea.rect.width - width) * .5f + radius,
                -(_boardArea.rect.height - height) * .5f - radius);
            _nodePositions.Clear();
            bool pulse = Time.unscaledTime < _pulseUntil;
            float pulseFade = _pulseDuration > 0 ? Mathf.Clamp01((_pulseUntil - Time.unscaledTime) / _pulseDuration) : 0;
            for (int i = 0; i < _cells.Count; i++)
            {
                CellView cell = _cells[i];
                NodeState node = FindNode(sim.S, cell.q, cell.r);
                Vector2 position = origin + new Vector2(1.73205f * radius * (cell.q + cell.r * .5f), -1.5f * radius * cell.r);
                cell.rect.anchorMin = cell.rect.anchorMax = new Vector2(0, 1);
                cell.rect.pivot = new Vector2(.5f, .5f);
                cell.rect.anchoredPosition = position;
                cell.rect.sizeDelta = new Vector2(radius * 1.83f, radius * 1.83f);
                cell.label.fontSize = Mathf.Clamp(radius * .34f, 9, 14);
                if (node == null)
                {
                    cell.graphic.color = new Color32(30, 48, 66, 255);
                    cell.label.color = new Color32(120, 142, 161, 255);
                    cell.label.text = cell.q + "," + cell.r;
                }
                else
                {
                    _nodePositions[node.id] = position;
                    Color tint = node.kind == NodeKind.Input ? new Color32(41, 102, 128, 255) : node.kind == NodeKind.Hidden ? new Color32(44, 78, 99, 255) : node.kind == NodeKind.Yes ? new Color32(40, 114, 107, 255) : new Color32(136, 104, 53, 255);
                    if (pulse && _pulseNodes.Contains(node.id)) tint = Color.Lerp(tint, _correctionPulse ? Purple : Teal, .65f * pulseFade);
                    cell.graphic.color = node.id == _selectedNode ? Gold : tint;
                    cell.label.color = node.id == _selectedNode ? Background : Ink;
                    cell.label.text = node.kind == NodeKind.Input ? GameText.T("输入", "Input") : node.kind == NodeKind.Yes ? GameText.T("是", "Yes") : node.kind == NodeKind.No ? GameText.T("否", "No") : "N" + node.id;
                }
            }
            _edgeSegments.Clear();
            Vector2 pivotOffset = new Vector2(-_boardArea.rect.width * .5f, _boardArea.rect.height * .5f);
            for (int i = 0; i < sim.S.edges.Count; i++)
            {
                EdgeState edge = sim.S.edges[i];
                if (!_nodePositions.TryGetValue(edge.from, out Vector2 start) || !_nodePositions.TryGetValue(edge.to, out Vector2 end)) continue;
                Color tint = new Color32(102, 164, 184, 255);
                if (pulse && _pulseNodes.Contains(edge.from) && _pulseNodes.Contains(edge.to)) tint = Color.Lerp(tint, _correctionPulse ? Purple : Teal, pulseFade);
                _edgeSegments.Add(new BoardEdgesGraphic.Segment { from = start + pivotOffset, to = end + pivotOffset, weight = (float)(edge.weight / sim.Config.maximumWeight), tint = tint });
            }
            _edgeGraphic.SetSegments(_edgeSegments);
            _lastBoardSize = _boardArea.rect.size;
            Text("BoardModeHint", _boardMode == "connect" ? GameText.T("有向连接 / 只连接相邻六边格", "Directed edges / Adjacent hexes only") : _boardMode == "place" ? GameText.T("隐藏节点 / 自动检查显存与成本", "Hidden nodes / VRAM and cost checked") : GameText.T("移除后相关连接同时删除", "Removing a node also removes its edges"));
            foreach (string mode in BoardModes) _buttons["BoardMode_" + mode].targetGraphic.color = mode == _boardMode ? new Color32(31, 101, 111, 255) : RaisedColor;
            Text("BoardStats", GameText.T("端口路径  ", "Output path  ") + (sim.HasOutputPath ? GameText.T("已接通", "Connected") : GameText.T("未接通", "Disconnected")) + GameText.T("\n心跳  ", "\nHeartbeats  ") + Number(sim.HeartbeatsPerSecond) + GameText.T(" / 秒\n扇出  ", " / sec\nFan-out  ") + sim.FanOutCount + GameText.T("  ·  汇流  ", "  ·  Convergence  ") + sim.ConvergeCount + GameText.T("\n节点 ", "\nNodes ") + sim.S.nodes.Count + GameText.T("  /  连线 ", "  /  Edges ") + sim.S.edges.Count + GameText.T("\n显存 ", "\nVRAM ") + Number(sim.MemoryUsed) + " / " + Number(sim.MemoryCapacity) + GameText.T(" MB\n瓶颈  ", " MB\nBottleneck  ") + GameText.Source(sim.Bottleneck));
            NodeState selected = _selectedNode >= 0 ? sim.S.nodes.Find(n => n.id == _selectedNode) : null;
            Text("BoardSelection", selected == null ? GameText.T("选择任意节点作为起点。\n输入/是/否端口不能删除。", "Select a source node.\nInput/Yes/No ports cannot be removed.") : GameText.T("起点 #", "Source #") + selected.id + " (" + selected.q + "," + selected.r + GameText.T(")\n电荷 ", ")\nCharge ") + Number(selected.charge) + GameText.T(" · 疲劳 ", " · Fatigue ") + Number(selected.fatigue) + GameText.T("\n再次点击此节点可取消。", "\nClick the node again to deselect."));
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_built && _boardArea != null && _lastBoardSize != _boardArea.rect.size) _dirty = true;
        }

        private static NodeState FindNode(GameState state, int q, int r)
        {
            for (int i = 0; i < state.nodes.Count; i++) if (state.nodes[i].q == q && state.nodes[i].r == r) return state.nodes[i];
            return null;
        }

        private void BuildSkills(RectTransform page)
        {
            RectTransform semantic = Panel("SemanticSkillPanel", page, 0, 0, .49f, .74f);
            RectTransform training = Panel("TrainingSkillPanel", page, .51f, 0, 1, .74f);
            Label("SemanticTitle", semantic, "语义理解", new Vector2(20, 19), new Vector2(380, 31), 25, Teal, FontStyles.Bold);
            RegionLabel("SemanticStats", semantic, "", 0, .22f, 1, .74f, 17, Ink, 20);
            UnityEngine.UI.Button upgrade = Button("UpgradeSkill", semantic, "升级语义理解", () => Run(() => _runtime.Sim.UpgradeSkill()));
            Region(upgrade.GetComponent<RectTransform>(), .05f, .79f, .95f, .95f, 0);
            Label("TrainingTitle", training, "训练效率", new Vector2(20, 19), new Vector2(380, 31), 25, Gold, FontStyles.Bold);
            RegionLabel("TrainingStats", training, "", 0, .22f, 1, .74f, 17, Ink, 20);
            UnityEngine.UI.Button train = Button("UpgradeTraining", training, "升级训练效率", () => Run(() => _runtime.Sim.UpgradeTraining()));
            Region(train.GetComponent<RectTransform>(), .05f, .79f, .95f, .95f, 0);
            RectTransform locked = Panel("FutureSkillsLocked", page, 0, .77f, 1, 1);
            FillLabel("FutureSkillText", locked, "后续能力未开放\n长上下文、工具调用、联网知识库不属于第一章；这里不会虚构可用功能。", 15, Muted, 19, 12, 19, 12);
        }

        private void RefreshSkills(ChapterOneSim sim)
        {
            bool semanticMax = sim.S.semanticLevel >= sim.Config.maxSkillLevel;
            bool trainingMax = sim.S.trainingLevel >= sim.Config.maxTrainingLevel;
            Text("SemanticStats", GameText.T("当前等级  ", "Level  ") + sim.S.semanticLevel + " / " + sim.Config.maxSkillLevel + GameText.T("\n模拟识别率  ", "\nSimulated accuracy  ") + Percent(sim.Accuracy) + GameText.T("\n\n改善规则模型的分类表现。\n", "\n\nImproves the rule model's classification.\n") + (semanticMax ? GameText.T("已达本章上限", "Chapter limit reached") : GameText.T("本次需要 ", "Cost: ") + Number(sim.Config.skillPointCost * sim.S.semanticLevel) + GameText.T(" 学习点", " learning points")));
            Text("TrainingStats", GameText.T("当前等级  ", "Level  ") + sim.S.trainingLevel + " / " + sim.Config.maxTrainingLevel + GameText.T("\n累计训练步  ", "\nTotal training steps  ") + Number(sim.S.steps) + GameText.T("\n\n提升网络训练效率。\n", "\n\nImproves network training efficiency.\n") + (trainingMax ? GameText.T("已达本章上限", "Chapter limit reached") : GameText.T("本次需要 ", "Cost: ") + Number(sim.Config.trainingPointCost * sim.S.trainingLevel) + GameText.T(" 学习点", " learning points")));
            _buttons["UpgradeSkill"].interactable = !semanticMax;
            _buttons["UpgradeTraining"].interactable = !trainingMax;
        }

        private void BuildShop(RectTransform page)
        {
            RectTransform gpu = Panel("GpuShopPanel", page, 0, 0, .49f, .72f);
            RectTransform cases = Panel("CaseShopPanel", page, .51f, 0, 1, .72f);
            gpu.GetComponent<UnityEngine.UI.Image>().color = new Color32(41, 47, 62, 255);
            cases.GetComponent<UnityEngine.UI.Image>().color = new Color32(40, 49, 62, 255);
            Label("GpuShopTitle", gpu, "寻宝自营 / 消费级 GPU", new Vector2(20, 17), new Vector2(465, 40), 24, Orange, FontStyles.Bold);
            RegionLabel("GpuShopStats", gpu, "", 0, .23f, 1, .71f, 17, Ink, 20);
            UnityEngine.UI.Button buy = Button("BuyGpu", gpu, "购买 GPU", () => Run(() => _runtime.Sim.BuyGpu()));
            Region(buy.GetComponent<RectTransform>(), .05f, .78f, .49f, .95f, 0);
            buy.targetGraphic.color = new Color32(147, 89, 36, 255);
            UnityEngine.UI.Button sell = Button("SellGpu", gpu, "出售一张", () => Run(() => _runtime.Sim.SellGpu()));
            Region(sell.GetComponent<RectTransform>(), .52f, .78f, .95f, .95f, 0);
            Label("CaseShopTitle", cases, "整机配件 / 扩展机箱", new Vector2(20, 17), new Vector2(465, 40), 24, Orange, FontStyles.Bold);
            RegionLabel("CaseShopStats", cases, "", 0, .23f, 1, .71f, 17, Ink, 20);
            UnityEngine.UI.Button buyCase = Button("BuyCase", cases, "购买机箱", () => Run(() => _runtime.Sim.BuyCase()));
            Region(buyCase.GetComponent<RectTransform>(), .05f, .78f, .95f, .95f, 0);
            buyCase.targetGraphic.color = new Color32(147, 89, 36, 255);
            RectTransform notes = Panel("ShopNotes", page, 0, .75f, 1, 1);
            FillLabel("ShopSummary", notes, "", 14, Muted, 20, 12, 20, 12);
        }

        private void RefreshShop(ChapterOneSim sim)
        {
            Text("GpuShopStats", GameText.T("单价  ¥", "Unit price ¥") + Money(sim.Config.gpuPrice) + GameText.T("\n持有  ", "\nOwned  ") + sim.S.gpuCount + GameText.T(" 张\n槽位  ", " GPUs\nSlots  ") + sim.S.gpuCount + " / " + (sim.S.caseCount * sim.Config.gpusPerCase) + GameText.T("\n单卡显存  ", "\nVRAM per GPU  ") + Number(sim.Config.memoryPerGpu) + GameText.T(" MB\n回收价  ¥", " MB\nResale ¥") + Money(sim.Config.gpuPrice * sim.Config.gpuResaleFraction));
            Text("CaseShopStats", GameText.T("单价  ¥", "Unit price ¥") + Money(sim.Config.casePrice) + GameText.T("\n持有  ", "\nOwned  ") + sim.S.caseCount + GameText.T(" 台\n每台提供  ", " cases\nEach provides  ") + sim.Config.gpusPerCase + GameText.T(" 个 GPU 槽位\n\n机箱只扩展安装空间，\n不会直接增加显存或算力。", " GPU slots\n\nCases only expand installation space,\nnot VRAM or compute directly."));
            Text("ShopSummary", GameText.T("已用显存 ", "VRAM used ") + Number(sim.MemoryUsed) + " / " + Number(sim.MemoryCapacity) + GameText.T(" MB   ·   当前功率 ", " MB   ·   Current power ") + Number(sim.PowerWatts) + GameText.T(" W\n新手获赠 ", " W\nStarter gift: ") + sim.Config.starterGpus + GameText.T(" 张 GPU 与 ¥", " GPUs and ¥") + Money(sim.Config.starterMoney) + GameText.T(" 启动金。所有购买、出售与扩容都由本地模拟校验；失败原因显示在下方。售卡不能挤掉网络所需显存。", " in funds. All transactions are checked locally; failures appear below. Selling cannot leave insufficient VRAM for the network."));
        }

        private void BuildJobs(RectTransform page)
        {
            RectTransform main = Panel("JobPanel", page, 0, 0, .64f, 1);
            RectTransform aside = Panel("JobAside", page, .66f, 0, 1, 1);
            Label("JobTitle", main, "家庭收入 / 邮件分类订单", new Vector2(20, 19), new Vector2(590, 35), 24, Teal, FontStyles.Bold);
            RegionLabel("JobDescription", main, "先在「邮件」核对至少 3 封，再让灵光网络处理模拟订单。\n订单开关不会停止学习；收入仍受网络、识别率、电力与硬件状态约束。", 0, .18f, 1, .43f, 17, Ink, 20);
            RegionLabel("JobStats", main, "", 0, .43f, 1, .78f, 18, Ink, 20);
            UnityEngine.UI.Button toggle = Button("ToggleJob", main, "启用订单", () =>
            {
                _runtime.Sim.SetJobEnabled(!_runtime.Sim.S.jobEnabled);
                _status = _runtime.Sim.LastMessage;
                Refresh();
            });
            Region(toggle.GetComponent<RectTransform>(), .04f, .82f, .96f, .96f, 0);
            Label("JobAsideTitle", aside, "接单须知", new Vector2(18, 19), new Vector2(290, 32), 22, Gold, FontStyles.Bold);
            RegionLabel("JobAsideText", aside, "自动收入来自模拟订单，\n不会发送真实邮件。\n\n训练数据请打开「邮件」。\n硬件采购请打开「寻宝」。\n网络与考试请打开「灵光.exe」。\n\n关闭或最小化任意窗口\n不会暂停订单与用电。", 0, .18f, 1, .96f, 16, Muted, 18);
        }

        private void RefreshJobs(ChapterOneSim sim)
        {
            Text("JobStats", GameText.T("订单状态  ", "Job status  ") + (sim.S.jobEnabled ? GameText.T("已启用", "Enabled") : GameText.T("已暂停", "Paused")) + GameText.T("\n当前收入  ¥", "\nCurrent income ¥") + Money(sim.IncomePerSecond) + GameText.T(" / 秒\n累计获得  ¥", " / sec\nTotal earned ¥") + Money(sim.S.totalEarned) + GameText.T("\n网络状态  ", "\nNetwork status  ") + GameText.Source(sim.Bottleneck));
            ButtonText("ToggleJob", sim.S.jobEnabled ? GameText.T("暂停订单", "Pause jobs") : GameText.T("启用订单", "Enable jobs"));
        }

        private void BuildPower(RectTransform page)
        {
            RectTransform power = Panel("PowerPanel", page, 0, 0, .49f, 1);
            RectTransform bill = Panel("BillPanel", page, .51f, 0, 1, 1);
            Label("PowerTitle", power, "电力与散热", new Vector2(20, 18), new Vector2(390, 34), 25, Teal, FontStyles.Bold);
            RegionLabel("PowerStats", power, "", 0, .17f, 1, .58f, 18, Ink, 20);
            RegionLabel("PowerHint", power, "温度过高会降低心跳频率；功率超限会跳闸。\n先出售多余 GPU 降低负载，再恢复断路器；强行重置不能绕过保护。", 0, .6f, 1, .79f, 14, Muted, 20);
            UnityEngine.UI.Button reset = Button("ResetBreaker", power, "尝试恢复断路器", () => Run(() => _runtime.Sim.ResetBreaker()));
            Region(reset.GetComponent<RectTransform>(), .05f, .83f, .95f, .96f, 0);
            Label("BillTitle", bill, "家庭用电账单", new Vector2(20, 18), new Vector2(390, 34), 25, Gold, FontStyles.Bold);
            RegionLabel("BillStats", bill, "", 0, .17f, 1, .58f, 18, Ink, 20);
            RegionLabel("BillHint", bill, "每天自动扣费，余额不足会停机。\n欠费后仍能手工标注：正确标注直接抵扣欠费，不会重复发放现金。", 0, .6f, 1, .79f, 14, Muted, 20);
            UnityEngine.UI.Button pay = Button("PayBill", bill, "缴纳账单", () => Run(() => _runtime.Sim.PayBill()));
            Region(pay.GetComponent<RectTransform>(), .05f, .83f, .95f, .96f, 0);
        }

        private void RefreshPower(ChapterOneSim sim)
        {
            Text("PowerStats", GameText.T("实际功率  ", "Actual power  ") + Number(sim.PowerWatts) + GameText.T(" W\n需求功率  ", " W\nRequested power  ") + Number(sim.S.gpuCount * sim.Config.gpuWatts + sim.S.caseCount * sim.Config.caseWatts) + " / " + Number(sim.Config.powerLimitWatts) + GameText.T(" W\n设备温度  ", " W\nDevice temperature  ") + Number(sim.S.temperature) + GameText.T(" °C\n断路器  ", " °C\nBreaker  ") + (sim.S.breakerTripped ? GameText.T("已跳闸", "Tripped") : GameText.T("已闭合", "Closed")));
            Text("BillStats", GameText.T("待付电费  ¥", "Electricity due ¥") + Money(sim.S.billDue) + GameText.T("\n参考电价  ¥", "\nReference rate ¥") + Money(sim.Config.electricityPrice) + GameText.T(" / kWh\n账户余额  ¥", " / kWh\nBalance ¥") + Money(sim.S.money) + GameText.T("\n账单状态  ", "\nBill status  ") + (sim.S.unpaidPower ? GameText.T("欠费停机", "Suspended for arrears") : GameText.T("供电正常", "Power normal")));
        }

        private bool _nameDraftEdited;

        private void BuildPersona(RectTransform page)
        {
            RectTransform identity = Panel("IdentityPanel", page, 0, 0, .6f, 1);
            RectTransform savePanel = Panel("SavePanel", page, .62f, 0, 1, 1);
            Label("IdentityTitle", identity, "给你的 AI 一个名字", new Vector2(20, 18), new Vector2(520, 33), 25, Teal, FontStyles.Bold);
            RegionLabel("IdentityHint", identity, "名字只保存在本机，不会改变模型能力。\n第一章使用规则与种子模拟，不调用真实大模型。", 0, .15f, 1, .34f, 15, Muted, 20);
            RectTransform inputRect = Rect("AiNameInput", identity);
            Region(inputRect, .04f, .37f, .96f, .5f, 0);
            BackgroundImage(inputRect, Background);
            _nameInput = inputRect.gameObject.AddComponent<TMP_InputField>();
            RectTransform viewport = Rect("TextViewport", inputRect);
            Fill(viewport, 10, 3, 10, 3);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            TMP_Text inputText = FillLabel("NameInputText", viewport, "", 19, Ink, 0, 0, 0, 0);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.overflowMode = TextOverflowModes.Overflow;
            TMP_Text placeholder = FillLabel("NamePlaceholder", viewport, "输入名字（最多 16 个字）", 16, Muted, 0, 0, 0, 0);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            _nameInput.textViewport = viewport;
            _nameInput.textComponent = (TextMeshProUGUI)inputText;
            _nameInput.placeholder = placeholder;
            // Runtime validates Unicode text elements; allow enough UTF-16 units for sixteen names.
            _nameInput.characterLimit = 64;
            _nameInput.lineType = TMP_InputField.LineType.SingleLine;
            _nameInput.richText = false;
            _nameInput.onValueChanged.AddListener(_ => _nameDraftEdited = true);
            UnityEngine.UI.Button record = Button("RecordName", identity, "保存名字", () =>
            {
                bool accepted = _runtime.RecordName(_nameInput.text);
                if (accepted) _nameDraftEdited = false;
                _status = _runtime.SaveStatus;
                Refresh();
            });
            Region(record.GetComponent<RectTransform>(), .04f, .55f, .96f, .68f, 0);
            RegionLabel("PersonaStats", identity, "", 0, .72f, 1, .98f, 15, Ink, 20);
            Label("SavePanelTitle", savePanel, "本机存档", new Vector2(18, 18), new Vector2(290, 33), 23, Gold, FontStyles.Bold);
            RegionLabel("SaveDetails", savePanel, "", 0, .17f, 1, .71f, 14, Muted, 18);
            UnityEngine.UI.Button reset = Button("OpenResetConfirmation", savePanel, "重新开始…", () => SetResetDialogVisible(true));
            Region(reset.GetComponent<RectTransform>(), .05f, .81f, .95f, .95f, 0);
            reset.targetGraphic.color = new Color32(113, 58, 61, 255);
        }

        private void RefreshPersona(ChapterOneSim sim)
        {
            if (!_nameInput.isFocused && !_nameDraftEdited && _nameInput.text != sim.S.aiName) _nameInput.SetTextWithoutNotify(sim.S.aiName ?? "");
            Text("PersonaStats", GameText.T("当前名字  ", "Current name  ") + sim.S.aiName + GameText.T("\n本章标注  ", "\nReviewed this chapter  ") + sim.S.cardsReviewed + GameText.T(" 张   ·   纠正模型 ", "   ·   Model corrections ") + sim.S.correctedCards + GameText.T(" 次\n身份状态  ", "\nIdentity  ") + (sim.S.chapterOneComplete ? GameText.T("已通过入学考试", "Entrance exam passed") : GameText.T("初生的学习者", "A new learner")));
            Text("SaveDetails", GameText.Source(_runtime.SaveStatus ?? "") + GameText.T("\n\n保存包含网络、资源、当前卡片与考试进度。读取会以已保存状态替换当前进度。\n\n自动运行不依赖窗口是否打开。\n离线收益以运行时的有界结算为准。\n\n重新开始需要二次确认，不会影响旧版原型数据。", "\n\nSaves include network, resources, current card and exam progress. Loading replaces current progress.\n\nThe learner runs with windows closed. Offline gains use bounded settlement.\n\nRestart requires confirmation and does not affect older prototype data."));
        }

        private void BuildExam(RectTransform page)
        {
            RectTransform summary = Panel("ExamSummaryPanel", page, 0, 0, 1, .26f);
            RegionLabel("ExamSummary", summary, "", 0, 0, .76f, 1, 14, Ink, 14);
            UnityEngine.UI.Button start = Button("StartExam", summary, "开始入学考试", () =>
            {
                bool result = _runtime.Sim.StartExam();
                _status = _runtime.Sim.LastMessage;
                if (result) _feedback = "考试已开始。请按邮件内容选择真实分类；完成全部试题后统一结算。";
                Refresh();
            });
            Region(start.GetComponent<RectTransform>(), .77f, .16f, .985f, .84f, 0);
            RectTransform cards = Rect("ExamQuestionArea", page);
            Region(cards, 0, .29f, 1, 1, 0);
            BuildCards(cards, "ExamCard", true);
        }

        private void RefreshExam(ChapterOneSim sim)
        {
            string gates = GameText.T("准备条件：标注 ", "Requirements: reviewed ") + sim.S.cardsReviewed + "/" + sim.Config.examRequiredCards + GameText.T("  ·  训练步 ", "  ·  Training steps ") + Number(sim.S.steps) + "/" + sim.Config.examRequiredSteps + GameText.T("  ·  模拟识别率 ", "  ·  Simulated accuracy ") + Percent(sim.Accuracy) + "/" + Percent(sim.Config.examRequiredAccuracy);
            if (sim.S.chapterOneComplete)
            {
                Text("ExamSummary", GameText.T("第一章已完成！通过入学考试，结算奖励仅发放一次。\n上次成绩 ", "Chapter One complete! Exam reward is granted only once.\nLast score ") + sim.S.examCorrect + " / " + sim.Config.examQuestions + GameText.T("   ·   第二章尚未实装。", "   ·   Chapter Two is not implemented."));
                ButtonText("StartExam", GameText.T("第一章已完成", "Chapter One complete"));
                _buttons["StartExam"].interactable = false;
            }
            else if (sim.S.examActive)
            {
                Text("ExamSummary", GameText.T("考试进行中：", "Exam in progress: ") + sim.S.examAnswered + " / " + sim.Config.examQuestions + GameText.T(" 题   ·   当前答对 ", "   ·   Correct so far ") + sim.S.examCorrect + GameText.T(" 题\n达标线：", "\nPassing score: ") + sim.Config.examPassScore + " / " + sim.Config.examQuestions + GameText.T("。可随时保存，稍后继续。", ". Save at any time and continue later."));
                ButtonText("StartExam", GameText.T("考试进行中", "Exam in progress"));
                _buttons["StartExam"].interactable = false;
            }
            else
            {
                Text("ExamSummary", (sim.S.examAttempts > 0 ? GameText.T("上次成绩 ", "Last score ") + sim.S.examCorrect + " / " + sim.Config.examQuestions + GameText.T("，未通过可重试。\n", ". You may retry after a failed attempt.\n") : GameText.T("入学考试：", "Entrance exam: ") + sim.Config.examQuestions + GameText.T(" 封邮件，答对 ", " emails; ") + sim.Config.examPassScore + GameText.T(" 封通过；首次奖励 ¥", " correct to pass. First-pass reward ¥") + Money(sim.Config.examReward) + "。\n") + gates + "\n" + (sim.CanStartExam ? GameText.T("条件已满足，可以开始。", "All requirements met. Ready to start.") : GameText.T("还需满足条件并保持输出路径与供电正常。", "Meet the remaining requirements and keep both outputs and power working.")));
                ButtonText("StartExam", sim.S.examAttempts > 0 ? GameText.T("重新考试", "Retake exam") : GameText.T("开始入学考试", "Start entrance exam"));
                // Keep this enabled: the Core command provides the precise missing prerequisite.
                _buttons["StartExam"].interactable = true;
            }
            RefreshCards(sim, true);
        }

        private const string IntroMessage = "老周  ·  09:41\n\n机器到了？桌上的 GPU 是给你的启动礼物。启动资金是零，先去「标注台」靠自己的判断赚第一笔钱。\n\n先打开「邮件」读信，选择它到底是不是垃圾邮件。模型会给建议，但会犯错。\n\n「灵光.exe」管网络、训练与考试；「家庭」管收入和电费；买显卡去「寻宝」。有问题就回 YY 找我的留言。\n\n准备好以后，在灵光完成 20 封邮件的入学考试。答对 16 封，就迈出了第一步。";
        private string _chatMessage = IntroMessage;
        private string _chatQuestion = "从哪里开始？";

        private void BuildChat(RectTransform page)
        {
            RectTransform conversation = Panel("LaoZhouConversation", page, 0, 0, 1, .76f);
            conversation.GetComponent<UnityEngine.UI.Image>().color = new Color32(23, 46, 65, 255);
            Label("LaoZhouTitle", conversation, "老周  /  训练顾问", new Vector2(20, 13), new Vector2(550, 38), 23, Teal, FontStyles.Bold);
            Label("ConversationNotice", conversation, "本地留言，不代表联系人在线", new Vector2(620, 21), new Vector2(365, 26), 13, Muted);
            RectTransform bubble = Panel("LaoZhouMessageBubble", conversation, .025f, .19f, .975f, .975f);
            bubble.GetComponent<UnityEngine.UI.Image>().color = new Color32(31, 65, 82, 255);
            RegionLabel("LaoZhouMessage", bubble, "", 0, 0, 1, 1, 15, Ink, 18);
            RectTransform topics = Panel("ConversationReplyBar", page, 0, .79f, 1, 1);
            RegionLabel("ChatQuestion", topics, "", .015f, .01f, .47f, .42f, 13, Gold, 10);
            RegionLabel("ChatDisclosure", topics, "选择一句话 · 读取老周的预设回复", .49f, .01f, .99f, .42f, 12, Muted, 10);
            string[] topicsIds = { "intro", "board", "money", "exam" };
            string[] topicsText = { "从哪里开始？", "怎么连接网络？", "没钱交电费？", "怎么通过考试？" };
            for (int i = 0; i < topicsIds.Length; i++)
            {
                string id = topicsIds[i];
                UnityEngine.UI.Button button = Button("ChatHelp_" + id, topics, topicsText[i], () => ShowHelp(id));
                Region(button.GetComponent<RectTransform>(), .015f + .245f * i, .48f, .245f + .245f * i, .92f, 0);
                button.GetComponentInChildren<TMP_Text>().fontSize = 14;
            }
        }

        public void ShowHelp(string topic)
        {
            if (!OwnsTab("chat")) return;
            switch (topic)
            {
                case "board": _chatQuestion = "怎么连接网络？"; _chatMessage = "老周  ·  网络连线\n\n打开「灵光.exe」的神经网络。初始网络已经接好，可以直接训练。\n\n连线：点起点，再点相邻终点。箭头有方向；再连一次相同边就是删除。\n\n放置或移除模式可编辑普通节点；输入和两个输出端口受到保护。线宽表示权重，紫色是纠错反馈。\n\n更多显存请去「寻宝」买 GPU；别忘记在「家庭」检查功耗。"; break;
                case "money": _chatQuestion = "没钱交电费？"; _chatMessage = "老周  ·  现金与电费\n\n先在「邮件」核对 3 封，再打开「家庭」启用邮件分类订单，开始赚维护费。\n\n关窗口不会停机，每天仍自动扣电费；不要在「寻宝」一次花光钱包。\n\n欠费了，去「邮件」继续正确标注，报酬会先抵扣账单，不同时发现金。\n\n跳闸则先去「寻宝」出售多余 GPU，再回「家庭」恢复断路器。高温只会降速。"; break;
                case "exam": _chatQuestion = "怎么通过考试？"; _chatMessage = "老周  ·  入学考试\n\n去「邮件」积累标注，在「灵光.exe」训练网络，达到标注数、训练步和识别率门槛。\n\n考试在「灵光.exe」进行，期间邮件分拣会暂停。你要给出真实分类，不是附和模型。\n\n完成 20 封，答对 16 封就通过；失败保留训练进度，可以重试。\n\n首次通过才有奖励。第二章尚未开放。供电出问题就去「家庭」查看。"; break;
                default: _chatQuestion = "从哪里开始？"; _chatMessage = IntroMessage; break;
            }
            if (_built) { ShowTab("chat"); Refresh(); }
        }

        private void RefreshChat(ChapterOneSim sim) { Text("LaoZhouMessage", GameText.Source(_chatMessage)); Text("ChatQuestion", GameText.T("我：", "Me: ") + GameText.Source(_chatQuestion)); }

        private void BuildResetDialog()
        {
            _resetDialog = Rect("ResetConfirmationOverlay", _root);
            Fill(_resetDialog);
            BackgroundImage(_resetDialog, new Color(0, 0, 0, .83f));
            RectTransform panel = Panel("ResetConfirmationPanel", _resetDialog, .24f, .26f, .76f, .73f);
            Label("ResetTitle", panel, "重新开始第一章？", new Vector2(24, 22), new Vector2(490, 37), 27, Danger, FontStyles.Bold);
            RegionLabel("ResetDescription", panel, "这会重置本模块的资源、网络、训练记录和考试进度，并写入新的存档。\n\n不会删除旧版 LingGuang 原型或其他桌面文件。此操作不能通过本界面撤销。", 0, .25f, 1, .67f, 16, Ink, 24);
            UnityEngine.UI.Button cancel = Button("CancelReset", panel, "保留进度", () => SetResetDialogVisible(false));
            Region(cancel.GetComponent<RectTransform>(), .04f, .75f, .48f, .93f, 0);
            UnityEngine.UI.Button confirm = Button("ConfirmReset", panel, "确认重新开始", () => PersistenceAction("reset"));
            Region(confirm.GetComponent<RectTransform>(), .52f, .75f, .96f, .93f, 0);
            confirm.targetGraphic.color = new Color32(127, 57, 62, 255);
            _resetDialog.gameObject.SetActive(false);
        }

        private void SetResetDialogVisible(bool visible)
        {
            for (int i = 0; i < _modalBlockedGroups.Count; i++) _modalBlockedGroups[i].interactable = !visible;
            _resetDialog.gameObject.SetActive(visible);
            // Selection uses the existing EventSystem; never create or replace one.
            if (visible) _buttons["CancelReset"].Select();
            else if (_activeTab == "persona") _buttons["OpenResetConfirmation"].Select();
        }

        private void PersistenceAction(string action)
        {
            bool success = action == "save" ? _runtime.SaveNow() : action == "reload" ? _runtime.ReloadSave() : _runtime.ResetProgress();
            _status = _runtime.SaveStatus ?? (success ? "操作完成。" : "操作未完成。");
            if (success && action != "save")
            {
                _nameDraftEdited = false;
                _feedback = action == "reset" ? "新的第一章已开始。请根据邮件内容选择真实分类。" : "已读取存档，当前卡片与考试进度已恢复。";
                _completionAnnounced = _runtime.Sim.S.chapterOneComplete;
            }
            if (action == "reset") SetResetDialogVisible(false);
            Refresh();
        }

        private void Run(Func<bool> command)
        {
            if (_runtime == null) return;
            command();
            _status = _runtime.Sim.LastMessage ?? "操作已处理。";
            Refresh();
        }

        private RectTransform Panel(string name, RectTransform parent, float left, float top, float right, float bottom)
        {
            RectTransform rect = Rect(name, parent);
            Region(rect, left, top, right, bottom, 0);
            BackgroundImage(rect, PanelColor);
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.layer = 5;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static UnityEngine.UI.Image BackgroundImage(RectTransform rect, Color tint)
        {
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = tint;
            image.raycastTarget = true;
            return image;
        }

        private UnityEngine.UI.Button Button(string name, RectTransform parent, string caption, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(name, parent);
            UnityEngine.UI.Image image = BackgroundImage(rect, RaisedColor);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            SetButtonColors(button);
            button.onClick.AddListener(action);
            TMP_Text label = FillLabel(name + "Text", rect, caption, 16, Ink, 7, 3, 7, 3);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 16;
            _buttons.Add(name, button);
            return button;
        }

        private static void SetButtonColors(UnityEngine.UI.Button button)
        {
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1);
            colors.pressedColor = new Color(.78f, .9f, .96f, 1);
            colors.selectedColor = new Color(1.10f, 1.10f, 1.10f, 1);
            colors.disabledColor = new Color(.5f, .55f, .6f, .75f);
            colors.fadeDuration = .12f;
            colors.colorMultiplier = 1;
            button.colors = colors;
        }

        private TMP_Text Label(string name, RectTransform parent, string value, Vector2 position, Vector2 size, float fontSize, Color tint, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            // Noto CJK's line box is ~1.45 em. Ellipsis rejects the entire first line
            // when the box is shorter, even when visible glyph outlines would fit.
            // Compute once at construction; never autosize frequently-changing counters.
            float lineRatio = _font != null && _font.faceInfo.pointSize > 0
                ? _font.faceInfo.lineHeight / _font.faceInfo.pointSize : 1.5f;
            size.y = Mathf.Max(size.y, Mathf.Ceil(fontSize * lineRatio + 2));
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size;
            return ConfigureLabel(name, rect, value, fontSize, tint, style);
        }

        private TMP_Text FillLabel(string name, RectTransform parent, string value, float fontSize, Color tint, float left, float top, float right, float bottom)
        {
            RectTransform rect = Rect(name, parent); Fill(rect, left, top, right, bottom);
            return ConfigureLabel(name, rect, value, fontSize, tint, FontStyles.Normal);
        }

        private TMP_Text RegionLabel(string name, RectTransform parent, string value, float left, float top, float right, float bottom, float fontSize, Color tint, float margin)
        {
            RectTransform rect = Rect(name, parent); Region(rect, left, top, right, bottom, margin);
            // Horizontal reading margins must not eat short rows (especially the exam suggestion).
            float verticalInset = Mathf.Min(8, margin);
            rect.offsetMin = new Vector2(margin, verticalInset);
            rect.offsetMax = new Vector2(-margin, -verticalInset);
            return ConfigureLabel(name, rect, value, fontSize, tint, FontStyles.Normal);
        }

        private TMP_Text ConfigureLabel(string name, RectTransform rect, string value, float size, Color tint, FontStyles style)
        {
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            if (!string.IsNullOrEmpty(value)) _authoredLabels[label] = value;
            label.text = GameText.Source(value); label.fontSize = size; label.color = tint; label.fontStyle = style;
            label.richText = false; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.lineSpacing = 3;
            _labels[name] = label;
            return label;
        }

        private static void Fill(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Top(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Bottom(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0);
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void Region(RectTransform rect, float left, float top, float right, float bottom, float margin)
        {
            rect.anchorMin = new Vector2(left, 1 - bottom); rect.anchorMax = new Vector2(right, 1 - top);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
        }

        private void Text(string key, string value)
        {
            if (!_labels.TryGetValue(key, out TMP_Text label)) return;
            _authoredLabels.Remove(label); // A refreshed field now owns its dynamic presentation.
            if (label.text != value) label.text = value;
        }

        private void ButtonText(string key, string value) { Text(key + "Text", value); }
        private static string Number(double value) { return value.ToString(Math.Abs(value) >= 100 ? "0" : "0.0", System.Globalization.CultureInfo.InvariantCulture); }
        private static string ShortText(string value, int maximum) { return string.IsNullOrEmpty(value) || value.Length <= maximum ? value : value.Substring(0, maximum) + "…"; }
        private static string FirstLine(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            int cr = value.IndexOf('\r'), lf = value.IndexOf('\n');
            int end = cr < 0 ? lf : lf < 0 ? cr : Math.Min(cr, lf);
            return (end < 0 ? value : value.Substring(0, end)).Trim();
        }
        private static string Money(double value) { return value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture); }
        private static string Percent(double value) { return (value * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%"; }

        private sealed class CellView
        {
            public int q, r;
            public RectTransform rect;
            public HexCellGraphic graphic;
            public TMP_Text label;
        }
    }
}
