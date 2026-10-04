using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace Michsky.DreamOS
{
    public class CommanderManager : MonoBehaviour
    {
        // Command List
        public List<CommandItem> commands = new List<CommandItem>();

        // Settings
        [TextArea] public string errorText = "is not recognized as a command.";
        [TextArea] public string onEnableText = "Welcome to Commander.";
        public string helpCommand = "help";
        public Color textColor;
        public bool enableHelpCommand = true;
        [SerializeField] private bool antiFlicker = true;

        // Resources
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private TextMeshProUGUI commandHistory;
        [SerializeField] private Scrollbar scrollbar;

        // Typewriter
        public bool useTypewriterEffect = false;
        [Range(0.001f, 0.5f)] public float typewriterDelay = 0.03f;

        // Time
        public bool getTimeData = true;
        public Color timeColor = new Color(0, 255, 0);

        // Helpers
        string currentCommand;
        int commandIndex;
        string typewriterHelper;
        RectTransform historyRT;
        RectTransform historyParentRT;

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

        void OnEnable()
        {
            commandHistory.text = "";
            commandInput.text = "";
            UpdateColors();
            if (getTimeData && DateAndTimeManager.instance != null) { UpdateTime(); }
            commandHistory.text = commandHistory.text + onEnableText;
            commandInput.ActivateInputField();
            StartCoroutine("FixLayout");
        }

        void Awake()
        {
            historyRT = commandHistory.GetComponent<RectTransform>();
            historyParentRT = commandHistory.transform.parent.GetComponent<RectTransform>();
        }

        void Update()
        {
            if (string.IsNullOrEmpty(commandInput.text) || EventSystem.current.currentSelectedGameObject != commandInput.gameObject) { return; }
            else if (!commandInput.isFocused) { commandInput.ActivateInputField(); }

            if (Keyboard.current.enterKey.wasPressedThisFrame) { ProcessCommand(); }
        }

        public void UpdateColors()
        {
            commandInput.textComponent.color = textColor;
            commandHistory.color = textColor;
        }

        public void ProcessCommand()
        {
            // Reset previously called command
            currentCommand = "";
            commandIndex = -1;
            currentCommand = commandInput.text;

            // Stop previous typewriter
            StopCoroutine("ApplyTypewriter");

            // Apply a new line
            commandHistory.text = commandHistory.text + "\n";

            // Update time data
            if (getTimeData && DateAndTimeManager.instance != null) { UpdateTime(); }

            // Apply called command
            commandHistory.text = commandHistory.text + commandInput.text;

            // Check for help command
            if (enableHelpCommand && currentCommand == helpCommand)
            {
                // Apply commands
                for (int i = 0; i < commands.Count; i++)
                {
                    if (!commands[i].includeToHelp) { continue; }
                    commandHistory.text = commandHistory.text + string.Format("\n[{0}] {1} ", commands[i].command, commands[i].commandDescription);
                }

                // Reset input
                commandInput.text = "";

                // Make sure that layout is displayed properly
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
                StartCoroutine("FixLayout");

                // Set scrollbar value
                if (scrollbar != null) { scrollbar.value = 0; }

                // Don't process further
                return;
            }

            // Search within command list
            for (int i = 0; i < commands.Count; i++)
            {
                if (currentCommand == commands[i].command)
                {
                    currentCommand = commands[i].command;
                    commandIndex = i;
                    break;
                }
            }

            // Show error if there's an error
            if (commandIndex == -1)
            {
                // Apply a new line
                commandHistory.text = commandHistory.text + "\n";

                // Update time data
                if (getTimeData && DateAndTimeManager.instance != null) { UpdateTime(); }

                // Apply error text
                commandHistory.text = string.Format("{0}'{1}' {2}", commandHistory.text, commandInput.text, errorText);

                // Reset input
                commandInput.text = "";

                // Make sure that layout is displayed properly
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
                StartCoroutine("FixLayout");

                // Set scrollbar value
                if (scrollbar != null) { scrollbar.value = 0; }

                // Don't process further
                return;
            }

            // Process feedback test
            if (commands[commandIndex].feedbackText != "") { StartCoroutine("WaitForFeedbackDelay", commands[commandIndex].feedbackDelay); }

            // Invoke events
            if (commands[commandIndex].onProcessDelay == 0) { commands[commandIndex].onProcessEvent.Invoke(); }
            else { StartCoroutine("WaitForProcessDelay", commands[commandIndex].onProcessDelay); }

            // Reset input
            commandInput.text = "";

            // Make sure that layout is displayed properly
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            StartCoroutine("FixLayout");

            // Set scrollbar value
            if (scrollbar != null) { scrollbar.value = 0; }
        }

        IEnumerator ApplyTypewriter(float delay)
        {
            for (int i = 0; i < typewriterHelper.Length; i++)
            {
                commandHistory.text = commandHistory.text + typewriterHelper[i].ToString();
               
                // Prevent stuttering/flickering issues if there's any
                if (antiFlicker) { LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT); }

                // Set scrollbar value
                if (scrollbar != null) { scrollbar.value = 0; }

                yield return new WaitForSeconds(delay);
            }

            // Rebuild layout to prevent visual issues
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
        }

        public void UpdateTime()
        {
            if (getTimeData && DateAndTimeManager.instance != null)
            {
                string colorValue = ColorUtility.ToHtmlStringRGB(timeColor);
                commandHistory.text = string.Format("{0}<color=#{1}>[", commandHistory.text, colorValue);

                if (DateAndTimeManager.instance.currentHour.ToString().Length == 1) { commandHistory.text = string.Format("{0}0{1}:", commandHistory.text, DateAndTimeManager.instance.currentHour); }
                else { commandHistory.text = string.Format("{0}{1}:", commandHistory.text, DateAndTimeManager.instance.currentHour); }

                if (DateAndTimeManager.instance.currentMinute.ToString().Length == 1) { commandHistory.text = string.Format("{0}0{1}:", commandHistory.text, DateAndTimeManager.instance.currentMinute); }
                else { commandHistory.text = string.Format("{0}{1}:", commandHistory.text, DateAndTimeManager.instance.currentMinute); }

                if (DateAndTimeManager.instance.currentSecond.ToString("F0").Length == 1) { commandHistory.text = string.Format("{0}0{1}", commandHistory.text, DateAndTimeManager.instance.currentSecond.ToString("F0")); }
                else { commandHistory.text = string.Format("{0}{1}", commandHistory.text, DateAndTimeManager.instance.currentSecond.ToString("F0")); }

                commandHistory.text = string.Format("{0}]</color> ", commandHistory.text);
            }
        }

        public void AddToHistory(string text, bool useTypewriter, float typewriterDelay = 0.1f)
        {
            commandHistory.text = commandHistory.text + "\n";

            UpdateTime();
            StopCoroutine("ApplyTypewriter");

            if (useTypewriter == true) { typewriterHelper = text; StartCoroutine("ApplyTypewriter", typewriterDelay); }
            else { commandHistory.text = commandHistory.text + text; }

            // Make sure that layout is displayed properly
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            StartCoroutine("FixLayout");
        }

        public void ClearHistory()
        {
            // Reset input
            commandHistory.text = "";

            // Apply enable text
            if (getTimeData && DateAndTimeManager.instance != null) { UpdateTime(); }
            commandHistory.text = commandHistory.text + onEnableText;

            // Make sure that layout is displayed properly
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            StartCoroutine("FixLayout");
        }

        public void AddNewCommand() { commands.Add(null); }

        IEnumerator FixLayout()
        {
            yield return new WaitForSeconds(0.02f);

            // Rebuild layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);

            // Set scrollbar value
            if (scrollbar != null) { scrollbar.value = 0; }
        }

        IEnumerator WaitForFeedbackDelay(float forSec)
        {
            yield return new WaitForSeconds(forSec);
          
            // Add new line and apply time
            commandHistory.text = commandHistory.text + "\n";
            UpdateTime();

            // Check for typewriter
            if (useTypewriterEffect == true) { typewriterHelper = commands[commandIndex].feedbackText; StartCoroutine("ApplyTypewriter", typewriterDelay); }
            else { commandHistory.text = commandHistory.text + commands[commandIndex].feedbackText; }

            // Make sure that layout is displayed properly
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyRT);
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentRT);
            StartCoroutine("FixLayout");
        }

        IEnumerator WaitForProcessDelay(float forSec)
        {
            yield return new WaitForSeconds(forSec);
            commands[commandIndex].onProcessEvent.Invoke();
            StopCoroutine("WaitForProcessDelay");
        }
    }
}