# 赌场严格 UI / 磁盘候选

主控串行 Unity 入口：`CasinoScamActualUiRunner.Run`。菜单：`Tools/Local Verification/Casino scam actual UI`。在 EditMode 调用；不经 SidebarLayoutChecks / BookLibrary / 旧 CasinoPlayQa。支持 `-verificationExit` 在回到 EditMode 后退出。

仅新增 `Assets/LocalBuild/Editor/CasinoScamActualUiRunner.cs` 及 `.meta`。复用已交群预设 runner 的独立场景宿主，并使用现有生产 `FilesWindowUiChecks.Click` 的 LCD 射线命中检查 + down/up。脚本要求真实 Application 前台状态，不伪造焦点。浏览器/YY 打开是显式导航夹具，不声称使用系统鼠标/键盘。

## 场景与证据

- 双语 paid / refused / ignored：明确提供已完成 29 把历史总量，最后一注实际点击，等待自然揭晓和风控通知。精确验证本局返还、冻结扣款、保证金、钱包、一次样本和真实事件来源。ignored 只推进显式日历夹具，由生产 CasinoDesktop.Update 自动关闭。
- 每个结局先真实 SaveSlot(6) 保存 frozen；新旧两档都保持 frozen，保留同一个可见 CasinoPage 并等待它重建新档控件，再回放旧 Pay/Quit UnityEvent。这样不会靠“旧档已经关站”或“旧网页已隐藏”制造假通过。它是明确的迟到回调模拟，不是旧按钮物理点击。
- 真正 LoadArchive(6) 恢复 frozen 后操作，然后 closed 再存盘换档恢复。比较钱包、冻结/费用、观察字段、牌局、样本、真实记录、群历史与群消息 receipt。slot6 文件每次复制到本次输出的 disk-evidence，保留相应状态快照。
- 双语已接受黑杰克：实际发牌、扣一次注；之后显式注入“遗留 frozen + active hand”兼容夹具（不是当前自然流程），真实存盘重载；确认风险层没有遮住 Stand，加倍和新注被禁用，实际 LCD Stand 只结算一次，再存盘重载验证。
- 双语关站公开群消息：明确预设 beat1/2 已发标志，观察生产 beat3 的真实 40 秒定时器，提前没有消息，到期仅一条且精确等于公开文案，没有私有 AI 名/金额/训练内容。真实存盘重载 receipt，再等完整 41 秒不得重复。独立跨档用例在旧档 pending 后换成未赌新档，等旧 deadline 过去，断言双方都没有泄漏/虚假 receipt。
- 预计 20 张图（19 LCD + 1 整帧），报告附检查明细。运行上限 600 秒，典型自然等待约 200 秒另加动画/导航。

输出：`LocalVerification/CasinoScamActual/<UTC>/`，包含唯一 Save、DesktopProfile、report.json、截图、disk-evidence 和成功才生成的 passed.txt。运行时只短时开启这个独立目录的存档写入；finally 关闭写入。还原原场景、GameView 尺寸、输入设备/设置、语言、保存根/资料根和 FPS 设置。

报告的 `featureChecks` 等于具名 `checks` 列表数量；`assertionCallsIncludingFrameGuards` 包括逐帧前台/超时/等待条件守卫，不能作为功能用例数引用。

尚未执行 Unity。独立编译通过不等于点击、截图或磁盘流程通过。没有普通玩家渐进进度、实际模型、物理输入、自动保存成功、失败保存重试、普通 Player 或性能结论；支付按钮生产 SaveNow 返回值的体验不在本候选断言范围内。

## 编译与接续

`verification/CasinoUiNarrow.csproj` 针对生产与 Unity 现有程序集，仅编译新 runner。最终日志 `verification/compile-narrow.log` 为零错误/警告。初次 ExtraSamples 返回 double 的夹具声明错误已经纠正，原日志保留。

先核对 manifest 的依赖哈希，再 `git apply --check casino-scam-ui.patch`，应用两份新增文件。由主控唯一 Unity 进程导入并调用入口。若有实际运行失败，应保留 report、failure 截图、Editor.log 后修正；不能将编译或静态断言数当作 Play 通过。
