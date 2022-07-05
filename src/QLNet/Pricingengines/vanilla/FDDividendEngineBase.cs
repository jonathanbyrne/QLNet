using System.Collections.Generic;
using QLNet.Instruments;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    public abstract class FDDividendEngineBase : FDMultiPeriodEngine
    {
        // required for generics
        protected FDDividendEngineBase()
        {
        }

        //public FDDividendEngineBase(GeneralizedBlackScholesProcess process,
        //    Size timeSteps = 100, Size gridPoints = 100, bool timeDependent = false)
        protected FDDividendEngineBase(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent)
        {
        }

        public abstract FDVanillaEngine factory2(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent);

        public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
            int timeSteps, int gridPoints, bool timeDependent) =>
            factory2(process, timeSteps, gridPoints, timeDependent);

        public override void setupArguments(IPricingEngineArguments a)
        {
            var args = a as DividendVanillaOption.Arguments;
            Utils.QL_REQUIRE(args != null, () => "incorrect argument ExerciseType");
            var events = new List<Event>();
            foreach (Event e in args.cashFlow)
            {
                events.Add(e);
            }

            base.setupArguments(a, events);
        }

        protected double getDiscountedDividend(int i)
        {
            var dividend = getDividendAmount(i);
            var discount = process_.riskFreeRate().link.discount(events_[i].date()) /
                           process_.dividendYield().link.discount(events_[i].date());
            return dividend * discount;
        }

        protected double getDividendAmount(int i)
        {
            var dividend = events_[i] as Dividend;
            if (dividend != null)
            {
                return dividend.amount();
            }

            return 0.0;
        }
    }
}
