using System.Collections.Generic;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class YoYInflationCollar : YoYInflationCapFloor
    {
        public YoYInflationCollar(List<CashFlow> yoyLeg, List<double> capRates, List<double> floorRates)
            : base(CapFloorType.Collar, yoyLeg, capRates, floorRates) { }
    }
}