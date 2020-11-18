//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define _USE_DISRUPTOR

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using LoginServer.Data;
using LoginServer.Database.Util;
using Common.Constants;
using Core.Database.Core.Database;
using Core.Event;
using Serialization;
using Serialization.Data;
using ServerFramework;
using ServerFramework.Database;

namespace LoginServer.Database
{
    public class DatabaseManager : DatabaseLayer
    {
        /// <summary>
        /// Instance of the Singleton from DB Manager.
        /// </summary>
        private static volatile DatabaseManager s_instance;

        /// <summary>
        /// Lock for Instance.
        /// </summary>
        private static readonly object s_syncRoot = new Object();

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Singleton.
        /// </summary>
        public static DatabaseManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_syncRoot)
                    {
                        if (s_instance == null)
                        {
                            ConstructorInfo constructorInfo = typeof(DatabaseManager).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                            s_instance = (DatabaseManager)constructorInfo?.Invoke(new object[0]);
                        }
                    }
                }

                return s_instance;
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        private DatabaseManager() { }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Initialization of the Database Manager.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            return base.Start();
        }

        //---------------------------------------------------------------------------------------------------
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

            // Process the database event.
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

            // If func is not null, let to execute.
            if (funcEvent != null)
            {
                // Execute the function.
                Task.Run(funcEvent);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public async Task OnEventServerListAsync()
        {
            List<ServerData> listServer = new List<ServerData>();

            SqlConnection conn = null;

            try
            {
                // Open the SQL Connection.
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

                // Close the SQL Connection.
                SFDBUtil.Close(ref conn);
            }
            finally
            {
                // Close the SQL Connection.
                SFDBUtil.Close(ref conn);
            }

            // Update the server list with new values.
            LoginManager.Instance.UpdateServerList(listServer);
        }

        //---------------------------------------------------------------------------------------------------
        public async Task OnEventAccountRequestAsync(string strSender, ulong ulSenderUid, UL_ACCOUNT_REQ packet)
        {
            SqlConnection conn = null;

            int nRetCode = (int)EnMessageError.Unknown;

            try
            {
                // Open the SQL Connection.
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

                            // Generate some random key.
                            data.SessionKey = new byte[64].GenerateRandomKey(64);

                            nRetCode = (int)EnMessageError.OK;

                            // Close the SQL Connection.
                            SFDBUtil.Close(ref conn);

                            // Check if need to create a new character.
                            this.QueueingEvent((ushort)EnDatabaseEventID.DB_EVENT_ACCOUNT_UPDATE_REQ, strSender, ulSenderUid, data);

                            // Return because already found the account and now update new session key.
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
                // Close the SQL Connection.
                SFDBUtil.Close(ref conn);
            }

            // Send response.
            LoginManager.Instance.ClientManager.QueueingEventTo(strSender, (ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_RES, nRetCode);
        }

        //---------------------------------------------------------------------------------------------------
        private async Task OnEventAccountUpdateAsync(string strSender, ulong ulSenderUid, VerifyAccountData data)
        {
            SqlConnection conn = null;
            SqlTransaction trans = null;

            int nRetCode = (int)EnMessageError.Unknown;

            try
            {
                // Open the SQL Connection.
                conn = await DatabaseUtil.OpenAsyncUserDBConnection();
                trans = conn.BeginTransaction();

                await using (SqlCommand command = new SqlCommand("usp_UpdateUserLogin", conn, trans))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parameter's
                    command.Parameters.Add("@sUserId", SqlDbType.Int).Value = data.UserID;
                    command.Parameters.Add("@bSessionKey", SqlDbType.NVarChar).Value = data.SessionKey.ToHexString();
                    command.Parameters.Add("@dtoLastLoginTime", SqlDbType.DateTime).Value = DateTime.Now;
                    command.Parameters.Add("@sLastLoginIp", SqlDbType.NVarChar).Value = data.Ip;

                    // Out's
                    command.Parameters.Add("ReturnValue", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;

                    // Executes a SQL statement async.
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                    // Get the return value.
                    nRetCode = Convert.ToInt32(command.Parameters["ReturnValue"].Value);
                    if (nRetCode != (int)EnMessageError.OK)
                    {
                        nRetCode = (int)EnMessageError.Unknown; //nReturnValue;
                    }
                }

                // Commit the changes.
                SFDBUtil.Commit(ref trans);

                // Close the SQL Connection.
                SFDBUtil.Close(ref conn);
            }
            finally
            {
                // Rollback the changes.
                SFDBUtil.Rollback(ref trans);

                // Close the SQL Connection.
                SFDBUtil.Close(ref conn);
            }

            // Send response.
            LoginManager.Instance.ClientManager.QueueingEventTo(strSender, (ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_RES, data, nRetCode);
        }
    }
}
