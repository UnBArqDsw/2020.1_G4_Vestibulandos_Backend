using System;

namespace Logging
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(string name);
    }
}
