using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Threading
{
    public static class Scheduler
    {
        public static event EventHandler<EventArgs<Exception>> OnExceptionOccur;

        private static Thread m_thread;

        private static SortedDictionary<DateTime, JobPair> m_dictSchedule;
        private static ManualResetEvent m_eventEnqueue;

        private static bool m_bDomainUnloading = false;

        //---------------------------------------------------------------------------------------------------
        private struct JobPair
        {
            public JobProcessor JobProcessor;
            public IJob Job;

            public JobPair(JobProcessor jobProcessor, IJob job)
            {
                JobProcessor = jobProcessor;
                Job = job;
            }
        }

        //---------------------------------------------------------------------------------------------------
        static Scheduler()
        {
            m_eventEnqueue = new ManualResetEvent(false);
            m_dictSchedule = new SortedDictionary<DateTime, JobPair>();

            m_thread = new Thread(Loop)
            {
                IsBackground = true
            };

            m_thread.Start();

            AppDomain.CurrentDomain.DomainUnload += Abort;
            AppDomain.CurrentDomain.ProcessExit += Abort;
        }

        //---------------------------------------------------------------------------------------------------
        private static void Abort(object sender, EventArgs e)
        {
            m_bDomainUnloading = true;
            m_eventEnqueue.Set();
        }

        //---------------------------------------------------------------------------------------------------
        public static long Schedule(JobProcessor loop, IJob job, int milliSecond)
        {
            return Schedule(loop, job, DateTime.UtcNow.AddTicks(milliSecond * 10000));
        }

        //---------------------------------------------------------------------------------------------------
        public static long Schedule(JobProcessor loop, IJob job, TimeSpan timeSpan)
        {
            return Schedule(loop, job, DateTime.UtcNow + timeSpan);
        }

        //---------------------------------------------------------------------------------------------------
        private static long Schedule(JobProcessor loop, IJob job, DateTime time)
        {
            lock (m_dictSchedule)
            {
                while (m_dictSchedule.ContainsKey(time))
                {
                    time = time.AddTicks(1);
                }

                m_dictSchedule.Add(time, new JobPair(loop, job));
            }

            m_eventEnqueue.Set();

            return time.Ticks;
        }

        //---------------------------------------------------------------------------------------------------
        public static bool Cancel(long scheduleID)
        {
            lock (m_dictSchedule)
            {
                DateTime time = new DateTime(scheduleID);
                if (m_dictSchedule.ContainsKey(time))
                {
                    m_dictSchedule.Remove(time);
                    return true;
                }
            }

            return false;
        }

        //---------------------------------------------------------------------------------------------------
        private static void Loop()
        {
            int nDelay = 0;

            while (!m_bDomainUnloading)
            {
                m_eventEnqueue.WaitOne(nDelay, false);

                lock (m_dictSchedule)
                {
                    nDelay = GetNextDelay(out JobPair job);

                    if (nDelay == 0)
                    {
                        RemoveFirstScheduled();

                        nDelay = GetNextDelay();

                        if (job.JobProcessor == null)
                        {
                            try
                            {
                                job.Job.Do();
                            }
                            catch (Exception value)
                            {
                                try
                                {
                                    OnExceptionOccur?.Invoke(null, new EventArgs<Exception>(value));
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            job.JobProcessor.Enqueue(job.Job);
                        }

                        job.Job = null;
                    }

                    m_eventEnqueue.Reset();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------
        private static int GetNextDelay()
        {
            return GetNextDelay(out JobPair jobPair);
        }

        //---------------------------------------------------------------------------------------------------
        private static int GetNextDelay(out JobPair jobPair)
        {
            jobPair = default;

            using (SortedDictionary<DateTime, JobPair>.Enumerator job = m_dictSchedule.GetEnumerator())
            {
                if (job.MoveNext())
                {
                    jobPair = job.Current.Value;

                    long diffTicks = (job.Current.Key.Ticks - DateTime.UtcNow.Ticks) / 10000;

                    if (diffTicks < 0)
                        return 0;

                    if (diffTicks > int.MaxValue)
                        return int.MaxValue;

                    return (int)diffTicks;
                }
            }

            return -1;
        }

        //---------------------------------------------------------------------------------------------------
        private static void RemoveFirstScheduled()
        {
            using (SortedDictionary<DateTime, JobPair>.Enumerator job = m_dictSchedule.GetEnumerator())
            {
                if (job.MoveNext())
                {
                    m_dictSchedule.Remove(job.Current.Key);
                }
            }
        }
    }
}
