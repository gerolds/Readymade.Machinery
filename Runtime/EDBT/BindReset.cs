using Readymade.Machinery.Shared;

namespace Readymade.Machinery.EDBT
{
    public class BindReset : DecoratorBase
    {
        private readonly IResettable[] _toReset;

        public BindReset(ITask child, params IResettable[] toReset) : base(child)
        {
            _toReset = toReset;
        }

        /// <inheritdoc />
        protected override void OnResumed()
        {
        }

        /// <inheritdoc />
        protected override void OnSuspended()
        {
        }

        /// <inheritdoc />
        protected override TaskState OnTick() => TaskState.Running;

        /// <inheritdoc />
        protected override void OnStarted()
        {
            Owner.Start(Child, OnChildComplete);
        }

        /// Handler to be called when the child terminates which then facilitates the continuation (event pattern).
        private void OnChildComplete(TaskState taskState)
        {
            ((ITask) this).Stop(Child.TaskState);
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
            _toReset.ForEach(it => it.Reset());
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
        }
    }
}