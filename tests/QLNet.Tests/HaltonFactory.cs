using QLNet.Math.randomnumbers;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public class HaltonFactory : IRNGFactory
{

    //typedef HaltonRsg generator_type;
    public HaltonFactory(bool randomStart, bool randomShift)
    {
        start_ = randomStart;
        shift_ = randomShift;
    }

    public IRNG make(int dim, ulong seed) => new HaltonRsg(dim, seed, start_, shift_);

    public string name()
    {
        var prefix = start_ ? "random-start " : "";
        if (shift_)
        {
            prefix += "random-shift ";
        }

        return prefix + "Halton";
    }

    private bool start_, shift_;
}