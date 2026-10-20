using System;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgPackSwipe : XgPackQuestion
    {
        XgPhrase phrase; RectTransform card; float start;
        TMP_Text statement;bool falling,dragging;float fallOffset;int smsRounds;
        XgTeachingPrediction prediction;TMP_Text predictionText,thresholdLabel;
        public XgPackSwipe(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            var pack=XgTeachingPacks.Find(C.Id);
            if(pack.dataset=="headline"&&XgNewsCatalog.All.Count==0){var news=Resources.Load<TextAsset>("LingGuangV05/Browser/browser_news");if(news!=null)XgNewsCatalog.Load(news.text);}
            phrase=C.Sim.PickTeachingPhrase(pack.dataset,C.Random,out int seed);
            QuestionCard=XgSim.TeachingPhraseCard(pack.dataset,phrase,seed);prediction=C.Sim.PredictTeachingBinary(QuestionCard);
            promptZh=phrase.text;promptEn=phrase.textEn;
            string left=pack.dataset=="spam"?"垃圾短信":pack.dataset=="review"?"刷单评论":"标题党";
            string leftEn=pack.dataset=="spam"?"Spam":pack.dataset=="review"?"Fake review":"Clickbait";
            Header(pack.name,pack.nameEn,"左划："+left+"；右划：正常。","Swipe left for "+leftEn.ToLowerInvariant()+", right for genuine.");
            falling=pack.dataset=="spam";dragging=false;fallOffset=0;
            card=C.Box("SwipeCard",150,falling?90:110,600,falling?145:190,Color.white).rectTransform;
            var label=C.Text("Statement",175,falling?105:130,550,falling?115:145,XgUi.T(promptZh,promptEn),falling?25:27);label.raycastTarget=false;statement=label;
            C.Pointer(card,p=>{start=p.x;dragging=true;},p=>{float dx=p.x-start;card.anchoredPosition+=new Vector2(dx,0);label.rectTransform.anchoredPosition+=new Vector2(dx,0);},p=>{
                dragging=false;float shift=card.offsetMin.x-150;if(Math.Abs(shift)>70)Pick(shift<0);else{card.anchoredPosition-=new Vector2(shift,0);label.rectTransform.anchoredPosition-=new Vector2(shift,0);}});
            B("Left",150,345,275,56,XgUi.T("← "+left,"← "+leftEn),()=>Pick(true),24);
            B("Right",475,345,275,56,XgUi.T("正常 →","Genuine →"),()=>Pick(false),24);
            if(C.Sim.TeachingAiReady){predictionText=T("Ai",170,305,570,35,"",20,purple);RefreshPrediction();}
            if(C.Sim.Has("bias"))BuildThreshold();
            if(C.Sim.Has("label.hard"))foreach(var review in C.Sim.TeachingReviewTray(false))if(review.dataset==pack.dataset&&review.text==phrase.text){T("HardReviewBadge",680,80,190,25,XgUi.T("复核","Review"),17,purple);break;}
        }
        public override void Tick(float dt)
        {
            bool advancing=submitted;base.Tick(dt);
            if(advancing||submitted||!C.IsActive||!falling||dragging||card==null||statement==null)return;
            // Falling is presentation only. Park the same message before the answer controls;
            // reading slowly never answers, loses a sample, or sends an actual YY message.
            float next=Mathf.Min(65,fallOffset+Mathf.Max(0,dt)*Mathf.Min(85,26+smsRounds*3));
            float delta=next-fallOffset;fallOffset=next;
            card.anchoredPosition-=new Vector2(0,delta);statement.rectTransform.anchoredPosition-=new Vector2(0,delta);
        }
        void RefreshPrediction()
        {
            prediction=C.Sim.PredictTeachingBinary(QuestionCard);if(prediction==null)return;
            string answer=prediction.guessed&&Math.Abs(C.Sim.TeachingThreshold(QuestionCard.dataset)-.5)<.001?XgUi.T("……不知道","…not sure"):prediction.yes?"←":"→";
            aiText=answer;
            CapturePrediction(prediction.yes==phrase.yes);
            if(predictionText!=null){predictionText.text=C.Sim.AiDisplayName+XgUi.T("：",": ")+answer+(C.Sim.Has("sigmoid")?" · "+Math.Round(prediction.confidence*100)+"%":"");predictionText.color=PredictionInk;}
        }
        void BuildThreshold()
        {
            thresholdLabel=T("DecisionThresholdLabel",30,407,185,28,"",16);
            var track=C.Box("DecisionThreshold",220,416,460,16,new Color(.78f,.81f,.86f)).rectTransform;
            track.GetComponent<Image>().raycastTarget=true;
            var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));var rect=handle.GetComponent<RectTransform>();rect.SetParent(track,false);rect.sizeDelta=new Vector2(16,26);handle.GetComponent<Image>().color=purple;
            var slider=track.gameObject.AddComponent<Slider>();slider.handleRect=rect;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=.05f;slider.maxValue=.95f;
            slider.SetValueWithoutNotify((float)C.Sim.TeachingThreshold(QuestionCard.dataset));
            Action refresh=()=>thresholdLabel.text=XgUi.T("门槛 ","Threshold ")+Math.Round(C.Sim.TeachingThreshold(QuestionCard.dataset)*100)+"%";
            refresh();slider.onValueChanged.AddListener(v=>{if(submitted||!C.IsActive)return;C.Sim.SetTeachingThreshold(QuestionCard.dataset,v);refresh();RefreshPrediction();});
            T("ThresholdMeaning",690,407,200,28,XgUi.T("← 多判是 · 少判是 →","← More yes · Fewer yes →"),13);
        }
        void Pick(bool yes)
        {
            if(submitted)return;var p=XgTeachingPacks.Find(C.Id);
            C.Sim.RememberTeachingBinary(QuestionCard,yes,prediction,phrase.why,phrase.whyEn);
            chosenText=yes?(p.dataset=="spam"?XgUi.T("垃圾短信","Spam"):p.dataset=="review"?XgUi.T("刷单评论","Fake review"):XgUi.T("标题党","Clickbait")):XgUi.T("正常","Genuine");
            string why=phrase.why,whyEn=phrase.whyEn;
            if(falling){smsRounds++;if(!yes&&phrase.yes){why="模拟收件箱：垃圾短信被放行了。"+why;whyEn="Practice inbox: the spam text was let through. "+whyEn;}}
            Submit(yes==phrase.yes,why,whyEn);
        }
    }
    sealed class XgPackPhotos : XgPackQuestion
    {
        readonly bool captcha,meme,digits;
        readonly bool[] selected=new bool[9]; readonly int[] items=new int[9]; readonly int[] photoIndices=new int[9];
        int target,captchaRound;
        bool textCaptcha;string captchaCode="",captchaInput="";TMP_Text captchaEntry;
        XgMiniGame handwriting;XgTeachingAugmentSession augmentation;bool[] augmentationKeep;
        public XgPackPhotos(XgMiniContext c):base(c){captcha=c.Id=="captcha";meme=c.Id=="meme_word";digits=c.Id=="mnist_write";}
        protected override void DrawQuestion()
        {
            target=C.Random.Next(digits?10:meme?6:XgTeachingPictures.LibraryCategories.Length);
            if(captcha){textCaptcha=captchaRound++%2==0;if(textCaptcha)DrawTextCaptcha();else DrawGrid();return;}
            if(!digits&&!meme){DrawPhotoWord();return;}
            Header(digits?"手写数字":meme?"表情包配字":"看图选词",digits?"Handwritten digit":meme?"Caption the face":"Picture to word",
                "看清图片，再选择对应的词。","Look at the image and choose its matching word.");
            int seed=C.Random.Next();
            QuestionCard = new XgCard { dataset=digits?"mnist":"meme", digit=target, seed=seed, teachingImageKind=digits?"handwriting28":"face", teachingImageKey=target };
            var texture=digits?XgTeachingPictures.Handwriting28(target,seed):meme?XgTeachingPictures.Face(target):XgTeachingPictures.Photo(target);
            var picture=C.Rect("QuestionImage",digits?80:355,digits?145:90,digits?224:190,digits?224:145).gameObject.AddComponent<RawImage>();picture.texture=texture;picture.raycastTarget=false;
            promptZh=digits?"图片里是哪个数字？":"图片对应哪个词？";promptEn=digits?"Which digit is shown?":"Which word matches the image?";
            if(digits)
            {
                string[] opts={target.ToString(),((target+1)%10).ToString(),((target+3)%10).ToString(),((target+6)%10).ToString()};Choices(opts,opts,0,180,left:390,buttonWidth:230,columnStep:245);
            }
            else if(meme)
            {
                string[] zh={"开心","难过","惊讶","生气","疑惑","得意"},en={"Happy","Sad","Surprised","Angry","Confused","Smug"};Choices(zh,en,target,235);
            }
            else Choices(XgTeachingPictures.PhotoWords,XgTeachingPictures.PhotoWordsEn,target,235);
            if(digits)B("WriteDigit",20,90,160,40,XgUi.T("自己写一个","Write a digit"),()=>{Clear("","");handwriting=new XgActionDraw(C);handwriting.Build();C.Button("ReturnToDigit",700,8,180,35,XgUi.T("返回数字题","Back to digit"),()=>{handwriting.Dispose();handwriting=null;Build();},16);},18);
            if(digits)
            {
                ShowRealDigitGuess();
                if(C.Sim.TeachingAiReady)B("ShiftProbe",700,95,160,38,XgUi.T("挪一格试试","Try a shift"),()=>{
                    if(submitted||QuestionCard.teachingImageKind=="handwriting28shift")return;
                    var result=C.Sim.ObserveTeachingDigitShift(target,seed);picture.texture=XgTeachingPictures.HandwritingVariant28(target,seed,0);QuestionCard.teachingImageKind="handwriting28shift";
                    aiText=GuessText(result.after);CapturePrediction(result.after.valid?(bool?)(result.after.digit==target):null);
                    var label=ActiveChild("AiAnswer")?.GetComponent<TMP_Text>();if(label!=null){label.text=C.Sim.AiDisplayName+XgUi.T("：原来 ",": before ")+GuessText(result.before)+XgUi.T(" → 挪后 "," → shifted ")+aiText;label.color=PredictionInk;}
                },16);
                if(C.Sim.Has("augment"))B("Augment",525,95,160,38,XgUi.T("变","Vary"),()=>OpenAugmentation(seed),19);
            }
        }
        public override void Tick(float dt)
        {
            if(handwriting!=null){handwriting.Tick(dt);return;}
            if(augmentation!=null){if(C.IsActive)elapsed+=dt;return;}
            bool advancing=submitted;base.Tick(dt);if(advancing||!captcha||!textCaptcha||submitted||!C.IsActive)return;
            var keyboard=Keyboard.current;if(keyboard==null)return;
            foreach(char c in XgVisualRaster.Alphabet){Key key=(Key)Enum.Parse(typeof(Key),char.IsDigit(c)?"Digit"+c:c.ToString());if(C.KeyDown(key))AddCaptcha(c);}
            if(C.KeyDown(Key.Backspace))BackCaptcha();if(C.KeyDown(Key.Enter)||C.KeyDown(Key.NumpadEnter))SubmitCaptcha();
        }
        public override void Dispose(){handwriting?.Dispose();base.Dispose();}
        string GuessText(XgHandwritingGuess guess)=>!guess.valid?XgUi.T("……不知道","…not sure"):guess.digit.ToString()+(C.Sim.Has("sigmoid")?" · "+Math.Round(guess.confidence*100)+"%":"");
        Transform ActiveChild(string name) { foreach (Transform child in C.Root) if (child.name == name && child.gameObject.activeSelf) return child; return null; }
        void ShowRealDigitGuess()
        {
            if(!C.Sim.TeachingAiReady)return;var ink=XgVisualRaster.Handwriting28(target,(QuestionCard.seed&int.MaxValue)%16);
            var guess=C.Sim.ReadHandwriting(XgTeachingAugmentation.RecognitionRaster(ink));aiText=GuessText(guess);
            CapturePrediction(guess.valid?(bool?)(guess.digit==target):null);
            var label=ActiveChild("AiAnswer")?.GetComponent<TMP_Text>();if(label!=null){label.text=C.Sim.AiDisplayName+XgUi.T("：",": ")+aiText;label.color=PredictionInk;}
        }
        void OpenAugmentation(int seed)
        {
            if(submitted||augmentation!=null)return;augmentation=C.Sim.BeginTeachingAugmentation(target,seed);if(augmentation==null)return;
            augmentationKeep=new[]{true,true,true,true};
            ClearPredictionFeedback(); // This subtask displays no AI prediction; do not grade the old digit guess.
            Header("变一变，标签还对吗？","Vary it: does the label still fit?","点图下按钮，剔掉已经不像原数字的图，再提交。","Discard images that no longer match the original digit, then submit.");
            string[] zh={"挪一格","转半圈","左右翻","调亮度"},en={"Shift one pixel","Turn 180°","Mirror","Brightness"};
            T("OriginalLabel",25,82,850,30,XgUi.T("原标签：","Original label: ")+target,22);
            for(int i=0;i<4;i++)
            {
                int variant=i;T("VariantName"+i,20+i*220,118,200,28,XgUi.T(zh[i],en[i]),18);
                var image=C.Rect("Variant"+i,30+i*220,155,180,180).gameObject.AddComponent<RawImage>();image.texture=XgTeachingPictures.HandwritingVariant28(target,seed,i);image.raycastTarget=false;
                var b=B("KeepVariant"+i,30+i*220,345,180,40,XgUi.T("✓ 保留","✓ Keep"),()=>{},18);
                b.button.onClick.RemoveAllListeners();int v=version;b.button.onClick.AddListener(()=>{if(!C.IsActive||augmentation==null||v!=version)return;augmentationKeep[variant]=!augmentationKeep[variant];b.Set(augmentationKeep[variant]?XgUi.T("✓ 保留","✓ Keep"):XgUi.T("× 剔除","× Discard"),true);});
            }
            B("CommitAugmentation",290,398,320,38,XgUi.T("提交这些选择","Submit choices"),()=>{
                var result=C.Sim.FinishTeachingAugmentation(augmentation,augmentationKeep);if(result==null)return;augmentation=null;
                promptZh="变换后的图片还符合标签 "+target+" 吗？";promptEn="Do the transformed images still match label "+target+"?";
                chosenText=string.Join(",",Array.ConvertAll(augmentationKeep,x=>x?"keep":"discard"));
                Submit(result.correct,"保留 "+result.kept+" 张，其中错误标签 "+result.bad+" 张。","Kept "+result.kept+" images, including "+result.bad+" incorrect labels.");
            },19);
        }
        void DrawTextCaptcha()
        {
            Header("四位验证码","Four-character captcha","照着图片输入四位字母数字，再提交。","Type the four letters and digits shown, then submit.");
            int seed=C.Random.Next(128);captchaCode=XgVisualRaster.Code(seed);captchaInput="";
            promptZh="输入图中的四位验证码";promptEn="Type the four-character code in the image";
            QuestionCard=new XgCard{dataset="cifar",seed=seed,teachingImageKind="captcha4"};
            var image=C.Rect("CaptchaImage",322,95,256,80).gameObject.AddComponent<RawImage>();image.texture=XgTeachingPictures.Captcha4(seed);image.raycastTarget=false;
            captchaEntry=T("CaptchaEntry",275,185,350,48,"_ _ _ _",28);
            for(int i=0;i<XgVisualRaster.Alphabet.Length;i++){char c=XgVisualRaster.Alphabet[i];B("Type"+c,170+(i%8)*70,250+(i/8)*36,64,32,c.ToString(),()=>AddCaptcha(c),19);}
            B("Backspace",170,402,190,32,XgUi.T("退一格","Backspace"),BackCaptcha,17);
            B("SubmitCode",540,402,190,32,XgUi.T("提交","Submit"),SubmitCaptcha,17);
            if(C.Sim.TeachingAiReady){aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):C.Random.NextDouble()<C.Skill?captchaCode:XgVisualRaster.Code((seed+37)%128);CapturePrediction(C.Skill<.55?(bool?)null:aiText==captchaCode);ShowAi(200,215,500);}
        }
        void AddCaptcha(char c){if(submitted||captchaInput.Length>=4)return;captchaInput+=c;RefreshCaptchaEntry();}
        void BackCaptcha(){if(submitted||captchaInput.Length==0)return;captchaInput=captchaInput.Substring(0,captchaInput.Length-1);RefreshCaptchaEntry();}
        void RefreshCaptchaEntry(){captchaEntry.text=captchaInput.PadRight(4,'_');}
        void SubmitCaptcha(){if(captchaInput.Length!=4||submitted)return;chosenText=captchaInput;Submit(captchaInput==captchaCode,"图中是 "+captchaCode,"The image reads "+captchaCode);}
        void DrawGrid()
        {
            Array.Clear(selected,0,selected.Length);
            Header("12306 九宫格","12306 image grid","点出所有符合要求的照片，再提交。","Select all matching photographs, then submit.");
            promptZh="找出所有"+XgTeachingPictures.LibraryWords[target];promptEn="Select every "+XgTeachingPictures.LibraryWordsEn[target];
            T("Target",430,125,430,90,XgUi.T(promptZh,promptEn),28);
            for(int i=0;i<9;i++) items[i]=i<2?target:C.Random.Next(XgTeachingPictures.LibraryCategories.Length);
            for(int i=8;i>0;i--){int j=C.Random.Next(i+1);int tmp=items[i];items[i]=items[j];items[j]=tmp;}
            for(int i=0;i<9;i++) { int variant; do{variant=C.Random.Next(XgTeachingPictures.PhotosPerCategory);}while(Array.IndexOf(photoIndices,items[i]*10+variant,0,i)>=0);photoIndices[i]=items[i]*10+variant;}
            QuestionCard=new XgCard{dataset="cifar",teachingImageKind="grid80",teachingImageKey=target,teachingImageKeys=(int[])photoIndices.Clone()};
            for(int i=0;i<9;i++)
            {
                int k=i;float x=50+(i%3)*105,y=95+(i/3)*105;
                var b=B("Cell"+i,x,y,99,99,"",()=>{selected[k]=!selected[k];PaintSelected(k);});
                var image=C.Rect("Photo"+i,x+4,y+4,91,91).gameObject.AddComponent<RawImage>();image.texture=XgTeachingPictures.LibraryPhoto(photoIndices[i]);image.raycastTarget=false;
            }
            if(C.Sim.TeachingAiReady){aiText="";bool guessedGridCorrect=true;
                if(C.Skill<.55)aiText=XgUi.T("……不知道","…not sure");
                else for(int i=0;i<9;i++){bool pick=items[i]==target;if(C.Random.NextDouble()>C.Skill)pick=!pick;if(pick!=(items[i]==target))guessedGridCorrect=false;if(pick)aiText+=(i+1)+" ";}
                if(aiText.Length==0)aiText=XgUi.T("都不选","None");CapturePrediction(C.Skill<.55?(bool?)null:guessedGridCorrect);ShowAi(430,225,430);}
            B("Submit",450,290,340,70,XgUi.T("提交","Submit"),()=>{
                bool ok=true;chosenText="";for(int i=0;i<9;i++){if(selected[i]!=(items[i]==target))ok=false;if(selected[i])chosenText+=(i+1)+" ";}
                Submit(ok,"目标是"+XgTeachingPictures.LibraryWords[target]+"，只选这些照片。","Select only the photographs of "+XgTeachingPictures.LibraryWordsEn[target]+".");},28);
        }
        void DrawPhotoWord()
        {
            Header("看图选词","Picture to word","看照片，选择它的名字。","Look at the photograph and choose its name.");
            // Five distinct photos per class are reused from the licensed ten-photo grid library.
            int imageIndex=target*10+C.Random.Next(5);
            QuestionCard=new XgCard{dataset="imagenet",teachingImageKind="photo80",teachingImageKey=imageIndex};
            var image=C.Rect("QuestionImage",350,92,200,200).gameObject.AddComponent<RawImage>();image.texture=XgTeachingPictures.LibraryPhoto(imageIndex);image.raycastTarget=false;
            promptZh="照片里是什么？";promptEn="What is in the photograph?";
            var categories=new System.Collections.Generic.List<int>{target};
            // Apple phones are intentional distractors, never mislabeled as fruit positives.
            if(target==6)categories.Add(7);else if(target==7)categories.Add(6);
            while(categories.Count<4){int n=C.Random.Next(XgTeachingPictures.LibraryCategories.Length);if(!categories.Contains(n))categories.Add(n);}
            var zh=new string[4];var en=new string[4];for(int i=0;i<4;i++){zh[i]=XgTeachingPictures.LibraryWords[categories[i]];en[i]=XgTeachingPictures.LibraryWordsEn[categories[i]];}
            Choices(zh,en,0,335);
        }
        void PaintSelected(int i)
        {
            var t=C.Root.Find("Cell"+i);if(t!=null){var image=t.GetComponent<Image>();if(image!=null)image.color=selected[i]?new Color(.2f,.65f,.3f):Color.white;}
        }
    }
    sealed class XgPackPoem : XgPackQuestion
    {
        static readonly string[] Five={"床前明月光","疑是地上霜","举头望明月","低头思故乡","白日依山尽","黄河入海流","欲穷千里目","更上一层楼","春眠不觉晓","处处闻啼鸟"};
        static readonly string[] Seven={"日照香炉生紫烟","遥看瀑布挂前川","飞流直下三千尺","疑是银河落九天","两岸猿声啼不住","轻舟已过万重山"};
        int questionRound;XgPoemQuote quote;
        public XgPackPoem(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            quote=null;
            if(++questionRound%4==0){DrawQuote();return;}
            var bank=C.Sim.Has("lstm")||C.Sim.Has("gru")?Seven:Five;string line=bank[C.Random.Next(bank.Length)];int count=C.Random.Next(2,line.Length);
            string good=line[count].ToString();string[] options={good,"山","月","人"};for(int i=1;i<4;i++)if(options[i]==good)options[i]=new[]{"风","水","春"}[i-1];
            promptZh=line.Substring(0,count)+"□";promptEn="Next character: "+promptZh;
            Header("唐诗 · 下一个字","Tang poems · next character","选出诗句的下一个字。门控记忆解锁七言。","Choose the next character. Gated memory opens seven-character lines.");
            T("Line",45,105,810,100,promptZh,38);Choices(options,options,0);
        }
        void DrawQuote()
        {
            quote=XgTeachingPoetry.Quotes[C.Random.Next(XgTeachingPoetry.Quotes.Length)];
            promptZh=quote.line+"\n这是原诗吗？";promptEn=quote.line+"\n"+quote.lineEn+"\nIs this the original poem?";
            Header("原诗还是改编？","Original or parody?","别被押韵骗了，判断题目里的诗句。","Rhyming is not enough: judge the quoted line itself.");
            T("PoemQuote",35,100,830,145,XgUi.T(promptZh,promptEn),27);
            Choices(new[]{"原诗","改编"},new[]{"Original","Parody"},quote.original?0:1,300,XgTeachingPoetry.Explain(quote,false),XgTeachingPoetry.Explain(quote,true));
        }
    }
    sealed class XgPackDanmu : XgPackQuestion
    {
        TMP_Text scrollingComment;float scrollX,scrollWidth;
        public XgPackDanmu(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            var line=XgTeachingDanmu.Pick(C.Sim.Today,C.Random);promptZh=line.text;promptEn=line.textEn;
            Header("弹幕夸还是骂","Praise or jab","判断这句话是在夸、骂，还是说反话。","Is this praise, a jab, or sarcasm?");
            var viewport=C.Rect("CommentViewport",35,105,830,100);viewport.gameObject.AddComponent<RectMask2D>();
            scrollingComment=T("ScrollingComment",0,0,830,100,XgUi.T(promptZh,promptEn),28);scrollingComment.rectTransform.SetParent(viewport,false);
            scrollingComment.enableAutoSizing=false;scrollingComment.textWrappingMode=TextWrappingModes.NoWrap;scrollingComment.overflowMode=TextOverflowModes.Overflow;
            scrollWidth=Mathf.Max(400,scrollingComment.GetPreferredValues(scrollingComment.text).x+24);scrollingComment.rectTransform.sizeDelta=new Vector2(scrollWidth,100);
            scrollX=Mathf.Max(0,830-scrollWidth);PlaceComment();
            Choices(new[]{"夸","骂","反话"},new[]{"Praise","Jab","Sarcasm"},line.tone);
        }
        void PlaceComment(){var r=scrollingComment.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(scrollX,0);}
        public override void Tick(float dt)
        {
            bool advancing=submitted;base.Tick(dt);if(advancing||submitted||!C.IsActive||scrollingComment==null)return;
            scrollX-=Mathf.Max(0,dt)*65;if(scrollX < -scrollWidth)scrollX=830;PlaceComment();
            // Leaving the viewport loops the same question; only a player's answer settles it.
        }
    }
    sealed class XgPackEnglish : XgPackQuestion
    {
        static readonly string[][] Zh={new[]{"Good good study, day day up.","努力学习，天天向上。","Study hard and make progress every day.","Study good and day up.","Good days study up.","Day day good study."},new[]{"Please don't touch me.","请勿触摸。（博物馆展品标牌）","Please do not touch.","Please don't touch myself.","No to touch me.","Please do not touches."},new[]{"Carefully slide.","小心地滑。","Caution: slippery floor.","Careful slipping.","The floor is carefully.","Slip the floor carefully."},new[]{"I very like this game.","我很喜欢这个游戏。","I really like this game.","I am very like this game.","I very likes this game.","I like very this game."}};
        static readonly string[][] Spans={new[]{"Good good","study,","day day","up."},new[]{"Please","don't","touch","me."},new[]{"Carefully","slide."},new[]{"I","very","like","this game."}};
        static readonly int[][] ErrorSpans={new[]{0,2},new[]{3},new[]{1},new[]{1}};
        static readonly string[] ContextEn={"A slogan about diligent study and daily progress.","A museum display sign warning visitors not to touch the exhibit.","A sign warning that the floor is slippery.","Express strong enjoyment of this game."};
        static readonly string[] ReasonZh={"good good、day day 都是逐字硬译。","标牌提醒勿触摸展品，不是在说别碰我。","地滑是 slippery，不是让人 slide。","very 不能直接修饰 like。"};
        static readonly string[] ReasonEn={"Good good and day day are both literal mistranslations.","The sign warns about touching the exhibit, not touching the speaker.","A slippery floor is a hazard, not an instruction to slide.","Very cannot directly modify like."};
        int questionIndex;bool choosingRewrite;
        public XgPackEnglish(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            questionIndex=C.Random.Next(Zh.Length);choosingRewrite=false;var q=Zh[questionIndex];promptZh=q[1]+"\n"+q[0];promptEn=ContextEn[questionIndex]+"\n"+q[0];
            Header("中式英语改错","Fix the Chinglish","先点出错的字词，再选自然的改写。","First locate the error, then choose a natural rewrite.");
            T("Source",35,100,830,100,XgUi.T(promptZh,promptEn),27);
            for(int i=0;i<Spans[questionIndex].Length;i++){int span=i;B("ErrorSpan"+i,35+i*215,260,200,65,Spans[questionIndex][i],()=>PickError(span),23);}
            if(C.Sim.TeachingAiReady){aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):Spans[questionIndex][ErrorSpans[questionIndex][0]];CapturePrediction(C.Skill<.55?(bool?)null:true);ShowAi(35,210,830);}
        }
        void PickError(int span)
        {
            if(submitted||choosingRewrite)return;bool valid=Array.IndexOf(ErrorSpans[questionIndex],span)>=0;
            chosenText=Spans[questionIndex][span];
            if(!valid){Submit(false,ReasonZh[questionIndex],ReasonEn[questionIndex]);return;}
            choosingRewrite=true;var q=Zh[questionIndex];
            Header("中式英语改错","Fix the Chinglish","找到问题了，现在选意思准确、说法自然的改写。","Error found. Now choose the accurate, natural rewrite.");
            T("Source",35,100,830,100,XgUi.T(promptZh,promptEn),25);
            T("FoundError",35,202,830,42,XgUi.T("问题词：","Problem phrase: ")+chosenText,21,purple);
            string[] answers={q[2],q[3],q[4],q[5]};Choices(answers,answers,0,300,ReasonZh[questionIndex],ReasonEn[questionIndex]);
        }
    }

}
