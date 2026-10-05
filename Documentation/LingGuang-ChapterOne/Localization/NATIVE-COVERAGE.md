> 本文是数据制作阶段记录。最终集成与运行验证见 README.md；最终家庭应用英文名为 Home，原生主页通过原生 key 区分。

# Main UI 原生与场景翻译数据覆盖

## 交付边界

本工作项只创建翻译数据和本说明；未修改 DreamOS 供应商代码、预制体、语言资产、场景 YAML、全局配置或存档，未调用 Unity Editor。以下是**数据静态验证**，不是场景接线、CJK 字体、运行时切换、动态窗口或构建验证的通过声明。

交付文件：

- `/Users/kk/competition/Assets/LingGuangV05/Resources/Localization/NativeChinese.tsv`
- `/Users/kk/competition/Assets/LingGuangV05/Resources/Localization/SceneText.json`

## 原生表：232 / 232

来源：`/Users/kk/competition/Assets/DreamOS - Complete OS UI/Localization/Languages/English (en-US).asset`

来源 SHA-256：`cf8ac7226f5114030400e155bd0bb3a8e98085cbec79ee62e25d4b11fb08e36e`

| 原生 tableIndex | 表 | 源条数 | 简体中文条数 |
|---:|---|---:|---:|
| 0 | UI | 124 | 124 |
| 1 | Game Hub | 3 | 3 |
| 2 | Mail | 8 | 8 |
| 3 | Messaging | 80 | 80 |
| 4 | Notepad | 4 | 4 |
| 5 | Web Browser | 7 | 7 |
| 6 | Widget | 6 | 6 |
| **总计** | | **232** | **232** |

TSV 为 UTF-8、无表头，一行一条：`tableIndex<TAB>key<TAB>简体中文内容`。字段间使用真实制表符；内容中的换行写作两个字符 `\n`，内容中的制表符如有应写为 `\t`。消费端必须解码转义，不应按空白字符任意拆分。源 CRLF/CR 换行归一为 LF 语义。保留拼写不规范但承担查找职责的原始 key，例如 `LabelConntect`、`NoutFoundErrorTitle`，不擅自纠正 key。

已执行的静态断言：

- 恰好 232 行，每行恰好三列，中文内容非空。
- `(tableIndex, key)` 与原始 English 资产集合完全相等，无重复、遗漏或多余项。
- 每条译文的富文本标签序列与来源一致。
- 关键触发词、API 调用、菜单路径、金额、百分比、数值与专名保持：`What's up?`、`SEQ_0`、`StoryTeller`、`'Call After'`、`'Debug StoryTeller'`、`MessagingManager.CreateStoryTeller("Chat ID", "StoryTeller ID");`、`Create > DreamOS > New Mail Item`、`'Notepad Manager'`、`Erin`、`$1.000.000`、`100%`、`21,196`、`13,171`。
- 原生七张表顺序未改变。原生 LocalizedObject 按整数索引取表，集成时也必须保留该顺序。

原生 SampleNote2Content 是对供应商样例文字的翻译，不是本项目新增或核验后的历史研究结论。SampleMail/ExampleChat/StoryTeller 样例保留为示例内容，没有将其包装为用户真实消息。

## 场景文案：225 对，覆盖 228 种原始文本

来源：`/Users/kk/competition/Evidence/Localization/scene-unkeyed.json`

来源 SHA-256：`64b13076d02f64919a3541c24016bff95a8d362655d0a958e44b81f80fda28f7`

源导出包含 993 个 TMP 记录。排除 `isInput=true`、非空原生 key、空文本后，得到 287 种不同文本；它们不全是需要翻译的 UI 文案，也包括数字、专名与占位数据。

`SceneText.json` 结构为 `{ "entries": [{ "zh": "…", "en": "…" }] }`。225 个不同双语对覆盖其中 228 种原始文本；某些中英文版本已分别存在于场景，所以原始文本覆盖数略高于双语对数。每对都至少有一侧精确匹配源导出。使用 JSON 原生 `\n` 转义保存段落，保留源文字中的有意义空格和标点。

### 有意排除的 59 种原始文本

