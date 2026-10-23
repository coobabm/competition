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
    public sealed partial class ChapterOneRuntime : MonoBehaviour
    {
        public bool useDiskSave = true;
        public bool tickEnabled = true;
        // Desktop-owned presentation hold. No desktop assembly reference and no accumulated catch-up time.
        public Func<bool> EpiloguePresentationHeld;
        public ChapterOneSim Sim { get; private set; }
        public event Action Changed;
        /// <summary>Named events for the story layer: simulation signals plus runtime ones ("name.set").</summary>
        public event Action<string, string> Signal;
        /// <summary>Raised right before the save is serialized: apps write their state into GameState here.</summary>
        public event Action Saving;
        string saveStatus = "尚未初始化";
        public string SaveStatus { get => SnapshotStatusText.Localize(saveStatus, GameText.IsEnglish); private set => saveStatus = value; }
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
#if UNITY_EDITOR
        // Editor verification keeps additive Main reloads on the same isolated save root.
        public static string VerificationSaveDirectory;
#endif
        [NonSerialized] internal string SaveDirectoryOverride;
        long UtcNow => ClockOverride != null ? ClockOverride() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Awake() { EnsureInitialized(); }

        public void EnsureInitialized()
        {
            if (initialized) return;
            // Invalid explicit verification arguments throw before any player store/profile is opened.
            string verificationDirectory = VerificationSavePaths.Root;
            initialized = true;
            offlineSessionStartedUnix = UtcNow;
            if (useDiskSave)
            {
#if RECORDING_PREVIEW
                // Dedicated recording build: keep every save and slot beside this copy of the game.
                SaveDirectoryOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserData"));
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--recording-smoke") >= 0)
                    SaveDirectoryOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "VerificationData"));
#endif
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(VerificationSaveDirectory)) SaveDirectoryOverride = VerificationSaveDirectory;
#endif
                if (verificationDirectory != null) SaveDirectoryOverride = verificationDirectory;
                store = new AtomicSaveStore(string.IsNullOrEmpty(SaveDirectoryOverride) ? Path.Combine(Application.persistentDataPath, "LingGuangV05") : SaveDirectoryOverride);
                profiles = new PlayerProfileStore(store);
                if (TryRead(out var restored, out string message))
                {
                    // The last game ended with the computer sold and 重新开始 was chosen: a new game (the old save is kept as it is).
                    // The opening was finished before, so it starts right after it.
                    if (restored.S.restartChosen)
                    {
                        bool begun = BeginNewGame(false, KnownProfile(restored, true));
                        SaveStatus = begun
                            ? GameText.T("上一局已经结束，开始新的一局；旧存档留作备份。", "The last game ended; a new one has begun. The old save is kept as a backup.")
                            : UnsavedNewGameStatus();
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
                bool started = BeginNewGame(false, KnownProfile(null, true));
                SaveStatus = started ? message : message + " " + saveStatus;
                return;
            }
            else SaveStatus = "测试模式：不写入本机存档";
            var fresh = new ChapterOneSim();
            fresh.S.narrationRunId = Guid.NewGuid().ToString("N");
            Attach(fresh);
        }

        bool TryRead(out ChapterOneSim result, out string message)
        {
            result = null;
            ChapterOneSim parsed = null;
            var read = store.Load(payload =>
            {
                if (!payload.Contains("\"version\"") || !payload.Contains("\"nodes\"") || !payload.Contains("\"rngState\"")) return false;
                var state = ReadNarrationState(payload);
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
            NarrationReceiptStore.EnsureIdentity(Sim.S);
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
            ApplyOfflineIncome();
            Sim.S.lastSeenUnix = Math.Max(last, now);
        }

        string OfflineSummary() => !string.IsNullOrEmpty(OfflineIncomeStatus) ? " " + OfflineIncomeStatus : OfflineIncome > 0
            ? GameText.T(" 离线收益 ¥", " Offline income ¥") + OfflineIncome.ToString("0.00")
                + GameText.T("（" + Pct + "效率，最多8小时，仅资金）。", " (" + Pct + " efficiency, up to 8 hours, funds only).") : "";
        static string Pct => (XgOfflineRules.Efficiency * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

        void OnSimulationChanged() { dirty = true; notify = true; }
        void OnSimulationSignal(string name, string arg) { Signal?.Invoke(name, arg); }

        /// <summary>Story or other non-simulation state changed; save on the next interval.</summary>
        public void MarkDirty() { Sim?.RecordFunds("其他收支"); dirty = true; }

        /// <summary>Raise a runtime-level signal (desktop events such as an app opening).</summary>
        public void RaiseSignal(string name, string arg = null) { Signal?.Invoke(name, arg); }

        void Update()
        {
            EnsureInitialized();
            if (Sim == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 2f);
            // In the prologue only the clock runs: no bills, heat or income before the game proper starts.
            if (tickEnabled && !(EpiloguePresentationHeld?.Invoke() ?? false)) { if (Sim.InPrologue) Sim.AdvanceClock(dt); else Sim.Tick(dt); }
            sinceSave += dt;
            sinceNotify += dt;
            if (notify && sinceNotify >= .12f) { notify = false; sinceNotify = 0; Changed?.Invoke(); }
            TryAutosave();
        }

        /// <summary>Seconds between autosaves while something changed (the lab marks itself dirty every second).</summary>
        public const float AutosaveSeconds = 300f;
        public const double AutosaveIdleSeconds = 2;
        static double lastDesktopInput = double.NegativeInfinity;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDesktopActivity() { lastDesktopInput = double.NegativeInfinity; }
        // Desktop reports input; persistence has no InputSystem dependency.
        public static void NotifyDesktopInput() { lastDesktopInput = Time.realtimeSinceStartupAsDouble; }
        public static bool DesktopIdleForAutosave => Time.realtimeSinceStartupAsDouble - lastDesktopInput >= AutosaveIdleSeconds;
        public bool TryAutosave()
        {
            if (!useDiskSave || !dirty || sinceSave < AutosaveSeconds || !DesktopIdleForAutosave) return false;
            return SaveNow();
        }

        /// <summary>
        /// Checks an archive's lab text before it may replace the main save (XingGuangController sets it: the runtime
        /// cannot read XgState itself). Null means not checked.
        /// </summary>
        public static Func<string, bool> LabStateReadable;

        /// <summary>Writes a text copy beside the save (name-yyyyMMddTHHmmss.txt) and returns its path, or null.</summary>
        public string BackupText(string name, string text)
        {
            if (!useDiskSave || store == null || string.IsNullOrEmpty(text)) return null;
            try
            {
                string path = Path.Combine(store.Folder, name + "-" + DateTime.Now.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture) + ".txt");
                File.WriteAllText(path, text);
                return path;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { Debug.LogWarning("[存档] 副本没写成：" + error.Message); return null; }
        }

        public bool SaveNow()
        {
            EnsureInitialized();
            if (endingArchiveBusy) return false; // Archive collection/switch callbacks cannot write either primary.
            sinceSave = 0;
            if (!useDiskSave) { SaveStatus = "测试模式：不写入本机存档"; return true; }
            if (persistenceBlocked) { SaveStatus = "存档受保护：请备份后使用“重置进度”，不会自动覆盖。"; Debug.LogWarning("[存档] " + SaveStatus); notify = true; return false; }
            // Design v1.1 §6: the prologue is never saved; the save starts with the opening setup (Step 8).
            if (Sim.InPrologue) { SaveStatus = "序章不存档"; dirty = false; return true; }
            Sim.S.lastSeenUnix = Math.Max(Sim.S.lastSeenUnix, UtcNow);
            try { Saving?.Invoke(); CaptureOfflineIncome(); }
            catch (Exception error)
            {
                // App snapshots are part of the save transaction. Never persist a partial snapshot.
                SaveStatus = GameText.T("保存失败：应用状态未写入，原存档保持不变。", "Save failed: app state could not be collected. The previous save is unchanged.")
                    + " " + error.Message;
                dirty = true;
                notify = true;
                Debug.LogWarning("[Save] " + SaveStatus);
                return false;
            }
            bool saved;
            try { saved = store.TrySaveSnapshot(JsonUtility.ToJson(Sim.S), out var status); SaveStatus = status; }
            catch (ArgumentException error) { saved = false; SaveStatus = "无法序列化存档：" + error.Message; }
            if (saved) { dirty = false; SaveArchiveThumbnail(1); }
            // One line per write, so a save that silently failed (or never happened) shows up in the Editor log.
            if (saved) Debug.Log("[存档] 已保存 · day " + Sim.S.day + " · " + Sim.S.gameSeconds.ToString("0") + " s · lab " + (Sim.S.labState == null ? 0 : Sim.S.labState.Length) + " chars");
            else Debug.LogWarning("[存档] 没写进去：" + SaveStatus);
            notify = true;
            return saved;
        }

        public bool ReloadSave()
        {
            EnsureInitialized();
            if (endingArchiveBusy) return false;
            if (!useDiskSave) { SaveStatus = "测试模式未启用磁盘读档"; Changed?.Invoke(); return false; }
            if (!TryRead(out var restored, out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
            if (restored.S.restartChosen)
            {
                bool begun = BeginNewGame(false, KnownProfile(restored, true));
                SaveStatus = begun
                    ? GameText.T("上一局已经结束，开始新的一局；旧存档留作备份。", "The last game ended; a new one has begun. The old save is kept as a backup.")
                    : UnsavedNewGameStatus();
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
            if (endingArchiveBusy) return false;
            if (Sim == null || !Sim.S.bankrupt) return false;
            // What the player chose in the opening, learned from this very save if it was never written down (old installs).
            var known = KnownProfile(Sim, true);
            if (useDiskSave && store != null)
            {
                // The ended game as it is first, then the mark on the primary.
                if (!store.CopyForRestart(out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
                Sim.S.restartChosen = true;
                dirty = true;
                // A failed choice write must keep the ended game active so the player can retry.
                if (!SaveNow())
                {
                    Sim.S.restartChosen = false;
                    SaveStatus = GameText.T("没有重新开始：", "Did not start again: ") + saveStatus;
                    Changed?.Invoke();
                    return false;
                }
            }
            persistenceBlocked = false;
            OfflineSeconds = 0;
            // The saved choice commits the restart; a failed first save stays dirty for the next retry.
            bool begun = BeginNewGame(replayOpening, known);
            SaveStatus = begun
                ? GameText.T("重新开始：旧存档留作备份。", "Started again: the old save is kept as a backup.")
                : UnsavedNewGameStatus();
            Changed?.Invoke();
            return true;
        }

        string UnsavedNewGameStatus() =>
            GameText.T("新的一局已开始，旧存档留作备份；但新进度还没有存进磁盘，会在下次保存时重试：",
                "A new game has begun and the old save is kept as a backup, but the new progress is not on disk yet; the next save retries: ") + saveStatus;

        /// <summary>Caller must obtain an explicit reset confirmation first.</summary>
        public bool ResetProgress(bool replayOpening = false)
        {
            EnsureInitialized();
            if (endingArchiveBusy) { SaveStatus = GameText.T("正在保存结局，请稍后再开始新游戏。", "Saving the ending. Please start the new game again shortly."); Changed?.Invoke(); return false; }
            // Learn the player's setup before the files are moved aside.
            var known = KnownProfile(Sim, true);
            if (useDiskSave && !store.ArchiveForReset(out var message)) { SaveStatus = message; Changed?.Invoke(); return false; }
            persistenceBlocked = false;
            OfflineSeconds = 0;
            BeginNewGame(replayOpening, known);
            newGameInOpening = Sim;
            bool saved = !dirty || SaveNow();
            // The prologue is never saved, so say plainly that the new game is running and when it will be saved.
            SaveStatus = !saved ? UnsavedNewGameStatus()
                : Sim.InPrologue ? GameText.T("新游戏已开始 · 开场结束后自动存档", "New game started · It autosaves once the opening ends")
                : GameText.T("新游戏已开始 · 已自动存档", "New game started · Autosaved");
            Changed?.Invoke();
            return saved;
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
            if (known != null && useDiskSave) profiles.Write(known, out _);
            return known;
        }

        /// <summary>
        /// A new game. With a known profile (and no wish to see the opening again) the opening story and the
        /// install are skipped: the game ends the prologue through the usual path with that profile, so it starts
        /// where a first-timer is right after the setup. Otherwise it starts in the prologue.
        /// False when the new game left the prologue but its first save failed (it stays dirty).
        /// </summary>
        bool BeginNewGame(bool replayOpening, PlayerProfile known)
        {
            var fresh = new ChapterOneSim();
            fresh.S.narrationRunId = Guid.NewGuid().ToString("N");
            Attach(fresh);
            if (replayOpening || !PlayerProfile.IsUsable(known)) return true;
            return FinishPrologue(known.CopyOfSetup(), false) || Sim.InPrologue;
        }

        public bool RecordName(string name)
        {
            EnsureInitialized();
            string problem = AiNamePolicy.Problem(name, GameText.IsEnglish);
            if (problem != null) { SaveStatus = problem; Changed?.Invoke(); return false; }
            name = AiNamePolicy.Normalize(name);
            Sim.S.aiName = name;
            dirty = true;
            Signal?.Invoke("name.set", name);
            bool saved = SaveNow();
            if (saved && useDiskSave && profiles != null)
            {
                var profile = PlayerProfile.FromState(Sim.S);
                if (profile != null) profiles.Write(profile, out _);
            }
            if (saved) SaveStatus = GameText.T("名字已更新，桌面程序现在叫 ", "Name updated. The desktop app is now ") + name + ".exe";
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
            if (!Sim.CompletePrologue(profile, legacyProfile: !remember)) { SaveStatus = PrologueProfile.Problem(profile, GameText.IsEnglish) ?? "序章已结束。"; Changed?.Invoke(); return false; }
            if (remember && useDiskSave && profiles != null)
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
#if UNITY_EDITOR
        // Stopping Play in the editor saves before the scene is torn down, whatever order the quit callbacks run in.
        void OnEnable() { UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged; }
        void OnDisable() { UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged; }
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        { if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode && initialized && useDiskSave && !TestMode) SaveNow(); }
#endif
        void OnDestroy() { if (Sim != null) { Sim.Changed -= OnSimulationChanged; Sim.Signal -= OnSimulationSignal; } }
    }
}
