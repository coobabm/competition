using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One stack of a retired consumable in an old save; read once for the refund, then emptied.</summary>
    [Serializable]
    public sealed class XgStack
    {
        public string id = "";
        public int count;
        public double cooldown;
    }

    public sealed partial class XgState
    {
        /// <summary>Old saves only: consumable stock and waiting rollbacks, refunded once and then cleared (XgSim.RepairConsumablesRetired).</summary>
        public List<XgStack> consumables = new List<XgStack>();
        public int rollbackGuards;
        /// <summary>Price of the retired consumables still owed to the wallet; paid once on the next tick, then 0.</summary>
        public double consumableRefund;
    }

    /// <summary>
    /// The household bill for a training slice, when the host can scale it (ChapterOneSim.BillTraining). The host keeps the
    /// money and the card wear; the lab only says how much cheaper and how much safer this slice is. A host without it bills
    /// <see cref="IXgHost.Train"/> as before.
    /// </summary>
    public interface IXgTrainingBill
    {
        /// <summary>
        /// Bills the slice: electricity price × <paramref name="priceFactor"/> (relics), energy used × <paramref name="energyFactor"/>
        /// (cuDNN: fewer kWh, so the month's tier fills slower too), card burn-out chance × <paramref name="wearFactor"/>
        /// (九州风神 halves it).
        /// </summary>
        void TrainScaled(double seconds, double priceFactor, double energyFactor, double wearFactor);
    }

    /// <summary>The permanent training-bill items (cuDNN, the cooler) and the relics' share of the bill. The consumables are gone.</summary>
    public sealed partial class XgSim
    {
        /// <summary>cuDNN's share of the electricity (−30%) and 九州风神's share of the burn-out chance (−50%).</summary>
        public const double CuDnnEnergyFactor = .7, CoolerWearFactor = .5;

        /// <summary>Share of a round's electricity price that is billed (relics).</summary>
        public double TrainPriceFactor => RelicPowerFactor;
        /// <summary>Share of the energy a round uses (cuDNN: 70%).</summary>
        public double TrainEnergyFactor => Has("cudnn") ? CuDnnEnergyFactor : 1;
        /// <summary>Share of the card burn-out chance that is left (九州风神 half).</summary>
        public double TrainWearFactor => Has("cooler") ? CoolerWearFactor : 1;

        /// <summary>Bills a slice of training through the host (scaled when it can be).</summary>
        void ChargeTraining(IXgHost host, double seconds, bool round)
        {
            if (host == null || seconds <= 0) return;
            if (host is IXgTrainingBill bill) bill.TrainScaled(seconds, round ? TrainPriceFactor : 1, TrainEnergyFactor, TrainWearFactor);
            else host.Train(seconds);
        }

        // ───────────── the consumables are gone ─────────────

        /// <summary>What one unit of each retired consumable cost: price of a pack over the units in it.</summary>
        static readonly Dictionary<string, double> RetiredConsumableUnit = new Dictionary<string, double>
        {
            { "offpeak", 30.0 / 3 }, { "paste", 30.0 / 2 }, { "redbull", 18.0 / 6 }, { "ckpt", 40.0 / 2 }, { "sgdr", 80.0 }, { "clean", 50.0 },
        };

        /// <summary>
        /// An old save's unused consumables (and a rollback waiting for a drop) are turned into a refund at their purchase price,
        /// to be paid once; the stock is cleared.
        /// </summary>
        void RepairConsumablesRetired()
        {
            if (!Finite(S.consumableRefund) || S.consumableRefund < 0) S.consumableRefund = 0;
            double owed = 0;
            if (S.consumables != null)
                foreach (var stack in S.consumables)
                    if (stack != null && stack.count > 0 && RetiredConsumableUnit.TryGetValue(stack.id ?? "", out double unit)) owed += Math.Min(stack.count, 9999) * unit;
            if (S.rollbackGuards > 0) owed += Math.Min(S.rollbackGuards, 9999) * RetiredConsumableUnit["ckpt"];
            S.consumableRefund += Math.Round(owed);
            S.consumables = new List<XgStack>();
            S.rollbackGuards = 0;
        }

        /// <summary>Pays the pending consumable refund into the wallet and says so in the feed; runs once per save.</summary>
        void PayConsumableRefund(IXgHost host)
        {
            if (host == null || S.consumableRefund <= 0) return;
            double amount = S.consumableRefund;
            S.consumableRefund = 0;
            host.Earn(amount);
            Say(T("消耗品下线了，退回 ¥" + F(amount, "0"), "Consumables are gone, ¥" + F(amount, "0") + " refunded"));
        }
    }
}
