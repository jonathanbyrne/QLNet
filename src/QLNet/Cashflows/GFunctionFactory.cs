using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class GFunctionFactory
    {
        public enum YieldCurveModel
        {
            Standard,
            ExactYield,
            ParallelShifts,
            NonParallelShifts
        }

        //===========================================================================//
        //                              GFunctionExactYield                          //
        //===========================================================================//
        private class GFunctionExactYield : GFunction
        {
            // accruals fraction
            protected readonly List<double> accruals_;
            //             fraction of a period between the swap start date and the pay date
            protected readonly double delta_;

            public GFunctionExactYield(CmsCoupon coupon)
            {
                var swapIndex = coupon.swapIndex();
                var swap = swapIndex.underlyingSwap(coupon.fixingDate());

                var schedule = swap.fixedSchedule();
                var rateCurve = swapIndex.forwardingTermStructure();

                var dc = swapIndex.dayCounter();

                var swapStartTime = dc.yearFraction(rateCurve.link.referenceDate(), schedule.startDate());
                var swapFirstPaymentTime = dc.yearFraction(rateCurve.link.referenceDate(), schedule.date(1));

                var paymentTime = dc.yearFraction(rateCurve.link.referenceDate(), coupon.date());

                delta_ = (paymentTime - swapStartTime) / (swapFirstPaymentTime - swapStartTime);

                var fixedLeg = new List<CashFlow>(swap.fixedLeg());
                var n = fixedLeg.Count;
                accruals_ = new List<double>();
                for (var i = 0; i < n; ++i)
                {
                    var coupon1 = fixedLeg[i] as Coupon;
                    accruals_.Add(coupon1.accrualPeriod());
                }
            }

            public override double firstDerivative(double x)
            {
                var c = -1.0;
                var derC = 0.0;
                var b = new List<double>();
                for (var i = 0; i < accruals_.Count; i++)
                {
                    var temp = 1.0 / (1.0 + accruals_[i] * x);
                    b.Add(temp);
                    c *= temp;
                    derC += accruals_[i] * temp;
                }

                c += 1.0;
                c = 1.0 / c;
                derC *= c - c * c;

                return -delta_ * accruals_[0] * System.Math.Pow(b[0], delta_ + 1.0) * x * c + System.Math.Pow(b[0], delta_) * c + System.Math.Pow(b[0], delta_) * x * derC;
            }

            public override double secondDerivative(double x)
            {
                var c = -1.0;
                var sum = 0.0;
                var sumOfSquare = 0.0;
                var b = new List<double>();
                for (var i = 0; i < accruals_.Count; i++)
                {
                    var temp = 1.0 / (1.0 + accruals_[i] * x);
                    b.Add(temp);
                    c *= temp;
                    sum += accruals_[i] * temp;
                    sumOfSquare += System.Math.Pow(accruals_[i] * temp, 2.0);
                }

                c += 1.0;
                c = 1.0 / c;
                var derC = sum * (c - c * c);

                return (-delta_ * accruals_[0] * System.Math.Pow(b[0], delta_ + 1.0) * c + System.Math.Pow(b[0], delta_) * derC) * (-delta_ * accruals_[0] * b[0] * x + 1.0 + x * (1.0 - c) * sum) + System.Math.Pow(b[0], delta_) * c * (delta_ * System.Math.Pow(accruals_[0] * b[0], 2.0) * x - delta_ * accruals_[0] * b[0] - x * derC * sum + (1.0 - c) * sum - x * (1.0 - c) * sumOfSquare);
            }

            public override double value(double x)
            {
                var product = 1.0;
                for (var i = 0; i < accruals_.Count; i++)
                {
                    product *= 1.0 / (1.0 + accruals_[i] * x);
                }

                return x * System.Math.Pow(1.0 + accruals_[0] * x, -delta_) * (1.0 / (1.0 - product));
            }
        }

        //===========================================================================//
        //                              GFunctionStandard                            //
        //===========================================================================//
        private class GFunctionStandard : GFunction
        {
            //             fraction of a period between the swap start date and the pay date
            protected readonly double delta_;
            // number of period per year
            protected readonly int q_;
            // length of swap
            protected readonly int swapLength_;

            public GFunctionStandard(int q, double delta, int swapLength)
            {
                q_ = q;
                delta_ = delta;
                swapLength_ = swapLength;
            }

            public override double firstDerivative(double x)
            {
                double n = swapLength_ * q_;
                var a = 1.0 + x / q_;
                var AA = a - delta_ / q_ * x;
                var B = System.Math.Pow(a, n - delta_ - 1.0) / (System.Math.Pow(a, n) - 1.0);

                var secNum = n * x * System.Math.Pow(a, n - 1.0);
                var secDen = q_ * System.Math.Pow(a, delta_) * (System.Math.Pow(a, n) - 1.0) * (System.Math.Pow(a, n) - 1.0);
                var sec = secNum / secDen;

                return AA * B - sec;
            }

            public override double secondDerivative(double x)
            {
                double n = swapLength_ * q_;
                var a = 1.0 + x / q_;
                var AA = a - delta_ / q_ * x;
                var A1 = (1.0 - delta_) / q_;
                var B = System.Math.Pow(a, n - delta_ - 1.0) / (System.Math.Pow(a, n) - 1.0);
                var Num = (1.0 + delta_ - n) * System.Math.Pow(a, n - delta_ - 2.0) - (1.0 + delta_) * System.Math.Pow(a, 2.0 * n - delta_ - 2.0);
                var Den = (System.Math.Pow(a, n) - 1.0) * (System.Math.Pow(a, n) - 1.0);
                var B1 = 1.0 / q_ * Num / Den;

                var C = x / System.Math.Pow(a, delta_);
                var C1 = (System.Math.Pow(a, delta_) - delta_ / q_ * x * System.Math.Pow(a, delta_ - 1.0)) / System.Math.Pow(a, 2 * delta_);

                var D = System.Math.Pow(a, n - 1.0) / ((System.Math.Pow(a, n) - 1.0) * (System.Math.Pow(a, n) - 1.0));
                var D1 = ((n - 1.0) * System.Math.Pow(a, n - 2.0) * (System.Math.Pow(a, n) - 1.0) - 2 * n * System.Math.Pow(a, 2 * (n - 1.0))) / (q_ * (System.Math.Pow(a, n) - 1.0) * (System.Math.Pow(a, n) - 1.0) * (System.Math.Pow(a, n) - 1.0));

                return A1 * B + AA * B1 - n / q_ * (C1 * D + C * D1);
            }

            public override double value(double x)
            {
                double n = swapLength_ * q_;
                return x / System.Math.Pow(1.0 + x / q_, delta_) * 1.0 / (1.0 - 1.0 / System.Math.Pow(1.0 + x / q_, n));
            }
        }

        private class GFunctionWithShifts : GFunction
        {
            private class ObjectiveFunction : ISolver1d
            {
                private readonly GFunctionWithShifts o_;
                private double derivative_;
                private double Rs_;

                public ObjectiveFunction(GFunctionWithShifts o, double Rs)
                {
                    o_ = o;
                    Rs_ = Rs;
                }

                public override double derivative(double UnnamedParameter1) => derivative_;

                public GFunctionWithShifts gFunctionWithShifts() => o_;

                public void setSwapRateValue(double x)
                {
                    Rs_ = x;
                }

                public override double value(double x)
                {
                    double result = 0;
                    derivative_ = 0;
                    for (var i = 0; i < o_.accruals_.Count; i++)
                    {
                        var temp = o_.accruals_[i] * o_.swapPaymentDiscounts_[i] * System.Math.Exp(-o_.shapedSwapPaymentTimes_[i] * x);
                        result += temp;
                        derivative_ -= o_.shapedSwapPaymentTimes_[i] * temp;
                    }

                    result *= Rs_;
                    derivative_ *= Rs_;
                    var temp1 = o_.swapPaymentDiscounts_.Last() * System.Math.Exp(-o_.shapedSwapPaymentTimes_.Last() * x);

                    result += temp1 - o_.discountAtStart_;
                    derivative_ -= o_.shapedSwapPaymentTimes_.Last() * temp1;
                    return result;
                }
            }

            private readonly List<double> accruals_;
            private readonly double accuracy_;
            private readonly double discountAtStart_;
            private readonly double discountRatio_;
            private readonly Handle<Quote> meanReversion_;
            private readonly ObjectiveFunction objectiveFunction_;
            private readonly double shapedPaymentTime_;
            private readonly List<double> shapedSwapPaymentTimes_;
            private readonly List<double> swapPaymentDiscounts_;
            private readonly double swapRateValue_;
            private readonly double swapStartTime_;
            private double calibratedShift_;
            private double tmpRs_;

            //===========================================================================//
            //                            GFunctionWithShifts                            //
            //===========================================================================//
            public GFunctionWithShifts(CmsCoupon coupon, Handle<Quote> meanReversion)
            {
                meanReversion_ = meanReversion;
                calibratedShift_ = 0.03;
                tmpRs_ = 10000000.0;
                accuracy_ = 1.0e-14;

                var swapIndex = coupon.swapIndex();
                var swap = swapIndex.underlyingSwap(coupon.fixingDate());

                swapRateValue_ = swap.fairRate();

                objectiveFunction_ = new ObjectiveFunction(this, swapRateValue_);

                var schedule = swap.fixedSchedule();
                var rateCurve = swapIndex.forwardingTermStructure();
                var dc = swapIndex.dayCounter();

                swapStartTime_ = dc.yearFraction(rateCurve.link.referenceDate(), schedule.startDate());
                discountAtStart_ = rateCurve.link.discount(schedule.startDate());

                var paymentTime = dc.yearFraction(rateCurve.link.referenceDate(), coupon.date());

                shapedPaymentTime_ = shapeOfShift(paymentTime);

                var fixedLeg = new List<CashFlow>(swap.fixedLeg());
                var n = fixedLeg.Count;

                shapedSwapPaymentTimes_ = new List<double>();
                swapPaymentDiscounts_ = new List<double>();
                accruals_ = new List<double>();

                for (var i = 0; i < n; ++i)
                {
                    var coupon1 = fixedLeg[i] as Coupon;
                    accruals_.Add(coupon1.accrualPeriod());
                    var paymentDate = new Date(coupon1.date().serialNumber());
                    var swapPaymentTime = dc.yearFraction(rateCurve.link.referenceDate(), paymentDate);
                    shapedSwapPaymentTimes_.Add(shapeOfShift(swapPaymentTime));
                    swapPaymentDiscounts_.Add(rateCurve.link.discount(paymentDate));
                }

                discountRatio_ = swapPaymentDiscounts_.Last() / discountAtStart_;
            }

            public override double firstDerivative(double Rs)
            {
                var calibratedShift = calibrationOfShift(Rs);
                return functionZ(calibratedShift) + Rs * derZ_derX(calibratedShift) / derRs_derX(calibratedShift);
            }

            public override double secondDerivative(double Rs)
            {
                var calibratedShift = calibrationOfShift(Rs);
                return 2.0 * derZ_derX(calibratedShift) / derRs_derX(calibratedShift) + Rs * der2Z_derX2(calibratedShift) / System.Math.Pow(derRs_derX(calibratedShift), 2.0) - Rs * derZ_derX(calibratedShift) * der2Rs_derX2(calibratedShift) / System.Math.Pow(derRs_derX(calibratedShift), 3.0);
            }

            public override double value(double Rs)
            {
                var calibratedShift = calibrationOfShift(Rs);
                return Rs * functionZ(calibratedShift);
            }

            //* calibration of shift*/
            private double calibrationOfShift(double Rs)
            {
                if (Rs.IsNotEqual(tmpRs_))
                {
                    double initialGuess;
                    double N = 0;
                    double D = 0;
                    for (var i = 0; i < accruals_.Count; i++)
                    {
                        N += accruals_[i] * swapPaymentDiscounts_[i];
                        D += accruals_[i] * swapPaymentDiscounts_[i] * shapedSwapPaymentTimes_[i];
                    }

                    N *= Rs;
                    D *= Rs;
                    N += accruals_.Last() * swapPaymentDiscounts_.Last() - objectiveFunction_.gFunctionWithShifts().discountAtStart_;
                    D += accruals_.Last() * swapPaymentDiscounts_.Last() * shapedSwapPaymentTimes_.Last();
                    initialGuess = N / D;

                    objectiveFunction_.setSwapRateValue(Rs);
                    var solver = new Newton();
                    solver.setMaxEvaluations(1000);

                    // these boundaries migth not be big enough if the volatility
                    // of big swap rate values is too high . In this case the G function
                    // is not even integrable, so better to fix the vol than increasing
                    // these values
                    double lower = -20;
                    var upper = 20.0;

                    try
                    {
                        calibratedShift_ = solver.solve(objectiveFunction_, accuracy_, System.Math.Max(System.Math.Min(initialGuess, upper * .99), lower * .99), lower, upper);
                    }
                    catch (Exception e)
                    {
                        Utils.QL_FAIL("meanReversion: " + meanReversion_.link.value() + ", swapRateValue: " + swapRateValue_ + ", swapStartTime: " + swapStartTime_ + ", shapedPaymentTime: " + shapedPaymentTime_ + "\n error message: " + e.Message);
                    }

                    tmpRs_ = Rs;
                }

                return calibratedShift_;
            }

            private double der2Rs_derX2(double x)
            {
                var denOfRfunztion = 0.0;
                var derDenOfRfunztion = 0.0;
                var der2DenOfRfunztion = 0.0;
                for (var i = 0; i < accruals_.Count; i++)
                {
                    denOfRfunztion += accruals_[i] * swapPaymentDiscounts_[i] * System.Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    derDenOfRfunztion -= shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * System.Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    der2DenOfRfunztion += shapedSwapPaymentTimes_[i] * shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * System.Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                }

                var denominator = System.Math.Pow(denOfRfunztion, 4);

                double numOfDerR = 0;
                numOfDerR += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * denOfRfunztion;
                numOfDerR -= (discountAtStart_ - swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * derDenOfRfunztion;

                var denOfDerR = System.Math.Pow(denOfRfunztion, 2);

                var derNumOfDerR = 0.0;
                derNumOfDerR -= shapedSwapPaymentTimes_.Last() * shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * denOfRfunztion;
                derNumOfDerR += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * derDenOfRfunztion;

                derNumOfDerR -= shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * derDenOfRfunztion;
                derNumOfDerR -= (discountAtStart_ - swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * der2DenOfRfunztion;

                var derDenOfDerR = 2 * denOfRfunztion * derDenOfRfunztion;

                var numerator = derNumOfDerR * denOfDerR - numOfDerR * derDenOfDerR;
                if (denominator.IsEqual(0.0))
                {
                    Utils.QL_FAIL("GFunctionWithShifts::der2Rs_derX2: denominator == 0");
                }

                return numerator / denominator;
            }

            private double der2Z_derX2(double x)
            {
                var denOfZfunction = 1.0 - discountRatio_ * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x);
                var derDenOfZfunction = shapedSwapPaymentTimes_.Last() * discountRatio_ * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x);
                var denominator = System.Math.Pow(denOfZfunction, 4);
                if (denominator.IsEqual(0))
                {
                    Utils.QL_FAIL("GFunctionWithShifts::der2Z_derX2: denominator == 0");
                }

                double numOfDerZ = 0;
                numOfDerZ -= shapedPaymentTime_ * System.Math.Exp(-shapedPaymentTime_ * x) * denOfZfunction;
                numOfDerZ -= shapedSwapPaymentTimes_.Last() * System.Math.Exp(-shapedPaymentTime_ * x) * (1.0 - denOfZfunction);

                var denOfDerZ = System.Math.Pow(denOfZfunction, 2);
                var derNumOfDerZ = -shapedPaymentTime_ * System.Math.Exp(-shapedPaymentTime_ * x) * (-shapedPaymentTime_ + (shapedPaymentTime_ * discountRatio_ - shapedSwapPaymentTimes_.Last() * discountRatio_) * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) - shapedSwapPaymentTimes_.Last() * System.Math.Exp(-shapedPaymentTime_ * x) * (shapedPaymentTime_ * discountRatio_ - shapedSwapPaymentTimes_.Last() * discountRatio_) * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x);

                var derDenOfDerZ = 2 * denOfZfunction * derDenOfZfunction;
                var numerator = derNumOfDerZ * denOfDerZ - numOfDerZ * derDenOfDerZ;

                return numerator / denominator;
            }

            private double derRs_derX(double x)
            {
                double sqrtDenominator = 0;
                double derSqrtDenominator = 0;
                for (var i = 0; i < accruals_.Count; i++)
                {
                    sqrtDenominator += accruals_[i] * swapPaymentDiscounts_[i] * System.Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    derSqrtDenominator -= shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * System.Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                }

                var denominator = sqrtDenominator * sqrtDenominator;

                double numerator = 0;
                numerator += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * sqrtDenominator;
                numerator -= (discountAtStart_ - swapPaymentDiscounts_.Last() * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * derSqrtDenominator;
                if (denominator.IsEqual(0.0))
                {
                    Utils.QL_FAIL("GFunctionWithShifts::derRs_derX: denominator == 0");
                }

                return numerator / denominator;
            }

            private double derZ_derX(double x)
            {
                var sqrtDenominator = 1.0 - discountRatio_ * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x);
                var denominator = sqrtDenominator * sqrtDenominator;
                if (denominator.IsEqual(0.0))
                {
                    Utils.QL_FAIL("GFunctionWithShifts::derZ_derX: denominator == 0");
                }

                double numerator = 0;
                numerator -= shapedPaymentTime_ * System.Math.Exp(-shapedPaymentTime_ * x) * sqrtDenominator;
                numerator -= shapedSwapPaymentTimes_.Last() * System.Math.Exp(-shapedPaymentTime_ * x) * (1.0 - sqrtDenominator);

                return numerator / denominator;
            }

            private double functionZ(double x) => System.Math.Exp(-shapedPaymentTime_ * x) / (1.0 - discountRatio_ * System.Math.Exp(-shapedSwapPaymentTimes_.Last() * x));

            //* function describing the non-parallel shape of the curve shift*/
            private double shapeOfShift(double s)
            {
                var x = s - swapStartTime_;
                var meanReversion = meanReversion_.link.value();
                if (meanReversion > 0)
                {
                    return (1.0 - System.Math.Exp(-meanReversion * x)) / meanReversion;
                }

                return x;
            }
        }

        public static GFunction newGFunctionExactYield(CmsCoupon coupon) => new GFunctionExactYield(coupon);

        public static GFunction newGFunctionStandard(int q, double delta, int swapLength) => new GFunctionStandard(q, delta, swapLength);

        public static GFunction newGFunctionWithShifts(CmsCoupon coupon, Handle<Quote> meanReversion) => new GFunctionWithShifts(coupon, meanReversion);
    }
}
