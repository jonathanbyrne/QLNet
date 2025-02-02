﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System.Collections.Generic;
using QLNet.Math;
using QLNet.Math.MatrixUtilities;
using QLNet.Models;

namespace QLNet.legacy.libormarketmodels
{
    // libor forward correlation model
    public abstract class LmCorrelationModel
    {
        protected List<Parameter> arguments_;
        protected int size_;

        protected LmCorrelationModel(int size, int nArguments)
        {
            size_ = size;
            arguments_ = new InitializedList<Parameter>(nArguments);
        }

        public abstract Matrix correlation(double t, Vector x = null);

        public virtual double correlation(int i, int j, double t, Vector x = null) =>
            // inefficient implementation, please overload in derived classes
            correlation(t, x)[i, j];

        public virtual int factors() => size_;

        public virtual bool isTimeIndependent() => false;

        public List<Parameter> parameters() => arguments_;

        public virtual Matrix pseudoSqrt(double t, Vector x = null) =>
            MatrixUtilitites.pseudoSqrt(correlation(t, x),
                MatrixUtilitites.SalvagingAlgorithm.Spectral);

        public void setParams(List<Parameter> arguments)
        {
            arguments_ = arguments;
            generateArguments();
        }

        public virtual int size() => size_;

        protected abstract void generateArguments();
    }
}
