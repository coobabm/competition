# 科技词条恢复交付

日期：2026-10-13。原处理《技能树词条策划》第3行；随后完成第4行成长承载页，主聊天57/58验收并勾选。当前仅审阅紧邻的第6行分隔线。未改全局进度。

**4:003 已由主聊天42轮通过；4:004 已由主聊天57 Growth、58 Tech及图验通过；紧邻4:006为结构分隔行，无生产改动。**

## 本轮变更

- `XgTechTreeCopy.cs`：复核后语义合并 tech-010 和 tech-040。ReLU 保留正式名称，区分真实 ReLU 原理与游戏的信号保留模型；沿用公共处理量模板，补自动采用、98%/95%逐层保留与超过结构层数后75%→85%的估算。数据增强删除未实现的“裁剪”，补有效样本数、包含原题的一次性倍率、自动生效时点及库存/总进度不变。
- `XgTechTreePage.cs`：离屏桌面相机使用其 `pixelRect` 作为可见屏幕边界，避免错误地把 GameView 的 `Screen.safeArea` 当成源纹理像素。普通直接屏幕画布仍使用 `Screen.safeArea`。重新显示科技页时清除旧悬停提示，防止跨页后旧卡片短暂残留。
- `SkillTreeCopyChecks.cs`：增加两项补交的中英文文案约束与纯内存模拟对照：ReLU 自动选择/处理量/基础保留率及归一化影响；7类数据的增强题池和有效样本倍率、库存保持；数字平移保留答案与无位置特征。
- `DesignWallpaperSmoke.Tech.cs`：在已有隔离存档框架中增加 `RunTech()` 和 `TickTech(...)`，使用本文件自己的 `techStep`。主聊天负责在公共 setup 后将共享 step 置180并分发到本入口。本文件没有改主框架。

## 补交核对

tech-010 仍没有 result.md，采用其 copy.md 和 changes.patch 作为候选，再逐项读当前机制核对；不伪造原任务的交付记录。tech-040 的 result.md 已到达。两份来源清单共25条 SHA256 全部与恢复时的生产依赖吻合。即使身份吻合，也没有机械套补丁：ReLU 继续复用已经整合过的公共 Speed 模板，避免重复堆入卡数上下限和购买条件全文；数据增强采用两处分支的内容。

当前核对依据：`XgSim.Board.BestActivation/Knobs/AugmentFactor/PoolSize/CardsPerEpoch`、`XgKnobs.G/RelayKeep`、`XgSim.ProgressionSpeed`、`XgSim.SamplesEffective/OverfitScale`、`XgSim.AutoModel.CeilingParts`、`XgBoardData.Augment`。没有改核心玩法数值、购买逻辑或存档接口。

## 统一测试入口

1. 纯内存检查：`SkillTreeCopyChecks.Run()`；输出 `LocalVerification/DesignPass/SkillTreeCopy/checks.json`。
2. 真实场景检查：`DesignWallpaperSmoke.RunTech()`，菜单 `Tools/Design Pass/Tech Tree Scene Smoke`。此入口会复用公共 `Start("tech")` 的隔离路径，并再次运行纯内存检查。沿用公共 `result.txt`、`checks.txt` 和 `Finish`；截图与 `tech-cases.txt` 写到 `LocalVerification/DesignPass/WallpaperSmoke/`，文件名以 `tech-` 开头。

场景入口按真实提示框（含追加的前置/折扣正文）测量，分别选择中英文最长标题、最长效果。在实际应用窗口里取得840×560、240×320、160×240的科技页区域，并覆盖1、1.25、0.75内容缩放及四个屏边。屏边用页面超出源相机边界16像素的夹具验证可见区域交集。

断言包括：TMP标题/等级/效果/费用高度足够、每条换行不超宽、卡片留在页面与实际LCD边界内；从节点跨到提示框后的悬停宽限；真实源画布raycast找到提示框；滚轮不移动/缩放科技树；能到最底且完整费用栏可读；滚动条的指针/拖动处理能回到标题；悬停中切语言、快速换节点、未发现节点保护、中英文满级；跨页重开及隐藏节点关闭旧提示；最终通过LCD输入路径回节点购买。

## 验证状态与局限

