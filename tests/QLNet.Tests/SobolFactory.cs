using QLNet.Math.RandomNumbers;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public class SobolFactory : IRNGFactory
{
    //typedef SobolRsg generator_type;

    public SobolFactory(SobolRsg.DirectionIntegers unit)
    {
        unit_ = unit;
    }

    public IRNG make(int dim, ulong seed) => new SobolRsg(dim, seed, unit_);

    public string name()
    {
        var prefix = "";
        switch (unit_)
        {
            case SobolRsg.DirectionIntegers.Unit:
                prefix = "unit-initialized ";
                break;
            case SobolRsg.DirectionIntegers.Jaeckel:
                prefix = "Jäckel-initialized ";
                break;
            case SobolRsg.DirectionIntegers.SobolLevitan:
                prefix = "SobolLevitan-initialized ";
                break;
            case SobolRsg.DirectionIntegers.SobolLevitanLemieux:
                prefix = "SobolLevitanLemieux-initialized ";
                break;
            case SobolRsg.DirectionIntegers.Kuo:
                prefix = "Kuo";
                break;
            case SobolRsg.DirectionIntegers.Kuo2:
                prefix = "Kuo2";
                break;
            case SobolRsg.DirectionIntegers.Kuo3:
                prefix = "Kuo3";
                break;
            default:
                QAssert.Fail("unknown direction integers");
                break;
        }
        return prefix + "Sobol sequences: ";

    }

    private SobolRsg.DirectionIntegers unit_;
}