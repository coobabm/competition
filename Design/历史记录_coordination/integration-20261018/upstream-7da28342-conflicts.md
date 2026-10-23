# 7da28342 上游剧情与 world / YY 补丁兼容性审计

审计范围：30fbe6e4 → 7da28342 的6次提交。仅读取主仓库；候选与冲突文件均在 coordination，不改 main，不干扰正在运行的采样。

## 一、补丁机械兼容性

按原顺序在独立目录逐份检查、应用后：

| 单元 | 结果 | 局部处理 |
|---|---|---|
| legacy-port-main-30fbe6 | six_stage_lines.json 一段拒绝 | 保留上游新增剧情结构，把 s2_name_quiz/pick/moved/choice_ask 继续改为主人公独白，并保留两条 think key；同步英文沿用已移植 Localization |
| world-rent | 可应用 | 不覆盖上游内容 |
| world-news-presets | YYChatHub.Send 一段拒绝 | LaoZhouHeard 与 SendGroupPreset 是不同联系人的分支，均保留；预设在普通群聊概率回复之前 |
| world-life | 可应用 | 保留上游 zhou_card 对第3章借卡过场的排重条件，不能用旧文件覆盖；保留 Finale 新钩子 |
| yy-learning | XgYyTalk 两段拒绝 | 上游已经 partial，接受；RememberLearningSource 与新增 NightHeard 均保留；Notebook/Persona 主动问题在 NightPlaying 期间禁止，避免抢占夜谈 |

候选：legacy-world-yy-rebased-7da28342-context-only.patch，56 个原补丁所属文件。对干净 7da28342 所属文件 git apply --check 通过，JSON 全部可解析。

**此候选仅完成上下文重基，下面的上游设计冲突尚未消除。不能据此声称可以直接发布。**

证据：upstream-patch-compatibility.json；逐补丁 *.upstream-apply.txt；upstream-7da28342-audit/ 下 .rej 保留原拒绝内容供复核。未进行新上游完整编译/Unity验收。

## 二、必须语义重写的冲突（保留玩法）

### 1. 老周从猜测到知道秘密程序——直接违反“除了玩家任何人都不知道这个应用程序”

文件/入口：XgSim.ZhouChat.cs 的 ZhouStageNoteNow、ZhouNode(confess1/2/3)、ZhouApply、ZhouSteerNow、ZhouEndingLine；LaoZhouYY.cs 的 StageNote/Told/Ending；LocalLLM/LlmPersonas.cs 的 System(laozhou)、Regard(laozhou)；XgSim.CardLoan.cs 的 zhouKnows 分支。

上游会设置 S.zhouKnows=true，然后把当前结构、验证准确率、学会能力数量通过 ZhouLabLine 注入人类提示词。光改台词或藏掉“坦白”按钮不够：老存档 true 仍会泄露。

建议：保留完整3轮对话、阶段科普、奖励、怀疑/亲近反馈和旧存档字段；将“坦白程序”改为“分享公开论文里的想法/虚构实验案例”，老周只讨论一般机器学习。将实际训练遥测提示迁移到主人公自学便签/内心独白；人类模型上下文永不注入 ZhouLabLine。旧 zhouKnows 保留用于旧进度兼容，但不再代表知道秘密程序；显示层解释为交流过机器学习，不抹掉已经选择的历史。新增明确 privacy-safe 语义字段或版本门槛再迁移，不能简单重置全段剧情。

借卡保留：改“我知道你那个程序离不了它”为“手边有空闲显卡吗”；不读取实时显卡数量替玩家告诉老周。机箱教学、拔卡、归还与答谢全部保留。新 clStep 与旧 life-event 的排重条件需保留。

### 2. 阿杰手机备份——网友上门、进房拿U盘、群内追问私人程序

文件：XgSim.PhoneSort.cs；DesktopBridge/Story/AjiePhoneSort.cs（Start/次日Morning/Ask/Finish）；six_stage_lines.json 的 hook_s2_name/hook_s2_moved。

上游虽写“阿杰不知道AI”，却预设他知道租住地址、亲自进屋放盘、次日趁玩家睡觉进入拿盘；群聊要求玩家帮忙整理四千多张图、追问“写了啥程序”。群成员不是现实串门好友，且不得找主角处理私人任务。我们的四句独白补丁不会自动修掉新增的备份篇章和 hook。

