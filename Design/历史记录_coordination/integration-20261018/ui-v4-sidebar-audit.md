# UI v4 第一批侧栏审计与暂存交付

项目：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016/`。
暂存：`C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/ui-v4-sidebar-stage/`。
未开Unity、未写生产项目；父代理负责合入及串行实测。

## 现状与最小改动

- `Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.cs:68` 原NavWidth=148。暂存改112；Pages与Toast按常量计算，无须另造页面偏移。移除Build中旧Grid/XgGridGraphic，保持XgDark.Page背景。
- `XingGuangView.Frame.cs:233` 已有唯一四项teach/train/tree/home，保留ID、NavButton别名、引导指向与旧入口。原NavVisible把未解锁项隐藏，改四项常驻；FeatureOpen仍为真实授权门槛，锁定Button不允许跳转。
- 原图标20px、文字横排、行高上限84。改50px原图标在上、名字下方、锁提示再下一行，锁用几何图形不依赖emoji字体。四项始终均分剩余空间，footer固定120，解锁和蜘蛛出现不会挤动四项中心。
- 原侧栏底部YY条直到crowd开启才出现；本批保留门槛与真实XgYyTalk.Open，压为“YY聊天”。没有偷偷提前开放YY联系人。
- 保留SpiderAround显示门槛、residentQuiet真实休息/游走状态和XgResidentSpider.Recall真实回叫。压成两键“走走/休息”“叫回”；英文Roam/Rest/Recall，字号11、左右2px留白。没有增加新技能、赠送技能书或更改蜘蛛所有权。
- 音效/减少特效仍改真实S.mute/S.reduceFx及Juice状态；按钮38×38圆形，既有中英tooltip保留；短标签S♪/S×与FX/FX−，后续可图形化。
- 第二轮按父代理要求调用新XgUi.RoundSurface(row.back,12)，选中白10%、悬停白4%，旧青绿色侧条不显示。

## 路由与锁提示的保留边界

`XingGuangView.FeatureOpen`现行规则：teach始终；tree需12题；train需train.curve；home需首次聊天（旧档非teachingFlow保留可见）。对应锁提示从同一FeatureOpen推导，科技计数使用TeachingAnswers而非样本数，错误答案计入12题。

原型名字页要求脑子窗口购买后开放，而当前home为首次聊天；本批按父代理指令仅记录差异，**不改门槛**。侧栏仍传生产ShowTab，避免新导航绕过旧cards/final/backpack条件。新“更多”收纳和双视图大脑是后续页面工作；本批不删旧功能。

`NavButton`保持旧路由映射：data/repo/wiring→train；board/cards/final→home；label→teach；items/abilities/growth→tree。`XgGuideHighlight.TabIds`直接引用NavIds，不另造不一致列表。

## 测试准确入口及本批更新

1. **`SidebarLayoutChecks.Run`**（`Assets/LocalBuild/Editor/SidebarLayoutChecks.cs`）：从EditMode调用，无`-quit`；自行开Main场景、隔离存档/桌面配置、关闭本地模型、退出Play并在batch退出。报告在`LocalVerification/SidebarLayout/<UTC时间>/report.json`、`passed.txt`。
   - 旧16场景：4个进度×2个窗口尺寸960×600/1400×900×中英。旧断言“解锁几项只显示几项”“activeSelf==FeatureOpen”已经因v4被替代，不直接删除验收：现改成始终4项、interactable==FeatureOpen、锁图及说明是否真实显示。
   - 保留等宽高、等间距、行位于menu内、footer不重叠、原创图标、双语TMP尺寸。
   - 新增112逻辑像素宽、50px图标、实际root无Grid、所有footer文字宽高、锁说明无溢出。
   - 新增真实Aero输入反投影→TryMap往返<1px→严格click/down接收者→OnPointerDown/Up，逐项验证已开页实际active、未开页不切换。不是onClick.Invoke模拟。
   - 新增音效和减少特效实际状态切换/恢复；有蜘蛛的受控旧档夹具验证休息状态切换/恢复。未宣称三本蜘蛛新技能已实现。
   - 该夹具人为设置解锁状态，不是自然开局经济测试。
2. **`DesignWallpaperSmoke.RunLessonGoal`**：真实DeskGame输入0→12→付费曲线、错误样本/进度、英语与读档；旧记录为PASS，新布局后仍需重跑。别恢复旧classic按钮，显示题桌覆盖其上。
3. **`DesignWallpaperSmoke.RunGrowth`**：保留别名映射到唯一tree导航的断言，验证旧入口不丢。与侧栏四项常驻不冲突。
4. **`KnowledgeVisualsQa.Run`**（核方法当前文件）：验证知识卡收集和NavButton(tree)/Badge未读点；侧栏重构保留Badge节点名和真正清除时机。
5. **`FirstOpenTextQa.Run`**：首次可见任务大厅与YY字网格，用于窄侧栏改变正文可用宽度后的补充回归，不替代指针导航测试。
6. **`TechTreeUiPlayQa.Run`**：科技窗口实际布局/拖缩放与节点状态回归；它会自行启动隔离Play，文件旧“already isolated Play”注释与Run实现不一致，执行以Run的EditMode检查为准。

## 补丁状态

- 首轮`sidebar.patch`已交父代理，并已收到其应用通知。
- 最终二轮请用`sidebar-increment.patch`，仅Frame与SidebarLayoutChecks。**不复制暂存XingGuangView.cs覆盖当前主文件**：父代理新增`Juice.DisableMotionWhenReduced=true`应保留。
- 第一、二轮Core/UI/Editor安全静态编译均曾exit0；父代理随后新增UiV4PressChecks和反馈API，最终已同步新源并重新编译UI/Editor均exit0（Assembly-CSharp.final.log、Assembly-CSharp-Editor.final.log）。Unity实际布局/指针/截图仍待父代理运行，不记PASS。
