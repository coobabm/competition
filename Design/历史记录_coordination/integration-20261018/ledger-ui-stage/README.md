# 可嵌入现有 Sidebar fixture 的账本真实 UI 检查

未生产写入、未编译、未运行Unity。三个独立补丁：

- ledger-ui-checks.patch：新增 LedgerUiChecks.cs，不修改Sidebar任何phase。
- training-price-visibility.patch：RefreshBill移除!home.EconomyActive条件。真实报价不再被房租是否启动隐藏；保留训练开放和Host判断。
- accounts-language-refresh.patch：资金账本按钮保存为fundsBook字段，每次RefreshLedger刷新文字；英文短名Accounts。已有Side最新改动作为基准，不整文件替换。

检查依赖上述两个窄修。账本UI helper在已有同一个窗口切换中英、960×600/1400×900，覆盖未买、实际封印LCD点击到对应科技详情、科技余额例外、低余额、电费欠付、房租断电风险、正式BuyNode实付和重复购买、旧序列化档迁移绑定、再切新未买档。资金账本按钮断言精确本地化文字与宽高，其他显示文字检验overflow和布局尺寸。

共32张实际LCD截图，capture回调总在当前状态断言前执行。账户门槛状态重绑使用独立内存XgSim+真实View.Bind，明确不是磁盘存档读写验证。购买使用正式BuyNode和现有真实XingGuangHost，不用假Host；夹具准备的资金/进度不冒称自然游玩。原Controller暂停模拟以避免截图期间自动收入或事件干扰，view继续渲染。每轮隔离stage-enter quiz；保留原场景上下文和父级已做的StoryFocus隔离。

## 接入建议（由主会话编辑，不覆盖crawler phase6）

添加IEnumerator ledgerCheck字段。保留现有phase6的crawler推进；在crawler结束并Dispose之后，先创建：

ledgerCheck = LedgerUiChecks.Frames(ctl,
    en => { language = en ? GameText.English : "zh-CN"; GameText.PublishChanged(); },
    ClickSidebar,
    filename => { Check(QaDesktopCapture.Save(Path.Combine(Root, filename)), "ledger LCD capture " + filename); report.screenshots.Add(filename); Save(); });
phase=7; return;

新增phase7：每实际渲染帧调用一次ledgerCheck.MoveNext()；完成后Dispose并置null，再走原通过/退出代码。外层原Time.frameCount门槛保留。Finish异常路径也应Dispose ledgerCheck，保证helper的finally恢复原Sim、house状态、窗口尺寸、locale和controller.enabled。

helper要求runtime.useDiskSave=false、tickEnabled=false且TestMode=false，沿用父fixture已建好的安全隔离和真实Host。没有自建场景或对真实用户档的操作。

## 未验证边界

目前所有代码只做静态审阅。实际截图可读性/字体缺字、首次价格显示、真实pointer路由、首次旧档门槛、弹出提示是否遮挡仍以主会话运行结果为准。最后的FundsPanel完整弹窗未打开，检查聚焦账本入口与顶栏/练页；Core LedgerAccessChecks已覆盖经济和存档字段。