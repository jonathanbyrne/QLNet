//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    /// <summary>
    ///     Catastrophe Bond - CAT
    ///     <remarks>
    ///         A catastrophe bond (CAT) is a high-yield debt instrument that is usually
    ///         insurance-linked and meant to raise money in case of a catastrophe such
    ///         as a hurricane or earthquake.
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class CatBond : Bond
    {
        public new class Arguments : Bond.Arguments
        {
            public NotionalRisk notionalRisk;
            public Date startDate;

            public override void validate()
            {
                base.validate();
                QLNet.Utils.QL_REQUIRE(notionalRisk != null, () => "null notionalRisk");
            }
        }

        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        public new class Results : Bond.Results
        {
            public double exhaustionProbability;
            public double expectedLoss;
            public double lossProbability;
        }

        protected double exhaustionProbability_;
        protected double expectedLoss_;
        protected double lossProbability_;
        protected NotionalRisk notionalRisk_;

        public CatBond(int settlementDays,
            Calendar calendar,
            Date issueDate,
            NotionalRisk notionalRisk)
            : base(settlementDays, calendar, issueDate)
        {
            notionalRisk_ = notionalRisk;
        }

        public double exhaustionProbability() => exhaustionProbability_;

        public double expectedLoss() => expectedLoss_;

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);
            var results = r as Results;
            QLNet.Utils.QL_REQUIRE(results != null, () => "wrong result ExerciseType");

            lossProbability_ = results.lossProbability;
            expectedLoss_ = results.expectedLoss;
            exhaustionProbability_ = results.exhaustionProbability;
        }

        public double lossProbability() => lossProbability_;

        public override void setupArguments(IPricingEngineArguments args)
        {
            var arguments = args as Arguments;
            QLNet.Utils.QL_REQUIRE(arguments != null, () => "wrong arguments ExerciseType");

            base.setupArguments(args);

            arguments.notionalRisk = notionalRisk_;
            arguments.startDate = issueDate();
        }
    }
}
