namespace LoginServer.Database
{
    public enum EnDatabaseEventID
    {
        DB_EVENT_NONE = 0,

        DB_EVENT_SERVER_LIST_REQ,

        DB_EVENT_VERIFY_ACCOUNT_REQ,
        DB_EVENT_VERIFY_ACCOUNT_RES,

        DB_EVENT_ACCOUNT_UPDATE_REQ,

        DB_EVENT_MAX
    }
}
