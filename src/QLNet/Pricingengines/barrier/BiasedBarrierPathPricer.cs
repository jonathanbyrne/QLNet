using System.Collections.Generic;
using System.Linq;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.barrier
{
    [JetBrains.Annotations.PublicAPI] public class BiasedBarrierPathPricer : PathPricer<IPath>
    {
        public BiasedBarrierPathPricer(Barrier.Type barrierType,
            double? barrier,
            double? rebate,
            QLNet.Option.Type type,
            double strike,
            List<double> discounts)
            : base()
        {
            barrierType_ = barrierType;
            barrier_ = barrier;
            rebate_ = rebate;
            payoff_ = new PlainVanillaPayoff(type, strike);
            discounts_ = discounts;

            Utils.QL_REQUIRE(strike >= 0.0,
                () => "strike less than zero not allowed");
            Utils.QL_REQUIRE(barrier > 0.0,
                () => "barrier less/equal zero not allowed");
        }

        public double value(IPath path)
        {
            var n = path.length();
            Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

            var isOptionActive = false;
            int? knockNode = null;
            var asset_price = (path as Path).front();
            int i;

            switch (barrierType_)
            {
                case Barrier.Type.DownIn:
                    isOptionActive = false;
                    for (i = 1; i < n; i++)
                    {
                        asset_price = (path as Path)[i];
                        if (asset_price <= barrier_)
                        {
                            isOptionActive = true;
                            if (knockNode == null)
                                knockNode = i;
                        }
                    }
                    break;
                case Barrier.Type.UpIn:
                    isOptionActive = false;
                    for (i = 1; i < n; i++)
                    {
                        asset_price = (path as Path)[i];
                        if (asset_price >= barrier_)
                        {
                            isOptionActive = true;
                            if (knockNode == null)
                                knockNode = i;
                        }
                    }
                    break;
                case Barrier.Type.DownOut:
                    isOptionActive = true;
                    for (i = 1; i < n; i++)
                    {
                        asset_price = (path as Path)[i];
                        if (asset_price <= barrier_)
                        {
                            isOptionActive = false;
                            if (knockNode == null)
                                knockNode = i;
                        }
                    }
                    break;
                case Barrier.Type.UpOut:
                    isOptionActive = true;
                    for (i = 1; i < n; i++)
                    {
                        asset_price = (path as Path)[i];
                        if (asset_price >= barrier_)
                        {
                            isOptionActive = false;
                            if (knockNode == null)
                                knockNode = i;
                        }
                    }
                    break;
                default:
                    Utils.QL_FAIL("unknown barrier ExerciseType");
                    break;
            }

            if (isOptionActive)
            {
                return payoff_.value(asset_price) * discounts_.Last();
            }
            else
            {
                switch (barrierType_)
                {
                    case Barrier.Type.UpIn:
                    case Barrier.Type.DownIn:
                        return rebate_.GetValueOrDefault() * discounts_.Last();
                    case Barrier.Type.UpOut:
                    case Barrier.Type.DownOut:
                        return rebate_.GetValueOrDefault() * discounts_[(int)knockNode];
                    default:
                        Utils.QL_FAIL("unknown barrier ExerciseType");
                        return -1;
                }
            }
        }

        protected Barrier.Type barrierType_;
        protected double? barrier_;
        protected double? rebate_;
        protected PlainVanillaPayoff payoff_;
        protected List<double> discounts_;
    }
}