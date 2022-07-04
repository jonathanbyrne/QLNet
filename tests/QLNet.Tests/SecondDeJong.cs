using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class SecondDeJong : CostFunction
{
    public override Vector values(Vector x)
    {
        var retVal = new Vector(x.size(), value(x));
        return retVal;
    }
    public override double value(Vector x) =>
        100.0 * (x[0] * x[0] - x[1]) * (x[0] * x[0] - x[1])
        + (1.0 - x[0]) * (1.0 - x[0]);
}