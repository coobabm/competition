using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Drives the first-run account setup wizard (2026-10-19). Replaces DreamOS's SetupManager: same GUID,
    /// serialized fields and public API.
    ///
    /// The wizard is a list of <see cref="StepItem"/>s (panel, step indicator, background) that cross-fade as the
    /// player moves with <see cref="NextStep"/>/<see cref="PrevStep"/>/<see cref="SetStep"/>. While the information or
    /// privacy step is showing, <see cref="Update"/> validates the typed name/password against the limits on the
    /// <see cref="UserManager"/> and publishes the result in its nameOK/passwordOK flags, which
    /// <see cref="SetupManagerButtonLimiter"/> turns into error popups and a locked Next button.
    /// <see cref="FinishUp"/> saves the account and reboots.
    /// </summary>
    public class SetupManager : MonoBehaviour
    {
        public List<StepItem> steps = new List<StepItem>();

        // Resources
        public UserManager userManager;
        [SerializeField] private Animator setupScreen;
        [SerializeField] private TMP_InputField firstNameInput;
        [SerializeField] private TMP_InputField lastNameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField passwordRetypeInput;
        [SerializeField] private TMP_InputField secQuestionInput;
        [SerializeField] private TMP_InputField secAnswerInput;

        // Animator states
        const string PanelFadeIn = "Panel In";
        const string PanelFadeOut = "Panel Out";
        const string IndicatorCheck = "Check";

        int currentStepIndex;
        float cachedSetupLength = 1;
        float cachedPanelLength = 1;
        float cachedIndicatorLength = 1;
        Animator currentPanel;
        Animator nextPanel;

        [System.Serializable]
        public class StepItem
        {
            public string title = "Step";
            public ImageFading background;
            public Animator indicator;
            public Animator panel;
            public StepContent content;
        }

        public enum StepContent { Default, Information, Privacy }

        void Awake()
        {
            StepItem first = steps[currentStepIndex];
            cachedSetupLength = DreamOSInternalTools.GetAnimatorClipLength(setupScreen, "SetupScreen_In") + 0.1f;
            cachedPanelLength = DreamOSInternalTools.GetAnimatorClipLength(first.panel, "StepPanel_In") + 0.1f;
            if (first.indicator != null) { cachedIndicatorLength = DreamOSInternalTools.GetAnimatorClipLength(first.indicator, "StepIndicator_Done") + 0.1f; }
        }

        void OnEnable()
        {
            InitializeSteps();
        }

        public void InitializeSteps()
        {
            setupScreen.enabled = true;
            setupScreen.Play("In");

            currentStepIndex = 0;

            currentPanel = steps[currentStepIndex].panel;
            currentPanel.enabled = true;
            currentPanel.gameObject.SetActive(true);
            currentPanel.Play(PanelFadeIn);
            if (steps[currentStepIndex].background != null) { steps[currentStepIndex].background.FadeIn(); }

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].panel == null) { continue; }
                if (steps[i].indicator != null) { steps[i].indicator.enabled = true; }
                if (i != currentStepIndex)
                {
                    steps[i].panel.gameObject.SetActive(false);
                    if (steps[i].background != null) { steps[i].background.gameObject.SetActive(false); }
                }
            }

            StartCoroutine(nameof(DisableSetupAnimator));
            StartCoroutine(nameof(DisableAnimators));
            StartCoroutine(nameof(DisableIndicatorAnimators));

            firstNameInput.text = "";
            lastNameInput.text = "";
            passwordInput.text = "";
            passwordRetypeInput.text = "";
            secQuestionInput.text = "";
            secAnswerInput.text = "";
        }

        void Update()
        {
            if (userManager == null) { return; }

            StepContent content = steps[currentStepIndex].content;

            if (content == StepContent.Information)
            {
                if (IsLengthBetween(firstNameInput.text, userManager.minNameCharacter, userManager.maxNameCharacter))
                {
                    userManager.nameOK = true;
                    userManager.lastNameOK = IsLengthBetween(lastNameInput.text, userManager.minNameCharacter, userManager.maxNameCharacter);
                }
                else
                {
                    userManager.nameOK = false;
                }
            }
            else if (content == StepContent.Privacy)
            {
                string pass = passwordInput.text;

                // An empty password is allowed: the account then simply has none.
                if (IsLengthBetween(pass, userManager.minPasswordCharacter, userManager.maxPasswordCharacter) || pass.Length == 0)
                {
                    userManager.passwordOK = true;
                    userManager.passwordRetypeOK = pass == passwordRetypeInput.text;
                }
                else
                {
                    userManager.passwordOK = false;
                }
            }
        }

        static bool IsLengthBetween(string text, int min, int max)
        {
            return text.Length >= min && text.Length <= max;
        }

        public void SetStep(string newStep)
        {
            int newStepIndex = steps.FindIndex(step => step.title == newStep);
            if (newStepIndex < 0)
            {
                Debug.LogWarning("There is no step named '" + newStep + "' in the step list.", this);
                return;
            }

            if (newStepIndex == currentStepIndex) { return; }

            currentPanel = steps[currentStepIndex].panel;

            Animator leavingIndicator = steps[currentStepIndex].indicator;
            if (leavingIndicator != null)
            {
                leavingIndicator.enabled = true;
                leavingIndicator.Play(IndicatorCheck);

                StopCoroutine(nameof(DisableIndicatorAnimators));
                StartCoroutine(nameof(DisableIndicatorAnimators));
            }

            if (steps[currentStepIndex].background != null) { steps[currentStepIndex].background.FadeOut(); }

            currentStepIndex = newStepIndex;

            nextPanel = steps[currentStepIndex].panel;
            nextPanel.gameObject.SetActive(true);

            if (steps[currentStepIndex].background != null) { steps[currentStepIndex].background.FadeIn(); }

            currentPanel.enabled = true;
            nextPanel.enabled = true;

            currentPanel.Play(PanelFadeOut);
            nextPanel.Play(PanelFadeIn);

            StopCoroutine(nameof(DisablePreviousPanel));
            StartCoroutine(nameof(DisablePreviousPanel));

            StopCoroutine(nameof(DisableAnimators));
            StartCoroutine(nameof(DisableAnimators));
        }

        public void SetStepByIndex(int stepIndex)
        {
            if (stepIndex > steps.Count || stepIndex < 0)
            {
                Debug.LogWarning("Index '" + stepIndex.ToString() + "' doesn't exist.", this);
                return;
            }

            SetStep(steps[stepIndex].title);
        }

        public void NextStep()
        {
            if (currentStepIndex <= steps.Count - 2) { SetStepByIndex(currentStepIndex + 1); }
        }

        public void PrevStep()
        {
            if (currentStepIndex >= 1) { SetStepByIndex(currentStepIndex - 1); }
        }

        public void FinishUp()
        {
            userManager.SetUserCreated(true);
            userManager.SetFirstName(firstNameInput);
            userManager.SetLastName(lastNameInput);
            userManager.SetPassword(passwordInput);
            userManager.SetSecurityQuestion(secQuestionInput);
            userManager.SetSecurityAnswer(secAnswerInput);

            Animator lastIndicator = steps[currentStepIndex].indicator;
            lastIndicator.enabled = true;
            lastIndicator.Play(IndicatorCheck);

            setupScreen.enabled = true;
            setupScreen.Play("Out");
            userManager.bootManager.Reboot();
        }

        // The animators are switched off once their transitions have played, so they stop costing time.

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSeconds(cachedPanelLength + 0.1f);

            for (int i = 0; i < steps.Count; i++)
            {
                if (i == currentStepIndex) { continue; }
                steps[i].panel.gameObject.SetActive(false);
            }
        }

        IEnumerator DisableSetupAnimator()
        {
            yield return new WaitForSeconds(cachedSetupLength + 0.1f);
            setupScreen.enabled = false;
        }

        IEnumerator DisableAnimators()
        {
            yield return new WaitForSeconds(cachedPanelLength + 0.1f);
            if (currentPanel != null) { currentPanel.enabled = false; }
            if (nextPanel != null) { nextPanel.enabled = false; }
        }

        IEnumerator DisableIndicatorAnimators()
        {
            yield return new WaitForSeconds(cachedIndicatorLength + 0.1f);

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].indicator == null) { continue; }
                steps[i].indicator.enabled = false;
            }
        }
    }
}
