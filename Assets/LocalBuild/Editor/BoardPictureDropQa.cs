#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Fast pixel/drag lifecycle checks, not a substitute for Main-scene pointer/raycast QA.</summary>
public static class BoardPictureDropQa
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static void Set(object target,Type type,string name,object value)=>type.GetProperty(name,Flags).GetSetMethod(true).Invoke(target,new[]{value});
    static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);Debug.Log("PASS BoardPictureDropQa: "+label);}
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in Edit Mode");
        var old=XingGuangController.Instance;
        var root=new GameObject("Board picture QA isolated objects");Texture2D rawTexture=null,changed=null;
        try
        {
            var ctl=root.AddComponent<XingGuangController>();ctl.enabled=false;
            Set(null,typeof(XingGuangController),"Instance",ctl);
            var sim=new XgSim();sim.S.stage=3;Set(ctl,typeof(XingGuangController),"Sim",sim);
            var eventObject=new GameObject("Events",typeof(EventSystem));eventObject.transform.SetParent(root.transform);
            var sourceObject=new GameObject("Source",typeof(RectTransform),typeof(Image));sourceObject.transform.SetParent(root.transform);
            var source=sourceObject.AddComponent<XgBoardPictureDrag>();var image=sourceObject.GetComponent<Image>();image.sprite=DesktopMedia.Picture(BoardClubPicture.Key);
            var targetObject=new GameObject("Target",typeof(RectTransform),typeof(Image));targetObject.transform.SetParent(root.transform);
            var target=targetObject.AddComponent<XgBoardPictureDrop>();target.Owner=sim;
            var e=new PointerEventData(eventObject.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerDrag=sourceObject};
            int accepted=0;byte[] received=null;target.Accept=rgb=>{accepted++;received=rgb;};
            source.OnBeginDrag(e);target.OnDrop(e);Check(accepted==0,"Unpurchased Lv2 rejects the real drag");source.OnEndDrag(e);
            sim.S.unlocked.Add("board.logic2");double samples=sim.Labels("go");
            source.OnBeginDrag(e);target.OnDrop(e);Check(accepted==1&&GomokuPicture.TryRead(received,240,240,out var board),"Public Image reads actual pixels");
            GomokuPicture.TryRead(received,240,240,out board);
            Check(board.Moves==8&&board[3,5]==1&&board[1,1]==2&&board[3,9]==0,"Top-down public image orientation is correct");
            GomokuLogic.Pick(board,1,2,new System.Random(1),null,out int x,out int y);Check(GomokuLogic.Point(x,y)=="F6","Solver independently finds the winning move");
            target.OnDrop(e);Check(accepted==1,"One drag is consumed once");
            source.OnBeginDrag(e);sourceObject.SetActive(false);target.OnDrop(e);Check(accepted==1,"Hidden source rejects payload");sourceObject.SetActive(true);
            source.OnBeginDrag(e);targetObject.SetActive(false);target.OnDrop(e);Check(accepted==1,"Hidden target rejects payload");targetObject.SetActive(true);source.OnEndDrag(e);
            source.OnBeginDrag(e);var other=new XgSim();other.S.unlocked.Add("board.logic2");Set(ctl,typeof(XingGuangController),"Sim",other);target.Owner=other;target.OnDrop(e);Check(accepted==1,"Old source drag cannot cross saves");
            source.OnBeginDrag(e);target.Owner=sim;target.OnDrop(e);Check(accepted==1,"Old target cannot accept a new save's image");source.OnEndDrag(e);
            Set(ctl,typeof(XingGuangController),"Sim",sim);target.Owner=sim;
            source.OnBeginDrag(e);var prior=sim.S;var replacement=new XgState();replacement.unlocked.Add("board.logic2");Set(sim,typeof(XgSim),"S",replacement);target.OnDrop(e);Check(accepted==1,"State replacement on the same Sim rejects stale pixels");Set(sim,typeof(XgSim),"S",prior);
            changed=new Texture2D(240,240,TextureFormat.RGB24,false);var colors=image.sprite.texture.GetPixels32();colors[(239-(5*16+8-3))*240+(3*16+8-3)]=Color.magenta;changed.SetPixels32(colors);changed.Apply();
            var changedSprite=Sprite.Create(changed,new Rect(0,0,240,240),new Vector2(.5f,.5f));image.sprite=changedSprite;
            source.OnBeginDrag(e);target.OnDrop(e);Check(accepted==2&&!GomokuPicture.TryRead(received,240,240,out _),"Corrupt pixels fail even with unchanged source component and image size");UnityEngine.Object.DestroyImmediate(changedSprite);
            UnityEngine.Object.DestroyImmediate(image);
            var raw=sourceObject.AddComponent<RawImage>();rawTexture=new Texture2D(240,240,TextureFormat.RGB24,false);rawTexture.LoadRawTextureData(GomokuPicture.Render(board));rawTexture.Apply();raw.texture=rawTexture;raw.uvRect=new Rect(0,1,1,-1);
            source.OnBeginDrag(e);target.OnDrop(e);Check(accepted==3&&GomokuPicture.TryRead(received,240,240,out var rawBoard)&&rawBoard[3,5]==1,"Existing inverted question RawImage keeps correct orientation");
            Check(sim.Labels("go")==samples&&sim.S.boardRounds.Count==0&&sim.S.dataFiles.Count==0,"Reading awards no samples, records or packs");
            Debug.Log("PASS BoardPictureDropQa complete. Main-scene cross-window UI remains a separate check.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);if(rawTexture!=null)UnityEngine.Object.DestroyImmediate(rawTexture);if(changed!=null)UnityEngine.Object.DestroyImmediate(changed);
            Set(null,typeof(XingGuangController),"Instance",old);
        }
    }
}
#endif
