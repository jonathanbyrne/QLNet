/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 *
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

namespace QLNet.Indexes
{
    //! Region class, used for inflation applicability.
    [PublicAPI]
    public class Region
    {
        protected struct Data
        {
            public readonly string code;
            public readonly string name;

            public Data(string Name, string Code)
            {
                name = Name;
                code = Code;
            }
        }

        protected Data data_;

        protected Region()
        {
        }

        public static bool operator ==(Region r1, Region r2)
        {
            if (ReferenceEquals(r1, r2))
            {
                return true;
            }

            if ((object)r1 == null || (object)r2 == null)
            {
                return false;
            }

            return r1.Equals(r2);
        }

        public static bool operator !=(Region r1, Region r2) => !(r1 == r2);

        public string code() => data_.code;

        public override bool Equals(object o) => name() == ((Region)o).name();

        public override int GetHashCode() => 0;

        // Inspectors
        public string name() => data_.name;
    }

    //! Australia as geographical/economic region

    //! European Union as geographical/economic region

    //! France as geographical/economic region

    //! United Kingdom as geographical/economic region

    //! USA as geographical/economic region

    //! South Africa as geographical/economic region
}
