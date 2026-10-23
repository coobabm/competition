using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// One Esc closes one layer. Independent Esc handlers take the key with TryConsume; later callers in the same frame see
    /// it as already used. The overlays run their Update early, topmost first (the orders below follow their canvas sorting
    /// orders), so the layer on top wins and the desktop handlers at the default order only get an Esc nobody above used.
    /// </summary>
    public static class EscapeKey
    {
        /// <summary>Game settings (sorting 32000), saves (31000), funds ledger (30900); desktop windows stay at 0.</summary>
        public const int SettingsOrder = -260, SaveSlotsOrder = -250, FundsOrder = -240;
        static int consumedFrame = -1;

        public static bool Pressed => consumedFrame != Time.frameCount && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        public static bool TryConsume()
        {
            if (!Pressed) return false;
            consumedFrame = Time.frameCount;
            return true;
        }
    }
}
