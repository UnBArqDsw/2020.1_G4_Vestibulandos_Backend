using System;

namespace Core.Threading
{
    public static class Job
    {
        //---------------------------------------------------------------------------------------------------
        public static IJob Create(Action func)
        {
            return new Caller(func);
        }

        //---------------------------------------------------------------------------------------------------
        public static IJob Create<T1>(Action<T1> func, T1 arg1)
        {
            return new Caller<T1>(func, arg1);
        }

        //---------------------------------------------------------------------------------------------------
        public static IJob Create<T1, T2>(Action<T1, T2> func, T1 arg1, T2 arg2)
        {
            return new Caller<T1, T2>(func, arg1, arg2);
        }

        //---------------------------------------------------------------------------------------------------
        public static IJob Create<T1, T2, T3>(Action<T1, T2, T3> func, T1 arg1, T2 arg2, T3 arg3)
        {
            return new Caller<T1, T2, T3>(func, arg1, arg2, arg3);
        }

        //---------------------------------------------------------------------------------------------------
        public static IJob Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new Caller<T1, T2, T3, T4>(func, arg1, arg2, arg3, arg4);
        }

        //---------------------------------------------------------------------------------------------------
        public static IJob Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return new Caller<T1, T2, T3, T4, T5>(func, arg1, arg2, arg3, arg4, arg5);
        }

        //---------------------------------------------------------------------------------------------------
        private abstract class BaseJob : IJob
        {
            public long EnqueueTick { get; set; }
            public long StartTick { get; set; }
            public long EndTick { get; set; }

            //---------------------------------------------------------------------------------------------------
            public abstract void Do();
        }

        private class Caller : BaseJob
        {
            private Action func;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action func)
            {
                this.func = func;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func();
            }
        }

        //---------------------------------------------------------------------------------------------------
        private class Caller<T1> : BaseJob
        {
            private Action<T1> func;

            private T1 arg1;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action<T1> func, T1 arg1)
            {
                this.func = func;
                this.arg1 = arg1;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func(arg1);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private class Caller<T1, T2> : BaseJob
        {
            private Action<T1, T2> func;

            private T1 arg1;
            private T2 arg2;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action<T1, T2> func, T1 arg1, T2 arg2)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func(arg1, arg2);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private class Caller<T1, T2, T3> : BaseJob
        {
            private Action<T1, T2, T3> func;

            private T1 arg1;
            private T2 arg2;
            private T3 arg3;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action<T1, T2, T3> func, T1 arg1, T2 arg2, T3 arg3)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.arg3 = arg3;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func(arg1, arg2, arg3);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private class Caller<T1, T2, T3, T4> : BaseJob
        {
            private Action<T1, T2, T3, T4> func;

            private T1 arg1;
            private T2 arg2;
            private T3 arg3;
            private T4 arg4;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action<T1, T2, T3, T4> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.arg3 = arg3;
                this.arg4 = arg4;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func(arg1, arg2, arg3, arg4);
            }
        }

        //---------------------------------------------------------------------------------------------------
        private class Caller<T1, T2, T3, T4, T5> : BaseJob
        {
            private Action<T1, T2, T3, T4, T5> func;

            private T1 arg1;
            private T2 arg2;
            private T3 arg3;
            private T4 arg4;
            private T5 arg5;

            //---------------------------------------------------------------------------------------------------
            public Caller(Action<T1, T2, T3, T4, T5> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.arg3 = arg3;
                this.arg4 = arg4;
                this.arg5 = arg5;
            }

            //---------------------------------------------------------------------------------------------------
            public override void Do()
            {
                func(arg1, arg2, arg3, arg4, arg5);
            }
        }
    }
}
