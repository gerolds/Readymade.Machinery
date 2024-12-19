using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Readymade.Machinery.Shared;
using UnityEngine;
using Vertx.Debugging;

namespace Readymade.Machinery.Acting
{
    /// <inheritdoc />
    /// <summary>
    /// A gesture that can be injected with delegates. Useful for creating functional, ad-hoc or in-place gestures that need not
    /// track state themselves. Provided as an alternative to <see cref="Gesture"/>.
    /// </summary>
    /// <typeparam name="IActor">The actor type that can run this gesture.</typeparam>
    /// <seealso cref="Gesture"/>
    public sealed class FunGesture : IGesture<IActor>
    {
        /// <summary>
        /// Holds ctor arguments.
        /// </summary>
        public struct Args
        {
            // This struct has no constructor because that would defeat the purpose of having an args-struct.

            /// <summary>
            /// Delegate to ge the required pose for this gesture. Ignored if default.
            /// </summary>
            public Func<Pose> GetPose;

            /// <summary>
            /// Delegate to ge the required prop for this gesture. Ignored if default.
            /// </summary>
            public Func<PropCount> GetProp;

            /// <summary>
            /// Delegate to ge the required prop for this gesture. Ignored if default.
            /// </summary>
            public Func<int> GetCount;

            /// <summary>
            /// The action to run when the gesture is complete (timer elapsed).
            /// </summary>
            public GestureAct OnComplete;

            /// <summary>
            /// The action to run when the gesture is ticked.
            /// </summary>
            public GestureFunc<bool> OnTick;

            /// <summary>
            /// The action to run when the gesture starts.
            /// </summary>
            public GestureFunc<bool> OnStart;

            /// <summary>
            /// The action to run when the gesture has failed (did not complete but start was invoked).
            /// </summary>
            public GestureAct OnFailed;

            /// <summary>
            /// Delegate to check if the gesture can complete.
            /// </summary>
            public GestureFunc<bool> CanComplete;

            /// <summary>
            /// Delegate to check if the gesture can be ticked.
            /// </summary>
            public GestureFunc<bool> CanTick;

            /// <summary>
            /// Pose comparer to use internally.
            /// </summary>
            public IEqualityComparer<Pose> Comparer;
            
            /// <summary>
            /// A descriptive name for this gesture. 
            /// </summary>
            public string Name;
        }

        /// <summary>
        /// The action to run when the gesture is complete (timer elapsed).
        /// </summary>
        private readonly GestureAct _onComplete;

        /// <summary>
        /// The action to run when the gesture is ticked.
        /// </summary>
        private readonly GestureFunc<bool> _onTick;

        /// <summary>
        /// The action to run when the gesture starts.
        /// </summary>
        private readonly GestureFunc<bool> _onStart;

        /// <summary>
        /// The action to run when the gesture has failed (did not complete but start was invoked).
        /// </summary>
        private readonly GestureAct _onFailed;

        /// <summary>
        /// Delegate to check if the gesture can complete.
        /// </summary>
        private readonly GestureFunc<bool> _canComplete;

        /// <summary>
        /// Delegate to check if the gesture can be ticked.
        /// </summary>
        private readonly GestureFunc<bool> _canTick;

        /// <summary>
        /// Delegate to ge the required pose for this gesture. Ignored if default.
        /// </summary>
        private readonly Func<Pose> _getPose;

        /// <summary>
        /// Delegate to ge the required prop for this gesture. Ignored if default.
        /// </summary>
        private readonly Func<PropCount> _getProp;

        /// <summary>
        /// Whether the pose should be ignored.
        /// </summary>
        private readonly bool _ignorePose;

        /// <summary>
        /// Whether the prop should be ignored.
        /// </summary>
        private readonly bool _ignoreProp;

        /// <summary>
        /// Duration of the gesture.
        /// </summary>
        private readonly float _duration;

        /// <summary>
        /// Time the gesture was started.
        /// </summary>
        private float _timeStarted;

        /// <summary>
        /// Time the gesture was last ticked.
        /// </summary>
        private float _timeLastTick;

        private readonly SpatialHeuristic _heuristic = SpatialHeuristic.RandomProximity;
        private readonly IEqualityComparer<Pose> _comparer;

