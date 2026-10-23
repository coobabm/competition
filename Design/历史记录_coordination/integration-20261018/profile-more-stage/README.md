# C2 名字页“更多”独立候选

基准：实际 main `bf354fdbce3745a81056fec6d17452ca91b15f21`，另有已整合事件 core/host 工作区改动。候选只改 `Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Profile.cs`；原文件完整保存在 base，完整候选在 candidate。没有生产写入、Unity启动/编译或git提交。依据《整合记录》#21已确认“旧功能收进更多A”；不改名字页首次聊天/脑窗购买的待确认门槛。

## 入口映射与中文/英文

| 原入口 | 候选入口 | 原路由与条件 |
|---|---|---|
| 近况与性格 / About me | 名字页左上保留；子页显示 返回名字页 / Back to profile | ShowTab("home")，FeatureOpen("home")与OpeningLocksLabTab完全未改 |
| 闪卡与成就 / Cards & awards | 更多 / More → 闪卡与成就 / Cards & awards | ShowTab("cards")，保留原Profile始终启用按钮及路由到home的行为 |
| 背包 / Backpack | 更多 / More → 背包 / Backpack | 原FeatureOpen("backpack")，relicPicks>0或relicOwned非空，并继续受opening gate限制 |
| 终章 / Finale | 更多 / More → 终章 / Finale | 原FeatureOpen("final")与ShowTab前置检查，原stage>=6及opening gate不变 |

注意：当前Profile对Cards按钮一直传true；ShowTab把cards映射home后检查名字页开放条件，并不再用FeatureVisible("cards")的首张成就规则。此包保留实际既有行为，不借收纳改变权限。

## 保留项与菜单行为

- 原Home/Cards/Backpack/Finale实例、页面内容、收藏、已装备/拥有物品、终章状态及Shown回调均保留；没有存档字段写入或迁移。
- 背包原有未读红点保留，并汇总到More上；打开More不调用背包Shown、不标记已读。真正进入背包仍由原页面标记已读。
- 顶部由四个并列入口变为左侧概况/返回、右侧More；下拉只出现原已开放目的地，没有空白槽。两种语言各保留短标签。
- 选择目的地、点击菜单外页面区域、切换主栏/子页、切档、窗口关闭或最小化会收起；再次点击More可收起。菜单内空白不触发底层页面；菜单外点击只关闭菜单。
- 无新增逐帧Update、动画驱动或图像资源；菜单采用现有XgUi控件与字体。该文件在原RefreshProfileSections里更新显示，不需要修改共享View路由。

## 已执行与未执行

`python finalize.py`：检查生产Profile仍匹配base、生成统一diff/sha256 manifest，检查原页面与关键路由/门槛引用、无Sim.S写入和无新增Update。`git apply --check --ignore-space-change --whitespace=nowarn profile-more.patch`仅检查可应用性。它们不是Unity运行验收。

未编译或启动Unity。最终真实字体、布局、鼠标、已购物品/收藏回归与旧档读写由root串行验收。新增低风险UI收纳没有单独复制实现的自动化单元测试。

## 建议真实窗口验收

1. 名字页尚未开放的真实档：现有锁定原因和不可进入行为不变；不因More出现提前开放。
2. 名字页已开放、无稀有物品、阶段1：仅More→Cards & awards；打开相册后可返回名字页，旧收藏/进度一致。
3. 有稀有物品或待选次数：More出现背包，未读点同时显示在More与Backpack；只展开不清点，实际进入背包按旧规则清点。旧装备、收藏和组合效果一致。
4. 阶段6：More显示Finale，旧终章操作和返回行为一致；阶段<6不出现且外部ShowTab("final")仍拒绝。
5. 中英文分别确认菜单文字、不同菜单高度、窗口原尺寸/缩放/最大化、真实鼠标命中。展开后点击More/外部、切科技页、切档、最小化与重开，菜单不残留到其它页或新档。
6. 外部快捷方式直接Open("cards"/"backpack"/"final")仍走既有路由；打开More本身不改变钱包、成就、relicFresh或终章状态。

共享接口请求：无。该包不修改图鉴统一、同源脑图或名字页其他布局，相关已确认工作仍由其所有者承接。
