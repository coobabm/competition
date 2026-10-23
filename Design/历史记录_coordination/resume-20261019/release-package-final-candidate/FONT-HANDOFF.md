# 字体通道交接同步清单（只读审查，未动生产）

本次读到的新源：

- `Assets/LingGuangV05/Fonts/UiV4/MiSans-Medium.ttf`，name table为MiSans Medium / Version 4.009。
- `Assets/LingGuangV05/Fonts/UiV4/MiSans-Semibold.ttf`，name table为MiSans Semibold / Version 4.009。
- 两者版权均为2020–2025 Beijing Xiaomi Mobile Software Co.,Ltd.，与既有MiSans族/许可相符。原字体文件和设计不由包装通道更改。

当前实际差异：Resources/UiV4/FONT_CREDITS已列Regular、Medium、Semibold，而StreamingAssets/Licenses/Fonts/FONT_CREDITS仍只列Regular；StreamingAssets/SOURCE_MANIFEST尚无Medium/Semibold条目。

请字体负责人在正式交付时同步：

1. 在 `Assets/StreamingAssets/Licenses/Fonts/FONT_CREDITS.txt` 列出三个实际使用的MiSans字重，保留已有版权、来源和许可范围；与Resources下用于UI显示的声明一致。
2. 在同目录 `SOURCE_MANIFEST.json` 补Medium/Semibold的真实file、release、url、bytes、sha256和fontNames；依据实际官方源，不复用Regular的hash。其它字体已有条目保留。
3. 原 `MiSans-license.pdf` 保留完整且未改写；不因增加字重另造授权或分发裸字体。包内字体按Unity嵌入，许可文件由StreamingAssets保留。
4. 随既定字体交付核对source GUID、TMP各字重与fallback关联；新Medium/Semibold的source GUID当前分别为 `a019dc2b7cc843a1b473e6599331644e`、`f986ce496703486c8a718f8688ee5d04`。包装通道不更改字体设计/weight表/atlas。
5. 上述文字清单完成后再做本轮Player构建，使来源清单与构建文件一致。正式包装候选会检测遗漏字重、陈旧manifest或构建后改动，不能用跳过门禁替代重新构建。

本清单不是新UI要求，也不要求新增购买流程；只是已新增字重的来源/许可材料同步。当前静态审查不运行Unity，不评价字重视觉效果，不修改他人资产。
