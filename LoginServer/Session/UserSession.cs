using Common.Constants;
using Core.Client;
using Core.Network;
using LoginServer.Data;
using LoginServer.Database;
using LoginServer.FSM;
using Security;
using ServerFramework;
using System;
using Core.Event;
using System.Collections.Generic;
using System.Text;
using LoginServer.Network.Event;

namespace LoginServer.Session
{
    public class UserSession : Client
    {
        public ulong UserID { get; private set; }

        public byte[] SessionKey { get; private set; }

        private const int m_dwUpdateServerListLimitTime = 60 * 1000;    // 60 seconds.

        private const int m_dwLoginStayLimitTime = 30 * 60 * 1000;      // 30 minutes.
        private const int m_dwConnectStayLimitTime = 30 * 60 * 1000;    // 30 minutes

        #region Ticks
        enum ENUM_TICKS
        {
            CONNECT_TICK = 0,
            AUTHEN_TICK,
            SEND_SERVER_LIST,
            SEND_CHANNEL_NEWS,
            SERVER_STAY_TICK,

            MAX_TICKS
        };

        private long[] m_auiTickCount;

        private long GetTick(int nIndex) { return m_auiTickCount[nIndex]; }

        private void SetTick(int nIndex) { m_auiTickCount[nIndex] = Environment.TickCount64; }
        #endregion

        protected DateTimeOffset m_prevUpdateTime = DateTimeOffset.MinValue;

        protected DateTimeOffset m_currentUpdateTime = DateTimeOffset.MinValue;

        #region Worker

#if _USE_WORKER
        private readonly object m_syncObject = new object();
        public object SyncObject => m_syncObject;

        protected bool m_bReleased;

        private readonly SFDynamicWorker m_worker = new SFDynamicWorker();

        //---------------------------------------------------------------------------------------------------
        protected void InitWorker(DateTimeOffset currentTime)
        {
            this.m_worker.IsAsyncErrorLogging = true;
            this.m_worker.Start();

            this.m_prevUpdateTime = currentTime;
            this.m_currentUpdateTime = currentTime;
        }

        //---------------------------------------------------------------------------------------------------
        public void AddWork(ISFRunnable work, bool bGlobalLockRequired)
        {
            this.m_worker.EnqueueWork(new SFAction<ISFRunnable, bool>(this.RunWork, work, bGlobalLockRequired));
        }

        //---------------------------------------------------------------------------------------------------
        private void RunWork(ISFRunnable work, bool bGlobalLockRequired)
        {
            if (bGlobalLockRequired)
            {
                lock (Global.SyncObject)
                {
                    this.InvokeRunWorkInternal(work);
                    return;
                }
            }

            this.InvokeRunWorkInternal(work);
        }

        //---------------------------------------------------------------------------------------------------
        protected void InvokeRunWorkInternal(ISFRunnable work)
        {
            lock (SyncObject)
            {
                RunWorkInternal(work);
            }
        }

        //---------------------------------------------------------------------------------------------------
        protected void RunWorkInternal(ISFRunnable work)
        {
            if (this.m_bReleased) return;

            work.Run();
        }

