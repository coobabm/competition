using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    /// <summary>Three-line technology cards. Explanations belong to the explicit Details action.</summary>
    public static class XgTechTreeCopy
    {
        sealed class Copy
        {
            public readonly string name, nameEn, summary, summaryEn, effect, effectEn;
            public Copy(string n,string en,string s,string se,string e,string ee){name=n;nameEn=en;summary=s;summaryEn=se;effect=e;effectEn=ee;}
        }
        static string T(XgSim sim,string zh,string en)=>sim.English?en:zh;
        static readonly Dictionary<string,Copy> All=new Dictionary<string,Copy>(StringComparer.Ordinal)
        {
            { "perceptron", new Copy("感知机 P 1958","Perceptron P 1958","用直线分两边","Split with one line","启用基础脑型","Enables basic architecture") },
            { "mlp", new Copy("多层感知机 MLP 1986","Multilayer MLP 1986","把简单线索组合起来","Combine simple clues","加入多层脑型","Adds multilayer architecture") },
            { "lenet", new Copy("手写卷积 LeNet 1998","Digit CNN LeNet 1998","小窗口寻找笔画","Find strokes locally","加入手写卷积脑型","Adds digit CNN architecture") },
            { "rnn", new Copy("循环网络 RNN 1986","Recurrent RNN 1986","边读边记前文","Read and remember","加入循环脑型","Adds recurrent architecture") },
            { "alexnet", new Copy("深层卷积 AlexNet 2012","Deep CNN AlexNet 2012","叠起更多图像线索","Stack visual clues","加入深层卷积脑型","Adds deep CNN architecture") },
            { "vgg", new Copy("小窗卷积 VGG 2014","Small-filter VGG 2014","小窗口层层叠起来","Stack small filters","加入小窗卷积脑型","Adds VGG architecture") },
            { "googlenet", new Copy("多路卷积 Inception 2014","Multi-path Inception 2014","大小窗口一起看","Use several window sizes","加入多路卷积脑型","Adds multi-path architecture") },
            { "lstm", new Copy("门控记忆 LSTM 1997","Gated memory LSTM 1997","让重要线索多留一会","Keep useful clues longer","加入门控记忆脑型","Adds gated-memory architecture") },
            { "gru", new Copy("简化记忆 GRU 2014","Compact memory GRU 2014","用更少参数记前文","Remember with fewer weights","加入简化记忆脑型","Adds compact-memory architecture") },
            { "textcnn", new Copy("文字卷积 CNN 2014","Text CNN 2014","用小窗口读短句","Read local word groups","加入文字卷积脑型","Adds text CNN architecture") },
            { "resnet", new Copy("残差网络 ResNet 2015","Residual ResNet 2015","给信息开一条近道","Give signals a shortcut","启用跨层直连","Enables skip connections") },
            { "seq2seq", new Copy("编码解码 Seq2Seq 2014","Encoder-decoder S2S 2014","读完一句再组织输出","Read, then compose","加入编码解码脑型","Adds encoder-decoder architecture") },
            { "attention", new Copy("回看注意力 Attn 2014","Look-back Attn 2014","说话时回头看原文","Look back at the source","移除翻译瓶颈折减","Removes translation bottleneck") },
            { "caption", new Copy("图像描述 Cap 2015","Image caption Cap 2015","给图像配一句话","Describe a picture","表情题显示描述","Shows descriptions on meme cards") },
            { "transformer", new Copy("自研新脑 TF 探索2016","Own TF experiment 2016","让整句线索一起看","Attend across the sentence","加入自研注意力脑型","Adds experimental architecture") },
            { "weights", new Copy("权重 W 1958","Weights W 1958","给线索分配分量","Weight each clue","训练处理量×1.1","Training throughput ×1.1") },
            { "learnrule", new Copy("感知学习 PL 1958","Perceptron rule PL 1958","答错就朝正确方向调","Adjust after mistakes","训练处理量×1.15","Training throughput ×1.15") },
            { "step", new Copy("阶跃开关 Step 1943","Threshold Step 1943","过线才打开开关","Switch at a threshold","成绩上限项加1点","Ceiling bonus term +1 point") },
            { "bias", new Copy("偏置 Bias 1958","Bias 1958","让判断门槛能移动","Move the decision threshold","成绩上限项加2点","Ceiling bonus term +2 points") },
            { "features", new Copy("组合线索 FE 工具2016","Feature tools FE tool 2016","人先帮它整理线索","Prepare combined clues","解除逻辑55%上限","Removes the 55% base logic cap") },
            { "shared.lr", new Copy("学习率搜索 LRS 工具2016","Rate search LRS tool 2016","试试哪种步子合适","Try different step sizes","显示六档学习率","Shows six learning rates") },
            { "sigmoid", new Copy("平滑开关 Sigmoid 1986","Smooth Sigmoid 1986","小误差也能传回来","Pass small errors back","启用平滑激活","Enables smooth activation") },
            { "momentum", new Copy("动量下降 MSGD 1964","Momentum MSGD 1964","顺着之前的方向走","Follow recent direction","优化器处理量×1.3","Optimizer throughput ×1.3") },
            { "backprop", new Copy("小批量 MB 工具2016","Mini-batch MB tool 2016","看一批题再改一次","Update after a small batch","训练处理量×1.2","Training throughput ×1.2") },
            { "chainrule", new Copy("自动求导 AD 1970","Auto-diff AD 1970","让程序算调整方向","Calculate update directions","训练处理量×1.1","Training throughput ×1.1") },
            { "cudnn", new Copy("显卡加速 cuDNN 2014","GPU library cuDNN 2014","用显卡擅长的算法","Use GPU-tuned routines","训练用电量×0.7","Training energy ×0.7") },
            { "cooler", new Copy("塔式散热 FAN 工具2016","Tower cooler FAN tool 2016","把训练热量带走","Carry training heat away","训练烧卡风险减半","Halves training burn-out risk") },
            { "relu", new Copy("整流激活 ReLU 2010","Rectifier ReLU 2010","正数传下去负数归零","Pass positives, zero negatives","训练处理量×1.3","Training throughput ×1.3") },
            { "gradclip", new Copy("梯度裁剪 Clip 2013","Gradient Clip 2013","截短过大的改动","Limit oversized updates","单权重改动限幅","Clamps per-weight updates") },
            { "irnn", new Copy("单位初始化 IRNN 2015","Identity init IRNN 2015","先让记忆原样传递","Start with identity memory","提高循环记忆保留率","Raises recurrent retention") },
            { "dropout", new Copy("随机失活 Dropout 2012","Dropout 2012","少靠几条熟悉线索","Avoid relying on few clues","过拟合风险项减半","Halves overfit risk term") },
            { "augment", new Copy("数据增强 AUG 工具2016","Data augmentation AUG tool 2016","换个样子继续认","Recognise varied views","扩充图像训练题池","Expands image training pool") },
            { "dataclean", new Copy("数据清洗 CLEAN 工具2016","Data cleanup CLEAN tool 2016","先挑掉错误示范","Remove wrong examples","外来有效错标减半","Halves effective external noise") },
            { "rmsprop", new Copy("自适应步长 RMSProp 2012","Adaptive RMSProp 2012","每个参数各走各的","Adapt each parameter's step","优化器处理量×1.6","Optimizer throughput ×1.6") },
            { "batchnorm", new Copy("批归一化 BN 2015","Batch norm BN 2015","把一批信号调齐","Align a batch of signals","结构耐受层数加6","Architecture depth tolerance +6") },
            { "adam", new Copy("自适应动量 Adam 2014","Adaptive momentum Adam 2014","方向和步幅一起调","Adapt direction and step","优化器处理量×2","Optimizer throughput ×2") },
            { "lrschedule", new Copy("学习率衰减 LRD 工具2016","Rate decay LRD tool 2016","先大步再慢慢细调","Large steps, then fine steps","降低成绩上限误差项","Lowers ceiling error term") },
            { "position", new Copy("位置标记 POS 探索2016","Own position experiment 2016","给每个字一个位置","Tag each word's position","启用文字位置线索","Enables word-position clues") },
            { "transfer", new Copy("迁移学习 TL 工具2016","Transfer learning TL tool 2016","换脑型也带上旧知识","Carry knowledge to a new model","换脑型保留60%增益","Keeps 60% above-chance accuracy") },
            { "multihead", new Copy("多路注意 MH 探索2016","Own multi-head experiment 2016","几路同时寻找线索","Look for several clue types","随自研脑型获得","Granted with experimental model") },
            { "layernorm", new Copy("层归一化 LN 2016","Layer norm LN 2016","把内部信号调齐","Align internal signals","随自研脑型获得","Granted with experimental model") },
            { "residual", new Copy("残差直连 RC 2015","Residual links RC 2015","留一条不绕远的路","Leave a direct signal path","随自研脑型获得","Granted with experimental model") },
            { "warmup", new Copy("学习率预热 WU 工具2016","Rate warm-up WU tool 2016","换设置后先小步走","Start gently after changes","前期步幅渐进提升","Ramps initial learning steps") },
            { "fp16", new Copy("半精度 FP16 2016","Half precision FP16 2016","用更短的数字计算","Compute with shorter numbers","预训练规模×2","Pretraining scale ×2") },
            { "earlystop", new Copy("早停研究 ES 1990","Early-stop study ES 1990","研究何时该停下来","Study when to stop","训练处理量×1.05","Training throughput ×1.05") },
            { "wordvec", new Copy("词向量 W2V 2013","Word vectors W2V 2013","把词语变成数字","Represent words as numbers","训练处理量×1.08","Training throughput ×1.08") },
            { "beamsearch", new Copy("束搜索 BS 1977","Beam search BS 1977","多留几条候选句","Keep several candidates","训练处理量×1.1","Training throughput ×1.1") },
            { "subword", new Copy("子词切分 BPE 2016","Subword BPE 2016","生僻词拆成小块","Split rare words into pieces","训练处理量×1.05","Training throughput ×1.05") },
            { "sft", new Copy("示范跟答 SFT 探索2016","Own answer-tuning trial 2016","照着好回答学说话","Learn from good answers","训练处理量×1.05","Training throughput ×1.05") },
            { "cot", new Copy("分步解题 CoT 探索2016","Own reasoning trial 2016","先想几步再回答","Work through steps first","训练处理量×1.05","Training throughput ×1.05") },
            { "secret.6", new Copy("配置指南 GUIDE 工具2016","Setup guide GUIDE tool 2016","看看预训练怎样配","Read the pretraining setup","显示预训练配置提示","Shows pretraining setup hints") },
            { "datacenter", new Copy("机柜租赁 IDC 工具2016","Rack rental IDC tool 2016","给大训练找个机位","Make room for larger training","开放机房机架","Opens the data-centre rack") },
            { "label.raise", new Copy("答题加薪 PAY 工具2016","Answer pay PAY tool 2016","每道题多赚一点","Earn more per answer","提高人工答题报酬","Raises hand-answer pay") },
            { "label.auto", new Copy("自动答题 AUTO 工具2016","Auto answering AUTO tool 2016","让合格模型代劳","Let qualified models help","每级每桌加0.15题秒","Adds 0.15 cards/s per desk/level") },
            { "label.coop", new Copy("把握门槛 CONF 工具2016","Confidence gate CONF tool 2016","拿不准就留给你","Leave uncertain cards to you","开放把握阈值设置","Unlocks confidence threshold") },
            { "label.brain", new Copy("本地判题 LLM 工具2016","Local judging LLM tool 2016","让它读题说说理由","Let it read and judge","开放本地模型判题","Unlocks local-model judging") },
            { "label.hard", new Copy("难例奖励 HARD 工具2016","Hard-case reward HARD tool 2016","难题复核更值钱","Reward difficult reviews","新题复核报酬×1.5","New-card review pay ×1.5") },
            { "label.audit", new Copy("答案抽检 QA 工具2016","Answer audit QA tool 2016","拦住一部分错答案","Catch wrong submissions","错题抽检率40%","Audits 40% of wrong answers") },
            { "label.parallel", new Copy("复核扩容 Q 工具2016","Review inbox Q tool 2016","存下拿不准的题","Keep uncertain answers","复核容量20→60","Review capacity 20 to 60") },
            { "label.trapmemory", new Copy("金题匹配 GOLD 工具2016","Gold-item match GOLD tool 2016","认出见过的检查题","Recognise known test cards","金题转人工复核","Routes gold items to review") },
            { "label.captcha", new Copy("验证码代填 OCR 工具2016","Captcha helper OCR tool 2016","让它替你抄数字","Let it type the digits","新验证码自动提交","Auto-submits new captchas") },
            { "train.data", new Copy("数据导入 DATA 工具2016","Data Import DATA tool 2016","把资料装进大脑","Load files into its brain","开放资料夹、安装与网址","Opens files, import and URLs") },
            { "train.ledger", new Copy("记账本 LEDGER 工具2016","Ledger LEDGER tool 2016","看清每笔钱的去向","Track every payment","显示资金收入与电力","Shows funds, income and power") },
            { "train.curve", new Copy("训练曲线 CURVE 工具2016","Training curve CURVE tool 2016","看看每轮有没有进步","Watch progress each round","解锁练页","Unlocks Train") },
            { "train.holdout", new Copy("留出新题 VAL 工具2016","Holdout set VAL tool 2016","用没练过的题考它","Test unseen questions","显示新题成绩曲线","Shows validation curve") },
            { "train.checkpoint", new Copy("检查点 CKPT 工具2016","Checkpoint CKPT tool 2016","保留最好的一版","Keep the best version","开放最佳成绩回档","Unlocks best-checkpoint restore") },
            { "train.monitor", new Copy("训练监控 MON 工具2016","Training monitor MON tool 2016","先看这轮有哪些风险","Preview this round's risks","显示训练风险","Shows training risks") },
            { "train.weights", new Copy("权重可视化 VIS 工具2016","Weight viewer VIS tool 2016","看看它在注意哪里","See which clues it uses","开放看它学","Opens the learning viewer") },
            { "brain.window", new Copy("脑子窗口 BRAIN 工具2016","Brain window BRAIN tool 2016","看看它怎么想","Watch its activity","开放脑子窗口","Opens the brain window") },
            { "label.ai", new Copy("{AI}代答 ANS 工具2016","{AI} answers ANS tool 2016","每题先看看它怎么答","See its answer on each card","每题显示它的回答","Shows its answer on every card") },
        };
        public static bool Covered(XgNode n)=>n!=null&&(XgSim.IsAtlas(n)||All.ContainsKey(n.id)||n.kind==XgNodeKind.Depth||n.kind==XgNodeKind.Width||n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack.")||n.id.StartsWith("persona."));
        public static string Name(XgNode n,XgSim sim)
        {
            if(n==null)return "?";
            if(sim.NodeMystery(n))return sim.NodeName(n);
            if(XgSim.IsAtlas(n))return sim.NodeName(n);
            if(All.TryGetValue(n.id,out var c))return T(sim,c.name,c.nameEn).Replace("{AI}",sim.AiDisplayName);
            if(n.kind==XgNodeKind.Depth)return T(sim,"网络深度 D 工具2016","Network depth D tool 2016");
            if(n.kind==XgNodeKind.Width)return T(sim,"网络宽度 W 工具2016","Network width W tool 2016");
            if(n.id=="danmu.pack")return T(sim,"弹幕情绪 DATA 工具2016","Comment sentiment DATA 2016");
            if(n.id=="pack.poem_next")return T(sim,"唐诗接字 DATA 工具2016","Tang verse DATA 2016");
            return sim.NodeName(n)+T(sim,n.id.StartsWith("persona.")?" PERS 工具2016":" DATA 工具2016",n.id.StartsWith("persona.")?" PERS tool 2016":" DATA tool 2016");
        }
        public static string Summary(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return T(sim,"还没发现这条线索","An undiscovered clue");
            if(XgSim.IsAtlas(n))return T(sim,"把遇到的变化记下来","Remember what changed");
            if(All.TryGetValue(n.id,out var c))return T(sim,c.summary,c.summaryEn);
            if(n.kind==XgNodeKind.Depth)return T(sim,"多一层组合线索","Combine clues in more layers");
            if(n.kind==XgNodeKind.Width)return T(sim,"同时容纳更多线索","Hold more clues per layer");
            if(n.id.StartsWith("persona."))return T(sim,"聊聊它会怎样选择","Explore its choices");
            if(n.kind==XgNodeKind.Dataset||n.id.StartsWith("pack."))return T(sim,"换一套题继续教它","Teach it a new question set");
            return T(sim,"内容待核对","Copy needs review");
        }
        public static string Effect(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return T(sim,"▶ 发现后查看","▶ Discover to reveal");
            if(XgSim.IsAtlas(n))return T(sim,"▶ 查看成长记录","▶ View the growth record");
            if(All.TryGetValue(n.id,out var c))return "▶ "+T(sim,c.effect,c.effectEn);
            if(n.kind==XgNodeKind.Depth)return "▶ "+T(sim,"可用层数上限"+n.value,"Layer cap "+n.value);
            if(n.kind==XgNodeKind.Width)return "▶ "+T(sim,"每层宽度上限"+XgCatalog.Widths[Math.Max(0,Math.Min(n.value,XgCatalog.Widths.Length-1))],"Width cap "+XgCatalog.Widths[Math.Max(0,Math.Min(n.value,XgCatalog.Widths.Length-1))]);
            if(n.id.StartsWith("pack."))return T(sim,"▶ 开放对应题桌","▶ Opens this question desk");
            if(n.id.StartsWith("persona."))return T(sim,"▶ 开放对应性格题包","▶ Opens this personality pack");
            if(n.kind==XgNodeKind.Dataset)return T(sim,"▶ 获得数据包使用权","▶ Grants access to this dataset");
            return T(sim,"▶ 功能待核对","▶ Effect needs review");
        }
        public static string Card(XgNode n,XgSim sim)=>Name(n,sim)+"\n"+Summary(n,sim)+"\n"+Effect(n,sim);
        // Kept for older callers. This is the short two-line body, never the glossary.
        public static string Note(XgNode n,XgSim sim)=>Summary(n,sim)+"\n"+Effect(n,sim);
        public static string Detail(XgNode n,XgSim sim)
        {
            if(n==null||sim.NodeMystery(n))return Summary(n,sim)+"。";
            if(n.id=="transformer"||n.id=="multihead"||n.id=="position"||n.id=="sft"||n.id=="cot")
                return T(sim,"这是你在2016年尝试的自研方向，不是当年公开发表成果的介绍。","This is your own experiment in 2016, not a claim about a publication from that year.")+"\n"+Summary(n,sim)+T(sim,"。",".");
            if(n.id=="label.parallel")return T(sim,"待复核容量从20题增加到60题，满后自动暂停。多留四十道题，等你有空再来复核。","The review inbox expands from 20 to 60 cards; automation pauses when full. Keep forty more cards until you have time to review them.");
            if(n.id.StartsWith("pack.")){var p=XgTeachingPacks.Find(n.id.Substring(5));return p==null?Summary(n,sim):T(sim,p.play,p.playEn);}
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
                case "weights": return T(sim, "给每条线索分配不同的分量，重要的线索更能影响答案。", "Give each clue a weight so important clues influence the answer more.");
                case "learnrule": return T(sim, "答错后，把线索的分量往正确答案的方向调一点。", "After a wrong answer, adjust the weights a little toward the right one.");
                case "bias": return T(sim, "让判断的门槛可以移动，不必总从同一个起点划线。", "Move the decision threshold instead of always drawing the boundary from the same starting point.");
                case "step": return T(sim, "像开关一样：线索总分过门槛就开，否则就关。", "Like a switch: turn on when the combined clues cross the threshold, otherwise stay off.");
                case "sigmoid": return T(sim, "把突然开关改成平滑变化，让细小的误差也能传回去。", "Replace an abrupt switch with a smooth curve so small errors can travel backward.");
                case "relu": return T(sim, "原理是正数照传、负数归零；游戏用较高的层间信号保留率表现深层网络更容易学习。", "ReLU passes positive values through and sets negative values to zero. The game represents easier deep learning with higher signal retention between layers.");
                case "backprop": return T(sim, "小批量训练的原理是先看一小批例题，汇总调整方向，再更新网络。", "Mini-batch training means combining suggested changes from a small group of examples before updating the network.");
                case "chainrule": return T(sim, "由程序算出每个参数该往哪边改，省去手工推导。", "Let the program calculate how each parameter should change.");
                case "features": return T(sim, "人先把零散线索整理成组合，再交给网络判断。", "People combine raw clues before the network makes its decision.");
                case "irnn": return T(sim, "让循环网络初始时尽量原样传递记忆，少丢掉前面的字。", "Start a recurrent network with wiring that passes memory onward with less loss.");
                case "position": return T(sim, "给每个字加上位置编号，让网络分清前后顺序。", "Tag each word with its position so the network can tell their order.");
                case "warmup": return T(sim, "刚换设置时先小步学习，再慢慢恢复正常步幅。", "After a settings change, start with small learning steps and gradually return to the normal size.");
                case "fp16": return T(sim, "用较短的数字表示参与计算，降低大模型计算的资源需求。", "Use shorter number representations to reduce the resources needed for large-model computation.");
                case "momentum": return T(sim, "记住前几次调整的方向，少走来回摇摆的路。", "Remember recent update directions to reduce back-and-forth motion.");
                case "rmsprop": return T(sim, "根据每个参数最近变化的大小，分别调整学习步幅。", "Adjust each parameter's step size using the size of its recent changes.");
                case "adam": return T(sim, "同时参考过去的方向和变化大小，为每个参数调整步幅。", "Use both recent direction and change size to adjust each parameter's steps.");
                case "gradclip": return T(sim, "一次改动太大时把它截短，减少训练突然失稳。", "Trim oversized updates to reduce sudden training instability.");
                case "dropout": return T(sim, "训练时让部分线索暂停更新，减少只靠熟题背答案。", "Pause updates to some clues during training to reduce reliance on memorized examples.");
                case "augment": return T(sim, "把图片题平移，或把围棋局面旋转、镜像，换个样子继续练，答案保持不变。", "Shift picture examples, or rotate and mirror Go positions, to practise different appearances with the same answers.");
                case "dataclean": return T(sim, "清理杂包、众包和爬取数据里的错误示范。", "Clean incorrect examples from junk packs, crowd data and crawled data.");
                case "shared.lr": return T(sim, "比较不同学习步幅，寻找更合适的训练设置。", "Compare learning step sizes to find a better training setting.");
                case "lrschedule": return T(sim, "原理是先大步接近答案，再小步调整细节；游戏将它体现为成绩上限改善。", "The principle is to take larger steps first and refine with smaller ones; the game represents this through a better score ceiling.");
                case "batchnorm": return T(sim, "把一批数据的信号范围调齐，让层与层之间更容易配合。", "Align signal ranges across a batch so successive layers work together more easily.");
                case "cudnn": return T(sim, "使用显卡厂商优化过的计算方法，减少重复计算的开销。", "Use GPU-optimized routines to reduce the cost of repeated calculations.");
                case "cooler": return T(sim, "用更好的散热器带走训练时产生的热量。", "Use a better cooler to carry away heat from training.");
                case "transfer": return T(sim, "换一种网络结构时，带走已经学过、还能用的基础知识。", "Carry useful basic knowledge over when changing network architecture.");
                case "earlystop": return T(sim, "早停的原理是：验证成绩不再提高时，就结束训练。", "Early stopping means ending training when validation scores stop improving.");
                case "wordvec": return T(sim, "用一串数字表示词语，让相似含义可以互相比较。", "Represent words with lists of numbers so similar meanings can be compared.");
                case "beamsearch": return T(sim, "生成句子时多保留几条候选，继续比较哪条更合适。", "Keep several candidate sentences while generating and compare how they develop.");
                case "subword": return T(sim, "把生僻词拆成常见的小片段，减少完全没见过的词。", "Split rare words into common pieces to reduce unfamiliar vocabulary.");
                case "sft": return T(sim, "用指令和示范答案教模型按要求回答。", "Teach the model to follow requests using instructions and example answers.");
                case "cot": return T(sim, "把解题过程分成几步，先写中间推理再给答案。", "Break a problem into steps, working through intermediate reasoning before the answer.");
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
