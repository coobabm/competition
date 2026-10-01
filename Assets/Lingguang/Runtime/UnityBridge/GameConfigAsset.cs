using System;
using System.IO;
using Lingguang.Core;
using UnityEngine;

namespace Lingguang.UnityBridge
{
    /// <summary>
    /// 全部数值的 Inspector 资源（Create → 灵光一闪 → 游戏配置）。
    /// 运行时不要直接改 Config 字段给模拟器用，而是调用 Build() 拿一份深拷贝：
    /// 这样 Inspector 里改数值不会悄悄改掉正在进行的一局，热重载由 ConfigHotReloader 统一处理。
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "灵光一闪/游戏配置 GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        public const string DefaultJsonFileName = "lingguang_config.json";

        [Tooltip("可选：用单独的地图资源覆盖下面 Config.Layout。留空则用 Config.Layout。")]
        public BoardLayoutAsset LayoutOverride;

        [Tooltip("电量相关数值都是内部值（显示值 × 2）。")]
        public GameConfig Config = GameConfig.CreateDefault();

        /// <summary>Inspector 里有改动时触发（编辑器内）。</summary>
        public event Action<GameConfigAsset> Changed;

        public GameConfig Build()
        {
            var copy = JsonUtility.FromJson<GameConfig>(JsonUtility.ToJson(Config));
            if (LayoutOverride != null) copy.Layout = LayoutOverride.Build();
            return copy;
        }

        public string ToJson() => JsonUtility.ToJson(Build(), true);

        public static string JsonPath(string fileName) => Path.Combine(Application.streamingAssetsPath, fileName);

        void Reset()
        {
            Config = GameConfig.CreateDefault();
        }

        void OnValidate()
        {
            if (Changed != null) Changed(this);
        }

        [ContextMenu("恢复规格默认值")]
        void RestoreDefaults()
        {
            Config = GameConfig.CreateDefault();
            OnValidate();
        }

        [ContextMenu("导出 JSON 到 StreamingAssets")]
        public void ExportJson()
        {
            string path = JsonPath(DefaultJsonFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ToJson());
            Debug.Log("[灵光一闪] 已导出配置：" + path);
        }

        [ContextMenu("从 StreamingAssets 导入 JSON")]
        public void ImportJson()
        {
            string path = JsonPath(DefaultJsonFileName);
            if (!File.Exists(path)) { Debug.LogWarning("[灵光一闪] 找不到 " + path); return; }
            Config = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));
            OnValidate();
            Debug.Log("[灵光一闪] 已导入配置：" + path);
        }
    }
}
