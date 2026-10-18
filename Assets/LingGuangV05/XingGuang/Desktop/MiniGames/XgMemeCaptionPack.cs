using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgMemeCaptionPack:XgPackQuestion
    {
        int mood;TMP_InputField captionInput;
        public XgMemeCaptionPack(XgMiniContext context):base(context){}
        protected override void DrawQuestion()
        {
            mood=C.Random.Next(6);captionInput=null;
            bool typed=C.Sim.Has("caption");
            Header("表情包配字","Caption the meme",typed?"用一个词形容表情。":"看表情，选择它的情绪。",typed?"Describe the expression in one word.":"Look at the face and choose its emotion.");
            promptZh="这个表情是什么情绪？";promptEn="What emotion does this face show?";
            string imageKind=XgTeachingMoods.ImageKind(mood,C.Sim.Today);
            QuestionCard=new XgCard{dataset="meme",teachingImageKind=imageKind,teachingImageKey=mood,digit=mood};
            float height=typed?145:110,width=imageKind=="face"?height:height*1.5f;
            var image=C.Rect("QuestionImage",450-width/2,90,width,height).gameObject.AddComponent<RawImage>();image.texture=XgTeachingPictures.ForCard(QuestionCard);image.raycastTarget=false;
            if(!typed){Choices(XgTeachingMoods.Chinese,XgTeachingMoods.English,mood,245);return;}
            GuessBinary(true,XgTeachingMoods.Chinese[mood],XgTeachingMoods.English[mood],"不知道","Not sure");ShowAi(180,242,560);
            var box=C.Rect("MemeCaptionInput",180,285,540,50);var back=XgUi.Panel(box,Color.white);back.raycastTarget=true;
            var area=XgUi.Rect("Text Area",box,Vector2.zero,Vector2.one,new Vector2(12,4),new Vector2(-12,-4));area.gameObject.AddComponent<RectMask2D>();
            var placeholder=C.Ui.Text(XgUi.Rect("Placeholder",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),XgUi.T("输入情绪词……","Type an emotion…"),22,new Color(.45f,.47f,.52f),TextAlignmentOptions.MidlineLeft);
            var text=C.Ui.Text(XgUi.Rect("Text",area,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",22,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;
            captionInput=box.gameObject.AddComponent<TMP_InputField>();captionInput.textViewport=area;captionInput.textComponent=text;captionInput.placeholder=placeholder;
            captionInput.fontAsset=C.Ui.font;captionInput.pointSize=22;captionInput.characterLimit=24;captionInput.richText=false;captionInput.lineType=TMP_InputField.LineType.SingleLine;
            captionInput.onSubmit.AddListener(_=>SubmitCaption());B("SubmitCaption",320,365,260,54,XgUi.T("提交","Submit"),SubmitCaption,24);
        }
        void SubmitCaption()
        {
            if(submitted||captionInput==null||!C.IsActive)return;string word=captionInput.text;
            if(string.IsNullOrWhiteSpace(word)){C.Say(XgUi.T("先写一个情绪词。","Write an emotion first."));return;}
            chosenText=word.Trim();bool correct=XgTeachingMoods.Matches(mood,word);
            Submit(correct,"这个表情是："+XgTeachingMoods.Chinese[mood],"This expression is "+XgTeachingMoods.English[mood].ToLowerInvariant()+".");
        }
    }
}
