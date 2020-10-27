using System;
using System.Diagnostics;
using log4net;

namespace Logging.Log4Net
{
    public sealed class Log4NetLoggerFactory : ILoggerFactory
    {
        public static readonly Log4NetLoggerFactory Instance = new Log4NetLoggerFactory();

        private Log4NetLoggerFactory()
        {
        }

        public ILogger CreateLogger(string name)
        {
            StackFrame frame = new StackFrame(3, false);
            return new Log4NetLogger(log4net.LogManager.GetLogger(frame.GetMethod().DeclaringType.Assembly, name));
        }
    }
}