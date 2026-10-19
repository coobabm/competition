using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Fakes a system crash screen (2026-10-19). Replaces DreamOS's BSODManager: same GUID, serialized fields and
    /// public API.
    ///
    /// <see cref="CreateBSOD"/> puts a copy of the BSOD prefab on top of the canvas with the given error id, then walks
    /// the step list: each <see cref="StepItem"/> sets the "N% complete" line and waits for its duration. When the
    /// list is done the canvas is toggled (a soft restart), the screen is destroyed and <see cref="onCrashEnd"/> fires.
    /// </summary>
    public class BSODManager : MonoBehaviour
    {
        [SerializeField] private List<StepItem> steps = new List<StepItem>();

        // Resources
        [SerializeField] private GameObject BSODScreen;
        [SerializeField] private Canvas targetCanvas;

        // Events
        public UnityEvent onCrashStart;
        public UnityEvent onCrashEnd;

        // Settings
        public string progressText = "% complete";

        BSODScreen generatedScreen;
        int currentStep;
        int currentProgress = 0;

        [System.Serializable]
        public class StepItem
        {
            [Range(0, 100)] public int progress;
            [Range(0, 10)] public int duration;
            public UnityEvent onStepChanged;
        }

        public void CreateBSOD(string errorID)
        {
            if (generatedScreen != null) { return; }
            if (targetCanvas == null) { targetCanvas = gameObject.GetComponentInParent<Canvas>(); }

            onCrashStart.Invoke();

            GameObject screenObject = Instantiate(BSODScreen, Vector3.zero, Quaternion.identity);
            screenObject.transform.SetParent(targetCanvas.transform, false);
            screenObject.transform.SetAsLastSibling();
            generatedScreen = screenObject.GetComponent<BSODScreen>();

            if (generatedScreen.errorText != null) { generatedScreen.errorText.text = errorID; }

            generatedScreen.progressText.text = string.Format("{0}{1}", currentProgress, progressText);
            StartCoroutine(RunSteps(steps[0].duration));
        }

        /// <summary>
        /// The first wait uses step 0's duration and so does the wait after showing step 0's progress; from then on each
        /// step's own duration follows its progress line (kept as DreamOS had it).
        /// </summary>
        IEnumerator RunSteps(float firstWait)
        {
            float wait = firstWait;

            while (true)
            {
                yield return new WaitForSeconds(wait);

                if (currentStep > steps.Count - 1) { break; }

                generatedScreen.progressText.text = string.Format("{0}{1}", steps[currentStep].progress, progressText);
                wait = steps[currentStep].duration;
                currentStep++;
            }

            currentStep = 0;
            targetCanvas.gameObject.SetActive(false);
            targetCanvas.gameObject.SetActive(true);
            Destroy(generatedScreen.gameObject);
            onCrashEnd.Invoke();
        }
    }
}