- 数字、算式、时钟、日期、尺寸/下载进度、纯符号：`+`、`-`、`.`、`0`、`00:00`、`00:00 PM`、`0:00`、`1`、`10 MB`、`10 MB / 10 MB`、`12 +`、`12+12`、`12:00`、`2`、`2021.01.01`、`2023.1.1`、`24`、`3`、`4`、`5`、`6`、`7`、`8`、`9`、`=`、`×`、`÷`、`—`、`□`、`▣`。
- 物理按键/设备标签、品牌与产品/应用标识：`C`、`d`、`STD`、`HM-2310`、`HONGMENG-DESKTOP`、`DreamOS / Unity`、`Internet Explorer — hongmengos`、`Discord`、`Twitter`、`YouTube`、`Wi-Fi`、`YY`、`YY 2010`、`hongmengos`、`hongmengos\nAero 2010`、`hongmengos 2010`、`寻宝`、`灵光.exe`。多行实物后盖铭牌 `HONGMENG HM-2310 / 16:9 WIDE LCD DISPLAY / 电源规格 / SERIAL HM2010-2311` 也保留，避免改动品牌、序列号和硬件印字。
- 个人姓名、用户信息和版本模板：`Jason`、`老周`、`User Name`、`{Last name here}`、`{Name here}`、`{Username}`、`{x.x}`。
- 安全问题的示例用户值 `My favorite teacher...`，网址 `www.url.com`，以及零宽字符 U+200B。

复合 UI 标签中的专名不改动，例如 `老周  /  Online`、`Chat with 老周`、`灵光.exe  —  Neural Network · Skills · My AI · Exam`、`寻宝  —  Computer Hardware Shopping`。其中保留的汉字是姓名/应用名，不是遗漏翻译。

### 消费端必须处理的语义边界

1. **不是按全场文本运行替换的词典。** 仅用来在安装/绑定时识别确定的静态 authored label；输入内容、真实用户姓名、安全问题答案、任意聊天文字、笔记和运行时数值不应绑定。
2. 某些“标题”“价格”“当前订单”等文字是动态 presenter 的初始占位。表中有译文不等于应绑定这些字段；动态字段由对应 presenter 格式化并响应语言事件，静态 binder 必须排除这些路径。`10 Songs` 只是场景模板实例，不是通用计数格式；歌曲数量变化需要动态模板。
3. 英文值无重复；中文有三个大小写别名组：`New Message` / `NEW MESSAGE` → `新消息`，`Playlist Title` / `PLAYLIST TITLE` → `播放列表标题`，`Video Title Here` / `VIDEO TITLE HERE` → `视频标题`。按英文来源绑定时保留原条目身份；反向中文查找可取每组首条规范写法。不要对中文键直接 `Dictionary.Add` 而忽略重复。
4. `Play` 在媒体与游戏按钮间有语境差异。通用映射和原生 `LabelPlay` 都采用“播放”；游戏入口如需“开始游戏”，使用路径/组件级别覆盖，不要把全局 Play 翻译改为游戏专用词。
5. 原生 `MessagingTitle` 翻译为“聊天”，场景独立 `Messaging` 标签为“即时通讯”，用来与独立导航 `Chats` →“聊天”区分。原生 key 控制的文字以原生表为准。
6. `Home` →“主页”；第一章“家庭”应用 → `Household`，避免把应用名反向映射为导航 `Home`。
7. 不通过这两个文件修改 AppLibrary.appTitle、appID、聊天 ID、StoryTeller ID、文件路径、保存键或其他逻辑标识。
8. Chinese 默认语言、历史语言回退、按钮事件、inactive 窗口、生成行、AM/PM 和月份、CJK 字体均需由集成层处理，本数据不宣称已解决这些运行时问题。

## 未执行的验证

- 未运行 Unity 编译、EditMode/PlayMode 测试或游戏构建。
- 未验收按钮交互、首次启动默认语言、重启恢复、动态内容重生成、失效保存 ID 回退。
- 未验收字体缺字、布局溢出、翻译后按钮宽度、多行行高和字重。
- 未验证供应商示例应用的全部原生缺陷，亦未修改这些应用的行为。

后续集成验收必须把“232/232 原生数据齐全”与“整个 Main UI 已可双向切换”分开报告。
