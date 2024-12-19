using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Interface that all tasks share.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Ticks the current task and calls event handlers based on any state transition that may have happened.
        /// </summary>
        internal TaskState Tick();

        /// <summary>
        /// A descriptive name for this task.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The current <see cref="TaskState"/> of the task.
        /// </summary>
        public TaskState TaskState { get; }

        /// <summary>
        /// The parent of this task
        /// </summary>
        public ITask Parent { get; }

        /// <summary>
        /// The tree that executes this task
        /// </summary>
        public BehaviourTree Owner { get; }

        /// <summary>
        /// Whether this node is queued to execute
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Whether this node has finished updating for whatever reason (completed or aborted). 
        /// </summary>
        public bool IsTerminated { get; }

        /// <summary>
        /// Whether this node has decided on its success/failure state. This is not a complement to <see cref="IsActive"/> which also considers <see cref="TaskState.Aborted"/>.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Whether this node is currently suspended.
        /// </summary>
        public bool IsSuspended { get; }

        /// <summary>
        /// Whether this node was aborted.
        /// </summary>
        public bool IsAborted { get; }

        /// <summary>
        /// Sets the owner of this <see cref="ITask"/>.
        /// </summary>
        /// <param name="tree">The tree that owns this <see cref="ITask"/>.</param>
        internal void SetOwner(BehaviourTree tree);

        /// <summary>
        /// Set the parent of this <see cref="ITask"/>.
        /// </summary>
        /// <param name="parent">The <see cref="ITask"/> that is the parent of this <see cref="ITask"/>.</param>
        internal void SetParent(ITask parent);

        /// <summary>
        /// Abort this task.
        /// </summary>
        /// <param name="policy">The abort propagation policy.</param>
        void Abort(AbortPolicy policy = AbortPolicy.Self);

        /// <summary>
        /// Resets this task to its initial state.
        /// </summary>
        /// <remarks>This is an internal API for implementing low level <see cref="ITask"/> variants. It should not be called
        /// manually from the outside. Ensure that, before calling reset, the tree's task queue is clean, e.g. by calling <see cref="Abort"/> on
        /// all affected tasks.</remarks>
        public void Reset();

        /// <summary>
        /// Notify the observer of this task of its current state. 
        /// </summary>
        /// <remarks>This is an internal API that should not be called manually.</remarks>
        internal void NotifyObserver();

        /// <summary>
        /// Register the observer for this task  .
        /// </summary>
        /// <remarks>This is an internal API that should not be called manually.</remarks>
        internal void SetObserver(Action<TaskState> observer);

        /// <summary>
        /// Completes the task with the given <paramref name="state"/> and notifies any registered observer. This is an internal API that should not be called manually.
        /// </summary>
        /// <remarks>This is an internal API that should not be called manually. Use <see cref="BehaviourTree.Abort"/> to stop the execution of a tree. <seealso cref="BehaviourTree"/></remarks>
        internal void Stop(TaskState state);

        /// <summary>
        /// Suspends this task (pauses execution).
        /// </summary>
        /// <remarks>This is an internal API that should not be called manually. Use <see cref="BehaviourTree.Suspend"/> to suspend the execution of a tree. <seealso cref="BehaviourTree"/></remarks>
        /// <exception cref="InvalidOperationException">If the task is not active. <seealso cref="IsActive"/></exception>
        internal void Suspend();

        /// <summary>
        /// Resumes this task.
        /// </summary>
        /// <remarks>This is an internal API that should not be called manually. Use <see cref="BehaviourTree.Resume"/> to suspend the execution of a tree. <seealso cref="BehaviourTree"/></remarks>
        /// <exception cref="InvalidOperationException">If the task is not suspended. <seealso cref="IsSuspended"/></exception>
        internal void Resume();
    }
}