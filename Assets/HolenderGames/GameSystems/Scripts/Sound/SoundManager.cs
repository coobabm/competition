using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HolenderGames.Sound
{
    /// <summary>
    /// Managed all the sound effects in the game. 
    /// Pooling of AudioSources, playing sound entities with modifiable pitch, volume, etc.
    /// </summary>
    public class SoundManager : MonoBehaviour, ICoroutineControl
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private bool _canGrowPool = true;

        public float globalVolumeMultiplier = 1f;

        [SerializeField]
        private SoundEntity[] _soundList = default;

        private Dictionary<GameSound, List<SoundEntity>> _soundMap = new Dictionary<GameSound, List<SoundEntity>>();
        private Stack<SoundPlayer> _soundPlayerPool;

        private readonly Vector3 _zeroVector = Vector3.zero;

        private const float RELEASE_MARGIN = 0.05f;
        private const float RETRYRELEASE_WAIT = 0.1f;
        private const string SOUNDPLAYER_GO_NAMEBASE = "SoundPlayer";
        private int _soundPlayerNameIndex = 0;

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

          

            PopulateSoundMap();
            GrowPool(_initialPoolSize);

            DontDestroyOnLoad(transform.gameObject);

        }



        public AudioSource PlaySound(GameSound soundType)
        {
            return Play_Internal(soundType, globalVolumeMultiplier, 1);
        }
      
        private AudioSource Play_Internal(GameSound soundType, float volumeMultiplier, float pitchMultiplier)
        {
            var (canPlay, soundList) = SoundPlayPreChecks(soundType);

            if (!canPlay)
                return null;

            return _soundPlayerPool.Pop()
                .Play(GetRandomSound(soundList), volumeMultiplier, pitchMultiplier);
        }

        /// <summary>
        /// Converts the Editor-compatible array into a fast-lookup dictionary map.
        /// Creates a list for each sound type, to support multiple sounds of the same type.
        /// </summary>
        private void PopulateSoundMap()
        {
            foreach (var s in _soundList)
            {
                // Silently skip entries where 'None' is selected as soundtype
                if (s.soundType == GameSound.None)
                    continue;

                // Skip entries where audioclip is missing
                if (s.audioClip == null)
                {
                    continue;
                }

                if (_soundMap.TryGetValue(s.soundType, out var list))
                    // If a list already exists for the given sound type, simply add an additional entry to it.
                    list.Add(s);
                else
                    // If the list doesn't exist yet, instantiate and add a new list, and initialize it to contain the first entry.
                    _soundMap.Add(s.soundType, new List<SoundEntity>() { s });
            }
         
        }

        /// <summary>
        /// Grows pool by the specified number. Creates pool if it doesn't yet exist.
        /// </summary>
        private void GrowPool(int num)
        {
            if (_soundPlayerPool == null)
                CreatePool(num);

            for (int i = 0; i < num; i++)
                _soundPlayerPool.Push(CreateSoundPlayer());

            void CreatePool(int capacity)
            {
                // If initial pool size is greater, use that instead
                if (_initialPoolSize > capacity)
                    capacity = _initialPoolSize;
                // If pool can grow, reserve double
                if (_canGrowPool)
                    capacity *= 2;

                _soundPlayerPool = new Stack<SoundPlayer>(capacity);
            }

            SoundPlayer CreateSoundPlayer()
            {
                _soundPlayerNameIndex++;
                var go = new GameObject(SOUNDPLAYER_GO_NAMEBASE + _soundPlayerNameIndex);
                go.transform.parent = this.transform;

                var audioSource = go.AddComponent<AudioSource>();

                var soundPlayer = new SoundPlayer(audioSource, this);
                soundPlayer.PlaybackComplete += OnPlaybackFinished;

                return soundPlayer;
            }
        }

        /// <summary>
        /// Returns SoundPlayer to the pool after it finished playing a sound.
        /// </summary>
        private void OnPlaybackFinished(SoundPlayer player)
            => _soundPlayerPool.Push(player);

        /// <summary>
        /// Returns whether playback of a given soundtype is possible, and if so, returns all sound variations available for the given soundtype (or null).
        /// </summary>
        private (bool canPlay, List<SoundEntity> availableSounds) SoundPlayPreChecks(GameSound soundType)
        {
            if (soundType == GameSound.None)
            {
                return (canPlay: false, null);
            }

            var soundListExists = _soundMap.TryGetValue(soundType,
                out var soundList); 

            if (!soundListExists)
            {
                return (canPlay: false, null);
            }

            if (_soundPlayerPool.Count == 0)
            {
                // Playback fails if pool is exhausted, and we can't grow
                if (!_canGrowPool)
                {
                    return (canPlay: false, null);
                }

                // If pool can grow, grow pool, and proceed with playback
                GrowPool(1);
            }

            return (canPlay: true, soundList);
        }

        private SoundEntity GetRandomSound(List<SoundEntity> list)
            => list[Random.Range(0, list.Count)];

        /// <summary>
        /// Encapsulates sound playback and AudioSource handling responsibilities.
        /// Provides notification of playback completion.
        /// </summary>
        private class SoundPlayer
        {
            public event Action<SoundPlayer> PlaybackComplete;
            public bool IsPlaying => _isWaiting;

            private readonly AudioSource _audioSource;
            private readonly Transform _audioTransform;
            private readonly ICoroutineControl _coroutines;

            private GameSound _currentSound;
            private Coroutine _currentCoroutine;
            private bool _isWaiting;

            public SoundPlayer(AudioSource audioSource, ICoroutineControl coroutineControl)
            {
                _audioSource = audioSource;
                _coroutines = coroutineControl;

                // Cache
                _audioTransform = _audioSource.transform;
            }

            public AudioSource Play(SoundEntity soundEntity, float volumeMultiplier, float pitchMultiplier)
            {
                _currentSound = soundEntity.soundType;
                var waitingTime = Play_Internal(soundEntity, volumeMultiplier, pitchMultiplier);
                _currentCoroutine = _coroutines.StartCoroutine(
                PlaybackWaiter(waitingTime));
               
                return _audioSource;
            }

            /// <summary>
            /// Preps the AudioSource and plays the specified sound.
            /// </summary>
            private float Play_Internal(SoundEntity sound, float volumeMultiplier, float pitchMultiplier)
            {
                // Prepare audio source
                var pitch = Random.Range(sound.pitchLow, sound.pitchHigh) * pitchMultiplier;
                _audioSource.volume = Random.Range(sound.volumeLow, sound.volumeHigh) * volumeMultiplier;
                _audioSource.pitch = pitch;
                _audioSource.clip = sound.audioClip;
                _audioSource.time = sound.startTime;

                // Calculate actual time length of sound playback 
                var playTime = Mathf.Abs(sound.audioClip.length / pitch); // Abs() is to support negative pitch

                // Start actual playback
                _audioSource.Play();

                return playTime;
            }

            /// <summary>
            /// Waits for audio playback to finish, then executes notifications
            /// </summary>
            private IEnumerator PlaybackWaiter(float releaseAfterSeconds)
            {
                _isWaiting = true;

                // Actual wait
                yield return new WaitForSecondsRealtime(releaseAfterSeconds + RELEASE_MARGIN);

                // Make sure it's actually finished
                while (_audioSource.isPlaying)
                {
                    yield return new WaitForSeconds(RETRYRELEASE_WAIT);
                }

                _isWaiting = false;
                DoStopped();
            }
            private void DoStopped()
            {
                if (_isWaiting)
                    throw new InvalidOperationException("Playback completion handling cannot execute. Active playback still registered.");

                PlaybackComplete?.Invoke(this);
                _currentCoroutine = null;
                _currentSound = GameSound.None;
            }
        }
    }

    public interface ICoroutineControl
    {
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);
    }
}
