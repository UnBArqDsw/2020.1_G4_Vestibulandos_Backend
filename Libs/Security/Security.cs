namespace Security
{
    public static class Security
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        private static SecurityAssociationDatabase s_SADB = null;

        //---------------------------------------------------------------------------------------------------
        public static void InitSecurity()
        {
            // Init Security Assocciation Database.
            if (s_SADB == null)
            {
                s_SADB = new SecurityAssociationDatabase();
            }
        }

        //---------------------------------------------------------------------------------------------------
        public static void ReleaseSecurity()
        {
            // Clear the database.
            if (s_SADB != null)
            {
                s_SADB.Clear();
            }

            // Set null.
            s_SADB = null;
        }

        //---------------------------------------------------------------------------------------------------
        public static SecurityAssociationDatabase GetSADB()
        {
            return s_SADB ??= new SecurityAssociationDatabase();
        }
    }
}
