using System.Text;
using Lingguang.Core;
using UnityEngine;

namespace Lingguang.UnityBridge
{
    /// <summary>
    /// 可编辑的棋盘形状资源（Create → 灵光一闪 → 棋盘地图）。规格第 3 节的地图只是第一版草稿：
    /// 改 Rows（每行用空格分隔，\".\" 为无格子，字母为区域）、区域解锁阶段、中线与窄桥、封闭区域与缺口、灯塔锚点后，
    /// 右键组件标题 →“校验地图”检查格子数、连通性和次级沟能否生成。
    /// </summary>
    [CreateAssetMenu(fileName = "BoardLayout", menuName = "灵光一闪/棋盘地图 BoardLayout")]
    public sealed class BoardLayoutAsset : ScriptableObject
    {
        public BoardLayout Layout = BoardLayout.CreateDefault();

        public BoardLayout Build() => JsonUtility.FromJson<BoardLayout>(JsonUtility.ToJson(Layout));

        void Reset()
        {
            Layout = BoardLayout.CreateDefault();
        }

        void OnValidate()
        {
            try
            {
                Board.Create(Build(), null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[灵光一闪] 地图格式有误：" + e.Message, this);
            }
        }

        [ContextMenu("恢复规格默认地图")]
        void RestoreDefaults()
        {
            Layout = BoardLayout.CreateDefault();
        }

        [ContextMenu("校验地图")]
        public void Validate()
        {
            var layout = Build();
            Board board;
            try
            {
                board = Board.Create(layout, null);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[灵光一闪] 地图无法解析：" + e.Message, this);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[灵光一闪] 地图共 ").Append(board.Cells.Count).Append(" 格；");
            foreach (var r in layout.Regions)
                sb.Append(r.Letter).Append('(').Append(r.Name).Append(")=").Append(board.CountRegion(r.Letter[0])).Append(' ');
            sb.AppendLine();
            foreach (Stage st in System.Enum.GetValues(typeof(Stage)))
                sb.Append(st).Append(board.IsConnected(st) ? " 连通  " : " 不连通！  ");
            sb.AppendLine();

            int ok = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                try { Board.Create(layout, new Rng(seed)); ok++; }
                catch (System.InvalidOperationException) { }
            }
            sb.Append("次级沟：100 个种子中 ").Append(ok).Append(" 个能生成连通棋盘；可选边 ").Append(board.SecondaryGrooveCandidates().Count).Append(" 条");
            if (ok == 100) Debug.Log(sb.ToString(), this);
            else Debug.LogWarning(sb.ToString(), this);
        }
    }
}
