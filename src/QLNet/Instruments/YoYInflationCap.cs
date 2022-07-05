using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class YoYInflationCap : YoYInflationCapFloor
    {
        public YoYInflationCap(List<CashFlow> yoyLeg, List<double> exerciseRates)
            : base(CapFloorType.Cap, yoyLeg, exerciseRates, new List<double>())
        {
        }
    }
}
