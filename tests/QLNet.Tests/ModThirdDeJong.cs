using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class ModThirdDeJong : CostFunction
{
    public override Vector values(Vector x)
    {
        var retVal = new Vector(x.size(), value(x));
        return retVal;
    }
    public override double value(Vector x)
    {
        var fx = 0.0;
        for (var i = 0; i < x.size(); ++i)
        {
            fx += System.Math.Floor(x[i]) * System.Math.Floor(x[i]);
        }
        return fx;
    }
}