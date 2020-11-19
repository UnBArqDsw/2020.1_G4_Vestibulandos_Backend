using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using ServerFramework.Database;
using System.Threading.Tasks;

namespace LoginServer.Database.Util
{
    public static class DatabaseUtil
    {
        public static string MemberDBConnectionString { get; private set; }

        public static string LogDBConnectionString { get; private set; }

        public static void SetMemberDBConnection(string address, string db, string user, string pwd)
        {
            MemberDBConnectionString = $"Data Source={address}; Initial Catalog={db}; User ID={user}; Password={pwd}";
        }

        public static void SetLogDBConnection(string address, string db, string user, string pwd)
        {
            LogDBConnectionString = $"Data Source={address}; Initial Catalog={db}; User ID={user}; Password={pwd}";
        }

        public static SqlConnection OpenUserDBConnection()
        {
            return SFDBUtil.OpenConnection(MemberDBConnectionString);
        }

        public static Task<SqlConnection> OpenAsyncUserDBConnection()
        {
            return SFDBUtil.OpenAsyncConnection(MemberDBConnectionString);
        }

        public static SqlConnection OpenLogDBConnection()
        {
            return SFDBUtil.OpenConnection(LogDBConnectionString);
        }

        public static Task<SqlConnection> OpenLogAsyncDBConnection()
        {
            return SFDBUtil.OpenAsyncConnection(LogDBConnectionString);
        }
    }
}
