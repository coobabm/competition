using System;

namespace LingGuangV05.XingGuang
{
    // Design glossary: 91 entries, including 24 hardware entries. Pure presentation data; no save state.
    public static class XgKnowledge
    {
        public sealed class Entry
        {
            public readonly string Key, English, ChineseBody, EnglishBody;
            public readonly int Category;
            public readonly string[] Related;
            public Entry(string key, string english, int category, string zh, string en, string[] related)
            { Key = key; English = english; Category = category; ChineseBody = zh; EnglishBody = en; Related = related; }
            public string Name(bool english) => english ? English : Key;
            public string Body(bool english) => english ? EnglishBody : ChineseBody;
            public bool Matches(string query) => string.IsNullOrWhiteSpace(query) || (Key + " " + English + " " + ChineseBody + " " + EnglishBody).IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public static readonly Entry[] Entries = {
            new Entry("参数", "Parameters", 0, "模型里可以训练的数，包括权重和偏置。参数更多通常增加容量和显存需求，但不保证泛化更好。", "Trainable numbers, including weights and biases. More parameters increase capacity and memory use, but do not guarantee better generalisation.", new[] { "权重", "偏置" }),
            new Entry("权重", "Weights", 0, "模型里可调的数值，训练就是不断改这些数。", "Trainable coefficients that control how strongly signals contribute to a result.", new[] { "参数" }),
            new Entry("偏置", "Bias", 0, "给神经元加的一个常数，让判断门槛能整体平移。", "An added constant that shifts a neuron's decision threshold.", new[] { "参数" }),
            new Entry("阶跃开关", "Step activation", 0, "最早的开关：过线输出 1，不过线输出 0；不能求导。", "Outputs zero or one across a threshold; its discontinuity prevents ordinary gradient training.", new[] { "参数" }),
            new Entry("平滑开关", "Sigmoid activation", 0, "把硬跳变换成 S 形曲线，小误差也能传回去。", "A smooth S-shaped function that allows derivatives, but can saturate and weaken gradients.", new[] { "参数" }),
            new Entry("整流激活", "ReLU", 0, "小于 0 的输出归零，大于 0 原样通过；深层网络因此训得动。", "Returns zero for negative inputs and the input otherwise. It often helps deep networks train.", new[] { "参数" }),
            new Entry("感知机", "Perceptron", 0, "1958 年的一层线性分类器，用一条直线分开两类。", "A linear classifier introduced in 1958. It separates linearly separable classes.", new[] { "参数" }),
            new Entry("感知学习", "Perceptron learning", 0, "答错时调整权重；经典感知机算法的收敛保证要求训练数据线性可分。", "Corrects weights after a wrong answer; convergence requires linearly separable training data.", new[] { "参数" }),
            new Entry("多层感知机", "Multilayer perceptron", 0, "把多层感知机叠起来，能拟合非线性的关系。", "Layers of neurons and nonlinear activations can represent nonlinear relationships.", new[] { "感知机" }),
            new Entry("结构", "Architecture", 0, "信息在模型里怎么流动：一层层往下传，还是回头再看前文。", "The connections and operations that determine how information flows through a model.", new[] { "参数" }),
            new Entry("特征", "Features", 0, "模型自己总结出的「看什么」：底层是笔画，高层是偏旁和数字。", "Useful patterns in data, such as edges, strokes or combinations of shapes.", new[] { "参数" }),
            new Entry("组合线索", "Feature engineering", 0, "人先把几个线索拼成一个特征，再喂给模型。", "People combine or transform input clues before giving them to a model.", new[] { "参数" }),
            new Entry("梯度", "Gradient", 0, "损失对每个参数的偏导，告诉参数往哪调、调多少。\n\n反向传播从损失往回算，每层乘上局部导数；层数越多，连乘的次数越多。", "Derivatives of the loss with respect to parameters; they guide a local update.", new[] { "梯度消失", "梯度裁剪" }),
            new Entry("梯度消失", "Vanishing gradients", 0, "层数一多，反向传播时梯度越乘越小，前面的层几乎不更新。", "Backpropagated gradients can become very small, slowing updates in early layers.", new[] { "参数" }),
            new Entry("梯度裁剪", "Gradient clipping", 0, "梯度太大时按比例缩回去，防止一步走飞。", "Limits large gradients to reduce unstable updates; it does not fix every cause of divergence.", new[] { "参数" }),
            new Entry("学习率", "Learning rate", 0, "每次更新走多大一步。\n\n步子太大越过最优点来回震荡，步子太小半天走不到；所以要么自动搜，要么按计划变小。", "Controls the size of parameter updates. Too large can destabilise training; too small can slow it.", new[] { "学习率衰减", "学习率预热" }),
            new Entry("学习率衰减", "Learning-rate decay", 0, "训练后期把步长调小，收敛更稳。", "Reduces the step size later in training to help refine a solution.", new[] { "参数" }),
            new Entry("学习率预热", "Learning-rate warmup", 0, "开头用小步长，稳定后再放大，避免一上来就发散。", "Starts with smaller updates and gradually raises the rate to reduce early instability.", new[] { "参数" }),
            new Entry("损失函数", "Loss function", 0, "衡量「答得有多错」的分数，训练就是把它压低。", "A numerical measure of prediction error that training tries to reduce.", new[] { "参数" }),
            new Entry("反向传播", "Backpropagation", 0, "从损失出发，逐层往回算每个参数该负多少责任。", "Applies the chain rule backwards through a network to calculate gradients.", new[] { "参数" }),
            new Entry("自动求导", "Automatic differentiation", 0, "框架自动算梯度，不用手推公式。", "Computes derivatives from the operations performed by a program.", new[] { "参数" }),
            new Entry("小批量", "Mini-batch", 0, "每次拿一小批样本算梯度：比全量快，比单样本稳。", "A small set of examples used for one update; size affects memory, throughput and gradient noise.", new[] { "参数" }),
            new Entry("动量下降", "Momentum", 0, "沿历史更新方向累积，减少来回震荡。", "Accumulates previous update directions to help reduce oscillation.", new[] { "参数" }),
            new Entry("自适应步长", "Adaptive learning rate", 0, "按每个参数梯度的大小，自动调它的步长。", "Scales updates per parameter using gradient statistics.", new[] { "参数" }),
            new Entry("自适应动量", "Adam", 0, "Adam 记录梯度的一阶、二阶矩，为各参数缩放更新步长。效果取决于任务与调参，不保证总比固定学习率快或稳。", "Tracks first and second gradient moments to scale each parameter's update. It is useful, but not always faster or better than other optimisers.", new[] { "参数", "梯度" }),
            new Entry("早停研究", "Early stopping", 0, "新题成绩不再提升就停，防止把练习题背下来。", "Stops when held-out performance stops improving, to reduce overfitting.", new[] { "参数" }),
            new Entry("单位初始化", "Identity initialisation", 0, "把循环网络的初始权重设成单位矩阵，让早期记忆不被冲掉。", "Initialises recurrent weights with an identity matrix in certain architectures to help preserve early signals.", new[] { "参数" }),
            new Entry("过拟合", "Overfitting", 0, "练习题背下来了，新题不会。", "Fitting training examples closely while performing poorly on unseen examples.", new[] { "参数" }),
            new Entry("随机失活", "Dropout", 0, "训练时随机关掉一部分神经元，逼模型别只靠某几个特征。", "Randomly removes activations during training to discourage reliance on individual features.", new[] { "参数" }),
            new Entry("归一化", "Normalisation", 0, "把每层的输出拉到相近的范围。\n\n数值范围稳定后，梯度不会忽大忽小，层数才能叠深。", "Rescales values using selected statistics; its exact behaviour depends on the method.", new[] { "批归一化", "层归一化" }),
            new Entry("批归一化", "Batch normalisation", 0, "按一批样本统计均值和方差再归一化，训练更快更稳。", "Uses batch statistics during training to normalise activations, often improving optimisation.", new[] { "参数" }),
            new Entry("层归一化", "Layer normalisation", 0, "按单个样本在层内归一化，适合长度不一的句子。", "Normalises features within one example; introduced in 2016 and useful for recurrent models.", new[] { "参数" }),
            new Entry("残差", "Residual connection", 0, "让信息跨层直连。\n\n输出 = 这一层的结果 + 输入，梯度可以走捷径回到前面的层。", "Adds an input to a learned transformation, giving signals and gradients a shortcut.", new[] { "残差直连" }),
            new Entry("残差直连", "Residual shortcut", 0, "输出 = 这一层的结果 + 输入，深层也能训得动。", "A direct connection across layers; matching dimensions may require a projection.", new[] { "参数" }),
            new Entry("精度", "Numerical precision", 0, "数字的表示方式和位数。低精度可以节省存储，但速度和数值稳定性取决于硬件与实现。", "The representation and number of bits used for numbers. Lower precision trades range or accuracy for storage and possible speed.", new[] { "半精度" }),
            new Entry("半精度", "Half precision", 0, "16位浮点能节省张量存储；是否提速取决于硬件，稳定训练可能仍需高精度累加。", "16-bit floating point reduces tensor storage; speed depends on hardware. Stable training may need higher-precision accumulation.", new[] { "参数" }),
            new Entry("迁移学习", "Transfer learning", 0, "用已经练出来的成果初始化新结构，保留部分成绩。", "Reuses learned weights for a new task or model where compatible, often reducing required training data.", new[] { "参数" }),
            new Entry("预训练参数", "Pretraining configuration", 0, "预训练时的层数、宽度、数据量等配置。", "In this game: layer count, width and data settings. These settings differ from the learned numerical weights.", new[] { "参数" }),
            new Entry("检查点", "Checkpoint", 0, "某一轮训练结束时的模型快照，可以回退或对比。", "A saved model state used to resume, compare or roll back training.", new[] { "参数" }),
            new Entry("留出新题", "Held-out data", 0, "留一部分题不参与训练，专门用来测真实水平。", "Examples excluded from training and used to estimate performance on unseen data.", new[] { "参数" }),
            new Entry("训练曲线", "Training curve", 0, "训练成绩随轮次变化的曲线，用来看有没有练歪。", "A plot of training or validation metrics over time that helps reveal instability or overfitting.", new[] { "参数" }),
            new Entry("网络层数", "Layer count", 0, "从输入到输出要经过几层计算；层多能学更复杂的规律，也更容易训坏。", "The number of computational layers; greater depth adds capacity but can make optimisation harder.", new[] { "网络深度" }),
            new Entry("网络深度", "Network depth", 0, "卡片里具体能叠到几层；每多一层，表达能力强一点，显存也多一截。", "The game's layer upgrade. More layers consume resources and do not guarantee higher accuracy.", new[] { "参数" }),
            new Entry("网络宽度", "Network width", 0, "每层有多少个通道；宽能装更多知识，也更吃显存。", "The number of units or channels per layer; wider networks usually need more parameters and memory.", new[] { "参数" }),
            new Entry("卷积", "Convolution", 1, "同一小窗口在输入上滑动，提取局部特征。共享权重有助于处理位移，但不保证对所有平移都不变。", "Slides a shared filter over local regions. Weight sharing is useful for images but does not guarantee invariance to all shifts.", new[] { "手写卷积", "深层卷积", "小窗卷积", "多路卷积", "文字卷积" }),
            new Entry("手写卷积", "Handwriting convolution", 1, "认笔画的卷积：浅层认横竖撇捺，深层拼成数字。", "A convolutional model for learning strokes and combining them into handwritten digits.", new[] { "卷积" }),
            new Entry("深层卷积", "Deep convolution", 1, "把卷积层叠深，认更复杂的图形组合。", "Stacks convolutional layers to build increasingly complex visual features.", new[] { "卷积" }),
            new Entry("小窗卷积", "Small-filter convolution", 1, "只用很小的窗口，一层层叠起来，参数更省。", "Stacks small filters to expand the receptive field with efficient parameter use in many designs.", new[] { "卷积" }),
            new Entry("多路卷积", "Multi-branch convolution", 1, "几种不同大小的窗口同时看，再拼起来。", "Runs different filter paths in parallel and combines their outputs.", new[] { "卷积" }),
            new Entry("文字卷积", "Text convolution", 1, "把一句话当成一维序列，用窗口抓词组。", "Slides filters over a token sequence to detect local patterns such as phrases.", new[] { "卷积" }),
            new Entry("循环网络", "Recurrent network", 1, "按顺序读，边读边把前文记在一个状态里。", "Processes a sequence while carrying a hidden state from earlier steps.", new[] { "卷积" }),
            new Entry("门控记忆", "LSTM memory", 1, "带三个门的循环网络：记什么、忘什么、现在输出什么。", "Uses input, forget and output gates to control a recurrent memory cell.", new[] { "卷积" }),
            new Entry("简化记忆", "GRU memory", 1, "两个门的版本，参数更少、训练更快。", "Uses update and reset gates; often has fewer parameters than a comparable LSTM.", new[] { "卷积" }),
            new Entry("注意力", "Attention", 1, "处理当前词时，给句子里其他词分配不同的权重。", "Weights relevant parts of an input when computing a result; attention predates 2016.", new[] { "多路注意", "回看注意力" }),
            new Entry("回看注意力", "Encoder-decoder attention", 1, "生成每个词时，回头看一眼输入句的相关部分。", "Looks back at relevant input positions while generating each output token.", new[] { "卷积" }),
            new Entry("多路注意", "Multi-head attention", 1, "多组注意力分别学习不同关系。常见的Transformer多头注意力发表于2017年；在本作2016背景下属于推演中的突破。", "Several attention heads model different relationships. The familiar Transformer form dates to 2017; here it is a speculative breakthrough beyond the 2016 setting.", new[] { "卷积" }),
            new Entry("位置标记", "Position encoding", 1, "把词序信息提供给模型。常见的Transformer位置编码应用属于2017年，不是2016年已有的成熟方案。", "Represents sequence positions. Its familiar Transformer use belongs to 2017, beyond this game's starting year.", new[] { "卷积" }),
            new Entry("自研新脑", "New brain research", 1, "本作推演的纯注意力结构；现实中的Transformer发表于2017年，应区分游戏突破与2016年的技术现状。", "The game's speculative attention-only architecture. The historical Transformer was published in 2017, not available as an established 2016 design.", new[] { "卷积" }),
            new Entry("词向量", "Word embeddings", 1, "把词变成一串数字，意思相近的词距离也近。", "Represent words as numerical vectors learned from usage; proximity can reflect related meanings.", new[] { "卷积" }),
            new Entry("束搜索", "Beam search", 1, "生成时同时保留前几个候选，不只看当前最优。", "Keeps several promising output sequences at each step; it is approximate and can miss the best sequence.", new[] { "卷积" }),
            new Entry("子词切分", "Subword tokenisation", 1, "把词拆成更小的片段，生僻词也能表示。", "Splits words into reusable smaller pieces, helping represent uncommon words.", new[] { "卷积" }),
            new Entry("示范跟答", "Learning from demonstrations", 1, "用示范答案教它怎么答，而不只是判对错。", "Trains on example input-output pairs. This general technique is older than modern instruction-tuned assistants.", new[] { "卷积" }),
            new Entry("分步解题", "Step-by-step solving", 1, "本作让模型先写步骤再作答的训练设想；现代大模型的思维链提示在2016年之后才广泛流行。", "The game's guided reasoning exercise. Modern large-model chain-of-thought prompting became prominent after 2016.", new[] { "卷积" }),
            new Entry("数据增强", "Data augmentation", 2, "把一张图平移、旋转、裁剪，当成多张新题。", "Transforms examples, such as rotating or cropping images, when the correct label remains valid.", new[] { "数据清洗" }),
            new Entry("数据清洗", "Data cleaning", 2, "找出标错的样本并修正，减少噪声。", "Finds and corrects problematic examples or labels to improve data quality.", new[] { "数据增强" }),
            new Entry("准确率", "Accuracy", 2, "答对的题占全部题的比例。", "The fraction of evaluated examples answered correctly; class balance and test design affect its usefulness.", new[] { "数据增强" }),
            new Entry("连续性", "Continuity", 2, "后台不因复核箱满、验证码而停下的程度。", "A game automation measure: how long background work proceeds without review or verification interruptions.", new[] { "数据增强" }),
            new Entry("显卡", "Graphics card", 3, "适合并行矩阵运算的硬件。相对CPU的提速取决于模型、数据、显存和软件，不能统一说快几十倍。", "Hardware suited to parallel matrix operations. Training speedups over a CPU depend on workload, memory and software.", new[] { "算力", "显存", "显卡加速" }),
            new Entry("GPU", "GPU", 3, "显卡的计算单元，专门做大规模并行运算；本作里「显卡」与 GPU 同义。", "A processor designed for highly parallel work; this game uses graphics card and GPU interchangeably.", new[] { "显卡" }),
            new Entry("显卡加速", "GPU acceleration", 3, "用 cuDNN 这类库把卷积、矩阵乘调到最快。", "Libraries such as cuDNN optimise supported GPU operations. Actual gains depend on the model and hardware.", new[] { "显卡" }),
            new Entry("CPU", "CPU", 3, "通用处理器，适合小模型推理和日常逻辑。", "A general-purpose processor that handles application logic, data preparation and many inference workloads.", new[] { "显卡" }),
            new Entry("塔式散热", "Tower cooler", 3, "塔式风冷帮助带走热量，减少因高温降频；不能保证硬件永不损坏。", "An air cooler that helps remove heat. Good cooling reduces thermal throttling but does not guarantee hardware reliability.", new[] { "降频" }),
            new Entry("算力", "Compute", 3, "单位时间能完成多少运算。峰值算力翻倍不保证训练速度翻倍：读数据、显存带宽和多卡通信都可能成为瓶颈。", "The rate of computation. Doubling peak compute does not guarantee twice the training speed: data loading, memory bandwidth and communication may dominate.", new[] { "浮点运算", "显存", "多卡并行" }),
            new Entry("浮点运算", "Floating-point operations", 3, "对浮点数进行乘加等运算，每秒运算次数用FLOPS表示。2016年GTX 1080加速频率下FP32峰值约9 TFLOPS；P100支持更高的FP16吞吐。峰值规格不等于真实训练速度。", "Arithmetic on floating-point numbers. FLOPS measures operations per second. The 2016 GTX 1080 offered roughly 9 TFLOPS FP32 at boost; P100 supported higher FP16 throughput. Peak specifications are not measured training speed.", new[] { "精度", "半精度" }),
            new Entry("显存", "VRAM", 3, "显卡自带的工作内存，用于参数、中间结果、梯度等。容量限制模型与批量；半精度能减少部分存储，但不一定让训练总显存减半。", "GPU memory stores parameters, activations, gradients and other working data. Capacity limits model and batch size; half precision does not necessarily halve total training memory.", new[] { "参数", "小批量", "显存带宽" }),
            new Entry("显存带宽", "Memory bandwidth", 3, "显存和计算单元之间每秒能搬多少数据。\n\n很多时候卡不是算不动，而是数据搬不过来；带宽越大，计算单元越少空等。", "How much data can move between GPU memory and compute units each second. Memory-bound workloads can spend time waiting for transfers.", new[] { "显存" }),
            new Entry("多卡并行", "Multi-GPU training", 3, "多张卡分工计算再合并结果。通信与同步常使提速低于卡数比例，不能保证加卡就线性加速。", "Divides work across GPUs and combines results. Synchronisation and communication often make speedup sublinear; hardware alone does not guarantee scaling.", new[] { "数据并行", "卡间互联" }),
            new Entry("数据并行", "Data parallelism", 3, "每张卡放一份模型，各算一批数据，再把梯度合起来。", "Each GPU holds a model copy and processes different examples, then gradients are combined for an update.", new[] { "小批量", "梯度" }),
            new Entry("卡间互联", "GPU interconnect", 3, "显卡之间传数据的通道。2016年常见PCIe，部分P100服务器系统开始支持NVLink高速互联。", "A path for transfers between GPUs. PCIe was common in 2016; selected P100 systems also supported NVLink.", new[] { "PCIe" }),
            new Entry("PCIe", "PCIe", 3, "主板上插显卡的插槽和总线，显卡和电脑其他部分靠它通信。", "An expansion bus connecting GPUs and other devices. Lane count, generation and topology affect transfer speed.", new[] { "主板插槽" }),
            new Entry("主板插槽", "Motherboard slots", 3, "显卡数量不只受插槽数限制，还取决于PCIe通道、空间、电源、散热与软件支持。", "Physical slots are only one limit on GPU count; lanes, spacing, power, cooling and software support also matter.", new[] { "PCIe", "电源功率" }),
            new Entry("电源功率", "Power-supply rating", 3, "电源在规定条件下可提供的额定功率。超载可能触发保护或不稳定，并非必然让家用电路跳闸。", "The power a PSU is rated to deliver under specified conditions. Overloading can trigger protection or instability; it does not always trip a household breaker.", new[] { "功耗", "电费" }),
            new Entry("功耗", "Power draw", 3, "单位时间消耗的电能，单位瓦。2016年不少高端显卡额定功率约180至250瓦；整机还需计入CPU等功耗，实际值随负载变化。", "Energy consumed per second, measured in watts. Many high-end 2016 cards had rated power around 180-250 W; total system draw is higher and varies with load.", new[] { "电费", "降频" }),
            new Entry("电费", "Electricity cost", 3, "平均功率（千瓦）乘时间（小时）得到用电量（度），再乘电价。整机与散热也消耗电能。", "Energy in kWh equals average power in kW times hours. Multiply by the applicable tariff; cooling and other hardware also consume energy.", new[] { "功耗" }),
            new Entry("降频", "Thermal throttling", 3, "达到温度等限制时，硬件会降低频率保护自身。散热有助于维持速度，但电源或功率限制仍可能降频。", "Hardware reduces clocks when thermal or other limits are reached. Cooling can help sustain speed, but power limits can still apply.", new[] { "功耗", "算力" }),
            new Entry("机房", "Server room", 3, "集中放置服务器，提供机位、供电、散热与网络的场所。收费方式和可用显卡因服务商而异。", "A facility that supplies space, power, cooling and networking for servers. Pricing and GPU availability vary by provider.", new[] { "机柜", "多卡并行" }),
            new Entry("机柜", "Rack", 3, "机房里装服务器的铁架子，一个机柜能放好几台带卡的服务器。", "A standard frame that holds servers and related equipment; space, power and cooling constrain capacity.", new[] { "机房" }),
            new Entry("内存", "RAM", 3, "电脑的工作区，读进来的数据先放这里，再送进显存。", "The computer's working memory for data and programs, distinct from GPU memory.", new[] { "显存", "硬盘" }),
            new Entry("硬盘", "Storage drive", 3, "长期存数据集和检查点的地方；装不下就下载不了新数据包。", "Persistent storage for datasets and checkpoints. Capacity and read speed can both constrain a training workflow.", new[] { "内存" }),
            new Entry("CUDA", "CUDA", 3, "英伟达的显卡编程平台，深度学习框架靠它把运算交给显卡。", "NVIDIA's GPU computing platform, used by compatible deep-learning software to run supported computations on NVIDIA GPUs.", new[] { "显卡加速" }),
            new Entry("专用芯片", "Special-purpose chip", 3, "为特定任务设计的芯片，在适合的任务上可能更高效。谷歌2016年公开的第一代TPU主要用于内部推理，并非当时公开出售的通用训练卡。", "Hardware designed for a narrower workload can improve efficiency for that workload. Google disclosed its first TPU in 2016 for internal inference, not general public training-card sales.", new[] { "算力" }),
        };
        public static Entry Find(string key) => Array.Find(Entries, e => e.Key == key);
    }
}
