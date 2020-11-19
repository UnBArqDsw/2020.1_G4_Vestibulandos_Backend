using Disruptor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Util
{
    public class AppTimer
    {
        bool mTimerStopped;

        int mLastElapsedTime;
        int mBaseTime;
        int mStopTime;

        public AppTimer()
        {
            mTimerStopped= true;

            mLastElapsedTime = 0;
            mBaseTime = 0;
            mStopTime = 0;
        }

        public void Start()
        {
            int nTime = Environment.TickCount;

            if(mTimerStopped)
            {
                mBaseTime += nTime - mStopTime;
            }

            mStopTime = 0;
            mLastElapsedTime = nTime;
            mTimerStopped = false;
        }

        public void Stop()
        {
            int nTime = 0;

            if(mStopTime != 0)
            {
                nTime = mStopTime;
            }
            else
            {
                nTime = Environment.TickCount;
            }

            if (!mTimerStopped)
            {
                mStopTime = nTime;
                mLastElapsedTime = nTime;
                mTimerStopped = true;
            }
        }

        public void Reset()
        {
            int nTime = 0;

            if (mStopTime != 0)
            {
                nTime = mStopTime;
            }
            else
            {
                nTime = Environment.TickCount;
            }

            if (mTimerStopped)
            {
                mBaseTime += nTime - mStopTime;
            }

            mBaseTime = nTime;
            mLastElapsedTime = nTime;
            mStopTime = 0;
            mTimerStopped = false;
        }

        public void Advance()
        {
            int nTime = 0;

            if (mStopTime != 0)
            {
                nTime = mStopTime;
            }
            else
            {
                nTime = Environment.TickCount;
            }

            mStopTime += 100; /// += 0.1f
        }

        public int GetDeltaTime()
        {
            long deltaTime = 0;

            if(mStopTime != 0)
            {
                if (mStopTime < mLastElapsedTime)
                    deltaTime = mStopTime - mLastElapsedTime + 4294967295; // ( 2^32 - 1 )
                else
                    deltaTime = mStopTime - mLastElapsedTime;

                mLastElapsedTime = mStopTime;
            }
            else
            {
                int time = Environment.TickCount;
                if (time < mLastElapsedTime)
                    deltaTime = time - mLastElapsedTime + 4294967295; //( 2^32 - 1 )
                else
                    deltaTime = time - mLastElapsedTime;

                mLastElapsedTime = time;
            }

            return (int)deltaTime;
        }
    }
}
