#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
// Reads the desktop's actual rendered monitor surface; no scene or UI mutation.
// Unlike batch ScreenCapture, this does not wait for a visible GameView window.
public static class QaDesktopCapture
{
    public static bool Save(string path)
    {
        RenderTexture source=null;
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            var target=camera.targetTexture;
            if(!camera.isActiveAndEnabled||target==null||!target.IsCreated()||target.width<256||target.height<256)continue;
            if(camera.name.IndexOf("LCD",StringComparison.OrdinalIgnoreCase)>=0){source=target;break;}
            if(source==null||target.width*target.height>source.width*source.height)source=target;
        }
        if(source==null)return false;
        var temporary=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
        var before=RenderTexture.active;Texture2D image=null;
        try
        {
            Graphics.Blit(source,temporary);RenderTexture.active=temporary;
            image=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,source.width,source.height),0,0);image.Apply();
            var pixels=image.GetPixels32();var first=pixels[0];bool varied=false;
            for(int i=0;i<pixels.Length;i+=97)if(Math.Abs(pixels[i].r-first.r)+Math.Abs(pixels[i].g-first.g)+Math.Abs(pixels[i].b-first.b)>24){varied=true;break;}
            if(!varied)return false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path,image.EncodeToPNG());
            return File.Exists(path)&&new FileInfo(path).Length>100;
        }
        finally{RenderTexture.active=before;if(image!=null)UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(temporary);}
    }
}
#endif