- 已完成来源哈希复核、当前机制/API链阅读、限定文件的 `git diff --check`；本agent未启动 Unity，编译与场景运行均由主聊天统一执行。
- 主聊天已运行35、38、39轮。39已通过布局、双语、窄窗、费用可读与滚动断言，最终购买失败；原因与夹具修复见下，不能宣称整项PASS。
- 窄窗夹具保留真实场景/窗口/字体/科技页，固定 `UiFitScale` 的比例，使科技页能达到明确的逻辑尺寸。它覆盖内容缩放，不代表已经覆盖所有OS显示DPI或所有真实显示器分辨率。
- 自动检查布局期间禁用LCD自动鼠标relay，防止机器鼠标在挪动窗口时干扰悬停；使用实际源画布raycast和生产滚轮/滚动条事件处理器。最后恢复relay，并走原有LCD指针点击路径验证购买。因此滚轮和拖条是事件级场景验收，并非硬件鼠标端到端输入实测。
- 该入口禁用模拟定时推进，所有人工状态仅存在于框架新建的隔离状态；未读写玩家存档。截图还需人工查看；没有把截图生成等同视觉验收。
- 未处理独立旧记录中的并发服务1→2进程验收，也未扩展到其他词条的完整玩法实验。没有打包、提交、推送或恢复任何独立聊天。

主聊天运行若出现失败，先保留断言与失败证据，逐项定位，不以降低断言掩盖问题。

## 35-tech-scene 首轮失败与修复

主聊天已完成首次 Unity 编译和场景运行。`35-tech-scene` 在 `tech-zh-3-transformer`（160×240、0.75缩放）的真实源画布命中断言失败；此前中文长标题/效果、240×320、滚轮和拖条以及该最窄案例的TMP高度/换行/屏幕几何断言已通过。不得据此写成整项PASS。

复核已有 `tech-zh-2-lstm-top.png` 发现费用栏落在原生任务栏下，被实际任务栏遮盖。生产提示框只避开源屏幕边缘，未扣掉任务栏占用区；本轮在 `XgTechTreePage` 缓存同源画布的 Taskbar，并用实时世界矩形转换到页面坐标，在计算可用区域时扣除底部任务栏。窗口超出屏边的原夹具和窄窗尺寸保留。

另发现旧截图在请求 `CaptureRendered` 后同一tick立刻滚动，导致名为top的截图实际拍到bottom，最窄失败也发生在真正截图之前。场景入口现在在顶部、底部截图后各等待一个短渲染间隔；每次raycast将源坐标、画布尺寸及排序后的完整命中路径写入 `tech-raycasts.txt`。新增任务栏不重叠断言与底部费用中心的真实命中断言，既有中心命中断言未移除或放宽。

35并未保存失败帧或命中对象，因此尚不能宣称最窄中心命中失败完全由任务栏造成；任务栏遮挡是已从截图确认的独立根因，下一轮需用新增证据确认最窄案例。源码再次冻结，等待主聊天复跑；本agent没有启动Unity。

## 38-tech-scene 的确切遮挡对象

38重复最窄中心命中失败，但新增证据已明确：源坐标 `(3719,226)` 第一命中是 `Desktop/Prologue Layer/Tray Popup Area/Tray Popup`（depth1614），科技Tooltip为第二命中（depth1499）。失败帧 `tech-zh-3-transformer-top.png` 显示“淘货”广告通知完全盖住右下科技页。38的任务栏不重叠与前序费用无遮挡断言已通过，不能把本次通知遮挡与上轮任务栏缺陷混为一谈。

这是正常桌面通知仍在场景夹具上方导致的测试前置问题。科技提示不应覆盖系统通知。入口现在在每个边缘案例前，通过通知真实源画布raycast找到实际Close按钮，发送其生产点击事件，等待正常退场和队列清空；15秒未退场仍硬失败。没有禁用/隐藏通知组件或修改通知生产逻辑，也没有跳过、放宽科技页原中心命中断言。窄窗尺寸与屏边位置全部保留。等待主聊天复跑。

## 39-tech-scene 的最终购买前置修复

39已通过中英文长文案、四角与窄窗、任务栏避让、滚轮/拖条、底部费用、语言切换、满级/未发现以及提示关闭断言，仅最后的实际LCD购买失败。当前 `XingGuangHost.Live` 明确要求 `!runtime.TestMode`，因此夹具即使给隔离钱包设置10000，真实Host仍暴露0资金并拒绝Spend；此前最终交易一直处于TestMode，属于测试前置不成立。

本轮仅在最终购买的try/finally内临时关闭TestMode，保持 `useDiskSave=false`、`tickEnabled=false`、控制器停止自动推进，并在成功/异常/迭代器释放时恢复原TestMode。未修改生产钱包或购买逻辑，未访问玩家存档。购买仍走共享 `Click(weights.square)` 的真实LCD输入路径，不直接调用BuyNode。

新增实际Host余额与Buyable状态前置、源画布节点命中、购买前渲染帧 `tech-before-purchase`、真实relay命中诊断，以及所有权/钱包扣费/totalSpent增加均与当前动态费用一致且只发生一次的硬断言。既有必须实际购买成功断言保留。源码冻结，可复跑；本修复尚未由Unity运行验证，不记录为PASS。

