# lesson-009 · 第一张教学卡输入对应关系

日期：2026-10-13（Asia/Singapore）。范围仅为已授权第 13 行的第一张教学卡键盘 1/2、鼠标、按钮文字和输入焦点保护。未读取新策划正文。

## 结论与交付

确认一处缺陷，交付 `changes.patch`，仅修改 `Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Spider.cs:171` 的页面白名单：允许 `label` 或 `teach`。生产源码未修改，补丁待整合负责人采纳。

当前教学入口 `XingGuangView.cs:75,126` 默认使用 `teach` 并创建 `XgLabelPage`；`:105` 返回该页面名，`:324` 调用活动页面 Tick。`XgLabelPage.cs:484` 调用 `TickKeys()`，但 `XgLabelPage.Spider.cs:171` 遇到 `host.Tab != "label"` 立即返回。因此当前第一教学卡鼠标可答，按钮虽显示 1/2，键盘路径却被拒绝。`ZhongbaoView.cs:56,75,360` 的旧 `label` 入口仍须保留。

已核对 `Documentation/DesignPass/Agents/issues-task-001.md`：已有教学评价修正和模型层检查，没有修正上述页面过滤；本补丁不重复修改教学语义、不增加模型层重复测试。

## 已正确的部分：无需修改

- `XgLabelPage.cs:187–188`：鼠标按钮分别绑定 `OnAnswer(true)`、`OnAnswer(false)`；`ZhongbaoSkin.cs:132–138` 将回调转交 `XgUi.Button`，`XgUi.cs:198` 注册至按钮 `onClick`。
- `XgLabelPage.cs:671–672,689`：左按钮“猜对了 / Guessed right”显示 1，右按钮“猜错了 / Guessed wrong”显示 2；`XgLabelPage.Spider.cs:172–173` 的主键盘/数字键盘 1、2 分别调用同一按钮的 `onClick`。加入 teach 白名单后不需要另建键盘回答逻辑。
- `XgLabelPage.cs:373` 将按钮布尔值作为“猜测是否正确”传给 `TryEvaluateTeachingGuess`；`XgSim.Teaching.cs:53` 用 `guessWasRight ? shownGuess : !shownGuess` 转换题目标注。AI 猜“否”时，1 认可“否”，2 纠正为“是”，没有倒置。
- `XgLabelPage.Spider.cs:192–195` 检查 EventSystem 当前选中对象上的 `TMP_InputField` 和 `UnityEngine.UI.InputField`；`:171` 在处理按键前检查 `Typing()`。补丁完整保留该保护。
- 当前 uGUI 包的 TMP 输入框 `TMP_InputField.cs:4327–4333`、传统输入框 `InputField.cs:3150–3156` 激活时将自身设置为 EventSystem 选中对象；对应 `OnDeselect`（TMP :4463、传统 :3279）调用停用。现有组件选择判断覆盖这两种标准焦点输入路径，未发现必须增加父级遍历或全场景扫描的代码依据。
- 验证码显示、窗口不可见、非最前窗口、按钮不可交互或层级隐藏时的快捷键拦截保持原样。同帧同时按 1/2 的现有 `if/else if` 最多触发一个键盘按钮（1 优先）；不扩展到鼠标与键盘同帧去重等其他任务。

## 精确补丁与依赖版本

补丁含目标路径、目标 SHA256 和原始上下文。合并时保留目标文件 CRLF；若目标哈希已变化，先复核该方法，不直接覆盖文件。

相对工程根的依赖及 SHA256：

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Spider.cs | A2D644947B93FCA18C64526DFB4706DEFF287169D837EF2E50255D9583F20A73 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.cs | 615ED6C31B9512919BB1E907D9A097B4B88A350A3D3B4FCF5CB8E6EEE3925A03 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/DesktopBridge/Zhongbao/ZhongbaoSkin.cs | FD384CD890509D7E290858F3FCCD4ABDCA725B9C17BB4A7FF098EA2C76F277C4 |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs | A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14 |
| Documentation/DesignPass/Agents/issues-task-001.md | C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971 |

输入框辅助证据来自当前只读包 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/`：TMP/TMP_InputField.cs SHA256 为 `391015B232A8C856E1E9143C2AA65F456CC7A225A61779AAE19030E1CD0F1CE0`；UGUI/UI/Core/InputField.cs 为 `9E7225484588F4950FC10F5CFDAEF23A67104F383204AE25E8854DB4C0293068`。

## 检查与待验证

静态检查：已审阅上述调用链；已执行 `git apply --check --ignore-space-change Documentation/DesignPass/Parallel100/lesson-009/changes.patch`，退出码 0，仅检查可应用性，未应用补丁。交付前再次核对上表七个依赖，哈希与首次记录一致。

实际运行：未启动 Unity、游戏或编译，未运行 DesignFirstLessonChecks。视觉验收：未执行。以下均为待主聊天使用内存测试状态/隔离测试场景验证，不能据本报告标为运行通过：

| 边界 | 预期 |
| --- | --- |
| 默认 teach 首卡，AI 分别猜是/否；1、2、数字键盘 1/2、鼠标分别独立操作 | 1 与左侧“猜对了”完全一致；2 与右侧“猜错了”完全一致；比较相同初始卡状态的结算、学习与事件次数 |
| 保留 label 入口，其他页面如 board/home | label 原操作不回退；其他页面按数字不作答 |
| TMP 与传统输入框分别获得焦点，输入及长按 1/2、数字键盘 1/2，包括选中文字、光标移动、输入法组合期间 | 卡片 id、AnswerCount、钱包、样本、LessonTaught 次数均不变；数字按输入框规则输入/过滤 |
| 输入框失焦后重新按键；点击回答按钮 | 失焦后新按键恢复作答；明确点击回答按钮按其文字提交 |
| 验证码、非前台窗口、隐藏窗口、禁用/隐藏回答按钮 | 数字键不误答教学卡 |
| 同帧 1 和 2；按住单键跨帧 | 同帧键盘路径最多一次，1 优先；仅按下沿触发 |
| 中英文与窄窗口 | 当前文字及 1/2 提示可辨识，鼠标点击目标正确 |

待整合：仅本目录的一行补丁。统一编译、输入事件时序和真实场景截图由主聊天负责；不将第 13 行整体或全局原则标记完成。
