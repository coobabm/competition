# Win7「{名字}的资料」窗口：只读实现方案与核心对接

状态：只读源码核查 + 与 legacy_port_audit 协调；没有生产编辑/编译。数据入口¥50尚待用户答复，本方案不落地节点、价格或权限。

## 推荐最小窗口骨架

保留既有桌面壳：WindowManager + WindowDragger/ResizeAnchor + 原Win7/Aero标题栏，内容为浅色资源管理器。不要用.exe深色XgUi窗口模拟桌面窗口。可复用 ChapterOneInstaller.cs:212 CloneWindow / 224 PrepareWindow / 324–395 ConfigureTaskbar 的模板和注册方法，但这些是Editor私有工具，不是现成可调用的运行时创建API；需单独的幂等安装器或提取小型公共壳构建器。

新增XgFilesWindow控制器绑定已有XingGuangController；自身只管可见窗口/选中/预览/拖放，模拟进度仍由XgSim负责。桌面按钮直接调用其Open，窗口复用DreamOS开/关/最小化/聚焦，不重复写整套状态机。DesktopWindowIndex会发现WindowManager并供既有焦点/保存协同使用。任务栏必须新增AeroTaskbarLayout.Entry，绑定真实TaskbarButton/window/rect/icon/visibility/windowGroup/face；不能仿脑窗把自制按钮写死在托盘右边，易与脑窗/其它托盘拥挤。

ChapterOneDesktopRouter目前只登记ChapterOneDesktopBridge，ConfigureNativeApp又固定接受ChapterOneNativeAppView；自定义文件视图不能假装现成能按files profile路由。最小版直接由持有窗口控制器的桌面图标/完成提醒调用Open；若其它系统需要router.Open("ai-files")，再加明确generic窗口注册适配，不填空NativeAppView或借用home等已有身份。

首次尺寸建议1000×720逻辑像素，最小760×520，具体沿DesktopWindowDefaults.SizeFor按屏幕比例缩放和边界钳制。保存/移动后不反复复位尺寸。这里只是可读性建议，实施后需和.exe两窗并排拖拽截图验证。

## 标题与身份

稳定内部ID ai-files；稳定窗口对象名 AI Files（用于默认尺寸/定位）；可见标题、桌面名、任务栏tooltip都动态取Sim.AiDisplayName + 中文“的资料”/英文“'s files”。玩家改名、切档、切语言后统一刷新，禁止把len或灵光硬编码为后续AI名字。起名前名称遵循主程序现有AiDisplayName返回，不新造另一套默认命名。

## 虚拟目录

根目录始终有“数据包”文件夹和“使用说明.txt”“训练日志.txt”“答题记录.csv”；有检查点资格才出现存档_第N轮.ckpt。左侧树固定“资料/数据包”，顶部面包屑只显示虚拟路径，右侧预览选中文件，底部状态条显示选中数/下载状态。单击预览、双击进入目录；下载中的zip显示进度，完成未装/已装区分，不把已装再次可拖入。

所有文件是游戏存档数据的虚拟呈现，不创建/读用户真实磁盘路径。说明正文说明“下载到这里后拖进练”；训练日志用真实现有轮次/成绩记录；检查点来自S.models与现有FileName规则，不造存档。答题记录csv应从后续统一答题记录模型投影（当前XgAnswerTrace是运行时事件，不等同持久全历史），不能假称旧档所有历史齐全；缺记录显示“这份存档没有记录此项”。

## 与legacy核心接口已对齐

其候选路径：../data-file-core-stage/data-file-core.patch（仍是staging，非已验收生产）。

XgDownload新增fileId（唯一GUID）、nodeId、installed；S.downloadQueue为下载中，S.dataFiles为已下载（含已安装）；另有installedDataBatches/installedDataOffers/installedTeachingNodes与dataFilesVersion。DownloadQueue()副本会带fileId/nodeId。

UI行key只用fileId，不用dataset或列表索引：同dataset多批次必须能分别展示/安装。DataFileReady(fileId)决定能否选为可安装文件；InstallDataFile(fileId) / InstallDataFiles(IEnumerable<string>)是唯一提交安装入口，UI不自行Add样本或解题桌。批量返回成功数后重读各文件installed，清除成功项选择，失败仍保留并给一致原因提示。

