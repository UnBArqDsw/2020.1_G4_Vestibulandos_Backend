using System;
using System.Diagnostics;
using log4net;

namespace Logging.Log4Net
{
    /// <summary>
    /// This <see cref="T:Core.Logging.ILoggerFactory" /> creates <see cref="T:Core.Logging.ILogger" /> that log to the log4net framework.
    /// </summary>
    public sealed class Log4NetLoggerFactory : ILoggerFactory
    {
        // ==================================================================================================
        // STATIC's
        // ==================================================================================================

        /// <summary>
        /// The singleton.
        /// </summary>
        public static readonly Log4NetLoggerFactory Instance = new Log4NetLoggerFactory();

        // ==================================================================================================
        // FUNCTION's
        // ==================================================================================================

        /// <summary>
        /// Prevents a default instance of the <see cref="T:Core.Logging.Log4Net.Log4NetLoggerFactory" /> class from being created.
        /// </summary>
        private Log4NetLoggerFactory()
        {
        }

        /// <summary>
        /// Creates a new <see cref="T:Core.Logging.Log4Net.Log4NetLogger" /> with the specicified name.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A new <see cref="T:Core.Logging.Log4Net.Log4NetLogger" />.
        /// </returns>
        public ILogger CreateLogger(string name)
        {
            StackFrame frame = new StackFrame(3, false);
            return new Log4NetLogger(log4net.LogManager.GetLogger(frame.GetMethod().DeclaringType.Assembly, name));
        }
    }
}