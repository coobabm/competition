using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_STANDALONE_WIN || UNITY_WSA
using UnityEngine.Windows.Speech;
#endif

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Voice assistant "Cori": listens for a wake keyword, opens the Cori popup, then matches spoken commands to
    /// UnityEvents. Also offers plain dictation. Replaces DreamOS's SpeechRecognition (2026-10-19): same GUID,
    /// serialized fields and public API.
    ///
    /// The recognizers only exist on Windows (UnityEngine.Windows.Speech). Everywhere else the component switches
    /// itself off in Awake via <see cref="EnableSpeechRecognition"/>. The serialized fields are declared on every
    /// platform on purpose: building the Windows player from a macOS editor fails with "script class layout is
    /// incompatible" when the editor and player layouts differ (kept from the 2026-10-03 local change).
    /// </summary>
    public class SpeechRecognition : MonoBehaviour
    {
#pragma warning disable 0414
        // Recognition
        public string[] keywords = new string[] { "Hey Cori" };
        public List<CommandItem> commands = new List<CommandItem>();
        public List<string> listeningMessages = new List<string>();
        public UnityEvent onKeywordCall;
        public UnityEvent onDismiss;

        // Resources
        [SerializeField] private PopupPanelManager coriPopup;
        [SerializeField] private TextMeshProUGUI listeningText;
        [SerializeField] private GameObject taskbarShortcut;

        // Settings
        public bool enableKeywords = true;
        [SerializeField] private bool enableLogs = false;
        [Range(1, 30)] public float dismissAfter = 4;
        public AudioClip listeningEffect;
        public AudioClip dismissEffect;

        // Debug
        [TextArea] public string hypotheses;
        [TextArea] public string recognitions;
#pragma warning restore 0414

        [System.Serializable]
        public class CommandItem
        {
            [TextArea] public string command;
            public UnityEvent onCalled = new UnityEvent();
        }

#if UNITY_STANDALONE_WIN || UNITY_WSA
        const string LogPrefix = "<b>[Speech Recognition]</b> ";

        readonly List<string> commandsHelper = new List<string>();

        DictationRecognizer dictationRecognizer;
        KeywordRecognizer keywordRecognizer;
        KeywordRecognizer commandRecognizer;

        bool stopSTT = false;

        void Awake()
        {
            try
            {
                PhraseRecognitionSystem.OnStatusChanged += SpeechSystemStatusFn;
                InitializeKeywords();
            }

            catch
            {
                Debug.LogWarning("<b>[Speech Recognition]</b> Cannot initialize STT. Make sure that there is " +
                    "at least a single voice package installed on your Windows OS.");
                enabled = false;
                stopSTT = true;
            }
        }

        void OnDestroy()
        {
            if (stopSTT == true)
                return;

            if (keywordRecognizer != null && keywordRecognizer.IsRunning) { StopKeywordRecognizer(); }
            if (commandRecognizer != null && commandRecognizer.IsRunning) { StopCommandRecognizer(); }
            if (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped) { PhraseRecognitionSystem.Shutdown(); }
        }

        void InitializeKeywords()
        {
            for (int i = 0; i < commands.Count; i++) { commandsHelper.Add(commands[i].command); }
            if (enableKeywords == true) { StartKeywordRecognizer(); }
        }

        void SpeechSystemStatusFn(SpeechSystemStatus status)
        {
            Log("Speech System Status: " + status);
        }

        void Log(string message)
        {
            if (enableLogs == true) { Debug.Log(LogPrefix + message); }
        }

        // "text (confidence)" line shown in the debug box.
        static string DescribeResult(PhraseRecognizedEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, System.Environment.NewLine);
            return builder.ToString();
        }

        public void StartDictation()
        {
            if (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
            {
                PhraseRecognitionSystem.Shutdown();
            }

            dictationRecognizer = new DictationRecognizer();
            dictationRecognizer.DictationResult += (string text, ConfidenceLevel confidence) =>
            {
                recognitions += text + "\n";
            };

            dictationRecognizer.DictationHypothesis += ((string text) =>
            {
                hypotheses += text + "\n";
            });

            dictationRecognizer.DictationComplete += ((DictationCompletionCause completionCause) =>
            {
                if (enableLogs == true && completionCause != DictationCompletionCause.Complete)
                    Debug.LogErrorFormat(LogPrefix + "Dictation completed unsuccessfully: {0}.", completionCause);
            });

            dictationRecognizer.DictationError += ((string error, int hresult) =>
            {
                if (enableLogs == true)
                    Debug.LogErrorFormat(LogPrefix + "Dictation error: {0}; HResult = {1}.", error, hresult);
            });

            dictationRecognizer.Start();
            recognitions = "";
            hypotheses = "";
        }

        public void StopDictation()
        {
            dictationRecognizer.Dispose();
            dictationRecognizer.Stop();
            dictationRecognizer = null;

            Log("Dictation stopped.");
        }

        public void OpenCoriPopup()
        {
            coriPopup.OpenPanel();
            StopKeywordRecognizer();
            StartCommandRecognizer();
        }

        public void CloseCoriPopup()
        {
            coriPopup.ClosePanel();
            onDismiss.Invoke();
            StopCommandRecognizer();

            if (enableKeywords == true)
            {
                StartKeywordRecognizer();
            }
        }

        public void StartKeywordRecognizer()
        {
            keywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Low);
            keywordRecognizer.OnPhraseRecognized += ((PhraseRecognizedEventArgs args) =>
            {
                recognitions += DescribeResult(args);

                Log("Keyword recognized: <b>" + args.text + " (" + args.confidence + ")</b>");
                if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(listeningEffect); }
                if (coriPopup != null) { coriPopup.OpenPanel(); }

                onKeywordCall.Invoke();
                StopKeywordRecognizer();
                StartCommandRecognizer();
                StartCoroutine(nameof(WaitForCommand), dismissAfter);
            });

            recognitions = "";
            keywordRecognizer.Start();
            hypotheses = "Listening for: \n" + string.Join(", ", keywords);
        }

        public void StopKeywordRecognizer()
        {
            if (keywordRecognizer == null) { return; }
            if (keywordRecognizer.IsRunning) { keywordRecognizer.Stop(); }
            Log("Keyword recognizer stopped.");

            keywordRecognizer.Dispose();
            keywordRecognizer = null;
        }

        public void StartCommandRecognizer()
        {
            if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(listeningEffect); }
            if (listeningEffect != null) { listeningText.text = listeningMessages[UnityEngine.Random.Range(0, listeningMessages.Count)]; }

            commandRecognizer = new KeywordRecognizer(commandsHelper.ToArray(), ConfidenceLevel.Low);
            commandRecognizer.OnPhraseRecognized += ((PhraseRecognizedEventArgs args) =>
            {
                StopCoroutine(nameof(WaitForCommand));

                recognitions += DescribeResult(args);

                Log("Command recognized: <b>" + args.text + " (" + args.confidence + ")</b>");
                for (int i = 0; i < commands.Count; i++)
                {
                    if (args.text == commands[i].command)
                        commands[i].onCalled.Invoke();
                }

                StopCommandRecognizer();

                if (enableKeywords == true)
                {
                    StartKeywordRecognizer();
                }
            });

            recognitions = "";
            commandRecognizer.Start();
            hypotheses = "Listening for: \n" + string.Join(", ", commandsHelper);
        }

        public void StopCommandRecognizer()
        {
            if (coriPopup != null) { coriPopup.ClosePanel(); }
            if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(dismissEffect); }
            if (commandRecognizer == null) { return; }
            if (commandRecognizer.IsRunning) { commandRecognizer.Stop(); }
            Log("Command recognizer stopped.");

            commandRecognizer.Dispose();
            commandRecognizer = null;
        }

        IEnumerator WaitForCommand(float waitFor)
        {
            yield return new WaitForSeconds(waitFor);

            onDismiss.Invoke();

            if (commandRecognizer != null && commandRecognizer.IsRunning) { StopCommandRecognizer(); }
            if (enableKeywords == true) { StartKeywordRecognizer(); }
        }
#else
        void Awake()
        {
            EnableSpeechRecognition(false);
        }
#endif

        public void EnableSpeechRecognition(bool value)
        {
            gameObject.SetActive(value);
#if UNITY_STANDALONE_WIN || UNITY_WSA
            if (coriPopup != null) { coriPopup.gameObject.SetActive(value); }
            if (taskbarShortcut != null) { taskbarShortcut.SetActive(value); }
#endif
        }
    }
}
