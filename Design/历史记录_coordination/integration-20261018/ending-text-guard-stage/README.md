# D014 终局文本过滤候选

新增 `LingGuangV05.XingGuang.XgEndingTextGuard`，不是对不存在的 ContentFilter/LlmContentGuard 的伪接入。只有本地字符串处理，不调用模型、网络或收费接口。

- `Filter(string value, bool english)`：玩家输入和模型动态 2016 文本。Unicode FormKC、CR/LF 归一，保留换行和数学比较符号；制表/空白处理、去控制/格式字符、去实际 TMP 格式标签、复用 AiNamePolicy 同一不当词基线与 EraLexicon.Scrub。最多4096文本元素，原输入最多32768 UTF-16单元。非法Unicode、不当内容、超长返回null；不截断造成句义变化。不复用名字的16字、文件名符号、设备名限制。
- `FilterReviewed(string value, bool english)`：仅已审稿静态原题/历史文章。保持原字、标点、故意错误陈述、CRLF和2017内容；只检查Unicode/隐藏控制/16384文本元素上限。不能用于原始玩家、YY或模型输出。所有展示必须 richText=false，因此原文中的尖括号不会被解释成格式。
- `AiNamePolicy.ContainsAbusiveLanguage` 最小共享patch提取原有私有词表判断。原 Problem 调用此函数，旧名字错误文案、顺序与文件名条件保持原样。没有复制第二份词表，也没有将身份验证函数用于句子。

这是一套可审查的窄词库，不声称完整语义审查、个人信息识别或对所有绕过形式有效。保留既有英文词边界规则。不会自动把错误知识修正为正确答案。

E-D host 默认动态委托可配置为 `XgEndingTextGuard.Filter`，返回null必须视为拒绝。原试卷的已审稿字段应使用 reviewed 路径；从玩家身份/真实对话/答题历史插入的动态片段须在插入前使用动态路径。不要把整份含原始聊天的内容简单标为 reviewed。历史尾声 E-B/E-C 资源在已审稿目录中走 reviewed 验证，不能先额外 EraLexicon.Scrub，否则绕过本类仍会误删2017内容。

六个 NUnit 测试已准备，包括名字限制隔离、控制符/标签、共享不当词、年代分流、非法Unicode/长度、已审稿原文精确保留。未执行编译、Unity或测试；root统一单实例验证。无新增玩家可见对白，guard返回处理后的原文或null，错误提示由host双语映射。

候选只写 games 根 .coordination 下。生产 AiNamePolicy 未被覆盖。`delivery.patch` 包含两新C#与meta、一个最小共享helper patch。