旧已安装存档保留收益、不伪造下载历史；文件夹可据owned/installed数量显示“原存档已安装”只读项，不伪造fileId和下载时间。零metadata教学dataset例如chess的sizeMB=0尚无批准大小/体验时长，UI先不写虚假的MB或时间。

核心下一批必须统一CanInstallData等权限检查；目前候选尚未接待确认入口门槛，不能裸接UI发布。UI与核心须同一getter，仅UI挡住不足以防其它入口绕过。所有失败文案中英配对。

## 单选/多选/拖拽

单击替换选择，Ctrl+单击切换；“全选可装的”只收DataFileReady的条目，下载中/已安装不会进入提交集。开始拖动时复制不可变fileId列表，同时记录source Sim身份；换档、关窗口、按Esc取消并销毁ghost，不允许旧档拖拽载荷投到新档。

现有XgDataPackDrag/XgGpuInstallDrop（XgTrainPage.cs:721起）只携带单dataset并要求drag.Page==Page，且目标是GPU区域，不能直接复用来实现跨窗口批量。新轻量XgFilesDrag携带fileId[]；ghost放同一个LCD sourceCanvas顶层而非资料窗口内，CanvasGroup.blocksRaycasts=false；坐标用eventData.position/pressEventCamera，经现有AeroLcdInputSurface映射，不混真实屏幕与电脑内屏幕坐标。

整练页挂统一IDropHandler；AeroLcdInputSurface.cs:211已有ExecuteHierarchy(dropHandler)，多数子控件命中可向上找到练页。需检查原GPU drop handler是否先吞掉新payload：要让旧handler识别并委派同一安装入口，或将其改为仅根处理，不能两个处理器重复安装。整页“松手，装进去”遮罩应raycastTarget=false，防遮罩自己抢drop；放在教/科技/桌面不安装。接收前再验页面可见、当前Sim、权限及每个文件ready，不因Ctrl批选绕过门槛。

## URL下载

URL入口仍在练的数据来源模块，文件夹地址栏仅导航虚拟目录。legacy确认下一批做核心白名单URL解析，同一购买/下载队列，不真联网。UI只提交用户文本到该核心接口，不能通过包含mnist等字符串随便推断，不自行补价/创建下载。未知地址、能力未到、入口未购各给真实原因；网页/书中已见线索不代表自动拥有包。大小写/尾斜杠/协议规范化由核心统一，避免重复网址生成重复购买。

## 任务栏完成提示的重要缺口

AeroTaskbarLayout当前仅以window.isOn显示Entry；关闭资料窗口时，其taskbar按钮被LateUpdate设为0宽/透明。现成框架没有“关闭窗也闪资料”的attention API。最小正确补充是Entry的unread/attention状态及RequestAttention(window)入口，布局以isOn||attention显示并闪烁一个提示图层；点击Open后清除attention。不要假装SetMinimized就是现成通知接口、不要为闪烁自动抢焦点打开窗口。

完成事件只做一次短提示（合并同帧多文件），减少特效时改静态未读点，读状态后停止。若下一批核心无到达事件，用下载列表快照差异在可见/等待完成时低频检查，并仅对真实queue→dataFiles转移通知；旧档恢复dataFiles不能冒充新下载完成。

## 最小验收顺序

1. 单实例、桌面图标/任务栏开关/最小化/恢复/聚焦、改名/中英/切档；任务栏不与脑窗或托盘重叠。
2. 下载只增加虚拟zip，样本和教题桌此时不变；安装后才变化，重复单拖/批拖无二次收益。
3. Ctrl批选和全选只收ready文件；放到按钮/曲线/答卷区域均能整练页接收；拖出窗口/跨档/关闭/Esc取消无漏装。
4. 已有旧档包不回退、不伪造历史；未知URL/能力不足/未购入口均真实拦截，权限必须核心重验。
5. 下载结束任务栏提示一次，关闭文件窗也可重新打开，不抢焦点；隐藏停止重建UI；小窗中英文本/文件预览可读。

待用户回复：数据来源入口门槛/价格；本报告不替用户决定。待核心下一批：统一安装gate、URL白名单契约、零metadata包体验时长。未伪称这些已实现。