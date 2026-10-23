# 教页小游戏暂存交付（尚未写入 Assets）

## 合并入口
- `teaching-games.patch`：14 个文件。因主工程原有 CRLF，应用时用 `git apply --ignore-space-change`；同参数 `--check` 已通过。
- `manifest.json`：文件清单。不要复制整个暂存目录到 Assets；其中含编译验证产物。
- 已按 root 要求冻结，不扩展后续任务。必须先恢复 Unity 许可证，再逐项导入/验收。

## 已实现
- 19 个 minigame_packs ID 全部有真实玩法 factory。数据清单实际标记 8 个新玩法（文档文字写9）；未凭空创造第9个。
- 算24：穷举判可解、实际拖选/点选牌和四则运算、每张牌只用一次、可重排原牌；没有输入表达式 eval。
- 扫雷：实际5×5数字与地雷，一格未知，保证至少一个相邻数字可推导。找规律、分类找不同、成语接龙、五子棋任意合法五连。
- 象棋：57题来源保留；运行时读经 PGN 复盘验证的 FEN。7道入门全完成才开放50道进阶；二步杀节点阶段4才开，因此此前一步杀会重复入门供练习。所有合法将杀都接受；二步杀第一步检查所有对手应手，取最难防守，不能只匹配标准答案。升变有选择。Lichess CC0 在题面显示。
- spam/headline/review滑卡，唐诗续字，弹幕三分类，中式英语改错，九宫格真实照片，数字真纹理，表情原生绘制。数字为现有项目模拟笔画数据，不冒充MNIST原始素材。
- arith_mole打正确算式；旧dadishu保持旧玩法。卧底虚拟角色改名，AI动态名字。
- 图片工具共用可读纹理，数字变体限制160张缓存，不会销毁仍在答卷显示的纹理。XgCard与TeachingNote保留图片来源字段。
- 通用选择题可按1–6，作答下一帧切题；选项真实文本存answerText，AI候选存aiAnswer。旧二元题仍保留truth/playerAnswer。

## 已完成验证（不能替代Unity验收）
- 使用 Unity 6000.4.5 自带 Roslyn 和项目 Bee 响应文件，**Core 与 Desktop 分别编译**，保留程序集边界，两者退出0。
- core-compile.log / desktop-compile.log：无错误。现有两处警告为 DayStats 自比较、XgItemsPage.Shown 隐藏继承方法。
- 算24直接提取暂存实现的 Solve 方法，以 PowerShell Add-Type 编译运行6组：1111不可解、3388可解、1234可解、11113可解、5551可解、1112不可解，全部通过。包含需要分数的解。
- 19个数据ID与factory case集合完全一致；57题的 chess-audit.json 均 valid=true（design_inventory先前对真实ChessGame做完整验证）。
- patch --check --ignore-space-change 通过。

## 尚未完成，不能写成已交付
- Unity导入/游戏实测/可运行构建均未进行，本批没有写到main。
- 第4阶段找错字、第5阶段翻译评分、第6阶段挑好答案的新题桌；旧桌须保留原格式，不能覆盖常识桌来冒充。
- 偏置门槛拖动、sigmoid把握排序、数据清洗托盘、复核角标、阶段首次进场动效。
- AI代答已在选择题/滑卡覆盖，但棋盘、扫雷、24点、九宫格等仍需逐个补，不能声称所有题有AI答案。
- 视觉图像与训练真题的对应：mnist与表情卡带原图；九宫格多图还需保存格子快照。数字手写入口有画布，但需补返回本题按钮。
- 24点/扫雷/棋盘等操作类题目的answerText还需完整记录实际操作；YY RecordAsked尚未在RecordTeachingQuestion统一接入（已告知legacy agent接口）。
- 弹幕暂为静态题卡，未做月度梗/滚动；小游戏各自击中、爆炸、棋子落点的美术动效需打磨。
- credits：还需在制作人员名单加 Lichess CC0（目前题面有署名），在 Media/PHOTO_CREDITS.txt 写新增原生表情绘制与数字模拟数据说明；引用的6张照片是现有已署名项目资源。
- 常识题卡/旧玩法长文案、小游戏所有入口的命名和三行科技词条仍需整体收尾。

## 与其他代理接口
- design_inventory答卷重建时需复制 teachingImageKind/teachingImageKey；已通知。
- legacy_port_audit可在RecordTeachingQuestion末尾调用RecordAsked(dataset,本地化题干,answerText为空时使用playerAnswer的对/错,aiAnswer)。不要用正确性结果替代玩家选项。
- 当前已有 main 的manifest/UI19行只是上一批，未接本批factory前新增小游戏不会完整运行。应紧接恢复Unity后合入验收，不能发包。
