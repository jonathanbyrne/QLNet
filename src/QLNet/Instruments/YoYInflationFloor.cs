using System.Collections.Generic;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class YoYInflationFloor : YoYInflationCapFloor
    {
        public YoYInflationFloor(List<CashFlow> yoyLeg, List<double> exerciseRates)
            : base(CapFloorType.Floor, yoyLeg, new List<double>(), exerciseRates)
        { }
    }
}