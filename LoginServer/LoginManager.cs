using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Core.Client;
using Core.Network;
using Core.Singleton;
using LoginServer.Application;
using LoginServer.Database;
using LoginServer.FSM;
using LoginServer.Session;
using Serialization.Data;
using ServerFramework;

namespace LoginServer
{
    public class LoginManager /*: Singleton<LoginManager>*/
    {
        private static volatile LoginManager s_instance;
        private static readonly object s_syncRoot = new Object();

        public static LoginManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_syncRoot)
                    {
                        s_instance = new LoginManager();
                    }
                }
                return s_instance;
            }
        }

        private bool m_bReleased = false;

        public bool Init()
        {
            SFLogUtil.Info(base.GetType(), "LoginManager.Init() started.");

            this.m_kClientManager.SetClientFactory(new ClientFactory<UserSession, UserFSM>());

            OnUpdateServerList();

            this.m_updateTimer = new Timer(OnUpdateTimerTick);
            this.m_updateTimer.Change(UPDATE_INTERVAL, UPDATE_INTERVAL);

            this.m_fCurrentDeltaTime = 0f;

            SFLogUtil.Info(base.GetType(), "LoginManager.Init() finished.");
            return true;
        }

        #region Tick

        public const int UPDATE_INTERVAL = 100;

        private Timer m_updateTimer = null;

        private DateTimeOffset m_prevUpdateTime = DateTimeOffset.MinValue;

        private DateTimeOffset m_currentUpdateTime = DateTimeOffset.MinValue;

        private float m_fCurrentDeltaTime = 0f;

        public float DeltaTime => m_fCurrentDeltaTime;

        private void OnUpdateTimerTick(object state)
        {
            Global.Instance.AddWork(new SFAction(this.OnUpdate));
        }

        private void DisposeUpdateTimer()
        {
            if (this.m_updateTimer == null) return;

            this.m_updateTimer.Dispose();
            this.m_updateTimer = null;
        }

        private void OnUpdate()
        {
            this.m_prevUpdateTime = this.m_currentUpdateTime;
            this.m_currentUpdateTime = DateTime.Now;
            this.m_fCurrentDeltaTime = (float)(this.m_currentUpdateTime - this.m_prevUpdateTime).TotalSeconds;

            if (this.m_prevUpdateTime.Date != this.m_currentUpdateTime.Date)
            {
                this.OnDateChanged();
            }

            this.m_kClientManager.Tick();

            this.OnUpdateServerList();
        }

        private void OnDateChanged()
        {
            SFLogUtil.Info(base.GetType(), "LoginManager.OnDateChanged() finished.");
        }

        public void OnUpdateServerList()
        {
            if (this.m_prevUpdateTime.Second / 30 == this.m_currentUpdateTime.Second / 30)
            {
                return;
            }

            DatabaseManager.Instance.QueueingID((ushort)EnDatabaseEventID.DB_EVENT_SERVER_LIST_REQ, "", 0);
        }

        #endregion

        #region Client's

        private ClientManager m_kClientManager = new ClientManager();

        public ClientManager ClientManager => m_kClientManager;

        public void SetCreateClientEnable(bool bEnable) { m_kClientManager.SetCreateClientEnable(bEnable); }

        public bool CreateClient(ulong uid, TcpClient session)
        {
            UserSession client = m_kClientManager.CreateClient<UserSession>((int)EnUserFSMInput.INPUT_CONNECT);
            if (client == null)
            {
                SFLogUtil.Warn(base.GetType(), "Failed to create a new client.");

                session.Disconnect();
                return false;
            }

            client.UID = uid;

            client.SetSocketInfo(session);

            client.Session.Tag = client;

            session.OnConnectionSucceed(EventArgs.Empty);

            client.OnAcceptConnection();

            return m_kClientManager.Add(client);
        }

        #endregion


        #region Server List

        private object m_csServerInfo = new object();

        private List<ServerData> m_vecServerInfoList = new List<ServerData>();

        public List<ServerData> ServerInfoList
        {
            get
            {
                lock (m_csServerInfo)
                {
                    return m_vecServerInfoList;
                }
            }
        }

        public void UpdateServerList(List<ServerData> listServer)
        {
            lock (m_csServerInfo)
            {
                m_vecServerInfoList.Clear();

                foreach (ServerData server in listServer)
                {
                    m_vecServerInfoList.Add(server);
                }
            }
        }

        #endregion

        public void OnShutDown()
        {
            SFLogUtil.Info(base.GetType(), "LoginManager.OnShutDown() finished.");
        }

        public void Release()
        {
            if (this.m_bReleased)
            {
                return;
            }

            this.DisposeUpdateTimer();

            this.m_bReleased = true;

            SFLogUtil.Info(base.GetType(), "LoginManager.Release() finished.");
        }

    }
}
