using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Core.Network;
using Core.Threading;
using log4net;

namespace Core.PipedNetwork
{
    public class Acceptor
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public event Action<Peer> OnAccepted;

        private ITcpServer _acceptor;

        internal JobProcessor Thread { get; private set; }

        public IPEndPoint EndPointAddress => _acceptor.LocalEndPoint;

        public Acceptor(JobProcessor thread) 
            : this(new TcpServer(), thread)
        {
        }

        public Acceptor(ITcpServer tcpServer, JobProcessor thread)
        {
            _acceptor = tcpServer;

            Thread = thread;

            _acceptor.ClientAccept += OnAcceptorClientAccept;
            _acceptor.ExceptionOccur += OnAcceptorExceptionOccur;
        }

        public void Start(int port)
        {
            _acceptor.Start(Thread, port);
        }

        public void Stop()
        {
            _acceptor.Stop();
        }

        private void OnAcceptorExceptionOccur(object sender, EventArgs<Exception> e)
        {
            _logger.FatalFormat("Exception occured in PipedNetwork.Acceptor. Exception: {0}", e.Value);
        }

        private void OnAcceptorClientAccept(object sender, AcceptEventArgs e)
        {
            e.JobProcessor = Thread;

            Peer obj = Peer.CreateFromServer(this, e.Client);
            OnAccepted?.Invoke(obj);
        }
    }
}