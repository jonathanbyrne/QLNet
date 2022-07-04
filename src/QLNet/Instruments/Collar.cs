using System.Collections.Generic;

namespace QLNet.Instruments
{
    /// <summary>
    /// Concrete collar class
    /// \ingroup instruments
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class Collar : CapFloor
    {
        public Collar(List<CashFlow> floatingLeg, List<double> capRates, List<double> floorRates)
            : base(CapFloorType.Collar, floatingLeg, capRates, floorRates) { }
    }
}