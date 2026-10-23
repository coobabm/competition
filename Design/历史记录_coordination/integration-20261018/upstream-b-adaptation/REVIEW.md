# 7da28342 与 B-01/B-02 兼容审核

基线：30fbe6e → origin/main 7da28342。只做只读分析及暂存，不修改主工程。

## 保留
1. **六档学习率完整保留**：RateNotches={3,2,1,0,-1,-2}、+2 新中档、+3 ×9、rateSchema=2；旧 schema1 的 +2→+3 必须保留，避免老存档风险/速度变化。新版《开局流程简化_教练科技》207 行明确六档，取代旧三档建议。默认安全档和旧 autoLr 兼容保留；是否新增 Adam 后的自动按钮仍按后续设计，不覆盖这次六档。
2. **训练取消 combo 完整保留**：训练不 Hit、不保住教页倒计时、NaN 不清教页连击、手动训练改固定 HandCardFactor=1.4。与用户的“教”连击跨套无限不矛盾；不要从训练取消误删教页 Hit。实际手动速度会变，最终两小时节奏应再测。
3. **老问题/能力对照作为新的表现保留**：保留每阶段同一道题答案变丰富、视觉/文字路径不同答案；这有助于直接展示 AI 提升。
4. **原有章节场景约束保留到总协调审核**：ExamReady、TakeExam、ClaimExamPass 新增 ChapterSceneHolds 不可在 B 模块绕过。B 小题奖励不应被当作能力考通过。

## 必须适配
- 上游 XgTrainPage.cs 仍直接访问已被 B01 删除的 gpuTarget。合并时保留 B01 的 `gpuTarget != null && ...` 和 gpuGlow 判空，同时保留上游风扇固定音高、不显示训练combo。不能整文件覆盖。
- B01 对 rateStrip 的科技门控在 RefreshTrainingParts 最后执行，仍兼容上游 RefreshRates 六档；无须改六档核心。
- B02 的 small_step 判定使用 RateNotch<0，不依赖数组长度，兼容新六档；NaN 仍 e.diverged 触发。
- B02 知识卡不能只等 StoryFocus，必须同时等待 XgEmergenceCutscene.Busy（包括排队），否则新升阶阶段小题/知识卡可能抢到涌现之前。`B02-emergence-queue.patch` 为精确两行适配，完整候选在 XgTrainPage.Lessons.cs。已独立 Roslyn 编译退出0，不是Unity运行验收。
- 上游涌现把 BeforeAfter 与 OldQuestion 两张卡并排展示，不符合“一次只显示一样”。暂存 `upstream-narrative.patch` 在 ShowOldBefore 隐藏 compare；完整960×540独占、月份→演示→第一句→解锁清单仍由 D 系统统筹，不能声称这两行已完成 D。

## “不能假定玩家做过”的真实冲突
XgSim.OldQuestion 完全无状态，却写“你第一天就问过我”“你每学会一样都要问一遍”；Opening 也宣称第一天答过，UI写“中间答过”。现有代码没有确保玩家问过该题。建议保留效果并改成“同题能力对照/最初能力/能力变化”，未来接实际题本记录后才显示历史回忆。暂存 upstream-narrative.patch 已双语改写，不删除比较功能。
如产品坚持展示真实历史，应新增记录字段并在真实问答后写入，只展示实录；不能用能力等级推导未发生的历史。此更大方案未擅自实施。

## 待总协调（不在本补丁处理）
- 新强制阿杰手机分类/老周借卡章节与“群友不知道AI、不找玩家私聊”冲突；尤其 PhoneSortHolds/CardLoanHolds 会阻断升阶。交给世界/YY负责人与主代理判断，不能只改文案不处理门槛。
- 上游 YesNoNight 强制场景可能增加第一章时长；与做12题→买曲线→首轮的开局顺序需实测。
- 上游新增 combo 音效只用于教页，但资源是 Git LFS，需确认拉取真实 WAV 后再听音效。

## 文件
- upstream-narrative.patch：对 origin/main 的3文件最小差异；未编译上游全工程、未写main。
- B02-emergence-queue.patch：对原staging-b草稿的2行差异。
- preview/：仅对照预览，不可整文件覆盖main。
- queue-compile.log：B02队列适配独立编译日志（退出0）。
