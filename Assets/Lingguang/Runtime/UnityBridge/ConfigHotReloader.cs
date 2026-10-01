using System;
using System.IO;
using Lingguang.Core;
using UnityEngine;

namespace Lingguang.UnityBridge
{
    /// <summary>
    /// 运行时热重载（规格第 2 节）。两个来源，任一变化都会发出 Reloaded：
    /// 1. Inspector 里修改 GameConfigAsset（编辑器内）；
    /// 2. StreamingAssets/lingguang_config.json 文件被改写（打包后也可用，方便试玩时调参）。
    /// 订阅者（Presenter / 调试面板）收到新配置后调用 GameSession.ApplyConfig；返回 false（地图形状变了）时重新开局。
    /// </summary>
    public sealed class ConfigHotReloader : MonoBehaviour
    {
        public GameConfigAsset Asset;
        [Tooltip("优先读取 StreamingAssets 里的 JSON；文件不存在时用 Asset。")]
        public bool PreferJsonFile = true;
        public string JsonFileName = GameConfigAsset.DefaultJsonFileName;
        [Min(0.2f)] public float PollSeconds = 1f;

        public GameConfig Current { get; private set; }
        public event Action<GameConfig> Reloaded;

        DateTime lastWrite;
        float nextPoll;

        void Awake()
        {
            Current = LoadOrKeep(Current);
        }

        void OnEnable()
        {
            if (Asset != null) Asset.Changed += OnAssetChanged;
        }

        void OnDisable()
        {
            if (Asset != null) Asset.Changed -= OnAssetChanged;
        }

        void Update()
        {
            if (!PreferJsonFile || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + PollSeconds;
            string path = GameConfigAsset.JsonPath(JsonFileName);
            if (!File.Exists(path)) return;
            if (File.GetLastWriteTimeUtc(path) == lastWrite) return;
            Reload();
        }

        void OnAssetChanged(GameConfigAsset asset)
        {
            if (PreferJsonFile && File.Exists(GameConfigAsset.JsonPath(JsonFileName))) return;
            Reload();
        }

        [ContextMenu("立即重载")]
        public void Reload()
        {
            var next = LoadOrKeep(Current);
            if (ReferenceEquals(next, Current)) return;
            Current = next;
            Debug.Log("[灵光一闪] 配置已重载");
            if (Reloaded != null) Reloaded(Current);
        }

        /// <summary>读取并校验新配置；失败时打印错误并保留旧配置。</summary>
        GameConfig LoadOrKeep(GameConfig previous)
        {
            try
            {
                GameConfig cfg;
                string path = GameConfigAsset.JsonPath(JsonFileName);
                if (PreferJsonFile && File.Exists(path))
                {
                    lastWrite = File.GetLastWriteTimeUtc(path);
                    cfg = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));
                }
                else if (Asset != null)
                {
                    cfg = Asset.Build();
                }
                else
                {
                    cfg = GameConfig.CreateDefault();
                }
                Board.Create(cfg.Layout, null); // 校验地图能解析
                return cfg;
            }
            catch (Exception e)
            {
                Debug.LogError("[灵光一闪] 配置重载失败，继续使用旧配置：" + e.Message);
                return previous ?? GameConfig.CreateDefault();
            }
        }
    }
}
