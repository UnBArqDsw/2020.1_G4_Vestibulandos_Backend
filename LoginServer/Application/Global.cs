using ServerFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoginServer.Application
{
    public class Global
    {
        private static Global m_instance = new Global();
        public static Global Instance => m_instance;
        private bool m_bReleased;
        private readonly SFDynamicWorker m_worker = new SFDynamicWorker();
        private static readonly object m_syncObject = new object();
        public static object SyncObject => m_syncObject;

        public void Init()
        {
            m_worker.IsAsyncErrorLogging = true;
            m_worker.Start();
        }

        public void AddWork(ISFRunnable work)
        {
            m_worker.EnqueueWork(new SFAction<ISFRunnable>(new Action<ISFRunnable>(RunWork), work));
        }

        private void RunWork(ISFRunnable work)
        {
            lock (m_syncObject)
            {
                if (!m_bReleased)
                {
                    work.Run();
                }
            }
        }

        public void Release()
        {
            if (m_bReleased)
            {
                return;
            }

            m_worker.Stop(true);
            m_bReleased = true;
        }
    }
}
