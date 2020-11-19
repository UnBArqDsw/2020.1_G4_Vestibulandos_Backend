using System;
using Core.Threading;

namespace Core.Network
{
    [Serializable]
    public sealed class AcceptEventArgs : EventArgs
    {
        /// <summary>
        /// TCP Client.
        /// </summary>
        public TcpClient Client { get; private set; }

        /// <summary>
        /// Job Processor.
        /// </summary>
        public JobProcessor JobProcessor { get; set; }

        /// <summary>
        /// Is Cancel.
        /// </summary>
        public bool Cancel { get; set; }

        //---------------------------------------------------------------------------------------------------
        public AcceptEventArgs(TcpClient tcpClient)
        {
            Client = tcpClient;
        }
    }
}
