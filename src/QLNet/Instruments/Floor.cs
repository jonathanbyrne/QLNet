using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    /// <summary>
    ///     Concrete floor class
    ///     \ingroup instruments
    /// </summary>
    [PublicAPI]
    public class Floor : CapFloor
    {
        public Floor(List<CashFlow> floatingLeg, List<double> exerciseRates)
            : base(CapFloorType.Floor, floatingLeg, new List<double>(), exerciseRates)
        {
        }
    }
}
