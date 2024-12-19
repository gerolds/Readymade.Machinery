using Readymade.Machinery.Shared;

namespace Readymade.Machinery.EDBT
{
    public class BindCancel : DecoratorBase
    {
        private readonly ICancellable[] _toCancel;

        public BindCancel(ITask child, params ICancellable[] toCancel) : base(child)
        {
            _toCancel = toCancel;
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
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
            _toCancel.ForEach(it => it.Cancel());
        }

        protected override void OnAborted()
        {
            base.OnAborted();
            _toCancel.ForEach(it => it.Cancel());
        }
    }
}