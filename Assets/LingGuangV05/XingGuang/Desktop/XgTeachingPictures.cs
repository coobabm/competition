using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>One readable image source shared by the question card, marked sheet and attention view.</summary>
    public static class XgTeachingPictures
    {
        static readonly Dictionary<string,Texture2D> Cache=new Dictionary<string,Texture2D>();
        static bool TryTexture(string key,out Texture2D texture) { return Cache.TryGetValue(key,out texture) && texture!=null; }
        public static readonly string[] PhotoKeys={"avatar_guaji","avatar_ajie","avatar_afang","avatar_aunt","avatar_akazhan","product_case"};
        public static readonly string[] PhotoWords={"猫","手柄","牡丹","玫瑰","显卡","机箱"};
        public static readonly string[] PhotoWordsEn={"Cat","Controller","Peony","Rose","Graphics card","Computer case"};
        public static readonly string[] LibraryCategories={"airplane","car","cat","dog","red_wine","towel","phone","apple"};
        public static readonly string[] LibraryWords={"飞机","汽车","猫","狗","红酒","毛巾","手机","苹果（水果）"};
        public static readonly string[] LibraryWordsEn={"Airplane","Car","Cat","Dog","Red wine","Towel","Phone","Apple (fruit)"};
        public const int PhotosPerCategory=10;
        public static Texture2D ForCard(XgCard card)
        {
            if(card==null)return null;
            if(card.teachingImageKind=="photo80")return LibraryPhoto(card.teachingImageKey);
            if(card.teachingImageKind=="grid80")return LibraryGrid(card.teachingImageKeys);
            if(card.teachingImageKind=="handwriting28shift")return HandwritingVariant28(card.digit,card.seed,0);
            if(card.teachingImageKind=="handwriting28")return Handwriting28(card.digit,card.seed);
            if(card.teachingImageKind=="captcha4")return Captcha4(card.seed);
            if(!string.IsNullOrEmpty(card.patternA))return Pattern(card.patternA);
            if(card.dataset=="mnist")return Digit(card.digit,card.seed);
            if(card.teachingImageKind=="grid")return Grid(card.teachingImageKeys);
            if(card.teachingImageKind=="face")return Face(card.teachingImageKey);
            if(card.teachingImageKind=="meme_couch_2016"||card.teachingImageKind=="meme_power_2016")return PeriodMeme(card.teachingImageKind)??Face(card.teachingImageKey);
            if(card.teachingImageKind=="photo")return Photo(card.teachingImageKey);
            return null;
        }
        public static Texture2D ForNote(XgTeachingNote note)
        {
            if(note==null)return null;
            return ForCard(new XgCard{dataset=note.dataset,seed=note.seed,digit=note.digit,patternA=note.patternA,
                teachingImageKind=note.teachingImageKind,teachingImageKey=note.teachingImageKey,
                teachingImageKeys=note.teachingImageKeys,teachingPosition=note.teachingPosition});
        }
        public static Texture2D Grid(int[] keys)
        {
            if(keys==null||keys.Length!=9)return null;
            string key="grid:"+string.Join(",",keys);if(TryTexture(key,out var cached))return cached;
            var photos=new Texture2D[9];
            for(int i=0;i<9;i++){if(keys[i]<0||keys[i]>=PhotoKeys.Length)return null;photos[i]=Photo(keys[i]);if(photos[i]==null)return null;}
            const int side=192,tile=64;var pixels=new Color[side*side];
            for(int i=0;i<9;i++)for(int y=0;y<tile;y++)for(int x=0;x<tile;x++)
                pixels[((2-i/3)*tile+y)*side+(i%3)*tile+x]=photos[i].GetPixelBilinear((x+.5f)/tile,(y+.5f)/tile);
            var texture=new Texture2D(side,side,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();return Keep(key,texture);
        }
        static Texture2D Keep(string key,Texture2D texture)
        {
            // Never destroy a texture while a visible card or marked sheet holds it.
            texture.name="Teaching "+key;texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            Cache[key]=texture;return texture;
        }
        public static Texture2D Handwriting28(int digit,int seed)
        {
            string key="handwriting28:"+digit+":"+(seed&int.MaxValue)%16;if(TryTexture(key,out var texture))return texture;
            var ink=XgVisualRaster.Handwriting28(digit,(seed&int.MaxValue)%16);const int side=224;var colors=new Color[side*side];
            for(int y=0;y<side;y++)for(int x=0;x<side;x++)
            {
                float v=ink[(27-y/8)*28+x/8]/255f;var color=Color.Lerp(new Color(.98f,.97f,.94f),new Color(.1f,.12f,.15f),v);
                if(x%8==0||y%8==0)color=Color.Lerp(color,new Color(.62f,.67f,.69f),.25f);colors[y*side+x]=color;
            }
            texture=new Texture2D(side,side,TextureFormat.RGBA32,false);texture.SetPixels(colors);texture.Apply();Keep(key,texture);texture.filterMode=FilterMode.Point;return texture;
        }
        public static Texture2D HandwritingVariant28(int digit,int seed,int variant)
        {
            seed=(seed&int.MaxValue)%16;string key="handwriting-variant:"+digit+":"+seed+":"+variant;if(TryTexture(key,out var cached))return cached;
            var ink=XgTeachingAugmentation.Transform(XgVisualRaster.Handwriting28(digit,seed),variant);var colors=new Color[784];
            for(int y=0;y<28;y++)for(int x=0;x<28;x++)colors[y*28+x]=Color.Lerp(new Color(.98f,.97f,.94f),new Color(.1f,.12f,.15f),ink[(27-y)*28+x]/255f);
            var texture=new Texture2D(28,28,TextureFormat.RGBA32,false);texture.SetPixels(colors);texture.Apply();Keep(key,texture);texture.filterMode=FilterMode.Point;return texture;
        }
        public static Texture2D Captcha4(int seed)
        {
            seed=(seed&int.MaxValue)%128;string key="captcha4:"+seed;if(TryTexture(key,out var texture))return texture;
            var ink=XgVisualRaster.Captcha(XgVisualRaster.Code(seed),seed);int w=XgVisualRaster.CaptchaWidth,h=XgVisualRaster.CaptchaHeight;var colors=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)colors[y*w+x]=Color.Lerp(new Color(.97f,.95f,.89f),new Color(.12f,.2f,.31f),ink[(h-1-y)*w+x]/255f);
            texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.SetPixels(colors);texture.Apply();return Keep(key,texture);
        }
        public static Texture2D Digit(int digit,int seed)
        {
            seed=(seed&int.MaxValue)%16;
            string key="digit:"+digit+":"+seed;if(TryTexture(key,out var cached))return cached;
            var sample=XgTinyData.Digit(Math.Max(0,Math.Min(9,digit)),seed);const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {double ink=sample.pixels[Math.Min(7,(size-1-y)*8/size)*8+Math.Min(7,x*8/size)];pixels[y*size+x]=Color.Lerp(new Color(.97f,.96f,.92f),new Color(.12f,.13f,.17f),(float)ink);}
            texture.SetPixels(pixels);texture.Apply();return Keep(key,texture);
        }
        public static Texture2D Pattern(string pattern)
        {
            string key="pattern:"+pattern;if(TryTexture(key,out var cached))return cached;
            var cells=new List<char>();foreach(char c in pattern)if(!char.IsWhiteSpace(c)&&c!='/'&&c!='|')cells.Add(c);
            int side=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(cells.Count)));var tex=new Texture2D(side,side,TextureFormat.RGBA32,false);var pix=new Color[side*side];
            for(int i=0;i<pix.Length;i++)pix[(side-1-i/side)*side+i%side]=i<cells.Count&&cells[i]!='0'&&cells[i]!='.'?new Color(.15f,.25f,.4f):Color.white;
            tex.SetPixels(pix);tex.Apply();return Keep(key,tex);
        }
        public static Texture2D Photo(int index)
        {
            index=Math.Abs(index)%PhotoKeys.Length;string key="photo:"+PhotoKeys[index];if(TryTexture(key,out var cached))return cached;
            var original=Resources.Load<Texture2D>("LingGuangV05/Media/Photos/"+PhotoKeys[index]);if(original==null)return null;
            var target=RenderTexture.GetTemporary(192,192,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
            try{Graphics.Blit(original,target);RenderTexture.active=target;var texture=new Texture2D(192,192,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,192,192),0,0);texture.Apply();return Keep(key,texture);}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
        }
        // New photo indices are separate from the six legacy keys, preserving saved notebooks.
        public static Texture2D LibraryPhoto(int index)
        {
            if(index<0||index>=LibraryCategories.Length*PhotosPerCategory)return null;
            string asset="teach_"+LibraryCategories[index/PhotosPerCategory]+"_"+(index%PhotosPerCategory).ToString("00");
            string key="photo80:"+asset;if(TryTexture(key,out var cached))return cached;
            var original=Resources.Load<Texture2D>("LingGuangV05/Media/Photos/"+asset);if(original==null)return null;
            var target=RenderTexture.GetTemporary(192,192,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
            try
            {
                // Fit the whole photograph rather than stretching a phone or cropping off an aircraft.
                RenderTexture.active=target;GL.Clear(true,true,Color.white);GL.PushMatrix();GL.LoadPixelMatrix(0,192,192,0);
                try {float scale=Mathf.Min(192f/original.width,192f/original.height);float w=original.width*scale,h=original.height*scale;Graphics.DrawTexture(new Rect((192-w)/2,(192-h)/2,w,h),original);}
                finally{GL.PopMatrix();}
                var texture=new Texture2D(192,192,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,192,192),0,0);texture.Apply();return Keep(key,texture);
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
        }
        public static Texture2D LibraryGrid(int[] keys)
        {
            if(keys==null||keys.Length!=9)return null;
            string key="grid80:"+string.Join(",",keys);if(TryTexture(key,out var cached))return cached;
            var photos=new Texture2D[9];for(int i=0;i<9;i++){photos[i]=LibraryPhoto(keys[i]);if(photos[i]==null)return null;}
            const int side=192,tile=64;var pixels=new Color[side*side];
            for(int i=0;i<9;i++)for(int y=0;y<tile;y++)for(int x=0;x<tile;x++)pixels[((2-i/3)*tile+y)*side+(i%3)*tile+x]=photos[i].GetPixelBilinear((x+.5f)/tile,(y+.5f)/tile);
            var texture=new Texture2D(side,side,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();return Keep(key,texture);
        }
        public static Texture2D PeriodMeme(string kind)
        {
            string file=kind=="meme_couch_2016"?"teach_meme_couch_original":kind=="meme_power_2016"?"teach_meme_power_original":null;
            return file==null?null:Resources.Load<Texture2D>("LingGuangV05/Media/Photos/"+file);
        }
        public static Texture2D Face(int mood)
        {
            mood=Math.Abs(mood)%6;string key="face:"+mood;if(TryTexture(key,out var cached))return cached;
            const int s=128;var pix=new Color[s*s];
            for(int y=0;y<s;y++)for(int x=0;x<s;x++)
            {
                float xx=(x-64)/54f,yy=(y-64)/54f;bool face=xx*xx+yy*yy<1;
                Color col=face?new Color(1,.81f,.29f):Color.clear;
                bool eye=Math.Abs(yy-.24f)<.10f&&(Math.Abs(xx-.32f)<.07f||Math.Abs(xx+.32f)<.07f);
                if(mood==4)eye=Math.Abs(yy-.24f)<.025f&&(Math.Abs(xx-.32f)<.15f||Math.Abs(xx+.32f)<.15f);
                bool mouth=mood==0?Math.Abs(yy-(-.5f+.5f*xx*xx))<.035f&&Math.Abs(xx)<.5f
                    :mood==1?Math.Abs(yy-(-.3f-.5f*xx*xx))<.035f&&Math.Abs(xx)<.5f
                    :mood==2?Math.Abs(xx*xx+(yy+.4f)*(yy+.4f)-.035f)<.012f
                    :mood==3?Math.Abs(yy+.42f)<.04f&&Math.Abs(xx)<.45f
                    :mood==4?Math.Abs(xx*xx+(yy+.4f)*(yy+.4f)-.045f)<.012f
                    :Math.Abs(yy-(-.35f-.25f*xx))<.04f&&Math.Abs(xx)<.45f;
                if(face&&(eye||mouth))col=new Color(.22f,.15f,.08f);
                if(mood==1&&face&&yy<.12f&&yy>-.28f&&Math.Abs(xx+.32f)<.04f)col=new Color(.2f,.55f,.95f);
                if(mood==3&&face&&Math.Abs(yy-(.30f+.35f*Math.Abs(xx)))<.025f&&Math.Abs(xx)<.52f)col=Color.black;
                // Closed eyes + yawn: sleepy. Uneven eyebrows + tilted mouth: confused.
                if(mood==5&&face&&((xx<0&&Math.Abs(yy-(.48f+.2f*xx))<.028f&&xx>-.52f)||(xx>0&&Math.Abs(yy-(.40f-.2f*xx))<.028f&&xx<.52f)))col=Color.black;
                pix[y*s+x]=col;
            }
            var tex=new Texture2D(s,s,TextureFormat.RGBA32,false);tex.SetPixels(pix);tex.Apply();return Keep(key,tex);
        }
    }
}
