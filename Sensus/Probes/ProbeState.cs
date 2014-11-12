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
    /// Probe is currently initializing itself.
    /// </summary>
    Initializing,

    /// <summary>
    /// Probe is initialized but has not started polling.
    /// </summary>
    Initialized,

    /// <summary>
    /// Probe was initialized but its test method failed.
    /// </summary>
    TestFailed,

    /// <summary>
    /// Probe has been started.
    /// </summary>
    Started,

    /// <summary>
    /// Probe is disposing and will soon be stopped.
    /// </summary>
    Stopping,

    /// <summary>
    /// Probe is stopped
    /// </summary>
    Stopped
}