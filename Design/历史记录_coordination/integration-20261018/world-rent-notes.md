# 租金单元（候选，尚未 Unity 验证）
应用顺序：先 legacy-port-main-30fbe6.patch，再 world-rent.patch。world-base 保存 rent patch 基线；patch已做基线可应用检查。

已做：七游戏天一期、第三阶段后预告一天、房东应付账单和橙色转账卡、YY工具栏输入金额、精确打字转账、部分付款、不自动扣租、1/2/3/4日提醒/警告/拉闸/押电脑、补齐恢复供电、准时付款计数、兼容旧欠款净资产、众包账本/状态栏删除自动扣租文案。保留main「确认卖电脑」才执行失败结局的选择。

验证：rent-check/RentCheck.csproj 在 .NET 10 编译全部 Core（含legacy合并的Core），运行真实 ChapterOneSim 检查首期2100、无自动扣租、部分付款、每日升级、补齐复电、防重复付款、早期免租、无效金额不扣款，全通过。Unity的YY UI及信号转发待主agent整合后编译和实际点击验收。

仍需世界任务后续：5期准时的冬至饺子事件、租金涨价事件态度联动；本单元已保存rentOnTimePeriods可读取。资金独立面板旧文案将在全局文案同步处理。现有EconomyTests仍断言旧自动租金，主agent跑检查时需用新行为更新相关断言；不能以旧失败断言判定新功能失败。
