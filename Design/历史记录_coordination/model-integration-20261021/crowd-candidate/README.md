# 众包规则独立候选（未合入）

未修改 authoritative，未启动 Unity。本目录 candidate 为可审查覆盖文件，patches 为相对权威基线的差异，manifest.json 记录基线及候选 SHA256。保留目标既有 .meta。

## 已准备

- 采用 external-observed 中众包安装前无档位收入、安装后待支付见面礼、档位收入账单、安装提示、功能stage2等更改。未采纳任何 XgCatalog.DataPacks.cs 更改，三个数据包stage保持权威值。
- 众包群聊stage2，删除该剧本固定日期条件；企业群聊stage3，仍需众包安装。Resources与Design两份JSON同步。其它剧本日期未修改。
- 通知要求stage>=2且已安装；队列继续检查原存档身份和当前安装状态。YY群聊引导由YY发送（XgAppAcquisitionHost.cs已核查），因此不会被众包平台通知门禁阻止。
- 安装前的众包升级toast及罚款批次被抑制，已安装stage2可显示实际收入通知。
- 旧档只有fundedAbility>=2或crowdTier>=2才补众包安装授权；不使用stage或totalIncome判断。现有有效appAcquisition保留，不覆盖已有安装记录。
- 调整通知单测及真实Tray队列检查，增加旧档无证据/有账本/有档位和重载案例；保留external-observed收入一次性支付测试。

## 迁移证据审计

Core全目录赋值搜索：fundedAbility默认1，生产代码仅PayAbilityResearch在host.Earn后写入已支付能力编号。crowdTier仅该付款路径提升，或RepairDownloads从fundedAbility>=2恢复；另有范围钳制。未发现根据stage、离线时间或任意总收入生成档位的路径。因此下载修复前后字段均可追溯到历史众包付款，而非仅到第二章。Core的contracts门禁本来就是stage3，未改购买或合同前置条件。

## 验证与剩余设定

最终审核补充：核心 CanSign 与 ContractIncome 均要求众包已安装，避免未安装但带有历史已签合同的存档继续按秒获得企业收入。安装后恢复原合同收入，不重新支付签约费。安装提示改为根据已达成能力判断，避免新档 crowdTier 尚未支付时漏掉安装提示。保留基础企业合同第三阶段及高阶合同原有第四阶段条件。新增未安装签约拒绝、历史合同暂停/恢复和重复签约拒绝回归，并为既有合同/接线/遗物测试明确安装前置条件。构建脚本、候选补丁和 SHA256 清单已同步更新；尚未运行 C# 或 Unity 测试。

追加回归：能力达到第3/6档、安装前分别等待60/600秒，经过JSON序列化重载和实际三步安装，再断言每档见面礼仅支付一次、收入只计算当前0.25秒；两次后续序列化重载仅支付当前2秒收入。测试同时覆盖未完成安装及重复完成安装。静态计时审计：XgSim.Tick 使用局部 left 并当次消耗完毕，未保存安装前待支付时长；XingGuangController 每帧传入 min(unscaledDeltaTime,2)，暂停分支跳过Tick而不累积；PayAbilityResearch补档按固定见面礼而非经过时间。独立离线结算走XgIdleRules的自动协作报价，并非该档位/企业合同的Tick补发路径。本次仅准备测试，未执行Unity。

已执行候选生成、JSON解析、静态调用检查及差异检查。未编译C#、未运行Unity测试；须由主线程统一验证。原权威 XgCrowdNoticePolicyTests 和 CrowdNoticeChecks 必须与候选一起替换，否则旧stage3预期将失败。

待确认的细节：external-observed沿用“每个能力档位一次见面礼”。若玩家到高章节才安装，会补付各已达成档位的见面礼（每档仅一次），不是所有游戏进程只付一笔；本候选保留该现有语义。另，已经被旧的stage宽松规则保存为installed的非空收据无法单凭当前字段证明是否误授权，本候选不撤销已有有效安装记录。

生成入口：python build_candidate.py。执行前可比对manifest基线；不要整目录覆盖其它UI任务改动，采用逐项补丁审查。
