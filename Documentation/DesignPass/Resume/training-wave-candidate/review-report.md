# 1:084 候选审查与限定修补

截至本次交付：只修改 Assets 外候选；未启动 Unity、未改 Assets、未访问真实存档。接线候选仍冻结。本报告只覆盖当前 1:084。

## 结论与实际修补

未发现新的 Core 行为阻塞问题。修补了 `XgBoard.Idle(cards)` 的覆盖缺口：它也经真实 R4 删除概念，现与 Train 使用同一个 `DecayAll` 采样点和事件；没有删除时不发布空事件，非正 cards 不作任何修改。

`XgWallpaperTraining.CellOpacity` 改为按 ID 查询 `births` 字典，避免每个可见格子再扫描最多 128 条记录。新增／替换同 ID、容量淘汰、自然到期、Clear、Detach 都同步清理索引。原时间曲线、最多 128 个生灭记录及 4 个光波不变。

原候选的两个代码文件保存在 `review-baseline`，只含上述限定变化及采样文档的小补丁见 `review.patch`。生产集成仍应使用候选根目录的最终文件，不能把 review-baseline 或 review-checks 放入 Assets。

## 事件与取样语义

| 事件 | 真正来源 | 不可变 Weight / Strength 的取样时刻 |
| --- | --- | --- |
| 全局光波 | `XgSim.BeginEpoch` 成功设置 epochActive 后的现有 EpochStarted | 无推断进度；重复／被阻止的开始不发事件 |
| ElementBorn | Train 的未知元素 Create 确实返回新对象 | 初生 w=0、s=1，在 R1 拉动之前 |
| Merged | R2 达阈值、真实 Create 成功并赋父权均值 | 赋权后、该卡 R4 之前；真实父 ID 保存在记录中 |
| Decayed | Train 或 Idle 的 R4 阈值判断后、Remove 之前 | 已乘衰减因子后的数值 |

R1 记录表达“出生”而非“已学习后权重”；动画落到真实末态格子时，底层格子的正常颜色仍来自当前 Board。R2 不是根据位置猜父格。R3 挤出、叠加失败、结构重置、读档和 Plant 不伪装成 R4 死亡。seed / pinned 被原 R4 规则跳过。记录对象只暴露 get 属性，批次用只读包装，未加入 XgState / XgBoardState 存档结构。

## 同帧末态与生命周期

- 每次 Core Train 已完成 R4 才发布该卡记录；同帧多次回调只将 refreshConcepts 置为 true。LateUpdate 再调用现有只读概念快照刷新，因此同一帧较晚的训练（即使没有新事件）也进入最终图。
- 每次绘制按真实 ID 重新定位出生格子和 R2 父格；最终被 R3 挤掉或重置移除的 ID 不会留在刷新后的映射。父格缺失时跳过连线，不造替代父格。
- R4 保存事件发生时最后显示的 UV；没有映射的 LOD 格子不会凭空出现死亡残影。同 ID 若从出生变死亡，先移除旧 birth 记录／索引。位置刷新不会把死亡点重新投到别的 ID。
- runtime Changed、source / Board 改变、禁用／销毁都会 Detach；不可见时 Update 清理活动记录；自然时间在 .6s / .7s 后清理并重绘最后的空帧。重新启用重新观察，不重放旧事件。
- Quiet 通过 LightLevel 降低亮度，Reduced 去掉横向移动波、降低透明度并不隐藏底层格子。Quiet 没有另行延长时长或降低光波移动速度，这是当前实现的具体语义。
- 以上为源码路径审查；真实 Unity 中的换档／换 Board、同帧多次训练后 EOF mesh、禁用再启用以及字典容量／到期仍需场景检查，不能用本次 Core PASS 替代。

## 顶点与性能

新增覆盖层使用独立 MaskableGraphic。`XgDraw.Disc(n=10)` 为 11 顶点，Seg 为 4 顶点；最重单个 R2 生灭是两 Disc 加一 Seg，共 26。普通波是 24 段 × 两 Seg，共 192。总静态上界为 `128×26 + 4×192 = 4096` 顶点；Reduced 上界 3344。现有概念层只改透明度，没有增加它的顶点数量。

CellOpacity 从最多 512×128 次比较改为每格一次字典查询。事件时去重仍是至多 128 条的扫描；同帧概念快照刷新合并到 LateUpdate。Core 没有订阅者时不创建记录列表；有订阅时仍会每张训练卡创建一个列表，即使最后为空，这属于后续真实帧性能测量项，尚无性能故障证据，未扩大修改。

## 已执行的保护性纯 Core 验证

`review-checks/TrainingWaveChecks.csproj` 对同一 Program 分别构建生产 XgBoard 与候选 XgBoard；无包源、无 Unity runtime，UseAppHost=false，只运行 DLL。Main 顶层捕获并返回 1；stderr 本身写入失败也不会使 catch 再次抛错。

`run_review.py` 隐藏启动每个子进程，编译限时 60s、正式检查 45s、保护验证 15s；所有 stdout / stderr 独立落在 `LocalVerification/DesignPass/TrainingWaveReview`，`exit-codes.json` 显式记录每步退出码。正常程序自然退出。

- 生产版：编译 exit0；刻意失败被捕获 exit1；正常 12 项 exit0。
- 候选版：编译 exit0；刻意失败被捕获 exit1；正常 28 项 exit0。
- 唯一编译 warning 为已有 `XgSim.DayStats.cs:33` 的 CS1718，不是本候选新增。
- 256 张真实 Board.Train 卡覆盖多 region、深度、容量、Dropout、clip、Idle、重置；加真实 4 次教学和 4 次完整训练、开始事件；比较每步结果、完整 Board／Sim 状态、预测及钱包数值。
- 生产版与开启被动订阅的候选版得到完全相同摘要：`E29722CE9D2D8D6B625E0BCD2354F8DBB9E556913520FA5F3E5E77292A725B42`。
- 额外验证 R1 初生快照、R2 父 ID 及 R4 前赋权、R4 移除后证据、对象及批次不可变、seed／pinned 排除、R3 不假死亡、reset／Plant 不发事件、Idle 正常／空／负输入。

尚未编译或运行 Unity 呈现候选，也没有实际截图；主任务仍需统一检查并验收。
