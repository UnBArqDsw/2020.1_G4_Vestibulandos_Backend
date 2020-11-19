using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Core.Configuration;
using ServerFramework;
using ServerFramework.Application;
using LoginServer.Database;
using LoginServer.Database.Util;
using Core.Threading;
using LoginServer.Network;

namespace LoginServer.Application
{
    public enum QueuingWorkTargetType { None = 0, Member, Log }

    public class LoginServerApp : SFApplication
    {
        public static LoginServerApp Inst => Instance as LoginServerApp;
        private const uint SERVER_SLEEP_CONST = 50;
        private const uint SERVER_REDUCE_MEMORY_CONST = 60 * 60 * 1000;
        private int m_nServerId = 0;
        public int ServerId => m_nServerId;
        private long m_lStartupBegin = 0;
        private TimeSpan m_currentTimeOffset = TimeSpan.Zero;
        private bool m_bClose = false;
        protected Timer m_reduceMemoryTimer = null;
        public TimeSpan CurrentTimeOffset => m_currentTimeOffset;
        public DateTimeOffset CurrentTime => DateTimeOffset.Now + m_currentTimeOffset;

        public void Start()
        {
            SFLogUtil.Info(GetType(), "Starting the server...");

            OnStart("Login", "Login Server");
            OnRunning();
        }

        public void Shutdown()
        {
            SFLogUtil.Info(GetType(), "Stopping the server...");
            OnStop();
        }
        public void Close()
        {
            m_bClose = true;
        }
        private bool LoadConfiguration()
        {
            return ConfigMgr.Load("LoginServer.conf", BinaryPath);
        }

        protected override void Initialize()
        {
            m_lStartupBegin = Environment.TickCount64;

            base.Initialize();

            if (!LoadConfiguration())
            {
                SFLogUtil.Fatal(GetType(), "Failed to load configuration file.");
                Shutdown();

                return;
            }

            DatabaseConfiguration();

            m_nServerId = Convert.ToInt16(ConfigMgr.GetDefaultValue("ServerID", "0"));
            if (m_nServerId == 0)
            {
                SFLogUtil.Fatal(GetType(), "Invalid Server Index, please check the configuration file.");
                Shutdown();

                return;
            }

            if (m_nServerId < (int)EnServerIndex.LoginServerBegin || m_nServerId > (int)EnServerIndex.LoginServerEnd)
            {
                SFLogUtil.Fatal(GetType(), $"Invalid Server Index, index need to be min {(int)EnServerIndex.LoginServerBegin} and max {(int)EnServerIndex.LoginServerEnd}. " +
                                                "Please check the configuration file.");
                Shutdown();

                return;
            }

            Global.Instance.Init();
            //WorkLogManager.instance.Init();

            InitGlobalTimer();

            lock (Global.SyncObject)
            {
                DatabaseManager.Instance.Init();

                if (!LoginManager.Instance.Init())
                {
                    SFLogUtil.Info(GetType(), "Failed to initialize the Login Manager.");
                    return;
                }

                Security.Security.InitSecurity();
            }

            StartConnectors();
        }

        public void DatabaseConfiguration()
        {
            DatabaseUtil.SetMemberDBConnection(
                ConfigMgr.GetDefaultValue("DatabaseMemberInfo.Address", ""), ConfigMgr.GetDefaultValue("DatabaseMemberInfo.Database", ""),
                ConfigMgr.GetDefaultValue("DatabaseMemberInfo.Username", ""), ConfigMgr.GetDefaultValue("DatabaseMemberInfo.Password", ""));

            DatabaseUtil.SetLogDBConnection(
                ConfigMgr.GetDefaultValue("DatabaseLogInfo.Address", ""), ConfigMgr.GetDefaultValue("DatabaseLogInfo.Database", ""),
                ConfigMgr.GetDefaultValue("DatabaseLogInfo.Username", ""), ConfigMgr.GetDefaultValue("DatabaseLogInfo.Password", ""));
        }

