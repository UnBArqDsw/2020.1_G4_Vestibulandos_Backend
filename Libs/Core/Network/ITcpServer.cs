using System;
using System.Net;
using Core.Threading;

namespace Core.Network
{
    public interface ITcpServer
    {
        event EventHandler<AcceptEventArgs> ClientAccept;
        event EventHandler<EventArgs<Exception>> ExceptionOccur;

        object Tag { get; set; }

        IPEndPoint LocalEndPoint { get; }

        //---------------------------------------------------------------------------------------------------
        void Start(JobProcessor jobProcessor, int port, int backlog = 0x7FFFFFFF);

        //---------------------------------------------------------------------------------------------------
        void Start(JobProcessor jobProcessor, IPAddress bindAddress, int port, int backlog = 0x7FFFFFFF);

        //---------------------------------------------------------------------------------------------------
        void Stop();
    }
}
