# D2 后 YY / Tieba 主线程 RAW 独立审阅

结论：新采样支持“Go 后下一次 Update 的冗余重画减少”这一窄结论，**不支持整体性能提升**。当前 rendered mean/p95/max 为 **20.068 / 34.260 / 138.181 ms**，旧采样为 **19.326 / 32.840 / 105.621 ms**。两次源代码及字体状态不同，不能把上述整帧变化归因于D2，更不能据此断言音乐连续性或Player表现。

本轮只读已导出的JSON/TSV、采样器与marker源码；没有产品改动、Unity启动、重采样或新优化。仅运行本目录 `analyze_tieba.py` 整理证据。

## 来源与口径

- 新：`LocalVerification/IntegrationProfiler/20261019-112234-894-8d0eff`，RAW导出 `raw-analysis-20261019-112800-020`，采样和RAW均有complete.txt。
- 旧：`LocalVerification/IntegrationProfiler/20261019-092802-102-66ee98`，RAW导出 `raw-analysis-20261019-093254-815`。
- 两次都是模型关闭、隔离Editor、3秒预热/30秒测量；YY/Tieba均20/20次动作，每1.5秒交替。10次Tieba显式Open(home)，另外10次YY打开。不是实际鼠标自然浏览，也不是最终Player。
- rendered统计来自采样器按Time.frameCount收集 `Time.unscaledDeltaTime`；RAW是主线程frameTimeMs，旧1554帧、新1497帧，分别与rendered的1550/1493不同。不可把RAW帧号直接视为rendered统计索引。
- 新RAW主线程mean **20.044849ms**、max **235.679703ms（frame270）**。这不等于rendered max138.181ms出错；二者时钟/阶段边缘/Editor循环口径不同，本导出没有建立一一映射。

## 新 Tieba marker：真实调用数来自 RAW

下面均为主线程，分母1497个有效主线程帧。总时间由TSV保留6位小数的每帧均值×帧数反推，因此总量/单调用均值为近似值。Max为原导出的单样本最大值。

| Marker | 调用数 | inclusive总量约ms | 单调用均值约ms | 最大单次ms / RAW帧 |
|---|---:|---:|---:|---|
| Redraw(Go) | 10 | 363.710 | 36.371 | 48.694698 / 784 |
| Redraw(Cleared) | 0 | 0 | — | 无RAW样本 |
| Redraw(KeyChanged) | 2 | 62.783 | 31.391 | 40.212200 / 1376 |
| RefreshKey | 68 | 4.455 | 0.066 | 0.215900 / 421 |
| Redraw.Side | 12 | 51.859 | 4.322 | 6.166900 / 1128 |
| Redraw.Clear | 12 | 13.213 | 1.101 | 1.796600 / 1128 |
| Redraw.Page | 12 | 361.082 | 30.090 | 44.578999 / 784 |
| TiebaView.Update [Invoke] | 1495 | 82.271 | 0.055 | 40.295101 / 1376 |

`Go`、`KeyChanged`是父scope，Side/Clear/Page在其内部；不能把表中所有时间相加。两个父scope总约426.493ms，Page约361.082ms（约84.7%）说明本次已绘制部分主要耗在页面创建子树，不是RefreshKey字符串比较。Clear只测Destroy调用/即时disable等，并非随后真正OnDestroy、GC、layout和Canvas的全部成本。

**Recorder的1493 samples是轮询次数，不是marker调用数。** `IntegrationProfilerCapture` 按渲染帧读取容量1的 `ProfilerRecorder.LastValue`，再累加/取最大；没有把每次读取与对应RAW事件ID连接。因此recorders适合本次粗略数值参考，准确事件数和事件总量以RAW为准。例如KeyChanged recorder均值0.084105ms×1493约125.568ms，RAW只有两次合计约62.783ms；不能据此宣布发生了4次重画，也不能把125.568ms当作真实事件总量。当前材料不足以把这一差额逐帧归因到具体采样错位。

Cleared recorder均值/max均为0，同时RAW marker表没有此marker（RAW导出保留零时长调用），两者互相支持本采样区间内**确实没有执行Cleared分支**；不是“没有重画”，因为Go有10次、KeyChanged有2次。

## 重复重画是否减少

旧采样没有新诊断marker，不能编造旧Go/Cleared的精确计数。但有两条独立、同方向证据：

1. 旧RAW记录12个TiebaView.Update的≥2ms样本，帧为74/230/399/542/595/705/876/1041/1186/1338/1434/1480；新RAW仅2个，frame496/1376，路径明确属于KeyChanged。旧全部Update累计约353.855ms，新约82.271ms。累计差是这个marker的观测结果，不是受控A/B收益比例。
2. 整个主线程的ScrollRect构造调用22→12、TMP组件构造1332→910、GameObject.AddComponent 13448→9492；新Page/Side/Clear恰好各12次=10次显式Go+2次KeyChanged。这些构造统计是全主线程聚合，不把每一项都硬归为Tieba，但与去掉10次Go后重复重画的源码效果一致。

源码旧Go先清空signature再Redraw，后续Update必因非空now再画；缩减补丁Go先记原RefreshKey再Redraw，真正Changed事件仍会清空signature。当前10次Go与0次Cleared，结合旧12个重Update、新2个重Update，支持该明确冗余路径已被抑制。显式Open(home)本身仍会画一次，本补丁没有把重复导航改成缓存页面，也没有保证所有数据相同的重画均已消除。

