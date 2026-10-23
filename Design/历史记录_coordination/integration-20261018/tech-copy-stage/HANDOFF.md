# 科技三行卡交接（2026-10-18，恢复后）

已生成 `tech-three-line.patch`（只暂存，未改主工程），包含 XgTechTreeCopy 与 XgTechTreePage 的最小差异。

- 明确68条静态文案；层数/宽度、数据包、19个小游戏包采用结构化接口生成。persona.* 题包接口已覆盖，但该系统尚未接入不能当成功能完成。
- 卡面 Name / Summary / ▶Effect；科普仅点“详情 ›”后显示，关闭原自动悬停科普。详情使用最多3句解释，删除旧Effect大段限定和乘法说明。
- 卡片260宽/184行高，固定字号；边框状态、价格、链式升级沿用。尚需Unity视觉检查，特别英文长名/动态AI名。
- 2016年的应用工具标“工具2016”，未来自研方向标“探索2016”，时代卡不再写2017公开成果。
- 全局串行推理下 label.parallel 没有明确新增购买收益，因此写“保持单请求推理”，详情明确待整合；这是待产品确认，不伪装为加速功能。

验证：
- Core 与完整 Assembly-CSharp 在独立Roslyn、Unity实际引用下编译退出0，日志 core-compile.log/ui-compile.log。
- 运行覆盖检查154节点（当前catalog+全部19pack接口+1persona接口），0错误。包含中文长度、3行、中英、禁词、详情句数、增购前后实际核心处理量倍率、能耗/烧卡常量。coverage.log；检查源位于相邻tech-copy-check/Program.cs。
- 长度按中文summary≤12、▶effect不含格式空格≤14；另对68条静态词条含“▶ ”也检查≤14。
- 未启动Unity，未完成实际卡片排版/缩放/购买/详情交互验收。勿把独立编译等同Unity运行通过。

效果核对依据：XgSim.Progression.ProgressionSpeed、XgSim.Collection.FeelSpeed、XgSim.SpeedResearch/OptSpeed、TrainBill能耗及烧卡常量、AutoModel的架构配置、DataPacks购买与TeachingPackAvailable、TrainingCatalog.TrainPart。

真实性边界：架构写“加入可用脑型”不承诺立刻提高分数；bias/step写“上限项”不承诺现分；cooler为训练烧卡风险不是一切故障。brain.window、persona文案依赖后续窗口/题包实现。layernorm/residual/multihead只写随模型获得，不虚构独立效果。

## 更新：复核扩容依赖
label.parallel 的无收益问题已解决，按主代理确认改为复核容量20→60。必须先合 ../review-capacity-stage/review-capacity.patch，再合本目录 tech-three-line.patch。此前“保持单请求推理/购买收益待确认”的说明已被此方案取代；价格、ID、前置及旧档权益保留。77项容量/聊天优先断言通过，三行覆盖扩大到184节点，0错误。
