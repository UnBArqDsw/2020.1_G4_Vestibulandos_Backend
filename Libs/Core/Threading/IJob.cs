namespace Core.Threading
{
    public interface IJob
    {
        long EnqueueTick { get; set; }
        long StartTick { get; set; }
        long EndTick { get; set; }

        //---------------------------------------------------------------------------------------------------
        void Do();
    }
}
