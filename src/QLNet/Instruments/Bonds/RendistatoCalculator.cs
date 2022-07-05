using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Patterns;
using QLNet.Pricingengines.Bond;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class RendistatoCalculator : LazyObject
    {
        private RendistatoBasket basket_;
        private Handle<YieldTermStructure> discountCurve_;
        private double duration_;
        private List<double> durations_;
        private int equivalentSwapIndex_;
        private Euribor euriborIndex_;
        private int nSwaps_;
        private List<double?> swapBondDurations_;
        private List<double?> swapBondYields_, swapRates_;
        private List<double> swapLenghts_;
        private List<VanillaSwap> swaps_;
        private List<double> yields_;

        public RendistatoCalculator(RendistatoBasket basket, Euribor euriborIndex, Handle<YieldTermStructure> discountCurve)
        {
            basket_ = basket;
            euriborIndex_ = euriborIndex;
            discountCurve_ = discountCurve;
            yields_ = new InitializedList<double>(basket_.size(), 0.05);
            durations_ = new List<double>(basket_.size());
            nSwaps_ = 15; // TODO: generalize number of swaps and their lenghts
            swaps_ = new List<VanillaSwap>(nSwaps_);
            swapLenghts_ = new List<double>(nSwaps_);
            swapBondDurations_ = new InitializedList<double?>(nSwaps_, null);
            swapBondYields_ = new InitializedList<double?>(nSwaps_, 0.05);
            swapRates_ = new InitializedList<double?>(nSwaps_, null);

            basket_.registerWith(update);
            euriborIndex_.registerWith(update);
            discountCurve_.registerWith(update);

            var dummyRate = 0.05;
            for (var i = 0; i < nSwaps_; ++i)
            {
                swapLenghts_[i] = i + 1;
                swaps_[i] = new MakeVanillaSwap(new Period((int)swapLenghts_[i], TimeUnit.Years),
                        euriborIndex_, dummyRate, new Period(1, TimeUnit.Days))
                    .withDiscountingTermStructure(discountCurve_);
            }
        }

        #region LazyObject interface

        protected override void performCalculations()
        {
            var btps = basket_.btps();
            var quotes = basket_.cleanPriceQuotes();
            var bondSettlementDate = btps[0].settlementDate();
            for (var i = 0; i < basket_.size(); ++i)
            {
                yields_[i] = BondFunctions.yield(btps[i], quotes[i].link.value(),
                    new ActualActual(ActualActual.Convention.ISMA),
                    Compounding.Compounded, Frequency.Annual,
                    bondSettlementDate,
                    // accuracy, maxIterations, guess
                    1.0e-10, 100, yields_[i]);

                durations_[i] = BondFunctions.duration(btps[i], yields_[i], new ActualActual(ActualActual.Convention.ISMA),
                    Compounding.Compounded, Frequency.Annual, Duration.Type.Modified,
                    bondSettlementDate);
            }

            duration_ = 0;
            basket_.weights().ForEach((ii, vv) => duration_ += vv * yields()[ii]);

            var settlDays = 2;
            var fixedDayCount = swaps_[0].fixedDayCount();
            equivalentSwapIndex_ = nSwaps_ - 1;
            swapRates_[0] = swaps_[0].fairRate();
            var swapBond = new FixedRateBond(settlDays,
                100.0, // faceAmount
                swaps_[0].fixedSchedule(),
                new List<double> { swapRates_[0].Value },
                fixedDayCount); // redemption
            swapBondYields_[0] = BondFunctions.yield(swapBond,
                100.0, // floating leg NPV including end payment
                new ActualActual(ActualActual.Convention.ISMA),
                Compounding.Compounded, Frequency.Annual,
                bondSettlementDate,
                // accuracy, maxIterations, guess
                1.0e-10, 100, swapBondYields_[0].Value);

            swapBondDurations_[0] = BondFunctions.duration(swapBond, swapBondYields_[0].Value,
                new ActualActual(ActualActual.Convention.ISMA),
                Compounding.Compounded, Frequency.Annual,
                Duration.Type.Modified, bondSettlementDate);
            for (var i = 1; i < nSwaps_; ++i)
            {
                swapRates_[i] = swaps_[i].fairRate();
                var swapBond2 = new FixedRateBond(settlDays,
                    100.0, // faceAmount
                    swaps_[i].fixedSchedule(),
                    new List<double> { swapRates_[i].Value },
                    fixedDayCount); // redemption

                swapBondYields_[i] = BondFunctions.yield(swapBond2, 100.0, // floating leg NPV including end payment
                    new ActualActual(ActualActual.Convention.ISMA),
                    Compounding.Compounded, Frequency.Annual,
                    bondSettlementDate,
                    // accuracy, maxIterations, guess
                    1.0e-10, 100, swapBondYields_[i].Value);

                swapBondDurations_[i] = BondFunctions.duration(swapBond2, swapBondYields_[i].Value,
                    new ActualActual(ActualActual.Convention.ISMA),
                    Compounding.Compounded, Frequency.Annual,
                    Duration.Type.Modified, bondSettlementDate);
                if (swapBondDurations_[i] > duration_)
                {
                    equivalentSwapIndex_ = i - 1;
                    break; // exit the loop
                }
            }
        }

        #endregion

        #region Calculations

        public double yield()
        {
            double inner_product = 0;
            basket_.weights().ForEach((ii, vv) => inner_product += vv * yields()[ii]);
            return inner_product;
        }

        public double duration()
        {
            calculate();
            return duration_;
        }

        // bonds
        public List<double> yields()
        {
            calculate();
            return yields_;
        }

        public List<double> durations()
        {
            calculate();
            return durations_;
        }

        // swaps
        public List<double> swapLengths() => swapLenghts_;

        public List<double?> swapRates()
        {
            calculate();
            return swapRates_;
        }

        public List<double?> swapYields()
        {
            calculate();
            return swapBondYields_;
        }

        public List<double?> swapDurations()
        {
            calculate();
            return swapBondDurations_;
        }

        #endregion

        #region Equivalent Swap proxy

        public VanillaSwap equivalentSwap()
        {
            calculate();
            return swaps_[equivalentSwapIndex_];
        }

        public double equivalentSwapRate()
        {
            calculate();
            return swapRates_[equivalentSwapIndex_].Value;
        }

        public double equivalentSwapYield()
        {
            calculate();
            return swapBondYields_[equivalentSwapIndex_].Value;
        }

        public double equivalentSwapDuration()
        {
            calculate();
            return swapBondDurations_[equivalentSwapIndex_].Value;
        }

        public double equivalentSwapLength()
        {
            calculate();
            return swapLenghts_[equivalentSwapIndex_];
        }

        public double equivalentSwapSpread() => yield() - equivalentSwapRate();

        #endregion
    }
}
