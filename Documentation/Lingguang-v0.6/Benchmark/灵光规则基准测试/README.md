# 《灵光一闪》规则基准测试

## 内容
- `sim.py`：最小参考模型，只实现三条底层规则与漏电、疲劳、废退、扰动（本轮内短期增强）几个属性。
- `bench.py`：基准测试，在半径 4 的六角网格上运行 8 项涌现检查。
- `board_bench.py`：同样的前 6 项检查，换到游戏真实的 42 格大脑棋盘上（含中线、窄桥、外侧沟）。
- `board_ref.py`：在真实棋盘上生成跨实现一致性用例 `ReferenceCases.txt`，供 Unity 里的 C# 测试逐拍对照。

## 运行
```
python3 bench.py          # 8 项涌现检查
python3 board_bench.py    # 真实棋盘上的 6 项检查
python3 board_ref.py      # 重新生成 ReferenceCases.txt
```
无第三方依赖，Python 3.8 以上即可。临时切换语义可用环境变量，例如 `LEAK_STORED=1 LEAK_LAST=1 COUNT_WHOLE_BEAT=0 python3 bench.py`（= 原版行为）。

## 用途
1. 每次修改规则或数值，先在参考模型中同步修改，再运行一遍。某项从“通过”变成“未通过”，说明改动影响了涌现行为，需要人工判断是否接受。
2. 改完参考模型后运行 `board_ref.py`，把新的 `ReferenceCases.txt` 复制到 `Assets/Lingguang/Tests/EditMode/`，Unity 里的 `ReferenceConsistencyTests` 会逐轮比较两边的每一次闪光与每条光丝的导通次数。
3. 涟漪、微弱连接、灯塔等参考模型没有的规则，用 Unity 里的 `EmergenceBench`（Test Runner 中单独运行）检查。

## 当前结果（规格 v0.6，2026-10-01）
- `bench.py`：8 / 8。`sim.py` 的默认语义已与规格 v0.6 一致（漏电只作用于本次连锁中收到过电的灵光、最后一拍不流失、导通按整拍计）；原版语义也通过。
- `board_bench.py`：6 / 6。
- 一致性：24 个随机布局、480 轮、2823 次闪光（其中 108 轮出现回荡），C# 与参考模型完全一致。
- `EmergenceBench`（C#，完整规则 + v0.6 的涟漪与微弱连接）：6 / 6。

## 与原型规格的差异
未包含涟漪、流光、记忆折叠、窄桥惩罚、计分；起点按“度数最高的 3 个灵光中随机选取”模拟玩家习惯。这些简化只用于检查规则本身，不代表最终玩法数值。
