# 数据包原生预览来源说明

日期：2026-10-07。范围：淘货数据页的 **15 个公开包 + 13 个杂包，共 28 个商品预览**。

## 交付形式与真实性边界

- 这些预览直接显示在商品的 112 × 112 UI 槽位中，不依赖 PNG、外部照片、图集或离线截图。
- 内容来自当前游戏已有的 `XgSim.CreateLabelCard` 或 `XgBoardData.Make(...).source`。数字、验证码、表情和围棋使用同一批现有 `Xg*Graphic`；文本显示实际卡片字段，不用无关图片或空框代替。
- 它们是**游戏的确定性教学样本**，不是 MNIST、CIFAR-10、ImageNet、人民日报或任何商业数据包的原始记录；也不是大模型内部状态或实际学习结果。
- 当前游戏本身将 `imagenet` 的概念板样本映射为验证码图标，将 `news` 映射为诗句下一字任务。预览忠实复用这一实现，**不将它们宣称为真实 ImageNet 图像或人民日报原文**。本轮没有修改这些游戏规则。
- 公开包和杂包的完整 `offer.id` 不同，使用不同确定性种子。预览不额外制造错误标签；杂包的噪声、价格和有效样本量仍由原有玩法决定。
- 开发中的离线 PreviewScene/RenderTexture 截图方案未通过空白检查，已经撤销。失败截图不属于本交付；不存在“28 张已成功生成的 PNG”这一结论。

## 来源与复现入口

| 项目 | 位置或方法 |
|---|---|
| 商品目录与来源分类 | `Assets/LingGuangV05/XingGuang/Core/XgSim.DataSources.cs` 中的 `XgSim.DataOfferCatalog`，仅取 `Public` / `Junk` |
| 原生预览实现 | `Assets/LingGuangV05/DesktopBridge/Taohuo/DataPackPreview.cs` |
| 商品接线 | `Assets/LingGuangV05/DesktopBridge/Taohuo/TaohuoView.Data.cs` 的 `DataRow` |
| 稳定种子 | `DataPackPreview.SeedFor(string offerId)` → `XgBoardData.Seed(offerId, 0, XgBoardData.Use.Train, 20261007)` |
| 样本审计副本 | `DataPackPreview.SampleDescription(string offerId)`，返回完整 `XgCard` JSON 字符串，不暴露可变缓存 |
| 预览渲染 | `DataPackPreview.Paint(RectTransform parent, XgDataOffer offer, TMP_FontAsset font)`，返回是否成功创建预览；缺少必要输入时保留商品原有文字 fallback |
| 现有字体 | 淘货页面已经使用的 TMP 字体参数，不新增字体依赖 |

采样使用独立、内存中的 `XgSim`，`GoldChance = 0`。样本日期固定为 `max(20160601, offer.month * 100 + 1)`，阶段限定为 `1..5`；种子写入这个临时模拟的 `S.rng`。不读取或修改玩家模拟、存档、现实日期、主场景或全局渲染设置。每个 offer 的样本在进程内缓存，重复打开商品页不重新随机抽样。

## 28 项来源清单

`Label` 表示 `XgSim.CreateLabelCard(dataset)`。`Board` 表示 `XgBoardData.Make(dataset, seed, 1, fixedDate).source`。

