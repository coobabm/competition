using LingGuangV05.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop
{
    // Runs before the runtime's -1500 Update so a click at the deadline defers that save.
    [DefaultExecutionOrder(-1600), DisallowMultipleComponent]
    public sealed class DesktopSaveActivity : MonoBehaviour
    {
        void Update()
        {
            var keyboard=Keyboard.current;var mouse=Mouse.current;var touch=Touchscreen.current;var pad=Gamepad.current;
            bool active=keyboard!=null&&keyboard.anyKey.isPressed;
            active|=mouse!=null&&(mouse.delta.ReadValue().sqrMagnitude>0||mouse.scroll.ReadValue().sqrMagnitude>0||mouse.leftButton.isPressed||mouse.rightButton.isPressed||mouse.middleButton.isPressed);
            active|=touch!=null&&touch.primaryTouch.press.isPressed;
            active|=pad!=null&&(pad.buttonSouth.isPressed||pad.buttonNorth.isPressed||pad.buttonEast.isPressed||pad.buttonWest.isPressed||pad.startButton.isPressed||pad.selectButton.isPressed||pad.leftShoulder.isPressed||pad.rightShoulder.isPressed||pad.leftTrigger.ReadValue()>.05f||pad.rightTrigger.ReadValue()>.05f||pad.leftStick.ReadValue().sqrMagnitude>.04f||pad.rightStick.ReadValue().sqrMagnitude>.04f||pad.dpad.ReadValue().sqrMagnitude>0);
            if(active)ChapterOneRuntime.NotifyDesktopInput();
        }
    }
}
