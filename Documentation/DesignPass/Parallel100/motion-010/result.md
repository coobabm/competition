# motion-010：暂停、低帧率与长帧间隔下的自然结束

日期：2026-10-13（Asia/Singapore）。结论：发现一处可静态证明的提前清理路径，交付候选补丁；未修改生产文件。未读取新策划正文，未启动 Unity／游戏，未访问玩家存档，未创建聊天或子 agent。

## 结论与原文件证据

- `Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:214–223` 使用 `Time.unscaledDeltaTime`，每次效果推进量上限 `.05f`。因此 `timeScale=0` 本身不会阻塞文字反馈。不能把 `.18/.12` 秒理解成低帧率下严格的墙钟截止时间；10 fps 时至少需要 4／3 次有效效果更新，2 fps 时同样需要 4／3 次。长帧不会一次跳过完整曲线。本任务保留该已有时钟和旧动效。
- 同文件 `:298–306` 在 `p.t / p.life >= 1` 时先准确恢复保存的 `localScale` 和 `anchoredPosition`，再移除 punch；`:310–315` 的取消路径也复位并清空。现有恢复逻辑无需改动。
- `Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs:40–53` 另在 `LateUpdate` 累计同样的 capped delta，以独立 `elapsed >= duration` 提前调用 `Clear`。两处公式相同并不保证计时起点相同：已存在的私有 XgJuice 在某帧先执行 Update，然后 EventSystem 触发再次按下并将 punch 时间置零，随后按钮 LateUpdate 仍计入本帧 delta。这是未约定 Update 先后顺序时允许的执行路径。
- 具体反例：每帧 raw `.1` 秒，触发帧的效果 Update 已结束。普通反馈在第 4 次 LateUpdate 被清理时，punch 只推进了 `.15` 秒，尚未到 `.18`；减少动效在第 3 次 LateUpdate 时仅推进 `.10`，尚未到 `.12`。虽然 Clear 仍能正确复位，但会截断回弹尾段。暂停不改变此反例；大帧间隔使一帧误差达到 capped `.05` 秒。
- `XgUi.cs:188–210` 已连接专用按钮与 Label，无需改动。`MotionButtonChecks.cs` 现有检查以及 `Documentation/DesignPass/Agents/motion-task-001.md` 已记录只覆盖登记／取消，未推进自然动画帧。本任务补充该边界，不重复提交现有实现。

## 候选变更与接口合同

`changes.patch` 是带上下文的统一差异，头部列出两个目标当前源文件 SHA256。仅包含：

1. XgJuice 新增只读 `public bool HasActiveKnock(RectTransform target)`，目标仍在自身 punch 集合中返回 true，空／已销毁／已移除目标返回 false。Knock 自然完成先恢复变换再移除；Clear／OnDisable 取消后也返回 false。查询不分配集合、不推进时间、不更改目标。
2. 按钮删除重复计时，保留传给 Knock 的局部 duration；LateUpdate 使用 `!pressFx.HasActiveKnock(Label)` 判断自然结束，再复用 ResetPress 停用私有效果。不可交互、空 Label、宿主不可见／禁用等立即取消条件仍保留。

此方案以效果本身的进度作为完成依据，不人为延后固定一帧，不更改 capped delta、动画力度、曲线、减少动效参数、宿主业务效果或音效。私有文字栈没有调用 HitStop；宿主的全局 timeScale=0 不会使其停滞。若未来主动给私有栈加 HitStop，查询也会等待其实际恢复推进，而不是让另一个计时器提前结束。

## 校验结果与依赖复核

已实际运行 `python Documentation/DesignPass/Parallel100/motion-010/verify_clock.py --write-patch`：退出码 0，核对下列 5 个源文件哈希，生成候选差异，并完成 84 组 float32 时钟模型断言。覆盖 `.18/.12` 秒、60/20/10/2 fps、2 秒间隔、零增量后恢复、混合增量、触发帧已更新／未更新，以及 timeScale 0／0.1／1。原实现反例分别在效果年龄约 `.15/.10` 时提前结束；候选模型等到效果移除才清理，正常顺序不额外加帧。

结束前再次执行无写入参数的验证脚本，5 个依赖哈希仍与基线一致、84 组模型断言仍成立；`git apply --check` 退出码 0，仅检查候选补丁可应用性，没有应用补丁。

该脚本只检验源码前提与调度模型，不能证明 Unity 实际调度、对象生命周期或视觉效果；它未编译或执行 C#。哈希变化会直接失败，要求重新审查，避免把旧补丁套到整合中的新代码。有限完成的前提是后续仍有足够的正 unscaled delta；应用完全挂起／不再渲染时不可能在无帧期间展示复位。

基线 SHA256：

| 文件（工程相对路径） | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs | 1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78 |
| Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs | 2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC |
| Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs | A81D4AA3591C28EFA023FB2CAB4DCC976CC581A1B132CFE910FD181CEDC50F14 |
| Assets/LocalBuild/Editor/MotionButtonChecks.cs | 22E10F5BD0C8DC49168D401F727214E47B9782B7B26C6D801C48FB3D8D59530E |
| Documentation/DesignPass/Agents/motion-task-001.md | 755DBC9C34002157AE71792FA8484A02F0776311C36CF586518DC97D9C99B9EE |

## 待整合与待验证

- 整合负责人：01a130ed-1766-7ee2-b60d-0d4d755c4e33。仅提交专属目录，不向其他聊天发送消息。补丁须同时整合按钮调用和 XgJuice 查询接口；若不接受新增查询接口，应提供等价的效果完成通知，不能单独删掉旧清理条件。
- 统一编译：未运行；生产源码未应用补丁。
- Unity 实际运行：未运行。使用临时隔离对象、非默认 resting scale `(1.2,.9,1)` 和 position `(7,3)`；普通／减少动效各在 `timeScale=0` 下完成一次反馈，确认自然完成后原变换准确恢复、私有组件停用且 host punch 保留。finally 恢复原 timeScale 并销毁临时对象，不读取玩家存档。
- 调度边界：在已创建过私有栈后，于该栈本帧 Update 之后再触发按下，验证其 punch 仍 pending 时 LateUpdate 不清理。用 raw `.1/.5` 的隔离推进或实际低帧率验证；不要仅修改 `Time.captureDeltaTime` 后假定它控制了 unscaled delta。记录真实 unscaled delta 与实际执行顺序。再验触发早于效果 Update 的路径，避免人为多等一帧。
- 长帧间隔：反馈中制造可控间隔，下一帧按当前 capped clock 继续，随后自然结束；不要求单个长帧就结束整个反馈。逐帧核对 pending → 已复位／移除 → 停用顺序。零增量帧允许不推进，恢复正增量后应完成。
- 视觉验收／截图：未进行。主聊天观察普通与减少动效尾段、低帧率和暂停场景中的文字复位；低帧率本身缺少采样帧，本补丁不承诺消除掉帧卡顿。

本交付仅处理本条下的反馈时钟与自然完成，未标记“三层反馈”全局原则或整份策划完成。
