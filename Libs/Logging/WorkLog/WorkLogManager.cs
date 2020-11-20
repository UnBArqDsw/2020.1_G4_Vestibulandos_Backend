using System;
using System.Collections.Generic;
using System.Threading;

namespace Logging.WorkLog
{
    public class WorkLogManager
    {
        // ==================================================================================================
        // CONSTANT's
        // ==================================================================================================

        /// <summary>
        /// Save interval.
        /// </summary>
        public const int SAVE_TIMER_INTERVAL = 60000;

        // ==================================================================================================
        // STATIC's
        // ==================================================================================================

        /// <summary>
        /// Singleton.
        /// </summary>
        private static WorkLogManager s_instance = new WorkLogManager();

        // ==================================================================================================
        // VARIABLE's
        // ==================================================================================================

        /// <summary>
        /// Mutex.
        /// </summary>
        private object m_syncObject = new object();

        /// <summary>
        /// Dictionary with the log.
        /// </summary>
        private Dictionary<Type, WorkLog> m_dictLog = new Dictionary<Type, WorkLog>();

        /// <summary>
        /// Timer for save in every period.
        /// </summary>
        private Timer m_saveTimer = null;

        /// <summary>
        /// Released.
        /// </summary>
        private bool m_bReleased = false;

        // ==================================================================================================
        // PROPERTY'ies
        // ==================================================================================================

        /// <summary>
        /// Get the instance.
        /// </summary>
        public static WorkLogManager instance => s_instance;

        // ==================================================================================================
        // FUNCTION's
        // ==================================================================================================

        //---------------------------------------------------------------------------------------------------
        public void Init()
        {
            lock (this.m_syncObject)
            {
                this.m_saveTimer = new Timer(new TimerCallback(this.OnSaveTimerTick));
                this.m_saveTimer.Change(SAVE_TIMER_INTERVAL, SAVE_TIMER_INTERVAL);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private WorkLog GetLog(Type type)
        {
            if (!this.m_dictLog.TryGetValue(type, out WorkLog log))
                return null;

            return log;
        }

        //---------------------------------------------------------------------------------------------------
        private WorkLog GetOrCreateLog(Type type)
        {
            WorkLog log = this.GetLog(type);

            if (log == null)
            {
                log = new WorkLog(type);
                this.m_dictLog.Add(log.Type, log);
            }

            return log;
        }

        //---------------------------------------------------------------------------------------------------
        public void AddLog(Type type)
        {
            if (type == null)
                return;

            lock (this.m_syncObject)
            {
                this.GetOrCreateLog(type).RequestCount += 1;
            }
        }

        //---------------------------------------------------------------------------------------------------
        private WorkLog[] GetLogs()
        {
            WorkLog[] insts = new WorkLog[this.m_dictLog.Count];

            int nIndex = 0;
            foreach (WorkLog inst in this.m_dictLog.Values)
            {
                insts[nIndex++] = inst.Clone();
            }

            this.m_dictLog.Clear();
            return insts;
        }

        //---------------------------------------------------------------------------------------------------
        private void OnSaveTimerTick(object state)
        {
            WorkLog[] arrLog = null;
            lock (this.m_syncObject)
            {
                if (m_bReleased)
                    return;

                arrLog = this.GetLogs();
            }

            this.Save(arrLog);
        }

        //---------------------------------------------------------------------------------------------------
        private void Save(WorkLog[] logs)
        {
            // TODO : Check this function !!
            //DateTimeOffset currentTime = DateTimeUtil.currentTime;
            //SqlConnection conn = null;
            //SqlTransaction trans = null;
            //try
            //{
            //    conn = DBUtil.OpenGameLogDBConnection();
            //    trans = conn.BeginTransaction();
            //    Guid logId = Guid.NewGuid();
            //    if (GameLogDac.AddWorkLog(conn, trans, logId, currentTime) != 0)
            //    {
            //        throw new Exception("작업로그 등록 실패.");
            //    }
            //    foreach (WorkLog log in logs)
            //    {
            //        if (GameLogDac.AddWorkLogEntry(conn, trans, Guid.NewGuid(), logId, log.type.Name, log.requestCount) != 0)
            //        {
            //            throw new Exception("작업로그항목 등록 실패.");
            //        }
            //    }
            //    SFDBUtil.Commit(ref trans);
            //    SFDBUtil.Close(ref conn);
            //}
            //finally
            //{
            //    SFDBUtil.Rollback(ref trans);
            //    SFDBUtil.Close(ref conn);
            //}
        }

        //---------------------------------------------------------------------------------------------------
        private void DisposeSaveTimer()
        {
            if (this.m_saveTimer == null)
            {
                return;
            }

            this.m_saveTimer.Dispose();
            this.m_saveTimer = null;
        }

        //---------------------------------------------------------------------------------------------------
        public void Release()
        {
            lock (this.m_syncObject)
            {
                if (!this.m_bReleased)
                {
                    this.DisposeSaveTimer();
                    this.m_bReleased = true;
                }
            }
        }
    }
}
