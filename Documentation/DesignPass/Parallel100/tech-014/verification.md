# tech-014 静态交付复核

- 复核日期：2026-10-13（Asia/Singapore）。
- 已执行：对 source-hashes.json 中全部 10 份生产源码重新计算 SHA256，均与核对时记录一致；XgTechTreeCopy.cs 保持 308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。
- 已执行：git apply --check --whitespace=error Documentation/DesignPass/Parallel100/tech-014/changes.patch，退出码 0。只验证文本补丁能应用，无实际应用。
- 补丁范围：仅 XgTechTreeCopy.Effect 的 batchnorm 分支，1 行旧文案替换为 3 行双语文案；邻接分支只作上下文。名称、解释、ScaleItems 追加及神秘节点分支未动。
- 静态证据见 result.md；源码快照指纹见 source-hashes.json。所有交付仅写在 tech-014 专属目录。
- 未执行：C# / Unity 编译、游戏或模拟运行、视觉验收、真实存档检查。result.md 的边界用例是待执行预期，不能作为运行通过记录。
- 整合前再次核对当前文件与分支；其他并行聊天在本次复核后仍可能继续更新源码。
