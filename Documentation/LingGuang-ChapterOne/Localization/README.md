# Main 桌面语言切换

## 使用
- 默认简体中文，不跟随操作系统语言。支持简体中文 / English。
- 任务栏右侧原生按钮 **中 / EN**；或 **设置 → 辅助功能 → 语言**。
- 选择保存至 `LingGuangV05.Desktop.Language`。不修改 DreamOS 原有 System.json 语言项；失效/不支持的值回退中文。
- 目标：`Assets/LingGuangV05/Scenes/Main.unity`（先前复制的 2010desktop）。原始 2010desktop 场景保持不变。

## 覆盖及数据边界
- 原生 7 表 / 232 条，项目自有双语副本；原生设置、桌面、菜单和控件保留原组件。
- 游戏的邮件、灵光、寻宝、家庭、YY 动态文案；卡片/反馈、账单/硬件/考试、NPC 预设历史、剧情旁白/标题/过场。
- 语言切换不推进模拟/剧情，不重启正在显示的旁白；聊天正文、草稿、AI 名字不修改。
- 原生输入、联系人自定义名称、歌曲/文件/播放列表名、笔记正文等视为内容而非 UI，不进行机器翻译或改写。原生内置示例笔记沿用其初始化时的翻译，后续按文档内容保留。
- 品牌、设备型号、单位、代码/命令原样保留；英文应用名 Mail / Lingguang.exe / Xunbao / Home / YY。
- 中文字体为项目内 Noto CJK 后备字体；复制的主字体拥有自己的材质/纹理，避免释放原生共享资源。

## 实现
- DreamOS LocalizationManager 仍是唯一语言状态。DesktopLanguageController 提前初始化并提供保存隔离；GameText 只负责展示层翻译与事件。
- DesktopLocalizedText 仅绑定作者静态标签，显式排除输入及原生 payload 字段，并在其他 presenter 接管时停止写入。
- 动态消息和日期采用专用 presenter/adapter；不逐帧扫描场景替换字符串。
- GameText 精确查询 O(1)；模板查找最坏 O(P × L)，使用前后缀筛选、20ms regex 上限和每种语言最多512项缓存。空间 O(目录大小 + 模板数 + 512)。
- 不新增 UPM 包；不修改供应商源码/语言表/预制体；不改 Core 规则或已有存档结构。

## 验证证据
- `Evidence/Localization/editmode-all-tests.json`：当前整个项目 EditMode **261/261**，不是仅语言测试。
- `Evidence/Localization/play-qa.json`：**76/76**，真实 Main Play 下跨应用双向切换、原生按钮/设置选择器、草稿/正文/名字、隐藏窗口、剧情打字进度。
- `Evidence/Localization/persistence-qa.json`：退出 Play 后再次进入，英文选择被恢复。
- `Evidence/Localization/payload-ownership-qa.json` / `monitor-qa.json`：首次显示播放列表名不被覆盖，OSD语言切换不改亮度。
- `Evidence/Localization/source-before.sha256`：原始场景保留校验。
- `Evidence/Localization/gameplay-catalog-dotnet-check.txt`：777种生成文案的英文覆盖辅助检查；与 Unity 运行证据区分。

验证边界：Editor Play 与 EditMode；未做独立 Player 构建、跨平台字体/系统测试，也未逐条执行所有供应商示例应用业务。保留品牌/用户内容不属于漏译。
