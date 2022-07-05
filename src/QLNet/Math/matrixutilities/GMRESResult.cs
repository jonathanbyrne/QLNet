using System.Collections.Generic;

namespace QLNet.Math.matrixutilities
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
