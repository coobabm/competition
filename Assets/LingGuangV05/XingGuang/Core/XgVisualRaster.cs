using System;
namespace LingGuangV05.XingGuang
{
    /// <summary>Original procedural teaching artwork. Not MNIST samples, photographs or generated external assets.</summary>
    public static class XgVisualRaster
    {
        public const string Alphabet="23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        static readonly string[] Glyphs={
            "01110/10001/00001/00010/00100/01000/11111","11110/00001/00001/01110/00001/00001/11110","00010/00110/01010/10010/11111/00010/00010","11111/10000/10000/11110/00001/00001/11110","01110/10000/10000/11110/10001/10001/01110","11111/00001/00010/00100/01000/01000/01000","01110/10001/10001/01110/10001/10001/01110","01110/10001/10001/01111/00001/00001/01110",
            "01110/10001/10001/11111/10001/10001/10001","11110/10001/10001/11110/10001/10001/11110","01111/10000/10000/10000/10000/10000/01111","11110/10001/10001/10001/10001/10001/11110","11111/10000/10000/11110/10000/10000/11111","11111/10000/10000/11110/10000/10000/10000","01111/10000/10000/10111/10001/10001/01111","10001/10001/10001/11111/10001/10001/10001",
            "00111/00010/00010/00010/00010/10010/01100","10001/10010/10100/11000/10100/10010/10001","10000/10000/10000/10000/10000/10000/11111","10001/11011/10101/10101/10001/10001/10001","10001/11001/11001/10101/10011/10011/10001","11110/10001/10001/11110/10000/10000/10000","01110/10001/10001/10001/10101/10010/01101","11110/10001/10001/11110/10100/10010/10001","01111/10000/10000/01110/00001/00001/11110","11111/00100/00100/00100/00100/00100/00100","10001/10001/10001/10001/10001/10001/01110","10001/10001/10001/10001/10001/01010/00100","10001/10001/10001/10101/10101/11011/10001","10001/10001/01010/00100/01010/10001/10001","10001/10001/01010/00100/00100/00100/00100","11111/00001/00010/00100/01000/10000/11111"};
        static readonly float[][][][] Styles =
        {
            new[] { // 0: generated ellipses (see Zero); entries only count the variants
                new[] { new float[0] }, new[] { new float[0] }, new[] { new float[0] },
            },
            new[] { // 1
                new[] { new[] { .42f, .78f, .55f, .9f, .55f, .1f } },
                new[] { new[] { .52f, .9f, .48f, .1f } },
                new[] { new[] { .4f, .76f, .56f, .9f, .56f, .1f }, new[] { .44f, .1f, .68f, .1f } },
                new[] { new[] { .42f, .76f, .53f, .86f, .6f, .92f, .57f, .5f, .53f, .1f } },
            },
            new[] { // 2
                new[] { new[] { .25f, .72f, .35f, .86f, .55f, .9f, .72f, .8f, .72f, .62f, .25f, .12f, .78f, .12f } },
                new[] { new[] { .28f, .78f, .45f, .9f, .66f, .86f, .7f, .66f, .5f, .42f, .26f, .12f, .44f, .2f, .36f, .26f, .3f, .14f, .8f, .1f } },
                new[] { new[] { .26f, .8f, .5f, .9f, .72f, .78f, .66f, .58f, .3f, .14f, .76f, .14f } },
            },
            new[] { // 3
                new[] { new[] { .27f, .85f, .7f, .85f, .48f, .55f, .7f, .45f, .72f, .25f, .55f, .1f, .27f, .15f } },
                new[] { new[] { .26f, .78f, .42f, .9f, .64f, .88f, .7f, .72f, .56f, .56f, .44f, .54f, .56f, .52f, .72f, .38f, .7f, .18f, .5f, .08f, .26f, .16f } },
                new[] { new[] { .3f, .84f, .66f, .82f, .46f, .56f }, new[] { .46f, .56f, .7f, .42f, .68f, .2f, .48f, .1f, .28f, .18f } },
            },
            new[] { // 4
                new[] { new[] { .62f, .1f, .62f, .9f, .22f, .35f, .8f, .35f } },
                new[] { new[] { .32f, .9f, .26f, .44f, .8f, .44f }, new[] { .62f, .72f, .6f, .1f } },
                new[] { new[] { .3f, .88f, .28f, .5f, .6f, .5f, .66f, .62f }, new[] { .66f, .9f, .64f, .1f } },
            },
            new[] { // 5
                new[] { new[] { .72f, .88f, .32f, .88f, .28f, .55f, .6f, .58f, .74f, .4f, .68f, .18f, .5f, .1f, .27f, .17f } },
                new[] { new[] { .34f, .88f, .3f, .56f, .52f, .62f, .7f, .5f, .72f, .26f, .52f, .1f, .26f, .2f }, new[] { .34f, .88f, .74f, .9f } },
                new[] { new[] { .7f, .9f, .36f, .86f, .32f, .58f, .46f, .62f, .66f, .56f, .7f, .34f, .56f, .14f, .3f, .14f } },
            },
            new[] { // 6
                new[] { new[] { .68f, .88f, .4f, .65f, .28f, .35f, .35f, .14f, .55f, .1f, .7f, .25f, .65f, .45f, .45f, .5f, .3f, .38f } },
                new[] { new[] { .64f, .92f, .46f, .74f, .32f, .5f, .3f, .26f, .44f, .1f, .62f, .12f, .7f, .28f, .6f, .42f, .4f, .42f, .32f, .3f } },
                new[] { new[] { .56f, .92f, .36f, .56f, .34f, .2f, .5f, .1f, .66f, .22f, .62f, .4f, .42f, .44f, .34f, .32f } },
            },
            new[] { // 7
                new[] { new[] { .25f, .88f, .75f, .88f, .45f, .1f } },
                new[] { new[] { .25f, .86f, .76f, .88f, .52f, .44f, .42f, .1f }, new[] { .34f, .5f, .68f, .5f } },
                new[] { new[] { .24f, .74f, .27f, .88f, .76f, .88f, .5f, .1f } },
            },
            new[] { // 8
                new[] { new[] { .5f, .52f, .3f, .68f, .36f, .86f, .5f, .9f, .64f, .86f, .7f, .68f, .5f, .52f, .28f, .35f, .32f, .14f, .5f, .1f, .68f, .14f, .72f, .35f, .5f, .52f } },
                new[] { new[] { .64f, .82f, .5f, .9f, .34f, .82f, .36f, .64f, .66f, .4f, .68f, .18f, .5f, .08f, .32f, .18f, .34f, .4f, .64f, .64f, .64f, .82f } },
            },
            new[] { // 9
                new[] { new[] { .7f, .62f, .55f, .5f, .35f, .55f, .3f, .72f, .42f, .88f, .6f, .88f, .7f, .72f, .68f, .4f, .6f, .1f } },
                new[] { new[] { .7f, .72f, .58f, .88f, .38f, .86f, .3f, .7f, .4f, .56f, .6f, .56f, .7f, .7f, .7f, .1f } },
                new[] { new[] { .68f, .76f, .5f, .9f, .32f, .8f, .32f, .62f, .5f, .54f, .68f, .66f, .66f, .4f, .5f, .1f } },
            },
        };

