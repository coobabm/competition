# 全桌面中文默认与语言切换 Implementation Plan

**Goal:** Main 桌面及各应用默认简体中文，可即时切换英文并保存选择。
**Architecture:** 保留 DreamOS LocalizationManager 作为唯一语言状态；复制本地化/主题资产，不改供应商资源。GameText 只做动态展示层门面，原生 selector 驱动状态。为场景未绑定的静态控件建立显式绑定，禁止扫描替换用户内容。
**Tech Stack:** Unity 6000.4.5f1 / uGUI / TMP / DreamOS 原生本地化。
**Spec:** 用户本轮要求；工作范围为复制后的 Assets/LingGuangV05/Scenes/Main.unity 及其各软件视图，不修改原始 2010desktop、其它独立原型。

- [x] 编写并运行缺失默认中文/双语资产/动态接口的失败测试。
- [x] GameText 展示门面与持久化桥；原生设置页语言切换控件。
- [x] 复制原生双语表与主题；补齐全桌面静态中文、字体与日期。
- [x] 游戏应用动态提示、卡片、账单、聊天、剧情内容中英文；保留玩家输入/名字。
- [x] EditMode 回归、Play 下默认中文/双向切换/重新打开窗口/持久化验证与截图。
- [x] 保存 Main，释放 QA 状态；交付准确覆盖边界。

风险：空 localizationKey；prefab 动态内容；保存旧不支持语言；隐藏窗口；持久化用户输入误翻译；字体借用资源销毁；现有同项目并行编辑。
