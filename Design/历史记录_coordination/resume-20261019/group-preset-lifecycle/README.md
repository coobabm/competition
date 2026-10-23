# 群预设生命周期：恢复交付

最新主件见 [RESUME_REPORT.md](RESUME_REPORT.md) 和 `manifest.json`。此前未编译v1草稿已存 `README.original-v1.md` / `original-v1/`。

最终增量 `group-preset-lifecycle.patch`（4修改+8新增）已相对生产当前工作树通过 git apply --check。已吸收只读审阅的Claude v2，不能再叠加v1/v2。

四个程序集隔离编译0错误；9个纯核心NUnit测试通过，3个Unity原生JsonUtility测试待验。Unity及真实UI/磁盘/延迟/模型仍由主控单实例验收，入口与边界均在报告中。
