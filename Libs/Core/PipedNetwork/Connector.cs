using Core.Network;
using Core.Threading;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Serialization;
using ServerFramework;

namespace Core.PipedNetwork
{
    public abstract class Connector : IDisposable
    {
        public event Action<Peer> OnConnected;

        public string Name { get; private set; }

        public event EventHandler Disposed;

        private Acceptor m_acceptor = null;

        private Dictionary<IPEndPoint, Peer> m_dictPeers;
        protected List<Peer> m_listAcceptedPeers;

        private bool _disposed;

        public JobProcessor Thread { get; private set; }

        public bool IsStarted { get; private set; }

        protected internal IEnumerable<Peer> Peers => (_disposed) ? new List<Peer>().AsEnumerable<Peer>() : m_listAcceptedPeers.Union(m_dictPeers.Values);

        public Connector()
        {
            IsStarted = false;

            m_dictPeers = new Dictionary<IPEndPoint, Peer>();
            m_listAcceptedPeers = new List<Peer>();
        }

        ~Connector()
        {
            Dispose(false);
        }

        public abstract void Initialize(JobProcessor thread);

        public void Initialize(string name, JobProcessor thread)
        {
            Initialize<TcpServer>(name, thread);
        }

        public void Initialize<T>(string name, JobProcessor thread) where T : ITcpServer, new()
        {
            Name = name;

            Thread = thread;
            thread.Enqueue(Job.Create(() => { System.Threading.Thread.CurrentThread.Name = Name; }));

            Thread.ExceptionFilter += Thread_ExceptionFilter;
            Thread.ExceptionOccur += Thread_ExceptionOccur;

            m_acceptor = new Acceptor((default(T) == null) ? Activator.CreateInstance<T>() : default(T), thread);
            m_acceptor.OnAccepted += OnPeerAccepted;
        }

        private void Thread_ExceptionOccur(object sender, EventArgs<Exception> e)
        {
            SFLogUtil.Fatal(base.GetType(), $"Thread Exception. Ex: {e}");
        }

        private void Thread_ExceptionFilter(object sender, EventArgs<Exception> e)
        {
            SFLogUtil.Fatal(base.GetType(), $"Thread Exception Filter. Ex: {e}");
        }

        public void Start(int port)
        {
            m_acceptor.Start(port);
            IsStarted = true;
        }

        public virtual bool Stop()
        {
            m_acceptor.Stop();
            IsStarted = false;

            return true;
        }

        protected virtual void OnPeerAccepted(Peer peer)
        {
            SFLogUtil.Error(base.GetType(), "OnPeerAccepted function is virtual, this should not be happens.");
        }

        protected virtual void OnPeerDisconnected(Peer obj)
        {
            SFLogUtil.Error(base.GetType(), "OnPeerDisconnected function is virtual, this should not be happens.");
        }

        protected virtual void OnPeerPacketReceived(Peer peer, IPacket obj)
        {
            SFLogUtil.Error(base.GetType(), "OnPeerPacketReceived function is virtual, this should not be happens.");
        }

        protected virtual TcpClient CreateTcpClient()
        {
            return new TcpClient();
        }

        public void ConnectToIP(IPEndPoint location, Action<Peer> onCallback)
        {
            if (_disposed)
            {
                onCallback(null);
                return;
            }

            if (!m_dictPeers.TryGetValue(location, out Peer peer))
            {
                peer = Peer.CreateClient(m_acceptor, CreateTcpClient());
                m_dictPeers[location] = peer;

                //peer.PacketReceived += OnPeerPacketReceived;
                peer.Connected += (x) =>
                {
                    OnConnected?.Invoke(x);

#if DEBUG
                    SFLogUtil.Debug(base.GetType(), $"Connected to [{Name}]: {location}");
#endif

                    onCallback(x);
                };

                peer.Disconnected += (x) =>
                {
                    SFLogUtil.Info(base.GetType(), $"Disconnected peer {x}");
                    RemovePeer(location, x);
                };

                peer.ConnectionFailed += (x) =>
                {
                    SFLogUtil.Error(base.GetType(), $"Connection failed : {location}");
                    onCallback(null);

                    RemovePeer(location, x);
                };

                peer.Connect(location);
                return;
            }

            if (peer.IsConnected)
            {
                onCallback(peer);
                return;
            }

            peer.Connected += (x) => { onCallback(peer); };
        }

        private void RemovePeer(IPEndPoint location, Peer peer)
        {
            if (m_dictPeers != null && m_dictPeers.TryGetValue(peer.RemoteEndPoint, out Peer p) && p == peer)
            {
                m_dictPeers.Remove(location);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                SFLogUtil.Error(base.GetType(), $"{Name} connector not disposed");
                return;
            }

            if (_disposed)
            {
                SFLogUtil.Error(base.GetType(), $"{Name} connector already disposed!");
                return;
            }

            _disposed = true;

            SFLogUtil.Info(base.GetType(), $"{Name} connector disposing...");
            if (m_acceptor != null)
            {
                m_acceptor.Stop();
                m_acceptor = null;
            }

            if (m_dictPeers != null)
            {
                foreach (Peer peer in m_dictPeers.Values.ToArray<Peer>())
                {
                    peer.Disconnect();
                }

                m_dictPeers.Clear();
                m_dictPeers = null;
            }

            if (Thread != null)
            {
                Thread.Stop();
                Thread = null;
            }

            if (m_listAcceptedPeers != null)
            {
                foreach (Peer p in m_listAcceptedPeers.ToArray())
                {
                    p.Disconnect();
                }

                m_listAcceptedPeers.Clear();
                m_listAcceptedPeers = null;
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
