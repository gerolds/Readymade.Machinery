#if ASTAR_PATHFINDING
using System;
using Pathfinding;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    public class Seek : TaskBase
    {
        /// <inheritdoc cref="Destination"/>
        private Func<Vector3> _destination;

        /// <summary>
        /// The <see cref="IAstarAI"/> used for pathfinding and steering 
        /// </summary>
        private readonly IAstarAI _seeker;

        /// <summary>
        /// The agent that owns this behaviour.
        /// </summary> 
        public GameObject Agent { get; }

        /// <summary>
        /// The position the <see cref="Agent"/> wants to reach.
        /// </summary>
        public Vector3 Destination => _destination.Invoke ();

        /// <summary>
        /// Create a task that uses <see cref="IAstarAI"/> on <paramref name="agent"/> to move it to <paramref name="destination"/>
        /// </summary>
        /// <param name="maxSpeed"></param>
        /// <param name="name">A description of this task instance's purpose.</param>
        /// <param name="agent">The agent that owns this task. Expected to have a component which implements <see cref="IAstarAI"/>.</param>
        /// <param name="destination">The position the <see cref="Agent"/> wants to reach.</param>
        public Seek (
            string name,
            GameObject agent,
            Func<Vector3> destination,
            float maxSpeed = 2f
        ) : base (
            name
        ) {
            Agent = agent;
            _seeker = agent.GetComponent<IAstarAI> ();
            _seeker.maxSpeed = maxSpeed;
            _destination = destination;
        }

        /// <inheritdoc />
        public Seek (
            GameObject agent,
            Func<Vector3> destination,
            float maxSpeed = 2f
        ) : this (
            default,
            agent,
            destination,
            maxSpeed
        ) {
            Agent = agent;
            _seeker = agent.GetComponent<IAstarAI> ();
            _seeker.maxSpeed = maxSpeed;
            _destination = destination;
        }

        /// <inheritdoc />
        protected override void OnAborted () {
            _seeker.canMove = false;
            _seeker.canSearch = false;
        }

        /// <inheritdoc />
        protected override void OnResumed () {
            _seeker.isStopped = false;
            _seeker.canSearch = true;
        }

        /// <inheritdoc />
        protected override void OnSuspended () {
            _seeker.isStopped = true;
            _seeker.canSearch = false;
        }

        /// <inheritdoc />
        protected override TaskState OnTick () {
            // pathfinding got cancelled for some reason
            if ( !_seeker.canMove ) {
                Debug.Log ( $"[{nameof ( Seek )}] FAILURE Cannot move", _seeker as Component );
                return TaskState.Failure;
            }

            // waiting for a path
            if ( _seeker.pathPending ) {
                return TaskState.Running;
            }

            // we didn't find a path
            if ( !_seeker.pathPending && !_seeker.hasPath ) {
                Debug.Log ( $"[{nameof ( Seek )}] FAILURE Could not find a path", _seeker as Component );
                return TaskState.Failure;
            }

            // we're on our way to the destination
            if ( _seeker.hasPath && !_seeker.reachedDestination ) {
                return TaskState.Running;
            } else if ( _seeker.reachedDestination ) {
                //Debug.Log ( $"[{nameof ( Seek )}] SUCCESS Reached destination", _seeker as Component );
                return TaskState.Success;
            }

            // unexpected situations result in failure
            Debug.Log ( $"[{nameof ( Seek )}] FAILURE Unexpected" );
            return TaskState.Failure;
        }

        /// <inheritdoc />
        protected override void OnStarted () {
            Debug.Assert ( _destination != null, "ASSERTION FAILED: _destination != Vector3.zero", _seeker as Component );
            Debug.Assert ( _seeker != null, "ASSERTION FAILED: _seeker != null" , _seeker as Component);
            _seeker.canMove = true;
            _seeker.canSearch = true;
            _seeker.destination = Destination;
            _seeker.SearchPath ();
        }

        /// <inheritdoc />
        protected override void OnReset () {
            _seeker.canMove = false;
            _seeker.canSearch = false;
        }

        /// <inheritdoc />
        protected override void OnStopped ( TaskState state ) {
            _seeker.canMove = false;
            _seeker.canSearch = false;
            _seeker.destination = _seeker.position;
        }
    }
}
#endif