using System;

namespace Logging.WorkLog
{
    public class WorkLog
    {
        // ==================================================================================================
        // VARIABLE's
        // ==================================================================================================

        /// <summary>
        /// Type.
        /// </summary>
        private Type m_type = null;

        /// <summary>
        /// Request count.
        /// </summary>
        private long m_lRequestCount = 0;

        // ==================================================================================================
        // PROPERTY'ies
        // ==================================================================================================

        public Type Type => this.m_type;

        public long RequestCount
        {
            get => this.m_lRequestCount;
            set => this.m_lRequestCount = value;
        }

        // ==================================================================================================
        // FUNCTION's
        // ==================================================================================================

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type"></param>
        public WorkLog(Type type)
        {
            this.m_type = type;
        }

        //---------------------------------------------------------------------------------------------------
        public WorkLog Clone()
        {
            return new WorkLog(m_type)
            {
                RequestCount = this.m_lRequestCount
            };
        }
    }
}
