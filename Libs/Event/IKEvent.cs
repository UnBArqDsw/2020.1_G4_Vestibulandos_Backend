namespace Core.Event
{
    public interface IKEvent 
    {
        ushort EventID { get; set; }
        
        object Buffer { get; set; }

        int RetCode { get; set; }
        
        int From { get; set; }
    }
}
