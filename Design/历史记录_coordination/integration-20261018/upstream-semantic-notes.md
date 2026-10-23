# upstream 7da28342 剧情隐私语义移植

## 合入顺序与边界

先合入 `legacy-world-yy-rebased-7da28342-context-only.patch`，再按顺序应用以下三层。本轮只改 coordination staging，未写 main，未启动 Unity。

1. `upstream-semantic-01-phone.patch`：手机备份改为主人公自己的旧手机；保留 4,312 张、八文件夹、纠错加样本、证件照选择、横图识别失败、阶段门槛与旧 ps 字段。结束复盘为私人窗口；便签不再引导去群里；两条阶段钩子双语同步。同步 PhoneSort 原有测试中的剧情断言。
2. `upstream-semantic-02-private-study.patch`：阶段教学、原三步坦白树转为私密自学便签和三步猜想/证据复盘。自学字段默认 0/false；旧 zhouKnows/zhouDoubt 保留但不注入真人对话。老周离线回答和模型提示均不读取训练状态；历史非玩家秘密内容从人类 prompt 中滤除。借卡保留资源与玩法，改泛问备用卡、电脑市场交接。教学 fallback 改内心独白。便签队列按消息绑定各自选择，连续课程不覆盖选择，当前复盘后续优先于后来的课程。更新原 ZhouChat 测试。
3. `upstream-semantic-03-interactions.patch`：红包/已送礼事务不消耗晴雯当前剧情选项；去掉晴雯没有前置依据的“上次戒了”“昨晚十二点才回”；保留 13 段剧情。是非夜就业问题改升职，中英同步。

## 验证

- 三层补丁在与基线一致的干净文件副本上 `git apply --check` 全通过。
- 所有 staged JSON 可解析；新增钩子中文有英文映射。
- .NET 10 完整 Core + XingGuang Core overlay 编译通过。
- `semantic-check/Program.cs`：27 条实际行为断言通过，覆盖三步复盘、旧知识标记不泄露、反复结算幂等、手机整理两个证件照分支、纠错奖励幂等、阶段解锁和个人纪事。用 `dotnet semantic-check/bin/Debug/net10.0/SemanticCheck.dll` 运行，避免 exe 启动异常弹窗。
- Unity 6000.4.5f1 自带 Roslyn，完整游戏源码 overlay 编译 0 错误；日志 `semantic-check/unity-compile.log`。这属于静态编译，尚非 Unity 编辑器/画面验收。运行态便签排队、手机复盘窗口、晴雯红包/礼物仍需 parent 在 Unity 中验收。
- 新版 NUnit 剧情断言已随补丁更新；未在 Unity Test Runner 内执行。

## 待确认和后续范围

- 老周原教育台词目录仍作为私密自学摘录复用；运行的阶段入口改为玩家独白，没有整段删除教学能力。
- 本轮未生成群聊 13 张图；此前数据新闻只有 43 条的问题未捏造补足。
- 已知既有“送你”匹配可能误解“不送你手机”的问题仍未在这层处理，应在礼物语言验收单独修复。
- 静态编译已有 DayStats 自比较警告，不属于此次剧情移植。
