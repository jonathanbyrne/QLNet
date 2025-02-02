/*
 Copyright (C) 2008 Andrea Maggiulli

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
using QLNet;
using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_ExchangeRate
    {
        [Fact]
        public void testDirect()
        {

            Currency EUR = new EURCurrency(), USD = new USDCurrency();

            var eur_usd = new ExchangeRate(EUR, USD, 1.2042);

            var m1 = 50000.0 * EUR;
            var m2 = 100000.0 * USD;

            Money.conversionType = Money.ConversionType.NoConversion;

            var calculated = eur_usd.exchange(m1);
            var expected = new Money(m1.value * eur_usd.rate, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            calculated = eur_usd.exchange(m2);
            expected = new Money(m2.value / eur_usd.rate, EUR);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }
        }

        /// <summary>
        /// Testing derived exchange rates
        /// </summary>
        [Fact]
        public void testDerived()
        {

            Currency EUR = new EURCurrency(), USD = new USDCurrency(), GBP = new GBPCurrency();

            var eur_usd = new ExchangeRate(EUR, USD, 1.2042);
            var eur_gbp = new ExchangeRate(EUR, GBP, 0.6612);

            var derived = ExchangeRate.chain(eur_usd, eur_gbp);

            var m1 = 50000.0 * GBP;
            var m2 = 100000.0 * USD;

            Money.conversionType = Money.ConversionType.NoConversion;

            var calculated = derived.exchange(m1);
            var expected = new Money(m1.value * eur_usd.rate / eur_gbp.rate, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            calculated = derived.exchange(m2);
            expected = new Money(m2.value * eur_gbp.rate / eur_usd.rate, GBP);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }
        }

        /// <summary>
        /// Testing lookup of direct exchange rates
        /// </summary>
        [Fact]
        public void testDirectLookup()
        {
            var rateManager = ExchangeRateManager.Instance;
            rateManager.clear();

            Currency EUR = new EURCurrency(), USD = new USDCurrency();

            var eur_usd1 = new ExchangeRate(EUR, USD, 1.1983);
            var eur_usd2 = new ExchangeRate(USD, EUR, 1.0 / 1.2042);
            rateManager.add(eur_usd1, new Date(4, Month.August, 2004));
            rateManager.add(eur_usd2, new Date(5, Month.August, 2004));

            var m1 = 50000.0 * EUR;
            var m2 = 100000.0 * USD;

            Money.conversionType = Money.ConversionType.NoConversion;

            var eur_usd = rateManager.lookup(EUR, USD, new Date(4, Month.August, 2004), ExchangeRate.Type.Direct);
            var calculated = eur_usd.exchange(m1);
            var expected = new Money(m1.value * eur_usd1.rate, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            eur_usd = rateManager.lookup(EUR, USD, new Date(5, Month.August, 2004), ExchangeRate.Type.Direct);
            calculated = eur_usd.exchange(m1);
            expected = new Money(m1.value / eur_usd2.rate, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            var usd_eur = rateManager.lookup(USD, EUR, new Date(4, Month.August, 2004), ExchangeRate.Type.Direct);

            calculated = usd_eur.exchange(m2);
            expected = new Money(m2.value / eur_usd1.rate, EUR);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            usd_eur = rateManager.lookup(USD, EUR, new Date(5, Month.August, 2004), ExchangeRate.Type.Direct);

            calculated = usd_eur.exchange(m2);
            expected = new Money(m2.value * eur_usd2.rate, EUR);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }
        }

        /// <summary>
        /// Testing lookup of triangulated exchange rates
        /// </summary>
        [Fact]
        public void testTriangulatedLookup()
        {

            var rateManager = ExchangeRateManager.Instance;
            rateManager.clear();

            Currency EUR = new EURCurrency(), USD = new USDCurrency(), ITL = new ITLCurrency();

            var eur_usd1 = new ExchangeRate(EUR, USD, 1.1983);
            var eur_usd2 = new ExchangeRate(EUR, USD, 1.2042);
            rateManager.add(eur_usd1, new Date(4, Month.August, 2004));
            rateManager.add(eur_usd2, new Date(5, Month.August, 2004));

            var m1 = 50000000.0 * ITL;
            var m2 = 100000.0 * USD;

            Money.conversionType = Money.ConversionType.NoConversion;

            var itl_usd = rateManager.lookup(ITL, USD, new Date(4, Month.August, 2004));
            var calculated = itl_usd.exchange(m1);
            var expected = new Money(m1.value * eur_usd1.rate / 1936.27, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            itl_usd = rateManager.lookup(ITL, USD, new Date(5, Month.August, 2004));
            calculated = itl_usd.exchange(m1);
            expected = new Money(m1.value * eur_usd2.rate / 1936.27, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            var usd_itl = rateManager.lookup(USD, ITL, new Date(4, Month.August, 2004));

            calculated = usd_itl.exchange(m2);
            expected = new Money(m2.value * 1936.27 / eur_usd1.rate, ITL);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            usd_itl = rateManager.lookup(USD, ITL, new Date(5, Month.August, 2004));

            calculated = usd_itl.exchange(m2);
            expected = new Money(m2.value * 1936.27 / eur_usd2.rate, ITL);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }
        }

        /// <summary>
        /// Testing lookup of derived exchange rates
        /// </summary>
        [Fact]
        public void testSmartLookup()
        {

            Currency EUR = new EURCurrency(), USD = new USDCurrency(), GBP = new GBPCurrency(),
            CHF = new CHFCurrency(), SEK = new SEKCurrency(), JPY = new JPYCurrency();

            var rateManager = ExchangeRateManager.Instance;
            rateManager.clear();

            var eur_usd1 = new ExchangeRate(EUR, USD, 1.1983);
            var eur_usd2 = new ExchangeRate(USD, EUR, 1.0 / 1.2042);
            rateManager.add(eur_usd1, new Date(4, Month.August, 2004));
            rateManager.add(eur_usd2, new Date(5, Month.August, 2004));

            var eur_gbp1 = new ExchangeRate(GBP, EUR, 1.0 / 0.6596);
            var eur_gbp2 = new ExchangeRate(EUR, GBP, 0.6612);
            rateManager.add(eur_gbp1, new Date(4, Month.August, 2004));
            rateManager.add(eur_gbp2, new Date(5, Month.August, 2004));

            var usd_chf1 = new ExchangeRate(USD, CHF, 1.2847);
            var usd_chf2 = new ExchangeRate(CHF, USD, 1.0 / 1.2774);
            rateManager.add(usd_chf1, new Date(4, Month.August, 2004));
            rateManager.add(usd_chf2, new Date(5, Month.August, 2004));

            var chf_sek1 = new ExchangeRate(SEK, CHF, 0.1674);
            var chf_sek2 = new ExchangeRate(CHF, SEK, 1.0 / 0.1677);
            rateManager.add(chf_sek1, new Date(4, Month.August, 2004));
            rateManager.add(chf_sek2, new Date(5, Month.August, 2004));

            var jpy_sek1 = new ExchangeRate(SEK, JPY, 14.5450);
            var jpy_sek2 = new ExchangeRate(JPY, SEK, 1.0 / 14.6110);
            rateManager.add(jpy_sek1, new Date(4, Month.August, 2004));
            rateManager.add(jpy_sek2, new Date(5, Month.August, 2004));

            var m1 = 100000.0 * USD;
            var m2 = 100000.0 * EUR;
            var m3 = 100000.0 * GBP;
            var m4 = 100000.0 * CHF;
            var m5 = 100000.0 * SEK;
            var m6 = 100000.0 * JPY;

            Money.conversionType = Money.ConversionType.NoConversion;

            // two-rate chain

            var usd_sek = rateManager.lookup(USD, SEK, new Date(4, Month.August, 2004));
            var calculated = usd_sek.exchange(m1);
            var expected = new Money(m1.value * usd_chf1.rate / chf_sek1.rate, SEK);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            usd_sek = rateManager.lookup(SEK, USD, new Date(5, Month.August, 2004));
            calculated = usd_sek.exchange(m5);
            expected = new Money(m5.value * usd_chf2.rate / chf_sek2.rate, USD);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            // three-rate chain

            var eur_sek = rateManager.lookup(EUR, SEK, new Date(4, Month.August, 2004));
            calculated = eur_sek.exchange(m2);
            expected = new Money(m2.value * eur_usd1.rate * usd_chf1.rate / chf_sek1.rate, SEK);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            eur_sek = rateManager.lookup(SEK, EUR, new Date(5, Month.August, 2004));
            calculated = eur_sek.exchange(m5);
            expected = new Money(m5.value * eur_usd2.rate * usd_chf2.rate / chf_sek2.rate, EUR);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            // four-rate chain

            var eur_jpy = rateManager.lookup(EUR, JPY, new Date(4, Month.August, 2004));
            calculated = eur_jpy.exchange(m2);
            expected = new Money(m2.value * eur_usd1.rate * usd_chf1.rate * jpy_sek1.rate / chf_sek1.rate, JPY);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            eur_jpy = rateManager.lookup(JPY, EUR, new Date(5, Month.August, 2004));
            calculated = eur_jpy.exchange(m6);
            expected = new Money(m6.value * jpy_sek2.rate * eur_usd2.rate * usd_chf2.rate / chf_sek2.rate, EUR);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            // five-rate chain

            var gbp_jpy = rateManager.lookup(GBP, JPY, new Date(4, Month.August, 2004));
            calculated = gbp_jpy.exchange(m3);
            expected = new Money(m3.value * eur_gbp1.rate * eur_usd1.rate * usd_chf1.rate * jpy_sek1.rate / chf_sek1.rate, JPY);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }

            gbp_jpy = rateManager.lookup(JPY, GBP, new Date(5, Month.August, 2004));
            calculated = gbp_jpy.exchange(m6);
            expected = new Money(m6.value * jpy_sek2.rate * eur_usd2.rate * usd_chf2.rate * eur_gbp2.rate / chf_sek2.rate, GBP);

            if (!Math.Utils.close(calculated, expected))
            {
                QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
            }
        }
    }
}
