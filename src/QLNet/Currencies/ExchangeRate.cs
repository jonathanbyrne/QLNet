/*
 Copyright (C) 2008 Andrea Maggiulli
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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Currencies
{
    /// <summary>
    ///     Exchange rate between two currencies
    ///     application of direct and derived exchange rate is
    ///     tested against calculations.
    /// </summary>
    [PublicAPI]
    public class ExchangeRate
    {
        /// <summary>
        ///     given directly by the user
        /// </summary>
        public enum Type
        {
            /// <summary>
            ///     given directly by the user
            /// </summary>
            Direct,
            /// <summary>
            ///     Derived from exchange rates between other currencies
            /// </summary>
            Derived
        }

        private double? rate_;
        private KeyValuePair<ExchangeRate, ExchangeRate> rateChain_;

        public ExchangeRate()
        {
            rate_ = null;
        }

        /// <summary>
        ///     the rate r  is given with the convention that a
        ///     unit of the source is worth r units of the target.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="rate"></param>
        public ExchangeRate(Currency source, Currency target, double rate)
        {
            this.source = source;
            this.target = target;
            rate_ = rate;
            type = Type.Direct;
        }

        public bool HasValue => rate_.HasValue;

        /// <summary>
        ///     the exchange rate (when available)
        /// </summary>
        /// <returns></returns>
        public double rate => rate_.Value;

        /// <summary>
        ///     the source currency.
        /// </summary>
        public Currency source { get; private set; }

        /// <summary>
        ///     the target currency.
        /// </summary>
        public Currency target { get; private set; }

        /// <summary>
        ///     the ExerciseType
        /// </summary>
        /// <returns></returns>
        public Type type { get; private set; }

        /// <summary>
        ///     chain two exchange rates
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        public static ExchangeRate chain(ExchangeRate r1, ExchangeRate r2)
        {
            var result = new ExchangeRate();
            result.type = Type.Derived;
            result.rateChain_ = new KeyValuePair<ExchangeRate, ExchangeRate>(r1, r2);
            if (r1.source == r2.source)
            {
                result.source = r1.target;
                result.target = r2.target;
                result.rate_ = r2.rate_ / r1.rate_;
            }
            else if (r1.source == r2.target)
            {
                result.source = r1.target;
                result.target = r2.source;
                result.rate_ = 1.0 / (r1.rate_ * r2.rate_);
            }
            else if (r1.target == r2.source)
            {
                result.source = r1.source;
                result.target = r2.target;
                result.rate_ = r1.rate_ * r2.rate_;
            }
            else if (r1.target == r2.target)
            {
                result.source = r1.source;
                result.target = r2.source;
                result.rate_ = r1.rate_ / r2.rate_;
            }
            else
            {
                QLNet.Utils.QL_FAIL("exchange rates not chainable");
            }

            return result;
        }

        /// <summary>
        ///     Utility methods
        ///     apply the exchange rate to a cash amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public Money exchange(Money amount)
        {
            switch (type)
            {
                case Type.Direct:
                    if (amount.currency == source)
                    {
                        return new Money(amount.value * rate_.Value, target);
                    }

                    if (amount.currency == target)
                    {
                        return new Money(amount.value / rate_.Value, source);
                    }

                    QLNet.Utils.QL_FAIL("exchange rate not applicable");
                    return null;

                case Type.Derived:
                    if (amount.currency == rateChain_.Key.source || amount.currency == rateChain_.Key.target)
                    {
                        return rateChain_.Value.exchange(rateChain_.Key.exchange(amount));
                    }

                    if (amount.currency == rateChain_.Value.source || amount.currency == rateChain_.Value.target)
                    {
                        return rateChain_.Key.exchange(rateChain_.Value.exchange(amount));
                    }

                    QLNet.Utils.QL_FAIL("exchange rate not applicable");
                    return null;
                default:
                    QLNet.Utils.QL_FAIL("unknown exchange-rate ExerciseType");
                    return null;
            }
        }
    }
}
