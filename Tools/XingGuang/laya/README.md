# Laya 本机逻辑题评测

**2026-10-03：完成真实模型测试，但不满足接入门槛，未修改游戏玩法或接入运行时 AI。**

中文主测试：均衡集 88/180（48.89%）；游戏自然分布 233/500（46.60%）。
结果、逐题概率、错误样例、环境版本和 Unity 兼容性边界见：
`/Users/kk/competition/Documentation/Laya/REPORT.md`。

## 重跑

需要本机已有的 .NET SDK、Python 3.12 和 uv。仅首次安装/下载需要网络；无 API key、云推理或收费服务。

```bash
cd /Users/kk/competition
uv venv --python 3.12 Tools/XingGuang/laya/.venv
uv pip install --python Tools/XingGuang/laya/.venv/bin/python \
  -r Tools/XingGuang/laya/requirements-lock.txt

# 已有数据可直接复用；导出器拒绝覆盖文件。
dotnet run --project Tools/XingGuang/laya/Export.csproj -- \
  Documentation/Laya/questions-new.jsonl

# 首次运行自动下载固定 revision 的多语言模型，并检查权重和 tokenizer SHA-256。
# output 必须是新目录，避免把两次实验混起来。
TOKENIZERS_PARALLELISM=false Tools/XingGuang/laya/.venv/bin/python \
  Tools/XingGuang/laya/evaluate.py \
  --dataset Documentation/Laya/questions-new.jsonl \
  --output Documentation/Laya/run-new --device cpu --threads 4

# 权重缓存齐全后，上述命令前加 HF_HUB_OFFLINE=1 即为完全离线重跑。
```

若 venv 已存在，跳过 `uv venv`。已安装依赖已锁在 `requirements-lock.txt`，不改全局 Python 环境。
默认 CPU 路径已实测；`--device mps` 是可选路径，本次未验证其数值或性能。明确请求的设备不可用时拒绝静默回落。

## 工具与证据

- `Export.cs` 直接编译链接游戏原有 `Core/XgLogic.cs`，不复制/移植题目生成算法。
- 180 道均衡题：九类各 20 道、10 是/10 否，题面去重。
- 500 次自然抽样：五级各 100 次，保留重复题以反映现有出题器分布；本次 424 道不同题面。
- 主格式：题面作为 state，明确的 noul 是/否 criteria；诊断格式：题面作为 instructions。两种格式在推理前已固定。
- 中文/英文各跑主格式 680 次，诊断格式各跑 180 次，总计 1,720 次真实推理，不把同一题的不同语言算作独立题目。
- 模型输入只取题面，绝不送入 `truth`、`why`、种子或题型。推理完成后才比较标签。
- `predictions.jsonl` 逐题落盘；`metadata.json` 保存版本、源文件摘要；`evaluate.snapshot.py` 保存运行时代码；`summary.json` 保存分题型/难度结果。
- 正确率分母包含推理错误；任何错误、截断或缺项均不得通过接入门槛。达到 10 个错误时结束并写 `complete=false`。
- 启动、依赖或预热异常会非零退出，可能只留下不完整目录；不存在完整 summary 就不是成功测试。
- **退出码 0 表示实验完整完成，不表示模型达标。集成必须同时检查 `complete=true` 和 `gate.passed=true`。**

## 验收门槛

预先固定：中文主格式在均衡集、自然集均至少 75%；均衡集中每类至少 60%；类别配额、去重要求满足且无错误/截断/漏题。诊断格式和英文测试不替代中文主门槛。

概率分数未在游戏领域校准。`P(yes)` 是回答方向；所选答案分数是 `max(p,1-p)`，**不是已验证的答对概率**。报告同时给出 Brier、十区间 ECE、混淆矩阵及高分错误率。没有在测试题上微调、调阈值或拟合温度。

## 验证

```bash
cd /Users/kk/competition
Tools/XingGuang/laya/.venv/bin/python -m unittest discover -s Tools/XingGuang/laya -v
dotnet test Tools/XingGuang/tests/Tests.csproj --no-restore -v minimal
```

本次工具测试 10/10、原游戏核心测试 27/27。未改变 Unity 运行时代码，未声称已完成新 AI 的 Play Mode 或 Player 打包验证。

## 复杂度与限制

- 导出生成有 200,000 次尝试上限；输出 O(N)，额外存储 O(N)。内部出题成本沿用游戏实现。
- 固定模型单条 self-attention 对 token 长度 L 的主要计算约 O(L²d + Ld²)，注意力中间张量上界 O(L²)；实际算子融合/局部注意力会改变内存常数。总推理对题目数 N 线性增长。
- 当前评测汇总保留全部结果，O(N) 内存；延迟分位数排序 O(N log N)。1,720 次规模无需复杂流式统计。
- 这是固定模板生成器上的零样本基线，不证明开放域推理能力；数列题采用生成器意图中的简单规律，并非数学上唯一的延拓。
- 下一步若要训练，须另外划分训练/校准/测试种子，并尽可能按模板或结构划分；本次公开测试集不可继续充当“未见测试集”。
