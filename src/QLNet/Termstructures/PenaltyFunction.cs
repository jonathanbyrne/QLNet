using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Termstructures
{
    [PublicAPI]
    public class PenaltyFunction<T, U> : CostFunction
        where T : Curve<U>, new()
        where U : TermStructure
    {
        private T curve_;
        private int initialIndex_;
        private int localisation_, start_, end_;
        private List<BootstrapHelper<U>> rateHelpers_;

        public PenaltyFunction(T curve, int initialIndex, List<BootstrapHelper<U>> rateHelpers, int start, int end)
        {
            curve_ = curve;
            initialIndex_ = initialIndex;
            rateHelpers_ = rateHelpers;
            start_ = start;
            end_ = end;
            localisation_ = end - start;
        }

        public override double value(Vector x)
        {
            x.ForEach((j, v) => curve_.updateGuess(curve_.data_, v, j + initialIndex_));

            curve_.interpolation_.update();

            var penalty = rateHelpers_.GetRange(start_, localisation_)
                .Aggregate(0.0, (acc, v) => System.Math.Abs(v.quoteError()));
            return penalty;
        }

        public override Vector values(Vector x)
        {
            x.ForEach((j, v) => curve_.updateGuess(curve_.data_, v, j + initialIndex_));

            curve_.interpolation_.update();

            var penalties = rateHelpers_.GetRange(start_, localisation_).Select(c => System.Math.Abs(c.quoteError())).ToList();
            return new Vector(penalties);
        }
    }
}
