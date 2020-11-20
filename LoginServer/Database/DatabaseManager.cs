#define _USE_DISRUPTOR

using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using LoginServer.Database.Util;
using Core.Database.Core.Database;
using System.Reflection;
using ServerFramework;
using ServerFramework.Database;
using System.Threading.Tasks;
using System.Data.SqlClient;
using LoginServer.Data;
using Core.Event;
using Serialization;
using Serialization.Data;
using Common.Constants;

namespace LoginServer.Database
{
    public class DatabaseManager : DatabaseLayer
    {
        private static volatile DatabaseManager s_instance;
        private static readonly object s_syncRoot = new Object();

        public static DatabaseManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_syncRoot)
                    {
                        s_instance = new DatabaseManager();
                    }
                }
                return s_instance;
            }
        }

        public bool Init()
        {
            return Start();
        }

#if _USE_DISRUPTOR
        protected override void ProcessEvent(object objSender, KIntEvent dbEvent)
#else
        protected override void ProcessEvent(KIntEvent dbEvent)
#endif
        {
            // Check if is a valid database event.
            if (dbEvent.EventID <= (ushort)EnDatabaseEventID.DB_EVENT_NONE || dbEvent.EventID >= (ushort)EnDatabaseEventID.DB_EVENT_MAX)
            {
                SFLogUtil.Error(base.GetType(), $"Unknown EventID: {((EnDatabaseEventID)dbEvent.EventID).ToString()}");
                return;
            }

            Func<Task> funcEvent = null;

            switch ((EnDatabaseEventID)dbEvent.EventID)
            {
                case EnDatabaseEventID.DB_EVENT_SERVER_LIST_REQ:
                    funcEvent = OnEventServerListAsync;
                    break;
                case EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_REQ:
                    funcEvent = () =>
                        OnEventAccountRequestAsync(dbEvent.Sender, dbEvent.SenderUID, (UL_ACCOUNT_REQ)dbEvent.Buffer);
                    break;
                case EnDatabaseEventID.DB_EVENT_ACCOUNT_UPDATE_REQ:
                    funcEvent = () =>
                        OnEventAccountUpdateAsync(dbEvent.Sender, dbEvent.SenderUID, (VerifyAccountData)dbEvent.Buffer);
                    break;
                default:
                    SFLogUtil.Error(base.GetType(),
                        $"ProcessEvent - Unknown EventID: {((EnDatabaseEventID)dbEvent.EventID).ToString()}");
                    break;
            }

            if (funcEvent != null)
            {
                Task.Run(funcEvent);
            }
        }
        public async Task OnEventServerListAsync()
        {
            List<ServerData> listServer = new List<ServerData>();

            SqlConnection conn = null;

            try
            {
                conn = await DatabaseUtil.OpenAsyncUserDBConnection();

                await using (SqlCommand command = new SqlCommand("usp_GetAllServerList", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    await using SqlDataReader reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        listServer.Add(new ServerData
                        {
                            ServerId = reader.GetInt32("serverId"),
                            Name = reader.GetString("name"),
                            ServerIp = reader.GetString("serverIP"),
                            ServerPort = reader.GetInt32("serverPort"),
                            CurrentUserCount = reader.GetInt32("currentUserCount"),
                            MaxUserCount = reader.GetInt32("maxUserCount"),
                            Status = reader.GetInt32("status"),
                            IsNew = reader.GetBoolean("isNew"),
                            IsMaintenance = reader.GetBoolean("isMaintenance")
                        });
                    }
                }

                SFDBUtil.Close(ref conn);
            }
            finally
            {
                SFDBUtil.Close(ref conn);
            }

            LoginManager.Instance.UpdateServerList(listServer);
        }

        public async Task OnEventAccountRequestAsync(string strSender, ulong ulSenderUid, UL_ACCOUNT_REQ packet)
        {
            SqlConnection conn = null;

            int nRetCode = (int)EnMessageError.Unknown;

            try
            {
                conn = await DatabaseUtil.OpenAsyncUserDBConnection();

                await using (SqlCommand command = new SqlCommand("usp_User", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("@username", SqlDbType.NVarChar).Value = packet.Login;

                    await using SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        VerifyAccountData data = new VerifyAccountData();

                        ulong strId = (ulong)reader.GetInt32("userId");
                        string strPwd = reader.GetString("password");
                        int nLastServer = reader.GetInt32("lastServer");

                        if (!string.Equals(strPwd, packet.Password))
                        {
                            nRetCode = (int)EnMessageError.LoginInvalidPasswordNotMatch;
                        }
                        else
                        {
                            data.Name = packet.Login;
                            data.UserID = strId;
                            data.LastServerID = nLastServer;
                            data.Ip = packet.IP;

                            data.SessionKey = new byte[64].GenerateRandomKey(64);

                            nRetCode = (int)EnMessageError.OK;

                            SFDBUtil.Close(ref conn);

                            this.QueueingEvent((ushort)EnDatabaseEventID.DB_EVENT_ACCOUNT_UPDATE_REQ, strSender, ulSenderUid, data);

                            return;
                        }
                    }
                    else
                    {
                        nRetCode = (int)EnMessageError.LoginInvalidUserNotFound;
                    }
                }
            }
            finally
            {
                SFDBUtil.Close(ref conn);
            }

            LoginManager.Instance.ClientManager.QueueingEventTo(strSender, (ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_RES, nRetCode);
        }

        private async Task OnEventAccountUpdateAsync(string strSender, ulong ulSenderUid, VerifyAccountData data)
        {
            SqlConnection conn = null;
            SqlTransaction trans = null;

            int nRetCode = (int)EnMessageError.Unknown;

            try
            {
                conn = await DatabaseUtil.OpenAsyncUserDBConnection();
                trans = conn.BeginTransaction();

                await using (SqlCommand command = new SqlCommand("usp_UpdateUserLogin", conn, trans))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("@sUserId", SqlDbType.Int).Value = data.UserID;
                    command.Parameters.Add("@bSessionKey", SqlDbType.NVarChar).Value = data.SessionKey.ToHexString();
                    command.Parameters.Add("@dtoLastLoginTime", SqlDbType.DateTime).Value = DateTime.Now;
                    command.Parameters.Add("@sLastLoginIp", SqlDbType.NVarChar).Value = data.Ip;

                    command.Parameters.Add("ReturnValue", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                    nRetCode = Convert.ToInt32(command.Parameters["ReturnValue"].Value);
                    if (nRetCode != (int)EnMessageError.OK)
                    {
                        nRetCode = (int)EnMessageError.Unknown;
                    }
                }

                SFDBUtil.Commit(ref trans);

                SFDBUtil.Close(ref conn);
            }
            finally
            {
                SFDBUtil.Rollback(ref trans);

                SFDBUtil.Close(ref conn);
            }

            LoginManager.Instance.ClientManager.QueueingEventTo(strSender, (ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_RES, data, nRetCode);
        }
    }
}
