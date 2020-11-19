//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define METHOD_BEGIN_ACCEPT

using System;
using System.Net;
using System.Net.Sockets;
using Core.Threading;

namespace Core.Network
{
    public abstract class TcpServerBase<TClient> : 
        ITcpServer where TClient : Core.Network.TcpClient, 
        new()
    {
        public event EventHandler<AcceptEventArgs> ClientAccept;
        public event EventHandler<EventArgs<Exception>> ExceptionOccur;

        private bool m_bActive = false;

        private Socket m_socket = null;

        private JobProcessor m_jobProcessor = null;

        public object Tag { get; set; }

        public IPEndPoint LocalEndPoint { get; private set; }

#if !METHOD_BEGIN_ACCEPT
        private int m_nAsyncAccepts = 0;

        public bool SyncAccept { get; set; } = true;
        private SocketAsyncEventArgs _acceptEventArgs = new SocketAsyncEventArgs();
        private Action<Socket> OnAcceptCallBack;
#endif

        //---------------------------------------------------------------------------------------------------
        public void Start(JobProcessor jobProcessor, int nPort, int nBacklog)
        {
            Start(jobProcessor, IPAddress.Any, nPort, nBacklog);
        }

        //---------------------------------------------------------------------------------------------------
        public void Start(JobProcessor jobProcessor, IPAddress bindAddress, int nPort, int nBacklog)
        {
            if (m_bActive)
            {
                throw new InvalidOperationException("Already activated.");
            }

            m_bActive = true;

            m_jobProcessor = jobProcessor;

            // Create the socket.
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the listening socket to the port.
            m_socket.Bind(new IPEndPoint(bindAddress, nPort));

            try
            {
                // Start listening.
                m_socket.Listen(nBacklog);
            }
            catch (SocketException)
            {
                Stop();
                throw;
            }

#if METHOD_BEGIN_ACCEPT
            m_socket.BeginAccept(0, EndAccept, m_socket);
#else
            OnAcceptCallBack = OnListenAcceptCallBack;

            if (SyncAccept)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((o) => OnSyncAccept());
            }
            else
            {
                OnAsyncAccept();
            }
#endif // _METHOD_BEGIN_ACCEPT

            LocalEndPoint = new IPEndPoint(bindAddress, (m_socket.LocalEndPoint as IPEndPoint).Port);
        }

#if METHOD_BEGIN_ACCEPT

        //---------------------------------------------------------------------------------------------------
        private void EndAccept(IAsyncResult ar)
        {
            Socket socket = null;

            try
            {
                socket = ar.AsyncState as Socket;

                if (m_socket != socket)
                {
                    try
                    {
                        socket.EndAccept(ar).Close();
                    }
                    catch { }
                }
                else
                {
                    Socket arg = null;

                    try
                    {
                        arg = socket.EndAccept(ar);
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception value)
                    {
                        if (m_jobProcessor == null)
                        {
                            OnExceptionOccur(new EventArgs<Exception>(value));
                        }
                        else
                        {
                            m_jobProcessor.Enqueue(Job.Create(OnExceptionOccur, new EventArgs<Exception>(value)));
                        }
                    }

                    socket?.BeginAccept(0, EndAccept, socket);

                    if (m_jobProcessor == null)
                    {
                        ProcessAccept(arg);
                    }
                    else
                    {
                        m_jobProcessor.Enqueue(Job.Create(ProcessAccept, arg));
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                if (m_jobProcessor == null)
                {
                    OnExceptionOccur(new EventArgs<Exception>(ex));
                }
                else
                {
                    m_jobProcessor.Enqueue(Job.Create(OnExceptionOccur, new EventArgs<Exception>(ex)));
                }
            }
        }
#else
        //---------------------------------------------------------------------------------------------------
        private void OnSyncAccept()
        {
            try
            {
                while (true)
                {
                    OnAcceptCallBack(m_socket.Accept());
                }
            }
            catch (Exception ex)
            {
                ServerFramework.SFLogUtil.Fatal(base.GetType(), ex.Message);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnListenAcceptCallBack(Socket e)
        {
            System.Threading.Tasks.Task.Run(() => ProcessAccept(e));
        }

        //---------------------------------------------------------------------------------------------------
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    e.AcceptSocket = null;
                    OnAcceptCallBack(e.AcceptSocket);
                }
                else
                {
                    ServerFramework.SFLogUtil.Error(base.GetType(), $"Accept completed socket error {e.SocketError}!");
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                if (m_jobProcessor == null)
                {
                    OnExceptionOccur(new EventArgs<Exception>(ex));
                }
                else
                {
                    m_jobProcessor.Enqueue(Job.Create(OnExceptionOccur, new EventArgs<Exception>(ex)));
                }
            }
            finally
            {
                if (m_nAsyncAccepts >= 50)
                {
                    m_nAsyncAccepts = 0;
                    System.Threading.Tasks.Task.Run(() => { OnAsyncAccept(); });
                }
                else
                {
                    if (m_jobProcessor == null)
                    {
                        OnAsyncAccept();
                    }
                    else
                    {
                        m_jobProcessor.Enqueue(Job.Create(OnAsyncAccept));
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnAsyncAccept()
        {
            try
            {
                _acceptEventArgs.AcceptSocket = null;

                if (!m_socket.AcceptAsync(_acceptEventArgs))
                {
                    m_nAsyncAccepts++;

                    OnAcceptCompleted(this, _acceptEventArgs);
                }
                else
                {
                    m_nAsyncAccepts = 0;
                }

            }
            catch (Exception ex)
            {
                ServerFramework.SFLogUtil.Fatal(base.GetType(), ex.Message);
            }
        }
#endif // _METHOD_BEGIN_ACCEPT

        //---------------------------------------------------------------------------------------------------
        public void Stop()
        {
            if (m_socket != null)
            {
                m_socket.Close();
                m_socket = null;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnClientAccept(AcceptEventArgs e)
        {
            ClientAccept?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnExceptionOccur(EventArgs<Exception> e)
        {
            ExceptionOccur?.Invoke(this, e);
        }

        //---------------------------------------------------------------------------------------------------
        private void ProcessAccept(Socket socket)
        {
            TcpClient tcpClient = Activator.CreateInstance<TClient>();
            AcceptEventArgs acceptEventArgs = new AcceptEventArgs(tcpClient);

            try
            {
                //OnClientAccept(acceptEventArgs);

                if (acceptEventArgs.Cancel)
                {
                    socket.Close();
                }
                else
                {
                    if (acceptEventArgs.JobProcessor == null)
                    {
                        acceptEventArgs.JobProcessor = m_jobProcessor;
                    }

                    tcpClient.Activate(socket, acceptEventArgs.JobProcessor);

                    // Call the client accept event.
                    OnClientAccept(acceptEventArgs);
                }
            }
            catch (Exception ex)
            {
                socket.Close();
                OnExceptionOccur(new EventArgs<Exception>(ex));
            }
        }
    }
}