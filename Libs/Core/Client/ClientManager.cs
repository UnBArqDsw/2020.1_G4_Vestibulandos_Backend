using System;
using Collections.Pooled;
using Core.Constants;
using Core.Event;
using Serialization;
using ServerFramework;

namespace Core.Client
{
    public class ClientManager
    {
        /// <summary>
        /// Current Max tick.
        /// </summary>
        private static int s_nMaxTick = 50;

        /// <summary>
        /// Client Factory.
        /// </summary>
        protected IClientFactory m_spClientFactory = null;

        /// <summary>
        /// It protects m_listClient and m_dictClientName at the same time.
        /// </summary>
        private object m_csClient = new object();

        /// <summary>
        /// List with all clients connecteds.
        /// </summary>
        protected PooledList<Core.Client.Client> m_listClient = null;

        /// <summary>
        /// Dictionary with all clients connecteds.
        /// </summary>
        protected PooledDictionary<string, Core.Client.Client> m_dictClientName = null;

        /// <summary>
        /// Client's count.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this.m_csClient)
                {
                    return this.m_dictClientName.Count;
                }
            }
        }

        /// <summary>
        /// Since it is independent of the creation of the object, only a single thread is accessed.
        /// </summary>
        protected PooledDictionary<ulong, Core.Client.Client> m_mapClientUID = null;

        /// <summary>
        /// Mutex Queue Reserved Delete.
        /// </summary>
        private object m_csDel = new object();

        /// <summary>
        /// Queue with client's reserved to delete.
        /// </summary>
        protected PooledQueue<string> m_setDelReserved = null;

        /// <summary>
        /// Mutex Queue Event's.
        /// </summary>
        private object m_csEventQueue = new object();

        /// <summary>
        /// Queue with all event's.
        /// </summary>
        protected PooledQueue<KIntEvent> m_queueEvent = null;

        /// <summary>
        /// If server is already accepting new clients connections.
        /// </summary>
        protected bool m_bCreateClientEnable = false;

        /// <summary>
        /// Queue event size.
        /// </summary>
        protected int m_nQueEventSize = 0;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClientManager()
        {
            this.m_bCreateClientEnable = false;
            this.m_nQueEventSize = 0;

            // Initialize the list client.
            this.m_listClient = new PooledList<Core.Client.Client>(NetworkConst.MAX_CCU);

            // Intialize the dictionary client by name.
            this.m_dictClientName = new PooledDictionary<string, Core.Client.Client>();
            
            // Initialize the dictionary client by uid.
            this.m_mapClientUID = new PooledDictionary<ulong, Core.Client.Client>();

            // Initiliaze the queue reserved to delete client's.
            this.m_setDelReserved = new PooledQueue<string>();

            // Initialize the queue event's.
            this.m_queueEvent = new PooledQueue<KIntEvent>();
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Process event's, process tick's from all client's and delete the reserved client's.
        /// </summary>
        public void Tick()
        {
            // Current tick.
            int nElapsedTime = Environment.TickCount;

            // Step 1. Distribute the events in the queue to each client.
            DistributeEvent();

            // Calcule the elapsed time by distribuing the event's.
            nElapsedTime = Environment.TickCount - nElapsedTime;
            if (nElapsedTime > s_nMaxTick)
            {
                // Update the tick.
                s_nMaxTick = nElapsedTime;

                // Set the warning log.
                SFLogUtil.Warn(base.GetType(), $"Max DistributeEvent Time: {s_nMaxTick}");
            }

            // Step 2. Handle event for each client.
            lock (this.m_csClient)
            {
                for (int nIndex = 0; nIndex < m_listClient.Count; nIndex++)
                {
                    // Tick the client.
                    this.m_listClient[nIndex]?.Tick();
                }
            }

            // Step 3. Destroy pending user objects in the deleted state.
            // (Cannot be deleted or inserted in the loop in Step 2.)
            if (SwapDeleteReservedList(out PooledQueue<string> queueDelReserved))
            {
                for (int nIndex = 0; nIndex < queueDelReserved.Count; nIndex++)
                {
                    Delete(queueDelReserved.Dequeue());
                }

                queueDelReserved.Dispose();
            }
        }

        //---------------------------------------------------------------------------------------------------
        private void DistributeEvent()
        {
            // Check if swapped with success the queue.
            if (!SwapQueue(out PooledQueue<KIntEvent> queueEvent))
            {
                return;
            }

            // Let to process the queue events.
            while (queueEvent.Count > 0)
            {
                // Dequeue the event.
                KIntEvent currEvent = queueEvent.Dequeue();
                
                // Check if event is null, if yes, go to next event.
                if (currEvent == null)
                {
                    // Event is null, ignore it.
                    continue;
                }

                // Get the client. If UID from sender received is null, try to get client by name.
                Core.Client.Client client = (currEvent.SenderUID == 0) ? 
                    GetByName(currEvent.Sender) : 
                    GetByUID(currEvent.SenderUID);

                // Queieng the event.
                client?.QueueingIntEvent(currEvent);
            }

            // Dispose the event.
            queueEvent.Dispose();
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Add a new client.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Add(in Core.Client.Client client)
        {
            // Check if is a valid client.
            if (client == null)
            {
                SFLogUtil.Error(base.GetType(), "Trying to add a null client.");
                return false;
            }

            lock (this.m_csClient)
            {
                // Check if already has a client with current name added.
                if (GetByName(client.Name) != null)
                {
                    SFLogUtil.Error(base.GetType(), $"The key {client.Name} of the object to add to Client Manager is already registered.");
                    return false;
                }

                // Add the client in the list.
                this.m_listClient.Add(client);

                // Add the client in the dictionary by name.
                this.m_dictClientName.Add(client.Name, client);
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Delete the client.
        /// </summary>
        /// <param name="strName"></param>
        /// <returns></returns>
        public bool Delete(in string strName)
        {
            // Check if is a valid name.
            if (string.IsNullOrEmpty(strName))
            {
                SFLogUtil.Error(base.GetType(), "Name is null or empty.");
                return false;
            }

            lock (this.m_csClient)
            {
                // Try to get the client.
                if (!this.m_dictClientName.TryGetValue(strName, out Core.Client.Client client))
                {
                    SFLogUtil.Error(base.GetType(), "Does not have an object to delete. " +
                                                    $"Name: {strName} | Dictionary Client Name Count: {m_dictClientName.Count}");
                    return false;
                }

                // Call the OnRemove in the client.
                client.OnRemove();

                // If the UID is registered, delete it.
                this.m_mapClientUID.Remove(client.UID);

                // Remove the client from Vector.
                this.m_listClient.Remove(client);

                // Finally delete the Name key.
                this.m_dictClientName.Remove(strName);
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Reserve the client to be deleted at the next Tick().
        /// </summary>
        /// <param name="strName"></param>
        public void ReserveDelete(in string strName)
        {
            lock (this.m_csDel)
            {
                // Enqueue the client want to delete.
                this.m_setDelReserved.Enqueue(strName);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Destroy all client's.
        /// </summary>
        public void ReserveDestroyAll()
        {
            lock (this.m_csClient)
            {
                this.m_listClient.ForEach(client => client.ReserveDestroy());
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Register new client by the UID.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool RegByUID(in Core.Client.Client client)
        {
            // Check if is trying to add some invalid client.
            if (client == null)
            {
                SFLogUtil.Error(base.GetType(), "Failed to register by uid.");
                return false;
            }

            // Check if already exist some client using same uid.
            // NOTE: this should be impossible.
            if (m_mapClientUID.ContainsKey(client.UID))
            {
                SFLogUtil.Error(base.GetType(), $"Already has a client with same UID in the dictionary. UID: {client.UID}.");
                return false;
            }

            // Add the client.
            lock (this.m_csClient)
            {
                this.m_mapClientUID[client.UID] = client;
            }

            return true;
        }
        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a client by the UID.
        /// </summary>
        /// <param name="ulUID"></param>
        /// <returns></returns>
        public Core.Client.Client GetByUID(in ulong ulUID)
        {
            Core.Client.Client client = null;

            lock (this.m_csClient)
            {
                // Try to get the client.
                this.m_mapClientUID.TryGetValue(ulUID, out client);
            }

            return client;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get a client by the name.
        /// </summary>
        /// <param name="strName"></param>
        /// <returns></returns>
        public Core.Client.Client GetByName(in string strName)
        {
            Core.Client.Client client = null;

            lock (this.m_csClient)
            {
                // Try to get the client.
                this.m_dictClientName.TryGetValue(strName, out client);
            }

            return client;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Rename the client by a new name.
        /// </summary>
        /// <param name="strCurrentName"></param>
        /// <param name="strNewName"></param>
        /// <returns></returns>
        public bool Rename(in string strCurrentName, in string strNewName)
        {
            lock (this.m_csClient)
            {
                // Get the client by name.
                Core.Client.Client client = GetByName(strCurrentName);
                
                // Check if found the client.
                if (client == null)
                {
                    SFLogUtil.Error(base.GetType(), $"Failed to get the Client by name. Name: {strCurrentName} and tried to rename to {strNewName}.");
                    return false;
                }

                // Erase takes a key argument.
                if (!this.m_dictClientName.Remove(client.Name))
                {
                    SFLogUtil.Error(base.GetType(), $"Failed to remove the Client. Name: {strCurrentName} and tried to rename to {strNewName}.");
                    return false;
                }

                // Update current name for new name received.
                client.Name = strNewName;

                // Try to add new client renamed.
                return this.m_dictClientName.TryAdd(strNewName, client);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Create the new Client.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iFSMIndex"></param>
        /// <returns></returns>
        public T CreateClient<T>(in int iFSMIndex) where T : Core.Client.Client
        {
            // Check if is enabled to create new client and to create the new client in the factory.
            return !this.m_bCreateClientEnable ? null : this.m_spClientFactory.CreateClient<T>(iFSMIndex);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the Client Factory.
        /// </summary>
        /// <param name="clientFactory"></param>
        public void SetClientFactory(IClientFactory clientFactory)
        {
            this.m_spClientFactory = clientFactory;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Enable if can accept new clien'ts.
        /// </summary>
        /// <param name="bEnable"></param>
        public void SetCreateClientEnable(bool bEnable)
        {
            this.m_bCreateClientEnable = bEnable;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Swap queue between event's.
        /// </summary>
        /// <param name="queueEvents"></param>
        /// <returns></returns>
        private bool SwapQueue(out PooledQueue<KIntEvent> queueEvents)
        {
            queueEvents = null;

            lock (this.m_csEventQueue)
            {
                // Check if current queue event has events, if not, only to ignore.
                if (this.m_queueEvent.Count <= 0)
                {
                    return false;
                }

                queueEvents = new PooledQueue<KIntEvent>();

                // Save the event queue size.
                int nQueueSize = this.m_queueEvent.Count;
                SetEventQueueSize(nQueueSize);

                // Swap the queues.
                (this.m_queueEvent, queueEvents) = (queueEvents, this.m_queueEvent);
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Swap queue between reserved delete client's.
        /// </summary>
        /// <param name="queueReserved"></param>
        /// <returns></returns>
        private bool SwapDeleteReservedList(out PooledQueue<string> queueReserved)
        {
            queueReserved = null;

            lock (this.m_csDel)
            {
                // Check if has requested to delete any client.
                if (this.m_setDelReserved.Count <= 0)
                {
                    return false;
                }

                queueReserved = new PooledQueue<string>();
                
                // Swap.
                (this.m_setDelReserved, queueReserved) = (queueReserved, this.m_setDelReserved);
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Set the current event queue size.
        /// </summary>
        /// <param name="nQueueSize"></param>
        public void SetEventQueueSize(in int nQueueSize)
        {
            this.m_nQueEventSize += nQueueSize;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Get the current event queue size.
        /// </summary>
        /// <param name="nQueueSize"></param>
        /// <returns></returns>
        public bool GetEventQueueSize(out int nQueueSize)
        {
            nQueueSize = 0;

            lock (this.m_csEventQueue)
            {
                nQueueSize = this.m_nQueEventSize;
                this.m_nQueEventSize = 0;
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        #region Send Function's

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send packet to the Client by Name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strName"></param>
        /// <param name="data"></param>
        /// <param name="bCompress"></param>
        /// <returns></returns>
        public bool SendTo<T>(string strName, T data, bool bCompress = false) 
            where T : IPacket
        {
            // Get the user.
            Client client = GetByName(strName);
            if (client == null)
            {
                // Failed to find the user.
                SFLogUtil.Error(base.GetType(), $"Failed to find the Client {strName}.");
                return false;
            }

            return client.Send(data, bCompress);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send packet to the Client by UID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ulUID"></param>
        /// <param name="data"></param>
        /// <param name="bCompress"></param>
        /// <returns></returns>
        public bool SendTo<T>(ulong ulUID, T data, bool bCompress = false) 
            where T : IPacket
        {
            // Get the user.
            Client client = GetByUID(ulUID);
            if (client == null)
            {
                // Failed to find the user.
                SFLogUtil.Error(base.GetType(), $"Failed to find the Client {ulUID}.");
                return false;
            }

            return client.Send(data, bCompress);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send packet to the all client's connected.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="bCompress"></param>
        public void SendToAll<T>(T data, bool bCompress) 
            where T : IPacket
        {
            lock (m_csClient)
            {
                for (int nIndex = 0; nIndex < m_listClient.Count; nIndex++)
                {
                    m_listClient[nIndex]?.Send(data, bCompress);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send packet to the all client's connected except by the Name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strName"></param>
        /// <param name="data"></param>
        /// <param name="bCompress"></param>
        public void SendToAllExMe<T>(string strName, T data, bool bCompress) 
            where T : IPacket
        {
            lock (m_csClient)
            {
                Client client = null;

                for (int nIndex = 0; nIndex < m_listClient.Count; nIndex++)
                {
                    client = m_listClient[nIndex];
                    if (client != null && client.Name != strName)
                    {
                        client.Send(data, bCompress);
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Send packet to the all client's connected except by the UID.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ulUID"></param>
        /// <param name="data"></param>
        /// <param name="bCompress"></param>
        public void SendToAllExMe<T>(ulong ulUID, T data, bool bCompress) 
            where T : IPacket
        {
            lock (m_csClient)
            {
                Client client = null;

                for (int nIndex = 0; nIndex < m_listClient.Count; nIndex++)
                {
                    client = m_listClient[nIndex];
                    if (client != null && client.UID != ulUID)
                    {
                        client.Send(data, bCompress);
                    }
                }
            }
        }

        #endregion Send Function's

        //---------------------------------------------------------------------------------------------------
        #region Queueing Event's

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Queueing the event.
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="usEventID"></param>
        /// <param name="nFrom"></param>
        public void QueueingEventTo(in string strName, in ushort usEventID, in int nRetCode, in int nFrom = 0)
        {
            KIntEvent spEvent = new KIntEvent
            {
                EventID = usEventID,
                Sender = strName,
                From = nFrom,
                RetCode = nRetCode,
                Buffer = null
            };

            lock (this.m_csEventQueue)
            {
                this.m_queueEvent.Enqueue(spEvent);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Queueing the event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="strName"></param>
        /// <param name="usEventID"></param>
        /// <param name="data"></param>
        /// <param name="nFrom"></param>
        public void QueueingEventTo<T>(in string strName, in ushort usEventID, in T data, in int nRetCode, in int nFrom = 0)
            where T : struct
        {
            KIntEvent spEvent = new KIntEvent
            {
                EventID = usEventID,
                Sender = strName,
                From = nFrom,
                RetCode =  nRetCode,
                Buffer = data
            };

            lock (this.m_csEventQueue)
            {
                this.m_queueEvent.Enqueue(spEvent);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Queueing event to all the client's.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="usEventID"></param>
        /// <param name="data"></param>
        /// <param name="nFrom"></param>
        public void QueueingToAll<T>(in ushort usEventID, in T data, in int nFrom = 0) 
            where T : class
        {
            lock (m_csClient)
            {
                for (int nIndex = 0; nIndex < m_listClient.Count; nIndex++)
                {
                    m_listClient[nIndex]?.QueueingEvent(usEventID, data, nFrom);
                }
            }
        }

        #endregion
    }
}
