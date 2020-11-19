using System.IO;

namespace System.Collections.Generic
{
    public class Array<T> : List<T>
    {
        private int m_nLimit;

        //---------------------------------------------------------------------------------------------------
        public Array(int size) : base(size)
        {
            m_nLimit = size;
        }

        //---------------------------------------------------------------------------------------------------
        public Array(params T[] args) : base(args)
        {
            m_nLimit = args.Length;
        }

        //---------------------------------------------------------------------------------------------------
        public Array(int size, T defaultFillValue) : base(size)
        {
            m_nLimit = size;

            Fill(defaultFillValue);
        }

        //---------------------------------------------------------------------------------------------------
        public void Fill(T value)
        {
            for (var i = 0; i < m_nLimit; ++i)
            {
                Add(value);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public new T this[int index]
        {
            get => base[index];
            set
            {
                if (index >= Count)
                {
                    if (Count >= m_nLimit)
                        throw new InternalBufferOverflowException("Attempted to read more array elements from packet " + Count + 1 + " than allowed " + m_nLimit);

                    Insert(index, value);
                }
                else
                {
                    base[index] = value;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        public int GetLimit() { return m_nLimit; }

        //---------------------------------------------------------------------------------------------------
        public static implicit operator T[] (Array<T> array)
        {
            return array.ToArray();
        }
    }
}
