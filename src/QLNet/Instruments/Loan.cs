﻿/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class Loan : Instrument
    {
        public enum Type { Deposit = -1, Loan = 1 }
        public enum Amortising
        {
            Bullet = 1,
            Step = 2,
            French = 3
        }
        protected List<List<CashFlow>> legs_;
        protected List<double> payer_;
        protected List<double> notionals_;
        protected List<double?> legNPV_;

        public Loan(int legs)
        {
            legs_ = new InitializedList<List<CashFlow>>(legs);
            payer_ = new InitializedList<double>(legs);
            notionals_ = new List<double>();
            legNPV_ = new InitializedList<double?>(legs);
        }

        ///////////////////////////////////////////////////////////////////
        // Instrument interface
        public override bool isExpired()
        {
            var today = Settings.evaluationDate();
            return !legs_.Any(leg => leg.Any(cf => !cf.hasOccurred(today)));
        }

        protected override void setupExpired()
        {
            base.setupExpired();
            legNPV_ = new InitializedList<double?>(legNPV_.Count);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            var arguments = args as Arguments;
            if (arguments == null)
                throw new ArgumentException("wrong argument ExerciseType");

            arguments.legs = legs_;
            arguments.payer = payer_;
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);

            var results = r as Results;
            if (results == null)
                throw new ArgumentException("wrong result ExerciseType");

            if (results.legNPV.Count != 0)
            {
                if (results.legNPV.Count != legNPV_.Count)
                    throw new ArgumentException("wrong number of leg NPV returned");
                legNPV_ = new List<double?>(results.legNPV);
            }
            else
            {
                legNPV_ = new InitializedList<double?>(legNPV_.Count);
            }

        }

        ////////////////////////////////////////////////////////////////
        // arguments, results, pricing engine
        [JetBrains.Annotations.PublicAPI] public class Arguments : IPricingEngineArguments
        {
            public List<List<CashFlow>> legs { get; set; }
            public List<double> payer { get; set; }
            public virtual void validate()
            {
                if (legs.Count != payer.Count)
                    throw new ArgumentException("number of legs and multipliers differ");
            }
        }

        public new class Results : Instrument.Results
        {
            public List<double?> legNPV { get; set; }
            public override void reset()
            {
                base.reset();
                // clear all previous results
                if (legNPV == null)
                    legNPV = new List<double?>();
                else
                    legNPV.Clear();
            }
        }

        [JetBrains.Annotations.PublicAPI] public class Engine : GenericEngine<Arguments, Results> { }

    }
}
