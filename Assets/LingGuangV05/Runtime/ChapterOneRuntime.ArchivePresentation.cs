using System;
using System.IO;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed partial class ChapterOneRuntime
    {
        public const int ManualArchiveLimit=20;
        public static Func<byte[]> ArchiveThumbnailProvider;
        static string ThumbnailRevision(string savePath)
        {
            if(!File.Exists(savePath))return null;
            using(var hash=System.Security.Cryptography.SHA256.Create())using(var input=File.OpenRead(savePath))
                return "thumb-v1\n"+File.GetLastWriteTimeUtc(savePath).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+BitConverter.ToString(hash.ComputeHash(input));
        }
        public byte[] ReadArchiveThumbnail(int slot,bool chapter=false)
        {
            var archive=ArchiveStore(slot,chapter);if(archive==null)return null;
            string path=Path.Combine(archive.Folder,"thumb.png"),stamp=Path.Combine(archive.Folder,"thumb.rev");
            try {
                if(!File.Exists(path)||!File.Exists(stamp)||new FileInfo(path).Length>1024*1024||new FileInfo(stamp).Length>512)return null;
                string revision=ThumbnailRevision(archive.SavePath);
                if(revision==null||File.ReadAllText(stamp)!=revision)return null;
                return File.ReadAllBytes(path);
            } catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}
        }
        void InvalidateArchiveThumbnail(int slot)
        {
            var archive=ArchiveStore(slot,false);if(archive==null)return;
            // Revoke the revision first. Even if an image is locked, it cannot be displayed as the new save.
            foreach(string name in new[]{"thumb.rev","thumb.png"})try{string path=Path.Combine(archive.Folder,name);if(File.Exists(path))File.Delete(path);}catch(IOException){}catch(UnauthorizedAccessException){}
        }
        void SetArchiveThumbnail(int slot,byte[] bytes)
        {
            try {
                InvalidateArchiveThumbnail(slot);
                if(bytes==null||bytes.Length==0||bytes.Length>1024*1024)return;
                var archive=ArchiveStore(slot,false);string file=Path.Combine(archive.Folder,"thumb.png"),stamp=Path.Combine(archive.Folder,"thumb.rev");
                string revision=ThumbnailRevision(archive.SavePath);if(revision==null)return;
                File.WriteAllBytes(file+".tmp",bytes);if(File.Exists(file))File.Replace(file+".tmp",file,null);else File.Move(file+".tmp",file);
                File.WriteAllText(stamp+".tmp",revision);if(File.Exists(stamp))File.Replace(stamp+".tmp",stamp,null);else File.Move(stamp+".tmp",stamp);
            } catch(Exception e) {InvalidateArchiveThumbnail(slot);Debug.LogWarning("[Save UI] Thumbnail unavailable: "+e.Message);}
        }
        void SaveArchiveThumbnail(int slot)
        {
            // A missing/failed capture must not retain an image of a different session. Do not borrow the primary
            // image for a manual save: the current in-memory progress can be newer than the primary on disk.
            try { SetArchiveThumbnail(slot,ArchiveThumbnailProvider?.Invoke()); }
            catch(Exception e) {InvalidateArchiveThumbnail(slot);Debug.LogWarning("[Save UI] Thumbnail unavailable: "+e.Message);}
        }
        [Serializable] public sealed class ArchiveCardInfo { public string name="", savedAt="", revision="", gameDate=""; public int stage=1; public bool exists, readable; }
        [Serializable] sealed class ArchiveLabel { public string name; }
        [Serializable] sealed class ArchiveStage { public int stage=1; }
        public int FirstEmptyManualArchive()
        { for(int i=2;i<=ManualArchiveLimit+1;i++)if(!ArchiveExists(i))return i;return 0; }
        public ArchiveCardInfo DescribeArchiveCard(int slot,bool chapter=false)
        {
            var result=new ArchiveCardInfo{name=slot==1&&!chapter?GameText.T("自动存档","Autosave"):GameText.T("存档 ","Save ")+(slot-1)};
            var archive=ArchiveStore(slot,chapter);if(archive==null)return result;
            result.exists=ArchiveExists(slot,chapter);if(!result.exists)return result;
            var load=archive.Load(ValidArchive);result.readable=load.Success;
            if(!load.Success){result.savedAt=GameText.T("无法读取","Unreadable");return result;}
            try {
                var state=JsonUtility.FromJson<Core.GameState>(load.Payload);
                if(!string.IsNullOrEmpty(state.labState))result.stage=Mathf.Clamp(JsonUtility.FromJson<ArchiveStage>(state.labState)?.stage??1,1,6);
                result.gameDate=Core.GameCalendar.Now(state).ToString("yyyy/MM/dd");
                result.revision=File.GetLastWriteTimeUtc(archive.SavePath+(load.Recovered?".bak":"")).Ticks.ToString();
                result.savedAt=File.GetLastWriteTime(archive.SavePath+(load.Recovered?".bak":"")).ToString("MM/dd HH:mm");
                string label=Path.Combine(archive.Folder,"label.json");
                if(File.Exists(label)&&new FileInfo(label).Length<=4096){var m=JsonUtility.FromJson<ArchiveLabel>(File.ReadAllText(label));if(!string.IsNullOrWhiteSpace(m?.name))result.name=m.name;}
            } catch(Exception e) when(e is IOException||e is ArgumentException||e is UnauthorizedAccessException) { Debug.LogWarning("[Save UI] Metadata unavailable: "+e.Message); }
            return result;
        }
        public bool RenameManualArchive(int slot,string name)
        {
            if(slot<2||slot>ManualArchiveLimit+1||!ArchiveExists(slot)||!useDiskSave)return false;
            name=(name??"").Trim();if(name.Length<1||name.Length>12||name.IndexOfAny(new[]{'\r','\n','\t'})>=0)return false;
            var archive=ArchiveStore(slot,false);string file=Path.Combine(archive.Folder,"label.json"),temp=file+".tmp";
            try { File.WriteAllText(temp,JsonUtility.ToJson(new ArchiveLabel{name=name}));if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);return true; }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){SaveStatus=GameText.T("命名未保存：","Name not saved: ")+e.Message;return false;}
        }
        // A confirmed delete hides only this manual slot; the full directory remains recoverable.
        public bool RemoveManualArchive(int slot)
        {
            if(slot<2||slot>ManualArchiveLimit+1||!useDiskSave||!ArchiveExists(slot))return false;
            var archive=ArchiveStore(slot,false);
            try { Directory.Move(archive.Folder,archive.Folder+".removed-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));return true; }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){SaveStatus=GameText.T("删除未完成：","Delete not completed: ")+e.Message;return false;}
        }
    }
}
