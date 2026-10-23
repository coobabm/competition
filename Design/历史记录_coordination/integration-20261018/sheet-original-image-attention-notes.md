# 答卷原图与遮挡热力图

补丁：`sheet-original-image-attention.patch`。只在 staging 改动，基线为当前 main 工作内容；需先合入 `teaching-traces/question-traces.patch` 的 teachingImageKeys / teachingPosition 字段。

## 内容

- 答卷每题按 `SheetCard(item).source` 取原题纹理，48×48 缩略图；无可重现图片则不画，不用文字或随机照片顶替。
- 复用 `XgTeachingPictures.ForCard`，补九宫格原照片顺序合成和 `ForNote`（保留 kind/key/keys/position）。缺照片或不支持的棋盘/验证码源返回 null；不会擅自画另一个问题。
- 练页打开的新“看它学”窗口始终使用原题图；不再进入旧 XgTinyLook 为匹配答卷答案而挑选/混合数字替身的路径。旧显微镜工具保留供其他独立教学使用。
- 数字题在现有小型网络上执行原图 49 个位置的 2×2 遮挡（原图为 8×8）；一次原图 + 49 次遮挡，正负差值分别用红/蓝显示。输入与缩略图 Digit 的数字/seed 取模规则完全相同，不根据答卷对错修改输入。
- 感知机、MLP、CNN 使用对应实际演示网络；CNN 家族统一明确显示 CNN 演示，不冒充完整 LeNet/AlexNet 权重。无法匹配的架构/照片/表情等只展示真实原图，说明暂无对应演示分类器。
- 明示“小网络的实算结果，不是聊天模型视觉推理，也不决定答卷对错”。中英双语。
- 无 Update；只有打开窗口或可见时结构变化才计算。模型和结果缓存，结果缓存最多 32 项；隐藏父窗口不计算。不同结构家族输出有实测差异。

## 验证

- `sheet-attention-check/core.log` 和 `desktop.log`：Unity 6000.4.5f1 Roslyn core + desktop 编译零错误（未启动 Unity）。
- `sheet-attention-check/Check.csproj` 可运行：`dotnet sheet-attention-check/bin/Debug/net10.0/Check.dll`。
- 57 条断言通过：每一个遮挡位置与数值、50 次前向调用、原像素不变、缓存复用、MLP/CNN 差异、原题 seed 一致、拒绝照片/不支持架构的伪归因、数值有限。
- 补丁在基线副本 `git apply --check` 通过。
- 尚待 Unity 实机验证：小窗口下 48×48 行排版、图与热力图叠层、切换结构后刷新、切换英文；未宣称已完成视觉验收。

## 边界

- ForNote 已支持追踪元数据，但本单元没有强行把题本全部挤进训练答卷；答卷继续读取已有 SheetCard 来源。
- 未为没有匹配分类器的照片虚构热力图。这是主会话认可的范围，不是漏做。
