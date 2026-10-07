using System;
using System.Globalization;
using System.Text;

namespace LingGuangV05.Core.Story
{
    /// <summary>Authored in-game commission. Learned capability unlocks it; powered work advances it; only player actions send it.</summary>
    public static class EraAdvantageStory
    {
        public const int Goal = 20;
        public const string Draft = "谢谢确认规格。现有颜色可以先核对数量；黑色这批周五才到，我暂不承诺明天发货。具体发货日确认后再回复您。";
        static bool On(StoryState s, string key) => s != null && s.GetFlag(key) > 0;
        static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        static void First(StoryState s, string id, string text, double value = 0, int day = 0, double seconds = 0)
        { s.RecordFirst(new StoryFirst { id=id, text=text, value=value, day=day, gameSeconds=seconds }); }
        public static bool BeginBatch(StoryState s, bool capable)
        {
            if(s==null || !capable || !s.HasFired("era_delivery_brief") || On(s,"era.batch.started"))return false;
            s.SetFlag("era.batch.started",1);
            First(s,"era.delivery.brief","亲友委托：先交20份资料齐全的商品英文页。未确认的尺寸与交期单独列出，不要混交。只有你确认后才发送。");
            return true;
        }
        public static int AdvanceBatch(StoryState s, double seconds, double poweredRate, int day, double gameSeconds)
        {
            if(s==null || !On(s,"era.batch.started") || !On(s,"era.batch.on") || On(s,"era.batch.ready") || !Finite(seconds) || !Finite(poweredRate) || seconds<=0 || poweredRate<=0)return 0;
            double work=Math.Max(0,s.GetFlag("era.batch.work"))+Math.Min(seconds,60)*Math.Min(poweredRate,4);
            int before=(int)s.GetFlag("era.batch.good"), after=Math.Min(Goal,before+(int)Math.Floor(work));
            for(int i=before;i<after;i++)First(s,"era.delivery.item."+(i+1).ToString("00"),ProductDraft(i),1,day,gameSeconds);
            s.SetFlag("era.batch.work",after==Goal?0:work-(after-before));s.SetFlag("era.batch.good",after);
            if(after==Goal)
            {
                s.SetFlag("era.batch.ready",1);s.SetFlag("era.batch.on",0);
                First(s,"era.delivery.review","待你核对（不在已确认交付的20份内）\n\n补充样品 A：尺寸只写了12×8，没有单位。不能擅自补成厘米。\n补充样品 B：黑色批次尚未确认到货时间。不能承诺明天发货。\n\n已完成的20份采用资料齐全的其他款式。",2,day,gameSeconds);
            }
            return after-before;
        }
        // Deliberately authored examples, not claims of a live language-model service or real exported files.
        public static string ProductDraft(int index)
        {
            string[] zh={"帆布单肩包，蓝色，38×32厘米，内置小口袋。","A5横线笔记本，80页，牛皮纸封面。","硅胶理线夹，灰色，四只装。","桌面收纳盒，PP材质，24×16×10厘米。","尼龙旅行拉链袋，藏青色，两只装。","软木杯垫，直径10厘米，六片装。","铝制手机支架，可折叠，银色。","不锈钢长尾夹，十二只装。","A4水彩练习纸，20张，未附画框。","USB桌灯，5V供电，附1米线，不含电源适配器。","棉混纺短袜，70%棉、30%聚酯纤维，三双装。","收纳布袋，浅灰色，28×20厘米。","陶瓷花盆，直径8厘米，底部有排水孔。","棉围裙，米色，前侧一个口袋。","不锈钢随行杯，450毫升。","榉木梳，长16厘米。","键盘桌垫，60×30厘米，深绿色。","硅胶锅铲，木柄，单只装。","黑色中性笔，0.5毫米，五支装。","PU卡套，六个卡位，棕色。"};
            string[] en={"Blue canvas shoulder bag, 38 × 32 cm, with an inside pocket.","A5 lined notebook with 80 pages and a kraft-paper cover.","Grey silicone cable clips. Pack of four.","Polypropylene desk organizer, 24 × 16 × 10 cm.","Navy nylon travel zip pouches. Set of two.","Cork coasters, 10 cm in diameter. Set of six.","Foldable aluminium phone stand in silver.","Stainless-steel binder clips. Pack of twelve.","A4 watercolour practice paper. Twenty sheets; frame not included.","USB desk lamp, powered by 5 V. Includes a 1 m cable; power adapter not included.","Ankle socks made of 70% cotton and 30% polyester. Three pairs.","Light-grey fabric storage pouch, 28 × 20 cm.","Ceramic plant pot, 8 cm in diameter, with a drainage hole.","Beige cotton apron with one front pocket.","Stainless-steel travel cup, 450 ml.","Beech-wood comb, 16 cm long.","Dark-green keyboard desk mat, 60 × 30 cm.","Silicone spatula with a wooden handle. One piece.","Black gel pens with a 0.5 mm tip. Pack of five.","Brown PU card holder with six card slots."};
            int k=Math.Max(0,Math.Min(Goal-1,index));
            return "商品英文页 "+(k+1).ToString("00")+".txt\n\n原始资料："+zh[k]+"\n\n英文初稿：\n"+en[k]+"\n\n核对：保留了原有数字、单位和限制；未补写交货日期或未经提供的性能。\n状态：已整理，等待你确认后统一交付。";
        }
        public static bool ConfirmDelivery(StoryState s)
        {
            if(!On(s,"era.batch.ready") || !s.HasFired("era_delivery_ready") || On(s,"era.batch.delivered"))return false;
            First(s,"era.delivery.receipt","交付回执\n\n已由你确认并发送：20份商品英文页。\n另附：2条待核对事项，不冒充已经确认的结果。\n执行者：本机模型；审核与交付决定：你。",Goal);
            s.SetFlag("era.batch.delivered",1);return true;
        }
        public static bool ChooseIdentity(StoryState s,int choice)
        {
            if((choice!=1&&choice!=2)||!On(s,"era.batch.delivered")||!s.HasFired("era_delivery_confirmed")||On(s,"era.identity.reply"))return false;
            s.SetFlag("era.identity.reply",choice);First(s,"era.identity.reply",choice==1?"就我一个。":"我和它。",choice);return true;
        }
        public static bool ChooseContext(StoryState s,int choice)
        {
            if((choice!=1&&choice!=2)||s==null||!s.HasFired("era_context_brief")||On(s,"era.context.choice"))return false;
            s.SetFlag("era.context.choice",choice);First(s,"era.context.reply",choice==1?Draft:"这次我自己回。",choice);return true;
        }
        public static bool RequestChallenge(StoryState s,int stage,string dataset="logic")
        {
            if(s==null||stage<4||On(s,"era.challenge.requested"))return false;
            if(string.IsNullOrEmpty(dataset))return false;
            s.SetFlag("era.challenge.requested",1);First(s,"era.challenge.request",dataset);return true;
        }
        public static bool RequestExpert(StoryState s,int stage)
        {
            if(s==null||stage<5||!On(s,"era.batch.delivered")||On(s,"era.expert.requested"))return false;
            s.SetFlag("era.expert.requested",1);return true;
        }
        public static bool RecordExam(StoryState s,string dataset,int right,int count,string transcript,bool independent)
        {
            if(s==null||!independent||string.IsNullOrEmpty(dataset)||count!=20||right<0||right>count||string.IsNullOrEmpty(transcript))return false;
            double accuracy=(double)right/count; bool changed=false;
            string target=s.GetFirst("era.challenge.request")?.text;
            if(On(s,"era.challenge.requested")&&!On(s,"era.challenge.passed")&&dataset==target&&accuracy>=.8)
            {s.SetFlag("era.challenge.passed",1);First(s,"era.challenge.receipt",transcript,accuracy);changed=true;}
            if(On(s,"era.expert.requested")&&s.HasFired("era_expert_invite")&&!On(s,"era.expert.passed")&&dataset=="translate"&&accuracy>=.85)
            {s.SetFlag("era.expert.passed",1);First(s,"era.expert.receipt",transcript,accuracy);changed=true;}
            return changed;
        }
        public static string DeliveryText(StoryState s)
        {
            var b=new StringBuilder("交付文件夹\n\n已完成："+(int)s.GetFlag("era.batch.good")+" / "+Goal+" 份\n");
            if(!On(s,"era.batch.started"))return b+"\n先接下这批，再把接线页的本体输出接到「亲友委托」。\n电脑没电或线路断开时，这里的文件不会继续增加。";
            b.Append(On(s,"era.batch.delivered")?"状态：已由你确认交付\n":On(s,"era.batch.ready")?"状态：初稿就绪，尚未发送\n":"状态：制作中；需要保持接线和供电\n");
            for(int i=1;i<=Goal;i++){var item=s.GetFirst("era.delivery.item."+i.ToString("00"));if(item!=null)b.Append("\n").Append(item.text).Append("\n────────────\n");}
            var review=s.GetFirst("era.delivery.review");if(review!=null)b.Append("\n").Append(review.text);
            var receipt=s.GetFirst("era.delivery.receipt");if(receipt!=null)b.Append("\n\n").Append(receipt.text);
            return b.ToString();
        }
    }
}
