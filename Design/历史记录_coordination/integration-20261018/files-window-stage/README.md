# 虚拟资料窗口候选（最新批准 A，未编译）

仅暂存，生产未改。完整新版 `files-window-first-batch.patch` 取代此前版本，不能叠加旧补丁。

## 已确认规则

- 节点 `train.data` 名为「数据导入」/ Data Import，训练曲线前置、普通价 ¥50，由核心候选定义。
- 购买后同时开放「{AI名}的资料」、安装与网址入口，不额外向资料夹收费。
- 本 UI 直接读取 `Sim.CanInstallData`，没有可随意授权的回调，也没有开局免费文件夹规则。
- 实际桌面 runtime 进入第一章才显示图标。换档/回标题/权限消失关闭窗口与拖动。

## 补丁及依赖

1. 先合最新 data-file-core-stage（正式 Has(train.data) gate）及 controller-window-audit 的 RegisterTree(GameObject)。
2. 此补丁含 XgFilesWindow.cs、XgFilesWindow.Records.cs、XgFilesTrainDrop.cs、Editor/ChapterOneInstaller.Files.cs 与 meta；ChapterOneInstaller 改 partial；DesktopWindowDefaults 新增 AI Files 默认 1000×720；XgTrainPage 接整页投放。
3. 编译后在已保存 Main 场景执行 Tools/LingGuang/Install Virtual Files Window。复用原生 Notepad 窗口壳与 AeroTaskbarLayout 真条目；编辑器不自动保存/运行，需检查场景后保存。Awake 显式 RegisterTree，覆盖隐藏工厂。
4. 静态 git apply --check 通过，未做 Unity 编译/运行。父会话负责串行合入和验收。

## 真实资料预览

- 训练数据：实际下载队列、完成及已装文件，身份只用 GUID。没有历史文件时不造出来。
- 训练日志.txt：实际 Runs 的轮数、准确率及 histTrain/histVal 保留曲线采样；明确采样序号不是日期/轮次，不虚构训练发生时间。
- 答题记录.csv：真实 teachingNotebook 的题目、玩家答案、题目标签、实际保留的 AI 回答；CSV 字段转义。
- 对话题本.csv：真实 askedLog 的题库、题目、双方原始答案、实际游戏时钟值。没有保留记录即显示空状态。
- 标注记录目录：逐题记录文本、答案、解释，附同一记录保存的图片描述。
- 训练图片目录：仅已有答题记录的视觉资料，沿用 XgTeachingPictures.ForNote 及真实 seed/key；只在预览点击时取图，保留现有 DeskOpen/TeachingPackDeskOpen 门槛。不浏览整个资源图库、不读取用户磁盘。
- 模型检查点：仅 train.checkpoint 已拥有时出现，逐项读取 S.models 的真实模型、轮次、准确率、日期（有值才显示）、是否保留权重。无记录时不造 ckpt。
- 长文本滚动预览、图片按比例预览；原始记录可能保留答题时语言，不伪造翻译历史。

## 安装与生命周期

整页 XgFilesTrainDrop 接收 GUID payload；旧 GPU 子节点转交。先检查可见 Train 页、源 Sim、payload 有效期、统一核心 gate，再一次消费防重入，逐项 InstallDataFile。每项复核源窗口仍有效、当前存档未变化、页可见、gate 仍可用。重复/跨档/过期包不能安装。安装数提示并刷新 Train。关闭/最小化/Esc/换档取消 ghost。使用新 InputSystem 的 Ctrl/Esc。

## 尚待完成与验收

- URL 白名单实际输入接线由其他候选负责，本补丁不网络访问。
- 关闭窗口时下载完成的任务栏 attention、安装动画、开始菜单入口尚未实现。
- 图标暂沿用既有默认绘制器，尚需文件夹视觉定稿；原生模板 resize disabled，不声称已验收自由缩放。
- 必须 Unity 编译并验收：购买前无图标/购买后可开、老档加载、中英、1280/1920、窗口打开/关/最小化/换档、真实记录空/非空、图片来源与门槛、无checkpoint权不显示模型、长CSV滚动、Ctrl多包含已装/下载中、重复drop、跨档、桌面模拟鼠标坐标。

整合记录建议：数据导入同时解锁资料夹；真实训练/答题/图片/检查点预览及训练整页GUID批量安装接线已暂存，未编译验收，任务栏完成通知/安装动画未完成。
