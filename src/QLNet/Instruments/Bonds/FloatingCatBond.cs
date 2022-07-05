using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    /// <summary>
    ///     floating-rate cat bond (possibly capped and/or floored)
    /// </summary>
    [PublicAPI]
    public class FloatingCatBond : CatBond
    {
        public FloatingCatBond(int settlementDays,
            double faceAmount,
            Schedule schedule,
            IborIndex iborIndex,
            DayCounter paymentDayCounter,
            NotionalRisk notionalRisk,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            int fixingDays = 0,
            List<double> gearings = null,
            List<double> spreads = null,
            List<double?> caps = null,
            List<double?> floors = null,
            bool inArrears = false,
            double redemption = 100.0,
            Date issueDate = null)
            : base(settlementDays, schedule.calendar(), issueDate, notionalRisk)
        {
            maturityDate_ = schedule.endDate();

            cashflows_ = new IborLeg(schedule, iborIndex)
                .withFixingDays(fixingDays)
                .withGearings(gearings)
                .withSpreads(spreads)
                .withCaps(caps)
                .withFloors(floors)
                .inArrears(inArrears)
                .withPaymentDayCounter(paymentDayCounter)
                .withNotionals(faceAmount)
                .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new InitializedList<double>(1, redemption));

            Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
            Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

            iborIndex.registerWith(update);
        }

        public FloatingCatBond(int settlementDays,
            double faceAmount,
            Date startDate,
            Date maturityDate,
            Frequency couponFrequency,
            Calendar calendar,
            IborIndex iborIndex,
            DayCounter accrualDayCounter,
            NotionalRisk notionalRisk,
            BusinessDayConvention accrualConvention = BusinessDayConvention.Following,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            int fixingDays = 0,
            List<double> gearings = null,
            List<double> spreads = null,
            List<double?> caps = null,
            List<double?> floors = null,
            bool inArrears = false,
            double redemption = 100.0,
            Date issueDate = null,
            Date stubDate = null,
            DateGeneration.Rule rule = DateGeneration.Rule.Backward,
            bool endOfMonth = false)
            : base(settlementDays, calendar, issueDate, notionalRisk)
        {
            maturityDate_ = maturityDate;

            Date firstDate = null, nextToLastDate = null;
            switch (rule)
            {
                case DateGeneration.Rule.Backward:
                    firstDate = new Date();
                    nextToLastDate = stubDate;
                    break;
                case DateGeneration.Rule.Forward:
                    firstDate = stubDate;
                    nextToLastDate = new Date();
                    break;
                case DateGeneration.Rule.Zero:
                case DateGeneration.Rule.ThirdWednesday:
                case DateGeneration.Rule.Twentieth:
                case DateGeneration.Rule.TwentiethIMM:
                    Utils.QL_FAIL("stub date (" + stubDate + ") not allowed with " +
                                  rule + " DateGeneration.Rule");
                    break;
                default:
                    Utils.QL_FAIL("unknown DateGeneration::Rule (" + rule + ")");
                    break;
            }

            var schedule = new Schedule(startDate, maturityDate_, new Period(couponFrequency),
                calendar_, accrualConvention, accrualConvention,
                rule, endOfMonth, firstDate, nextToLastDate);

            cashflows_ = new IborLeg(schedule, iborIndex)
                .withFixingDays(fixingDays)
                .withGearings(gearings)
                .withSpreads(spreads)
                .withCaps(caps)
                .withFloors(floors)
                .inArrears(inArrears)
                .withPaymentDayCounter(accrualDayCounter)
                .withPaymentAdjustment(paymentConvention)
                .withNotionals(faceAmount);

            addRedemptionsToCashflows(new InitializedList<double>(1, redemption));

            Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
            Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

            iborIndex.registerWith(update);
        }
    }
}
