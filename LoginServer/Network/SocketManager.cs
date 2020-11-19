#define _GENERATOR_UNIQUE_INDEX_GENERATOR

using System;
using System.Net;
using Core.Network;
using Core.Threading;
using ServerFramework;
#if _GENERATOR_UNIQUE_INDEX_GENERATOR
using IdGen;
#else
using Troschuetz.Random.Generators;
#endif

namespace LoginServer.Network
{
    public class SocketManager
    {
        private readonly ITcpServer m_server;

        private readonly JobProcessor m_jobThread;

        public JobProcessor Thread => m_jobThread;

#if _GENERATOR_UNIQUE_INDEX_GENERATOR
        private readonly IdGenerator m_generator = null;

        private readonly object m_objGeneratorLock = null;
#else
        private readonly MT19937Generator m_mtRandom = new MT19937Generator();
#endif

        public SocketManager(int nServerID, JobProcessor thread)
        {
#if _GENERATOR_UNIQUE_INDEX_GENERATOR
            DateTime epoch = new DateTime(2015, 4, 1, 0, 0, 0, DateTimeKind.Utc);

#if false
            MaskConfig mc = new MaskConfig(32, 10, 21);

            // Start the unique index generator.
            m_generator = new IdGenerator(nServerID, epoch, mc);
#else

            MaskConfig mc = new MaskConfig(45, 2, 16);

            m_generator = new IdGenerator(0, epoch, mc);

            m_objGeneratorLock = new object();
#endif

#endif

            m_jobThread = thread;

            m_server = new TcpServer();

            m_server.ClientAccept += OnClientAccept;
        }

        public bool Start(string strIP, int nPort, int nBacklog)
        {
            try
            {
                if (!IPAddress.TryParse(strIP, out IPAddress address))
                {
                    SFLogUtil.Error(base.GetType(), $"Server can't be started: Invalid IP-Address ({strIP})");
                    return false;
                }

                m_server.Start(m_jobThread, address, nPort, nBacklog);

                return true;
            }
            catch (Exception ex)
            {
                SFLogUtil.Fatal(base.GetType(), "Exception exception occurred on SocketManager.Start()", ex);
                return false;
            }
        }

        public void Stop()
        {
            if (m_server != null)
            {
                m_server.ClientAccept -= OnClientAccept;

                m_server.Stop();
            }
        }

        private void OnClientAccept(object objSender, AcceptEventArgs args)
        {
            args.JobProcessor = m_jobThread;

            m_jobThread.Enqueue(
                Job.Create(() =>
                LoginManager.Instance.CreateClient(GenerateKey(), args.Client)));
        }

        private ulong GenerateKey()
        {
#if _GENERATOR_UNIQUE_INDEX_GENERATOR
            long lIndex = 0;

            bool bRet = false;

            do
            {
                lock (m_objGeneratorLock)
                {
                    // Generate the key.
                    //lKey = m_generator.CreateId();

                    bRet = m_generator.TryCreateId(out lIndex);
                }

            } while (bRet && lIndex <= 0);

            return (ulong)lIndex;
#else
            int count = 0;

            do
            {
                uint key = m_mtRandom.NextUInt(1, uint.MaxValue);

                if (LoginManager.Instance.ClientManager.GetByUID(key) == null)
                    return key;

                if (count++ > 1000) SFLogUtil.Warn(base.GetType(), $"It was processed over {count} times to generate a key.");

            } while (true);
#endif
        }
    }
}
