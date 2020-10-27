using System;
using System.Diagnostics;
using System.Threading;

namespace Logging
{
    public static class LoggerManager
    {
        static LoggerManager()
        {
            SetLoggerFactory(null);
        }

        public static ILogger GetCurrentClassLogger()
        {
            StackFrame frame = new StackFrame(1, false);
            return GetLogger(frame.GetMethod().DeclaringType.FullName);
        }

        public static ILogger GetLogger(string name)
        {
            return new LazyLoggerWrapper(name);
        }

        public static void SetLoggerFactory(ILoggerFactory factory)
        {
            s_iLoggerFactory = (factory ?? EmptyLoggerFactory.Instance);

            int nCount = Interlocked.Exchange(ref s_nCreateLoggerCount, 0);
            if (nCount != 0)
            {
                GetCurrentClassLogger().WarnFormat((nCount == 1) ?
                    "LoggerManager.SetLoggerFactory: 1 ILogger instance created with previous factory!" :
                    "LoggerManager.SetLoggerFactory: {0} ILogger instances created with previous factory!", nCount);
            }
        }

        private static int s_nCreateLoggerCount = 0;

        private static ILoggerFactory s_iLoggerFactory = default;

        private sealed class EmptyLogger : ILogger
        {
            private readonly string m_strName = "";

            public bool IsDebugEnabled => false;

            public bool IsErrorEnabled => false;

            public bool IsFatalEnabled => false;

            public bool IsInfoEnabled => false;

            public bool IsWarnEnabled => false;

            public string Name => this.m_strName;

            public EmptyLogger(string strName)
            {
                this.m_strName = strName;
            }

            public void Debug(object message) { }

            public void Debug(object message, Exception exception) { }

            public void DebugFormat(string format, params object[] args) { }

            public void DebugFormat(IFormatProvider provider, string format, params object[] args) { }
            public void Error(object message) { }

            public void Error(object message, Exception exception) { }

            public void ErrorFormat(string format, params object[] args) { }

            public void ErrorFormat(IFormatProvider provider, string format, params object[] args) { }

            public void Fatal(object message) { }

            public void Fatal(object message, Exception exception) { }

            public void FatalFormat(string format, params object[] args) { }

            public void FatalFormat(IFormatProvider provider, string format, params object[] args) { }

            public void Info(object message) { }

            public void Info(object message, Exception exception) { }

            public void InfoFormat(string format, params object[] args) { }

            public void InfoFormat(IFormatProvider provider, string format, params object[] args) { }

            public void Warn(object message) { }

            public void Warn(object message, Exception exception) { }

            public void WarnFormat(string format, params object[] args) { }

            public void WarnFormat(IFormatProvider provider, string format, params object[] args) { }
        }

        private sealed class EmptyLoggerFactory : ILoggerFactory
        {
            public static readonly EmptyLoggerFactory Instance = new EmptyLoggerFactory();

            public ILogger CreateLogger(string name)
            {
                return new EmptyLogger(name);
            }
        }

        private sealed class LazyLoggerWrapper : ILogger
        {
            private readonly string m_strName = "";

            private Func<ILogger> m_funcLogger = null;

            private ILogger m_logger = null;

            public bool IsDebugEnabled => this.m_funcLogger().IsDebugEnabled;
            public bool IsErrorEnabled => this.m_funcLogger().IsErrorEnabled;

            public bool IsFatalEnabled => this.m_funcLogger().IsFatalEnabled;

            public bool IsInfoEnabled => this.m_funcLogger().IsInfoEnabled;

            public bool IsWarnEnabled => this.m_funcLogger().IsWarnEnabled;

            public string Name => this.m_strName;

            public LazyLoggerWrapper(string name)
            {
                this.m_strName = name;
                this.m_funcLogger = new Func<ILogger>(this.CreateLogger);
            }

            private ILogger CreateLogger()
            {
                lock (this)
                {
                    if (this.m_logger == null)
                    {
                        Interlocked.Increment(ref s_nCreateLoggerCount);
                        this.m_logger = s_iLoggerFactory.CreateLogger(this.m_strName);
                        Interlocked.Exchange(ref this.m_funcLogger, new Func<ILogger>(this.GetLogger));
                    }
                }

                return this.m_logger;
            }

            private ILogger GetLogger()
            {
                return this.m_logger;
            }

            public void Debug(object message)
            {
                this.m_funcLogger().Debug(message);
            }

            public void Debug(object message, Exception exception)
            {
                this.m_funcLogger().Debug(message, exception);
            }

            public void DebugFormat(string format, params object[] args)
            {
                this.m_funcLogger().DebugFormat(format, args);
            }

            public void DebugFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().DebugFormat(provider, format, args);
            }

            public void Error(object message)
            {
                this.m_funcLogger().Error(message);
            }

            public void Error(object message, Exception exception)
            {
                this.m_funcLogger().Error(message, exception);
            }

            public void ErrorFormat(string format, params object[] args)
            {
                this.m_funcLogger().ErrorFormat(format, args);
            }

            public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().ErrorFormat(provider, format, args);
            }

            public void Fatal(object message)
            {
                this.m_funcLogger().Fatal(message);
            }

            public void Fatal(object message, Exception exception)
            {
                this.m_funcLogger().Fatal(message, exception);
            }

            public void FatalFormat(string format, params object[] args)
            {
                this.m_funcLogger().FatalFormat(format, args);
            }

            public void FatalFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().FatalFormat(provider, format, args);
            }

            public void Info(object message)
            {
                this.m_funcLogger().Info(message);
            }

            public void Info(object message, Exception exception)
            {
                this.m_funcLogger().Info(message, exception);
            }

            public void InfoFormat(string format, params object[] args)
            {
                this.m_funcLogger().InfoFormat(format, args);
            }

            public void InfoFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().InfoFormat(provider, format, args);
            }

            public void Warn(object message)
            {
                this.m_funcLogger().Warn(message);
            }

            public void Warn(object message, Exception exception)
            {
                this.m_funcLogger().Warn(message, exception);
            }

            public void WarnFormat(string format, params object[] args)
            {
                this.m_funcLogger().WarnFormat(format, args);
            }

            public void WarnFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().WarnFormat(provider, format, args);
            }
        }
    }
}
