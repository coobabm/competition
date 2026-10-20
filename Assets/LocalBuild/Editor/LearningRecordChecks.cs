using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.XingGuang;

public static class LearningRecordChecks
{
    static int checks;
    static void Need(bool yes,string message) { checks++; if(!yes)throw new Exception(message); }
    public static int Run(Func<XgState,XgState> clone)
    {
        checks=0;
        var old=new XgState();old.askedLog.Add(new XgAskedEntry{question="legacy",bankId="sense",at=90000});old.askedLog.Add(new XgAskedEntry{id=80,question="old indexed",bankId="sense",gameDate=20160229,gameSecondsOfDay=321});
        var sim=new XgSim(old);sim.RepairLearningRecords();
        Need(old.askedLog[0].id==81&&old.learningRecordSerial==81,"legacy IDs assigned above max existing");
        Need(sim.LearningTimestamp(old.askedLog[0])=="未记录","legacy elapsed clock never becomes a date");
        Need(sim.LearningTimestamp(old.askedLog[1])=="2016-02-29 00:05:21","exact game timestamp retained");
        sim.LearningClock=()=>new XgLearningTime(20161019,3601);
        sim.RecordAsked("sense","new one","player no","AI yes");sim.RecordAsked("sense","new two","player yes","AI no");
        var page=sim.QueryLearningRecords(new XgLearningQuery{limit=1});
        Need(page.records.Count==1&&page.total==4&&page.HasMore&&page.records[0].question=="new two","newest-first timestamp and stable equal-time serial");
        Need(sim.LearningTimestamp(page.records[0])=="2016-10-19 01:00:01","host game time only");
        var index=typeof(XgSim).GetField("learningSorted",BindingFlags.Instance|BindingFlags.NonPublic);var cached=index.GetValue(sim);
        sim.QueryLearningRecords(new XgLearningQuery{offset=1,limit=1});Need(ReferenceEquals(cached,index.GetValue(sim)),"second page reuses sorted index");
        sim.RecordAsked("train.mnist","third","label","model");Need(ReferenceEquals(cached,index.GetValue(sim)),"append inserts in existing sorted index");
        Need(sim.QueryLearningRecords(new XgLearningQuery{sourceKind="train",sourceId="train.mnist"}).total==1,"source kind and id filter");
        Need(sim.QueryLearningRecords(new XgLearningQuery{text="AI NO"}).total==1,"case insensitive answer search");
        var source=sim.QueryLearningRecords(new XgLearningQuery{text="new one"}).records[0];
        long before=sim.S.learningRecordSerial;double income=sim.S.totalIncome,spent=sim.S.totalSpent;
        long retest=sim.RecordLearningRetest(source.id,"new actual answer","delivery1");
        Need(retest>before&&sim.RecordLearningRetest(source.id,"changed duplicate","delivery1")==retest,"delivery key deduplicates retest");
        Need(sim.LearningRecord(retest).aiAnswer=="new actual answer"&&sim.LearningRecord(source.id).aiAnswer=="AI yes","retest preserves original answer and stores actual new answer");
        Need(sim.S.totalIncome==income&&sim.S.totalSpent==spent,"retest never grants money");
        long correction=sim.RecordLearningCorrection(source.id,"player explicitly corrected","correction1");
        Need(sim.LearningRecord(correction).correction=="player explicitly corrected","explicit correction recorded separately");
        Need(sim.RecordLearningCorrection(source.id,"duplicate","correction1")==correction,"correction key deduplicated");
        var safe=sim.LearningRetestInput(source.id);
        Need(safe.question==source.question,"retest input preserves original prompt");
        Need(typeof(XgLearningRetest).GetFields().All(field=>field.Name!="aiAnswer"&&field.Name!="playerAnswer"&&field.Name!="truth"&&field.Name!="correction"),"model input DTO excludes labels/answers/corrections");
        var picture=new XgCard{teachingImageKind="grid",teachingImageKeys=new[]{1,3,2},teachingPosition="original-strokes"};
        sim.RecordTeachingQuestion("图题","Image question",true,false,"explanation","explanation","mnist",picture,"clicked 1","model 3","explicit correction");
        picture.teachingImageKeys[0]=99;picture.teachingPosition="changed";
        var note=sim.S.teachingNotebook.Last();var record=sim.LearningRecord(note.learningRecordId);
        Need(record.teachingNoteId==note.id&&sim.LearningRetestInput(record.id).requiresImage,"durable record points to exact image snapshot");
        Need(note.teachingImageKeys[0]==1&&note.teachingPosition=="original-strokes","image and pen position cloned");
        Need(record.aiAnswer=="model 3"&&record.playerAnswer=="clicked 1"&&record.correction=="explicit correction","real model/player/correction values remain distinct");
        Need(sim.QueryLearningRecords(new XgLearningQuery{sourceId="mnist",textOnly=true}).total==0,"text-only random selection excludes image questions");
        long book=sim.RecordBookCompletion("book1","书一","Book One","book.complete:book1");
        Need(book>0&&sim.RecordBookCompletion("book1","other","Other","book.complete:book1")==book,"book completion is idempotent");
        Need(sim.LearningRetestInput(book)==null,"book completion is not a quiz input");
        Need(sim.RecordBookCompletion("book1","x","x","")==0,"empty completion key rejected");
        var loaded=new XgSim(clone(sim.S));loaded.RepairLearningRecords();
        Need(loaded.RecordBookCompletion("book1","repeat","Repeat","book.complete:book1")==book,"book idempotence survives JSON reload");
        Need(loaded.RecordLearningRetest(source.id,"repeat","delivery1")==retest,"retest idempotence survives reload");
        Need(loaded.LearningTeachingNote(note.id).teachingImageKeys[0]==1,"image ref survives reload");
        Need(loaded.S.askedLog.Select(x=>x.id).SequenceEqual(sim.S.askedLog.Select(x=>x.id)),"stable record serial survives reload");
        long max=loaded.S.askedLog.Max(x=>x.id);loaded.RecordAsked("sense","without clock","","");
        Need(loaded.S.askedLog.Last().id==max+1&&loaded.S.askedLog.Last().gameDate==0,"new post-reload record uses max+1, absent clock stays unknown");
        loaded.LearningClock=()=>new XgLearningTime(20160230,1);loaded.RecordAsked("sense","bad date","","");
        Need(loaded.LearningTimestamp(loaded.S.askedLog.Last())=="未记录","invalid calendar dates marked unknown");
        var many=new XgSim();many.LearningClock=()=>new XgLearningTime(20160101,0);
        for(int i=0;i<350;i++)many.RecordTeachingQuestion("Q"+i,"Q"+i,true,true,"","","sense");
        Need(many.S.teachingNotebook.Count==350&&many.S.askedLog.Count==350,"no 200/256 permanent deletion");
        var notesPage=many.QueryTeachingNotes(50,50);
        Need(notesPage.total==350&&notesPage.records.Count==50&&notesPage.HasMore&&notesPage.records[0].text=="Q299","snapshot pages expose all notes newest first");
        var notesIndex=typeof(XgSim).GetField("learningNotesNewest",BindingFlags.Instance|BindingFlags.NonPublic);var notesCached=notesIndex.GetValue(many);
        many.QueryTeachingNotes(100,50);Need(ReferenceEquals(notesCached,notesIndex.GetValue(many)),"snapshot paging reuses ordered index");
        var all=new HashSet<long>();for(int offset=0;offset<350;offset+=50){var slice=many.QueryLearningRecords(new XgLearningQuery{offset=offset,limit=50});foreach(var row in slice.records)Need(all.Add(row.id),"no duplicate across pages");}
        Need(all.Count==350,"all retained records reachable by paging");
        many.RecordTeachingQuestion("Q350","Q350",true,true,"","","sense");
        Need(ReferenceEquals(notesCached,notesIndex.GetValue(many))&&many.QueryTeachingNotes(0,1).records[0].text=="Q350","new snapshot inserted into existing ordered index");
        var noteCount = typeof(XgSim).GetField("learningNotesCount",BindingFlags.Instance|BindingFlags.NonPublic);
        var noteDictionary = typeof(XgSim).GetField("learningNotesById",BindingFlags.Instance|BindingFlags.NonPublic);
        var cachedDictionary = noteDictionary.GetValue(many);
        for(int i=351;i<551;i++)
        {
            many.RecordTeachingQuestion("Q"+i,"Q"+i,true,true,"","","sense");
            var newest=many.S.teachingNotebook.Last();
            Need((int)noteCount.GetValue(many)==many.S.teachingNotebook.Count&&ReferenceEquals(notesCached,notesIndex.GetValue(many))&&ReferenceEquals(cachedDictionary,noteDictionary.GetValue(many)),"consecutive append updates note count and reuses both indexes "+i);
            Need(ReferenceEquals(many.LearningTeachingNote(newest.id),newest)&&many.QueryTeachingNotes(0,1).records[0].id==newest.id,"consecutive append lookup and sorted head correct "+i);
        }
        many.LearningClock=()=>new XgLearningTime(20151231,0);
        many.RecordTeachingQuestion("backdated","backdated",true,true,"","","sense");
        Need(ReferenceEquals(notesCached,notesIndex.GetValue(many))&&many.QueryTeachingNotes(0,1).records[0].text=="Q550"&&many.QueryTeachingNotes(551,1).records[0].text=="backdated","binary note insertion uses linked record date before ordering");
        for(int i=0;i<70;i++)many.RegisterYyReply("raw "+i,"delivered "+i);
        Need(many.S.yyReplies.Count==50&&many.QueryLearningRecords(new XgLearningQuery{sourceKind="chat"}).total==70,"short YY cache capped while durable reply history complete");
        many.English=true;Need(many.LearningTimestamp(new XgAskedEntry())=="Not recorded","unknown date English label");
        return checks;
    }
}