## 42-tech 实测收口

主聊天已统一编译并运行42轮，`LocalVerification/DesignPass/42-tech-result.txt` 为PASS；末尾检查包含真实Host可买、源画布命中、LCD购买成功，以及钱包与totalSpent精确匹配动态费用。截图存于 `LocalVerification/DesignPass/42-tech/`。主聊天已完成图验，并在总清单勾选4:003；本agent未改主进度。

## 4:004 的紧邻后续候选

当前条目为「适用于《合并删减策划》里的『成长』页（科技、道具、能力合一）」。明确引用§3.2后发现三页面和三导航仍分离，虽然既有科技图已含道具节点，能力参数/样本进度及消耗品货架还不在同页，不能据此判完成。

主聊天已同意限定所有权：`XgTechTreePage.cs`及新增Growth partial、`XingGuangView.cs`、`XingGuangView.Frame.cs`、独立Editor检查和本记录；View/Frame必须小片段合并，保留首课保护及其他协作者内容。当前已将完整候选置于 [tech-growth-candidate](tech-growth-candidate/README.md)，未写上述生产Assets，避免打断当前Unity验收。

候选复用当前科技图与交易路径，以六项真实能力为横轴；折扣道具按真实受益能力归列，保留其他目录内容和未知节点显隐。能力读取当前门槛与有效样本，消耗品在右侧货架。旧tree/items/abilities入口和ab.*目标均兼容；界面只保留单个成长入口。没有更改玩法数值/存档，也未实施4:010以后的节点格式。

独立场景候选覆盖旧路由、双语能力门槛/TMP、真实LCD道具购买及同页门槛变化、真实货架采购的库存/钱包/totalSpent。已核对实际API与补丁片段唯一性，`git apply --check`通过；没有运行Unity、没有标记4:004完成。主聊天整合后仍需RunGrowth及RunTech回归，并查看新截图。

## 4:004 已落地、等待场景验收

主聊天确认53已PASS退出后，本agent复核三源与manifest字节SHA完全相同，再应用25/3/5个小片段，复制Growth partial与独立场景检查并生成两份meta。未覆盖完整View/Frame，未改主框架、Controller、Catalog或总进度。部署文件及最新SHA见 `tech-growth-candidate/deployed-manifest.json`。Assets已冻结。

按主聊天复核意见补强了长英文和窄货架：能力头按TMP实际高度展开，原门槛独立一行，并同步调整所有下方节点、连线及聚焦坐标；货架名称/效果/库存/购买控件按实际高度排版，标题与关闭按钮分区，窄窗口靠滚动保留可读字号。没有修改核心数值和购买路径。

场景检查新增中英文840×560、240×320、160×240真实页面尺寸、真实全折扣组合与大当前参数；硬断言覆盖TMP行高/行宽、卡片内行间不重叠、货架不越页面、关闭/导航源画布命中、滚轮到底且树不动、最后购买按钮完整显示、关闭后从页头重开。所有缩放、窗口尺寸、语言、模拟字段和输入/推进开关都有finally恢复。除实际交易段外保持TestMode；整个入口禁磁盘保存。

候选和部署版本分别以Unity自带Roslyn完成全运行时/Editor源集编译，全部退出码0；输出只写 `LocalVerification/DesignPass/GrowthCompile/`，没有启动Unity或替换Library程序集。限定文件 `git diff --check`通过。尚无Unity场景PASS，不沿用42或53轮代替成长验证；主聊天负责growth→step220/TickGrowth的公共路由、RunGrowth/RunTech及图验。

## 54-growth 失败与修正

54已通过路由、六能力、折扣归列、双语长门槛、全部窄窗TMP/命中/滚动/关闭重开，以及真实LCD购买MLP和同页门槛更新；最终货架首包交易断言失败。源码确认窄窗检查最后停在货架底部，而重开保留滚动位置，最终夹具却直接点击已在视口外的首包。54未记录最终命中或钱/库存数值，因此仍不将该推断写成历史实测唯一根因。

修正仅让最终夹具先记录当前滚动/首包视口坐标和命中，再经真实源画布滚轮回到顶部，硬断言首包完整可见、可交互、真实Host余额足够、源画布命中正确后只走一次LCD点击。新增实际relay命中断言和 `growth-purchase.txt`，记录滚前、滚后、点击前、点击即时、稳定后五个时点的sourceHit、relayHit、stock、wallet/Host money、totalSpent和隔离开关；旧库存/精确扣款/累计支出断言保留。未改生产钱包和购买规则。

