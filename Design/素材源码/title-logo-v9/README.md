# 开局名字标 · v9（最终）

## 这是什么

开局页 `CaseMenuHud` 的标题贴图。「我的电脑」+ 显示器图标（屏中「活」、氵第一点青绿发光、青绿光标、右下金点）+「了」（薄荷白、柔和青绿光晕）。标题里不再单独出现「活」字，显示器屏幕半透明、叠在场景上能透出背景。

采用 **简洁版**（无小蜘蛛/蛛丝/电源环/信号线）——这套点缀在 720p 下只有 1–1.5 物理像素，压在《三体》海报上会杂乱闪烁；完整版留在商店物料里用。

## 来源与校验

| 项 | 值 |
|---|---|
| 原始交付 | `发布材料/封面与宣传视频_20261019/TapTap物料_v9_20261020/游戏Logo_简洁版_透明底_1775x1040.png` |
| 交付说明 | 同目录 `上传说明.md`（“游戏内标题建议用这版”） |
| 工程内副本 | `Assets/LingGuangV05/Resources/LingGuangV05/Branding/TitleLogoV9.png` |
| 尺寸 | 1775 × 1040（两行，1.707:1） |
| SHA256 | `4e416f4f7f5e5673b1a34cb9aec3c29d2d756386a9b0370e6aa3fa8124257fb4` |
| 导入设置 | maxTextureSize 4096、nPOTScale 0、alphaIsTransparency 1、无 mipmap、不压缩（与 v5 一致） |
| 生成方 | Claude（游戏宣传片分镜设计 会话），2026-10-20 03:15 落盘 |

## 版式

- 宽 500 参考像素，锚点左上 `(0,1)`，位置 `(65, −98)`：素材左留白 17px → 500/1775 缩放下约 4.8px，墨迹左缘落在 x≈70，与 Eyebrow/副标题对齐。
- 高度 500 × 1040/1775 = 292.96；右缘 565，离图灵海报左缘（≈715）很远；与 v5 的 500×267 版位几乎相同（只高 26px）。
- 副标题随之下移：`−98 − 292.96 − 8 = −398.96`。

## 关联文件

- `Assets/LingGuangV05/CaseMenu/CaseMenuHud.cs`（`TitleLogoLeft/Top/Width/SubheadingGap` 常量 + `BuildChrome`）
- `Assets/LingGuangV05/UI/Tests/Editor/CaseMenuHudTests.cs`
- `Assets/LocalBuild/Editor/TitleMarkPreview.cs`（1920×1080 与 1280×720 离屏截图）
- `Assets/LocalBuild/Editor/NamingDesktopSmoke.cs`（第 12 步改为按 RawImage 名称校验标题已渲染）

v5 资源（`TitleLogoV5.png`，2078×1110）暂时保留以便回滚；v9 玩家端验收通过后可从 Resources 移出，避免多发 382KB。

## 验收

- `CaseMenuHudTests` 通过；`TitleMarkPreview` 在 1920×1080 / 1280×720 实际渲染截图见 `.coordination/integration-20261020/title-v9-preview/`。
- 仍需在 `ComputerCaseMenu` 实机（Player）复核标题右缘与墙面关系，以及开关机淡入淡出。
