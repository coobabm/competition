using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The player asked the resident spider to keep quiet: it sits still in a corner. Old saves read false.</summary>
        public bool residentQuiet;
    }

    /// <summary>What the resident spider is doing.</summary>
    public enum XgResidentMode { Hidden = 0, Roam = 1, Watch = 2, Parked = 3 }

    /// <summary>A rectangle in desktop pixels (y up), for the resident's rules without engine types.</summary>
    public struct XgBox
    {
        public double x, y, w, h;
        public XgBox(double x, double y, double w, double h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public double XMax => x + w;
        public double YMax => y + h;
        public double CenterX => x + w / 2;
        public double CenterY => y + h / 2;
        public bool Contains(double px, double py) => px >= x && px <= XMax && py >= y && py <= YMax;
    }

    /// <summary>
    /// The resident spider: 灵光 given a body, living on the desktop over every window and reading whatever is open
    /// (XgResidentSpider draws it). These are its rules, free of the engine so they can be tested: when it is there at
    /// all, how it grows with the abilities, which chat it keeps off, and where it perches to watch the labelling card.
    /// </summary>
    public static class XgResident
    {
        /// <summary>Legs by abilities learned: four at first, six from the second ability, all eight from the fifth.</summary>
        public static int LegCount(int abilities) => abilities <= 1 ? 4 : abilities <= 4 ? 6 : 8;

        /// <summary>Body scale by abilities: 0.7 at the first, 0.1 more per ability, 1.2 from the sixth.</summary>
        public static double Size(int abilities) => .7 + .1 * Math.Max(0, Math.Min(5, abilities - 1));

        /// <summary>
        /// On the desktop at all: 灵光.exe is installed and set up (the prologue is over), and nothing is playing that
        /// owns the screen (a cutscene, the epiphany, the love question, the origin curtain, a fullscreen video).
        /// </summary>
        public static bool Present(bool installed, bool inPrologue, bool cutscene, bool fullscreenVideo) =>
            installed && !inPrologue && !cutscene && !fullscreenVideo;

        /// <summary>
        /// May it walk on 晴雯's YY conversation? Not once the player answered 【算了】 to the love question
        /// (<see cref="XgState.loveConsent"/> = −1); before the question, or with consent, it may. Mail and other
        /// people's chats are always open to it.
        /// </summary>
        public static bool MayReadQingwen(XgState s) => s == null || s.loveConsent != -1;

        /// <summary>Gap between the labelling card's edge and the spider's body when it perches.</summary>
        public const double PerchGap = 46;

        /// <summary>
        /// Where it sits to watch the labelling card: beside the card's right edge a little above the middle, or beside
        /// the left edge when the right one would leave the screen. Returns the body position and whether it faces left
        /// (toward a card on its left).
        /// </summary>
        public static void Perch(XgBox card, XgBox screen, double size, out double x, out double y, out bool facesLeft)
        {
            double gap = PerchGap * Math.Max(.5, size);
            y = Math.Max(screen.y + gap, Math.Min(screen.YMax - gap, card.y + card.h * .58));
            if (card.XMax + gap * 1.6 <= screen.XMax) { x = card.XMax + gap; facesLeft = true; }
            else { x = Math.Max(screen.x + gap, card.x - gap); facesLeft = false; }
        }

        /// <summary>The parking corner when it is told to keep quiet: low on the right, above the taskbar.</summary>
        public static void ParkingSpot(XgBox screen, double size, out double x, out double y)
        {
            x = screen.XMax - 70 * Math.Max(.5, size);
            y = screen.y + 60 * Math.Max(.5, size);
        }
    }

    /// <summary>
    /// The resident's mood while the player labels, as a small state machine: it comes over when the 标注台 shows,
    /// stays while answers keep coming, leans in when the player hesitates, looks a beat longer at a card where the
    /// player went against its own guess, pulses after every answer, and wanders off after a while without one.
    /// </summary>
    public sealed class XgResidentMind
    {
        /// <summary>Seconds without an answer before it wanders off again.</summary>
        public const double WanderAfter = 12;
        /// <summary>Seconds on one card before it leans in.</summary>
        public const double HesitateAfter = 3.5;
        /// <summary>Seconds it keeps looking at a card the player answered against its guess.</summary>
        public const double PonderSeconds = 1.6;

        public XgResidentMode Mode { get; private set; } = XgResidentMode.Hidden;
        /// <summary>Seconds since the last answer (or since the 标注台 came up).</summary>
        public double SinceAnswer { get; private set; }
        /// <summary>Leaning toward the card: the player is taking a while.</summary>
        public bool Leaning => Mode == XgResidentMode.Watch && SinceAnswer >= HesitateAfter && Pondering <= 0;
        /// <summary>Seconds left of the longer look after a disagreement.</summary>
        public double Pondering { get; private set; }
        /// <summary>Answers seen; the view pulses when it goes up.</summary>
        public int Pulses { get; private set; }

        int answers = -1;
        bool labelWasVisible;

        /// <summary>
        /// One step. <paramref name="answerCount"/> is the label page's running count of answers;
        /// <paramref name="lastDisagreed"/> tells whether the latest answer went against the checkpoint's guess.
        /// </summary>
        public XgResidentMode Tick(double dt, bool present, bool quiet, bool labelVisible, int answerCount, bool lastDisagreed)
        {
            if (dt < 0 || double.IsNaN(dt)) dt = 0;
            SinceAnswer += dt;
            if (Pondering > 0) Pondering = Math.Max(0, Pondering - dt);
            bool answered = answers >= 0 && answerCount > answers;
            answers = answerCount;
            if (!present) { Mode = XgResidentMode.Hidden; labelWasVisible = false; return Mode; }
            if (quiet) { Mode = XgResidentMode.Parked; labelWasVisible = labelVisible; return Mode; }
            if (labelVisible && !labelWasVisible) { SinceAnswer = 0; Mode = XgResidentMode.Watch; }
            labelWasVisible = labelVisible;
            if (!labelVisible) { Mode = XgResidentMode.Roam; return Mode; }
            if (answered)
            {
                SinceAnswer = 0; Pulses++;
                Mode = XgResidentMode.Watch;
                if (lastDisagreed) Pondering = PonderSeconds;
            }
            if (Mode == XgResidentMode.Watch && SinceAnswer >= WanderAfter && Pondering <= 0) Mode = XgResidentMode.Roam;
            if (Mode == XgResidentMode.Hidden || Mode == XgResidentMode.Parked) Mode = XgResidentMode.Roam;
            return Mode;
        }
    }
}
