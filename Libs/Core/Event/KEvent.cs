namespace Core.Event
{
    public class KEvent : IKEvent
    {
        public ushort EventID { get; set; }
        public object Buffer { get; set; }
        public int RetCode { get; set; }
        public int From { get; set; }

        public enum FROM_TYPE
        {
            FT_NONE = 0,
            FT_INNER,
            FT_OUTTER,

            FT_MAX
        }
    }
}
