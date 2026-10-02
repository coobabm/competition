# Spark · 2D Living Neurons

独立表现验证场景，不是正式玩法，不依赖也不修改 Core、EmergenceApp 或主菜单。

## 使用
打开同目录 Spark_NeuronLife_Demo.unity，进入 Play。节点、光丝和 HUD 在 Start 时创建；退出 Play 后这些运行时对象正常消失。

- 点击胞体：启动一次视觉传播（8 个节点，每波每节点最多激活一次）。
- 空格或“暂停活动”：暂停/恢复所有动画与传播。
- “自发活动”：切换周期性自发放电。
- “声音”：开启/关闭轻量合成提示音，默认关闭。

## 实现
- 使用 AI 生成的 SparkNeuron_Cyan_v01.png，而非付费 Pixel Neurons 素材。
- 专用 URP 透明着色器 + 24×24 网格：胞体附近固定、枝梢轻微形变。
- 不同相位的呼吸、局部发光环、胞体光晕。
- 11 条弯曲连接、32 个池化脉冲渲染器。
- 本场景使用专属字体与 Volume Profile；不修改项目级渲染设置。
- 将来接正式逻辑时，只复用表现层；本演示的无向视觉传播不能替代 GDD 的方向、阈值、逐拍和计分规则。

## 验证记录
- Unity 6000.4.5f1 / URP / Metal，场景保存并进入 Play。
- 专用 shader supported=True；未发现该 shader 编译错误。
- SparkNeuronLifeChecks.Run 在 Play 实测 PASS：8 节点、刺激激活、非法节点拒绝、脉冲上限、暂停阻止刺激。
- 实际鼠标点击中央胞体后 ManualStimulations 从 1 增至 2。
- 实际空格暂停，两次读取 AnimationTime 均为 58.32184，TotalActivations 均为 104；随后恢复。
- 传播中与静息截图已检查。单波终止的定时验收受 Editor 停止 Play 干扰，未形成独立 PASS 记录。
- 控制台按 SparkNeuron 过滤为 0 条错误/警告；项目仍有既存 Unity AI NoSubscription 和 MCP 端口重连消息。
- Evidence/Emergence/Spark_NeuronLife_Final.png 为运行截图。
