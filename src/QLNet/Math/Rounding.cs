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

namespace QLNet.Math
{
    //! rounding methods
    /*! The rounding methods follow the OMG specification available
        at ftp://ftp.omg.org/pub/docs/formal/00-06-29.pdf
        \warning the names of the Floor and Ceiling methods might be misleading. Check the provided reference. */

    /// <summary>
    ///     Basic rounding class
    /// </summary>
    [PublicAPI]
    public class Rounding
    {
        public enum Type
        {
            /// <summary>
            ///     do not round: return the number unmodified
            /// </summary>
            None,
            /// <summary>
            ///     the first decimal place past the precision will be
            ///     rounded up. This differs from the OMG rule which
            ///     rounds up only if the decimal to be rounded is
            ///     greater than or equal to the rounding digit
            /// </summary>
            Up,
            /// <summary>
            ///     all decimal places past the precision will be
            ///     truncated
            /// </summary>
            Down,
            /// <summary>
            ///     the first decimal place past the precision
            ///     will be rounded up if greater than or equal
            ///     to the rounding digit; this corresponds to
            ///     the OMG round-up rule.  When the rounding
            ///     digit is 5, the result will be the one
            ///     closest to the original number, hence the
            ///     name.
            /// </summary>
            Closest,
            /// <summary>
            ///     positive numbers will be rounded up and negative
            ///     numbers will be rounded down using the OMG round up
            ///     and round down rules
            /// </summary>
            Floor,
            /// <summary>
            ///     positive numbers will be rounded down and negative
            ///     numbers will be rounded up using the OMG round up
            ///     and round down rules
            /// </summary>
            Ceiling
        }

        /// <summary>
        ///     default constructor
        ///     Instances built through this constructor don't perform
        ///     any rounding.
        /// </summary>
        public Rounding()
        {
            getType = Type.None;
        }

        public Rounding(int precision, Type type)
            : this(precision, type, 5)
        {
        }

        public Rounding(int precision)
            : this(precision, Type.Closest, 5)
        {
        }

        public Rounding(int precision, Type type, int digit)
        {
            Precision = precision;
            getType = type;
            Digit = digit;
        }

        public int Digit { get; }

        public Type getType { get; }

        public int Precision { get; }

        /// <summary>
        ///     Up-rounding
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public double Round(double value)
        {
            if (getType == Type.None)
            {
                return value;
            }

            var mult = System.Math.Pow(10.0, Precision);
            var neg = value < 0.0;
            var lvalue = System.Math.Abs(value) * mult;
            var integral = 0.0;
            var modVal = lvalue - (integral = System.Math.Floor(lvalue));

            lvalue -= modVal;
            switch (getType)
            {
                case Type.Down:
                    break;
                case Type.Up:
                    lvalue += 1.0;
                    break;
                case Type.Closest:
                    if (modVal >= Digit / 10.0)
                    {
                        lvalue += 1.0;
                    }

                    break;
                case Type.Floor:
                    if (!neg)
                    {
                        if (modVal >= Digit / 10.0)
                        {
                            lvalue += 1.0;
                        }
                    }

                    break;
                case Type.Ceiling:
                    if (neg)
                    {
                        if (modVal >= Digit / 10.0)
                        {
                            lvalue += 1.0;
                        }
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown rounding method");
                    break;
            }

            return neg ? -(lvalue / mult) : lvalue / mult;
        }
    }

    //!
}
