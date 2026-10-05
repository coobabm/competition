using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Media
{
    /// <summary>Generated raster art, sliced on the GPU into cached sprites. No file copies/crops at runtime.</summary>
    public static class DesktopMedia
    {
        const string Root="LingGuangV05/Media/";
        static readonly string[] People={"me","laozhou","cousin","qingwen","ajie","xiaogang","dawei","netbar","zhou_now","dad","mom","aunt","wang","liu","xiaolu","afang"};
        static readonly string[] Pictures={"ai","gpu","desk","esports","space","phone","cinema","shopping","flowers","plush","headset","data","go","news","books","laptop"};
        static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
        static readonly Dictionary<string,string> Aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"我","me"},{"老周","laozhou"},{"lao zhou","laozhou"},{"周而复始","laozhou"},{"周而复始_","zhou_now"},
            {"表姐","cousin"},{"f.cousin","cousin"},{"晴雯ˇ","qingwen"},{"晴雯","qingwen"},{"林晴雯","qingwen"},
            {"阿杰","ajie"},{"f.ajie","ajie"},{"小刚","xiaogang"},{"f.xiaogang","xiaogang"},{"大伟","dawei"},
            {"a-jie","ajie"},{"xiao gang","xiaogang"},{"da wei","dawei"},{"莉莉","lili"},
            {"老爸","dad"},{"爸爸","dad"},{"妈妈","mom"},{"老妈","mom"},{"二姨","aunt"},{"auntie","aunt"},
            {"王科长","wang"},{"刘师傅","liu"},{"小鹿","xiaolu"},{"阿芳","afang"},
            {"g.family","netbar"},{"jx.team","netbar"},{"g.work","netbar"},
            {"akazhan","@gpu"},{"dengdeng","@desk"},{"yanglaji","@laptop"},{"kedaibiao","@books"},
            {"chigua","xiaogang"},{"haixing","@go"},{"xiezhi","wang"},{"liandan","zhou_now"},{"xiaobai","me"},{"guaji","ajie"}
        };
        public static Sprite Avatar(string id)
        {
            string key=(id??"").Trim().Trim('[',']').ToLowerInvariant();
            if(key=="lingguang"||key=="灵光")return AiJoinsYy.PixelZero();
            if(Aliases.TryGetValue(key,out var alias))key=alias;
            if(key=="lili")return Full("lili");
            if(key.StartsWith("@",StringComparison.Ordinal))return Picture(key.Substring(1));
            int i=Array.IndexOf(People,key);
            return i>=0?Tile("portraits_atlas",i):Picture("news");
        }
        public static Sprite Picture(string key)
        {
            key=(key??"").ToLowerInvariant();
            if(key=="casino")return Full("casino_backdrop");
            if(key=="iphone")return Full("iphone_2016");
            int i=Array.IndexOf(Pictures,key);return i>=0?Tile("content_atlas",i):null;
        }
        static Sprite Full(string file)
        {
            if(cache.TryGetValue(file,out var sprite)&&sprite!=null)return sprite;
            var texture=Resources.Load<Texture2D>(Root+file);if(texture==null)return null;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name=file;sprite.hideFlags=HideFlags.DontSave;cache[file]=sprite;return sprite;
        }
        static Sprite Tile(string file,int index)
        {
            string key=file+":"+index;
            if(cache.TryGetValue(key,out var sprite)&&sprite!=null)return sprite;
            var texture=Resources.Load<Texture2D>(Root+file);if(texture==null)return null;
            float w=texture.width/4f,h=texture.height/4f;
            // Top-to-bottom atlas order; inset avoids adjacent tile bleed at small sizes/mips.
            var rect=new Rect(index%4*w+2,(3-index/4)*h+2,w-4,h-4);
            sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprite.name=key;sprite.hideFlags=HideFlags.DontSave;cache[key]=sprite;return sprite;
        }
        public static Image Paint(RectTransform parent,string key,bool cover=true)
        {
            var sprite=Picture(key);if(parent==null||sprite==null)return null;
            if(cover&&parent.GetComponent<RectMask2D>()==null&&parent.GetComponent<Mask>()==null)parent.gameObject.AddComponent<RectMask2D>();
            var rect=PrologueDesk.Rect("Generated Art "+key,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.preserveAspect=!cover;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
            if(cover){var aspect=rect.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio=sprite.rect.width/sprite.rect.height;}
            rect.SetAsFirstSibling();return image;
        }
    }
}
