using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Play QA for the opening and 第一阶段全程引导: a memory-only new game (useDiskSave off first), 摆渡众包's starting desks,
    /// the combo hits, then the opening learned and the stage-one lessons (inner voice + 老周's message). Driven from MCP.
    /// </summary>
    public static class StageOneTutorPlayQa
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        const string ActiveKey = "StageOneTutorPlayQa.Active";
        static string Root => Path.GetFullPath(Path.Combine("LocalVerification", "StageOneTutor"));

        /// <summary>The next Play runs isolated: its own save folder and desktop profile, and no local model (it can crash the editor on load).</summary>
        public static string Arm()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            Directory.CreateDirectory(Path.Combine(Root, "Save")); Directory.CreateDirectory(Path.Combine(Root, "DesktopProfile"));
            SessionState.SetBool(ActiveKey, true);
            return Root;
        }

        /// <summary>Ends the isolation. Leaving Play keeps the domain, so the redirected save folder and desktop profile would
        /// stay set for the editor's own tests: a script reload resets them.</summary>
        public static void Disarm()
        {
            SessionState.EraseBool(ActiveKey);
            if (!EditorApplication.isPlaying) EditorUtility.RequestScriptReload();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Isolate()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            ChapterOneRuntime.VerificationSaveDirectory = Path.Combine(Root, "Save");
            var profile = typeof(Michsky.DreamOS.DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic);
            profile?.SetValue(null, Path.Combine(Root, "DesktopProfile") + Path.DirectorySeparatorChar);
            SceneManager.sceneLoaded -= NoModel;
            SceneManager.sceneLoaded += NoModel;
        }

        static void NoModel(Scene scene, LoadSceneMode mode)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var runtime in root.GetComponentsInChildren<ChapterOneRuntime>(true))
                {
                    var llm = runtime.GetComponent<LocalLlm>() ?? runtime.gameObject.AddComponent<LocalLlm>();
                    llm.enabled = false;
                }
        }

        static bool Isolated(ChapterOneRuntime rt) => rt != null && (!rt.useDiskSave || SessionState.GetBool(ActiveKey, false) && Path.GetFullPath(rt.SavePath).StartsWith(Root));

        static XingGuangController Ctl()
        {
            var rt = Object.FindAnyObjectByType<ChapterOneRuntime>();
            return rt != null ? rt.GetComponent<XingGuangController>() : null;
        }

        public static string Setup()
        {
            var rt = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (!EditorApplication.isPlaying || !Isolated(rt)) return "refused: needs an isolated Play (Arm) or useDiskSave off";
            var hub = LingGuangV05.Desktop.YY.YYChatHub.Instance;
            if (hub != null && hub.S != null) hub.S.lingguangUnlocked = true; // skip the YY join cutscene
            var lab = new XgSim();
            lab.S.gradualSystems = true;
            lab.BeginOpening();
            var house = new LingGuangV05.Core.ChapterOneSim();
            house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 1200;
            house.S.gpuModels.Add("gtx980ti"); house.S.gpuCount = house.S.gpuModels.Count;
            house.S.labState = JsonUtility.ToJson(lab.S);
            rt.GetType().GetMethod("Attach", Flags).Invoke(rt, new object[] { house });
            var ctl = rt.GetComponent<XingGuangController>();
            ctl.GetType().GetMethod("BindSave", Flags).Invoke(ctl, new object[] { house });
            ctl.Open();
            ZhongbaoView.Instance?.Open("label");
            return Status();
        }

        /// <summary>n right answers in a row on 摆渡's current desk (the combo).</summary>
        public static string Hits(int n)
        {
            var ctl = Ctl(); var sim = ctl.Sim;
            for (int i = 0; i < n; i++) { var c = sim.Card(sim.S.desk); sim.Answer(sim.S.desk, c.truth, ctl.Host); }
            return "combo " + sim.S.combo + " timer " + sim.S.comboTimer.ToString("0.0") + " desk " + sim.S.desk;
        }

        /// <summary>Labels and trains on 常识判断 until the yes/no is learned (the opening ends).</summary>
        public static string Learn()
        {
            var ctl = Ctl(); var sim = ctl.Sim;
            for (int round = 0; round < 60 && sim.OpeningFocus; round++)
            {
                for (int i = 0; i < 10; i++) { var c = sim.Card(XgMemes.SenseDesk); sim.Answer(XgMemes.SenseDesk, c.truth, ctl.Host); }
                sim.TrainEpoch(XgTrack.Sequence, ctl.Host);
            }
            sim.AddLine("me", "冰是烫的吗？");
            return Status();
        }

        public static string Status()
        {
            var ctl = Ctl(); var sim = ctl != null ? ctl.Sim : null;
            if (sim == null) return "no lab";
            var b = new StringBuilder();
            b.Append("stage " + sim.S.stage + " opening " + sim.OpeningFocus + " desks:");
            foreach (var d in sim.OpenDesks()) b.Append(" " + d.id + "(" + XgMiniGames.ForDesk(d.id) + ")");
            b.Append("\ncombo " + sim.S.combo + " yes/no " + sim.YesNoAccuracy.ToString("0.00"));
            var top = GuideNote.TopStep;
            b.Append("\nnote: " + (top == null ? "-" : top.id + " " + top.target + " | " + top.zh + " | why: " + top.whyZh.Replace("\n", " / ")));
            b.Append("\ntold: " + string.Join(",", sim.S.tutorSeen) + " innerBusy " + InnerVoice.Busy);
            var tieba = TiebaHub.Instance;
            if (tieba != null && tieba.S != null)
            {
                var conv = tieba.S.Conversation(ForumLibrary.LaoZhou);
                if (conv.messages.Count > 0) b.Append("\n老周 last: " + conv.messages[conv.messages.Count - 1].text + " (unread " + conv.unread + ")");
            }
            return b.ToString();
        }

        /// <summary>The physical LCD as the player sees it, without bringing any window forward.</summary>
        public static string Shot(string file)
        {
            var cam = GameObject.Find("LCD Source Camera").GetComponent<Camera>();
            var target = cam.targetTexture;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return file;
        }
    }
}
