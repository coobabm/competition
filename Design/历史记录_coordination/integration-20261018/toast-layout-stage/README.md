# 底部 Owned/已拥有长条最小修正候选

从121838-870实际截图核对，问题对象为XingGuangView.Toast，不是科技详情。正式重复购买Say(Owned)触发OnMessage→3.5秒Toast；Bind切换Sim不清瞬时消息，后续新档继承；Toast位于root底部16–52px，而Pages只留12px，真实遮挡训练与账本。

修正只改XingGuangView和LedgerUiChecks：
- Pages底部固定留60px（提示框顶52+间隔8）；不随任何Toast显示/消失改变页面尺寸，避免答题/训练连续消息导致跳动。
- 切换到不同Sim、Window关闭/最小化、View禁用/不可见都清旧消息文本/计时/可见性。正常Owned/余额不足/其他消息保留。
- 消息文本也不接收射线，不挡真实点击。空消息或非正持续时间明确清除。
- 已有账本UI截图前置保存保持。新增断言：新档/旧档重绑无旧消息；真实重复购买后Owned仍显示；提示上沿不超过Pages、训练按钮和账本下沿；反复显示不改变Pages两边偏移；真实CloseWindow/OpenWindow后无旧提示。

补丁git apply --check通过，未应用/编译/运行Unity。固定提示带让页面可用高度减少48px，必须重跑五模块大小窗几何验收和32张账本截图，人工检查新增空白带/小窗答卷可读性；不以新增断言代替视觉完成。