        public static byte[] Handwriting28(int digit,int seed)
        {
            digit=Math.Max(0,Math.Min(9,digit));var random=new XgTinyRandom(seed);var strokes=Styles[digit][random.Below(Styles[digit].Length)];
            double slant=random.Range(-.16,.16),width=random.Range(.94,1.06),radius=random.Range(.026,.043);var pixels=new byte[28*28];
            for(int y=0;y<28;y++)for(int x=0;x<28;x++)
            {
                double yy=1-(y+.5)/28,xx=((x+.5)/28-.5)/width+.5+slant*(yy-.5),distance=100;
                if(digit==0)distance=Math.Abs(Math.Sqrt((xx-.5)*(xx-.5)/(.24*.24)+(yy-.5)*(yy-.5)/(.39*.39))-1)*.24;
                else foreach(var stroke in strokes)for(int i=2;i<stroke.Length;i+=2)
                {
                    double ax=stroke[i-2],ay=stroke[i-1],dx=stroke[i]-ax,dy=stroke[i+1]-ay;
                    double t=Math.Max(0,Math.Min(1,((xx-ax)*dx+(yy-ay)*dy)/Math.Max(.00001,dx*dx+dy*dy)));
                    double rx=xx-ax-t*dx,ry=yy-ay-t*dy;distance=Math.Min(distance,Math.Sqrt(rx*rx+ry*ry));
                }
                pixels[y*28+x]=(byte)(255*Math.Max(0,Math.Min(1,(radius+.018-distance)/.036)));
            }
            return pixels;
        }
        public static string Code(int seed)
        {
            var random=new XgTinyRandom(seed);var chars=new[]{Alphabet[random.Below(8)],Alphabet[8+random.Below(Alphabet.Length-8)],Alphabet[random.Below(Alphabet.Length)],Alphabet[random.Below(Alphabet.Length)]};
            for(int i=3;i>0;i--){int j=random.Below(i+1);char c=chars[i];chars[i]=chars[j];chars[j]=c;}return new string(chars);
        }
        public const int CaptchaWidth=256,CaptchaHeight=80;
        public static byte[] Captcha(string code,int seed)
        {
            if(code==null||code.Length!=4)throw new ArgumentException("Four supported characters required");
            var random=new XgTinyRandom(seed);var pixels=new byte[CaptchaWidth*CaptchaHeight];
            for(int i=0;i<4;i++)
            {
                int index=Alphabet.IndexOf(code[i]);if(index<0)throw new ArgumentException("Unsupported captcha character");
                var rows=Glyphs[index].Split('/');double slope=random.Range(-.12,.12),phase=random.Range(0,6.28);int top=12+random.Below(7);
                for(int y=0;y<49;y++)for(int x=0;x<35;x++)if(rows[y/7][x/7]=='1')
                {
                    int xx=20+i*58+x+(int)Math.Round(2*Math.Sin(y*.16+phase)+slope*(y-24)),yy=top+y+(int)Math.Round(2*Math.Sin(x*.12+phase));
                    if(xx>=0&&xx<CaptchaWidth&&yy>=0&&yy<CaptchaHeight)pixels[yy*CaptchaWidth+xx]=235;
                }
            }
            // Light crossing lines remain distinguishable from the dark character strokes.
            for(int line=0;line<3;line++){double a=random.Range(8,72),b=random.Range(8,72);for(int x=0;x<CaptchaWidth;x++){int y=(int)(a+(b-a)*x/(CaptchaWidth-1));pixels[y*CaptchaWidth+x]=Math.Max(pixels[y*CaptchaWidth+x],(byte)105);}}
            return pixels;
        }
    }
}
