using System;
using System.Net;
using System.Net.Sockets;
using Core.Threading;

namespace Core.Network
{
    public class TcpClient
    {
        /// <summary>
        /// Connection Succeed Event.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionSucceed = null;

        /// <summary>
        /// Connection Fail Event.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> ConnectionFail = null;

        /// <summary>
        /// Packet Receive Event.
        /// </summary>
        public event EventHandler<EventArgs<ArraySegment<byte>>> PacketReceive = null;

        /// <summary>
        /// Disconnected Event.
        /// </summary>
        public event EventHandler<EventArgs> Disconnected = null;

        /// <summary>
        /// Exception Occur Event.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> ExceptionOccur = null;

        /// <summary>
        /// Check if session is active.
        /// </summary>
        private bool m_bActive = false;

        /// <summary>
        /// Socket.
        /// </summary>
        private IAsyncSocket m_asyncSocket = null;

        public long TotalReceived => (m_asyncSocket != null) ? m_asyncSocket.TotalReceived : 0;
        public long TotalSent => (m_asyncSocket != null) ? m_asyncSocket.TotalSent : 0;

        public int TotalReceivedCount => (m_asyncSocket != null) ? m_asyncSocket.TotalReceivedCount : 0;
        public int TotalSentCount => (m_asyncSocket != null) ? m_asyncSocket.TotalSentCount : 0;

        public object Tag { get; set; }

        public bool Connected => m_asyncSocket != null && m_asyncSocket.Connected;

        public IPEndPoint LocalEndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public IntPtr Handle { get; private set; }

        //---------------------------------------------------------------------------------------------------
        internal void Activate(Socket connectedSocket, JobProcessor jobProcessor)
        {
            try
            {
                m_bActive = true;

                CreateAsyncSocket(connectedSocket, jobProcessor);

                LocalEndPoint = (connectedSocket.LocalEndPoint as IPEndPoint);
                RemoteEndPoint = (connectedSocket.RemoteEndPoint as IPEndPoint);

                Handle = connectedSocket.Handle;

                //OnConnectionSucceed(EventArgs.Empty);
                m_asyncSocket.Activate();
            }
            catch (Exception ex)
            {
                m_bActive = false;
                OnConnectionFail(new EventArgs<Exception>(ex));
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Connect(JobProcessor jobProcessor, IPEndPoint remoteEndPoint)
        {
            if (m_bActive)
            {
                throw new InvalidOperationException("Can't use on connected instance");
            }

            m_bActive = true;
            RemoteEndPoint = remoteEndPoint;

            // Initialize the socket.
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(remoteEndPoint, EndConnect, new object[]
            {
                socket,
                jobProcessor
            });

            LocalEndPoint = (socket.LocalEndPoint as IPEndPoint);
            Handle = socket.Handle;
        }

        //---------------------------------------------------------------------------------------------------
        public void Connect(JobProcessor jobProcessor, IPAddress ipAddress, int nPort)
        {
            Connect(jobProcessor, new IPEndPoint(ipAddress, nPort));
        }

        //---------------------------------------------------------------------------------------------------
        public void Connect(JobProcessor jobProcessor, string strIP, int nPort)
        {
            if (IPAddress.TryParse(strIP, out IPAddress ipAddress))
            {
                Connect(jobProcessor, ipAddress, nPort);
                return;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Disconnect()
        {
            if (m_asyncSocket != null)
            {
                m_asyncSocket.Shutdown();
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Send(ArraySegment<byte> packet)
        {
            if (m_asyncSocket != null)
            {
                m_asyncSocket.Send(packet);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public /*protected*/ virtual void OnConnectionSucceed(EventArgs e)
        {
            ConnectionSucceed?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnConnectionFail(EventArgs<Exception> e)
        {
            ConnectionFail?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnPacketReceive(EventArgs<ArraySegment<byte>> e)
        {
            PacketReceive?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnDisconnected(EventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnExceptionOccur(EventArgs<Exception> e)
        {
            ExceptionOccur?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        private void EndConnect(IAsyncResult ar)
        {
            object[] arrAsync = (object[])ar.AsyncState;

            // Get the socket from args.
            Socket socket = (Socket)arrAsync[0];

            // Get the  job processor from args.
            JobProcessor job = (JobProcessor)arrAsync[1];

            if (job != null && !job.IsInThread())
            {
                job.Enqueue(Job.Create(EndConnect, ar));
                return;
            }

            try
            {
                socket.EndConnect(ar);

                CreateAsyncSocket(socket, job);
                
                //OnConnectionSucceed(EventArgs.Empty);
                m_asyncSocket.Activate();
            }
            catch (Exception exception)
            {
                m_bActive = false;
                OnConnectionFail(new EventArgs<Exception>(exception));
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void CreateAsyncSocket(Socket socket, JobProcessor jobProcessor)
        {
            m_asyncSocket = CreateAsyncSocket(socket);

            if (jobProcessor == null)
            {
                m_asyncSocket.PacketReceive += (sender, e) => { OnPacketReceive(e); };
                m_asyncSocket.SocketException += (sender, e) => { OnExceptionOccur(e); };
                m_asyncSocket.SocketClose += (sender, e) => { m_bActive = false; OnDisconnected(e); };

                return;
            }

            m_asyncSocket.PacketReceive += (sender, e) =>
            {
                jobProcessor.Enqueue(Job.Create(OnPacketReceive, e));
            };

            m_asyncSocket.SocketException += (sender, e) =>
            {
                jobProcessor.Enqueue(Job.Create(OnExceptionOccur, e));
            };

            m_asyncSocket.SocketClose += (sender, e) =>
            {
                m_bActive = false;
                jobProcessor.Enqueue(Job.Create(OnDisconnected, e));
            };
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual IAsyncSocket CreateAsyncSocket(Socket socket)
        {
            return new AsyncTcpWorker(socket);
        }
    }
}
