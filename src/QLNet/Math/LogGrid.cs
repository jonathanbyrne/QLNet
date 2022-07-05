using JetBrains.Annotations;

namespace QLNet.Math
{
    [PublicAPI]
    public class LogGrid : TransformedGrid
    {
        public LogGrid(Vector grid) : base(grid, System.Math.Log)
        {
        }

        public double logGrid(int i) => transformedGrid(i);

        public Vector logGridArray() => transformedGridArray();
    }
}
