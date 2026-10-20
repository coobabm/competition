using System;
using System.Linq;
using LingGuangV05.XingGuang;

public static class EndingLetterChecks
{
    static int count;
    static void Check(bool value,string label){count++;if(!value)throw new Exception("EndingLetterChecks: "+label);}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Local Verification/Ending letters catalogue and protocol")]
    public static void Run()
    {
        var resource=UnityEngine.Resources.Load<UnityEngine.TextAsset>(EndingLetterCatalog.ResourcePath);
        if(resource==null)throw new Exception("Ending letter resource missing");
        int assertions=Verify(UnityEngine.JsonUtility.FromJson<EndingLetterResource>(resource.text));
        UnityEngine.Debug.Log("EndingLetterChecks PASS: "+assertions+" content/protocol assertions; no real save, UI or completed-playthrough claim.");
    }
#endif
    public static int Verify(EndingLetterResource data)
    {
        count=0;var catalog=new EndingLetterCatalog(data);
        foreach(var id in EndingLetterCatalog.ArticleIds)
        {
            var article=catalog.Get(id);Check(article!=null&&article.source=="public_article"&&article.sourceArticleId==id,"stable public source "+id);
            Check(article.paragraphs.Length==9&&article.paragraphs[0].id=="greeting"&&article.paragraphs[8].id=="signature","full letter and distinct signature "+id);
            Check(article.paragraphs.Select(p=>p.id).Distinct().Count()==9,"stable bilingual paragraph identity "+id);
            foreach(var p in article.paragraphs)Check(p.zh.Length>0&&p.en.Length>0,"paired complete paragraph "+p.id);
            Check(catalog.Recommended(article.routeRecommendation)[0].id==id&&catalog.Recommended(article.routeRecommendation).Length==3,"local ordering never excludes public articles");
        }
        Check(catalog.Recommended("").Select(x=>x.id).SequenceEqual(EndingLetterCatalog.ArticleIds),"unknown legacy route remains unclassified");
        Check(catalog.Recommended("E1")[0].id=="zhou_open_door"&&EndingLetterCatalog.Route("E1")=="","old ending IDs not mapped to new routes");
        Check(catalog.EligibleAppendix("zhou_open_door",null)==null,"no disclosure means no private insertion");
        var fact=new EndingLetterDisclosure{evidenceKey="disclosed_online",timing="planned"};
        Check(catalog.EligibleAppendix("zhou_open_door",fact)==null,"fact key alone is insufficient");
        fact.authorBound=fact.receivedByZhou=true;Check(catalog.EligibleAppendix("zhou_open_door",fact)==null,"source reference required");
        fact.sourceRef="test:explicit-message";Check(catalog.EligibleAppendix("zhou_open_door",fact)?.id=="LP1","eligible explicit evidence projection");
        fact.timing="already_done";Check(catalog.EligibleAppendix("zhou_open_door",fact)==null,"past-tense claim not manufactured from route");
        foreach(string key in new[]{"UI-SAVE","UI-WARN","UI-CONFIRM","UI-READ","UI-LATER","UI-FINISH","UI-BACK","UI-FAIL","UI-REPLAY"})
        {Check(catalog.Ui(key,false).Length>0&&catalog.Ui(key,true).Length>0,"mandatory bilingual UI "+key);}

        object owner=new object();long revision=7;Action<EndingLetterResult> response=null;EndingLetterRequest request=null;
        int saves=0,opens=0,positions=0,completions=0,backs=0;
        var actions=new EndingLetterCallbacks
        {
            IsCurrent=(o,r)=>ReferenceEquals(o,owner)&&r==revision,
            RequestSaveArticle=(r,done)=>{saves++;request=r;response=done;},
            RequestOpen=(r,done)=>{opens++;request=r;response=done;},
            ReadingPositionChanged=(r,done)=>{positions++;request=r;response=done;},
            RequestComplete=(r,done)=>{completions++;request=r;response=done;},
            RequestBackToDesktop=r=>backs++
        };
        var session=new EndingLetterSession(catalog);
        var input=new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_open_door",completionEligible=true};session.Bind(input,actions);
        Check(!session.OpenPrompt()&&!session.Complete()&&saves==0,"binding/selection neither saves nor opens nor completes");
        Check(session.SaveArticle(true)&&saves==1&&session.Busy&&!session.Input.saved,"explicit save waits for durable owner reply");
        Check(!session.SaveArticle()&&!session.OpenPrompt(),"duplicate save/open blocked during transaction");
        response(new EndingLetterResult{owner=owner,revision=revision,success=false});
        Check(!session.Input.saved&&session.ErrorCopyId=="UI-SAVE-FAIL"&&!session.Busy,"save failure never produces unread/saved state");
        session.SaveArticle(true);revision++;response(new EndingLetterResult{owner=owner,revision=revision,success=true});
        Check(session.Input.saved&&session.View==EndingLetterView.Consent&&completions==0,"save success prompts consent without completing");
        session.CancelOpen();Check(session.View==EndingLetterView.Preview&&opens==0,"cancel does not open or mutate owner");
        session.OpenPrompt();session.OpenAccepted();Check(opens==1&&!session.Input.opened,"read-first requests persisted opening");
        revision++;response(new EndingLetterResult{owner=owner,revision=revision,success=true});
        Check(session.View==EndingLetterView.Reading&&session.Input.opened&&session.Input.consentShown,"opening has separate persisted state");
        Check(!session.SavePosition("unknown")&&!session.SavePosition("greeting"),"only changed stable paragraph IDs save");
        session.SavePosition("p03");Check(request.paragraphId=="p03"&&positions==1&&!session.Complete(),"position request owns its revision while pending");
        revision++;response(new EndingLetterResult{owner=owner,revision=revision,success=true});
        Check(session.Input.paragraphId=="p03"&&session.Input.revision==revision,"position response advances binding revision");
        Check(session.Complete()&&completions==1&&!session.Complete(),"only explicit final action requests completion once");
        response(new EndingLetterResult{owner=owner,revision=revision,success=false});
        Check(session.View==EndingLetterView.Reading&&!session.Input.readOnly&&session.ErrorCopyId=="UI-FAIL","failed completion stays on page and is not completed");
        session.Complete();revision++;var finish=response;finish(new EndingLetterResult{owner=owner,revision=revision,success=true});
        Check(session.View==EndingLetterView.Complete&&session.Input.readOnly,"successful owner receipt seals presenter");
        finish(new EndingLetterResult{owner=owner,revision=revision,success=true});Check(completions==2&&!session.Complete(),"duplicate completion response cannot cause another request");

        input=session.Input;input.paragraphId="p03";input.readOnly=true;session.Bind(input,actions);session.OpenPrompt();
        Check(session.View==EndingLetterView.Reading&&!session.SaveArticle()&&!session.SavePosition("p04")&&!session.Complete(),"archive read has zero write paths");
        Check(session.Input.paragraphId=="p04","archive keeps local paragraph for language changes without persisting");
        Check(saves==2&&positions==1&&completions==2,"archive neither rewards nor writes");
        session.Back();Check(session.View==EndingLetterView.Closed&&backs==1,"read-only back is navigation only");

        input=new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_copies",saved=true,opened=true,consentShown=true,completionEligible=true,paragraphId="p02"};
        session.Bind(input,actions);session.OpenPrompt();var oldOpen=response;
        object second=new object();owner=second;revision=1;session.Bind(new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_copies"},actions);
        oldOpen(new EndingLetterResult{owner=input.owner,revision=input.revision+1,success=true});
        Check(!session.Input.opened&&session.View==EndingLetterView.Preview,"old-save asynchronous opening cannot alter new binding");
        session.SaveArticle();var hiddenResponse=response;session.Hide();revision++;hiddenResponse(new EndingLetterResult{owner=owner,revision=revision,success=true});
        Check(session.View==EndingLetterView.Closed&&!session.Input.saved,"hidden view ignores late save response");
        input=new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_unpressed_button",saved=true};session.Bind(input,actions);
        revision++;Check(!session.OpenPrompt()&&!session.SaveArticle(),"external same-owner revision invalidates stale controls");
        session.Bind(new EndingLetterInput{owner=owner,revision=revision,articleId="bad"},actions);Check(session.View==EndingLetterView.Closed&&session.Input==null,"unknown article is fail-closed");
        return count;
    }
}
