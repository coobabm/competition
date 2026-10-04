using UnityEngine;
namespace HongmengOS.Retro
{
    public sealed class RetroStartMenu : MonoBehaviour
    {
        public GameObject menu;
        public void Toggle() { if(menu!=null) menu.SetActive(!menu.activeSelf); }
        public void Close() { if(menu!=null) menu.SetActive(false); }
    }
}
