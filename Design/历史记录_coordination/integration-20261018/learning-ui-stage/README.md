# C3 真实Main学习记录UI检查候选

状态：仅coordination，新检查源码与共享runner hunk已准备；未编译/运行Unity、未修改生产、未提交git。直接复用现有SidebarLayoutChecks隔离Main、真实经济宿主、Aero LCD输入和QaDesktopCapture。

应用learning-ui.patch，然后协调应用runner.patch（共享SidebarLayoutChecks；不要用整文件覆盖其它新分支）。入口SidebarLayoutChecks.RunLearning，内部调用LearningRecordsUiChecks.Frames(ctl,locale,FilesWindowUiChecks.Click,capture)。**使用FilesWindowUiChecks.Click**，它接受XgFilesRow的IPointerClickHandler；普通ClickSidebar额外要求IPointerDownHandler，会错误拒绝文件行。runner上限300秒，每页都有屏幕截图。

夹具要求useDiskSave=false、tickEnabled=false、TestMode=false且真实文件/YY组件已安装。每种语言新建120条学习日志+120个教学快照，119条2016三天时间、1条未知旧时间且at非零，实际AI字符串与标签不同；首条可见记录含原图key数组/position。实际保存覆盖是root已有大档检查，本包不会写玩家存档。

覆盖：资料notes每页50/50/20，逐行滚入视口并经真实LCD点击，核对预览答案、实际日期、未知日期、图片同引用；answers和asked CSV各三页，校验顺序/全量可达/50条TMP上限/前后页禁用；换档后notes/CSV第一页与新内容；YY题本三页、旧页搜索、真实AI名、含图行禁用和直接文本复考API拒绝、仅图搜索下随机不发送；两档相同count/serial情况下YY归零重绘，不能由clamp或close/reopen掩盖。没有点击可发送的纯文字题，不依赖LLM或手造回复。

控制说明：页面/行/工具/随机动作均通过真实LCD click委托；ScrollRect位置和TMP_InputField.text由夹具确定性设置（触发真实onValueChanged）。不声称验证物理滚轮或逐键输入。截图在两次布局帧之后采集；最终仍需root看图。源数据提示明确标fixture，不能冒充自然游玩历史。

finally恢复原house JSON、controller Sim、YYState引用、talk绑定/接口、组件enabled和窗口开闭，不关闭用户真实会话；只可在上面的隔离fixture运行。已有YY正在Thinking/NightPlaying时检查拒绝启动。

静态发现已报告root：原YY题本questionEn未显示、同count/serial换档owner缺失。root已告知生产修复，候选直接验证修复后的目标行为，不修生产。FilesWindow.cs/.Records.cs当前惰性CSV分页完全不覆盖。
