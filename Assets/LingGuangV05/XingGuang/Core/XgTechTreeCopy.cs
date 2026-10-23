using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    /// <summary>Three-line technology cards. Metadata is separate; explanations appear on hover or icon press.</summary>
    public static partial class XgTechTreeCopy
    {
        sealed class Copy
        {
            public readonly string name, nameEn, tag, tagEn, summary, summaryEn, effect, effectEn;
            public Copy(string n,string en,string s,string se,string e,string ee)
            {
                SeparateMetadata(n,en,out name,out nameEn,out tag,out tagEn);
                summary=s;summaryEn=se;effect=e;effectEn=ee;
            }
        }
        static string T(XgSim sim,string zh,string en)=>sim.English?en:zh;
        static readonly Dictionary<string,Copy> All=new Dictionary<string,Copy>(StringComparer.Ordinal)
        {
            { "daily.dice", new Copy("概率直觉","Probability intuition","细化概率提示","Refine probability hints","显示概率题与风险预报","Show probability questions and risk forecasts") },
            { "daily.news", new Copy("时事","Current affairs","显示新闻回顾","Show news review","复习已读新闻","Review news already read") },
            { "daily.surf", new Copy("冲浪","Surfing","提示资料链接","Highlight learning links","高亮手写数字资料","Highlight handwritten-digit data") },
            { "daily.bargain", new Copy("砍价","Bargaining","降低显卡价格","Reduce GPU purchase prices","二手显卡价格减5%","Used GPU purchase price −5%") },
            { "daily.care", new Copy("体贴","Considerate","提醒重要日期","Remind of important dates","显示已知日期提醒","Show reminders for known dates") },
            { "daily.slang", new Copy("网络用语","Internet slang","解释网络用语","Explain internet slang","显示用语解释与斗图建议","Show slang explanations and sticker tips") },
            { "daily.rhythm", new Copy("节奏","Rhythm","同步音乐节奏","Follow the music rhythm","完成提示跟随音乐重音","Completion cues follow music accents") },
            { "perceptron", new Copy("感知机 P 1958","Perceptron P 1958","学习基础判断","Learn basic classification","使用感知机学习分类","Classify with a perceptron") },
            { "mlp", new Copy("多层感知机 MLP 1986","Multilayer MLP 1986","学习复杂判断","Learn complex classification","组合多层判断线索","Combine clues across layers") },
            { "lenet", new Copy("手写卷积 LeNet 1998","Digit CNN LeNet 1998","识别图像特征","Recognise visual features","使用卷积识别笔画","Recognise strokes with convolution") },
            { "rnn", new Copy("循环网络 RNN 1990","Recurrent RNN 1990","利用前文信息","Use preceding context","使用循环记忆","Use recurrent memory") },
            { "alexnet", new Copy("深层卷积 AlexNet 2012","Deep CNN AlexNet 2012","扩展识图能力","Expand image recognition","使用深层卷积模型","Use a deep convolutional model") },
            { "vgg", new Copy("小窗卷积 VGG 2014","Small-filter VGG 2014","扩展识图能力","Expand image recognition","使用小窗卷积模型","Use a small-filter convolutional model") },
            { "googlenet", new Copy("多路卷积 Inception 2014","Multi-path Inception 2014","扩展识图能力","Expand image recognition","使用多路卷积模型","Use a multi-path convolutional model") },
            { "lstm", new Copy("门控记忆 LSTM 1997","Gated memory LSTM 1997","保留长句信息","Retain longer context","使用门控记忆","Use gated memory") },
            { "gru", new Copy("简化记忆 GRU 2014","Compact memory GRU 2014","精简记忆结构","Simplify memory structure","使用简化门控记忆","Use compact gated memory") },
            { "textcnn", new Copy("文字卷积 CNN 2014","Text CNN 2014","识别短语特征","Recognise phrase features","使用文字卷积","Use text convolution") },
            { "resnet", new Copy("残差网络 ResNet 2015","Residual ResNet 2015","改善深层学习","Improve deep learning","增加跨层信息通道","Add shortcuts between layers") },
            { "seq2seq", new Copy("编码解码 Seq2Seq 2014","Encoder-decoder S2S 2014","学习句子转换","Learn sentence conversion","使用编码解码模型","Use an encoder-decoder model") },
            { "attention", new Copy("回看注意力 Attn 2014","Look-back Attn 2014","利用原句信息","Use source-sentence context","解除翻译瓶颈限制","Remove the translation bottleneck") },
            { "caption", new Copy("图像描述 Cap 2015","Image caption Cap 2015","生成图像描述","Generate image descriptions","显示表情图描述","Show descriptions for expression images") },
            { "transformer", new Copy("自研新脑 TF 探索2016","Own TF experiment 2016","综合句中信息","Combine sentence context","使用自研注意力模型","Use an experimental attention model") },
            { "weights", new Copy("权重 W 1958","Weights W 1958","提升训练效率","Increase training throughput","训练处理量×1.1","Training throughput ×1.1") },
            { "learnrule", new Copy("感知学习 PL 1958","Perceptron rule PL 1958","提升训练效率","Increase training throughput","训练处理量×1.15","Training throughput ×1.15") },
            { "step", new Copy("阶跃开关 Step 1943","Threshold Step 1943","提高成绩上限","Raise the score ceiling","上限加成+1点","Ceiling bonus +1 point") },
            { "bias", new Copy("偏置 Bias 1958","Bias 1958","提高成绩上限","Raise the score ceiling","上限加成+2点","Ceiling bonus +2 points") },
            { "features", new Copy("组合线索 FE 工具2016","Feature tools FE tool 2016","扩展逻辑学习","Expand logic learning","解除逻辑55%上限","Remove the base 55% logic ceiling") },
            { "shared.lr", new Copy("学习率搜索 LRS 工具2016","Rate search LRS tool 2016","调整学习步幅","Adjust learning step size","显示六档学习率","Show six learning rates") },
            { "sigmoid", new Copy("平滑开关 Sigmoid 1986","Smooth Sigmoid 1986","细化判断方式","Refine decision signals","使用连续判断数值","Use continuous decision values") },
            { "momentum", new Copy("动量下降 MSGD 1964","Momentum MSGD 1964","提升优化效率","Increase optimisation throughput","优化器处理量×1.3","Optimizer throughput ×1.3") },
            { "backprop", new Copy("反向传播 BP 1986","Backpropagation BP 1986","提升训练效率","Increase training throughput","训练处理量×1.2","Training throughput ×1.2") },
            { "chainrule", new Copy("自动求导 AD 1970","Auto-diff AD 1970","提升训练效率","Increase training throughput","训练处理量×1.1","Training throughput ×1.1") },
            { "cudnn", new Copy("显卡加速 cuDNN 2014","GPU library cuDNN 2014","节省训练用电","Reduce training energy","训练用电量×0.7","Training energy ×0.7") },
            { "cooler", new Copy("机箱风道 FAN 工具2016","Case airflow FAN tool 2016","降低硬件风险","Reduce hardware risk","显卡高温损耗减半","Halve heat wear on the graphics cards") },
            { "relu", new Copy("整流激活 ReLU 2010","Rectifier ReLU 2010","提升训练效率","Increase training throughput","训练处理量×1.3","Training throughput ×1.3") },
            { "gradclip", new Copy("梯度裁剪 Clip 2013","Gradient Clip 2013","限制更新幅度","Limit update size","限制单次参数改动","Limit individual parameter updates") },
            { "irnn", new Copy("单位初始化 IRNN 2015","Identity init IRNN 2015","保留前文信息","Retain preceding context","提高循环记忆保留率","Improve recurrent memory retention") },
            { "dropout", new Copy("随机失活 Dropout 2012","Dropout 2012","改善新题表现","Improve unseen-question performance","过拟合风险项减半","Halve the overfitting risk term") },
            { "augment", new Copy("数据增强 AUG 工具2016","Data augmentation AUG tool 2016","扩充训练题目","Expand training examples","扩充图像变换题池","Add transformed image examples") },
            { "dataclean", new Copy("数据清洗 CLEAN 工具2016","Data cleanup CLEAN tool 2016","改善标注质量","Improve label quality","外来有效错标减半","Halve effective external label noise") },
            { "rmsprop", new Copy("自适应步长 RMSProp 2012","Adaptive RMSProp 2012","提升优化效率","Increase optimisation throughput","优化器处理量×1.6","Optimizer throughput ×1.6") },
            { "batchnorm", new Copy("批归一化 BN 2015","Batch norm BN 2015","支持更深网络","Support deeper networks","耐受层数增加6层","Increase tolerated depth by 6 layers") },
            { "adam", new Copy("自适应动量 Adam 2014","Adaptive momentum Adam 2014","提升优化效率","Increase optimisation throughput","优化器处理量×2","Optimizer throughput ×2") },
            { "lrschedule", new Copy("学习率衰减 LRD 工具2016","Rate decay LRD tool 2016","细化后期学习","Refine later learning","降低上限误差项","Reduce the ceiling error term") },
            { "position", new Copy("位置标记 POS 探索2016","Own position experiment 2016","识别词语顺序","Recognise word order","使用词语位置线索","Use word-position clues") },
            { "transfer", new Copy("迁移学习 TL 工具2016","Transfer learning TL tool 2016","复用学习成果","Reuse learned progress","更换模型保留部分成果","Retain some progress when changing models") },
            { "multihead", new Copy("多路注意 MH 探索2016","Own multi-head experiment 2016","综合多类线索","Combine different clue types","随自研模型获得","Included with the experimental model") },
            { "layernorm", new Copy("层归一化 LN 2016","Layer norm LN 2016","规范信号范围","Normalise signal ranges","随自研模型获得","Included with the experimental model") },
            { "residual", new Copy("残差直连 RC 2015","Residual links RC 2015","改善信息传递","Improve signal transfer","随自研模型获得","Included with the experimental model") },
            { "warmup", new Copy("学习率预热 WU 工具2016","Rate warm-up WU tool 2016","平缓参数更新","Gradual parameter updates","学习步幅逐步增加","Gradually increase learning steps") },
            { "fp16", new Copy("半精度 FP16 2016","Half precision FP16 2016","扩大训练规模","Expand training scale","预训练规模×2","Pretraining scale ×2") },
            { "earlystop", new Copy("早停研究 ES 1990","Early-stop study ES 1990","提升训练效率","Increase training throughput","训练处理量×1.05","Training throughput ×1.05") },
            { "wordvec", new Copy("词向量 W2V 2013","Word vectors W2V 2013","提升训练效率","Increase training throughput","训练处理量×1.08","Training throughput ×1.08") },
            { "beamsearch", new Copy("束搜索 BS 1977","Beam search BS 1977","提升训练效率","Increase training throughput","训练处理量×1.1","Training throughput ×1.1") },
            { "subword", new Copy("子词切分 BPE 2016","Subword BPE 2016","提升训练效率","Increase training throughput","训练处理量×1.05","Training throughput ×1.05") },
            { "sft", new Copy("示范跟答 DEMO 探索2016","Own answer-tuning trial 2016","提升训练效率","Increase training throughput","训练处理量×1.05","Training throughput ×1.05") },
            { "cot", new Copy("分步解题 STEP 探索2016","Own reasoning trial 2016","提升训练效率","Increase training throughput","训练处理量×1.05","Training throughput ×1.05") },
            { "secret.6", new Copy("配置指南 GUIDE 工具2016","Setup guide GUIDE tool 2016","显示配置建议","Show setup suggestions","显示预训练配置","Show pretraining setup") },
            { "datacenter", new Copy("机柜租赁 IDC 工具2016","Rack rental IDC tool 2016","扩展计算设备","Expand computing hardware","显示机房机架","Show the server rack") },
            { "label.raise", new Copy("答题加薪 PAY 工具2016","Answer pay PAY tool 2016","提高答题报酬","Increase answer pay","提高手动答题报酬","Increase pay for manual answers") },
            { "label.auto", new Copy("自动答题 AUTO 工具2016","Auto answering AUTO tool 2016","自动处理标注题","Automate labelling cards","每级每桌加0.2题/秒","Add 0.2 cards/s per desk per level") },
            { "label.coop", new Copy("把握门槛 CONF 工具2016","Confidence gate CONF tool 2016","筛选待复核题","Select cards for review","低把握题转人工","Route low-confidence cards to manual review") },
            { "label.brain", new Copy("本地判题 LM 工具2016","Local judging LM tool 2016","启用模型判题","Enable model judgment","文字题交由模型判断","Let the model judge text questions") },
            { "label.hard", new Copy("难例奖励 HARD 工具2016","Hard-case reward HARD tool 2016","提高复核报酬","Increase review pay","新题复核报酬×1.5","New-card review pay ×1.5") },
            { "label.audit", new Copy("答案抽检 QA 工具2016","Answer audit QA tool 2016","抽查自动答案","Audit automatic answers","抽检40%自动错题","Audit 40% of wrong automatic answers") },
            { "label.parallel", new Copy("复核扩容 Q 工具2016","Review inbox Q tool 2016","扩大复核容量","Expand review capacity","待复核题40→120","Review capacity 40→120") },
            { "label.trapmemory", new Copy("金题匹配 GOLD 工具2016","Gold-item match GOLD tool 2016","识别已知检查题","Recognise known checks","检查题转人工复核","Route known checks to manual review") },
            { "label.captcha", new Copy("验证码代填 OCR 工具2016","Captcha helper OCR tool 2016","代填数字验证码","Fill numeric captchas","自动填写并提交","Automatically fill and submit") },
            { "relic.slots8", new Copy("道具扩容 I","Relic slots I","增加装备位置","Add equipment slots","装备格4→8","Equipment slots 4→8") },
            { "arith.mixed", new Copy("算术：混合运算","Mixed arithmetic","扩展运算题型","Expand arithmetic types","加入括号与多步运算","Add brackets and multi-step calculations") },
            { "arith.powers", new Copy("算术：幂指对","Powers, exponents and logs","扩展运算题型","Expand arithmetic types","加入幂指对与根式","Add powers, exponents, logs and roots") },
            { "arith.calculus", new Copy("算术：微积分","Calculus","扩展运算题型","Expand arithmetic types","加入极限导数积分","Add limits, derivatives and integrals") },
            { "arith.auto", new Copy("算术自动作答","Auto arithmetic","自动完成运算题","Automate arithmetic answers","自动选择打地鼠答案","Select whack-a-mole answers automatically") },
            { "spider.acquire", new Copy("灵宠蜘蛛","Resident spider","采集网页内容","Collect webpage content","开放网页采集与拖题","Enable web reading and dragged answers") },
            { "spider.fast", new Copy("敏捷爬行","Agile crawler","提升采集速度","Increase collection speed","爬行读取速度×2","Movement and reading speed ×2") },
            { "spider.browse", new Copy("自主浏览","Autonomous browsing","自动浏览页面","Browse pages automatically","自动翻页切换页面","Scroll and switch pages automatically") },
            { "spider.autoanswer", new Copy("快速自动答题","Fast automatic answers","提升作答速度","Increase answer speed","答题间隔3秒→1秒","Answer interval 3s→1s") },
            { "data.review", new Copy("数据回顾","Learning review","显示学习记录","Show learning records","显示YY数据页与再考","Show YY Data and retesting") },
            { "gamedata", new Copy("对局记录","Game records","收集对局数据","Collect game records","对局记录生成训练包","Turn game records into training packs") },
            { "board.logic1", new Copy("一步判断","One-move judgment","提升棋局判断","Improve board judgment","寻找直接连五或挡四","Find an immediate win or block four") },
            { "board.logic2", new Copy("看懂棋盘截图","Read board pictures","识别棋盘图片","Recognise board images","显示五子棋读图","Show the Gomoku image reader") },
            { "board.logic3", new Copy("想两步以上","Plan ahead","提升棋局判断","Improve board judgment","开启多步走法判断","Enable multi-step move planning") },
            { "board.logic4", new Copy("自己跟自己下","Self-play","自动进行练棋","Automate board practice","开启夜间自对弈","Enable night self-play") },
            { "nextmove", new Copy("下一步走哪","Next move","扩展棋局题型","Expand board-game questions","显示棋局选择题","Show board-game multiple-choice questions") },
            { "books.library", new Copy("图书馆网页","Library website","显示图书馆入口","Show the library entrance","图书另行购买","Books are purchased separately") },
            { "relic.slots12", new Copy("道具扩容 II","Relic slots II","增加装备位置","Add equipment slots","装备格8→12","Equipment slots 8→12") },
            { "train.data", new Copy("数据导入 DATA 工具2016","Data Import DATA tool 2016","显示资料文件夹","Show Training files","支持拖入与网址导入","Import by dragging or URL") },
            { "train.ledger", new Copy("记账本 LEDGER 工具2016","Ledger LEDGER tool 2016","显示资金与电力","Show funds and power","显示收入和用电信息","Show income and power information") },
            { "train.curve", new Copy("训练曲线 CURVE 工具2016","Training curve CURVE tool 2016","显示训练页面","Show the training page","开放训练操作","Enable training controls") },
            { "train.holdout", new Copy("留出新题 VAL 工具2016","Holdout set VAL tool 2016","显示新题成绩","Show unseen-question scores","显示验证成绩曲线","Show the validation curve") },
            { "train.checkpoint", new Copy("检查点 CKPT 工具2016","Checkpoint CKPT tool 2016","保留最佳成果","Keep the best progress","显示最佳版本恢复","Show best-checkpoint restoration") },
            { "train.monitor", new Copy("训练监控 MON 工具2016","Training monitor MON tool 2016","显示训练风险","Show training risks","显示本轮风险提示","Show risks for this training round") },
            { "train.weights", new Copy("权重可视化 VIS 工具2016","Weight viewer VIS tool 2016","显示学习特征","Show learned features","显示「看它学」","Show the learning visualisation") },
            { "brain.window", new Copy("脑子窗口 BRAIN 工具2016","Brain window BRAIN tool 2016","显示网络活动","Show network activity","显示大脑动画窗口","Show the animated brain window") },
            { "label.ai", new Copy("{AI}代答 ANS 工具2016","{AI} answers ANS tool 2016","显示参考答案","Show reference answers","每题显示它的回答","Show its answer on every card") },
        };
        public static bool Covered(XgNode n)=>n!=null&&(XgSim.IsAtlas(n)||All.ContainsKey(n.id)||n.kind==XgNodeKind.Depth||n.kind==XgNodeKind.Width||n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack.")||n.id.StartsWith("persona."));
        public static string Name(XgNode n,XgSim sim)
        {
            if(n==null)return "?";
            if(sim.NodeMystery(n))return sim.NodeName(n);
            if(XgSim.IsAtlas(n))return sim.NodeName(n);
            if(All.TryGetValue(n.id,out var c))return T(sim,c.name,c.nameEn).Replace("{AI}",sim.AiDisplayName);
            if(n.kind==XgNodeKind.Depth)return T(sim,"网络深度","Network depth");
            if(n.kind==XgNodeKind.Width)return T(sim,"网络宽度","Network width");
            if(n.id=="danmu.pack")return T(sim,"弹幕情绪","Comment sentiment");
            if(n.id=="crosssentence.pack")return T(sim,"跨句理解","Cross-sentence understanding");
            if(n.id=="pack.poem_next")return T(sim,"唐诗接字","Tang verse");
            return sim.NodeName(n);
        }
        public static string Summary(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return T(sim,"还没发现这条线索","An undiscovered clue");
            if(XgSim.IsAtlas(n))return T(sim,"把遇到的变化记下来","Remember what changed");
            if(All.TryGetValue(n.id,out var c))return T(sim,c.summary,c.summaryEn);
            if(n.kind==XgNodeKind.Depth)return T(sim,"扩展网络层数","Expand network depth");
            if(n.kind==XgNodeKind.Width)return T(sim,"扩展模型容量","Expand model capacity");
            if(n.id==XgSim.PersonaArchiveNode)return T(sim,"看懂性格如何变化","Understand personality changes");
            if(n.id.StartsWith("persona."))return T(sim,"聊聊它会怎样选择","Explore its choices");
            if(n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack."))return T(sim,"扩展题目类型","Expand question types");
            // Neutral fallback for a node without curated copy: its own note, else its name.
            string note=FirstSentence(T(sim,n.note,string.IsNullOrEmpty(n.noteEn)?n.note:n.noteEn),sim.English);
            return string.IsNullOrEmpty(note)?Name(n,sim):note;
        }
        public static string Effect(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return T(sim,"▶ 发现后查看","▶ Discover to reveal");
            if(XgSim.IsAtlas(n))return T(sim,"▶ 查看成长记录","▶ View the growth record");
            if(n.id=="label.raise")
            {
                int next=Math.Min(XgCatalog.RaiseMax,sim.RaiseLevel+1);
                return "▶ "+T(sim,"报酬","Answer pay")+" ×"+sim.RaiseMultiplier+"→×"+XgCatalog.RaiseMultipliers[next];
            }
            if(n.id=="label.ai")return "▶ "+T(sim,"每题显示"+sim.AiDisplayName+"回答","Show "+sim.AiDisplayName+"'s answer on every card");
            if(All.TryGetValue(n.id,out var c))return "▶ "+T(sim,c.effect,c.effectEn);
            if(n.kind==XgNodeKind.Depth)return "▶ "+T(sim,"可用层数上限"+n.value,"Layer cap "+n.value);
            if(n.kind==XgNodeKind.Width)return "▶ "+T(sim,"每层宽度上限"+XgCatalog.Widths[Math.Max(0,Math.Min(n.value,XgCatalog.Widths.Length-1))],"Width cap "+XgCatalog.Widths[Math.Max(0,Math.Min(n.value,XgCatalog.Widths.Length-1))]);
            if(n.id.StartsWith("pack."))return "▶ "+T(sim,"显示"+sim.NodeName(n)+"题桌","Show the "+sim.NodeName(n)+" desk");
            if(n.id==XgSim.PersonaArchiveNode)return T(sim,"▶ 名字页打开性格档案","▶ Open Personality archive from the profile");
            if(n.id.StartsWith("persona."))return T(sim,"▶ 开放对应性格题包","▶ Opens this personality pack");
            if(n.kind==XgNodeKind.Dataset)return T(sim,"▶ 获得数据包使用权","▶ Grants access to this dataset");
            return T(sim,"▶ 查看说明","▶ See details");
        }
        public static string Card(XgNode n,XgSim sim)=>Name(n,sim)+"\n"+Summary(n,sim)+"\n"+Effect(n,sim);
        // Kept for older callers. This is the short two-line body, never the glossary.
        public static string Note(XgNode n,XgSim sim)=>Summary(n,sim)+"\n"+Effect(n,sim);
        public static string Detail(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return Summary(n,sim)+T(sim,"。",".");
            if(n.id==XgSim.PersonaArchiveNode)return T(sim,"对照开局愿望，查看近期性格变化。","Compare recent personality changes with the opening wish.");
            if(ShortDetails.TryGetValue(n.id,out var detail))return T(sim,detail[0],detail[1]);
            return FirstSentence(LegacyDetail(n,sim),sim.English);
        }
        static string LegacyDetail(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return Summary(n,sim)+"。";
            if(XgDailyLife.Node(n.id)!=null)return XgDailyLifeCopy.Detail(n.id,sim.English);
            if(n.id=="transformer"||n.id=="multihead"||n.id=="position"||n.id=="sft"||n.id=="cot")
                return T(sim,"这是你在2016年尝试的自研方向，不是当年公开发表成果的介绍。","This is your own experiment in 2016, not a claim about a publication from that year.")+"\n"+Summary(n,sim)+T(sim,"。",".");
            if(n.id=="label.parallel")return T(sim,"待复核容量从40题增加到120题；满了也不停，只是拿不准的题先跳过。多留八十道题，等你有空再来复核。","The review inbox expands from 40 to 120 cards; when full, automation keeps going and only skips the unsure cards. Keep eighty more cards until you have time to review them.");
            if(n.id.StartsWith("pack.")){var p=XgTeachingPacks.Find(n.id.Substring(5));return p==null?Summary(n,sim):T(sim,p.play,p.playEn);}
            if(n.id==XgSim.PersonaArchiveNode)return T(sim,"阶段2起，赞/踩满15次或发现性格漂移后花¥300解锁。对照开局愿望，查看近14游戏日的性格变化和原因。","From stage 2, unlock for ¥300 after 15 ratings or observed drift. Compare personality with your opening wish and explore changes and causes over the last 14 game days.");
            if(n.id.StartsWith("persona."))return T(sim,"这组问题用来了解它的选择。回答与标签记录在YY的性格页。","These questions explore its choices. Answers and labels are kept on YY's Personality page.");
            if(n.kind==XgNodeKind.Depth||n.kind==XgNodeKind.Width)return Summary(n,sim)+T(sim,"。购买提高可用上限，模型按结构和显存配置。",". Buying raises the available cap; the model is configured for its architecture and VRAM.");
            if(n.kind==XgNodeKind.Dataset)return T(sim,"例题和答案构成训练资料。不同题库练习不同本领。","Examples and answers form the training material. Different datasets practise different skills.");
            string text=Explanation(n,sim);
            return string.IsNullOrEmpty(text)?Summary(n,sim)+T(sim,"。","."):text;
        }
        static string Explanation(XgNode node, XgSim sim)
        {
            switch (node.kind)
            {
                case XgNodeKind.Depth: return T(sim, "多一层，就多一次把简单线索组合成复杂线索的机会。", "Each extra layer can combine simple clues into more complex ones.");
                case XgNodeKind.Width: return T(sim, "同一层能同时容纳更多线索，网络就更宽。", "A wider layer can hold more clues at the same time.");
                case XgNodeKind.Dataset: return T(sim, "用已经整理好的例题和答案，扩充可训练的数据。", "Add a prepared collection of examples and answers to the training data.");
                case XgNodeKind.Arch: return ArchitectureExplanation(node, sim);
            }
            switch (node.id)
            {
                case "board.logic2": return T(sim, "把摆渡或贴吧的本站240×240标准五子棋原图拖入游戏中心的棋盘读图入口。它从像素识别15路棋盘，再建议黑棋下一步。", "Drag this site's original 240×240 Gomoku image from Bodu or Tieba into the Game Hub board reader. It reads the 15×15 board from its pixels and suggests Black's next move.");
                case "board.logic3": return T(sim, "五子棋和国际象棋会搜索后续走法，再判断当前这步。飞行棋会避险，跳棋会规划路线，大富翁会留出现金。", "Gomoku and chess search later moves before choosing the current move. Aeroplane chess considers risk, Chinese checkers plans routes, and Monopoly-style play keeps a cash reserve.");
                case "board.logic4": return T(sim, "游戏内22:00–06:00、保持游戏运行时，每夜选一种棋类自对弈，按五子棋、飞行棋、跳棋、大富翁轮换。完成3局后，早晨生成记录包。把包拖入练页安装后才更新偏好。", "During in-game 22:00–06:00 while the game is running, one game per night rotates among Gomoku, aeroplane chess, Chinese checkers and Monopoly-style play. Three finished games are packed in the morning. Drag the pack into Train to update preferences.");
                case "weights": return T(sim, "给每条线索分配不同的分量，重要的线索更能影响答案。", "Give each clue a weight so important clues influence the answer more.");
                case "learnrule": return T(sim, "答错后，把线索的分量往正确答案的方向调一点。", "After a wrong answer, adjust the weights a little toward the right one.");
                case "bias": return T(sim, "让判断的门槛可以移动，不必总从同一个起点划线。", "Move the decision threshold instead of always drawing the boundary from the same starting point.");
                case "step": return T(sim, "像开关一样：线索总分过门槛就开，否则就关。", "Like a switch: turn on when the combined clues cross the threshold, otherwise stay off.");
                case "sigmoid": return T(sim, "把突然开关改成平滑变化，让细小的误差也能传回去。", "Replace an abrupt switch with a smooth curve so small errors can travel backward.");
                case "relu": return T(sim, "原理是正数照传、负数归零；游戏用较高的层间信号保留率表现深层网络更容易学习。", "ReLU passes positive values through and sets negative values to zero. The game represents easier deep learning with higher signal retention between layers.");
                case "backprop": return T(sim, "从输出的误差出发，用链式法则一层层往回算，得出每个权重该改多少（Rumelhart、Hinton、Williams 1986 年；Werbos 1974 年已提出）。", "Start from the output error and work back layer by layer with the chain rule to find how much each weight should change (Rumelhart, Hinton and Williams, 1986; Werbos had proposed it in 1974).");
                case "chainrule": return T(sim, "由程序算出每个参数该往哪边改，省去手工推导。", "Let the program calculate how each parameter should change.");
                case "features": return T(sim, "人先把零散线索整理成组合，再交给网络判断。", "People combine raw clues before the network makes its decision.");
                case "irnn": return T(sim, "让循环网络初始时尽量原样传递记忆，少丢掉前面的字。", "Start a recurrent network with wiring that passes memory onward with less loss.");
                case "position": return T(sim, "给每个字加上位置编号，让网络分清前后顺序。", "Tag each word with its position so the network can tell their order.");
                case "warmup": return T(sim, "刚换设置时先小步学习，再慢慢恢复正常步幅。", "After a settings change, start with small learning steps and gradually return to the normal size.");
                case "fp16": return T(sim, "用较短的数字表示参与计算，降低大规模计算的资源需求。", "Use shorter number representations to reduce the resources needed for large-scale computation.");
                case "momentum": return T(sim, "记住前几次调整的方向，少走来回摇摆的路。", "Remember recent update directions to reduce back-and-forth motion.");
                case "rmsprop": return T(sim, "根据每个参数最近变化的大小，分别调整学习步幅。", "Adjust each parameter's step size using the size of its recent changes.");
                case "adam": return T(sim, "同时参考过去的方向和变化大小，为每个参数调整步幅。", "Use both recent direction and change size to adjust each parameter's steps.");
                case "gradclip": return T(sim, "一次改动太大时把它截短，减少训练突然失稳。", "Trim oversized updates to reduce sudden training instability.");
                case "dropout": return T(sim, "训练时每次随机让一部分神经元输出 0，逼其他神经元也能独立判断，减少只靠熟题背答案；考试时全员上场。", "Each training pass zeroes the outputs of a random set of neurons so the rest must decide on their own, reducing reliance on memorized examples; at test time every neuron is used.");
                case "augment": return T(sim, "把图片题平移，或把围棋局面旋转、镜像，换个样子继续练，答案保持不变。", "Shift picture examples, or rotate and mirror Go positions, to practise different appearances with the same answers.");
                case "dataclean": return T(sim, "清理杂包、众包和爬取数据里的错误示范。", "Clean incorrect examples from junk packs, crowd data and crawled data.");
                case "shared.lr": return T(sim, "比较不同学习步幅，寻找更合适的训练设置。", "Compare learning step sizes to find a better training setting.");
                case "lrschedule": return T(sim, "原理是先大步接近答案，再小步调整细节；游戏将它体现为成绩上限改善。", "The principle is to take larger steps first and refine with smaller ones; the game represents this through a better score ceiling.");
                case "batchnorm": return T(sim, "把一批数据的信号范围调齐，让层与层之间更容易配合。", "Align signal ranges across a batch so successive layers work together more easily.");
                case "cudnn": return T(sim, "使用显卡厂商优化过的计算方法，减少重复计算的开销。", "Use GPU-optimized routines to reduce the cost of repeated calculations.");
                case "cooler": return T(sim, "加强机箱风道，把显卡的热量排出去：少降频，也少一些高温老化。", "Improve case airflow to carry GPU heat away: less throttling and less heat wear.");
                case "transfer": return T(sim, "换一种网络结构时，带走已经学过、还能用的基础知识。", "Carry useful basic knowledge over when changing network architecture.");
                case "earlystop": return T(sim, "早停的原理是：验证成绩不再提高时，就结束训练。", "Early stopping means ending training when validation scores stop improving.");
                case "wordvec": return T(sim, "用一串数字表示词语，让相似含义可以互相比较。", "Represent words with lists of numbers so similar meanings can be compared.");
                case "beamsearch": return T(sim, "生成句子时多保留几条候选，继续比较哪条更合适。", "Keep several candidate sentences while generating and compare how they develop.");
                case "subword": return T(sim, "把生僻词拆成常见的小片段，减少完全没见过的词。", "Split rare words into common pieces to reduce unfamiliar vocabulary.");
                case "sft": return T(sim, "用指令和示范答案教模型按要求回答。", "Teach the model to follow requests using instructions and example answers.");
                case "cot": return T(sim, "把解题过程分成几步，先写中间步骤再给答案。", "Break a problem into steps, working through intermediate reasoning before the answer.");
                case "multihead": return T(sim, "几组注意力同时寻找不同线索，再合并结果。", "Several attention heads look for different clues and combine their results.");
                case "layernorm": return T(sim, "把一个字内部的各项信号调到相近范围。", "Bring the signals within one word representation into comparable ranges.");
                case "residual": return T(sim, "给信息留一条绕过中间层的直路，减少层层传递时的损失。", "Give information a shortcut around intermediate layers to reduce transmission loss.");
                case "datacenter": return T(sim, "把显卡放进有专门供电的机柜，容纳更大的训练任务。", "Put GPUs in a rack with dedicated power for larger training jobs.");
                case "secret.6": return T(sim, "查看预训练所需的参数、数据和道具倍数如何配合。", "Read how parameters, data and item multipliers combine for pre-training.");
                case "label.raise": return T(sim, "提高亲手答题的单题报酬。", "Increase the pay for answers you give by hand.");
                case "label.auto": return T(sim, "用评估合格的模型代答标注题。", "Let an evaluated model answer labelling cards automatically.");
                case "label.coop": return T(sim, "有把握的题自动交，拿不准的题留给你复核。", "Submit confident answers automatically and send uncertain ones to you for review.");
                case "label.brain": return T(sim, "让本地 AI 阅读文字题，再给出自己的判断。", "Let the local AI read text cards and give its own judgment.");
                case "label.hard": return T(sim, "为你处理需要复核的难题提供额外奖励。", "Reward you for handling difficult cards that need human review.");
                case "label.audit": return T(sim, "抽查自动提交的答案，在错题进入训练前拦下一部分。", "Sample automatic answers and catch some mistakes before they enter the training data.");
                case "label.parallel": return T(sim, "把拿不准的题先存起来，等你有空再复核。", "Keep uncertain answers until you have time to review them.");
                case "label.trapmemory": return T(sim, "记住平台用来检查质量的标准答案题。", "Remember known-answer items the platform uses to check quality.");
                case "label.captcha": return T(sim, "自动完成人机验证，省去手输四位数字的步骤。", "Complete four-digit verification automatically, saving you from typing it.");
                default: return null;
            }
        }

        static string ArchitectureExplanation(XgNode node, XgSim sim)
        {
            switch (node.id)
            {
                case "perceptron": return T(sim, "给线索分配分量，加总后与门槛比较，回答是或否；复杂组合需要人先整理线索。", "Weight the clues and compare their sum with a threshold to answer yes or no; complex combinations need prepared clues.");
                case "mlp": return T(sim, "把简单线索一层层组合起来；位置或顺序变了，仍可能认不出来。", "Combine simple clues across layers; changed positions or order can still confuse it.");
                case "lstm": case "gru": return T(sim, "边读边筛选记忆：学到重要的线索就多留一会儿，其余线索更快淡去。", "Filter memory while reading: learned important clues last longer, while others fade faster.");
                case "seq2seq": return T(sim, "先把一句话压成固定大小的摘要，再组织输出；摘要装不下的细节可能丢失。", "Compress a sentence into a fixed-size summary before producing output; details that do not fit may be lost.");
                case "caption": return T(sim, "用一句话描述图片里有什么、正在做什么。", "Describe what is in a picture and what is happening in one sentence.");
                case "lenet": case "alexnet": return T(sim, "用小窗口寻找相邻笔画，再把线索组合起来；同一套局部线索可在别的位置复用。", "Find neighbouring strokes through small windows and combine them; reuse the same local clues at other positions.");
                case "resnet": return T(sim, "给信息留一条绕过中间层的近道，让原来的线索直接传下去。", "Give information a shortcut around intermediate layers so original clues can pass straight through.");
                default: var arch = XgCatalog.Arch(node.target); return T(sim, arch.note, arch.noteEn);
            }
        }

    }
}
