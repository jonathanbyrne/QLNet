using System.Collections.Generic;

namespace QLNet.Instruments
{
    /// <summary>
    /// Concrete cap class
    /// \ingroup instruments
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class Cap : CapFloor
    {
        public Cap(List<CashFlow> floatingLeg, List<double> exerciseRates)
            : base(CapFloorType.Cap, floatingLeg, exerciseRates, new List<double>()) { }
    }
}