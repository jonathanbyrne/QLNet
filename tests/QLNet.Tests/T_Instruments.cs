﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

using Xunit;
using QLNet.Instruments;
using QLNet.Quotes;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Instruments
    {
        [Fact]
        public void testObservable()
        {
            //.("Testing observability of instruments...");

            var me1 = new SimpleQuote(0.0);
            var h = new RelinkableHandle<Quote>(me1);
            Instrument s = new Stock(h);

            var f = new Flag();

            s.registerWith(f.update);

            s.NPV();
            me1.setValue(3.14);
            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of instrument change");
            }

            s.NPV();
            f.lower();
            var me2 = new SimpleQuote(0.0);
            h.linkTo(me2);
            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of instrument change");
            }

            f.lower();
            s.freeze();
            s.NPV();
            me2.setValue(2.71);
            if (f.isUp())
            {
                QAssert.Fail("Observer was notified of frozen instrument change");
            }

            s.NPV();
            s.unfreeze();
            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of instrument change");
            }
        }
    }
}
