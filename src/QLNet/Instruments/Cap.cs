using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    /// <summary>
    ///     Concrete cap class
    ///     \ingroup instruments
    /// </summary>
    [PublicAPI]
    public class Cap : CapFloor
    {
        public Cap(List<CashFlow> floatingLeg, List<double> exerciseRates)
            : base(CapFloorType.Cap, floatingLeg, exerciseRates, new List<double>())
        {
        }
    }
}
