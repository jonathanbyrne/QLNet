using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    public abstract class RelativeDateRateHelper : RateHelper
    {
        protected Date evaluationDate_;

        ///////////////////////////////////////////
        // constructors
        protected RelativeDateRateHelper(Handle<Quote> quote)
            : base(quote)
        {
            Settings.registerWith(update);
            evaluationDate_ = Settings.evaluationDate();
        }

        protected RelativeDateRateHelper(double quote)
            : base(quote)
        {
            Settings.registerWith(update);
            evaluationDate_ = Settings.evaluationDate();
        }

        //////////////////////////////////////
        //! Observer interface
        public override void update()
        {
            if (evaluationDate_ != Settings.evaluationDate())
            {
                evaluationDate_ = Settings.evaluationDate();
                initializeDates();
            }

            base.update();
        }

        ///////////////////////////////////////////
        protected abstract void initializeDates();
    }
}
