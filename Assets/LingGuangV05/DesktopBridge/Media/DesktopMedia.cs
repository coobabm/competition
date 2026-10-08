using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Media
{
    /// <summary>Content-specific, locally bundled photographs. Sprites are cached and cropped only by UI layout.</summary>
    public static class DesktopMedia
    {
        const string Root="LingGuangV05/Media/";
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        // Compatibility names for older callers. Current editorial/product views use their actual content IDs.
        static readonly Dictionary<string,string> Pictures=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"ai","thread_ai_perceptron"},{"gpu","gpu_gtx970"},{"desk","home_jobs"},
            {"esports","thread_my_souls"},{"space","news_shenzhou11"},{"phone","news_note7_recall"},
            {"cinema","thread_santi_film"},{"shopping","news_d11_total"},{"flowers","avatar_mom"},
            {"plush","gift_plush"},{"headset","avatar_guaji"},{"data","thread_tip_data"},
            {"go","thread_ai_master"},{"news","news_banner"},{"books","thread_tip_translate"},
            {"laptop","product_cafebox"},{"iphone","gift_iphone7"}
        };
        static readonly Dictionary<string,string> Aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"我","me"},{"老周","laozhou"},{"lao zhou","laozhou"},{"周而复始","laozhou"},{"周而复始_","zhou_now"},
            {"表姐","cousin"},{"f.cousin","cousin"},{"晴雯ˇ","qingwen"},{"晴雯","qingwen"},{"林晴雯","qingwen"},
            {"阿杰","ajie"},{"f.ajie","ajie"},{"小刚","xiaogang"},{"f.xiaogang","xiaogang"},{"大伟","dawei"},
            {"a-jie","ajie"},{"xiao gang","xiaogang"},{"da wei","dawei"},{"莉莉","lili"},
            {"老爸","dad"},{"爸爸","dad"},{"妈妈","mom"},{"老妈","mom"},{"二姨","aunt"},{"auntie","aunt"},
            {"王科长","wang"},{"刘师傅","liu"},{"小鹿","xiaolu"},{"阿芳","afang"},
            {"g.family","family_group"},{"g.work","work_group"}
        };
        public static Sprite Avatar(string id)
        {
            string key=(id??"").Trim().Trim('[',']').ToLowerInvariant();
            if(key=="lingguang"||key=="灵光")return AiJoinsYy.PixelZero();
            if(Aliases.TryGetValue(key,out var alias))key=alias;
            return ValidKey(key)?Full("Photos/avatar_"+key):null;
        }
        public static Sprite Picture(string key)
        {
            key=(key??"").Trim().ToLowerInvariant();
            if(key=="casino")return Full("casino_backdrop");
            if(Pictures.TryGetValue(key,out var alias))key=alias;
            return ValidKey(key)?Full("Photos/"+key):null;
        }
        static bool ValidKey(string key)
        {
            if(string.IsNullOrEmpty(key)||key.Length>100||key.Contains(".."))return false;
            foreach(char c in key)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'||c=='.'||c=='-'))return false;
            return true;
        }
        static Sprite Full(string file)
        {
            if(cache.TryGetValue(file,out var sprite)&&sprite!=null)return sprite;
            var texture=Resources.Load<Texture2D>(Root+file);if(texture==null)return null;
            var crop=new Rect(0,0,texture.width,texture.height);
            if(file.StartsWith("Photos/avatar_",StringComparison.Ordinal))
            {
                float side=Mathf.Min(texture.width,texture.height);
                crop=new Rect((texture.width-side)*.5f,(texture.height-side)*.5f,side,side);
            }
            else if(file=="Photos/gift_iphone7")
                crop=new Rect(0,texture.height*.15f,texture.width*.61f,texture.height*.85f);
            sprite=Sprite.Create(texture,crop,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name=file;sprite.hideFlags=HideFlags.DontSave;cache[file]=sprite;return sprite;
        }
        public static Image Paint(RectTransform parent,string key,bool cover=true)
        {
            var sprite=Picture(key);if(parent==null||sprite==null)return null;
            if(cover&&parent.GetComponent<RectMask2D>()==null&&parent.GetComponent<Mask>()==null)parent.gameObject.AddComponent<RectMask2D>();
            var rect=PrologueDesk.Rect("Artwork "+key,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.preserveAspect=!cover;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
            if(cover){var aspect=rect.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio=sprite.rect.width/sprite.rect.height;}
            rect.SetAsFirstSibling();return image;
        }
    }
}
