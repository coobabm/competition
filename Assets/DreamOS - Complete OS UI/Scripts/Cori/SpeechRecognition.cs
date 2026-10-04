using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
#if UNITY_STANDALONE_WIN || UNITY_WSA
using UnityEngine.Windows.Speech;
#endif

namespace Michsky.DreamOS
{
	public class SpeechRecognition : MonoBehaviour
	{
		// Serialized fields exist on every platform so the editor and player layouts match: building the Windows
		// player from a macOS editor fails otherwise ("script class layout is incompatible"). Only the Windows
		// speech API code below stays platform-specific. (Local change for 灵光, 2026-10-03.)
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
		List<string> commandsHelper = new List<string>();

		// Helpers
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
				this.enabled = false;
				stopSTT = true;
				return;
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
			if (enableLogs == true)
			{
				Debug.Log("<b>[Speech Recognition]</b> Speech System Status: " + status);
			}
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
					Debug.LogErrorFormat("<b>[Speech Recognition]</b> Dictation completed unsuccessfully: {0}.", completionCause);
			});

			dictationRecognizer.DictationError += ((string error, int hresult) =>
			{
				if (enableLogs == true)
					Debug.LogErrorFormat("<b>[Speech Recognition]</b> Dictation error: {0}; HResult = {1}.", error, hresult);
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

			if (enableLogs == true)
			{
				Debug.Log("<b>[Speech Recognition]</b> Dictation stopped.");
			}
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
				StringBuilder builder = new StringBuilder();
				builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
				recognitions += builder.ToString();

                if (enableLogs == true) { Debug.Log("<b>[Speech Recognition]</b> Keyword recognized: <b>" + args.text + " (" + args.confidence + ")</b>"); }
				if (AudioManager.instance != null) { AudioManager.instance.audioSource.PlayOneShot(listeningEffect); }
                if (coriPopup != null) { coriPopup.OpenPanel(); }

                onKeywordCall.Invoke();
				StopKeywordRecognizer();
				StartCommandRecognizer();
				StartCoroutine("WaitForCommand", dismissAfter);
			});

			recognitions = "";
			keywordRecognizer.Start();
			hypotheses = "Listening for: \n" + string.Join(", ", keywords);
		}

		public void StopKeywordRecognizer()
		{
			if (keywordRecognizer.IsRunning) { keywordRecognizer.Stop(); }
            if (enableLogs == true) { Debug.Log("<b>[Speech Recognition]</b> Keyword recognizer stopped."); }

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
				StopCoroutine("WaitForCommand");

				StringBuilder builder = new StringBuilder();
				builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
				recognitions += builder.ToString();

				if (enableLogs == true) { Debug.Log("<b>[Speech Recognition]</b> Command recognized: <b>" + args.text + " (" + args.confidence + ")</b>"); }
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
			if (commandRecognizer != null && commandRecognizer.IsRunning) { commandRecognizer.Stop(); }
            if (enableLogs == true) { Debug.Log("<b>[Speech Recognition]</b> Command recognizer stopped."); }

            commandRecognizer.Dispose();
			commandRecognizer = null;
		}

        IEnumerator WaitForCommand(float waitFor)
		{
			yield return new WaitForSeconds(waitFor);

			onDismiss.Invoke();

			if (commandRecognizer.IsRunning) { StopCommandRecognizer(); }
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