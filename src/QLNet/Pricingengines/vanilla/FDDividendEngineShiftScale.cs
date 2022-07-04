﻿using System;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class FDDividendEngineShiftScale : FDDividendEngineBase
    {
        public FDDividendEngineShiftScale(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) { }

        public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent) => throw new NotImplementedException();

        protected override void setGridLimits()
        {
            var underlying = process_.stateVariable().link.value();
            for (var i = 0; i < events_.Count; i++)
            {
                var dividend = events_[i] as Dividend;
                if (dividend == null)
                    continue;
                if (getDividendTime(i) < 0.0)
                    continue;
                underlying -= dividend.amount(underlying);
            }

            setGridLimits(underlying, getResidualTime());
            ensureStrikeInGrid();
        }

        protected override void executeIntermediateStep(int step)
        {
            var dividend = events_[step] as Dividend;
            if (dividend == null)
                return;
            var adder = new DividendAdder(dividend);
            sMin_ = adder.value(sMin_);
            sMax_ = adder.value(sMax_);
            center_ = adder.value(center_);
            intrinsicValues_.transformGrid(adder.value);

            intrinsicValues_.sample(payoff_.value);
            prices_.transformGrid(adder.value);

            initializeOperator();
            initializeModel();

            initializeStepCondition();
            stepCondition_.applyTo(prices_.values(), getDividendTime(step));
        }

        class DividendAdder
        {
            private Dividend dividend;

            public DividendAdder(Dividend d)
            {
                dividend = d;
            }
            public double value(double x) => x + dividend.amount(x);
        }
    }
}