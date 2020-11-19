using Collections.Pooled;
using Troschuetz.Random.Generators;

namespace Security
{
    /// <summary>
    /// Security Association Database.
    /// </summary>
    public class SecurityAssociationDatabase
    {
        /// <summary>
        /// Mutex.
        /// </summary>
        protected object m_lock = new object();
        
        /// <summary>
        /// Dictionary Database Security Association.
        /// </summary>
        protected PooledDictionary<ushort, SecurityAssociation> m_dictSecurityAssociation = null;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public SecurityAssociationDatabase()
        {
            lock (this.m_lock)
            {
                this.m_dictSecurityAssociation = new PooledDictionary<ushort, SecurityAssociation>
                {
                    // Insert default Security Association with SPIndex 0.
                    { 
                        0, 
                        new SecurityAssociation()
                    }
                };
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Clear()
        {
            lock (this.m_lock) 
            {
                this.m_dictSecurityAssociation.Clear();
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Insert(out ushort usSPI, SecurityAssociation securityAssociation)
        {
            MT19937Generator mt = new MT19937Generator();

            lock (this.m_lock) 
            {
                for (; ; )
                {
                    // Generate a random number for be the SPI.
                    usSPI = (ushort)mt.Next(1, ushort.MaxValue); // 1 or more. Because SPI with index 0 is already added in the constructor.

                    // Search for the number in the database. If it's not there, use it.
                    if (!this.Find(usSPI))
                    {
                        // Found the key.
                        break;
                    }
                }

                this.m_dictSecurityAssociation.Add(usSPI, securityAssociation);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void Delete(ushort usSPI)
        {
            lock (this.m_lock) 
            {
                this.m_dictSecurityAssociation.Remove(usSPI);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public bool Find(ushort usSPI)
        {
            lock (this.m_lock) 
            {
                return this.m_dictSecurityAssociation.ContainsKey(usSPI);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public SecurityAssociation GetSA(ushort usSPI)
        {
            lock (this.m_lock) 
            {
                // If there is no SA to find, it returns a constant key set to SPI 0.
                return this.m_dictSecurityAssociation.TryGetValue(usSPI, out SecurityAssociation sa) ? sa : this.m_dictSecurityAssociation[0];
            }
        }

        //---------------------------------------------------------------------------------------------------
        public SecurityAssociation CreateNewSA(out ushort usSPI)
        {
            // Create new security association.
            SecurityAssociation sa = new SecurityAssociation();

            // Define new keys.
            sa.ResetRandomizeKey();

            // Insert new security association and generate new key.
            this.Insert(out usSPI, sa);

            // Return security created.
            return this.GetSA(usSPI);
        }
    }
}
