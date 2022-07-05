using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public interface ISectionHelper
    {
        double fNext();

        double primitive(double x);

        double value(double x);
    }
}
