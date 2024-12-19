using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// The base class for all tasks (with and without child).
    /// </summary>
    public abstract class TaskBase : ITask
    {
        /// <summary>
        /// A callback to be invoked whenever the state of this task changes.
        /// </summary>
        private Action<TaskState> _observer;

        /// <summary>
        /// The <see cref="BehaviourTree"/> this task belongs to.
        /// </summary>
        private BehaviourTree _owner;

        /// <summary>
        /// The parent of this task.
        /// </summary>
        private ITask _parent;

        /// <inheritdoc />
        public TaskState TaskState { get; private set; } = TaskState.Initial;

        /// <inheritdoc />
        public ITask Parent => _parent;

        /// <inheritdoc />
        public BehaviourTree Owner => _owner;

        /// <inheritdoc />
        public bool IsActive => TaskState is TaskState.Running or TaskState.Initial;

        /// <inheritdoc />
        public bool IsTerminated => IsComplete || IsAborted;

        /// <inheritdoc />
        public bool IsComplete => TaskState is TaskState.Failure or TaskState.Success;

        /// <inheritdoc />
        public bool IsSuspended => TaskState is TaskState.Suspended;

        /// <inheritdoc />
        public bool IsAborted => TaskState is TaskState.Aborted;

        /// <inheritdoc />
        void ITask.SetOwner(BehaviourTree tree) => _owner = tree;

        /// <inheritdoc />
        void ITask.SetParent(ITask parent)
        {
            _parent = parent;
        }

        /// <inheritdoc />
        [NotNull]
        public string Name { get; internal set; } = string.Empty;

        /// <summary>
        /// Create a new task instance.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        protected TaskBase(string name)
        {
            Name = name ?? string.Empty;
        }

        /// <inheritdoc />
        public void Abort(AbortPolicy policy)
        {
            switch (policy)
            {
                case AbortPolicy.None:
                    break;
                case AbortPolicy.Self:
                    AbortSelf();
                    break;
                case AbortPolicy.LowerPriority:
                    NotifyParent();
                    break;
                case AbortPolicy.Both:
                    AbortSelf();
                    NotifyParent();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(policy), policy, null);
            }

            void AbortSelf()
            {
                if (TaskState is TaskState.Running or TaskState.Initial)
                {
                    TaskState = TaskState.Aborted;
                }

                // We immediately call the handler here (no scheduling into the Tick() since we don't expect any more ticks after
                // an abort)
                // Debug.Log ( $"[{GetType ().GetNiceName ()}] '{Name}' was aborted." );
                OnAborted();
            }

            void NotifyParent()
            {
                if (Parent is IComposite composite)
                {
                    composite.AbortNotifyFromChild(AbortPolicy.LowerPriority);
                }
                else if (Parent is IDecorator decorator)
                {
                    decorator.AbortNotifyFromChild(AbortPolicy.LowerPriority);
                }
            }
        }


        /// <inheritdoc />
        TaskState ITask.Tick()
        {
            if (TaskState == TaskState.Initial)
            {
                Debug.Assert(Owner != null, "Owner != null");
                Debug.Assert(Parent != null || Owner.Root == this, "Parent != null || Owner.Root == this");
                TaskState = TaskState.Running;
                OnStarted();
                /*
                if ( this is IComposite )
                    Owner.OnTransition?.Invoke ( new BehaviourTree.Transition ( this, TaskState.Initial, TaskState.Running ) );
                */
            }

            TaskState = OnTick();
            switch (TaskState)
            {
                case TaskState.Running:
                    break;
                case TaskState.Suspended:
                    OnSuspended();
                    break;
                case TaskState.Failure:
                case TaskState.Success:
                    OnStopped(TaskState);
                    break;
                case TaskState.Aborted:
                    // OnAborted() is called immediately from Abort() as we do not expect further ticks after an abort.
                    break;
                case TaskState.Initial:
                    throw new InvalidOperationException(
                        $"A task was unexpectedly in {nameof(TaskState)} == {nameof(TaskState.Initial)} after {nameof(OnTick)}()");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return TaskState;
        }

        /// <inheritdoc />
        void ITask.Reset()
        {
            TaskState = TaskState.Initial;
            OnReset();
        }

        /// <inheritdoc />
        void ITask.SetObserver(Action<TaskState> observer) => _observer = observer;

        /// <inheritdoc />
        void ITask.Stop(TaskState state)
        {
            StopWithoutNotify(state);
            ((ITask) this).NotifyObserver();
        }

        /// <summary>
        /// Completes this task with the given state <paramref name="setTaskState"/> but does <i>not</i> notify any registered observer.  
        /// </summary>
        public void StopWithoutNotify(TaskState setTaskState)
        {
            Debug.Assert(setTaskState is TaskState.Failure or TaskState.Success,
                "setTaskState is TaskState.Failure or TaskState.Success");
            Debug.Assert(IsActive, "IsActive");

            if (!IsActive)
                throw new InvalidEnumArgumentException(
                    $"{GetType().Name} '{Name}' is not in a stoppable state ({TaskState})");
            TaskState prevSate = TaskState;
            TaskState = setTaskState;
            Owner.OnTransition?.Invoke(new BehaviourTree.Transition(this, prevSate, TaskState));
        }

        /// <inheritdoc />
        void ITask.NotifyObserver()
        {
            Debug.Assert(!IsActive, "!IsActive");
            if (IsActive)
                throw new InvalidOperationException(
                    $"{GetType().Name} '{Name}' is active; Observers can only be notified when a task is not running");
            _observer?.Invoke(TaskState);
        }

        /// <inheritdoc />
        void ITask.Suspend()
        {
            if (!IsActive)
                throw new InvalidOperationException(
                    $"{GetType().Name} '{Name}' is not active; Only an active task can be suspended");
            TaskState = TaskState.Suspended;
        }

        /// <inheritdoc />
        void ITask.Resume()
        {
            if (!IsSuspended)
                throw new InvalidOperationException(
                    $"{GetType().Name} '{Name}' is not suspended; Only a suspended task can be resumed");
            TaskState = TaskState.Running;
            OnResumed();
        }

        /// <summary>
        /// Called when the previously suspended task resumes.
        /// </summary>
        protected abstract void OnResumed();

        /// <summary>
        /// Called when the task is suspended in its current state. 
        /// </summary>
        protected abstract void OnSuspended();

        /// <summary>
        /// Called each time the behaviour is ticked while this task <see cref="IsActive"/> 
        /// </summary>
        protected abstract TaskState OnTick();

        /// <summary>
        /// Called once before <see cref="OnTick"/> when the task starts executing.  
        /// </summary>
        protected abstract void OnStarted();

        /// <summary>
        /// Called once when the task is reset to its initial state.
        /// </summary>
        protected abstract void OnReset();

        /// <summary>
        /// Called once when the task <see cref="IsComplete"/> (i.e. it has decided on its success or failure state). Will not
        /// be called when a task <see cref="IsAborted"/>. Once a task is stopped, any acquired and owned resources should be released.
        /// <seealso cref="OnAborted"/>
        /// </summary>
        protected abstract void OnStopped(TaskState state);

        /// <summary>
        /// Called once when the task is aborted. This only gets called on <see cref="IsActive"/> (running) tasks and would
        /// therefore happen independently of any calls to <see cref="OnStopped"/>. In both cases the task should clean up after
        /// itself. Once a task is aborted, any acquired and owned resources should be released.
        /// </summary>
        protected abstract void OnAborted();
    }
}