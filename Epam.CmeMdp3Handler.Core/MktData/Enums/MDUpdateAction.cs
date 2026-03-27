using System;

namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum MDUpdateAction
    {
        New        = 0,
        Change     = 1,
        Delete     = 2,
        DeleteThru = 3,
        DeleteFrom = 4,
        Overlay    = 5
    }

    public static class MDUpdateActionExtensions
    {
        public static MDUpdateAction FromFIX(short fixValue) => fixValue switch
        {
            0 => MDUpdateAction.New,
            1 => MDUpdateAction.Change,
            2 => MDUpdateAction.Delete,
            3 => MDUpdateAction.DeleteThru,
            4 => MDUpdateAction.DeleteFrom,
            5 => MDUpdateAction.Overlay,
            _ => throw new ArgumentException($"Unknown MDUpdateAction FIX value: {fixValue}")
        };
    }
}
