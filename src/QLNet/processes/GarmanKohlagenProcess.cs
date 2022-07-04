using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.processes
{
    [JetBrains.Annotations.PublicAPI] public class GarmanKohlagenProcess : GeneralizedBlackScholesProcess
    {
        public GarmanKohlagenProcess(Handle<Quote> x0,
            Handle<YieldTermStructure> foreignRiskFreeTS,
            Handle<YieldTermStructure> domesticRiskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS)
            : this(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, new EulerDiscretization())
        { }

        public GarmanKohlagenProcess(Handle<Quote> x0, Handle<YieldTermStructure> foreignRiskFreeTS,
            Handle<YieldTermStructure> domesticRiskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS, IDiscretization1D d)
            : base(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, d)
        { }

        public GarmanKohlagenProcess(Handle<Quote> x0, Handle<YieldTermStructure> foreignRiskFreeTS,
            Handle<YieldTermStructure> domesticRiskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            RelinkableHandle<LocalVolTermStructure> localVolTS,
            IDiscretization1D d = null)
            : base(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, localVolTS, d)
        { }

    }
}