# 「它记得」迁移至玩家命名页

正式源码已冻结，未运行Unity、未commit。

本人变更：
- Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Profile.cs
- Assets/LingGuangV05/XingGuang/Desktop/XgEndingPreparationHost.ProfileMemories.cs（含meta）
- Assets/LocalBuild/Editor/ProfileMemoriesUiChecks.cs（新；冻结时未手工生成meta，Unity可导入生成）
- Assets/LocalBuild/Editor/EndingPrerequisitesActualUiChecks.cs（接入上述helper）

协作：design agent独占XgEndingPreparationHost.Narrative.cs，负责删除整个桌面EndingNotesLauncher及两个按钮，处理每阶段提问派发。本人未修改该文件；保留其原OpenGallery/NarrativeAvailable/来源追溯逻辑。

入口放在.exe玩家命名页的现有顶部导航（近况与性格、性格档案、它记得、更多）。显示真实记忆数，中英对应“它记得（N） / It remembers (N)”。原Current存档绑定、NarrativeAvailable叙事权限及host启用状态均保留，count=0或原权限暂不可用时隐藏；点击再次校验。无新购买/无新存档字段，只打开原ItRemembersGallery。

顶栏按可用宽度分配四按钮，正常宽度保持原前两项/更多尺寸；窄宽度按比例缩减，记忆与性格标签允许11–14字号自适应，避免新增项覆盖更多。

真实UI验收入口：EndingPrerequisitesActualUiChecks.Run（已有Tools/Local Verification/Ending prerequisites actual LCD，独立隔离Play流程）。新断言已插入其原中英循环：
1. 替换fixture后不暴露上一存档的记忆计数；初始0条时profile入口隐藏。
2. 原真实host发现两条来源支持的fixture记忆后，临时隐藏结局面板。
3. 同步调用ProfileMemoriesUiChecks.Run：真实profile按钮回调打开原gallery、检查原记忆卡与来源结构、原返回按钮关闭；读前读后XgState JSON一致。
4. 640px顶栏实际Rect world corners无重叠，恢复原布局。
5. 断言桌面没有EndingNotesLauncher；中英循环分别核对真实数量和标签。
6. 恢复原StoryFocus测试探针与结局面板，继续原已有完整结局前置验收。

测试范围：真实Unity UI控件回调与既有宿主，不声称新增测试使用物理/LCD指针点击，也不伪造新记忆。临时移除StoryFocus测试hold仅发生在同一同步调用内，不跨帧运行其它story host；原其它叙事门禁仍需真实满足。

静态：上述4份C#通过带UNITY_EDITOR的Roslyn语法检查；跟踪变更diff --check通过。Unity语义编译和实际验收待主控。

安装提示的补充核对：ui.Card外框仅Image，内层Image和TMP文字均不截获IDropHandler；LCD使用ExecuteEvents.ExecuteHierarchy寻找dropHandler，因此沿父链到Train root原XgFilesTrainDrop。默认训练内容约1064px，提示正文13号字、48px高，预计两行；850px窄窗首句可能两行加Ctrl行仍有48px空间。尚未真实字体渲染测量，不声称截图验收通过。提示源码已冻结。
## 后续修正（已落地并冻结）

- 按用户新指示删除无效“近况与性格 / About me”按钮及对应返回标题按钮状态；profileOverview字段/创建/刷新/布局已完全移除。
- 顶栏仅保留性格档案、它记得、更多，按三项重排。左栏玩家命名的home导航仍是有效返回路径。
- CanOpenProfileMemories增加seedPanel==null；待答题存在时不展示入口，已排队的旧按钮callback也再次检查，避免OpenGallery内部CloseSeed把尚未回答的题吞掉。
- ProfileMemoriesUiChecks新增真实左栏home按钮从Cards子页返回Home、顶栏仅三按钮、640px无重叠；通过原ShowSeed构建作者题目展示fixture（不写派发记录），验证待答题期间入口隐藏、陈旧回调不能关闭同题/打开gallery、状态JSON不变，最后清理题面继续原gallery验证。
- 3份候选先存于.coord/profile-followup-candidate，主控结束Unity进程并通知后才核对原文件SHA256落地；正式源码再次冻结。
- 更新通过带UNITY_EDITOR的Roslyn语法解析与diff检查，Unity执行仍待主控。新helper.meta已存在/补齐。
- XgTrainPage.DataInstallHint.cs和FilesWindowUiChecks.cs所有权已移交OS agent处理最新“资料文件夹”命名，本人不再编辑。