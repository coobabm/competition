using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class EndingLetterParagraph
    { public string id="",zh="",en=""; public string Text(bool english)=>english?en:zh; }
    [Serializable] public sealed class EndingLetterArticle
    {
        public string id="",titleZh="",titleEn="",source="public_article",sourceArticleId="",routeRecommendation="";
        public int version=1,publishedDate=20170613;
        public EndingLetterParagraph[] paragraphs=Array.Empty<EndingLetterParagraph>();
        public string Title(bool english)=>english?titleEn:titleZh;
        public bool HasParagraph(string id){foreach(var p in paragraphs)if(p.id==id)return true;return false;}
    }
    [Serializable] public sealed class EndingLetterAppendix
    { public string id="",articleId="",evidenceKey="",requiredTiming="planned",zh="",en=""; }
    [Serializable] public sealed class EndingLetterResource
    {
        public int schemaVersion=1;
        public EndingLetterArticle[] letters=Array.Empty<EndingLetterArticle>();
        public EndingLetterParagraph[] ui=Array.Empty<EndingLetterParagraph>();
        public EndingLetterAppendix[] optionalPersonalAppendices=Array.Empty<EndingLetterAppendix>();
    }
    // A verified fact projection supplied by the owner, never derived from route, YY membership or hidden state.
    public sealed class EndingLetterDisclosure
    { public string evidenceKey="",sourceRef="",timing=""; public bool authorBound,receivedByZhou; }

    public sealed class EndingLetterCatalog
    {
        public const string ResourcePath="LingGuangV05/Epilogue/ending_letters";
        public static readonly string[] ArticleIds={"zhou_open_door","zhou_copies","zhou_unpressed_button"};
        readonly Dictionary<string,EndingLetterArticle> articles=new Dictionary<string,EndingLetterArticle>(StringComparer.Ordinal);
        readonly Dictionary<string,EndingLetterParagraph> copy=new Dictionary<string,EndingLetterParagraph>(StringComparer.Ordinal);
        readonly EndingLetterAppendix[] appendices;
        public EndingLetterCatalog(EndingLetterResource data)
        {
            if(data==null||data.schemaVersion!=1||data.letters==null||data.letters.Length!=3)throw new ArgumentException("Invalid ending-letter resource schema");
            foreach(var a in data.letters)
            {
                if(a==null||Array.IndexOf(ArticleIds,a.id)<0||articles.ContainsKey(a.id)||a.version<1||a.publishedDate!=20170613
                    ||a.source!="public_article"||a.sourceArticleId!=a.id||string.IsNullOrWhiteSpace(a.titleZh)||string.IsNullOrWhiteSpace(a.titleEn)
                    ||a.paragraphs==null||a.paragraphs.Length!=9)throw new ArgumentException("Invalid public ending article");
                var ids=new HashSet<string>();
                foreach(var p in a.paragraphs)if(p==null||string.IsNullOrEmpty(p.id)||!ids.Add(p.id)||string.IsNullOrWhiteSpace(p.zh)||string.IsNullOrWhiteSpace(p.en))throw new ArgumentException("Invalid bilingual letter paragraph");
                if(!ids.Contains("greeting")||!ids.Contains("signature"))throw new ArgumentException("Letter greeting/signature missing");
                if(a.routeRecommendation!=Route(a.id))throw new ArgumentException("Invalid letter recommendation route");
                articles.Add(a.id,a);
            }
            foreach(var p in data.ui??Array.Empty<EndingLetterParagraph>())
            {
                if(p==null||string.IsNullOrEmpty(p.id)||copy.ContainsKey(p.id)||string.IsNullOrWhiteSpace(p.zh)||string.IsNullOrWhiteSpace(p.en))throw new ArgumentException("Invalid bilingual UI copy");
                copy.Add(p.id,p);
            }
            appendices=data.optionalPersonalAppendices??Array.Empty<EndingLetterAppendix>();
        }
        public EndingLetterArticle Get(string id)=>id!=null&&articles.TryGetValue(id,out var article)?article:null;
        public string Ui(string id,bool english)=>copy.TryGetValue(id,out var p)?p.Text(english):throw new ArgumentException("Unknown ending letter UI copy: "+id);
        public static string Route(string id)=>id=="zhou_open_door"?"online":id=="zhou_copies"?"public_model":id=="zhou_unpressed_button"?"local_only":"";
        public EndingLetterArticle[] Recommended(string route)
        {
            var result=new List<EndingLetterArticle>();
            if(!string.IsNullOrEmpty(route))foreach(var id in ArticleIds)if(Route(id)==route)result.Add(articles[id]);
            foreach(var id in ArticleIds)if(!result.Contains(articles[id]))result.Add(articles[id]);
            return result.ToArray(); // All three are public; recommendation is local presentation only.
        }
        public EndingLetterAppendix EligibleAppendix(string articleId,EndingLetterDisclosure fact)
        {
            if(fact==null||!fact.authorBound||!fact.receivedByZhou||string.IsNullOrEmpty(fact.sourceRef))return null;
            foreach(var p in appendices)if(p!=null&&p.articleId==articleId&&p.evidenceKey==fact.evidenceKey&&p.requiredTiming==fact.timing)return p;
            return null; // No inference of past-tense disclosure from the current release route.
        }
    }

    public enum EndingLetterView { Closed, Preview, Consent, Reading, Complete }
    public sealed class EndingLetterInput
    {
        public EndingLetterDisclosure personalDisclosure;
        public object owner;
        public long revision;
        public string articleId="",paragraphId="greeting";
        public bool saved,opened,consentShown,readOnly,completionEligible,english,reduceFx,soundEnabled;
        public EndingLetterInput Copy()
        {
            var copy=(EndingLetterInput)MemberwiseClone();
            if(personalDisclosure!=null)copy.personalDisclosure=new EndingLetterDisclosure { evidenceKey=personalDisclosure.evidenceKey, sourceRef=personalDisclosure.sourceRef, timing=personalDisclosure.timing, authorBound=personalDisclosure.authorBound, receivedByZhou=personalDisclosure.receivedByZhou };
            return copy;
        }
    }
    public sealed class EndingLetterRequest
    {
        public object owner;
        public long revision,requestId;
        public string articleId="",sourceArticleId="",paragraphId="";
        public int version;
    }
    public sealed class EndingLetterResult
    {
        public bool success;
        public object owner;
        public long revision;
        public string code="";
    }
    public sealed class EndingLetterCallbacks
    {
        public Func<object,long,bool> IsCurrent;
        public Action<EndingLetterRequest,Action<EndingLetterResult>> RequestSaveArticle,RequestOpen,ReadingPositionChanged,RequestComplete;
        public Action<EndingLetterRequest> RequestBackToDesktop, RequestPublicArchive;
    }

    // Presentation protocol only: no XgState, ChapterOneRuntime, money, friends, achievements or storage access.
    // The owner must independently validate permissions and atomically persist before reporting success.
    public sealed class EndingLetterSession
    {
        readonly EndingLetterCatalog catalog;
        EndingLetterCallbacks callbacks;
        EndingLetterInput input;
        long generation,sequence,pending;
        string pendingKind="";
        public EndingLetterView View {get;private set;}
        public string ErrorCopyId {get;private set;}="";
        public EndingLetterInput Input=>input?.Copy();
        public bool Busy=>pending!=0;
        public string PendingKind=>pendingKind;
        public event Action Changed;
        public EndingLetterSession(EndingLetterCatalog catalog){this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));}
        public void Bind(EndingLetterInput value,EndingLetterCallbacks ownerCallbacks)
        {
            generation++;pending=0;pendingKind="";ErrorCopyId="";callbacks=ownerCallbacks;
            input=value?.Copy();
            if(input==null||input.owner==null||catalog.Get(input.articleId)==null){input=null;View=EndingLetterView.Closed;Changed?.Invoke();return;}
            if(!catalog.Get(input.articleId).HasParagraph(input.paragraphId))input.paragraphId="greeting";
            View=EndingLetterView.Preview;Changed?.Invoke();
        }
        public bool Current=>input!=null&&callbacks?.IsCurrent!=null&&callbacks.IsCurrent(input.owner,input.revision);
        EndingLetterRequest Request(string paragraph="")
        {
            var article=catalog.Get(input.articleId);
            return new EndingLetterRequest{owner=input.owner,revision=input.revision,requestId=++sequence,articleId=article.id,version=article.version,sourceArticleId=article.sourceArticleId,paragraphId=paragraph};
        }
        public bool SaveArticle(bool openAfterSaving=false)
        {
            if(!Current||Busy||input.readOnly)return false;
            if(input.saved){if(openAfterSaving)OpenPrompt();return false;}
            return Begin("save",callbacks.RequestSaveArticle,()=>{input.saved=true;if(openAfterSaving)OpenPrompt();},"UI-SAVE-FAIL");
        }
        public bool OpenPrompt()
        {
            if(!Current||Busy||View==EndingLetterView.Closed)return false;
            ErrorCopyId="";
            if(input.readOnly){View=EndingLetterView.Reading;Changed?.Invoke();return true;}
            if(!input.saved)return false;
            if(input.consentShown)return OpenAccepted();
            View=EndingLetterView.Consent;Changed?.Invoke();return true;
        }
        public void CancelOpen()
        {
            if(View!=EndingLetterView.Consent||Busy)return;
            View=EndingLetterView.Preview;ErrorCopyId="";Changed?.Invoke();
        }
        public bool OpenAccepted()
        {
            if(!Current||Busy||input.readOnly||!input.saved||(View!=EndingLetterView.Consent&&!input.consentShown))return false;
            return Begin("open",callbacks.RequestOpen,()=>{input.opened=true;input.consentShown=true;View=EndingLetterView.Reading;},"UI-OPEN-FAIL");
        }
        public bool SavePosition(string paragraphId)
        {
            if(!Current||Busy||View!=EndingLetterView.Reading||paragraphId==input.paragraphId||!catalog.Get(input.articleId).HasParagraph(paragraphId))return false;
            if(input.readOnly){input.paragraphId=paragraphId;return false;} // Local viewing position only; no owner write.
            return Begin("position",callbacks.ReadingPositionChanged,()=>input.paragraphId=paragraphId,"UI-OPEN-FAIL",paragraphId);
        }
        public bool Complete()
        {
            if(!Current||Busy||View!=EndingLetterView.Reading||input.readOnly||!input.saved||!input.opened||!input.completionEligible)return false;
            return Begin("complete",callbacks.RequestComplete,()=>{View=EndingLetterView.Complete;input.readOnly=true;},"UI-FAIL");
        }
        bool Begin(string kind,Action<EndingLetterRequest,Action<EndingLetterResult>> send,Action accepted,string error,string paragraph="")
        {
            if(send==null){ErrorCopyId=error;Changed?.Invoke();return false;}
            var request=Request(paragraph);long epoch=generation;pending=request.requestId;pendingKind=kind;ErrorCopyId="";Changed?.Invoke();
            Action<EndingLetterResult> reply=result=>
            {
                if(epoch!=generation||pending!=request.requestId||input==null||!ReferenceEquals(input.owner,request.owner))return;
                // Success advances the owner's revision; compare to the returned binding, never to the obsolete one.
                if(result==null||!ReferenceEquals(result.owner,request.owner)||result.revision<request.revision||callbacks?.IsCurrent==null
                    ||!callbacks.IsCurrent(result.owner,result.revision))
                {Hide();return;}
                pending=0;pendingKind="";input.revision=result.revision;
                if(result.success){accepted();ErrorCopyId="";}else ErrorCopyId=error;
                Changed?.Invoke();
            };
            try{send(request,reply);}catch{reply(new EndingLetterResult{owner=request.owner,revision=request.revision,success=false,code="callback_exception"});}
            return true;
        }
        public void Back()
        {
            if(input==null||pendingKind=="complete")return;
            var request=Current?Request():null;var callback=callbacks?.RequestBackToDesktop;
            Hide();if(request!=null)callback?.Invoke(request);
        }
        public void Hide(){generation++;pending=0;pendingKind="";View=EndingLetterView.Closed;Changed?.Invoke();}
    }
}
