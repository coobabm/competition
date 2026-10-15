# VGG 可采用文案

## 中文

名称：VGG-16

解释：全用小窗口，叠得很深，吃显存。

效果：购买后加入视觉结构候选，无需手动装备；系统在训练轮次之间按数据、已解锁尺寸和显存选用，不保证立即切换或提分。选用时基础结构限额为 16 层（相关已购技术可提高限额），不等于立即获得 16 层。参数量＝层数 × 每层宽度² × 9，显存随尺寸增加；参数不足造成的误差分量采用 ×0.55 系数，不是准确率加成。常规训练处理量系数 ×0.8，与优化器及研究等倍率相乘；最终每轮卡数还受取整和上下限影响。超过 12 层，每多一层，在成绩上限和单题答对率各自的计算中扣 3 个百分点；最终结果仍受各自下限约束，不能合并成固定扣分。架构之间不叠加，成绩要训练后评估。

## English

Name: VGG-16

Explanation: Small windows only, stacked deep. Eats memory.

Effect: Buying adds VGG to the vision architecture pool; no manual equip is needed. Between epochs, the system selects for the data, unlocked sizes and VRAM; an immediate switch or score gain is not guaranteed. When selected, its base architecture limit is 16 layers (relevant owned techniques can raise it), not an immediate 16-layer model. Parameter count = layers × width per layer squared × 9; memory use grows with size. The error component from insufficient parameters uses a ×0.55 coefficient, not an accuracy bonus. Normal training throughput uses ×0.8, multiplied with optimizer, research and other factors; rounding and caps still affect cards per epoch. Above 12 layers, each extra layer subtracts 3 percentage points separately in the score-ceiling and per-card success calculations, subject to their respective floors; these are not one fixed combined deduction. Architectures do not stack. Evaluate after training to see the score.
