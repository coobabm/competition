# 稀有道具扩容候选：交 Claude 独立审查

状态：仅暂存候选，未合入生产，未启动 Unity，未编译或运行以下检查。用户要求当前收尾后暂停；本模块留给 Claude 审查、完善和回交，由 root 统一合入与串行 Unity 验证。不要把候选存在或 `git apply --check` 成功写成游戏验收通过。

## 基准与批准来源

- 项目：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。
- 准备时 HEAD：`c67a7db50ace0b9df6356b7c3b79b75c404e95e0`。
- 以准备时工作区八个相关文件为精确基准，副本在 `base/`，规范化 SHA256 在 `manifest.json`。根代理同时处理 Board/Controller 训练预热性能，本候选不改这些文件。
- `Design/整合记录.md:345`「新增科技定价确认1：装备扩容A」：原稀有道具栏4→8，阶段3、普通标价¥12,000；8→12，阶段4、普通标价¥48,000且须第一次。可选、不作通关前置；标价不额外乘StagePrice，沿用合法折扣。
- `Design/UI_v4_不一致清单.md` #20 与 `Design/UI_v4_执行边界.md` 最新覆盖：扩的是现有稀有道具栏，保留道具与效果，不是新增显卡/散热槽。执行边界旧“价格待确认”已被上述后续批准覆盖。
- 不动名字页、学习历史、蜘蛛增益，也不扩大到其它未确认模块。

## 交付物

- `relic-expansion.patch`：8个生产源码变更 + 2个Editor检查源码及meta，共12个文件。
- `candidate/`：候选完整文件，供独立审查；不要用整目录覆盖更新后的生产源码。
- `base/`：八个原文件，供三方比对。
- `build_candidate.py`、`add_tip.py`、`finalize.py`：制作脚本；交付以最终patch为准，不需重新运行。若重新生成，顺序为 build_candidate → add_tip → finalize；检查源码留在 candidate 内。
- `static-verification.json`：只记录补丁可应用与原文件未改的静态事实。

## 改动文件与行为

所有下述生产文件均在 `Assets/LingGuangV05/XingGuang/`：

| 文件 | 内容 |
| --- | --- |
| `Core/XgRelics.cs` | 保留`Slots=4`作为初始/旧档容量，新增`MaxSlots=12`。 |
| `Core/XgSim.Relics.cs` | `relic.slots8`、`relic.slots12`决定容量；装备、卸下、自动装备按购买容量；缓存覆盖全部12槽；旧四格不变，已购扩容持久化；非法多余装备回到仍拥有的背包。 |
| `Core/XgTrainingCatalog.cs` | 两个独立科技节点，直接添加批准的普通价格，绕过目录StagePrice；第二个parent为第一个。 |
| `Core/XgSim.Progression.cs` | 价格走现有折扣和购买逻辑但不乘旧0.25节奏倍率；锁定节点可见以供定位，明确阶段阻挡；真实购买后同步槽数组。 |
| `Core/XgTechTreeCopy.cs` | 两节点中英文三行说明。 |
| `Desktop/XgBackpackPage.cs` | 12格分三行放入滚动内容，小窗能访问；未购格为封印、点击定位科技；动态容量标题，满槽选择器支持12个位置；保留旧背包、套装与收藏。 |
| `Desktop/XgRelicUi.cs` | 空位提示显示真实4/8/12上限，补空Sim保护。 |
| `Desktop/XingGuangView.Relics.cs` | 顶栏保持143宽，4格一页、三页；箭头换组，未购格跳对应科技。重绑Sim回第一页。 |

## 已做的静态核对

- `git apply --check relic-expansion.patch` 对当前生产工作区退出0。
- 规范化内容比对：八个基准生产文件与base完全相同，表示本候选未改生产。
- 全部变更在stage内；无Unity进程、场景写入、生产导入或构建操作。
- **未验证C#编译、真实页面可见性、真实输入、效果数值或存档回归**；请保留这个状态直到实际执行。

## root串行验证入口

1. root审查并应用patch后，先由其唯一Unity会话完成导入编译。
2. 执行 `RelicExpansionChecks.Run`（菜单 `Tools/Local Verification/Relic expansion purchase and persistence`）。它使用真实 `BuyNode` 与隔离钱包，测两次价/阶段/前置/不足/去重、原四格保留、5–12逐格效果缓存、跨第5/12槽套装、合法10%折扣、8/12格JsonUtility往返、旧档不赠容量。只证明内存JSON往返，不是旧磁盘存档全验收。
3. 在既有 `SidebarLayoutChecks` 隔离真实经济fixture内嵌 `RelicExpansionUiChecks.Frames(ctl,setEnglish,click,capture)`。此接口沿用LedgerUiChecks形式，要求`useDiskSave=false`、`tickEnabled=false`、`TestMode=false`，每个`MoveNext`对应真实渲染帧。覆盖中英×960×600/1400×900、封印真实LCD点击到正确详情、不扣钱、真实Host两次购买、12格标题和第12格卸下，输出截图待人工查看。不自行建立并行Unity。
4. 另人工验收顶栏三页切换、满12格时替换选择器、第三行滚动、中英封印字体边界；现UI检查尚未覆盖这些项，不冒称全覆盖。
5. 既有 `XgRelicTests` 回归，重点旧坏档修复与所有套装效果。新检查不得替代它。

## 审查关注点

- 不把`XgRelics.Slots`全局改12，否则新档免费扩容；所有容量读取必须用`RelicSlotCapacity`，UI池大小用`MaxSlots`。
- 科技普通价格必须同时避开目录StagePrice和NodeCost里的StoryResearchPrice，合法稀有道具折扣仍有效；不用另造购买扣费入口。
- 锁槽定位采用既有`FocusNodeDetails`，早期阶段也应可查看但不能购买。确认真实树布局/筛选不遮掉未来扩容节点。
- 直接篡改存档槽数组不应赠容量。原四格及背包拥有关系保留；合法已购12格读档不能被旧修复截为4。
- 独立审查若发现问题，继续在该stage产生更新patch与说明，交root统一合入；当前按用户要求暂停，不新增其它任务。
