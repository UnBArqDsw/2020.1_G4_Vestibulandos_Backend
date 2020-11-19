using System.Net.Sockets;

namespace Core.Network
{
    public class TcpClient2 : TcpClient
    {
        //---------------------------------------------------------------------------------------------------
        protected override IAsyncSocket CreateAsyncSocket(Socket socket)
        {
            return new AsyncSocket(socket);
        }

        //---------------------------------------------------------------------------------------------------
        public static void Init()
        {
            AsyncSocket.Init();
        }
    }
}
