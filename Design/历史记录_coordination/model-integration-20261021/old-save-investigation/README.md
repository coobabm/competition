# 旧存档排查（已暂停）

用户要求旧存档问题先不处理，已停止。本轮没有任何源码修改，没有启动 Unity，没有改动真实存档或 PlayerPrefs，没有干预正在运行的游戏。

仅将 DefaultCompany/competition/LingGuangV05 复制为此目录 snapshot 后读取。现阶段 1 的主档和备份，以及阶段 5 的两份 reset 历史快照，均通过 SHA256 封套/清单/分块/总载荷校验。GameState 和 lab version 均为 1，均有 8 个节点和 7 条边，未标记结局或 restart。尚未进行 Unity 实际恢复，因此没有确认无法进入游戏的根因。

本目录及解包 JSON 含玩家数据，只作本地隔离证据，不加入 Git 或发布包。
