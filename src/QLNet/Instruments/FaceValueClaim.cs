using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class FaceValueClaim : Claim
    {
        public override double amount(Date d, double notional, double recoveryRate) => notional * (1.0 - recoveryRate);
    }
}
