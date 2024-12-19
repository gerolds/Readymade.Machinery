using System;
using System.Collections;
using Readymade.Machinery.Shared;
using Readymade.Persistence;
using UnityEngine;

// This file is not commented because it is a duplicate of the code part in the README which deliberately omits comments.

namespace Readymade.Machinery.Acting.Examples
{
    /*
    A very simple example of an Actor going to fix a broken water pipe. Illustrates the fundamental usage and API of
    the acting system. Note the callback whenever the enumerator moves on to the next gesture. Allowing the performing
    actor to suspend iterating the enumerator until the required pose is reached.
    */

    public static class Planner
    {
        public static readonly Director Director = new(default);
    }

    public enum Role
    {
        Default
    }

    public class Actor : MonoBehaviour, IActor
    {
        private enum Mode
        {
            Perform,
            Move
        }

        private IEnumerator _enum;
        private IPerformance<IActor> _performance;
        private Mode _updateMode;
        private Pose _targetPose;
        [SerializeField] private Sprite portrait;

        Pose IActor.Pose => new(transform.position, transform.rotation);

        public IInventory<SoProp> Inventory { get; } = new Inventory();
        public IEquipment<SoSlot, SoProp> Equipment => new Equipment<SoSlot, SoProp>(default);

        /// <inheritdoc />
        public string Name => name;

        public IPropConsumer Consumer => default;
        public Sprite Portrait => portrait;

        public void OnFx(ActorFx fx)
        {
        }

        private void Update()
        {
            if (_performance == null)
            {
                if (Planner.Director.TryClaim(this, out _performance))
                {
                    // when the enumerator switches to the next gesture, we pause updating the enumerator and instead
                    // move towards the required pose.
                    _performance.NextGesture += NextGestureHandler;

                    // after registering the callback above, we can now generate the enumerator and start updating it.
                    _updateMode = Mode.Perform;
                    _enum = _performance.RunAsync(this);

                    void NextGestureHandler(Performance performance, IActor actor)
                    {
                        _updateMode = Mode.Move;
                        _targetPose = performance.CurrentGesture.Pose;
                    }
                }
            }

            switch (_updateMode)
            {
                case Mode.Perform:
                    if (_enum.MoveNext())
                    {
                    }
                    else
                    {
                        _enum = default;
                        _performance = default;
                    }

                    break;
                case Mode.Move:
                    MoveTo(_targetPose, Timing.Source.DeltaTime);

                    // when we've reached the next pose, we resume updating the enumerator.
                    if (Planner.Director.PoseComparer.Equals(_targetPose, PoseExtensions.PoseFrom(transform)))
                    {
                        _updateMode = Mode.Perform;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveTo(Pose pose, float deltaTime)
        {
            transform.position = Vector3.MoveTowards(transform.position, pose.position, 1f * deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, pose.rotation, 10f * deltaTime);
        }

        public Guid EntityID => GetComponentInParent<PackIdentity>(true)?.EntityID ?? Guid.Empty;
        public GameObject GetObject() => gameObject;
    }

    public static class WaterSystem
    {
        private class Pipe
        {
            public string Name;
            public Pose Pose;
            public int Durability;
        }

        private static readonly Pipe[] s_pipes =
        {
            new()
            {
                Name = "pipe one",
                Pose = new Pose(Vector3.one, Quaternion.identity),
                Durability = 100,
            },
            new()
            {
                Name = "pipe zero",
                Pose = new Pose(Vector3.one, Quaternion.identity),
                Durability = 0
            },
        };

        public static void WaterSystemTick()
        {
            foreach (Pipe pipe in s_pipes)
            {
                if (pipe.Durability < 100)
                {
                    ScheduleRepair(pipe);
                }
            }
        }

        private static void ScheduleRepair(Pipe pipe)
        {
            Performance repairPerformance = new(
                new FunGesture(
                    duration: 1f,
                    pose: pipe.Pose,
                    prop: IGesture.NoProp,
                    handlers: (
                        onStart: _ =>
                        {
                            SuspendPump();
                            return true;
                        },
                        onTick: default,
                        onComplete: _ =>
                        {
                            Repair(pipe, 100);
                            ResumePump();
                        },
                        onFailed: args =>
                        {
                            Debug.Log(
                                $"Actor {args.actor} has failed gesture {args.gesture} as part of performance {args.performance} in {args.gesture.Pose}");
                        })
                )
            );
            Planner.Director.Schedule(key: pipe.GetHashCode(), performance: repairPerformance);
        }

        private static void SuspendPump() => Debug.Log($"Pump suspended");

        private static void ResumePump() => Debug.Log($"Pump resumed");

        private static void Repair(Pipe pipe, int amount) => pipe.Durability += amount;
    }
}