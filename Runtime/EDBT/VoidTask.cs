using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A sentinel task that throws exceptions when used.
    /// </summary>
    public class VoidTask : ITask
    {
        /// <inheritdoc />#
        /// <exception cref="T:System.NotImplementedException"></exception>
        TaskState ITask.Tick() => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public string Name => "VOID";

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public TaskState TaskState => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public ITask Parent
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public bool IsActive => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public bool IsTerminated => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public bool IsComplete => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public bool IsSuspended => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public bool IsAborted => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public BehaviourTree Owner => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.SetOwner(BehaviourTree tree) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.SetParent(ITask parent) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        public void Abort(AbortPolicy policy = AbortPolicy.Self) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.Reset() => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.NotifyObserver() => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.SetObserver(Action<TaskState> observer) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.Stop(TaskState state) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.Suspend() => throw new NotImplementedException();

        /// <inheritdoc />
        /// <exception cref="T:System.NotImplementedException"></exception>
        void ITask.Resume() => throw new NotImplementedException();

        /// <exception cref="NotImplementedException"></exception>
        public void StopWithoutNotify(TaskState result) => throw new NotImplementedException();
    }
}