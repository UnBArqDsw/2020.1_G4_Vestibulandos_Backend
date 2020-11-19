//---------------------------------------------------------------------------------------------------
// DEFINE'S CONFIGURATIONS
//---------------------------------------------------------------------------------------------------

#define _USE_DISRUPTOR

using System;
using System.Collections.Generic;
using System.Threading;
using Core.Event;
using ServerFramework;
#if _USE_DISRUPTOR
using Disruptor;
using Disruptor.Dsl;
using System.Linq;
using System.Threading.Tasks;
#endif

namespace Core.Database
{
#if _USE_DISRUPTOR
    public class DatabaseEventHandler : IEventHandler<KIntEvent>
    {
        public event EventHandler<KIntEvent> OnEventHandler;

        public ulong Count { get; private set; } = 0;

        public long? LastSeenSequenceNumber { get; private set; } = null;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventHandler"></param>
        public DatabaseEventHandler(EventHandler<KIntEvent> eventHandler)
        {
            OnEventHandler += eventHandler;
        }

        //---------------------------------------------------------------------------------------------------
        public void OnEvent(KIntEvent data, long lSequence, bool bEndOfBatch)
        {
            this.Count++;
            this.LastSeenSequenceNumber = lSequence;

            OnEventHandler?.Invoke(this, data);
        }
    }
#endif

    namespace Core.Database
    {
        public class DatabaseLayer
        {
            /// <summary>
            /// Running state.
            /// </summary>
            protected bool m_bRunning;

#if _USE_DISRUPTOR
            private readonly int RING_BUFFER_SIZE = (int)Math.Pow(64, 2); // Caution: the size need to be power of the 2!

            private RingBuffer<KIntEvent> m_ringBuffer = null;
            private Disruptor<KIntEvent> m_disruptor = null;
            private DatabaseEventHandler m_dbEventHandler = null;

            //---------------------------------------------------------------------------------------------------
            private class ThreadPerTaskScheduler : TaskScheduler
            {
                protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

                //---------------------------------------------------------------------------------------------------
                protected override void QueueTask(Task task)
                {
                    new Thread(() => TryExecuteTask(task)) { IsBackground = true }.Start();
                }

                //---------------------------------------------------------------------------------------------------
                protected override bool TryExecuteTaskInline(Task task, bool bTaskWasPreviouslyQueued)
                {
                    return TryExecuteTask(task);
                }
            }

#else

        /// <summary>
        /// Own thread of the DatabaseLayer.
        /// </summary>
        private Thread m_thread;

        /// <summary>
        /// Workers.
        /// </summary>
        private Queue<KIntEvent> m_works = new Queue<KIntEvent>();

        /// <summary>
        /// 
        /// </summary>
        private ManualResetEvent m_waitHandle = new ManualResetEvent(false);

#endif

            //---------------------------------------------------------------------------------------------------
#if !_USE_DISRUPTOR
        public int QueueCount
        {
            get
            {
                int count = 0;

                lock (this)
                {
                    count = this.m_works.Count;
                }

                return count;
            }
        }
#endif

            //---------------------------------------------------------------------------------------------------
            public DatabaseLayer()
            {
#if !_USE_DISRUPTOR
            this.m_thread = new Thread(this.Run);
            //m_thread.IsBackground = true;
#endif
            }

            //---------------------------------------------------------------------------------------------------
#if _USE_DISRUPTOR

            private Disruptor<KIntEvent> CreateDisruptor()
            {
                Disruptor<KIntEvent> disruptor =
                    new Disruptor<KIntEvent>(() => new KIntEvent()
                        , RING_BUFFER_SIZE
                        , new ThreadPerTaskScheduler()
                        , ProducerType.Multi
                        , new BlockingWaitStrategy() /*BusySpinWaitStrategy()*/);

                m_dbEventHandler = new DatabaseEventHandler(ProcessEvent);
                disruptor.HandleEventsWith(m_dbEventHandler);

                return disruptor;
            }

#endif

            //---------------------------------------------------------------------------------------------------

#if _USE_DISRUPTOR
            protected bool EnqueueWork(string strSender, ulong ulSenderUid, ushort usEventId, object objBuffer, int nFrom)
#else
        protected bool EnqueueWork(KIntEvent dbEvent)
#endif
            {

#if _USE_DISRUPTOR
                // Grab the next sequence
                m_ringBuffer.TryNext(out long lSequence);

                try
                {
                    // Get the event in the Disruptor for the sequence
                    KIntEvent data = m_ringBuffer[lSequence];

                    // Fill with data
                    data.Sender = strSender;
                    data.SenderUID = ulSenderUid;
                    data.EventID = usEventId;
                    data.Buffer = objBuffer;
                    data.From = nFrom;
                }
                finally
                {
                    // Publish the event
                    m_ringBuffer.Publish(lSequence);
                }

#else
            if (dbEvent == null) throw new ArgumentNullException("dbEvent");

            lock (this)
            {
                if (!this.m_bRunning)
                {
                    return false;
                }

                this.m_works.Enqueue(dbEvent);
                this.m_waitHandle.Set();
            }
#endif

                return true;
            }

