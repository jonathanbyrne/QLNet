/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Extensions;

namespace QLNet
{
    /// <summary>
    ///     Amount of cash
    ///     Money arithmetic is tested with and without currency conversions.
    /// </summary>
    [PublicAPI]
    public class Money
    {
        #region Define

        public enum ConversionType
        {
            /// <summary>
            ///     do not perform conversions
            /// </summary>
            NoConversion,
            /// <summary>
            ///     convert both operands to the base currency before converting
            /// </summary>
            BaseCurrencyConversion,
            /// <summary>
            ///     return the result in the currency of the first operand
            /// </summary>
            AutomatedConversion
        }

        #endregion

        #region Attributes

        [ThreadStatic]
        private static ConversionType conversionType_;
        [ThreadStatic]
        private static Currency baseCurrency_;

        public static ConversionType conversionType
        {
            get => conversionType_;
            set => conversionType_ = value;
        }

        public static Currency baseCurrency
        {
            get => baseCurrency_;
            set => baseCurrency_ = value;
        }

        #endregion

        #region Constructor

        public Money()
        {
            value = 0.0;
        }

        public Money(Currency currency, double value)
        {
            this.value = value;
            this.currency = currency;
        }

        public Money(double value, Currency currency) : this(currency, value)
        {
        }

        #endregion

        #region Get/Set

        public Currency currency { get; }

        public double value { get; private set; }

        #endregion

        #region Methods

        public static void convertTo(ref Money m, Currency target)
        {
            if (m.currency != target)
            {
                var rate = ExchangeRateManager.Instance.lookup(m.currency, target);
                m = rate.exchange(m).rounded();
            }
        }

        public static void convertToBase(ref Money m)
        {
            Utils.QL_REQUIRE(!baseCurrency.empty(), () => "no base currency set");
            convertTo(ref m, baseCurrency);
        }

        public Money rounded() => new Money(currency.rounding.Round(value), currency);

        public override string ToString() => rounded().value + "-" + currency.code + "-" + currency.symbol;

        #endregion

        #region Operators

        public static Money operator *(Money m, double x) => new Money(m.value * x, m.currency);

        public static Money operator *(double x, Money m) => m * x;

        public static Money operator /(Money m, double x) => new Money(m.value / x, m.currency);

        public static Money operator +(Money m1, Money m2)
        {
            var m = new Money(m1.currency, m1.value);

            if (m1.currency == m2.currency)
            {
                m.value += m2.value;
            }
            else if (conversionType == ConversionType.BaseCurrencyConversion)
            {
                convertToBase(ref m);
                var tmp = m2;
                convertToBase(ref tmp);
                m += tmp;
            }
            else if (conversionType == ConversionType.AutomatedConversion)
            {
                var tmp = m2;
                convertTo(ref tmp, m.currency);
                m += tmp;
            }
            else
            {
                Utils.QL_FAIL("currency mismatch and no conversion specified");
            }

            return m;
        }

        public static Money operator -(Money m1, Money m2)
        {
            var m = new Money(m1.currency, m1.value);

            if (m.currency == m2.currency)
            {
                m.value -= m2.value;
            }
            else if (conversionType == ConversionType.BaseCurrencyConversion)
            {
                convertToBase(ref m);
                var tmp = m2;
                convertToBase(ref tmp);
                m -= tmp;
            }
            else if (conversionType == ConversionType.AutomatedConversion)
            {
                var tmp = m2;
                convertTo(ref tmp, m.currency);
                m -= tmp;
            }
            else
            {
                Utils.QL_FAIL("currency mismatch and no conversion specified");
            }

            return m;
        }

        public static bool operator ==(Money m1, Money m2)
        {
            if ((object)m1 == null && (object)m2 == null)
            {
                return true;
            }

            if ((object)m1 == null || (object)m2 == null)
            {
                return false;
            }

            if (m1.currency == m2.currency)
            {
                return m1.value.IsEqual(m2.value);
            }

            if (conversionType == ConversionType.BaseCurrencyConversion)
            {
                var tmp1 = m1;
                convertToBase(ref tmp1);
                var tmp2 = m2;
                convertToBase(ref tmp2);
                return tmp1 == tmp2;
            }

            if (conversionType == ConversionType.AutomatedConversion)
            {
                var tmp = m2;
                convertTo(ref tmp, m1.currency);
                return m1 == tmp;
            }

            Utils.QL_FAIL("currency mismatch and no conversion specified");
            return false;
        }

        public static bool operator !=(Money m1, Money m2) => !(m1 == m2);

        public override bool Equals(object o) => (this == (Money)o);

        public override int GetHashCode() => 0;

        #endregion
    }
}
