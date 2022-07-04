using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class KernelInterpolationImpl<Kernel> : Interpolation.templateImpl where Kernel : IKernelFunction
    {
        public KernelInterpolationImpl(List<double> xBegin, int size, List<double> yBegin, Kernel kernel)
            : base(xBegin, size, yBegin)
        {
            xSize_ = size;
            invPrec_ = 1.0e-7;
            M_ = new Matrix(xSize_, xSize_);
            alphaVec_ = new Vector(xSize_);
            yVec_ = new Vector(xSize_);
            kernel_ = kernel;
        }

        public override void update()
        {
            updateAlphaVec();
        }

        public override double value(double x)
        {
            var res = 0.0;
            for (var i = 0; i < xSize_; ++i)
            {
                res += alphaVec_[i] * kernelAbs(x, xBegin_[i]);
            }
            return res / gammaFunc(x);
        }

        public override double primitive(double d)
        {
            Utils.QL_FAIL("Primitive calculation not implemented for kernel interpolation");
            return 0;
        }

        public override double derivative(double d)
        {
            Utils.QL_FAIL("First derivative calculation not implemented for kernel interpolation");
            return 0;
        }

        public override double secondDerivative(double d)
        {
            Utils.QL_FAIL("Second derivative calculation not implemented for kernel interpolation");
            return 0;
        }

        // the calculation will solve y=M*a for a.  Due to
        // singularity or rounding errors the recalculation
        // M*a may not give y. Here, a failure will be thrown if
        // |M*a-y|>=invPrec_

        public void setInverseResultPrecision(double invPrec) { invPrec_ = invPrec; }

        private double kernelAbs(double x1, double x2) => kernel_.value(System.Math.Abs(x1 - x2));

        private double gammaFunc(double x)
        {
            var res = 0.0;

            for (var i = 0; i < xSize_; ++i)
            {
                res += kernelAbs(x, xBegin_[i]);
            }
            return res;
        }

        private void updateAlphaVec()
        {
            // Function calculates the alpha vector with given
            // fixed pillars+values

            // Write Matrix M
            var tmp = 0.0;

            for (var rowIt = 0; rowIt < xSize_; ++rowIt)
            {

                yVec_[rowIt] = yBegin_[rowIt];
                tmp = 1.0 / gammaFunc(xBegin_[rowIt]);

                for (var colIt = 0; colIt < xSize_; ++colIt)
                {
                    M_[rowIt, colIt] = kernelAbs(xBegin_[rowIt],
                        xBegin_[colIt]) * tmp;
                }
            }

            // Solve y=M*\alpha for \alpha
            alphaVec_ = MatrixUtilities.qrSolve(M_, yVec_);

            // check if inversion worked up to a reasonable precision.
            // I've chosen not to check determinant(M_)!=0 before solving
            var test = M_ * alphaVec_;
            var diffVec = Vector.Abs(M_ * alphaVec_ - yVec_);

            for (var i = 0; i < diffVec.size(); ++i)
            {
                Utils.QL_REQUIRE(diffVec[i] < invPrec_, () =>
                    "Inversion failed in 1d kernel interpolation");
            }

        }

        private int xSize_;
        private double invPrec_;
        private Matrix M_;
        private Vector alphaVec_, yVec_;
        private Kernel kernel_;

    }
}