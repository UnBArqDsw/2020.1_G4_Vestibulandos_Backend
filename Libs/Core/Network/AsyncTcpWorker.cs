using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Core.Constants;
using ServerFramework;

namespace Core.Network
{
    internal class AsyncTcpWorker : IAsyncSocket, IDisposable
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

        private int m_iActive = 0;
        private int m_iClosed = 0;

        /// <summary>
        /// Socket.
        /// </summary>
        private readonly Socket m_socket = null;

        /// <summary>
        /// Remote Address.
        /// </summary>
        private byte[] m_arrRemoveAddress = null;
        
        /// <summary>
        /// Remote Port
        /// </summary>
        private int m_iRemortPort;

        /// <summary>
        /// Sending mutex.
        /// </summary>
        private int m_iSending = 0;

        /// <summary>
        /// Queue send.
        /// </summary>
        private ConcurrentQueue<ArraySegment<byte>> m_queueSend = null;

        /// <summary>
        /// Total sent.
        /// </summary>
        private long m_lTotalSent = 0;
        public long TotalSent => m_lTotalSent;

        /// <summary>
        /// Total received.
        /// </summary>
        private long m_lTotalReceived = 0;
        public long TotalReceived => m_lTotalReceived;

        /// <summary>
        /// Total sent count.
        /// </summary>
        private int m_iTotalSentCount = 0;
        public int TotalSentCount => m_iTotalSentCount;

        /// <summary>
        /// Total received count.
        /// </summary>
        private int m_iTotalReceivedCount = 0;
        public int TotalReceivedCount => m_iTotalReceivedCount;

        /// <summary>
        /// List of the send buffer.
        /// </summary>
        private readonly List<ArraySegment<byte>> m_listSendBuffer = null;

        private readonly byte[] m_arrReceiveBuffer = null;

        /// <summary>
        /// Check if is connected.
        /// </summary>
        public bool Connected => m_socket != null && m_socket.Connected;

        /// <summary>
        /// Remote Address.
        /// </summary>
        public byte[] RemoteAddress => m_arrRemoveAddress;

        /// <summary>
        /// Remote Port.
        /// </summary>
        public int RemotePort => m_iRemortPort;

        //---------------------------------------------------------------------------------------------------
        public AsyncTcpWorker(Socket socket)
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

            socket.SendBufferSize = 0;

            /*
             * The Nagle algorithm is designed to reduce network traffic by causing
             * the socket to buffer small packets and then combine and send them in one
             * packet under certain circumstances. A TCP packet consists of 40 bytes of
             * header plus the data being sent. When small packets of data are sent with TCP,
             * the overhead resulting from the TCP header can become a significant part of
             * the network traffic.On heavily loaded networks, the congestion resulting from
             * this overhead can result in lost datagrams and retransmissions, as well as excessive
             * propagation time caused by congestion. The Nagle algorithm inhibits the sending of new
             * TCP segmentswhen new outgoing data arrives from the user if any
             * previously transmitted data on the connection remains unacknowledged. 
            */

            // Disable the Nagle Algorithm.
            socket.NoDelay = true;

            m_socket = socket;
            m_queueSend = new ConcurrentQueue<ArraySegment<byte>>();

            IPEndPoint ipendPoint = (IPEndPoint)socket.RemoteEndPoint;
            m_arrRemoveAddress = ipendPoint.Address.GetAddressBytes();
            m_iRemortPort = ipendPoint.Port;

