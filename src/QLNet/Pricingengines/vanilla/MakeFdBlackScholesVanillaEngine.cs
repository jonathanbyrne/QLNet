using JetBrains.Annotations;
using QLNet.Methods.Finitedifferences.Solvers;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.Processes;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class MakeFdBlackScholesVanillaEngine
    {
        protected FdBlackScholesVanillaEngine.CashDividendModel cashDividendModel_;
        protected double? illegalLocalVolOverwrite_;
        protected bool localVol_;
        protected GeneralizedBlackScholesProcess process_;
        protected FdmQuantoHelper quantoHelper_;
        protected FdmSchemeDesc schemeDesc_;
        protected int tGrid_, xGrid_, dampingSteps_;

        public MakeFdBlackScholesVanillaEngine(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
            tGrid_ = 100;
            xGrid_ = 100;
            dampingSteps_ = 0;
            schemeDesc_ = new FdmSchemeDesc().Douglas();
            localVol_ = false;
            illegalLocalVolOverwrite_ = null;
            quantoHelper_ = null;
            cashDividendModel_ = FdBlackScholesVanillaEngine.CashDividendModel.Spot;
        }

        public IPricingEngine getAsPricingEngine() =>
            new FdBlackScholesVanillaEngine(
                process_,
                quantoHelper_,
                tGrid_, xGrid_, dampingSteps_,
                schemeDesc_,
                localVol_,
                illegalLocalVolOverwrite_,
                cashDividendModel_);

        public MakeFdBlackScholesVanillaEngine withCashDividendModel(
            FdBlackScholesVanillaEngine.CashDividendModel cashDividendModel)
        {
            cashDividendModel_ = cashDividendModel;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withDampingSteps(int dampingSteps)
        {
            dampingSteps_ = dampingSteps;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withFdmSchemeDesc(FdmSchemeDesc schemeDesc)
        {
            schemeDesc_ = schemeDesc;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withIllegalLocalVolOverwrite(
            double illegalLocalVolOverwrite)
        {
            illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withLocalVol(bool localVol)
        {
            localVol_ = localVol;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withQuantoHelper(FdmQuantoHelper quantoHelper)
        {
            quantoHelper_ = quantoHelper;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withTGrid(int tGrid)
        {
            tGrid_ = tGrid;
            return this;
        }

        public MakeFdBlackScholesVanillaEngine withXGrid(int xGrid)
        {
            xGrid_ = xGrid;
            return this;
        }
    }
}
