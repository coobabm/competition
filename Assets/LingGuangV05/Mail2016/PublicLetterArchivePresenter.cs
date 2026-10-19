using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Mail
{
    // A public catalogue projection, not a mailbox delivery or completion permission.
    public sealed class PublicLetterArchiveInput
    {
        public string route = "", recommendedId = "";
        public string[] savedArticleIds = Array.Empty<string>();
        public bool english, completed;
    }

    public sealed class PublicLetterArchivePresenter : MonoBehaviour
    {
        static readonly Color Paper = new Color32(250,250,247,255), Ink = new Color32(34,40,52,255), Muted = new Color32(80,93,108,255);
        EndingLetterCatalog catalog;
        PublicLetterArchiveInput input;
        Func<bool> current;
        Action<string> saveArticle;
        Action continueEnding, closed;
        RectTransform root, list, detail, content;
        TMP_Text heading, note, source, status;
        TMP_FontAsset font;
        WindowManager window;
        ScrollRect scroll;
        Button back, save, continuing;
        readonly List<Button> articleButtons = new List<Button>();
        readonly List<TMP_Text> paragraphs = new List<TMP_Text>();
        string selected = "";
        bool failed;
        int bindingEpoch;
        public RectTransform Root => root;
        public bool Visible => root != null && root.gameObject.activeInHierarchy;
        public string SelectedArticleId => selected;
        public int SavedCount => input?.savedArticleIds?.Length ?? 0;
        public bool Available => input != null && current != null && current();
        string T(string zh,string en) => input != null && input.english ? en : zh;

        public static PublicLetterArchivePresenter Install(RectTransform host, TMP_FontAsset font, WindowManager window)
        {
            var p = host.GetComponent<PublicLetterArchivePresenter>() ?? host.gameObject.AddComponent<PublicLetterArchivePresenter>();
            if (p.root != null) return p;
            var asset = Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath);
            p.catalog = new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(asset.text));
            p.font = font; p.window = window; p.Build(host);
            if (window != null) { window.onClose.AddListener(p.Hide); window.onMinimize.AddListener(p.Hide); }
            return p;
        }
        public void Bind(PublicLetterArchiveInput value, Func<bool> isCurrent, Action<string> save,
            Action continueCurrent, Action onClosed)
        {
            bindingEpoch++;
            input = value; current = isCurrent; saveArticle = save; continueEnding = continueCurrent; closed = onClosed;
            failed = false;
            if (input == null) { selected = ""; Hide(); return; }
            Render();
        }
        public void SetLanguage(bool english)
        {
            if (input == null || input.english == english) return;
            input.english = english; float position = scroll.verticalNormalizedPosition;
            Render(); Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = position;
        }
        public void Show()
        {
            if (!Available) return;
            root.gameObject.SetActive(true); root.SetAsLastSibling(); Render();
        }
        public void Hide() { if (root != null) root.gameObject.SetActive(false); }
        public void ReportSaveFailure() { failed = true; Render(); }
        void SelectArticle(string id)
        {
            if (!Available || catalog.Get(id) == null) return;
            selected = id; failed = false; Render(); Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        }
        bool Saved(string id) => input?.savedArticleIds != null && Array.IndexOf(input.savedArticleIds,id) >= 0;
        void Back()
        {
            if (selected.Length > 0) { selected = ""; failed = false; Render(); return; }
            bool valid = Available; Hide(); if (valid) closed?.Invoke();
        }
        void Build(RectTransform host)
        {
            root = Rect("PublicLetterArchive",host,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); Fill(root,Paper);
            heading = Text(Rect("Title",root,new Vector2(0,1),Vector2.one,new Vector2(20,-58),new Vector2(-20,-12)),23,Ink);
            note = Text(Rect("PublicNotice",root,new Vector2(0,1),Vector2.one,new Vector2(20,-118),new Vector2(-20,-64)),13,Muted);
            list = Rect("Articles",root,Vector2.zero,Vector2.one,new Vector2(20,64),new Vector2(-20,-126));
            for (int i=0;i<3;i++)
            {
                int index=i;
                var b=Button("PublicArticle"+i,list,()=>
                { if (Available) SelectArticle(catalog.Recommended(input.route)[index].id); });
                var r=(RectTransform)b.transform;r.anchorMin=new Vector2(0,1-(i+1)/3f);r.anchorMax=new Vector2(1,1-i/3f);r.offsetMin=new Vector2(0,6);r.offsetMax=new Vector2(0,-6);
                articleButtons.Add(b);
            }
            detail=Rect("ArticleDetail",root,Vector2.zero,Vector2.one,new Vector2(20,154),new Vector2(-20,-126));
            scroll=detail.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect("Viewport",detail,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Fill(viewport,Paper);viewport.gameObject.AddComponent<RectMask2D>();
            content=Rect("Paragraphs",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(8,8,8,18);layout.spacing=16;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            for(int i=0;i<9;i++)paragraphs.Add(Text(Rect("Paragraph"+i,content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),18,Ink));
            source=note;
            status=Text(Rect("SaveStatus",root,Vector2.zero,new Vector2(1,0),new Vector2(24,118),new Vector2(-24,150)),12,Muted);
            save=Button("SavePublicArticle",root,()=>{if(Available&&selected.Length>0&&!Saved(selected))saveArticle?.Invoke(selected);});
            continuing=Button("ContinueRecommendedLetter",root,()=>{if(Available&&selected==input.recommendedId)continueEnding?.Invoke();});
            back=Button("BackFromPublicLetters",root,Back);
            Place(back,8,48);Place(save,58,108);Place(continuing,58,108);
            root.gameObject.SetActive(false);
        }
        void Render()
        {
            if(input==null||root==null)return;
            bool reading=selected.Length>0;var article=reading?catalog.Get(selected):null;
            heading.text=reading?article.Title(input.english):T("老周的公开来信","Lao Zhou's public letters");
            source.text=reading?T("公开文章 · 2017-06-13 · 阅读不完成章节。","Public article · 2017-06-13 · Reading does not complete the chapter.")
                :T("同一天的三篇公开文章。本局推荐只影响顺序；收藏保存在本地，不发送地址或通知作者。","Three public articles from the same day. Your recommendation changes only their order. Bookmarks stay local; no address or choice is sent to the author.");
            list.gameObject.SetActive(!reading);detail.gameObject.SetActive(reading);
            var ordered=catalog.Recommended(input.route);
            for(int i=0;i<3;i++)
            {
                var a=ordered[i];articleButtons[i].name="PublicArticle_"+a.id;
                string articleId=a.id;BindClick(articleButtons[i],()=>SelectArticle(articleId));
                articleButtons[i].GetComponentInChildren<TMP_Text>().text=a.Title(input.english)+"\n"+
                    (a.id==input.recommendedId?T("本局推荐 · ","Recommended for this ending · "):"")+
                    (Saved(a.id)?T("已保存到本地档案","Saved in local archive"):T("公开阅读","Read public article"));
            }
            if(reading)for(int i=0;i<9;i++)
            {
                paragraphs[i].name="PublicParagraph_"+article.paragraphs[i].id;paragraphs[i].text=article.paragraphs[i].Text(input.english);
                bool replyEnd=article.paragraphs[i].id=="signature";
                var notice=paragraphs[i].rectTransform.Find("AiContentNotice");
                if(replyEnd&&notice==null)notice=LingGuangV05.Runtime.AiContentNotice.CreateFooter(paragraphs[i].rectTransform,font,Muted).transform.parent;
                if(notice!=null){notice.gameObject.SetActive(replyEnd);notice.GetComponentInChildren<TMP_Text>(true).text=LingGuangV05.Runtime.AiContentNotice.Footer(input.english);}
                paragraphs[i].margin=new Vector4(0,0,0,replyEnd?28:0);
            }
            bool recommended=reading&&selected==input.recommendedId;
            // Recommended reading retains its existing, separate ending-letter transaction and consent.
            // Other public articles never receive a completion button or a completion callback.
            save.gameObject.SetActive(reading&&!recommended);
            string visibleId=selected;
            BindClick(save,()=>{if(visibleId.Length>0&&!Saved(visibleId))saveArticle?.Invoke(visibleId);});
            BindClick(continuing,()=>{if(visibleId==input.recommendedId)continueEnding?.Invoke();});
            BindClick(back,Back);
            save.interactable=Available&&!Saved(selected);
            continuing.gameObject.SetActive(recommended);continuing.interactable=Available;
            save.GetComponentInChildren<TMP_Text>().text=Saved(selected)?T("已保存到本地档案","Saved in local archive"):T("保存到本地档案","Save to local archive");
            continuing.GetComponentInChildren<TMP_Text>().text=input.completed?T("回看本局已存来信","Read this ending's saved letter"):T("继续本局来信","Continue this ending letter");
            back.GetComponentInChildren<TMP_Text>().text=reading?T("返回三封来信","Back to all three letters"):T("返回邮箱","Back to mailbox");
            status.gameObject.SetActive(reading);
            status.text=failed?T("没有保存成功，本篇尚未存档。请重试。","Saving failed. This article was not archived. Please retry.")
                :recommended?T("通关操作仍在本局来信中，公开阅读不会触发。","Completion stays in this ending's letter; public reading never triggers it.")
                :Saved(selected)?T("本地收藏，不是作者私信；未改变本局来信的已读状态。","A local bookmark, not private mail; your ending letter's read status is unchanged.")
                :T("仅保存文章来源，不通知作者，不完成章节。","Saves this public source locally; does not notify its author or complete the chapter.");
        }
        void Update()
        {
            if(!Visible)return;
            if(!Available){Hide();return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)Back();
        }
        void OnDestroy(){if(window!=null){window.onClose.RemoveListener(Hide);window.onMinimize.RemoveListener(Hide);}if(root!=null)Destroy(root.gameObject);}
        Button Button(string name,RectTransform parent,Action action)
        {
            var r=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var bg=Fill(r,new Color32(223,233,245,255));
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=bg;BindClick(b,action);
            var text=Text(Rect("Text",r,Vector2.zero,Vector2.one,new Vector2(10,4),new Vector2(-10,-4)),15,Ink);text.alignment=TextAlignmentOptions.Center;
            if(window!=null)r.gameObject.AddComponent<ChapterOneWindowFocus>().Window=window;
            return b;
        }
        void BindClick(Button button,Action action)
        {
            int epoch=bindingEpoch;button.onClick.RemoveAllListeners();
            button.onClick.AddListener(()=>{if(epoch==bindingEpoch&&Available)action();});
        }
        static void Place(Button b,float bottom,float top){var r=(RectTransform)b.transform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,0);r.offsetMin=new Vector2(20,bottom);r.offsetMax=new Vector2(-20,top);}
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 a,Vector2 b){var g=new GameObject(name,typeof(RectTransform));g.layer=parent.gameObject.layer;var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=a;r.offsetMax=b;return r;}
        static Image Fill(RectTransform r,Color c){var i=r.gameObject.AddComponent<Image>();i.color=c;return i;}
        TMP_Text Text(RectTransform r,float size,Color color){var t=r.gameObject.AddComponent<TextMeshProUGUI>();if(font!=null)t.font=font;t.fontSize=size;t.color=color;t.richText=false;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;return t;}
    }
}
