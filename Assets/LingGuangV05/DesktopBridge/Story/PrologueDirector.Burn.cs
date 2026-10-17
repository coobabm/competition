using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The prologue's 是 (Step 5): 0.txt said the BIOS was half written, and it was right. The picture tears apart, the
    /// machine blue-screens with the stuck 滋—— of a crashed sound card, and the power dies; a black card says it took a
    /// long repair; it boots to the same desktop, the thing is still there (<see cref="Prologue.StillHereKey"/>), and the
    /// question comes back. It repeats for as long as the player says 是.
    /// </summary>
    public sealed partial class PrologueDirector
    {
        const float TearSeconds = 1.8f, BsodSeconds = 4.6f, DeadSeconds = .8f;
        const float RepairFade = .6f, RepairHold = 2.6f, BootSeconds = 2.4f;
        const int Strips = 18;
        static readonly Color BsodBlue = new Color32(0, 0, 170, 255);

        /// <summary>The same 您是否要关机？ again, spoken again, the mouse given back.</summary>
        void AskShutdownAgain()
        {
            var question = ShowShutdownDialog();
            question.maxVisibleCharacters = 99999;
            var clip = PrologueContent.ShutdownVoice();
            if (clip != null) voice.PlayOneShot(clip);
            Hijack(false);
            dialog.SetAsLastSibling();
        }

        IEnumerator BurnAndRepair(int burns)
        {
            Hijack(true);
            desk.HideCursor();
            XgJuice.Sfx.Init(gameObject);
            var size = desk.layer.rect.size;

            var root = PrologueDesk.Rect("Crash", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetAsLastSibling();
            spawned.Add(root.gameObject);
            PrologueDesk.Fill(PrologueDesk.Rect("Block", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.clear); // eats the clicks

            // 1. The picture tears: the frame on screen, cut into bands that slip sideways, split into red and cyan, stick.
            var frozen = FreezeFrame();
            var tear = PrologueDesk.Rect("Tear", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var strips = new List<(RawImage main, RawImage red, RawImage cyan, float y0, float y1)>();
            if (frozen != null)
            {
                PrologueDesk.Fill(PrologueDesk.Rect("Under", tear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.black, false);
                for (int i = 0; i < Strips; i++)
                {
                    float y0 = i / (float)Strips, y1 = (i + 1) / (float)Strips;
                    var red = Band(tear, frozen, y0, y1, new Color(1, .1f, .1f, .55f));
                    var cyan = Band(tear, frozen, y0, y1, new Color(.1f, 1, 1, .55f));
                    var main = Band(tear, frozen, y0, y1, Color.white);
                    strips.Add((main, red, cyan, y0, y1));
                }
            }
            var flash = PrologueDesk.Fill(PrologueDesk.Rect("Flash", tear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.clear, false);
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Buzz, .7f);
            bool second = false;
            float nextJolt = 0;
            for (float t = 0; t < TearSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / TearSeconds; // it gets worse
                if (!second && k > .5f) { second = true; XgJuice.Sfx.Play(XgJuice.Sfx.Id.Buzz, .45f); }
                if (t >= nextJolt)
                {
                    nextJolt = t + Random.Range(.03f, .09f);
                    foreach (var s in strips)
                    {
                        bool slip = Random.value < .25f + .6f * k;
                        float dx = slip ? Random.Range(-1f, 1f) * size.x * (.02f + .25f * k) : 0;
                        float split = slip ? Random.Range(4f, 10f + 30f * k) : 0;
                        // A stuck band shows another band's picture.
                        float src = Random.value < .15f * k ? Random.Range(0, Strips) / (float)Strips : s.y0;
                        var uv = new Rect(0, src, 1, s.y1 - s.y0);
                        s.main.uvRect = s.red.uvRect = s.cyan.uvRect = uv;
                        s.main.rectTransform.anchoredPosition = new Vector2(dx, 0);
                        s.red.rectTransform.anchoredPosition = new Vector2(dx - split, 0);
                        s.cyan.rectTransform.anchoredPosition = new Vector2(dx + split, 0);
                        bool gone = Random.value < .08f * k;
                        s.main.enabled = !gone; s.red.enabled = s.cyan.enabled = !gone && split > 0;
                    }
                    flash.color = Random.value < .12f ? new Color(1, 1, 1, Random.Range(.1f, .4f)) : Random.value < .1f + .2f * k ? new Color(0, 0, 0, Random.Range(.3f, .9f)) : Color.clear;
                }
                yield return null;
            }

            // 2. The blue screen, and the sound card stuck on its last few milliseconds: 滋——
            Destroy(tear.gameObject);
            if (frozen != null) frozen.Release();
            var bsod = PrologueDesk.Fill(PrologueDesk.Rect("Bsod", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), BsodBlue, false);
            float font = Mathf.Max(14, size.y / 36);
            var text = desk.Text(PrologueDesk.Rect("Text", bsod.rectTransform, Vector2.zero, Vector2.one, new Vector2(size.x * .06f, size.y * .06f), new Vector2(-size.x * .06f, -size.y * .07f)),
                "", font, Color.white, TextAlignmentOptions.TopLeft);
            var buzz = Buzz();
            string body = L("bsod");
            for (float t = 0; t < BsodSeconds; t += Time.unscaledDeltaTime)
            {
                // The dump counts up while the buzz holds.
                int dumped = Mathf.Clamp(Mathf.FloorToInt((t - .8f) / (BsodSeconds - 1.6f) * 100), 0, 100);
                text.text = body + (t > .8f ? "  " + dumped : "");
                yield return null;
            }

            // 3. The power goes: the buzz stops dead, black.
            buzz.Stop();
            Destroy(buzz);
            Destroy(bsod.gameObject);
            XgJuice.Sfx.Play(XgJuice.Sfx.Id.Thud, .5f);
            PrologueDesk.Fill(PrologueDesk.Rect("Black", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.black, false);
            yield return PrologueDesk.Wait(DeadSeconds);

            // 4. The repair.
            var card = desk.Text(PrologueDesk.Centered("Repair", root, Vector2.zero, new Vector2(size.x - 160, 120)), L(Prologue.RepairKey(burns)), 30, Color.clear, TextAlignmentOptions.Center);
            for (float t = 0; t < RepairFade; t += Time.unscaledDeltaTime) { card.color = new Color(1, 1, 1, t / RepairFade); yield return null; }
            card.color = Color.white;
            yield return PrologueDesk.Wait(RepairHold);
            for (float t = 0; t < RepairFade; t += Time.unscaledDeltaTime) { card.color = new Color(1, 1, 1, 1 - t / RepairFade); yield return null; }
            card.text = "";

            // 5. It boots, to the same desktop.
            var boot = desk.Text(PrologueDesk.Centered("Boot", root, new Vector2(0, -120), new Vector2(800, 60)), L("boot"), 30, Color.white, TextAlignmentOptions.Center);
            var dots = new List<Image>();
            for (int i = 0; i < 4; i++) dots.Add(PrologueDesk.Fill(PrologueDesk.Centered("Dot", root, new Vector2(-45 + i * 30, 0), new Vector2(18, 18)), new Color32(70, 160, 255, 255), false));
            for (float t = 0; t < BootSeconds; t += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < dots.Count; i++) dots[i].color = new Color(.27f, .63f, 1, .35f + .65f * Mathf.Clamp01(Mathf.Sin(t * 6 - i * .8f)));
                yield return null;
            }
            boot.text = "";
            Destroy(root.gameObject);
            if (notepad != null) notepad.SetAsLastSibling(); // 0.txt is still open, and still right
        }

        /// <summary>A copy of what the monitor shows right now (null if the LCD camera is not found).</summary>
        static RenderTexture FreezeFrame()
        {
            var go = GameObject.Find("LCD Source Camera");
            var cam = go != null ? go.GetComponent<Camera>() : null;
            var target = cam != null ? cam.targetTexture : null;
            if (target == null) return null;
            var copy = new RenderTexture(target.descriptor);
            Graphics.Blit(target, copy);
            return copy;
        }

        static RawImage Band(RectTransform parent, Texture frame, float y0, float y1, Color tint)
        {
            var rt = PrologueDesk.Rect("Band", parent, new Vector2(0, y0), new Vector2(1, y1), Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = frame; img.uvRect = new Rect(0, y0, 1, y1 - y0); img.color = tint; img.raycastTarget = false;
            return img;
        }

        static AudioClip buzzClip;

        /// <summary>The crashed sound card: one ~46 ms buffer (mains hum, a whine and noise) looped until the power dies.</summary>
        AudioSource Buzz()
        {
            if (buzzClip == null)
            {
                const int rate = 44100, n = 2048;
                var data = new float[n];
                var rng = new System.Random(20160524);
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    float hum = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 120 * t)) * .35f;
                    float whine = Mathf.Sin(2 * Mathf.PI * 2350 * t) * .2f;
                    float noise = ((float)rng.NextDouble() * 2 - 1) * .3f;
                    data[i] = Mathf.Clamp(hum + whine + noise, -1, 1) * .8f;
                }
                buzzClip = AudioClip.Create("BsodBuzz", n, 1, rate, false);
                buzzClip.SetData(data, 0);
            }
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = buzzClip; source.loop = true; source.volume = .45f; source.spatialBlend = 0; source.ignoreListenerPause = true;
            source.Play();
            return source;
        }
    }
}
