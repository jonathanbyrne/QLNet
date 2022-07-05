using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Optimization;
using QLNet.Termstructures.Volatility;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class SABR
    {
        public const bool global = true;
        private double alpha_, beta_, nu_, rho_;
        private bool alphaIsFixed_, betaIsFixed_, nuIsFixed_, rhoIsFixed_;
        private SabrApproximationModel approximationModel_;
        private EndCriteria endCriteria_;
        private double errorAccept_;
        private double forward_;
        private int maxGuesses_;
        private OptimizationMethod optMethod_;
        private double shift_;
        private double t_;
        private bool useMaxError_;
        private bool vegaWeighted_;
        private VolatilityType volatilityType_;

        public SABR(double t, double forward, double alpha, double beta, double nu, double rho,
            bool alphaIsFixed, bool betaIsFixed, bool nuIsFixed, bool rhoIsFixed,
            bool vegaWeighted = false,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null,
            double errorAccept = 0.0020, bool useMaxError = false, int maxGuesses = 50, double shift = 0.0,
            VolatilityType volatilityType = VolatilityType.ShiftedLognormal,
            SabrApproximationModel approximationModel = SabrApproximationModel.Hagan2002)
        {
            t_ = t;
            forward_ = forward;
            alpha_ = alpha;
            beta_ = beta;
            nu_ = nu;
            rho_ = rho;
            alphaIsFixed_ = alphaIsFixed;
            betaIsFixed_ = betaIsFixed;
            nuIsFixed_ = nuIsFixed;
            rhoIsFixed_ = rhoIsFixed;
            vegaWeighted_ = vegaWeighted;
            endCriteria_ = endCriteria;
            optMethod_ = optMethod;
            errorAccept_ = errorAccept;
            useMaxError_ = useMaxError;
            maxGuesses_ = maxGuesses;
            shift_ = shift;
            volatilityType_ = volatilityType;
            approximationModel_ = approximationModel;
        }

        public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin) =>
            new SABRInterpolation(xBegin, xEnd, yBegin, t_, forward_, alpha_, beta_, nu_, rho_,
                alphaIsFixed_, betaIsFixed_, nuIsFixed_, rhoIsFixed_, vegaWeighted_,
                endCriteria_, optMethod_, errorAccept_, useMaxError_, maxGuesses_, shift_,
                volatilityType_, approximationModel_);
    }
}
