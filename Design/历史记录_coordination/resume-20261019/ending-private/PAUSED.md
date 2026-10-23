# 用户要求暂停：最终保存状态

截至2026-10-19，本agent已停止开发/验证，未改生产，未运行Unity/Player/构建。

唯一最新候选补丁：`patch/ending-private-paused.patch`
SHA256：`fb5de21daf3406359b5fcaff0f454b6e07102651639295bd34217880aaf125d1`
文件清单：`manifest-paused.json`（15项，5个既有源码修改、5个新源码及各自meta）。

`patch/ending-private.patch`和`manifest.json`是此前快照，**不是最新候选，不要应用旧包再叠加最新包**。最新包完整替代旧包。最新包尚未重新做git apply --check；旧快照在当时生产通过过，不能移用为最新包证据。最新包由当前baseline/candidate统一生成，可供接手者在生产执行git apply --check后决定合入；不得整文件覆盖。

README.md描述主体设计与默认A；以下信息优先于README的旧数量/状态：核心NUnit现在20个展开用例，均未运行。新增`Assets/LocalBuild/Editor/EndingPrivateActualUiChecks.cs`是可组合实际UI检查候选，无独立启动runner、未编译、未运行。入口为`EndingPrivateActualUiChecks.Frames(ctl, language, click, capture)`，需要接手者在新的隔离LocalVerification目录、真实Main/LCD和已隔离模型/独立剧情驱动的串行runner内调用；使用专用slot6，不能调用在普通存档。不得把夹具联系人/阶段/SI信/路线/长R3文本称为自然游戏进程或真实模型输出。

最后已保存的修正：

- R1中文“并试着运行过”、英文“tried running it”，拔电源早退不会声称完整一晚。
- 新入口必须yyLinkSource==tieba_dm_accept；legacy_existing保留普通聊天兼容但不获得新私人披露入口。
- 核心tests不依赖默认Assembly-CSharp中的YYState，YY宿主序列化/裁剪检查已放到LocalBuild Editor候选。
- 渲染时捕获私人预览动作类别；语言/同owner状态过期时取消，不把“可选…”按钮标签回退发送成普通聊天。

实际UI候选覆盖：双语960×600，真实按钮预览/取消/发送；原生关窗和换档取消；同帧语言切换旧按钮；真实SaveSlot/LoadArchive；生产邮件桥展示LP4与无披露9段；真实LCD滚轮长R3预览取消/确认。尚需编译审查、串行跑完以及修正任何真实失败，不承诺当前候选测试全部可运行。旧公开信资源、通关事务未改。

遗留：SeesExe/额外坦白缺生产实现及可沿用批准原句，未恢复；仍需严格知情设计，不可把披露收据升级成任意读取私有训练数据的权限。真实路线/自然全流程/最终Player仍未验证。
