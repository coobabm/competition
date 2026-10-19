#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Caller supplies an isolated active native Mail host and the existing actual LCD click helper.
// Save callbacks are explicit fixtures. Real SaveNow rollback is tested separately in core/bridge integration.
public static class PublicLetterArchiveUiChecks
{
    static void Need(bool value,string label){if(!value)throw new InvalidOperationException("PublicLetterArchiveUiChecks: "+label);}
    static Button Named(PublicLetterArchivePresenter p,string name)=>p.Root.GetComponentsInChildren<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
    public static IEnumerator Frames(RectTransform host,TMP_FontAsset font,WindowManager window,Action<RectTransform> click,Action<string> capture)
    {
        var p=PublicLetterArchivePresenter.Install(host,font,window);
        bool current=true,fail=true,saved=false;int writes=0,continuations=0;
        Action bind=null;
        bind=()=>p.Bind(new PublicLetterArchiveInput{route="local_only",recommendedId="zhou_unpressed_button",savedArticleIds=saved?new[]{"zhou_copies"}:Array.Empty<string>()},
            ()=>current,id=>{Need(id=="zhou_copies","only explicitly selected article saved");writes++;if(fail){p.ReportSaveFailure();return;}saved=true;bind();},()=>continuations++,()=>{});
        bind();p.Show();yield return null;Canvas.ForceUpdateCanvases();
        var cards=p.Root.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("PublicArticle_")).OrderByDescending(b=>((RectTransform)b.transform).anchorMax.y).ToArray();
        Need(cards.Length==3&&cards[0].name=="PublicArticle_zhou_unpressed_button","all three titles present, recommendation first");
        foreach(var id in new[]{"zhou_open_door","zhou_copies","zhou_unpressed_button"})
        {
            click((RectTransform)Named(p,"PublicArticle_"+id).transform);yield return null;
            Need(p.Root.GetComponentsInChildren<TMP_Text>().Count(t=>t.name.StartsWith("PublicParagraph_"))==9,"all nine authored paragraphs shown");
            Need(!p.Root.GetComponentsInChildren<Button>().Any(b=>b.name=="CompleteEnding"),"public reading has no completion button");
            Need(writes==0&&continuations==0,"opening public article never writes or continues ending");
            p.SetLanguage(true);yield return null;
            Need(p.SelectedArticleId==id,"language switch retains article identity");
            capture("public-letter-"+id+"-en");
            click((RectTransform)Named(p,"BackFromPublicLetters").transform);yield return null;
        }
        click((RectTransform)Named(p,"PublicArticle_zhou_copies").transform);yield return null;
        click((RectTransform)Named(p,"SavePublicArticle").transform);yield return null;
        Need(writes==1&&!saved&&Named(p,"SavePublicArticle").IsInteractable(),"failure remains unsaved and retryable");
        Need(p.Root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="SaveStatus"&&t.text.Contains("Saving failed")),"explicit save failure visible");
        fail=false;click((RectTransform)Named(p,"SavePublicArticle").transform);yield return null;
        Need(saved&&writes==2&&!Named(p,"SavePublicArticle").IsInteractable(),"successful bookmark disables duplicate save");
        Need(continuations==0,"bookmark never enters completion flow");
        current=false;click((RectTransform)Named(p,"BackFromPublicLetters").transform);yield return null;
        Need(writes==2&&!p.Visible,"stale save identity dismisses panel with no write");
        p.Bind(null,null,null,null,null);
    }
}
#endif
