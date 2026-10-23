# 持续已读新闻 → 私人 AI：UI 接入候选

本轮只写独立候选目录，未修改生产工程、未开启 Unity/Player、未提交或推送。Claude 终局批 A、888，以及图鉴/群预设产品代码不在本候选范围。

## 基线与交付

- 生产：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。
- HEAD：`3533141a406940d571e064f3797328c7d214235a`，基线为当前未提交工作区，不是干净 HEAD 源码。
- 当前 `Assets/LocalBuild/Editor/SidebarLayoutChecks.cs` SHA256：`f80553299b01803d8467bf67ba90b3fa8febf6b563b97b8fe417960400a82616`。
- 候选根目录：`C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/read-news-ui-resumed`。
- 补丁：`patch/read-news-ui-integration.patch`，SHA256：`3ae9ab0a40b71fd1a32042bb2d5643704228669204b9f979e5182c15ba2d89d5`。
- 基线快照位于 `baseline/`，可审阅候选位于 `candidate/`；逐文件及编译引用 hash 见 `manifest.json`。

本补丁包含 3 文件：新 `ReadNewsRagActualUiChecks.cs` 及其 `.meta`，与当前 `SidebarLayoutChecks.cs` 的最小接线差异。辅助复用 `../read-news-rag/ui-candidate/` 原稿，SHA256 `9e6631e9056a0988bfda8bf73d05e2bd17b7fcc0dea2d64e22447837989044c5`，未重写其验证内容。持续阅读产品已在生产，不能重复应用旧产品补丁。

## 接线行为

新增可选入口 `SidebarLayoutChecks.RunReadNewsRag`，phase 23，420 秒上限；沿用共享 runner 的 Main、隔离 Save/DesktopProfile、语言、截图和模型禁用机制。主循环逐帧 MoveNext，结束收集 Checks/AssertionCount/ReadClicks，要求双语共 14 次确认（每种语言六次正常和一次同帧重复）。

Finish 负责释放该迭代器和清除请求标志；释放异常记为失败并移除本次验证的 passed.txt。收到 ExitingPlayMode 且该迭代器仍在运行时，记录中断失败并走清理。这个退出与清理分支只完成编译检查，仍需运行时验证。

图鉴 agent 若也向共享 runner 加入口，主控按独立 hunks 整合，phase 23 已告知主控预留；不得用整份候选覆盖已有共享修改。

## 辅助覆盖范围（已编写，尚未运行）

- 中文/英文真实新闻列表 → 正文 → 读完按钮共六篇；打开不计已读，第六篇继续入历史，news5 解锁计数封顶五。
- 第六篇同帧重复 LCD 点击去重；真实返回首页后的旧按钮拒绝；显式替换存档 owner 后旧按钮不能改任一存档。
- 独立 LocalVerification 保存目录 slot 5 的真实 SaveSlot/LoadArchive，再打开第六篇，检查历史和已读界面。换档本身是夹具/API操作，不是读档 UI 验收。
- 购买前无阅读引用；使用真实 BuyNode 核心方法购买 daily.news 后，仅私人 AI 的生产 LlmPersonas 提示构造包含相关的第六篇本地化数据及来源规则，未确认第七篇不入提示；人类联系人提示无私人阅读数据；专用 XgYyTalk 系统提示也含来源规则。
- 阅读与提示构造不能自动增加论坛或其他人类联系人消息/事实。
- 预期四截图：`read-news-zh-sixth-read.png`、`read-news-zh-sixth-reloaded.png`、英文同名两图。

日期、阶段、软件安装、资金、存档切换、核心购买和滚动定位为明确夹具。真实焦点、LCD 命中和输入门禁保留。查询为合成提示输入，未发送给模型、未写聊天。**提示来源验证不是模型真实回答验证，不提供推理、自然全流程或最终 Player 证据。**

## 本轮实际验证

1. 只读核对最新暂停后交接、协作状态、整合记录与六小时决策 D029；读取当前生产新闻页和 runner，确认产品守卫与 phase 23 未占用。
2. 使用已有 Unity 安装中的 dotnet+csc 编译候选两个 C# 文件，引用当前生产已编译程序集及既有 Unity 编辑器依赖。退出 0，12 个警告（8 个旧 API 弃用，4 个共享 runner 既有字段未用/未赋值）。响应文件 `compile/ReadNewsUiIntegration.rsp`，输出 `compile/ReadNewsUiIntegration.dll`，日志 `compile/compile.log`。这是隔离编译，不是 Unity 导入或全工程重新编译。
3. `git -C <production> apply --check <candidate>/patch/read-news-ui-integration.patch` 通过；仅检查，未应用。生成补丁后重新核对生产 Sidebar hash 与基线一致。

## 主控接续与未验证

先核对基线或审阅共享文件新差异，只应用此 UI 补丁并与其他 runner 接线手工合并。由主控确认单 Unity 占用后重新导入/编译，执行 `SidebarLayoutChecks.RunReadNewsRag`，保持实际前台、原有隔离目录与禁模型设置。检查 report.json、失败日志、四截图、保存文件以及中断恢复。

尚未做：生产合入、Unity 导入/编译、实际 UI 运行、截图审看、slot 磁盘运行证明、中断/失败清理实测、模型推理、Player、性能、音频、构建、发布。本轮没有重跑历史 35 项核心测试；其通过事实来自交接，不是本候选新增执行结果。
