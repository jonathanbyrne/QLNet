/*
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

using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    //! %Currency specification
    [PublicAPI]
    public class Currency
    {
        protected string formatString_;
        protected int fractionsPerUnit_;
        protected string name_, code_;
        protected int numeric_;
        protected Rounding rounding_;
        protected string symbol_, fractionSymbol_;
        protected Currency triangulated_;

        // default constructor
        // Instances built via this constructor have undefined behavior. Such instances can only act as placeholders
        // and must be reassigned to a valid currency before being used.
        public Currency()
        {
        }

        public Currency(string name, string code, int numericCode, string symbol, string fractionSymbol,
            int fractionsPerUnit, Rounding rounding, string formatString) :
            this(name, code, numericCode, symbol, fractionSymbol, fractionsPerUnit, rounding, formatString,
                new Currency())
        {
        }

        public Currency(string name, string code, int numericCode, string symbol, string fractionSymbol,
            int fractionsPerUnit, Rounding rounding, string formatString,
            Currency triangulationCurrency)
        {
            name_ = name;
            code_ = code;
            numeric_ = numericCode;
            symbol_ = symbol;
            fractionSymbol_ = fractionSymbol;
            fractionsPerUnit_ = fractionsPerUnit;
            rounding_ = rounding;
            triangulated_ = triangulationCurrency;
            formatString_ = formatString;
        }

        public string code => code_; //! ISO 4217 three-letter code, e.g, "USD"

        // output format
        // The format will be fed three positional parameters, namely, value, code, and symbol, in this order.
        public string format => formatString_;

        public int fractionsPerUnit => fractionsPerUnit_; //! number of fractionary parts in a unit, e.g, 100

        public string fractionSymbol => fractionSymbol_; //! fraction symbol, e.g, "¢"

        // Inspectors
        public string name => name_; //! currency name, e.g, "U.S. Dollar"

        public int numericCode => numeric_; //! ISO 4217 numeric code, e.g, "840"

        public Rounding rounding => rounding_; //! rounding convention

        public string symbol => symbol_; //! symbol, e.g, "$"

        public Currency triangulationCurrency => triangulated_; //! currency used for triangulated exchange when required

        /*! \relates Currency */
        public static bool operator ==(Currency c1, Currency c2)
        {
            if ((object)c1 == null && (object)c2 == null)
            {
                return true;
            }

            if ((object)c1 == null || (object)c2 == null)
            {
                return false;
            }

            return c1.name == c2.name;
        }

        public static bool operator !=(Currency c1, Currency c2) => !(c1 == c2);

        public static Money operator *(double value, Currency c) => new Money(value, c);

        //! Other information
        //! is this a usable instance?
        public bool empty() => name_ == null;

        public override bool Equals(object o) => this == (Currency)o;

        public override int GetHashCode() => 0;

        public override string ToString() => code;
    }
}
