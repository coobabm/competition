# 2026.10.21.1 实际发行题库复核

用户反馈：扩充题目未进游戏。本次只读核对，未重复应用外部补丁、未改源码、未启动 Unity、未读玩家存档。

## 直接检查的发行文件

- `competition-model-integration-20261021/Build/MyComputerAlive-2026.10.21.1-20261020-185518/MyComputerAlive_Data/Managed/LingGuangV05.XingGuang.Core.dll`
- 同目录 `Assembly-CSharp.dll`
- `发布材料/Windows_20261021_1/MyComputerAlive-Windows-2026.10.21.1.zip`

PowerShell 反射加载发行 DLL，读取静态构造完成后的运行时数组（不是仅统计源码）：

| 内容 | 实际题量 |
|---|---:|
| 短句找错字 | 101 |
| 找不同 | 102 |
| 翻译打分 | 101 |
| 挑好答案 | 100 |
| 中式英语 | 101 |
| 谁是卧底 | 100 |
| 唐诗五言 | 100 |
| 唐诗七言 | 100 |
| 成语接龙 | 105 |
| 原诗还是改编 | 100 |
| 弹幕夸还是骂 | 102 |
| 划短信 | 161 |
| 标题党 | 108 |
| 刷单评论 | 102 |

源码路由：`XgPackGames.cs` 中 `danmu_mood` 创建 `XgPackDanmu`；`XgPackContentGames.cs` 中该类通过 `XgTeachingDanmu.Pick` 抽题；`XgTeachingDanmu` 静态构造追加 `XgQuestionBank.Danmu`。

对发行 DLL 的真实 `XgTeachingDanmu.Pick(20160701, new Random(712))` 调用 1000 次，得到 84 条不同且当天已开放的题目。新增题示例包括“网速好稳啊，一局重连了八次。”及“野区都空了，你在发什么呆？”。

ZIP 内核心 DLL 的解压流 SHA256 与反射检查的构建 DLL 相同：

`B0B139DA17248D209438C2FF51095701B09FC7EF08BD3505F27324D82CF807B2`

权威仓库与验证工程的 `XgQuestionBank.cs` SHA256 也一致：

`91F716815547A247682703D9BD8BA5A5F875875CA532E3CDA6CE880BACDA9F6F`

## 判断及边界

该本地最终发行 ZIP 的扩充已经实际接入。不能将用户反馈直接归因为“忘了合并”，也不应该重复打补丁造成重复题。尚未证明用户当时启动的是此 EXE，截图中的外部聊天说明也不等于该版本的运行证据。

弹幕抽样目前有 50% 概率优先抽当月题，且没有近期题目排重；这可导致旧题重复体感，但不是新增题无法进入抽样池。本次未改变这项行为。建议在下一轮真实 UI 验收中标明正在运行的版本并观察新增题，必要时另做近期去重与题量提示。

本次为真实 DLL 的数据及抽样调用检查，不代表全部题目 UI、答案结算、鼠标操作已验收。

## 2026.10.21.2 新包复核

检查新构建目录 `competition-model-integration-20261021/Build/MyComputerAlive-2026.10.21.2-20261020-200654/MyComputerAlive_Data/Managed` 与 `发布材料/Windows_20261021_2/MyComputerAlive-Windows-2026.10.21.2.zip`。

- 新ZIP、新构建与旧版已经真实抽样验证的核心DLL SHA256均为 `B0B139DA17248D209438C2FF51095701B09FC7EF08BD3505F27324D82CF807B2`。因此核心扩充题库和抽样实现字节一致，无需重复抽样。
- 新ZIP与新构建的 `Assembly-CSharp.dll` SHA256均为 `F1FF0849DC6B61D3760A95DF13FFC5F21579305E8F084F0B9023D7C092A8CB03`（与旧版不同）。已直接反射加载新DLL确认静态追加仍生效：短句101、找不同102、翻译101、挑好答案100、中式英语101、卧底100、五言100、七言100、成语105。
- 结论：2026.10.21.2实际ZIP仍包含扩充题库，不是仅引用旧版验证记录。未启动Unity，未读取玩家存档，未修改代码。本检查仍不代表全部题目UI与结算的完整试玩。