            //---------------------------------------------------------------------------------------------------

#if !_USE_DISRUPTOR

        private KIntEvent DequeueWork()
        {
            KIntEvent worker;

            lock (this)
            {
                if (this.m_works.Count == 0)
                {
                    this.m_waitHandle.Reset();
                    worker = null;
                }
                else
                {
                    worker = this.m_works.Dequeue();
                }
            }

            return worker;
        }

        //---------------------------------------------------------------------------------------------------
        private void Run()
        {
            for (; ; )
            {
                KIntEvent event_ = this.DequeueWork();

                if (event_ != null)
                {
                    try
                    {
                        ProcessEvent(event_);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        SFLogUtil.Error(base.GetType(), ex.Message);
                        continue;
                    }
                }

                if (!this.m_bRunning)
                {
                    break;
                }

                this.m_waitHandle.WaitOne();
            }
        }

#endif

            //---------------------------------------------------------------------------------------------------

#if _USE_DISRUPTOR
            protected virtual void ProcessEvent(object objSender, KIntEvent data)

#else
        protected virtual void ProcessEvent(KIntEvent dbEvent)
#endif
            {
                SFLogUtil.Error(base.GetType(), "Pure Virtual Function Call... Check Please....");
            }

            //---------------------------------------------------------------------------------------------------
            public bool Start()
            {
#if _USE_DISRUPTOR
                if (this.m_bRunning)
                {
                    return false;
                }

                this.m_bRunning = true;

                m_disruptor = CreateDisruptor();
                m_ringBuffer = m_disruptor.Start();
#else

            lock (this)
            {
                if (this.m_bRunning) return false;

                this.m_bRunning = true;
                this.m_thread.Start();
            }

#endif

                return true;
            }

            //---------------------------------------------------------------------------------------------------
            public bool Stop(bool bClearQueue)
            {
#if _USE_DISRUPTOR

                if (!this.m_bRunning)
                {
                    return false;
                }

                this.m_bRunning = false;

                //m_dbEventHandler.OnEventHandler -= ProcessEvent;
                //m_disruptor.Halt();
                m_disruptor.Shutdown();

#else
            lock (this)
            {
                if (!this.m_bRunning) return false;

                this.m_bRunning = false;

                if (bClearQueue)
                {
                    this.m_works.Clear();
                }

                this.m_waitHandle.Set();
            }

#endif

                return true;
            }

            //---------------------------------------------------------------------------------------------------
            public void StopAndWaitFinish(bool bClearQueue)
            {
                if (this.Stop(bClearQueue))
                {
                    this.WaitFinish();
                }
            }

            //---------------------------------------------------------------------------------------------------
            public void WaitFinish()
            {
#if _USE_DISRUPTOR
                // nothing...
#else
            this.m_thread.Join();
#endif
            }

            //---------------------------------------------------------------------------------------------------
            public bool QueueingID(ushort usEventId, string strName, ulong ulUid)
            {
                if (!m_bRunning)
                {
                    return false;
                }

#if _USE_DISRUPTOR

                return EnqueueWork((!string.IsNullOrEmpty(strName)) ? strName : "", ulUid, usEventId, null, 0);
#else

            KIntEvent spEvent = new KIntEvent
            {
                EventID = usEventId,
                SenderUID = ulUid,
                Sender = (!string.IsNullOrEmpty(strName)) ? strName : "",
            };

            EnqueueWork(spEvent);
#endif
            }

            //---------------------------------------------------------------------------------------------------
            public bool QueueingEvent<T>(ushort usEventId, string strName, ulong ulUid, T data) 
                /*where T : IPacket*/
            {
                if (!m_bRunning)
                {
                    return false;
                }

#if _USE_DISRUPTOR

                return EnqueueWork((!string.IsNullOrEmpty(strName)) ? strName : "", ulUid, usEventId, data, 0);

#else

            KIntEvent spEvent = new KIntEvent
            {
                EventID = usEventId,
                SenderUID = ulUid,
                Sender = (!string.IsNullOrEmpty(strName)) ? strName : "",
                Buffer = data
            };

            EnqueueWork(spEvent);
#endif
            }
        }
    }
}