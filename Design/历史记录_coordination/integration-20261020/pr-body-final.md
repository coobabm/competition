# 2026.10.20.1：上游整合、真实模型记忆挑战、最终 v9 标题与 1.7B 轻量包

## 这个 PR 做了什么

把 `RipplePrototype/lingguang-v05` 的两条新提交（`d371a2b7`、`3df48181`）合到 `497d9acd` 整合检查点之上，接入 Claude 交付的 v9 开局标题，并把本地模型从 Qwen3.5-4B 换成 Qwen3-1.7B，使商店包体从 3.00 GB 降到 **1.30 GiB**。

主要提交：`9c03d499`（合并快照）、`ec319937`（上游整合）、`defe2035`（v9 标题）、`f68e4b49`（1.7B 模型）、`40d885e1`（Play 配置与声明修正）。

## 主要变更

**忘言夜（第四阶段可选记忆谈话）**
- 上游原本预置「第三轮必忘」；按用户决定改为**真实本地模型回答**，不再演出遗忘。
- 只有 YY 真正显示回复并拿到 `learningReplyId` 回执后才提交该轮证据与纪事。
- 模型未就绪/失败：取消该轮、提示重问、进度不动，不生成结论，也没有离线兜底。
- 结果只写纪事，不影响结局判定，不新增升阶门槛。

**题库与垃圾短信**
- 正规群发营销（含「退订回 TD」）算垃圾短信；账单、消费、取件通知仍为正常。
- 2016 年参数化题干按日期解锁；商品用途与优缺点按产品配对。
- 唐诗干扰项修正整数边界溢出；常识库保持 216 条。

**开局标题（v9）**
- `TitleLogoV9.png`（1775×1040，简洁版，无蜘蛛/信号线）替换 v5：500 参考像素宽、锚点 `(65,-98)`、副标题 `-398.96`、右缘 565 不压图灵海报。
- `TitleMarkPreview` 的间距断言改为在共享 chrome 矩形内按参考像素计算；`NamingDesktopSmoke` 改为校验 RawImage 网格。

**模型换成 Qwen3-1.7B（为压包体）**
- 4B 单模型 2.74 GB，无法把整包压进 1.8 GB；换 1.7B（1,107,409,472 B，Unsloth Q4_K_M，Apache-2.0，基座 `70d244cc`，量化 `d7f544ee`）。
- `LocalModelRelease` 的四个常量、`LLMManager.json`、模型卡/量化卡/LICENSE、`MODEL_SOURCE.json`、`SHA256SUMS.txt`、`THIRD_PARTY.txt` 全部同步；旧通知里把量化方写成 bartowski 的错误一并改正。
- `.gguf` 仍不入版本库（`.gitignore`），构建时由 `IntegratedReleaseBuild` 校验 size+SHA。
- 提示缓存的设计注释按 dense transformer 修正；`CheckpointsPerSlot` 保留待实测。

## 验证

| 项目 | 结果 |
| --- | --- |
| EditMode 全套（合并后） | 2469 项：2462 通过、0 失败、6 跳过、1 inconclusive（`XgDownloadsTests` 既有 `Assume` 不成立，合并前同样如此） |
| 定向回归（文案改动后） | 5 个类 61/61 |
| `XgForgetNightTests`（修夹具后） | 21/21 |
| 开局标题 | `CaseMenuHudTests` 3/3；`TitleMarkPreview` 1920×1080 / 1280×720 通过 |
| 4B 构建 | `Succeeded`，0 error，275 warning，payload 3.41 GB |
| 1.7B 构建 | `Succeeded`，0 error，payload 1.78 GB；包内 `sourceCommit=f68e4b49` 前的 `defe2035`，`sourceDirty=true`（仅 `Packages/Aura/.gitignore`） |

说明：测试套件跑在合并与标题内容上；换模型只改了常量、配置与文档，**未重跑**测试。真实 Player 内的本地模型推理、音频与完整通关仍未验收。

## 交付

| 形态 | 体积 | SHA-256 |
| --- | --- | --- |
| ZIP（TapTap 上传，exe 在根层） | 1,366,081,459 B | `ab7aa275…12a57b74` |
| 7z 单卷（GitHub/朋友拷贝） | 1,346,249,446 B | `be2c4629…52d157d5e4` |

解压后 1,779,714,936 B / 308 文件。完整 ZIP 仍超 GitHub 单资产 2 GiB 限制，公开 Release 用分卷。

## 已知风险

- **1.7B 质量低于 4B**：长回答、多轮一致性与「忘言夜」判断会变弱；代码有保守兜底（空白/失败取消该轮，不硬判遗忘）。
- 房间《三体》海报与部分 AI 商品图提示词涉及真实品牌，按作者决定保留。
- `sourceDirty=true`：唯一未提交改动是 `Packages/Aura/.gitignore`（包导入重写）。
- 历史经过一次 LFS 修复性改写（两条旧指针内容置空），旧提交哈希与备份见 `C:\Users\xomem\rel\branch-backup-before-lfs-fix.bundle`。
