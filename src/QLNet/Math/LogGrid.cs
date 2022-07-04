namespace QLNet.Math
{
    [JetBrains.Annotations.PublicAPI] public class LogGrid : TransformedGrid
    {
        public LogGrid(Vector grid) : base(grid, System.Math.Log) { }

        public Vector logGridArray() => transformedGridArray();

        public double logGrid(int i) => transformedGrid(i);
    }
}