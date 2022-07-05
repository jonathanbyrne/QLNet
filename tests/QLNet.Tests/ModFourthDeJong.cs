using QLNet.Math;
using QLNet.Math.Optimization;
using QLNet.Math.RandomNumbers;

namespace QLNet.Tests;

class ModFourthDeJong : CostFunction
{
    public ModFourthDeJong()
    {
        uniformRng_ = new MersenneTwisterUniformRng(4711);
    }

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
            fx += (i + 1.0) * System.Math.Pow(x[i], 4.0) + uniformRng_.nextReal();
        }
        return fx;
    }
    MersenneTwisterUniformRng uniformRng_;
}