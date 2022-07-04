﻿/*
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

namespace QLNet.Indexes
{
    //! Region class, used for inflation applicability.
    [JetBrains.Annotations.PublicAPI] public class Region
    {
        // Inspectors
        public string name() => data_.name;

        public string code() => data_.code;

        protected Region() { }
        protected struct Data
        {
            public readonly string name;
            public readonly string code;

            public Data(string Name, string Code)
            {
                name = Name;
                code = Code;
            }
        }
        protected Data data_;

        public static bool operator ==(Region r1, Region r2)
        {
            if (ReferenceEquals(r1, r2))
                return true;
            if ((object)r1 == null || (object)r2 == null)
                return false;
            return r1.Equals(r2);
        }

        public static bool operator !=(Region r1, Region r2) => !(r1 == r2);

        public override bool Equals(object o) => name() == ((Region)o).name();

        public override int GetHashCode() => 0;
    }

    //! Australia as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class AustraliaRegion : Region
    {
        public AustraliaRegion()
        {
            var AUdata = new Data("Australia", "AU");
            data_ = AUdata;
        }

    }

    //! European Union as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class EURegion : Region
    {
        public EURegion()
        {
            var EUdata = new Data("EU", "EU");
            data_ = EUdata;
        }
    }

    //! France as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class FranceRegion : Region
    {
        public FranceRegion()
        {
            var FRdata = new Data("France", "FR");
            data_ = FRdata;
        }
    }


    //! United Kingdom as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class UKRegion : Region
    {
        public UKRegion()
        {
            var UKdata = new Data("UK", "UK");
            data_ = UKdata;
        }
    }

    //! USA as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class USRegion : Region
    {
        public USRegion()
        {
            var USdata = new Data("USA", "US");
            data_ = USdata;
        }
    }

    //! South Africa as geographical/economic region
    [JetBrains.Annotations.PublicAPI] public class ZARegion : Region
    {
        public ZARegion()
        {
            var ZAdata = new Data("South Africa", "ZA");
            data_ = ZAdata;
        }
    }
}
