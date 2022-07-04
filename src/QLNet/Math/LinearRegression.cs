using System;
using System.Collections.Generic;

namespace QLNet.Math
{
    [JetBrains.Annotations.PublicAPI] public class LinearRegression
    {
        private LinearLeastSquaresRegression<List<double>> reg_;


        //! one dimensional linear regression
        public LinearRegression(List<double> x, List<double> y)
        {
            reg_ = new LinearLeastSquaresRegression<List<double>>(argumentWrapper(x), y, linearFcts(1));
        }

        //! multi dimensional linear regression
        public LinearRegression(List<List<double>> x, List<double> y)
        {
            reg_ = new LinearLeastSquaresRegression<List<double>>(x, y, linearFcts(x[0].Count));
        }

        public Vector coefficients() => reg_.coefficients();

        public Vector residuals() => reg_.residuals();

        public Vector standardErrors() => reg_.standardErrors();

        class LinearFct
        {
            private int i_;

            public LinearFct(int i)
            {
                i_ = i;
            }

            public double value(List<double> x) => x[i_];
        }

        private List<Func<List<double>, double>> linearFcts(int dims)
        {
            var retVal = new List<Func<List<double>, double>>();
            retVal.Add(x => 1.0);

            for (var i = 0; i < dims; ++i)
            {
                retVal.Add(new LinearFct(i).value);
            }

            return retVal;
        }

        private List<List<double>> argumentWrapper(List<double> x)
        {
            var retVal = new List<List<double>>();

            foreach (var v in x)
                retVal.Add(new List<double>() { v });

            return retVal;
        }
    }
}