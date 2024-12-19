namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Abort propagation policy.
    /// </summary>
    public enum AbortPolicy
    {
        /// <summary>
        /// Don't abort anything.
        /// </summary>
        None,

        /// <summary>
        /// Abort the current task and any subtree
        /// </summary>
        Self,

        /// <summary>
        /// Abort any subtrees of the parent that are executed after the current task's subtree
        /// </summary>
        LowerPriority,

        /// <summary>
        /// Combines <see cref="Self"/> and <see cref="LowerPriority"/>  
        /// </summary>
        Both
    }
}