# 送礼否定句与明确意图

补丁顺序：`legacy-world-yy-rebased-7da28342-context-only.patch` → semantic 01/02/03 三层 → `world-gift-explicit-intent.patch`。最后一份只改 `Core/Girlfriend/GirlfriendRules.Money.cs` 与 `YY2016/YYGirlfriend.cs`，基线为 upstream-semantic-stage；不依赖新的训练/题桌改动。未改 main、未启动 Unity。

## 行为

- 将“含礼物名和送字就寄出”收紧成明确当前动作：送你/送给你/请你喝/寄给你，或 I am sending/giving you、A gift for you:、This … is for you 等。
- 否定、疑问、引用、假设、将来计划、撤回玩笑等不自动寄。指定的“不要送你X/还没打算送你X/不是送你X”均拒绝且库存不变。
- 对整个目录先识别名字，再检查 GiftHeld。还在路上、已寄出、已签收或退回都不能发；即使只持有手机，“送你手机壳”也不会被误读为手机。多件不同礼物同时提及不随便挑一件寄。
- 英文做单词边界匹配，避免子串误命中；保留所有礼物面板的中英完整发送文案。特例实测：“奶茶外卖（饿了么）”里的“么”不会被当成疑问。
- `GiveFromPlayerLine(..., byAi)` 是原子入口，先验证意图/库存/AI标志才修改订单。YY 传入 `LastPlayerLineByAi()`。
- 保留 semantic 03 的 giftWasSent/packet 保护：已成功送礼或红包不吞晴雯上游剧情选项；未改 QingwenArc 和各段剧情。
- 解析刻意保守，不保证理解每种自由表述；不明确的句子正常聊天、不动库存。礼物面板仍是明确送礼入口。

## 检查

- `gift-intent-check/Check.csproj`：实际 Core 代码编译并执行 139 条断言通过。
- 覆盖中英否定/疑问/将来/撤回/引用；11种肯定句；相同句子的 AI 代回；所有非held订单状态；二次发送幂等；手机壳/手机冲突；多礼物歧义；8件礼物×中英文面板完整文案；送达及拒绝都不修改 arcOpen。
- `gift-intent-check/report.json` 保留数值结果。`Program.cs` 可审查具体语句。
- 补丁 apply-check 通过。Unity 自带 Roslyn 完整 overlay 编译日志在 `gift-intent-check/compile.log`；静态编译不是 Unity 运行验收。

建议主会话运行检查：晴雯有未回答剧情时，持有手机输入“不要送你手机”，确认仍在家；再输入“送你手机”确认只寄一次；AI代回相同句仍不寄；点击礼物面板的奶茶与电影票中英各一次。
