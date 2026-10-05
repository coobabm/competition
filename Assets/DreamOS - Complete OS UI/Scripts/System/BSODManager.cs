using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    public class BSODManager : MonoBehaviour
    {
        // List
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

            GameObject go = Instantiate(BSODScreen, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            go.transform.SetParent(targetCanvas.transform, false);
            go.transform.SetAsLastSibling();
            generatedScreen = go.GetComponent<BSODScreen>();

            if (generatedScreen.errorText != null)
            {
                generatedScreen.errorText.text = errorID;
            }

            generatedScreen.progressText.text = string.Format("{0}{1}", currentProgress, progressText);
            StartCoroutine(StoryTellerHelper(steps[0].duration));
        }

        IEnumerator StoryTellerHelper(float timer)
        {
            yield return new WaitForSeconds(timer);

            if (currentStep <= steps.Count - 1)
            {
                generatedScreen.progressText.text = string.Format("{0}{1}", steps[currentStep].progress, progressText);
                StartCoroutine(StoryTellerHelper(steps[currentStep].duration));
                currentStep++;
            }

            else
            {
                currentStep = 0;
                targetCanvas.gameObject.SetActive(false);
                targetCanvas.gameObject.SetActive(true);
                Destroy(generatedScreen.gameObject);
                onCrashEnd.Invoke();
            }
        }
    }
}