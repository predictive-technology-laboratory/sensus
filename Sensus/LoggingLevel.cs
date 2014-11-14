namespace Sensus
{
    /// <summary>
    /// Logging levels.
    /// </summary>
    public enum LoggingLevel
    {
        /// <summary>
        /// No logging.
        /// </summary>
        Off,

        /// <summary>
        /// Normal logging:  messages that get generated on startup and shutdown, plus exceptions.
        /// </summary>
        Normal,

        /// <summary>
        /// Verbose logging:  Normal plus additional, frequent messages.
        /// </summary>
        Verbose,

        /// <summary>
        /// Debug logging:  Verbose plus all possible messages.
        /// </summary>
        Debug
    }
}