using System;

namespace Logging
{
    /// <summary>
    /// The implementor creates <see cref="T:Core.Logging.ILogger" /> instances.
    /// </summary>
    public interface ILoggerFactory
    {
        // ==================================================================================================
        // FUNCTION's
        // ==================================================================================================

        /// <summary>
        /// Creates a logger for a name.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// A new <see cref="T:Core.Logging.ILogger" />.
        /// </returns>
        ILogger CreateLogger(string name);
    }
}
