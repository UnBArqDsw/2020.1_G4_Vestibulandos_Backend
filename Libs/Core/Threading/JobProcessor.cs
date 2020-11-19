using System;
using System.Diagnostics;
using System.Threading;
using Core.Collections;
using Core.ExceptionHandler;

namespace Core.Threading
{
    public class JobProcessor
    {
        [ThreadStatic]
        private static JobProcessor s_current;

        public static JobProcessor Current => s_current;

        /// <summary>
        /// Event Handler when Job filter some exception.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> ExceptionFilter;

        /// <summary>
        /// Event Handler when Job occur some exception.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> ExceptionOccur;

        /// <summary>
        /// Event Handler when Job is enqueued.
        /// </summary>
        public event EventHandler<EventArgs<IJob>> Enqueued;

        /// <summary>
        /// Event Handler when Job is dequeued.
        /// </summary>
        public event EventHandler<EventArgs<IJob>> Dequeued;

        /// <summary>
        /// Event Handler when Job is done.
        /// </summary>
        public event EventHandler<EventArgs<IJob>> Done;

        /// <summary>
        /// Thread Status.
        /// </summary>
        private enum Status
        {
            Ready,
            Running,
            Closing,
            Closed
        }

        /// <summary>
        /// Queue with the job.
        /// </summary>
        private WriteFreeQueue<IJob> m_queueJob;

        /// <summary>
        /// Current Job.
        /// </summary>
        private IJob m_currentJob;
        public IJob CurrentJob => m_currentJob;

        /// <summary>
        /// Thread Priority.
        /// </summary>
        private ThreadPriority m_enThreadPriority;

        /// <summary>
        /// Status.
        /// </summary>
        private Status m_enStatus;

        /// <summary>
        /// Thread.
        /// </summary>
        private System.Threading.Thread m_thread;

        /// <summary>
        /// Event.
        /// </summary>
        private AutoResetEvent m_enqueueEvent;

        /// <summary>
        /// Count Job.
        /// </summary>
        private int m_nJobCount;

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor.
        /// </summary>
        public JobProcessor()
        {
            m_enqueueEvent = new AutoResetEvent(false);

            m_queueJob = new WriteFreeQueue<IJob>();
            m_nJobCount = 1;
            m_enStatus = Status.Ready;

            m_enThreadPriority = ThreadPriority.Normal;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Start the Job Processor (Thread).
        /// </summary>
        public void Start()
        {
            if (m_enStatus == Status.Ready)
            {
                m_thread = new System.Threading.Thread(Loop)
                {
                    Priority = m_enThreadPriority
                };

                m_enStatus = Status.Running;
                m_thread.Start();

                return;
            }

            if (m_enStatus == Status.Running)
            {
                throw new InvalidOperationException("Already Started");
            }

            throw new InvalidOperationException("Already Closed");
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Stop the Job Processor.
        /// </summary>
        public void Stop()
        {
            Stop(false);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Stop the Job Processor, check if need to finish the remain jobs.
        /// </summary>
        public void Stop(bool bDoRemainJob)
        {
            m_enStatus = (bDoRemainJob) ? Status.Closing : Status.Closed;

            m_enqueueEvent.Set();
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Check if is in thread.
        /// </summary>
        /// <returns></returns>
        public bool IsInThread()
        {
            return System.Threading.Thread.CurrentThread == m_thread;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Enqueue the Job.
        /// </summary>
        /// <param name="job"></param>
        public void Enqueue(IJob job)
        {
            if (m_enStatus != Status.Ready && 
                m_enStatus != Status.Running)
            {
                return;
            }

            m_queueJob.Enqueue(job);

            job.EnqueueTick = Stopwatch.GetTimestamp();

            Enqueued?.Invoke(this, new EventArgs<IJob>(job));

            if (Interlocked.Increment(ref m_nJobCount) == 1)
            {
                m_enqueueEvent.Set();
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Join the thread.
        /// </summary>
        public void Join()
        {
            if (m_enStatus == Status.Ready)
                throw new InvalidOperationException("Loop didn't started.");

            if (IsInThread())
                throw new InvalidOperationException("Can't join on current thread.");

            if (m_enStatus == Status.Closed)
                return;

            m_thread.Join();
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Loop process.
        /// </summary>
        private void Loop()
        {
            s_current = this;

            do
            {
                FilterException.Filter(() =>
                {
                    while (m_enStatus == Status.Running)
                    {
                        if (m_nJobCount == 1)
                        {
                            Native.Thread.SwitchToThread();
                        }

                        if (Interlocked.Decrement(ref m_nJobCount) == 0)
                        {
                            m_enqueueEvent.WaitOne();

                            if (m_enStatus != Status.Running)
                            {
                                break;
                            }
                        }

                        IJob job = m_queueJob.Dequeue();

                        m_currentJob = job;
                        job.StartTick = Stopwatch.GetTimestamp();

                        Dequeued?.Invoke(this, new EventArgs<IJob>(job));

                        job.Do();
                        job.EndTick = Stopwatch.GetTimestamp();

                        m_currentJob = null;

                        Done?.Invoke(this, new EventArgs<IJob>(job));
                    }

                    if (m_enStatus == Status.Closing)
                    {
                        while (!m_queueJob.Empty)
                        {
                            IJob job = m_queueJob.Dequeue();

                            m_currentJob = job;
                            job.StartTick = Stopwatch.GetTimestamp();

                            Dequeued?.Invoke(this, new EventArgs<IJob>(job));

                            job.Do();
                            job.EndTick = Stopwatch.GetTimestamp();

                            m_currentJob = null;

                            Done?.Invoke(this, new EventArgs<IJob>(job));
                        }

                        m_enStatus = Status.Closed;
                    }
                }, exception =>
                {
                    if (ExceptionFilter != null)
                    {
                        try
                        {
                            ExceptionFilter(this, new EventArgs<Exception>(exception));
                        }
                        catch { }
                    }
                }, exception =>
                {
                    if (ExceptionOccur != null)
                    {
                        try
                        {
                            ExceptionOccur(this, new EventArgs<Exception>(exception));
                        }
                        catch { }
                    }
                });
            } while (m_enStatus != Status.Closed);

            m_queueJob.Clear();
            m_enqueueEvent.Reset();
        }
    }
}
