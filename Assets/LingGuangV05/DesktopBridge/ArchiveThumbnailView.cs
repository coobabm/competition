using LingGuangV05.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    // Owns only decoded thumbnail textures; shared fallback art remains owned by Resources.
    [RequireComponent(typeof(RawImage))]
    public sealed class ArchiveThumbnailView : MonoBehaviour
    {
        Texture2D decoded;
        int boundSlot=-1;bool boundChapter;ChapterOneRuntime boundRuntime;
        string stamp;
        public void Bind(ChapterOneRuntime runtime,int slot,bool chapter=false,string changedAt=null)
        {
            if(boundRuntime==runtime&&boundSlot==slot&&boundChapter==chapter&&stamp==changedAt)return;
            boundRuntime=runtime;boundSlot=slot;boundChapter=chapter;stamp=changedAt;
            if(decoded!=null){Release(decoded);decoded=null;}
            var image=GetComponent<RawImage>();image.raycastTarget=false;
            image.texture=Resources.Load<Texture2D>("LingGuangV05/Branding/ArchiveRoomFallback");
            // A missing package resource must not render RawImage's solid white default.
            image.enabled=image.texture!=null;
            byte[] bytes=runtime.ReadArchiveThumbnail(slot,chapter);
            // Only our fixed-size PNG sidecars may be decoded. A hand-edited file cannot allocate a huge image.
            if(bytes==null||bytes.Length<24||bytes[0]!=137||bytes[1]!=80||bytes[2]!=78||bytes[3]!=71)return;
            int w=(bytes[16]<<24)|(bytes[17]<<16)|(bytes[18]<<8)|bytes[19];
            int h=(bytes[20]<<24)|(bytes[21]<<16)|(bytes[22]<<8)|bytes[23];
            if(w!=384||h!=216)return;
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(texture.LoadImage(bytes)){decoded=texture;image.texture=decoded;image.enabled=true;}else Release(texture);
        }
        static void Release(Texture2D texture){if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
        void OnDestroy(){if(decoded!=null)Release(decoded);}
    }
}
