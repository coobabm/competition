using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The one dictionary of interface words (名词简化与训练页减负策划 §1). The screen shows only the plain word; its
    /// hover card says, in one or two natural sentences, what it is, naming the trade term inside the sentence. Terms
    /// are written between 【】 and drawn in amber bold by <see cref="Mark"/>.
    /// </summary>
    public static class XgGlossary
    {
        /// <summary>Amber for the trade term inside a hover card (the card is dark blue-grey).</summary>
        public const string TermColor = "#FFC46B";
        /// <summary>Grey-blue for the how-to line under the explanation.</summary>
        public const string NoteColor = "#9FB0C8";

        sealed class Entry { public string zh, en, bodyZh, bodyEn; }

        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        static void Add(string zh, string en, string bodyZh, string bodyEn)
        {
            entries[zh] = new Entry { zh = zh, en = en, bodyZh = bodyZh, bodyEn = bodyEn };
        }

        static XgGlossary()
        {
            // 1.2 training
            Add("题", "Question", "教它用的每一道题，行内叫一条【样本】；它没有课本，这些题就是它的全部课本。",
                "Every question you teach it with is one 【sample】. It has no textbook: these questions are all the textbook it gets.");
            Add("题库", "Question bank", "同一类题攒成一本练习册，就是一个【数据集】：垃圾短信一本，唐诗一本，看图一本。",
                "Questions of one kind make a workbook, a 【dataset】: one for spam texts, one for Tang poems, one for pictures.");
            Add("教", "Teach", "你把每道题的正确答案告诉它，这一步叫【标注】，就像老师在作业本上打勾打叉。",
                "You tell it the right answer to each question. That step is 【labelling】, a teacher ticking and crossing a workbook.");
            Add("练", "Practise", "让它对着题库自己琢磨一遍，这一遍叫一轮【训练】（行内说一个【Epoch】）；没人在旁边，它自己翻答案、自己改。",
                "It works through the bank on its own, checking the answers and fixing itself. One pass is a round of 【training】, an 【epoch】.");
            Add("考", "Test", "拿它从没见过的题考一次，这叫【评估】；练习册做得再熟，期中考试才看得出真本事。",
                "A test on questions it has never seen is an 【evaluation】: knowing the workbook by heart proves nothing until the midterm.");
            Add("分", "Score", "考试答对的比例，行内叫【验证准确率】，这里折成 0 到 1000 分；只拿新题来算，背熟的旧题一分不加。",
                "The share of test questions it gets right, its 【validation accuracy】, shown as 0 to 1000. Only new questions count; memorised ones add nothing.");
            Add("等级", "Grade", "分数换成 D、C、B、A、S 五档【评级】，跟成绩单上的优良中差一个意思。",
                "The score turned into a 【grade】, D to S, like the letters on a report card.");
            Add("存档", "Save", "考得最好的那一版脑子会自动存起来，行内叫【检查点】，和游戏存档一样，随时能退回来。",
                "The best-scoring version of the brain is kept automatically, a 【checkpoint】: a save you can always go back to.");
            Add("回档", "Load save", "练坏了，就退回到最好的那次存档，这一下叫【回滚】，跟读档重来一模一样。",
                "When practice goes wrong, it goes back to the best save. That is a 【rollback】, exactly like loading a game.");
            Add("退步", "Drop", "有时候越复习越糊涂：这一轮练完，分数反而掉了，这就是【退步】，学会的能力不会丢。",
                "Sometimes more revision means more confusion: the score falls after a round. That is a 【drop】; learned abilities are never lost.");
            Add("天花板", "Ceiling", "这套脑子再怎么练也考不过的那条线叫【上限】；小学生熬夜刷题也考不过高考卷，得换更大的脑子。",
                "The line this brain cannot pass however hard it practises is its 【ceiling】. A ten-year-old cramming all night still fails the university exam; it needs a bigger brain.");
            Add("死记硬背", "Rote learning", "练习题全对、新题全错，行内叫【过拟合】：它记住的是答案本身，没记住为什么。",
                "Every practice question right, every new one wrong: 【overfitting】. It remembered the answers, not why.");
            Add("举一反三", "Generalising", "没见过的题也能答对，这叫【泛化】，是真懂了的学生才有的本事。",
                "Getting questions right that it has never seen is 【generalisation】, what only a student who truly understands can do.");
            Add("错题", "Wrong answers", "练习册印错了答案，它照着学就会学歪；这种标错的题叫【噪声】，也叫【脏标注】。",
                "A workbook with misprinted answers teaches it wrong. Those mislabelled questions are 【noise】, or 【dirty labels】.");
            Add("挑错", "Clean up", "把印错答案的题找出来扔掉，这一步叫【清洗】，相当于给练习册出一张勘误表。",
                "Finding the misprinted questions and throwing them out is 【cleaning】, an errata sheet for the workbook.");
            Add("一题多变", "Variations", "把一道题换个角度、换个数字，变成好几道新题，这叫【数据增强】；题没多买，练得却更多。",
                "Turning one question into several by changing the angle or the numbers is 【data augmentation】: no new questions bought, more practice done.");
            Add("练崩了", "Blown up", "一次改得太猛，脑子里的数全乱成一锅粥，屏幕上显示成【NaN】，行内说训练【发散】了，只能回档。",
                "One change too big and every number in the brain turns to mush, shown as 【NaN】: the training 【diverged】. Only a rollback helps.");
            // 1.3 the brain
            Add("脑子", "Brain", "它用来想事情的那整套东西，行内叫【模型】；你练的、存的、换的，都是这一颗脑子。",
                "The whole thing it thinks with is the 【model】. Everything you train, save and swap is this one brain.");
            Add("脑容量", "Brain size", "脑子里能调的小旋钮一共有多少个，叫【参数量】；旋钮越多，能记住的规律越多，就像脑细胞越多越聪明。",
                "How many little knobs the brain has to turn is its 【parameter count】. More knobs hold more patterns, like more brain cells.");
            Add("记住的东西", "Things it knows", "它从一堆题里自己总结出的一条规律，在棋盘上叫一个【概念】，相当于学生脑子里的一个知识点。",
                "A pattern it worked out from many questions is a 【concept】 on the board, one point of knowledge in a student's head.");
            Add("层数", "Layers", "想一件事要绕几道弯，叫【深度】，每一道弯是一【层】；层越多，越能想清楚复杂的事。",
                "How many turns a thought takes is the 【depth】; each turn is a 【layer】. More layers think through harder things.");
            Add("宽度", "Width", "每一层能同时盯着几样东西，叫【宽度】（也叫【通道】数），就像一眼能看到多大一片。",
                "How many things each layer watches at once is its 【width】 (its number of 【channels】): how much one glance takes in.");
            Add("脑型", "Brain type", "脑子里的线是怎么连起来的，叫【架构】；有的脑型天生会看图，有的天生会读句子，像理科脑和文科脑。",
                "How the brain is wired is its 【architecture】. Some are born to see pictures, some to read sentences, like a maths mind and a language mind.");
            Add("桌面", "Desk space", "显卡上能摊开多大的脑子，取决于【显存】；书桌太小，大部头的书就摊不开，脑子太大也跑不起来。",
                "How big a brain the card can lay out depends on its 【VRAM】. A small desk cannot open a big book, and a brain too big will not run.");
            Add("算力", "Compute", "显卡每秒能算多少道题，叫【算力】，就是它做题的手速。",
                "How many questions the card works per second is its 【compute】: how fast its hands move.");
            Add("步子", "Step size", "每次发现错了，改多大一步，叫【学习率】；改作文一次只改一句很稳，一次改一整段学得快，也容易改崩。",
                "How big a fix it makes after each mistake is the 【learning rate】. Editing an essay one sentence at a time is safe; a whole paragraph at a time is fast and easy to ruin.");
            // 1.4 abilities and progress
            Add("能力", "Ability", "它学会做的一件新事。能力不是一点点长出来的，脑子和题攒够了，会在某一刻突然冒出来，行内叫【涌现】。",
                "A new thing it can do. Abilities do not grow bit by bit: with enough brain and questions they appear all at once, which is called 【emergence】.");
            Add("阶段", "Stage", "每多一项能力就进一个阶段，一共六个，对应人工智能真实走过的六段历史。",
                "Each new ability is a new stage. There are six, one for each chapter of real AI history.");
            Add("题量", "Questions learned", "它一共学过多少道题，也就是练过的【样本】总数。",
                "How many questions it has learned from in all: its total 【samples】.");
            Add("关卡", "Wall", "每个阶段都有一堵过不去的墙，行内叫【瓶颈】；历史上每次卡住，都是某个新点子把墙推倒的。",
                "Every stage has a wall it cannot pass, a 【bottleneck】. In history each one fell to a new idea.");
            Add("攻略", "Guide", "把过关的配方直接告诉你，这里叫【秘籍】；自己琢磨出来的人，会拿到一笔奖金。",
                "The recipe for the wall, told straight out, is a 【cheat sheet】. Work it out yourself and you get a bonus.");
            Add("通读", "Read everything", "终章把所有题一次全喂进去，不再一道道教答案，让它自己猜下一个字，行内叫【预训练】。",
                "In the finale every question goes in at once. Nobody teaches answers; it guesses the next word itself. That is 【pre-training】.");
            Add("挑好答案", "Pick the better reply", "两个回答摆在面前，你挑哪个更好，它就往哪边长，这叫【偏好对齐】；你总挑好听的，它就学会讨好你。",
                "Two replies side by side; it grows towards the one you pick. That is 【preference alignment】. Always pick the flattering one and it learns to flatter you.");
            Add("底线", "Bottom line", "写进脑子最深处、以后再也改不动的几条规矩，叫【底层规则】。",
                "A few rules written into the deepest part of the brain, never to change again: its 【core rules】.");
            // 1.5 economy
            Add("接活", "Jobs", "公司付钱请它干活，签的是一份【订单】；按时交、达标了才给钱。",
                "A company pays it to do a job under a 【contract】. Deliver on time and up to standard, and it pays.");
            Add("外包", "Crowd work", "在平台上发题、接题挣钱，叫【众包】：一道题几分钱，答的人遍布全国。",
                "Earning by answering questions on a platform is 【crowdsourcing】: a few fen a question, workers all over the country.");
            Add("抽查", "Spot check", "平台会偷偷混进几道早就有标准答案的题，叫【金标题】；答错了要罚钱，错多了封号。",
                "The platform slips in questions whose answers it already knows, 【gold questions】. Miss one and you are fined; miss many and you are banned.");
            Add("错上加错", "Errors on errors", "它自己标的题里藏着错，再拿来教自己，错就越滚越多，行内叫【近亲繁殖】。",
                "Its own labels carry mistakes; teach it with them and the mistakes snowball. That is 【inbreeding】.");
            Add("找人帮忙", "Get help", "花钱请网吧的朋友替你答题，叫【转包】；钱少赚一点，手腾出来了。",
                "Paying friends at the internet café to answer for you is 【subcontracting】: a bit less money, free hands.");
            Add("自动", "Auto", "让它自己练、自己考、自己答题，叫【自动化】；按钮下面写着下一级怎么开。",
                "Letting it practise, test and answer on its own is 【automation】. The button says how to open the next level.");
            Add("夜间电", "Night power", "半夜用电便宜一半，电力公司叫【谷电】；显卡在夜里练最划算。",
                "Power after midnight costs half, 【off-peak】 rates. Night is the cheapest time to practise.");
        }

        /// <summary>Whether the word has a hover card.</summary>
        public static bool Has(string zh) => zh != null && entries.ContainsKey(zh);

        /// <summary>The interface word in the player's language.</summary>
        public static string Word(string zh, bool english) => entries.TryGetValue(zh, out var e) ? (english ? e.en : e.zh) : zh;

        /// <summary>The explanation with every 【term】 drawn amber bold.</summary>
        public static string Body(string zh, bool english) => entries.TryGetValue(zh, out var e) ? Mark(english ? e.bodyEn : e.bodyZh) : "";

        /// <summary>
        /// The whole hover card: the word in white bold, the sentence(s) with the trade terms in amber, and an optional
        /// how-to line in small grey-blue. Unknown words give just the extra text.
        /// </summary>
        public static string Tip(string zh, bool english, string extra = null)
        {
            if (!entries.TryGetValue(zh, out var e)) return extra ?? "";
            var sb = new StringBuilder();
            sb.Append("<size=19><b>").Append(english ? e.en : e.zh).Append("</b></size>\n");
            sb.Append(Mark(english ? e.bodyEn : e.bodyZh));
            if (!string.IsNullOrEmpty(extra)) sb.Append("\n\n<size=15><color=").Append(NoteColor).Append('>').Append(extra).Append("</color></size>");
            return sb.ToString();
        }

        /// <summary>Several words in one card (a line of numbers): each word as a heading, then its sentence.</summary>
        public static string Tips(bool english, params string[] words)
        {
            var sb = new StringBuilder();
            foreach (var w in words)
            {
                if (!entries.TryGetValue(w, out var e)) continue;
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append("<b>").Append(english ? e.en : e.zh).Append("</b>  ").Append(Mark(english ? e.bodyEn : e.bodyZh));
            }
            return sb.ToString();
        }

        /// <summary>【term】 → amber bold rich text. Text without brackets is returned unchanged.</summary>
        public static string Mark(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('【') < 0) return text ?? "";
            return text.Replace("【", "<color=" + TermColor + "><b>").Replace("】", "</b></color>");
        }
    }
}
