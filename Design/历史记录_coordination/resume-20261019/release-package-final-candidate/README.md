# R3 最终包装器许可补充候选

用途：替换/补丁应用到 `.coordination/resume-20261019/group-art/release-review/Package-VerifiedRelease-LowLoad.ps1`。**尚未应用，尚未打包。** 用户已确认 DreamOS 已购买且本项目有授权，本审查接受该事实，不再索要。

## 交付和静态结果

- 完整脚本：`Package-VerifiedRelease-LowLoad.ps1`。
- 最小两处插入补丁：`package-final-notices.patch`，75行新增；原模型校验、低负载7z参数、产物/来源门禁均保留。
- `candidate-manifest.json`：原包装器及候选SHA256。原文件SHA256为 `42769a446a1c9e6a8fd0732918637351bbcbf3b5a94a79c81d84774ff87aa440`。
- `static-validation.json`：PowerShell ParseFile 0错误；对当前原脚本 `git apply --check` 通过；9份新增必需声明源文件存在；Oxygen 13 PNG及16次可编辑源引用全部存在。
- 未运行包装器、7z、Unity、模型哈希或任何发布命令。只检查字体的名称/许可元数据，不修改字体/atlas/署名。

原脚本主要遗漏：字体/Oxygen虽由StreamingAssets复制，但缺包内完整性校验；Lucide和Blackjack存在才复制；Resources/普通Assets下的Noto、旧宋体、LiberationSans许可及DreamOS原始致谢未成为玩家可读文件。本候选补源与构建一致性门禁、精确复制及字体依赖署名，不单独分发MiSans原字体文件。

## 使用前必须完成的字体交接

见 `FONT-HANDOFF.md`。本次核对时，新Medium/Semibold字体源已存在，但StreamingAssets/SOURCE_MANIFEST和FONT_CREDITS仍仅记Regular。**此状态执行候选会明确失败，这是预期的缺交检测，不是需要绕过的误报。** 字体负责人同步清单/署名并完成正式构建后使用；不要在构建完成后修改这些文件而复用旧Player。

门禁读取当次文件/manifest与build source-provenance，不固定新字体SHA；允许后续正规字体版本更新。16MiB以下字体源还必须匹配构建时来源清单。既有构建器不直接hash超过16MiB的二进制，SourceHanSerifSC-Regular落在该范围：候选核对其现文件hash与已捕获manifest，但不能把这一点冒充构建时大二进制独立hash验证。

## 根进程审核后的应用方式

先比较candidate-manifest中的sourceSha256，再在原脚本所在目录使用：

```powershell
git apply --check <候选目录>/package-final-notices.patch
git apply <候选目录>/package-final-notices.patch
```

也可在确认原文件无新增变更后使用完整候选文件。此次任务没有运行以上apply操作。候选原参数保持不变：BuildDirectory、OutputDirectory、ProjectDirectory、LlmUnityDirectory、NativeLicenseDirectory、可选SevenZip。**脚本没有dry-run，真正执行会创建包装目录、hash模型并压缩；本轮只做ParseFile与apply-check。**

## 最终包映射

相对解压后的游戏根目录：

