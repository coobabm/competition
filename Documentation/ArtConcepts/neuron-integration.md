# LingGuang 神经元素材接入（2026-10-02）

## 范围

使用内置 image_gen，根据用户的金色神经元参考图生成五张独立透明 PNG。
生成提示词保存在同目录 neuron-shapes-generation.json。

素材路径：Assets/LingGuang/Resources/NeuronArt/。

| 图形 | 素材 | 当前规则映射 |
|---|---|---|
| 传导 / 纺锤 | conduct.png | Shape.Conduct，单向输出 |
| 锥体 / 三角 | cone.png | Shape.Cone，三向输出 |
| 星形 | star.png | Shape.Instinct，本能的六向输出；不改变原数值 |
| 汇聚 / 半圆 | converge.png | Shape.Converge，多来源计数、单向输出 |
| 回路 / 圆形 | loop.png | 已导入备用；当前没有独立的回路节点枚举，不擅自新增机制 |

用户参考图第二层的网络结构不是单个节点素材，不替换现有回路识别与计分逻辑。

## 实现

- NeuronArt 缓存四类 Sprite，棋盘与候选卡共用。加载缺失时棋盘保留原占位图形。
- 原图向上，棋盘方向 0 为右上；旋转使用 -30 - direction * 60 度。
- 各 PNG 的导入 pivot 对齐各自胞体发光核心，而非机械使用图片中心。
- 棋盘普通节点和放置虚影都使用新 Sprite；充能弧、输出端口、选中框、记忆/海螺标记、闪光与消散保留。
- 保留原金色纹理，端口与充能弧仍使用类型色。虚影半透明，失效节点变暗。
- 不修改 Core、StreamingAssets/GameConfig.json、原场景文件或菜单。
- 纹理 1254×1254，单 Sprite，FullRect，1254 PPU，alpha transparency，Clamp，无 mipmap，不可 CPU 读。
- 常驻图形缓存最多四类，查询均摊 O(1)；每个节点每帧新增 O(1) 颜色/缩放更新，不做纹理生成或文件读取。

## 验证

- 先添加 13 项视觉验收测试：新素材未接入时按预期失败，原 27 项规则测试通过。
- 接入后 EditMode 40/40 通过，包括全部四类素材映射与每类六个方向的虚影旋转。
- 首次绿测请求受导入后的 domain reload 影响未启动；等待编辑器就绪后重新运行通过。
- 实际 Play Mode 已确认棋盘四类新素材、候选卡图标及连锁特效共存。
- qa-four-shapes.png 是临时运行时验证布局，额外放入节点用于同时查看四类；未保存到场景或规则配置。
- gameplay-neurons.png 是正常新局截图。
- 规则与 JSON 使用 before/rule-sha256.txt 校验未变化。
- 控制台已有 Unity AI 插件 NoSubscription 错误，与本次 Sprite/规则无关；未宣称整个项目控制台零错误。

证据：Evidence/NeuronArt/tests-red.json、tests-green.json、runtime.json、gameplay-neurons.png、qa-four-shapes.png。
修改前的 BoardView.cs、Hud.cs 与测试 asmdef 保留在 Evidence/NeuronArt/before/。
