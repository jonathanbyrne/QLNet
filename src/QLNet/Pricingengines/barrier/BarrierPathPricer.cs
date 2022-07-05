using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.randomnumbers;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.barrier
{
    [PublicAPI]
    public class BarrierPathPricer : PathPricer<IPath>
    {
        protected double? barrier_;
        protected Barrier.Type barrierType_;
        protected StochasticProcess1D diffProcess_;
        protected List<double> discounts_;
        protected PlainVanillaPayoff payoff_;
        protected double? rebate_;
        protected IRNG sequenceGen_;

        public BarrierPathPricer(
            Barrier.Type barrierType,
            double? barrier,
            double? rebate,
            QLNet.Option.Type type,
            double strike,
            List<double> discounts,
            StochasticProcess1D diffProcess,
            IRNG sequenceGen)
        {
            barrierType_ = barrierType;
            barrier_ = barrier;
            rebate_ = rebate;
            diffProcess_ = diffProcess;
            sequenceGen_ = sequenceGen;
            payoff_ = new PlainVanillaPayoff(type, strike);
            discounts_ = discounts;
            Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
            Utils.QL_REQUIRE(barrier > 0.0, () => "barrier less/equal zero not allowed");
        }

        public double value(IPath path)
        {
            var n = path.length();
            Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

            var isOptionActive = false;
            int? knockNode = null;
            var asset_price = (path as Path).front();
            double new_asset_price;
            double x, y;
            double vol;
            var timeGrid = (path as Path).timeGrid();
            double dt;
            var u = sequenceGen_.nextSequence().value;
            int i;

            switch (barrierType_)
            {
                case Barrier.Type.DownIn:
                    isOptionActive = false;
                    for (i = 0; i < n - 1; i++)
                    {
                        new_asset_price = (path as Path)[i + 1];
                        // terminal or initial vol?
                        vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                        dt = timeGrid.dt(i);

                        x = System.Math.Log(new_asset_price / asset_price);
                        y = 0.5 * (x - System.Math.Sqrt(x * x - 2 * vol * vol * dt * System.Math.Log(u[i])));
                        y = asset_price * System.Math.Exp(y);
                        if (y <= barrier_)
                        {
                            isOptionActive = true;
                            if (knockNode == null)
                            {
                                knockNode = i + 1;
                            }
                        }

                        asset_price = new_asset_price;
                    }

                    break;
                case Barrier.Type.UpIn:
                    isOptionActive = false;
                    for (i = 0; i < n - 1; i++)
                    {
                        new_asset_price = (path as Path)[i + 1];
                        // terminal or initial vol?
                        vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                        dt = timeGrid.dt(i);

                        x = System.Math.Log(new_asset_price / asset_price);
                        y = 0.5 * (x + System.Math.Sqrt(x * x - 2 * vol * vol * dt * System.Math.Log(1 - u[i])));
                        y = asset_price * System.Math.Exp(y);
                        if (y >= barrier_)
                        {
                            isOptionActive = true;
                            if (knockNode == null)
                            {
                                knockNode = i + 1;
                            }
                        }

                        asset_price = new_asset_price;
                    }

                    break;
                case Barrier.Type.DownOut:
                    isOptionActive = true;
                    for (i = 0; i < n - 1; i++)
                    {
                        new_asset_price = (path as Path)[i + 1];
                        // terminal or initial vol?
                        vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                        dt = timeGrid.dt(i);

                        x = System.Math.Log(new_asset_price / asset_price);
                        y = 0.5 * (x - System.Math.Sqrt(x * x - 2 * vol * vol * dt * System.Math.Log(u[i])));
                        y = asset_price * System.Math.Exp(y);
                        if (y <= barrier_)
                        {
                            isOptionActive = false;
                            if (knockNode == null)
                            {
                                knockNode = i + 1;
                            }
                        }

                        asset_price = new_asset_price;
                    }

                    break;
                case Barrier.Type.UpOut:
                    isOptionActive = true;
                    for (i = 0; i < n - 1; i++)
                    {
                        new_asset_price = (path as Path)[i + 1];
                        // terminal or initial vol?
                        vol = diffProcess_.diffusion(timeGrid[i], asset_price);
                        dt = timeGrid.dt(i);

                        x = System.Math.Log(new_asset_price / asset_price);
                        y = 0.5 * (x + System.Math.Sqrt(x * x - 2 * vol * vol * dt * System.Math.Log(1 - u[i])));
                        y = asset_price * System.Math.Exp(y);
                        if (y >= barrier_)
                        {
                            isOptionActive = false;
                            if (knockNode == null)
                            {
                                knockNode = i + 1;
                            }
                        }

                        asset_price = new_asset_price;
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
}
