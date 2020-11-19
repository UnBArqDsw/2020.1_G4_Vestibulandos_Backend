//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define _USE_POOLED

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Collections.Pooled;
using Common.Constants;
using Core.Util;
using Serialization;
using MessagePack;
using Security;
using ServerFramework;

namespace Core.Network.Session
{
    //---------------------------------------------------------------------------------------------------
    public class Session : BaseSession
    {
        protected static readonly object s_csOverRecvEvent = new object();

#if _USE_POOLED
        protected static PooledList<string> s_listOverRecvEvent = new PooledList<string>();
#else
        protected static List<string> s_vecOverRecvEvent = new List<string>();
#endif // _USE_POOLED

        protected static readonly object s_csPacketCount = new object();

#if _USE_POOLED
        protected static PooledDictionary<ushort, ulong> s_dictPacketCount = new PooledDictionary<ushort, ulong>();
#else
        protected static Dictionary<ushort, ulong> s_maPacketCount = new Dictionary<ushort, ulong>();
#endif // _USE_POOLED

        #region MessagePack

        /// <summary>
        /// MessagePack Options.
        /// </summary>
        private static readonly MessagePackSerializerOptions s_messagePackOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        #endregion MessagePack

        /// <summary>
        /// Disconnected Event.
        /// </summary>
        public event EventHandler<EventArgs> Disconnected = null;

        /// <summary>
        /// Proxy.
        /// </summary>
        private readonly bool m_bIsProxy = false;

        /// <summary>
        /// SPI.
        /// </summary>
        protected ushort m_usSPIndex = 0;
        public ushort SPIndex => m_usSPIndex;

        /// <summary>
        /// Variable that verifies that the security key has been received.
        /// </summary>
        private bool m_bAuthKeyRecved = false;
        private int m_nWaitStartTick = 0;

        /// <summary>
        /// Maximum time to wait for a security key to be received upon connection
        /// </summary>
        public static int m_nWaitStartGap = 5000;

        #region Tick Check

        /// <summary>
        /// Enables hb check on/off for debug convenience.
        /// </summary>
        protected readonly bool m_bCheckHBTick = true;

        /// <summary>
        /// Heart beat tick.
        /// </summary>
        protected long m_lHBTick = 0;

        /// <summary>
        /// Maximum time to wait for a heart beat.
        /// </summary>
        public static int s_nHBGap = 30000;

        #endregion

        #region Protection Checker

        /// <summary>
        /// Number of max times packet authentication can to fail.
        /// </summary>
        public static uint m_uiPacketAuthLimitNum = 0;

        protected readonly object m_csPacketAuthFailCnt = new object();
        protected uint m_uiPacketAuthFailCnt;

        /// <summary>
        /// EventID pushback received between specified tick gaps
        /// </summary>
        protected List<ushort> m_vecRecvEventTemp = new List<ushort>();
        protected int m_dwRecvCountTick;
        protected int m_dwRecvCountTickGap;

        /// <summary>
        /// Number of events considered to be an attack
        /// </summary>
        protected ushort m_usOverCount;

        /// <summary>
        /// Is it recorded? (Once recorded, no longer recorded)
        /// </summary>
        protected bool m_bCheckedOverCount;

        /// <summary>
        /// How many times you want to record after the last record
        /// </summary>
        protected ushort m_usOverCountGap;

        /// <summary>
        /// Record count
        /// </summary>
        protected ushort m_usOverCountWrite;

        /// <summary>
        /// Maximum recordable number
        /// </summary>
        protected ushort m_usMaxOverCountWrite;

        protected Dictionary<ushort, int> m_mapUserPacketSendCount = new Dictionary<ushort, int>();
        protected int m_nStartPacketTickCount;

        public static bool m_bHackCheckEnable = false;
        public static int m_nLogPacketSendCount = 100;
        public static int m_nPacketSendCount = 25;
        public static uint m_uiSendCountTickGap = 2 * 1000;

        #endregion

        #region Disconnect

        private object m_csDestroyReserved = new object();

        private bool m_bDestroyReserved = false;

        protected EnDisconnectReasonWhy m_enDisconnectReason = 0;
        public EnDisconnectReasonWhy DisconnectReason => m_enDisconnectReason;

        #endregion Disconnect

#if DEBUG
        private bool m_bPacketVerbose = false;
#endif

