# motion-001：鼠标按键与业务回调边界

日期：2026-10-13（Asia/Singapore）。结论：当前生产实现无需改动，不提供 changes.patch。仅完成源码静态核查；本聊天未编译、未运行 Unity／游戏、未做视觉验收。

## 范围与已有成果

仅核查 XgButtonPressFeedback 的左／右／中键输入及业务回调是否保持 uGUI Button 语义。已阅读已有记录 `Documentation/DesignPass/Agents/motion-task-001.md` 与 `Assets/LocalBuild/Editor/MotionButtonChecks.cs`，不重复其实现或验证入口，不读取新的策划正文，不标记「三层反馈」全局原则完成。生产源码只读；交付仅此文件。

## 源码证据与结论

下列路径均相对工程根目录。uGUI 基准目录为 `Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/`。

| 边界 | 当前源码依据 | 静态结论 |
| --- | --- | --- |
| 左键按下 | `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:18` 先调用 base.OnPointerDown，再仅在 Left 时调用 PressLabel；PressLabel 无 onClick 调用 | 按下登记文字效果，不提前执行业务；保留基类选中及背景状态转换 |
| 左键抬起／点击 | 子类未覆盖 OnPointerUp 或 OnPointerClick。`UI/Core/Selectable.cs:1241` 仅在左键抬起时清除按下状态；`UI/Core/Button.cs:110` 仅接受左键点击，随后调用 Press；`:65` 的 Press 检查 IsActive 和 IsInteractable 后调用一次 m_OnClick.Invoke | 抬起本身不执行业务；每次合法左键 Click 只进行一次 UnityEvent 调用。监听器自身多次注册或主动重入不在这个保证内 |
| 右键与中键 | `Selectable.cs:1201`、`:1241` 对非 Left 直接返回；子类 `:21` 的 Left 判断阻止 PressLabel；继承的 Button.OnPointerClick 对非 Left 直接返回 | 两者都不启动文字按压、不改变 Selectable 的按下状态、不调用按钮业务。基础悬停、输入模块的选择处理不属于此处“无按压／无回调”的保证 |
| 左键按住时夹杂右／中键 | 相同的非 Left 返回路径，对私有 pressFx 也无写入 | 右／中键事件不会重启或清除正在播放的左键文字效果，也不会替左键抬起；不存在额外回调来源 |
| 禁用／不可交互 | 子类 `:34` 在登记效果前检查 IsActive、IsInteractable；Button.Press 使用同样的两个检查；`Selectable.cs:486` 包含 CanvasGroup 的交互许可 | 已禁用输入不会新建按压或执行业务；按下后变为不可交互，再收到 Click 时业务仍被基类拦截 |
| 按下后移出释放 | `EventSystem/InputModules/StandaloneInputModule.cs:207` 的 ReleaseMouse 只在释放目标点击处理器匹配且 eligibleForClick 时分发 Click | 是否构成点击由输入模块决定。不能为子类添加“每次 PointerUp 都触发业务”；也不能用手动无条件发送 Click 来证明移出取消 |
| 拖动接管 | `EventSystem/InputModules/PointerInputModule.cs:371` 在拖动对象不同于按压对象时发送 Up 并清除 eligibleForClick | 子类未绕过该取消机制。文字短反馈可在按下时已启动，这是既有设计，不代表业务成功 |
| 回调接入 | `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:188` 创建反馈子类；`:198` 只添加一次传入 onClick；`:210` 设置 Label | 通用接入没有第二条鼠标业务调用路径，原有业务及强动效无需改动 |

补充边界：直接调用 OnPointerClick(Left) 可以在没有先行 Down 的情况下触发业务，这是 Button 原生语义；点击资格由输入模块负责，不能新增子类“必须先 Down”的门槛。直接 onClick.Invoke() 同样不属于玩家鼠标事件路径。宿主 Visible／enabled 对 PressLabel 的限制只影响呈现，不替代 Button 的业务许可检查。

## 待整合与待验证

无需整合生产补丁。现有 MotionButtonChecks 已覆盖右键、左键按下无业务、点击一次回调，以及禁用等场景；没有中键或混合按键断言。记录中的既往编译结果属于原任务，本次不据此宣称运行通过。

主聊天统一验证时补充下列边界即可，无需为覆盖缺口改动生产逻辑：

1. 对 Right、Middle 分别发送 Down → Up → Click：回调增量为 0，首次使用时无文字效果组件；对 Left：Down 和 Up 后增量均为 0，Click 后为 1。
2. Left Down 后插入 Right／Middle 的 Down → Up → Click：业务仍为 0；左键按下状态仍保留；已有文字效果的对象、进度及参数未被重启。最终合法 Left Up → Click 的业务增量为 1。宜与普通 Button 同序列对照，避免只复述子类实现。
3. Left Down 后禁用按钮或父 CanvasGroup，再释放：回调增量为 0。恢复后一次合法点击的增量为 1。
4. 使用实际输入模块验证按住移出并在外释放、移出再返回后释放、拖动接管；与普通 Button 比较业务次数。仅直接 ExecuteEvents.Click 无法验证射线目标及点击资格。
5. 回调中隐藏或禁用自身时，一次点击仍只调用业务一次，并由主聊天观察既有反馈的复位和保留的业务动效。

以上均为待运行用例，未执行、未记 PASS；真实帧进度和视觉效果待统一场景验收。本任务不新增独立脚本以免重复已有入口，亦未启动 Unity、读取玩家存档、提交、推送或发消息给其他聊天。

## 核查基线 SHA256

| 文件 | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs | `1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78` |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs | `A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14` |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs | `2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC` |
| Assets/LocalBuild/Editor/MotionButtonChecks.cs | `22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E` |
| Documentation/DesignPass/Agents/motion-task-001.md | `755DBC9C34002157AE71792FA8484A02F0776311C36CF586518DC97D9C99B9EE` |
| uGUI 基准目录下 UI/Core/Button.cs | `3AD850616D59ED6303135BA648A620A3730398EE9D398914612C581BECF7C69C` |
| uGUI 基准目录下 UI/Core/Selectable.cs | `02EBB0484A16FE6FA0D34EA1CA5248B845503214D3D6E7318E055D54B8537596` |
| uGUI 基准目录下 EventSystem/InputModules/StandaloneInputModule.cs | `286252C0E94B556B359751812B845C6E655364C62D80E90CA6D01A499E54D4C1` |
| uGUI 基准目录下 EventSystem/InputModules/PointerInputModule.cs | `022612B030DE58C8A07FAD32763210C99BF120427661880DA4695F36AA9EBD77` |

交付前复核结果另附于下方；此结论仅适用于上述源码版本。

交付前已再次计算全部 9 项依赖的 SHA256，均与上述基线一致。此复核是文件一致性检查，不是运行测试。
