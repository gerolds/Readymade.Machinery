using System;
using System.Collections;
using System.Collections.Generic;
using Readymade.Machinery.Shared;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A performance describes a sequence of gestures that can be performed by a given actor type (<typeparamref name="IActor" />).
    /// </summary>
    /// <typeparam name="IActor">The actor type that can enact the gestures in this performance.</typeparam>
    public sealed class Performance : IPerformance<IActor>
    {
        /// <summary>
        /// Stores the gestures that compose this performance.
        /// </summary>
        private readonly List<IGesture<IActor>> _gestures = new();

        /// <summary>
        /// An optional timeout (max runtime) of this performance.
        /// </summary>
        private float _timeout;

        /// <summary>
        /// Whether this performance should be cancelled.
        /// </summary>
        private bool _isCancelled;

        /// <summary>
        /// Whether this performance is currently running.
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// Whether this performance has completed.
        /// </summary>
        private bool _isComplete = false;

        /// <summary>
        /// The actor running this performance, if any
        /// </summary>
        private IActor _runActor;

        /// <summary>
        /// The gesture currently run by <see cref="_runActor"/>, if any.
        /// </summary>
        private IGesture<IActor> _currentGesture;

        /// <inheritdoc />        
        public event Action<Performance, IActor> Started;

        /// <inheritdoc />
        public event Action<Performance, IActor> Completed;

        /// <inheritdoc />     
        public event Action<Performance, IActor> Failed;

        /// <inheritdoc />     
        public event Action<Performance, IActor> NextGesture;

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public IPerformance.RunPhase Phase { get; private set; }

        /// <inheritdoc />
        public IEnumerable<IGesture> Gestures => _gestures;

        /// <inheritdoc />
        public int RunCount { get; private set; }

        /// <inheritdoc />
        IGesture IPerformance.CurrentGesture => CurrentGesture;

        /// <inheritdoc />
        public IGesture<IActor> CurrentGesture => _currentGesture;

        /// <inheritdoc />
        public IActor RunActor => _runActor;

        /// <inheritdoc />
        public bool IsReady => !_isComplete && !_isCancelled && !_isRunning;

        /// <inheritdoc />
        public bool IsRunning => _isRunning;

        /// <inheritdoc />
        public bool IsFailed => _isCancelled;

        /// <inheritdoc />     
        bool IPerformance<IActor>.IsTimeoutElapsed() => _timeout < Timing.Source.Time;

        /// <summary>
        /// Factory method to create a <see cref="IPerformance"/> with one <see cref="IGesture{IActor}"/>  that will acquire a static <see cref="SoProp"/>.
        /// </summary>
        /// <param name="prop">The <see cref="SoProp"/> to get.</param>
        public static Performance CreateFromProp(PropCount prop)
        {
            return new Performance(new FunGesture(prop));
        }

        /// <summary>
        /// Factory method to create a <see cref="IPerformance"/> with one <see cref="IGesture{IActor}"/> that will acquire a dynamic <see cref="SoProp"/>.
        /// </summary>
        /// <param name="getProp">A <see cref="Func{SoProp}"/> delegate to get the required <see cref="SoProp"/>.</param>
        public static Performance CreateFromProp(Func<PropCount> getProp)
        {
            return new Performance(new FunGesture(getProp));
        }

        /// <summary>
        /// Factory method to create a <see cref="IPerformance"/> with one <see cref="IGesture{IActor}"/> that will assume a static <see cref="Pose"/>.
        /// </summary>
        public static Performance CreateFromPose(Pose pose)
        {
            return new Performance(new FunGesture(pose, PoseComparer.Default, default));
        }

        /// <summary>
        /// Factory method to create a <see cref="IPerformance"/> with one <see cref="IGesture{IActor}"/> that will assume a dynamic <see cref="Pose"/>.
        /// </summary>
        /// <param name="getPose">A <see cref="Func{Pose}"/> delegate to get the required <see cref="Pose"/>.</param>
        public static Performance CreateFromPose(Func<Pose> getPose)
        {
            return new Performance(new FunGesture(getPose, PoseComparer.Default, default));
        }

        /// <summary>
        /// Creates a new instance of a <see cref="IPerformance{IActor}"/> with a given list of <paramref name="gestures"/> added in sequence.
        /// </summary>
        /// <param name="gestures">The gestures to add to the performance, in order.</param>
        public Performance(params IGesture<IActor>[] gestures)
        {
            _gestures.AddRange(gestures);
#if UNITY_EDITOR
            PerformanceRegistry.Register(this);
#endif
        }

        /// <summary>
        /// Creates a new instance of a <see cref="IPerformance{IActor}"/> with a given list of <paramref name="gestures"/> added in sequence.
        /// </summary>
        /// <param name="name">A descriptive name for this gesture.</param>
        /// <param name="gestures">The gestures to add to the performance, in order.</param>
        public Performance(string name, params IGesture<IActor>[] gestures)
        {
            _gestures.AddRange(gestures);
            Name = name;
#if UNITY_EDITOR
            PerformanceRegistry.Register(this);
#endif
        }

        /// <summary>
        /// Creates a new instance of a <see cref="IPerformance{IActor}"/> with a given list of <paramref name="gestures"/> added
        /// in sequence and a set of delegates that will be added on to its lifecycle events. This API is provided as a convenience.
        /// </summary>
        /// <seealso cref="OnStarted"/><seealso cref="OnCompleted"/> <seealso cref="OnFailed"/>
        /// <param name="gestures">The gestures to add to the performance, in order.</param>
        /// <param name="onFailed">The delegate to add to the <see cref="Failed"/> event.</param>
        /// <param name="onStarted">The delegate to add to the <see cref="Started"/> event.</param>
        /// <param name="onCompleted">The delegate to add to the <see cref="Completed"/> event.</param>
        public Performance(
            Action<Performance, IActor> onStarted,
            Action<Performance, IActor> onCompleted,
            Action<Performance, IActor> onFailed,
            params IGesture<IActor>[] gestures
        )
        {
            _gestures.AddRange(gestures);
            OnStarted(onStarted);
            OnCompleted(onCompleted);
            OnFailed(onFailed);
#if UNITY_EDITOR
            PerformanceRegistry.Register(this);
#endif
        }

        /// <summary>
        /// Creates a new named instance of a <see cref="IPerformance{IActor}"/> with a given list of <paramref name="gestures"/> added
        /// in sequence and a set of delegates that will be added on to its lifecycle events. This API is provided as a convenience.
        /// </summary>
        /// <seealso cref="OnStarted"/><seealso cref="OnCompleted"/> <seealso cref="OnFailed"/>
        /// <param name="name">A descriptive name for this performance.</param>
        /// <param name="gestures">The gestures to add to the performance, in order.</param>
        /// <param name="onFailed">The delegate to add to the <see cref="Failed"/> event.</param>
        /// <param name="onStarted">The delegate to add to the <see cref="Started"/> event.</param>
        /// <param name="onCompleted">The delegate to add to the <see cref="Completed"/> event.</param>
        public Performance(
            string name,
            Action<Performance, IActor> onStarted,
            Action<Performance, IActor> onCompleted,
            Action<Performance, IActor> onFailed,
            params IGesture<IActor>[] gestures
        )
        {
            Name = name;
            _gestures.AddRange(gestures);
            OnStarted(onStarted);
            OnCompleted(onCompleted);
            OnFailed(onFailed);
#if UNITY_EDITOR
            PerformanceRegistry.Register(this);
#endif
        }

        /// <inheritdoc/>
        public IPerformance<IActor> AppendGesture(IGesture<IActor> gesture)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            _gestures.Add(gesture);
            return this;
        }

        /// <summary>
        /// Append a gesture by specifying a set of <see cref="FunGesture.Args"/> parameters.
        /// </summary>
        /// <param name="args">The <see cref="FunGesture.Args"/> instance that defines the gesture.</param>
        /// <returns>A self reference to this performance (Builder API).</returns>
        /// <exception cref="InvalidOperationException">If the gesture is running.</exception>
        public IPerformance<IActor> AppendGesture(FunGesture.Args args)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            _gestures.Add(new FunGesture(args));
            return this;
        }

        /// <summary>
        /// Append a gesture by specifying an object that implements the <see cref="IGestureHandler"/> interface.
        /// </summary>
        /// <param name="handler">The <see cref="IGestureHandler"/> instance that defines the gesture.</param>
        /// <returns>A self reference to this performance (Builder API).</returns>
        /// <exception cref="InvalidOperationException">If the gesture is running.</exception>
        public IPerformance<IActor> AppendGesture(IGestureHandler handler)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            _gestures.Add(new Gesture(handler));
            return this;
        }

        /// <inheritdoc/>
        public IPerformance<IActor> OnCompleted(Action<Performance, IActor> action)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            Completed -= action;
            Completed += action;
            return this;
        }

        /// <inheritdoc/>
        public IPerformance<IActor> OnFailed(Action<Performance, IActor> action)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            Failed -= action;
            Failed += action;
            return this;
        }

        /// <inheritdoc/>
        public IPerformance<IActor> OnStarted(Action<Performance, IActor> action)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Cannot modify a performance while it is running.");
            }

            Started -= action;
            Started += action;
            return this;
        }

        /// <inheritdoc/>
        /// <remarks>Requires a call to <see cref="Reset"/> before another valid call to <see cref="RunAsync"/> can be made.</remarks>
        public void Cancel()
        {
            if (_isCancelled || _isComplete)
            {
                return;
            }

            // we call the handler immediately since we cannot expect further iteration of the enumerator once we're cancelled.
            _isCancelled = true;
            OnCancel(_runActor);
            _isCancelled = true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            if (_isRunning)
            {
                Cancel();
            }

            _isCancelled = false;
            _isRunning = false;
            _isComplete = false;
            _runActor = default;
            Phase = IPerformance.RunPhase.Waiting;
            _currentGesture = default;
            foreach (IGesture<IActor> gesture in _gestures)
            {
                gesture.Reset();
            }
        }


        /// <inheritdoc />
        public IEnumerator RunAsync(IActor actor)
        {
            // a few pedantic checks that force the user to explicitly deal with resetting the performance which should hopefully
            // expose some bugs that would otherwise be hidden/swallowed.
            if (_isComplete)
            {
                throw new InvalidOperationException(
                    "Cannot run a performance that is complete. Call Reset() and try again.");
            }

            if (_isCancelled)
            {
                throw new InvalidOperationException(
                    "Cannot run a performance that is cancelled. Call Reset() and try again.");
            }

            if (_isRunning)
            {
                throw new InvalidOperationException(
                    "Cannot re-run a performance while it is still running. Call Reset() and try again.");
            }

            // we keep a reference to the actor that started the run in the object state to enable validation from the outside.
            _runActor = actor;

            // to execute a performance we use a Enumerator as a state machine and simple abstraction that can represent
            // generic asynchronous execution. Here we splice any lifecycle/status callbacks and delays into the while
            // loop that iterates gestures collection based on the config of those gestures.

            RunCount++;
            _isRunning = true;
            _isCancelled = false;
            using IEnumerator<IGesture<IActor>> gestureEnumerator = _gestures.GetEnumerator();
            Phase = IPerformance.RunPhase.Running;
            int iteration = -1;
            while (gestureEnumerator.MoveNext())
            {
                iteration++;

                _currentGesture = gestureEnumerator.Current;
                _timeout = gestureEnumerator.Current?.Timeout <= 0
                    ? float.PositiveInfinity
                    : (gestureEnumerator.Current?.Timeout + Timing.Source.Time ?? Timing.Source.Time);

                // we invoke the started event after the iterator has had a chance to populate the current state properties, this
                // way the event subscriber does not have to worry about them being unset in start.
                if (iteration == 0)
                {
                    Started?.Invoke(this, actor);
                }

                NextGesture?.Invoke(this, actor);
                yield return null;

                if (_currentGesture?.TryStart(actor, this) ?? false)
                {
                }
                else
                {
                    Phase = IPerformance.RunPhase.Failed;
                    Debug.LogWarning(
                        $"[{GetType().GetNiceName()}] A gesture failed to start;"
                        /*
                        +
                        $"\nPose required: {_currentGesture?.IsPoseRequired} {( _currentGesture?.IsPoseRequired ?? false ? $"{Vector3.Distance ( _currentGesture.Pose.position, actor.Pose.position ):f2} units away" : string.Empty )}" +
                        $"\nProp required: {_currentGesture?.IsPropRequired} {( _currentGesture?.IsPropRequired ?? false ? string.IsNullOrEmpty ( _currentGesture.Prop?.Name ) ? "unnamed" : _currentGesture.Prop.Name : string.Empty )}" +
                        $"\nInventory:\n{string.Join ( "\n    ", actor.Inventory.Items.Select ( it => $"{it.Count}x {it.Prop?.Name}" ) )}"
                        */
                    );
                    Failed?.Invoke(this, actor);
                    yield break;
                }

                do
                {
                    // whether the current gesture's OnTick runs is an implementation detail of the respective IGesture 
                    gestureEnumerator.Current.Tick(actor, this);

                    yield return null;

                    // OnCancel() handler was already called and we just ensure that we do not continue iterating the
                    // IEnumerable.
                    if (_isCancelled)
                    {
                        // yield break should break out of the RunAsync scope right?
                        Phase = IPerformance.RunPhase.Failed;
                        Debug.LogWarning($"[{GetType().GetNiceName()}] A gesture was cancelled.");
                        Failed?.Invoke(this, actor);
                        _isRunning = false;
                        yield break;
                    }

                    // we check the timeout that helps us free agents stuck on performances that won't complete.
                    if (((IPerformance<IActor>)this).IsTimeoutElapsed())
                    {
                        Phase = IPerformance.RunPhase.Failed;
                        Debug.LogWarning($"[{GetType().GetNiceName()}] A gesture has timed out.");
                        Failed?.Invoke(this, actor);
                        _isRunning = false;
                        yield break;
                    }
                } while (!_isCancelled && (!gestureEnumerator.Current?.TryComplete(actor, this) ?? false));

                yield return null;
            }

            // why do we need to check again here?
            if (_isCancelled)
            {
                Debug.Log("This should not happen!");
                yield break;
            }

            Phase = IPerformance.RunPhase.Complete;
            Completed?.Invoke(this, actor);
            _isComplete = true;
            _isRunning = false;
        }

        private void OnCancel(IActor actor)
        {
            Debug.Log($"[{GetType().GetNiceName()}] Performance {this} was cancelled");
            _currentGesture?.Abort(actor, this);
            Phase = IPerformance.RunPhase.Failed;
            Failed?.Invoke(this, actor);
        }

        /// <inheritdoc />
        public void Dispose()
        {
#if UNITY_EDITOR
            PerformanceRegistry.Unregister(this);
#endif
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return
                $"({GetType().GetNiceName()} {GetHashCode().ToString("X")[..4]}{(string.IsNullOrEmpty(Name) ? "" : $" {Name}")})";
        }
    }
}