        /// <inheritdoc cref="IGesture{IActor}"/>
        public bool IsPoseRequired => !_ignorePose && _getPose != null;

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public bool IsPropRequired => !_ignoreProp && _getProp != null;

        /// <inheritdoc />
        public Pose Pose
        {
            get
            {
                Debug.Assert(!IsPoseRequired || _getPose != default,
                    "ASSERTION FAILED: !IsPoseRequired || _getPose != default");
                return _getPose?.Invoke() ?? default;
            }
        }

        /// <inheritdoc />
        public PropCount Prop
        {
            get
            {
                Debug.Assert(!IsPropRequired || _getProp != default,
                    "ASSERTION FAILED: !IsPropRequired || _getProp != default");
                return _getProp?.Invoke() ?? default;
            }
        }

        /// <inheritdoc />
        public float SinceStarted => Timing.Source.Time - _timeStarted;

        /// <inheritdoc />
        public float SinceTick => Timing.Source.Time - _timeLastTick;

        /// <inheritdoc />
        public float Timeout { get; set; }

        /// <inheritdoc />
        public IGesture.RunPhase Phase { get; private set; }

        /// Disable the default ctor.
        private FunGesture()
        {
        }

        /// <inheritdoc />
        void IGesture.Reset()
        {
            _timeLastTick = 0;
            _timeStarted = 0;
        }

        /// <summary>
        /// Check whether <see cref="_duration"/> has elapsed since the gesture was started.
        /// </summary>
        private bool TimerElapsed(IActor actor, IPerformance<IActor> performance, IGesture<IActor> gesture)
            => SinceStarted > _duration;

        /// <summary>
        /// Creates a new instance of a gesture that is fully customizable via delegates from an <see cref="Args"/> struct. 
        /// </summary>
        /// <param name="args">A configuration struct</param>
        /// <remarks>This ctor is easier to use in an IDE with code generation than the plain ctor and potentially yields a
        /// more readable call syntax.</remarks>
        public FunGesture(Args args)
        {
            _ignorePose = args.GetPose == default;
            _ignoreProp = args.GetProp == default;
            _getPose = args.GetPose;
            _getProp = args.GetProp;
            _canComplete = args.CanComplete ?? (_ => true);
            _canTick = args.CanTick ?? (_ => true);
            _onComplete = args.OnComplete ?? (_ => { });
            _onStart = args.OnStart ?? (_ => true);
            _onFailed = args.OnFailed ?? (_ => { });
            _onTick = args.OnTick ?? (_ => true);
            _comparer = args.Comparer ?? PoseComparer.Default;
            Name = args.Name;
        }

        /// <summary>
        /// Creates a new instance of a gesture that is fully customizable via delegates.
        /// </summary>
        /// <param name="getPose">The pose in which the gesture can be executed.</param> 
        /// <param name="getProp">The property (prop) this gesture requires.</param>
        /// <param name="canComplete">The delegate to control whether a gesture is complete. Can be used to inject a dynamic
        /// timer or any state value observer.</param>
        /// <param name="canTick">Decide whether the tick delegate should be invoked. Will be dynamically checked each
        /// time the gesture is ticked. Can be used to implement interval ticks and thus looping/repeated gestures. </param>
        /// <param name="comparer"></param>
        /// <param name="handlers">A set of handler delegates to define the behaviour of this gesture.</param>
        public FunGesture(
            Func<Pose> getPose = default,
            Func<PropCount> getProp = default,
            GestureFunc<bool> canComplete = default,
            GestureFunc<bool> canTick = default,
            IEqualityComparer<Pose> comparer = default,
            (GestureFunc<bool> onStart,
                GestureFunc<bool> onTick,
                GestureAct onComplete,
                GestureAct onFailed) handlers = default
        )
        {
            _ignorePose = getPose == default;
            _ignoreProp = getProp == default;
            _getPose = getPose;
            _getProp = getProp;
            _canComplete = canComplete ?? (_ => true);
            _canTick = canTick ?? (_ => true);
            _comparer = comparer ?? PoseComparer.Default;
            if (handlers == default)
            {
                _onComplete = _ => { };
                _onStart = _ => true;
                _onFailed = _ => { };
                _onTick = _ => true;
            }
            else
            {
                _onComplete = handlers.onComplete ?? (_ => { });
                _onStart = handlers.onStart ?? (_ => true);
                _onFailed = handlers.onFailed ?? (_ => { });
                _onTick = handlers.onTick ?? (_ => true);
            }
        }

