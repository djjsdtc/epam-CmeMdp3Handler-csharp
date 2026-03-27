using System;

namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum MDEntryType
    {
        Bid,
        Offer,
        Trade,
        OpeningPrice,
        SettlementPrice,
        TradingSessionHighPrice,
        TradingSessionLowPrice,
        TradeVolume,
        OpenInterest,
        ImpliedBid,
        ImpliedOffer,
        EmptyBook,
        SessionHighBid,
        SessionLowOffer,
        FixingPrice,
        ElectronicVolume,
        ThresholdLimitsandPriceBandVariation
    }

    public static class MDEntryTypeExtensions
    {
        public static MDEntryType FromFIX(char fixValue) => fixValue switch
        {
            '0' => MDEntryType.Bid,
            '1' => MDEntryType.Offer,
            '2' => MDEntryType.Trade,
            '4' => MDEntryType.OpeningPrice,
            '6' => MDEntryType.SettlementPrice,
            '7' => MDEntryType.TradingSessionHighPrice,
            '8' => MDEntryType.TradingSessionLowPrice,
            'B' => MDEntryType.TradeVolume,
            'C' => MDEntryType.OpenInterest,
            'E' => MDEntryType.ImpliedBid,
            'F' => MDEntryType.ImpliedOffer,
            'J' => MDEntryType.EmptyBook,
            'N' => MDEntryType.SessionHighBid,
            'O' => MDEntryType.SessionLowOffer,
            'W' => MDEntryType.FixingPrice,
            'e' => MDEntryType.ElectronicVolume,
            'g' => MDEntryType.ThresholdLimitsandPriceBandVariation,
            _   => throw new ArgumentException($"Unknown MDEntryType FIX value: {fixValue}")
        };
    }
}
