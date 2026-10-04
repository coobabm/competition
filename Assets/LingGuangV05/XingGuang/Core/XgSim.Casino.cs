using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgCasinoRound
    {
        public int number, stake, returned, choice;
        public string game;
        public int[] values;
    }

    [Serializable]
    public sealed class XgCasinoState
    {
        // Separate from the learning RNG: visiting the casino must not change training examples.
        public long rng;
        public int rounds;
        public double wagered, returned, nextBetAt;
        public List<XgCasinoRound> history = new List<XgCasinoRound>();
        public bool adVisible;
        public double adRemaining = 120;
        public XgBlackjackHand blackjack;
    }

    public sealed partial class XgState { public XgCasinoState casino; }

    /// <summary>Fictional local games, not a network service. Returns are gross (include the stake).</summary>
    public static class CasinoRules
    {
        public const double RoundSeconds = 1.5;
        public static bool IsStake(int stake) => stake == 10 || stake == 50 || stake == 100 || stake == 500;
        public static bool IsChoice(string game, int choice) => game == "slots" ? choice == 0
            : (game == "dice" || game == "roulette" || game == "dragon") && (choice == 0 || choice == 1);
        public static bool IsAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            string value = address.Trim();
            if (!value.Contains("://")) value = "https://" + value;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == "https" || uri.Scheme == "http") && uri.UserInfo.Length == 0
                && uri.IsDefaultPort && (uri.Host == "888.vip" || uri.Host == "www.888.vip")
                && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0;
        }

        public static bool Red(int n)
        {
            switch (n)
            {
                case 1: case 3: case 5: case 7: case 9: case 12: case 14: case 16: case 18:
                case 19: case 21: case 23: case 25: case 27: case 30: case 32: case 34: case 36: return true;
                default: return false;
            }
        }

        public static int ReturnMultiplier(string game, int choice, int[] v)
        {
            if (!IsChoice(game, choice) || v == null) return 0;
            int count = game == "roulette" ? 1 : game == "dragon" ? 2 : 3;
            int min = game == "slots" || game == "roulette" ? 0 : 1;
            int max = game == "slots" ? 5 : game == "roulette" ? 36 : game == "dragon" ? 13 : 6;
            if (v.Length != count) return 0;
            foreach (int n in v) if (n < min || n > max) return 0;
            switch (game)
            {
                case "slots": return v[0] == v[1] && v[1] == v[2] ? 20 : v[0] == v[1] || v[1] == v[2] || v[0] == v[2] ? 1 : 0;
                case "dice":
                    if (v[0] == v[1] && v[1] == v[2]) return 0;
                    return (v[0] + v[1] + v[2] <= 10 ? 0 : 1) == choice ? 2 : 0;
                case "roulette": return v[0] != 0 && (Red(v[0]) ? 0 : 1) == choice ? 2 : 0;
                case "dragon": return v[0] != v[1] && (v[0] > v[1] ? 0 : 1) == choice ? 2 : 0;
                default: return 0;
            }
        }
    }

    public sealed partial class XgSim
    {
        public XgCasinoState Casino
        {
            get
            {
                if (S.casino == null) S.casino = new XgCasinoState();
                if (S.casino.history == null) S.casino.history = new List<XgCasinoRound>();
                return S.casino;
            }
        }

        /// <summary>Single synchronous settlement; animations never award money. O(1) time/space, 8-entry history.</summary>
        public bool TryCasinoBet(string game, int choice, int stake, IXgHost host, out XgCasinoRound round)
        {
            round = null;
            if (BlackjackActive || !CasinoRules.IsStake(stake) || !CasinoRules.IsChoice(game, choice)
                || !CasinoFunds(host, stake) || Clock < Casino.nextBetAt) return false;
            if (!host.Spend(stake)) return false;
            var c = Casino;
            c.nextBetAt = Clock + CasinoRules.RoundSeconds;
            EnsureCasinoRng();
            int count = game == "roulette" ? 1 : game == "dragon" ? 2 : 3;
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = CasinoNext(game == "roulette" ? 37 : game == "dragon" ? 13 : 6) + (game == "dice" || game == "dragon" ? 1 : 0);
            int returned = stake * CasinoRules.ReturnMultiplier(game, choice, values);
            round = new XgCasinoRound { number = ++c.rounds, game = game, choice = choice, stake = stake, returned = returned, values = values };
            c.wagered += stake; c.returned += returned;
            if (c.history.Count >= 8) c.history.RemoveRange(0, c.history.Count - 7);
            c.history.Add(round);
            if (returned > 0) host.Earn(returned);
            return true;
        }

        int CasinoNext(int bound)
        {
            uint n = (uint)Casino.rng;
            n ^= n << 13; n ^= n >> 17; n ^= n << 5;
            Casino.rng = n;
            return (int)(n % (uint)bound);
        }

        bool ValidCasinoClock => !double.IsNaN(Clock) && !double.IsInfinity(Clock) && Clock >= 0;
        bool CasinoFunds(IXgHost host, int amount) => host != null && ValidCasinoClock && !double.IsNaN(host.Money)
            && !double.IsInfinity(host.Money) && host.Money >= amount;
        void EnsureCasinoRng()
        {
            if (Casino.rng != 0) return;
            Casino.rng = (uint)(S.rng ^ 0x6C8E9CF5L);
            if (Casino.rng == 0) Casino.rng = 0x6C8E9CF5L;
        }

        /// <summary>Only active eligible desktop time counts; offline catch-up never calls this.</summary>
        public void AdvanceCasinoAd(double seconds, bool held)
        {
            if (S.stage < 3 || S.chapterComplete || held || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            if (Casino.adVisible) return;
            Casino.adRemaining = Math.Max(0, Casino.adRemaining - seconds);
            if (Casino.adRemaining == 0) Casino.adVisible = true;
        }

        public void CloseCasinoAd()
        {
            if (!Casino.adVisible) return;
            Casino.adVisible = false;
            Casino.adRemaining = 300;
        }
    }
}
