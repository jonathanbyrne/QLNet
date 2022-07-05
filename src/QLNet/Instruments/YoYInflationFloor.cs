using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class YoYInflationFloor : YoYInflationCapFloor
    {
        public YoYInflationFloor(List<CashFlow> yoyLeg, List<double> exerciseRates)
            : base(CapFloorType.Floor, yoyLeg, new List<double>(), exerciseRates)
        {
        }
    }
}
