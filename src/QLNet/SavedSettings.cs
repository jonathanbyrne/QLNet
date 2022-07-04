using System;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class SavedSettings : IDisposable
    {
        private Date evaluationDate_;
        private bool enforcesTodaysHistoricFixings_;
        private bool includeReferenceDateEvents_;
        private bool? includeTodaysCashFlows_;

        public SavedSettings()
        {
            evaluationDate_ = Settings.evaluationDate();
            enforcesTodaysHistoricFixings_ = Settings.enforcesTodaysHistoricFixings;
            includeReferenceDateEvents_ = Settings.includeReferenceDateEvents;
            includeTodaysCashFlows_ = Settings.includeTodaysCashFlows;
        }

        public void Dispose()
        {
            if (evaluationDate_ != Settings.evaluationDate())
                Settings.setEvaluationDate(evaluationDate_);
            Settings.enforcesTodaysHistoricFixings = enforcesTodaysHistoricFixings_;
            Settings.includeReferenceDateEvents = includeReferenceDateEvents_;
            Settings.includeTodaysCashFlows = includeTodaysCashFlows_;
        }
    }
}