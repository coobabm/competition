# 两处界面语言刷新修复

2026-10-19。按主控授权直接修改生产，仅涉及以下两份产品文件；未启动 Unity、编译、模型或压缩，未提交/推送。保持低功率，只做定向读取、补丁和源码检查。

## 已做

1. `Assets/LingGuangV05/YY2016/YYChatView.cs`
   - Attach 时保留原生 Status 的 TMP_Text 引用（仍排除 Content 内部标签）。
   - 现有 OnLanguage 回调直接更新原生底栏，继续使用已有 Lang.En 翻译。
   - 不重建 YY、不改变选中联系人、聊天输入或消息历史；保留原 OnLanguage 的 dirty/signature 处理。

2. `Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.Narrative.cs`
   - 保存当前实际展示的 prompt 与题干、选项、Later 标签引用。
   - 复用现有 PollNarrative，在原可用性门禁之后、0.5秒快照节流之前检查语言；无语言变化时只比较布尔值后返回。
   - 发生变化时只更新当前控件的 TMP_Text.text；题干与选项继续经过原 Safe 内容过滤。
   - 没有重建面板/按钮，不换 prompt、不捕获新 token、不修改 save/state 引用或输入焦点，不回答、不写收据、不调用持久化，也不改变 nextSeedAllowed/nextNarrativePoll。
   - CloseSeed 同步释放新增引用；主 host 文件无需修改。原来 Settings/剧情独占等触发的 SuspendNarrative 行为未改。

## 已完成源码检查

- `git diff --check -- Assets/LingGuangV05/YY2016/YYChatView.cs Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.Narrative.cs`：没有空白错误输出。
- 审阅 Narrative 完整差异、YY 更新片段：仅上述展示刷新；关机题目的按钮回调、答案索引、绑定校验、冷却写入保持原样。
- 复用 `System.Array.Empty`、TMP_Text 与现有 PollNarrative 生命周期；未增加事件订阅和全局查找。
- 没有新增镜像实现的单元测试。源码检查不等于编译或实际 UI 通过。

## 交给主控串行验证

- 正常 Attach 的 YY：中→英→中切换，底栏同步；窗口隐藏后切语言再打开也正确，当前联系人与输入框文本不变。
- 已打开关机题面：中→英→中，题干、所有选项、Later 同步。比对面板与各按钮对象 ID、当前 EventSystem 选择、shownSeed、原绑定和 nextSeedAllowed 不变；换语言本身不得新增答案/记忆/保存回执。
- 换语言后选择原按钮仍提交对应原题目和原 option index 一次；旧档切换后旧按钮拒绝仍沿用现有绑定检查。
- 补拍英文题面截图并检查字形 contained；本补丁没有更改布局尺寸或预判英语字数一定适配。

## 修改后 SHA256

- YYChatView.cs：D61EAD6CDE6F447A139D2A0BFAD3B8B529C99E44D978B59E7E1D1C7BF5831750
- XgEndingPreparationHost.Narrative.cs：3BC30616EE4AD38782615932E41B19F2A6ED9D2DE721D17762E65625E1BCF6E7

YYChatView.cs 本来已有本轮未提交的群图/私人预览改动，均保留；没有把相对 HEAD 全部差异声称为本次修复。原 style.md 是修复前审查证据，继续保留，不倒写旧截图。
