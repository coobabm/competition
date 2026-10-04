using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Games
{
    /// <summary>
    /// One chess move. Squares are 0..63 with <c>square = rank * 8 + file</c> (a1 = 0, h1 = 7, a8 = 56).
    /// <see cref="Promotion"/> is a lowercase piece letter ('q', 'r', 'b', 'n') or '\0' when the move does not promote.
    /// </summary>
    public readonly struct ChessMove : IEquatable<ChessMove>
    {
        public const int FlagCapture = 1, FlagEnPassant = 2, FlagCastle = 4, FlagDoublePush = 8, FlagPromotion = 16;

        public readonly int From, To, Flags;
        public readonly char Promotion;

        public ChessMove(int from, int to, int flags = 0, char promotion = '\0')
        {
            From = from; To = to; Flags = flags; Promotion = promotion;
        }

        /// <summary>"No move": returned by the AI when the game is already over.</summary>
        public static readonly ChessMove None = new ChessMove(-1, -1);

        public bool IsValid => From >= 0 && To >= 0;
        public int FromFile => From & 7;
        public int FromRank => From >> 3;
        public int ToFile => To & 7;
        public int ToRank => To >> 3;
        public bool IsCapture => (Flags & FlagCapture) != 0;
        public bool IsEnPassant => (Flags & FlagEnPassant) != 0;
        public bool IsCastle => (Flags & FlagCastle) != 0;
        public bool IsPromotion => (Flags & FlagPromotion) != 0;

        public bool Equals(ChessMove o) => From == o.From && To == o.To && Promotion == o.Promotion;
        public override bool Equals(object obj) => obj is ChessMove m && Equals(m);
        public override int GetHashCode() => From * 64 * 128 + To * 128 + Promotion;

        /// <summary>Long algebraic (UCI) notation, e.g. "e2e4" or "e7e8q".</summary>
        public override string ToString()
        {
            if (!IsValid) return "0000";
            var s = new StringBuilder(5);
            s.Append((char)('a' + FromFile)).Append((char)('1' + FromRank)).Append((char)('a' + ToFile)).Append((char)('1' + ToRank));
            if (Promotion != '\0') s.Append(Promotion);
            return s.ToString();
        }
    }

    /// <summary>
    /// 国际象棋: standard chess with full legal move generation (castling, en passant, promotion), check, checkmate,
    /// stalemate, the fifty-move rule, threefold repetition and insufficient material. The board indexer returns FEN
    /// letters: uppercase for White ('P','N','B','R','Q','K'), lowercase for Black, '.' for an empty square.
    /// White is the human by default. The AI is a small alpha-beta search over material and piece-square tables with
    /// captures ordered first and a capture-only quiescence tail. Pure and deterministic for a given position.
    /// </summary>
    public sealed class ChessGame
    {
        public const int White = 1, Black = 2;
        public const int Ongoing = 0, WhiteWins = 1, BlackWins = 2, Draw = 3;
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        const int P = 1, Nt = 2, Bp = 3, Rk = 4, Qn = 5, Kg = 6;
        const int Inf = 1000000, Mate = 100000;
        const int QuiesceLimit = 6;

        // Board: positive codes are White, negative Black, magnitude is the piece type (1 pawn .. 6 king).
        readonly int[] sq = new int[64];
        readonly int[] kingSq = new int[2];
        int side;          // 0 = White to move, 1 = Black to move
        int castling;      // 1 = White O-O, 2 = White O-O-O, 4 = Black O-O, 8 = Black O-O-O
        int ep = -1;       // en passant target square, or -1
        int halfmove;
        int fullmove = 1;

        struct Undo
        {
            public ChessMove Move;
            public int Captured, Castling, Ep, Halfmove;
        }

        readonly List<Undo> undo = new List<Undo>(256);
        readonly List<ulong> history = new List<ulong>(256);

        /// <summary><see cref="White"/> or <see cref="Black"/>.</summary>
        public int SideToMove => side == 0 ? White : Black;

        /// <summary>0 ongoing, 1 White wins, 2 Black wins, 3 draw.</summary>
        public int Result { get; private set; }

        /// <summary>Why the game ended, in Chinese ("将死", "逼和", ...); empty while it is running.</summary>
        public string ResultReason { get; private set; } = "";

        /// <summary>Why the game ended, in English ("Checkmate", "Stalemate", ...); empty while it is running.</summary>
        public string ResultReasonEnglish { get; private set; } = "";

        /// <summary>The last move played through <see cref="Move(ChessMove)"/>, or <see cref="ChessMove.None"/>.</summary>
        public ChessMove LastMove { get; private set; } = ChessMove.None;

        public int HalfmoveClock => halfmove;
        public int FullmoveNumber => fullmove;

        /// <summary>Plies played through <see cref="Move(ChessMove)"/> since the game (or FEN) was set up.</summary>
        public int Plies { get; private set; }

        /// <summary>True when the side to move is in check.</summary>
        public bool InCheck => Attacked(kingSq[side], side ^ 1);

        /// <summary>FEN letter of the piece on (file, rank), both 0..7 with (0, 0) = a1; '.' for an empty square.</summary>
        public char this[int file, int rank] => PieceChar(sq[rank * 8 + file]);

        public ChessGame() : this(StartFen) { }

        public ChessGame(string fen) { LoadFen(fen); }

        public static ChessGame FromFen(string fen) => new ChessGame(fen);

        // ---------------------------------------------------------------- public play

        /// <summary>All legal moves for the side to move (empty once the game is decided).</summary>
        public List<ChessMove> LegalMoves()
        {
            var pseudo = new List<ChessMove>(48);
            Generate(pseudo, false);
            var legal = new List<ChessMove>(pseudo.Count);
            foreach (var m in pseudo)
            {
                Make(m);
                if (!Attacked(kingSq[side ^ 1], side)) legal.Add(m);
                Unmake();
            }
            return legal;
        }

        /// <summary>
        /// Plays a move by coordinates (files and ranks 0..7). <paramref name="promotion"/> picks the piece a pawn becomes
        /// ('q', 'r', 'b' or 'n'; anything else means queen). False if the move is illegal or the game is over.
        /// </summary>
        public bool Move(int fromFile, int fromRank, int toFile, int toRank, char promotion = 'q')
        {
            if (Result != Ongoing) return false;
            if (!OnBoard(fromFile, fromRank) || !OnBoard(toFile, toRank)) return false;
            int from = fromRank * 8 + fromFile, to = toRank * 8 + toFile;
            char promo = char.ToLowerInvariant(promotion);
            if (promo != 'q' && promo != 'r' && promo != 'b' && promo != 'n') promo = 'q';
            foreach (var m in LegalMoves())
                if (m.From == from && m.To == to && (!m.IsPromotion || m.Promotion == promo))
                    return Commit(m);
            return false;
        }

        /// <summary>Plays a move produced by <see cref="LegalMoves"/> or <see cref="Best"/>. False if it is not legal here.</summary>
        public bool Move(ChessMove move)
        {
            if (Result != Ongoing || !move.IsValid) return false;
            foreach (var m in LegalMoves())
                if (m.Equals(move)) return Commit(m);
            return false;
        }

        bool Commit(ChessMove m)
        {
            Make(m);
            undo.Clear();
            if (halfmove == 0) history.Clear();
            history.Add(Hash());
            LastMove = m;
            Plies++;
            UpdateResult();
            return true;
        }

        /// <summary>Counts leaf nodes of the legal move tree to <paramref name="depth"/> plies (move generator check).</summary>
        public long Perft(int depth)
        {
            if (depth <= 0) return 1;
            var moves = new List<ChessMove>(48);
            Generate(moves, false);
            long n = 0;
            foreach (var m in moves)
            {
                Make(m);
                if (!Attacked(kingSq[side ^ 1], side)) n += depth == 1 ? 1 : Perft(depth - 1);
                Unmake();
            }
            return n;
        }

        // ---------------------------------------------------------------- AI

        /// <summary>
        /// The AI's move for the side to move: alpha-beta to <paramref name="depth"/> plies plus a capture-only tail,
        /// scored by material and piece-square tables. The first of equally good moves wins, so it is deterministic.
        /// Returns <see cref="ChessMove.None"/> when the game is over.
        /// </summary>
        public ChessMove Best(int depth = 2)
        {
            if (Result != Ongoing) return ChessMove.None;
            if (depth < 1) depth = 1;
            var moves = LegalMoves();
            if (moves.Count == 0) return ChessMove.None;
            Order(moves);
            var best = moves[0];
            int alpha = -Inf;
            foreach (var m in moves)
            {
                Make(m);
                int score = -Search(depth - 1, -Inf, -alpha, 1);
                Unmake();
                if (score > alpha) { alpha = score; best = m; }
            }
            return best;
        }

        int Search(int depth, int alpha, int beta, int ply)
        {
            if (halfmove >= 100) return 0;
            if (depth <= 0) return Quiesce(alpha, beta, 0);
            var moves = new List<ChessMove>(48);
            Generate(moves, false);
            Order(moves);
            int legal = 0;
            foreach (var m in moves)
            {
                Make(m);
                if (Attacked(kingSq[side ^ 1], side)) { Unmake(); continue; }
                legal++;
                int s = -Search(depth - 1, -beta, -alpha, ply + 1);
                Unmake();
                if (s >= beta) return beta;
                if (s > alpha) alpha = s;
            }
            if (legal == 0) return Attacked(kingSq[side], side ^ 1) ? -Mate + ply : 0;
            return alpha;
        }

        int Quiesce(int alpha, int beta, int qdepth)
        {
            int stand = Evaluate();
            if (stand >= beta) return beta;
            if (stand > alpha) alpha = stand;
            if (qdepth >= QuiesceLimit) return alpha;
            var moves = new List<ChessMove>(16);
            Generate(moves, true);
            Order(moves);
            foreach (var m in moves)
            {
                Make(m);
                if (Attacked(kingSq[side ^ 1], side)) { Unmake(); continue; }
                int s = -Quiesce(-beta, -alpha, qdepth + 1);
                Unmake();
                if (s >= beta) return beta;
                if (s > alpha) alpha = s;
            }
            return alpha;
        }

        static readonly int[] Value = { 0, 100, 320, 330, 500, 900, 0 };

        /// <summary>Most valuable victim first, then least valuable attacker; promotions next; quiet moves keep their order.</summary>
        void Order(List<ChessMove> moves)
        {
            int n = moves.Count;
            var keys = new int[n];
            for (int i = 0; i < n; i++)
            {
                var m = moves[i];
                int k = 0;
                if (m.IsCapture)
                {
                    int victim = m.IsEnPassant ? P : Math.Abs(sq[m.To]);
                    k = 100000 + Value[victim] * 10 - Value[Math.Abs(sq[m.From])] / 10;
                }
                if (m.IsPromotion) k += m.Promotion == 'q' ? 90000 : 1000;
                keys[i] = k;
            }
            // Stable insertion sort, descending by key.
            for (int i = 1; i < n; i++)
            {
                var m = moves[i];
                int k = keys[i], j = i - 1;
                while (j >= 0 && keys[j] < k) { keys[j + 1] = keys[j]; moves[j + 1] = moves[j]; j--; }
                keys[j + 1] = k; moves[j + 1] = m;
            }
        }

        /// <summary>Static score from the side to move's point of view, in centipawns.</summary>
        int Evaluate()
        {
            int heavy = 0;
            for (int s = 0; s < 64; s++)
            {
                int t = Math.Abs(sq[s]);
                if (t >= Nt && t <= Qn) heavy += Value[t];
            }
            bool endgame = heavy <= 1600;
            int score = 0;
            for (int s = 0; s < 64; s++)
            {
                int p = sq[s];
                if (p == 0) continue;
                int t = Math.Abs(p), file = s & 7, rank = s >> 3;
                int idx = p > 0 ? (7 - rank) * 8 + file : rank * 8 + file;
                int v = Value[t] + Pst(t, idx, endgame);
                score += p > 0 ? v : -v;
            }
            return side == 0 ? score : -score;
        }

        static int Pst(int type, int idx, bool endgame)
        {
            switch (type)
            {
                case P: return PawnTable[idx];
                case Nt: return KnightTable[idx];
                case Bp: return BishopTable[idx];
                case Rk: return RookTable[idx];
                case Qn: return QueenTable[idx];
                default: return endgame ? KingEndTable[idx] : KingMidTable[idx];
            }
        }

        // Piece-square tables from White's point of view, laid out as seen from White: first row is rank 8.
        static readonly int[] PawnTable =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0,
        };
        static readonly int[] KnightTable =
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
        };
        static readonly int[] BishopTable =
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
        };
        static readonly int[] RookTable =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0,
        };
        static readonly int[] QueenTable =
        {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20,
        };
        static readonly int[] KingMidTable =
        {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20,
        };
        static readonly int[] KingEndTable =
        {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50,
        };

        // ---------------------------------------------------------------- result

        void UpdateResult()
        {
            Result = Ongoing; ResultReason = ResultReasonEnglish = "";
            if (LegalMoves().Count == 0)
            {
                if (InCheck) End(side == 0 ? BlackWins : WhiteWins, "将死", "Checkmate");
                else End(Draw, "逼和", "Stalemate");
            }
            else if (halfmove >= 100) End(Draw, "五十回合规则", "Fifty-move rule");
            else if (InsufficientMaterial()) End(Draw, "子力不足", "Insufficient material");
            else if (Repetitions() >= 3) End(Draw, "三次重复", "Threefold repetition");
        }

        void End(int result, string zh, string en) { Result = result; ResultReason = zh; ResultReasonEnglish = en; }

        int Repetitions()
        {
            if (history.Count == 0) return 0;
            ulong h = history[history.Count - 1];
            int n = 0;
            foreach (var x in history) if (x == h) n++;
            return n;
        }

        /// <summary>K v K, K + minor v K, and K + B v K + B with both bishops on the same colour.</summary>
        bool InsufficientMaterial()
        {
            int minors = 0, bishopsW = 0, bishopsB = 0, bishopColourW = -1, bishopColourB = -1;
            for (int s = 0; s < 64; s++)
            {
                int p = sq[s], t = Math.Abs(p);
                if (t == 0 || t == Kg) continue;
                if (t == P || t == Rk || t == Qn) return false;
                minors++;
                if (t == Bp)
                {
                    int colour = ((s & 7) + (s >> 3)) & 1;
                    if (p > 0) { bishopsW++; bishopColourW = colour; } else { bishopsB++; bishopColourB = colour; }
                }
            }
            if (minors <= 1) return true;
            return minors == 2 && bishopsW == 1 && bishopsB == 1 && bishopColourW == bishopColourB;
        }

        // ---------------------------------------------------------------- make / unmake

        static int Sign(int side) => side == 0 ? 1 : -1;

        static int PromoType(char c) => c == 'n' ? Nt : c == 'b' ? Bp : c == 'r' ? Rk : Qn;

        // Castling rights kept after a move touches a square (rook or king squares clear their rights).
        static readonly int[] CastleMask = BuildCastleMask();

        static int[] BuildCastleMask()
        {
            var m = new int[64];
            for (int i = 0; i < 64; i++) m[i] = 15;
            m[0] = 15 & ~2; m[4] = 15 & ~3; m[7] = 15 & ~1;
            m[56] = 15 & ~8; m[60] = 15 & ~12; m[63] = 15 & ~4;
            return m;
        }

        void Make(ChessMove m)
        {
            var u = new Undo { Move = m, Castling = castling, Ep = ep, Halfmove = halfmove };
            int piece = sq[m.From];
            int captured = sq[m.To];
            if (m.IsEnPassant)
            {
                int capSq = m.To + (side == 0 ? -8 : 8);
                captured = sq[capSq];
                sq[capSq] = 0;
            }
            u.Captured = captured;
            sq[m.To] = m.IsPromotion ? Sign(side) * PromoType(m.Promotion) : piece;
            sq[m.From] = 0;
            if (m.IsCastle)
            {
                if (m.To == 6) { sq[5] = sq[7]; sq[7] = 0; }
                else if (m.To == 2) { sq[3] = sq[0]; sq[0] = 0; }
                else if (m.To == 62) { sq[61] = sq[63]; sq[63] = 0; }
                else if (m.To == 58) { sq[59] = sq[56]; sq[56] = 0; }
            }
            if (Math.Abs(piece) == Kg) kingSq[side] = m.To;
            castling &= CastleMask[m.From] & CastleMask[m.To];
            ep = (m.Flags & ChessMove.FlagDoublePush) != 0 ? (m.From + m.To) / 2 : -1;
            halfmove = Math.Abs(piece) == P || captured != 0 ? 0 : halfmove + 1;
            if (side == 1) fullmove++;
            side ^= 1;
            undo.Add(u);
        }

        void Unmake()
        {
            var u = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            var m = u.Move;
            side ^= 1;
            if (side == 1) fullmove--;
            castling = u.Castling; ep = u.Ep; halfmove = u.Halfmove;
            int piece = m.IsPromotion ? Sign(side) * P : sq[m.To];
            sq[m.From] = piece;
            if (m.IsEnPassant)
            {
                sq[m.To] = 0;
                sq[m.To + (side == 0 ? -8 : 8)] = u.Captured;
            }
            else sq[m.To] = u.Captured;
            if (m.IsCastle)
            {
                if (m.To == 6) { sq[7] = sq[5]; sq[5] = 0; }
                else if (m.To == 2) { sq[0] = sq[3]; sq[3] = 0; }
                else if (m.To == 62) { sq[63] = sq[61]; sq[61] = 0; }
                else if (m.To == 58) { sq[56] = sq[59]; sq[59] = 0; }
            }
            if (Math.Abs(piece) == Kg) kingSq[side] = m.From;
        }

        // ---------------------------------------------------------------- move generation

        static readonly int[][] KnightTargets = BuildJumps(new[] { 1, 2, 2, 1, -1, -2, -2, -1 }, new[] { 2, 1, -1, -2, -2, -1, 1, 2 });
        static readonly int[][] KingTargets = BuildJumps(new[] { 1, 1, 0, -1, -1, -1, 0, 1 }, new[] { 0, 1, 1, 1, 0, -1, -1, -1 });

        // Rays[square][direction]: directions 0..3 are orthogonal, 4..7 diagonal.
        static readonly int[] RayDf = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] RayDr = { 0, 0, 1, -1, 1, -1, 1, -1 };
        static readonly int[][][] Rays = BuildRays();

        static bool OnBoard(int f, int r) => f >= 0 && f < 8 && r >= 0 && r < 8;

        static int[][] BuildJumps(int[] df, int[] dr)
        {
            var t = new int[64][];
            var list = new List<int>(8);
            for (int s = 0; s < 64; s++)
            {
                list.Clear();
                for (int i = 0; i < df.Length; i++)
                {
                    int f = (s & 7) + df[i], r = (s >> 3) + dr[i];
                    if (OnBoard(f, r)) list.Add(r * 8 + f);
                }
                t[s] = list.ToArray();
            }
            return t;
        }

        static int[][][] BuildRays()
        {
            var rays = new int[64][][];
            var list = new List<int>(8);
            for (int s = 0; s < 64; s++)
            {
                rays[s] = new int[8][];
                for (int d = 0; d < 8; d++)
                {
                    list.Clear();
                    int f = (s & 7) + RayDf[d], r = (s >> 3) + RayDr[d];
                    while (OnBoard(f, r)) { list.Add(r * 8 + f); f += RayDf[d]; r += RayDr[d]; }
                    rays[s][d] = list.ToArray();
                }
            }
            return rays;
        }

        /// <summary>True if square s is attacked by <paramref name="by"/> (0 White, 1 Black).</summary>
        bool Attacked(int s, int by)
        {
            int sign = Sign(by), f = s & 7, r = s >> 3;
            // Pawns: a White pawn attacks diagonally upward, so it sits one rank below the target.
            int pr = by == 0 ? r - 1 : r + 1;
            if (pr >= 0 && pr < 8)
            {
                if (f > 0 && sq[pr * 8 + f - 1] == sign * P) return true;
                if (f < 7 && sq[pr * 8 + f + 1] == sign * P) return true;
            }
            foreach (int t in KnightTargets[s]) if (sq[t] == sign * Nt) return true;
            foreach (int t in KingTargets[s]) if (sq[t] == sign * Kg) return true;
            var rays = Rays[s];
            for (int d = 0; d < 8; d++)
            {
                int slider = d < 4 ? Rk : Bp;
                foreach (int t in rays[d])
                {
                    int p = sq[t];
                    if (p == 0) continue;
                    if (p == sign * slider || p == sign * Qn) return true;
                    break;
                }
            }
            return false;
        }

        static readonly char[] Promotions = { 'q', 'r', 'b', 'n' };

        void AddPawnMove(List<ChessMove> list, int from, int to, int flags, bool capturesOnly)
        {
            int rank = to >> 3;
            if (rank == 7 || rank == 0)
            {
                foreach (char c in Promotions)
                {
                    if (capturesOnly && c != 'q' && (flags & ChessMove.FlagCapture) == 0) continue;
                    list.Add(new ChessMove(from, to, flags | ChessMove.FlagPromotion, c));
                }
            }
            else if (!capturesOnly || (flags & ChessMove.FlagCapture) != 0) list.Add(new ChessMove(from, to, flags));
        }

        /// <summary>Pseudo-legal moves (own king may be left in check). With capturesOnly: captures and promotions only.</summary>
        void Generate(List<ChessMove> list, bool capturesOnly)
        {
            int sign = Sign(side);
            for (int s = 0; s < 64; s++)
            {
                int p = sq[s];
                if (p == 0 || (p > 0) != (side == 0)) continue;
                int t = Math.Abs(p), f = s & 7, r = s >> 3;
                switch (t)
                {
                    case P:
                    {
                        int dir = side == 0 ? 8 : -8, startRank = side == 0 ? 1 : 6;
                        int fwd = s + dir;
                        if (fwd >= 0 && fwd < 64 && sq[fwd] == 0)
                        {
                            bool promo = (fwd >> 3) == 7 || (fwd >> 3) == 0;
                            if (promo || !capturesOnly) AddPawnMove(list, s, fwd, 0, capturesOnly);
                            if (!capturesOnly && r == startRank && sq[fwd + dir] == 0)
                                list.Add(new ChessMove(s, fwd + dir, ChessMove.FlagDoublePush));
                        }
                        for (int df = -1; df <= 1; df += 2)
                        {
                            int cf = f + df, cr = (s + dir) >> 3;
                            if (cf < 0 || cf > 7 || cr < 0 || cr > 7) continue;
                            int target = cr * 8 + cf;
                            if (sq[target] * sign < 0) AddPawnMove(list, s, target, ChessMove.FlagCapture, capturesOnly);
                            else if (target == ep) list.Add(new ChessMove(s, target, ChessMove.FlagCapture | ChessMove.FlagEnPassant));
                        }
                        break;
                    }
                    case Nt:
                    case Kg:
                        foreach (int to in t == Nt ? KnightTargets[s] : KingTargets[s])
                        {
                            int q = sq[to];
                            if (q == 0) { if (!capturesOnly) list.Add(new ChessMove(s, to)); }
                            else if (q * sign < 0) list.Add(new ChessMove(s, to, ChessMove.FlagCapture));
                        }
                        break;
                    default:
                    {
                        int d0 = t == Bp ? 4 : 0, d1 = t == Rk ? 4 : 8;
                        for (int d = d0; d < d1; d++)
                            foreach (int to in Rays[s][d])
                            {
                                int q = sq[to];
                                if (q == 0) { if (!capturesOnly) list.Add(new ChessMove(s, to)); continue; }
                                if (q * sign < 0) list.Add(new ChessMove(s, to, ChessMove.FlagCapture));
                                break;
                            }
                        break;
                    }
                }
            }
            if (!capturesOnly) GenerateCastles(list);
        }

        void GenerateCastles(List<ChessMove> list)
        {
            int them = side ^ 1;
            if (side == 0)
            {
                if (sq[4] != Kg) return;
                if ((castling & 1) != 0 && sq[7] == Rk && sq[5] == 0 && sq[6] == 0
                    && !Attacked(4, them) && !Attacked(5, them) && !Attacked(6, them))
                    list.Add(new ChessMove(4, 6, ChessMove.FlagCastle));
                if ((castling & 2) != 0 && sq[0] == Rk && sq[1] == 0 && sq[2] == 0 && sq[3] == 0
                    && !Attacked(4, them) && !Attacked(3, them) && !Attacked(2, them))
                    list.Add(new ChessMove(4, 2, ChessMove.FlagCastle));
            }
            else
            {
                if (sq[60] != -Kg) return;
                if ((castling & 4) != 0 && sq[63] == -Rk && sq[61] == 0 && sq[62] == 0
                    && !Attacked(60, them) && !Attacked(61, them) && !Attacked(62, them))
                    list.Add(new ChessMove(60, 62, ChessMove.FlagCastle));
                if ((castling & 8) != 0 && sq[56] == -Rk && sq[57] == 0 && sq[58] == 0 && sq[59] == 0
                    && !Attacked(60, them) && !Attacked(59, them) && !Attacked(58, them))
                    list.Add(new ChessMove(60, 58, ChessMove.FlagCastle));
            }
        }

        // ---------------------------------------------------------------- FEN and hashing

        static char PieceChar(int p)
        {
            if (p == 0) return '.';
            char c = " PNBRQK"[Math.Abs(p)];
            return p > 0 ? c : char.ToLowerInvariant(c);
        }

        static int PieceCode(char c)
        {
            int t = "PNBRQK".IndexOf(char.ToUpperInvariant(c)) + 1;
            if (t == 0) throw new FormatException("Bad FEN piece '" + c + "'");
            return char.IsUpper(c) ? t : -t;
        }

        void LoadFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) throw new ArgumentException("Empty FEN", nameof(fen));
            var parts = fen.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var rows = parts[0].Split('/');
            if (rows.Length != 8) throw new FormatException("FEN needs 8 ranks: " + fen);
            Array.Clear(sq, 0, 64);
            kingSq[0] = kingSq[1] = -1;
            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i, file = 0;
                foreach (char c in rows[i])
                {
                    if (char.IsDigit(c)) { file += c - '0'; continue; }
                    if (file > 7) throw new FormatException("FEN rank too long: " + rows[i]);
                    int p = PieceCode(c);
                    sq[rank * 8 + file] = p;
                    if (p == Kg) kingSq[0] = rank * 8 + file;
                    if (p == -Kg) kingSq[1] = rank * 8 + file;
                    file++;
                }
                if (file != 8) throw new FormatException("FEN rank has wrong length: " + rows[i]);
            }
            if (kingSq[0] < 0 || kingSq[1] < 0) throw new FormatException("FEN needs both kings: " + fen);
            side = parts.Length > 1 && parts[1] == "b" ? 1 : 0;
            castling = 0;
            if (parts.Length > 2)
                foreach (char c in parts[2])
                    castling |= c == 'K' ? 1 : c == 'Q' ? 2 : c == 'k' ? 4 : c == 'q' ? 8 : 0;
            ep = -1;
            if (parts.Length > 3 && parts[3] != "-" && parts[3].Length == 2)
                ep = (parts[3][1] - '1') * 8 + (parts[3][0] - 'a');
            halfmove = parts.Length > 4 && int.TryParse(parts[4], out int hm) ? hm : 0;
            fullmove = parts.Length > 5 && int.TryParse(parts[5], out int fm) ? Math.Max(1, fm) : 1;
            undo.Clear();
            history.Clear();
            history.Add(Hash());
            LastMove = ChessMove.None;
            Plies = 0;
            UpdateResult();
        }

        /// <summary>The position as a FEN string.</summary>
        public string ToFen()
        {
            var s = new StringBuilder(90);
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    int p = sq[rank * 8 + file];
                    if (p == 0) { empty++; continue; }
                    if (empty > 0) { s.Append(empty); empty = 0; }
                    s.Append(PieceChar(p));
                }
                if (empty > 0) s.Append(empty);
                if (rank > 0) s.Append('/');
            }
            s.Append(side == 0 ? " w " : " b ");
            if (castling == 0) s.Append('-');
            else
            {
                if ((castling & 1) != 0) s.Append('K');
                if ((castling & 2) != 0) s.Append('Q');
                if ((castling & 4) != 0) s.Append('k');
                if ((castling & 8) != 0) s.Append('q');
            }
            s.Append(' ');
            if (ep < 0) s.Append('-');
            else s.Append((char)('a' + (ep & 7))).Append((char)('1' + (ep >> 3)));
            s.Append(' ').Append(halfmove).Append(' ').Append(fullmove);
            return s.ToString();
        }

        // Zobrist keys from a fixed seed, so hashes (and repetition draws) are the same on every run.
        static readonly ulong[] Zobrist = BuildZobrist();

        static ulong[] BuildZobrist()
        {
            var z = new ulong[12 * 64 + 1 + 16 + 8];
            ulong x = 0x9E3779B97F4A7C15UL;
            for (int i = 0; i < z.Length; i++)
            {
                // SplitMix64
                x += 0x9E3779B97F4A7C15UL;
                ulong v = x;
                v = (v ^ (v >> 30)) * 0xBF58476D1CE4E5B9UL;
                v = (v ^ (v >> 27)) * 0x94D049BB133111EBUL;
                z[i] = v ^ (v >> 31);
            }
            return z;
        }

        ulong Hash()
        {
            ulong h = 0;
            for (int s = 0; s < 64; s++)
            {
                int p = sq[s];
                if (p == 0) continue;
                int idx = (p > 0 ? p - 1 : 5 - p) * 64 + s;
                h ^= Zobrist[idx];
            }
            if (side == 1) h ^= Zobrist[12 * 64];
            h ^= Zobrist[12 * 64 + 1 + castling];
            if (ep >= 0) h ^= Zobrist[12 * 64 + 17 + (ep & 7)];
            return h;
        }
    }
}
