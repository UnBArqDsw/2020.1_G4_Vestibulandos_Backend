using System;
using System.Linq;
using System.Security.Cryptography;
using Troschuetz.Random.Generators;

namespace Security
{
    /// <summary>
    /// Security Association class.
    /// </summary>
    public class SecurityAssociation
    {
        // All size unit of data is byte. ( not bit )
        public const int IV_SIZE = 8;
        public const int ICV_SIZE = 10;
        public const int BLOCK_SIZE = 8;

        /// <summary>
        /// Packet Header length.
        /// </summary>
        public const int PACKET_LENGTH_HEADER = sizeof(ushort);

        /// <summary>
        /// Packet Compress Header length.
        /// </summary>
        public const int PACKET_COMPRESS_HEADER = sizeof(bool);

        /// <summary>
        /// Packet SPI Header length.
        /// </summary>
        public const int PACKET_SPI_HEADER = sizeof(ushort);
        
        /// <summary>
        /// Packet Sequence Number Header length.
        /// </summary>
        public const int PACKET_SEQUENCE_NUMBER_HEADER = sizeof(uint);

        /* 
         * 1 block should be padded unconditionally. If the data length is correct in block units, 
         * fill one complete block, otherwise fill one incomplete block to fill the missing. 
         * The value specified below specifies the number of extra padding blocks to hide the data size thereafter. 
         * If the value is 0, no additional padding is done. 
         */

        /// <summary>
        /// Max Extra Pad Blocks.
        /// One block should be padded unconditionally. If the data length is correct in block units, 
        /// fill one complete block, otherwise fill one incomplete block to fill the missing.
        /// The value specified below specifies the number of extra padding blocks to hide the data size thereafter.
        /// If the value is 0, no additional padding is done.
        /// </summary>
        public const uint MAX_EXTRA_PAD_BLOCKS = 1;

        /// <summary>
        /// Auth Key length.
        /// </summary>
        public const int AUTH_KEY_SIZE = 8;
        
        /// <summary>
        /// Crypto Key length.
        /// </summary>
        public const int CRYPTO_KEY_SIZE = 8;

        // 0xC0 0xD3 0xBD 0xC3 0xB7 0xCE 0xB8 0xB8 = 임시로만드는키
        public readonly byte[] DEFAULT_AUTH_KEY = { 0xC0, 0xD3, 0xBD, 0xC3, 0xB7, 0xCE, 0xB8, 0xB8 };

        // 0xC7 0xD8 0xC4 0xBF 0xB5 0xE9 0xC0 0xFD = 해커들절대모를키
        public readonly byte[] DEFAULT_CRYPTO_KEY = { 0xC7, 0xD8, 0xC4, 0xBF, 0xB5, 0xE9, 0xC0, 0xFD };

        /// <summary>
        /// Auth Key.
        /// </summary>
        protected byte[] m_bsAuthKey = null;
        
        /// <summary>
        /// Crypto Key.
        /// </summary>
        protected byte[] m_bsCryptoKey = null;

        /// <summary>
        /// Sequence Number
        /// </summary>
        protected uint m_nSequenceNum = 0;
        
        /// <summary>
        /// Last Sequence Number.
        /// </summary>
        protected uint m_nLastSequenceNum = 0;

        /// <summary>
        /// Replay Window Mask.
        /// </summary>
        protected uint m_nReplayWindowMask = 0;

        /// <summary>
        /// Get the Last Sequence Number.
        /// </summary>
        public uint LastSequenceNum => m_nLastSequenceNum;

        /// <summary>
        /// Get the Replay Window Mask.
        /// </summary>
        public uint ReplayWindowMask => m_nReplayWindowMask;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public SecurityAssociation()
        {
            this.m_nSequenceNum = 1;
            this.m_nLastSequenceNum = 0;
            this.m_nReplayWindowMask = 0;

            this.m_bsAuthKey = DEFAULT_AUTH_KEY;
            this.m_bsCryptoKey = DEFAULT_CRYPTO_KEY;
        }

        //---------------------------------------------------------------------------------------------------
        public void Clear()
        {
            this.m_nSequenceNum = 1;
            this.m_nLastSequenceNum = 0;
            this.m_nReplayWindowMask = 0;

            this.m_bsAuthKey = DEFAULT_AUTH_KEY;
            this.m_bsCryptoKey = DEFAULT_CRYPTO_KEY;
        }

