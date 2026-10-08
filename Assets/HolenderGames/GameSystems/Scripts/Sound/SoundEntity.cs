using System;
using UnityEngine;

namespace HolenderGames.Sound
{
    /// <summary>
    /// This class holds all data related to the playback of a single AudioClip.
    /// </summary>
    [Serializable]
    public class SoundEntity
    {
        public GameSound soundType;
        public AudioClip audioClip;

        [Range(0, 1)]
        public float volumeLow = 1f;
        [Range(0, 1)]
        public float volumeHigh = 1f;
        [Range(0, 2)]
        public float pitchLow = 1f;
        [Range(0, 2)]
        public float pitchHigh = 1f;
        public float startTime = 0f;

    }
}