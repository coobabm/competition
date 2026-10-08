using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgBrainModule
    {
        public string dataset = "";
        /// <summary>Zero explicitly removes a skill. Missing row follows the legacy best checkpoint.</summary>
        public int modelId;
    }

    public sealed partial class XgState
    {
        /// <summary>One manual skill selection per dataset, independent of training and order deployment.</summary>
        public List<XgBrainModule> brainModules = new List<XgBrainModule>();
    }

    public sealed partial class XgSim
    {
        public XgModelEntry BrainModel(string dataset)
        {
            if (XgCatalog.Dataset(dataset) == null) return null;
            var selected = BrainSelection(dataset, out bool duplicate);
            if (duplicate) return null;
            int id = selected != null ? selected.modelId : Best(dataset)?.modelId ?? 0;
            var model = ValidBrainCheckpoint(id);
            return model != null && model.dataset == dataset ? model : null;
        }

        /// <summary>Skill checkpoint accuracy; BrainAccuracy separately measures recent real-LLM judgments.</summary>
        public double BrainModelAccuracy(string dataset)
        {
            if (XgCatalog.Dataset(dataset) == null) return 0;
            var selected = BrainSelection(dataset, out bool duplicate);
            if (duplicate) return 0;
            if (selected != null) return BrainModel(dataset)?.acc ?? 0;
            // Old saves/tests can contain best accuracy without a repository entry. Preserve that fallback only
            // when the player has not made an explicit installation/removal choice.
            double accuracy = BestAcc(dataset);
            return BrainProbability(accuracy) ? accuracy : 0;
        }

        public bool IsBrainInstalled(int id)
        {
            var model = ValidBrainCheckpoint(id);
            return model != null && BrainModel(model.dataset) == model;
        }

        public List<XgModelEntry> BrainModels()
        {
            var models = new List<XgModelEntry>();
            foreach (var dataset in XgCatalog.Datasets)
            {
                var model = BrainModel(dataset.id);
                if (model != null) models.Add(model);
            }
            return models;
        }

        public bool InstallBrainModel(int id, out string reason)
        {
            reason = "";
            var model = ValidBrainCheckpoint(id);
            if (model == null)
            {
                reason = T("检查点不存在或数据损坏，未改变大脑装配。", "The checkpoint is missing or invalid; the brain assembly was not changed.");
                return false;
            }
            var selected = BrainSelection(model.dataset, out bool duplicate);
            if (duplicate)
            {
                reason = T("同一技能存在重复装配记录，未改变大脑装配。", "This skill has duplicate selections; the brain assembly was not changed.");
                return false;
            }
            if (selected != null) selected.modelId = id;
            else
            {
                if (S.brainModules == null) S.brainModules = new List<XgBrainModule>();
                S.brainModules.Add(new XgBrainModule { dataset = model.dataset, modelId = id });
            }
            return true;
        }

        public bool RemoveBrainModel(string dataset, out string reason)
        {
            reason = "";
            if (XgCatalog.Dataset(dataset) == null)
            {
                reason = T("未知技能，未改变大脑装配。", "Unknown skill; the brain assembly was not changed.");
                return false;
            }
            var selected = BrainSelection(dataset, out bool duplicate);
            if (duplicate)
            {
                reason = T("同一技能存在重复装配记录，未改变大脑装配。", "This skill has duplicate selections; the brain assembly was not changed.");
                return false;
            }
            if (selected != null) selected.modelId = 0;
            else
            {
                if (S.brainModules == null) S.brainModules = new List<XgBrainModule>();
                S.brainModules.Add(new XgBrainModule { dataset = dataset, modelId = 0 });
            }
            return true;
        }

        /// <summary>Only trusted catalog labels and numeric accuracy enter the prompt, never user-authored filenames.</summary>
        public string BrainModuleSummary(int limit = 6)
        {
            limit = Math.Max(0, Math.Min(6, limit));
            if (limit == 0) return "";
            var models = BrainModels();
            if (models.Count == 0)
                return S.brainModules != null && S.brainModules.Count > 0
                    ? T("\n当前装配的模型能力：无。", "\nInstalled model capabilities: none.") : "";
            var text = new StringBuilder(T("\n当前装配的模型能力：", "\nInstalled model capabilities:"));
            for (int i = 0; i < models.Count && i < limit; i++)
            {
                var model = models[i]; var dataset = XgCatalog.Dataset(model.dataset); var arch = XgCatalog.Arch(model.arch);
                string item = "\n" + T(dataset.name, dataset.nameEn) + " / " + T(arch.name, arch.nameEn) + " / " + Pct(model.acc);
                if (text.Length + item.Length > 512) break;
                text.Append(item);
            }
            return text.ToString();
        }

        XgBrainModule BrainSelection(string dataset, out bool duplicate)
        {
            duplicate = false; XgBrainModule selected = null;
            if (S.brainModules == null) return null;
            foreach (var module in S.brainModules)
            {
                if (module == null || module.dataset != dataset) continue;
                if (selected != null) { duplicate = true; return null; }
                selected = module;
            }
            return selected;
        }

        XgModelEntry ValidBrainCheckpoint(int id)
        {
            if (id <= 0 || S.models == null) return null;
            XgModelEntry model = null;
            foreach (var candidate in S.models)
            {
                if (candidate == null || candidate.id != id) continue;
                if (model != null) return null;
                model = candidate;
            }
            if (model == null) return null;
            var dataset = XgCatalog.Dataset(model.dataset);
            if (dataset == null || model.track != (int)dataset.track || !ArchitectureFits(XgCatalog.Arch(model.arch), dataset.track)
                || model.depth < 1 || model.depth > 999 || model.width < 0 || model.width >= XgCatalog.Widths.Length
                || model.lr < 0 || model.lr >= RateValues.Length || model.epoch < 0
                || !BrainProbability(model.acc) || !BrainProbability(model.trainAcc)
                || !BrainNonnegative(model.steps) || !BrainNonnegative(model.score) || !BrainNonnegative(model.gameSeconds)) return null;
            return model;
        }

        static bool BrainProbability(double value) => BrainNonnegative(value) && value <= 1;
        static bool BrainNonnegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
