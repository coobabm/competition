using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    public class WindowPanelManager : MonoBehaviour
    {
        // List
        public List<PanelItem> panels = new List<PanelItem>();

        // Resources
        [SerializeField] private RectTransform indicator;

        // Settings
        public int currentPanelIndex = 0;
        private int currentButtonIndex = 0;
        private int newPanelIndex;
        public bool cullPanels = true;
        [SerializeField] private bool initializeButtons = true;
        [SerializeField] private bool bypassAnimationOnEnable = false;
        [Range(0.75f, 2)] public float panelAnimationSpeed = 1;
        [SerializeField] private AnimationCurve indicatorCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f));
        [SerializeField] [Range(0.1f, 5)] private float indicatorDuration = 0.5f;

        // Events
        [System.Serializable] public class PanelChangeEvent : UnityEvent<int> { }
        public PanelChangeEvent onPanelChanged;

        // Helpers
        Animator currentPanel;
        Animator nextPanel;
        PanelButton currentButton;
        PanelButton nextButton;
        string panelFadeIn = "In";
        string panelFadeOut = "Out";
        string animSpeedKey = "AnimSpeed";
        bool isInitialized = false;
        float cachedStateLength = 1;

        [System.Serializable]
        public class PanelItem
        {
            [Tooltip("[Required] This is the variable that you use to call specific panels.")]
            public string panelName;
            [Tooltip("[Required] Main panel object.")]
            public Animator panelObject;
            [Tooltip("[Optional] If you want the panel manager to have tabbing capability, you can assign a panel button here.")]
            public PanelButton panelButton;
            [Tooltip("[Optional] Alternate panel button variable that supports standard buttons instead of panel buttons.")]
            public ButtonManager altPanelButton;
        }

        void Awake()
        {
            if (panels.Count == 0)
                return;

            cachedStateLength = DreamOSInternalTools.GetAnimatorClipLength(panels[currentPanelIndex].panelObject, "WindowPanel_In") + 0.1f;
            InitializePanels();
        }

        void Start()
        {
            if (panels.Count == 0)
                return;

            InitializePanels();
        }

        void OnEnable()
        {
            if (isInitialized == false)
                return;

            if (bypassAnimationOnEnable)
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    if (panels[i].panelObject == null)
                        continue;

                    if (currentPanelIndex == i)
                    {
                        panels[i].panelObject.gameObject.SetActive(true);
                        panels[i].panelObject.enabled = true;
                        panels[i].panelObject.Play("Instant In");
                    }

                    else
                    {
                        panels[i].panelObject.gameObject.SetActive(false);
                    }
                }
            }

            else if (isInitialized && !bypassAnimationOnEnable && nextPanel == null)
            {
                currentPanel.enabled = true;
                currentPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                currentPanel.Play(panelFadeIn);
                if (currentButton != null) { currentButton.SetSelected(true); }
            }

            else if (isInitialized && !bypassAnimationOnEnable && nextPanel != null)
            {
                nextPanel.enabled = true;
                nextPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                nextPanel.Play(panelFadeIn);
                if (nextButton != null) { nextButton.SetSelected(true); }
            }

            StopCoroutine("DisablePreviousPanel");
            StopCoroutine("DisableAnimators");
            StartCoroutine("DisableAnimators");
        }

        public void InitializePanels()
        {
            if (panels[currentPanelIndex].panelButton != null)
            {
                currentButton = panels[currentPanelIndex].panelButton;
                currentButton.SetSelected(true);
            }

            currentPanel = panels[currentPanelIndex].panelObject;
            currentPanel.enabled = true;
            currentPanel.gameObject.SetActive(true);

            currentPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
            currentPanel.Play(panelFadeIn);

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelObject == null) { continue; }
                if (i != currentPanelIndex && cullPanels) { panels[i].panelObject.gameObject.SetActive(false); }
                if (initializeButtons)
                {
                    string tempName = panels[i].panelName;
                    if (panels[i].panelButton != null) { panels[i].panelButton.onClick.AddListener(() => OpenPanel(tempName)); }
                    if (panels[i].altPanelButton != null) { panels[i].altPanelButton.onClick.AddListener(() => OpenPanel(tempName)); }
                }
            }

            if (indicator != null && panels[currentPanelIndex].panelButton != null)
            {
                indicator.anchorMin = new Vector2(0, 0.5f);
                indicator.anchorMax = new Vector2(0, 0.5f);

                if (panels[currentPanelIndex].panelButton != null)
                {
                    indicator.transform.SetParent(panels[currentPanelIndex].panelButton.transform, true);
                    indicator.anchoredPosition = new Vector2(0, 0);
                }

                else
                {
                    indicator.sizeDelta = new Vector2(indicator.rect.width, 0);
                }
            }

            onPanelChanged.Invoke(currentPanelIndex);
            isInitialized = true;

            StopCoroutine("DisableAnimators");
            StartCoroutine("DisableAnimators");
        }

        public void OpenFirstPanel() { OpenPanelByIndex(0); }
        public void OpenPanel(string newPanel)
        {
            bool catchedPanel = false;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == newPanel)
                {
                    newPanelIndex = i;
                    catchedPanel = true;
                    break;
                }
            }

            if (!catchedPanel)
            {
                Debug.LogWarning("There is no panel named '" + newPanel + "' in the panel list.", this);
                return;
            }

            if (newPanelIndex != currentPanelIndex)
            {
                if (cullPanels) { StopCoroutine("DisablePreviousPanel"); }

                currentPanel = panels[currentPanelIndex].panelObject;

                if (panels[currentPanelIndex].panelButton != null) { currentButton = panels[currentPanelIndex].panelButton; }

                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.gameObject.SetActive(true);

                currentPanel.enabled = true;
                nextPanel.enabled = true;

                currentPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                nextPanel.SetFloat(animSpeedKey, panelAnimationSpeed);

                currentPanel.Play(panelFadeOut);
                nextPanel.Play(panelFadeIn);

                if (cullPanels) { StartCoroutine("DisablePreviousPanel"); }

                currentButtonIndex = newPanelIndex;

                if (currentButton != null) { currentButton.SetSelected(false); }
                if (panels[currentButtonIndex].panelButton != null)
                {
                    nextButton = panels[currentButtonIndex].panelButton;
                    nextButton.SetSelected(true);
                }

                // Check for indicator and start the coroutine
                StopCoroutine("MoveIndicatorToParent");

                if (indicator != null && panels[currentButtonIndex].panelButton != null) { StartCoroutine("MoveIndicatorToParent", panels[currentButtonIndex].panelButton.transform); }
                else if (indicator != null && panels[currentButtonIndex].panelButton == null) { StartCoroutine("SetIndicatorHeight", 0); }

                onPanelChanged.Invoke(currentPanelIndex);

                StopCoroutine("DisableAnimators");
                StartCoroutine("DisableAnimators");
            }
        }

        public void OpenPanelByIndex(int panelIndex)
        {
            if (panelIndex > panels.Count || panelIndex < 0)
            {
                Debug.LogWarning("Index '" + panelIndex.ToString() + "' doesn't exist.", this);
                return;
            }

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == panels[panelIndex].panelName)
                {
                    OpenPanel(panels[panelIndex].panelName);
                    break;
                }
            }
        }

        public void NextPanel()
        {
            if (currentPanelIndex <= panels.Count - 2)
            {
                OpenPanelByIndex(currentPanelIndex + 1);
            }
        }

        public void PrevPanel()
        {
            if (currentPanelIndex >= 1)
            {
                OpenPanelByIndex(currentPanelIndex - 1);
            }
        }

        public void ShowCurrentPanel()
        {
            if (nextPanel == null)
            {
                StopCoroutine("DisableAnimators");
                StartCoroutine("DisableAnimators");

                currentPanel.enabled = true;
                currentPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                currentPanel.Play(panelFadeIn);
            }

            else
            {
                StopCoroutine("DisableAnimators");
                StartCoroutine("DisableAnimators");

                nextPanel.enabled = true;
                nextPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                nextPanel.Play(panelFadeIn);
            }
        }

        public void HideCurrentPanel()
        {
            if (nextPanel == null)
            {
                StopCoroutine("DisableAnimators");
                StartCoroutine("DisableAnimators");

                currentPanel.enabled = true;
                currentPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                currentPanel.Play(panelFadeOut);
            }

            else
            {
                StopCoroutine("DisableAnimators");
                StartCoroutine("DisableAnimators");

                nextPanel.enabled = true;
                nextPanel.SetFloat(animSpeedKey, panelAnimationSpeed);
                nextPanel.Play(panelFadeOut);
            }
        }

        public void ShowCurrentButton()
        {
            if (nextButton == null) { currentButton.SetSelected(true); }
            else { nextButton.SetSelected(true); }
        }

        public void HideCurrentButton()
        {
            if (nextButton == null) { currentButton.SetSelected(false); }
            else { nextButton.SetSelected(false); }
        }

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSeconds((cachedStateLength + 0.1f) * panelAnimationSpeed);

            for (int i = 0; i < panels.Count; i++)
            {
                if (i == currentPanelIndex)
                    continue;

                panels[i].panelObject.gameObject.SetActive(false);
            }
        }

        IEnumerator DisableAnimators()
        {
            yield return new WaitForSeconds((cachedStateLength + 0.1f) * panelAnimationSpeed);
            if (currentPanel != null) { currentPanel.enabled = false; }
            if (nextPanel != null) { nextPanel.enabled = false; }
        }

        IEnumerator MoveIndicatorToParent(Transform parent)
        {
            float elapsedTime = 0;
            StopCoroutine("SetIndicatorHeight");

            if (parent != null) 
            {
                indicator.transform.SetParent(parent, true);
                StartCoroutine("SetIndicatorHeight", parent.GetComponent<RectTransform>().sizeDelta.y); 
            }

            else 
            {
                StartCoroutine("SetIndicatorHeight", 0);
                StopCoroutine("MoveIndicatorToParent");
            }

            while (elapsedTime < indicatorDuration)
            {
                elapsedTime += Time.deltaTime / indicatorDuration;
                indicator.anchoredPosition = new Vector2(indicator.anchoredPosition.x, Mathf.Lerp(indicator.anchoredPosition.y, 0, indicatorCurve.Evaluate(elapsedTime)));
                yield return null;
            }

            indicator.anchoredPosition = new Vector2(indicator.anchoredPosition.x, 0);
        }

        IEnumerator SetIndicatorHeight(float targetHeight = 0)
        {
            float elapsedTime = 0;
            Vector2 startPos = new Vector2(indicator.sizeDelta.x, indicator.sizeDelta.y);
            Vector2 endPos = new Vector2(indicator.sizeDelta.x, targetHeight);

            while (elapsedTime < (indicatorDuration * 2))
            {
                elapsedTime += Time.deltaTime / (indicatorDuration / 4);
                indicator.sizeDelta = Vector2.Lerp(startPos, endPos, indicatorCurve.Evaluate(elapsedTime));
                yield return null;
            }

            indicator.sizeDelta = endPos;
        }
    }
}