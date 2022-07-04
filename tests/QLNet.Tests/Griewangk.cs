using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class Griewangk : CostFunction
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
            fx += x[i] * x[i] / 4000.0;
        }
        var p = 1.0;
        for (var i = 0; i < x.size(); ++i)
        {
            p *= System.Math.Cos(x[i] / System.Math.Sqrt(i + 1.0));
        }
        return fx - p + 1.0;
    }
}