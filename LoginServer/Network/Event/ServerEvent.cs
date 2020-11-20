using Serialization;
using Serialization.Data;
using ServerFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoginServer.Network.Event
{
    public class ServerEvent
    {
        private static readonly Type m_type = typeof(ServerEvent);

        public static bool Send<T>(in Core.Network.Session.Session session, T packet, bool bCompress = false)
            where T : IPacket
        {
            if (session == null)
            {
                SFLogUtil.Error(m_type, "Session it's null.");
                return false;
            }

            if (packet == null)
            {
                SFLogUtil.Error(m_type, "Packet it's null.");
                return false;
            }

            return session.Send(packet, bCompress);
        }

        public static void SendHeartBeart(in Core.Network.Session.Session session)
        {
            Send(session, new LU_HEARTBEAT());
        }

        public static void SendAcceptConnection(in Core.Network.Session.Session session, ushort usSPI, byte[] arrAuthKey, byte[] arrCryptoKey,
            uint uiSequenceNum, uint uiLastSequenceNum, uint uiReplayWindowMask)
        {
            Send(session, new LU_ACCEPT_CONNECTION
            {
                SPI = usSPI,
                AuthKey = arrAuthKey,
                CryptoKey = arrCryptoKey,
                SequenceNum = uiSequenceNum,
                LastSequenceNum = uiLastSequenceNum,
                ReplayWindowMask = uiReplayWindowMask
            });
        }

        public static void SendServerList(in Core.Network.Session.Session session, List<ServerData> listServerData)
        {
            Send(session, new LU_SERVER_LIST
            {
                ServerList = listServerData
            });
        }

        public static void SendAccountResponse(in Core.Network.Session.Session session, ulong ulUniqueKey, string strAccount,
            byte[] arrSessionKey, int nRetcode, int nLastServerID)
        {
            Send(session, new LU_ACCOUNT_RES
            {
                UniqueKey = ulUniqueKey,
                Account = strAccount,
                SessionKey = arrSessionKey,
                RetCode = nRetcode,
                LastLobbyID = nLastServerID
            });
        }

        public static void SendLogoutResponse(in Core.Network.Session.Session session, int nRetcode)
        {
            Send(session, new LU_LOGOUT_RES
            {
                RetCode = nRetcode
            });
        }
    }
}
