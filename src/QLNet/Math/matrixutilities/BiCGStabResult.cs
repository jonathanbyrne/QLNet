namespace QLNet.Math.matrixutilities
{
    public struct BiCGStabResult
    {
        public BiCGStabResult(int i, double e, Vector xx)
        {
            iterations = i;
            error = e;
            x = xx;
        }

        private int iterations;
        private double error;
        private Vector x;

        public int Iterations { get => iterations;
            set => iterations = value;
        }
        public double Error { get => error;
            set => error = value;
        }
        public Vector X { get => x;
            set => x = value;
        }
    }
}