using System;
using System.Diagnostics;
using System.Linq;

namespace Security
{
    public class SecureBuffer
    {
        private ByteStream m_bsBuf = null;
        private ushort m_nSPIndex;

        public byte[] GetData
        {
            get => m_bsBuf.Buffer;
            protected set => m_bsBuf.Buffer = value;
        }

        private int m_nSize;
        public int GetSize => m_nSize;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="usSPI"></param>
        public SecureBuffer(ushort usSPI)
        {
            m_nSPIndex = usSPI;
            m_nSize = 0;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="usSPIIndex"></param>
        /// <param name="pRecvData"></param>
        /// <param name="nBytes"></param>
        public SecureBuffer(ushort usSPIIndex, byte[] arrBuffer, ulong nBytes)
        {
            this.m_bsBuf = new ByteStream(arrBuffer);
            this.m_nSPIndex = usSPIIndex;
            this.m_nSize = 0;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create the payload with Security Association.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool Create(ByteStream payload)
        {
            this.m_bsBuf = new ByteStream(GetMaxSecureSize(payload));

            // Add data: SPI
            this.m_bsBuf.Append(BitConverter.GetBytes(this.m_nSPIndex), this.m_nSize, SecurityAssociation.PACKET_SPI_HEADER);
            this.m_nSize += SecurityAssociation.PACKET_SPI_HEADER;

            // Add data: sequence number
            this.m_bsBuf.Append(BitConverter.GetBytes(GetSA().GetSequenceNum()), this.m_nSize, SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER);
            this.m_nSize += SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER;

            // IV is randomly generated every time.
            ByteStream iv = GenerateIV();
            this.m_bsBuf.Append(iv.Buffer, this.m_nSize, SecurityAssociation.IV_SIZE);
            this.m_nSize += SecurityAssociation.IV_SIZE;

            // Encrypt it with the generated IV. (encrypts to padding and padding size)
            ByteStream crypt = GenerateCrypt(payload, iv);
            if (crypt.Length <= 0)
            {
                // Invalid crypt length.
                return false;
            }

            // Add data: payload, padding, padding size
            this.m_bsBuf.Append(crypt.Buffer, this.m_nSize, crypt.Length);
            this.m_nSize += crypt.Length;

            // Generate ICV with input data so far
            ByteStream icv = GenerateICV(this.m_bsBuf, this.m_nSize);
            if (icv.Length <= 0)
            {
                // Invalid ICV length.
                return false;
            }

            // Add data: ICV
            this.m_bsBuf.Append(icv.Buffer, this.m_nSize, icv.Length);
            this.m_nSize += icv.Length;

            // Increase the sequence number of SecurityAssociation. (Sequence number of the next packet to send.)
            GetSA().IncrSequenceNum();

            // Created with success.
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Validate a secure KByteStream by examining the authentication data
        /// </summary>
        /// <returns>Return 'true' if is authenticated and 'false' if not.</returns>
        public bool IsAuthentic()
        {
            // Obtain the SPI from the data and verify that it is the SPI currently in the SADB.
            ushort usSPIndex = 0;
            if (!IsValidSPIndex(ref usSPIndex)) 
            {
                // Invalid SPI Index.
                return false;
            }

            // Check packet size
            if (!IsValidSize())
            {
                // Invalid packet size.
                return false;
            }

            // Validate the Integrity Check Value (ICV)
            if (!IsValidICV())
            {
                // Invalid ICV.
                return false;
            }

            // Check sequence number
            uint nSequenceNum = 0;
            if (!IsValidSequenceNum(ref nSequenceNum))
            {
                // Invalid sequence number.
                return false;
            }

            // It's authentic.
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Extract the payload from a secure KByteStream. Returns false if the payload
        /// is invalid or cannot be retrieved. Assumes the KByteStream has already been
        /// authenticated.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool GetPayload(ref ByteStream payload)
        {
            // Check if packet is authentic.
            if (!IsAuthentic())
            {
                // Packet is not authentic.
                return false;
            }

            // Obtain IV from the data. IV is located after SPI and SeqNum.
            ulong nPos = SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER + SecurityAssociation.PACKET_SPI_HEADER;
            ulong nSize = SecurityAssociation.IV_SIZE;
            if (nPos + nSize > (ulong) this.m_bsBuf.Length)
            {
                // Invalid length.
                return false;
            }

            ByteStream iv = new ByteStream(this.m_bsBuf.SubStr((int)nPos, (int)nSize));

            // Fill in the remaining portion of the IV with zero bytes
            // Initialize the remaining digits after IV.
            // Obtained IV is already the path of IV_SIZE.

            // Extracts a portion to be decoded from the data. IV, and the ICV at the end is to be subtracted.
            nPos = SecurityAssociation.PACKET_SPI_HEADER + SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER + nSize;
            if (nPos + SecurityAssociation.ICV_SIZE > (ulong) this.m_bsBuf.Length)
            {
                // Invalid ICV.
                return false;
            }

            nSize = (ulong)this.m_bsBuf.Length - nPos - SecurityAssociation.ICV_SIZE;
            ByteStream crypt = new ByteStream(this.m_bsBuf.SubStr((int)nPos, (int)nSize));

            // Decryption.
            payload = new ByteStream(GetSA().Decryption(crypt.Buffer, iv.Buffer));
            if (payload.Empty)
            {
                // Invalid payload.
                return false;
            }

            // Obtain the padding length and verify that the value of padding is correct.
            ulong nPadBytes = 0;
            if (!IsValidPadding(payload, ref nPadBytes))
            {
                // Invalid padding.
                return false;
            }

            // Remove padding and padding size bytes.
            if ((ulong) payload.Length < nPadBytes + sizeof(byte))
            {
                // Invalid padding length.
                return false;
            }

            payload.Resize(payload.Length - (long)nPadBytes - sizeof(byte));
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Marks a given KByteStream as authenticated and accepted. Automatically adjusts
        /// the replay window, so that IsAuthentic() will no longer validate the
        /// KByteStream correctly. Call this function only after calling IsAuthentic() and
        /// GetPayload(), but prior to calling IsAuthentic() on the next packet.
        /// </summary>
        void SetAccepted()
        {
            uint nSequenceNum = 0;
            bool bSuccess = IsValidSequenceNum(ref nSequenceNum);

            Debug.Assert(bSuccess);

            // Update the replay window.
            GetSA().UpdateReplayWindow(nSequenceNum);
        }

        //---------------------------------------------------------------------------------------------------
        private ByteStream GenerateIV()
        {
            ByteStream bsIV = new ByteStream(SecurityAssociation.IV_SIZE);

            System.Random random = new System.Random();
            byte arrIV = (byte)random.Next('A', 'h');

            for (int nIndex = 0; nIndex < SecurityAssociation.IV_SIZE; nIndex++)
            {
                bsIV.Buffer[nIndex] = arrIV;
            }

            return bsIV;
        }

        //---------------------------------------------------------------------------------------------------
        private ByteStream GenerateCrypt(ByteStream payload, ByteStream iv)
        {
            ByteStream crypt = new ByteStream(payload);

            // Append padding, if any
            ByteStream pad = new ByteStream(GeneratePadding(payload));
            crypt.Append(pad.Buffer);

            // Append pad length
            crypt.Append(BitConverter.GetBytes(pad.Length), sizeof(byte));

            return new ByteStream(GetSA().Encryption(crypt.Buffer, iv.Buffer));
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Generate the Integrity Check Value of the given ByteStream.
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="nSize"></param>
        /// <returns></returns>
        private ByteStream GenerateICV(ByteStream auth, int nSize)
        {
            return new ByteStream(GetSA().GenerateICV(auth.Buffer, nSize));
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Generate the padding bytes based on the given payload
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private byte[] GeneratePadding(ByteStream payload)
        {
            // The size of the payload, the padding and the pad length (1 byte)
            // must be evenly divisible by nBlockBytes
            const ulong ulBlockBytes = SecurityAssociation.BLOCK_SIZE;

            // Once you get the number of bytes that should be padded by default.
            ulong ulPadBytes = ulBlockBytes - ((ulong)payload.Length + sizeof(byte)) % ulBlockBytes;

            // Add some random padding to hide the true size of the payload
            byte nRand = 0;
            ulong ulRandBlocks = 0;

            // If the maximum number of extra blocks is 5, the value of [0,5] must be mod to 6, which is 5 + 1.
            // If the extra padding size is 0, rand is% 1, so the value is always 0.
            ulRandBlocks = nRand % (SecurityAssociation.MAX_EXTRA_PAD_BLOCKS + sizeof(byte));
            ulPadBytes += ulBlockBytes * ulRandBlocks;

            // Create the padding buffer.
            byte[] arrPad = new byte[ulPadBytes];

            // RFC 2406 says padding bytes are initialized with a series of 
            // one-byte integer values
            for (ulong nIndex = 1; nIndex <= ulPadBytes; ++nIndex)
            {
                arrPad[nIndex - 1] = (byte)nIndex;
            }

            return arrPad;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Determine the max size of the secure ByteStream based on the given payload.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private ulong GetMaxSecureSize(ByteStream payload)
        {
            ulong ulSize = 0;

            ulSize += SecurityAssociation.PACKET_SPI_HEADER;              // SPI.
            ulSize += SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER;  // Sequence.
            ulSize += SecurityAssociation.IV_SIZE;                        // IV length.
            ulSize += (ulong)payload.Length;                              // Payload length.
            ulSize += GetMaxPadSize(payload);                             // Padding.
            ulSize += SecurityAssociation.MAX_EXTRA_PAD_BLOCKS;           // Pad length.
            ulSize += SecurityAssociation.ICV_SIZE;                       // ICV length.

            return ulSize;
        }

        //---------------------------------------------------------------------------------------------------
        private ulong GetMaxPadSize(ByteStream payload)
        {
            // Obtain the block size.
            const ulong ulBlockBytes = SecurityAssociation.BLOCK_SIZE;

            // Once you get the number of bytes that should be padded by default.
            ulong ulPadBytes = ulBlockBytes - ((ulong)payload.Length + sizeof(byte)) % ulBlockBytes;

            // It adds extra padding can obtain additional blocks.
            ulPadBytes += SecurityAssociation.MAX_EXTRA_PAD_BLOCKS * ulBlockBytes;

#if DEBUG
            Debug.Assert(((ulong)payload.Length + sizeof(byte) + ulPadBytes) % ulBlockBytes == 0);
            Debug.Assert(ulPadBytes < byte.MaxValue);
#endif // DEBUG

            return ulPadBytes;
        }

        //---------------------------------------------------------------------------------------------------
        private bool IsValidSize()
        {
            // Assume no padding for quick check
            if (this.m_bsBuf.Length < SecurityAssociation.PACKET_SPI_HEADER + // SPI
                SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER + // Sequence Number
                SecurityAssociation.IV_SIZE + // IV
                SecurityAssociation.MAX_EXTRA_PAD_BLOCKS + // Pad size
                SecurityAssociation.ICV_SIZE) // ICV
            {
                // Invalid size.
                return false;
            }

            // ByteStream meets minimum requires; other checks performed later
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Check if packet received has same SPI from current Client.
        /// </summary>
        /// <param name="usSPIIndex"></param>
        /// <returns></returns>
        public bool IsValidSPIndex(ref ushort usSPIIndex)
        {
            // Checks whether the SPI of the data stored in the buffer is valid.

            // The size of the data is smaller than the SPI size.
            if (this.m_bsBuf.Length < SecurityAssociation.PACKET_SPI_HEADER)
            {
                // The packet size is smaller than the required SPI header size.
                return false;
            }

            // Extract the SPI from the data. It is the first one.
            usSPIIndex = BitConverter.ToUInt16(this.m_bsBuf.SubStr(0, SecurityAssociation.PACKET_SPI_HEADER), 0);

            // PS: The SPIndex registered between the server and the client can be different.

            return (m_nSPIndex == usSPIIndex);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Check if packet received has valid sequence number.
        /// </summary>
        /// <param name="nSequenceNum"></param>
        /// <returns></returns>
        private bool IsValidSequenceNum(ref uint nSequenceNum)
        {
            // Read the Sequence Number from the data. It is right behind the SPI.
            nSequenceNum = BitConverter.ToUInt32(this.m_bsBuf.SubStr(SecurityAssociation.PACKET_SPI_HEADER, SecurityAssociation.PACKET_SEQUENCE_NUMBER_HEADER), 0);

            // Check if sequence is valid.
            return GetSA().IsValidSequenceNum(nSequenceNum);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Determines if the ICV is valid.
        /// </summary>
        /// <returns></returns>
        private bool IsValidICV()
        {
            // Read the ICV at the end of the data.
            ulong nSize = SecurityAssociation.ICV_SIZE;
            ulong nPos = (ulong)this.m_bsBuf.Length - nSize;

            ByteStream icv = new ByteStream(this.m_bsBuf.SubStr((int)nPos, (int)nSize));

            // Reads data except for the ICV value (data to be indexed in ICV calculation).
            nSize = nPos;
            ByteStream auth = new ByteStream(this.m_bsBuf.SubStr(0, (int)nSize));

            // Calculate the ICV.
            ByteStream icvCompare = GenerateICV(auth, auth.Length);

            // Compare the calculated icv and received icv values.
            if (!icv.Buffer.SequenceEqual(icvCompare.Buffer))
            {
                return false;
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        private bool IsValidPadding(ByteStream payload, ref ulong ulPadBytes)
        {
            // Receives decoded payload as input (actual data + padding + padding size).

            // At a minimum, padding size should be included.
            if (payload.Length == 0)
            {
                // Invalid size.
                return false;
            }

            // Get padding size. It is located in the last byte.
            ulPadBytes = payload.SubStr(payload.Length - sizeof(byte), sizeof(byte)).First();

            // Data must be at least as large as the padding size and larger than the size indicated by the size of the bytes.
            if (ulPadBytes + sizeof(byte) > (ulong) payload.Length)
            {
                // Invalid.
                return false;
            }

            // Gets the padded part of the data.
            ulong ulPos = ((ulong)payload.Length - ulPadBytes - sizeof(byte));
            byte[] arrPad = payload.SubStr((int)ulPos, (int)ulPadBytes);

            // Verify that the padding bytes are correct. It must be a contiguous one-byte integer starting at 1.
            for (ulong nIndex = 1; nIndex <= ulPadBytes; ++nIndex)
            {
                if (arrPad[nIndex - 1] != (byte) nIndex)
                {
                    // Invalid pad.
                    return false;
                }
            }

            // Padding is good.
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        public SecurityAssociation GetSA()
        {
            return Security.GetSADB().GetSA(m_nSPIndex);
        }
    }
}
