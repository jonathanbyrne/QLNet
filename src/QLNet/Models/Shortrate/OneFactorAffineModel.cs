using System;
using QLNet.Math;

namespace QLNet.Models.Shortrate
{
    public abstract class OneFactorAffineModel : OneFactorModel,
        IAffineModel
    {
        protected OneFactorAffineModel(int nArguments)
            : base(nArguments)
        { }

        public virtual double discountBond(double now,
            double maturity,
            Vector factors) =>
            discountBond(now, maturity, factors[0]);

        public double discountBond(double now, double maturity, double rate) => A(now, maturity) * System.Math.Exp(-B(now, maturity) * rate);

        public double discount(double t)
        {
            var x0 = dynamics().process().x0();
            var r0 = dynamics().shortRate(0.0, x0);
            return discountBond(0.0, t, r0);
        }

        public virtual double discountBondOption(Option.Type type,
            double strike,
            double maturity,
            double bondMaturity) =>
            throw new NotImplementedException();

        protected abstract double A(double t, double T);
        protected abstract double B(double t, double T);
    }
}