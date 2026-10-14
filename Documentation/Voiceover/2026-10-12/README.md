# 灵光中文独白离线配音 — 2026-10-12

## 交付
- 项目：`/Users/kk/competition`，场景 `Assets/LingGuangV05/Scenes/Main.unity`。
- 148 个文本映射：133 条完整固定独白（含原有12条录音）+ 15 条动态句的固定片段；新增136个 WAV。
- 17 种已知动态模板：15 种播放固定部分，实时数字、名字、引用内容仍显示原字幕。金额差额、序号计数2种无法保留完整固定语义的句子仅字幕。
- 覆盖序章、手工标注、自动收入/罚款等后感、YY恋爱、是非学习、爬虫、皮层揭示、Era节点、破产及女友尾声。AI自身结局台词、聊天窗口消息不混作主角独白。
- 原始字幕未改；原有12个录音未覆盖；无需玩家运行 VoiceStudio、不新增网络调用；不读取或改写游戏存档。

发行资源：
`/Users/kk/competition/Assets/LingGuangV05/Resources/LingGuangV05/Voice/generated/`

运行时清单：
`/Users/kk/competition/Assets/LingGuangV05/Resources/LingGuangV05/Voice/voicestudio_manifest.json`

## 试听
- `listening-preview.wav`：约20秒，依次为警觉、疲惫、自嘲、欣喜、歉疚的目标方向。
- `all-new-lines.wav`：最终136段全量试听带，每段间隔1秒；准确时间索引在 `all-new-lines-index.json`。
- `manifest.json`：每句原文、源文件、表演目标、参考录音、语速、实际合成脚本。

## 实际生成能力与局限
本机 VoiceStudio 0.5.6 / OmniVoice / MPS，`/generate`，32 steps；用项目已有主角录音作为音色与表演参考。没有下载模型、调用付费/云服务或修改全局设置。目标为 local（已实查 workers/target）。

情绪目标通过参考表演、语速和标点实现，**不是**向 OmniVoice 发送它不支持的 happy/sad 指令。停顿用真实 `[pause Nms]`。术语调参加拼音、`.exe` 改为字母拼读，引用的“是/否”避免被拆成多个语义不完整短句。不要把参数配置或ASR回读称为专业演员级情绪质量认证。

## 验证
- 9项 Python 测试：归一化/去重、停顿保字、动态识别、误提内部标志、WAV截断、断点损坏恢复、情绪误分类。
- 7项 Unity EditMode 测试：原录音兼容、缺失/省略号静音、新录音、动态时间、动态引用、英文静音、全部148映射与15动态有声模板的导入匹配。
- 新增136 WAV均检查实际帧数/声明帧数、24kHz/PCM16/mono、时长、非静音、削波、服务端漏段标头；最终发行文件与验证源文件逐个校验一致。
- Unity 编辑器实际音频预览：24kHz mono、Loaded，播放位置推进至6.613秒，停止后 isPlaying=false。没有进入当前游戏或触发存档。
- 本地 large-v3 回读过全量136段；后续4段修订独立复检。`qa-reel*` 和 `qa-aligned*` 是修订前检查证据，不是最终试听带。最终声音以 `takes/`、发行目录与 `all-new-lines.wav` 为准。
- `asr-large-v3.json` 用声音SHA256识别修订版本。ASR同音字/繁简体/字母拼写有差异；分数只是筛查，不能证明每个音节、重音和情绪都完美。调参、exe、摆渡众包等术语以及动态引用转场建议人耳重点试听。
- 未逐段人工听审，也没有逐个游玩全部剧情触发路径。全量资源映射测试和编辑器播放不等于完整剧情E2E验收。

## 复现
在 `/Users/kk/competition` 运行：

```sh
python3 -m unittest discover -s Tools/Voiceover -v
python3 Tools/Voiceover/voiceover.py inventory
python3 Tools/Voiceover/voiceover.py render
```

生成只写本目录 `takes/`，不会自动覆盖发行资源。新增/变更台词请审阅manifest和动态固定片段，验证后再同步发行目录。普通文本使用curl --form-string；证明文件原子替换；无效证明视为未完成；请求失败会停止而非无上限重试。回读脚本使用VoiceStudio既有venv和已缓存Whisper模型（local_files_only）。

## 工程边界
复用 VoiceLines / InnerVoice；精确匹配优先，随后为有界且有超时的动态模板匹配。未找到音频仍显示原字幕；切换英文、Clear及销毁内心独白会停止旧音频。既有字幕保留到音频时长结束的逻辑沿用。

台词盘点 O(源文件总长度)，音频验证 O(样本数)，每段生成串行；运行时精确查找平均 O(文本长度)，未命中时至多17个受长度和超时约束的模板。音频按需 Resources.Load，不增加运行时模型成本。
