//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define _USE_POOLED

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Core.Constants;

namespace Core.Network
{
    internal sealed class AsyncSocket : IAsyncSocket, IDisposable
    {
        /// <summary>
        /// Packet Receive Event.
        /// </summary>
        public event EventHandler<EventArgs<ArraySegment<byte>>> PacketReceive = null;

        /// <summary>
        /// Socket Close Event.
        /// </summary>
        public event EventHandler<EventArgs> SocketClose = null;

        /// <summary>
        /// Socket Exception Event.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> SocketException = null;

        /// <summary>
        /// Buffer Manager.
        /// </summary>
        private static readonly BufferManager m_bufferManager = new BufferManager(NetworkConst.MAX_CCU * 2, NetworkConst.MAX_PACKET_SIZE);

        /// <summary>
        /// Receive Pool.
        /// </summary>
        private static readonly SocketAsyncEventArgsPool m_receiveArgsPool = new SocketAsyncEventArgsPool(NetworkConst.MAX_CCU, m_bufferManager, OnReceive);
        
        /// <summary>
        /// Send Pool.
        /// </summary>
        private static readonly SocketAsyncEventArgsPool m_sendArgsPool = new SocketAsyncEventArgsPool(NetworkConst.MAX_CCU, m_bufferManager, OnSend);

        private int m_nActive = 0;
        private int m_nClosed = 0;

        private Socket m_socket = null;

        private readonly SocketAsyncEventArgs m_receiveArg = null;
        private readonly SocketAsyncEventArgs m_sendArg = null;

        private int m_nSendSize = 0;
        private int m_nSending = 0;

        private ArraySegment<byte> m_sendingPacket = null;

        private ConcurrentQueue<ArraySegment<byte>> m_sendQueue = null;

        //---------------------------------------------------------------------------------------------------

        public long TotalSent { get; private set; }
        public int TotalSentCount { get; private set; }
        public long TotalReceived { get; private set; }
        public int TotalReceivedCount { get; private set; }

        public bool Connected => m_socket != null && m_socket.Connected;

        public byte[] RemoteAddress { get; private set; }

        public int RemotePort { get; private set; }

        //---------------------------------------------------------------------------------------------------
        public static void Init() { }

        //---------------------------------------------------------------------------------------------------
        public AsyncSocket(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            if (!socket.Connected)
            {
                throw new ArgumentException("Can't activate on closed socket.", "socket");
            }

            if (socket.SocketType != SocketType.Stream || socket.AddressFamily != AddressFamily.InterNetwork || socket.ProtocolType != ProtocolType.Tcp)
            {
                throw new ArgumentException("Only TCP/IPv4 socket available.", "socket");
            }

            LingerOption lingerOption = socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger) as LingerOption;
            if (lingerOption == null || lingerOption.Enabled)
            {
                throw new ArgumentException("Linger option must be disabled.", "socket");
            }

            if (socket.UseOnlyOverlappedIO)
            {
                throw new ArgumentException("Socket must be bind on completion port.", "socket");
            }

            /*
             * The Nagle algorithm is designed to reduce network traffic by causing
             * the socket to buffer small packets and then combine and send them in one
             * packet under certain circumstances. A TCP packet consists of 40 bytes of
             * header plus the data being sent. When small packets of data are sent with TCP,
             * the overhead resulting from the TCP header can become a significant part of
             * the network traffic.On heavily loaded networks, the congestion resulting from
             * this overhead can result in lost datagrams and retransmissions, as well as excessive
             * propagation time caused by congestion. The Nagle algorithm inhibits the sending of new
             * TCP segments when new outgoing data arrives from the user if any
             * previously transmitted data on the connection remains unacknowledged. 
            */

            // Disable the Nagle Algorithm.
            socket.NoDelay = true;

            m_socket = socket;

            m_sendQueue = new ConcurrentQueue<ArraySegment<byte>>();

            IPEndPoint ipendPoint = (IPEndPoint)socket.RemoteEndPoint;
            RemoteAddress = ipendPoint.Address.GetAddressBytes();
            RemotePort = ipendPoint.Port;

            m_receiveArg = m_receiveArgsPool.Pop();
            m_receiveArg.UserToken = this;

