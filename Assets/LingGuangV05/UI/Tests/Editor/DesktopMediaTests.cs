using System;
using System.Reflection;
using System.Collections.Generic;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Forum;
using LingGuangV05.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
namespace LingGuangV05.UI.Tests
{
    public class DesktopMediaTests
    {
        static Type Media=>Type.GetType("LingGuangV05.Desktop.Media.DesktopMedia, Assembly-CSharp",true);
        static Sprite Avatar(string id)=>(Sprite)Media.GetMethod("Avatar").Invoke(null,new object[]{id});
        static Sprite Picture(string id)=>(Sprite)Media.GetMethod("Picture").Invoke(null,new object[]{id});
        [Test] public void DifferentForumAccountsNeverBorrowAnNpcPortrait()
        {
            var sprites=new HashSet<Sprite>();
            foreach(string id in new[]{"me","laozhou","cousin","qingwen","ajie","xiaogang","dawei","netbar","zhou_now","dad","mom","aunt","wang","liu","xiaolu","afang","lili","akazhan","dengdeng","chigua","yanglaji","xiaobai","kedaibiao","haixing","xiezhi","liandan","guaji","tiaocan","nlplsj"})
            {
                var sprite=Avatar(id); Assert.IsNotNull(sprite,id);
                Assert.IsTrue(sprites.Add(sprite),"Different identity reused an avatar: "+id);
                Assert.IsTrue(sprite.name.StartsWith("Photos/avatar_",StringComparison.Ordinal),"Identity still uses generated portrait atlas: "+id);
                Assert.AreEqual(sprite.rect.width,sprite.rect.height,"Avatar must fill its square/circular slot: "+id);
            }
        }
        [Test] public void EveryAuthoredNewsAndThreadHasAnIndependentPhotograph()
        {
            var textures=new HashSet<Texture2D>();
            foreach(var e in EraContent.Events.All)
                if(e.channel==EraEvents.News||e.channel==EraEvents.Tieba)
                    AssertUniquePicture((e.channel==EraEvents.News?"news_":"era_")+e.id,textures);
            var forum=ForumLibrary.Parse(Resources.Load<TextAsset>("LingGuangV05/Forum/forum").text);
            foreach(var thread in forum.Threads)AssertUniquePicture("thread_"+thread.id,textures);
        }
        [Test] public void ProductsAndStorySlotsHaveIndividualPictures()
        {
            var textures=new HashSet<Texture2D>();
            foreach(string id in new[]{"gtx750ti","gtx970","gtx980ti","gtx1080","gtx1070","rx480","gtx1060","titanxp","gtx1080ti"})AssertUniquePicture("gpu_"+id,textures);
            foreach(string id in new[]{"milktea","plush","band","powerbank","lipstick","tickets","iphone7","case"})AssertUniquePicture("gift_"+id,textures);
            foreach(string id in new[]{"product_case","product_nvme","product_cafebox","home_power","home_jobs","origin_video","news_banner","chat_qingwen_cat","chat_geyou"})AssertUniquePicture(id,textures);
        }
        [Test] public void ChatImagesUseExactSenderAndPhraseWhitelist()
        {
            const string cat="[图片] 宿舍门口那只橘猫", meme="[阿杰] [图片：葛优躺] 今天的我。";
            Assert.IsNotNull(ChatPhoto("qingwen",cat));
            Assert.IsNotNull(ChatPhoto("qingwen","[图片] the cat at the dorm gate"));
            Assert.IsNotNull(ChatPhoto("netbar",meme));
            Assert.IsNotNull(ChatPhoto("netbar","[A-Jie] [Image: Ge You slouch] Me today."));
            Assert.IsNull(ChatPhoto("me",cat));Assert.IsNull(ChatPhoto("ajie",meme));
            Assert.IsNull(ChatPhoto("qingwen",cat+"。"));
            Assert.IsNull(ChatPhoto("qingwen",cat,"File"));
        }
        static Sprite ChatPhoto(string sender,string text,string kind="Text")
        {
            var messageType=Type.GetType("LingGuangV05.Desktop.YY.YYMessage, Assembly-CSharp",true);
            var message=Activator.CreateInstance(messageType);
            messageType.GetField("from").SetValue(message,sender);messageType.GetField("text").SetValue(message,text);
            var field=messageType.GetField("kind");field.SetValue(message,Enum.Parse(field.FieldType,kind));
            var view=Type.GetType("LingGuangV05.Desktop.YY.YYChatView, Assembly-CSharp",true);
            return (Sprite)view.GetMethod("AuthoredPhoto",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new[]{message});
        }
        [Test] public void MissingOrUnsafeResourceNamesDoNotBorrowUnrelatedArtwork()
        {
            foreach(string key in new[]{null,"","../content_atlas","Photos/foo","not_a_real_asset"})Assert.IsNull(Picture(key));
            Assert.IsNull(Avatar("unmapped_user"));Assert.IsNull(Avatar(null));
        }
        [Test] public void AllDataPacksShowStableDistinctExistingSamplesWithoutBlockingClicks()
        {
            var preview=Type.GetType("LingGuangV05.Desktop.Taohuo.DataPackPreview, Assembly-CSharp",true);
            var sim=Type.GetType("LingGuangV05.XingGuang.XgSim, LingGuangV05.XingGuang.Core",true);
            var offers=(System.Collections.IEnumerable)sim.GetProperty("DataOfferCatalog").GetValue(null);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Assert.IsNotNull(font);
            var seeds=new HashSet<int>();var samples=new HashSet<string>();var visibleSamples=new HashSet<string>();int count=0;
            foreach(var offer in offers)
            {
                var type=offer.GetType();string source=type.GetField("source").GetValue(offer).ToString();
                if(source!="Public"&&source!="Junk")continue;
                string id=(string)type.GetField("id").GetValue(offer);
                int seed=(int)preview.GetMethod("SeedFor").Invoke(null,new object[]{id});
                string description=(string)preview.GetMethod("SampleDescription").Invoke(null,new object[]{id});
                string visible=(string)preview.GetMethod("VisibleSampleDescription").Invoke(null,new object[]{id});
                Assert.IsTrue(seeds.Add(seed),"Duplicate seed: "+id);Assert.IsTrue(samples.Add(description),"Duplicate sample: "+id);
                Assert.IsTrue(visibleSamples.Add(visible),"Duplicate visible sample despite a different seed: "+id);
                Assert.AreEqual(description,preview.GetMethod("SampleDescription").Invoke(null,new object[]{id}),"Sample changed: "+id);
                var go=new GameObject("Preview "+id,typeof(RectTransform));
                try
                {
                    ((RectTransform)go.transform).sizeDelta=new Vector2(112,112);
                    Assert.IsTrue((bool)preview.GetMethod("Paint").Invoke(null,new object[]{go.transform,offer,font}),id);
                    var graphics=go.GetComponentsInChildren<Graphic>();Assert.Greater(graphics.Length,2,id);
                    foreach(var graphic in graphics)Assert.IsFalse(graphic.raycastTarget,id+"/"+graphic.name);
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
                count++;
            }
            Assert.AreEqual(29,count,"The public/junk catalog changed; review its previews.");
        }
        static void AssertUniquePicture(string key,HashSet<Texture2D> textures)
        {
            var sprite=Picture(key);Assert.IsNotNull(sprite,"Missing artwork: "+key);
            Assert.IsTrue(textures.Add(sprite.texture),"Different content reused one texture: "+key);
            Assert.IsTrue(sprite.name.StartsWith("Photos/",StringComparison.Ordinal),"Still uses a legacy atlas: "+key);
        }
        [Test] public void EveryYyIdentityHasArtAndGroupSenderAliasesStayConsistent()
        {
            foreach(string id in new[]{"me","laozhou","cousin","qingwen","netbar","ajie","xiaogang","dawei","lingguang","lili"})Assert.IsNotNull(Avatar(id),id);
            Assert.AreSame(Avatar("ajie"),Avatar("[阿杰]"));Assert.AreSame(Avatar("cousin"),Avatar("f.cousin"));
            Assert.AreSame(Avatar("ajie"),Avatar("A-Jie"));Assert.AreSame(Avatar("xiaogang"),Avatar("Xiao Gang"));Assert.AreSame(Avatar("dawei"),Avatar("Da Wei"));Assert.AreSame(Avatar("lili"),Avatar("莉莉"));
            Assert.AreNotSame(Avatar("qingwen"),Avatar("cousin"));Assert.AreNotSame(Avatar("laozhou"),Avatar("zhou_now"));
            Assert.LessOrEqual(Avatar("lingguang").texture.width,16,"keep the story's pixel-zero avatar");
        }
        [Test] public void AppIllustrationsExistAndAtlasCellsHaveDistinctRects()
        {
            Sprite previous=null;
            foreach(string id in new[]{"casino","iphone","ai","gpu","desk","esports","space","phone","cinema","shopping","flowers","plush","headset","data","go","news","books","laptop"})
            {
                var s=(Sprite)Media.GetMethod("Picture").Invoke(null,new object[]{id});Assert.IsNotNull(s,id);
                Assert.Greater(s.rect.width,16);if(previous!=null&&previous.texture==s.texture)Assert.AreNotEqual(previous.rect,s.rect,id);previous=s;
            }
        }
        [Test] public void DecorativeArtworkNeverInterceptsInput()
        {
            var go=new GameObject("Media slot",typeof(RectTransform));
            try
            {
                var image=(Image)Media.GetMethod("Paint").Invoke(null,new object[]{(RectTransform)go.transform,"gpu",true});
                Assert.IsNotNull(image);Assert.IsFalse(image.raycastTarget);Assert.IsNotNull(go.GetComponent<RectMask2D>());
                Assert.AreEqual(1,go.transform.childCount);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
