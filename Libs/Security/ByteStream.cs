using System.Text;

namespace Security
{
    public class ByteStream
    {
        private byte[] m_bsBuffer = null;
        public byte[] Buffer
        {
            get { return m_bsBuffer; }
            set { m_bsBuffer = value; }
        }

        public int Length => m_bsBuffer.Length;
        public bool Empty => (Length <= 0) ? true : false;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (byte b in m_bsBuffer)
                sb.AppendFormat("{0:X2} ", b);

            return sb.ToString();
        }

        //---------------------------------------------------------------------------------------------------
        public ByteStream(ulong nSize)
        {
            m_bsBuffer = new byte[nSize];
        }

        //---------------------------------------------------------------------------------------------------
        public ByteStream(byte[] bsBuffer)
        {
            m_bsBuffer = bsBuffer;
        }

        //---------------------------------------------------------------------------------------------------
        public ByteStream(ByteStream bsBuffer)
        {
            m_bsBuffer = bsBuffer.Buffer;
        }

        //---------------------------------------------------------------------------------------------------
        public void Append(byte[] objAssign)
        {
            byte[] outputBytes = new byte[m_bsBuffer.Length + objAssign.Length];

            System.Buffer.BlockCopy(m_bsBuffer, 0, outputBytes, 0, m_bsBuffer.Length);
            System.Buffer.BlockCopy(objAssign, 0, outputBytes, m_bsBuffer.Length, objAssign.Length);

            m_bsBuffer = outputBytes;
        }

        //---------------------------------------------------------------------------------------------------
        public void Append(byte[] objAssign, int nSize)
        {
            byte[] outputBytes = new byte[m_bsBuffer.Length + nSize];

            System.Buffer.BlockCopy(m_bsBuffer, 0, outputBytes, 0, m_bsBuffer.Length);
            System.Buffer.BlockCopy(objAssign, 0, outputBytes, m_bsBuffer.Length, nSize);

            m_bsBuffer = outputBytes;
        }

        //---------------------------------------------------------------------------------------------------
        public void Append(byte[] objAssign, int nPos, int nSize)
        {
            byte[] outputBytes = new byte[m_bsBuffer.Length];

            System.Buffer.BlockCopy(m_bsBuffer, 0, outputBytes, 0, m_bsBuffer.Length);
            System.Buffer.BlockCopy(objAssign, 0, outputBytes, nPos, nSize);

            m_bsBuffer = outputBytes;
        }

        //---------------------------------------------------------------------------------------------------
        public byte[] SubStr(int nPos, int nCount)
        {
            byte[] output = new byte[nCount];
            System.Buffer.BlockCopy(m_bsBuffer, nPos, output, 0, nCount);

            return output;
        }

        //---------------------------------------------------------------------------------------------------
        public void Resize(long length)
        {
            if (length < 0)
            {
                // Invalid reserve capacity!
                return;
            }

            byte[] output = new byte[length];
            System.Buffer.BlockCopy(m_bsBuffer, 0, output, 0, (int)length);
            m_bsBuffer = output;
        }
    }
}
