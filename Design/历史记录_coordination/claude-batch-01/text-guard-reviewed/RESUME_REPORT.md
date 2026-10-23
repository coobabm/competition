# 敏感文本小批次：联系方式与精确网址例外

按主控后续范围收敛，只增强现有 `XgEndingTextGuard`，不新增 `ContentFilter`，不修改生产。候选补丁 `text-guard-reviewed.patch` 仅两个已有文件，SHA256 `86b2dbdb12c075a541eaab619f0b27ebcf972e9f1fbf7879ef15159183d7e393`。

## 行为

- `Filter` 的动态文本在 Unicode 标准化后先检查联系方式，防止被 TMP 标签移除而漏查；移除格式、年代词之后再次检查，防止拼接出新的未检查联系方式。
- 覆盖连续大陆手机号、带 QQ/扣扣/企鹅标记的号码、常见邮箱、显式协议网址、www 和有限常见裸域名。不新增短信/手机号分段拼接、模糊谐音或语义分类规则，不宣称覆盖所有联系方式。
- 已登记数据网址例外直接复用生产 `XgSim.ResolveDataUrl`。当前真实登记只有 MNIST。接受其已支持的 http/https、主机名大小写和末尾单斜线；不搬入旧设计里未登记的 WMT/Wikipedia/SMS 地址。
- 不使用旧 ContentFilter 的 `Contains` 或字符串 `Replace` 白名单。域名后缀、相似域名、userinfo、非白名单路径、query、fragment、编码、点段、双斜线、显式端口、第二个非白名单链接均不得借可信地址片段放行。生产 ResolveDataUrl 已经严格，本批复用它；没有宣称生产原来存在旧候选的子串漏洞。
- `FilterReviewed` 完全不改：已审稿题目、故意错误陈述与2017史料保持原文。普通正文仍用4096字符限制，未套用姓名16字/文件名限制。
- 原有窄辱骂词表与年代规则不改，常见游戏词（关机、病毒、诈骗、破产、下载、训练、赌场、888、21点、熊猫烧香、灰鸽子）保持可用。不设整句关键词白名单来屏蔽其它联系人匹配。
- 正则有50ms超时，超时拒绝该动态文本。不记录玩家原文，不联网，不调用模型，不添加 UI 或现代热线。

## 暂不纳入这一小批次

旧方案的 A/B/C/care 实际词表未提供，只存在配置和 allow.txt。按主控要求不引入广泛新词库、不扩大现有玩家流程，本批不添加空的分档API，也不接线 YY、贴吧、名字验证或 LocalLlm 全局输出。因此不能声称“全游戏统一过滤”或“输入三档、自伤关怀已经完成”。现有仅调用 XgEndingTextGuard.Filter 的终局动态文本路径会获得本次联系信息检查。

不新增12356，不将现代热线混入2016 NPC台词，不引入旧方案的进程全局 `CareNext`（存在串联系人/存档风险）。后续如果单独批准聊天关怀流程，应使用当前请求/会话绑定的确定性回复；不应靠旧草案的一次性全局标记接线。

## 验证

- 针对真实脏生产 `git apply --check` 通过；补丁按实际文件换行字节生成，不写生产。
- 隔离 .NET harness 编译并执行 **35个NUnit case，全通过**，包含原6个测试及新增29个case。代码使用候选 guard、实际生产 Core/XingGuang.Core DLL 中的 AiNamePolicy/EraLexicon/ResolveDataUrl。为直接替换 guard 类型，harness 显式抑制预期的同名源码优先警告 CS0436；它不是整个 Unity 程序集重编译。
- 日志：`verification/guard-checks.log`。harness 与测试代码齐全；没有启动 Unity、模型、Player 或做全场景 UI 验证。
- 主控 Unity 检查 filter：`LingGuangV05.XingGuang.Tests.XgEndingTextGuardTests`。

## 文件

1. `Assets/LingGuangV05/XingGuang/Core/XgEndingTextGuard.cs`
2. `Assets/LingGuangV05/XingGuang/Tests/Editor/XgEndingTextGuardTests.cs`

只有局部补丁，不要覆盖整个目录。和 ending-A 作者 followup 分开合入，不修改 R3 自动执行/钉卡行为。
