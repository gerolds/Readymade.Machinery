namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Execution states for tasks.
    /// </summary>
    public enum TaskState
    {
        /// <summary>
        /// The task has not been ticked yet. 
        /// </summary>
        Initial,

        /// <summary>
        /// The task has been ticked at least once but did not complete yet. 
        /// </summary>
        Running,

        /// <summary>
        /// The behaviour tree was suspended while this task was running. 
        /// </summary>
        Suspended,

        /// <summary>
        /// The task has completed successfully. 
        /// </summary>
        Success,

        /// <summary>
        /// The task has completed with a failure. 
        /// </summary>
        Failure,

        /// <summary>
        /// The task was aborted while running before it could complete. 
        /// </summary>
        Aborted
    }
}