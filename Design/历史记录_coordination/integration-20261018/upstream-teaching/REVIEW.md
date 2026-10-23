# 7da28342 教页/连击适配

## 必须保留的意图
- 玩家连击是存档 S.combo：跨题套持续、不封顶，只有答错才断。
- 音效是 XgJuice 独立静态计数：每次有效答对升一级，3秒未答后重回第1声，最高播放Kill10，不改玩家连击。
- 上游取消训练增加连击可以保留。手动训练只计 clicks；不能为它恢复 +1/+5 连击。
- 上游白闪、拖尾、粒子、连击音频可以保留，小游戏也需要接统一结算声效。

## 补丁顺序
1. 合并7da28342与当前main本地改动。
2. 应用 `upstream-streak-guard.patch`：针对上游的3文件补丁。删除 Tick 过时重置，修正训练测试断言；音效 goesOn 从 >= 改 >，避免答对1题→答错→再答对1题被误当作第2声。
3. `existing-teaching-test-strengthen.patch`：当前已有教学测试每题后 Tick(4)，确保超3秒仍保留121连击。
4. 先合原 teaching-stage/teaching-games.patch，再合 `after-games-combo-sound.patch`，给小游戏结算统一音效和粒子。此步骤依赖上游 XgJuice.PlayCombo/ComboHit。

不要直接覆盖本目录同名完整源码；它们用于审查，不含其他代理的全部本地更新。补丁按意图合并。

## 审查证据
- 5个重叠文件本地/30fbe6e4/7da28342三方合并，文本冲突均为0（merge-*/result.cs）。
- 但 XgSim 自动合并结果会静默恢复 `if (S.comboTimer <= 0) BreakCombo()`；必须显式移除，不能依赖无冲突标记。
- StageOneTutor 无冲突接受 LaoZhouYY.Lesson 与贴吧私聊回退，违反无人知道AI规则风险已告知世界代理；不要作为教学流程已验收。
- 音效原始方法抽取后7组通过；针对同计数错误重启修正后9组全部通过：首次/递增/同帧去重/实际连击+2仍只升一声/超3秒从1声/失败后回1/1题后失败又答对仍第1声。
- 测试纯逻辑没有运行Unity，没有写main；需要root恢复后编译验收。

## 本次发现的存量问题（不是新上游引入）
- 经典卡 TickCard 超时直接BreakCombo、计handWrong、换题，与倒计时只算快答奖励不符。当前常识小游戏不这样做，但切经典卡仍会触发。
- XgSim.Answer 在玩家答对且TeachingGuess存在时，会根据AI答对与否BreakCombo；玩家连击应按玩家结果，modelStreak才按AI结果。建议随后单独修复并加覆盖。
- 上游教页新声效只接了经典卡和蜘蛛，小游戏原本仅Tick；本次after-games补丁补足小游戏入口。
- 部分旧小游戏有内部每轮combo，不能把它们作为玩家全局连击；全局结算始终以S.combo展示与支付。