        public void Run()
        {
            JobProcessor thread = new JobProcessor();
            thread.Start();

            SocketManager socketMgr = new SocketManager(m_nServerId, thread);
            if (!socketMgr.Start(
                ConfigMgr.GetDefaultValue("IP", "0.0.0.0"),
                Convert.ToUInt16(ConfigMgr.GetDefaultValue("Port", "0")),
                Convert.ToUInt16(ConfigMgr.GetDefaultValue("Backlog", "100"))))
            {
                SFLogUtil.Info(base.GetType(), "Failed to initialize the Socket Manager!");

                this.Shutdown();
                return;
            }

            LoginManager.Instance.SetCreateClientEnable(true);
            SFLogUtil.Info(base.GetType(), "Enabled to receive new client's.");

            long uiStartupDuration = Environment.TickCount64 - m_lStartupBegin;
            SFLogUtil.Info(base.GetType(),
                $"{base.InstanceName} initialized in {(uiStartupDuration / 60000)} minutes {((uiStartupDuration % 60000) / 1000)} seconds.", null, false, false);

            if (ConfigMgr.GetDefaultValue("Console.Enable", true))
            {
                Thread commandThread = new Thread(CommandHandler)
                {
                    IsBackground = true
                };

                commandThread.Start();
            }

            long lRealPreviousTime = Environment.TickCount64;

            while (base.Running)
            {
                long lRealCurrentTime = Environment.TickCount64;

                long lDiff = lRealCurrentTime - lRealPreviousTime;

                if (lDiff > SERVER_SLEEP_CONST * 2)
                {
                    SFLogUtil.Warn(base.GetType(), $"Server took {lDiff} more time than usual to process.");
                }

                lRealPreviousTime = lRealCurrentTime;

                long lExecutionTimeDiff = Environment.TickCount64 - lRealCurrentTime;

                if (m_bClose)
                {
                    break;
                }

                if (lExecutionTimeDiff < SERVER_SLEEP_CONST)
                {
                    Thread.Sleep((int)(SERVER_SLEEP_CONST - lExecutionTimeDiff));
                }
            }

            try
            {
                DatabaseManager.Instance.StopAndWaitFinish(true);

                socketMgr.Stop();
                thread.Stop(true);
            }
            catch (Exception e)
            {
                SFLogUtil.Error(base.GetType(), "", e);
                throw;
            }
        }

        protected override void InitQueuingWorkManager()
        {
            foreach (object obj in Enum.GetValues(typeof(QueuingWorkTargetType)))
            {
                int nTargetType = (int)obj;
                this.m_queuingWorkManager.CreateWorkCollection(nTargetType);
            }
        }

        protected override void OnShutDown()
        {
            StopGlobalTimer();

            lock (Global.SyncObject)
            {
                LoginManager.Instance.OnShutDown();
                WorkLogManager.Instance.Release();

                LoginManager.Instance.Release();
                Global.Instance.Release();
                Security.Security.ReleaseSecurity();
            }

            base.OnShutDown();
        }

        private void StartConnectors()
        {
            // Game Connector
        }

        private bool InitGlobalTimer()
        {
            if (this.m_reduceMemoryTimer == null)
            {
                this.m_reduceMemoryTimer = new Timer(this.ReduceMemory, null, SERVER_REDUCE_MEMORY_CONST, SERVER_REDUCE_MEMORY_CONST);
            }
            else
            {
                this.m_reduceMemoryTimer.Change(SERVER_REDUCE_MEMORY_CONST, SERVER_REDUCE_MEMORY_CONST);
            }

            return true;
        }

        private bool StopGlobalTimer()
        {
            if (this.m_reduceMemoryTimer != null)
            {
                this.m_reduceMemoryTimer.Change(-1, -1);
                this.m_reduceMemoryTimer.Dispose();
                this.m_reduceMemoryTimer = null;
            }

            return true;
        }

        private void ReduceMemory(object sender)
        {
            SFLogUtil.Info(base.GetType(), $"Cleaning Memory at {DateTime.Now}.");

            int iTickNow = Environment.TickCount;

            GC.Collect();

            SFLogUtil.Info(base.GetType(), $"Cleaning Memory in {(Environment.TickCount - iTickNow)} ms.");
        }

        public void CommandHandler()
        {
            ReadLine.HistoryEnabled = true;

            while (!m_bClose)
            {
                string cmd = ReadLine.Read();
                ReadLine.AddHistory(cmd);

                switch (cmd)
                {
                    case "gc":
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }
                        break;
                    case "exit":
                        {
                            this.Close();
                            //this.Shutdown();
                        }
                        break;
                    case "cls":
                        {
                            Console.Clear();
                        }
                        break;
                    default:
                        {
                            SFLogUtil.Warn(base.GetType(), $"Command [{cmd}] not exists.");
                        }
                        break;
                }
            }
        }

    }
}
