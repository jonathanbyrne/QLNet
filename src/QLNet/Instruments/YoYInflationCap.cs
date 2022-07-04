using System.Collections.Generic;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class YoYInflationCap : YoYInflationCapFloor
    {
        public YoYInflationCap(List<CashFlow> yoyLeg, List<double> exerciseRates)
            : base(CapFloorType.Cap, yoyLeg, exerciseRates, new List<double>())
        { }
    }
}