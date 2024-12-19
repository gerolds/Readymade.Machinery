namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Interface that all decorators share.
    /// </summary>
    public interface IDecorator : ITask
    {
        /// <summary>The child of this decorator.</summary>
        public ITask Child { get; }

        /// <summary>
        /// Notification that a child of this composite was aborted.
        /// </summary>
        /// <param name="policy">The abort policy to implement.</param>
        public void AbortNotifyFromChild(AbortPolicy policy);
    }
}