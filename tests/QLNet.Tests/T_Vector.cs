//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using System;
using System.Collections.Generic;
using Xunit;
using QLNet.Math;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Vector
    {
        /// <summary>
        /// Sample values.
        /// </summary>
        protected readonly List<double> Data = new List<double>() { 1, 2, 3, 4, 5 };

        /// <summary>
        /// Test vector clone
        /// </summary>
        [Fact]
        public void testClone()
        {
            var vector = new Vector(Data);
            var clone = vector.Clone();

            QAssert.AreNotSame(vector, clone);
            QAssert.AreEqual(vector.Count, clone.Count);
            QAssert.CollectionAreEqual(vector, clone);
            vector[0] = 100;
            QAssert.CollectionAreNotEqual(vector, clone);

        }

        /// <summary>
        /// Test clone a vector using <c>IClonable</c> interface method.
        /// </summary>
        [Fact]
        public void testCloneICloneable()
        {
            var vector = new Vector(Data);
            var clone = (Vector)((ICloneable)vector).Clone();

            QAssert.AreNotSame(vector, clone);
            QAssert.AreEqual(vector.Count, clone.Count);
            QAssert.CollectionAreEqual(vector, clone);
            vector[0] = 100;
            QAssert.CollectionAreNotEqual(vector, clone);
        }

        /// <summary>
        /// Test vectors equality.
        /// </summary>
        [Fact]
        public void testEquals()
        {
            var vector1 = new Vector(Data);
            var vector2 = new Vector(Data);
            var vector3 = new Vector(4);
            QAssert.IsTrue(vector1.Equals(vector1));
            QAssert.IsTrue(vector1.Equals(vector2));
            QAssert.IsFalse(vector1.Equals(vector3));
            QAssert.IsFalse(vector1.Equals(null));
            QAssert.IsFalse(vector1.Equals(2));
        }

        /// <summary>
        /// Test Vector hash code.
        /// </summary>
        [Fact]
        public void testHashCode()
        {
            var vector = new Vector(Data);
            QAssert.AreEqual(vector.GetHashCode(), vector.GetHashCode());
            QAssert.AreEqual(vector.GetHashCode(),
            new Vector(new List<double>() { 1, 2, 3, 4, 5 }).GetHashCode());
            QAssert.AreNotEqual(vector.GetHashCode(), new Vector(new List<double>() { 1 }).GetHashCode());
        }
    }
}
