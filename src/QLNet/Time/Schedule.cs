/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2015 Francois Botha (igitur@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Exceptions;
using QLNet.Time.Calendars;

namespace QLNet.Time
{
    /// <summary>
    ///     Payment schedule
    /// </summary>
    [PublicAPI]
    public class Schedule
    {
        private Calendar calendar_;
        private BusinessDayConvention convention_;
        private List<Date> dates_ = new List<Date>();
        private bool? endOfMonth_;
        private Date firstDate_, nextToLastDate_;
        private IList<bool> isRegular_ = new List<bool>();
        private DateGeneration.Rule? rule_;
        private Period tenor_;
        private BusinessDayConvention? terminationDateConvention_;

        public Date this[int i]
        {
            get
            {
                if (i >= dates_.Count)
                {
                    throw new ArgumentException("i (" + i + ") must be less than or equal to " + (dates_.Count - 1));
                }

                return dates_[i];
            }
        }

        public int Count => dates_.Count;

        public Date at(int i) => this[i];

        public BusinessDayConvention businessDayConvention() => convention_;

        public Calendar calendar() => calendar_;

        public Date date(int i) => this[i];

        public List<Date> dates() => dates_;

        // Other inspectors
        public bool empty() => dates_.Count == 0;

        public Date endDate() => dates_.Last();

        public bool endOfMonth()
        {
            QLNet.Utils.QL_REQUIRE(endOfMonth_.HasValue, () => "full interface (end of month) not available");
            return endOfMonth_.Value;
        }

        public bool isRegular(int i)
        {
            QLNet.Utils.QL_REQUIRE(isRegular_.Count > 0, () => "full interface (isRegular) not available");
            QLNet.Utils.QL_REQUIRE(i <= isRegular_.Count && i > 0, () => "index (" + i + ") must be in [1, " + isRegular_.Count + "]");
            return isRegular_[i - 1];
        }

        public IList<bool> isRegular()
        {
            QLNet.Utils.QL_REQUIRE(isRegular_.Count > 0, () => "full interface (isRegular) not available");
            return isRegular_;
        }

        public Date nextDate(Date d)
        {
            var i = dates_.BinarySearch(d);
            if (i < 0 || i == dates_.Count - 1)
            {
                return null;
            }

            return dates_[i + 1];
        }

        public Date previousDate(Date d)
        {
            var i = dates_.BinarySearch(d);
            if (i <= 0)
            {
                return null;
            }

            return dates_[i - 1];
        }

        public DateGeneration.Rule rule()
        {
            QLNet.Utils.QL_REQUIRE(rule_.HasValue, () => "full interface (rule) not available");
            return rule_.Value;
        }

        // Date access
        public int size() => dates_.Count;

        public Date startDate() => dates_.First();

        public Period tenor()
        {
            QLNet.Utils.QL_REQUIRE(tenor_ != null, () => "full interface (tenor) not available");
            return tenor_;
        }

        public BusinessDayConvention terminationDateBusinessDayConvention()
        {
            QLNet.Utils.QL_REQUIRE(terminationDateConvention_.HasValue, () => "full interface (termination date business day convention) not available");
            return terminationDateConvention_.Value;
        }

        //! truncated schedule
        public Schedule until(Date truncationDate)
        {
            var result = (Schedule)MemberwiseClone();

            QLNet.Utils.QL_REQUIRE(truncationDate > result.dates_[0], () =>
                "truncation date " + truncationDate +
                " must be later than schedule first date " +
                result.dates_[0]);

            if (truncationDate < result.dates_.Last())
            {
                // remove later dates
                while (result.dates_.Last() > truncationDate)
                {
                    result.dates_.RemoveAt(dates_.Count - 1);
                    result.isRegular_.RemoveAt(isRegular_.Count - 1);
                }

                // add truncationDate if missing
                if (truncationDate != result.dates_.Last())
                {
                    result.dates_.Add(truncationDate);
                    result.isRegular_.Add(false);
                    result.terminationDateConvention_ = BusinessDayConvention.Unadjusted;
                }
                else
                {
                    result.terminationDateConvention_ = convention_;
                }

                if (result.nextToLastDate_ >= truncationDate)
                {
                    result.nextToLastDate_ = null;
                }

                if (result.firstDate_ >= truncationDate)
                {
                    result.firstDate_ = null;
                }
            }

            return result;
        }

        private bool allowsEndOfMonth(Period tenor) =>
            (tenor.units() == TimeUnit.Months || tenor.units() == TimeUnit.Years)
            && tenor >= new Period(1, TimeUnit.Months);

        private Date nextTwentieth(Date d, DateGeneration.Rule rule)
        {
            var result = new Date(20, d.month(), d.year());
            if (result < d)
            {
                result += new Period(1, TimeUnit.Months);
            }

            if (rule == DateGeneration.Rule.TwentiethIMM ||
                rule == DateGeneration.Rule.OldCDS ||
                rule == DateGeneration.Rule.CDS ||
                rule == DateGeneration.Rule.CDS2015)
            {
                var m = result.month();
                if (m % 3 != 0)
                {
                    // not a main IMM nmonth
                    var skip = 3 - m % 3;
                    result += new Period(skip, TimeUnit.Months);
                }
            }

            return result;
        }

        private Date previousTwentieth(Date d, DateGeneration.Rule rule)
        {
            var result = new Date(20, d.month(), d.year());
            if (result > d)
            {
                result -= new Period(1, TimeUnit.Months);
            }

            if (rule == DateGeneration.Rule.TwentiethIMM ||
                rule == DateGeneration.Rule.OldCDS ||
                rule == DateGeneration.Rule.CDS ||
                rule == DateGeneration.Rule.CDS2015)
            {
                var m = result.month();
                if (m % 3 != 0)
                {
                    // not a main IMM nmonth
                    var skip = m % 3;
                    result -= new Period(skip, TimeUnit.Months);
                }
            }

            return result;
        }

        #region Constructors

        public Schedule()
        {
        }

        /// <summary>
        ///     constructor that takes any list of dates, and optionally
        ///     meta information that can be used by client classes. Note
        ///     that neither the list of dates nor the meta information is
        ///     checked for plausibility in any sense.
        /// </summary>
        /// <param name="dates"></param>
        /// <param name="calendar"></param>
        /// <param name="convention"></param>
        /// <param name="terminationDateConvention"></param>
        /// <param name="tenor"></param>
        /// <param name="rule"></param>
        /// <param name="endOfMonth"></param>
        /// <param name="isRegular"></param>
        public Schedule(List<Date> dates,
            Calendar calendar = null,
            BusinessDayConvention convention = BusinessDayConvention.Unadjusted,
            BusinessDayConvention? terminationDateConvention = null,
            Period tenor = null,
            DateGeneration.Rule? rule = null,
            bool? endOfMonth = null,
            IList<bool> isRegular = null)
        {
            calendar_ = calendar ?? new NullCalendar();
            isRegular_ = isRegular ?? new List<bool>();

            tenor_ = tenor;
            convention_ = convention;
            terminationDateConvention_ = terminationDateConvention;
            rule_ = rule;
            endOfMonth_ = tenor != null && !allowsEndOfMonth(tenor) ? false : endOfMonth;
            dates_ = dates;

            QLNet.Utils.QL_REQUIRE(isRegular_.Count == 0 || isRegular_.Count == dates.Count - 1, () =>
                string.Format("isRegular size ({0}) must be zero or equal to the number of dates minus 1 ({1})", isRegular_.Count, dates.Count - 1));
        }

        /// <summary>
        ///     rule based constructor
        /// </summary>
        /// <param name="effectiveDate"></param>
        /// <param name="terminationDate"></param>
        /// <param name="tenor"></param>
        /// <param name="calendar"></param>
        /// <param name="convention"></param>
        /// <param name="terminationDateConvention"></param>
        /// <param name="rule"></param>
        /// <param name="endOfMonth"></param>
        /// <param name="firstDate"></param>
        /// <param name="nextToLastDate"></param>
        public Schedule(Date effectiveDate,
            Date terminationDate,
            Period tenor,
            Calendar calendar,
            BusinessDayConvention convention,
            BusinessDayConvention terminationDateConvention,
            DateGeneration.Rule rule,
            bool endOfMonth,
            Date firstDate = null,
            Date nextToLastDate = null)
        {
            calendar_ = calendar ?? new NullCalendar();
            firstDate_ = firstDate == effectiveDate ? null : firstDate;
            nextToLastDate_ = nextToLastDate == terminationDate ? null : nextToLastDate;

            tenor_ = tenor;
            convention_ = convention;
            terminationDateConvention_ = terminationDateConvention;
            rule_ = rule;
            endOfMonth_ = allowsEndOfMonth(tenor) && endOfMonth;

            // sanity checks
            QLNet.Utils.QL_REQUIRE(terminationDate != null, () => "null termination date");

            // in many cases (e.g. non-expired bonds) the effective date is not
            // really necessary. In these cases a decent placeholder is enough
            if (effectiveDate == null && firstDate == null && rule == DateGeneration.Rule.Backward)
            {
                var evalDate = Settings.evaluationDate();
                QLNet.Utils.QL_REQUIRE(evalDate < terminationDate, () => "null effective date", QLNetExceptionEnum.NullEffectiveDate);
                int y;
                if (nextToLastDate != null)
                {
                    y = (nextToLastDate - evalDate) / 366 + 1;
                    effectiveDate = nextToLastDate - new Period(y, TimeUnit.Years);
                }
                else
                {
                    y = (terminationDate - evalDate) / 366 + 1;
                    effectiveDate = terminationDate - new Period(y, TimeUnit.Years);
                }

                // More accurate , is the previous coupon date
                if (effectiveDate > evalDate)
                {
                    effectiveDate = effectiveDate - new Period(tenor_.length(), TimeUnit.Months);
                }
                else if (effectiveDate + new Period(tenor_.length(), TimeUnit.Months) < evalDate)
                {
                    effectiveDate = effectiveDate + new Period(tenor_.length(), TimeUnit.Months);
                }
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(effectiveDate != null, () => "null effective date", QLNetExceptionEnum.NullEffectiveDate);
            }

            QLNet.Utils.QL_REQUIRE(effectiveDate < terminationDate, () =>
                "effective date (" + effectiveDate +
                ") later than or equal to termination date (" +
                terminationDate + ")"
            );

            if (tenor_.length() == 0)
            {
                rule_ = DateGeneration.Rule.Zero;
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(tenor.length() > 0, () => "non positive tenor (" + tenor + ") not allowed");
            }

            if (firstDate_ != null)
            {
                switch (rule_.Value)
                {
                    case DateGeneration.Rule.Backward:
                    case DateGeneration.Rule.Forward:
                        QLNet.Utils.QL_REQUIRE(firstDate_ > effectiveDate &&
                                                        firstDate_ < terminationDate, () =>
                            "first date (" + firstDate_ + ") out of effective-termination date range [" +
                            effectiveDate + ", " + terminationDate + ")");
                        // we should ensure that the above condition is still verified after adjustment
                        break;
                    case DateGeneration.Rule.ThirdWednesday:
                        QLNet.Utils.QL_REQUIRE(IMM.isIMMdate(firstDate_, false), () => "first date (" + firstDate_ + ") is not an IMM date");
                        break;
                    case DateGeneration.Rule.Zero:
                    case DateGeneration.Rule.Twentieth:
                    case DateGeneration.Rule.TwentiethIMM:
                    case DateGeneration.Rule.OldCDS:
                    case DateGeneration.Rule.CDS:
                    case DateGeneration.Rule.CDS2015:
                        QLNet.Utils.QL_FAIL("first date incompatible with " + rule_.Value + " date generation rule");
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown rule (" + rule_.Value + ")");
                        break;
                }
            }

            if (nextToLastDate_ != null)
            {
                switch (rule_.Value)
                {
                    case DateGeneration.Rule.Backward:
                    case DateGeneration.Rule.Forward:
                        QLNet.Utils.QL_REQUIRE(nextToLastDate_ > effectiveDate && nextToLastDate_ < terminationDate, () =>
                            "next to last date (" + nextToLastDate_ + ") out of effective-termination date range (" +
                            effectiveDate + ", " + terminationDate + "]");
                        // we should ensure that the above condition is still verified after adjustment
                        break;
                    case DateGeneration.Rule.ThirdWednesday:
                        QLNet.Utils.QL_REQUIRE(IMM.isIMMdate(nextToLastDate_, false), () => "next-to-last date (" + nextToLastDate_ +
                                                                                                     ") is not an IMM date");
                        break;
                    case DateGeneration.Rule.Zero:
                    case DateGeneration.Rule.Twentieth:
                    case DateGeneration.Rule.TwentiethIMM:
                    case DateGeneration.Rule.OldCDS:
                    case DateGeneration.Rule.CDS:
                    case DateGeneration.Rule.CDS2015:
                        QLNet.Utils.QL_FAIL("next to last date incompatible with " + rule_.Value + " date generation rule");
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown rule (" + rule_.Value + ")");
                        break;
                }
            }

            // calendar needed for endOfMonth adjustment
            Calendar nullCalendar = new NullCalendar();
            var periods = 1;
            Date seed = new Date(), exitDate = new Date();
            switch (rule_.Value)
            {
                case DateGeneration.Rule.Zero:
                    tenor_ = new Period(0, TimeUnit.Years);
                    dates_.Add(effectiveDate);
                    dates_.Add(terminationDate);
                    isRegular_.Add(true);
                    break;

                case DateGeneration.Rule.Backward:
                    dates_.Add(terminationDate);

                    seed = terminationDate;
                    if (nextToLastDate_ != null)
                    {
                        dates_.Insert(0, nextToLastDate_);
                        var temp = nullCalendar.advance(seed, -periods * tenor_, convention_, endOfMonth_.Value);
                        if (temp != nextToLastDate_)
                        {
                            isRegular_.Insert(0, false);
                        }
                        else
                        {
                            isRegular_.Insert(0, true);
                        }

                        seed = nextToLastDate_;
                    }

                    exitDate = effectiveDate;
                    if (firstDate_ != null)
                    {
                        exitDate = firstDate_;
                    }

                    while (true)
                    {
                        var temp = nullCalendar.advance(seed, -periods * tenor_, convention_, endOfMonth_.Value);
                        if (temp < exitDate)
                        {
                            if (firstDate_ != null && calendar_.adjust(dates_.First(), convention_) !=
                                calendar_.adjust(firstDate_, convention_))
                            {
                                dates_.Insert(0, firstDate_);
                                isRegular_.Insert(0, false);
                            }

                            break;
                        }

                        // skip dates that would result in duplicates
                        // after adjustment
                        if (calendar_.adjust(dates_.First(), convention_) != calendar_.adjust(temp, convention_))
                        {
                            dates_.Insert(0, temp);
                            isRegular_.Insert(0, true);
                        }

                        ++periods;
                    }

                    if (calendar_.adjust(dates_.First(), convention) != calendar_.adjust(effectiveDate, convention))
                    {
                        dates_.Insert(0, effectiveDate);
                        isRegular_.Insert(0, false);
                    }

                    break;

                case DateGeneration.Rule.Twentieth:
                case DateGeneration.Rule.TwentiethIMM:
                case DateGeneration.Rule.ThirdWednesday:
                case DateGeneration.Rule.OldCDS:
                case DateGeneration.Rule.CDS:
                case DateGeneration.Rule.CDS2015:
                    QLNet.Utils.QL_REQUIRE(!endOfMonth, () => "endOfMonth convention incompatible with " + rule_.Value + " date generation rule");
                    goto case DateGeneration.Rule.Forward; // fall through

                case DateGeneration.Rule.Forward:
                    if (rule_.Value == DateGeneration.Rule.CDS ||
                        rule_.Value == DateGeneration.Rule.CDS2015)
                    {
                        dates_.Add(previousTwentieth(effectiveDate, rule_.Value));
                    }
                    else
                    {
                        dates_.Add(effectiveDate);
                    }

                    seed = dates_.Last();
                    if (firstDate_ != null)
                    {
                        dates_.Add(firstDate_);
                        var temp = nullCalendar.advance(seed, periods * tenor_, convention_, endOfMonth_.Value);
                        if (temp != firstDate_)
                        {
                            isRegular_.Add(false);
                        }
                        else
                        {
                            isRegular_.Add(true);
                        }

                        seed = firstDate_;
                    }
                    else if (rule_.Value == DateGeneration.Rule.Twentieth ||
                             rule_.Value == DateGeneration.Rule.TwentiethIMM ||
                             rule_.Value == DateGeneration.Rule.OldCDS ||
                             rule_.Value == DateGeneration.Rule.CDS ||
                             rule_.Value == DateGeneration.Rule.CDS2015)
                    {
                        var next20th = nextTwentieth(effectiveDate, rule_.Value);
                        if (rule_ == DateGeneration.Rule.OldCDS)
                        {
                            // distance rule inforced in natural days
                            long stubDays = 30;
                            if (next20th - effectiveDate < stubDays)
                            {
                                // +1 will skip this one and get the next
                                next20th = nextTwentieth(next20th + 1, rule_.Value);
                            }
                        }

                        if (next20th != effectiveDate)
                        {
                            dates_.Add(next20th);
                            isRegular_.Add(false);
                            seed = next20th;
                        }
                    }

                    exitDate = terminationDate;
                    if (nextToLastDate_ != null)
                    {
                        exitDate = nextToLastDate_;
                    }

                    if (rule_ == DateGeneration.Rule.CDS2015
                        && nextTwentieth(terminationDate, rule_.Value) == terminationDate
                        && terminationDate.month() % 2 == 1)
                    {
                        exitDate = nextTwentieth(terminationDate + 1, rule_.Value);
                    }

                    while (true)
                    {
                        var temp = nullCalendar.advance(seed, periods * tenor_, convention_, endOfMonth_.Value);
                        if (temp > exitDate)
                        {
                            if (nextToLastDate_ != null &&
                                calendar_.adjust(dates_.Last(), convention_) != calendar_.adjust(nextToLastDate_, convention_))
                            {
                                dates_.Add(nextToLastDate_);
                                isRegular_.Add(false);
                            }

                            break;
                        }

                        // skip dates that would result in duplicates
                        // after adjustment
                        if (calendar_.adjust(dates_.Last(), convention_) != calendar_.adjust(temp, convention_))
                        {
                            dates_.Add(temp);
                            isRegular_.Add(true);
                        }

                        ++periods;
                    }

                    if (calendar_.adjust(dates_.Last(), terminationDateConvention_.Value) !=
                        calendar_.adjust(terminationDate, terminationDateConvention_.Value))
                    {
                        if (rule_.Value == DateGeneration.Rule.Twentieth ||
                            rule_.Value == DateGeneration.Rule.TwentiethIMM ||
                            rule_.Value == DateGeneration.Rule.OldCDS ||
                            rule_.Value == DateGeneration.Rule.CDS)
                        {
                            dates_.Add(nextTwentieth(terminationDate, rule_.Value));
                            isRegular_.Add(true);
                        }
                        else if (rule_ == DateGeneration.Rule.CDS2015)
                        {
                            var tentativeTerminationDate = nextTwentieth(terminationDate, rule_.Value);
                            if (tentativeTerminationDate.month() % 2 == 0)
                            {
                                dates_.Add(tentativeTerminationDate);
                                isRegular_.Add(true);
                            }
                        }
                        else
                        {
                            dates_.Add(terminationDate);
                            isRegular_.Add(false);
                        }
                    }

                    break;

                default:
                    QLNet.Utils.QL_FAIL("unknown rule (" + rule_.Value + ")");
                    break;
            }

            // adjustments
            if (rule_ == DateGeneration.Rule.ThirdWednesday)
            {
                for (var i = 1; i < dates_.Count - 1; ++i)
                {
                    dates_[i] = Date.nthWeekday(3, DayOfWeek.Wednesday, dates_[i].Month, dates_[i].Year);
                }
            }

            if (endOfMonth && calendar_.isEndOfMonth(seed))
            {
                // adjust to end of month
                if (convention_ == BusinessDayConvention.Unadjusted)
                {
                    for (var i = 1; i < dates_.Count - 1; ++i)
                    {
                        dates_[i] = Date.endOfMonth(dates_[i]);
                    }
                }
                else
                {
                    for (var i = 1; i < dates_.Count - 1; ++i)
                    {
                        dates_[i] = calendar_.endOfMonth(dates_[i]);
                    }
                }

                if (terminationDateConvention_ != BusinessDayConvention.Unadjusted)
                {
                    dates_[0] = calendar_.endOfMonth(dates_.First());
                    dates_[dates_.Count - 1] = calendar_.endOfMonth(dates_.Last());
                }
                else
                {
                    // the termination date is the first if going backwards,
                    // the last otherwise.
                    if (rule_ == DateGeneration.Rule.Backward)
                    {
                        dates_[dates_.Count - 1] = Date.endOfMonth(dates_.Last());
                    }
                    else
                    {
                        dates_[0] = Date.endOfMonth(dates_.First());
                    }
                }
            }
            else
            {
                // first date not adjusted for CDS schedules
                if (rule_ != DateGeneration.Rule.OldCDS)
                {
                    dates_[0] = calendar_.adjust(dates_[0], convention_);
                }

                for (var i = 1; i < dates_.Count - 1; ++i)
                {
                    dates_[i] = calendar_.adjust(dates_[i], convention_);
                }

                // termination date is NOT adjusted as per ISDA specifications, unless otherwise specified in the
                // confirmation of the deal or unless we're creating a CDS schedule
                if (terminationDateConvention_.Value != BusinessDayConvention.Unadjusted
                    && rule_.Value != DateGeneration.Rule.CDS
                    && rule_.Value != DateGeneration.Rule.CDS2015)
                {
                    dates_[dates_.Count - 1] = calendar_.adjust(dates_.Last(), terminationDateConvention_.Value);
                }
            }

            // Final safety checks to remove extra next-to-last date, if
            // necessary.  It can happen to be equal or later than the end
            // date due to EOM adjustments (see the Schedule test suite
            // for an example).
            if (dates_.Count >= 2 && dates_[dates_.Count - 2] >= dates_.Last())
            {
                isRegular_[isRegular_.Count - 2] = dates_[dates_.Count - 2] == dates_.Last();
                dates_[dates_.Count - 2] = dates_.Last();

                dates_.RemoveAt(dates_.Count - 1);
                isRegular_.RemoveAt(isRegular_.Count - 1);
            }

            if (dates_.Count >= 2 && dates_[1] <= dates_.First())
            {
                isRegular_[1] = dates_[1] == dates_.First();
                dates_[1] = dates_.First();
                dates_.RemoveAt(0);
                isRegular_.RemoveAt(0);
            }

            QLNet.Utils.QL_REQUIRE(dates_.Count > 1,
                () => "degenerate single date (" + dates_[0] + ") schedule" +
                      "\n seed date: " + seed +
                      "\n exit date: " + exitDate +
                      "\n effective date: " + effectiveDate +
                      "\n first date: " + firstDate +
                      "\n next to last date: " + nextToLastDate +
                      "\n termination date: " + terminationDate +
                      "\n generation rule: " + rule_.Value +
                      "\n end of month: " + endOfMonth_.Value);
        }

        #endregion
    }
}
