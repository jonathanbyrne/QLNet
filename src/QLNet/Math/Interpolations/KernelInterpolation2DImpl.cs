using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class KernelInterpolation2DImpl<Kernel> : Interpolation2D.templateImpl where Kernel : IKernelFunction
    {
        private Vector alphaVec_, yVec_;
        private double invPrec_;
        private Kernel kernel_;
        private Matrix M_;
        private int xySize_; // xSize_,ySize_,

        public KernelInterpolation2DImpl(List<double> xBegin, int size, List<double> yBegin, int ySize,
            Matrix zData, Kernel kernel)
            : base(xBegin, size, yBegin, ySize, zData)
        {
            xSize_ = size;
            ySize_ = yBegin.Count;
            xySize_ = xSize_ * ySize_;
            invPrec_ = 1.0e-10;
            alphaVec_ = new Vector(xySize_);
            yVec_ = new Vector(xySize_);
            M_ = new Matrix(xySize_, xySize_);
            kernel_ = kernel;

            QLNet.Utils.QL_REQUIRE(zData.rows() == xSize_, () =>
                "Z value matrix has wrong number of rows");
            QLNet.Utils.QL_REQUIRE(zData.columns() == ySize_, () =>
                "Z value matrix has wrong number of columns");
        }

        public override void calculate()
        {
            updateAlphaVec();
        }

        // the calculation will solve y=M*a for a.  Due to
        // singularity or rounding errors the recalculation
        // M*a may not give y. Here, a failure will be thrown if
        // |M*a-y|>=invPrec_
        public void setInverseResultPrecision(double invPrec)
        {
            invPrec_ = invPrec;
        }

        public override double value(double x1, double x2)
        {
            var res = 0.0;

            Vector X = new Vector(2), Xn = new Vector(2);
            X[0] = x1;
            X[1] = x2;

            var cnt = 0; // counter

            for (var j = 0; j < ySize_; ++j)
            {
                for (var i = 0; i < xSize_; ++i)
                {
                    Xn[0] = xBegin_[i];
                    Xn[1] = yBegin_[j];
                    res += alphaVec_[cnt] * kernelAbs(X, Xn);
                    cnt++;
                }
            }

            return res / gammaFunc(X);
        }

        private double gammaFunc(Vector X)
        {
            var res = 0.0;
            var Xn = new Vector(X.size());

            for (var j = 0; j < ySize_; ++j)
            {
                for (var i = 0; i < xSize_; ++i)
                {
                    Xn[0] = xBegin_[i];
                    Xn[1] = yBegin_[j];
                    res += kernelAbs(X, Xn);
                }
            }

            return res;
        }

        // returns K(||X-Y||) where X,Y are vectors
        private double kernelAbs(Vector X, Vector Y) => kernel_.value(Vector.Norm2(X - Y));

        private void updateAlphaVec()
        {
            // Function calculates the alpha vector with given
            // fixed pillars+values

            Vector Xk = new Vector(2), Xn = new Vector(2);

            int rowCnt = 0, colCnt = 0;
            var tmpVar = 0.0;

            // write y-vector and M-Matrix
            for (var j = 0; j < ySize_; ++j)
            {
                for (var i = 0; i < xSize_; ++i)
                {
                    yVec_[rowCnt] = zData_[i, j];
                    // calculate X_k
                    Xk[0] = xBegin_[i];
                    Xk[1] = yBegin_[j];

                    tmpVar = 1 / gammaFunc(Xk);
                    colCnt = 0;

                    for (var jM = 0; jM < ySize_; ++jM)
                    {
                        for (var iM = 0; iM < xSize_; ++iM)
                        {
                            Xn[0] = xBegin_[iM];
                            Xn[1] = yBegin_[jM];
                            M_[rowCnt, colCnt] = kernelAbs(Xk, Xn) * tmpVar;
                            colCnt++; // increase column counter
                        } // end iM
                    } // end jM

                    rowCnt++; // increase row counter
                } // end i
            } // end j

            alphaVec_ = MatrixUtilities.MatrixUtilities.qrSolve(M_, yVec_);

            // check if inversion worked up to a reasonable precision.
            // I've chosen not to check determinant(M_)!=0 before solving

            var diffVec = Vector.Abs(M_ * alphaVec_ - yVec_);
            for (var i = 0; i < diffVec.size(); ++i)
            {
                QLNet.Utils.QL_REQUIRE(diffVec[i] < invPrec_, () =>
                    "inversion failed in 2d kernel interpolation");
            }
        }
    }
}
