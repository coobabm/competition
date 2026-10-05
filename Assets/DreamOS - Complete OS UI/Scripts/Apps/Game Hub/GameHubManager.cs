using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class GameHubManager : MonoBehaviour
    {
        // Content
        public List<GameItem> games = new List<GameItem>();

        // Resources
        [SerializeField] private GameObject gameContent;
        [SerializeField] private Transform gameParent;
        [SerializeField] private ImageFading gameTransition;
        [SerializeField] private GameObject sliderIndicator;
        [SerializeField] private Transform sliderIndicatorParent;
        [SerializeField] private GameObject libraryPreset;
        [SerializeField] private Transform libraryParent;
        [SerializeField] private Image transitionHelper;
        [SerializeField] private Image sliderBanner;
        [SerializeField] private Image sliderIcon;
        [SerializeField] private TextMeshProUGUI sliderDescription;
        [SerializeField] private ButtonManager sliderPlayButton;
        [SerializeField] private Canvas targetCanvas;

        // Settings
        [SerializeField] private bool useLocalization = true;
        [Range(2, 30)] public float sliderTimer = 4;
        [Range(0.05f, 1)] public float sliderScaleSpeed = 0.1f;
        [Range(1, 10)] public float transitionSpeed = 4;

        // Helpers
        int currentSliderIndex;
        float sliderTimerBar;
        bool isInTransition;
        GameObject currentGame;
        GameHubSliderIndicatorItem currentIndicator;
        List<GameHubSliderIndicatorItem> sliderIndicators = new List<GameHubSliderIndicatorItem>();

        public enum WindowMode { Dynamic, FullscreenOnly }

        [System.Serializable]
        public class GameItem
        {
            public string gameTitle = "Game Title";
            [TextArea(2, 6)] public string gameDescription = "Description";
            public Sprite gameIcon;
            public Sprite gameBanner;
            public GameObject gamePrefab;
            public WindowMode windowMode = WindowMode.Dynamic;
            public bool addToSlider = true;

            [Header("Localization")]
            public string descriptionKey;
        }

        void Awake()
        {
            Initialize();
            transitionHelper.color = new Color(transitionHelper.color.r, transitionHelper.color.g, transitionHelper.color.b, 0);
        }

        void OnEnable()
        {
            ResetSlider();
            UpdateSliderInfo();
        }

        void Update()
        {
            if (sliderTimerBar <= sliderTimer && !isInTransition && currentIndicator.gameObject.activeInHierarchy)
            {
                sliderTimerBar += Time.deltaTime;
                currentIndicator.bar.fillAmount = sliderTimerBar / sliderTimer;
                if (currentIndicator.bar.fillAmount >= 1) { NextSliderItem(); }
            }

            sliderBanner.transform.localScale += new Vector3(0.1f, 0.1f, 0.1f) * sliderScaleSpeed * Time.deltaTime;
        }

        void Initialize()
        {
            // Destroy cached objects in parents
            foreach (Transform child in sliderIndicatorParent) { Destroy(child.gameObject); }
            foreach (Transform child in libraryParent) { Destroy(child.gameObject); }

            // Instantiate indicator objects to their parent
            for (int i = 0; i < games.Count; ++i)
            {
                // Cache the index
                int index = i;

                // Spawn library object
                GameObject go = Instantiate(libraryPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(libraryParent, false);
                go.gameObject.name = games[i].gameTitle;

                // Get item component
                GameHubLibraryItem itemComp = go.GetComponent<GameHubLibraryItem>();
                itemComp.gameIndex = index;
                itemComp.SetIcon(games[i].gameIcon);
                itemComp.SetBanner(games[i].gameBanner);
                itemComp.playButton.onClick.AddListener(delegate { LaunchGame(itemComp.gameIndex); });

                if (games[i].addToSlider)
                {
                    // Spawn indicator object
                    GameObject inGo = Instantiate(sliderIndicator, new Vector3(0, 0, 0), Quaternion.identity);
                    inGo.transform.SetParent(sliderIndicatorParent, false);
                    inGo.gameObject.name = games[i].gameTitle;

                    // Get item component
                    GameHubSliderIndicatorItem siComp = inGo.GetComponent<GameHubSliderIndicatorItem>();
                    siComp.gameIndex = index;
                    siComp.animator.enabled = false;
                    siComp.button.onClick.AddListener(delegate { SetSliderItem(inGo.transform.GetSiblingIndex()); });

                    // Add the item to list
                    sliderIndicators.Add(siComp);
                }
            }
        }

        public void LaunchGame(int index)
        {
            if (currentGame != null)
                return;

            if (gameTransition == null) { LaunchGameHelper(index); }
            else
            {
                gameTransition.onFadeInEnd.RemoveAllListeners();
                gameTransition.onFadeInEnd.AddListener(delegate
                {
                    LaunchGameHelper(index);
                    gameTransition.gameObject.SetActive(false);
                });
                gameTransition.FadeIn();
            }
        }

        void LaunchGameHelper(int index)
        {
            // Spawn game object
            GameObject go = Instantiate(games[index].gamePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(gameParent, false);
            currentGame = go;

            // Get game component
            GameHubGameContent goComp = go.GetComponent<GameHubGameContent>();
            goComp.manager = this;
            goComp.gameIndex = index;

            // Enable the game panel
            gameContent.SetActive(true);

            // Check for window mode
            if (games[index].windowMode == WindowMode.FullscreenOnly)
            {
                if (targetCanvas == null) { targetCanvas = GetComponentInParent<Canvas>(); }
                go.transform.SetParent(targetCanvas.transform, false);

                RectTransform tempRect = go.GetComponent<RectTransform>();
                tempRect.offsetMin = new Vector2(0, 0);
                tempRect.offsetMax = new Vector2(0, 0);
            }
        }

        public void ExitGame()
        {
            gameContent.SetActive(false);
            Destroy(currentGame);
            currentGame = null;

            if (gameTransition != null)
            {
                gameTransition.onFadeOutEnd.RemoveAllListeners();
                gameTransition.onFadeOutEnd.AddListener(delegate { gameTransition.gameObject.SetActive(false); });
                gameTransition.FadeOut();
            }
        }

        public void NextSliderItem()
        {
            if (currentSliderIndex == sliderIndicators.Count - 1) { currentSliderIndex = 0; }
            else { currentSliderIndex++; }

            SetSliderItem(currentSliderIndex);
        }

        public void SetSliderItem(int index)
        {
            // Set current slider index
            currentSliderIndex = index;

            if (currentIndicator != null && currentIndicator.gameObject.activeInHierarchy)
            {
                currentIndicator.animator.enabled = true;
                currentIndicator.animator.Play("Out");
            }

            sliderTimerBar = 0;
            currentIndicator.bar.fillAmount = 0;

            // Set new indicator
            currentIndicator = sliderIndicators[currentSliderIndex];
            currentIndicator.animator.enabled = true;
            currentIndicator.animator.Play("In");

            StopCoroutine("DisableIndicatorAnimators");
            StopCoroutine("SliderTransitionIn");
            StopCoroutine("SliderTransitionOut");

            StartCoroutine("DisableIndicatorAnimators");
            StartCoroutine("SliderTransitionIn");
        }

        public void UpdateSliderInfo()
        {
            sliderBanner.sprite = games[currentIndicator.gameIndex].gameBanner;
            sliderIcon.sprite = games[currentIndicator.gameIndex].gameIcon;

            LocalizedObject tempLoc = sliderDescription.gameObject.GetComponent<LocalizedObject>();

            if (!useLocalization || string.IsNullOrEmpty(games[currentIndicator.gameIndex].descriptionKey) || tempLoc == null || !tempLoc.CheckLocalizationStatus()) { sliderDescription.text = games[currentIndicator.gameIndex].gameDescription; }
            else if (tempLoc != null)
            {
                sliderDescription.text = tempLoc.GetKeyOutput(games[currentIndicator.gameIndex].descriptionKey);
            }

            sliderPlayButton.onClick.RemoveAllListeners();
            sliderPlayButton.onClick.AddListener(delegate { LaunchGame(currentIndicator.gameIndex); });

            LayoutRebuilder.ForceRebuildLayoutImmediate(sliderDescription.GetComponent<RectTransform>());
            LayoutRebuilder.MarkLayoutForRebuild(sliderDescription.GetComponent<RectTransform>());
        }

        public void ResetSlider()
        {
            sliderTimerBar = 0;
            if (currentIndicator != null) { currentIndicator.bar.fillAmount = 0; }

            currentIndicator = sliderIndicators[currentSliderIndex];

            if (currentIndicator.gameObject.activeInHierarchy)
            {
                currentIndicator.animator.enabled = true;
                currentIndicator.animator.Play("In");
            }

            StopCoroutine("DisableIndicatorAnimators");
            StartCoroutine("DisableIndicatorAnimators");
        }

        IEnumerator SliderTransitionIn()
        {
            isInTransition = true;

            while (transitionHelper.color.a < 1)
            {
                float alphaValue = transitionHelper.color.a;
                alphaValue += Time.deltaTime * transitionSpeed;
                transitionHelper.color = new Color(transitionHelper.color.r, transitionHelper.color.g, transitionHelper.color.b, alphaValue);
                yield return null;
            }

            isInTransition = false;
            transitionHelper.color = new Color(transitionHelper.color.r, transitionHelper.color.g, transitionHelper.color.b, 1);
            sliderBanner.transform.localScale = new Vector3(1, 1, 1);

            UpdateSliderInfo();
            StartCoroutine("SliderTransitionOut");
        }

        IEnumerator SliderTransitionOut()
        {
            while (transitionHelper.color.a > 0)
            {
                float alphaValue = transitionHelper.color.a;
                alphaValue -= Time.deltaTime * transitionSpeed;
                transitionHelper.color = new Color(transitionHelper.color.r, transitionHelper.color.g, transitionHelper.color.b, alphaValue);
                yield return null;
            }

            transitionHelper.color = new Color(transitionHelper.color.r, transitionHelper.color.g, transitionHelper.color.b, 0);
        }

        IEnumerator DisableIndicatorAnimators()
        {
            yield return new WaitForSeconds(0.55f);
            for (int i = 0; i < sliderIndicators.Count; i++) { sliderIndicators[i].animator.enabled = false; }
        }
    }
}