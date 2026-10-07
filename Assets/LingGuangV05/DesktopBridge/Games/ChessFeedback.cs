using System;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Games
{
    /// <summary>
    /// Chess presentation only. Owns two reusable sprite ghosts, never board rules or the global clock.
    /// The caller commits the legal move and reveals the destination piece in the exactly-once callback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChessFeedback : MonoBehaviour
    {
        static readonly Color Mint = new Color32(93, 192, 139, 255);
        static readonly Color Gold = new Color32(245, 192, 72, 255);
        static readonly Color Coral = new Color32(226, 124, 107, 255);
        static readonly Color Muted = new Color32(131, 151, 174, 255);
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        const float TravelSeconds = .22f, CaptureSeconds = .25f, LandingSeconds = .12f;

        RectTransform board, ghostLayer;
        Vector2 boardRest;
        Vector3 boardScale;
        Image movingGhost, capturedGhost;
        XgRingGraphic selection;
        XgJuice juice;
        Material flashMaterial;
        Func<bool> visible;
        Action completion;
        Vector2 from, to, capturedAt, knockDirection, outcomeAt;
        float elapsed, freezeRemaining, selectionAge, celebrationAge, lastTrailAt = -10;
        int whiteCombo, blackCombo;
        bool initialized, moving, capture, whiteMover, impact, reduced, hadVisibility, celebrating, extraWave;

        public bool Busy => moving;
        public int PlayerCombo => whiteCombo;

        public void Init(RectTransform board, TMP_FontAsset font, Func<bool> visible)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            this.visible = visible;
            if (initialized) return;
            this.board = board;
            boardRest = board.anchoredPosition; boardScale = board.localScale;
            juice = GetComponent<XgJuice>();
            if (juice == null) juice = gameObject.AddComponent<XgJuice>();
            // Tests never add AudioSources to an inspected Editor scene. Real play reuses the desktop host.
            var runtime = Application.isPlaying ? FindAnyObjectByType<ChapterOneRuntime>() : null;
            juice.Init(board, board, font, runtime != null ? runtime.gameObject : null);
            juice.LocalHitStop = true; juice.Visible = IsVisible;

            ghostLayer = Rect("Chess Sprite Feedback", board);
            ghostLayer.anchorMin = Vector2.zero; ghostLayer.anchorMax = Vector2.one;
            ghostLayer.offsetMin = ghostLayer.offsetMax = Vector2.zero;
            movingGhost = Ghost("Chess Moving Ghost");
            capturedGhost = Ghost("Chess Captured Ghost");
            var shader = Resources.Load<Shader>("LingGuangV05/Chess/ChessPieceFlash");
            if (shader != null)
            {
                flashMaterial = new Material(shader) { name = "Chess capture silhouette flash", hideFlags = HideFlags.HideAndDontSave };
                capturedGhost.material = flashMaterial;
            }
            else Debug.LogWarning("Chess silhouette flash shader is missing; capture movement remains available.", this);

            var halo = Rect("Chess Selection Glow", ghostLayer);
            halo.sizeDelta = Vector2.one * 78;
            selection = halo.gameObject.AddComponent<XgRingGraphic>();
            selection.Width = 3; selection.raycastTarget = false; selection.color = Color.clear;
            initialized = true;
            SyncPreferences();
            ResetGhosts();
        }

        /// <summary>Board-centred UI coordinates; capturedAt differs from to for en passant.</summary>
        public void Move(Sprite moving, Sprite captured, Vector2 from, Vector2 to, Vector2 capturedAt,
            bool whiteMover, Action completed)
        {
            CompletePending();
            if (!initialized || !IsVisible() || moving == null)
            { InvokeCompleted(completed); return; }
            SyncPreferences();
            this.from = from; this.to = to; this.capturedAt = capturedAt; this.whiteMover = whiteMover;
            capture = captured != null;
            if (whiteMover) whiteCombo = capture ? whiteCombo + 1 : 0;
            else blackCombo = capture ? blackCombo + 1 : 0;
            knockDirection = (to - from).sqrMagnitude > .01f ? (to - from).normalized : Vector2.up;
            elapsed = freezeRemaining = 0; impact = false; this.moving = true; completion = completed;
            hadVisibility = true;
            SetGhost(movingGhost, moving, from);
            if (capture) SetGhost(capturedGhost, captured, capturedAt);
            else capturedGhost.gameObject.SetActive(false);
            SetFlash(0);
            ghostLayer.SetAsLastSibling(); juice.BringToFront();
            juice.Play(XgJuice.Sfx.Id.Whoosh, whiteMover ? 1.2f : .95f, .22f);
        }

        public void Selected(RectTransform visual)
        {
            if (!initialized || !IsVisible() || visual == null) return;
            SyncPreferences();
            selection.rectTransform.anchoredPosition = board.InverseTransformPoint(visual.TransformPoint(visual.rect.center));
            selection.rectTransform.sizeDelta = Vector2.one * 78;
            selectionAge = 0; selection.color = new Color(Mint.r, Mint.g, Mint.b, reduced ? .35f : .7f);
            selection.gameObject.SetActive(true);
            juice.Play(XgJuice.Sfx.Id.Tick, 1.15f, .16f);
        }

        public void Trail(Vector2 at)
        {
            if (!initialized || !IsVisible()) return;
            SyncPreferences();
            if (reduced || Time.unscaledTime - lastTrailAt < .06f) return;
            lastTrailAt = Time.unscaledTime;
            juice.Burst(at, 1, Mint, XgJuice.Shape.Spark, 15, 0, .18f);
        }

        public void Check(Vector2 at)
        {
            if (!initialized || !IsVisible()) return;
            SyncPreferences();
            juice.Shockwave(at, new Color(Coral.r, Coral.g, Coral.b, .7f), reduced ? 36 : 55, .45f, 3);
            juice.Float(at + Vector2.up * 22, GameText.T("将军", "Check"), Coral, 21, 28, .8f, 1.08f);
            juice.Play(XgJuice.Sfx.Id.Ding, .86f, .27f);
        }

        public void Outcome(int chessResult, Vector2 at)
        {
            if (!initialized || !IsVisible() || chessResult == ChessGame.Ongoing) return;
            SyncPreferences(); outcomeAt = at;
            if (chessResult == ChessGame.WhiteWins)
            {
                celebrating = true; extraWave = false; celebrationAge = 0;
                juice.Float(at + Vector2.up * 34, GameText.T("你赢了！", "You win!"), Gold, 38, 65, 1.5f, 1.2f);
                // XgJuice's generic Confetti randomizes hues; two coloured square bursts keep this palette calm.
                juice.Burst(at + Vector2.left * 40, reduced ? 7 : 16, Gold, XgJuice.Shape.Square, 180, 210, 1.35f);
                juice.Burst(at + Vector2.right * 40, reduced ? 7 : 16, Mint, XgJuice.Shape.Square, 180, 210, 1.35f);
                juice.Shockwave(at, new Color(Gold.r, Gold.g, Gold.b, .5f), 145, .8f, 4);
                juice.Play(XgJuice.Sfx.Id.Fanfare, 1.05f, .52f);
            }
            else if (chessResult == ChessGame.BlackWins)
            {
                juice.Float(at, GameText.T("再试一局", "Try again"), Muted, 28, 25, 1.1f, 1);
                juice.Play(XgJuice.Sfx.Id.Thud, .8f, .24f);
            }
            else if (chessResult == ChessGame.Draw)
            {
                juice.Float(at, GameText.T("和棋", "Draw"), Muted, 28, 25, 1.1f, 1);
                juice.Play(XgJuice.Sfx.Id.Ding, .8f, .2f);
            }
        }

        void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>Deterministic presentation step, also used by focused Editor tests.</summary>
        public void Step(float seconds)
        {
            if (!initialized) return;
            if (!IsVisible())
            {
                if (moving || hadVisibility) ClearFeedback();
                hadVisibility = false;
                return;
            }
            hadVisibility = true;
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            SyncPreferences();
            float dt = Mathf.Min(seconds, .05f);
            if (freezeRemaining > 0) { freezeRemaining = Mathf.Max(0, freezeRemaining - dt); return; }
            if (selection != null && selection.gameObject.activeSelf)
            {
                selectionAge += dt;
                float t = Mathf.Clamp01(selectionAge / .45f);
                var c = selection.color; c.a = (1 - t) * (reduced ? .35f : .7f); selection.color = c;
                selection.rectTransform.localScale = Vector3.one * (1 + (reduced ? 0 : .13f * t));
                if (t >= 1) selection.gameObject.SetActive(false);
            }
            if (celebrating)
            {
                celebrationAge += dt;
                if (!extraWave && celebrationAge >= .35f)
                {
                    extraWave = true;
                    juice.Shockwave(outcomeAt, new Color(Mint.r, Mint.g, Mint.b, .35f), 205, 1.1f, 3);
                }
                if (celebrationAge >= 1.5f) celebrating = false;
            }
            if (!moving) return;
            elapsed += dt;
            float travel = reduced ? .12f : TravelSeconds;
            if (elapsed < travel)
            {
                float t = Mathf.Clamp01(elapsed / travel), ease = 1 - Mathf.Pow(1 - t, 3);
                movingGhost.rectTransform.anchoredPosition = Vector2.Lerp(from, to, ease)
                    + Vector2.up * Mathf.Sin(t * Mathf.PI) * (reduced ? 0 : 14);
                movingGhost.rectTransform.localScale = reduced ? Vector3.one : new Vector3(1.02f, .98f, 1);
                return;
            }
            movingGhost.rectTransform.anchoredPosition = to;
            if (!impact)
            {
                impact = true; elapsed = travel;
                if (capture) Impact(); else juice.Play(XgJuice.Sfx.Id.Clack, whiteMover ? 1.12f : .92f, .26f);
                if (freezeRemaining > 0) return;
            }
            float after = elapsed - travel, landing = reduced ? .06f : LandingSeconds;
            float squash = reduced ? 0 : Mathf.Sin(Mathf.Clamp01(after / landing) * Mathf.PI) * .13f;
            movingGhost.rectTransform.localScale = new Vector3(1 + squash, 1 - squash, 1);
            if (capture)
            {
                float t = Mathf.Clamp01(after / (reduced ? .12f : CaptureSeconds));
                float ease = 1 - Mathf.Pow(1 - t, 2);
                capturedGhost.rectTransform.anchoredPosition = capturedAt + (reduced ? Vector2.zero
                    : knockDirection * (38 * ease) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 10));
                capturedGhost.rectTransform.localRotation = Quaternion.Euler(0, 0, reduced ? 0 : (whiteMover ? 22 : -22) * ease);
                float scale = reduced ? 1 : 1 - t * .5f;
                capturedGhost.rectTransform.localScale = new Vector3(scale * (1 + .18f * Mathf.Sin(t * Mathf.PI)),
                    scale * (1 - .18f * Mathf.Sin(t * Mathf.PI)), 1);
                capturedGhost.color = new Color(1, 1, 1, 1 - t * t);
                SetFlash((reduced ? .35f : 1) * (1 - Mathf.Clamp01(after / .09f)));
                if (t < 1) return;
            }
            else if (after < landing) return;
            CompletePending();
        }

        void Impact()
        {
            SetFlash(reduced ? .35f : 1);
            freezeRemaining = reduced ? 0 : .045f;
            juice.HitStop(45); juice.Shake(3, .16f);
            Color accent = whiteMover ? Mint : Coral;
            juice.Burst(capturedAt, reduced ? 4 : 9, accent, XgJuice.Shape.Spark, 110, 200, .36f);
            juice.Shockwave(capturedAt, new Color(accent.r, accent.g, accent.b, .55f), 56, .28f, 3);
            int combo = whiteMover ? whiteCombo : blackCombo;
            string text = combo > 1
                ? GameText.T(whiteMover ? "连吃 × " : "对手连吃 × ", whiteMover ? "Capture chain × " : "Opponent chain × ") + combo
                : GameText.T(whiteMover ? "吃子" : "对手吃子", whiteMover ? "Capture" : "Opponent capture");
            juice.Float(capturedAt + Vector2.up * 25, text, accent, combo > 1 ? 25 : 21, 36, .8f, 1.12f);
            juice.Play(XgJuice.Sfx.Id.Clack, Mathf.Min(1.3f, 1 + (combo - 1) * .05f), .45f);
            juice.Play(XgJuice.Sfx.Id.Thud, .88f, .2f);
        }

        public void ClearFeedback()
        {
            Action done = completion;
            completion = null; moving = false; freezeRemaining = 0; elapsed = 0;
            whiteCombo = blackCombo = 0; celebrating = false; extraWave = false;
            if (juice != null) juice.Clear();
            ResetGhosts();
            if (selection != null) { selection.color = Color.clear; selection.gameObject.SetActive(false); }
            if (board != null) { board.anchoredPosition = boardRest; board.localScale = boardScale; }
            InvokeCompleted(done);
        }

        void CompletePending()
        {
            Action done = completion;
            completion = null; moving = false; freezeRemaining = 0;
            ResetGhosts();
            InvokeCompleted(done);
        }
        static void InvokeCompleted(Action completed)
        {
            if (completed == null) return;
            try { completed(); }
            catch (Exception error) { Debug.LogException(error); }
        }
        void ResetGhosts()
        {
            ResetGhost(movingGhost); ResetGhost(capturedGhost); SetFlash(0);
        }
        static void ResetGhost(Image ghost)
        {
            if (ghost == null) return;
            ghost.gameObject.SetActive(false); ghost.color = Color.white;
            ghost.rectTransform.localScale = Vector3.one; ghost.rectTransform.localRotation = Quaternion.identity;
        }
        void SetFlash(float value)
        {
            if (flashMaterial == null || capturedGhost == null) return;
            flashMaterial.SetFloat(FlashId, value);
            // Stencil masks may give this Graphic a cached derived material; update its single render copy too.
            var renderMaterial = capturedGhost.materialForRendering;
            if (renderMaterial != null && renderMaterial != flashMaterial && renderMaterial.HasProperty(FlashId))
                renderMaterial.SetFloat(FlashId, value);
        }
        void SyncPreferences()
        {
            reduced = PlayerPrefs.GetInt(StoryDesktopPresenter.ReduceMotionPref, 0) != 0;
            if (juice != null) juice.Reduced = reduced;
        }
        bool IsVisible() => initialized && isActiveAndEnabled && board != null && board.gameObject.activeInHierarchy
            && (visible == null || visible());

        Image Ghost(string name)
        {
            var rt = Rect(name, ghostLayer); rt.sizeDelta = Vector2.one * 64;
            var image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false; image.preserveAspect = true;
            return image;
        }
        static void SetGhost(Image image, Sprite sprite, Vector2 position)
        {
            image.sprite = sprite; image.color = Color.white;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.localScale = Vector3.one; image.rectTransform.localRotation = Quaternion.identity;
            image.gameObject.SetActive(true);
        }
        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            return rect;
        }
        void OnDisable() { ClearFeedback(); }
        void OnDestroy()
        {
            ClearFeedback();
            if (flashMaterial != null)
            {
                if (Application.isPlaying) Destroy(flashMaterial); else DestroyImmediate(flashMaterial);
            }
        }
    }
}
