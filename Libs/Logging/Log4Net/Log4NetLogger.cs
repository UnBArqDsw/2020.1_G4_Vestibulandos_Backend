using System;
using log4net;

namespace Logging.Log4Net
{
    /// <summary>
    /// An <see cref="T:Core.Logging.ILogger" /> that wraps log4net <see cref="T:log4net.ILog" />.
    /// </summary>
    public sealed class Log4NetLogger : ILogger
    {
        // ==================================================================================================
        // VARIABLE's
        // ==================================================================================================

        /// <summary>
        /// The log4net logger.
        /// </summary>
        private readonly ILog m_log4Net = null;

        // ==================================================================================================
        // PROPERTY'ies
        // ==================================================================================================

        /// <summary>
        /// Gets a value indicating whether IsDebugEnabled.
        /// </summary>
        public bool IsDebugEnabled => this.m_log4Net.IsDebugEnabled;

        /// <summary>
        /// Gets a value indicating whether IsErrorEnabled.
        /// </summary>
        public bool IsErrorEnabled => this.m_log4Net.IsErrorEnabled;

        /// <summary>
        /// Gets a value indicating whether IsFatalEnabled.
        /// </summary>
        public bool IsFatalEnabled => this.m_log4Net.IsFatalEnabled;

        /// <summary>
        /// Gets a value indicating whether IsInfoEnabled.
        /// </summary>
        public bool IsInfoEnabled => this.m_log4Net.IsInfoEnabled;

        /// <summary>
        /// Gets a value indicating whether IsWarnEnabled.
        /// </summary>
        public bool IsWarnEnabled => this.m_log4Net.IsWarnEnabled;

        /// <summary>
        /// Gets Name.
        /// </summary>
        public string Name => this.m_log4Net.Logger.Name;

        // ==================================================================================================
        // FUNCTION's
        // ==================================================================================================

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Core.Logging.Log4Net.Log4NetLogger" /> class.
        /// </summary>
        /// <param name="logger">
        /// The logger.
        /// </param>
        public Log4NetLogger(ILog logger)
        {
            this.m_log4Net = logger;
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Debug(object message)
        {
            this.m_log4Net.Debug(message);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Debug(object message, Exception exception)
        {
            this.m_log4Net.Debug(message, exception);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void DebugFormat(string format, params object[] args)
        {
            this.m_log4Net.DebugFormat(format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.DebugFormat(provider, format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Error(object message)
        {
            this.m_log4Net.Error(message);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Error(object message, Exception exception)
        {
            this.m_log4Net.Error(message, exception);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void ErrorFormat(string format, params object[] args)
        {
            this.m_log4Net.ErrorFormat(format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.ErrorFormat(provider, format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Fatal(object message)
        {
            this.m_log4Net.Fatal(message);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Fatal(object message, Exception exception)
        {
            this.m_log4Net.Fatal(message, exception);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void FatalFormat(string format, params object[] args)
        {
            this.m_log4Net.FatalFormat(format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.FatalFormat(provider, format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Info(object message)
        {
            this.m_log4Net.Info(message);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Info(object message, Exception exception)
        {
            this.m_log4Net.Info(message, exception);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void InfoFormat(string format, params object[] args)
        {
            this.m_log4Net.InfoFormat(format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.InfoFormat(provider, format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Warn(object message)
        {
            this.m_log4Net.Warn(message);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void Warn(object message, Exception exception)
        {
            this.m_log4Net.Warn(message, exception);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void WarnFormat(string format, params object[] args)
        {
            this.m_log4Net.WarnFormat(format, args);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="provider">
        /// The provider.
        /// </param>
        /// <param name="format">
        /// The format.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.m_log4Net.WarnFormat(provider, format, args);
        }
    }
}
