using System;

namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum HaltReason
    {
        GroupSchedule      = 0,
        SurveillanceIntervention = 1,
        MarketEvent        = 2,
        InstrumentActivation = 3,
        InstrumentExpiration = 4,
        Unknown            = 5,
        RecoveryInProcess  = 6
    }

    public static class HaltReasonExtensions
    {
        public static HaltReason? FromFIX(short fixValue) => fixValue switch
        {
            0 => HaltReason.GroupSchedule,
            1 => HaltReason.SurveillanceIntervention,
            2 => HaltReason.MarketEvent,
            3 => HaltReason.InstrumentActivation,
            4 => HaltReason.InstrumentExpiration,
            5 => HaltReason.Unknown,
            6 => HaltReason.RecoveryInProcess,
            _ => null
        };
    }
}
