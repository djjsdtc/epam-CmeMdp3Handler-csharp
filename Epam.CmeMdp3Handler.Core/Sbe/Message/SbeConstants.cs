namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public static class SbeConstants
    {
        public const int MDP_PACKET_MAX_SIZE        = 32768;
        public const int MESSAGE_SEQ_NUM_OFFSET     = 0;
        public const int MESSAGE_SENDING_TIME_OFFSET = 4;
        public const int MDP_HEADER_SIZE            = 12;
        public const int HEADER_SIZE                = 10;
        public const int MSG_SIZE_OFFSET            = 0;
        public const int BLOCK_LENGTH_OFFSET        = 2;
        public const int TEMPLATE_ID_OFFSET         = 4;
        public const int VERSION_OFFSET             = 6;
        public const int RESERVED_OFFSET            = 7;
        public const int MATCHEVENTINDICATOR_TAG    = 5799;
    }
}
