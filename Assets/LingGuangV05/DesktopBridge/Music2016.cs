using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.Networking;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop
{
    /// <summary>
    /// The music player's "2016 热歌" playlist. The recordings are copyrighted, so the game ships none: it lists the
    /// hits of 2016 and earlier, and plays the ones the player has put in the Music2016 folder next to the save
    /// (file names "歌手 - 歌名.mp3", .ogg or .wav; anything else in the folder is added as it is). On the first run the
    /// folder gets a 歌单.txt with the expected file names. The native demo library stays as it was.
    /// </summary>
    public static class Music2016
    {
        public sealed class Hit
        {
            public string title, artist, album; public int year;
            public Hit(string title, string artist, string album, int year) { this.title = title; this.artist = artist; this.album = album; this.year = year; }
            public string FileStem => artist + " - " + title;
        }

        /// <summary>Hits people were singing in 2016 (released 2016 or earlier).</summary>
        public static readonly Hit[] Hits =
        {
            new Hit("告白气球", "周杰伦", "周杰伦的床边故事", 2016),
            new Hit("演员", "薛之谦", "绅士", 2015),
            new Hit("小幸运", "田馥甄", "我的少女时代 电影原声带", 2015),
            new Hit("不为谁而作的歌", "林俊杰", "和自己对话", 2015),
            new Hit("李白", "李荣浩", "模特", 2013),
            new Hit("南山南", "马頔", "孤岛", 2014),
            new Hit("小苹果", "筷子兄弟", "老男孩之猛龙过江 电影原声", 2014),
            new Hit("平凡之路", "朴树", "后会无期 电影原声", 2014),
            new Hit("匆匆那年", "王菲", "匆匆那年 电影原声", 2014),
            new Hit("当你老了", "莫文蔚", "当你老了", 2015),
            new Hit("奇妙能力歌", "陈粒", "如也", 2015),
            new Hit("董小姐", "宋冬野", "安和桥北", 2013),
            new Hit("泡沫", "邓紫棋", "Xposed", 2012),
            new Hit("突然好想你", "五月天", "后青春期的诗", 2008),
            new Hit("最炫民族风", "凤凰传奇", "最炫民族风", 2009),
            new Hit("青春修炼手册", "TFBOYS", "青春修炼手册", 2014),
            new Hit("刚好遇见你", "李玉刚", "刚好遇见你", 2016),
            new Hit("十年", "陈奕迅", "黑·白·灰", 2003),
            new Hit("晴天", "周杰伦", "叶惠美", 2003),
            new Hit("存在", "汪峰", "生无所求", 2011),
        };

        public static readonly string[] Extensions = { ".mp3", ".ogg", ".wav" };
        public static string Folder => Path.Combine(Application.persistentDataPath, "Music2016");

        /// <summary>Finds the native music player and adds the playlist once the clips are loaded.</summary>
        public static IEnumerator Install()
        {
            var manager = Array.Find(UnityEngine.Object.FindObjectsByType<MusicPlayerManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), m => m != null);
            if (manager == null) yield break;
            try { Directory.CreateDirectory(Folder); WriteGuide(); }
            catch (Exception e) { Debug.LogWarning("Music2016: cannot prepare " + Folder + ": " + e.Message); yield break; }

            var playlist = ScriptableObject.CreateInstance<MusicPlayerPlaylist>();
            playlist.hideFlags = HideFlags.DontSave;
            playlist.playlistName = Lang.T("2016 热歌");
            var fallbackCover = manager.libraryPlaylist != null ? manager.libraryPlaylist.coverImage : null;
            playlist.coverImage = fallbackCover;
            foreach (var file in Directory.GetFiles(Folder))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (Array.IndexOf(Extensions, ext) < 0) continue;
                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, ext == ".mp3" ? AudioType.MPEG : ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { Debug.LogWarning("Music2016: cannot load " + file + ": " + request.error); continue; }
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip == null) continue;
                    string stem = Path.GetFileNameWithoutExtension(file);
                    clip.name = stem;
                    var hit = Array.Find(Hits, h => string.Equals(h.FileStem, stem, StringComparison.OrdinalIgnoreCase));
                    Split(stem, out string artist, out string title);
                    playlist.playlist.Add(new MusicPlayerPlaylist.MusicItem
                    {
                        musicTitle = hit != null ? hit.title : title,
                        artistTitle = hit != null ? hit.artist : artist,
                        albumTitle = hit != null ? hit.album + " · " + hit.year : Lang.T("本地音乐"),
                        musicClip = clip,
                        musicCover = fallbackCover,
                        excludeFromLibrary = true,
                    });
                }
            }
            if (playlist.playlist.Count == 0) yield break;
            manager.customPlaylists.Add(playlist);
            // The player builds its lists in Awake; if that has already happened, add this one now.
            if (manager.libraryParent != null && manager.libraryParent.childCount > 0) manager.InstantiatePlaylist(playlist);
        }

        static void Split(string stem, out string artist, out string title)
        {
            int dash = stem.IndexOf(" - ", StringComparison.Ordinal);
            if (dash > 0) { artist = stem.Substring(0, dash).Trim(); title = stem.Substring(dash + 3).Trim(); }
            else { artist = ""; title = stem; }
        }

        /// <summary>歌单.txt: what to put in the folder. Written once; the player may edit or delete it.</summary>
        static void WriteGuide()
        {
            string guide = Path.Combine(Folder, "歌单.txt");
            if (File.Exists(guide)) return;
            var sb = new StringBuilder();
            sb.AppendLine("2016 热歌 · 把你自己的正版音频文件放进这个文件夹，重开游戏后会出现在「音乐播放器」的「2016 热歌」歌单里。");
            sb.AppendLine("Put your own legally obtained audio files in this folder; after restarting the game they appear in the music player's \"2016 hits\" playlist.");
            sb.AppendLine("文件名 / file name: 歌手 - 歌名.mp3（也可以 .ogg / .wav）");
            sb.AppendLine();
            foreach (var h in Hits) sb.AppendLine(h.FileStem + ".mp3    （" + h.album + "，" + h.year + "）");
            File.WriteAllText(guide, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
