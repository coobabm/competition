using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.Casino
{
    /// <summary>
    /// The 888 site's own sounds and icons (Resources/LingGuangV05/Casino, made by Tools/CasinoAssets/casino_sfx.py and
    /// casino_icons.py). Sounds: chip, allin, deal, flip, dice, reel_spin, reel_stop, roulette, win, bigwin, lose.
    /// Icons: logo, slots, dice, roulette, blackjack, chip. A missing file returns null, and callers fall back to the old look.
    /// </summary>
    public static class CasinoAssets
    {
        const string Folder = "LingGuangV05/Casino/casino_";
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        public static AudioClip Sound(string name)
        {
            if (clips.TryGetValue(name, out var clip) && clip != null) return clip;
            clip = Resources.Load<AudioClip>(Folder + name);
            clips[name] = clip;
            return clip;
        }

        public static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var sprite) && sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(Folder + name);
            sprite = texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            if (sprite != null) sprite.name = "casino_" + name;
            icons[name] = sprite;
            return sprite;
        }
    }
}
