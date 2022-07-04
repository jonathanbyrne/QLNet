using QLNet.Time;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class FaceValueClaim : Claim
    {
        public override double amount(Date d, double notional, double recoveryRate) => notional * (1.0 - recoveryRate);
    }
}