using System;
using Collections.Pooled;
using Core.Event;
using ServerFramework;

namespace Core.Network.Session
{
    public class Performer: ISimObject
    {
        /// <summary>
        /// Unique name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique Index.
        /// </summary>
        public ulong UID { get; set; }

        /// <summary>
        /// Queue events mutex.
        /// </summary>
        private object m_csEventQueue = new object();

        /// <summary>
        /// Queue events.
        /// </summary>
        protected PooledQueue<IKEvent> m_queEvent = new PooledQueue<IKEvent>();

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        protected Performer()
        {
            // Generate an unique name.
            Name = $"{Guid.NewGuid()}"; //$"SIM_{DateTime.Now.ToString($"yyyy-MM-dd_HH:mm:ss")}_{Guid.NewGuid()}";
        }

        //---------------------------------------------------------------------------------------------------
        public virtual object CreateKEvent()
        {
            SFLogUtil.Error(base.GetType(), "Virtual Function Call... Check Please....");
            return null;
        }

        //---------------------------------------------------------------------------------------------------
        protected virtual void ProcessEvent(IKEvent spEvent_)
        {
            SFLogUtil.Error(base.GetType(), "Virtual Function Call... Check Please....");
        }

        //---------------------------------------------------------------------------------------------------
        protected bool GetEvent(out IKEvent spEvent_)
        {
            spEvent_ = null;

            lock (m_csEventQueue)
            {
                if (m_queEvent.Count > 0)
                {
                    spEvent_ = m_queEvent.Dequeue();
                    return true;
                }
            }

            return false;
        }

        //---------------------------------------------------------------------------------------------------
        public virtual void Tick()
        {
            // Consume event queue
            lock (m_csEventQueue)
            {
                while (m_queEvent.Count > 0)
                {
                    IKEvent spEvent = m_queEvent.Dequeue();
                    ProcessEvent(spEvent);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void QueueingEvent<T>(ushort usEventId, T data, int nFrom = (int)KEvent.FROM_TYPE.FT_NONE) 
            where T : class /*IKEvent*/
        {
            KEvent spEvent = new KEvent
            {
                EventID = usEventId, 
                Buffer = data
            };

            QueueingEvent(spEvent, nFrom);
        }

        //---------------------------------------------------------------------------------------------------
        public void QueueingEvent(KEvent spEvent_, int nFrom_)
        {
            spEvent_.From = nFrom_;
            QueueingSPEvent(spEvent_);
        }

        //---------------------------------------------------------------------------------------------------
        public void QueueingEvent(object objData, int nFrom)
        {
            KEvent spEvent = new KEvent
            {
                Buffer = objData
            };

            QueueingSPEvent(spEvent);
        }

        //---------------------------------------------------------------------------------------------------
        public void QueueingSPEvent(KEvent spEvent_)
        {
            lock (m_csEventQueue)
            {
                m_queEvent.Enqueue(spEvent_);
            }
        }

        //---------------------------------------------------------------------------------------------------
        public void QueueingIntEvent(KIntEvent spEvent_)
        {
            KEvent spEvent = new KEvent()
            {
                Buffer = spEvent_.Buffer,
                From = spEvent_.From,
                EventID = spEvent_.EventID,
                RetCode = spEvent_.RetCode
            };

            QueueingSPEvent(spEvent);
        }

        //---------------------------------------------------------------------------------------------------
        public int GetQueueSize()
        {
            lock (m_csEventQueue)
            {
                return m_queEvent.Count;
            }
        }
    }
}
