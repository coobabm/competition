using UnityEngine;
using UnityEngine.EventSystems;

namespace HongmengOS.Aero2010
{
    /// <summary>Consume blank panel clicks instead of bubbling them into the outside-click dismiss button.</summary>
    public sealed class AeroStartMenuPanel : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData data) { }
    }
}
