using System;
using System.Net;
using System.Reflection;
using Core.Network;
using log4net;
using MessagePack;
using Serialization;

namespace Core.PipedNetwork
{
    public class Peer
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ThreadStatic]
        private static Peer _currentPeer;

        public event Action<Peer> Connected;
        public event Action<Peer> ConnectionFailed;
        public event Action<Peer> Disconnected;
        public event Action<Peer, IPacket> PacketReceived;

        private TcpClient _client;

        private Acceptor _parent;

        private bool _connected;

        public static Peer CurrentPeer => _currentPeer;

        public object Tag { get; set; }

        public IPEndPoint LocalEndPoint => _client.LocalEndPoint;

        public IPEndPoint RemoteEndPoint => _client.RemoteEndPoint;

        public IntPtr Handle => _client.Handle;

        public bool IsConnected => _connected;

        private Peer()
        {
        }

        public static Peer CreateClient(Acceptor parent, TcpClient client)
        {
            Peer peer = new Peer();

            peer._parent = parent;
            peer._client = client;

            peer._client.ConnectionSucceed += peer.OnConnectionSucceed;
            peer._client.ConnectionFail += peer.OnConnectionFail;
            peer._client.PacketReceive += peer.OnPacketReceived;
            peer._client.ExceptionOccur += peer.OnExceptionOccur;
            peer._client.Disconnected += peer.OnDisconnected;

            return peer;
        }

        public static Peer CreateFromServer(Acceptor parent, TcpClient client)
        {
            Peer peer = new Peer();
            peer._parent = parent;
            peer._client = client;

            peer._client.PacketReceive += peer.OnPacketReceived;
            peer._client.ExceptionOccur += peer.OnExceptionOccur;
            peer._client.Disconnected += peer.OnDisconnected;

            peer._connected = true;

            return peer;
        }

        public void Connect(IPEndPoint location)
        {
            if (_connected)
            {
                throw new InvalidOperationException("An attempt was made to connect another peer that is already connected.");
            }

            _client.Connect(_parent.Thread, location);
        }

        public void Disconnect()
        {
            _connected = false;

            _client.Disconnect();
        }

        public void Send<T>(T packet) where T : IPacket
        {
            byte[] buffer = MessagePackSerializer.Serialize<IPacket>(packet);
            _client.Send(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }

        private void OnPacketReceived(object sender, EventArgs<ArraySegment<byte>> e)
        {
            try
            {
                // TODO : Add same PacketReceive from Session here.
                // It's break when receive too manys packets.

                IPacket packet = MessagePackSerializer.Deserialize<IPacket>(e.Value); ;

                _currentPeer = this;

                PacketReceived?.Invoke(this, packet);
            }
            catch (Exception exception)
            {
                _logger.FatalFormat(exception.ToString());
            }
            finally
            {
                _currentPeer = null;
            }
        }

        private void OnConnectionFail(object sender, EventArgs<Exception> e)
        {
            _connected = false;

            _logger.ErrorFormat("PipedNetworkPeer connection failed. Exception: {0}", e.Value);

            ConnectionFailed?.Invoke(this);
        }

        private void OnConnectionSucceed(object sender, EventArgs e)
        {
            _connected = true;

            Connected?.Invoke(this);

            Connected = null;
            ConnectionFailed = null;
        }

        private void OnExceptionOccur(object sender, EventArgs<Exception> e)
        {
            _logger.ErrorFormat("Exception Occured in Peer. Exception: {0}", e.Value);
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _logger.ErrorFormat("{0} Disconnected to {1}", LocalEndPoint, RemoteEndPoint);

            _connected = false;

            Disconnected?.Invoke(this);
        }
    }
}