        /// <summary>
        /// Creates a new instance of a gesture that just defines a prop and a completion callback. Useful if the actor should
        /// just get some prop.
        /// </summary>
        /// <param name="prop">The prop required for the gesture to execute.</param>
        /// <param name="comparer"></param>
        /// <param name="heuristic"></param>
        /// <param name="onComplete">The delegate to invoke when the gesture is complete</param>
        public FunGesture(
            PropCount prop,
            IEqualityComparer<Pose> comparer = default,
            SpatialHeuristic heuristic = SpatialHeuristic.RandomProximity,
            GestureAct onComplete = default
        )
        {
            Debug.Assert(!prop.Equals(default), "ASSERTION FAILED: prop != default");
            _heuristic = heuristic;
            _ignorePose = true;
            _ignoreProp = false;
            _getPose = default;
            _getProp = () => prop;
            _canComplete = _ => true;
            _canTick = _ => true;
            _onComplete = _ => { };
            _onStart = _ => true;
            _onFailed = _ => { };
            _onTick = _ => true;
            _onComplete = onComplete ?? (_ => { });
            _comparer = comparer ?? PoseComparer.Default;
        }

        /// <summary>
        /// Creates a new instance of a gesture that just defines a dynamic prop and a completion callback. Useful if the actor should
        /// just get some prop.
        /// </summary>
        /// <param name="getProp">A delegate that returns the prop required for the gesture to execute.</param>
        /// <param name="comparer"></param>
        /// <param name="onComplete">The delegate to invoke when the gesture is complete</param>
        public FunGesture(
            [NotNull] Func<PropCount> getProp,
            IEqualityComparer<Pose> comparer = default,
            GestureAct onComplete = default
        )
        {
            _ignorePose = true;
            Debug.Assert(getProp != null, "ASSERTION FAILED: getProp != null");
            _ignoreProp = false;
            _getPose = default;
            _getProp = getProp;
            _canComplete = _ => true;
            _canTick = _ => true;
            _onComplete = _ => { };
            _onStart = _ => true;
            _onFailed = _ => { };
            _onTick = _ => true;
            _onComplete = onComplete ?? (_ => { });
            _comparer = comparer ?? PoseComparer.Default;

        }

        /// <summary>
        /// Creates a new instance of a gesture that just defines a pose and a completion callback. Useful if the actor should
        /// just move somewhere.
        /// </summary>
        /// <param name="pose">The pose in which the gesture can be executed.</param>
        /// <param name="comparer"></param>
        /// <param name="onComplete">The delegate to invoke when the gesture is complete</param>
        public FunGesture(
            Pose pose,
            IEqualityComparer<Pose> comparer = default,
            GestureAct onComplete = default
        )
        {
            Debug.Assert(pose != default, "ASSERTION FAILED: pose != default");
            _ignorePose = pose == default;
            _ignoreProp = true;
            _getPose = () => pose;
            _getProp = default;
            _canComplete = _ => true;
            _canTick = _ => true;
            _onComplete = _ => { };
            _onStart = _ => true;
            _onFailed = _ => { };
            _onTick = _ => true;
            _onComplete = onComplete ?? (_ => { });
            _comparer = comparer ?? PoseComparer.Default;

        }

        /// <summary>
        /// Creates a new instance of a gesture that just defines a dynamic pose and a completion callback. Useful if the actor should
        /// just move somewhere.
        /// </summary>
        /// <param name="getPose">The pose in which the gesture can be executed.</param>
        /// <param name="comparer"></param>
        /// <param name="onComplete">The delegate to invoke when the gesture is complete</param>
        public FunGesture(
            Func<Pose> getPose = default,
            IEqualityComparer<Pose> comparer = default,
            GestureAct onComplete = default
        )
        {
            Debug.Assert(getPose != default, "ASSERTION FAILED: getPose != default");
            _ignorePose = getPose == default;
            _ignoreProp = true;
            _getPose = getPose;
            _getProp = default;
            _canComplete = _ => true;
            _canTick = _ => true;
            _onComplete = _ => { };
            _onStart = _ => true;
            _onFailed = _ => { };
            _onTick = _ => true;
            _onComplete = onComplete ?? (_ => { });
            _comparer = comparer ?? PoseComparer.Default;

        }

