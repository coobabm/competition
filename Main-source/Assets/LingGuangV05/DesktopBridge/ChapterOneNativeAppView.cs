using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.UI;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop
{
    /// <summary>Binds builder-owned native controls. The only generated view is the neural diagram.</summary>
    public sealed class ChapterOneNativeAppView : MonoBehaviour
    {
        [SerializeField] private ChapterOneRuntime runtime;
        [SerializeField] private string profileId = "lingguang";
        [SerializeField] private NativeAppBindings bindings = new NativeAppBindings();

        private readonly Dictionary<TMP_Text, string> _authoredTexts = new Dictionary<TMP_Text, string>();
        private readonly Dictionary<ButtonManager, string> _authoredButtons = new Dictionary<ButtonManager, string>();
        private readonly Dictionary<string, TMP_Text> _texts = new Dictionary<string, TMP_Text>();
        private readonly Dictionary<string, ButtonManager> _buttons = new Dictionary<string, ButtonManager>();
        private readonly Dictionary<string, TMP_InputField> _inputs = new Dictionary<string, TMP_InputField>();
        private readonly Dictionary<string, NativeAppPage> _pages = new Dictionary<string, NativeAppPage>();
        private readonly Dictionary<ButtonManager, UnityAction> _clicks = new Dictionary<ButtonManager, UnityAction>();
        private readonly Dictionary<string, RectTransform> _controls = new Dictionary<string, RectTransform>();
        private readonly List<Cell> _cells = new List<Cell>(35);
        private readonly List<BoardEdgesGraphic.Segment> _segments = new List<BoardEdgesGraphic.Segment>(210);
        private readonly Dictionary<int, Vector2> _positions = new Dictionary<int, Vector2>(35);
        private readonly HashSet<int> _pulseNodes = new HashSet<int>();
        private NativeAppFontFallback _fonts;
        private ChapterOneSim _observed;
        private RectTransform _diagram;
        private BoardEdgesGraphic _edges;
        private Transform _productGpu, _productCase;
        private string _activeTab, _boardMode = "connect", _status = "本地学习机已连接；各软件共享同一份进度。";
        private string _examFeedback = "入学考试由本地规则判定，不调用真实大模型。";
        private bool _initialized, _dirty = true, _nameDraft, _correction;
        private int _selectedNode = -1, _displayedExamCard = -1, _lastSubmitFrame = -1;
        private float _nextRefresh, _pulseUntil, _pulseDuration, _reloadConfirmUntil;

        public string ProfileId => profileId;
        public string ActiveTab => _activeTab;
        public string[] AvailableTabs => TabsFor(profileId);
        public bool IsInitialized => _initialized;
        public NativeAppBindings Bindings => bindings;
        public ChapterOneRuntime Runtime => runtime;

        public void Configure(ChapterOneRuntime simulationRuntime, string profile, NativeAppBindings controls)
        {
            TabsFor(profile); // validate without side effects
            if (simulationRuntime == null) throw new ArgumentNullException(nameof(simulationRuntime));
            if (controls == null) throw new ArgumentNullException(nameof(controls));
            if (_initialized)
            {
                if (runtime == simulationRuntime && profileId == profile && ReferenceEquals(bindings, controls)) return;
                throw new InvalidOperationException("原生应用已初始化；请先销毁旧控制器后重新配置。");
            }
            runtime = simulationRuntime; profileId = profile; bindings = controls;
            _activeTab = TabsFor(profile)[0];
        }

        private static string[] TabsFor(string id)
        {
            switch (id)
            {
                case "lingguang": return new[] { "board", "skills", "persona", "exam" };
                case "xunbao": return new[] { "shop" };
                case "home": return new[] { "power", "jobs" };
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "未知的原生应用配置。");
            }
        }

        public void EnsureInitialized()
        {
            if (_initialized || runtime == null || bindings == null) return;
            runtime.EnsureInitialized();
            _fonts = _fonts ?? new NativeAppFontFallback();
            foreach (NativeAppText item in bindings.texts)
            {
                if (item == null || item.control == null || string.IsNullOrEmpty(item.id)) continue;
                _texts[item.id] = item.control; _controls[item.id] = item.control.rectTransform;
                if (!string.IsNullOrEmpty(item.control.text)) _authoredTexts[item.control] = item.control.text;
                item.control.richText = false; ApplyNativeFont(item.control);
            }
            foreach (NativeAppButton item in bindings.buttons)
            {
                if (item == null || item.control == null || string.IsNullOrEmpty(item.id)) continue;
                _buttons[item.id] = item.control; _controls[item.id] = item.control.GetComponent<RectTransform>();
                PrepareButtonFont(item.control);
                if (!string.IsNullOrEmpty(item.control.buttonText)) _authoredButtons[item.control] = item.control.buttonText;
            }
            foreach (NativeAppInput item in bindings.inputs)
            {
                if (item == null || item.control == null || string.IsNullOrEmpty(item.id)) continue;
                _inputs[item.id] = item.control; _controls[item.id] = item.control.GetComponent<RectTransform>();
                item.control.richText = false;
                if (item.control.textComponent != null) { item.control.textComponent.richText = false; ApplyNativeFont(item.control.textComponent); }
                if (item.control.placeholder is TMP_Text placeholder) { placeholder.richText = false; ApplyNativeFont(placeholder); }
            }
            foreach (NativeAppImage item in bindings.images)
                if (item != null && item.control != null && !string.IsNullOrEmpty(item.id)) _controls[item.id] = item.control.rectTransform;
            string[] allowed = TabsFor(profileId);
            foreach (NativeAppPage page in bindings.pages)
            {
                if (page == null || page.page == null || Array.IndexOf(allowed, page.id) < 0) continue;
                _pages[page.id] = page;
                if (page.tabButton != null)
                {
                    string id = page.id;
                    _buttons["Tab_" + id] = page.tabButton;
                    _controls["Tab_" + id] = page.tabButton.GetComponent<RectTransform>();
                    Listen(page.tabButton, () => ShowTab(id));
                    PrepareButtonFont(page.tabButton);
                    _authoredButtons[page.tabButton] = page.tabButton.buttonText;
                }
            }
            WireCommands();
            if (profileId == "lingguang" && bindings.boardArea != null) BuildDiagram();
            if (_inputs.TryGetValue("NameInput", out TMP_InputField name))
            {
                name.characterLimit = 64; name.onValueChanged.AddListener(OnNameEdited);
            }
            if (_inputs.TryGetValue("SearchInput", out TMP_InputField search))
            {
                search.characterLimit = 64; search.onValueChanged.AddListener(FilterProducts);
                if (_pages.TryGetValue("shop", out NativeAppPage shop))
                {
                    foreach (Transform child in shop.page.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "ProductGpu") _productGpu = child;
                        else if (child.name == "ProductCase") _productCase = child;
                    }
                }
            }
            runtime.Changed += MarkDirty;
            GameText.Changed += OnLanguageChanged;
            _initialized = true;
            TranslateAuthoredControls();
            BindSimulation();
            if (string.IsNullOrEmpty(_activeTab)) _activeTab = allowed[0];
            ShowTab(_activeTab);
            if (search != null) FilterProducts(search.text);
        }

        private void WireCommands()
        {
            Command("SaveNow", () => { runtime.SaveNow(); _status = runtime.SaveStatus; });
            Command("ReloadSave", ReloadWithConfirmation);
            if (profileId == "lingguang")
            {
                Command("BoardMode_connect", () => SetBoardMode("connect"));
                Command("BoardMode_place", () => SetBoardMode("place"));
                Command("BoardMode_remove", () => SetBoardMode("remove"));
                Command("UpgradeSkill", () => Run(runtime.Sim.UpgradeSkill));
                Command("UpgradeTraining", () => Run(runtime.Sim.UpgradeTraining));
                Command("RecordName", RecordName);
                Command("StartExam", () => { if (runtime.Sim.StartExam()) _examFeedback = "考试已开始，请独立判断真实分类。"; _status = runtime.Sim.LastMessage; });
                Command("ExamCardAnswerYes", () => SubmitAnswer(true));
                Command("ExamCardAnswerNo", () => SubmitAnswer(false));
            }
            else if (profileId == "xunbao")
            {
                Command("BuyGpu", () => Run(runtime.Sim.BuyGpu));
                Command("SellGpu", () => Run(runtime.Sim.SellGpu));
                Command("BuyCase", () => Run(runtime.Sim.BuyCase));
            }
            else
            {
                Command("PayBill", () => Run(runtime.Sim.PayBill));
                Command("ResetBreaker", () => Run(runtime.Sim.ResetBreaker));
                Command("ToggleJob", () => { runtime.Sim.SetJobEnabled(!runtime.Sim.S.jobEnabled); _status = runtime.Sim.LastMessage; });
            }
        }

        private void Command(string id, Action action)
        {
            if (!_buttons.TryGetValue(id, out ButtonManager button)) return;
            Listen(button, () =>
            {
                if (!button.isInteractable) return;
                action(); Refresh();
            });
        }

        private void Listen(ButtonManager button, UnityAction action)
        {
            if (_clicks.TryGetValue(button, out UnityAction existing)) button.onClick.RemoveListener(existing);
            _clicks[button] = action; button.onClick.AddListener(action);
        }

        public void ShowTab(string tab)
        {
            EnsureInitialized();
            if (!_initialized || !_pages.ContainsKey(tab)) return;
            _activeTab = tab;
            foreach (KeyValuePair<string, NativeAppPage> page in _pages) page.Value.page.SetActive(page.Key == tab);
            foreach (TMP_Text label in _pages[tab].page.GetComponentsInChildren<TMP_Text>(true)) ApplyNativeFont(label);
            Refresh();
        }

        public RectTransform FindControl(string id)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(id) && _controls.TryGetValue(id, out RectTransform control) ? control : null;
        }

        public void ApplyNativeFont(TMP_Text label)
        {
            _fonts = _fonts ?? new NativeAppFontFallback();
            _fonts.Apply(label);
        }

        private void PrepareButtonFont(ButtonManager button)
        {
            TMP_Text[] labels = { button.normalTextObj, button.highlightTextObj, button.pressedTextObj, button.disabledTextObj };
            foreach (TMP_Text label in labels) if (label != null) { label.richText = false; ApplyNativeFont(label); }
        }

        private void OnEnable() { _dirty = true; if (_initialized) OnLanguageChanged(); }
        private void TranslateAuthoredControls()
        {
            // Explicit builder-owned controls only: inputs and user names are never translated.
            foreach (KeyValuePair<TMP_Text, string> item in _authoredTexts)
                if (item.Key != null) { item.Key.text = GameText.Source(item.Value); ApplyNativeFont(item.Key); }
            foreach (KeyValuePair<ButtonManager, string> item in _authoredButtons)
                if (item.Key != null) { item.Key.SetText(GameText.Source(item.Value)); PrepareButtonFont(item.Key); }
        }
        private void OnLanguageChanged()
        {
            if (!_initialized) return;
            TranslateAuthoredControls();
            Refresh();
            if (_reloadConfirmUntil > Time.unscaledTime) ButtonText("ReloadSave", GameText.T("确认读取", "Confirm load"));
        }
        private void MarkDirty() { _dirty = true; }
        private void OnNameEdited(string ignored) { _nameDraft = true; }

        private void Update()
        {
            if (!_initialized) return;
            if (Time.unscaledTime >= _nextRefresh && (_dirty || (_activeTab == "board" && Time.unscaledTime < _pulseUntil))) Refresh();
        }

        private void BindSimulation()
        {
            if (ReferenceEquals(_observed, runtime.Sim)) return;
            if (_observed != null) { _observed.Pulsed -= OnPulse; _observed.CardResolved -= OnCardResolved; }
            _observed = runtime.Sim;
            _observed.Pulsed += OnPulse; _observed.CardResolved += OnCardResolved;
            _selectedNode = -1; _displayedExamCard = -1;
        }

        private void OnCardResolved(CardResolution resolution)
        {
            if (profileId != "lingguang" || resolution.cardId != _displayedExamCard) return;
            _examFeedback = (resolution.correct ? "判断正确。" : "本题有误。") + resolution.explanation;
            _dirty = true;
        }

        public void Refresh()
        {
            EnsureInitialized();
            if (!_initialized) return;
            BindSimulation();
            ChapterOneSim sim = runtime.Sim;
            GameState state = sim.S;
            _dirty = false; _nextRefresh = Time.unscaledTime + .25f;
            Text("HeaderMoney", "¥" + Money(state.money));
            Text("HeaderSamples", GameText.T("样本 ", "Samples ") + Whole(state.samples));
            Text("HeaderSteps", GameText.T("训练步 ", "Steps ") + Whole(state.steps));
            Text("HeaderLearning", GameText.T("学习点 ", "Learning points ") + Whole(state.learningPoints));
            Text("SaveStatus", GameText.Source(runtime.SaveStatus));
            Text("Status", GameText.Source(_status));
            if (_reloadConfirmUntil > 0 && Time.unscaledTime > _reloadConfirmUntil) { _reloadConfirmUntil = 0; ButtonText("ReloadSave", GameText.T("读取存档", "Load save")); }
            if (profileId == "xunbao") RefreshShop(sim);
            else if (profileId == "home") RefreshHome(sim);
            else RefreshWorkbench(sim);
        }

        private void RefreshWorkbench(ChapterOneSim sim)
        {
            GameState state = sim.S;
            Text("Objective", state.chapterOneComplete ? GameText.T("第一章已完成。第二章尚未开放；仍可优化网络和管理设备。", "Chapter One complete. Chapter Two is not available; you can still optimize the network and manage hardware.") : state.examActive ? GameText.T("入学考试 ", "Entrance exam ") + state.examAnswered + " / " + sim.Config.examQuestions + GameText.T("；邮件分拣暂停，请在这里继续。", "; mail sorting is paused. Continue here.") : GameText.T("在「邮件」标注，在「家庭」接单与缴费，在「寻宝」添置硬件；达标后参加入学考试。", "Label in Mail, manage jobs and bills in Home, buy hardware in Xunbao, then take the entrance exam."));
            Text("SemanticStats", GameText.T("语义等级 ", "Semantic level ") + state.semanticLevel + " / " + sim.Config.maxSkillLevel + GameText.T("\n模拟识别率 ", "\nSimulated accuracy ") + Percent(sim.Accuracy) + GameText.T("\n升级需要 ", "\nUpgrade cost: ") + Whole(sim.Config.skillPointCost * state.semanticLevel) + GameText.T(" 学习点", " learning points"));
            Text("TrainingStats", GameText.T("训练等级 ", "Training level ") + state.trainingLevel + " / " + sim.Config.maxTrainingLevel + GameText.T("\n当前心跳 ", "\nHeartbeats ") + Decimal(sim.HeartbeatsPerSecond) + GameText.T(" / 秒\n升级需要 ", " / sec\nUpgrade cost: ") + Whole(sim.Config.trainingPointCost * state.trainingLevel) + GameText.T(" 学习点", " learning points"));
            Interactable("UpgradeSkill", state.semanticLevel < sim.Config.maxSkillLevel);
            Interactable("UpgradeTraining", state.trainingLevel < sim.Config.maxTrainingLevel);
            Text("PersonaStats", GameText.T("已保存名称：", "Saved name: ") + state.aiName + GameText.T("\n标注 ", "\nReviewed ") + state.cardsReviewed + GameText.T(" 封 · 纠正模型 ", " emails · Model corrections ") + state.correctedCards + GameText.T(" 次\n仅本地规则模拟，不是真实大模型。", "\nLocal rule simulation, not a real language model."));
            if (_inputs.TryGetValue("NameInput", out TMP_InputField name) && !name.isFocused && !_nameDraft && name.text != state.aiName) name.SetTextWithoutNotify(state.aiName);
            if (_activeTab == "board") RefreshDiagram(sim);
            RefreshExam(sim);
        }

        private void RefreshExam(ChapterOneSim sim)
        {
            GameState state = sim.S;
            Text("ExamSummary", state.chapterOneComplete ? GameText.T("第一章完成 · ", "Chapter One complete · ") + state.examCorrect + " / " + sim.Config.examQuestions + GameText.T("\n首次奖励只发放一次；第二章尚未开放。", "\nFirst-pass reward is granted once. Chapter Two is not available.") : state.examActive ? GameText.T("入学考试 ", "Entrance exam ") + state.examAnswered + " / " + sim.Config.examQuestions + GameText.T(" · 当前答对 ", " · Correct so far ") + state.examCorrect + GameText.T("\n通过线 ", "\nPassing score: ") + sim.Config.examPassScore + GameText.T(" 题，可以保存后继续。", ". Save and continue later if needed.") : GameText.T("准备：标注 ", "Requirements: reviewed ") + state.cardsReviewed + "/" + sim.Config.examRequiredCards + GameText.T(" · 训练步 ", " · Training steps ") + Whole(state.steps) + "/" + Whole(sim.Config.examRequiredSteps) + GameText.T("\n模拟识别率 ", "\nSimulated accuracy ") + Percent(sim.Accuracy) + "/" + Percent(sim.Config.examRequiredAccuracy) + GameText.T("；两个输出与供电须正常。", "; both outputs and power must work.") + (state.examAttempts > 0 ? GameText.T(" 上次答对 ", " Previous correct answers: ") + state.examCorrect + GameText.T(" 题。", ".") : ""));
            CardData card = state.examActive ? sim.CurrentCard : null;
            _displayedExamCard = card == null ? -1 : card.id;
            Text("ExamPrompt", card == null ? state.chapterOneComplete ? GameText.T("考试已通过。你可以返回网络继续优化。", "Exam passed. You can return to the network to keep optimizing.") : GameText.T("开始考试后，在此阅读邮件并判断真实分类。", "Start the exam, then read each email here and judge its actual category.") : GameText.Source(card.prompt));
            Text("ExamSuggestion", card == null ? "" : GameText.T("模拟模型建议：", "Simulated suggestion: ") + (card.predictedYes ? GameText.T("垃圾邮件", "Spam") : GameText.T("正常邮件", "Not spam")) + GameText.T(" · 模拟置信度 ", " · Simulated confidence ") + Percent(card.confidence));
            Text("ExamFeedback", GameText.Source(_examFeedback));
            Interactable("StartExam", !state.examActive && !state.chapterOneComplete);
            Interactable("ExamCardAnswerYes", card != null);
            Interactable("ExamCardAnswerNo", card != null);
        }

        public void SubmitAnswer(bool answer)
        {
            if (!_initialized || profileId != "lingguang" || _activeTab != "exam" || !runtime.Sim.S.examActive || _lastSubmitFrame == Time.frameCount) return;
            CardData card = runtime.Sim.CurrentCard;
            if (card == null || card.id != _displayedExamCard) { Refresh(); return; }
            _lastSubmitFrame = Time.frameCount;
            runtime.Sim.SubmitCard(card.id, answer); _status = runtime.Sim.LastMessage; Refresh();
        }

        private void RefreshShop(ChapterOneSim sim)
        {
            GameState state = sim.S;
            Text("Objective", GameText.T("本地硬件集市 · 所有金额属于游戏，不会真实付款。", "Local hardware market · All payments are simulated in-game."));
            Text("GpuPrice", "¥" + Money(sim.Config.gpuPrice));
            Text("CasePrice", "¥" + Money(sim.Config.casePrice));
            Text("GpuStats", GameText.T("已安装 ", "Installed ") + state.gpuCount + GameText.T(" 张 / ", " / ") + state.caseCount * sim.Config.gpusPerCase + GameText.T(" 槽位\n每张显存 ", " slots\nVRAM per GPU ") + Whole(sim.Config.memoryPerGpu) + GameText.T(" MB\n回收价 ¥", " MB\nResale ¥") + Money(sim.Config.gpuPrice * sim.Config.gpuResaleFraction));
            Text("CaseStats", GameText.T("已拥有 ", "Owned ") + state.caseCount + GameText.T(" 台\n每台 ", "\nEach case provides ") + sim.Config.gpusPerCase + GameText.T(" 个 GPU 插槽\n机箱扩展安装空间，也增加基础功耗。", " GPU slots\nCases expand installation space but add base power consumption."));
            Text("ShopSummary", GameText.T("显存 ", "VRAM ") + Whole(sim.MemoryUsed) + " / " + Whole(sim.MemoryCapacity) + GameText.T(" MB · 当前功率 ", " MB · Current power ") + Whole(sim.PowerWatts) + GameText.T(" W\n商品采用模拟供货；可安装数量受余额、机箱插槽与显存约束。电费请查看「家庭」。", " W\nSimulated stock; purchases depend on funds, case slots and VRAM. Check Home for electricity bills."));
        }

        private void RefreshHome(ChapterOneSim sim)
        {
            GameState state = sim.S;
            Text("Objective", GameText.T("家庭账务 · 展示当前真实模拟账单，不生成虚构历史账单。", "Home accounts · Current simulated bills only; no invented billing history."));
            Text("BillAmount", "¥" + Money(state.billDue));
            Text("BillDetails", GameText.T("第 ", "Day ") + state.day + GameText.T(" 天 · 本日累计 ", " · Usage today ") + state.energyKwh.ToString("0.000", CultureInfo.InvariantCulture) + GameText.T(" kWh\n电价 ¥", "kWh\nRate ¥") + Money(sim.Config.electricityPrice) + GameText.T(" / kWh\n本日未结算电费 ¥", " / kWh\nUnbilled today ¥") + Money(state.energyKwh * sim.Config.electricityPrice) + GameText.T("\n每日自动结算；待付金额只表示已到期欠费。", "\nAutomatically billed daily; amount due includes overdue charges only."));
            Text("BillStatus", state.unpaidPower ? GameText.T("欠费停机 · 可缴费或在「邮件」正确标注抵扣欠费。", "Power suspended for arrears · Pay the bill or label correctly in Mail to repay it.") : GameText.T("当前无到期欠费。下一账期仍会按实际用电扣款。", "No overdue balance. The next bill still charges for actual simulated usage."));
            Text("PowerStats", "GPU " + state.gpuCount + GameText.T(" 张 × ", " × ") + Whole(sim.Config.gpuWatts) + GameText.T(" W\n机箱 ", " W\nCases ") + state.caseCount + GameText.T(" 台 × ", " × ") + Whole(sim.Config.caseWatts) + GameText.T(" W\n实际功率 ", " W\nActual power ") + Whole(sim.PowerWatts) + GameText.T(" W · 上限 ", " W · Limit ") + Whole(sim.Config.powerLimitWatts) + GameText.T(" W\n温度 ", " W\nTemperature ") + Decimal(state.temperature) + " °C · " + (state.breakerTripped ? GameText.T("断路器已跳闸", "Breaker tripped") : GameText.T("断路器正常", "Breaker normal")));
            Text("JobStats", GameText.T("当前收入 ¥", "Current income ¥") + Money(sim.IncomePerSecond) + GameText.T(" / 秒\n累计订单收入 ¥", " / sec\nTotal job income ¥") + Money(state.totalEarned) + GameText.T("\n已核对邮件 ", "\nEmails reviewed ") + state.cardsReviewed + GameText.T(" / 3 封启动条件\n网络瓶颈：", " / 3 required to start\nBottleneck: ") + GameText.Source(sim.Bottleneck));
            Text("JobStatus", state.jobEnabled ? GameText.T("订单已启用；能否产生收入取决于训练与供电状态。", "Jobs enabled. Earnings depend on training and power status.") : GameText.T("订单已暂停；训练和用电不会因此暂停。", "Jobs paused. Training and power usage continue."));
            ButtonText("ToggleJob", state.jobEnabled ? GameText.T("暂停订单", "Pause jobs") : GameText.T("启用订单", "Enable jobs"));
        }

        private void RecordName()
        {
            if (!_inputs.TryGetValue("NameInput", out TMP_InputField field)) return;
            if (runtime.RecordName(field.text)) _nameDraft = false;
            _status = runtime.SaveStatus;
        }

        private void ReloadWithConfirmation()
        {
            if (_reloadConfirmUntil <= Time.unscaledTime)
            {
                _reloadConfirmUntil = Time.unscaledTime + 5;
                _status = "读取会替换未保存进度。5 秒内再次点击「确认读取」继续。";
                ButtonText("ReloadSave", "确认读取"); return;
            }
            _reloadConfirmUntil = 0; ButtonText("ReloadSave", "读取存档");
            if (runtime.ReloadSave()) _nameDraft = false;
            _status = runtime.SaveStatus;
        }

        private void FilterProducts(string query)
        {
            string key = (query ?? "").Trim().ToLowerInvariant();
            bool all = key.Length == 0 || key.Contains("电脑") || key.Contains("硬件") || key.Contains("全部") || key.Contains("computer") || key.Contains("hardware") || key == "all";
            bool gpu = all || key.Contains("gpu") || key.Contains("显卡") || key.Contains("显存");
            bool tower = all || key.Contains("机箱") || key.Contains("机壳") || key.Contains("tower") || key.Contains("case");
            if (_productGpu != null) _productGpu.gameObject.SetActive(gpu);
            if (_productCase != null) _productCase.gameObject.SetActive(tower);
            _status = gpu || tower ? "商品筛选已更新。" : "没有匹配商品。试试「显卡」「机箱」或清空搜索。";
            _dirty = true;
        }

        private void Run(Func<bool> command) { command(); _status = runtime.Sim.LastMessage; }
        private void Text(string id, string value) { if (_texts.TryGetValue(id, out TMP_Text label) && label.text != value) { label.text = value ?? ""; ApplyNativeFont(label); } }
        private void Interactable(string id, bool value) { if (_buttons.TryGetValue(id, out ButtonManager button) && button.isInteractable != value) button.Interactable(value); }
        private void ButtonText(string id, string value) { value = GameText.Source(value); if (_buttons.TryGetValue(id, out ButtonManager button) && button.buttonText != value) { button.SetText(value); PrepareButtonFont(button); } }
        private static string Money(double value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static string Whole(double value) { return value.ToString("0", CultureInfo.InvariantCulture); }
        private static string Decimal(double value) { return value.ToString("0.0", CultureInfo.InvariantCulture); }
        private static string Percent(double value) { return (value * 100).ToString("0", CultureInfo.InvariantCulture) + "%"; }

        private void OnDestroy()
        {
            GameText.Changed -= OnLanguageChanged;
            if (runtime != null) runtime.Changed -= MarkDirty;
            if (_observed != null) { _observed.Pulsed -= OnPulse; _observed.CardResolved -= OnCardResolved; }
            foreach (KeyValuePair<ButtonManager, UnityAction> click in _clicks) if (click.Key != null) click.Key.onClick.RemoveListener(click.Value);
            if (_inputs.TryGetValue("NameInput", out TMP_InputField name) && name != null) name.onValueChanged.RemoveListener(OnNameEdited);
            if (_inputs.TryGetValue("SearchInput", out TMP_InputField search) && search != null) search.onValueChanged.RemoveListener(FilterProducts);
            _fonts?.Dispose();
            if (_diagram != null) { if (Application.isPlaying) Destroy(_diagram.gameObject); else DestroyImmediate(_diagram.gameObject); }
        }

        // Neural diagram only: O(V² + E) refresh with V <= 35, one reusable weighted-edge mesh.
        private void BuildDiagram()
        {
            _diagram = NewRect("NativeNeuralDiagram", bindings.boardArea);
            Stretch(_diagram);
            RectTransform edgeRect = NewRect("WeightedEdges", _diagram);
            Stretch(edgeRect);
            edgeRect.gameObject.AddComponent<CanvasRenderer>();
            _edges = edgeRect.gameObject.AddComponent<BoardEdgesGraphic>();
            _edges.raycastTarget = false;
            TMP_FontAsset diagramFont = bindings.font;
            if (diagramFont == null)
                foreach (TMP_Text text in _texts.Values) if (text.font != null) { diagramFont = text.font; break; }
            for (int r = 0; r < 5; r++) for (int q = 0; q < 7; q++)
            {
                int col = q, row = r;
                string id = "BoardCell_" + q + "_" + r;
                RectTransform rect = NewRect(id, _diagram);
                rect.gameObject.AddComponent<CanvasRenderer>();
                HexCellGraphic graphic = rect.gameObject.AddComponent<HexCellGraphic>();
                UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = graphic;
                button.onClick.AddListener(() => ClickBoardCell(col, row));
                RectTransform textRect = NewRect("NodeLabel", rect);
                Stretch(textRect); textRect.gameObject.AddComponent<CanvasRenderer>();
                TextMeshProUGUI label = textRect.gameObject.AddComponent<TextMeshProUGUI>();
                if (diagramFont != null) label.font = diagramFont;
                label.fontSize = 13; label.alignment = TextAlignmentOptions.Center;
                label.richText = false; label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                ApplyNativeFont(label);
                _cells.Add(new Cell { q = q, r = r, rect = rect, graphic = graphic, text = label });
                _controls[id] = rect;
            }
            _edges.transform.SetAsLastSibling();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform)); obj.layer = 5;
            RectTransform rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.localScale = Vector3.one; return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private void OnPulse(TrainingPulse pulse)
        {
            if (profileId != "lingguang") return;
            if (!pulse.correction && _correction && Time.unscaledTime < _pulseUntil) return;
            _pulseNodes.Clear();
            if (pulse.nodeIds != null) foreach (int id in pulse.nodeIds) _pulseNodes.Add(id);
            _correction = pulse.correction;
            _pulseDuration = pulse.correction ? 1.8f : 1.05f;
            _pulseUntil = Time.unscaledTime + _pulseDuration; _dirty = true;
        }

        public void SetBoardMode(string mode)
        {
            if (profileId != "lingguang" || (mode != "connect" && mode != "place" && mode != "remove")) return;
            _boardMode = mode; _selectedNode = -1;
            _status = mode == "connect" ? "连接：先点起点，再点相邻终点；重复连接可删边。" : mode == "place" ? "放置：点中间五列的空格，系统检查显存。" : "移除：点普通节点；输入和输出端口受到保护。";
            Refresh();
        }

        public void ClickBoardCell(int q, int r)
        {
            if (!_initialized || profileId != "lingguang" || _activeTab != "board" || q < 0 || q > 6 || r < 0 || r > 4) return;
            ChapterOneSim sim = runtime.Sim;
            NodeState node = NodeAt(sim.S, q, r);
            if (_boardMode == "place") { Run(() => sim.AddNode(q, r)); Refresh(); return; }
            if (_boardMode == "remove")
            {
                if (node == null) _status = "此处没有节点。";
                else Run(() => sim.RemoveNode(node.id));
                Refresh(); return;
            }
            if (node == null) { _status = "连接需要已有节点。请先放置节点。"; Refresh(); return; }
            if (_selectedNode < 0) { _selectedNode = node.id; _status = "已选起点 #" + node.id + "；请选择相邻终点。"; Refresh(); return; }
            if (_selectedNode == node.id) { _selectedNode = -1; _status = "已取消起点。"; Refresh(); return; }
            if (sim.ToggleEdge(_selectedNode, node.id)) _selectedNode = -1;
            _status = sim.LastMessage; Refresh();
        }

        private static NodeState NodeAt(GameState state, int q, int r)
        {
            for (int i = 0; i < state.nodes.Count; i++) if (state.nodes[i].q == q && state.nodes[i].r == r) return state.nodes[i];
            return null;
        }

        private void RefreshDiagram(ChapterOneSim sim)
        {
            if (_diagram == null || _diagram.rect.width <= 0 || _diagram.rect.height <= 0) return;
            float radius = Mathf.Max(10, Mathf.Min((_diagram.rect.width - 24) / 15.8564f, (_diagram.rect.height - 26) / 8f));
            Vector2 origin = new Vector2((_diagram.rect.width - 15.8564f * radius) * .5f + radius,
                -(_diagram.rect.height - 8 * radius) * .5f - radius);
            _positions.Clear();
            float fade = _pulseDuration > 0 ? Mathf.Clamp01((_pulseUntil - Time.unscaledTime) / _pulseDuration) : 0;
            Color pulseTint = _correction ? new Color32(185, 147, 240, 255) : new Color32(83, 211, 184, 255);
            foreach (Cell cell in _cells)
            {
                NodeState node = NodeAt(sim.S, cell.q, cell.r);
                Vector2 position = origin + new Vector2(1.73205f * radius * (cell.q + cell.r * .5f), -1.5f * radius * cell.r);
                cell.rect.anchorMin = cell.rect.anchorMax = new Vector2(0, 1);
                cell.rect.pivot = new Vector2(.5f, .5f); cell.rect.anchoredPosition = position;
                cell.rect.sizeDelta = new Vector2(radius * 1.83f, radius * 1.83f);
                cell.text.fontSize = Mathf.Clamp(radius * .34f, 10, 14);
                if (node == null)
                {
                    cell.graphic.color = new Color32(32, 53, 74, 255);
                    cell.text.color = new Color32(152, 178, 196, 255);
                    cell.text.text = cell.q + "," + cell.r;
                }
                else
                {
                    _positions[node.id] = position;
                    Color tint = node.kind == NodeKind.Input ? new Color32(41, 102, 128, 255) : node.kind == NodeKind.Yes ? new Color32(40, 114, 107, 255) : node.kind == NodeKind.No ? new Color32(136, 104, 53, 255) : new Color32(44, 78, 99, 255);
                    if (_pulseNodes.Contains(node.id)) tint = Color.Lerp(tint, pulseTint, .65f * fade);
                    cell.graphic.color = node.id == _selectedNode ? new Color32(245, 203, 112, 255) : tint;
                    cell.text.color = node.id == _selectedNode ? new Color32(12, 24, 38, 255) : new Color32(234, 242, 250, 255);
                    cell.text.text = node.kind == NodeKind.Input ? GameText.T("输入", "Input") : node.kind == NodeKind.Yes ? GameText.T("是", "Yes") : node.kind == NodeKind.No ? GameText.T("否", "No") : "N" + node.id;
                }
            }
            _segments.Clear();
            Vector2 pivot = new Vector2(-_diagram.rect.width * .5f, _diagram.rect.height * .5f);
            foreach (EdgeState edge in sim.S.edges)
            {
                if (!_positions.TryGetValue(edge.from, out Vector2 from) || !_positions.TryGetValue(edge.to, out Vector2 to)) continue;
                Color tint = new Color32(102, 164, 184, 255);
                if (_pulseNodes.Contains(edge.from) && _pulseNodes.Contains(edge.to)) tint = Color.Lerp(tint, pulseTint, fade);
                _segments.Add(new BoardEdgesGraphic.Segment { from = from + pivot, to = to + pivot, weight = (float)(edge.weight / sim.Config.maximumWeight), tint = tint });
            }
            _edges.SetSegments(_segments);
            Text("BoardStats", GameText.T("节点 ", "Nodes ") + sim.S.nodes.Count + GameText.T(" · 连接 ", " · Edges ") + sim.S.edges.Count + GameText.T("\n心跳 ", "\nHeartbeats ") + Decimal(sim.HeartbeatsPerSecond) + GameText.T(" / 秒\n扇出 ", " / sec\nFan-out ") + sim.FanOutCount + GameText.T(" · 汇流 ", " · Convergence ") + sim.ConvergeCount + GameText.T("\n显存 ", "\nVRAM ") + Whole(sim.MemoryUsed) + "/" + Whole(sim.MemoryCapacity) + GameText.T(" MB\n瓶颈：", " MB\nBottleneck: ") + GameText.Source(sim.Bottleneck));
            NodeState selected = _selectedNode < 0 ? null : sim.S.nodes.Find(node => node.id == _selectedNode);
            Text("BoardSelection", GameText.T("当前模式：", "Mode: ") + (_boardMode == "connect" ? GameText.T("连接", "Connect") : _boardMode == "place" ? GameText.T("放置", "Place") : GameText.T("移除", "Remove")) + "\n" + (selected == null ? GameText.T("选择节点开始编辑。\n线宽表示权重，紫色表示纠错。", "Select a node to edit.\nLine width shows weight; purple marks corrections.") : GameText.T("起点 #", "Source #") + selected.id + GameText.T("\n电荷 ", "\nCharge ") + Decimal(selected.charge) + GameText.T(" · 疲劳 ", " · Fatigue ") + Decimal(selected.fatigue)));
        }

        private sealed class Cell { public int q, r; public RectTransform rect; public HexCellGraphic graphic; public TMP_Text text; }
    }
}
