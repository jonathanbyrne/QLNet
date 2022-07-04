//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using QLNet.processes;
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet.Pricingengines.Forward
{
    //! %Forward performance engine for vanilla options
    /*! \ingroup forwardengines

        \test
        - the correctness of the returned value is tested by
          reproducing results available in literature.
        - the correctness of the returned greeks is tested by
          reproducing numerical derivatives.
    */
    [JetBrains.Annotations.PublicAPI] public class ForwardPerformanceVanillaEngine : ForwardVanillaEngine
    {
        public ForwardPerformanceVanillaEngine(GeneralizedBlackScholesProcess process, GetOriginalEngine getEngine)
           : base(process, getEngine) { }
        public override void calculate()
        {
            setup();
            originalEngine_.calculate();
            getOriginalResults();
        }
        protected override void getOriginalResults()
        {
            var rfdc = process_.riskFreeRate().link.dayCounter();
            var resetTime = rfdc.yearFraction(process_.riskFreeRate().link.referenceDate(),
                                                 arguments_.resetDate);
            var discR = process_.riskFreeRate().link.discount(arguments_.resetDate);
            // it's a performance option
            discR /= process_.stateVariable().link.value();

            var temp = originalResults_.value;
            results_.value = discR * temp;
            results_.delta = 0.0;
            results_.gamma = 0.0;
            results_.theta = process_.riskFreeRate().link.
                                  zeroRate(arguments_.resetDate, rfdc, Compounding.Continuous, Frequency.NoFrequency).value()
                                  * results_.value;
            results_.vega = discR * originalResults_.vega;
            results_.rho = -resetTime * results_.value + discR * originalResults_.rho;
            results_.dividendRho = discR * originalResults_.dividendRho;

        }
    }
}
