//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math.Distributions;
using QLNet.Math.Solvers1d;
using QLNet.Quotes;

namespace QLNet.Pricingengines
{
    //! Black delta calculator class
    /*! Class includes many operations needed for different applications
        in FX markets, which has special quoation mechanisms, since
        every price can be expressed in both numeraires.
    */
    [PublicAPI]
    public class BlackDeltaCalculator
    {
        private double dDiscount_, fDiscount_;
        private DeltaVolQuote.DeltaType dt_;
        private double fExpPos_, fExpNeg_;
        private QLNet.Option.Type ot_;
        private int phi_;
        private double stdDev_, spot_, forward_;

        // A parsimonious constructor is chosen, which for example
        // doesn't need a strike. The reason for this is, that we'd
        // like this class to calculate deltas for different strikes
        // many times, e.g. in a numerical routine, which will be the
        // case in the smile setup procedure.
        public BlackDeltaCalculator(QLNet.Option.Type ot,
            DeltaVolQuote.DeltaType dt,
            double spot,
            double dDiscount, // domestic discount
            double fDiscount, // foreign discount
            double stdDev)
        {
            dt_ = dt;
            ot_ = ot;
            dDiscount_ = dDiscount;
            fDiscount_ = fDiscount;
            stdDev_ = stdDev;
            spot_ = spot;
            forward_ = spot * fDiscount / dDiscount;
            phi_ = (int)ot;

            Utils.QL_REQUIRE(spot_ > 0.0, () => "positive spot value required: " + spot_ + " not allowed");
            Utils.QL_REQUIRE(dDiscount_ > 0.0, () => "positive domestic discount factor required: " + dDiscount_ + " not allowed");
            Utils.QL_REQUIRE(fDiscount_ > 0.0, () => "positive foreign discount factor required: " + fDiscount_ + " not allowed");
            Utils.QL_REQUIRE(stdDev_ >= 0.0, () => "non-negative standard deviation required: " + stdDev_ + " not allowed");

            fExpPos_ = forward_ * System.Math.Exp(0.5 * stdDev_ * stdDev_);
            fExpNeg_ = forward_ * System.Math.Exp(-0.5 * stdDev_ * stdDev_);
        }

        // The following function can be calculated without an explicit strike
        public double atmStrike(DeltaVolQuote.AtmType atmT)
        {
            var res = 0.0;

            switch (atmT)
            {
                case DeltaVolQuote.AtmType.AtmDeltaNeutral:
                    if (dt_ == DeltaVolQuote.DeltaType.Spot || dt_ == DeltaVolQuote.DeltaType.Fwd)
                    {
                        res = fExpPos_;
                    }
                    else
                    {
                        res = fExpNeg_;
                    }

                    break;

                case DeltaVolQuote.AtmType.AtmFwd:
                    res = forward_;
                    break;

                case DeltaVolQuote.AtmType.AtmGammaMax:
                case DeltaVolQuote.AtmType.AtmVegaMax:
                    res = fExpPos_;
                    break;

                case DeltaVolQuote.AtmType.AtmPutCall50:
                    Utils.QL_REQUIRE(dt_ == DeltaVolQuote.DeltaType.Fwd, () =>
                        "|PutDelta|=CallDelta=0.50 only possible for forward delta.");
                    res = fExpPos_;
                    break;

                default:
                    Utils.QL_FAIL("invalid atm ExerciseType");
                    break;
            }

            return res;
        }

        public double cumD1(double strike) // N(d1) or N(-d1)
        {
            var d1_ = 0.0;
            var cum_d1_pos_ = 1.0; // N(d1)
            var cum_d1_neg_ = 0.0; // N(-d1)

            var f = new CumulativeNormalDistribution();

            if (stdDev_ >= Const.QL_EPSILON)
            {
                if (strike > 0)
                {
                    d1_ = System.Math.Log(forward_ / strike) / stdDev_ + 0.5 * stdDev_;
                    return f.value(phi_ * d1_);
                }
            }
            else
            {
                if (forward_ < strike)
                {
                    cum_d1_pos_ = 0.0;
                    cum_d1_neg_ = 1.0;
                }
                else if (forward_.IsEqual(strike))
                {
                    d1_ = 0.5 * stdDev_;
                    return f.value(phi_ * d1_);
                }
            }

            if (phi_ > 0)
            {
                // if Call
                return cum_d1_pos_;
            }

            return cum_d1_neg_;
        }

        public double cumD2(double strike) // N(d2) or N(-d2)
        {
            var d2_ = 0.0;
            var cum_d2_pos_ = 1.0; // N(d2)
            var cum_d2_neg_ = 0.0; // N(-d2)

            var f = new CumulativeNormalDistribution();

            if (stdDev_ >= Const.QL_EPSILON)
            {
                if (strike > 0)
                {
                    d2_ = System.Math.Log(forward_ / strike) / stdDev_ - 0.5 * stdDev_;
                    return f.value(phi_ * d2_);
                }
            }
            else
            {
                if (forward_ < strike)
                {
                    cum_d2_pos_ = 0.0;
                    cum_d2_neg_ = 1.0;
                }
                else if (forward_.IsEqual(strike))
                {
                    d2_ = -0.5 * stdDev_;
                    return f.value(phi_ * d2_);
                }
            }

            if (phi_ > 0)
            {
                // if Call
                return cum_d2_pos_;
            }

            return cum_d2_neg_;
        }

