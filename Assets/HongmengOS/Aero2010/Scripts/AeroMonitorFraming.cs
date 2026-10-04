using UnityEngine;
namespace HongmengOS.Aero2010
{
 [RequireComponent(typeof(Camera))]
 public sealed class AeroMonitorFraming : MonoBehaviour
 {
  Camera view;
  void Awake(){view=GetComponent<Camera>();}
  void LateUpdate(){if(view==null)return;view.orthographicSize=Mathf.Max(1.64f,2.79f/Mathf.Max(.1f,view.aspect));}
 }
}