            m_arrReceiveBuffer = new byte[NetworkConst.MAX_PACKET_SIZE];
            m_listSendBuffer = new List<ArraySegment<byte>>(NetworkConst.MAX_SEND_PACKET_COUNT);
        }

        //---------------------------------------------------------------------------------------------------
        public void Activate()
        {
            if (Interlocked.CompareExchange(ref m_iActive, 1, 0) != 0)
            {
                throw new InvalidOperationException("Can't reactivate AsyncSocket instance!");
            }

            BeginReceive();
        }

        //---------------------------------------------------------------------------------------------------
        public void Shutdown()
        {
            Shutdown(SocketError.Success);
        }

        //---------------------------------------------------------------------------------------------------
        public void Shutdown(SocketError errorCode)
        {
            if (m_iActive == 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref m_iClosed, 1, 0) != 0)
            {
                return;
            }

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

            SocketClose?.Invoke(this, new EventArgs<SocketError>(errorCode));
        }

        //---------------------------------------------------------------------------------------------------
        public void Send(ArraySegment<byte> data)
        {
            if (m_iClosed == 0 && data.Count > 0)
            {
                m_queueSend.Enqueue(data);
                BeginSend();
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Send(IEnumerable<ArraySegment<byte>> dataList)
        {
            if (m_iClosed == 0)
            {
                foreach (ArraySegment<byte> item in dataList)
                {
                    m_queueSend.Enqueue(item);
                }

                BeginSend();
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void BeginReceive()
        {
            SocketError socketError;

            try
            {
                if (!m_socket.Connected)
                    return;

                m_iTotalReceivedCount++;

                m_socket.BeginReceive(m_arrReceiveBuffer, 0, m_arrReceiveBuffer.Length, SocketFlags.None, out socketError, EndReceive, null);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (socketError != SocketError.Success && socketError != SocketError.IOPending)
            {
                OnException(new SocketException((int)socketError));
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void EndReceive(IAsyncResult ar)
        {
            SocketError enSocketError = SocketError.SocketError;
            int nLength = 0;

            try
            {
                if (!m_socket.Connected)
                    return;

                nLength = m_socket.EndReceive(ar, out enSocketError);
                m_lTotalReceived += nLength;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (nLength == 0)
            {
                Shutdown(enSocketError);
                return;
            }

            if (enSocketError == SocketError.Success)
            {
                try
                {
                    PacketReceive?.Invoke(this, new EventArgs<ArraySegment<byte>>(new ArraySegment<byte>(m_arrReceiveBuffer, 0, nLength)));

                    BeginReceive();
                    return;
                }
                catch (Exception exception)
                {
                    OnException(exception);
                    return;
                }
            }

            OnException(new SocketException((int)enSocketError));
        }

        //---------------------------------------------------------------------------------------------------
        private bool AcquireSendLock()
        {
            return Interlocked.Increment(ref m_iSending) == 1;
        }

        //---------------------------------------------------------------------------------------------------
        private bool ReleaseSendLock()
        {
            return Interlocked.Exchange(ref m_iSending, 0) == 1;
        }

        //---------------------------------------------------------------------------------------------------
        private void BeginSend()
        {
            while (AcquireSendLock())
            {
                int iTotalCount = 0;
                for (int nIndex = 0; nIndex < m_listSendBuffer.Count; nIndex++)
                {
                    iTotalCount += m_listSendBuffer[nIndex].Count;
                }

                while (m_listSendBuffer.Count <= NetworkConst.MAX_SEND_PACKET_COUNT && iTotalCount < NetworkConst.MAX_PACKET_SIZE && m_queueSend.TryDequeue(out ArraySegment<byte> buffer))
                {
                    if (buffer.Count > 0)
                    {
                        if (buffer.Array == null)
                        {
                            SFLogUtil.Error(base.GetType(), "Error in AsyncTcpWorker.BeginSend. packet.Count > 0 but packet.Array is null.");
                            continue;
                        }

                        iTotalCount += buffer.Count;
                        m_listSendBuffer.Add(new ArraySegment<byte>(buffer.Array, buffer.Offset, buffer.Count));
                    }
                }

                if (m_listSendBuffer.Count != 0)
                {
                    SocketError socketError;

                    try
                    {
                        if (!m_socket.Connected)
                        {
                            Interlocked.Exchange(ref m_queueSend, new ConcurrentQueue<ArraySegment<byte>>());
                            ReleaseSendLock();

                            return;
                        }

                        m_iTotalSentCount++;

                        m_socket.BeginSend(m_listSendBuffer, SocketFlags.None, out socketError, EndSend, null);
                    }
                    catch (ObjectDisposedException)
                    {
                        Interlocked.Exchange(ref m_queueSend, new ConcurrentQueue<ArraySegment<byte>>());

                        ReleaseSendLock();
                        return;
                    }

                    if (socketError != SocketError.Success && socketError != SocketError.IOPending)
                    {
                        OnException(new SocketException((int)socketError));
                    }

                    return;
                }

                if (ReleaseSendLock())
                {
                    return;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void EndSend(IAsyncResult ar)
        {
            SocketError enSocketError = SocketError.SocketError;
            int nLength = 0;

            try
            {
                if (!m_socket.Connected)
                {
                    Interlocked.Exchange(ref m_queueSend, new ConcurrentQueue<ArraySegment<byte>>());
                    ReleaseSendLock();

                    return;
                }

                nLength = m_socket.EndSend(ar, out enSocketError);
                m_lTotalSent += nLength;

                int nSendLength = nLength;

                int nIndex;
                for (nIndex = 0; nIndex < m_listSendBuffer.Count; nIndex++)
                {
                    if (nSendLength < m_listSendBuffer[nIndex].Count)
                    {
                        m_listSendBuffer[nIndex] = new ArraySegment<byte>(m_listSendBuffer[nIndex].Array, m_listSendBuffer[nIndex].Offset + nSendLength, m_listSendBuffer[nIndex].Count - nSendLength);
                        break;
                    }

                    nSendLength -= m_listSendBuffer[nIndex].Count;
                }

                m_listSendBuffer.RemoveRange(0, nIndex);
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref m_queueSend, new ConcurrentQueue<ArraySegment<byte>>());
                ReleaseSendLock();

                return;
            }

            if (nLength == 0)
            {
                Shutdown();
                return;
            }

            if (enSocketError == SocketError.Success)
            {
                if (!ReleaseSendLock() || !(m_queueSend.IsEmpty && m_listSendBuffer.Count == 0))
                {
                    BeginSend();
                    return;
                }
            }
            else
            {
                OnException(new SocketException((int)enSocketError));
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
                catch
                {
                    // ignored
                }
            }

            Shutdown();
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Socket dispose.
            m_socket?.Dispose();
        }
    }
}
