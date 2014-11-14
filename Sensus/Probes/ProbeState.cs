namespace Sensus.Probes
{
    /// <summary>
    /// States that a probe can be in.
    /// </summary>
    public enum ProbeState
    {
        /// <summary>
        /// Probe is unsupported on the current device.
        /// </summary>
        Unsupported,

        /// <summary>
        /// Probe is uninitialized.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// Probe is being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Probe is initialized.
        /// </summary>
        Initialized,

        /// <summary>
        /// Probe failed to initialize.
        /// </summary>
        InitializeFailed,

        /// <summary>
        /// Probe is being started.
        /// </summary>
        Starting,

        /// <summary>
        /// Probe has been started.
        /// </summary>
        Started,

        /// <summary>
        /// Probe failed after being started.
        /// </summary>
        Failed,

        /// <summary>
        /// Probe is being stopped.
        /// </summary>
        Stopping,

        /// <summary>
        /// Probe is stopped.
        /// </summary>
        Stopped
    }
}