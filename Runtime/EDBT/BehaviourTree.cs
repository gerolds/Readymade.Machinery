using System;
using Readymade.Machinery.Shared;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A baseline implementation of an event-driven behaviour tree. "Event driven" here means that it does not report or evaluate
    /// state while its tasks are in a running-state. This significantly reduces overhead of executing a tree with frequent ticks.
    /// It provides minimal encapsulation to enable maximum opportunities for performance optimization.
    /// </summary>
    /// <example>
    /// <para>Note that a <see cref="BehaviourTree"/> is declared statically during initialization and then never changed once <see cref="Start(System.Action{TaskState})"/> was called.
    /// If structural changes are made to the tree after it was started, its behaviour is undefined.</para>
    /// <code>
    /// void Awake() {
    ///     BehaviourTree bt = new (
    ///         new Sequence (
    ///                new Do ( () => _counter++ ),
    ///                new Condition ( () => true ),
    ///                new Do ( () => _counter++ )
    ///         )
    ///     );
    ///     bt.Start ( state => Debug.Log($"BT completed with state {state}");
    /// }
    ///
    /// void Update() => bt.Tick ();
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// This implementation is intended as a starting point for future, case specific, optimizations like concurrent
    /// execution and shared memory (blackboards). Planned short-term extensions are external events and value observers, utility functions,
    /// agent cooperation and provisions for action planning (suspend, resume, abort, etc.).
    /// </para>
    /// </remarks>
    public class BehaviourTree : IDisposable
    {
        /// <summary>
        /// A reused sentinel task used in the scheduler  
        /// </summary>
        private static readonly ITask SENTINEL = new VoidTask();

        /// <summary>
        /// The double-ended-queue used to schedule <see cref="ITask"/>s
        /// </summary>
        private readonly Deque<ITask> _scheduler = new();

        /// <summary>
        /// how often was this behaviour tree ticked since instantiation.
        /// </summary>
        private int _tickCount;

        /// <summary>
        /// time since the last tick.
        /// </summary>
        private float _deltaTime;

        /// <summary>
        /// time of last tick.
        /// </summary>
        private float _lastTickTime;

        /// <summary>
        /// the root of the tree graph this instance manages.
        /// </summary>
        private ITask _root;

        /// <summary>
        /// the task at which execution was suspended.
        /// </summary>
        private ITask _suspendedTask;

        /// <summary>
        /// Delegate to inject debug callbacks on all state changes.
        /// </summary>
        public Action<Transition> OnTransition;

        private ICancellable[] _toCancel = Array.Empty<ICancellable>();
        private IResettable[] _toReset = Array.Empty<IResettable>();

        public event Action TreeTerminated;
        public event Action TreeReset;

        /// <summary>
        /// Register a handler to be invoked when this tree terminates (via <see cref="Abort"/> or naturally).
        /// </summary>
        /// <param name="onTerminated">The handler to invoke.</param>
        /// <returns>The <see cref="BehaviourTree"/> instance (Builder API).</returns>
        public BehaviourTree OnTerminated(Action onTerminated)
        {
            TreeTerminated += onTerminated;
            return this;
        }

        /// <summary>
        /// Register a handler to be invoked when this tree is reset (via <see cref="Reset"/>).
        /// </summary>
        /// <param name="onReset">The handler to invoke.</param>
        /// <returns>The <see cref="BehaviourTree"/> instance (Builder API).</returns>
        public BehaviourTree OnReset(Action onReset)
        {
            TreeReset += onReset;
            return this;
        }

        /// <summary>
        /// Binds the <paramref name="cancellables"/> to the execution cycle of this <see cref="BehaviourTree"/>.
        /// <see cref="ICancellable"/>.<see cref="ICancellable.Cancel"/> will be called when the <see cref="BehaviourTree"/> execution terminates, i.e.
        /// when no further tasks are queued up to be evaluated or after <see cref="Abort"/> was called.
        /// </summary>
        /// <returns>The <see cref="BehaviourTree"/> instance (Builder API).</returns>
        public BehaviourTree BindCancel(params ICancellable[] cancellables)
        {
            _toCancel = cancellables;
            return this;
        }

        /// <summary>
        /// Binds the <paramref name="resettables"/> to the execution cycle of this <see cref="BehaviourTree"/>.
        /// <see cref="IResettable"/>.<see cref="IResettable.Reset"/> will be called whenever <see cref="Reset"/> is called on this instance.
        /// </summary>
        /// <returns>The <see cref="BehaviourTree"/> instance (Builder API).</returns>
        public BehaviourTree BindReset(params IResettable[] resettables)
        {
            _toReset = resettables;
            return this;
        }

        /// <summary>
        /// Describes a transition of an internal task state.
        /// </summary>
        public struct Transition
        {
            /// <summary>
            /// Create a new transition struct.
            /// </summary>
            /// <param name="task">The task that this transition occurs in.</param>
            /// <param name="source">The source state of the transition.</param>
            /// <param name="destination">The destination state of the transition.</param>
            public Transition(ITask task, TaskState source, TaskState destination)
            {
                Source = source;
                Destination = destination;
                Task = task;
            }

            /// <summary>
            /// The destination state of the transition.
            /// </summary>
            public TaskState Destination;

            /// <summary>
            /// The source state of the transition.
            /// </summary>
            public TaskState Source;

            /// <summary>
            /// The task that this transition occurs in.
            /// </summary>
            public ITask Task;
        }

        /// <summary>
        /// Create a new <see cref="BehaviourTree"/> with a specific <see cref="Root"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for this tree.</param>
        /// <param name="root">The root task of the tree.</param>
        public BehaviourTree(string name, ITask root)
        {
            Name = name;
            SetRoot(root);
#if UNITY_EDITOR
            BehaviourTreeRegistry.Register(this);
#endif
        }

        /// <summary>
        /// Create a new <see cref="BehaviourTree"/> with a specific <see cref="Root"/> task.
        /// </summary>
        /// <param name="root">The root task of the tree.</param>
        public BehaviourTree(ITask root) : this(string.Empty, root)
        {
        }

        /// <summary>
        /// A descriptive name for this instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The root task of the <see cref="BehaviourTree"/>.
        /// </summary>
        public ITask Root => _root;

        /// <summary>
        /// Set the root node of the tree and ensure all tasks are properproperly configured to satisfy tree constraints.
        /// </summary>
        /// <remarks>
        /// Note that the root of a tree should not be changed once assigned.
        /// </remarks>
        internal void SetRoot(ITask rootTask)
        {
            if (_root != null)
            {
                Debug.LogWarning($"The root task of behaviour tree should not be changed once assigned.");
            }

            _root = rootTask;
            EnsureOwnership(_root);
        }

        /// <summary>
        /// Internal API. There should be no need to call this. Resets all tasks in the tree to their initial state. 
        /// </summary>
        internal void Reset()
        {
            ResetTasks(_root);
            TreeReset?.Invoke();
            _toReset.ForEach(it => it.Reset());
        }

        /// <summary>
        /// Ensure all tasks are owned by this behaviour tree.
        /// </summary>
        private void EnsureOwnership(ITask rootTask)
        {
            Traversal.DepthFirst(rootTask, (task) =>
            {
                return task switch
                {
                    IComposite composite => composite.Children,
                    IDecorator decorator => new[] {decorator.Child},
                    _ => null
                };
            }, task => { task.SetOwner(this); });
        }

        /// <summary>
        /// Resets all tasks in the tree to their initial state.
        /// </summary>
        private void ResetTasks(ITask rootTask)
        {
            Traversal.DepthFirst(rootTask, (task) =>
            {
                return task switch
                {
                    IComposite composite => composite.Children,
                    IDecorator decorator => new[] {decorator.Child},
                    _ => null
                };
            }, task => { task.Reset(); });
        }

        /// <summary>
        /// How often this tree has had <see cref="Tick"/> called.
        /// </summary>
        public int TickCount => _tickCount;

        /// <summary>
        /// Time that has passed since the last <see cref="Tick"/>. Use with caution, this has absolutely no guarantees or
        /// semantics. It is best to not have any behaviour depend on this value.
        /// </summary>
        public float DeltaTime => _deltaTime;

#if UNITY_EDITOR
        /// <summary>
        /// The current scheduler state. Use for Debug only.
        /// </summary>
        public Deque<ITask> Scheduler => _scheduler;
#endif

        /// <summary>
        /// Tick the tree once. This evaluates all currently active nodes, fires events and queues running ones for the next tick.
        /// </summary>
        /// <exception cref="InvalidOperationException">If no root node has been assigned.</exception>
        public void Tick()
        {
            if (Root == null)
            {
                throw new InvalidOperationException(
                    "No root defined in tree. Call Start() with a root node or pass the root to the constructor.");
            }

            _tickCount++;
            _deltaTime = Time.time - _lastTickTime;
            _lastTickTime = Time.time;

            if (_scheduler.Count == 0)
            {
                return;
            }

            _scheduler.AddLast(SENTINEL);
            while (Step())
            {
            }

            if (_scheduler.Count == 0)
            {
                TreeTerminated?.Invoke();
                _toCancel.ForEach(t => t.Cancel());
            }
        }

        /// <summary>
        /// Step through the tree, fire events and queue all running nodes for the next tick.
        /// </summary>
        private bool Step()
        {
            ITask current = _scheduler.PopFirst();
            if (current == SENTINEL)
            {
                return false;
            }

            // remove any tasks that are scheduled but have completed due to callbacks outside a tick. 
            if (current.IsTerminated)
            {
                return true;
            }

            TaskState previousState = current.TaskState;
            current.Tick();
            if (OnTransition != null && current.TaskState != previousState)
                OnTransition?.Invoke(new Transition(current, previousState, current.TaskState));

            switch (current.TaskState)
            {
                case TaskState.Running:
                    // queue running tasks for next tick
                    _scheduler.AddLast(current);
                    break;
                case TaskState.Success:
                case TaskState.Failure:
                    // notify observers to decide how to continue with finished tasks
                    current.NotifyObserver();
                    break;
                case TaskState.Aborted:
                    // ignore aborted
                    break;
                case TaskState.Suspended:
                    Debug.LogWarning("Suspended task encountered in Step(), this is not supposed to happen!");
                    break;
                case TaskState.Initial:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        /// <summary>
        /// Start executing the tree from the root node.
        /// </summary>
        /// <param name="observer">An optional observer to call when the root task completes.</param>
        public void Start(Action<TaskState> observer = null)
        {
            _scheduler.Clear();
            Reset();
            Start(Root, observer);
        }

        /// <summary>
        /// Start executing the tree from a specific task. If it is the first task in the tree it will become the root.
        /// </summary>
        /// <remarks>
        /// <para>Calling this method is optional if the root node of the tree has been assigned through other means (via ctor or <see cref="SetRoot"/>)</para>
        /// <para> This is an internal API that is only exposed for testing purposes. </para>
        /// </remarks>
        /// <param name="task">The task to execute.</param>
        /// <param name="observer">An optional observer to call when the task completes.</param>
        public void Start(ITask task, Action<TaskState> observer)
        {
            if (Root == null)
            {
                SetRoot(task);
            }

            task.SetObserver(observer);
            _scheduler.AddFirst(task);
        }

        /// <summary>
        /// Defer execution of the given task until the next <see cref="Tick"/>.
        /// </summary>
        /// <param name="task">The task to defer.</param>
        /// <param name="observer">An optional observer to call when the task completes.</param>
        /// <exception cref="InvalidOperationException">If the tree has no root defined.</exception>
        public void Defer(ITask task, Action<TaskState> observer = default)
        {
            if (Root == null)
            {
                throw new InvalidOperationException();
            }

            task.SetObserver(observer);
            _scheduler.AddLast(task);
        }

        /// <summary>
        /// Suspends the execution of the tree at the current task.
        /// </summary>
        /// <remarks>Only running/active tasks can be suspended.</remarks>
        public void Suspend(ITask task)
        {
            if (_suspendedTask != null)
                throw new InvalidOperationException(
                    "The behaviour tree is already suspended; Call Resume() to continue execution");

            _suspendedTask = task ?? throw new InvalidOperationException("Must specify a task to suspend");
            if (_suspendedTask.TaskState != TaskState.Running)
                throw new InvalidOperationException("Only a running task can be suspended");
            _suspendedTask.Suspend();
        }

        /// <summary>
        /// Resumes execution of the tree at the task where it was suspended.
        /// </summary>
        public void Resume()
        {
            _suspendedTask.Stop(TaskState.Running);
            _suspendedTask.Resume();
            _suspendedTask = default;
        }

        /// <summary>
        /// Aborts the execution of the tree.
        /// </summary>
        public void Abort()
        {
            Root.Abort(AbortPolicy.Self);
            _scheduler.Clear();
            TreeTerminated?.Invoke();
            _toCancel.ForEach(cancellable => cancellable?.Cancel());
        }


        /// <inheritdoc/>
        public void Dispose()
        {
#if UNITY_EDITOR
            // unregister from debug observer
            BehaviourTreeRegistry.Unregister(this);
#endif
        }
    }
}