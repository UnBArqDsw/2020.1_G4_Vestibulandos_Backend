namespace Common.Constants
{
    //---------------------------------------------------------------------------------------------------
    public enum EnDisconnectReasonWhy
    {
        /// <summary>
        /// Default.
        /// </summary>
        None = 0,

        /// <summary>
        /// Timeout at Login.
        /// </summary>
        LoginTimeOut,

        /// <summary>
        /// Dual connection termination.
        /// </summary>
        DuplicateConnection,

        /// <summary>
        /// LoginID obtained from DB is empty.
        /// </summary>
        EmptyLogin,

        /// <summary>
        /// Force termination by Administrator.
        /// </summary>
        KickByAdmin,

        /// <summary>
        /// Client crash.
        /// </summary>
        ByCrash,

        /// <summary>
        /// Connection zombie.
        /// </summary>
        Zombie,

        /// <summary>
        /// Authentication Failed.
        /// </summary>
        AuthenFail,

        /// <summary>
        /// Protocol version different.
        /// </summary>
        ProtocolVersionDiff,

        /// <summary>
        /// Send Buffer Full.
        /// </summary>
        SendBufferFull,

        /// <summary>
        /// Bad user.
        /// </summary>
        BadUser, 

        /// <summary>
        /// Failed to add new user.
        /// </summary>
        AddNewUserFail,

        /// <summary>
        /// Password incorrect.
        /// </summary>
        WrongPassword,

        /// <summary>
        /// LoginID does not exist.
        /// </summary>
        NotUser,

        /// <summary>
        /// Authentication Tick Count is different.
        /// </summary>
        AuthTickFail,

        /// <summary>
        /// Move server.
        /// </summary>
        ServerMigration,

        /// <summary>
        /// Traffic attack.
        /// </summary>
        TrafficAttack,

        /// <summary>
        /// Normal shutdown.
        /// </summary>
        NormalExit,

        /// <summary>
        /// Terminate server command.
        /// </summary>
        ServerCommend,

        /// <summary>
        /// Server maximum user exceeded.
        /// </summary>
        ServerFull,

        /// <summary>
        /// Block IP Termination.
        /// </summary>
        ServerBlockIP,

        /// <summary>
        /// Shutdown agent target user.
        /// </summary>
        ShutdownUser,

        /// <summary>
        /// Client hack.
        /// </summary>
        ClientHacking,

        /// <summary>
        /// User packet attack.
        /// </summary>
        PacketAttack,

        /// <summary>
        /// User packet invalid.
        /// </summary>
        PacketInvalid,

        /// <summary>
        /// Database result wrong.
        /// </summary>
        DatabaseResultWrong,

        /// <summary>
        /// Max.
        /// </summary>
        MAX
    }
}
