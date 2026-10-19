using System;

namespace LingGuangV05.XingGuang
{
    public static class XgDailyKnownDates
    {
        // Canonical authored source, not a claim that the player has already heard it:
        // Resources/LingGuang/Rag/qingwen.json, us-anniversary. Qingwen says Jan 1;
        // the player says Dec 31. The reminder preserves Qingwen's perspective.
        public const string AnniversarySource="LingGuang/Rag/qingwen#us-anniversary";
        static bool Sentence(string text,string statement)
        {
            string value=(text??"").Replace(" ","").ToLowerInvariant();
            int at=value.IndexOf(statement,StringComparison.Ordinal);
            while(at>=0)
            {
                int end=at+statement.Length;
                bool before=at==0||"。！.!\r\n".IndexOf(value[at-1])>=0;
                bool after=end==value.Length||"。！.!\r\n".IndexOf(value[end])>=0;
                if(before&&after)return true;
                at=value.IndexOf(statement,at+1,StringComparison.Ordinal);
            }
            return false;
        }
        // Only explicit affirmative received speech matching the authored date, not questions,
        // hypothetical plans, quoted examples, a player's assertion or the source file alone.
        public static bool DisclosesAnniversary(string text)=>
            Sentence(text,"我们的纪念日是1月1日")||Sentence(text,"纪念日我认1月1日")||
            Sentence(text,"我认的纪念日是1月1日")||Sentence(text,"ouranniversaryis1january")||
            Sentence(text,"ouranniversaryisjanuary1")||Sentence(text,"icountouranniversaryas1january");
        public static int DaysUntil(DateTime today,int month,int day)
        {
            var date=new DateTime(today.Year,month,day);
            if(date<today.Date)date=date.AddYears(1);
            return (date-today.Date).Days;
        }
    }
}
