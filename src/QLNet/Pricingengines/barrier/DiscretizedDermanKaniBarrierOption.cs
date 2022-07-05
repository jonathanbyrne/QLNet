using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Math;

namespace QLNet.PricingEngines.barrier
{
    [PublicAPI]
    public class DiscretizedDermanKaniBarrierOption : DiscretizedAsset
    {
        private DiscretizedBarrierOption unenhanced_;

        public DiscretizedDermanKaniBarrierOption(BarrierOption.Arguments args,
            StochasticProcess process, TimeGrid grid = null)
        {
            unenhanced_ = new DiscretizedBarrierOption(args, process, grid);
        }

        public override List<double> mandatoryTimes() => unenhanced_.mandatoryTimes();

        public override void reset(int size)
        {
            unenhanced_.initialize(method(), time());
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        protected override void postAdjustValuesImpl()
        {
            unenhanced_.rollback(time());

            var grid = method().grid(time());
            adjustBarrier(values_, grid);
            unenhanced_.checkBarrier(values_, grid); // compute payoffs
        }

        private void adjustBarrier(Vector optvalues, Vector grid)
        {
            var barrier = unenhanced_.arguments().barrier;
            var rebate = unenhanced_.arguments().rebate;
            switch (unenhanced_.arguments().barrierType)
            {
                case Barrier.Type.DownIn:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier && grid[j + 1] > barrier)
                        {
                            var ltob = barrier - grid[j];
                            var htob = grid[j + 1] - barrier;
                            var htol = grid[j + 1] - grid[j];
                            var u1 = unenhanced_.values()[j + 1];
                            var t1 = unenhanced_.vanilla()[j + 1];
                            optvalues[j + 1] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * t1 + htob.GetValueOrDefault() * u1) / htol);
                        }
                    }

                    break;
                case Barrier.Type.DownOut:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] <= barrier && grid[j + 1] > barrier)
                        {
                            var a = (barrier - grid[j]) * rebate;
                            var b = (grid[j + 1] - barrier) * unenhanced_.values()[j + 1];
                            var c = grid[j + 1] - grid[j];
                            optvalues[j + 1] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                    }

                    break;
                case Barrier.Type.UpIn:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] < barrier && grid[j + 1] >= barrier)
                        {
                            var ltob = barrier - grid[j];
                            var htob = grid[j + 1] - barrier;
                            var htol = grid[j + 1] - grid[j];
                            var u = unenhanced_.values()[j];
                            var t = unenhanced_.vanilla()[j];
                            optvalues[j] = System.Math.Max(0.0, (ltob.GetValueOrDefault() * u + htob.GetValueOrDefault() * t) / htol); // derman std
                        }
                    }

                    break;
                case Barrier.Type.UpOut:
                    for (var j = 0; j < optvalues.size() - 1; ++j)
                    {
                        if (grid[j] < barrier && grid[j + 1] >= barrier)
                        {
                            var a = (barrier - grid[j]) * unenhanced_.values()[j];
                            var b = (grid[j + 1] - barrier) * rebate;
                            var c = grid[j + 1] - grid[j];
                            optvalues[j] = System.Math.Max(0.0, (a.GetValueOrDefault() + b.GetValueOrDefault()) / c);
                        }
                    }

                    break;
            }
        }
    }
}
