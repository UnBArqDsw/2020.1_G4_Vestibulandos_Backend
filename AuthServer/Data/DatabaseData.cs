namespace LoginServer.Data
{
    //---------------------------------------------------------------------------------------------------
    public struct VerifyAccountData
    {
        public string Name;
        public ulong UserID;
        public byte[] SessionKey;
        public string Ip;
        
        public int LastServerID;
    }
}
