# 灵光一现 · 电脑入口与 Main

此仓库 `main` 保留电脑机箱入口 `ComputerCaseMenu`、Main 游戏场景及运行依赖，不包含旧版本工程或 `Main-source` 副本。

## 打开

- Unity **6000.4.5f1**。这是 Unity 项目资源，不是可执行程序。
- 克隆时使用 Git LFS；若资源显示为 LFS 指针，执行 `git lfs pull`。
- 用 Unity Hub 打开仓库根目录，等待 Packages 解析及资源导入。
- 正常入口：打开 `Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity`，进入 Play。也可以直接打开 `Assets/LingGuangV05/Scenes/Main.unity`。
- 构建场景顺序已由 Unity 原生 API 配置为 `ComputerCaseMenu` → `Main`。电脑入口会预载真实 Main；不要删除 Main 的构建条目。

## 包含范围

- 电脑机箱入口、物理按钮、风扇音效、开机画面、相机过渡和必要的 Main 启动桥。
- 现有 Main 场景、灵光训练/剧情/对话/赌场/21 点及特效代码；Main 场景文件保持本次上传前的原始内容。
- Main 引用的 DreamOS 桌面、HongmengOS 显示器、材质、动画、图标、字体、本地化与剧情数据。
- 按字符串加载的 Resources、桌面 StreamingAssets 图标、片尾视频，以及原始 `.meta` / GUID。
- 对应 URP、输入、音频等必要 ProjectSettings 和运行时 Packages。
- `Assets/Emergence` 下仅保留 Main 使用的中文字体与字体授权，不含旧游戏代码或场景。
- 第三方资源保留原授权说明；并非统一按 MIT 重新授权。

## 不包含

- 大模型权重、模型缓存、本机 llama 推理程序。无模型时保留项目已有的预设回复路径；本机若运行兼容服务，LocalLlm 代码仍可能连接它。
- Library / Temp / Logs、个人存档、IDE 设置、开发代理插件、旧原型、其他场景、测试和编辑器 QA 工具。

## 验证边界

- 资源根据 Unity 的 Main 依赖查询、运行时 Resources/StreamingAssets 和静态脚本依赖整理，不是直接复制全部 Assets。
- 本次上传的 5 个 Player 代码程序集静态编译通过；引用源工程安装的 Unity/包程序集。
- 临时上传工程已由 Unity 6000.4.5f1 完成导入、编译、电脑场景打开和引用检查：缺失脚本为 0，入口控制器的必要引用完整。
- 尚未在这份临时上传工程执行完整的电脑入口 → Main Play 流程；不将导入或静态检查等同于完整运行认证。
- 原工程已有的 21 个无法解析 GUID（含旧动画绑定、少量图标及渲染配置引用）原样保留，没有在上传时擅自修资源；说明见 `SNAPSHOT.json`。
- `FILES.sha256` 可校验下载后的实际文件；二进制资源需先拉取 Git LFS 内容。

旧文件从当前分支移除，但 Git 历史保留，仍可回溯。
