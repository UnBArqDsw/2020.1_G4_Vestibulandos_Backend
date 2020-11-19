using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Core.Collections
{
    public class SingleFreeQueue<T>
    {
        [StructLayout(LayoutKind.Sequential, Size = 128)]
        private class ReadStatus
        {
            public int Index;

            public Element[] CachedValueList;

            public int ReadSequence;
            public int ReadArrayIndex;

            public ElementList ElementList;
        }

        [StructLayout(LayoutKind.Sequential, Size = 128)]
        private class WriteStatus
        {
            public int WriteSequence;
            public int WriteArrayIndex;

            public ElementList ElementList;
        }

        private struct Element
        {
            public T Value;
            public bool Valid;
        }

        private class ElementList
        {
            public Element[] ValueList;
            public ElementList NextList;

            public ElementList(int capacity)
            {
                ValueList = new Element[capacity];
            }
        }

        private ReadStatus m_readStatus = null;
        private WriteStatus m_writeStatus = null;

        private int m_nCacheLength = 0;

        //---------------------------------------------------------------------------------------------------
        public bool Empty
        {
            get
            {
                if (m_readStatus.Index < m_nCacheLength && m_readStatus.CachedValueList[m_readStatus.Index].Valid)
                {
                    return false;
                }

                m_readStatus.Index = 0;

                LoadCache();

                return !m_readStatus.CachedValueList[m_readStatus.Index].Valid;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public T Head
        {
            get
            {
                if (Empty)
                {
                    throw new InvalidOperationException("Queue is Empty.");
                }

                return m_readStatus.CachedValueList[m_readStatus.Index].Value;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public SingleFreeQueue()
            : this(32)
        {
        }

        //---------------------------------------------------------------------------------------------------
        public SingleFreeQueue(int readCacheLength)
        {
            m_nCacheLength = readCacheLength;

            m_readStatus = new ReadStatus
            {
                CachedValueList = new Element[m_nCacheLength],
                ElementList = new ElementList(32)
            };

            m_writeStatus = new WriteStatus
            {
                ElementList = m_readStatus.ElementList
            };
        }

        //---------------------------------------------------------------------------------------------------
        private void LoadCache()
        {
            bool bHasNext = m_readStatus.ElementList.NextList != null;
            if (!m_readStatus.ElementList.ValueList[m_readStatus.ReadArrayIndex].Valid)
            {
                if (!bHasNext)
                {
                    return;
                }

                m_readStatus.ReadSequence = 0;
                m_readStatus.ReadArrayIndex = 0;
                m_readStatus.ElementList = m_readStatus.ElementList.NextList;
            }

            int nLength = m_readStatus.ElementList.ValueList.Length;
            WriteStatus writeStatus = this.m_writeStatus;

            int nCacheLength = 0;

            if (writeStatus.ElementList == m_readStatus.ElementList)
            {
                nCacheLength = writeStatus.WriteSequence - m_readStatus.ReadSequence;
                if (nCacheLength > m_nCacheLength)
                {
                    nCacheLength = m_nCacheLength;
                }
            }
            else
            {
                nCacheLength = m_nCacheLength;
            }

            if (m_readStatus.ReadArrayIndex + nCacheLength > nLength)
            {
                nCacheLength = nLength - m_readStatus.ReadArrayIndex;
            }

            Array.Copy(m_readStatus.ElementList.ValueList, m_readStatus.ReadArrayIndex, m_readStatus.CachedValueList, 0, nCacheLength);
            Array.Clear(m_readStatus.ElementList.ValueList, m_readStatus.ReadArrayIndex, nCacheLength);

            m_readStatus.ReadSequence += nCacheLength;
            m_readStatus.ReadArrayIndex = (m_readStatus.ReadSequence & (nLength - 1));
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue(T value)
        {
            if (this.m_writeStatus.ElementList.ValueList[this.m_writeStatus.WriteArrayIndex].Valid)
            {
                int nCapacity = this.m_writeStatus.ElementList.ValueList.Length << 1;
                if (nCapacity == int.MinValue)
                {
                    throw new OutOfMemoryException("Can't reserve more entry.");
                }

                WriteStatus writeStatus = new WriteStatus
                {
                    ElementList = new ElementList(nCapacity)
                };

                writeStatus.ElementList.ValueList[0].Value = value;
                writeStatus.ElementList.ValueList[0].Valid = true;

                writeStatus.WriteSequence = 1;
                writeStatus.WriteArrayIndex = 1;

                ElementList nextElement = this.m_writeStatus.ElementList;
                this.m_writeStatus = writeStatus;
                nextElement.NextList = writeStatus.ElementList;
            }
            else
            {
                this.m_writeStatus.ElementList.ValueList[this.m_writeStatus.WriteArrayIndex].Value = value;
                this.m_writeStatus.ElementList.ValueList[this.m_writeStatus.WriteArrayIndex].Valid = true;
                this.m_writeStatus.WriteSequence++;
                this.m_writeStatus.WriteArrayIndex = (this.m_writeStatus.WriteSequence & (this.m_writeStatus.ElementList.ValueList.Length - 1));
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue<U>(IEnumerable<U> collection, Converter<U, T> converter)
        {
            if (collection == null)
            {
                throw new InvalidOperationException("Collection is null.");
            }

            ElementList nextElement = null;
            ElementList currentElement = this.m_writeStatus.ElementList;

            int nWriteSequence = this.m_writeStatus.WriteSequence;
            int nArrayIndex = this.m_writeStatus.WriteArrayIndex;
            int nDiffSequence = 0;

            try
            {
                foreach (U item in collection)
                {
                    if (currentElement.ValueList[nArrayIndex].Valid)
                    {
                        int nCapacity = currentElement.ValueList.Length << 1;
                        if (nCapacity == int.MinValue)
                        {
                            throw new OutOfMemoryException("Can't reserve more entry.");
                        }

                        ElementList tempElement = new ElementList(nCapacity);
                        if (nextElement == null)
                        {
                            nextElement = tempElement;
                            currentElement = tempElement;

                            nDiffSequence = nWriteSequence - this.m_writeStatus.WriteSequence;
                        }
                        else
                        {
                            currentElement.NextList = tempElement;
                            currentElement = tempElement;
                        }

                        nWriteSequence = 0;
                        nArrayIndex = 0;
                    }

                    currentElement.ValueList[nArrayIndex].Value = converter(item);
                    currentElement.ValueList[nArrayIndex].Valid = true;

                    nWriteSequence++;
                    nArrayIndex = (nWriteSequence & (currentElement.ValueList.Length - 1));
                }

                if (nextElement == null)
                {
                    this.m_writeStatus.WriteSequence = nWriteSequence;
                    this.m_writeStatus.WriteArrayIndex = nArrayIndex;
                }
                else
                {
                    WriteStatus writeStatus = new WriteStatus
                    {
                        ElementList = currentElement, 
                        WriteSequence = nWriteSequence, 
                        WriteArrayIndex = nArrayIndex
                    };

                    ElementList writeNextElement = this.m_writeStatus.ElementList;
                    this.m_writeStatus = writeStatus;
                    writeNextElement.NextList = nextElement;
                }
            }
            catch (OutOfMemoryException)
            {
                if (nextElement == null)
                {
                    nDiffSequence = nWriteSequence - this.m_writeStatus.WriteSequence;
                }

                if (this.m_writeStatus.WriteArrayIndex + nDiffSequence > this.m_writeStatus.ElementList.ValueList.Length)
                {
                    int nLength = this.m_writeStatus.ElementList.ValueList.Length - this.m_writeStatus.WriteArrayIndex;

                    Array.Clear(this.m_writeStatus.ElementList.ValueList, this.m_writeStatus.WriteArrayIndex, nLength);
                    Array.Clear(this.m_writeStatus.ElementList.ValueList, 0, nDiffSequence - nLength);
                }
                else
                {
                    Array.Clear(this.m_writeStatus.ElementList.ValueList, this.m_writeStatus.WriteArrayIndex, nDiffSequence);
                }

                throw;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Enqueue<U>(ArraySegment<U> collection, Converter<U, T> converter)
        {
            if (collection == null)
            {
                throw new InvalidOperationException("Collection is null.");
            }

            ElementList nextElement = null;
            ElementList currentElement = this.m_writeStatus.ElementList;

            int nWriteSequence = this.m_writeStatus.WriteSequence;
            int nArrayIndex = this.m_writeStatus.WriteArrayIndex;
            int nDiffSequence = 0;

            try
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    if (currentElement.ValueList[nArrayIndex].Valid)
                    {
                        int nCapacity = currentElement.ValueList.Length << 1;
                        if (nCapacity == int.MinValue)
                        {
                            throw new OutOfMemoryException("Can't reserve more entry.");
                        }

                        ElementList tempElement = new ElementList(nCapacity);
                        if (nextElement == null)
                        {
                            nextElement = tempElement;
                            currentElement = tempElement;

                            nDiffSequence = nWriteSequence - this.m_writeStatus.WriteSequence;
                        }
                        else
                        {
                            currentElement.NextList = tempElement;
                            currentElement = tempElement;
                        }

                        nWriteSequence = 0;
                        nArrayIndex = 0;
                    }

                    currentElement.ValueList[nArrayIndex].Value = converter(collection.Array[i + collection.Offset]);
                    currentElement.ValueList[nArrayIndex].Valid = true;

                    nWriteSequence++;

                    nArrayIndex = (nWriteSequence & (currentElement.ValueList.Length - 1));
                }
                if (nextElement == null)
                {
                    this.m_writeStatus.WriteSequence = nWriteSequence;
                    this.m_writeStatus.WriteArrayIndex = nArrayIndex;
                }
                else
                {
                    WriteStatus writeStatus = new WriteStatus
                    {
                        ElementList = currentElement, 
                        WriteSequence = nWriteSequence, 
                        WriteArrayIndex = nArrayIndex
                    };

                    ElementList writeNextElement = this.m_writeStatus.ElementList;
                    this.m_writeStatus = writeStatus;
                    writeNextElement.NextList = nextElement;
                }
            }
            catch (OutOfMemoryException)
            {
                if (nextElement == null)
                {
                    nDiffSequence = nWriteSequence - this.m_writeStatus.WriteSequence;
                }

                if (this.m_writeStatus.WriteArrayIndex + nDiffSequence > this.m_writeStatus.ElementList.ValueList.Length)
                {
                    int nLength = this.m_writeStatus.ElementList.ValueList.Length - this.m_writeStatus.WriteArrayIndex;

                    Array.Clear(this.m_writeStatus.ElementList.ValueList, this.m_writeStatus.WriteArrayIndex, nLength);
                    Array.Clear(this.m_writeStatus.ElementList.ValueList, 0, nDiffSequence - nLength);
                }
                else
                {
                    Array.Clear(this.m_writeStatus.ElementList.ValueList, this.m_writeStatus.WriteArrayIndex, nDiffSequence);
                }

                throw;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public T Dequeue()
        {
            if (Empty)
            {
                throw new InvalidOperationException("Queue is Empty.");
            }

            T value = m_readStatus.CachedValueList[m_readStatus.Index].Value;

            m_readStatus.CachedValueList[m_readStatus.Index] = default(Element);
            m_readStatus.Index++;

            return value;
        }

        //---------------------------------------------------------------------------------------------------
        public bool TryDequeue(out T value)
        {
            if (Empty)
            {
                value = default(T);
                return false;
            }

            value = m_readStatus.CachedValueList[m_readStatus.Index].Value;

            m_readStatus.CachedValueList[m_readStatus.Index] = default(Element);
            m_readStatus.Index++;

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        public bool TryPeek(out T value)
        {
            if (Empty)
            {
                value = default(T);
                return false;
            }

            value = m_readStatus.CachedValueList[m_readStatus.Index].Value;
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        public void Clear()
        {
            WriteStatus writeStatus = this.m_writeStatus;
            if (m_readStatus.ElementList != writeStatus.ElementList)
            {
                m_readStatus.ReadArrayIndex = 0;
                m_readStatus.ReadSequence = 0;

                while (m_readStatus.ElementList != writeStatus.ElementList)
                {
                    m_readStatus.ElementList = m_readStatus.ElementList.NextList;
                }
            }

            int nLength = m_readStatus.ElementList.ValueList.Length;
            int nSequence = writeStatus.WriteSequence;
            int nIndex = nSequence & (nLength - 1);

            if (m_readStatus.ReadArrayIndex < nIndex)
            {
                Array.Clear(m_readStatus.ElementList.ValueList, m_readStatus.ReadArrayIndex, nSequence - m_readStatus.ReadSequence);
            }
            else if (nSequence - m_readStatus.ReadSequence > 0)
            {
                Array.Clear(m_readStatus.ElementList.ValueList, m_readStatus.ReadArrayIndex, nLength - m_readStatus.ReadArrayIndex);
                Array.Clear(m_readStatus.ElementList.ValueList, 0, nIndex);
            }

            m_readStatus.ReadSequence = nSequence;
            m_readStatus.ReadArrayIndex = nIndex;

            Array.Clear(m_readStatus.CachedValueList, 0, m_readStatus.CachedValueList.Length);

            m_readStatus.Index = 0;
        }
    }
}
