using System;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class FDDividendEngineMerton73 : FDDividendEngineBase
    {
        // required for generics
        public FDDividendEngineMerton73() { }

        public FDDividendEngineMerton73(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) { }

        public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent) => throw new NotImplementedException();
        // The value of the x axis is the NPV of the underlying minus the
        // value of the paid dividends.

        // Note that to get the PDE to work, I have to scale the values
        // and not shift them.  This means that the price curve assumes
        // that the dividends are scaled with the value of the underlying.
        //
        protected override void setGridLimits()
        {
            var paidDividends = 0.0;
            for (var i = 0; i < events_.Count; i++)
            {
                if (getDividendTime(i) >= 0.0)
                    paidDividends += getDiscountedDividend(i);
            }

            setGridLimits(process_.stateVariable().link.value() - paidDividends, getResidualTime());
            ensureStrikeInGrid();
        }

        // TODO:  Make this work for both fixed and scaled dividends
        protected override void executeIntermediateStep(int step)
        {
            var scaleFactor = getDiscountedDividend(step) / center_ + 1.0;
            sMin_ *= scaleFactor;
            sMax_ *= scaleFactor;
            center_ *= scaleFactor;

            intrinsicValues_.scaleGrid(scaleFactor);
            intrinsicValues_.sample(payoff_.value);
            prices_.scaleGrid(scaleFactor);
            initializeOperator();
            initializeModel();

            initializeStepCondition();
            stepCondition_.applyTo(prices_.values(), getDividendTime(step));
        }


    }
}