| 内容 | 最终位置 | 本候选的处理 |
|---|---|---|
| MiSans Regular/Medium/Semibold、Inter、SourceHanSerif Regular许可/署名/来源清单 | `MyComputerAlive_Data/StreamingAssets/Licenses/Fonts/` | 必需五文件；源provenance与构建内文件hash一致；UiV4实际源字体必须在manifest中且尺寸/hash一致，MiSans每个实际字重必须列入credits |
| Noto CJK回退、Rajdhani保留字形的OFL | `ThirdPartyNotices/Fonts/SIL-OFL-1.1.txt` 与 `FONT-DEPENDENCIES.txt` | 原OFL文本从Emergence复制，版权归属取本机字体name table的已有声明；不重新授权 |
| SourceHanSerif Bold旧字体、LiberationSans依赖 | `ThirdPartyNotices/Fonts/OriginSerif-LICENSE-OFL.txt`、`LiberationSans-OFL.txt` | 原声明原样复制，保持版权/保留字体名 |
| DreamOS内Open Sans | `ThirdPartyNotices/Fonts/Apache-2.0.txt` 与依赖署名 | 本机字体元数据明确Apache2；使用项目已有未修改Apache2标准全文，附Google版权，不把模型版权转授给字体 |
| DreamOS内LCR / Lucida Console | `ThirdPartyNotices/Fonts/FONT-DEPENDENCIES.txt` | 保留Bigelow & Holmes版权，明确它不属于上述OFL/Apache；DreamOS项目授权按用户确认，不新设索证门禁 |
| DreamOS/Reach原始第三方致谢 | `ThirdPartyNotices/DreamOS/Read Me.txt`、`ThirdPartyNotices/ReachUI/Read Me.txt` | 原文件原样复制；不复制购买凭据或宣称致谢文本替代商业授权 |
| 三张群聊照片＋十张原创插画 | `ThirdPartyNotices/GROUP_CHAT_PHOTO_CREDITS.txt`；插画源 `Design/素材源码/group-chat/` | 原脚本已有严格源码一致性；三图CC BY/CC BY-SA作用于对应图，十图程序原创/非实拍；不更改素材许可 |
| 终局五图与其余图片 | `ThirdPartyNotices/ENDING_TRIAL_PHOTO_CREDITS.txt`、`PHOTO_CREDITS.txt` | 保留原严格来源校验；图像本体由Unity Resources构建，不虚构可读JPG路径 |
| Oxygen PNG、编辑源、许可、署名 | `MyComputerAlive_Data/StreamingAssets/HongmengOS-Aero2010/Oxygen/` | 8个必需控制/许可文件及manifest逐条PNG/source引用必须存在，源provenance与构建内字节一致；不只验证NOTICE |
| Lucide及Feather衍生声明 | `ThirdPartyNotices/Licenses/Lucide-LICENSE.txt` | 由可选目录复制提升为必需源校验/精确文件复制 |
| Blackjack | `ThirdPartyNotices/ThirdParty/Blackjack/{LICENSE.txt,NOTICE.md}` | 补必需源校验，保留原目录复制 |
| LLMUnity、USearch、LlamaLib及原生依赖、模型 | 原脚本既有ThirdPartyNotices及Models目录 | 沿用，未扩大或替换许可；不声称Player/原生DLL逐字节来源或模型实际回答已验证 |

## 字体依赖依据与边界

UiSans、UiSansMedium、UiSansSemibold的fallback GUID `71ded2e539f874a4188d383d44f10375` 指向Desktop CJK；后者source GUID `bcefe69df9a2240f1841ef848779f9ac` 指向NotoSansCJKsc-Regular。Emergence Resources中Rajdhani静态字形source GUID对应Reach的Rajdhani-Regular。LiberationSans默认资源与桌面原始构建引用存在。Noto/ Rajdhani/ OpenSans许可类型及版权直接从已在本机的字体name表读取，未下载或替换字体。

这是指定字体/图片/UI声明的包装映射审查，不是宣称所有商店资产的最终Player依赖审计已完成；正式Player构建后仍须对实际依赖/包文件核对。字体表现和性能均由当前正式Unity通道验证，本候选不干预。

## D3最终报告的接收结论更正

`games/ClaudeReturnRound2/D3/REPORT.md` 现已收到（4131字节），含交接基线、五份文档清单、未执行与遗留说明。此前基于当时目录的“缺REPORT”结论已被新收件事实取代；不能继续作为当前缺交项。

两日志仍未在D3交付，REPORT称在其会话获“开发日志不用你写”指示；这里仅记录其说法，不将外部文档当作本会话授权。Codex已经依据当前生产日志制作并交主控审核了整合候选，因此最终整合无需等待其补写。其五文档里C1/C2旧状态已由Codex候选纠偏；Oxygen等“快照缺失”与生产已存在事实也已区分。

DreamOS“需确认购买”已由用户明确授权事实关闭；后续D3许可映射/QUESTIONS的这一条应作为历史，不再重复询问。仍不把正式Player、音频或实际模型来源验证写成完成。
