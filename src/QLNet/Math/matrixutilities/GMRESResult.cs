using System.Collections.Generic;

namespace QLNet.Math.matrixutilities
{
    public struct GMRESResult
    {
        public GMRESResult(List<double> e, Vector xx)
        {
            errors = e;
            x = xx;
        }

        private List<double> errors;
        private Vector x;

        public List<double> Errors { get => errors;
            set => errors = value;
        }
        public Vector X { get => x;
            set => x = value;
        }
    }
}