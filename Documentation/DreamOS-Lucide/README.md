# DreamOS → Lucide 图标试用

## 范围

- 项目：`/Users/kk/competition`。2026-10-02 导入 `lucide-static@1.50.0`，包 SHA-512 已与 npm 元数据校验一致。
- 替换 328 个 PNG，使用 99 个不同的 Lucide SVG。覆盖 DreamOS 的 32x/64x/128x/256x 通用及应用图标，以及 14 个设置项目图标。
- 保留原文件路径、像素尺寸和全部 `.meta`。现有场景、Prefab、UI 渐变色、布局、交互和 Sprite GUID/fileID 不变。
- 保留 44 个原 PNG：品牌/社交图标、Cori 标志、DreamOS 标志、二维码、音频消息特殊素材、拆分动画及布局辅助素材。具体清单见 `manifest.json` 的 `preserved`。
- 因为使用原路径替换，所有引用这些 DreamOS Sprite 的场景都会采用新图标，而非仅当前桌面。Asset Store 升级可能覆盖这次替换。

## 图形规范与许可证

- 官方 SVG 渲染为白色透明 PNG；24×24 坐标系、2 单位描边。现有 Unity Image tint / 渐变组件继续负责上色。
- Heart/Star/Pin/Play 的 Filled 版本保留白色填充，以区分选中与未选中状态。
- SVG 源文件见 `SourceSVG/`；完整 ISC 和适用的 Feather MIT 声明见 `LICENSE.txt`。分发这些图标时保留许可证。
- 使用离线 `@resvg/resvg-js` 渲染，没有向 Unity 添加 npm、SVG 插件、脚本或运行时依赖。
- 此前 Unity 里已存在 TMP_InputField.OnDisable / DestroyImmediate 错误，不属于本次图标变更。

## 回退

双击本目录 `restore-originals.command`，随后回到 Unity 刷新资源。

- 先校验所有原文件备份、当前 Lucide PNG 与 `.meta` 的 SHA-256，全部通过后才还原 PNG。
- 如果你后来又改过图标或其导入配置，脚本会拒绝覆盖，不会强制回退。
- 只还原 PNG，不重写 `.meta`，不触碰场景、Prefab、脚本或项目配置。
- 仅检查而不修改：运行 `./restore-originals.command --check`。

## 验证

- 328 个 PNG 均保持原始尺寸，有可见图形和透明背景。
- Unity 重导入后，328/328 Sprite 可加载；GUID、local fileID、导入尺寸全部与导入前相同。
- 646 个场景 Image 图标引用保持；1850 个受保护的 `.meta` / 场景 / Prefab / 资产配置 / C# 文件哈希不变。
- 备份及回退预检通过。
- 运行时视觉检查见补充的 `verification.json` 与截图。

`manifest.json` 记录每一项源 SVG 对应关系、变更前后哈希、渲染器版本以及原样保留的文件。

### 本轮运行验证结果

- GPU 回读检查：328/328 导入纹理都有有效图形与透明背景，无空白/全不透明异常。
- 在隔离副本中实际还原 328 个原 PNG，并验证后续用户修改会阻止回退、不会部分覆盖。
- Play 模式锁屏已显示 Lucide 图标；运行检查时 Console 为 0 error / 12 warning（现存告警未清理）。
- 桌面完整视觉检查尚未完成：当前 DreamOS 需要密码登录，已请用户在 Unity 中正常登录，未读取或绕过密码。
- 操作期间活动场景由 `Assets/Scenes/Desktop.unity` 变成资源包的 `Scenes/Desktop.unity`；本次未保存或重写任何场景。

### 后续验证：取消开机登录后

用户随后要求取消开机密码。已清空本地 DreamOS 账户的 UserPassword，并在资源包 Desktop 场景的 UserManager 上启用 disableLockScreen；没有修改原 Prefab 或自定义 Desktop 场景。重新进入 Play 后自动进入桌面，Lucide 桌面和任务栏图标已实际检查，见 desktop-after.png。此前“桌面待登录”状态已解除。现有 TMP_InputField.OnDisable / DestroyImmediate 错误仍存在，未在此任务中修改其代码。
