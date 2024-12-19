using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Readymade.Machinery.EDBT
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for all tasks with multiple children.
    /// </summary>
    public abstract class CompositeBase : TaskBase, IComposite
    {
        /// <summary>
        /// storage for the children of this <see cref="IComposite"/>
        /// </summary>
        private readonly List<ITask> _children = new();

        /// <summary>
        /// a reused iterator instance to process the children without garbage and maintain a simple FSM.
        /// </summary>
        protected IEnumerator<ITask> _childIterator;

        /// <inheritdoc/>
        public IEnumerable<ITask> Children => _children;

        /// <inheritdoc/>
        public int ChildCount => _children.Count;

        /// <summary>
        /// The child task currently being executed.
        /// </summary>
        public ITask Current => _childIterator.Current;

        /// <summary>
        /// Access children by index in the order they were added.
        /// </summary>
        public ITask this[int index] => _children[index];

        /// <inheritdoc/>
        public CompositeBase() : this(default, default, null)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Create a new instance of a <see cref="T:Readymade.Machinery.EDBT.CompositeBase" /> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="children">The child task of this <see cref="T:Readymade.Machinery.EDBT.IComposite" />.</param>
        public CompositeBase(string name, params ITask[] children) : base(name)
        {
            AddChildren(children);
        }

        /// <inheritdoc/>
        protected override TaskState OnTick()
        {
            // composite task will not be ticked and instead have their behaviour implemented in OnChildComplete() and OnStarted()
            return TaskState.Running;
        }

        /// <summary>
        /// Handler to be called when the child terminates which then facilitates the continuation (event pattern).
        /// </summary>
        protected abstract void OnChildComplete(TaskState state);

        /// <inheritdoc />
        /// <summary>
        /// Convenience method to add individual children
        /// </summary>
        /// <param name="task"></param>
        public void AddChild([NotNull] ITask task)
        {
            ConfigureChild(task);
            _children.Add(task);
        }

        // TODO: maybe implement up-stream propagation
        /// <inheritdoc />
        /// Handler to be called when a child terminates which then facilitates the continuation (event pattern).
        public void AbortNotifyFromChild(AbortPolicy policy)
        {
            switch (policy)
            {
                case AbortPolicy.LowerPriority:
                {
                    while (_childIterator.MoveNext())
                    {
                        if (_childIterator.Current?.IsActive ?? false)
                        {
                            _childIterator.Current?.Abort();
                        }
                    }

                    break;
                }
                case AbortPolicy.None:
                    break;
                case AbortPolicy.Both:
                case AbortPolicy.Self:
                    throw new InvalidOperationException(
                        "A child is not allowed to notify a parent to terminate itself.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(policy), policy, null);
            }
        }

        protected override void OnReset()
        {
            foreach (var child in _children)
            {
                child.Reset();
            }
        }

        /// <summary>
        /// Add a collection of tasks as children of this task.
        /// </summary>
        /// <param name="tasks">The tasks to add as children.</param>
        public void AddChildren([NotNull] IEnumerable<ITask> tasks)
        {
            foreach (ITask task in tasks ?? Enumerable.Empty<ITask>())
            {
                ConfigureChild(task);
                _children.Add(task);
            }
        }

        /// <summary>
        /// Inject dependencies into a child task.
        /// </summary>
        private void ConfigureChild(ITask child)
        {
            child.SetParent(this);
            child.SetOwner(Owner);
        }
    }
}