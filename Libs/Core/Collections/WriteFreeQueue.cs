using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Collections
{
    public class WriteFreeQueue<T>
    {
        [ThreadStatic]
        private static int s_nCurrentPriority;

        private static Converter<T, KeyValuePair<int, T>> s_priorityAttacher = AttachPriority;

        private SingleFreeQueue<KeyValuePair<int, T>>[] m_queueThreadLocal;

        private int[] m_arrQueueIndices;
        private int m_nQueueLength;

        private int m_nPriority;

        private int m_nNextIndex;
        private int m_nNextPriority;

        private int m_nReadCacheLength;

        //---------------------------------------------------------------------------------------------------
        private static KeyValuePair<int, T> AttachPriority(T value)
        {
            return new KeyValuePair<int, T>(s_nCurrentPriority, value);
        }

        //---------------------------------------------------------------------------------------------------
        public WriteFreeQueue()
        {
            m_nReadCacheLength = 32;
            m_queueThreadLocal = new SingleFreeQueue<KeyValuePair<int, T>>[32];

            m_arrQueueIndices = new int[32];
            m_nQueueLength = 0;

            m_nNextIndex = -1;
        }

        //---------------------------------------------------------------------------------------------------
        public WriteFreeQueue(int readCacheLength) 
            : this()
        {
            this.m_nReadCacheLength = readCacheLength;
        }

        //---------------------------------------------------------------------------------------------------
        public bool Empty
        {
            get
            {
                for (int i = 0; i < m_nQueueLength; i++)
                {
                    if (!m_queueThreadLocal[m_arrQueueIndices[i]].Empty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public T Head
        {
            get
            {
                SetPriorSingleFreeQueueIndex();
                if (m_nNextIndex == -1)
                {
                    throw new InvalidOperationException("Queue is Empty.");
                }

                return m_queueThreadLocal[m_nNextIndex].Head.Value;
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void ExtendThreadQueue(int threadID)
        {
            int i;
            for (i = m_queueThreadLocal.Length * 2; i <= threadID; i *= 2) { }

            SingleFreeQueue<KeyValuePair<int, T>>[] queue = new SingleFreeQueue<KeyValuePair<int, T>>[i];
            Array.Copy(m_queueThreadLocal, queue, m_queueThreadLocal.Length);

            m_queueThreadLocal = queue;
        }

        //---------------------------------------------------------------------------------------------------
        private void AllocateThreadQueue(int threadID)
        {
            m_queueThreadLocal[threadID] = new SingleFreeQueue<KeyValuePair<int, T>>(m_nReadCacheLength);

            if (m_nQueueLength == m_arrQueueIndices.Length)
            {
                int[] nDst = new int[m_arrQueueIndices.Length * 2];
                Buffer.BlockCopy(m_arrQueueIndices, 0, nDst, 0, 4 * m_arrQueueIndices.Length);
                m_arrQueueIndices = nDst;
            }

            m_arrQueueIndices[m_nQueueLength] = threadID;
            m_nQueueLength++;
        }

        //---------------------------------------------------------------------------------------------------
        private SingleFreeQueue<KeyValuePair<int, T>> GetSingleFreeQueue()
        {
            int nManagedThreadId = Thread.CurrentThread.ManagedThreadId;

            if (m_queueThreadLocal.Length <= nManagedThreadId)
            {
                lock (this)
                {
                    ExtendThreadQueue(nManagedThreadId);
                    AllocateThreadQueue(nManagedThreadId);

                    return m_queueThreadLocal[nManagedThreadId];
                }
            }

            if (m_queueThreadLocal[nManagedThreadId] == null)
            {
                lock (this)
                {
                    AllocateThreadQueue(nManagedThreadId);
                }
            }

            return m_queueThreadLocal[nManagedThreadId];
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue(T value)
        {
            m_nPriority++;

            GetSingleFreeQueue().Enqueue(new KeyValuePair<int, T>(m_nPriority, value));
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue(IEnumerable<T> collection)
        {
            s_nCurrentPriority = ++m_nPriority;

            GetSingleFreeQueue().Enqueue(collection, s_priorityAttacher);
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue(ArraySegment<T> collection)
        {
            s_nCurrentPriority = ++m_nPriority;

            GetSingleFreeQueue().Enqueue(collection, s_priorityAttacher);
        }

        //---------------------------------------------------------------------------------------------------
        private void SetPriorSingleFreeQueueIndex()
        {
            if (m_nNextIndex != -1 && m_queueThreadLocal[m_nNextIndex].TryPeek(out var threadLocal) && 
                threadLocal.Key == m_nNextPriority)
            {
                return;
            }

            m_nNextIndex = -1;

            for (int i = 0; i < m_nQueueLength; i++)
            {
                if (m_queueThreadLocal[m_arrQueueIndices[i]].TryPeek(out threadLocal))
                {
                    int nKey = threadLocal.Key;
                    if (m_nNextIndex == -1 || m_nNextPriority - nKey > 0)
                    {
                        m_nNextIndex = m_arrQueueIndices[i];
                        m_nNextPriority = nKey;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        public T Dequeue()
        {
            SetPriorSingleFreeQueueIndex();

            if (m_nNextIndex == -1)
            {
                throw new InvalidOperationException("Queue is Empty.");
            }

            return m_queueThreadLocal[m_nNextIndex].Dequeue().Value;
        }

        //---------------------------------------------------------------------------------------------------
        public bool TryDequeue(out T value)
        {
            SetPriorSingleFreeQueueIndex();

            if (m_nNextIndex == -1)
            {
                value = default(T);
                return false;
            }

            bool bFound = m_queueThreadLocal[m_nNextIndex].TryDequeue(out KeyValuePair<int, T> pair);
            value = pair.Value;

            return bFound;
        }

        //---------------------------------------------------------------------------------------------------
        public void Clear()
        {
            for (int i = 0; i < m_nQueueLength; i++)
            {
                m_queueThreadLocal[m_arrQueueIndices[i]].Clear();
            }
        }
    }
}
