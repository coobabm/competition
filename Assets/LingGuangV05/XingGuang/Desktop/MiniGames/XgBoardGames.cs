using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LingGuangV05.XingGuang;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    public static class XgBoardGames
    {
        public static XgMiniGame Create(string id,XgMiniContext c)
        {
            switch(id){case "alphago":return new XgGoGame(c);case "forgetax":return new XgForgeGame(c);
                case "vramtetris":return new XgTetrisGame(c);case "merge2048":return new XgMergeGame(c);
                case "sentence":return new XgSentenceGame(c);case "lianliankan":return new XgLinkGame(c);default:return null;}
        }
    }
    internal abstract class XgBoardMini : XgMiniGame
    {
        protected TMP_Text Status,Info;
        protected static readonly Color Dark=new Color(.13f,.18f,.25f),Blue=new Color(.2f,.46f,.78f),Green=new Color(.15f,.58f,.42f),Pale=new Color(.88f,.91f,.94f);
        protected XgBoardMini(XgMiniContext c):base(c){}
        protected void Frame(string title,string instructions)
        {C.Text("Title",20,8,850,32,title,25);Info=C.Text("Info",20,42,860,43,instructions,17);Status=C.Text("Status",440,110,435,190,"",20);Status.textWrappingMode=TextWrappingModes.Normal;}
        protected void Shuffle<T>(T[] a){for(int i=a.Length-1;i>0;i--){int j=C.Random.Next(i+1);T v=a[i];a[i]=a[j];a[j]=v;}}
        protected void Message(string text){Status.text=text;C.Say(text);}
    }
}
