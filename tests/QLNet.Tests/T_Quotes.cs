﻿/*
 Copyright (C) 2008-2009 Andrea Maggiulli

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

using System;
using Xunit;
using QLNet.Quotes;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Quotes
    {
        double add10(double x) => x + 10;

        double mul10(double x) => x * 10;

        double sub10(double x) => x - 10;

        double add
           (double x, double y) =>
            x + y;

        double mul(double x, double y) => x * y;

        double sub(double x, double y) => x - y;

        [Fact]
        public void testObservable()
        {
            // Testing observability of quotes

            var me = new SimpleQuote(0.0);
            var f = new Flag();

            me.registerWith(f.update);
            me.setValue(3.14);

            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of quote change");
            }
        }
        [Fact]
        public void testObservableHandle()
        {

            // Testing observability of quote handles

            var me1 = new SimpleQuote(0.0);
            var h = new RelinkableHandle<Quote>(me1);

            var f = new Flag();

            h.registerWith(f.update);

            me1.setValue(3.14);

            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of quote change");
            }

            f.lower();
            var me2 = new SimpleQuote(0.0);
            h.linkTo(me2);

            if (!f.isUp())
            {
                QAssert.Fail("Observer was not notified of quote change");
            }
        }

        [Fact]
        public void testDerived()
        {

            // Testing derived quotes

            Func<double, double>[] f = { add10, mul10, sub10 };

            Quote me = new SimpleQuote(17.0);
            var h = new Handle<Quote>(me);

            for (var i = 0; i < 3; i++)
            {
                var derived = new DerivedQuote(h, f[i]);
                double x = derived.value(),
                       y = f[i](me.value());
                if (System.Math.Abs(x - y) > 1.0e-10)
                {
                    QAssert.Fail("derived quote yields " + x + "function result is " + y);
                }
            }

        }

        [Fact]
        public void testComposite()
        {
            // Testing composite quotes

            Func<double, double, double>[] f = { add, mul, sub };

            Quote me1 = new SimpleQuote(12.0),
            me2 = new SimpleQuote(13.0);
            Handle<Quote> h1 = new Handle<Quote>(me1),
            h2 = new Handle<Quote>(me2);

            for (var i = 0; i < 3; i++)
            {
                var composite = new CompositeQuote(h1, h2, f[i]);
                double x = composite.value(),
                       y = f[i](me1.value(), me2.value());
                if (System.Math.Abs(x - y) > 1.0e-10)
                {
                    QAssert.Fail("composite quote yields " + x + "function result is " + y);
                }
            }
        }


    }
}
