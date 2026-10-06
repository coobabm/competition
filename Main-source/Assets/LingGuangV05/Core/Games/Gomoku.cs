using System;

namespace LingGuangV05.Core.Games
{
    /// <summary>
    /// 五子棋 (design v1.1 §3, stage 3+): 15 × 15, five in a row wins. The AI "looks at the board with a
    /// convolution": every empty point is scored by the same window over its four lines (its own chances and the
    /// player's threats), and it plays the best one. Pure and deterministic for a given position.
    /// </summary>
    public sealed class Gomoku
    {
        public const int Size = 15;
        public const int Empty = 0, Black = 1, White = 2;
        readonly int[,] cells = new int[Size, Size];
        public int Moves { get; private set; }
        public int Winner { get; private set; }
        public int LastX { get; private set; } = -1;
        public int LastY { get; private set; } = -1;

        public int this[int x, int y] => cells[x, y];

        public bool Place(int x, int y, int stone)
        {
            if (Winner != Empty || x < 0 || y < 0 || x >= Size || y >= Size || cells[x, y] != Empty || stone != Black && stone != White) return false;
            cells[x, y] = stone; Moves++; LastX = x; LastY = y;
            if (Five(x, y, stone)) Winner = stone;
            return true;
        }

        public bool Full => Moves >= Size * Size;

        static readonly int[,] Dirs = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };

        bool Five(int x, int y, int stone)
        {
            for (int d = 0; d < 4; d++) if (Run(x, y, Dirs[d, 0], Dirs[d, 1], stone) >= 5) return true;
            return false;
        }

        int Count(int x, int y, int dx, int dy, int stone)
        {
            int n = 0;
            for (int i = 1; i < 5; i++)
            {
                int cx = x + dx * i, cy = y + dy * i;
                if (cx < 0 || cy < 0 || cx >= Size || cy >= Size || cells[cx, cy] != stone) break;
                n++;
            }
            return n;
        }

        int Run(int x, int y, int dx, int dy, int stone) => 1 + Count(x, y, dx, dy, stone) + Count(x, y, -dx, -dy, stone);

        bool Open(int x, int y, int dx, int dy, int stone)
        {
            int n = Count(x, y, dx, dy, stone), cx = x + dx * (n + 1), cy = y + dy * (n + 1);
            return cx >= 0 && cy >= 0 && cx < Size && cy < Size && cells[cx, cy] == Empty;
        }

        /// <summary>How good an empty point is for <paramref name="stone"/>: the same scoring window on all four lines.</summary>
        public double Score(int x, int y, int stone)
        {
            if (cells[x, y] != Empty) return double.MinValue;
            double score = 0;
            for (int d = 0; d < 4; d++)
            {
                int dx = Dirs[d, 0], dy = Dirs[d, 1];
                int run = Run(x, y, dx, dy, stone);
                int open = (Open(x, y, dx, dy, stone) ? 1 : 0) + (Open(x, y, -dx, -dy, stone) ? 1 : 0);
                if (run >= 5) score += 100000;
                else if (run == 4) score += open == 2 ? 10000 : open == 1 ? 1000 : 0;
                else if (run == 3) score += open == 2 ? 900 : open == 1 ? 100 : 0;
                else if (run == 2) score += open == 2 ? 60 : open == 1 ? 10 : 0;
                else score += open;
            }
            return score;
        }

        /// <summary>The AI's move: attack and defence weighed, centre preferred on ties. False if nothing is left.</summary>
        public bool Best(int stone, out int bx, out int by)
        {
            int other = stone == Black ? White : Black;
            double best = double.MinValue; bx = by = -1;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (cells[x, y] != Empty) continue;
                    double s = Score(x, y, stone) * 1.1 + Score(x, y, other) - (Math.Abs(x - 7) + Math.Abs(y - 7)) * .01;
                    if (s > best) { best = s; bx = x; by = y; }
                }
            return bx >= 0;
        }

        /// <summary>
        /// The AI's move at a given strength: with chance <paramref name="skill"/> it plays <see cref="Best"/>, otherwise
        /// it misreads the board and plays one of the next few candidates instead (still near the stones, just weaker).
        /// </summary>
        public bool Pick(int stone, double skill, Random rng, out int bx, out int by)
        {
            if (rng == null || skill >= 1 || rng.NextDouble() < skill) return Best(stone, out bx, out by);
            int other = stone == Black ? White : Black;
            var ranked = new System.Collections.Generic.List<(double s, int x, int y)>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (cells[x, y] == Empty)
                        ranked.Add((Score(x, y, stone) * 1.1 + Score(x, y, other) - (Math.Abs(x - 7) + Math.Abs(y - 7)) * .01, x, y));
            if (ranked.Count == 0) { bx = by = -1; return false; }
            ranked.Sort((a, b) => b.s.CompareTo(a.s));
            int i = ranked.Count == 1 ? 0 : 1 + rng.Next(Math.Min(5, ranked.Count - 1));
            bx = ranked[i].x; by = ranked[i].y;
            return true;
        }
    }
}
