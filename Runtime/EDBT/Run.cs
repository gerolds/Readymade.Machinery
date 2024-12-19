using System.Collections;

namespace Readymade.Machinery.EDBT
{
    /// <inheritdoc />
    /// <summary>
    /// <see cref="T:Readymade.Machinery.EDBT.ITask" /> that iterates an <see cref="T:System.Collections.IEnumerator" /> to the end. Succeeds when the <see cref="T:System.Collections.IEnumerator" /> is completed.
    /// </summary>
    public class Run : TaskBase
    {
        /// <summary>
        /// Storage for the assigned delegate.
        /// </summary>
        private readonly IEnumerator _iterator;

        /// <inheritdoc />
        /// <summary>
        /// Create an instance of a <see cref="T:Readymade.Machinery.EDBT.Do" /> task.
        /// </summary>
        /// <param name="name">A descriptive name for this task.</param>
        /// <param name="iterator">The action to invoke when this task is ticked.</param>
        public Run(string name, IEnumerator iterator) : base(name)
        {
            _iterator = iterator;
        }

        /// <inheritdoc/>
        public Run(IEnumerator iterator) : this(default, iterator)
        {
        }

        /// <inheritdoc/>
        protected override void OnAborted()
        {
        }

        /// <inheritdoc/>
        protected override void OnResumed()
        {
        }

        /// <inheritdoc/>
        protected override void OnSuspended()
        {
        }

        /// <inheritdoc/>
        protected override TaskState OnTick()
        {
            return _iterator.MoveNext()
                ? TaskState.Running
                : TaskState.Success;
        }

        /// <inheritdoc/>
        protected override void OnStarted()
        {
        }

        /// <inheritdoc/>
        protected override void OnReset()
        {
            _iterator.Reset();
        }

        /// <inheritdoc/>
        protected override void OnStopped(TaskState state)
        {
        }
    }
}