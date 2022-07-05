using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Patterns;

namespace QLNet.Models
{
    [PublicAPI]
    public interface IAffineModel : IObservable
    {
        double discount(double t);

        double discountBond(double now, double maturity, Vector factors);

        double discountBondOption(Option.Type type, double strike, double maturity, double bondMaturity);
    }
}
