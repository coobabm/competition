# 众包项目与巨信删除验证（2026-10-07）

## 已完成

- 下架普通合同：`goclub`（死活题批改）、`parking`（车牌识别抬杆）、`support`（自动客服回复）。
- 下架付费标注桌：`translate`（中式英语）、`crosssentence`（跨句理解）；保留内部教学、训练数据包与阶段突破。
- 移除全部七个 SLA 高价单的目录、执行、结算、通知及 UI。只保留 `slaHeld` 作为旧档已赚尾款，一次性兑付后清零。
- 删除巨信 Hub/View/Core 业务、启动与接线路由，清除账本、本地化和提示词残留。
- 对原生 Photo Gallery 占位同时关闭窗口、桌面、任务栏、开始菜单条目与独立 Pictures 按钮；精确关闭对应持久 OpenWindow 监听，不按显示名称泛删。
- 旧档清理已退役合同/桌/队列/工人任务。保留钱包和已获样本；历史免费翻译/跨句包只有 unlock、没有 owned 的旧档仍可训练，不额外赠送数据，也不提前解锁新档。

## 证据

1. **修改前完整 Core 基线**：634 例，605 通过、29 失败（`baseline-tests.log`）。
2. **删除后的第一次完整 Core 回归**：634 例，604 通过、30 失败（`after-removal.trx`）。新增的一项是 `BrokenSaveIsRepaired` 仍期望退回未开放的 MNIST。已将该断言更新为退回开放的 logic 并验证 `DeskOpen`，没有放松可用性条件。
3. 余下 **29 项失败全部在修改前源码快照中重现**，见两个 `baseline-*.trx`。未为本删除任务修改这些已有逻辑。
4. 最终删除/存档/教学专项：**34/34 通过**（`removal-regressions.trx`）。
5. 最终排除上述已有失败方法后的回归：**610/610 通过**（`non-baseline-regressions.trx`）。这不是全套零失败证明；参数化方法按方法排除，同方法其他参数已在第一次完整回归中执行。
6. Unity 6000.4.5f1 已导入并成功加载新类型；Console `error CS` 查询为 0。
7. Test Runner MCP 两次报告 busy，未把它计为测试成功。确认编辑器非 Play、非编译、原生 TestRunner 无活动后，直接执行 MainSceneEntryTests 的六个无参 NUnit Test 方法：**6/6 通过**，包括旧 JSON 字段兼容、Pictures 持久事件不可复活及无关同名按钮不受影响。
8. 使用真实 `Assets/LingGuangV05/Scenes/Main.unity` 的独立预览场景验证生产删除逻辑：Photo Gallery/Pictures 两个持久按钮已禁用、事件 Off；直接 Invoke 后仍不能打开；开始菜单链接 0；其余 16 个窗口状态未改变，原活动场景未改变。预览场景已关闭，未保存场景、未改玩家存档。
9. 独立复审发现的两个 P2（旧免费桌训练访问、Pictures 入口）均已修复并通过针对性回归；复审确认代码层面无新增阻塞。
10. `git diff --check` 通过。没有提交 commit，保留其他任务/用户的已有修改。

## 验证边界

没有重新通玩完整六阶段，也没有把原有 29 项 Core 失败宣称为修复。实际 Main 场景的最终绿验是编辑器预览场景校验，不是整段 Play 游玩。并行期间其它任务修改的剧情和大脑界面文件未被本任务回滚。

## 修改前可复现的失败

- `AMetContractSendsBackLogsSlowlyBeforeStageFive`
- `APlainLoopShowsTheDistanceProblem`
- `AfterLabelsItSuggestsTrainingThenAnAssessment`
- `AutoAnsweringNeedsACheckpointAndPaysOnlyRightAnswers`
- `AutomationTrainsOnItsOwn`
- `ContractsNeedTheThresholdAndPayIntoTheWallet`
- `DriftLowersCheckpointAccuracyByFifteenPointsForAutoAndGhost`
- `EachPressTrainsOneEpochAndBillsPower`
- `EachStagesWallPassesWithItsGoldenSetting(2)`
- `EveryFourthEpochIsAssessedAndOnlyRecordsPay`
- `FreshGameCanReachAllSixStagesThroughPublicActions`
- `GlobalAutoKeepsEachDesksCheckpointAndSamplesSeparate`
- `LevelFourStopsAutomaticTrainingAfterThreeStaleAssessments`
- `LogicDeskGetsHarderAndPaysMore`
- `LongSentenceWallIsReliableAcrossSeedsAndHistoriesAndIsNotALock`
- `PassingIsJudgedByBehaviourNotByTheReferenceSetting`
- `PretrainingStallsWithoutScaleAndTripsWithoutAServerRoom`
- `StageFiveReadsTheGarbleLineByLine`
- `StageTwoNeedsBothDesksEachWithItsOwnWiring`
- `StepActivationOnTwoLayersIsNamed`
- `SuggestionIsRightAsOftenAsTheCheckpoint`
- `TheExamIsIndependentFarAndUnseen`
- `TheModelLabelsLogsAsWellAsItIsRight`
- `ThePlaytestLeNetThatStaysAtChanceIsToldToWiden`
- `TheScaleNamesTheNearestFamousModel`
- `TinyRateIsSlowAndHugeRateTears`
- `TrialsAreFairAndChangeNothingReal`
- `VanishingMemorisationAndAmnesiaAreDetected`
- `VramLimitsModelSize`