        /// <summary>
        /// Creates a new instance of a gesture that runs for a given duration and ticked at a certain interval.
        /// </summary>
        /// <param name="pose">The pose in which the gesture can be executed.</param>
        /// <param name="prop">The property (prop) this gesture requires.</param>
        /// <param name="duration">The desired time between onStart and onComplete are invoked.</param>
        /// <param name="tickInterval">The interval at which onTick can be invoked.</param>
        /// <param name="comparer"></param>
        /// <param name="handlers">A set of handler delegates to define the behaviour of this gesture.</param>
        public FunGesture(
            Pose pose = default,
            PropCount prop = default,
            float duration = 0,
            float tickInterval = 0,
            IEqualityComparer<Pose> comparer = default,
            ( GestureFunc<bool> onStart,
                GestureFunc<bool> onTick,
                GestureAct onComplete,
                GestureAct onFailed ) handlers = default
        )
        {
            _ignorePose = pose == default;
            _ignoreProp = prop == default;
            _getPose = () => pose;
            _getProp = () => prop;
            _duration = duration;
            _canComplete = args => args.gesture.SinceStarted < duration;
            _canTick = args => args.gesture.SinceTick > tickInterval;
            _comparer = comparer ?? PoseComparer.Default;
            if (handlers == default)
            {
                _onComplete = _ => { };
                _onStart = _ => true;
                _onFailed = _ => { };
                _onTick = _ => true;
            }
            else
            {
                _onComplete = handlers.onComplete ?? (_ => { });
                _onStart = handlers.onStart ?? (_ => true);
                _onFailed = handlers.onFailed ?? (_ => { });
                _onTick = handlers.onTick ?? (_ => true);
            }
        }

        /// <summary>
        /// Creates a new instance of a gesture that runs until <see cref="canComplete"/> returns true and is ticked at a regular interval.
        /// </summary>
        /// <param name="pose">The pose in which the gesture can be executed.</param>
        /// <param name="prop">The property (prop) this gesture requires.</param>
        /// <param name="canComplete">The delegate to control whether a gesture is complete. Can be used to inject a dynamic
        /// timer or any state value observer.</param>
        /// <param name="tickInterval">The interval at which onTick can be invoked.</param>
        /// <param name="comparer"></param>
        /// <param name="handlers">A set of handler delegates to define the behaviour of this gesture.</param>
        public FunGesture(
            Pose pose = default,
            PropCount prop = default,
            float tickInterval = 0,
            GestureFunc<bool> canComplete = default,
            IEqualityComparer<Pose> comparer = default,
            (GestureFunc<bool> onStart,
                GestureFunc<bool> onTick,
                GestureAct onComplete,
                GestureAct onFailed) handlers = default
        )
        {
            _ignorePose = pose == default;
            _getPose = () => pose;
            _ignoreProp = prop == default;
            _getProp = () => prop;
            _duration = 0;
            _canComplete = canComplete ?? (_ => true);
            _canTick = args => args.gesture.SinceTick > tickInterval;
            _comparer = comparer ?? PoseComparer.Default;
            if (handlers == default)
            {
                _onComplete = _ => { };
                _onStart = _ => true;
                _onFailed = _ => { };
                _onTick = _ => true;
            }
            else
            {
                _onComplete = handlers.onComplete ?? (_ => { });
                _onStart = handlers.onStart ?? (_ => true);
                _onFailed = handlers.onFailed ?? (_ => { });
                _onTick = handlers.onTick ?? (_ => true);
            }
        }

