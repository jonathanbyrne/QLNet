﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System.Collections.Generic;
using JetBrains.Annotations;
using component = System.Collections.Generic.KeyValuePair<QLNet.Instruments.Instrument, double>;

namespace QLNet.Instruments
{
    //! %Composite instrument
    /*! This instrument is an aggregate of other instruments. Its NPV
        is the sum of the NPVs of its components, each possibly
        multiplied by a given factor.

        \warning Methods that drive the calculation directly (such as
                 recalculate(), freeze() and others) might not work
                 correctly.

        \ingroup instruments
    */
    [PublicAPI]
    public class CompositeInstrument : Instrument
    {
        private List<component> components_ = new List<component>();

        //! adds an instrument to the composite
        public void add
            (Instrument instrument, double multiplier = 1.0)
        {
            components_.Add(new component(instrument, multiplier));
            instrument.registerWith(update);
            update();
        }

        // Instrument interface
        public override bool isExpired()
        {
            foreach (var c in components_)
            {
                if (!c.Key.isExpired())
                {
                    return false;
                }
            }

            return true;
        }

        //! shorts an instrument from the composite
        public void subtract(Instrument instrument, double multiplier = 1.0)
        {
            add
                (instrument, -multiplier);
        }

        protected override void performCalculations()
        {
            NPV_ = 0.0;
            foreach (var c in components_)
            {
                NPV_ += c.Value * c.Key.NPV();
            }
        }
    }
}
