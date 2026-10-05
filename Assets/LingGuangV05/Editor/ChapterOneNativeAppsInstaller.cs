#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.UI;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.Editor
{
    /// <summary>Three app-specific layouts assembled from actual native DreamOS widgets.</summary>
    public static class ChapterOneNativeAppsInstaller
    {
        private const string ButtonPath = "Assets/DreamOS - Complete OS UI/Prefabs/UI Elements/Button/Default Button.prefab";
        private const string InputPath = "Assets/DreamOS - Complete OS UI/Prefabs/UI Elements/Input Field/Input Field.prefab";
        private const string RowPath = "Assets/DreamOS - Complete OS UI/Prefabs/Settings/Settings Element - Button.prefab";
        private const string AeroThemePath = "Assets/HongmengOS/Aero2010/Theme/Aero 2010 UI.asset";
        private const string ThemeDirectory = "Assets/LingGuangV05/Content/Native/Themes";
        private const string ArtDirectory = "Assets/LingGuangV05/Art/Generated/";
        private static readonly string[] Profiles = { "lingguang", "xunbao", "home" };

        [MenuItem("Tools/LingGuang/Install Native Game Apps")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("原生应用安装器只能在完成编译后的编辑模式运行。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("请先保存当前场景；不会覆盖未保存修改。");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ChapterOneInstaller.ScenePath)
                scene = EditorSceneManager.OpenScene(ChapterOneInstaller.ScenePath, OpenSceneMode.Single);
            var router = FindInScene<ChapterOneDesktopRouter>(scene);
            var lcd = FindInScene<AeroLcdSourceBridge>(scene);
            if (router == null || lcd == null) throw new InvalidOperationException("Main 缺少五应用路由或 LCD 接入。");
            var buttonTemplate = Required<GameObject>(ButtonPath);
            var inputTemplate = Required<GameObject>(InputPath);
            var rowTemplate = Required<GameObject>(RowPath);
            var baseTheme = Required<UIManager>(AeroThemePath);
            var art = new Dictionary<string, Sprite>
            {
                { "NeuralArt", LoadArt("neural-workbench.png") },
                { "GpuArt", LoadArt("gpu-9800.png") },
                { "CaseArt", LoadArt("pc-tower.png") },
                { "MeterArt", LoadArt("household-meter.png") }
            };
            foreach (string profile in Profiles)
            {
                var binding = router.Get(profile);
                if (binding == null || binding.Window == null || binding.Runtime == null || binding.IsNativeChat)
                    throw new InvalidOperationException("原生应用接入缺少窗口/运行时：" + profile);
            }
            foreach (string profile in Profiles)
            {
                var bridge = router.Get(profile);
                var content = bridge.Window.transform.Find("Container/Content") as RectTransform;
                if (content == null) throw new InvalidOperationException("缺少原生 Content：" + profile);
                var theme = CreateTheme(baseTheme, profile);
                // Only the three approved content regions are rebuilt. Window chrome,
                // router identity, taskbar, Mail and native YY are not modified.
                foreach (var old in content.GetComponents<ChapterOneApp>()) Object.DestroyImmediate(old);
                foreach (var old in content.GetComponents<ChapterOneNativeAppView>()) Object.DestroyImmediate(old);
                for (int i = content.childCount - 1; i >= 0; i--) Object.DestroyImmediate(content.GetChild(i).gameObject);
                var background = content.GetComponent<Image>();
                if (background != null) background.color = theme.windowBGColorDark;
                var builder = new LayoutBuilder(content, profile, theme, buttonTemplate, inputTemplate, rowTemplate, art);
                NativeAppBindings controls = builder.Build();
                var view = content.gameObject.AddComponent<ChapterOneNativeAppView>();
                view.Configure(bridge.Runtime, profile, controls);
                var labels = new List<TMP_Text>();
                labels.AddRange(bridge.DesktopShortcut.GetComponentsInChildren<TMP_Text>(true));
                labels.AddRange(bridge.TaskbarShortcut.GetComponentsInChildren<TMP_Text>(true));
                var start = lcd.sourceCanvas.GetComponentInChildren<AeroStartMenu>(true);
                if (start != null && start.programs != null)
                    foreach (var entry in start.programs)
                        if (entry.window == bridge.Window && entry.row != null)
                            labels.AddRange(entry.row.GetComponentsInChildren<TMP_Text>(true));
                bridge.ConfigureNativeApp(bridge.Runtime, view, bridge.Window, content, bridge.DesktopShortcut,
                    bridge.TaskbarShortcut, bridge.CloseButton, bridge.MinimizeButton, bridge.MaximizeButton,
                    profile, labels.ToArray());
                foreach (var transform in content.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 5;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Validate(bridge);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ChapterOneInstaller.ScenePath))
                throw new InvalidOperationException("三个原生应用已创建，但 Main 保存失败。");
            Debug.Log("[LingGuang] 原生应用已安装：灵光计算工作站 / 寻宝电脑城 / 家庭账单。邮件与原生 YY 保持不变。", router);
        }

        private sealed class LayoutBuilder
        {
            private readonly RectTransform root;
            private readonly string profile;
            private readonly UIManager theme;
            private readonly GameObject buttonTemplate, inputTemplate;
            private readonly GameObject panelTemplate, titleTemplate, detailTemplate;
            private readonly Dictionary<string, Sprite> art;
            private readonly Dictionary<Transform, Vector2> sizes = new Dictionary<Transform, Vector2>();
            private readonly List<NativeAppText> texts = new List<NativeAppText>();
            private readonly List<NativeAppButton> buttons = new List<NativeAppButton>();
            private readonly List<NativeAppInput> inputs = new List<NativeAppInput>();
            private readonly List<NativeAppImage> images = new List<NativeAppImage>();
            private readonly List<NativeAppPage> pages = new List<NativeAppPage>();
            private RectTransform board;
            private Color Ink => theme.primaryColorDark;
            private Color Subtle => theme.secondaryColorDark;
            private Color Accent => theme.highlightedColorCustom;

            public LayoutBuilder(RectTransform content, string id, UIManager appTheme,
                GameObject button, GameObject input, GameObject row, Dictionary<string, Sprite> artwork)
            {
                profile = id; theme = appTheme; buttonTemplate = button; inputTemplate = input; art = artwork;
                panelTemplate = button.transform.Find("Normal/Background").gameObject;
                titleTemplate = row.transform.Find("Title").gameObject;
                detailTemplate = row.transform.Find("Description").gameObject;
                sizes[content] = new Vector2(1236, 693);
                root = Empty(content, "NativeApp_" + profile, 0, 0, 1236, 693);
            }

            public NativeAppBindings Build()
            {
                if (profile == "lingguang") BuildLingGuang();
                else if (profile == "xunbao") BuildShop();
                else BuildHome();
                Text(root, "Status", "系统就绪", 18, 654, 945, 25, 16, Subtle);
                Text(root, "SaveStatus", "本地存档", 985, 654, 230, 25, 14, Subtle, TextAlignmentOptions.MidlineRight);
                return new NativeAppBindings
                {
                    texts = texts.ToArray(), buttons = buttons.ToArray(), inputs = inputs.ToArray(),
                    images = images.ToArray(), pages = pages.ToArray(), boardArea = board,
                    font = theme.systemFontRegular
                };
            }

            private void BuildLingGuang()
            {
                var hero = Panel(root, "NativeNeuralHeader", 18, 16, 1200, 170, theme.backgroundColorDark);
                Art(hero, "NeuralArt", 840, 0, 340, 170);
                Text(hero, "Brand", "灵光  /  神经计算工作站", 22, 17, 770, 40, 30, Ink);
                Text(hero, null, "LINGGUANG LAB   ·   第一章 / 本地规则模拟", 24, 58, 770, 23, 15, Accent);
                Text(hero, "Objective", "从一条有效的信号路径开始。", 24, 84, 782, 34, 17, Subtle);
                Metrics(hero, 24, 126, 790, 28);
                var tabBoard = Button(root, "Tab_board", "神经网络", 18, 198, 172, 40);
                var tabSkills = Button(root, "Tab_skills", "技能训练", 198, 198, 172, 40);
                var tabPersona = Button(root, "Tab_persona", "我的 AI", 378, 198, 172, 40);
                var tabExam = Button(root, "Tab_exam", "入学考试", 558, 198, 172, 40);
                SaveButtons(root, 1000, 198);
                var boardPage = Page("board", tabBoard);
                var workbench = Panel(boardPage, "NativeNeuralWorkbench", 18, 250, 886, 388, theme.backgroundColorDark);
                Button(workbench, "BoardMode_connect", "连接 / 断开", 16, 12, 160, 36);
                Button(workbench, "BoardMode_place", "放置神经元", 185, 12, 160, 36);
                Button(workbench, "BoardMode_remove", "移除神经元", 354, 12, 160, 36);
                board = Empty(workbench, "NativeBoardArea", 10, 54, 866, 322);
                var stats = Panel(boardPage, "NativeSignalInspector", 918, 250, 300, 388, theme.backgroundColorDark);
                Text(stats, null, "信号监视器", 18, 15, 264, 28, 20, Ink);
                Text(stats, "BoardStats", "等待实时网络数据", 18, 54, 264, 155, 17, Ink);
                Text(stats, "BoardSelection", "选择神经元查看连接", 18, 224, 264, 132, 16, Subtle);
                var skillPage = Page("skills", tabSkills);
                var semantic = Panel(skillPage, "NativeSemanticTraining", 18, 250, 590, 388, theme.backgroundColorDark);
                Text(semantic, null, "01 / 语义理解", 24, 24, 530, 35, 25, Ink);
                Text(semantic, "SemanticStats", "语义训练数据", 24, 78, 530, 210, 20, Subtle);
                Button(semantic, "UpgradeSkill", "升级语义理解", 24, 315, 300, 46);
                var training = Panel(skillPage, "NativeTrainingEfficiency", 624, 250, 594, 388, theme.backgroundColorDark);
                Text(training, null, "02 / 训练效率", 24, 24, 530, 35, 25, Ink);
                Text(training, "TrainingStats", "训练效率数据", 24, 78, 530, 210, 20, Subtle);
                Button(training, "UpgradeTraining", "升级训练效率", 24, 315, 300, 46);
                var personaPage = Page("persona", tabPersona);
                var identity = Panel(personaPage, "NativeIdentity", 18, 250, 650, 388, theme.backgroundColorDark);
                Text(identity, null, "我的第一台学习机", 24, 24, 590, 35, 26, Ink);
                Text(identity, null, "为它取个名字。这里的置信度与回答均来自本地规则模拟。", 24, 77, 590, 68, 18, Subtle);
                Input(identity, "NameInput", "输入 AI 名字", 24, 171, 400, 48);
                Button(identity, "RecordName", "保存名字", 442, 171, 180, 48);
                Text(identity, "PersonaStats", "学习机资料", 24, 247, 590, 114, 18, Ink);
                var identityArt = Panel(personaPage, "NativeIdentityArtwork", 686, 250, 532, 388, theme.backgroundColorDark);
                Art(identityArt, "PersonaArtwork", 20, 28, 492, 246, "NeuralArt");
                Text(identityArt, null, "一点点学习，逐步建立自己的网络。", 24, 302, 484, 50, 18, Subtle);
                var examPage = Page("exam", tabExam);
                var admission = Panel(examPage, "NativeAdmissionExam", 18, 250, 1200, 388, theme.backgroundColorDark);
                Text(admission, "ExamSummary", "入学考试", 24, 20, 348, 176, 20, Ink);
                Button(admission, "StartExam", "开始入学考试", 24, 294, 298, 48);
                Text(admission, "ExamPrompt", "准备好后开始考试。", 406, 20, 770, 133, 22, Ink);
                Text(admission, "ExamSuggestion", "", 406, 164, 770, 65, 17, Subtle);
                Button(admission, "ExamCardAnswerYes", "是 · 垃圾邮件", 406, 239, 354, 48);
                Button(admission, "ExamCardAnswerNo", "否 · 正常邮件", 779, 239, 397, 48);
                Text(admission, "ExamFeedback", "", 406, 302, 770, 58, 17, Subtle);
            }

            private void BuildShop()
            {
                var header = Panel(root, "NativeMarketplaceHeader", 18, 14, 1200, 106, Color.white);
                Text(header, "Brand", "寻宝", 22, 8, 200, 52, 42, Accent);
                Text(header, null, "电脑城  /  本地模拟商店", 24, 65, 230, 24, 16, Subtle);
                Input(header, "SearchInput", "搜索显卡 / GPU / 机箱 / 电脑硬件", 266, 23, 584, 50);
                Text(header, "HeaderMoney", "余额", 876, 20, 300, 32, 23, Ink, TextAlignmentOptions.MidlineRight);
                Text(header, "Objective", "硬件直购 · 不连接真实商城", 876, 60, 300, 35, 15, Subtle, TextAlignmentOptions.MidlineRight);
                Text(root, null, "全部硬件   /   显卡加速   /   机箱扩展", 24, 126, 548, 28, 18, Accent);
                Text(root, "HeaderSamples", "样本", 610, 126, 190, 28, 15, Subtle);
                Text(root, "HeaderSteps", "训练", 813, 126, 190, 28, 15, Subtle);
                Text(root, "HeaderLearning", "学习点", 1018, 126, 200, 28, 15, Subtle);
                var shop = Page("shop", null);
                var gpu = Panel(shop, "ProductGpu", 18, 162, 591, 426, Color.white);
                Text(gpu, null, "750Ti 图形加速卡", 24, 17, 540, 32, 24, Ink);
                Art(gpu, "GpuArt", 10, 68, 278, 270);
                Text(gpu, "GpuPrice", "显卡售价", 302, 70, 263, 47, 31, Accent);
                Text(gpu, "GpuStats", "显卡性能与插槽", 302, 132, 263, 137, 18, Subtle);
                Button(gpu, "BuyGpu", "购买显卡", 302, 292, 263, 48);
                Button(gpu, "SellGpu", "出售一张", 302, 352, 263, 42);
                Text(gpu, null, "可提升并行训练能力\n收益仍受网络拓扑、温度与电力限制", 24, 349, 256, 58, 15, Subtle);
                var chassis = Panel(shop, "ProductCase", 626, 162, 592, 426, Color.white);
                Text(chassis, null, "扩展机箱 · 为成长留空间", 24, 17, 540, 32, 24, Ink);
                Art(chassis, "CaseArt", 8, 64, 277, 298);
                Text(chassis, "CasePrice", "机箱售价", 302, 70, 264, 47, 31, Accent);
                Text(chassis, "CaseStats", "机箱容量与电力预算", 302, 132, 264, 178, 18, Subtle);
                Button(chassis, "BuyCase", "购买机箱", 302, 345, 264, 49);
                Text(chassis, null, "扩展插槽与承载能力", 24, 374, 252, 24, 15, Subtle);
                Text(root, "ShopSummary", "所有价格与库存来自当前游戏状态。", 24, 599, 886, 48, 18, Ink);
                SaveButtons(root, 1000, 603);
            }

            private void BuildHome()
            {
                var header = Panel(root, "NativeHouseholdHeader", 18, 14, 1200, 116, Color.white);
                Text(header, "Brand", "家庭账单", 25, 14, 610, 42, 32, Ink);
                Text(header, "Objective", "先照顾好每一天的电费。", 27, 65, 638, 33, 18, Subtle);
                Text(header, "HeaderMoney", "家庭余额", 791, 18, 385, 37, 24, Ink, TextAlignmentOptions.MidlineRight);
                Text(header, "HeaderSamples", "样本", 680, 77, 150, 24, 14, Subtle);
                Text(header, "HeaderSteps", "训练", 850, 77, 150, 24, 14, Subtle);
                Text(header, "HeaderLearning", "学习点", 1018, 77, 160, 24, 14, Subtle);
                var powerTab = Button(root, "Tab_power", "用电账单", 18, 140, 186, 40);
                var jobsTab = Button(root, "Tab_jobs", "家庭收入", 216, 140, 186, 40);
                SaveButtons(root, 1000, 140);
                var power = Page("power", powerTab);
                var receipt = Panel(power, "NativeCurrentBill", 18, 194, 764, 442, Color.white);
                Text(receipt, null, "本期应付 / ELECTRICITY STATEMENT", 27, 20, 705, 25, 15, Subtle);
                Text(receipt, "BillAmount", "当前账单", 27, 59, 705, 67, 42, Ink);
                Text(receipt, "BillDetails", "用电计量与计费明细", 27, 142, 705, 166, 18, Ink);
                Text(receipt, "BillStatus", "账单状态", 27, 310, 705, 51, 17, Subtle);
                Button(receipt, "PayBill", "缴纳本期账单", 27, 372, 270, 47);
                Button(receipt, "ResetBreaker", "恢复断路器", 315, 372, 254, 47);
                var meter = Panel(power, "NativeHouseholdMeter", 798, 194, 420, 260, Color.white);
                Art(meter, "MeterArt", 75, 4, 262, 252);
                var powerSummary = Panel(power, "NativePowerConsumption", 798, 468, 420, 168, Color.white);
                Text(powerSummary, "PowerStats", "设备负载", 21, 17, 378, 135, 17, Ink);
                var jobs = Page("jobs", jobsTab);
                var income = Panel(jobs, "NativeIncomeStatement", 18, 194, 764, 442, Color.white);
                Text(income, null, "学习机兼职 / 邮件清洗订单", 27, 24, 705, 40, 27, Ink);
                Text(income, "JobStats", "当前订单收益", 27, 84, 705, 188, 20, Ink);
                Text(income, "JobStatus", "当前订单状态", 27, 287, 705, 66, 18, Subtle);
                Button(income, "ToggleJob", "启用订单", 27, 372, 270, 47);
                var incomeHelp = Panel(jobs, "NativeIncomeGuide", 798, 194, 420, 442, Color.white);
                Art(incomeHelp, "IncomeMeterIllustration", 56, 16, 302, 290, "MeterArt");
                Text(incomeHelp, null, "收入与用电共用同一家庭余额。\n\n订单仍需有效网络、足够资源和稳定供电。", 24, 304, 370, 117, 18, Subtle);
            }

            private void Metrics(Transform parent, float x, float y, float width, float height)
            {
                string[] ids = { "HeaderMoney", "HeaderSamples", "HeaderSteps", "HeaderLearning" };
                for (int i = 0; i < ids.Length; i++) Text(parent, ids[i], "—", x + i * width / 4, y, width / 4 - 8, height, 18, Ink);
            }
            private void SaveButtons(Transform parent, float x, float y)
            {
                Button(parent, "SaveNow", "保存", x - 64, y, 130, 38);
                Button(parent, "ReloadSave", "读取", x + 76, y, 130, 38);
            }
            private RectTransform Page(string id, ButtonManager tab)
            {
                var page = Empty(root, "NativePage_" + id, 0, 0, 1236, 693);
                bool first = pages.Count == 0;
                pages.Add(new NativeAppPage { id = id, page = page.gameObject, tabButton = tab });
                page.gameObject.SetActive(first);
                return page;
            }
            private RectTransform Empty(Transform parent, string name, float x, float y, float width, float height)
            {
                var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false); rect.gameObject.layer = 5;
                Place(rect, parent, x, y, width, height);
                return rect;
            }
            private RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color fill)
            {
                var panel = Clone(panelTemplate, parent, name, x, y, width, height);
                var image = panel.GetComponent<Image>();
                image.raycastTarget = false;
                var element = image.GetComponent<UIManagerElement>();
                if (element != null) Object.DestroyImmediate(element);
                image.color = fill;
                return panel;
            }
            private TMP_Text Text(Transform parent, string id, string value, float x, float y, float width, float height,
                float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
            {
                bool singleLine = height < size * 2.1f && value.IndexOf('\n') < 0;
                // Noto CJK's line box is about 1.55× font size. Reserve it once
                // in the authored layout; never run per-frame AutoSize fitting.
                if (singleLine) size = Mathf.Min(size, Mathf.Max(10, (height - 2) / 1.6f));
                var rect = Clone(size <= 17 ? detailTemplate : titleTemplate, parent, id ?? "NativeCaption", x, y, width, height);
                var text = rect.GetComponent<TMP_Text>();
                text.text = value; text.fontSize = size; text.enableAutoSizing = false;
                text.alignment = alignment; text.color = color; text.raycastTarget = false;
                text.margin = Vector4.zero;
                text.textWrappingMode = singleLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Ellipsis;
                var element = text.GetComponent<UIManagerElement>();
                if (element != null) { element.useCustomColor = true; element.useCustomFont = true; }
                foreach (var fitter in rect.GetComponents<ContentSizeFitter>()) fitter.enabled = false;
                if (id != null) texts.Add(new NativeAppText { id = id, control = text });
                return text;
            }
            private ButtonManager Button(Transform parent, string id, string caption, float x, float y, float width, float height)
            {
                var rect = Clone(buttonTemplate, parent, id, x, y, width, height);
                var button = rect.GetComponent<ButtonManager>();
                button.buttonText = caption; button.textSize = 20; button.enableIcon = false;
                button.enableText = true; button.useCustomContent = false; button.autoFitContent = false;
                button.useLocalization = button.useSounds = button.useRipple = button.useHoverEffect = false;
                button.checkForDoubleClick = false; button.useUINavigation = false;
                button.padding.top = button.padding.bottom = 0;
                button.onClick = new UnityEvent(); button.onDoubleClick = new UnityEvent();
                button.UpdateUI();
                // Keep the native sprites/state groups. Only remove the prefab's
                // 30px vertical content padding that clipped a 31px CJK line box.
                foreach (var fitter in rect.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
                foreach (var group in rect.GetComponentsInChildren<HorizontalLayoutGroup>(true))
                {
                    group.padding.top = group.padding.bottom = 0;
                    group.childControlHeight = true;
                    group.childForceExpandHeight = true;
                }
                foreach (var text in rect.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.margin = Vector4.zero;
                    text.alignment = TextAlignmentOptions.Center;
                    var line = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
                    line.minHeight = 32;
                    line.preferredHeight = 32;
                    line.flexibleHeight = 1;
                    var element = text.GetComponent<UIManagerElement>();
                    if (element != null) element.useCustomFont = true;
                }
                if (id == "BuyGpu" || id == "BuyCase" || id == "PayBill" || id == "StartExam"
                    || id == "UpgradeSkill" || id == "UpgradeTraining" || id == "RecordName")
                    AccentButton(button);
                buttons.Add(new NativeAppButton { id = id, control = button });
                return button;
            }
            private void AccentButton(ButtonManager button)
            {
                // Color the authored native state backgrounds; keep their sprites,
                // nine-slice borders, shadows, state groups and native interaction.
                foreach (var image in button.GetComponentsInChildren<Image>(true))
                {
                    if (image.name != "Background") continue;
                    var driver = image.GetComponent<UIManagerElement>();
                    if (driver != null) Object.DestroyImmediate(driver);
                    bool highlighted = image.transform.parent != null && image.transform.parent.name == "Highlighted";
                    image.color = highlighted ? Color.Lerp(Accent, Color.white, .14f) : Accent;
                }
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.color = Color.white;
                    var element = label.GetComponent<UIManagerElement>();
                    if (element != null) element.useCustomColor = true;
                }
            }
            private TMP_InputField Input(Transform parent, string id, string placeholder, float x, float y, float width, float height)
            {
                var rect = Clone(inputTemplate, parent, id, x, y, width, height);
                var field = rect.GetComponent<TMP_InputField>();
                field.SetTextWithoutNotify(string.Empty);
                field.richText = false; field.characterLimit = id == "NameInput" ? 24 : 80;
                field.customCaretColor = true; field.caretColor = Ink;
                // This authored native prefab intentionally leaves TMP_InputField's
                // m_Placeholder null; InputFieldManager animates its sibling instead.
                var label = field.placeholder as TMP_Text;
                if (label == null) label = rect.Find("Placeholder")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.text = placeholder;
                    label.richText = false;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Overflow;
                }
                if (field.textComponent != null) field.textComponent.richText = false;
                foreach (var element in rect.GetComponentsInChildren<UIManagerElement>(true))
                    if (element.objectType == UIManagerElement.ObjectType.Text) element.useCustomFont = true;
                foreach (var button in rect.GetComponentsInChildren<ButtonManager>(true))
                { button.useLocalization = false; button.useRipple = false; button.useHoverEffect = false; button.useSounds = false; }
                inputs.Add(new NativeAppInput { id = id, control = field });
                return field;
            }
            private Image Art(Transform parent, string id, float x, float y, float width, float height, string artKey = null)
            {
                var rect = Clone(panelTemplate, parent, id, x, y, width, height);
                var image = rect.GetComponent<Image>();
                foreach (var element in rect.GetComponents<UIManagerElement>()) Object.DestroyImmediate(element);
                image.sprite = art[artKey ?? id]; image.type = Image.Type.Simple;
                image.material = null; image.color = Color.white; image.preserveAspect = true;
                image.raycastTarget = false;
                images.Add(new NativeAppImage { id = id, control = image });
                return image;
            }
            private RectTransform Clone(GameObject source, Transform parent, string name, float x, float y, float width, float height)
            {
                var instance = Object.Instantiate(source, parent, false);
                instance.name = name;
                if (PrefabUtility.IsPartOfPrefabInstance(instance))
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var localized in instance.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
                foreach (var element in instance.GetComponentsInChildren<UIManagerElement>(true))
                {
                    element.themeManagerAsset = theme;
                    element.UpdateElement();
                    if (element.objectType == UIManagerElement.ObjectType.Text) element.useCustomFont = true;
                }
                foreach (var transform in instance.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 5;
                var rect = instance.GetComponent<RectTransform>();
                Place(rect, parent, x, y, width, height);
                return rect;
            }
            private void Place(RectTransform rect, Transform parent, float x, float y, float width, float height)
            {
                Vector2 area = sizes[parent];
                rect.anchorMin = new Vector2(x / area.x, 1 - (y + height) / area.y);
                rect.anchorMax = new Vector2((x + width) / area.x, 1 - y / area.y);
                rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                sizes[rect] = new Vector2(width, height);
            }
        }

        private static UIManager CreateTheme(UIManager source, string profile)
        {
            EnsureFolder(ThemeDirectory);
            string path = ThemeDirectory + "/" + profile + ".asset";
            var theme = AssetDatabase.LoadAssetAtPath<UIManager>(path);
            if (theme == null)
            {
                theme = Object.Instantiate(source); theme.name = "LingGuang Native " + profile;
                AssetDatabase.CreateAsset(theme, path);
            }
            theme.enableDynamicUpdate = false; theme.enableLocalization = false;
            theme.enableUIBlur = false; theme.selectedTheme = UIManager.SelectedTheme.Custom;
            if (profile == "lingguang")
            {
                theme.primaryColorDark = new Color32(231, 244, 253, 255);
                theme.secondaryColorDark = new Color32(153, 185, 206, 255);
                theme.windowBGColorDark = new Color32(9, 21, 35, 255);
                theme.backgroundColorDark = new Color32(18, 38, 57, 255);
                theme.highlightedColorCustom = new Color32(29, 158, 203, 255);
            }
            else
            {
                theme.primaryColorDark = new Color32(31, 45, 59, 255);
                theme.secondaryColorDark = new Color32(98, 110, 122, 255);
                theme.windowBGColorDark = profile == "xunbao" ? new Color32(245, 244, 241, 255) : new Color32(235, 240, 243, 255);
                theme.backgroundColorDark = Color.white;
                theme.highlightedColorCustom = profile == "xunbao" ? new Color32(233, 94, 29, 255) : new Color32(45, 112, 160, 255);
            }
            theme.highlightedColorSecondaryCustom = Color.white;
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssetIfDirty(theme);
            return theme;
        }
        private static Sprite LoadArt(string name)
        {
            string path = ArtDirectory + name;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) { AssetDatabase.ImportAsset(path); texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); }
            if (texture == null) throw new InvalidOperationException("缺少实际生成图片：" + path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return Required<Sprite>(path);
        }
        private static T Required<T>(string path) where T : Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new InvalidOperationException("缺少原生模板/资源：" + path);
            return value;
        }
        private static void Validate(ChapterOneDesktopBridge bridge)
        {
            if (bridge.NativeApp == null || bridge.App != null || bridge.IsNativeChat || bridge.NativeApp.Runtime != bridge.Runtime)
                throw new InvalidOperationException("原生应用绑定验证失败：" + bridge.ApplicationId);
            if (bridge.Content.GetComponentsInChildren<ChapterOneApp>(true).Length != 0)
                throw new InvalidOperationException("原生应用仍包含旧自绘 UI：" + bridge.ApplicationId);
        }
        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int index = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, index));
            AssetDatabase.CreateFolder(path.Substring(0, index), path.Substring(index + 1));
        }
    }
}
#endif
