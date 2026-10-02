# 涌现 · Unity 可玩版

实现日期：2026-10-01。工程：`/Users/kk/competition`。Unity：6000.4.5f1。

## 开始游戏

打开 `Assets/Emergence/Scenes/Emergence.unity`，按 Play。

也可使用编辑器菜单 `Tools > Emergence > Open Game Scene`。原有 `SampleScene` 保留；游戏场景已加入构建列表。

1. 右侧选择一个候选神经元，再点击棋盘空位。悬停空位预览新增连接。
2. 可使用随身记忆改造连接，或移动一个节点。相邻节点自动连线。
3. 点击起点查看前两拍预演，再按“释放脉冲”或空格。
4. 达标后领取奖励、进入商店。每三轮有一个 Boss，共十八轮。

上方可调节 1× / 2× / 4× 速度、静音、打开规则。结算时点击主按钮可立即结算；Esc 暂停或返回。突触可拖动换位，也可点击后用左右按钮调整。

## 已实现内容

- 半径 4 的 61 格六角棋盘，7 枚初始节点，每轮三选一生长一次。
- 普通、敏感、蓄能、延迟、远投、核心六类节点。
- 镀金、共振、多重三种变体；永久改造跟随节点 ID。
- 两次刺激、同步到达、跨刺激保留电位与来源记录、延迟传播。
- 回声重计分，不重新传播；事件流独立于动画与播放速度。
- 灵感 × 倍率计分、九种结构型，自动选择实际得分最高的结构。
- 15 个突触、9 份记忆、4 种药品、9 类星图。
- 五突触槽、六格分类背包、购买、出售、弃置、刷新、利息与超额奖励。
- 六个 Boss：隔膜、节拍器、静默、迟滞、遗忘、双生；Boss 后免费突触三选一。
- 自动存档、同种子重试、新种子、失败复盘、通关画面。
- 原生 uGUI 中文界面、程序绘制神经网络、光脉冲、计分浮字、程序合成音效与环境声。

## 本版统一的规则

节点的激活资源称为“电位”，得分资源称为“灵感”。每次刺激的倍率初值为 1；先处理加法，再结算乘法。节点变体与指定突触可以随重计分再次触发，但“复读”只监听真实的首次放电。

结构型按本次实际得分选择，升级已有结构不会压低得分。普通加乘道具换位不改变乘积；“镜面”复制左邻的乘法效果，“静默”禁用第一槽，二者使顺序具有明确用途。

药品每轮至多使用一份，效果只持续下一次刺激。第一次刺激后永久改造、移动及突触换位锁定；第二次刺激前仍可使用药品。无效目标不消耗物品。

目标曲线集中在 `Catalog.RoundTargets`，从 300 增长至 200,000。数值经过自动策略诊断，但尚未经真人长期试玩。

## 文件结构

| 位置 | 用途 |
| --- | --- |
| `Assets/Emergence/Scripts/Core/EmergenceEngine.cs` | 确定性传播、结算、经济与进度 |
| `Assets/Emergence/Scripts/Core/EmergenceCatalog.cs` | 节点和物品文本、结构数据、目标曲线 |
| `Assets/Emergence/Scripts/Core/EmergenceModels.cs` | 可序列化局内状态与事件 |
| `Assets/Emergence/Scripts/Presentation/EmergenceApp.cs` | 界面流程、存档、动画播放 |
| `Assets/Emergence/Scripts/Presentation/NeuralBoardGraphic.cs` | 神经网络网格绘制 |
| `Assets/Emergence/Scripts/Presentation/EmergenceAudio.cs` | 音效合成与音量控制 |
| `Assets/Emergence/Editor/EmergenceSetup.cs` | 场景创建及打开菜单 |
| `Assets/Emergence/Editor/EmergenceEngineChecks.cs` | 规则验收与多种子诊断 |
| `Evidence/Emergence/` | 实际 Unity 测试结果与运行截图 |

UI 以 1600×1000 为布局基准，使用 Expand 缩放并居中，在实际 1920×1080 Game 视图验证。中文字体为 Noto Sans CJK SC，授权文本随字体位于 `Resources/Fonts/OFL.txt`。

## 存档

存档采用临时文件写入后原子替换，并保留上一份 `.bak`；主存档无法解析时会尝试恢复备份。保存位置是 Unity 的 `Application.persistentDataPath` 下的 `emergence-run-v1.json`。每次已完成的编辑、购买、回合转换及完整刺激结算后保存。动画不写入存档；中途关闭会回到最近一次完成的操作。

存档属于本机局内状态，不上传网络。暂停菜单的新局操作会明确提示替换当前进度。

## 验证与边界

实际 Unity 内的 22 项规则检查全部通过，结果为 `Evidence/Emergence/engine-checks-unity.json`。详细数值诊断见 `Documentation/QA.md`。真实鼠标已验证生长、记忆加回声、放电、领取奖励、进入下一轮；另在运行中检查商店按钮、存档恢复与 Boss 界面。

这个版本是完整可玩的十八轮闭环，不是已经经过发行验收的最终产品。v0.3 的玻璃变体、复制/献祭记忆、孤勇突触、礼包、局外解锁与随机替换 Boss 尚未纳入本版。没有进行真人五人试玩、商店发行构建或长期性能压力测试。