        //---------------------------------------------------------------------------------------------------
        public void Release()
        {
            if (this.m_bReleased) return;

            this.ReleaseInternal();
            this.m_bReleased = true;
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void ReleaseInternal()
        {
            this.m_worker.Stop(true);
        }

#endif
        #endregion

        public UserSession()
        {
            // Set ticks.
            m_auiTickCount = new long[(int)ENUM_TICKS.MAX_TICKS];

            SetTick((int)ENUM_TICKS.SEND_SERVER_LIST);
            SetTick((int)ENUM_TICKS.AUTHEN_TICK);
            SetTick((int)ENUM_TICKS.SEND_CHANNEL_NEWS);
            SetTick((int)ENUM_TICKS.CONNECT_TICK);

#if _USE_WORKER
            // Worker.
            InitWorker(DateTimeOffset.Now);
#endif
        }

        protected override void OnDisconnected(object objSender, EventArgs e)
        {
            if (!(objSender is TcpClient client))
            {
                SFLogUtil.Error(base.GetType(), $"OnDisconnected received a sender-client invalid.");
                return;
            }

            ulong ulClientIndex = ((UserSession)client.Tag)?.UID ?? 0;
            string strClientName = ((UserSession)client.Tag)?.Name;

            if (this.IsReserveDestroy() == false)
            {
                LoginManager.Instance.ClientManager.ReserveDelete(this.Name);

                SetReserveDestroy(true);
            }

            client.Disconnected -= OnDisconnected;
        }

        public override void Tick()
        {
#if _USE_WORKER
            AddWork(new SFAction(OnTick), false);
#else
            OnTick();
#endif
        }

        protected void OnTick()
        {
            base.Tick();

            // Update prev and current update time.
            this.m_currentUpdateTime = DateTime.Now;
            this.m_prevUpdateTime = this.m_currentUpdateTime;

            UpdateTicksData();

            switch (GetStateID())
            {
                case (int)EnUserFSMState.STATE_INIT:
                case (int)EnUserFSMState.STATE_CONNECTED:
                case (int)EnUserFSMState.STATE_LOGINED:
                    {
                    }
                    break;
                case (int)EnUserFSMState.STATE_EXIT:
                    {
                        LoginManager.Instance.ClientManager.ReserveDelete(this.Name);

                        SetReserveDestroy(true);
                    }
                    break;
                default:
                    {
                        SFLogUtil.Error(base.GetType(), $"Invalid state id {GetStateID()}");
                    }
                    break;
            }

#if DEBUG
            int nElapsedTime = m_currentUpdateTime.Millisecond - m_prevUpdateTime.Millisecond;
            if (nElapsedTime > 150)
            {
                SFLogUtil.Warn(base.GetType(), $"take more than {nElapsedTime} ms.");
            }
#endif
        }

        private void UpdateTicksData()
        {
            switch ((EnUserFSMState)GetStateID())
            {
                case EnUserFSMState.STATE_CONNECTED:
                    {
                        if (Environment.TickCount64 - GetTick((int)ENUM_TICKS.CONNECT_TICK) >= m_dwConnectStayLimitTime)
                        {
                            SFLogUtil.Warn(base.GetType(),
                                $"Stayed connected for a long time {GetTick((int)ENUM_TICKS.CONNECT_TICK)} ticks, will be disconnected.");

                            ReserveDestroy();
                            return;
                        }
                    }
                    break;
                case EnUserFSMState.STATE_LOGINED:
                    {
                        if (Environment.TickCount64 - GetTick((int)ENUM_TICKS.AUTHEN_TICK) >= m_dwLoginStayLimitTime)
                        {
                            SFLogUtil.Warn(base.GetType(),
                                $"Inactive account without logging for a long time {GetTick((int)ENUM_TICKS.AUTHEN_TICK)} ticks, will be disconnected.");

                            ReserveDestroy();
                            return;
                        }

                        if (Environment.TickCount64 - GetTick((int)ENUM_TICKS.SEND_SERVER_LIST) >= m_dwUpdateServerListLimitTime)
                        {
                            ServerList();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void AcceptConnection(in ushort usSPI, in SecurityAssociation securityAssociation)
        {
            ServerEvent.SendAcceptConnection(this, usSPI, securityAssociation.GetAuthKey(), securityAssociation.GetCryptoKey(), securityAssociation.GetSequenceNum(),
                securityAssociation.LastSequenceNum, securityAssociation.ReplayWindowMask);
        }

        public override void OnRemove()
        {
#if _USE_WORKER
            // Release the worker.
            Release();
#endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            switch ((EnUserFSMState)GetStateID())
            {
                case EnUserFSMState.STATE_EXIT:
                    {
                        return;
                    }
                case EnUserFSMState.STATE_INIT:
                case EnUserFSMState.STATE_CONNECTED:
                case EnUserFSMState.STATE_LOGINED:
                    {
                        StateTransition((int)EnUserFSMInput.INPUT_EXIT_GAME);
                        return;
                    }
            }

            SFLogUtil.Error(this.GetType(), "User termination processing-nonsense. " +
                                            $"Index: {this.UID} | Name: {this.Name} | State: {(EnUserFSMState)GetStateID()}.");

            LoginManager.Instance.ClientManager.ReserveDelete(this.Name);
        }

        #region Packet's

        protected override void OnRecvCompleted(IPacket packet)
        {
#if DEBUG
            long lElapsedTime = Environment.TickCount64;
#endif

            switch ((OPCODE_UL)packet.OpCode)
            {
                case OPCODE_UL.UL_ACCOUNT_REQ: OnPacketCheckUser((UL_ACCOUNT_REQ)packet); break;
                case OPCODE_UL.UL_LOGOUT_REQ: OnPacketLogout((UL_LOGOUT_REQ)packet); break;
                default:
                    {
                        SFLogUtil.Warn(base.GetType(), $"Packet ID: {packet.OpCode} is not defined.");
                    }
                    break;
            }

#if DEBUG
            lElapsedTime = Environment.TickCount64 - lElapsedTime;
            if (lElapsedTime > 500)
            {
                SFLogUtil.Warn(base.GetType(),
                    $"Packet delayed processing longer than expected. Elapsed Time: {lElapsedTime} ms.");
            }
#endif
        }

        #region Process Packet's

        private void OnPacketCheckUser(UL_ACCOUNT_REQ packet)
        {
            packet.IP = this.Session?.RemoteEndPoint?.Address.ToString();

            DatabaseManager.Instance.QueueingEvent((ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_REQ, base.Name, 0, packet);
        }

        private void OnPacketLogout(UL_LOGOUT_REQ packet)
        {
            // TODO : ...
        }

        #endregion

        #endregion

        #region Event's

        protected override void ProcessEvent(IKEvent event_)
        {
#if DEBUG
            long elapsedTime = Environment.TickCount64;
#endif

            switch (event_.EventID)
            {
                case (ushort)EnDatabaseEventID.DB_EVENT_VERIFY_ACCOUNT_RES: OnEventAccountResponse(event_.Buffer, event_.RetCode); break;
                default:
                    {
                        SFLogUtil.Warn(base.GetType(), $"Event ID: {event_.EventID.ToString()} is not defined.");
                    }
                    break;
            }

#if DEBUG
            elapsedTime = Environment.TickCount64 - elapsedTime;
            if (elapsedTime > 500)
            {
                SFLogUtil.Warn(base.GetType(),
                    $"Event delayed processing longer than expected. Elapsed Time: {elapsedTime} ms.");
            }
#endif
        }

        #region Process Event's

        public void OnEventAccountResponse(object objBuffer, int nRetCode)
        {
            if (nRetCode != (int)EnMessageError.OK)
            {
                ServerEvent.SendAccountResponse(this, 0, "", null, nRetCode, -1);
                return;
            }

            if (objBuffer is null)
            {
                ReserveDestroy(EnDisconnectReasonWhy.AuthenFail);
                return;
            }

            VerifyAccountData data = (VerifyAccountData)objBuffer;

            UserID = data.UserID;
            SessionKey = data.SessionKey;

            LoginManager.Instance.ClientManager.Rename(base.Name, data.Name);

            base.UID = data.UserID;
            LoginManager.Instance.ClientManager.RegByUID(this);

            ServerList();

            ServerEvent.SendAccountResponse(this, data.UserID, data.Name, data.SessionKey, nRetCode, data.LastServerID);

            StateTransition((int)EnUserFSMInput.INPUT_VERIFICATION_OK);
        }

        #endregion

        #endregion

        private void ServerList()
        {
            ServerEvent.SendServerList(this, LoginManager.Instance.ServerInfoList);

            SetTick((int)ENUM_TICKS.SEND_SERVER_LIST);
        }

    }
}
