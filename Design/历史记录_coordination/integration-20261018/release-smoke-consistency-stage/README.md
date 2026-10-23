# Windows release smoke 一致性审计交付

- `RELEASE-SMOKE-CHECKLIST.md`：独立更新版；基于 release-smoke-current-stage 原清单，保留其实际 Player、隔离存档、PlayerPrefs 恢复、UI、音乐回录步骤。引用的工具继续使用原目录，没有复制或改动 helper。
- `evidence-template.json`：独立更新版；增加真实 trace 关联/来源/未完成任务字段，移除“当前没有逐请求追踪”和“必然启动 embedding”的过时默认。
- `third-party-notice.patch`：按主控追加指示，唯一生产候选改动是 `Assets/StreamingAssets/LingGuang/llama/THIRD_PARTY.txt`。明确实际 Qwen3/LLMUnity/LlamaLib 来源、版本、许可所在路径；保留旧 server bundle 声明并明确条件。没有删除旧声明、改模型、修改许可原文。
- `notice-manifest.json`：上述文件 baseline/candidate SHA256。
- `audit-evidence.json`：本次读取的源码、manifest/lock、helper、许可及版本证据的文本文件 SHA256；仅 GGUF 路径/大小清单，不重读大模型或 DLL。
- `static-checks.json`：七段 PowerShell 操作示例与三个已有 helper 的解析结果，全部零语法错误。只调用 Parser，未执行其命令。

关键更正：Trace 已导入，验证目录自动启用且不用新 native 参数；现有唯一 GGUF 为 Qwen3-4B，embedding 缺席时实际代码直接 BM25 返回；主角 VoiceLines.Play 已停止发声，音乐开场只显示内心字幕；当前构建入口为 IntegratedReleaseBuild.Run，版本2026.10.18.1，保留 DefaultCompany/competition 身份；原始 Unity Build 与补齐 ThirdPartyNotices 后的最终分卷包分别列明。

追踪的边界：native 表示相对 LocalLlm 清洗后文本未改变，不是原始 token 原样；native_filtered 必须单列。相同 request ID 的成功链还须配实际 YY UI/UTC/contact。structurallyValid 并不足够，还要检查失败、未完成任务和选定 request。峰值仅覆盖该次 LocalLlm，取消未实际发生就仍是 unverified，不能外推到自然全流程或所有阶段。

验证仅静态：notice patch 对 baseline 的 git apply --check 通过，UTF-8 LF，操作示例解析通过。没有启动 Unity、Player、模型、录音器或打包器；没有写生产、旧清单、helper 或历史验收证据。主控审阅 notice 后按最小补丁合入，最终构建/验收/打包仍由主控串行执行。
