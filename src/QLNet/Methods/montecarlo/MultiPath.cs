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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Methods.montecarlo
{
    //! Correlated multiple asset paths
    /*! MultiPath contains the list of paths for each asset, i.e.,
        multipath[j] is the path followed by the j-th asset.

        \ingroup mcarlo
    */
    [PublicAPI]
    public class MultiPath : IPath
    {
        private List<Path> multiPath_;

        public MultiPath()
        {
        }

        public MultiPath(int nAsset, TimeGrid timeGrid)
        {
            multiPath_ = new List<Path>(nAsset);
            for (var i = 0; i < nAsset; i++)
            {
                multiPath_.Add(new Path(timeGrid));
            }

            Utils.QL_REQUIRE(nAsset > 0, () => "number of asset must be positive");
        }

        public MultiPath(List<Path> multiPath)
        {
            multiPath_ = multiPath;
        }

        // read/write access to components
        public Path this[int j]
        {
            get => multiPath_[j];
            set => multiPath_[j] = value;
        }

        // inspectors
        public int assetNumber() => multiPath_.Count;

        // ICloneable interface
        public object Clone()
        {
            var temp = (MultiPath)MemberwiseClone();
            temp.multiPath_ = new List<Path>(multiPath_);
            return temp;
        }

        public int length() => pathSize();

        public int pathSize() => multiPath_[0].length();
    }
}