主聊天及本agent查看54的 `growth-shelf-en-160-top-full.png` 后确认另一实际视觉失败：能力文字透过货架底板。`XgBrainWindowGlass`依据底板相对面积将其renderer alpha降至.5/.12，TMP自身的尺寸断言无法发现此问题。按主聊天明确授权，增加`GrowthConsumables`及其后代的玻璃保护；受保护底板若已在treated集合，恢复renderer alpha=1并移出集合。保持自身color/控件透明度，不干扰其他动画。

检查新增实际玻璃半秒更新周期后的货架及六pack底板color/renderer/inherited alpha断言，写 `growth-backing.txt`；最窄英文夹具临时去除其受保护祖先名称，让真实glass先产生treated状态，再恢复名称并等待真实周期，验证旧alpha恢复。名称有finally恢复，不改玩法/存档字段。修正后仍输出顶部/底部及购买前截图，需要主聊天重跑后图验，未把源修复当作视觉PASS。

本轮只修改 `XgBrainWindowGlass.cs` 和 `DesignWallpaperSmoke.Growth.cs`；二者及所有其他Assets已再次冻结。独立全运行时/Editor编译均退出码0，限定diff检查通过；本agent没有启动Unity。主聊天随后可运行55 Wiring，成长回归另行安排。

## 56-growth 通过与入口权限别名补漏

主聊天报告56 RunGrowth已PASS，含54失败的货架真实采购与玻璃不透明检查；其图验由主聊天负责。随后lesson代理只读发现入口兼容遗漏：`ShowTab`和`NavButton`已将items/abilities/growth归到tree，但`FeatureOpen(items)`仍读旧评估门槛，导致新局首页BuyItems仅晃动，训练补货栏也隐藏。

按主聊天4:004范围授权，本轮只在`XingGuangView.FeatureOpen`入口统一这三个别名为tree，移除items独立旧分支；不改Core.FeatureVisible、存档字段或真正的购买状态条件。沿用既有首页/训练调用链即可恢复入口行为，不需改Home/Dock或Frame。

Growth检查追加真实新局前置（无best、paidAssessment、treeIntro、migrated旧解锁且Core tree门槛为false）、四入口FeatureOpen等价、首页BuyItems真实源画布命中及LCD点击进入成长货架、Host无资金时首包仍禁用且core旧门槛仍false、训练补货栏实际可见。不是只测Open/NavButton helper。该新增用例尚未运行，不能沿用56的PASS覆盖本次补丁。

只改View单片段及Growth检查，独立全运行时/Editor编译退出码0、限定diff检查通过，已冻结。最新SHA存入`tech-growth-candidate/deployed-manifest.json`。lesson另报GuideNote/XgGuideHighlight的独立旧别名链，本agent未编辑，交由主聊天分工。

## 正常页截图与年代卡Continue

主聊天检查56最终图发现MLP购买触发的1986年代卡仍挡在成长页中央。Growth夹具禁用了View推进，所以年代卡不会自然走完其8秒展示；正确收口是经过实际Continue按钮关闭，再拍正常页，不直接隐藏对象或修改时序。

`XgTechTreePage`原Continue只在Build时调用`Lang.T("继续")`赋值，后续语言切换未刷新。现保存按钮引用，用明确的「继续 / Continue」双语文案，在页面Refresh及年代卡显示时刷新；保持叙事正文、8秒计时和原关闭回调不变。Growth最终截图前新增中英文按钮文案断言、真实源画布命中、LCD点击Continue以及年代卡已关闭断言。

本部分独立运行时/Editor编译通过。lesson代理另外负责GuideNote/XgGuideHighlight别名修复和新GrowthGuide检查；本agent仅在首页/训练入口检查之后、beforeRead之前接入一个可释放子迭代器调用，不重复编辑其文件。lesson落地冻结后，已包含新GrowthGuide源重新完成全运行时/Editor独立编译，两者退出码均0。

统一冻结时Growth.cs SHA为`E64F3C0A060C7279B8CA56EC486A1FF63DED53DD191901D57EE45A8DA4D32A08`，GrowthGuide.cs为`43755A3C59A1012EC56F2FC2FF5C72C534B0B4D82298914D7B95B3436542DC08`。主聊天负责最终Growth和Tech回归；Unity运行期间本agent不再编辑Assets，必要修补先置候选。

## 57/58 收口与紧邻4:006

已核对 `57-growth-result.txt` 和 `58-tech-result.txt` 均为PASS；57末尾包含实际货架一次采购、Continue中英刷新及实际LCD关闭年代卡。主聊天确认实际截图并已勾选4:004。

按同文档顺序，紧邻未完成项是`4:006`，准确原文为`---`。原文第5行为空白，清单无4:005。此项仅是Markdown结构分隔线，没有新的产品实现或运行验收要求；详细审阅在 `tech-4-006-review.md`。本agent未读再后一条，未改主清单或Assets，未启动Unity；由主代理只对本条记录结构审阅完成。
