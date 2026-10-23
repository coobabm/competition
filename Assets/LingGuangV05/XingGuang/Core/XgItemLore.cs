using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The longer, popular-science explanation of each item (道具), shown when its card is opened. The card itself keeps
    /// the one-line note (XgArch.note, XgResearch.effect). Keyed by node id.
    /// </summary>
    public static class XgItemLore
    {
        public static string Get(string id, bool english)
        {
            return id != null && Lore.TryGetValue(id, out var t) ? (english ? t[1] : t[0]) : "";
        }

        static readonly Dictionary<string, string[]> Lore = new Dictionary<string, string[]>
        {
            // ───────────── 结构 ─────────────
            { "perceptron", new[] {
                "1958 年 Rosenblatt 做的。每个输入乘一个权重，加起来超过门槛就答「是」。\n\n它只能在数据里画一条直线。1969 年 Minsky 指出它学不会异或（两个条件只能有一个成立），神经网络因此冷了十几年。",
                "Built by Rosenblatt in 1958. Each input is multiplied by a weight; if the sum passes a threshold, it says yes.\n\nIt can only draw one straight line through the data. In 1969 Minsky showed it can't learn XOR, and neural networks went cold for over a decade." } },
            { "mlp", new[] {
                "在输入和输出之间加几层「隐藏层」。中间层先学出小组合（比如「至少一个成立」），上一层再拿组合做判断，异或就解开了。\n\n1986 年的反向传播让它真正练得动。缺点是每个位置各有一套连线，图里的东西挪一格，它就当没见过。",
                "A few hidden layers between input and output. The middle learns small combinations (\"at least one is true\"), the layer above judges those, and XOR is solved.\n\nBackpropagation (1986) made it trainable. But every position has its own wires, so shift a picture one cell and it's brand new." } },
            { "caption", new[] {
                "一个卷积网络看图，一个循环网络说话，中间用注意力连起来：说每个词时，回头看图里对应的那一块。\n\n2015 年的「Show, Attend and Tell」就是这么做的。",
                "A convolutional net looks, a recurrent net speaks, and attention joins them: for each word, it looks back at the matching part of the picture.\n\nThat's how 2015's \"Show, Attend and Tell\" worked." } },
            { "transformer", new[] {
                "不要循环，也不要卷积，只留注意力：每个字直接和句子里所有字比一比，看谁跟自己有关。\n\n不用一个字等一个字，整句同时算，显卡能吃满。它本身不知道字的先后，所以要配位置编号。后来几乎所有大模型都长这样。",
                "No recurrence, no convolution, only attention: every word compares itself with every other word to see what matters.\n\nNo waiting word by word, so the whole sentence runs at once and the GPU stays busy. It doesn't know word order by itself, so it needs position tags. Nearly every large model later looks like this." } },
            { "lenet", new[] {
                "杨立昆（Yann LeCun）1998 年的卷积网络。用一个 5×5 的小窗口扫过整张图，同一套权重到处用，再配上池化，数字稍微挪一挪位置也能认出来。\n\n美国的银行用它读支票上的手写数字，一读就是十几年。",
                "Yann LeCun's convolutional net from 1998. A small 5×5 window sweeps the picture with the same weights everywhere, and pooling on top lets a slightly shifted digit still be recognised.\n\nAmerican banks used it to read handwritten cheques for over a decade." } },
            { "alexnet", new[] {
                "2012 年 ImageNet 比赛，它把错误率一下砍掉十个百分点，深度学习从此出圈。\n\n秘诀：更深更宽、用 ReLU、用 Dropout 防背答案，再加两块 GTX 580 一起算。",
                "At ImageNet 2012 it cut the error rate by ten points in one go, and deep learning went mainstream.\n\nThe recipe: deeper and wider, ReLU, Dropout against memorising, and two GTX 580s working together." } },
            { "vgg", new[] {
                "牛津大学 2014 年的网络。思路很简单：全用 3×3 的小窗口，一层层叠到 16 层。两层 3×3 看到的范围和一层 5×5 一样大，参数却更少。\n\n缺点是总参数多，非常吃显存。",
                "Oxford, 2014. Simple idea: only 3×3 windows, stacked 16 layers deep. Two 3×3 layers see as much as one 5×5, with fewer parameters.\n\nThe catch: lots of parameters overall, and very hungry for memory." } },
            { "googlenet", new[] {
                "Google 2014 年的网络，名字致敬 LeNet。一层里并排放 1×1、3×3、5×5 几种窗口，让网络自己挑合适的尺度（Inception 模块）。\n\n比 VGG 还深，参数却少十几倍。",
                "Google, 2014, named in honour of LeNet. One layer holds 1×1, 3×3 and 5×5 windows side by side and lets the net pick the scale (the Inception module).\n\nDeeper than VGG with over ten times fewer parameters." } },
            { "resnet", new[] {
                "微软亚洲研究院 2015 年的网络。普通网络叠太深反而变差，ResNet 每隔几层开一条「近道」，信号可以直接跳过去。\n\n这样 152 层也练得动，拿了 2015 年 ImageNet 冠军和 2016 年 CVPR 最佳论文。",
                "Microsoft Research Asia, 2015. Plain nets get worse when stacked too deep; ResNet adds a shortcut every few layers so the signal can skip straight past.\n\n152 layers train fine. It won ImageNet 2015 and CVPR 2016's best paper." } },
            { "rnn", new[] {
                "读句子时，把上一个字的状态带到下一个字，一路传下去，像边听边记。\n\n问题是记忆每过一个字就淡一点，十来个字以前的就忘了；误差往回传时还容易越乘越大，直接练炸。",
                "While reading, it carries the state of the last word into the next, like remembering while you listen.\n\nBut memory fades a little with every word, so things a dozen words back are gone; and errors flowing back tend to blow up." } },
            { "lstm", new[] {
                "1997 年 Hochreiter 和 Schmidhuber 提出。给记忆加三道门：输入门管写什么，遗忘门管丢什么，输出门管说什么。\n\n重要的东西能一直留到句尾，长句也不忘。2016 年的输入法和语音识别里都有它。",
                "Hochreiter and Schmidhuber, 1997. Three gates on memory: one decides what to write, one what to forget, one what to say.\n\nWhat matters stays to the end of the sentence. In 2016 it's inside input methods and speech recognition." } },
            { "gru", new[] {
                "2014 年的 LSTM 简化版，把三道门合成两道。参数少一些，算得快一些，效果大多数时候差不多。",
                "A 2014 simplification of LSTM, with two gates instead of three. Fewer parameters, a little faster, and usually about as good." } },
            { "seq2seq", new[] {
                "2014 年 Google 提出。一个网络读完整句，压成一串数字；另一个网络从这串数字开始，一个词一个词往外说。\n\n神经网络第一次能做翻译。缺点是整句都挤在一串数字里，句子一长就丢东西。",
                "Google, 2014. One network reads the whole sentence and squeezes it into a list of numbers; another starts from those numbers and speaks it out word by word.\n\nNeural translation for the first time. But the whole sentence is crammed into one list, so long sentences lose things." } },
            { "attention", new[] {
                "2014–2015 年 Bahdanau 等人提出。翻译每个词时，不只看那串压缩过的数字，而是回头把原文每个词扫一遍，挑最相关的看。\n\n长句子不再丢细节。2016 年 9 月谷歌翻译换成神经网络，用的就是这一套。",
                "Bahdanau and others, 2014–2015. For each translated word it doesn't just use the squeezed numbers: it scans every source word and focuses on the relevant ones.\n\nLong sentences keep their details. Google Translate switched to this in September 2016." } },
            { "textcnn", new[] {
                "2014 年 Yoon Kim 把看图的卷积搬到文字上：用小窗口在句子上滑，一次看两三个词。\n\n整句可以一起算，非常快，判断好评差评很好用；但窗口外的字它看不见，抓不住隔得远的关系。",
                "Yoon Kim, 2014, moved the picture convolution onto text: a small window slides along the sentence, two or three words at a time.\n\nThe whole sentence runs at once, very fast, great for telling good reviews from bad; but it can't see beyond the window, so long-range links are lost." } },

            // ───────────── 技巧 ─────────────
            { "earlystop", new[] {
                "练久了，模型会开始背训练题，考新题反而变差。早停就是盯着考试分，几轮不涨就停，留下成绩最好的那一版。",
                "Train too long and the model starts memorising the training cards and does worse on new ones. Early stopping watches the test score, stops when it stalls, and keeps the best version." } },
            { "wordvec", new[] {
                "2013 年 Google 的 word2vec。把每个词变成几百个数字，意思相近的词，数字也接近，甚至能做加减：国王 − 男人 + 女人 ≈ 女王。\n\n有了它，模型不用从零认识每个词。",
                "Google's word2vec, 2013. Each word becomes a few hundred numbers; similar words get similar numbers, and you can even do sums: king − man + woman ≈ queen.\n\nThe model no longer has to learn every word from scratch." } },
            { "beamsearch", new[] {
                "逐词翻译时，每一步只挑当下最好的词，容易一步错、步步错。束搜索每一步留几条最有希望的半句话，最后挑整句最顺的那条。",
                "Picking only the best word at each step means one mistake ruins the rest. Beam search keeps a few promising half-sentences at each step and picks the smoothest whole sentence at the end." } },
            { "subword", new[] {
                "2016 年的办法。生僻词、新词拆成常见的小块，比如 unhappiness 拆成 un + happi + ness。词表不用太大，没见过的词也能拼出来。",
                "A 2016 idea. Rare and new words are split into common pieces, like un + happi + ness. The vocabulary stays small and unseen words can still be built." } },
            { "sft", new[] {
                "预训练只教会它往下续写。示范跟答再拿一批「问题 + 好回答」喂它，让它学会按要求办事。\n\n拿示范答案来训练是老办法；专门用它教模型听吩咐，要再过好几年才成套路。",
                "Pre-training only teaches it to continue text. Learning from demonstrations then feeds it question-and-good-answer pairs so it learns to do what it's asked.\n\nTraining on example answers is an old idea; using it to teach models to follow requests only became standard years later." } },
            { "cot", new[] {
                "让模型先把中间步骤写出来，再给答案，算术题和推理题会准很多。\n\n这个叫法 2022 年才出现。",
                "Have the model write out its steps before the answer, and arithmetic and reasoning get much more accurate.\n\nThe name only appears in 2022." } },
            { "bias", new[] {
                "每个神经元除了权重，再加一个固定的偏移量，相当于能调「多大才算是」的门槛。没有它，判断线只能穿过原点。",
                "Besides its weights, each neuron gets a constant offset: it can move the bar for \"how much counts as yes\". Without it, the decision line must pass through zero." } },
            { "weights", new[] {
                "1958 年 Rosenblatt 的感知机：每个输入乘一个数，这个数就是权重。权重越大，这个输入越重要。训练，就是在调这些数。",
                "Rosenblatt's 1958 perceptron: each input is multiplied by a number, its weight. The bigger the weight, the more that input matters. Training means adjusting these numbers." } },
            { "learnrule", new[] {
                "感知机最早的学法：答对了不动，答错了就把权重往正确答案那边挪一点。简单，但只对能用一条直线分开的问题管用。",
                "The perceptron's original rule: if right, change nothing; if wrong, nudge the weights toward the right answer. Simple, but it only works when one straight line can separate the answers." } },
            { "step", new[] {
                "最早的输出方式：加权和过了门槛就是 1，没过就是 0，像个开关。\n\n问题是开关没有坡度，不知道「差多少」，误差传不回前面的层。",
                "The earliest output: past the threshold it's 1, below it's 0, like a switch.\n\nA switch has no slope, so there's no \"how far off\", and errors can't flow back to earlier layers." } },
            { "sigmoid", new[] {
                "把开关换成一条平滑的 S 形曲线，输出在 0 到 1 之间慢慢变。这样就能算出每个权重该往哪边改、改多少，反向传播才用得上。\n\n缺点：曲线两头太平，层一多，误差传几层就没了。",
                "Replace the switch with a smooth S-curve that moves gradually from 0 to 1. Now each weight's direction and size of change can be worked out, and backpropagation works.\n\nThe ends are flat, though, so with many layers the error fades away after a few." } },
            { "backprop", new[] {
                "不是一张卡改一次，而是一次看几十张卡，算个平均方向再改。方向更准，显卡也能一次算一批，快得多。",
                "Instead of updating after every card, look at a few dozen, average the direction, then update. Steadier, and the GPU can process a whole batch at once, much faster." } },
            { "chainrule", new[] {
                "反向传播靠的是微积分里的链式法则：一层层往回乘。以前要自己推公式、写代码，2016 年的 Theano、TensorFlow 都能自动算。",
                "Backpropagation is the chain rule from calculus, multiplying back layer by layer. You used to derive and code it by hand; in 2016 Theano and TensorFlow do it for you." } },
            { "features", new[] {
                "深度学习之前的主流做法：人先想好哪些线索有用，整理成特征再喂给模型。比如把两个条件拼成「一真一假」，感知机也能做异或。\n\n费人工，换个任务就得重来。多层网络能自己学特征，就不用它了。",
                "The main approach before deep learning: people decide which clues matter and hand-build features. Pair two conditions into \"exactly one is true\" and even a perceptron does XOR.\n\nIt's slow work and must be redone for every task. Networks with layers learn features themselves." } },
            { "irnn", new[] {
                "2015 年 Le、Jaitly、Hinton 的办法。RNN 的回环权重一开始设成「原样转交」，再配 ReLU，记忆就不会一个字一个字地漏掉。\n\n副作用是误差也原样回传，学习率一大就炸，要配梯度裁剪。",
                "Le, Jaitly and Hinton, 2015. Start the RNN's loop weights as \"pass it on unchanged\" and use ReLU, and memory stops leaking word by word.\n\nThe error also comes back unchanged, so a high rate blows up. Pair it with gradient clipping." } },
            { "position", new[] {
                "注意力本身不管字的先后，「狗咬人」和「人咬狗」在它看来一样。给每个位置加一个编号信号，模型就知道谁在前、谁在后。",
                "Attention by itself ignores order: \"dog bites man\" and \"man bites dog\" look the same. Add a signal for each position and the model knows what comes first." } },
            { "warmup", new[] {
                "模型刚换时权重很乱，一上来就用大学习率容易练崩。预热就是前一段时间学习率从很小慢慢升上去，等稳了再全速。",
                "Right after a model changes, its weights are a mess, and a big rate straight away can wreck it. Warm-up starts the rate tiny and raises it slowly until things settle." } },
            { "fp16", new[] {
                "平时每个数用 32 位存，混合精度计算时改用 16 位，显存省一半、速度更快；再留一份 32 位的权重，精度不丢。2016 年新出的 Pascal 显卡开始支持。",
                "Numbers are usually 32-bit; mixed precision computes in 16-bit for half the memory and more speed, while keeping a 32-bit copy of the weights so nothing is lost. 2016's new Pascal cards support it." } },
            { "relu", new[] {
                "公式很简单：小于 0 输出 0，大于 0 原样输出。正的一半坡度是 1，误差往回传不会变小，深网络终于练得动。\n\n2010 年前后流行开，AlexNet 靠它一战成名。",
                "Dead simple: below zero, output zero; above zero, pass it through. The positive half has slope 1, so errors don't shrink on the way back and deep nets finally train.\n\nPopular from around 2010; AlexNet made its name with it." } },
            { "momentum", new[] {
                "像推一个球下坡：每一步不只看当前的方向，还带着上一步的惯性。遇到小坑能冲过去，平路上越走越快。",
                "Like a ball rolling downhill: each step carries momentum from the last. It rolls through small dips and speeds up on the flat." } },
            { "gradclip", new[] {
                "循环网络里误差会越乘越大，一步改太猛，模型直接炸成 NaN。梯度裁剪给每一步设个上限，超了就按比例缩回来。",
                "In recurrent nets the error can multiply up until one huge step turns the model into NaN. Gradient clipping caps each step and scales it back when it's too big." } },
            { "dropout", new[] {
                "Hinton 组 2012 年提出的办法（2014 年正式发表）。训练时每次随机关掉一部分神经元，逼模型不能只靠某几个巧合答题。\n\n就像考前不让你只背那几道题，得真懂才行。正式考试时全部打开。",
                "From Hinton's group, proposed in 2012 (formally published in 2014). During training, randomly switch off some neurons each time so the model can't lean on a few coincidences.\n\nLike not letting you cram a handful of questions before an exam. At test time, everything is on." } },
            { "augment", new[] {
                "数据不够，就把手上的数据变一变：图片平移、裁剪、改亮度；围棋棋盘转 90 度、照镜子还是同一个局面，一盘变八盘。\n\n别变坏了：数字 6 倒过来就成了 9。",
                "Not enough data? Vary what you have: shift, crop or brighten pictures; a Go board rotated or mirrored is the same position, so one game becomes eight.\n\nCareful: turn a 6 upside down and it's a 9." } },
            { "dataclean", new[] {
                "众包和便宜数据包里难免有标错的。清洗就是去重、找几个人对答案、把前后矛盾的扔掉。错标越少，模型越不会被带偏。",
                "Crowd work and cheap packs always have wrong labels. Cleaning means removing duplicates, cross-checking answers and dropping contradictions. Fewer bad labels, less misled model." } },
            { "rmsprop", new[] {
                "2012 年 Hinton 在网课里讲的办法。每个参数记住自己最近的梯度有多大，大的走小步，小的走大步，训练更稳。",
                "Hinton described it in a 2012 online course. Each parameter remembers how big its recent gradients were: big ones take small steps, small ones take big steps." } },
            { "lrschedule", new[] {
                "刚开始离答案远，学习率大一点走得快；快到了就调小，慢慢找最低点。就像倒车入库，先大把方向，再一点点修。",
                "Far from the answer, a big rate moves fast; close to it, turn it down and creep in. Like parking: big turns first, then small corrections." } },
            { "batchnorm", new[] {
                "2015 年 Google 提出。每一层的输出先拉回到差不多的范围再往下传，后面的层就不用一直追着前面的变化跑。\n\n可以用更大的学习率，层数也能更深。",
                "Google, 2015. Each layer's output is brought back to a similar range before passing on, so later layers don't keep chasing changes in earlier ones.\n\nBigger learning rates and deeper nets become possible." } },
            { "adam", new[] {
                "2014 年提出。把动量（带惯性）和 RMSProp（按参数调步子）合在一起，几乎不用调参就能用，是 2016 年最常用的优化器。",
                "Proposed in 2014. Combines momentum and RMSProp, works with almost no tuning, and is the most popular optimiser of 2016." } },
            { "cudnn", new[] {
                "NVIDIA 2014 年发布的深度学习加速库，把卷积、循环这些常用运算写成显卡上最快的版本。TensorFlow、Caffe 都靠它跑。",
                "NVIDIA's deep-learning library from 2014: convolutions, loops and other common operations written as fast as the GPU allows. TensorFlow and Caffe both run on it." } },
            { "cooler", new[] {
                "机箱前进风、后出风，配合显卡自己的散热器，把热风排出机箱，是 2016 年装机的讲究。显卡一直满载会很烫：温度高了先降频变慢，长期高温还会加快老化。风道好，高温损耗就少。",
                "Front-in, back-out case airflow working with the card's own cooler to push hot air out of the case, a 2016 build staple. Cards at full load run hot: first they throttle, and long-term heat ages them faster. Better airflow, less heat wear." } },
            { "transfer", new[] {
                "底层学到的东西大多通用：笔画、边角、常用词。换结构时把这些带过去接着用，只重新学上层的组合，比从头练快得多。\n\n2016 年大家常拿 ImageNet 上练好的网络当起点。",
                "What the bottom layers learn is mostly general: strokes, corners, common words. Carry them into the new structure and only relearn the combinations on top, far faster than starting over.\n\nIn 2016 people often start from a net pre-trained on ImageNet." } },

        };
    }
}
