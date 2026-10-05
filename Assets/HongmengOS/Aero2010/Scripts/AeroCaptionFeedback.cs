using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace HongmengOS.Aero2010
{
 // Own caption drags so button gestures never propagate to WindowDragger.
 public sealed class AeroCaptionFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
 {
  public Image face;Color rest;bool hover,pressed;
  void Awake(){if(face!=null)rest=face.color;}
  void Apply(){if(face!=null)face.color=pressed?rest*.75f:hover?Color.Lerp(rest,Color.white,.25f):rest;}
  public void OnPointerEnter(PointerEventData e){hover=true;Apply();}
  public void OnPointerExit(PointerEventData e){hover=false;pressed=false;Apply();}
  public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left){pressed=true;Apply();}}
  public void OnPointerUp(PointerEventData e){pressed=false;Apply();}
  public void OnBeginDrag(PointerEventData e){}
  public void OnDrag(PointerEventData e){}
  public void OnEndDrag(PointerEventData e){pressed=false;Apply();}
  void OnDisable(){hover=false;pressed=false;Apply();}
 }
}
