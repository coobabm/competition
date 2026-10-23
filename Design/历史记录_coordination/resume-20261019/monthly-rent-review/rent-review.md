# 月租改动独立只读审阅

日期：2026-10-19。审阅当前 production 的 ManualRent/Bills/Models、YY Rent 实现、EconomyTests，并追踪 Runtime 与 BankruptcyEnding 的事件桥。未修改源码，未启动 shell、Unity、模型或测试进程；主控正在运行的经济测试不在本审阅执行范围。

## 发现：P2 首帧出账可永久遗漏 YY 通知和账单卡

涉及：
- Core/ChapterOneSim.ManualRent.cs:45–65，首次启租后立即 SettleMonthlyRent，先推进 rentBilledMonth，再发 rent.due。
- Runtime/ChapterOneRuntime.cs:11 的 DefaultExecutionOrder(-1500)，以及 Update 中的 Sim.Tick。
- DesktopBridge/Story/BankruptcyEnding.cs:45–52，仅在自己的第一次 Update 中订阅 runtime.Signal；该组件没有提前执行标记。
- DesktopBridge/Story/BankruptcyEnding.cs:84–89，YY文字和ReceiveRentRequest的唯一房租事件桥；缺桌面时也直接return。

触发条件：加载已经到收费日期且租金未豁免、rentSystemStarted=false 的旧档，或者加载需要补发月账的v2档。Runtime第一次Update先运行，Tick→SettleCalendar立即产生首账/补账并推进月份。BankruptcyEnding尚未在其Update订阅事件，YY不会收到房东文字或橙色账单卡。之后同月不重发，故通知不会自行恢复；欠租与逾期计时仍存在，玩家可能先看到催缴而未见首张账单。

普通从新游戏玩到第三阶段时，订阅早已就绪，正常链条有效。本轮v1且rentSystemStarted=true的迁移会跳过当月出账，也不触发此边界。

建议：让房租通知拥有可靠的初始化/重放边界。可以在Runtime允许首次计费前完成事件绑定和YY初始化；更稳妥的是把待投递账单持久化，并按账单月份去重补投递。仅把订阅移到Awake但继续在desk/YY未就绪时丢弃事件仍不够。不要为补通知再次增加rentOutstanding。

这是静态可达路径审阅结果，未运行UI复现。当前经济测试直接订阅Sim.Signal，不安装BankruptcyEnding/YY，因此其通过不能排除此缺口。

## 指定边界的核对结果

| 边界 | 代码结论 |
|---|---|
| 新档首张账单 | rentSystemStarted为false且rentBilledMonth为0，首次结束免租时从当前月开始；不会补收免租月份；首账增加欠款而非自动扣钱包。正常游戏过程中进入收费阶段时事件监听已经存在。 |
| v1当前档不重发 | MigrateManualRent在已有rentSystemStarted时把rentBilledMonth置为当前年月，保留欠款/已付款/逾期时钟，并设置版本2；同月不再出账，下一月才新增。未发现重复收账。 |
| 跨月逐月价格 | SettleMonthlyRent循环从上次月份的下个月至当前月，每次MonthlyRentFor(due)；10月前2100，10月起2450，且保留每期附加额与当前租金系数。未发现跳过11月或全部按最终月价格计费的错误。 |
| 逾期时钟 | 月份跳跃本身不推进rentClockDays；无旧债时首笔新账重设rentOldestDueDays，有旧债时保留最早时钟；明确转账付清才清debtDays与断电。未发现本轮导致即时断电或付清后新账沿用旧倒计时的错误。 |
| YY实际通知 | 正常运行时rent.due同步经BankruptcyEnding向YY发文字（包括未读/通知）再加Transfer卡。卡保存月份与当时累计欠款，最后一张有效卡显示当前总欠款。上述首帧遗漏是本次唯一报告的真实缺口。 |

## 测试覆盖与限制

已读当前EconomyTests：新增/更新用例覆盖首次月账、同月不重复、重载、跨月循环、v1零债与有债迁移、按游戏时长催缴、部分/全额转账及下一月重新计时。未执行这些测试，也不声明测试通过。

CalendarJumpBillsEachMonthAtItsOwnPriceWithoutInstantForeclosure从9月30日已付租跳到12月，核对10/11/12各2450；代码本身按due日期计价。补一例8月跳11月可直接覆盖同一循环内2100→2450，但这是测试覆盖建议，不是另一个已发现bug。

Models新增rentBilledMonth字段为加性迁移，保留旧rentNextDueDays未参与新排程，未发现其造成运行错误。旧注释仍含按日/按周描述，本报告按要求不把文案陈旧列为bug。

路径基目录：C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016/Assets/LingGuangV05/。
