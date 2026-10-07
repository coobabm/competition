using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.Games
{
    /// <summary>Native LCD events already use source-camera coordinates. Drop commits; EndDrag only cancels.</summary>
    public sealed class ChessSquareInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public ChessPanel panel;
        public int file, rank;
        public void OnBeginDrag(PointerEventData e) { if (panel != null && e.button == PointerEventData.InputButton.Left) panel.BeginPieceDrag(file, rank, e); }
        public void OnDrag(PointerEventData e) { if (panel != null) panel.DragPiece(e); }
        public void OnEndDrag(PointerEventData e) { if (panel != null) panel.CancelPieceDrag(); }
        public void OnDrop(PointerEventData e) { if (panel != null && e.button == PointerEventData.InputButton.Left) panel.DropPiece(file, rank, e); }
        void OnDisable() { if (panel != null) panel.CancelPieceDrag(); }
    }
}