建议：保留 4312 张、8 文件夹、28 张可检查图、猫耳错分、旋转照片失认、31 张敏感照片清理选择、逐张纠错反馈与阶段完成门槛；改成“我自己的旧手机备份”。入口为玩家点击旧U盘或便签，次日总结改为我对AI的复盘；群里只保留无关的斗图/换手机闲聊。身份证分支改为“发现旧办事照片，决定从备份删除/保留”，没有替阿杰作主或群聊传身份证的问题。psStep/psPicks/psFixed/psIds/psAlibi 字段和完成记录原样兼容，不直接把 psStep 标完成。psAlibi 可在新档重解释为“对整理方法的自我归因”，旧值仅用于回顾不重新发群消息。

### 3. 是否夜的“我能找到工作吗”与已工作两年冲突

XgSim.YesNoNight.cs round id=job 当前问题为“我能找到工作吗？/Will I find a job?”。建议保留同一round id和所有答案分布/内心反应，换“我能升职吗？/Will I get promoted?”。若老档已问过，日记保留真实旧文本，不把玩家历史问题重写成新句。

夜谈中的阿杰普通脑筋急转弯属于网友闲聊，不必一概删除；咸鱼本鱼是独立联系人，不能把“群网友不私聊”的限制误套到所有亲友。夜谈的阶段拦截必须保留，避免绕过新上游必看过场。

### 4. 晴雯13段支线有虚构玩家历史

QingwenArc.cs 中“你上次说戒了的”“那你昨晚为什么十二点才回我”等无对应历史条件，违背不能发生玩家没做的事。

建议保留13段亲情/闺蜜/考试/未来事件、好感和选择；把无证据追问改成当下愿望“可别玩到忘记回我”“忙的时候给我留一句话，好吗”。若要保留原句，需真实聊天/时间戳记录满足条件，否则用中性版本。不能只按训练次数认为玩家冷落女友。

“我室友”“宿舍蟑螂”说的是晴雯及她的同学，不是主人公有室友，不要全局替换。FlagToldMl 是玩家明确选择自学机器学习，并不一定泄漏应用，允许保留通用学习内容；禁止后续推断神秘程序的来源、名字、实际训练成绩。礼物地址建议用“她的住处”兼容校园/校外变化。

## 三、新上游与本次功能交叉风险

1. **红包被支线吞掉**：YYGirlfriend.OnPlayerLine 新增 `if(ArcHeard...) ... else if(packet>0)`；ArcHeard 对任何自由输入也返回true。支线正开时点红包，会先消费 arcOpen，跳过 GirlfriendRules.Packet。建议明确交易优先：红包/有效亲手送礼先处理，再按需要给支线保留未答状态。不要借此恢复AI代送。
2. **礼物状态兼容**：上游新增 arcOpen/arcAt，和 GiftHeld/GiftToHer 独立，字段都保留；Repair 必须同时修 arcOpen 空值和旧订单。不能整文件复制旧 GirlfriendState/Rules.Logic。
3. **YY题本/性格与是否夜**：新上游 NightHeard 先于普通答复；上下文候选已保留它并防止抽屉在 NightPlaying 抢问。严格解锁只在普通 Deliver 回答落地登记，不把夜谈、广播、好友消息视为AI对这句原话的答案。
4. **群聊预设与剧情按钮**：ChoicesOf 先返回临时/standing剧情选择再返回3预设，保留该优先级。PhoneSort若按建议迁回玩家桌面，不再占netbar回答路由；不要覆盖其他联系人的 standing choices。
5. **过场互斥**：新 LaoZhouYY、AjiePhoneSort、ZhouCardLoan 只检查 StoryFocus.Active；需要确认其自身播放期间是否持有统一队列/焦点，尤其夜谈每段之间。全局升阶串行化应覆盖新场景；不能只在旧9事件上加门。
6. **事件预算**：原9条和100条共享每阶段2次随机事件预算；新第1–3章故事是主线过场，不应误算成随机事件从而被预算吞掉。借卡老事件应继续避开第3章同题材新场景。
7. **新闻/租金**：上游未新增相同功能，本次手动转账和43条新闻可继续；43/45差额仍是待确认，不用上游新剧情假冒两条新闻。
8. **音效**：7da28342 只播连击 kill-streak 录音、前两提交改变阶梯与3秒重置。由主代理与教页代理保留其语义并协调用户三音量；本审计不改音频。

## 建议合并顺序

先按用户首批性能/科技拖动/开局/教页完成Unity验收；新上游进入隔离重基后保留所有必需新源码和meta，再应用上下文候选；随后逐条执行上述语义重写并写整合记录。不得将这个 context-only 补丁当成已解决人设冲突的最终版。
