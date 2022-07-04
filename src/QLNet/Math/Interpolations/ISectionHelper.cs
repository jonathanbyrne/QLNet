namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public interface ISectionHelper
    {
        double value(double x);
        double primitive(double x);
        double fNext();
    }
}