        //---------------------------------------------------------------------------------------------------
        public void ResetRandomizeKey()
        {
            MT19937Generator mt = new MT19937Generator();

            for (int nIndex = 0; nIndex < AUTH_KEY_SIZE; nIndex++)
            {
                // 1 or more. SPI 0 is already set in the constructor.
                this.m_bsAuthKey[nIndex] = Convert.ToByte(mt.Next(1, byte.MaxValue));
            }

            for (int nIndex = 0; nIndex < CRYPTO_KEY_SIZE; nIndex++)
            {
                // 1 or more. SPI 0 is already set in the constructor.
                this.m_bsCryptoKey[nIndex] = Convert.ToByte(mt.Next(1, byte.MaxValue));
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void SetAuthKey(byte[] bsKey) { this.m_bsAuthKey = bsKey; }

        //---------------------------------------------------------------------------------------------------
        public void SetCryptoKey(byte[] bsKey) { this.m_bsCryptoKey = bsKey; }

        //---------------------------------------------------------------------------------------------------
        public byte[] GetAuthKey() { return this.m_bsAuthKey; }

        //---------------------------------------------------------------------------------------------------
        public byte[] GetCryptoKey() { return this.m_bsCryptoKey; }

        //---------------------------------------------------------------------------------------------------
        public uint GetSequenceNum() { return this.m_nSequenceNum; }

        //---------------------------------------------------------------------------------------------------
        public void IncrSequenceNum() { ++this.m_nSequenceNum; }

        //---------------------------------------------------------------------------------------------------
        public bool IsValidSequenceNum(uint nSequenceNum)
        {
            // The following algorithm is based on the Sequence Space Window
            // Code Example presented in RFC 2401.
            //
            // The "right" edge of the window represents the highest validated
            // Sequence Number received. Packets that contain Sequence Numbers lower
            // than the "left" edge of the window are rejected. Packets falling
            // within the window are checked against a list of received packets
            // within the window. Duplicates are rejected. If the received packet 
            // falls within the window or is new, or if the packet is to the right 
            // of the window, the Sequence Number is valid and the packet moves on
            // to the next verification stage.

            // Check for sequence number wrap
            if (nSequenceNum == 0)
            {
                return false;
            }

            // Nominal case: the new number is larger than the last packet
            if (nSequenceNum > this.m_nLastSequenceNum)
            {
                return true;
            }

            const byte CHAR_BIT = 8;
            const uint nReplayWindowSize = PACKET_SEQUENCE_NUMBER_HEADER * CHAR_BIT;
            uint nDiff = this.m_nLastSequenceNum - nSequenceNum;

            // Packet is too old or wrapped
            if (nDiff >= nReplayWindowSize)
            {
                return false;
            }

            // Packet is a duplicate
            uint nBit = 1;

            if ((this.m_nReplayWindowMask & (nBit << (int) nDiff)) > 0)
            {
                return false;
            }

            // Out of order, but within window
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Update the replay window based on the given (validated) sequence number.
        /// </summary>
        /// <param name="nSequenceNum"></param>
        public void UpdateReplayWindow(uint nSequenceNum)
        {
            // The following algorithm is based on the Sequence Space Window
            // Code Example presented in RFC 2401.
            //
            // The "right" edge of the window represents the highest validated
            // Sequence Number received. Packets that contain Sequence Numbers lower
            // than the "left" edge of the window are rejected. Packets falling
            // within the window are checked against a list of received packets
            // within the window. Duplicates are rejected. If the received packet 
            // falls within the window or is new, or if the packet is to the right 
            // of the window, the Sequence Number is valid and the packet moves on
            // to the next verification stage.

            if (!IsValidSequenceNum(nSequenceNum))
            {
                // Invalid sequence number.
                return;
            }

            const byte CHAR_BIT = 8;
            uint nReplayWindowSize = (uint)PACKET_SEQUENCE_NUMBER_HEADER * CHAR_BIT;

            // Nominal case: the new number is larger than the last packet
            if (nSequenceNum > this.m_nLastSequenceNum)
            {
                uint nDiff = nSequenceNum - this.m_nLastSequenceNum;

                // If the packet is within the window, slide the window
                if (nDiff < nReplayWindowSize)
                {
                    this.m_nReplayWindowMask <<= (int)nDiff;
                    this.m_nReplayWindowMask |= 1;
                }
                else
                {
                    // packet way outside the window; reset the window
                    this.m_nReplayWindowMask = 1;
                }

                // Update the "last" sequence number
                this.m_nLastSequenceNum = nSequenceNum;
            }
            else
            {
                // New number is smaller than the last packet
                uint nDiff = this.m_nLastSequenceNum - nSequenceNum;

                // Mark the packet as seen
                uint nBit = 1;
                this.m_nReplayWindowMask |= (nBit << (int)nDiff);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets the data to be encrypted and returns the encrypted data
        /// </summary>
        /// <param name="arrPayload">Packet data to be encrypted</param>
        /// <param name="arrIV">Initialization vector</param>
        public byte[] Encryption(byte[] arrPayload, byte[] arrIV)
        {
            using (DESCryptoServiceProvider desProvider = new DESCryptoServiceProvider())
            {
                desProvider.Mode = CipherMode.CBC;
                desProvider.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = desProvider.CreateEncryptor(this.m_bsCryptoKey, arrIV))
                {
                    return encryptor.TransformFinalBlock(arrPayload, 0, arrPayload.Length);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Gets the received packet data and returns the decrypted data
        /// </summary>
        /// <param name="arrCrypt">The packet data the way it was received</param>
        /// <param name="IV">Initialization vector</param>
        public byte[] Decryption(byte[] arrCrypt, byte[] arrIV)
        {
            using (DESCryptoServiceProvider desProvider = new DESCryptoServiceProvider())
            {
                desProvider.Mode = CipherMode.CBC;
                desProvider.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = desProvider.CreateDecryptor(this.m_bsCryptoKey, arrIV))
                {
                    return decryptor.TransformFinalBlock(arrCrypt, 0, arrCrypt.Length);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Generates an HMAC hash to the encrypted packet data
        /// </summary>
        /// <param name="arrAuth"></param>
        public byte[] GenerateICV(byte[] arrAuth, int nSize)
        {
            using (HMACMD5 hmac = new HMACMD5(this.m_bsAuthKey))
            {
                byte[] hash = hmac.ComputeHash(arrAuth, 0, nSize);
                return hash.Take(ICV_SIZE).ToArray();
            }
        }
    }
}
