/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    //! Abstract base class for dividend engines
    /*! \todo The dividend class really needs to be made more
              sophisticated to distinguish between fixed dividends and fractional dividends
    */

    //! Finite-differences pricing engine for dividend options using
    // escowed dividend model
    /*! \ingroup vanillaengines */
    /* The merton 73 engine is the classic engine described in most
       derivatives texts.  However, Haug, Haug, and Lewis in
       "Back to Basics: a new approach to the discrete dividend
       problem" argues that this scheme underprices call options.
       This is set as the default engine, because it is consistent
       with the analytic version.
    */

    //! Finite-differences engine for dividend options using shifted dividends
    /*! \ingroup vanillaengines */
    /* This engine uses the same algorithm that was used in quantlib
       in versions 0.3.11 and earlier.  It produces results that
       are different from the Merton 73 engine.

       \todo Review literature to see whether this is described
    */

    // Use Merton73 engine as default.
    [JetBrains.Annotations.PublicAPI] public class FDDividendEngine : FDDividendEngineMerton73
    {

        public FDDividendEngine()
        { }

        public FDDividendEngine(GeneralizedBlackScholesProcess process, int timeSteps = 100, int gridPoints = 100,
                                bool timeDependent = false) : base(process, timeSteps, gridPoints, timeDependent)
        { }

        public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent) => new FDDividendEngine(process, timeSteps, gridPoints, timeDependent);
    }
}
