using System;
using Serialization;
using ServerFramework;

namespace Core.Network.Session
{
    //---------------------------------------------------------------------------------------------------
    public abstract class BaseSession : Performer
    {
        /// <summary>
        /// TCP Session.
        /// </summary>
        private TcpClient m_session = null;
        
        public TcpClient Session => m_session;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Called when the connection is successful.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnConnectionSucceeded(object sender, EventArgs e);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Called when the session is disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void OnDisconnected(object sender, EventArgs args);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Called when a packet is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnPacketReceive(object sender, EventArgs<ArraySegment<byte>> e);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send the packet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        /// <param name="bCompress"></param>
        /// <returns></returns>
        public abstract bool Send<T>(T packet, bool bCompress = false) where T : IPacket;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Called when an exception is raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnException(object sender, EventArgs<Exception> e);

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the socket informations on the initialization.
        /// </summary>
        /// <param name="client"></param>
        public virtual void SetSocketInfo(TcpClient client)
        {
            m_session = client;
            m_session.Tag = this;

            m_session.ConnectionSucceed += OnConnectionSucceeded;
            m_session.PacketReceive += OnPacketReceive;
            m_session.ExceptionOccur += OnException;
            m_session.Disconnected += OnDisconnected;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Dispose.
        /// </summary>
        public virtual void Dispose()
        {
            // Check if session is not null.
            if (m_session == null)
            {
                SFLogUtil.Error(base.GetType(), "Session is null.");
                return;
            }
            
            // Reset event args
            m_session.ConnectionSucceed -= OnConnectionSucceeded;
            m_session.PacketReceive -= OnPacketReceive;
            m_session.ExceptionOccur -= OnException;
            m_session.Disconnected -= OnDisconnected;

            m_session = null;
        }
    }
}
