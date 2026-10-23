# 本轮题库整合交接

工作目录：`C:/Users/xomem/Documents/ChatGPT/games/competition-integration-20261020`

本轮仅修改题库与必要回归测试；没有暂存、提交或运行 Unity。完成本次 GitHub 整合后按用户要求暂停，后续交接 Claude / DeepSeek。

## 已修改

- `Assets/LingGuangV05/XingGuang/Core/XgMemes.cs`：参数化标题候选增加日期元数据，按 `today` 过滤，生成结果保留 `since`。围棋三连败不早于 2016-03-12；LIGO 不早于 2016-02-11；高考提醒不早于 6 月 1 日、940 万考生信息不早于 6 月 6 日。高考文本改用明确的 2016 年 6 月日期，避免九月仍说“明天开考”。每组保留无事件日期依赖的候选，避免早期无题可出。
- 普通标题将发布单位与通知绑定，去掉会在任意月份出现的“6 月起”/高考期间措辞；保留常规公告变化。
- 真买家评论把商品、优点、缺点放在同一产品记录中，避免台灯玩游戏、鼠标风扇等不兼容组合。显卡的《守望先锋》体验在 2016-05-24 起开放，并携带该 `since`；此前仍可生成显卡开机/包装体验。
- `Assets/LingGuangV05/XingGuang/Core/XgMemes.Sense.cs`：删除上游新增的重复“黄河比长江还要长”题，保留原有“黄河是中国最长的河”题及准确解释。计数为 **216**，没有为历史 172 目标删题。保留长颈鹿“显著特征”的解释，鸟类解释英中文一致，孙杨英文恢复 `200 m freestyle`。
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgEraTests.cs`：新增日期前后边界及解锁覆盖、产品评论兼容性、营销与服务通知标签回归。保留上游模板多样性和诗词干扰字测试。
- 上游两条 10086 充值促销已经标为垃圾短信，已核对保留。新增测试覆盖合法促销=true，账单/消费/取件通知=false（静态与生成模板）。

## 验证状态

- 对本轮三个文件的 `git diff --check` 通过。
- 静态计数确认常识题 216 条。
- Unity / EditMode 测试未运行，交由主任务统一验证：`ParametricTemplatesStayIn2016AndRespectTruthAndTrick`、`DatedTemplatesWaitForTheirEventsAndCarryTheirReleaseDate`、`GeneratedRealReviewsKeepProductDetailsCompatible`、`MarketingIsSpamButServiceNotificationsAreNot`，以及现有常识题测试。
- 检查时全局 `git diff --check` 曾显示 `XgYyTalk.cs:288/292/299` 冲突标记；已告知主任务，可能正在被另一整合工作处理。本分工未触碰该文件。

## 已报告给主任务的联动冲突 / 未处理事项

- `Core/XgCatalog.DataPacks.cs:18` 的短信教学仍写“左划是诈骗 / left for scam”。
- `Desktop/MiniGames/XgPackContentGames.cs:22,70` 的分类标签仍写“诈骗短信 / Scam”，72 的练习收件箱反馈也写诈骗。合法营销被归为垃圾短信后，这些应改为垃圾短信 / Spam。已先报告主任务，不跨分工修改。
- 房租考题仍待用户确认，由主任务处理；这里没有改房租题。模板中的“陌生号码冒充房东要求转账 1350”保留为诈骗短信示例，不代表实际租金。
- `XgLogic.cs` 上游日历前后 14 天题型保留，本分工没有进一步修改。
- 未扩展四选一诗词、三分类弹幕、私教/众包流程、计时/连击或任何旧任务。
