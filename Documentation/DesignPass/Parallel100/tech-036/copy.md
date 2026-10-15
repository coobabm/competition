# momentum 可直接采用的中英文文案

中文名称：动量随机梯度下降（Momentum SGD）

中文解释：动量算法的原理是：调整时参考之前的方向，像推球时保留一点惯性。

中文效果：购买后无需装备：若它是已拥有的最快优化器，两条训练线在之后结算训练轮次时自动采用。相对基础 SGD，训练处理量 ×1.3；每轮卡数仍受算力、取整和上限影响，成绩需训练后评估。优化器之间只取最快一种，不相乘；与其他训练处理量倍数相乘。离线模拟固定每轮卡数，不受此倍率影响。购买本身不保证提分。

English name: Stochastic gradient descent with momentum (Momentum SGD)

English explanation: Momentum means using earlier update directions when making the next adjustment, like keeping a ball rolling.

English effect: No equipment step after purchase: if this is the fastest owned optimizer, both tracks use it when subsequent epochs settle. Relative to base SGD, training throughput ×1.3; cards per epoch still depend on compute, rounding and caps. Evaluate after training to see the score. Only the fastest owned optimizer applies; optimizer multipliers do not stack, but multiply with other throughput factors. Offline simulation uses a fixed card count per epoch, unaffected by this multiplier. Buying alone does not guarantee a higher score.

倍率取自目录 research.speed；1.3 是无量纲倍率（公式取整和限幅前增加 30%），不是 +1.3 张/秒或 +30 个百分点准确率。实际处理量单位为张/轮；补丁复用 Speed() 中已有的限制说明。
