using UnityEngine;

namespace LingGuangV05.UI
{
    /// <summary>A pointy-top hexagonal button; raycasts exclude its transparent corners.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HexCellGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            Vector2 center = bounds.center;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < 6; i++)
            {
                float angle = (90f + 60f * i) * Mathf.Deg2Rad;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * bounds.width * .5f,
                    Mathf.Sin(angle) * bounds.height * .5f), color, Vector2.zero);
            }
            for (int i = 0; i < 6; i++) mesh.AddTriangle(0, i + 1, (i + 1) % 6 + 1);
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint,
                eventCamera, out Vector2 local)) return false;
            Rect rect = rectTransform.rect;
            float x = Mathf.Abs((local.x - rect.center.x) / (rect.width * .5f));
            float y = Mathf.Abs((local.y - rect.center.y) / (rect.height * .5f));
            return x <= .86603f && y <= 1f - x / 1.73205f;
        }
    }
}
