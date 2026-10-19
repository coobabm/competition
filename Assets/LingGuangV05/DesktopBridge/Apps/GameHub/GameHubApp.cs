using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop game center (2026-10-19). Replaces DreamOS's GameHubManager: same GUID, serialized fields and public
    /// methods, so the Main scene's game list, slider and game panel carry over unchanged.
    ///
    /// The top slider cycles through the games every <see cref="sliderTimer"/> seconds (a dot per game fills up, the
    /// banner slowly zooms, a white flash covers the switch); the library below has a tile per game. Playing a game
    /// spawns its prefab in the game panel (or over the whole desktop for FullscreenOnly games).
    /// </summary>
    public class GameHubApp : MonoBehaviour
    {
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

        public List<GameItem> games = new List<GameItem>();

        [SerializeField] GameObject gameContent;
        [SerializeField] Transform gameParent;
        [SerializeField] ImageFading gameTransition;
        [SerializeField] GameObject sliderIndicator;
        [SerializeField] Transform sliderIndicatorParent;
        [SerializeField] GameObject libraryPreset;
        [SerializeField] Transform libraryParent;
        [SerializeField] Image transitionHelper;
        [SerializeField] Image sliderBanner;
        [SerializeField] Image sliderIcon;
        [SerializeField] TextMeshProUGUI sliderDescription;
        [SerializeField] ButtonManager sliderPlayButton;
        [SerializeField] Canvas targetCanvas;

        [SerializeField] bool useLocalization = true;
        [Range(2, 30)] public float sliderTimer = 4;
        [Range(0.05f, 1)] public float sliderScaleSpeed = 0.1f;
        [Range(1, 10)] public float transitionSpeed = 4;

        int currentSliderIndex;
        float sliderTimerBar;
        bool isInTransition;
        GameObject currentGame;
        GameHubDot currentIndicator;
        readonly List<GameHubDot> sliderIndicators = new List<GameHubDot>();
        Coroutine fadeIn, fadeOut, dotsOff;

        void Awake()
        {
            Initialize();
            SetFlash(0);
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
                if (currentIndicator.bar.fillAmount >= 1) NextSliderItem();
            }
            sliderBanner.transform.localScale += Vector3.one * .1f * sliderScaleSpeed * Time.deltaTime;
        }

        void Initialize()
        {
            foreach (Transform child in sliderIndicatorParent) Destroy(child.gameObject);
            foreach (Transform child in libraryParent) Destroy(child.gameObject);

            for (int i = 0; i < games.Count; i++)
            {
                int index = i;
                var game = games[i];
                var tile = AppSpawn.Row(libraryPreset, libraryParent, game.gameTitle).GetComponent<GameHubTile>();
                tile.gameIndex = index;
                tile.SetIcon(game.gameIcon);
                tile.SetBanner(game.gameBanner);
                tile.playButton.onClick.AddListener(() => LaunchGame(tile.gameIndex));

                if (!game.addToSlider) continue;
                var dotGo = AppSpawn.Row(sliderIndicator, sliderIndicatorParent, game.gameTitle);
                var dot = dotGo.GetComponent<GameHubDot>();
                dot.gameIndex = index;
                dot.animator.enabled = false;
                dot.button.onClick.AddListener(() => SetSliderItem(dotGo.transform.GetSiblingIndex()));
                sliderIndicators.Add(dot);
            }
        }

        /// <summary>Starts a game (behind the fade, when there is one). Only one game runs at a time.</summary>
        public void LaunchGame(int index)
        {
            if (currentGame != null) return;
            if (gameTransition == null) { Spawn(index); return; }
            gameTransition.onFadeInEnd.RemoveAllListeners();
            gameTransition.onFadeInEnd.AddListener(() =>
            {
                Spawn(index);
                gameTransition.gameObject.SetActive(false);
            });
            gameTransition.FadeIn();
        }

        void Spawn(int index)
        {
            var game = AppSpawn.Row(games[index].gamePrefab, gameParent, games[index].gamePrefab.name + "(Clone)");
            currentGame = game;
            var content = game.GetComponent<GameHubGame>();
            content.manager = this;
            content.gameIndex = index;
            gameContent.SetActive(true);

            if (games[index].windowMode == WindowMode.FullscreenOnly)
            {
                if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
                game.transform.SetParent(targetCanvas.transform, false);
                var rect = game.GetComponent<RectTransform>();
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        public void ExitGame()
        {
            gameContent.SetActive(false);
            Destroy(currentGame);
            currentGame = null;
            if (gameTransition == null) return;
            gameTransition.onFadeOutEnd.RemoveAllListeners();
            gameTransition.onFadeOutEnd.AddListener(() => gameTransition.gameObject.SetActive(false));
            gameTransition.FadeOut();
        }

        public void NextSliderItem() => SetSliderItem(currentSliderIndex == sliderIndicators.Count - 1 ? 0 : currentSliderIndex + 1);

        public void SetSliderItem(int index)
        {
            currentSliderIndex = index;
            if (currentIndicator != null && currentIndicator.gameObject.activeInHierarchy)
            {
                currentIndicator.animator.enabled = true;
                currentIndicator.animator.Play("Out");
            }
            sliderTimerBar = 0;
            currentIndicator.bar.fillAmount = 0;

            currentIndicator = sliderIndicators[currentSliderIndex];
            currentIndicator.animator.enabled = true;
            currentIndicator.animator.Play("In");

            Halt(ref fadeOut);
            Restart(ref dotsOff, DotsAnimatorsOff());
            Restart(ref fadeIn, FlashIn());
        }

        /// <summary>Shows the current slider game: banner, icon, (localized) description and the Play button.</summary>
        public void UpdateSliderInfo()
        {
            var game = games[currentIndicator.gameIndex];
            sliderBanner.sprite = game.gameBanner;
            sliderIcon.sprite = game.gameIcon;

            var loc = sliderDescription.GetComponent<LocalizedObject>();
            bool localized = useLocalization && !string.IsNullOrEmpty(game.descriptionKey) && loc != null && loc.CheckLocalizationStatus();
            sliderDescription.text = localized ? loc.GetKeyOutput(game.descriptionKey) : game.gameDescription;

            sliderPlayButton.onClick.RemoveAllListeners();
            sliderPlayButton.onClick.AddListener(() => LaunchGame(currentIndicator.gameIndex));

            var rect = sliderDescription.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        public void ResetSlider()
        {
            sliderTimerBar = 0;
            if (currentIndicator != null) currentIndicator.bar.fillAmount = 0;
            currentIndicator = sliderIndicators[currentSliderIndex];
            if (currentIndicator.gameObject.activeInHierarchy)
            {
                currentIndicator.animator.enabled = true;
                currentIndicator.animator.Play("In");
            }
            Restart(ref dotsOff, DotsAnimatorsOff());
        }

        void SetFlash(float alpha)
        {
            var c = transitionHelper.color;
            transitionHelper.color = new Color(c.r, c.g, c.b, alpha);
        }

        void Restart(ref Coroutine slot, IEnumerator routine)
        {
            Halt(ref slot);
            slot = StartCoroutine(routine);
        }

        void Halt(ref Coroutine slot)
        {
            if (slot != null) StopCoroutine(slot);
            slot = null;
        }

        // The flash covers the banner, the slide changes underneath, then the flash fades away.
        IEnumerator FlashIn()
        {
            isInTransition = true;
            while (transitionHelper.color.a < 1)
            {
                SetFlash(transitionHelper.color.a + Time.deltaTime * transitionSpeed);
                yield return null;
            }
            isInTransition = false;
            SetFlash(1);
            sliderBanner.transform.localScale = Vector3.one;
            UpdateSliderInfo();
            fadeIn = null;
            fadeOut = StartCoroutine(FlashOut());
        }

        IEnumerator FlashOut()
        {
            while (transitionHelper.color.a > 0)
            {
                SetFlash(transitionHelper.color.a - Time.deltaTime * transitionSpeed);
                yield return null;
            }
            SetFlash(0);
            fadeOut = null;
        }

        IEnumerator DotsAnimatorsOff()
        {
            yield return new WaitForSeconds(.55f);
            foreach (var dot in sliderIndicators) dot.animator.enabled = false;
            dotsOff = null;
        }
    }
}
