# 训练页数据包安装提示

已完成，可由主控执行Unity编译及现有Files Play验收。未运行Unity、未commit。

变更文件：
- Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs：构建提示区。
- Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.Parts.cs：在原模块刷新流程刷新提示与布局。
- Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.DataInstallHint.cs（新增及meta）：提示内容与布局。
- Assets/LocalBuild/Editor/FilesWindowUiChecks.cs：追加真实Files/Train路径断言。

真实路径：XgFilesTrainDrop安装目标就是整个Train root，支持immutable多文件payload逐包InstallDataFile。新提示位于root顶部，不另建动作或购买入口；拖到提示区仍由原root安装处理。成功/失败仍使用原Notify toast，不生成假进度。

显示规则：未购买既有Data Import节点时隐藏且不占高度；有权限后占84px+10px间距，两列内容向下让位，保留原模块科技封印显示。按真实dataFiles状态显示无可安装包/待安装数量/已安装且无待装数量。引导打开“{AI名}的资料 → 训练数据”，把待安装包拖到这里；Ctrl多选批量拖入与真实能力一致。中英文同步，玩家名字在说明文本中禁用richText解析。首次无包不会自动构造数据，旧包不会重复安装。

验证：四份变更C#通过带UNITY_EDITOR的Roslyn语法解析；已跟踪变更通过git diff --check。未触碰XgTrainPage.Books.cs、Core安装路径、模型或存档。

新增可运行断言：FilesWindowUiChecks.Run（现有SidebarLayoutChecks.RunFiles隔离Play入口）中确认未购买隐藏、买后空状态/真实AI名中文资料路径、英文同步、两份fixture文件待安装且未自动安装、原LCD批量drop后提示真实已装数量；仍执行原有撤销门槛拒绝/实际批量安装/重复安装no-op断言。父级应检查截图files-11-real-batch-installed.png中提示与模块布局。验收尚未执行，语法解析不代表Unity语义编译或Play通过。