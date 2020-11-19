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
using System.Collections.Generic;
#endif // _USE_POOLED

namespace Core.Network
{
    //---------------------------------------------------------------------------------------------------
    public sealed class BufferManager : IDisposable
    {
        private readonly object m_objLock = null;

#if _USE_POOLED
        private PooledList<byte[]> m_listBuffer = null;
        private PooledStack<int> m_stackFreeIndexPool = null;
#else
        private List<byte[]> m_buffers = null;
        private ConcurrentStack<int> m_freeIndexPool = null;
#endif // _USE_POOLED

        private int m_nBufferCount = 0;
        private int m_nBufferSize = 0;

        //---------------------------------------------------------------------------------------------------
        public BufferManager(int nBufferCount, int nBufferSize)
        {
            m_objLock = new object();

#if _USE_POOLED
            m_listBuffer = new PooledList<byte[]>();
            m_stackFreeIndexPool = new PooledStack<int>();
#else
            m_listBuffer = new List<byte[]>();
            m_stackFreeIndexPool = new ConcurrentStack<int>();
#endif // _USE_POOLED

            m_nBufferCount = nBufferCount;
            m_nBufferSize = nBufferSize;

            GrowBuffer();
        }

        //---------------------------------------------------------------------------------------------------
        public void SetBuffer(SocketAsyncEventArgs args)
        {
            int nCount = 0;

#if _USE_POOLED
            lock (m_objLock)
#endif // _USE_POOLED
            {
                while (!m_stackFreeIndexPool.TryPop(out nCount))
                {
                    GrowBuffer();
                }
            }

            int nIndex = nCount / m_nBufferCount;

            nCount %= m_nBufferCount;
            args.SetBuffer(m_listBuffer[nIndex], nCount * m_nBufferSize, m_nBufferSize);
        }

        //---------------------------------------------------------------------------------------------------
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
#if _USE_POOLED
            lock (m_objLock)
#endif // _USE_POOLED
            {
                m_stackFreeIndexPool.Push(args.Offset);
            }

            args.SetBuffer(null, 0, 0);
        }

        //---------------------------------------------------------------------------------------------------
        private void GrowBuffer()
        {
            lock (m_objLock)
            {
#if _USE_POOLED
                if (m_stackFreeIndexPool.Count <= 0)
#else
                if (m_stackFreeIndexPool.IsEmpty)
#endif // _USE_POOLED
                {
                    byte[] arrBuffer = new byte[m_nBufferCount * m_nBufferSize];
                    m_listBuffer.Add(arrBuffer);

                    for (int nIndex = 0; nIndex < m_nBufferCount; nIndex++)
                    {
                        m_stackFreeIndexPool.Push(nIndex + (m_listBuffer.Count - 1) * m_nBufferCount);
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Dispose()
        {
            m_listBuffer?.Dispose();
            m_stackFreeIndexPool?.Dispose();
        }
    }
}