        /// <inheritdoc />
        bool IGesture<IActor>.TryStart(IActor actor, IPerformance<IActor> performance)
        {
            PropCount requiredProp = _getProp?.Invoke() ?? default;
            Pose requiredPose = _getPose?.Invoke() ?? default;
            if (IsPoseRequired && !_comparer.Equals(actor.Pose, requiredPose))
            {
                Phase = IGesture.RunPhase.Failed;
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Gesture failed to start; The actor {actor.Name}'s pose did not match the gesture's required pose;\nDistance error is {Vector3.Distance(actor.Pose.position, requiredPose.position):f2};\nAlignment error is {Vector3.Angle(actor.Pose.forward, requiredPose.forward):f2}°;\n{requiredPose} is the gesture's required pose;\n{actor.Pose} is the actor's pose");
                _onFailed?.Invoke((actor, performance, this));
                return false;
            }

            if (IsPropRequired && actor.Inventory.GetAvailableCount(requiredProp.Identity) < requiredProp.Count)
            {
                Phase = IGesture.RunPhase.Failed;
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Gesture failed to start; A required token was not in actor {actor.Name}'s inventory ({requiredProp.Identity?.Name})");
                _onFailed?.Invoke((actor, performance, this));
                return false;
            }

            if (requiredProp != default)
            {
                actor.Inventory.TryTakeImmediately(requiredProp.Identity, requiredProp.Count);
            }

            Phase = IGesture.RunPhase.Running;
            _timeStarted = Timing.Source.Time;
            bool isStarted = _onStart.Invoke((actor, performance, this));
            D.raw(new Shape.Circle(actor.Pose.position, Vector3.up, .3f), Color.yellow, .05f);

            if (!isStarted)
            {
                Phase = IGesture.RunPhase.Failed;
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Gesture failed to start due to an error in the start delegate.");
                _onFailed?.Invoke((actor, performance, this));
                return false;
            }

            if (IsPoseRequired)
            {
                D.raw(new Shape.Circle(Pose.position, Vector3.up, .3f), Color.yellow, .05f);
                D.raw(new Shape.Line(Pose.position, actor.Pose.position), Color.yellow, .5f);
            }

            return true;
        }

        /// <inheritdoc />
        void IGesture<IActor>.Abort(IActor actor, IPerformance<IActor> performance)
        {
            Phase = IGesture.RunPhase.Failed;
            /*
            Debug.Log ( actor != null
                ? $"[{GetType ().GetNiceName ()}] Gesture aborted while run by actor '{actor.Name}' in performance {performance}"
                : $"[{GetType ().GetNiceName ()}] Gesture aborted in performance '{performance}'" );
            */
            _onFailed?.Invoke((actor, performance, this));
        }

        /// <inheritdoc />
        bool IGesture<IActor>.Tick(IActor actor, IPerformance<IActor> performance)
        {
            if (Phase != IGesture.RunPhase.Running && !((IGesture<IActor>)this).TryStart(actor, performance))
            {
                return false;
            }

            if (_canTick?.Invoke((actor, performance, this)) ?? false)
            {
                _timeLastTick = Timing.Source.Time;
                Phase = IGesture.RunPhase.Running;
                _onTick?.Invoke((actor, performance, this));

                D.raw(new Shape.Circle(actor.Pose.position, Vector3.up, .35f), Color.yellow, .05f);
                if (IsPoseRequired)
                {
                    D.raw(new Shape.Circle(Pose.position, Vector3.up, .35f), Color.yellow, .05f);
                    D.raw(new Shape.Line(Pose.position, actor.Pose.position), Color.yellow, .5f);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public Sprite Sprite { get; set; }

        /// <inheritdoc />
        public bool CheckPose(Pose pose) => _comparer.Equals(Pose, pose);

        /// <inheritdoc />
        bool IGesture<IActor>.TryComplete(IActor actor, IPerformance<IActor> performance)
        {
            if (IsPoseRequired && !_comparer.Equals(actor.Pose, Pose))
            {
                Phase = IGesture.RunPhase.Failed;
                Debug.LogWarning(
                    $"[{GetType().GetNiceName()}] Actor {actor.Name} failed to complete gesture; poses did not match");
                _onFailed?.Invoke((actor, performance, this));
                return false;
            }

            if (!(_canComplete?.Invoke((actor, performance, this)) ?? false))
            {
                // Debug.Log ( $"[{GetType ().GetNiceName ()}] Actor {actor.Name} could not complete gesture yet; completion condition was not met" );
                return false;
            }

            Phase = IGesture.RunPhase.Complete;
            _onComplete?.Invoke((actor, performance, this));

            D.raw(new Shape.Circle(actor.Pose.position, Vector3.up, .4f), Color.yellow, .5f);
            if (IsPoseRequired)
            {
                D.raw(new Shape.Circle(Pose.position, Vector3.up, .4f), Color.yellow, .5f);
                D.raw(new Shape.Line(Pose.position, actor.Pose.position), Color.yellow, .5f);
            }

            return true;
        }

        public bool LogDebugMessages { get; set; }
    }
}