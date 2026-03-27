namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// The states of MDP Channel.
    /// </summary>
    public enum ChannelState
    {
        INITIAL,
        SYNC,
        OUTOFSYNC,
        CLOSING,
        CLOSED
    }
}
