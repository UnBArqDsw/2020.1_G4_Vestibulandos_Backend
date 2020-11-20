using System;
using System.Diagnostics;
using System.Threading;

namespace Logging
{
    /// <summary>
    ///   The log manager provides methods to get instances of <see cref="T:Core.Logging.ILogger" /> using a <see cref="T:Core.Logging.ILoggerFactory" />.
    ///   Any logging framework of choice can be used by assigining a new <see cref="T:Core.Logging.ILoggerFactory" /> with <see cref="M:Logging.LoggerManager.SetLoggerFactory(Core.Logging.ILoggerFactory)" />.
    ///   The default logger factory creates <see cref="T:Core.Logging.ILogger" /> that do not log
    /// </summary>
    public static class LoggerManager
    {
        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   Initializes static members of the <see cref="T:Logging.LoggerManager" /> class.
        ///   Sets the default logger factory.
        /// </summary>
        static LoggerManager()
        {
            SetLoggerFactory(null);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   Gets an <see cref="T:Core.Logging.ILogger" /> for the calling class type.
        /// </summary>
        /// <returns>
        ///   A new <see cref="T:Core.Logging.ILogger" /> for the calling class type.
        /// </returns>
        public static ILogger GetCurrentClassLogger()
        {
            StackFrame frame = new StackFrame(1, false);
            return GetLogger(frame.GetMethod().DeclaringType.FullName);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   Gets an <see cref="T:Core.Logging.ILogger" /> for the specified name.
        /// </summary>
        /// <param name="name">
        ///   The name.
        /// </param>
        /// <returns>
        ///   A new <see cref="T:Core.Logging.ILogger" /> for the specified <paramref name="name" />.
        /// </returns>
        public static ILogger GetLogger(string name)
        {
            return new LazyLoggerWrapper(name);
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   Assigns a new <see cref="T:Core.Logging.ILoggerFactory" /> to create <see cref="T:Core.Logging.ILogger" /> instances.
        /// </summary>
        /// <param name="factory">
        ///   The new factory. Set null to disable logging.
        /// </param>
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

        /// <summary>
        ///   The number of loggers created.
        /// </summary>
        private static int s_nCreateLoggerCount = 0;

        /// <summary>
        ///   The used logger factory.
        /// </summary>
        private static ILoggerFactory s_iLoggerFactory = default;

        /// <summary>
        ///   A logger that does nothing.
        /// </summary>
        private sealed class EmptyLogger : ILogger
        {
            // ==================================================================================================
            // VARIABLE's
            // ==================================================================================================

            /// <summary>
            ///   The name.
            /// </summary>
            private readonly string m_strName = "";

            // ==================================================================================================
            // PROPERTY'ies
            // ==================================================================================================

            /// <summary>
            ///   Gets a value indicating whether IsDebugEnabled.
            /// </summary>
            /// <value>Always false.</value>
            public bool IsDebugEnabled => false;

            /// <summary>
            ///   Gets a value indicating whether IsErrorEnabled.
            /// </summary>
            /// <value>Always false.</value>
            public bool IsErrorEnabled => false;

            /// <summary>
            ///   Gets a value indicating whether IsFatalEnabled.
            /// </summary>
            /// <value>Always false.</value>
            public bool IsFatalEnabled => false;

            /// <summary>
            ///   Gets a value indicating whether IsInfoEnabled.
            /// </summary>
            /// <value>Always false.</value>
            public bool IsInfoEnabled => false;

            /// <summary>
            ///   Gets a value indicating whether IsWarnEnabled.
            /// </summary>
            /// <value>Always false.</value>
            public bool IsWarnEnabled => false;

            /// <summary>
            ///   Gets the logger name.
            /// </summary>
            public string Name => this.m_strName;

            // ==================================================================================================
            // FUNCTION's
            // ==================================================================================================

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Initializes a new instance of the <see cref="T:Logging.LoggerManager.EmptyLogger" /> class.
            /// </summary>
            /// <param name="strName">
            ///   The name.
            /// </param>
            public EmptyLogger(string strName)
            {
                this.m_strName = strName;
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Debug(object message) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Debug(object message, Exception exception) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void DebugFormat(string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void DebugFormat(IFormatProvider provider, string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Error(object message) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Error(object message, Exception exception) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void ErrorFormat(string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void ErrorFormat(IFormatProvider provider, string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Fatal(object message) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Fatal(object message, Exception exception) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void FatalFormat(string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void FatalFormat(IFormatProvider provider, string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Info(object message) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Info(object message, Exception exception) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void InfoFormat(string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void InfoFormat(IFormatProvider provider, string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Warn(object message) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Warn(object message, Exception exception) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void WarnFormat(string format, params object[] args) { }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Does nothing.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void WarnFormat(IFormatProvider provider, string format, params object[] args) { }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   An <see cref="T:Core.Logging.ILoggerFactory" /> that creates <see cref="T:Logging.LoggerManager.EmptyLogger" /> instances.
        ///   Assigning this factory disables logging.
        /// </summary>
        private sealed class EmptyLoggerFactory : ILoggerFactory
        {
            // ==================================================================================================
            // STATIC's
            // ==================================================================================================

            /// <summary>
            ///   The singleton instance.
            /// </summary>
            public static readonly EmptyLoggerFactory Instance = new EmptyLoggerFactory();

            // ==================================================================================================
            // FUNCTION's
            // ==================================================================================================

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Creates a new <see cref="T:Logging.LoggerManager.EmptyLogger" />.
            /// </summary>
            /// <param name="name">
            ///   The name.
            /// </param>
            /// <returns>
            ///   A new <see cref="T:Logging.LoggerManager.EmptyLogger" />
            /// </returns>
            public ILogger CreateLogger(string name)
            {
                return new EmptyLogger(name);
            }
        }

        //---------------------------------------------------------------------------------------------------
        /// <summary>
        ///   A logger wrapper for lazy logger initialization. This fixes a problem where loggers are created before assigning a custom factory.
        /// </summary>
        private sealed class LazyLoggerWrapper : ILogger
        {
            // ==================================================================================================
            // VARIABLE's
            // ==================================================================================================

            /// <summary>
            ///   The logger name.
            /// </summary>
            private readonly string m_strName = "";

            /// <summary>
            ///   A getter funcation for the logger.
            ///   Initially it is mapped to <see cref="M:Logging.LoggerManager.LazyLoggerWrapper.CreateLogger" />.
            /// </summary>
            private Func<ILogger> m_funcLogger = null;

            /// <summary>
            ///   The used logger.
            /// </summary>
            private ILogger m_logger = null;

            // ==================================================================================================
            // PROPERTY'ies
            // ==================================================================================================

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets a value indicating whether IsDebugEnabled.
            /// </summary>
            public bool IsDebugEnabled => this.m_funcLogger().IsDebugEnabled;

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets a value indicating whether IsErrorEnabled.
            /// </summary>
            public bool IsErrorEnabled => this.m_funcLogger().IsErrorEnabled;

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets a value indicating whether IsFatalEnabled.
            /// </summary>
            public bool IsFatalEnabled => this.m_funcLogger().IsFatalEnabled;

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets a value indicating whether IsInfoEnabled.
            /// </summary>
            public bool IsInfoEnabled => this.m_funcLogger().IsInfoEnabled;

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets a value indicating whether IsWarnEnabled.
            /// </summary>
            public bool IsWarnEnabled => this.m_funcLogger().IsWarnEnabled;

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Gets Name.
            /// </summary>
            public string Name => this.m_strName;

            // ==================================================================================================
            // FUNCTION's
            // ==================================================================================================

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Initializes a new instance of the <see cref="T:Logging.LoggerManager.LazyLoggerWrapper" /> class.
            /// </summary>
            /// <param name="name">
            ///   The name.
            /// </param>
            public LazyLoggerWrapper(string name)
            {
                this.m_strName = name;
                this.m_funcLogger = new Func<ILogger>(this.CreateLogger);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Creates a new <see cref="T:Core.Logging.ILogger" /> with the current logger factory.
            ///   It then switches the <see cref="F:Logging.LoggerManager.LazyLoggerWrapper.getLogger" /> function to <see cref="M:Logging.LoggerManager.LazyLoggerWrapper.GetLogger" />.
            /// </summary>
            /// <returns>
            ///   A new <see cref="T:Core.Logging.ILogger" />.
            /// </returns>
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

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Returns the logger that was created with the factory.
            /// </summary>
            /// <returns>
            ///   The logger that was created with the factory.
            /// </returns>
            private ILogger GetLogger()
            {
                return this.m_logger;
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Debug(object message)
            {
                this.m_funcLogger().Debug(message);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Debug(object message, Exception exception)
            {
                this.m_funcLogger().Debug(message, exception);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void DebugFormat(string format, params object[] args)
            {
                this.m_funcLogger().DebugFormat(format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void DebugFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().DebugFormat(provider, format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Error(object message)
            {
                this.m_funcLogger().Error(message);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Error(object message, Exception exception)
            {
                this.m_funcLogger().Error(message, exception);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void ErrorFormat(string format, params object[] args)
            {
                this.m_funcLogger().ErrorFormat(format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().ErrorFormat(provider, format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Fatal(object message)
            {
                this.m_funcLogger().Fatal(message);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Fatal(object message, Exception exception)
            {
                this.m_funcLogger().Fatal(message, exception);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void FatalFormat(string format, params object[] args)
            {
                this.m_funcLogger().FatalFormat(format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void FatalFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().FatalFormat(provider, format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Info(object message)
            {
                this.m_funcLogger().Info(message);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Info(object message, Exception exception)
            {
                this.m_funcLogger().Info(message, exception);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void InfoFormat(string format, params object[] args)
            {
                this.m_funcLogger().InfoFormat(format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void InfoFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().InfoFormat(provider, format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            public void Warn(object message)
            {
                this.m_funcLogger().Warn(message);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="message">
            ///   The message.
            /// </param>
            /// <param name="exception">
            ///   The exception.
            /// </param>
            public void Warn(object message, Exception exception)
            {
                this.m_funcLogger().Warn(message, exception);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void WarnFormat(string format, params object[] args)
            {
                this.m_funcLogger().WarnFormat(format, args);
            }

            //---------------------------------------------------------------------------------------------------
            /// <summary>
            ///   Log a message.
            /// </summary>
            /// <param name="provider">
            ///   The provider.
            /// </param>
            /// <param name="format">
            ///   The format.
            /// </param>
            /// <param name="args">
            ///   The args.
            /// </param>
            public void WarnFormat(IFormatProvider provider, string format, params object[] args)
            {
                this.m_funcLogger().WarnFormat(provider, format, args);
            }
        }
    }
}
