using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Stage-one lessons already told (<see cref="XgTutorLesson.id"/>), so each plays once per save.</summary>
        public List<string> tutorSeen = new List<string>();
    }

    /// <summary>
    /// One stage-one lesson: what the to-do note's current step is about, told once in plain words. The protagonist thinks
    /// it first (inner voice), then 老周 answers in the forum's private messages: the player told him they are teaching
    /// themselves machine learning, so he explains the idea without knowing about 灵光 (剧情修订: nobody knows).
    /// <see cref="whyZh"/> leads the step's hover text while stage one lasts.
    /// </summary>
    public sealed class XgTutorLesson
    {
        public string id, innerZh, innerEn, zhouZh, zhouEn, whyZh, whyEn;
    }

    /// <summary>
    /// 第一阶段全程引导: after it learns yes from no (the opening), every step the note gives until the 能力考 is passed
    /// comes with a lesson — new data, a structure that can bend, a bigger brain, training a new brain to grade C,
    /// more samples, labelling it yourself, plateaus, the exam and its retry, the pass. The note itself keeps to the
    /// main line in stage one (<see cref="XgGuide"/>: no side tasks or boosts), so the player learns one idea at a time.
    /// </summary>
    public sealed partial class XgSim
    {
        static XgTutorLesson L(string id, string innerZh, string innerEn, string zhouZh, string zhouEn, string whyZh, string whyEn) =>
            new XgTutorLesson { id = id, innerZh = innerZh, innerEn = innerEn, zhouZh = zhouZh, zhouEn = zhouEn, whyZh = whyZh, whyEn = whyEn };

        public static readonly XgTutorLesson[] TutorLessons =
        {
            L("data",
                "它只会是和否。想教它点别的……得问问懂行的。就跟老周说我在自学机器学习。",
                "It only knows yes and no. To teach it more… I need someone who knows. I'll tell Lao Zhou I'm teaching myself machine learning.",
                "哟，AlphaGo 把你也看动心了？行，先记住一句：模型吃什么，就长成什么。想让它认垃圾短信，就喂它垃圾短信。网上有现成的数据包，先挑个便宜的买。",
                "Oh, AlphaGo got you too? Fine. Rule one: a model becomes what it eats. Want it to spot spam? Feed it spam. There are ready-made datasets online; start with a cheap one.",
                "新本事要新数据：先买一个数据包，它才有新东西可学。",
                "A new skill needs new data: buy a dataset first, so it has something new to learn."),
            L("arch",
                "老周说我这个是感知机……一根筋的那种。",
                "Lao Zhou says mine is a perceptron… the one-track-mind kind.",
                "你现在用的是感知机吧？1958 年的老古董，只会在纸上画一条直线，把东西分成两边。题目稍微绕个弯它就懵。换多层感知机（MLP），中间多藏几层，它就学得会拐弯了。",
                "You're on a perceptron, right? A 1958 antique. It can only draw one straight line and split things in two; the moment a question bends, it's lost. Switch to a multi-layer perceptron (MLP): a few hidden layers in the middle and it learns to bend.",
                "感知机只会画一条直线分两边；多层感知机（MLP）中间有隐藏层，能学会拐弯的规律。",
                "A perceptron can only draw one straight line; an MLP has hidden layers and can learn patterns that bend."),
            L("capacity",
                "说白了，就是给它换个大点的脑子。",
                "In short: give it a bigger brain.",
                "层数是它能想几步，宽度是每一步能同时惦记几件事。脑子太小，装不下新本事。不过别一口气加满——越大越吃显存，也越难练。",
                "Layers are how many steps it can think; width is how many things it can keep in mind at each step. Too small a brain can't hold a new skill. Don't max it out in one go, though — bigger eats more VRAM and is harder to train.",
                "层数 = 能想几步，宽度 = 每一步能同时记几件事。模型越大装得越多，也越吃显存。",
                "Layers = how many steps it thinks; width = how much it holds at each step. Bigger holds more and eats more VRAM."),
            L("grade",
                "新脑子，也是空脑子。行，接着练。",
                "A new brain is an empty brain. Fine, keep training.",
                "换了脑子不等于变聪明。新脑子是空的，得重新练。练到 C 级再说它会了，别练一轮、看一眼成绩就跑。",
                "A new brain doesn't make it smart. It starts empty and has to be trained again. Call it learned at grade C, not after one round and a glance at the score.",
                "模型变大后是空的，要重新训练；成绩到 C 级，这些参数才算真学到了东西。",
                "A model that just grew is empty and must be trained again; at grade C its parameters have really learned something."),
            L("more",
                "原来它也会背答案……跟我高三一模一样。",
                "So it memorises answers too… just like me in my last year of high school.",
                "数据太少，它就死记硬背，跟考前背答案一样，题目一换就傻眼。多给它几批样本，越多越杂，它学到的才是规律。",
                "With too little data it just memorises, like cramming answers before an exam — change the question and it's lost. Give it more batches, more and more varied, and what it learns is the pattern.",
                "样本太少，模型只会死记硬背；样本越多越杂，它学到的才是规律。",
                "Too few samples and the model only memorises; more varied samples and it learns the pattern."),
            L("label",
                "标一张赚一块，顺便还教了它一张。这买卖不亏。",
                "Label one, earn a yuan, and teach it one on the side. Not a bad deal.",
                "数据买要花钱，自己标不花钱，就是费手。你那个兼职不就是标注吗？标的时候顺手喂给它，一举两得。",
                "Bought data costs money; labelling it yourself only costs your wrist. Isn't your side job labelling anyway? Feed it what you label — two birds, one stone.",
                "自己标的每一张，既是收入，也是它的训练数据。",
                "Every card you label is both pay and its training data."),
            L("plateau",
                "练了好几轮，成绩一动不动。……卡住了。",
                "Round after round and the score won't move… it's stuck.",
                "成绩不涨了？要么脑子不够大，要么书不够多。先看看是哪个卡住了，别光埋头一轮一轮地硬练。",
                "Score stopped climbing? Either the brain is too small or it hasn't read enough. Find out which is stuck first; don't just grind round after round.",
                "成绩卡住一般就两个原因：模型太小，或者数据太少。",
                "A stuck score usually has one of two causes: the model is too small, or the data is too thin."),
            L("exam",
                "考它没见过的题……那答对的就是真本事。",
                "Questions it has never seen… then whatever it gets right is real.",
                "学没学会，别听它的，考一下就知道。拿它没见过的题考，拿练过的题考那叫作弊。",
                "Don't take its word for it — test it. Use questions it has never seen; testing on the practice set is cheating.",
                "能力考用它没见过的题：8 道答对 6 道才算真学会，没考过也不扣钱。",
                "The ability exam uses questions it has never seen: 6 of 8 right means it really learned. Failing costs nothing."),
            L("retry",
                "没过。……也不扣钱。那就再练三轮。",
                "Failed… at least it's free. Three more rounds, then.",
                "没过很正常。我第一次组会汇报，被老板说得头都抬不起来。再练几轮，过两天再考。",
                "Failing is normal. At my first lab meeting my supervisor tore my talk apart. Train a few more rounds and test again in a bit.",
                "没考过不扣钱：再练几轮，到时间就能重考。",
                "A failed exam costs nothing: train a few more rounds and you can sit it again."),
            L("pass",
                "过了。它是真学会了，不是背下来的。",
                "It passed. It really learned it — it didn't just memorise.",
                "你那个小模型考过了？可以啊，比我们实验室新来的师弟上手还快。",
                "Your little model passed? Not bad — you picked this up faster than our lab's new first-year.",
                "考过了：点「继续」，它就学会这项新能力，进入下一阶段。",
                "Passed: press Continue and it learns the new ability; the next stage begins."),
        };

        public static XgTutorLesson TutorLesson(string id)
        {
            foreach (var l in TutorLessons) if (l.id == id) return l;
            return null;
        }

        /// <summary>Stage one, after the opening: the lesson that goes with the note's current step (null when none, or outside stage one).</summary>
        public XgTutorLesson TutorFor(XgGuideStep step)
        {
            if (step == null || S.stage != 1 || OpeningFocus) return null;
            switch (step.id)
            {
                // Progress explanations retain the recommended action's destination, but replace its step id.
                case XgGuide.ParameterLessonId: case XgGuide.SampleLessonId:
                    if (step.target != null && step.target.StartsWith("node:")) return TutorLesson(NodeLesson(step.target));
                    if (step.tab == "teach") return TutorLesson("label");
                    if (step.target == "label:训练一轮|Train 1 epoch") return TutorLesson("grade");
                    return TutorLesson(step.id == XgGuide.SampleLessonId ? "more" : "capacity");
                case "ability.study": case "ability.buy": return TutorLesson(NodeLesson(step.target));
                case "ability.params": case "ability.card": return TutorLesson("capacity");
                case "ability.train": return TutorLesson("grade");
                case "ability.batch": case "ability.data": case "ability.download": return TutorLesson("more");
                case "ability.label": case "ability.track": return TutorLesson("label");
                case "stage.confirm": return TutorLesson(ExamPassPending ? "pass" : "exam");
                case "exam.retry": return TutorLesson("retry");
                default: return step.id.StartsWith("plateau.") ? TutorLesson("plateau") : null;
            }
        }

        /// <summary>The same, only while it has not been told yet.</summary>
        public XgTutorLesson TutorDue(XgGuideStep step)
        {
            var l = TutorFor(step);
            return l == null || S.tutorSeen.Contains(l.id) ? null : l;
        }

        public void MarkTutor(XgTutorLesson lesson)
        {
            if (lesson != null && !S.tutorSeen.Contains(lesson.id)) S.tutorSeen.Add(lesson.id);
        }

        static string NodeLesson(string target)
        {
            var node = target != null && target.StartsWith("node:") ? XgCatalog.Node(target.Substring(5)) : null;
            if (node == null) return "capacity";
            switch (node.kind)
            {
                case XgNodeKind.Dataset: return "data";
                case XgNodeKind.Arch: return "arch";
                default: return "capacity";
            }
        }
    }
}
