using System;
using System.Linq;
using System.Collections.Generic;
using LingGuangV05.XingGuang;

public static class BookLibraryChecks
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Local Verification/Book library purchase and reading")]
    public static void Run()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Run in isolated EditMode.");
        int n = Verify(s => UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(s)));
        UnityEngine.Debug.Log("BookLibraryChecks PASS: " + n + " assertions with actual JsonUtility; no UI/disk-save claim.");
    }
#endif
    sealed class Host : IXgHost
    {
        public double cash = 200000, trained; public int payments; public bool refuse;
        public double Money => cash; public double Compute => 1; public double VramMB => 8192; public string Blocker => null;
        public bool Spend(double n) { if (refuse || n < 0 || n > cash) return false; cash -= n; payments++; return true; }
        public void Earn(double n) { cash += n; } public void Train(double seconds) { trained += seconds; }
    }
    static int count;
    static void Check(bool value, string message) { count++; if (!value) throw new Exception("Library: " + message); }
    static void Stage(XgSim s, int n) { s.S.abilitiesEmerged = Enumerable.Range(1,n).ToList(); s.S.stage=s.S.stageVision=s.S.stageSequence=n; }
    static XgSim Lab(int stage) { var s=new XgSim(); Stage(s,stage); s.Today=20160601; s.LearningClock=()=>new XgLearningTime(20160601,123); return s; }
    static XgSim Copy(XgSim s, Func<XgState,XgState> copy) { var r=new XgSim(copy(s.S)); r.Today=s.Today; r.LearningClock=s.LearningClock; return r; }
    static void Read(XgSim s, string id, Host host)
    {
        int guard=0;
        while(!s.BookDone(id) && guard++<30) { Check(s.StartReading(id,host)==null,"start "+id); s.Tick(20,host); }
        Check(s.BookDone(id),"completion bounded "+id);
    }
    public static int Verify(Func<XgState,XgState> copy)
    {
        count=0;
        Check(XgBooks.All.Count==100 && XgBooks.Library.Count==100,"exactly one hundred books");
        Check(XgBooks.Library.Count(x=>x.language=="zh")==90 && XgBooks.Library.Count(x=>x.language=="en")==10,"90 Chinese and 10 English");
        Check(XgBooks.All.Select(b=>b.id).Distinct().Count()==100,"stable unique IDs");
        Check(XgBooks.Library.Where(x=>x.IsGuide).Select(x=>x.introZh).Distinct().Count()==92,"92 distinct original introductions");
        Check(XgBooks.Library.Where(x=>x.IsGuide).Select(x=>x.insightZh).Distinct().Count()==92,"92 distinct interpretation passages");
        Check(XgBooks.LegacyBooks.Select(x=>x.paramsK).SequenceEqual(new double[]{2,6,20,40,80,100,150,300}),"legacy parameter rewards unchanged");
        foreach(var e in XgBooks.Library)
        {
            Check(!string.IsNullOrEmpty(e.author) && !string.IsNullOrEmpty(e.authorEn) && !string.IsNullOrEmpty(e.titleEn),"bilingual bibliography "+e.id);
            Check(e.sourceUrl.StartsWith("https://") && e.publication.Length>0 && e.rights.Length>0,"provenance and rights "+e.id);
            Check(e.stage>=2 && e.stage<=5 && e.availableBy<=20160101,"era and stage "+e.id);
            Check(e.band=="short" ? e.price>=50 && e.price<=150 : e.band=="classic" ? e.price>=200 && e.price<=500 : e.band=="advanced" && e.price>=800 && e.price<=1500,"approved price band "+e.id);
            var b=XgBooks.Get(e.id); Check(b!=null && b.chapters.Length>0,"actual runtime material "+e.id);
            if(e.IsGuide)
            {
                Check(b.paramsK==0 && b.quotes.Length==0,"no invented parameter bonus or source quotation "+e.id);
                Check(b.chapters.Sum(c=>c.chars)==(e.language=="en" ? e.introEn.Length+e.insightEn.Length : e.introZh.Length+e.insightZh.Length),"only supplied text counted "+e.id);
                Check(XgBooks.Puzzles(e.id).Length==1 && XgBooks.Puzzles(e.id)[0].chapter==1,"understanding question after both passages "+e.id);
            }
        }
        var node=XgCatalog.Node(XgSim.LibraryNode); var s=Lab(1);var host=new Host();
        Check(node!=null && node.cost==500 && node.stage==2 && s.NodeCost(node)==500,"approved entry quote without stage/quarter multiplier");
        Check(!s.BuyNode(node.id,host) && host.payments==0,"stage one cannot buy entry");
        Stage(s,2);Check(s.BuyNode(node.id,host) && host.cash==199500,"real entry debit");
        Check(s.S.bookOwned.Count==0 && !s.BuyNode(node.id,host),"entry grants no books and no duplicate debit");
        Check(!s.BuyBook("shiji",host),"later book stage blocked");
        Check(s.StartReading("qianziwen",host)!=null,"unowned cannot read");
        host.cash=79;Check(!s.BuyBook("qianziwen",host) && !s.BookOwned("qianziwen"),"insufficient book funds");
        host.cash=1000;host.refuse=true;Check(!s.BuyBook("qianziwen",host) && !s.BookOwned("qianziwen"),"failed host charge has no state mutation");host.refuse=false;
        int paid=host.payments;double before=host.cash;
        s.BookPurchased+=b=>Check(s.BookOwned(b.id) && s.S.bookPurchases.Any(p=>p.bookId==b.id),"purchase callback sees committed state");
        Check(s.BuyBook("qianziwen",host) && host.cash==before-80,"actual book debit");
        Check(!s.BuyBook("qianziwen",host) && host.payments==paid+1,"repeat buy is idempotent");
        int finished=0;s.BookFinished+=b=>{finished++;Check(s.S.bookCompletions.Any(c=>c.bookId==b.id),"finish callback after commit");};
        double samples=s.ExtraSamples("longtext");Read(s,"qianziwen",host);
        Check(finished==1 && s.S.bookCompletions.Count==1,"one completion event");
        Check(s.S.chat.Any(x=>x.from=="ai" && x.text.Contains("原创导读")),"early-stage feeling goes to YY and labels guide");
        Check(s.LearningRecordsNewest().Count(x=>x.sourceKind=="book")==1,"C3 history one completion");
        var pending=s.PendingBookCompletion;long seq=pending.historySequence;
        Check(seq>0 && s.RecordBookCompletion("qianziwen","unused","unused",pending.key)==seq,"C3 stable completion key");
        double after=s.ExtraSamples("longtext");Check(after>samples,"actual reading supplied samples");
        var reloaded=Copy(s,copy);Check(reloaded.PendingBookCompletion.key==pending.key,"unacknowledged notice survives load");
        Check(reloaded.StartReading("qianziwen",host)!=null && reloaded.ExtraSamples("longtext")==after,"reload cannot reread rewards");
        Check(reloaded.AcknowledgeBookCompletion(pending.key) && !reloaded.AcknowledgeBookCompletion(pending.key),"notice acknowledgement idempotent");
        Check(Copy(reloaded,copy).PendingBookCompletion==null,"acknowledged notice does not return");
        var old=new XgState();old.books.Add(new XgScore{key="lunyu",count=2});old.bookReading="lunyu";old.bookChapter=2;old.bookClock=2;old.bookDuration=10;
        var migrated=new XgSim(copy(old));Stage(migrated,2);
        Check(migrated.OwnedBooks().Count==8 && !migrated.LibraryOpen,"old eight entitlements preserved without entry");
        Check(migrated.ChaptersRead("lunyu")==2 && migrated.Reading && migrated.S.bookClock==2,"old chapter and active bar preserved");
        Check(migrated.S.bookCompletions.Count==0 && !migrated.LearningRecordsNewest().Any(x=>x.sourceKind=="book"),"migration invents no completion or timestamp");
        Check(Copy(migrated,copy).OwnedBooks().Count==8,"migration idempotent");
        var oldDone=new XgState();oldDone.books.Add(new XgScore{key="sanzijing",count=4,value=0});var unknown=new XgSim(copy(oldDone));
        Check(unknown.BookDoneDay(XgBooks.Get("sanzijing"))==0 && unknown.PendingBookCompletion==null,"unknown historical completion remains undated and silent");
        var dated=Lab(5);dated.S.unlocked.Add(node.id);dated.Today=18000101;
        Check(!dated.BuyBook("santi",new Host()),"cannot buy before conservative publication threshold");
        var unknownDate=new XgSim();Stage(unknownDate,5);unknownDate.S.unlocked.Add(node.id);
        Check(!unknownDate.BuyBook("santi",new Host()),"unknown game date cannot unlock dated commerce");
        var discounted=Lab(2);discounted.GrantRelic("coupon");Check(discounted.NodeCost(node)==450,"legitimate technology coupon preserved");
        Check(!XgCatalog.Nodes.Any(n=>n.id!=node.id && (n.parent==node.id || n.needs.Contains(node.id))),"library not a main-line prerequisite");
        foreach(var e in XgBooks.Library.Where(e=>e.IsGuide))
        {
            var actual=Lab(e.stage);var wallet=new Host();Check(actual.BuyNode(node.id,wallet) && actual.BuyBook(e.id,wallet),"real purchase "+e.id);
            int events=0;actual.BookFinished+=b=>events++;Read(actual,e.id,wallet);
            Check(events==1 && actual.S.bookCompletions.Count==1 && actual.S.bookPurchases.Count==1,"single book transaction "+e.id);
            var loaded=Copy(actual,copy);Check(loaded.BookOwned(e.id) && loaded.BookDone(e.id),"owned/progress load "+e.id);
            Check(loaded.LearningRecordsNewest().Count(r=>r.sourceKind=="book")==1,"stable learning completion "+e.id);
            Stage(loaded,2);loaded.English=true;
            Check(loaded.BookNote(XgBooks.Get(e.id)).Contains(e.titleEn),"English early note uses localized title "+e.id);
            Check(XgBooks.Get(e.id).chapters.All(ch=>ch.tag==""),"no invented cross-book topic "+e.id);
            Stage(actual,4);var puzzle=XgBooks.Puzzles(e.id)[0];actual.AskBookPuzzle(puzzle);
            Check(actual.BookPuzzlePending?.id==puzzle.id,"question available in YY "+e.id);
            int lines=actual.S.chat.Count;actual.AskBookPuzzle(puzzle);Check(actual.S.chat.Count==lines,"duplicate ask ignored "+e.id);
            double reward=actual.ExtraSamples("longtext");Check(actual.AnswerBookPuzzle(puzzle.keys[0])!=null,"answer existing YY path "+e.id);
            Check(actual.AnswerBookPuzzle(puzzle.keys[0])==null && actual.ExtraSamples("longtext")==reward,"duplicate answer never rewards "+e.id);
            actual.AskBookPuzzle(puzzle);Check(actual.BookPuzzlePending==null,"answered question not reoffered "+e.id);
        }
        return count;
    }
}
