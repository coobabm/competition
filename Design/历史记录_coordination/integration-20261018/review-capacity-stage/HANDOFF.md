# label.parallel 复核扩容（暂存）

主代理已确认：保留 ID、3000 价格、label.brain 前置、老档权益；收益改为复核容量 20→60，名称复核扩容。

现存瓶颈：ReviewFull 到20会暂停 AutoRate、FillJudgmentBuffer、TryTakeBrainTicket、Route、AutoPlan。扩容让玩家离开题桌聊天时仍能积存更多不确定题目；不提高准确率、单题报酬或模型并发。

## 应用顺序
1. 应用本目录 review-capacity.patch（6文件，Core ReviewLimit及修复/噪声复核；XgBrain；两个计数页面；通俗解释）。
2. 应用 ../tech-copy-stage/tech-three-line.patch（2文件），现在名称、效果、详情以及旧节点徽标均为复核容量20→60。
3. Unity 编译、窗口计数和实际模型验证。当前只做独立编译/纯规则测试，没有开 Unity。

ReviewCapacity=20 常量保留作默认及旧测试接口；全部真实容量控制用 ReviewLimit。拥有旧 label.parallel 立即享60；读取超容量无权益存档仍将溢出题移入prefetch，保留题目。
XgBrain 最多一个pending，购买不再扩展本地服务槽位。LocalLlm.Pump 未修改；原 active.Count/NativeCallsInFlight 双门禁与聊天优先仍在。模型上下文缓存槽与同时请求不同，未把3个角色缓存槽错误删成单槽。

## 验证
- Core、完整 UI 含三行词条独立 Roslyn 编译，无error。
- ../review-capacity-check/result.log：77项确定性断言通过：真实购买金额/前置、20满暂停、购后第21题继续、60满暂停、第61题拒绝、持有旧权益重载60题及顺序、无权益超容量不丢题、噪声复核剩余5槽、聊天等待/执行/输入阻止背景判断。
- ../tech-copy-check/coverage.log：184节点三行/长度/中英/科学详情规则0错误。
- 全局单请求通过既有 LocalLlm.Pump 代码未改的检查确认；没有运行真实模型并发录制，不冒称端到端验证。
