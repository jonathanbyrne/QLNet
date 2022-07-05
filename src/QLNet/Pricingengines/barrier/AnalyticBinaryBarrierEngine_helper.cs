using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.Processes;

namespace QLNet.PricingEngines.barrier
{
    [PublicAPI]
    public class AnalyticBinaryBarrierEngine_helper
    {
        private BarrierOption.Arguments arguments_;
        private AmericanExercise exercise_;
        private StrikedTypePayoff payoff_;
        private GeneralizedBlackScholesProcess process_;

        public AnalyticBinaryBarrierEngine_helper(
            GeneralizedBlackScholesProcess process,
            StrikedTypePayoff payoff,
            AmericanExercise exercise,
            BarrierOption.Arguments arguments)
        {
            process_ = process;
            payoff_ = payoff;
            exercise_ = exercise;
            arguments_ = arguments;
        }

        public double payoffAtExpiry(double spot, double variance, double discount)
        {
            var dividendDiscount = process_.dividendYield().link.discount(exercise_.lastDate());

            QLNet.Utils.QL_REQUIRE(spot > 0.0, () => "positive spot value required");
            QLNet.Utils.QL_REQUIRE(discount > 0.0, () => "positive discount required");
            QLNet.Utils.QL_REQUIRE(dividendDiscount > 0.0, () => "positive dividend discount required");
            QLNet.Utils.QL_REQUIRE(variance >= 0.0, () => "negative variance not allowed");

            var type = payoff_.optionType();
            var strike = payoff_.strike();
            var barrier = arguments_.barrier;
            QLNet.Utils.QL_REQUIRE(barrier > 0.0, () => "positive barrier value required");
            var barrierType = arguments_.barrierType;

            var stdDev = System.Math.Sqrt(variance);
            var mu = System.Math.Log(dividendDiscount / discount) / variance - 0.5;
            double K = 0;

            // binary cash-or-nothing payoff?
            if (payoff_ is CashOrNothingPayoff coo)
            {
                K = coo.cashPayoff();
            }

            // binary asset-or-nothing payoff?
            if (payoff_ is AssetOrNothingPayoff aoo)
            {
                mu += 1.0;
                K = spot * dividendDiscount / discount; // forward
            }

            var log_S_X = System.Math.Log(spot / strike);
            var log_S_H = System.Math.Log(spot / barrier.GetValueOrDefault());
            var log_H_S = System.Math.Log(barrier.GetValueOrDefault() / spot);
            var log_H2_SX = System.Math.Log(barrier.GetValueOrDefault() * barrier.GetValueOrDefault() / (spot * strike));
            var H_S_2mu = System.Math.Pow(barrier.GetValueOrDefault() / spot, 2 * mu);

            var eta = barrierType == Barrier.Type.DownIn ||
                      barrierType == Barrier.Type.DownOut
                ? 1.0
                : -1.0;
            var phi = type == QLNet.Option.Type.Call ? 1.0 : -1.0;

            double x1, x2, y1, y2;
            double cum_x1, cum_x2, cum_y1, cum_y2;
            if (variance >= Const.QL_EPSILON)
            {
                // we calculate using mu*stddev instead of (mu+1)*stddev
                // because cash-or-nothing don't need it. asset-or-nothing
                // mu is really mu+1
                x1 = phi * (log_S_X / stdDev + mu * stdDev);
                x2 = phi * (log_S_H / stdDev + mu * stdDev);
                y1 = eta * (log_H2_SX / stdDev + mu * stdDev);
                y2 = eta * (log_H_S / stdDev + mu * stdDev);

                var f = new CumulativeNormalDistribution();
                cum_x1 = f.value(x1);
                cum_x2 = f.value(x2);
                cum_y1 = f.value(y1);
                cum_y2 = f.value(y2);
            }
            else
            {
                if (log_S_X > 0)
                {
                    cum_x1 = 1.0;
                }
                else
                {
                    cum_x1 = 0.0;
                }

                if (log_S_H > 0)
                {
                    cum_x2 = 1.0;
                }
                else
                {
                    cum_x2 = 0.0;
                }

                if (log_H2_SX > 0)
                {
                    cum_y1 = 1.0;
                }
                else
                {
                    cum_y1 = 0.0;
                }

                if (log_H_S > 0)
                {
                    cum_y2 = 1.0;
                }
                else
                {
                    cum_y2 = 0.0;
                }
            }

            double alpha = 0;

            switch (barrierType)
            {
                case Barrier.Type.DownIn:
                    if (type == QLNet.Option.Type.Call)
                    {
                        // down-in and call
                        if (strike >= barrier)
                        {
                            // B3 (eta=1, phi=1)
                            alpha = H_S_2mu * cum_y1;
                        }
                        else
                        {
                            // B1-B2+B4 (eta=1, phi=1)
                            alpha = cum_x1 - cum_x2 + H_S_2mu * cum_y2;
                        }
                    }
                    else
                    {
                        // down-in and put
                        if (strike >= barrier)
                        {
                            // B2-B3+B4 (eta=1, phi=-1)
                            alpha = cum_x2 + H_S_2mu * (-cum_y1 + cum_y2);
                        }
                        else
                        {
                            // B1 (eta=1, phi=-1)
                            alpha = cum_x1;
                        }
                    }

                    break;

                case Barrier.Type.UpIn:
                    if (type == QLNet.Option.Type.Call)
                    {
                        // up-in and call
                        if (strike >= barrier)
                        {
                            // B1 (eta=-1, phi=1)
                            alpha = cum_x1;
                        }
                        else
                        {
                            // B2-B3+B4 (eta=-1, phi=1)
                            alpha = cum_x2 + H_S_2mu * (-cum_y1 + cum_y2);
                        }
                    }
                    else
                    {
                        // up-in and put
                        if (strike >= barrier)
                        {
                            // B1-B2+B4 (eta=-1, phi=-1)
                            alpha = cum_x1 - cum_x2 + H_S_2mu * cum_y2;
                        }
                        else
                        {
                            // B3 (eta=-1, phi=-1)
                            alpha = H_S_2mu * cum_y1;
                        }
                    }

                    break;

                case Barrier.Type.DownOut:
                    if (type == QLNet.Option.Type.Call)
                    {
                        // down-out and call
                        if (strike >= barrier)
                        {
                            // B1-B3 (eta=1, phi=1)
                            alpha = cum_x1 - H_S_2mu * cum_y1;
                        }
                        else
                        {
                            // B2-B4 (eta=1, phi=1)
                            alpha = cum_x2 - H_S_2mu * cum_y2;
                        }
                    }
                    else
                    {
                        // down-out and put
                        if (strike >= barrier)
                        {
                            // B1-B2+B3-B4 (eta=1, phi=-1)
                            alpha = cum_x1 - cum_x2 + H_S_2mu * (cum_y1 - cum_y2);
                        }
                        else
                        {
                            // always 0
                            alpha = 0;
                        }
                    }

                    break;
                case Barrier.Type.UpOut:
                    if (type == QLNet.Option.Type.Call)
                    {
                        // up-out and call
                        if (strike >= barrier)
                        {
                            // always 0
                            alpha = 0;
                        }
                        else
                        {
                            // B1-B2+B3-B4 (eta=-1, phi=1)
                            alpha = cum_x1 - cum_x2 + H_S_2mu * (cum_y1 - cum_y2);
                        }
                    }
                    else
                    {
                        // up-out and put
                        if (strike >= barrier)
                        {
                            // B2-B4 (eta=-1, phi=-1)
                            alpha = cum_x2 - H_S_2mu * cum_y2;
                        }
                        else
                        {
                            // B1-B3 (eta=-1, phi=-1)
                            alpha = cum_x1 - H_S_2mu * cum_y1;
                        }
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid barrier ExerciseType");
                    break;
            }

            return discount * K * alpha;
        }
    }
}
