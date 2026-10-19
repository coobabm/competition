using System;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Mail
{
    public sealed partial class MailInboxView
    {
        PublicLetterArchivePresenter publicLetters;
        Button publicLettersEntry;
        Action requestPublicLetters;
        public PublicLetterArchivePresenter PublicLetters => publicLetters;

        public void BindPublicLetters(PublicLetterArchiveInput input, Func<bool> isCurrent, Action<string> save,
            Action continueCurrent, Action close, Action show)
        {
            EnsureReady(); requestPublicLetters=show;
            if(publicLetters==null)
            {
                publicLetters=PublicLetterArchivePresenter.Install(root,font,window.w);
                var r=Rect("PublicLettersEntry",root.Find("List"),new Vector2(1,1),new Vector2(1,1),new Vector2(-184,-48),new Vector2(-12,-10));
                var fill=Fill(r,Selected);publicLettersEntry=r.gameObject.AddComponent<Button>();publicLettersEntry.targetGraphic=fill;
                Text(Rect("Text",r,Vector2.zero,Vector2.one,new Vector2(5,3),new Vector2(-5,-3)),"",12,Ink,TextAlignmentOptions.Center).richText=false;
                publicLettersEntry.onClick.AddListener(()=>{if(publicLetters.Available)requestPublicLetters?.Invoke();});AddFocus(r.gameObject);
            }
            publicLetters.Bind(input,isCurrent,save,continueCurrent,close);
            RefreshPublicLetterEntry();
        }
        public void ShowPublicLetters()
        {
            if(publicLetters==null||!publicLetters.Available)return;
            endingLetter?.Session.Hide();publicLetters.Show();
        }
        void RefreshPublicLetterEntry()
        {
            if(publicLettersEntry==null)return;
            bool available=publicLetters!=null&&publicLetters.Available;
            publicLettersEntry.gameObject.SetActive(available);
            if(publicLetters!=null)publicLetters.SetLanguage(GameText.IsEnglish);
            if(available)publicLettersEntry.GetComponentInChildren<TMP_Text>().text=GameText.T("公开专栏","Public letters")+" ("+publicLetters.SavedCount+")";
            var folder=root.Find("List/Folder") as RectTransform;
            if(folder!=null)folder.offsetMax=new Vector2(available?-190:-18,-10);
        }
    }
}
