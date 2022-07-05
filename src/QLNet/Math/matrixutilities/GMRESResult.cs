using System.Collections.Generic;

namespace QLNet.Math.MatrixUtilities
{
    public struct GMRESResult
    {
        public GMRESResult(List<double> e, Vector xx)
        {
            Errors = e;
            X = xx;
        }

        public List<double> Errors { get; set; }

        public Vector X { get; set; }
    }
}
