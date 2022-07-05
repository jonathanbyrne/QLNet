using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class YoYInflationCollar : YoYInflationCapFloor
    {
        public YoYInflationCollar(List<CashFlow> yoyLeg, List<double> capRates, List<double> floorRates)
            : base(CapFloorType.Collar, yoyLeg, capRates, floorRates)
        {
        }
    }
}
