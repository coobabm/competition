using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgMiniReward { Label, Training, Crawl, Organize }
    public sealed class XgMiniGameDef
    {
        public readonly string Id, Name, NameEn, Desk, Group, Rule, RuleEn;
        public readonly int Stage;
        public readonly XgMiniReward Reward;
        public XgMiniGameDef(string id, string name, string nameEn, string desk, string group, string rule, string ruleEn, int stage = 1, XgMiniReward reward = XgMiniReward.Label)
        { Id = id; Name = name; NameEn = nameEn; Desk = desk; Group = group; Rule = rule; RuleEn = ruleEn; Stage = stage; Reward = reward; }
    }
    public static class XgMiniGames
    {
        static readonly XgMiniGameDef[] games = {
            new XgMiniGameDef("draw", "你画我猜", "Draw and Guess", "mnist", "标注", "画数字，纠正识别；有效笔迹才计入样本", "Draw digits and correct the reading; only real ink counts as a sample"),
            new XgMiniGameDef("slice", "切短信", "Slice the SMS", "spam", "标注", "划掉垃圾短信，保留正常消息", "Slice the spam, keep the normal messages"),
            new XgMiniGameDef("esp", "对暗号", "Telepathy", "danmu", "标注", "独立选择情绪，再看灵光是否与你一致", "Pick the mood yourself, then see whether LingGuang agrees"),
            new XgMiniGameDef("forgetax", "敲锤炼模型", "Forge the model", "", "训练", "敲准了就开一轮训练，样本、显存和电费照旧要用", "A good strike starts a training round; samples, VRAM and power are still needed", 1, XgMiniReward.Training),
            new XgMiniGameDef("captcha12306", "12306 验证码", "Ticket captcha", "cifar", "标注", "限时选中所有符合要求的图块，再提交", "Pick every tile that fits before time runs out, then submit"),
            new XgMiniGameDef("alphago", "AlphaGo 死活题", "Go life and death", "go", "标注", "根据棋盘气数落子；提子、救活，禁止自杀", "Play by liberties: capture, save, no suicide"),
            new XgMiniGameDef("feihualing", "飞花令", "Flying flowers", "poems", "标注", "轮流接含关键字的诗句，不可重复", "Take turns with lines of poetry holding the key character, no repeats"),
            new XgMiniGameDef("doutu", "斗图", "Meme battle", "meme", "标注", "三秒内选合适的表情回应", "Pick the right meme to answer within three seconds"),
            new XgMiniGameDef("swipe", "左滑右滑", "Swipe left, swipe right", "headline", "标注", "标题党左滑，正常标题右滑", "Clickbait goes left, honest headlines go right"),
            new XgMiniGameDef("zhaoshuijun", "找水军", "Spot the shills", "review", "标注", "观察评论证据，找出刷单评论", "Read the evidence and find the fake reviews"),
            new XgMiniGameDef("dadishu", "打地鼠", "Whack-a-mole", "arith", "标注", "只敲算错的算式；漏敲和误敲都会失误", "Whack only the wrong sums; a miss or a false alarm both count against you"),
            new XgMiniGameDef("shuishiwodi", "谁是卧底", "Who is the spy", "logic", "标注", "听每个人描述，再投票找出卧底", "Hear everyone describe their word, then vote out the spy"),
            new XgMiniGameDef("kaixincidian", "开心辞典", "Quiz show", "sense", "标注", "判断十二句话对不对，求助机会有限", "Twelve true-or-false statements in a row; help is limited"),
            new XgMiniGameDef("danmushooter", "弹幕射击", "Danmaku shooter", "danmu", "标注", "移动飞机击落恶意弹幕，不伤友善弹幕", "Fly the plane and shoot down hostile comments, spare the friendly ones"),
            new XgMiniGameDef("spidersnake", "爬虫贪吃蛇", "Crawler snake", "news", "数据", "吃词避障；开始要付百科爬虫费，每轮采集有上限", "Eat words and dodge walls; a crawl fee is paid up front and the run limit applies", 3, XgMiniReward.Crawl),
            new XgMiniGameDef("vramtetris", "显存俄罗斯方块", "VRAM Tetris", "", "训练", "消行就开一轮训练，显存和电费照旧要用", "Clear a line to start a training round; VRAM and power are still needed", 1, XgMiniReward.Training),
            new XgMiniGameDef("merge2048", "2048 合成数据", "2048 data merge", "", "数据", "把现有数据合并整理，达标就开一轮训练", "Merge the data you have and start a training round at the goal", 2, XgMiniReward.Organize),
            new XgMiniGameDef("sentence", "句子排序", "Sentence order", "longtext", "数据", "把词块放回正确语序", "Put the word blocks back in order"),
            new XgMiniGameDef("chuanhua", "传话游戏", "Telephone", "longtext", "数据", "观察中英回译，修复丢失的意思", "Watch the round trip through translation and repair the lost meaning"),
            new XgMiniGameDef("turing", "图灵测试", "Turing test", "longtext", "剧情", "分别询问两位匿名聊天者，再辨认灵光", "Question two anonymous chatters, then tell which is LingGuang", 5),
            new XgMiniGameDef("qingwen", "教它认人", "Teach it people", "danmu", "剧情", "先判断情绪，再选择体贴的回复", "Judge the mood first, then pick a kind reply", 4),
            new XgMiniGameDef("zhaobutong", "找不同", "Spot the difference", "cifar", "剧情", "对照两幅图，检查灵光的提示", "Compare two pictures and check LingGuang's hints", 3),
            new XgMiniGameDef("lianliankan", "连连看", "Link-up", "cifar", "剧情", "把词语和对应图形配对", "Pair each word with its picture", 3)
        };
        public static IReadOnlyList<XgMiniGameDef> All { get; } = Array.AsReadOnly(games);
        public static XgMiniGameDef Find(string id) { foreach (var g in games) if (g.Id == id) return g; return null; }
        /// <summary>
        /// The game that is a desk's main view in 摆渡众包, or null when the desk keeps its yes/no cards (or has its own view:
        /// the digits desk is written on, XgSim.Handwriting.cs). Every mapped game labels that desk's own dataset.
        /// </summary>
        public static string ForDesk(string desk)
        {
            switch (desk) {
                case "spam": return "slice"; case "danmu": return "esp";
                case "cifar": return "captcha12306"; case "go": return "alphago"; case "poems": return "feihualing";
                case "meme": return "doutu"; case "headline": return "swipe"; case "review": return "zhaoshuijun";
                case "arith": return "dadishu"; case "logic": return "shuishiwodi"; case "sense": return "kaixincidian";
                default: return null;
            }
        }
    }
    [Serializable]
    public sealed class XgMiniRecord
    {
        public string id;
        public int correct, wrong, samples, trainingStarts, organized;
        public double income;
    }
    public sealed partial class XgState
    {
        public List<XgMiniRecord> miniGames = new List<XgMiniRecord>();
    }
    public struct XgMiniResult
    {
        public bool accepted, trainingStarted;
        public int samples;
        /// <summary>Total paid (bonus included), the bonus part of it, and the fine of a wrong answer.</summary>
        public double pay, bonus, fine;
        public string note;
    }
}