## 当前关键峰值链

### KeyChanged仍有40ms是一次完整页面重建

RAW **frame1376** 主线程75.242706ms：

`PlayerLoop → BehaviourUpdate → TiebaView.Update` **40.295101 inclusive / 0.015401 self**
→ `Redraw(KeyChanged)` **40.212200ms**（来自marker表最大样本，帧号相同）
→ `Redraw.Page` **33.005299 inclusive / 5.475199 self**。

同帧另有UGUI.Rendering.UpdateBatches **9.381000 inclusive / 7.045800 self**。这些不是KeyChanged重画次数增加：此处一次重画仍创建完整页面/UI组件，之后还需布局/渲染。Update本体self仅0.015ms，不能把40ms说成轮询或签名比较本身。

另一次KeyChanged在 **frame496**：主线程58.704300ms，TiebaView.Update **22.625200 / 0.010401ms**，Page **18.243999 / 2.304100ms**；另有UGUI.UpdateBatches **7.842400 / 5.793600ms**和ScrollRect.Layout→TMP Parse Text **6.847600ms**（内TMP.SetArraySizes 6.836200ms）。两次KeyChanged父scope合计约62.783ms，第一次父scope约22.571ms由总量减最大值推得，不能当作原始精确样本。

现有marker只证明当时 `now != signature` 且旧signature非空，**没有记录哪一个key字段变了**。不能断言是10秒帮助时间桶、未读/消息、日期/阶段、typing、choices、DataRequestKey或public-discourse stamp中的某一项，也不能说业务数据其实没变。D2保留原键和原0.5秒节拍，本次不改门禁或时间桶来压低指标。

### 显式Go也仍贵

RAW **frame784** 主线程109.595001ms：

`IntegrationProfilerCapture.Tick → Redraw(Go)` **48.694698ms**
→ Page **44.578999 / 3.658699ms**
→ GameObject.AddComponent最大样本 **14.055300ms**
→ Selectable.OnEnable **13.854100 / 13.854000ms**。

同帧捕获器Tick下还有Render→CanvasUpdate.PreRender **10.608000ms**、Layout→CanvasUpdate.Layout **3.063600ms**；游戏Update侧还有XingGuangView.Update **12.595100ms**、UGUI.UpdateBatches **9.504800ms**。不能把整帧109.595ms都归为Tieba，也不能把Tick全部扣成测试工具开销，因为Tick直接调用了产品Open/Go。

RAW **frame1128** 主线程79.721107ms，Side与Clear各自最大值在本帧（6.166900、1.796600ms），Page25.855400ms。它证明侧栏重复构建仍有可见成本，但本轮不据此扩大到侧栏缓存或替换公共UI。

### 全RAW最大帧不是40ms KeyChanged那一帧

RAW **frame270** 主线程 **235.679703ms**：

- `IntegrationProfilerCapture.Tick` **122.807106 inclusive / 24.770907 self**；它混合产品切换调用与采样器工作，不能一概归为工具或产品。
- `FixedUpdate.ScriptRunDelayedFixedFrameRate → CoroutinesDelayedCalls → Destroy → TMP.OnDestroy` **46.478199 inclusive / 14.074701 self**，内部 `GC.Collect` **32.333698ms**，二者不可相加。
- `UGUI.Rendering.UpdateBatches` **13.916900 / 8.763000ms**。
- `Profiler.FlushMemoryCounters` **2.360400ms**，属已识别工具开销。

`sample-peaks.tsv`只保留self≥2ms或选定脚本/GC/TMP等inclusive≥2ms的样本，不是完整树；本帧Tick的大部分子调用没有被完整列出。因此不能把上述几项当成闭合分解或把未列部分归给新字体/D2。没有新帧数据时也不把235.680替换成rendered max138.181去叙述同一帧。

## 精确下一步建议

1. 记录本轮结论为：**冗余刷新路径有减少证据；40ms级完整重建及更长UI/GC混合峰值仍存在；无整体性能改善证明。** 保留D2小修，不因整体max上升回滚或扩大优化。
2. 当前RAW不能识别两个KeyChanged的字段；如果主控后续必须定位，最小补充是仅在KeyChanged触发时记录旧/新key的字段差分、view/arg、frame、单调时间（有界内存记录，回合后落盘），并复用现有串行采样入口。不要先改10秒桶、flag门禁或绘制时序。**本次没有实现/运行该诊断，也不自动追加采样任务。**
3. 若继续解释frame270，优先离线导出该已存在RAW帧的完整主线程子树（尤其Tick和延迟Destroy），不先重跑；这需要主控安排既有离线Unity工具，当前代理不启动。只在完整树仍不足时再决定是否需要额外测量。
4. 最终Player/音频仍按原验收计划，不以Editor这组数据宣布稳定60fps、不卡顿或音乐问题已解决。新RAW还记录了无效负MasterDSP聚合，不能当音频时间证据。

详证：`tieba-raw-evidence.json`（原表行、次数、旧/新Update峰值、主线程top10）与 `selected-main-thread-peaks.txt`（原路径/帧号/inclusive/self）；`capture-comparison.json`保留主控原比较结果，本代理未覆盖。
