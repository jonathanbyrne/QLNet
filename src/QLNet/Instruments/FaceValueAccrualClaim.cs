using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class FaceValueAccrualClaim : Claim
    {
        private Bond referenceSecurity_;

        public FaceValueAccrualClaim(Bond referenceSecurity)
        {
            referenceSecurity_ = referenceSecurity;
            referenceSecurity.registerWith(update);
        }

        public override double amount(Date d, double notional, double recoveryRate)
        {
            var accrual = referenceSecurity_.accruedAmount(d) /
                          referenceSecurity_.notional(d);
            return notional * (1.0 - recoveryRate - accrual);
        }
    }
}
