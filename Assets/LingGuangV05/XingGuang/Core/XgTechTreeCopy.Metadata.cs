using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LingGuangV05.XingGuang
{
    public static partial class XgTechTreeCopy
    {
        // Metadata never reveals an undiscovered node. Ordinary tools have no historical year.
        public static string Tag(XgNode node, XgSim sim)
        {
            if(node==null||sim.NodeMystery(node)||XgSim.IsAtlas(node))return "";
            if(node.id=="lenet")return "LeNet-5 · 1998"; // XgCatalog identifies this specific version.
            return All.TryGetValue(node.id,out var copy)?T(sim,copy.tag,copy.tagEn):"";
        }

        static void SeparateMetadata(string zh,string en,out string name,out string nameEn,out string tag,out string tagEn)
        {
            name=zh;nameEn=en;tag=tagEn="";
            var match=Regex.Match(zh,@"^(.*) (\S+) (工具|探索)?(\d{4})$");
            if(!match.Success)return;
            name=match.Groups[1].Value;
            string abbreviation=match.Groups[2].Value, kind=match.Groups[3].Value, year=match.Groups[4].Value;
            nameEn=Regex.Replace(en,@" (?:tool )?\d{4}$","");
            if(nameEn.EndsWith(" "+abbreviation,StringComparison.Ordinal))
                nameEn=nameEn.Substring(0,nameEn.Length-abbreviation.Length-1);
            // These two existing English names include the technical abbreviation as a word.
            if(zh.StartsWith("文字卷积 ",StringComparison.Ordinal))nameEn="Text convolution";
            if(zh.StartsWith("平滑开关 ",StringComparison.Ordinal))nameEn="Smooth switch";
            if(kind=="探索") { tag="自研 · "+year;tagEn="Research · "+year;return; }
            if(kind=="工具")
            {
                // Do not invent labels such as BRAIN, GOLD, GUIDE or PAY for ordinary game tools.
                switch(abbreviation)
                {
                    case "LRS":case "CKPT":case "AUG":case "MB":case "TL":case "LLM":case "OCR":case "QA":case "IDC":
                        tag=tagEn=abbreviation;break;
                }
                return;
            }
            tag=tagEn=abbreviation+" · "+year;
        }

        static string FirstSentence(string text,bool english)
        {
            if(string.IsNullOrEmpty(text))return "";
            int newline=text.IndexOf('\n');
            if(newline>=0)text=text.Substring(0,newline);
            int end=english?text.IndexOf(". ",StringComparison.Ordinal):text.IndexOf('。');
            return end<0?text:text.Substring(0,end+1);
        }

        static readonly Dictionary<string,string[]> ShortDetails=new Dictionary<string,string[]>(StringComparer.Ordinal)
        {
            {"perceptron",new[]{"从例题中学习区分两类答案。","Learn to distinguish two answer classes from examples."}},
            {"mlp",new[]{"组合多条线索，学习更复杂的分类。","Combine clues to learn more complex classifications."}},
            {"lenet",new[]{"从局部笔画识别手写数字。","Recognise handwritten digits from local strokes."}},
            {"alexnet",new[]{"学习图片中不同层次的特征。","Learn visual features at different levels."}},
            {"vgg",new[]{"学习图片中不同层次的特征。","Learn visual features at different levels."}},
            {"googlenet",new[]{"结合不同大小的观察区域识别图片。","Combine visual regions of different sizes."}},
            {"rnn",new[]{"将前面的内容用于当前判断。","Use preceding content in the current judgment."}},
            {"lstm",new[]{"选择保留哪些前文信息。","Select which preceding information to retain."}},
            {"gru",new[]{"用较精简的结构保留前文信息。","Retain preceding context with a simpler structure."}},
            {"textcnn",new[]{"从局部词组寻找判断线索。","Find decision clues in local word groups."}},
            {"resnet",new[]{"前层信息直接传到后层，帮助深层学习。","Pass earlier signals directly to later layers to support deep learning."}},
            {"seq2seq",new[]{"先读入句子，再组织输出。","Read a sentence before composing the output."}},
            {"attention",new[]{"生成译文时参考原句的相关内容。","Consult relevant source content while translating."}},
            {"caption",new[]{"为图片生成简短描述。","Generate brief descriptions of images."}},
            {"transformer",new[]{"同时参考句中多处线索。","Consider clues from several parts of a sentence."}},
            {"multihead",new[]{"同时参考不同类型的词语关系。","Consider several types of word relationships together."}},
            {"position",new[]{"将词语的位置用于判断。","Use word positions as clues."}},
            {"sft",new[]{"用指令和示范答案练习回答。","Practise responses using instructions and examples."}},
            {"cot",new[]{"先写中间推理，再组织答案。","Work through intermediate reasoning before answering."}},
            {"transfer",new[]{"更换模型结构时继承部分已学成果。","Retain some learned progress when changing model structure."}},
            {"shared.lr",new[]{"选择每次学习调整的幅度。","Choose the size of each learning adjustment."}},
            {"warmup",new[]{"调整设置后逐步增加学习幅度。","Increase learning steps gradually after a settings change."}},
            {"gradclip",new[]{"缩小过大的参数调整。","Reduce oversized parameter updates."}},
            {"batchnorm",new[]{"调整内部信号范围，支持更深的训练。","Align internal signal ranges to support deeper training."}},
            {"layernorm",new[]{"调整同一层内部信号的范围。","Align the signal ranges within a layer."}},
            {"residual",new[]{"增加前层到后层的直接通道。","Add a direct path from earlier to later layers."}},
            {"label.parallel",new[]{"保留更多待复核题，容量满时暂停。","Keep more review cards; automation pauses when capacity is full."}},
            {"label.coop",new[]{"按把握门槛将题目分给模型或玩家。","Assign cards to the model or player using the confidence threshold."}},
            {"label.audit",new[]{"抽查自动答案，将发现的错题交回复核。","Sample automatic answers and return detected errors for review."}},
            {"label.captcha",new[]{"满足识别条件后代填数字验证码。","Fill numeric captchas when recognition requirements are met."}},
            {"train.data",new[]{"将已取得的数据包安装到练页。","Install acquired data packs in Train."}},
            {"train.ledger",new[]{"查看当前资金、收入与用电信息。","View current funds, income and power use."}},
            {"train.curve",new[]{"选择数据，开始一轮训练。","Choose data and start a training round."}},
            {"train.holdout",new[]{"用未参与训练的题目观察学习效果。","Measure learning on questions excluded from training."}},
            {"train.checkpoint",new[]{"恢复已保存的最佳训练版本。","Restore the saved best training checkpoint."}},
            {"train.monitor",new[]{"开始训练前查看本轮风险。","Review risks before starting this training round."}},
            {"train.weights",new[]{"观察训练中使用的特征和权重。","View features and weights used in training."}},
            {"brain.window",new[]{"用可视化演示呈现网络活动。","Illustrate network activity through a visualisation."}},
            {"data.review",new[]{"回顾已学内容，拖入问题再次提问。","Review learned content and drag a question in to ask again."}},
            {"books.library",new[]{"打开图书馆选购书籍。","Visit the library to purchase books."}},
            {"board.logic2",new[]{"从本站标准五子棋图片中识别棋子。","Read stones from the site's standard Gomoku images."}},
            {"board.logic3",new[]{"比较后续走法，改善当前选择。","Compare subsequent moves to inform the current choice."}},
            {"board.logic4",new[]{"游戏运行时夜间自对弈，早晨生成记录包。","Play at night while the game runs and create a record pack in the morning."}},
            {"spider.acquire",new[]{"读取游戏内网页，也可拖到题目上作答。","Read in-game webpages or drag the spider onto a question to answer."}},
            {"spider.fast",new[]{"加快移动与文字读取。","Increase movement and text-reading speed."}},
            {"spider.browse",new[]{"自动继续浏览游戏内网页与桌面。","Continue browsing in-game webpages and the desktop automatically."}},
            {"spider.autoanswer",new[]{"按界面显示的AI答案作答。","Submit the AI answer shown on the question card."}}
        };
    }
}
