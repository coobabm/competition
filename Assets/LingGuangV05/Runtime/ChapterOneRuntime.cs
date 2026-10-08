using System;
using System.Globalization;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    /// <summary>The sole clock and persistence owner. Never parent this to an app window.</summary>
    [DefaultExecutionOrder(-1500)]
    public sealed class ChapterOneRuntime : MonoBehaviour
    {
        public bool useDiskSave = true;
        public bool tickEnabled = true;
        public ChapterOneSim Sim { get; private set; }
        public event Action Changed;
        /// <summary>Named events for the story layer: simulation signals plus runtime ones ("name.set").</summary>
        public event Action<string, string> Signal;
        /// <summary>Raised right before the save is serialized: apps write their state into GameState here.</summary>
        public event Action Saving;
        public string SaveStatus { get; private set; } = "尚未初始化";
        public string SavePath => store == null ? "（测试内存存档）" : store.SavePath;
        public double OfflineSeconds { get; private set; }
        /// <summary>True after SetSimulationForTests: QA drives the desktop, so story overlays stay off.</summary>
        public bool TestMode { get; private set; }

        AtomicSaveStore store;
        PlayerProfileStore profiles;
        // NonSerialized: after a script hot reload in Play mode, Sim is gone, so initialization must run again (reloads the save).
        [NonSerialized] bool initialized, dirty, notify, persistenceBlocked;
        float sinceSave, sinceNotify;
        internal Func<long> ClockOverride;
        [NonSerialized] internal string SaveDirectoryOverride;
        long UtcNow => ClockOverride != null ? ClockOverride() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Awake() { EnsureInitialized(); }

        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            if (useDiskSave)
            {
                store = new AtomicSaveStore(string.IsNullOrEmpty(SaveDirectoryOverride) ? Path.Combine(Application.persistentDataPath, "LingGuangV05") : SaveDirectoryOverride);
                profiles = new PlayerProfileStore(store);
                if (TryRead(out var restored, out string message))
                {
                    // The last game ended with the computer sold and 重新开始 was chosen: a new game (the old save is kept as it is).
                    // The opening was finished before, so it starts right after it.
                    if (restored.S.restartChosen)
                    {
                        BeginNewGame(false, KnownProfile(restored, true));
                        SaveStatus = GameText.T("上一局已经结束，开始新的一局；旧存档留作备份。", "The last game ended; a new one has begun. The old save is kept as a backup.");
                        return;
                    }
                    Attach(restored);
                    // An older install: this save proves the opening was finished, so remember it outside the save.
                    KnownProfile(restored, false);
                    ApplyOffline();
                    SaveStatus = message + OfflineSummary();
                    // Persist the consumed offline interval immediately. Reopening the
                    // app or reloading the same save must not collect it repeatedly.
                    string offline = SaveStatus;
                    if (SaveNow()) SaveStatus = offline;
                    return;
                }
                // No (readable) save: a new game. A player who finished the opening before does not see it again.
                BeginNewGame(false, KnownProfile(null, true));
                SaveStatus = message;
                return;
            }
            else SaveStatus = "测试模式：不写入本机存档";
            Attach(new ChapterOneSim());
        }

        bool TryRead(out ChapterOneSim result, out string message)
        {
            result = null;
            ChapterOneSim parsed = null;
            var read = store.Load(payload =>
            {
                if (!payload.Contains("\"version\"") || !payload.Contains("\"nodes\"") || !payload.Contains("\"rngState\"")) return false;
                var state = JsonUtility.FromJson<GameState>(payload);
                if (state == null) return false;
                if (state.version != 1) throw new NotSupportedException("存档版本不受支持，未回退或覆盖。");
                if (state.nodes == null || state.nodes.Count < 3 || state.edges == null) return false;
                parsed = new ChapterOneSim(state);
                return true;
            });
            message = read.Message;
            persistenceBlocked = read.WriteBlocked;
            if (!read.Success) return false;
            result = parsed;
            return result != null;
        }

        void Attach(ChapterOneSim simulation)
        {
            if (Sim != null) { Sim.Changed -= OnSimulationChanged; Sim.Signal -= OnSimulationSignal; }
            Sim = simulation;
            Sim.Changed += OnSimulationChanged;
            Sim.Signal += OnSimulationSignal;
            sinceSave = 0;
            dirty = true;
            notify = true;
            Changed?.Invoke();
        }

        void ApplyOffline()
        {
            long now = UtcNow;
            long last = Sim.S.lastSeenUnix;
            // No offline progress: time away is not simulated (no income, power bills or training while closed).
            OfflineSeconds = 0;
            Sim.S.lastSeenUnix = Math.Max(last, now);
        }

        string OfflineSummary() => OfflineSeconds > 0 ? " 离线结算 " + Math.Round(OfflineSeconds) + " 秒（上限 30 分钟，含电费）。" : "";

        void OnSimulationChanged() { dirty = true; notify = true; }
        void OnSimulationSignal(string name, string arg) { Signal?.Invoke(name, arg); }

        /// <summary>Story or other non-simulation state changed; save on the next interval.</summary>
        public void MarkDirty() { dirty = true; }

        /// <summary>Raise a runtime-level signal (desktop events such as an app opening).</summary>
        public void RaiseSignal(string name, string arg = null) { Signal?.Invoke(name, arg); }

        void Update()
        {
            EnsureInitialized();
            if (Sim == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 2f);
            // In the prologue only the clock runs: no bills, heat or income before the game proper starts.
            if (tickEnabled) { if (Sim.InPrologue) Sim.AdvanceClock(dt); else Sim.Tick(dt); }
            sinceSave += dt;
            sinceNotify += dt;
            if (notify && sinceNotify >= .12f) { notify = false; sinceNotify = 0; Changed?.Invoke(); }
            if (useDiskSave && dirty && sinceSave >= 15f) SaveNow();
        }

        public bool SaveNow()
        {
            EnsureInitialized();
            sinceSave = 0;
            if (!useDiskSave) { SaveStatus = "测试模式：不写入本机存档"; return true; }
            if (persistenceBlocked) { SaveStatus = "存档受保护：请备份后使用“重置进度”，不会自动覆盖。"; notify = true; return false; }
            // Design v1.1 §6: the prologue is never saved; the save starts with the opening setup (Step 8).
            if (Sim.InPrologue) { SaveStatus = "序章不存档"; dirty = false; return true; }
            Sim.S.lastSeenUnix = Math.Max(Sim.S.lastSeenUnix, UtcNow);
            try { Saving?.Invoke(); }
            catch (Exception error) { Debug.LogWarning("保存前写入应用状态失败：" + error.Message); }
            bool saved;
            try { saved = store.TrySave(JsonUtility.ToJson(Sim.S), out var status); SaveStatus = status; }
            catch (ArgumentException error) { saved = false; SaveStatus = "无法序列化存档：" + error.Message; }
            if (saved) dirty = false;
            notify = true;
            return saved;
        }

        public bool ReloadSave()
        {
            EnsureInitialized();
            if (!useDiskSave) { SaveStatus = "测试模式未启用磁盘读档"; Changed?.Invoke(); return false; }
            if (!TryRead(out var restored, out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
            if (restored.S.restartChosen)
            {
                BeginNewGame(false, KnownProfile(restored, true));
                SaveStatus = GameText.T("上一局已经结束，开始新的一局；旧存档留作备份。", "The last game ended; a new one has begun. The old save is kept as a backup.");
                Changed?.Invoke();
                return true;
            }
            Attach(restored);
            KnownProfile(restored, false);
            ApplyOffline();
            string status = message + OfflineSummary();
            if (SaveNow()) SaveStatus = status;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 重新开始 on the failure ending's game-over card (the computer was sold): the save and its backup are copied
        /// with a timestamp in the same folder, the save is marked so the next start is a new game, and a new game
        /// starts now. Nothing is deleted. Only for a bankrupt save, and only when the player chose it.
        /// </summary>
        public bool StartOverAfterBankruptcy(bool replayOpening = false)
        {
            EnsureInitialized();
            if (Sim == null || !Sim.S.bankrupt) return false;
            // What the player chose in the opening, learned from this very save if it was never written down (old installs).
            var known = KnownProfile(Sim, true);
            if (useDiskSave && store != null)
            {
                // The ended game as it is first, then the mark on the primary.
                if (!store.CopyForRestart(out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
                Sim.S.restartChosen = true;
                dirty = true;
                SaveNow();
            }
            persistenceBlocked = false;
            OfflineSeconds = 0;
            BeginNewGame(replayOpening, known);
            SaveStatus = GameText.T("重新开始：旧存档留作备份。", "Started again: the old save is kept as a backup.");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Caller must obtain an explicit reset confirmation first.</summary>
        public bool ResetProgress(bool replayOpening = false)
        {
            EnsureInitialized();
            // Learn the player's setup before the files are moved aside.
            var known = KnownProfile(Sim, true);
            if (useDiskSave && !store.ArchiveForReset(out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
            persistenceBlocked = false;
            OfflineSeconds = 0;
            BeginNewGame(replayOpening, known);
            return !dirty || SaveNow();
        }

        // ───────────── the opening, once ─────────────

        /// <summary>
        /// What the player chose in the opening, if they have finished it before (player-profile.json next to the save).
        /// An older install without the file is read from the running or an existing save. Null for a first-ever player.
        /// </summary>
        public PlayerProfile OpeningProfile() { EnsureInitialized(); return KnownProfile(Sim, true); }

        PlayerProfile KnownProfile(ChapterOneSim from, bool scanSaves)
        {
            if (profiles == null) return null;
            var known = profiles.Read();
            if (known != null) return known;
            if (from != null) known = PlayerProfile.FromState(from.S);
            if (known == null && scanSaves) known = profiles.DeriveFromSaves();
            if (known != null) profiles.Write(known, out _);
            return known;
        }

        /// <summary>
        /// A new game. With a known profile (and no wish to see the opening again) the opening story and the
        /// install are skipped: the game ends the prologue through the usual path with that profile, so it starts
        /// where a first-timer is right after the setup. Otherwise it starts in the prologue.
        /// </summary>
        void BeginNewGame(bool replayOpening, PlayerProfile known)
        {
            Attach(new ChapterOneSim());
            if (replayOpening || !PlayerProfile.IsUsable(known)) return;
            FinishPrologue(known.CopyOfSetup(), false);
        }

        public bool RecordName(string name)
        {
            EnsureInitialized();
            name = (name ?? "").Trim();
            if (name.Length == 0 || new StringInfo(name).LengthInTextElements > 16)
            { SaveStatus = "名字请输入 1–16 个字。"; Changed?.Invoke(); return false; }
            foreach (char character in name)
                if (char.IsControl(character) || character == '<' || character == '>' || character == '[' || character == ']')
                { SaveStatus = "名字不能包含控制符或格式标签。"; Changed?.Invoke(); return false; }
            Sim.S.aiName = name;
            dirty = true;
            Signal?.Invoke("name.set", name);
            SaveStatus = "已记下名字；它会在后续章节开口时使用。";
            Changed?.Invoke();
            return true;
        }

        /// <summary>Prologue Step 8: stores the opening setup, ends the prologue and writes the first save.</summary>
        public bool CompletePrologue(PrologueProfile profile)
        {
            EnsureInitialized();
            return FinishPrologue(profile, true);
        }

        /// <summary>Ends the prologue with this setup. <paramref name="remember"/>: also write it to player-profile.json (the opening was just played).</summary>
        bool FinishPrologue(PrologueProfile profile, bool remember)
        {
            if (!Sim.CompletePrologue(profile)) { SaveStatus = PrologueProfile.Problem(profile, GameText.IsEnglish) ?? "序章已结束。"; Changed?.Invoke(); return false; }
            if (remember && profiles != null)
            {
                var played = PlayerProfile.FromSetup(profile);
                if (played != null) profiles.Write(played, out _);
            }
            Signal?.Invoke("name.set", Sim.S.aiName);
            dirty = true;
            bool saved = SaveNow();
            Changed?.Invoke();
            return saved;
        }

        public void SetSimulationForTests(ChapterOneSim simulation)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            useDiskSave = false;
            TestMode = true;
            initialized = true;
            persistenceBlocked = false;
            Attach(simulation);
            SaveStatus = "测试模式：不写入本机存档";
        }

        void OnApplicationPause(bool paused) { if (paused && initialized && useDiskSave) SaveNow(); }
        void OnApplicationQuit() { if (initialized && useDiskSave) SaveNow(); }
        void OnDestroy() { if (Sim != null) { Sim.Changed -= OnSimulationChanged; Sim.Signal -= OnSimulationSignal; } }
    }
}
