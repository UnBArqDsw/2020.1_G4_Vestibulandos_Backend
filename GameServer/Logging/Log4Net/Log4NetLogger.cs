using System;
using log4net;

namespace Logging.Log4Net
{
    public sealed class Log4NetLogger : ILogger
    {
        private readonly ILog m_log4Net = null;

        public bool IsDebugEnabled => this.m_log4Net.IsDebugEnabled;

        public bool IsErrorEnabled => this.m_log4Net.IsErrorEnabled;

        public bool IsFatalEnabled => this.m_log4Net.IsFatalEnabled;

        public bool IsInfoEnabled => this.m_log4Net.IsInfoEnabled;

        public bool IsWarnEnabled => this.m_log4Net.IsWarnEnabled;

        public string Name => this.m_log4Net.Logger.Name;

        public Log4NetLogger(ILog logger)
        {
            this.m_log4Net = logger;
        }

        public void Debug(object message)
        {
            this.m_log4Net.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            this.m_log4Net.Debug(message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            this.m_log4Net.DebugFormat(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.DebugFormat(provider, format, args);
        }

        public void Error(object message)
        {
            this.m_log4Net.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            this.m_log4Net.Error(message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            this.m_log4Net.ErrorFormat(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.ErrorFormat(provider, format, args);
        }

        public void Fatal(object message)
        {
            this.m_log4Net.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            this.m_log4Net.Fatal(message, exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            this.m_log4Net.FatalFormat(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.FatalFormat(provider, format, args);
        }

        public void Info(object message)
        {
            this.m_log4Net.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            this.m_log4Net.Info(message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            this.m_log4Net.InfoFormat(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.InfoFormat(provider, format, args);
        }

        public void Warn(object message)
        {
            this.m_log4Net.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            this.m_log4Net.Warn(message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            this.m_log4Net.WarnFormat(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.WarnFormat(provider, format, args);
        }
    }
}
