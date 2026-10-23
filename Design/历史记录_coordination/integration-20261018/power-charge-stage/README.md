# 取消教学免电费：独立暂存增量

用户旧规则“电力不搞教学期免费，正常收”仍有效，主会话明确允许本增量。补丁只改 XingGuangController 和 XgGuide，未编译/未应用；git apply --check通过。

审计：power 是 XgSim.Features 的阶段1开放键，不是玩家购买供电能力的科技。此前 Controller.BindSave 与每帧Update把核心 PowerCostsEnabled 重写成 SystemUnlocked(power)，使电费可能被教学门槛关闭。现改为true，功能可见性仍走原 SystemUnlocked。ChapterOneSim 本来默认收费true；不改电价、阶梯、训练耗电、谷电优惠、账目/欠费处理。

XgGuide 的真实欠费/跳闸/过载提示去掉教学阶段前置，不会收费后把必要处置提示藏起来。实际Power页面属于独立电脑软件；不因未买记账本阻止缴费/复位。

RentWaived只保留原房租教学宽限规则。审阅核心 BillTraining 可见其按PowerCostsEnabled直接调用真实 Bill 扣钱，并不受RentWaived；待机电费在日结走原扣款/欠费机制。未把房租提早、未改变宽带/维修/空调收费时机。

主会话验证：与账本包一起编译；LedgerAccessChecks 真实核心训练报价/扣款断言；实际新档第一轮Host报价与扣费；低余额和欠费时Power引导仍可达；重新加载和一帧Update后PowerCostsEnabled不会被覆盖为false。当前只有静态审计，未宣称实机通过。