using System;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>Technology-page copy only. Never changes catalog data, purchases or player state.</summary>
    public static class XgTechTreeCopy
    {
        static string T(XgSim sim, string zh, string en) => sim.English ? en : zh;
        static string N(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);

        public static string Name(XgNode node, XgSim sim)
        {
            if (node == null || sim.NodeMystery(node) || XgSim.IsAtlas(node)) return sim.NodeName(node);
            switch (node.id)
            {
                case "mlp": return T(sim, "多层感知机（MLP）", "Multilayer perceptron (MLP)");
                case "resnet": return T(sim, "残差网络（ResNet）", "Residual network (ResNet)");
                case "momentum": return T(sim, "动量随机梯度下降", "Momentum SGD");
                case "adam": return T(sim, "Adam 优化器", "Adam optimizer");
                case "sft": return T(sim, "监督微调（SFT）", "Supervised fine-tuning (SFT)");
                case "lrschedule": return T(sim, "学习率衰减", "Learning-rate decay");
                case "rnn": return T(sim, "循环神经网络（RNN）", "Recurrent neural network (RNN)");
                case "lstm": return T(sim, "长短期记忆网络（LSTM）", "Long short-term memory (LSTM)");
                case "gru": return T(sim, "门控循环单元（GRU）", "Gated recurrent unit (GRU)");
                case "seq2seq": return T(sim, "序列到序列模型", "Sequence-to-sequence model");
                case "attention": return T(sim, "注意力机制", "Attention mechanism");
                case "caption": return T(sim, "图像描述生成", "Image captioning");
                case "textcnn": return T(sim, "文本卷积网络", "Text convolutional network");
                case "step": return T(sim, "阶跃激活函数", "Step activation function");
                case "sigmoid": return T(sim, "Sigmoid 激活函数", "Sigmoid activation function");
                case "relu": return T(sim, "整流线性单元（ReLU）", "Rectified linear unit (ReLU)");
                case "irnn": return T(sim, "单位矩阵初始化", "Identity matrix initialization");
                case "position": return T(sim, "位置编码", "Positional encoding");
                case "dropout": return T(sim, "随机失活（Dropout）", "Dropout");
                case "batchnorm": return T(sim, "批量归一化", "Batch normalization");
                case "layernorm": return T(sim, "层归一化", "Layer normalization");
                case "cooler": return T(sim, "塔式散热器", "Tower cooler");
                case "datacenter": return T(sim, "IDC 机柜租赁", "IDC rack rental");
                case "secret.6": return T(sim, "预训练配置指南", "Pre-training configuration guide");
                case "label.raise": return T(sim, "标注报酬倍率", "Labelling pay multiplier");
                case "label.auto": return T(sim, "自动标注", "Automatic labelling");
                case "label.coop": return T(sim, "置信度分流", "Confidence-based routing");
                case "label.brain": return T(sim, "本地模型推理", "Local model inference");
                case "label.hard": return T(sim, "难例复核奖励", "Hard-case review rewards");
                case "label.audit": return T(sim, "标注质量抽检", "Labelling quality audit");
                case "label.parallel": return T(sim, "并发推理", "Concurrent inference");
                case "label.trapmemory": return T(sim, "金标准题匹配", "Gold-standard item matching");
                case "label.captcha": return T(sim, "验证码自动填充", "Captcha autofill");
                default: return sim.NodeName(node);
            }
        }

        public static string Note(XgNode node, XgSim sim)
        {
            if (node == null || sim.NodeMystery(node) || XgSim.IsAtlas(node)) return sim.NodeNote(node);
            string explanation = Explanation(node, sim), effect = Effect(node, sim);
            // Unknown future nodes retain their authored copy until their implementation is reviewed.
            if (explanation == null || effect == null) return sim.NodeNote(node);
            var body = new StringBuilder(explanation);
            body.Append(T(sim, "\n\n效果：", "\n\nEffect: ")).Append(effect);
            foreach (var item in XgSim.ScaleItems)
                if (item.item == node.id)
                    body.Append(T(sim, "\n预训练有效规模 ×", "\nEffective pre-training scale ×")).Append(N(item.factor)).Append(T(sim, "（与其他道具倍数相乘）。", " (multiplies with other items)."));
            return body.ToString();
        }

        static string Explanation(XgNode node, XgSim sim)
        {
            switch (node.kind)
            {
                case XgNodeKind.Depth: return T(sim, "多一层，就多一次把简单线索组合成复杂线索的机会。", "Each extra layer can combine simple clues into more complex ones.");
                case XgNodeKind.Width: return T(sim, "同一层能同时容纳更多线索，网络就更宽。", "A wider layer can hold more clues at the same time.");
                case XgNodeKind.Dataset: return T(sim, "用已经整理好的例题和答案，扩充可训练的数据。", "Add a prepared collection of examples and answers to the training data.");
                case XgNodeKind.Arch: return ArchitectureExplanation(node, sim);
                case XgNodeKind.Auto: return T(sim, "把重复的训练步骤交给程序按时执行。", "Let a program repeat training steps on a schedule.");
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
                case "label.parallel": return T(sim, "让本地模型同时处理几项判断请求。", "Let the local model handle several judgment requests at once.");
                case "label.trapmemory": return T(sim, "记住平台用来检查质量的标准答案题。", "Remember known-answer items the platform uses to check quality.");
                case "label.captcha": return T(sim, "自动完成人机验证，省去手输四位数字的步骤。", "Complete four-digit verification automatically, saving you from typing it.");
                default: return null;
            }
        }

        static string Speed(XgSim sim, double factor) => T(sim, "两条训练线在购买后的轮次结算时，训练处理量 ×", "At epoch settlement after purchase, both tracks gain training throughput ×") + N(factor)
            + T(sim, "，与其他处理量倍率相乘，无需装备。每轮卡数受算力、取整和上下限影响；离线模拟固定卡数不享受此加成。轮次时长不变，成绩需训练后评估。", ", multiplying with other throughput factors; no equipment step. Compute, rounding and caps affect cards per epoch; offline simulation keeps its fixed card count. Epoch duration is unchanged; evaluate after training for scores.");

        static string CeilingBonus(XgSim sim, int points) => T(sim, "购买后无需装备，两条训练线的准确率上限公式加", "No equipment step after purchase: add ") + points
            + T(sim, "个百分点，与同类加成相加，再受结构和数据集上限限制。不是当前成绩立即加分，需继续训练和评估。", " percentage points to both tracks' accuracy-ceiling formula. Adds to similar bonuses before architecture and dataset caps; train and evaluate to improve the actual score.");

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

        static string ArchitectureEffect(XgNode node, XgSim sim)
        {
            var arch = XgCatalog.Arch(node.target);
            var text = new StringBuilder(node.id == "perceptron"
                ? T(sim, "开局免费拥有，两条训练线的默认结构。", "Owned free from the start; the default for both tracks.")
                : T(sim, "加入可用结构，无需装备；系统在轮次之间按数据、已购上限和显存选用，不保证立即切换。", "Adds an architecture option with no equip step; selected between epochs for the data, purchased caps and VRAM, with no guaranteed immediate switch."));
            switch (node.id)
            {
                case "perceptron": text.Append(T(sim, "未拥有人工作成的组合线索时，逻辑数据的基础准确率上限为55%。", "Without engineered clues, the base accuracy ceiling on logic data is 55%.")); break;
                case "rnn": text.Append(T(sim, "采用时，前文信号每隔一个序列位置基础保留75%，逐步相乘；已生效技术可改变此比例，记忆没有固定字数截断。", "When selected, earlier signals retain a base 75% per sequence position, compounded each step. Active techniques can change this; there is no fixed word-count cutoff.")); break;
                case "lstm": text.Append(T(sim, "采用时用门控记忆保留重要线索，仍会随距离衰减。", "When selected, gated memory keeps important clues longer, but still fades with distance.")); break;
                case "gru": text.Append(T(sim, "采用时与LSTM使用相同门控记忆规则；同深度、同宽度的参数量为LSTM的75%。", "Uses the same gated-memory rule as LSTM, with 75% of its parameters at equal depth and width.")); break;
                case "seq2seq": text.Append(T(sim, "采用后可处理需要编码—解码的数据，翻译成绩上限仍受×0.75的瓶颈折减。", "When selected, handles data requiring an encoder-decoder; the translation score ceiling still has a ×0.75 bottleneck penalty.")); break;
                case "attention": text.Append(T(sim, "采用时，原文线索不再随距句尾的距离衰减，翻译上限不再受×0.75额外折减。", "When selected, source clues no longer fade with distance from the sentence end, and the extra ×0.75 translation-ceiling penalty is removed.")); break;
                case "caption": text.Append(T(sim, "拥有后新生成的表情包题会显示预设描述和教学关注区域，供你核对；不是模型现场生成描述。", "New meme cards show preset descriptions and a teaching focus region for you to check; descriptions are not generated live by an image model.")); break;
                case "mlp": text.Append(T(sim, "采用时，空间或顺序题的模拟答对率至多60%。", "When selected, simulated correctness on spatial or order cards is at most 60%.")); break;
                case "vgg": text.Append(T(sim, "采用时超过12层，每多一层在成绩上限和单题答对率的各自计算中扣3个百分点。", "When selected above 12 layers, each extra layer subtracts 3 percentage points in each separate calculation of the score ceiling and per-card correctness.")); break;
                case "resnet": text.Append(T(sim, "拥有后两条线下次自动配置都会启用跨层直连，层间传话不再衰减或被改写；阶跃激活仍不能传回学习信号。", "Ownership enables skip connections on both tracks at their next automatic configuration, removing inter-layer signal loss and distortion; step activation still blocks backward learning signals.")); break;
                case "transformer": text.Append(T(sim, "采用时基础每轮时长为", "When selected, base epoch duration is ")).Append(N(XgSim.DurationFor(new XgRun { arch = node.id })))
                    .Append(T(sim, "秒，资源阻塞仍可能延长耗时；序列准确率上限公式加6个百分点，仍受数据集上限限制。拥有时附送多头注意力、层归一化和残差标记；达标并确认第六阶段也可免费获得。", " s, subject to resource delays. The sequence accuracy-ceiling formula gains 6 percentage points, subject to dataset caps. Ownership grants the three accompanying technology markers; qualifying and confirming stage 6 also grants this architecture free.")); break;
            }
            if (arch.maxDepth < 999) text.Append(T(sim, "基础结构可承受", " Base architecture depth tolerance: ")).Append(arch.maxDepth)
                .Append(T(sim, "层；实际层数还受已购上限、其他技术和显存限制。", " layers; purchased caps, other techniques and VRAM still apply."));
            if (arch.speed != 1) text.Append(T(sim, "采用时训练处理量系数×", " When selected, training throughput factor ×")).Append(N(arch.speed))
                .Append(T(sim, "，与其他处理量倍数相乘，卡数仍受取整和上下限限制。", ", multiplying with other throughput factors; rounding and card limits still apply."));
            text.Append(T(sim, "每条线只用一种结构，架构效果不相乘。换结构可能需重新学习，训练后再看评估成绩。", " Each track uses one architecture, without stacking architecture effects. Switching may require relearning; train and evaluate to see the score."));
            return text.ToString();
        }

        static string Effect(XgNode node, XgSim sim)
        {
            if (node.kind == XgNodeKind.Depth || node.kind == XgNodeKind.Width)
            {
                bool depth = node.kind == XgNodeKind.Depth;
                string scope = node.tree == "trunk" ? T(sim, "两条训练线", "Both training tracks") : node.tree == "vision" ? T(sim, "视觉训练线", "The vision track") : T(sim, "序列训练线", "The sequence track");
                int cap = depth ? node.value : XgCatalog.Widths[Math.Max(0, Math.Min(node.value, XgCatalog.Widths.Length - 1))];
                return scope + T(sim, depth ? "可用层数上限解锁至 " : "每层宽度上限解锁至 ", depth ? " unlock a layer cap of " : " unlock a width cap of ") + cap
                    + T(sim, "。实际模型由系统按结构限制和可用显存自动配置；购买后不保证立即变大或提分。", ". Models are configured automatically within architecture and available VRAM limits; a purchase does not guarantee immediate growth or a higher score.");
            }
            if (node.kind == XgNodeKind.Dataset)
            {
                var data = XgCatalog.Dataset(node.target);
                string result = T(sim, "提供 ", "Provide ") + XgSim.SamplesText(data.samples) + T(sim, " 条训练样本；需启用数据包，已有等价剧情数据时不重复计数。", " training samples while the pack is enabled; equivalent story data is not counted twice.");
                if (data.samples >= XgSim.BigPackSamples) result += T(sim, "下载完成后生效；普通下载约 ", "Available after download; normal download takes about ") + N(XgSim.DownloadSeconds(data.samples)) + T(sim, " 秒。", " s.");
                if (data.dataWeight != 1) result += T(sim, "每条计入总数据进度的权重为 ", "Each sample counts toward total data progress with weight ") + N(data.dataWeight) + "。";
                return result;
            }
            if (node.kind == XgNodeKind.Arch) return ArchitectureEffect(node, sim);
            if (node.kind == XgNodeKind.Auto)
            {
                string result = T(sim, "每 ", "Every ") + N(XgSim.AutoInterval(node.value)) + T(sim, " 秒尝试自动训练一轮；需训练已开启、未暂停且资源充足。每轮训练效果为手动的 ", " s, attempt an automatic epoch; training must be unlocked, running and supplied with resources. Each automatic epoch has ") + N(XgSim.AutoEpochFactor * 100) + T(sim, "% 基础效果，不累积手动连击。", "% of a hand epoch's base effect, with no manual combo.");
                result += node.value == 2 ? T(sim, "须保持训练窗口打开。", "Keep the training window open.") : T(sim, "关闭训练窗口仍运行；退出游戏后停止。", "Continues with the training window closed; stops when the game exits.");
                if (node.value == 4) result += T(sim, "连续 ", "After ") + XgSim.EarlyStopEpochs + T(sim, " 轮没有新纪录就停，保留最佳检查点。", " epochs without a record, stops and keeps the best checkpoint.");
                if (node.value >= 5) result += T(sim, "自动签符合条件的订单，停滞时换数据集；提供与学习率衰减相同的上限改善，不重复叠加。", "Signs eligible contracts and switches stalled datasets; grants the same ceiling improvement as a learning-rate schedule, without stacking.");
                return result;
            }
            var research = XgCatalog.ResearchItem(node.id);
            if (research != null && research.kind == XgResearchKind.Optimizer)
                return T(sim, "自动使用已拥有的最快优化器，各优化器不叠加。被采用时，相对基础 SGD，", "Automatically uses the fastest owned optimizer; optimizers do not stack. When selected, relative to base SGD, ") + Speed(sim, research.speed)
                    + (node.id == "adam" || node.id == "rmsprop" ? T(sim, "同配置下可承受的失稳步幅阈值×", " At the same configuration, the instability step threshold is ×") + N(node.id == "adam" ? 1.25 : 1.15) + T(sim, "，与归一化、裁剪的阈值倍数相乘。", ", multiplying with normalization and clipping threshold factors.") : "");
            switch (node.id)
            {
                case "weights": case "chainrule": return Speed(sim, 1.1);
                case "learnrule": return Speed(sim, 1.15);
                case "backprop": return Speed(sim, 1.2);
                case "relu": return Speed(sim, 1.3) + T(sim, "自动配置优先采用ReLU，激活函数不叠加。采用后，每跨一层基础保留98%的反向学习信号、95%的前向信号，逐层相乘；其他技术可改变比例。超过结构可承受层数时，上限与过拟合估算中每个超出层的有效参数保留由75%改为85%。", " Automatic configuration prefers ReLU; activations do not stack. When selected, base retention is 98% for backward learning signals and 95% for forward signals per layer, compounded across layers; other techniques can change these rates. Above the architecture depth tolerance, effective parameter retention per excess layer changes from 75% to 85% in ceiling and overfitting estimates.");
                case "earlystop": return Speed(sim, 1.05) + T(sim, "自动停训由自动化节点「早停」提供。", "Automatic stopping is provided by the automation Early stopping level.");
                case "wordvec": return Speed(sim, 1.08);
                case "beamsearch": return Speed(sim, 1.1);
                case "subword": case "sft": case "cot": return Speed(sim, 1.05);
                case "bias": return CeilingBonus(sim, 2) + T(sim, "后续训练还能学习判断门槛。", " Further training can also learn a decision threshold.");
                case "step": return CeilingBonus(sim, 1) + T(sim, "阶跃激活原本就可使用。", " Step activation is already available before purchase.");
                case "sigmoid": return CeilingBonus(sim, 1) + T(sim, "自动配置时，尚无ReLU则采用Sigmoid，激活函数不叠加。", " Automatic configuration uses Sigmoid unless ReLU is owned; activations do not stack.");
                case "features": return T(sim, "购买解除感知机在逻辑数据上的55%基础上限。系统在轮次之间为感知机启用人工组合线索，无需装备；训练卡数计算值×", "Removes the perceptron's 55% base ceiling on logic data. Automatically enables engineered clues for perceptrons between epochs; no equipment step. Calculated training card count ×") + N(XgSim.FeaturesCardFactor) + T(sim, "，与其他处理量倍数相乘后取整和限幅；离线模拟卡数不变。需继续训练和评估。", ", multiplying with other throughput factors before rounding and limits; offline card counts are unchanged. Continue training and evaluate.");
                case "irnn": return T(sim, "自动配置采用普通RNN与ReLU时，前文信号每隔一个序列位置的保留率由75%改为", "When automatic configuration uses a plain RNN with ReLU, earlier-signal retention per sequence position changes from 75% to ") + N(XgBoard.IdentityKeep * 100) + T(sim, "%，逐步相乘；仍会衰减。梯度裁剪是购买前置，自动配置会启用；成绩需训练后评估。", "%, compounded each step; memory still fades. Clipping is a purchase prerequisite and is enabled automatically; evaluate after training for scores.");
                case "position": return T(sim, "轮次之间自动启用，无需装备。支持的结构可使用句尾距离、句首词或两句对齐位置的线索，具体取决于结构；继续训练才能学会利用这些线索。", "Enabled automatically between epochs with no equipment step. Supported architectures use distance from the end, the opening word or aligned positions across two sentences, depending on the wiring; further training learns to use these clues.");
                case "warmup": return T(sim, "轮次之间自动启用。首次训练或更换数据、配置后的前", "Enabled automatically between epochs. On first training or after changing data or settings, the first ") + XgSim.WarmupCards + T(sim, "张卡逐步把学习率升至设定值。大幅进步导致的回落风险分量×0.5，可与裁剪的×0.7相乘；不是总风险减半。", " cards gradually raise the learning rate to its configured value. The recent-large-gain component of drop risk is ×0.5, multiplying with clipping's ×0.7; total risk does not necessarily halve.");
                case "fp16": return T(sim, "拥有后立即计入下列规模倍数，可提高预训练进度上限；实际进度仍需运行预训练推进，不直接加速或把训练显存减半。", "Ownership immediately adds the scale multiplier below and can raise the pre-training progress cap. Actual progress still needs pre-training to run; this does not directly speed it up or halve training VRAM.");
                case "gradclip": return T(sim, "自动启用后，每张卡对单个权重的拉动限制为±", "When automatically enabled, each card's pull on a weight is limited to ±") + N(XgBoard.ClipLimit) + T(sim, "。可承受的失稳步幅基础阈值×2，取消循环结构的额外折减；大幅进步造成的回落风险分量×0.7。与相应减险倍数相乘，仍可能失稳。", ". The base instability step threshold is ×2, removing extra loop penalties; the recent-large-gain drop-risk component is ×0.7. These multiply with corresponding risk-reduction factors; instability remains possible.");
                case "dropout": return T(sim, "购买后训练会按固定规则跳过约", "After purchase, training skips updates to about ") + N(XgBoard.DropoutRate * 100) + T(sim, "%的匹配线索；背答案造成的回落风险分量和训练—评估差距系数各×0.5，总风险与显示差距不保证减半。无需装备，成绩需训练后观察。", "% of matched clues under a fixed rule. The overfitting drop-risk component and training/evaluation-gap coefficient each become ×0.5; total risk and the displayed gap need not halve. No equipment step; observe scores after training.");
                case "augment": return T(sim, "购买后自动生效，无需装备。训练题池大小与有效样本数：手写数字 ×2，围棋 ×8，CIFAR、ImageNet、表情包 ×3，文字不变；倍率含原题，只应用一次。减少成绩上限中的数据不足项和过拟合因素，后续训练再评估成绩；不保证固定提分。已收集样本数与总数据进度不变。", "Active automatically on purchase; no equipment needed. Training-pool size and effective sample count: digits ×2, Go ×8, CIFAR, ImageNet and memes ×3; text is unchanged. Multipliers include originals and apply once. Reduces the data-shortage term in the score ceiling and overfitting factors; train and evaluate for results, with no fixed score gain. Collected samples and total data progress stay unchanged.");
                case "dataclean": return T(sim, "购买后，已下载且纳入训练的杂包、爬取数据及众包的有效错标×0.5；只计一次，再扣按条清除的错标，最低为0。自身自动标注错题和公共、剧情包不享受此倍率。样本总数不变，后续训练使用新噪声统计。", "Once purchased, effective wrong labels from downloaded, included junk/crawl packs and crowd data become ×0.5, once, before subtracting individually cleaned labels with a floor of zero. Your automatic mistakes and public/story packs receive no such multiplier. Sample totals stay unchanged; subsequent training uses the updated noise count.");
                case "shared.lr": return T(sim, "购买后两条线自动生效：评估上限公式中的可缩减误差项乘", "Automatically applies to both tracks on purchase: multiply the reducible error term in the score-ceiling formula by ") + N(XgCatalog.LrFinal[3]) + T(sim, "，有机会提高成绩上限；更强的同项效果优先，不相乘。需训练后评估。", " for a potentially higher score ceiling. A stronger effect on the same term takes priority; they do not multiply together. Train and evaluate to see results.");
                case "lrschedule": return T(sim, "评估上限公式中的误差项乘 ", "Multiply the error term in the score-ceiling formula by ") + N(XgCatalog.LrFinal[4]) + T(sim, "，让可达到的成绩上限更高；替代学习率搜索，与 AutoML 的同项效果不叠加。需训练后评估。", " for a higher reachable score ceiling; replaces learning-rate search and does not stack with AutoML's same effect. Train and evaluate to see results.");
                case "batchnorm": return Speed(sim, 1.2) + T(sim, "有限结构可承受层数+6，仍受已购上限和显存限制。回落风险合计先×0.8再封顶；自动启用后可承受的失稳步幅阈值×1.5。", " Finite architecture depth limits gain 6 layers, still within purchased caps and VRAM. Combined drop risk is ×0.8 before its cap; automatic normalization gives a ×1.5 instability step threshold.");
                case "cudnn": return Speed(sim, 1.3) + T(sim, "训练用电量×", " Training electricity use ×") + N(XgSim.CuDnnEnergyFactor) + T(sim, "，之后按阶梯电价计费；总账单不保证减少30%。", ", then billed at tiered rates; the total bill need not fall by 30%.");
                case "cooler": return T(sim, "购买后连续训练造成的烧卡风险×", "After purchase, the burn-out risk from prolonged continuous training is ×") + N(XgSim.CoolerWearFactor) + T(sim, "，无需装备。须电力系统开启、至少两张显卡且连续训练足够久才会触发；硅脂保护期间不会因训练烧卡。", ", with no equipment step. It only triggers with power enabled, at least two GPUs and a long enough training streak; thermal-paste protection prevents training burn-outs while active.");
                case "transfer": return T(sim, "购买后自动适用：换结构保留准确率高于随机猜测基线部分的60%；同结构改尺寸或加载不同尺寸检查点时，保留比例", "Automatically applies after purchase: architecture changes keep 60% of accuracy above the chance baseline; resizing within one architecture or loading a differently sized checkpoint changes retention from ") + N(XgSim.GrowKeep * 100) + "% → " + N(XgSim.GrowKeepTransfer * 100) + T(sim, "%。连续变更逐次乘算，仍需训练；基础概念迁移另受新结构是否支持与位置无关线索的限制。", "%. Successive changes compound and still need training; basic-concept transfer also depends on support for position-independent clues in the new wiring.");
                case "multihead": return T(sim, "随Transformer获得的标记，无额外收费或独立倍率。实际采用Transformer时，单句训练保留词本身的线索；启用位置编码还可学习句首词，效果须训练后观察。", "Granted with Transformer, with no extra charge or independent multiplier. When Transformer is selected, single-sentence training retains word clues; positional encoding also allows an opening-word clue. Observe results after training.");
                case "layernorm": case "residual": return T(sim, "随 Transformer 一同获得的技术标记，不另收费，也不再叠加独立购买加成。", "A technology marker granted with Transformer, with no separate charge or additional purchase bonus.");
                case "datacenter": return T(sim, "拥有后可用训练显存×", "Ownership gives usable training VRAM ×") + N(XgSim.DatacenterVram)
                    + T(sim, "，并提供IDC机架。预训练须本体在线且放在机架上；自动放置会迁入，手动放置需自行调整。运行且未到当前进度上限时另付¥", " and provides an IDC rack. Pre-training needs the AI online on that rack; automatic placement moves it, while manual placement requires adjustment. While running below its current cap, the additional fee is ¥") + XgSim.DatacenterRent
                    + T(sim, "/游戏秒；付不起就停止并保留进度，到上限停滞时不扣该费用。", "/game second; insufficient funds stop work and preserve progress. Stalling at the cap does not incur this fee.");
                case "secret.6": return T(sim, "购买即显示配置提示，无需装备或训练，不提供额外倍率。在跑通预训练前购买，将放弃自悟奖金¥", "Buying immediately shows setup hints, without equipment, training or an added multiplier. Buying before completing pre-training forfeits a self-discovery bonus of ¥") + N(sim.NodeCost(node) * XgSim.SelfInsightBonus) + T(sim, "（本节点价格×", " (this item's price ×") + N(XgSim.SelfInsightBonus) + T(sim, "）。", ").");
                case "label.raise": return Raise(sim);
                case "label.auto": return T(sim, "每级每个合格标注台的基础速度增加 ", "Each level adds a base ") + N(XgCatalog.AutoRatePerLevel) + T(sim, " 题/秒；该台检查点须达到 ", " cards/s per eligible desk; that desk needs a checkpoint of at least ") + N(XgCatalog.AutoMinAccuracy * 100) + T(sim, "% 准确率。实际速度还受算力、复核队列和暂停状态限制。", "% accuracy. Compute, review capacity and pauses still limit actual speed.");
                case "label.coop": return T(sim, "购买后下一次分流即生效。把握阈值可设50%–99%（初始80%）：低于阈值交你复核，否则仍须通过其他检查；把握不等于准确率。所有台共用", "Applies at the next routing decision after purchase. Set confidence to 50%–99% (initially 80%): lower-confidence cards go to review; others still face enabled checks. Confidence is not accuracy. All desks share ") + XgSim.ReviewCapacity + T(sim, "个复核名额，满时自动标注暂停。", " review slots; automation pauses when full.");
                case "label.brain": return T(sim, "第四阶段起可购买。服务就绪后，为已开放且检查点≥", "Available from stage 4. Once the service is ready, judges supported text cards at open desks with a checkpoint ≥") + N(XgCatalog.AutoMinAccuracy * 100)
                    + T(sim, "%的受支持文字台判断新待判题；已有判断不重算，仍按把握阈值分流。离线或无有效结果则回退检查点。每次派发计", "% accuracy; existing judgments stay unchanged and confidence routing still applies. Offline service or invalid results fall back to checkpoints. Each dispatched request accounts for ") + N(XgSim.BrainGpuSeconds)
                    + T(sim, "GPU秒用量，并非答题耗时；断电、研发或复核满时不派发。", " GPU-seconds of usage, not response time; power loss, research or a full review queue blocks dispatch.");
                case "label.hard": return T(sim, "新题人工复核答对时，报酬 ×1.5，样本 ×2；旧数据重审不享受此奖励。", "Correct human reviews of new cards pay ×1.5 and award ×2 samples; rechecking old data does not earn this bonus.");
                case "label.audit": return T(sim, "购买后，每道进入自动提交分支的错题有", "Once purchased, each wrong answer reaching automatic submission has a ") + N(XgSim.AuditProbability * 100)
                    + T(sim, "%概率退回复核，避免直接污染数据。另解锁手动清洗：每批最多", "% chance of review before contaminating data. Also unlocks manual cleaning: up to ") + XgSim.NoiseCleanBatchLimit
                    + T(sim, "条，每条¥", " labels per batch at ¥") + N(XgSim.NoiseCleanMoneyPerItem) + T(sim, "并计", " plus ") + N(XgSim.NoiseCleanGpuSecondsPerItem)
                    + T(sim, "GPU秒；先清自身错标，再清外来错标，受经费、算力和现有错标量限制。", " GPU-seconds per label; clean your own mistakes first, then outside noise, limited by funds, compute and remaining mistakes.");
                case "label.parallel": return T(sim, "购买后，自启本地服务在空闲重启完成时，同时判题上限1→2题；聊天或输入时暂停派发新判题。外部服务保持已有容量，不自动扩容；实际速度取决于本机，无需训练。", "After purchase, once the game-started service is idle and restarts, simultaneous card judgments increase from 1 to 2. Chatting or typing pauses new judgments. External services keep their existing capacity; speed depends on the computer, with no training step required.");
                case "label.trapmemory": return T(sim, "再遇到已记住的金标准题时，转入人工复核，不自动提交。", "Send remembered gold-standard items to human review instead of submitting automatically.");
                case "label.captcha": return T(sim, "第三阶段、手写数字检查点≥95%且拥有置信度分流后可购买。之后新出现的验证码在", "Available at stage 3 with a digits checkpoint ≥95% and confidence routing. New captchas after purchase pass automatically after ") + N(XgSim.AutofillMinDelay) + "–" + N(XgSim.AutofillMaxDelay)
                    + T(sim, "秒后自动通过，已弹出的不补代填。通过后信用+1、解除本次验证暂停；多次代填后仍高速提交可能额外扣信用。", " s; already pending captchas are not scheduled retroactively. Passing grants 1 credit and clears that verification pause; repeated autofills with rapid submissions can incur a credit penalty.");
                default: return null;
            }
        }

        static string Raise(XgSim sim)
        {
            int level = Math.Max(0, Math.Min(sim.RaiseLevel, XgCatalog.RaiseMax));
            string result = T(sim, "人工答题报酬 ×", "Hand-answer pay ×") + N(XgCatalog.RaiseMultipliers[level]);
            if (level < XgCatalog.RaiseMax) result += " → ×" + N(XgCatalog.RaiseMultipliers[level + 1]);
            return result + T(sim, "；购买后立即用于后续人工结算，与其他适用报酬倍数相乘。与标注台商店共用；按人工报酬计算的答错罚款也会提高，实扣不超过余额。自动标注报酬不变。", "; applies to subsequent hand-answer settlements, multiplying with other applicable pay bonuses. Shared with the labelling shop; wrong-answer fines based on hand pay also rise, capped at the balance. Automatic labelling pay is unchanged.");
        }
    }
}
