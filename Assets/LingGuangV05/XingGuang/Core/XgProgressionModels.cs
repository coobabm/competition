using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgProject
    {
        public bool started, awaitingAnswer, completed;
        public double gpuSeconds;
        public int experiments, retries;
    }

    public sealed partial class XgState
    {
        public int progressionVersion;
        public int stage = 1, stageVision = 1, stageSequence = 1;
        public string firstSpecialty = "";
        /// <summary>Walls of older saves (design v1.1; removed by 参数量与数据量主线). Still read by the migration, never written.</summary>
        public List<string> walls = new List<string>();
        public List<string> explainedWalls = new List<string>();
        public List<string> migratedBeats = new List<string>();
        public XgProject project = new XgProject();
        public int comboFailureVersion;
        public int comboObservations, spatialObservations, orderObservations, memoryObservations;
        public bool visionCompressionObserved, sequenceCompressionObserved, uncertaintyObserved;
        public bool chapterComplete, endingRegret;
        /// <summary>How older saves passed their walls ("mnist#structure=features"). Read only.</summary>
        public List<string> wallRoutes = new List<string>();
    }

    public sealed partial class XgCard
    {
        public string kind = "";
        public int distance, length;
        public bool secondHalf, progressionObserved;
        public string attentionWord = "", attentionContext = "", explanation = "", explanationEn = "";
        public int attentionRegion = -1;
        public string captionTextEn = "";
        /// <summary>Frozen, explicitly simulated checkpoint prediction for a stage-one combination card. Reading is not displaying.</summary>
        public bool comboPredictionReady, comboPredictionShown, comboPredictedYes;
        public double comboPredictionAccuracy;
        public int comboCheckpointId;
        public bool bottleneckPreview;
        public string patternA = "", patternB = "";
        public string sourceText = "", sourceTextEn = "", candidateText = "", candidateTextEn = "";
    }
}
