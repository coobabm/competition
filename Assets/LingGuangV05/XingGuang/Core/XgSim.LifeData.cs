using System;
using System.Collections.Generic;
using LingGuangV05.Core.Story;

namespace LingGuangV05.XingGuang
{
    public interface IXgRentLife { int OnTimeRentPeriods { get; } bool RentEverLate { get; } void RaiseRent(double perPeriod); }

    public sealed partial class XgState
    {
        public List<string> lifeDataSeen = new List<string>(), lifeFlags = new List<string>();
        public int lifeDrawStage, lifeDrawCount, lifeSeenVersion;
        public double lifePauseUntil, lifeBoostUntil, lifeBoostFactor = 1;
    }

    public sealed partial class XgSim
    {
        static readonly Dictionary<string, Dictionary<string, object>> LifeData = new Dictionary<string, Dictionary<string, object>>();
        public static int LifeDataCount => LifeData.Count;
        static string LS(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v as string ?? "" : "";
        static double LN(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) && v is double n ? n : 0;
        static List<object> LA(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v as List<object> : null;

        public static void LoadLifeData(string json)
        {
            var root = StoryJson.Parse(json) as Dictionary<string, object>;
            var rows = LA(root, "events");
            if (rows == null) throw new FormatException("life_events_100: missing events");
            var parsed = new Dictionary<string, Dictionary<string, object>>();
            foreach (var raw in rows)
            {
                var d = raw as Dictionary<string, object>;
                string id = LS(d, "id");
                if (id.Length == 0 || parsed.ContainsKey(id) || LA(d, "choices") == null) throw new FormatException("life_events_100: invalid or duplicate " + id);
                foreach (string k in new[] { "title", "titleEn", "body", "bodyEn", "who", "whoEn" })
                    if (LS(d, k).Length == 0) throw new FormatException("life_events_100: missing " + k + " in " + id);
                ValidateLifeData(d,id);
                parsed.Add(id, d);
            }
            LifeData.Clear();
            foreach (var kv in parsed) LifeData.Add(kv.Key, kv.Value);
        }

        static void ValidateLifeData(Dictionary<string,object> d,string id)
        {
            bool Pair(List<object> a,double min,double max)=>a!=null&&a.Count==2&&a[0] is double lo&&a[1] is double hi&&Finite(lo)&&Finite(hi)&&lo>=min&&hi<=max&&lo<=hi;
            if(!Pair(LA(d,"stage"),1,6))throw new FormatException(id+": invalid stage range");
            if(d.ContainsKey("month")&&!Pair(LA(d,"month"),201601,201612))throw new FormatException(id+": invalid month range");
            string when=LS(d,"when");if(Array.IndexOf(new[]{"","night","day","training","bills","money1000","cards2"},when)<0)throw new FormatException(id+": unknown condition "+when);
            var choices=LA(d,"choices");if(choices.Count!=2)throw new FormatException(id+": needs two choices");
            foreach(var raw in choices){
                if(!(raw is Dictionary<string,object> c))throw new FormatException(id+": invalid choice");
                foreach(string key in new[]{"label","labelEn","result","resultEn"})if(LS(c,key).Length==0)throw new FormatException(id+": missing choice "+key);
                if(!c.TryGetValue("effects",out var fx)||!(fx is Dictionary<string,object> effects))throw new FormatException(id+": missing effects");
                foreach(var kv in effects){
                    if(kv.Key=="flag"){if(!(kv.Value is string flag)||flag.Length==0)throw new FormatException(id+": invalid flag");}
                    else if(kv.Key=="boost"){var a=kv.Value as List<object>;if(a==null||a.Count!=2||!(a[0] is double factor)||!(a[1] is double duration)||!Finite(factor)||!Finite(duration)||factor<1||duration<=0)throw new FormatException(id+": invalid boost");}
                    else if(kv.Key=="persona"){var a=kv.Value as List<object>;if(a==null||a.Count!=2||!(a[0] is string axis)||Array.IndexOf(new[]{"温度","玩心","主见"},axis)<0||!(a[1] is double delta)||!Finite(delta))throw new FormatException(id+": invalid persona");}
                    else if(Array.IndexOf(new[]{"money","samples","noise","pause","rentIncrease"},kv.Key)>=0){if(!(kv.Value is double number)||!Finite(number))throw new FormatException(id+": invalid "+kv.Key);}
                    else throw new FormatException(id+": unknown effect "+kv.Key);
                }
            }
        }

        string LifeEffectDetail(Dictionary<string,object> effect)
        {
            var parts=new List<string>();string Num(double v)=>v.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
            foreach(string key in new[]{"money","samples","noise"}){double v=LN(effect,key);if(v==0)continue;parts.Add((v>0?"+":"−")+(key=="money"?"¥":"")+Num(Math.Abs(v))+(key=="samples"?T("条题"," samples"):key=="noise"?T("条错题"," noisy labels"):""));}
            if(LN(effect,"pause")>0)parts.Add(T("暂停训练 ","Pause training ")+Num(LN(effect,"pause"))+T("秒","s"));
            var boost=LA(effect,"boost");if(boost!=null)parts.Add(T("训练 ×","Training ×")+Num((double)boost[0])+" · "+Num((double)boost[1])+T("秒","s"));
            var persona=LA(effect,"persona");if(persona!=null){string axis=(string)persona[0];double delta=(double)persona[1];parts.Add(T(axis,axis=="温度"?"Warmth":axis=="玩心"?"Playfulness":"Opinion")+" "+(delta>=0?"+":"")+Num(delta));}
            if(LN(effect,"rentIncrease")>0)parts.Add(T("下期房租 +¥","Next rent +¥")+Num(LN(effect,"rentIncrease")));
            if(LS(effect,"flag").Length>0)parts.Add(T("记下这个选择","Remember this choice"));
            return string.Join(" · ",parts);
        }

        static bool IsLifeDataId(string id) => id != null && id.Length == 4 && id[0] == 'e' && char.IsDigit(id[1]) && char.IsDigit(id[2]) && char.IsDigit(id[3]);
        void RepairLifeData()
        {
            if (S.lifeDataSeen == null) S.lifeDataSeen = new List<string>();
            if (S.lifeFlags == null) S.lifeFlags = new List<string>();
            if(S.lifeSeenVersion==0){if(S.lifeHistory!=null)foreach(var old in S.lifeHistory)if(old!=null&&!S.lifeDataSeen.Contains(old.id))S.lifeDataSeen.Add(old.id);S.lifeSeenVersion=1;}
            S.lifeDrawCount=Math.Max(0,Math.Min(2,S.lifeDrawCount));
            if(!Finite(S.lifePauseUntil))S.lifePauseUntil=0;if(!Finite(S.lifeBoostUntil))S.lifeBoostUntil=0;if(!Finite(S.lifeBoostFactor)||S.lifeBoostFactor<1)S.lifeBoostFactor=1;
            if (S.lifeDrawStage == S.stage) return;
            S.lifeDrawStage = S.stage;
            S.lifeDrawCount = 0;
            S.lifeNext = 180 + LifeRoll() * 300;
        }

        bool LifeDataEligible(string id, IXgHost host)
        {
            if (!LifeData.TryGetValue(id, out var d) || S.lifeDataSeen.Contains(id)) return false;
            var stage = LA(d, "stage");
            if (stage != null && stage.Count >= 2 && (S.stage < (double)stage[0] || S.stage > (double)stage[1])) return false;
            if (LN(d, "minDate") > 0 && (!TodayKnown || Today < LN(d, "minDate"))) return false;
            var month = LA(d, "month");
            if (month != null && month.Count >= 2 && (!TodayKnown || Today / 100 < (double)month[0] || Today / 100 > (double)month[1])) return false;
            string needs = LS(d, "requires");
            if (needs.Length > 0 && !S.lifeFlags.Contains(needs)) return false;
            if (id == "e083" && (!(House is IXgRentLife rent) || rent.OnTimeRentPeriods < 5 || !TodayKnown || Today < 20161221)) return false;
            switch (LS(d, "when"))
            {
                case "night": return LifeSecondsOfDay < 6 * 3600;
                case "day": return LifeSecondsOfDay >= 9 * 3600 && LifeSecondsOfDay < 18 * 3600;
                case "training": return S.lifeClock - S.lifeLastTrain <= 30;
                case "bills": return House != null && House.BillsActive;
                case "money1000": return host != null && host.Money >= 1000;
                case "cards2": return host is IXgRig rig && rig.Cards != null && rig.Cards.Count >= 2;
                case "": return true;
                default: return false;
            }
        }

        XgLifeEvent LifeDataView(IXgHost host)
        {
            if (!LifeData.TryGetValue(S.lifeOpen, out var d)) return null;
            var e = new XgLifeEvent { id = S.lifeOpen, who = T(LS(d, "who"), LS(d, "whoEn")), title = T(LS(d, "title"), LS(d, "titleEn")), body = T(LS(d, "body"), LS(d, "bodyEn")) };
            int index = 0;
            foreach (var raw in LA(d, "choices"))
            {
                var c = raw as Dictionary<string, object>;
                var effect = c != null && c.TryGetValue("effects", out var fx) ? fx as Dictionary<string, object> : null;
                double money = LN(effect, "money");
                e.choices.Add(new XgLifeChoice { id = (index++).ToString(), label = T(LS(c, "label"), LS(c, "labelEn")),
                    detail = LifeEffectDetail(effect), enabled = money >= 0 || host != null && host.Money >= -money });
            }
            return e;
        }

        string ApplyLifeDataChoice(string id, string choice, IXgHost host)
        {
            if (!LifeData.TryGetValue(id, out var d) || !int.TryParse(choice, out var i)) return null;
            var options = LA(d, "choices");
            if (i < 0 || i >= options.Count || !(options[i] is Dictionary<string, object> c)) return null;
            var effect = c.TryGetValue("effects", out var fx) ? fx as Dictionary<string, object> : null;
            double money = LN(effect, "money");
            if (money < 0 && (host == null || !host.Spend(-money))) return null;
            if (money < 0) S.totalSpent -= money;
            if (money > 0 && host != null) { host.Earn(money); S.totalIncome += money; }
            string ds = Selected?.dataset ?? ForumDataset();
            if (ds != null)
            {
                int samples = (int)LN(effect, "samples");
                if (samples > 0) AddGameSamples(ds, samples);
                else if (samples < 0) SetCount(S.labels, ds, Math.Max(0, Labels(ds) + samples));
                double noise = LN(effect, "noise");
                if (noise > 0) SetCount(S.noise, ds, Noise(ds) + noise);
                foreach (var run in Runs) if (run.dataset == ds) Evaluate(run);
            }
            double pause = LN(effect, "pause");
            if (pause > 0) S.lifePauseUntil = Math.Max(S.lifePauseUntil, S.lifeClock + pause);
            var boost = LA(effect, "boost");
            if (boost != null && boost.Count == 2)
            {
                S.lifeBoostFactor = Math.Max(1, (double)boost[0]);
                S.lifeBoostUntil = S.lifeClock + Math.Max(0, (double)boost[1]);
            }
            var persona = LA(effect, "persona");
            if (persona != null && persona.Count == 2)
            {
                int axis = (string)persona[0] == "温度" ? 0 : (string)persona[0] == "玩心" ? 1 : 2;
                double delta = (double)persona[1];
                ToneCard(delta >= 0 ? High[axis] : Low[axis], true, Math.Abs(delta) / 10);
                double target = Math.Max(0, Math.Min(100, TargetAxis(axis) + delta));
                if (axis == 0) Profile.warmth = target; else if (axis == 1) Profile.play = target; else Profile.opinion = target;
            }
            double rentIncrease = LN(effect, "rentIncrease");
            if (rentIncrease > 0 && House is IXgRentLife rent) rent.RaiseRent(id == "e051" && i == 1 && rent.RentEverLate ? 200 : rentIncrease);
            if (id == "e051" && i == 1 && House is IXgRentLife late && late.RentEverLate)
                return T("老王：「上回还拖过租，这次真不能少了。」下期起加 ¥200，到了交租日再转。", "Old Wang: \"You paid late before. I cannot lower it this time.\" Rent goes up ¥200 next period, payable on rent day.");
            string flag = LS(effect, "flag");
            if (flag.Length > 0 && !S.lifeFlags.Contains(flag)) S.lifeFlags.Add(flag);
            return T(LS(c, "result"), LS(c, "resultEn"));
        }

        public bool LifeTrainingPaused => S.lifeClock < S.lifePauseUntil;
        public double LifeTrainingBoost => S.lifeClock < S.lifeBoostUntil ? Math.Max(1, S.lifeBoostFactor) : 1;
    }
}
