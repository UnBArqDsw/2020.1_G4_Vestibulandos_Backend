namespace Core.Event
{
    public class KIntEvent : IKEvent
    {
        public string Sender { get; set; }
        public ulong SenderUID { get; set; }
        public ushort EventID { get; set; }
        public object Buffer { get; set; }
        public int RetCode { get; set; }
        public int From { get; set; }
    }
}