            m_sendArg = m_sendArgsPool.Pop();
            m_nSendSize = 0;
            m_sendArg.UserToken = this;
        }

        //---------------------------------------------------------------------------------------------------
        public void Activate()
        {
            if (Interlocked.CompareExchange(ref m_nActive, 1, 0) != 0)
            {
                throw new InvalidOperationException("Can't reactivate AsyncSocket instance!");
            }

            BeginReceive();
        }

        //---------------------------------------------------------------------------------------------------
        public void Shutdown()
        {
            if (m_nActive == 0)
                return;

            if (Interlocked.CompareExchange(ref m_nClosed, 1, 0) != 0)
                return;

            m_receiveArg.UserToken = null;
            m_receiveArgsPool.Push(m_receiveArg);

            m_sendArg.UserToken = null;
            m_sendArgsPool.Push(m_sendArg);

            try
            {
                m_socket.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                m_socket.Close();
            }
            catch { }

            SocketClose?.Invoke(this, EventArgs.Empty);
        }

        //---------------------------------------------------------------------------------------------------
        public void Send(ArraySegment<byte> data)
        {
            if (m_nClosed == 0 && data.Count > 0)
            {
                m_sendQueue.Enqueue(data);
                BeginSend();
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Send(IEnumerable<ArraySegment<byte>> dataList)
        {
            if (m_nClosed == 0)
            {
                foreach (ArraySegment<byte> item in dataList)
                {
                    m_sendQueue.Enqueue(item);
                }

                BeginSend();
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void BeginReceive()
        {
            try
            {
                if (m_socket.Connected)
                {
                    TotalReceivedCount++;

                    if (!m_socket.ReceiveAsync(m_receiveArg))
                    {
                        OnReceive(m_receiveArg);
                        //Task.Run(() => { OnReceive(_receiveArg); });
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception exception)
            {
                OnException(exception);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private static void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.UserToken is AsyncSocket socket)
            {
                socket.OnReceive(e);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0)
            {
                Shutdown();
                return;
            }

            if (e.SocketError == SocketError.Success)
            {
                try
                {
                    TotalReceived += e.BytesTransferred;

                    byte[] buffer = e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred).ToArray();

                    PacketReceive?.Invoke(this, new EventArgs<ArraySegment<byte>>(new ArraySegment<byte>(buffer)));

                    BeginReceive();
                    return;
                }
                catch (Exception exception)
                {
                    OnException(exception);
                    return;
                }
            }

            OnException(new SocketException((int)e.SocketError));
        }

        //---------------------------------------------------------------------------------------------------
        private bool AcquireSendLock()
        {
            return Interlocked.Increment(ref m_nSending) == 1;
        }

        //---------------------------------------------------------------------------------------------------
        private bool ReleaseSendLock()
        {
            return Interlocked.Exchange(ref m_nSending, 0) == 1;
        }

        //---------------------------------------------------------------------------------------------------
        private void BeginSend()
        {
            while (AcquireSendLock())
            {
                bool sended = false;

                if (Monitor.TryEnter(this))
                {
                    sended = _BeginSend();
                    Monitor.Exit(this);
                }

                if (sended || ReleaseSendLock())
                {
                    return;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        private bool _BeginSend()
        {
            if (m_nSendSize < NetworkConst.MAX_PACKET_SIZE)
            {
                if (m_sendingPacket != null && m_sendingPacket.Count > 0 && m_sendingPacket.Array != null)
                {
                    int iLength = Math.Min(m_sendingPacket.Count, NetworkConst.MAX_PACKET_SIZE - m_nSendSize);
                    Buffer.BlockCopy(m_sendingPacket.Array, m_sendingPacket.Offset, m_sendArg.Buffer, m_sendArg.Offset + m_nSendSize, iLength);
                    m_nSendSize += iLength;

                    if (iLength < m_sendingPacket.Count)
                    {
                        m_sendingPacket = new ArraySegment<byte>(m_sendingPacket.Array, m_sendingPacket.Offset + iLength, m_sendingPacket.Count - iLength);

                        try
                        {
                            m_sendArg.SetBuffer(m_sendArg.Offset, m_nSendSize);
                        }
                        catch (InvalidOperationException exception)
                        {
                            OnException(exception);
                            return false;
                        }

                        try
                        {
                            if (!m_socket.Connected)
                            {
                                Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                                return false;
                            }

                            TotalSentCount++;

                            if (!m_socket.SendAsync(m_sendArg))
                            {
                                OnSend(m_sendArg);
                                return false;
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                            return false;
                        }
                        catch (Exception ex)
                        {
                            OnException(ex);
                            return false;
                        }

                        return true;
                    }

                    m_sendingPacket = default;
                }

                // Process the queue send.
                while (m_nSendSize < NetworkConst.MAX_PACKET_SIZE && m_sendQueue.TryDequeue(out ArraySegment<byte> buffer))
                {
                    if (buffer == null || buffer.Count <= 0 || buffer.Array == null)
                    {
                        continue;
                    }

                    int iPacketSize = 0;
                    if (buffer.Count + m_nSendSize <= NetworkConst.MAX_PACKET_SIZE)
                    {
                        iPacketSize = buffer.Count;
                    }
                    else
                    {
                        iPacketSize = NetworkConst.MAX_PACKET_SIZE - m_nSendSize;
                        m_sendingPacket = new ArraySegment<byte>(buffer.Array, buffer.Offset + iPacketSize, buffer.Count - iPacketSize);
                    }

                    Buffer.BlockCopy(buffer.Array, buffer.Offset, m_sendArg.Buffer, m_sendArg.Offset + m_nSendSize, iPacketSize);
                    m_nSendSize += iPacketSize;
                }

                try
                {
                    m_sendArg.SetBuffer(m_sendArg.Offset, m_nSendSize);
                }
                catch (InvalidOperationException ex)
                {
                    OnException(ex);
                    return false;
                }
            }

            if (m_nSendSize == 0)
            {
                return false;
            }

            try
            {
                if (!m_socket.Connected)
                {
                    Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                    return false;
                }

                TotalSentCount++;

                if (!m_socket.SendAsync(m_sendArg))
                {
                    OnSend(m_sendArg);
                    return false;
                }
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                return false;
            }
            catch (Exception ex)
            {
                OnException(ex);
                return false;
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        private static void OnSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.UserToken is AsyncSocket asyncSocket)
            {
                asyncSocket.OnSend(e);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnSend(SocketAsyncEventArgs e)
        {
            try
            {
                if (!m_socket.Connected)
                {
                    Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                    m_nSending = 0;

                    return;
                }

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    if (e.BytesTransferred < e.Count)
                    {
                        Buffer.BlockCopy(e.Buffer, e.Offset + e.BytesTransferred, e.Buffer, e.Offset, e.Count - e.BytesTransferred);
                        m_nSendSize = e.Count - e.BytesTransferred;
                    }
                    else if (e.BytesTransferred <= e.Count)
                    {
                        m_nSendSize = 0;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref m_sendQueue, new ConcurrentQueue<ArraySegment<byte>>());
                ReleaseSendLock();
                return;
            }

            if (e.BytesTransferred == 0)
            {
                Shutdown();
                return;
            }

            if (e.SocketError == SocketError.Success)
            {
                if (!ReleaseSendLock() || !(m_sendQueue.IsEmpty && m_sendingPacket.Count == 0 && m_nSendSize == 0))
                {
                    BeginSend();
                    return;
                }
            }
            else
            {
                OnException(new SocketException((int)e.SocketError));
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnException(Exception exception)
        {
            SocketException ex = exception as SocketException;
            if ((ex == null || (ex.SocketErrorCode != SocketError.ConnectionAborted && ex.SocketErrorCode != SocketError.ConnectionReset)))
            {
                try
                {
                    SocketException?.Invoke(this, new EventArgs<Exception>(exception));
                }
                catch { }
            }

            Shutdown();
        }

        //---------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            // Socket dispose.
            m_socket?.Dispose();

            // Receive SocketAsyncEventArgs dispose.
            m_receiveArg?.Dispose();

            // Send SocketAsyncEventArgs dispose.
            m_sendArg?.Dispose();
        }
    }
}
