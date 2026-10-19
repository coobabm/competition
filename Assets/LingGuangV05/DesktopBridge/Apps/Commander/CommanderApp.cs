using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop command prompt (2026-10-19). Replaces DreamOS's CommanderManager: same GUID, serialized fields and
    /// public methods. The window is reused for the second-hand shop.
    ///
    /// Typing a command and pressing Enter echoes it into the history (with a [hh:mm:ss] stamp from the desktop
    /// clock), then prints the command's feedback and fires its event; "help" lists the commands.
    /// </summary>
    public class CommanderApp : MonoBehaviour
    {
        [System.Serializable]
        public class CommandItem
        {
            public string commandName = "Command Name";
            public string command = "Actual Command";
            [TextArea(3, 10)] public string commandDescription = "Command description";
            [TextArea(3, 10)] public string feedbackText;
            public float feedbackDelay = 0;
            public float onProcessDelay = 0;
            public bool includeToHelp = true;
            public UnityEvent onProcessEvent = new UnityEvent();
        }

        public List<CommandItem> commands = new List<CommandItem>();

        [TextArea] public string errorText = "is not recognized as a command.";
        [TextArea] public string onEnableText = "Welcome to Commander.";
        public string helpCommand = "help";
        public Color textColor;
        public bool enableHelpCommand = true;
        [SerializeField] bool antiFlicker = true;

        [SerializeField] TMP_InputField commandInput;
        [SerializeField] TextMeshProUGUI commandHistory;
        [SerializeField] Scrollbar scrollbar;

        public bool useTypewriterEffect = false;
        [Range(0.001f, 0.5f)] public float typewriterDelay = 0.03f;

        [Tooltip("Stamp each line with the desktop clock's time.")]
        public bool getTimeData = true;
        public Color timeColor = new Color(0, 255, 0);

        int commandIndex;
        RectTransform historyRT;
        RectTransform historyParentRT;
        Coroutine typewriter, processDelay;

        bool Stamped => getTimeData && DateAndTimeManager.instance != null;

        void Awake()
        {
            historyRT = commandHistory.GetComponent<RectTransform>();
            historyParentRT = commandHistory.transform.parent.GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            commandHistory.text = "";
            commandInput.text = "";
            UpdateColors();
            if (Stamped) UpdateTime();
            commandHistory.text += onEnableText;
            commandInput.ActivateInputField();
            StartCoroutine(FixLayout());
        }

        void Update()
        {
            if (string.IsNullOrEmpty(commandInput.text) || EventSystem.current.currentSelectedGameObject != commandInput.gameObject) return;
            if (!commandInput.isFocused) commandInput.ActivateInputField();
            if (Keyboard.current.enterKey.wasPressedThisFrame) ProcessCommand();
        }

        public void UpdateColors()
        {
            commandInput.textComponent.color = textColor;
            commandHistory.color = textColor;
        }

        public void ProcessCommand()
        {
            string typed = commandInput.text;
            commandIndex = -1;
            StopTypewriter();

            NewLine();
            commandHistory.text += typed;

            if (enableHelpCommand && typed == helpCommand)
            {
                foreach (var c in commands)
                    if (c.includeToHelp) commandHistory.text += string.Format("\n[{0}] {1} ", c.command, c.commandDescription);
                FinishInput();
                return;
            }

            commandIndex = commands.FindIndex(c => c.command == typed);
            if (commandIndex == -1)
            {
                NewLine();
                commandHistory.text = string.Format("{0}'{1}' {2}", commandHistory.text, typed, errorText);
                FinishInput();
                return;
            }

            var command = commands[commandIndex];
            if (command.feedbackText != "") StartCoroutine(FeedbackAfter(command.feedbackDelay));
            if (command.onProcessDelay == 0) command.onProcessEvent.Invoke();
            else processDelay = StartCoroutine(ProcessAfter(command.onProcessDelay));
            FinishInput();
        }

        void NewLine()
        {
            commandHistory.text += "\n";
            if (Stamped) UpdateTime();
        }

        // Clears the input, then makes the history lay out and scroll to the newest line.
        void FinishInput()
        {
            commandInput.text = "";
            Relayout();
            if (scrollbar != null) scrollbar.value = 0;
        }

        void Relayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            StartCoroutine(FixLayout());
        }

        /// <summary>Appends "[hh:mm:ss] " in <see cref="timeColor"/> from the desktop clock.</summary>
        public void UpdateTime()
        {
            if (!Stamped) return;
            var clock = DateAndTimeManager.instance;
            commandHistory.text = string.Format("{0}<color=#{1}>[{2}:{3}:{4}]</color> ", commandHistory.text,
                ColorUtility.ToHtmlStringRGB(timeColor), TwoDigits(clock.currentHour.ToString()),
                TwoDigits(clock.currentMinute.ToString()), TwoDigits(clock.currentSecond.ToString("F0")));
        }

        static string TwoDigits(string value) => value.Length == 1 ? "0" + value : value;

        public void AddToHistory(string text, bool useTypewriter, float typewriterDelay = 0.1f)
        {
            commandHistory.text += "\n";
            UpdateTime();
            StopTypewriter();
            if (useTypewriter) typewriter = StartCoroutine(TypeOut(text, typewriterDelay));
            else commandHistory.text += text;
            Relayout();
        }

        public void ClearHistory()
        {
            commandHistory.text = "";
            if (Stamped) UpdateTime();
            commandHistory.text += onEnableText;
            Relayout();
        }

        public void AddNewCommand() => commands.Add(null);

        void StopTypewriter()
        {
            if (typewriter != null) StopCoroutine(typewriter);
            typewriter = null;
        }

        IEnumerator TypeOut(string text, float delay)
        {
            foreach (char c in text)
            {
                commandHistory.text += c;
                // Rebuilding every character keeps the growing text from flickering.
                if (antiFlicker) LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
                if (scrollbar != null) scrollbar.value = 0;
                yield return new WaitForSeconds(delay);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            typewriter = null;
        }

        IEnumerator FixLayout()
        {
            yield return new WaitForSeconds(.02f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            if (scrollbar != null) scrollbar.value = 0;
        }

        IEnumerator FeedbackAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            commandHistory.text += "\n";
            UpdateTime();
            string feedback = commands[commandIndex].feedbackText;
            if (useTypewriterEffect) typewriter = StartCoroutine(TypeOut(feedback, typewriterDelay));
            else commandHistory.text += feedback;
            Relayout();
        }

        IEnumerator ProcessAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            commands[commandIndex].onProcessEvent.Invoke();
            processDelay = null;
        }
    }
}