| offer.id | 来源 | dataset | 采样入口 | 实际预览内容与现有渲染器 |
|---|---|---|---|---|
| `spam.pack` | 公开包 | `spam` | Label | 卡片短信正文；TMP 文本 |
| `mnist.pack` | 公开包 | `mnist` | Label | 手写数字及提问；`XgDigitGraphic` |
| `danmu.pack` | 公开包 | `danmu` | Label | 卡片弹幕正文；TMP 文本 |
| `headline.pack` | 公开包 | `headline` | Label | 卡片标题正文；TMP 文本 |
| `logic.pack` | 公开包 | `logic` | Label | 卡片逻辑题正文；TMP 文本 |
| `cifar.pack` | 公开包 | `cifar` | Label | 卡片验证码对象；`XgCaptchaGraphic`，有第二对象时同时显示 |
| `meme.pack` | 公开包 | `meme` | Label | 卡片表情和原有配文；`XgFaceGraphic` + TMP |
| `poems.pack` | 公开包 | `poems` | Label | 卡片诗句前缀与候选下一字；TMP 文本 |
| `longtext.pack` | 公开包 | `longtext` | Label | 卡片上文及候选句；TMP 文本 |
| `crosssentence.pack` | 公开包 | `crosssentence` | Label | 卡片上下句关联示例；TMP 文本 |
| `review.pack` | 公开包 | `review` | Label | 卡片评论正文；TMP 文本 |
| `go.pack` | 公开包 | `go` | Label | 卡片棋盘、标记棋子及气数问题；`XgGoGraphic` |
| `imagenet.pack` | 公开包 | `imagenet` | Board | 当前游戏的视觉分类教学图标；`XgCaptchaGraphic`，不是 ImageNet 原图 |
| `translate.pack` | 公开包 | `translate` | Label | 卡片原文/候选译文或注意力问题；TMP 文本 |
| `news.pack` | 公开包 | `news` | Board | 当前概念板的下一字教学示例；TMP 文本，不是新闻原文 |
| `spam.junk` | 杂包 | `spam` | Label | 独立种子的短信卡片；TMP 文本 |
| `mnist.junk` | 杂包 | `mnist` | Label | 独立种子的手写数字；`XgDigitGraphic` |
| `danmu.junk` | 杂包 | `danmu` | Label | 独立种子的弹幕卡片；TMP 文本 |
| `headline.junk` | 杂包 | `headline` | Label | 独立种子的标题卡片；TMP 文本 |
| `logic.junk` | 杂包 | `logic` | Label | 独立种子的逻辑题；TMP 文本 |
| `meme.junk` | 杂包 | `meme` | Label | 独立种子的表情与原配文；`XgFaceGraphic` + TMP |
| `cifar.junk` | 杂包 | `cifar` | Label | 独立种子的验证码对象；`XgCaptchaGraphic` |
| `poems.junk` | 杂包 | `poems` | Label | 独立种子的下一字卡片；TMP 文本 |
| `news.junk` | 杂包 | `news` | Board | 独立种子的现有下一字教学示例；TMP 文本，不是微博/新闻原文 |
| `review.junk` | 杂包 | `review` | Label | 独立种子的评论卡片；TMP 文本 |
| `go.junk` | 杂包 | `go` | Label | 独立种子的围棋局面；`XgGoGraphic` |
| `imagenet.junk` | 杂包 | `imagenet` | Board | 独立种子的视觉分类教学图标；`XgCaptchaGraphic`，不是爬虫原图 |
| `translate.junk` | 杂包 | `translate` | Label | 独立种子的翻译教学卡片；TMP 文本 |

若已有卡片包含有效的 `patternA`，预览优先调用游戏已有的 `XgLabelPreviewGraphic.Show(patternA, patternB)`，不重写其位图构造规则。

## 验证边界

- 已完成：源代码接线、目录静态核对、移除失败的 Editor 截图 helper、`git diff --check`。
- **尚未完成：Unity 编译后的 28 项运行验证、最终商品页截图验收。** 当前 Mac 锁屏，主任务统一等待解锁后执行；本文件不把静态来源核对当作运行成功。
- 独立种子不自动证明人眼下的所有图像内容都不同。最终验收应同时检查 28 个 `SeedFor` / `SampleDescription` 输出、实际出现的 Graphic/TMP 元素以及商品缩略图；如果少数有限题库碰撞，应在确认后调整选样而不是改动玩法规则。

本清单说明代码来源和复现方法，不引入额外外部素材许可证。
