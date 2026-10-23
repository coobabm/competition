# 当前设计检查迁移

补丁 localbuild-current-design-migration.patch 只改3个Editor检查，不改产品实现。

- PlayabilityChecks：保留旧学习/纠正/存档/结局/研究资金验证；另加真正新开局12次 TryAnswerStatement→买train.curve→首轮训练；电费使用真实报价及monthKwh；房租检查待付、部分转账、读档、全额和防重。旧乐观通关脚本补上升阶展示完成、能力考真实结果和实际采购路线，未修改门槛/样本/参数/钱，不伪造考卷分数。YY只在模拟层用已送达回复API和原始资源短语，明确不等于模型对话实测。
- SkillTreeCopyChecks：三行、中文12/14字、缩写年份、中英、详情≤3句、隐藏节点、安全空值、只读、全节点覆盖。原Pay/Auto长说明断言迁成实际手动倍率/自动无加薪、资格阈值、每桌速率、暂停行为。原下载批次改为所有付费小游戏实际购买、立即可选和无下载队列。保留ReLU/增强原本全部机制验证、链满级和tooltip几何。
- DesignWallpaperSmoke：仅 RunConcepts、RunLinks、RunRegions 迁移。真实12题→付费曲线→付费脑窗口，使用 XgBrainWindow.Drawing，并确认无旧壁纸覆盖；保留原概念颜色/信念更新/训练学习/关系去重/区域架构与连接等断言。新游戏断言改为窗口隐藏且真实新存档概念清空，避免强迫隐藏图形做无意义更新。其他旧壁纸入口尚未迁移，完整 Run 不能计为本轮验收。旧透明pin/桌面菜单与首分钟假设仍需单独逐项替换，不将其静默跳过。

验证：
- localbuild-migration-check/compile.log：完整Editor Roslyn编译0错误，out/refout均在coordination。
- localbuild-migration-check/full-core.log：完整 PlayabilityChecks.Run 加 SkillTreeCopy 的5个核心方法 × 中英均通过。临时 .NET harness 用 System.Text.Json 支持核心存档往返，并非Unity JsonUtility，因此真实Unity存档/tooltip仍需跑原入口；未运行Unity。
- 6秒操作间隔：41.2分钟完成涌现；12秒：52.2分钟。包含固定3分钟开场、每阶90秒观看/聊天、实际训练耗时和手动房租。仅乐观模拟路线可达性，不是玩家实际通关时间。

主线请逐个运行：SkillTreeCopyChecks.Run、PlayabilityChecks.Run、DesignWallpaperSmoke.RunConcepts / RunLinks / RunRegions。后3个会切场景并结束Editor，必须隔离执行，当前没有Unity通过声明。
