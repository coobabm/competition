using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.UI.Tests
{
    public class DesktopMediaTests
    {
        static Type Media=>Type.GetType("LingGuangV05.Desktop.Media.DesktopMedia, Assembly-CSharp",true);
        static Sprite Avatar(string id)=>(Sprite)Media.GetMethod("Avatar").Invoke(null,new object[]{id});
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
