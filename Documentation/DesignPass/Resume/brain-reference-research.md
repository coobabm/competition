# 大脑动画网络参考：暂停前的初查记录

## 单项恢复后的采纳记录（2026-10-14）

用户只恢复一项任务后，本轮限于1:084。已查看BrainFacts官方旋转大脑预览的正面与侧面关键帧：参考其清楚的整体轮廓、连续表面和柔和亮暗层次；本条保持现有数字大脑布局，只将训练读数的硬边圆片改为既有XgSoftDraw的柔边光点，并将扫描线改为柔边光带。参考图与来源哈希留在brain-reference-assets，不进入Assets或游戏包。

开源参考采纳的是表达方法：TensorFlow Playground把颜色/粗细绑定实际权重；3d-force-graph的emit-particles把流动绑定指定连线。本轮使用实际EpochStarted与R1/R2/R4瞬时记录，真实父概念中点的亮点过渡到新概念位置。没有复制这些仓库的代码或添加Web/3D依赖，具体实现复用游戏原有绘制工具。医疗大脑的外形不在本条内替换；其余图像候选仍只保留初查状态。

以下保留此前暂停时的初查记录，状态以上述本轮更新为准。

2026-10-14用户新增要求：大脑动画参考网上神经网络相关开源代码；形象设计参考精致的公开版权图像。随后用户要求先改开机动画，改完暂停；本研究停止在初查，不继续制作或集成。

本次实际访问并查看了以下代码和网页文本，尚未逐张查看参考图像，不宣称已完成视觉选型。

| 来源 | 已核对内容 | 可参考方向 |
|---|---|---|
| [TensorFlow Playground](https://github.com/tensorflow/playground) | [LICENSE](https://github.com/tensorflow/playground/blob/master/LICENSE)为Apache-2.0；src/playground.ts中的updateWeightsUI用真实权重决定线宽/颜色，用stroke-dashoffset表示活动 | 大脑强弱和变化来自真实学习数据，避免纯随机装饰 |
| [3d-force-graph](https://github.com/vasturiano/3d-force-graph) | [LICENSE](https://github.com/vasturiano/3d-force-graph/blob/master/LICENSE)为MIT，作者Vasco Asturiano；[emit-particles示例](https://github.com/vasturiano/3d-force-graph/blob/master/example/emit-particles/index.html)沿指定连线发出粒子 | 一次真实事件触发一次定向流动；不能把示例随机树当游戏真实概念图 |
| [Distill：Zoom In](https://distill.pub/2020/circuits/zoom-in/) | 已取得文章页面；[期刊规范](https://distill.pub/journal/)要求文章采用CC Attribution，并链接CC BY 3.0 US | 候选图像参考：局部结构、分层概念和可解释连接；具体图像与各自署名仍需逐张确认 |
| [BrainFacts 3D Brain](https://www.brainfacts.org/3d-brain) | 已取得页面，含Society for Neuroscience版权与署名说明 | 候选形态参考：明确的脑区、空间轮廓；尚未查看动态图，不作为已选定视觉方案 |
| [CajalCerebellum](https://commons.wikimedia.org/wiki/File:CajalCerebellum.jpg) | Wikimedia元数据标记Public domain，历史神经细胞绘图 | 候选分枝形态参考；尚未查看图像 |

目前没有把这些项目的代码、第三方图片或模型放进Assets，也没有改变1:084候选。恢复后只围绕当前条目的训练光波与真实概念生灭选型，保留旧版已接受的特效，按实际采纳情况登记版本、来源及许可证；新六份策划正文仍不提前读取。
