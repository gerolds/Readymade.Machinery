using System.Collections.Generic;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Interface that all composites share.
    /// </summary>
    public interface IComposite : ITask
    {
        /// <summary>
        /// Returns an enumerator for the children of this <see cref="IComposite"/>.
        /// </summary>
        public IEnumerable<ITask> Children { get; }

        /// <summary>
        /// The number of direct children of this <see cref="IComposite"/>.
        /// </summary>
        public int ChildCount { get; }

        /// <summary>
        /// Add a child to this composite. The child will be inserted after existing children. Changing the order of children is not supported.
        /// </summary>
        /// <param name="child">The child <see cref="ITask"/> to add.</param>
        public void AddChild(ITask child);

        /// <summary>
        /// Notification that a child of this composite was aborted.
        /// </summary>
        /// <param name="policy">The abort policy to implement.</param>
        public void AbortNotifyFromChild(AbortPolicy policy);
    }
}