using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Delivery acknowledgements only. Older activity is never scanned into new notices.
        public List<string> dailyAnnouncedNodes=new List<string>();
    }
    public sealed partial class XgSim
    {
        public event Action<string> DailyConditionCompleted;
        void DailyNoticeTransition(string bucket,int before,int after)
        {
            foreach(var node in XgDailyLife.Nodes)
            {
                int required=XgDailyLife.Required(node.condition);
                if(XgDailyLife.Bucket(node.condition)!=bucket||before>=required||after<required||Has(node.id)||DailyNoticeAcknowledged(node.id))continue;
                DailyConditionCompleted?.Invoke(node.id);
            }
        }
        public bool DailyNoticeAcknowledged(string id)=>S.dailyAnnouncedNodes!=null&&S.dailyAnnouncedNodes.Contains(id);
        public bool AcknowledgeDailyNotice(string id)
        {
            if(!DailyNodeReady(id)||Has(id)||DailyNoticeAcknowledged(id))return false;
            if(S.dailyAnnouncedNodes==null)S.dailyAnnouncedNodes=new List<string>();
            S.dailyAnnouncedNodes.Add(id);return true;
        }
    }
    public static class XgDailyLifeCopy
    {
        static bool Statement(string value,string sentence)
        {
            if(string.IsNullOrEmpty(value))return false;
            int at=value.IndexOf(sentence,StringComparison.Ordinal);
            while(at>=0)
            {
                int end=at+sentence.Length;
                bool before=at==0||"。！.!\r\n".IndexOf(value[at-1])>=0;
                bool after=end==value.Length||"。！.!\r\n".IndexOf(value[end])>=0;
                if(before&&after)return true;
                at=value.IndexOf(sentence,at+1,StringComparison.Ordinal);
            }
            return false;
        }
        // A narrow affirmative statement in an actually received Qingwen message, never a question or quoted substring.
        public static bool ConfirmedBirthday(string normalized)=>Statement(normalized,"我的生日是9月27日")||Statement(normalized,"我生日是9月27日")||
            Statement(normalized,"mybirthdayisseptember27")||Statement(normalized,"mybirthdayis27september");
        public static string Detail(string id,bool english)
        {
            switch(id)
            {
                case "daily.dice":return english?"Opens Probability questions immediately. If you later buy Training monitor, its forecast gains decimal precision and the actual conditional drop rules.":"立即开放概率小题。之后购买训练监控，风险预报会显示小数精度与实际退步条件。";
                case "daily.news":return english?"Opens News review using articles you have actually finished reading.":"开放新闻回顾题桌，题目来自你实际读完的新闻。";
                case "daily.surf":return english?"Highlights the existing handwritten-digit data link in forum posts so it is easier to find.":"在贴吧帖子里高亮已有的手写数字资料链接，方便找到它。";
                case "daily.bargain":return english?"Enables the existing used-card discount on Miaoyu. Check the displayed price before buying.":"在喵鱼启用现有二手显卡折扣，购买前可查看实际价格。";
                case "daily.care":return english?"Adds a considerate expression cue. Birthday and anniversary reminders require an explicit saved message from Qingwen matching her authored dates. Her anniversary is labelled as her 1 January view, not an agreed date. No affection or gifts are added.":"加入体贴表达提示。生日、纪念日提醒需要晴雯在实际聊天中明确披露且与剧情日期相符；纪念日标明是她认的1月1日，不假装双方一致。不增加好感或代送礼。";
                case "daily.slang":return english?"Adds the existing slang explanations and sticker suggestions to the YY sticker picker.":"在 YY 表情选择器里加入现有网络用语解释和斗图建议。";
                case "daily.rhythm":return english?"A completed training round can pulse with an audible accent in the music that is playing. Without a clear accent, it uses the normal completion cue. Training results are saved immediately.":"训练完成提示可跟随正在播放的音乐重音。没有可辨认的重音时，使用普通完成提示；训练结果立即记入。";
                default:return "";
            }
        }
    }
}
