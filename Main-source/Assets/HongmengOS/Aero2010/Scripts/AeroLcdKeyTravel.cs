using UnityEngine;
using UnityEngine.EventSystems;
namespace HongmengOS.Aero2010
{
    /// <summary>Small scene-local mechanical key movement; no frame polling.</summary>
    public sealed class AeroLcdKeyTravel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Transform keyCap;
        [Min(0)] public float travel=.018f;
        Vector3 rest;
        bool pressed;
        void Awake(){if(keyCap!=null)rest=keyCap.localPosition;}
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left||keyCap==null||pressed)return;
            rest=keyCap.localPosition;pressed=true;keyCap.localPosition=rest+Vector3.forward*travel;
        }
        public void OnPointerUp(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)Release();}
        public void OnPointerExit(PointerEventData data){Release();}
        void OnDisable(){Release();}
        void Release(){if(pressed&&keyCap!=null)keyCap.localPosition=rest;pressed=false;}
    }
}
