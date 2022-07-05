using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    /// <summary>
    ///     Concrete collar class
    ///     \ingroup instruments
    /// </summary>
    [PublicAPI]
    public class Collar : CapFloor
    {
        public Collar(List<CashFlow> floatingLeg, List<double> capRates, List<double> floorRates)
            : base(CapFloorType.Collar, floatingLeg, capRates, floorRates)
        {
        }
    }
}
