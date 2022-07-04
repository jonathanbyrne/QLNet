﻿using System.Collections.Generic;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class SVI
    {
        public SVI(double t, double forward, double a, double b, double sigma, double rho, double m,
            bool aIsFixed, bool bIsFixed, bool sigmaIsFixed, bool rhoIsFixed, bool mIsFixed,
            bool vegaWeighted = false,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null,
            double errorAccept = 0.0020, bool useMaxError = false, int maxGuesses = 50, List<double?> addParams = null)
        {
            t_ = t;
            forward_ = forward;
            a_ = a;
            b_ = b;
            sigma_ = sigma;
            rho_ = rho;
            m_ = m;
            aIsFixed_ = aIsFixed;
            bIsFixed_ = bIsFixed;
            sigmaIsFixed_ = sigmaIsFixed;
            rhoIsFixed_ = rhoIsFixed;
            mIsFixed_ = mIsFixed;
            vegaWeighted_ = vegaWeighted;
            endCriteria_ = endCriteria;
            optMethod_ = optMethod;
            errorAccept_ = errorAccept;
            useMaxError_ = useMaxError;
            maxGuesses_ = maxGuesses;
            addParams_ = addParams;
        }

        public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin) =>
            new SviInterpolation(xBegin, xEnd, yBegin, t_, forward_, a_, b_, sigma_, rho_, m_,
                aIsFixed_, bIsFixed_, sigmaIsFixed_, rhoIsFixed_, mIsFixed_, vegaWeighted_,
                endCriteria_, optMethod_, errorAccept_, useMaxError_, maxGuesses_);

        public const bool global = true;

        private double t_;
        private double forward_;
        private double a_, b_, sigma_, rho_, m_;
        private bool aIsFixed_, bIsFixed_, sigmaIsFixed_, rhoIsFixed_, mIsFixed_;
        private bool vegaWeighted_;
        private EndCriteria endCriteria_;
        private OptimizationMethod optMethod_;
        private double errorAccept_;
        private bool useMaxError_;
        private int maxGuesses_;
        private List<double?> addParams_;
    }
}