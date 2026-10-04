using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Games
{
    /// <summary>
    /// 围棋 (9 × 9): placement, capture, no suicide, simple ko and pass. Two passes in a row end the game, which is
    /// then scored by area (stones plus empty regions bordered by one colour only) with 6.5 komi for White. The AI is a
    /// one-ply "shape reader": every legal point is scored for captures, rescues, self-atari, eyes and territory, and
    /// it passes when nothing is worth playing. Pure and deterministic for a given position.
    /// </summary>
    public sealed class GoGame
    {
        public const int Size = 9;
        public const int Empty = 0, Black = 1, White = 2;
        public const double Komi = 6.5;

        /// <summary>After this many turns the AI only passes, so a game between two AIs always ends.</summary>
        public const int MaxMoves = Size * Size * 3;

        /// <summary>The AI passes unless its best point scores above this.</summary>
        const double PassThreshold = 5;

        const int N = Size * Size;
        static readonly int[][] Adj = BuildAdjacency();

        int[] board = new int[N];
        int[] scratch = new int[N];
        readonly int[] captures = new int[3];
        int ko = -1;
        int passes;

        // Flood-fill scratch space, reused so a move does not allocate.
        readonly int[] mark = new int[N], libMark = new int[N], stack = new int[N], groupBuf = new int[N];
        int stamp;

        // Filled by TryPlay for the stone just placed.
        int playedLibs, playedSize;

        // Region analysis used by the AI: border colour mask (1 Black, 2 White, 3 both) and size of each empty region.
        readonly int[] regionOwner = new int[N], regionSize = new int[N];
        readonly int[] seen = new int[4];

        /// <summary>Whose turn it is: <see cref="Black"/> or <see cref="White"/>. Black moves first.</summary>
        public int ToMove { get; private set; } = Black;

        /// <summary>Turns played so far, passes included.</summary>
        public int Moves { get; private set; }

        /// <summary>0 while the game is running; <see cref="Black"/> or <see cref="White"/> once two passes ended it.</summary>
        public int Winner { get; private set; }

        public bool Ended { get; private set; }

        /// <summary>The last stone placed; both are -1 after a pass (and before the first move).</summary>
        public int LastX { get; private set; } = -1;
        public int LastY { get; private set; } = -1;

        public int ConsecutivePasses => passes;

        /// <summary>The point the side to move may not play because of ko, or -1 when there is none.</summary>
        public int KoX => ko < 0 ? -1 : ko % Size;
        public int KoY => ko < 0 ? -1 : ko / Size;

        public int this[int x, int y] => board[y * Size + x];

        /// <summary>How many enemy stones <paramref name="colour"/> has captured.</summary>
        public int Captures(int colour) => colour == Black || colour == White ? captures[colour] : 0;

        public static int Other(int colour) => colour == Black ? White : Black;

        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

        /// <summary>Liberties of the group that owns (x, y); 0 for an empty or off-board point.</summary>
        public int Liberties(int x, int y)
        {
            if (!In(x, y) || board[y * Size + x] == Empty) return 0;
            return GroupLibs(board, y * Size + x, out _);
        }

        /// <summary>True if the side to move may play at (x, y) right now.</summary>
        public bool IsLegal(int x, int y)
        {
            if (Ended || !In(x, y)) return false;
            return TryPlay(board, y * Size + x, ToMove, scratch, out _, out _);
        }

        /// <summary>Plays a stone for <see cref="ToMove"/>. False (and nothing changes) if the move is illegal.</summary>
        public bool Place(int x, int y)
        {
            if (Ended || !In(x, y)) return false;
            if (!TryPlay(board, y * Size + x, ToMove, scratch, out int captured, out int newKo)) return false;
            var t = board; board = scratch; scratch = t;
            captures[ToMove] += captured;
            ko = newKo;
            passes = 0;
            Moves++;
            LastX = x; LastY = y;
            ToMove = Other(ToMove);
            return true;
        }

        /// <summary>Passes the turn. A second pass in a row ends and scores the game. False once the game is over.</summary>
        public bool Pass()
        {
            if (Ended) return false;
            passes++;
            Moves++;
            LastX = LastY = -1;
            ko = -1;
            ToMove = Other(ToMove);
            if (passes >= 2)
            {
                Ended = true;
                Score(out double b, out double w);
                Winner = b > w ? Black : White;
            }
            return true;
        }

        /// <summary>Area score: stones on the board plus empty regions touched by one colour only. White gets komi.</summary>
        public void Score(out double black, out double white)
        {
            AnalyseRegions();
            black = 0; white = Komi;
            for (int p = 0; p < N; p++)
            {
                int c = board[p];
                if (c == Black) black++;
                else if (c == White) white++;
                else if (regionOwner[p] == Black) black++;
                else if (regionOwner[p] == White) white++;
            }
        }

        /// <summary>
        /// The AI's move for <see cref="ToMove"/>. Returns false when it passes: the game is over, no legal point scores
        /// above the pass threshold, only its own eyes are left, or the opponent just passed and it is already ahead.
        /// </summary>
        public bool Best(out int bx, out int by)
        {
            bx = by = -1;
            if (Ended || Moves >= MaxMoves) return false;
            int me = ToMove, opp = Other(me);
            AnalyseRegions();
            bool early = Moves < 10;
            double best = PassThreshold;
            int bestP = -1;

            for (int p = 0; p < N; p++)
            {
                if (board[p] != Empty || p == ko || IsOwnEye(p, me)) continue;

                // Liberties of the friendly groups next to p before the move, to see whether p rescues one.
                double rescue = 0;
                int nSeen = 0;
                foreach (int n in Adj[p])
                {
                    if (board[n] != me) continue;
                    int libs = GroupLibs(board, n, out int gs);
                    int id = MinStone(gs);
                    bool dup = false;
                    for (int i = 0; i < nSeen; i++) if (seen[i] == id) dup = true;
                    if (dup) continue;
                    seen[nSeen++] = id;
                    if (libs == 1) rescue += 50 + 15 * gs;
                }

                if (!TryPlay(board, p, me, scratch, out int captured, out _)) continue;
                int ownLibs = playedLibs, ownSize = playedSize;
                double s = 0;

                if (captured > 0) s += 60 + 25 * captured;
                if (rescue > 0 && ownLibs >= 2) s += rescue + (ownLibs >= 3 ? 10 : 0);

                // Pressure on the enemy groups next to the new stone (looked at on the board after the move).
                nSeen = 0;
                foreach (int n in Adj[p])
                {
                    if (scratch[n] != opp) continue;
                    int libs = GroupLibs(scratch, n, out int gs);
                    int id = MinStone(gs);
                    bool dup = false;
                    for (int i = 0; i < nSeen; i++) if (seen[i] == id) dup = true;
                    if (dup) continue;
                    seen[nSeen++] = id;
                    if (libs == 1 && ownLibs >= 2) s += 15 + 5 * gs;
                    else if (libs == 2) s += 2;
                }

                // Self-atari is only acceptable when it captures (a ko or snapback-style capture).
                if (ownLibs == 1 && captured == 0) s -= 80 + 20 * ownSize;
                else if (ownLibs == 2) s -= 4;

                int x = p % Size, y = p / Size;
                int owner = regionOwner[p], rs = regionSize[p];
                bool open = owner == 0 || owner == 3 || rs >= 30;
                if (open) s += 10 + Shape(x, y, early) + Near(x, y, me, opp);
                else if (owner == me) s -= 15;
                else s += rs >= 12 ? 0 : -15;

                s += ((x * 7 + y * 13) % 11) * 0.01;
                if (s > best) { best = s; bestP = p; }
            }

            if (bestP < 0) return false;

            // When the opponent has just passed and nothing urgent is on the board, end the game if it is already won.
            if (passes == 1 && best < 40)
            {
                Score(out double b, out double w);
                if (me == Black ? b > w : w > b) return false;
            }

            bx = bestP % Size; by = bestP / Size;
            return true;
        }

        static double Shape(int x, int y, bool early)
        {
            int line = Math.Min(Math.Min(x, y), Math.Min(Size - 1 - x, Size - 1 - y));
            if (early)
            {
                // The 3-3 and 4-4 points and the centre are the natural opening moves on 9 × 9.
                switch (line)
                {
                    case 0: return -8;
                    case 1: return -4;
                    case 2: return 6;
                    case 3: return 5;
                    default: return 4;
                }
            }
            return line == 0 ? -3 : line == 1 ? 0 : 1;
        }

        double Near(int x, int y, int me, int opp)
        {
            double s = 0;
            bool own = false, enemy = false;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    int d = Math.Abs(dx) + Math.Abs(dy);
                    if (d == 0 || d > 2 || !In(x + dx, y + dy)) continue;
                    int c = board[(y + dy) * Size + x + dx];
                    if (c == me) own = true;
                    else if (c == opp) enemy = true;
                }
            if (own) s += 2;
            if (enemy) s += 2;
            return s;
        }

        /// <summary>
        /// A beginner's move for <see cref="ToMove"/>: a random legal point next to a stone (any point on an empty
        /// board), never its own eye. The lab's AI mixes these in while it is still weak. False if none is left.
        /// </summary>
        public bool Casual(Random rng, out int bx, out int by)
        {
            bx = by = -1;
            if (Ended || rng == null) return false;
            int me = ToMove;
            var near = new System.Collections.Generic.List<int>();
            var any = new System.Collections.Generic.List<int>();
            for (int p = 0; p < N; p++)
            {
                if (board[p] != Empty || p == ko || IsOwnEye(p, me) || !TryPlay(board, p, me, scratch, out _, out _)) continue;
                any.Add(p);
                foreach (int n in Adj[p]) if (board[n] != Empty) { near.Add(p); break; }
            }
            var pool = near.Count > 0 ? near : any;
            if (pool.Count == 0) return false;
            int pick = pool[rng.Next(pool.Count)];
            bx = pick % Size; by = pick / Size;
            return true;
        }

        /// <summary>A point whose four neighbours are all our own stones, with too few enemy diagonals to make it false.</summary>
        bool IsOwnEye(int p, int me)
        {
            foreach (int n in Adj[p]) if (board[n] != me) return false;
            int x = p % Size, y = p / Size, enemy = 0, offBoard = 0;
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    if (!In(x + dx, y + dy)) offBoard++;
                    else if (board[(y + dy) * Size + x + dx] == Other(me)) enemy++;
                }
            return offBoard > 0 ? enemy == 0 : enemy < 2;
        }

        int MinStone(int size)
        {
            int m = int.MaxValue;
            for (int i = 0; i < size; i++) if (groupBuf[i] < m) m = groupBuf[i];
            return m;
        }

        /// <summary>
        /// Plays <paramref name="colour"/> at p on a copy of <paramref name="src"/> written to <paramref name="dst"/>.
        /// False for an occupied point, the ko point (for the side to move) or suicide.
        /// </summary>
        bool TryPlay(int[] src, int p, int colour, int[] dst, out int captured, out int newKo)
        {
            captured = 0; newKo = -1;
            if (p < 0 || p >= N || src[p] != Empty) return false;
            if (p == ko && colour == ToMove) return false;
            Array.Copy(src, dst, N);
            dst[p] = colour;
            int opp = Other(colour), lastCaptured = -1;
            foreach (int n in Adj[p])
            {
                if (dst[n] != opp) continue;
                if (GroupLibs(dst, n, out int size) != 0) continue;
                for (int i = 0; i < size; i++) dst[groupBuf[i]] = Empty;
                captured += size;
                lastCaptured = groupBuf[0];
            }
            playedLibs = GroupLibs(dst, p, out playedSize);
            if (playedLibs == 0) return false;
            if (captured == 1 && playedSize == 1 && playedLibs == 1) newKo = lastCaptured;
            return true;
        }

        /// <summary>Liberty count of the group at <paramref name="start"/>; its stones are left in groupBuf.</summary>
        int GroupLibs(int[] b, int start, out int size)
        {
            stamp++;
            int colour = b[start], sp = 0, libs = 0;
            size = 0;
            stack[sp++] = start;
            mark[start] = stamp;
            while (sp > 0)
            {
                int q = stack[--sp];
                groupBuf[size++] = q;
                foreach (int n in Adj[q])
                {
                    int c = b[n];
                    if (c == Empty)
                    {
                        if (libMark[n] != stamp) { libMark[n] = stamp; libs++; }
                    }
                    else if (c == colour && mark[n] != stamp)
                    {
                        mark[n] = stamp;
                        stack[sp++] = n;
                    }
                }
            }
            return libs;
        }

        /// <summary>Flood-fills every empty region, recording which colours border it and how big it is.</summary>
        void AnalyseRegions()
        {
            stamp++;
            for (int p = 0; p < N; p++)
            {
                if (board[p] != Empty || mark[p] == stamp) continue;
                int sp = 0, size = 0, owner = 0;
                stack[sp++] = p;
                mark[p] = stamp;
                while (sp > 0)
                {
                    int q = stack[--sp];
                    groupBuf[size++] = q;
                    foreach (int n in Adj[q])
                    {
                        int c = board[n];
                        if (c == Empty)
                        {
                            if (mark[n] != stamp) { mark[n] = stamp; stack[sp++] = n; }
                        }
                        else owner |= c;
                    }
                }
                for (int i = 0; i < size; i++) { regionOwner[groupBuf[i]] = owner; regionSize[groupBuf[i]] = size; }
            }
        }

        static int[][] BuildAdjacency()
        {
            var adj = new int[N][];
            var list = new List<int>(4);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    list.Clear();
                    if (x > 0) list.Add(y * Size + x - 1);
                    if (x < Size - 1) list.Add(y * Size + x + 1);
                    if (y > 0) list.Add((y - 1) * Size + x);
                    if (y < Size - 1) list.Add((y + 1) * Size + x);
                    adj[y * Size + x] = list.ToArray();
                }
            return adj;
        }
    }
}
