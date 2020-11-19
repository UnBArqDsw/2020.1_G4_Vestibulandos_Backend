namespace Core.Network
{
    public class TcpServer : TcpServerBase<TcpClient2>
    {
        //---------------------------------------------------------------------------------------------------
        static TcpServer()
        {
            TcpClient2.Init();
        }
    }
}
