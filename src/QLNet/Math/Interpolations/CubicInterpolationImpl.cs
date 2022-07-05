using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Methods.Finitedifferences;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class CubicInterpolationImpl : Interpolation.templateImpl
    {
        private CubicInterpolation.DerivativeApprox da_;
        private CubicInterpolation.BoundaryCondition leftType_, rightType_;
        private double leftValue_, rightValue_;
        private bool monotonic_;
        private List<bool> monotonicityAdjustments_;

        public CubicInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
            CubicInterpolation.DerivativeApprox da,
            bool monotonic,
            CubicInterpolation.BoundaryCondition leftCondition,
            double leftConditionValue,
            CubicInterpolation.BoundaryCondition rightCondition,
            double rightConditionValue)
            : base(xBegin, size, yBegin)
        {
            da_ = da;
            monotonic_ = monotonic;
            leftType_ = leftCondition;
            rightType_ = rightCondition;
            leftValue_ = leftConditionValue;
            rightValue_ = rightConditionValue;

            // coefficients
            primitiveConst_ = new InitializedList<double>(size - 1);
            a_ = new InitializedList<double>(size - 1);
            b_ = new InitializedList<double>(size - 1);
            c_ = new InitializedList<double>(size - 1);
            monotonicityAdjustments_ = new InitializedList<bool>(size);

            if (leftType_ == CubicInterpolation.BoundaryCondition.Lagrange
                || rightType_ == CubicInterpolation.BoundaryCondition.Lagrange)
            {
                QLNet.Utils.QL_REQUIRE(size >= 4, () =>
                    "Lagrange boundary condition requires at least " +
                    "4 points (" + size + " are given)");
            }
        }

        public List<double> a_ { get; set; }

        public List<double> b_ { get; set; }

        public List<double> c_ { get; set; }

        public List<double> primitiveConst_ { get; set; }

        public override double derivative(double x)
        {
            var j = locate(x);
            var dx = x - xBegin_[j];
            return a_[j] + (2.0 * b_[j] + 3.0 * c_[j] * dx) * dx;
        }

        public override double primitive(double x)
        {
            var j = locate(x);
            var dx = x - xBegin_[j];
            return primitiveConst_[j]
                   + dx * (yBegin_[j] + dx * (a_[j] / 2.0
                                              + dx * (b_[j] / 3.0 + dx * c_[j] / 4.0)));
        }

        public override double secondDerivative(double x)
        {
            var j = locate(x);
            var dx = x - xBegin_[j];
            return 2.0 * b_[j] + 6.0 * c_[j] * dx;
        }

        public override void update()
        {
            var tmp = new Vector(size_);
            List<double> dx = new InitializedList<double>(size_ - 1),
                S = new InitializedList<double>(size_ - 1);

            for (var i = 0; i < size_ - 1; ++i)
            {
                dx[i] = xBegin_[i + 1] - xBegin_[i];
                S[i] = (yBegin_[i + 1] - yBegin_[i]) / dx[i];
            }

            // first derivative approximation
            if (da_ == CubicInterpolation.DerivativeApprox.Spline)
            {
                var L = new TridiagonalOperator(size_);
                for (var i = 1; i < size_ - 1; ++i)
                {
                    L.setMidRow(i, dx[i], 2.0 * (dx[i] + dx[i - 1]), dx[i - 1]);
                    tmp[i] = 3.0 * (dx[i] * S[i - 1] + dx[i - 1] * S[i]);
                }

                // left boundary condition
                switch (leftType_)
                {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        // ignoring end condition value
                        L.setFirstRow(dx[1] * (dx[1] + dx[0]), (dx[0] + dx[1]) * (dx[0] + dx[1]));
                        tmp[0] = S[0] * dx[1] * (2.0 * dx[1] + 3.0 * dx[0]) + S[1] * dx[0] * dx[0];
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        L.setFirstRow(1.0, 0.0);
                        tmp[0] = leftValue_;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        L.setFirstRow(2.0, 1.0);
                        tmp[0] = 3.0 * S[0] - leftValue_ * dx[0] / 2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                        // ignoring end condition value
                        throw new NotImplementedException("this end condition is not implemented yet");
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        L.setFirstRow(1.0, 0.0);
                        tmp[0] = cubicInterpolatingPolynomialDerivative(
                            xBegin_[0], xBegin_[1],
                            xBegin_[2], xBegin_[3],
                            yBegin_[0], yBegin_[1],
                            yBegin_[2], yBegin_[3],
                            xBegin_[0]);
                        break;
                    default:
                        throw new ArgumentException("unknown end condition");
                }

                // right boundary condition
                switch (rightType_)
                {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        // ignoring end condition value
                        L.setLastRow(-(dx[size_ - 2] + dx[size_ - 3]) * (dx[size_ - 2] + dx[size_ - 3]),
                            -dx[size_ - 3] * (dx[size_ - 3] + dx[size_ - 2]));
                        tmp[size_ - 1] = -S[size_ - 3] * dx[size_ - 2] * dx[size_ - 2] -
                                         S[size_ - 2] * dx[size_ - 3] * (3.0 * dx[size_ - 2] + 2.0 * dx[size_ - 3]);
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        L.setLastRow(0.0, 1.0);
                        tmp[size_ - 1] = rightValue_;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        L.setLastRow(1.0, 2.0);
                        tmp[size_ - 1] = 3.0 * S[size_ - 2] + rightValue_ * dx[size_ - 2] / 2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                        // ignoring end condition value
                        throw new NotImplementedException("this end condition is not implemented yet");
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        L.setLastRow(0.0, 1.0);
                        tmp[size_ - 1] = cubicInterpolatingPolynomialDerivative(
                            xBegin_[size_ - 4], xBegin_[size_ - 3],
                            xBegin_[size_ - 2], xBegin_[size_ - 1],
                            yBegin_[size_ - 4], yBegin_[size_ - 3],
                            yBegin_[size_ - 2], yBegin_[size_ - 1],
                            xBegin_[size_ - 1]);
                        break;
                    default:
                        throw new ArgumentException("unknown end condition");
                }

                // solve the system
                tmp = L.solveFor(tmp);
            }
            else if (da_ == CubicInterpolation.DerivativeApprox.SplineOM1)
            {
                var T_ = new Matrix(size_ - 2, size_, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    T_[i, i] = dx[i] / 6.0;
                    T_[i, i + 1] = (dx[i + 1] + dx[i]) / 3.0;
                    T_[i, i + 2] = dx[i + 1] / 6.0;
                }

                var S_ = new Matrix(size_ - 2, size_, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    S_[i, i] = 1.0 / dx[i];
                    S_[i, i + 1] = -(1.0 / dx[i + 1] + 1.0 / dx[i]);
                    S_[i, i + 2] = 1.0 / dx[i + 1];
                }

                var Up_ = new Matrix(size_, 2, 0.0);
                Up_[0, 0] = 1;
                Up_[size_ - 1, 1] = 1;
                var Us_ = new Matrix(size_, size_ - 2, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    Us_[i + 1, i] = 1;
                }

                var Z_ = Us_ * Matrix.inverse(T_ * Us_);
                var I_ = new Matrix(size_, size_, 0.0);
                for (var i = 0; i < size_; ++i)
                {
                    I_[i, i] = 1;
                }

                var V_ = (I_ - Z_ * T_) * Up_;
                var W_ = Z_ * S_;
                var Q_ = new Matrix(size_, size_, 0.0);
                Q_[0, 0] = 1.0 / (size_ - 1) * dx[0] * dx[0] * dx[0];
                Q_[0, 1] = 7.0 / 8 * 1.0 / (size_ - 1) * dx[0] * dx[0] * dx[0];
                for (var i = 1; i < size_ - 1; ++i)
                {
                    Q_[i, i - 1] = 7.0 / 8 * 1.0 / (size_ - 1) * dx[i - 1] * dx[i - 1] * dx[i - 1];
                    Q_[i, i] = 1.0 / (size_ - 1) * dx[i] * dx[i] * dx[i] + 1.0 / (size_ - 1) * dx[i - 1] * dx[i - 1] * dx[i - 1];
                    Q_[i, i + 1] = 7.0 / 8 * 1.0 / (size_ - 1) * dx[i] * dx[i] * dx[i];
                }

                Q_[size_ - 1, size_ - 2] = 7.0 / 8 * 1.0 / (size_ - 1) * dx[size_ - 2] * dx[size_ - 2] * dx[size_ - 2];
                Q_[size_ - 1, size_ - 1] = 1.0 / (size_ - 1) * dx[size_ - 2] * dx[size_ - 2] * dx[size_ - 2];
                var J_ = (I_ - V_ * Matrix.inverse(Matrix.transpose(V_) * Q_ * V_) * Matrix.transpose(V_) * Q_) * W_;
                var Y_ = new Vector(size_);
                for (var i = 0; i < size_; ++i)
                {
                    Y_[i] = yBegin_[i];
                }

                var D_ = J_ * Y_;
                for (var i = 0; i < size_ - 1; ++i)
                {
                    tmp[i] = (Y_[i + 1] - Y_[i]) / dx[i] - (2.0 * D_[i] + D_[i + 1]) * dx[i] / 6.0;
                }

                tmp[size_ - 1] = tmp[size_ - 2] + D_[size_ - 2] * dx[size_ - 2] + (D_[size_ - 1] - D_[size_ - 2]) * dx[size_ - 2] / 2.0;
            }
            else if (da_ == CubicInterpolation.DerivativeApprox.SplineOM2)
            {
                var T_ = new Matrix(size_ - 2, size_, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    T_[i, i] = dx[i] / 6.0;
                    T_[i, i + 1] = (dx[i] + dx[i + 1]) / 3.0;
                    T_[i, i + 2] = dx[i + 1] / 6.0;
                }

                var S_ = new Matrix(size_ - 2, size_, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    S_[i, i] = 1.0 / dx[i];
                    S_[i, i + 1] = -(1.0 / dx[i + 1] + 1.0 / dx[i]);
                    S_[i, i + 2] = 1.0 / dx[i + 1];
                }

                var Up_ = new Matrix(size_, 2, 0.0);
                Up_[0, 0] = 1;
                Up_[size_ - 1, 1] = 1;
                var Us_ = new Matrix(size_, size_ - 2, 0.0);
                for (var i = 0; i < size_ - 2; ++i)
                {
                    Us_[i + 1, i] = 1;
                }

                var Z_ = Us_ * Matrix.inverse(T_ * Us_);
                var I_ = new Matrix(size_, size_, 0.0);
                for (var i = 0; i < size_; ++i)
                {
                    I_[i, i] = 1;
                }

                var V_ = (I_ - Z_ * T_) * Up_;
                var W_ = Z_ * S_;
                var Q_ = new Matrix(size_, size_, 0.0);
                Q_[0, 0] = 1.0 / (size_ - 1) * dx[0];
                Q_[0, 1] = 1.0 / 2 * 1.0 / (size_ - 1) * dx[0];
                for (var i = 1; i < size_ - 1; ++i)
                {
                    Q_[i, i - 1] = 1.0 / 2 * 1.0 / (size_ - 1) * dx[i - 1];
                    Q_[i, i] = 1.0 / (size_ - 1) * dx[i] + 1.0 / (size_ - 1) * dx[i - 1];
                    Q_[i, i + 1] = 1.0 / 2 * 1.0 / (size_ - 1) * dx[i];
                }

                Q_[size_ - 1, size_ - 2] = 1.0 / 2 * 1.0 / (size_ - 1) * dx[size_ - 2];
                Q_[size_ - 1, size_ - 1] = 1.0 / (size_ - 1) * dx[size_ - 2];
                var J_ = (I_ - V_ * Matrix.inverse(Matrix.transpose(V_) * Q_ * V_) * Matrix.transpose(V_) * Q_) * W_;
                var Y_ = new Vector(size_);
                for (var i = 0; i < size_; ++i)
                {
                    Y_[i] = yBegin_[i];
                }

                var D_ = J_ * Y_;
                for (var i = 0; i < size_ - 1; ++i)
                {
                    tmp[i] = (Y_[i + 1] - Y_[i]) / dx[i] - (2.0 * D_[i] + D_[i + 1]) * dx[i] / 6.0;
                }

                tmp[size_ - 1] = tmp[size_ - 2] + D_[size_ - 2] * dx[size_ - 2] + (D_[size_ - 1] - D_[size_ - 2]) * dx[size_ - 2] / 2.0;
            }
            else
            {
                // local schemes
                if (size_ == 2)
                {
                    tmp[0] = tmp[1] = S[0];
                }
                else
                {
                    switch (da_)
                    {
                        case CubicInterpolation.DerivativeApprox.FourthOrder:
                            throw new NotImplementedException("FourthOrder not implemented yet");
                        case CubicInterpolation.DerivativeApprox.Parabolic:
                            // intermediate points
                            for (var i = 1; i < size_ - 1; ++i)
                            {
                                tmp[i] = (dx[i - 1] * S[i] + dx[i] * S[i - 1]) / (dx[i] + dx[i - 1]);
                            }

                            // end points
                            tmp[0] = ((2.0 * dx[0] + dx[1]) * S[0] - dx[0] * S[1]) / (dx[0] + dx[1]);
                            tmp[size_ - 1] = ((2.0 * dx[size_ - 2] + dx[size_ - 3]) * S[size_ - 2] -
                                              dx[size_ - 2] * S[size_ - 3]) / (dx[size_ - 2] + dx[size_ - 3]);
                            break;
                        case CubicInterpolation.DerivativeApprox.FritschButland:
                            // intermediate points
                            for (var i = 1; i < size_ - 1; ++i)
                            {
                                var Smin = System.Math.Min(S[i - 1], S[i]);
                                var Smax = System.Math.Max(S[i - 1], S[i]);
                                tmp[i] = 3.0 * Smin * Smax / (Smax + 2.0 * Smin);
                            }

                            // end points
                            tmp[0] = ((2.0 * dx[0] + dx[1]) * S[0] - dx[0] * S[1]) / (dx[0] + dx[1]);
                            tmp[size_ - 1] = ((2.0 * dx[size_ - 2] + dx[size_ - 3]) * S[size_ - 2] -
                                              dx[size_ - 2] * S[size_ - 3]) / (dx[size_ - 2] + dx[size_ - 3]);
                            break;
                        case CubicInterpolation.DerivativeApprox.Akima:
                            tmp[0] = (System.Math.Abs(S[1] - S[0]) * 2 * S[0] * S[1] +
                                      System.Math.Abs(2 * S[0] * S[1] - 4 * S[0] * S[0] * S[1]) * S[0]) /
                                     (System.Math.Abs(S[1] - S[0]) + System.Math.Abs(2 * S[0] * S[1] - 4 * S[0] * S[0] * S[1]));
                            tmp[1] = (System.Math.Abs(S[2] - S[1]) * S[0] + System.Math.Abs(S[0] - 2 * S[0] * S[1]) * S[1]) /
                                     (System.Math.Abs(S[2] - S[1]) + System.Math.Abs(S[0] - 2 * S[0] * S[1]));
                            for (var i = 2; i < size_ - 2; ++i)
                            {
                                if (S[i - 2].IsEqual(S[i - 1]) && S[i].IsNotEqual(S[i + 1]))
                                {
                                    tmp[i] = S[i - 1];
                                }
                                else if (S[i - 2].IsNotEqual(S[i - 1]) && S[i].IsEqual(S[i + 1]))
                                {
                                    tmp[i] = S[i];
                                }
                                else if (S[i].IsEqual(S[i - 1]))
                                {
                                    tmp[i] = S[i];
                                }
                                else if (S[i - 2].IsEqual(S[i - 1]) && S[i - 1].IsNotEqual(S[i]) && S[i].IsEqual(S[i + 1]))
                                {
                                    tmp[i] = (S[i - 1] + S[i]) / 2.0;
                                }
                                else
                                {
                                    tmp[i] = (System.Math.Abs(S[i + 1] - S[i]) * S[i - 1] + System.Math.Abs(S[i - 1] - S[i - 2]) * S[i]) /
                                             (System.Math.Abs(S[i + 1] - S[i]) + System.Math.Abs(S[i - 1] - S[i - 2]));
                                }
                            }

                            tmp[size_ - 2] = (System.Math.Abs(2 * S[size_ - 2] * S[size_ - 3] - S[size_ - 2]) * S[size_ - 3] +
                                              System.Math.Abs(S[size_ - 3] - S[size_ - 4]) * S[size_ - 2]) /
                                             (System.Math.Abs(2 * S[size_ - 2] * S[size_ - 3] - S[size_ - 2]) +
                                              System.Math.Abs(S[size_ - 3] - S[size_ - 4]));
                            tmp[size_ - 1] =
                                (System.Math.Abs(4 * S[size_ - 2] * S[size_ - 2] * S[size_ - 3] - 2 * S[size_ - 2] * S[size_ - 3]) *
                                    S[size_ - 2] + System.Math.Abs(S[size_ - 2] - S[size_ - 3]) * 2 * S[size_ - 2] * S[size_ - 3]) /
                                (System.Math.Abs(4 * S[size_ - 2] * S[size_ - 2] * S[size_ - 3] - 2 * S[size_ - 2] * S[size_ - 3]) +
                                 System.Math.Abs(S[size_ - 2] - S[size_ - 3]));
                            break;
                        case CubicInterpolation.DerivativeApprox.Kruger:
                            // intermediate points
                            for (var i = 1; i < size_ - 1; ++i)
                            {
                                if (S[i - 1] * S[i] < 0.0)
                                    // slope changes sign at point
                                {
                                    tmp[i] = 0.0;
                                }
                                else
                                    // slope will be between the slopes of the adjacent
                                    // straight lines and should approach zero if the
                                    // slope of either line approaches zero
                                {
                                    tmp[i] = 2.0 / (1.0 / S[i - 1] + 1.0 / S[i]);
                                }
                            }

                            // end points
                            tmp[0] = (3.0 * S[0] - tmp[1]) / 2.0;
                            tmp[size_ - 1] = (3.0 * S[size_ - 2] - tmp[size_ - 2]) / 2.0;
                            break;
                        case CubicInterpolation.DerivativeApprox.Harmonic:
                            // intermediate points
                            for (var i = 1; i < size_ - 1; ++i)
                            {
                                var w1 = 2 * dx[i] + dx[i - 1];
                                var w2 = dx[i] + 2 * dx[i - 1];
                                if (S[i - 1] * S[i] <= 0.0)
                                    // slope changes sign at point
                                {
                                    tmp[i] = 0.0;
                                }
                                else
                                    // weighted harmonic mean of S[i] and S[i-1] if they
                                    // have the same sign; otherwise 0
                                {
                                    tmp[i] = (w1 + w2) / (w1 / S[i - 1] + w2 / S[i]);
                                }
                            }

                            // end points [0]
                            tmp[0] = ((2 * dx[0] + dx[1]) * S[0] - dx[0] * S[1]) / (dx[1] + dx[0]);
                            if (tmp[0] * S[0] < 0.0)
                            {
                                tmp[0] = 0;
                            }
                            else if (S[0] * S[1] < 0)
                            {
                                if (System.Math.Abs(tmp[0]) > System.Math.Abs(3 * S[0]))
                                {
                                    tmp[0] = 3 * S[0];
                                }
                            }

                            // end points [n-1]
                            tmp[size_ - 1] = ((2 * dx[size_ - 2] + dx[size_ - 3]) * S[size_ - 2] - dx[size_ - 2] * S[size_ - 3]) / (dx[size_ - 3] + dx[size_ - 2]);
                            if (tmp[size_ - 1] * S[size_ - 2] < 0.0)
                            {
                                tmp[size_ - 1] = 0;
                            }
                            else if (S[size_ - 2] * S[size_ - 3] < 0)
                            {
                                if (System.Math.Abs(tmp[size_ - 1]) > System.Math.Abs(3 * S[size_ - 2]))
                                {
                                    tmp[size_ - 1] = 3 * S[size_ - 2];
                                }
                            }

                            break;
                        default:
                            throw new ArgumentException("unknown scheme");
                    }
                }
            }

            monotonicityAdjustments_.Erase();

            // Hyman monotonicity constrained filter
            if (monotonic_)
            {
                double correction;
                double pm, pu, pd, M;
                for (var i = 0; i < size_; ++i)
                {
                    if (i == 0)
                    {
                        if (tmp[i] * S[0] > 0.0)
                        {
                            correction = tmp[i] / System.Math.Abs(tmp[i]) *
                                         System.Math.Min(System.Math.Abs(tmp[i]),
                                             System.Math.Abs(3.0 * S[0]));
                        }
                        else
                        {
                            correction = 0.0;
                        }

                        if (correction.IsNotEqual(tmp[i]))
                        {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    }
                    else if (i == size_ - 1)
                    {
                        if (tmp[i] * S[size_ - 2] > 0.0)
                        {
                            correction = tmp[i] / System.Math.Abs(tmp[i]) *
                                         System.Math.Min(System.Math.Abs(tmp[i]), System.Math.Abs(3.0 * S[size_ - 2]));
                        }
                        else
                        {
                            correction = 0.0;
                        }

                        if (correction.IsNotEqual(tmp[i]))
                        {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    }
                    else
                    {
                        pm = (S[i - 1] * dx[i] + S[i] * dx[i - 1]) /
                             (dx[i - 1] + dx[i]);
                        M = 3.0 * System.Math.Min(System.Math.Min(System.Math.Abs(S[i - 1]), System.Math.Abs(S[i])),
                            System.Math.Abs(pm));
                        if (i > 1)
                        {
                            if ((S[i - 1] - S[i - 2]) * (S[i] - S[i - 1]) > 0.0)
                            {
                                pd = (S[i - 1] * (2.0 * dx[i - 1] + dx[i - 2])
                                      - S[i - 2] * dx[i - 1]) /
                                     (dx[i - 2] + dx[i - 1]);
                                if (pm * pd > 0.0 && pm * (S[i - 1] - S[i - 2]) > 0.0)
                                {
                                    M = System.Math.Max(M, 1.5 * System.Math.Min(
                                        System.Math.Abs(pm), System.Math.Abs(pd)));
                                }
                            }
                        }

                        if (i < size_ - 2)
                        {
                            if ((S[i] - S[i - 1]) * (S[i + 1] - S[i]) > 0.0)
                            {
                                pu = (S[i] * (2.0 * dx[i] + dx[i + 1]) - S[i + 1] * dx[i]) /
                                     (dx[i] + dx[i + 1]);
                                if (pm * pu > 0.0 && -pm * (S[i] - S[i - 1]) > 0.0)
                                {
                                    M = System.Math.Max(M, 1.5 * System.Math.Min(
                                        System.Math.Abs(pm), System.Math.Abs(pu)));
                                }
                            }
                        }

                        if (tmp[i] * pm > 0.0)
                        {
                            correction = tmp[i] / System.Math.Abs(tmp[i]) *
                                         System.Math.Min(System.Math.Abs(tmp[i]), M);
                        }
                        else
                        {
                            correction = 0.0;
                        }

                        if (correction.IsNotEqual(tmp[i]))
                        {
                            tmp[i] = correction;
                            monotonicityAdjustments_[i] = true;
                        }
                    }
                }
            }

            // cubic coefficients
            for (var i = 0; i < size_ - 1; ++i)
            {
                a_[i] = tmp[i];
                b_[i] = (3.0 * S[i] - tmp[i + 1] - 2.0 * tmp[i]) / dx[i];
                c_[i] = (tmp[i + 1] + tmp[i] - 2.0 * S[i]) / (dx[i] * dx[i]);
            }

            primitiveConst_[0] = 0.0;
            for (var i = 1; i < size_ - 1; ++i)
            {
                primitiveConst_[i] = primitiveConst_[i - 1]
                                     + dx[i - 1] *
                                     (yBegin_[i - 1] + dx[i - 1] *
                                         (a_[i - 1] / 2.0 + dx[i - 1] *
                                             (b_[i - 1] / 3.0 + dx[i - 1] * c_[i - 1] / 4.0)));
            }
        }

        public override double value(double x)
        {
            var j = locate(x);
            var dx = x - xBegin_[j];
            return yBegin_[j] + dx * (a_[j] + dx * (b_[j] + dx * c_[j]));
        }

        private double cubicInterpolatingPolynomialDerivative(
            double a, double b, double c, double d,
            double u, double v, double w, double z, double x) =>
            -((((a - c) * (b - c) * (c - x) * z - (a - d) * (b - d) * (d - x) * w) * (a - x + b - x)
               + ((a - c) * (b - c) * z - (a - d) * (b - d) * w) * (a - x) * (b - x)) * (a - b) +
              ((a - c) * (a - d) * v - (b - c) * (b - d) * u) * (c - d) * (c - x) * (d - x)
              + ((a - c) * (a - d) * (a - x) * v - (b - c) * (b - d) * (b - x) * u)
              * (c - x + d - x) * (c - d)) /
            ((a - b) * (a - c) * (a - d) * (b - c) * (b - d) * (c - d));
    }
}
