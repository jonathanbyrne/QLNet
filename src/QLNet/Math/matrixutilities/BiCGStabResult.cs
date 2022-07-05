namespace QLNet.Math.MatrixUtilities
{
    public struct BiCGStabResult
    {
        public BiCGStabResult(int i, double e, Vector xx)
        {
            Iterations = i;
            Error = e;
            X = xx;
        }

        public double Error { get; set; }

        public int Iterations { get; set; }

        public Vector X { get; set; }
    }
}
