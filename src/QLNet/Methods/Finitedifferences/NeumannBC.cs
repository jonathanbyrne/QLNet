using System;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public class NeumannBC : BoundaryCondition<IOperator>
    {
        private double value_;
        private Side side_;

        public NeumannBC(double value, Side side)
        {
            value_ = value;
            side_ = side;
        }

        // interface
        public override void applyBeforeApplying(IOperator o)
        {
            var L = o as TridiagonalOperator;
            switch (side_)
            {
                case Side.Lower:
                    L.setFirstRow(-1.0, 1.0);
                    break;
                case Side.Upper:
                    L.setLastRow(-1.0, 1.0);
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }

        public override void applyAfterApplying(Vector u)
        {
            switch (side_)
            {
                case Side.Lower:
                    u[0] = u[1] - value_;
                    break;
                case Side.Upper:
                    u[u.size() - 1] = u[u.size() - 2] + value_;
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }

        public override void applyBeforeSolving(IOperator o, Vector rhs)
        {
            var L = o as TridiagonalOperator;
            switch (side_)
            {
                case Side.Lower:
                    L.setFirstRow(-1.0, 1.0);
                    rhs[0] = value_;
                    break;
                case Side.Upper:
                    L.setLastRow(-1.0, 1.0);
                    rhs[rhs.size() - 1] = value_;
                    break;
                default:
                    throw new ArgumentException("unknown side for Neumann boundary condition");
            }
        }
        public override void applyAfterSolving(Vector v)
        {
            // Nothing to do here
        }

        public override void setTime(double t)
        {
            // Nothing to do here
        }
    }
}