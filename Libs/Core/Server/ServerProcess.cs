using Core.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Server
{
    public class ServerProcess
    {
        private System.Threading.Thread m_threadBackend = null;

        protected bool m_bRunServer = false;
        protected bool m_bEndServer = false;

        public ServerProcess()
        {
            m_threadBackend = null;

            m_bRunServer = false;
            m_bEndServer = false;
        }

        public bool Initialize()
        {
            m_threadBackend = new System.Threading.Thread(() => BackendThreadStartingPoint());
            m_threadBackend.Start();

            return true;
        }

        public void Shutdown(int nMaxWait)
        {
            m_bEndServer = true;
            m_bRunServer = false;

            if(m_threadBackend != null)
            {
                //m_threadBackend.Abort();
                m_threadBackend.Join(nMaxWait);
            }
        }

        public void BackendThreadStartingPoint()
        {
            BackendThread();
        }

        protected virtual void BackendThread()
        {
            int nBeginTick = 0;
            int nEndTick = 0;
            int nTickDiff = 0;

            while(true)
            {
                nBeginTick = Environment.TickCount;

                // Server shutdown, exit Thread.
                if (m_bEndServer == true)
                    break;

                nEndTick = Environment.TickCount;
                nTickDiff = nEndTick - nBeginTick;

                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
