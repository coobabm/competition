# 静态交付验证

时间：2026-10-13（Asia/Singapore）。

- 补丁可应用检查：git apply --check 返回退出码0；仅检查，没有应用补丁。
- 依赖复核：source-hashes.json 中12个文件全部与结束前读取的SHA256一致；final-source-hashes.json 保存复核值。
- 精确范围：changes.patch 只增加LSTM的Explanation/Effect分支，不修改名称、目录、模拟、可见性或购买逻辑。
- 当前XgTechTreeCopy基准SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。

本记录仅证明静态补丁检查和源码一致性；C#编译、实际模拟运行、Unity场景及视觉验收均未执行，不记为运行PASS。
