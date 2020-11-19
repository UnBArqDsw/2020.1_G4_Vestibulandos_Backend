//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define _USE_POOLED

using System;
using System.Net.Sockets;

#if _USE_POOLED
using Collections.Pooled;
#else
using System.Collections.Concurrent;
#endif // _USE_POOLED

namespace Core.Network
{
    public sealed class SocketAsyncEventArgsPool : IDisposable
    {
        private readonly EventHandler<SocketAsyncEventArgs> OnComplete = null;

#if _USE_POOLED
        private readonly object m_lock = null;
        private PooledStack<SocketAsyncEventArgs> m_stackPool = null;
#else
        private ConcurrentStack<SocketAsyncEventArgs> m_stackPool = null;
#endif // _USE_POOLED

        private BufferManager m_bufferManager = null;

        private int m_iBlockCount = 0;

        //---------------------------------------------------------------------------------------------------
        public SocketAsyncEventArgsPool(int nBlockCount, BufferManager bufferManager, EventHandler<SocketAsyncEventArgs> completeEvent)
        {
            m_iBlockCount = nBlockCount;
            m_bufferManager = bufferManager;

            OnComplete = completeEvent;

#if _USE_POOLED
            m_lock = new object();
            m_stackPool = new PooledStack<SocketAsyncEventArgs>();
#else
            m_stackPool = new ConcurrentStack<SocketAsyncEventArgs>();
#endif // _USE_POOLED

            for (int i = 0; i < m_iBlockCount; i++)
            {
                m_stackPool.Push(Create());
            }
        }

        //---------------------------------------------------------------------------------------------------
        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs eventArgs = null;

#if _USE_POOLED
            lock (m_lock)
#endif // _USE_POOLED
            {
                if (!m_stackPool.TryPop(out eventArgs))
                {
                    eventArgs = Create();
                }
            }

            return eventArgs;
        }

        //---------------------------------------------------------------------------------------------------
        public void Push(SocketAsyncEventArgs arg)
        {
#if _USE_POOLED
            lock (m_lock)
#endif // _USE_POOLED
            {
                m_stackPool.Push(arg);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private SocketAsyncEventArgs Create()
        {
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += OnComplete.Invoke;

            m_bufferManager.SetBuffer(eventArgs);
            return eventArgs;
        }

        //---------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            m_stackPool?.Dispose();
            m_bufferManager?.Dispose();
        }
    }
}
