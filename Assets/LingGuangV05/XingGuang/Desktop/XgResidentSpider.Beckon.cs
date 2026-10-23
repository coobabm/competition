using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 首次「打开灵光」 (玩家反馈 F3, 技能树与自动化 v4 §7.2): while the prologue waits for the player to open 灵光.exe the
    /// spider comes out early, before its usual rules allow it (<see cref="XgResident.Present"/> keeps it away until the
    /// setup is saved). It drops on a thread from the top of the screen above the icon (or walks there when it is already
    /// close), settles at the icon's lower right, turns to it and rubs its front legs together, a round every four seconds,
    /// looking at the pointer in between. With reduced effects it fades in beside the icon and rubs one round. It takes no
    /// clicks, and nothing it does changes the save; <see cref="EndBeckon"/> lets it hop once and go back to its rules.
    /// </summary>
    public sealed partial class XgResidentSpider
    {
        const float BeckonDrop = .9f, RubStroke = .4f, RubStrokes = 4, RubEvery = 4, BeckonHop = .3f;
        RectTransform beckonIcon;
        bool beckoning, beckonArrived, beckonReduced, wasRubbing, rubbingRound;
        float beckonClock, rubClock, hopClock = -1, dropClock = -1;
        int rubRounds;
        Vector2 beckonStand, dropFrom;

        /// <summary>The spider is beside the 灵光.exe icon for the first 「打开灵光」 beat (QA and the prologue read it).</summary>
        public bool Beckoning => beckoning;
        /// <summary>It has reached the icon and is rubbing its legs (or waiting for the next round).</summary>
        public bool BeckonArrived => beckonArrived;
        /// <summary>Front-leg rubbing rounds played so far in this beat.</summary>
        public int RubRounds => rubRounds;

        /// <summary>Sends the spider to <paramref name="icon"/> to beckon. False when there is no desktop to draw it on yet.</summary>
        public bool Beckon(RectTransform icon, bool reduced)
        {
            if (icon == null || !Find()) return false;
            if (beckonIcon != icon) { beckonArrived = false; beckonClock = 0; rubClock = 0; rubRounds = 0; dropClock = -1; }
            beckonIcon = icon; beckonReduced = reduced; hopClock = -1;
            return true;
        }

        /// <summary>Ends the beat: a little hop (when <paramref name="hop"/>), then the usual rules decide where it is.</summary>
        public void EndBeckon(bool hop)
        {
            if (!beckoning) { beckonIcon = null; return; }
            if (hop && !beckonReduced && walker != null) hopClock = 0;
            else StopBeckon();
        }

        void StopBeckon()
        {
            beckonIcon = null; beckoning = false; beckonArrived = false; hopClock = -1; dropClock = -1;
            if (walker != null) { walker.offset = Vector2.zero; walker.eyeBoost = 0; for (int i = 0; i < walker.legs.Length; i++) walker.SetLegPose(i, 0, 1, 0, 0); }
        }

        /// <summary>One frame of the beat. True while it owns the spider (the rest of Update is skipped).</summary>
        bool TickBeckon(float dt)
        {
            if (beckonIcon == null && hopClock < 0) { if (beckoning) StopBeckon(); return false; }
            if (beckonIcon != null && !beckonIcon.gameObject.activeInHierarchy) { StopBeckon(); return false; }
            if (layer == null || apps == null) return false;
            screen = layer.rect;
            // Above the prologue's dim (PrologueDirector.Spotlight.cs) when there is one, else where it always lives.
            var dim = desktop.Find("Prologue Spotlight");
            int above = (dim != null && dim.gameObject.activeSelf ? dim.GetSiblingIndex() : apps.GetSiblingIndex()) + 1;
            if (layer.GetSiblingIndex() != above) layer.SetSiblingIndex(above);
            int abilities = Sim != null ? Sim.AbilitiesCount : 0;
            int legs = XgResident.LegCount(abilities);
            float size = (float)XgResident.Size(abilities) * DesktopScale;
            if (beckonIcon != null)
            {
                var icon = LayerRect(beckonIcon);
                beckonStand = new Vector2(icon.xMax + 6 * size, icon.yMin + 4 * size);
                beckonStand.x = Mathf.Clamp(beckonStand.x, screen.xMin + 20, screen.xMax - 20);
                beckonStand.y = Mathf.Clamp(beckonStand.y, screen.yMin + 60, screen.yMax - 20);
            }
            if (!beckoning)
            {
                beckoning = true; beckonArrived = false; beckonClock = 0; rubClock = 0; rubRounds = 0;
                StopWork(); RestoreAll(); motion?.brain.Interrupt();
                bool far = walker == null || !shown || Vector2.Distance(walker.pos, beckonStand) > 900;
                if (walker == null || legCount != legs || Mathf.Abs(walker.size - size) > .01f)
                {
                    walker = new XgSpiderWalker(beckonStand, size, legs) { FindFoothold = Foothold, Planted = OnPlanted, heading = -Mathf.PI / 2 };
                    legCount = legs;
                }
                if (beckonReduced) { walker.pos = beckonStand; walker.Reseat(); layerGroup.alpha = 0; dropClock = -1; }
                else if (far) { dropFrom = new Vector2(beckonStand.x, screen.yMax + 30); walker.pos = dropFrom; dropClock = 0; layerGroup.alpha = 1; }
                else { dropClock = -1; layerGroup.alpha = 1; }
            }
            Show(true);
            beckonClock += dt;
            walker.maxSpeed = 260 * walker.size;
            if (beckonReduced) layerGroup.alpha = Mathf.MoveTowards(layerGroup.alpha, 1, dt / .2f);
            else layerGroup.alpha = 1;

            if (hopClock >= 0)
            {
                // Opened: a little hop, then back to the usual rules (which keep it away until the setup is saved).
                hopClock += dt;
                float k = Mathf.Clamp01(hopClock / BeckonHop);
                walker.offset = new Vector2(0, 14 * walker.size * Mathf.Sin(Mathf.PI * k));
                walker.Settled = true; walker.Tick(dt, walker.pos, 0, beckonReduced);
                graphic.Redraw();
                if (k >= 1) StopBeckon();
                return true;
            }

            if (dropClock >= 0)
            {
                // Down a thread from the top of the screen, feet dangling, onto the spot beside the icon.
                dropClock += dt;
                float k = Mathf.Clamp01(dropClock / BeckonDrop);
                float e = 1 - (1 - k) * (1 - k);
                walker.pos = Vector2.Lerp(dropFrom, beckonStand, e);
                walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, -90, 1 - Mathf.Exp(-6 * dt)) * Mathf.Deg2Rad;
                walker.Carry();
                walker.Tick(dt, walker.pos, 0, beckonReduced);
                if (k >= 1) { dropClock = -1; walker.Reseat(); }
                graphic.Redraw();
                return true;
            }

            float distance = Vector2.Distance(walker.pos, beckonStand);
            if (!beckonArrived && distance > 6)
            {
                walker.Settled = false; walker.turnRate = 6; walker.brake = 60;
                walker.Tick(dt, beckonStand, 1.6f, beckonReduced);
                ClampToScreen();
                graphic.Redraw();
                return true;
            }
            if (!beckonArrived) { beckonArrived = true; rubClock = -.15f; wasRubbing = rubbingRound = false; }

            // At the icon: face it and rub the front legs (4 strokes of 0.4 s), a round every 4 s; look at the pointer in between.
            walker.Settled = true;
            walker.pos = Vector2.Lerp(walker.pos, beckonStand, 1 - Mathf.Exp(-6 * dt));
            var iconCenter = beckonIcon != null ? LayerRect(beckonIcon).center : walker.pos + Vector2.left;
            float look = Mathf.Atan2(iconCenter.y - walker.pos.y, iconCenter.x - walker.pos.x);
            rubClock += dt;
            float roundLength = RubStroke * RubStrokes;
            if (rubClock >= RubEvery) rubClock = 0;
            bool inRound = rubClock >= 0 && rubClock < roundLength;
            if (inRound && !wasRubbing && (!beckonReduced || rubRounds < 1)) { rubRounds++; rubbingRound = true; }
            if (!inRound) rubbingRound = false;
            bool rubbing = inRound && rubbingRound;
            wasRubbing = inRound;
            if (!rubbing && XgCardArt.Pointer(layer, out var pointer))
                look = Mathf.Atan2(pointer.y - walker.pos.y, pointer.x - walker.pos.x);
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, look * Mathf.Rad2Deg, 1 - Mathf.Exp(-5 * dt)) * Mathf.Deg2Rad;
            float phase = rubbing ? rubClock / RubStroke * Mathf.PI * 2 : 0;
            float env = rubbing ? Mathf.Clamp01(Mathf.Min(rubClock, roundLength - rubClock) / .15f) : 0;
            float amp = beckonReduced ? .5f : 1;
            for (int i = 0; i < walker.legs.Length; i++)
            {
                if (i >= 2) { walker.SetLegPose(i, 0, 1, 0, 0); continue; }
                // The two front legs meet under the head and slide past each other, up and down in turn.
                float s = (i == 0 ? 1 : -1) * Mathf.Sin(phase);
                walker.SetLegPose(i, env, .55f, 9 + 3 * amp * s, -3.5f + .8f * amp * s);
            }
            // A slight lean toward the icon while it rubs.
            walker.offset = Vector2.Lerp(walker.offset, rubbing && !beckonReduced ? new Vector2(Mathf.Cos(look), Mathf.Sin(look)) * 3 * walker.size : Vector2.zero, 1 - Mathf.Exp(-8 * dt));
            walker.eyeBoost = rubbing ? .5f : .2f;
            walker.Tick(dt, walker.pos, 0, beckonReduced);
            graphic.Redraw();
            return true;
        }

        /// <summary>The thread it drops on, from the top of the screen to its back.</summary>
        void DrawBeckonThread(VertexHelper vh)
        {
            if (!beckoning || dropClock < 0 || walker == null) return;
            XgDraw.Seg(vh, new Vector2(walker.pos.x, screen.yMax + 40), walker.pos, 1.2f, new Color(214 / 255f, 238 / 255f, 245 / 255f, .55f));
        }
    }
}
