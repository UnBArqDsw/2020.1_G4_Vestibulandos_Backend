namespace Core.Constants
{
    public struct NetworkConst
    {
        /// <summary>
        /// Max packet size.
        /// </summary>
        public const int MAX_PACKET_SIZE = 48 * 1024;

        /// <summary>
        /// Max concurrent users allowed.
        /// </summary>
        public const int MAX_CCU = 2000;

        /// <summary>
        /// Max send packet count can be processed in the while.
        /// </summary>
        public const int MAX_SEND_PACKET_COUNT = 64;
    }
}
