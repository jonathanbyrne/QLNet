using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class FirstDeJong : CostFunction
{
    public override Vector values(Vector x)
    {
        var retVal = new Vector(x.size(), value(x));
        return retVal;
    }
    public override double value(Vector x) => Vector.DotProduct(x, x);
}