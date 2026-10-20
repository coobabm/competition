using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgTeachingPack
    {
        public string id, name, nameEn, dataset, needs, play, playEn;
        public int stage;
        public bool buy;
        public string NodeId => "pack." + id;
    }
    [Serializable] public sealed class XgTeachingPackFile { public XgTeachingPack[] packs; }
    public static class XgTeachingPacks
    {
        // Standalone simulation fallback generated from Design/data/minigame_packs.json.
        // The player replaces these from the bundled JSON before loading any scene.
        public static XgTeachingPack[] All = {
            new XgTeachingPack { id="sudoku", name="数独", nameEn="Sudoku", dataset="logic", needs="", stage=2, buy=true, play="每行、每列和每宫填入1—9，不重复。", playEn="Fill each row, column and box with 1–9 without repeats." },
            new XgTeachingPack { id = "spam_swipe", name = "划短信", nameEn = "Swipe the texts", dataset = "spam", needs = "", play = "短信一条条从上面掉下来，左划是垃圾短信，右划是正常。越到后面掉得越快。", playEn = "Texts fall one by one: swipe left for spam, right for genuine. They fall faster as you go.", stage = 1, buy = false },
            new XgTeachingPack { id = "arith_mole", name = "算术打地鼠", nameEn = "Whack-a-sum", dataset = "arith", needs = "", play = "地洞里冒出带算式的地鼠（7+5=12），算对的敲，算错的放过。", playEn = "Moles pop up wearing sums (7+5=12): whack the right ones, leave the wrong ones.", stage = 1, buy = false },
            new XgTeachingPack { id = "minesweeper_yn", name = "扫雷判官", nameEn = "Minesweeper judge", dataset = "logic", needs = "", play = "Windows 扫雷的残局，高亮一格，问「这格是雷吗？」。答是 / 否，点开验证。", playEn = "A Windows Minesweeper endgame with one cell highlighted: is it a mine? Answer yes or no, then it opens.", stage = 1, buy = false },
            new XgTeachingPack { id = "make24", name = "算 24", nameEn = "Make 24", dataset = "arith", needs = "", play = "四张扑克牌，问「能凑出 24 吗？」答能 / 不能；答能的话，限时把算式拖出来。", playEn = "Four playing cards: can they make 24? Yes or no; if yes, drag out the sum against the clock.", stage = 1, buy = true },
            new XgTeachingPack { id = "headline_swipe", name = "左滑右滑 · 标题党", nameEn = "Swipe · clickbait", dataset = "headline", needs = "", play = "新闻标题一张张划：左是标题党，右是正经新闻。", playEn = "Swipe headlines: left for clickbait, right for real news.", stage = 2, buy = true },
            new XgTeachingPack { id = "odd_one", name = "找不同", nameEn = "Odd one out", dataset = "logic", needs = "", play = "四个词（猫、狗、鱼、桌子），点那个不一样的。", playEn = "Four words (cat, dog, fish, table): tap the one that doesn't belong.", stage = 2, buy = false },
            new XgTeachingPack { id = "sequence", name = "找规律", nameEn = "Next in line", dataset = "logic", needs = "", play = "一串数字（2, 4, 8, ?），从四个选项里选下一个。", playEn = "A sequence (2, 4, 8, ?): pick the next from four options.", stage = 2, buy = false },
            new XgTeachingPack { id = "danmu_mood", name = "弹幕夸还是骂", nameEn = "Praise or jab", dataset = "danmu", needs = "", play = "弹幕从右往左飘过，点「夸 / 骂 / 反话」。", playEn = "Comments scroll past: tag each praise, jab or sarcasm.", stage = 2, buy = false },
            new XgTeachingPack { id = "mnist_write", name = "手写数字", nameEn = "Handwritten digits", dataset = "mnist", needs = "vision", play = "看一个手写数字点答案；或者自己用鼠标写一个，看它认不认得。", playEn = "Read a handwritten digit, or write one with the mouse and see if it can read it.", stage = 2, buy = true },
            new XgTeachingPack { id = "chess_mate1", name = "国际象棋 · 一步杀", nameEn = "Chess · mate in one", dataset = "chess", needs = "", play = "残局摆好，找一步将杀：在棋盘上走一步，或从四个候选里选。题库读 chess_mates.json。", playEn = "Find the mating move: play it on the board or pick from four candidates. Puzzles from chess_mates.json.", stage = 2, buy = true },
            new XgTeachingPack { id = "gomoku_win", name = "五子棋 · 一步连五", nameEn = "Gomoku · five in a row", dataset = "go", needs = "", play = "五子棋残局，点出那一步连成五子的位置；或者判断「黑棋下一步能赢吗」。", playEn = "A gomoku endgame: tap the point that makes five, or judge whether black wins next move.", stage = 2, buy = false },
            new XgTeachingPack { id = "captcha", name = "验证码", nameEn = "Captchas", dataset = "cifar", needs = "vision", play = "扭曲的字母数字，打出来；或者 12306 九宫格「点出所有的红酒」。", playEn = "Type the warped characters, or the 12306 grid: tap every wine bottle.", stage = 3, buy = true },
            new XgTeachingPack { id = "meme_word", name = "表情包配字", nameEn = "Caption the meme", dataset = "meme", needs = "vision", play = "一张表情，从六个词里选它的情绪；买了「图像描述」以后改成自己打一个词。", playEn = "A face: pick its mood from six words; after image captioning, type your own.", stage = 3, buy = true },
            new XgTeachingPack { id = "poem_next", name = "唐诗 · 下一个字", nameEn = "Tang poems · next character", dataset = "poems", needs = "memory", play = "五言诗先出半句，点下一个字；买了门控记忆后开七言。", playEn = "Half a five-character line: pick the next character; seven-character lines open with gated memory.", stage = 3, buy = true },
            new XgTeachingPack { id = "idiom_chain", name = "成语接龙", nameEn = "Idiom chain", dataset = "poems", needs = "memory", play = "它给一个成语，你从三个里选能接上的（首字等于上一个的尾字）；接得越长分越高。", playEn = "It gives an idiom; pick the one of three that starts with its last character. Longer chains score more.", stage = 3, buy = false },
            new XgTeachingPack { id = "chess_mate2", name = "国际象棋 · 两步杀", nameEn = "Chess · mate in two", dataset = "chess", needs = "lstm", play = "先走一步，对方应一步（自动），再走一步将杀。", playEn = "Make a move, it answers automatically, then deliver mate.", stage = 4, buy = true },
            new XgTeachingPack { id = "spy", name = "谁是卧底", nameEn = "Who is the spy", dataset = "longtext", needs = "", play = "四个人拿近义词，轮流描述，最后投票。玩家和 {名字}、小周、阿杰一起。", playEn = "Four players with near-synonyms describe in turn, then vote. You, {name}, Xiao Zhou and A-Jie.", stage = 4, buy = false },
            new XgTeachingPack { id = "review_swipe", name = "刷单评论", nameEn = "Fake reviews", dataset = "review", needs = "", play = "淘宝评论一条条划：刷单 / 真买家。", playEn = "Swipe Taobao reviews: fake or genuine.", stage = 4, buy = true },
            new XgTeachingPack { id = "chinglish", name = "中式英语改错", nameEn = "Fix the Chinglish", dataset = "translate", needs = "", play = "一句中式英语（Good good study, day day up），点出错的词，选正确的说法。", playEn = "A line of Chinglish: tap the wrong words and pick the right phrasing.", stage = 5, buy = true },
        };
        public static XgTeachingPack Find(string id) { foreach (var p in All) if (p.id == id) return p; return XgStageLessons.Find(id); }
    }
    public static partial class XgCatalog
    {
        static readonly XgDesk ChessTeachingDesk = new XgDesk { id = "chess", name = "国际象棋", nameEn = "Chess", kind = XgDeskKind.Logic, pay = 3, comboWindow = 60 };
        static readonly XgDesk TranslationTeachingDesk = new XgDesk { id = "translate", name = "翻译", nameEn = "Translation", kind = XgDeskKind.Text, pay = 3, comboWindow = 30 };
        static XgDesk ExtraTeachingDesk(string id) => id == "chess" ? ChessTeachingDesk : id == "translate" ? TranslationTeachingDesk : null;
        static void AddTeachingPackNodes(List<XgNode> list)
        {
            foreach (var p in XgTeachingPacks.All)
            {
                if (!p.buy) continue;
                var n = N(p.NodeId, "trunk", "train.curve", XgNodeKind.Research, p.dataset, 0,
                    15 * p.stage * p.stage, 0, 0, p.name, p.nameEn,
                    "一桌新的练习。▶ 开放" + p.name, "New practice. Opens " + p.nameEn);
                n.stage = p.stage; n.lane = "research"; list.Add(n);
            }
        }
    }
}
