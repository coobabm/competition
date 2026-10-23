# 便签漏掉数据导入：产品修复候选

结论：这是实际产品引导缺口。原有`FollowingOnlyTheNoteReachesTheSecondAbility`也没有模拟安装，但在产品便签明确给出安装动作前，给测试自动安装会掩盖缺陷。本候选先补产品链，再让测试仅跟随新出现的明确指令调用安装API。

没有启动Unity/Player、修改生产、提交或发布。6个文件小补丁，基线逐文件SHA在`baseline-manifest.json`。

## 最小复现

`verification/baseline-trace.log`用当前导入程序集与生产测试的同一Follow方法：买短信包→参数节点→练到C→继续标注，6000步仍能力1、有效样本5992。资料中已有1个ready短信包，但InstalledBatches=0、CanInstallData=false。生产XgGuide没有DataFile安装建议，GuideNote没有打开资料夹的导航。

候选诊断`verification/candidate-trace.log`：第11步提示训练曲线、第12步提示数据导入、第35步提示资料夹拖入练；安装后5批短信数据计入样本，第97步进入升阶确认并达到能力2。此为核心确定性模拟，不是自然完整玩家流程。

## 产品行为

- 有实际下载完成、未安装的虚拟文件时，便签先按现有节点依赖给出购买建议，然后给出中英短句“资料夹打开「训练数据」，把待装数据包拖到「练」”。当前条件仍尊重开局、科技页、练页门禁与终局主线。
- `train.data`本来就依赖`train.curve`，本候选没有新增前置或改变费用。本次无折扣开局实际NodeCost分别¥3与¥50；价格仍从产品NodeCost读取并按现有BuyNode扣费。资料夹/安装/网址是一份已有数据导入授权，不额外购买资料夹，拖入安装不再次收费。
- 便签点击与顶栏“去做”只打开练页和资料夹的训练数据目录，不自动购买、不调用InstallDataFile、不注入样本。玩家仍须选择文件并真正拖到练页。
- 文件夹导航检查当前Sim绑定及CanBrowse，旧存档引用无效时不打开；已有拖拽payload、同存档、入口门禁、目标可见性与重复安装保护都保留。
- 测试Follow仅在便签明确发出`data.install`时调用真实InstallDataFile；原路径的购买/训练行为继续沿用。新增断言验证购买扣款、导航不安装、导入无第二笔费用、重复导入不增加样本。

## 验证

- 全部24项XgGuideTests（含新增导入用例）在独立.NET运行：24过/0失败，`candidate-all-guide.log`。
- Unity自带Roslyn按当前Bee引用隔离编译：XingGuang.Core、Assembly-CSharp、Assembly-CSharp-Editor均退出0，日志在`verification/unity-compile/`。现有未用事件、旧API等警告保留，未宣称零警告。
- 普通`git apply --check`通过，LF字节补丁，不需ignore-space-change。
- 真正Unity/UI执行仍待主控，不能把上述编译或核心结果当作桌面输入通过。

## 现有runner的有限UI增量

入口仍是`FilesWindowUiChecks.Run`（内部`SidebarLayoutChecks.RunFiles`，不加`-quit`）。只在已有FilesWindowUiChecks末尾追加一个有界片段，无新大型套件：

1. 明确早期已评估的UI夹具，正常BuyNode支付训练曲线、数据导入、短信包，正常Tick推进真实下载；没有注入安装计数或样本。
2. 关闭文件夹与练页，最多6秒等待实际GuideNote更新出data.install，必要时实际点击展开按钮。
3. 用现有LCD坐标映射/真实raycast方法点击第一行便签；最多4秒等待资料夹训练数据目录和练页可交互，确认仍未安装、样本/钱包未改变。
4. 复用现有并排QA布局，通过LCD输入桥进行源文件拖动、实际训练根目标raycast和落下；确认安装后才增加样本，钱包无重复费用。
5. 新增截图`files-12-guide-ready-before-click.png`、`files-13-guide-navigation-before-drag.png`、`files-14-guide-real-drop-installed.png`和独立check文字。

该检查使用已有Main隔离宿主、模拟早期评估进度、关闭叙事驱动、并排布局；它不是物理鼠标或自然游玩，不冒称没有夹具。此前Files测试中其他显式数据记录仍保留原范围声明。任何失败都保留报告，不通过跳过便签或直接调用安装规避。

## 整合顺序

先完成当前Unity进程，再检查基线hash与普通apply-check，应用`patch/guide-data-import-flow.patch`小hunk；不要覆盖整文件。当前生产GuideTests已包含先前终局与已安装批次修复，本补丁在它们之上叠加，全部保留。主控可把现有Files runner接入此次串行UI队列，运行后再完成最终普通Player构建。

最终补丁SHA256及机器结果在`verification/result.json`；未合入生产。