        //---------------------------------------------------------------------------------------------------
        public Session(bool bIsProxy)
        {
            m_bIsProxy = bIsProxy;

            m_bAuthKeyRecved = false;
            m_nWaitStartTick = Environment.TickCount;

            //m_bCheckHBTick = true;
            m_lHBTick = Environment.TickCount;

            SetReserveDestroy(false);

            m_enDisconnectReason = (int)EnDisconnectReasonWhy.None;

            // Protection
            m_uiPacketAuthFailCnt = 0;
            m_dwRecvCountTickGap = 1000; // ms
            m_usOverCount = 50;
            m_bCheckedOverCount = false;
            m_usOverCountGap = 50;
            m_usOverCountWrite = 0;
            m_usMaxOverCountWrite = 3;

            m_dwRecvCountTick = Environment.TickCount;

            m_nStartPacketTickCount = Environment.TickCount;
        }

        //---------------------------------------------------------------------------------------------------
        public virtual void OnRemove() { }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnDestroy()
        {
            // Disconnect the client session.
            Session?.Disconnect();

            // Initialize the authentication key information when disconnecting the socket. So that you can connect again next time.
            m_bAuthKeyRecved = false;

            // Whether a shutdown was previously reserved or not, it will be lost once the Destroy call succeeds.
            SetReserveDestroy(false);

            // Clear all packets logs.
            RemovePacketCount();

            // Delete the Security Assocciation.
            if (SPIndex != 0)
            {
                Security.Security.GetSADB().Delete(SPIndex);
                m_usSPIndex = 0;
            }

            // Process events that have not yet been processed.
            if (m_enDisconnectReason != EnDisconnectReasonWhy.Zombie && m_enDisconnectReason != EnDisconnectReasonWhy.TrafficAttack &&
                m_enDisconnectReason != EnDisconnectReasonWhy.ServerBlockIP && m_enDisconnectReason != EnDisconnectReasonWhy.ClientHacking &&
                m_enDisconnectReason != EnDisconnectReasonWhy.PacketAttack && m_enDisconnectReason != EnDisconnectReasonWhy.PacketInvalid)
            {
                // Process rest of the queue.
                if (GetQueueSize() > 0)
                {
                    base.Tick();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected override void OnConnectionSucceeded(object objSender, EventArgs e)
        {
#if DEBUG
            SFLogUtil.Debug(base.GetType(), $"Incoming new session. UID: {UID} | Name {Name}.");
#endif //DEBUG
        }

        //---------------------------------------------------------------------------------------------------
        protected override void OnDisconnected(object objSender, EventArgs args)
        {
            Disconnected?.Invoke(objSender, args);
        }

        //---------------------------------------------------------------------------------------------------
        public void OnAcceptConnection()
        {
            // 1. Invoked when the client's connection is successful.
            // 2. Creates a SecurityAssociation object for packet security here (one per Session)
            // 3. It is intended to synchronize security authentication by sending SecurityAssociation object information to the client.

            // Create the Security Assoaciation.
            SecurityAssociation securityAssociation = Security.Security.GetSADB().CreateNewSA(out ushort usSPI);

            // Send SA information before to update SPI.
            AcceptConnection(usSPI, securityAssociation);

            // Change to a new key.
            m_usSPIndex = usSPI;
        }

        //---------------------------------------------------------------------------------------------------
        protected override void OnPacketReceive(object objSender, EventArgs<ArraySegment<byte>> e)
        {
            // Check if the session is still connected.
            if (!Session.Connected || e.Value.Count == 0)
            {
                SFLogUtil.Error(base.GetType(), "Closed by remote machine");
                OnSocketError();

                return;
            }

#if DEBUG
            // Verbose.
            if (m_bPacketVerbose)
            {
                // Complete packet buffer.
                SFLogUtil.Info(base.GetType(), ByteHelper.ToHexDump(
                    "\n\n-------------------------------\n[Complete Packet received] :\n-------------------------------\n",
                    e.Value.Array, e.Value.Offset, e.Value.Count));
            }
#endif

            // Packet left to read.
            int nPacketReadLeft = e.Value.Count;

            // Current packet offset.
            int nOffset = e.Value.Offset;

            // If the packet length is larger than the progress amount, the new packet possibility is continuously checked.
            while (nPacketReadLeft >= SecurityAssociation.PACKET_LENGTH_HEADER)
            {
                // Find out the packet length.
                ReadOnlySpan<byte> spanPacketHeader = e.Value.Slice(nOffset, SecurityAssociation.PACKET_LENGTH_HEADER);
                ushort usPacketLen = BinaryPrimitives.ReadUInt16LittleEndian(spanPacketHeader);

                // Update the offset.
                nOffset += SecurityAssociation.PACKET_LENGTH_HEADER;

                // If complete packet is available with the data received in the buffer, proceed to the next step.
                // There is not enough data to construct a complete packet. Exit the loop.
                if (nPacketReadLeft < usPacketLen)
                {
                    SFLogUtil.Warn(base.GetType(), $"OnReceivePacket: packetReadLeft {nPacketReadLeft} < packetLen: {usPacketLen}");
                    break;
                }

                // Check if packet received is compressed.
                bool bIsCompress = e.Value.Slice(nOffset, SecurityAssociation.PACKET_COMPRESS_HEADER)[0] != 0;

                // Update the offset.
                nOffset += SecurityAssociation.PACKET_COMPRESS_HEADER;

                // Only the data except the length is buffered.
                ReadOnlySpan<byte> spanPacketBuffer =
                    e.Value.Slice(nOffset, usPacketLen - SecurityAssociation.PACKET_LENGTH_HEADER - SecurityAssociation.PACKET_COMPRESS_HEADER);

                // If a DOS attack comes in, the server will be dangerous because it will print the error log as much as the attack amount.
                SecureBuffer kSecBuf = new SecureBuffer(SPIndex, spanPacketBuffer.ToArray(), (ulong)spanPacketBuffer.ToArray().Length);
                if (!kSecBuf.IsAuthentic())
                {
                    SFLogUtil.Error(base.GetType(), "Packet authentication failed.");

                    // If the packet authentication fails, raise the failure count.
                    IncreasePacketAuthFailCnt();

                    // Block access when a certain number of packet authentication failures occur.
                    if (GetPacketAuthFailCnt() > m_uiPacketAuthLimitNum)
                    {
                        SFLogUtil.Warn(base.GetType(), "Find Traffic Attacker!");

                        // Disconnect the session.
                        SetDisconnReason(EnDisconnectReasonWhy.TrafficAttack);
                        OnSocketError();

                        // Since I do not call InitRecv () until I return it, the IOCP completion event no longer arrives.
                        return;
                    }
                }
                else
                {
                    // After finishing the authentication and decryption process, only pure payload is obtained.
                    ByteStream payload = null;

                    // PS: Since you've already called IsAuthentic(), the second argument is false!
                    if (kSecBuf.GetPayload(ref payload) == false || payload == null)
                    {
                        SFLogUtil.Error(base.GetType(), "Packet invalid, without payload.");
                        return;
                    }

                    // Process the packet.
                    OnRecvCompleted(payload, bIsCompress);
                }

                // Update the offset from next packet to read.
                nOffset += usPacketLen;
                nPacketReadLeft -= usPacketLen;
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void OnRecvCompleted(ByteStream payload, bool bCompress)
        {
#if DEBUG
            if (m_bPacketVerbose)
            {
                // Only packet buffer.
                SFLogUtil.Info(base.GetType(), ByteHelper.ToHexDump(
                    "\n\n-------------------------------\n[Packet received] :\n-------------------------------\n",
                    payload.Buffer, 0, payload.Length));
            }
#endif

            // Packet.
            IPacket packet = null;

            try
            {
                // Deserialize the packet.
                packet = (bCompress) ?
                    MessagePackSerializer.Deserialize<IPacket>(payload.Buffer, s_messagePackOptions) : // Decompress the packet.
                    MessagePackSerializer.Deserialize<IPacket>(payload.Buffer);
            }
            catch (MessagePackSerializationException e)
            {
                // Exception.
                SFLogUtil.Error(base.GetType(), e.Message);
            }

            // Check if packet is valid.
            if (packet == null)
            {
                // Packet is null, close the socket.
                SetDisconnReason(EnDisconnectReasonWhy.PacketInvalid);
                OnSocketError();

                // Return.
                return;
            }

            // Packet check.
            CheckRecvPacketOver();
            IncreasePacketCount();

            // Packet hack check.
            if (!IncreaseUserPacketSendCount(out int nSendPacketCount))
            {
                if (!CheckUserPacketSendCount(nSendPacketCount))
                {
                    SFLogUtil.Warn(base.GetType(), "User packet attacker!");

                    // Disconnect the session.
                    SetDisconnReason(EnDisconnectReasonWhy.PacketAttack);
                    OnSocketError();

                    return;
                }
            }

#if DEBUG
            if (m_bPacketVerbose)
            {
                // Debug code.
                SFLogUtil.Debug(base.GetType(), $"Received packet ID: {packet.GetType().Name}");
            }
#endif

            // Process packets by ID.
            switch (packet.OpCode)
            {
                case (int)OPCODE_UG.UG_HEARTBEAT:
                case (int)OPCODE_UL.UL_HEARTBEAT:
                    {
                        // heart bit filtering - no queueing.

                        // Process the heart beat packet.
                        if (packet is IHeartBeat heartBeat)
                        {
                            OnRecvHeartBeat(heartBeat.Tick);
                        }
                    }
                    break;
                case (int)OPCODE_UG.UG_ACCEPT_CONNECTION:
                case (int)OPCODE_UL.UL_ACCEPT_CONNECTION:
                    {
                        // 1. First packet received from the server when the client's server connection is successful.
                        // 2. Receives a IAcceptionConnection object for packet security from the server and stores it in SADB.
                        // 3. SpiIndex arriving from server is not used.
                        // 4. Client generates a non-overlapping SpiIndex at random and uses it (IAcceptionConnection should use the same as received from the server).

                        m_bAuthKeyRecved = true;

                        // Send of the client connection it's established.
                        switch (packet.OpCode)
                        {
                            case (int)OPCODE_UL.UL_ACCEPT_CONNECTION:
                                {
                                    Send(new LU_CONNECTION_ESTABLISHED { RetCode = true });
                                }
                                break;
                            case (int)OPCODE_UG.UG_ACCEPT_CONNECTION:
                                {
                                    Send(new GU_CONNECTION_ESTABLISHED { RetCode = true });
                                }
                                break;
                            default:
                                {
                                    SFLogUtil.Error(base.GetType(), "Accept connection not defined.");
                                }
                                break;
                        }

                        if (m_bIsProxy)
                        {
                            OnAcceptConnectionProxy(packet as IAcceptionConnection);
                        }
                    }
                    break;
                default:
                    {
                        OnRecvCompleted(packet);
                        break;
                    }
            }

            if (!m_bIsProxy)
            {
                m_lHBTick = Environment.TickCount;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnAcceptConnectionProxy(IAcceptionConnection packet)
        {
            SFLogUtil.Error(base.GetType(),
                "OnAcceptConnectionProxy function is virtual, this should not be happens.");
        }

        //---------------------------------------------------------------------------------------------------
        public override void Tick()
        {
            // Tick the base.
            base.Tick();

            ////////////////////////////////////////////////////////////////////////////////////////

            // If the termination is reserved, try the termination again.
            if (IsReserveDestroy())
            {
                OnDestroy();
                return;
            }

            if (m_bIsProxy)
            {
                // Ignore if session is null or not connected.
                if (Session == null || Session.Connected == false)
                {
                    return;
                }

                // Heartbeat in 15s increments
                if (Environment.TickCount - m_lHBTick <= 15000)
                {
                    return;
                }

                // It's still before you get your security key. Do not send heart-bits at this time.
                if (!m_bAuthKeyRecved)
                {
                    return;
                }

                // Update the heart beat tick.
                m_lHBTick = Environment.TickCount;

                // Heart beat.
                HeartBeat();
            }
            else
            {
                // If you have not exceeded the wait time and have received no messages
                if (!m_bAuthKeyRecved)
                {
                    if (Environment.TickCount - m_nWaitStartTick > m_nWaitStartGap)
                    {
                        SFLogUtil.Warn(base.GetType(),
                            $"Server did not receive a security key. SPI: {SPIndex} | Key: {UID} | Name: {Name} | Tick: {Environment.TickCount - m_nWaitStartTick}.");

                        // Disconnect the session.
                        ReserveDestroy();
                        return;
                    }
                }

                // Heart-bit check in 30s increments
                if (m_bCheckHBTick && Environment.TickCount - m_lHBTick > s_nHBGap)
                {
                    SFLogUtil.Warn(base.GetType(),
                        $"Zombie connection detected. SPI: {SPIndex} | Key: {UID} | Name: {Name} | Tick: {Environment.TickCount - m_lHBTick}.");

                    // Disconnect the zombie connection.
                    SetDisconnReason(EnDisconnectReasonWhy.Zombie);

                    // Disconnect the session.
                    OnDestroy();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void AcceptConnection(in ushort usSPI, in SecurityAssociation securityAssociation)
        {
            SFLogUtil.Error(base.GetType(),
                "AcceptConnection function is virtual, this should not be happens.");
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void HeartBeat()
        {
            SFLogUtil.Error(base.GetType(),
                "HeartBeat function is virtual, this should not be happens.");
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnRecvHeartBeat(int nIndex) { }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnRecvCompleted(IPacket packet)
        {
            SFLogUtil.Error(base.GetType(),
                "OnRecvCompleted function is virtual, this should not be happens.");
        }

        //---------------------------------------------------------------------------------------------------
        public override bool Send<T>(T packet, bool bCompress = false)
        {
            // Attempt to send when shutdown is scheduled. This is true, so accept true.
            if (IsReserveDestroy())
            {
                return true;
            }

            // Check if packet is valid and not null.
            if (packet == null)
            {
                SFLogUtil.Error(base.GetType(), "ClientSession.Send() received a packet null.");
                return false;
            }

            // Check if session's yet connected.
            if (Session == null || Session.Connected == false)
            {
                SFLogUtil.Error(base.GetType(), "ClientSession.Send() is not connected!");
                return false;
            }

            try
            {
#if DEBUG
                if (m_bPacketVerbose)
                {
                    // Debug code.
                    SFLogUtil.Debug(base.GetType(),
                        $"Sending packet ID: {packet.GetType().Name}");
                }
#endif

                // Packet buffer.
                byte[] arrBuffer = null;

                try
                {
                    // Serialize the packet.
                    arrBuffer = (bCompress) ?
                        MessagePackSerializer.Serialize<IPacket>(packet, s_messagePackOptions) : // compress the packet with lz4.
                        MessagePackSerializer.Serialize<IPacket>(packet);   // normal packet without compress.
                }
                catch
                {
                    // ignored
                }

                // Fail to generate the packet.
                if (arrBuffer == null)
                {
                    SFLogUtil.Error(base.GetType(), "Failed to serialize the packet.");
                    return false;
                }

                // Convert packet to Secure Buffer.
                ByteStream kbuff_ = new ByteStream(arrBuffer);
                SecureBuffer kSecBuff = new SecureBuffer(SPIndex);
                kSecBuff.Create(kbuff_);

                ByteStream bsbuff = new ByteStream((ulong)(kSecBuff.GetSize + SecurityAssociation.PACKET_SPI_HEADER + SecurityAssociation.PACKET_COMPRESS_HEADER));

                // Set the packet length total.
                bsbuff.Append(BitConverter.GetBytes((ushort)(kSecBuff.GetSize + SecurityAssociation.PACKET_SPI_HEADER + SecurityAssociation.PACKET_COMPRESS_HEADER)),
                    0, SecurityAssociation.PACKET_SPI_HEADER);

                // Set the packet is compressed.
                bsbuff.Append(BitConverter.GetBytes(bCompress), SecurityAssociation.PACKET_SPI_HEADER, SecurityAssociation.PACKET_COMPRESS_HEADER);

                // Set the packet data.
                bsbuff.Append(kSecBuff.GetData, SecurityAssociation.PACKET_SPI_HEADER + SecurityAssociation.PACKET_COMPRESS_HEADER, kSecBuff.GetSize);

                // Send the packet.
                Session.Send(new ArraySegment<byte>(bsbuff.Buffer));
            }
            catch (Exception ex)
            {
                // Exception.
                SFLogUtil.Fatal(base.GetType(), ex.Message);

                OnSocketError();
                return false;
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        public void IncreasePacketAuthFailCnt()
        {
            lock (m_csPacketAuthFailCnt)
            {
                ++m_uiPacketAuthFailCnt;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public uint GetPacketAuthFailCnt()
        {
            lock (m_csPacketAuthFailCnt)
            {
                return m_uiPacketAuthFailCnt;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected void CheckRecvPacketOver()
        {
            if (m_bCheckedOverCount)
            {
                return;
            }

            // Tick Check
            if (Environment.TickCount - m_dwRecvCountTick > m_dwRecvCountTickGap)
            {
                if (m_vecRecvEventTemp.Count >= m_usOverCount)
                {
                    string strStm = $"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} | " +
                                 $"{Session?.LocalEndPoint.Address} | " +
                                 $"{Session?.RemoteEndPoint.Address} | " +
                                 $"{m_vecRecvEventTemp.Count} | ";

                    strStm = m_vecRecvEventTemp.Aggregate(strStm, (current, e) => current + $"{e},");

                    lock (s_csOverRecvEvent)
                    {
                        s_listOverRecvEvent.Add(strStm);
                    }

                    // Record when sending higher count than previous condition.
                    m_usOverCount = (ushort)m_vecRecvEventTemp.Count;
                    m_usOverCount += m_usOverCountGap;

                    // After recording up to the maximum number of times, it is no longer recorded.
                    ++m_usOverCountWrite;
                    if (m_usOverCountWrite >= m_usMaxOverCountWrite)
                    {
                        m_bCheckedOverCount = true;
                    }
                }

                m_vecRecvEventTemp.Clear();
            }

            // EventID Record
            m_vecRecvEventTemp.Add(SPIndex);

            // Update the recv count tick.
            m_dwRecvCountTick = Environment.TickCount;
        }

        //---------------------------------------------------------------------------------------------------
        protected void IncreasePacketCount()
        {
            lock (s_csPacketCount)
            {
                if (!s_dictPacketCount.ContainsKey(SPIndex))
                {
                    s_dictPacketCount.Add(SPIndex, 1);
                }
                else
                {
                    ++s_dictPacketCount[SPIndex];
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected void RemovePacketCount()
        {
            lock (s_csPacketCount)
            {
                if (!s_dictPacketCount.ContainsKey(SPIndex))
                {
                    return;
                }

                s_dictPacketCount.Remove(SPIndex);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private int GetPacketSendCount()
        {
            lock (s_csPacketCount)
            {
                return m_nPacketSendCount;
            }
        }

        //---------------------------------------------------------------------------------------------------
        private int GetLogPacketSendCount()
        {
            lock (s_csPacketCount)
            {
                return m_nLogPacketSendCount;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected bool IncreaseUserPacketSendCount(out int nPacketCount)
        {
            nPacketCount = 0;

            lock (s_csPacketCount)
            {
                if (m_mapUserPacketSendCount.Empty())
                {
                    m_nStartPacketTickCount = Environment.TickCount;
                }

                if (!m_mapUserPacketSendCount.ContainsKey(SPIndex))
                {
                    m_mapUserPacketSendCount.Add(SPIndex, 1);
                }
                else
                {
                    ++m_mapUserPacketSendCount[SPIndex];
                }

                if (!m_mapUserPacketSendCount.TryGetValue(SPIndex, out int nCount))
                {
                    return true;
                }

                // Log data.
                if (nPacketCount == GetLogPacketSendCount())
                {
                    if (Environment.TickCount - m_nStartPacketTickCount < m_uiSendCountTickGap)
                    {
                        SFLogUtil.Error(base.GetType(), $"User Packet Attack Log Data: {SPIndex} | {nPacketCount}");
                    }
                }

                // Packet SendCount Check.
                if (nCount >= GetPacketSendCount())
                {
                    nPacketCount = nCount;
                    return false;
                }

                return true;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected bool CheckUserPacketSendCount(int nPacketCount)
        {
            lock (s_csPacketCount)
            {
                if (Environment.TickCount - m_nStartPacketTickCount < m_uiSendCountTickGap)
                {
                    SFLogUtil.Error(base.GetType(), $"User packet attack: {SPIndex} | {nPacketCount}");

                    // Leave the log and terminate the user only when the option is turned on.
                    if (m_bHackCheckEnable)
                    {
                        return false;
                    }
                }

                m_mapUserPacketSendCount.Clear();
                m_nStartPacketTickCount = Environment.TickCount;

                return true;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected override void OnException(object objSender, EventArgs<Exception> exception)
        {
            SFLogUtil.Fatal(base.GetType(), exception.Value.Message);
        }

        //---------------------------------------------------------------------------------------------------
        public bool IsReserveDestroy()
        {
            lock (m_csDestroyReserved)
            {
                return m_bDestroyReserved;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void SetReserveDestroy(bool bIsReserved)
        {
            lock (m_csDestroyReserved)
            {
                m_bDestroyReserved = bIsReserved;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void ReserveDestroy(EnDisconnectReasonWhy enReason = EnDisconnectReasonWhy.None)
        {
            if (enReason != EnDisconnectReasonWhy.None)
            {
                SetDisconnReason(enReason);
            }

            SetReserveDestroy(true);
        }

        //---------------------------------------------------------------------------------------------------
        public void SetDisconnReason(EnDisconnectReasonWhy enReason)
        {
            if (enReason != EnDisconnectReasonWhy.None)
            {
                m_enDisconnectReason = enReason;
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void OnSocketError()
        {
            SetReserveDestroy(true);
        }

        //---------------------------------------------------------------------------------------------------
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
