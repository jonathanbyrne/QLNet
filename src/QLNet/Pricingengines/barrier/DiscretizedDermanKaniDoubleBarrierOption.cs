using System.Collections.Generic;
using QLNet.Instruments;
using QLNet.Math;

namespace QLNet.Pricingengines.barrier
{
    [JetBrains.Annotations.PublicAPI] public class DiscretizedDermanKaniDoubleBarrierOption : DiscretizedAsset
    {
        public DiscretizedDermanKaniDoubleBarrierOption(DoubleBarrierOption.Arguments args,
            StochasticProcess process, TimeGrid grid = null)
        {
            unenhanced_ = new DiscretizedDoubleBarrierOption(args, process, grid);
        }

        public override void reset(int size)
        {
            unenhanced_.initialize(method(), time());
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        public override List<double> mandatoryTimes() => unenhanced_.mandatoryTimes();

        protected override void postAdjustValuesImpl()
        {
            unenhanced_.rollback(time());

            var grid = method().grid(time());
            unenhanced_.checkBarrier(values_, grid);   // compute payoffs
            adjustBarrier(values_, grid);
        }

        private void adjustBarrier(Vector optvalues, Vector grid)
        {
            var barrier_lo = unenhanced_.arguments().barrier_lo;
            var barrier_hi = unenhanced_.arguments().barrier_hi;
            var rebate = unenhanced_.arguments().rebate;
            switch (unenhanced_.arguments().barrierType)
            {
                case DoubleBarrier.Type.KnockIn:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier_lo && grid[j + 1] > barrier_lo)
                        {
                            // grid[j+1] above barrier_lo, grid[j] under (in),
                            // interpolate optvalues[j+1]
                            var ltob = barrier_lo - grid[j];
                            var htob = grid[j + 1] - barrier_lo;
                            var htol = grid[j + 1] - grid[j];
                            var u1 = unenhanced_.values()[j + 1];
                            var t1 = unenhanced_.vanilla()[j + 1];
                            optvalues[j + 1] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * t1 + htob.GetValueOrDefault() * u1) / htol); // derman std
                        }
                        else if (grid[j] < barrier_hi && grid[j + 1] >= barrier_hi)
                        {
                            // grid[j+1] above barrier_hi (in), grid[j] under,
                            // interpolate optvalues[j]
                            var ltob = barrier_hi - grid[j];
                            var htob = grid[j + 1] - barrier_hi;
                            var htol = grid[j + 1] - grid[j];
                            var u = unenhanced_.values()[j];
                            var t = unenhanced_.vanilla()[j];
                            optvalues[j] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * u + htob.GetValueOrDefault() * t) / htol); // derman std
                        }
                    }
                    break;
                case DoubleBarrier.Type.KnockOut:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier_lo && grid[j + 1] > barrier_lo)
                        {
                            // grid[j+1] above barrier_lo, grid[j] under (out),
                            // interpolate optvalues[j+1]
                            var a = (barrier_lo - grid[j]) * rebate;
                            var b = (grid[j + 1] - barrier_lo) * unenhanced_.values()[j + 1];
                            var c = grid[j + 1] - grid[j];
                            optvalues[j + 1] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                        else if (grid[j] < barrier_hi && grid[j + 1] >= barrier_hi)
                        {
                            // grid[j+1] above barrier_hi (out), grid[j] under,
                            // interpolate optvalues[j]
                            var a = (barrier_hi - grid[j]) * unenhanced_.values()[j];
                            var b = (grid[j + 1] - barrier_hi) * rebate;
                            var c = grid[j + 1] - grid[j];
                            optvalues[j] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                    }
                    break;
                default:
                    Utils.QL_FAIL("unsupported barrier ExerciseType");
                    break;
            }
        }
        private DiscretizedDoubleBarrierOption unenhanced_;
    }
}