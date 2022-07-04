/*
 Copyright (C) 2008-2018  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

using QLNet.Math.Optimization;
using QLNet.Methods.montecarlo;
using QLNet.Patterns;
using System;
using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class XABRCoeffHolder<Model> where Model : IModel, new()
    {
        public XABRCoeffHolder(double t, double forward, List<double?> _params, List<bool> paramIsFixed,
                               List<double?> addParams)
        {
            t_ = t;
            forward_ = forward;
            params_ = _params;
            paramIsFixed_ = new InitializedList<bool>(paramIsFixed.Count, false);
            addParams_ = addParams;
            weights_ = new List<double>();
            error_ = null;
            maxError_ = null;
            XABREndCriteria_ = EndCriteria.Type.None;
            model_ = FastActivator<Model>.Create();

            Utils.QL_REQUIRE(t > 0.0, () => "expiry time must be positive: " + t + " not allowed");
            Utils.QL_REQUIRE(_params.Count == model_.dimension(), () =>
                             "wrong number of parameters (" + _params.Count + "), should be " + model_.dimension());
            Utils.QL_REQUIRE(paramIsFixed.Count == model_.dimension(), () =>
                             "wrong number of fixed parameters flags (" + paramIsFixed.Count + "), should be " +
                             model_.dimension());

            for (var i = 0; i < _params.Count; ++i)
            {
                if (_params[i] != null)
                    paramIsFixed_[i] = paramIsFixed[i];
            }

            model_.defaultValues(params_, paramIsFixed_, forward_, t_, addParams_);
            updateModelInstance();
        }

        public void updateModelInstance()
        {
            // forward might have changed
            modelInstance_ = model_.instance(t_, forward_, params_, addParams_);
        }

        /*! Expiry, Forward */
        public double t_ { get; set; }

        public double forward_ { get; set; }

        /*! Parameters */
        public List<double?> params_ { get; set; }
        public List<bool> paramIsFixed_ { get; set; }
        public List<double?> addParams_ { get; set; }

        public List<double> weights_ { get; set; }

        /*! Interpolation results */
        public double? error_ { get; set; }
        public double? maxError_ { get; set; }

        public EndCriteria.Type XABREndCriteria_ { get; set; }

        /*! Model instance (if required) */
        public IWrapper modelInstance_ { get; set; }
        public IModel model_ { get; set; }
    }

    //template <class I1, class I2, typename Model>

    //! No constraint
}
