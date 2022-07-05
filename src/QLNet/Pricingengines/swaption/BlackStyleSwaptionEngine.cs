using System;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.PricingEngines.Swap;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.PricingEngines.swaption
{
    [PublicAPI]
    public class BlackStyleSwaptionEngine<Spec> : SwaptionEngine
        where Spec : ISwaptionEngineSpec, new()
    {
        public enum CashAnnuityModel
        {
            SwapRate,
            DiscountCurve
        }

        private Handle<YieldTermStructure> discountCurve_;
        private double? displacement_;
        private CashAnnuityModel model_;
        private Handle<SwaptionVolatilityStructure> vol_;

        public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            double vol,
            DayCounter dc = null,
            double? displacement = 0.0,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
        {
            dc = dc == null ? new Actual365Fixed() : dc;
            discountCurve_ = discountCurve;
            vol_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(0, new NullCalendar(),
                BusinessDayConvention.Following, vol, dc, new Spec().type(), displacement));
            model_ = model;
            displacement_ = displacement;
            discountCurve_.registerWith(update);
        }

        public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<Quote> vol,
            DayCounter dc = null,
            double? displacement = 0.0,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
        {
            dc = dc == null ? new Actual365Fixed() : dc;
            discountCurve_ = discountCurve;
            vol_ = new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(0, new NullCalendar(),
                BusinessDayConvention.Following, vol, dc, new Spec().type(), displacement));
            model_ = model;
            displacement_ = displacement;
            discountCurve_.registerWith(update);
            vol_.registerWith(update);
        }

        public BlackStyleSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<SwaptionVolatilityStructure> volatility,
            double? displacement = 0.0,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
        {
            discountCurve_ = discountCurve;
            vol_ = volatility;
            model_ = model;
            displacement_ = displacement;
            discountCurve_.registerWith(update);
            vol_.registerWith(update);
        }

        public override void calculate()
        {
            var exerciseDate = arguments_.exercise.date(0);

            // the part of the swap preceding exerciseDate should be truncated
            // to avoid taking into account unwanted cashflows
            // for the moment we add a check avoiding this situation
            var swap = arguments_.swap;

            var strike = swap.fixedRate;
            var fixedLeg = swap.fixedLeg();
            var firstCoupon = fixedLeg[0] as FixedRateCoupon;

            QLNet.Utils.QL_REQUIRE(firstCoupon != null, () => "wrong coupon ExerciseType");

            QLNet.Utils.QL_REQUIRE(firstCoupon.accrualStartDate() >= exerciseDate,
                () => "swap start (" + firstCoupon.accrualStartDate() + ") before exercise date ("
                      + exerciseDate + ") not supported in Black swaption engine");

            // using the forecasting curve
            swap.setPricingEngine(new DiscountingSwapEngine(swap.iborIndex().forwardingTermStructure()));
            var atmForward = swap.fairRate();

            // Volatilities are quoted for zero-spreaded swaps.
            // Therefore, any spread on the floating leg must be removed
            // with a corresponding correction on the fixed leg.
            if (swap.spread.IsNotEqual(0.0))
            {
                var correction = swap.spread * System.Math.Abs(swap.floatingLegBPS() / swap.fixedLegBPS());
                strike -= correction;
                atmForward -= correction;
                results_.additionalResults["spreadCorrection"] = correction;
            }
            else
            {
                results_.additionalResults["spreadCorrection"] = 0.0;
            }

            results_.additionalResults["strike"] = strike;
            results_.additionalResults["atmForward"] = atmForward;

            // using the discounting curve
            swap.setPricingEngine(new DiscountingSwapEngine(discountCurve_, false));
            double annuity = 0;
            if (arguments_.settlementType == Settlement.Type.Physical ||
                arguments_.settlementType == Settlement.Type.Cash &&
                arguments_.settlementMethod == Settlement.Method.CollateralizedCashPrice)
            {
                annuity = System.Math.Abs(swap.fixedLegBPS()) / Const.BASIS_POINT;
            }
            else if (arguments_.settlementType == Settlement.Type.Cash &&
                     arguments_.settlementMethod == Settlement.Method.ParYieldCurve)
            {
                var dayCount = firstCoupon.dayCounter();

                // we assume that the cash settlement date is equal
                // to the swap start date
                var discountDate = model_ == CashAnnuityModel.DiscountCurve
                    ? firstCoupon.accrualStartDate()
                    : discountCurve_.link.referenceDate();

                var fixedLegCashBPS =
                    CashFlows.bps(fixedLeg,
                        new InterestRate(atmForward, dayCount, Compounding.Compounded, Frequency.Annual), false,
                        discountDate);

                annuity = System.Math.Abs(fixedLegCashBPS / Const.BASIS_POINT) * discountCurve_.link.discount(discountDate);
            }
            else
            {
                QLNet.Utils.QL_FAIL("unknown settlement ExerciseType");
            }

            results_.additionalResults["annuity"] = annuity;

            var swapLength = vol_.link.swapLength(swap.floatingSchedule().dates().First(),
                swap.floatingSchedule().dates().Last());
            results_.additionalResults["swapLength"] = swapLength;

            var variance = vol_.link.blackVariance(exerciseDate,
                swapLength,
                strike);
            var displacement = displacement_ == null
                ? vol_.link.shift(exerciseDate, swapLength)
                : Convert.ToDouble(displacement_);
            var stdDev = System.Math.Sqrt(variance);
            results_.additionalResults["stdDev"] = stdDev;
            var w = arguments_.type == VanillaSwap.Type.Payer ? QLNet.Option.Type.Call : QLNet.Option.Type.Put;
            results_.value = new Spec().value(w, strike, atmForward, stdDev, annuity, displacement);

            var exerciseTime = vol_.link.timeFromReference(exerciseDate);
            results_.additionalResults["vega"] =
                new Spec().vega(strike, atmForward, stdDev, exerciseTime, annuity, displacement);
        }

        public Handle<YieldTermStructure> termStructure() => discountCurve_;

        public Handle<SwaptionVolatilityStructure> volatility() => vol_;
    }
}
