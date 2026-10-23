# 群聊剩余十图：原创情景插画候选

本包补齐剩余 10 个场景资源位，全部为原创程序绘制的叙事插画。**这不是十张手机照片，也不是 ImageGen/API 生成成果。** 已有三张许可照片不替换、不重复计数；五张终局图完全不计入本包。若接受授权的插画替代方案，13 个场景槽位可成为“3 张许可照片 + 10 张原创插画”；原定照片风格的生成仍未实现。

本机内建 image_gen 工具不可用，OPENAI_API_KEY 环境变量存在性为 False。仅检查存在性，没有读取、打印或保存任何密钥，没有调用不存在的模型、图像 API 或下载外部图片。

## 审阅文件

- `contact-sheet.jpg`：十图联系表。
- `bubble-size-proof.jpg`：约 240 px 聊天缩略尺寸的静态审看图，**不是 Unity 运行截图**。
- `candidates/*.jpg`：逐张原尺寸候选。毕业图 600×800，其余 800×600；JPEG RGB，总计 900,229 bytes（约 879 KiB）。
- `manifest.json`：逐张题材、原配文、尺寸、SHA-256、来源方法、许可范围、差异和 Unity GUID。
- `ORIGINAL_ART_CREDITS.txt`：完整原创来源和使用说明。
- `source/draw_chat_scenes.py`：原创绘图源码；仅依赖 Pillow，可重现这十张图。没有外部素材输入。
- `patch/Assets/.../Photos/`：十张可导入 JPG 和十份独立 `.meta`。
- `patch/scene-notice-and-credits.diff`：只改 `YYChatView.IsSceneArt` 的十项名单、短说明文案，并追加两份 credits。

## 审看结果

已逐张审看原尺寸图，另检查缩略尺寸题材辨识。修正过馅饼咬口露馅和背景切口、断桥人群透视位置、网吧投影的游戏状态条、黎明屏幕的策略地图格子。没有可辨认真人面孔、品牌字样、水印、影视剧照或商业游戏截图。

十图分别呈现：五名毕业生背对抛帽；手持咬口绿豆馅饼与红砖街排队；湖桥拥挤人群和遮阳伞；3 点电视球场、饮品与花生；夜间空办公室的双屏工位；网吧大屏前举手欢呼；无标志亮黑手机背面与夜队；晨光策略地图与零食；猫卧快递堆顶；两张影院票根与爆米花。

画面为明显的平面插画风格。鼓浪屿、断桥及手机均采用概括造型，不是精确地标复原或认证产品照片。维持剧情配文，通过现有图片气泡显示“原创情景插画 · 非实拍 / Original illustration · not a photo”。原 `chat_geyou` 的影视截图说明保持不变。

## 最小导入步骤（由集成方执行）

1. 先看联系表；生产工程是 `C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。本子任务没有修改该工程。
2. 在生产工程运行 `git apply --check <本包绝对路径>/patch/scene-notice-and-credits.diff`。2026-10-19 已只读检查通过。若主线发生同文件修改，以 manifest 中 base SHA-256 核对并合并相同三个小改动，不覆盖新代码。
3. 应用该 diff。它只影响 `Assets/LingGuangV05/YY2016/YYChatView.cs`、`Media/PHOTO_CREDITS.txt`、`Media/GROUP_CHAT_PHOTO_CREDITS.txt`。
4. 将 `patch/Assets/.../Photos/` 下的 10 JPG + 10 `.meta` 复制到生产工程同相对路径。若任何目标已存在，停止并核对，勿覆盖。所有 GroupPhotos 中英映射已存在，不需新增剧情事件或资源键。
5. 允许 Unity 正常导入后，由有权启动 Unity 的集成执行器检查中英群聊：十图正常显示、毕业竖图保持纵横比、图片短说明清晰且不遮原配文、原葛优图说明和三张许可照片正常。再检查 Player 构建资源可加载。

本子任务未启动 Unity、Player 或任何模型，没有声称编译、运行显示或 Player 验收已通过。

## 已完成的静态验证

- `git apply --check` 通过。
- 十图可被 Pillow 解码，尺寸和 RGB 模式正确；候选与 overlay 图 SHA-256 全部一致。
- 十个 GUID 相互唯一，并与生产 `Assets/**/*.meta` 不冲突。
- 十个生产目标图当前均不存在，不会替换三张已许可图。
- 十个键的二十条中英 GroupPhotos 映射均已存在。
- 原创插画包不引入外部 CC/署名/传染许可义务；已有三图许可及终局图署名原样保留。原图和改图使用范围见 credits；不宣称法律上的独占权。

源脚本重现：在本目录执行 `python source/draw_chat_scenes.py`，再执行 `python source/package_candidates.py`。打包脚本只读取生产文件，在本目录写候选和 patch，不会写入生产工程。
