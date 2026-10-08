using System;
using System.IO;
using LingGuangV05.Core;
using UnityEngine;

namespace LingGuangV05.Runtime.Persistence
{
    /// <summary>
    /// <c>player-profile.json</c>, next to the save in the game's own save folder: that the opening story was finished
    /// once, and the setup the player chose in it. It belongs to the player, not to one game, so 重新开始, a reset or
    /// a deleted save does not lose it. Written by the game when the opening's setup completes (or when an older save
    /// proves it was finished); read when a new game starts. The previous file is kept as <c>.bak</c>; nothing is deleted.
    /// </summary>
    public sealed class PlayerProfileStore
    {
        public const string FileName = "player-profile.json";
        readonly AtomicSaveStore saves;
        public string Path { get; }

        public PlayerProfileStore(AtomicSaveStore store)
        {
            saves = store ?? throw new ArgumentNullException(nameof(store));
            Path = System.IO.Path.Combine(store.Folder, FileName);
        }

        /// <summary>The stored profile, or null if there is none or it cannot be used.</summary>
        public PlayerProfile Read()
        {
            try
            {
                if (!File.Exists(Path)) return null;
                var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(Path));
                return PlayerProfile.IsUsable(profile) ? profile : null;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { return null; }
        }

        public bool Write(PlayerProfile profile, out string message)
        {
            message = "";
            if (!PlayerProfile.IsUsable(profile)) { message = "玩家档案无效，未写入。"; return false; }
            string temp = Path + ".tmp";
            try
            {
                Directory.CreateDirectory(saves.Folder);
                File.WriteAllText(temp, JsonUtility.ToJson(profile, true), new System.Text.UTF8Encoding(false));
                if (File.Exists(Path)) File.Replace(temp, Path, Path + ".bak");
                else File.Move(temp, Path);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { message = "玩家档案写入失败：" + error.Message; return false; }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// Old installs: the profile an existing save (or its backups and timestamped copies) proves. The first file
        /// with a finished opening and a valid setup wins. Does not write anything.
        /// </summary>
        public PlayerProfile DeriveFromSaves()
        {
            foreach (string file in saves.SaveFiles())
            {
                if (!AtomicSaveStore.TryReadFile(file, out string payload)) continue;
                try
                {
                    if (!payload.Contains("\"version\"") || !payload.Contains("\"prologue\"")) continue;
                    var state = JsonUtility.FromJson<GameState>(payload);
                    if (state == null || state.version != 1) continue;
                    var profile = PlayerProfile.FromState(state);
                    if (profile != null) return profile;
                }
                catch (ArgumentException) { }
            }
            return null;
        }
    }
}
