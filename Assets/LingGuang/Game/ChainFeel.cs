using System;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>
    /// Chain "juice" ported from RipplePrototype's RippleFeedback: every fire emits an expanding ring,
    /// camera trauma accumulates with fires, and big chains briefly slow down at milestones.
    /// Presentation only — removing it never changes a rule result. Tune on the LingGuangGame component.
    /// </summary>
    [Serializable]
    public sealed class ChainFeelSettings
    {
        [Header("放电圆环")]
        public bool fireRings = true;
        [Tooltip("圆环最大半径（格）")]
        public float ringRadiusCells = 1.4f;
        [Tooltip("起点的圆环半径倍率")]
        public float startRingScale = 1.5f;
        [Tooltip("圆环扩散时长（秒）")]
        public float ringLife = 0.42f;
        public float ringWidthStart = 0.07f;
        public float ringWidthEnd = 0.012f;
        public float ringIntensity = 1.8f;
        [Tooltip("同一拍放电过多时，每拍最多画几个圆环")]
        public int maxRingsPerBeat = 12;

        [Header("震屏（随放电累积）")]
        public float shakePerFire = 0.012f;
        public float maxShake = 0.2f;
        [Tooltip("每秒恢复量")]
        public float shakeRecover = 0.7f;

        [Header("慢动作")]
        [Tooltip("本次连锁放电数达到这些值时触发慢动作")]
        public int[] slowMoMilestones = { 15, 30, 60 };
        [Range(0.05f, 1f)] public float slowMoScale = 0.35f;
        [Tooltip("慢动作持续（真实秒）")]
        public float slowMoDuration = 0.35f;

        public bool IsMilestone(int firesThisChain)
        {
            if (slowMoMilestones == null) return false;
            foreach (int m in slowMoMilestones)
                if (m > 0 && m == firesThisChain) return true;
            return false;
        }
    }
}
