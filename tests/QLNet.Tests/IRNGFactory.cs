using QLNet.Math.RandomNumbers;

namespace QLNet.Tests;

[JetBrains.Annotations.PublicAPI] public interface IRNGFactory
{
    string name();
    IRNG make(int dim, ulong seed);
}