        // Give strike, receive delta according to specified ExerciseType
        public double deltaFromStrike(double strike)
        {
            Utils.QL_REQUIRE(strike >= 0.0, () => "positive strike value required: " + strike + " not allowed");

            var res = 0.0;

            switch (dt_)
            {
                case DeltaVolQuote.DeltaType.Spot:
                    res = phi_ * fDiscount_ * cumD1(strike);
                    break;

                case DeltaVolQuote.DeltaType.Fwd:
                    res = phi_ * cumD1(strike);
                    break;

                case DeltaVolQuote.DeltaType.PaSpot:
                    res = phi_ * fDiscount_ * cumD2(strike) * strike / forward_;
                    break;

                case DeltaVolQuote.DeltaType.PaFwd:
                    res = phi_ * cumD2(strike) * strike / forward_;
                    break;

                default:
                    Utils.QL_FAIL("invalid delta ExerciseType");
                    break;
            }

            return res;
        }

        public double nD1(double strike) // n(d1)
        {
            var d1_ = 0.0;
            var n_d1_ = 0.0; // n(d1)

            if (stdDev_ >= Const.QL_EPSILON)
            {
                if (strike > 0)
                {
                    d1_ = System.Math.Log(forward_ / strike) / stdDev_ + 0.5 * stdDev_;
                    var f = new CumulativeNormalDistribution();
                    n_d1_ = f.derivative(d1_);
                }
            }

            return n_d1_;
        }

        public double nD2(double strike) // n(d2)
        {
            var d2_ = 0.0;
            var n_d2_ = 0.0; // n(d2)

            if (stdDev_ >= Const.QL_EPSILON)
            {
                if (strike > 0)
                {
                    d2_ = System.Math.Log(forward_ / strike) / stdDev_ - 0.5 * stdDev_;
                    var f = new CumulativeNormalDistribution();
                    n_d2_ = f.derivative(d2_);
                }
            }

            return n_d2_;
        }

        public void setDeltaType(DeltaVolQuote.DeltaType dt)
        {
            dt_ = dt;
        }

        public void setOptionType(QLNet.Option.Type ot)
        {
            ot_ = ot;
            phi_ = (int)ot_;
        }

        // Give delta according to specified ExerciseType, receive strike
        public double strikeFromDelta(double delta) => strikeFromDelta(delta, dt_);

        // alternative delta ExerciseType
        private double strikeFromDelta(double delta, DeltaVolQuote.DeltaType dt)
        {
            var res = 0.0;
            var arg = 0.0;
            var f = new InverseCumulativeNormal();

            Utils.QL_REQUIRE(delta * phi_ >= 0.0, () => "Option ExerciseType and delta are incoherent.");

            switch (dt)
            {
                case DeltaVolQuote.DeltaType.Spot:
                    Utils.QL_REQUIRE(System.Math.Abs(delta) <= fDiscount_, () => "Spot delta out of range.");
                    arg = -phi_ * f.value(phi_ * delta / fDiscount_) * stdDev_ + 0.5 * stdDev_ * stdDev_;
                    res = forward_ * System.Math.Exp(arg);
                    break;

                case DeltaVolQuote.DeltaType.Fwd:
                    Utils.QL_REQUIRE(System.Math.Abs(delta) <= 1.0, () => "Forward delta out of range.");
                    arg = -phi_ * f.value(phi_ * delta) * stdDev_ + 0.5 * stdDev_ * stdDev_;
                    res = forward_ * System.Math.Exp(arg);
                    break;

                case DeltaVolQuote.DeltaType.PaSpot:
                case DeltaVolQuote.DeltaType.PaFwd:
                    // This has to be solved numerically. One of the
                    // problems is that the premium adjusted call delta is
                    // not monotonic in strike, such that two solutions
                    // might occur. The one right to the max of the delta is
                    // considered to be the correct strike.  Some proper
                    // interval bounds for the strike need to be chosen, the
                    // numerics can otherwise be very unreliable and
                    // unstable.  I've chosen Brent over Newton, since the
                    // interval can be specified explicitly and we can not
                    // run into the area on the left of the maximum.  The
                    // put delta doesn't have this property and can be
                    // solved without any problems, but also numerically.

                    var f1 = new BlackDeltaPremiumAdjustedSolverClass(
                        ot_, dt, spot_, dDiscount_, fDiscount_, stdDev_, delta);

                    var solver = new Brent();
                    solver.setMaxEvaluations(1000);
                    var accuracy = 1.0e-10;

                    var rightLimit = 0.0;
                    var leftLimit = 0.0;

                    // Strike of not premium adjusted is always to the right of premium adjusted
                    if (dt == DeltaVolQuote.DeltaType.PaSpot)
                    {
                        rightLimit = strikeFromDelta(delta, DeltaVolQuote.DeltaType.Spot);
                    }
                    else
                    {
                        rightLimit = strikeFromDelta(delta, DeltaVolQuote.DeltaType.Fwd);
                    }

                    if (phi_ < 0)
                    {
                        // if put
                        res = solver.solve(f1, accuracy, rightLimit, 0.0, spot_ * 100.0);
                        break;
                    }
                    // find out the left limit which is the strike
                    // corresponding to the value where premium adjusted
                    // deltas have their maximum.

                    var g = new BlackDeltaPremiumAdjustedMaxStrikeClass(
                        ot_, dt, spot_, dDiscount_, fDiscount_, stdDev_);

                    leftLimit = solver.solve(g, accuracy, rightLimit * 0.5, 0.0, rightLimit);

                    var guess = leftLimit + (rightLimit - leftLimit) * 0.5;

                    res = solver.solve(f1, accuracy, guess, leftLimit, rightLimit);

                    break;

                default:
                    Utils.QL_FAIL("invalid delta ExerciseType");
                    break;
            }

            return res;
        }
    }
}
