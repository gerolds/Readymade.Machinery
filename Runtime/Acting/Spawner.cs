using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using App.Core.Utils;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Pathfinding;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Machinery.Acting;
using Readymade.Utils.Patterns;
using Readymade.Utils.Pooling;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Vertx.Debugging;
using Random = UnityEngine.Random;

namespace App.Core.Acting
{
    /// <summary>
    /// Simple spawner for <see cref="Actor"/> instances.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [BoxGroup("Prefab")]
        [Required]
        [SerializeField]
        private Actor actor;

        [BoxGroup("Timing")]
        [Tooltip("The min. interval at which to spawn units.")]
        [MinValue(1f)]
        [SerializeField]
        private float interval = 1;

        [BoxGroup("Timing")]
        [FormerlySerializedAs("count")]
        [Tooltip("The max number of live entities at any given time.")]
        [MinValue(1)]
        [SerializeField]
        private int maxAliveCount = 1;

        [BoxGroup("Timing")]
        [Tooltip("Whether to use the system count or a local count.")]
        [SerializeField]
        private bool useSystemCount;

        [ShowIf(nameof(useSystemCount))]
        [BoxGroup("Timing")]
        [SerializeField]
        [ValidateInput("@!useSystemCount || spawnerGroup != null",
            "System count is enabled, therefore a spawner group must be declared.")]
        private SoProp spawnerGroup;

        [BoxGroup("Distribution")]
        [Tooltip("The radius in which to spawn units.")]
        [MinValue(0)]
        [SerializeField]
        private int radius = 5;

        [BoxGroup("Distribution")]
        [SerializeField]
        private bool useAstarPath;

        [BoxGroup("Distribution")]
        [ShowIf(nameof(useAstarPath))]
        [SerializeField]
        [MinValue(0)]
        private float clearance;

        [BoxGroup("Projection")]
        [ShowIf(nameof(useAstarPath))]
        [SerializeField]
        private GraphMask graphMask;

        [BoxGroup("Projection")]
        [SerializeField]
        private bool projectSpawnPosition = true;

        [BoxGroup("Projection")]
        [ShowIf(nameof(projectSpawnPosition))]
        [SerializeField]
        private LayerMask mask;


        [BoxGroup("Projection")]
        [ShowIf(nameof(projectSpawnPosition))]
        [SerializeField]
        [Tooltip("Index of a " + nameof(IRaycastableGraph) + " in this scenes " + nameof(AstarPath) + " instance.")]
        private int graph = 0;

        [BoxGroup("Fx")] [SerializeField] private PoolableObject spawnFx;
        [BoxGroup("Events")] [SerializeField] private UnityEvent onMaxCountReached;
        [BoxGroup("Events")] [SerializeField] private UnityEvent onCanSpawn;
        [BoxGroup("Events")] [SerializeField] private UnityEvent onSpawned;
        private readonly List<Actor> _spawns = new();


        private SpawnerSystem _system;
        private List<Vector3> _spawnPoints = new();


        public string GraphName
        {
            get
            {
                if (AstarPath.active.graphs.Length > graph)
                {
                    if (AstarPath.active.graphs[graph] is IRaycastableGraph)
                    {
                        return AstarPath.active.graphs[graph].name;
                    }
                    else
                    {
                        return "Not a " + nameof(IRaycastableGraph);
                    }
                }
                else
                {
                    return "Undefined";
                }
            }
        }

        public SoProp SpawnerGroup => spawnerGroup;
        public int MaxCount => maxAliveCount;
        public int Count => _spawns.Count;

        public IEnumerable<Actor> Spawns => _spawns;

        private void Start()
        {
            _system = Services.Get<SpawnerSystem>();
            _system.Register(this);
            SpawnAsync(destroyCancellationToken).Forget();
            AstarPath.OnPostScan += ResetSpawnPoints;
            ResetSpawnPoints(AstarPath.active);
        }

        [Button]
        private void ResetSpawnPoints() => ResetSpawnPoints(AstarPath.active);

        private void ResetSpawnPoints(AstarPath astarPath)
        {
            if (!useAstarPath)
            {
                return;
            }

            if (!AstarPath.active || AstarPath.active.graphs.Length <= graph)
            {
                Debug.LogWarning("Could not find spawn points based on valid AstarPath.", this);
                return;
            }

            if (_spawnPoints.Count != MaxCount * 2)
            {
                _spawnPoints.Clear();
                List<Vector3> spiral = PathUtilities.GetSpiralPoints(MaxCount * 2, clearance);
                _spawnPoints.AddRange(spiral.Select(it => transform.position + it));
            }

            IRaycastableGraph raycastableGraph = astarPath.graphs[graph] as IRaycastableGraph;
            PathUtilities.GetPointsAroundPointWorld(
                transform.position,
                raycastableGraph,
                _spawnPoints,
                radius,
                clearance
            );
        }

        private void OnDestroy()
        {
            if (_system)
            {
                _system.Unregister(this);
            }

            AstarPath.OnPostScan -= ResetSpawnPoints;
        }

        private async UniTaskVoid SpawnAsync(CancellationToken ct = default)
        {
            bool isMaxCountReached = false;
            while (!ct.IsCancellationRequested)
            {
                bool shouldSpawn = CanSpawn;

                if (shouldSpawn)
                {
                    if (isMaxCountReached)
                    {
                        isMaxCountReached = false;
                        onCanSpawn.Invoke();
                    }

                    Vector3 point;
                    if (_spawnPoints.Count > 0)
                    {
                        point = _spawnPoints[Random.Range(0, _spawnPoints.Count)];
                    }
                    else
                    {
                        point = transform.position + Random.insideUnitCircle.ToXZ() * radius;
                    }

                    if (projectSpawnPosition)
                    {
                        bool isHit = DrawPhysics.Raycast(
                            origin: point,
                            direction: Vector3.down,
                            hitInfo: out RaycastHit hit,
                            maxDistance: radius * 4f,
                            layerMask: mask,
                            queryTriggerInteraction: QueryTriggerInteraction.Ignore
                        );
                        if (isHit)
                        {
                            RunLifeAsync(hit.point, ct).Forget();
                        }
                    }
                    else
                    {
                        RunLifeAsync(point, ct).Forget();
                    }
                }
                else
                {
                    if (!CanSpawn && !isMaxCountReached)
                    {
                        isMaxCountReached = true;
                        onMaxCountReached.Invoke();
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);
            }
        }

        public bool CanSpawn =>
            useSystemCount ? _system.GetCount(SpawnerGroup) < maxAliveCount : _spawns.Count < maxAliveCount;


        async UniTaskVoid RunLifeAsync(Vector3 pos, CancellationToken ct)
        {
            if (spawnFx)
            {
                spawnFx.TryGetInstance(pos, Quaternion.identity, null, out var fx, true);
            }

            _system.ClaimSpawnEvent(SpawnerGroup, transform.position);
            Actor instance = Instantiate(actor, pos, Quaternion.AngleAxis(Random.Range(-180f, 180f), Vector3.up));
            _spawns.Add(instance);
            onSpawned.Invoke();
            Debug.Log($"[{nameof(Spawner)}] Spawned {instance.name} at {pos:F0}.", this);
            await instance.OnDestroyAsync().AttachExternalCancellation(ct);
            if (useSystemCount && _system && SpawnerGroup)
            {
                _system.ReleaseSpawnEvent(SpawnerGroup, transform.position);
            }

            _spawns.Remove(instance);
        }

        private void OnDrawGizmosSelected()
        {
            D.raw(new Shape.Ray(transform.position + Vector3.up * radius, Vector3.down * radius * 2f));
            if (actor)
            {
                D.raw(new Shape.Text(transform.position, actor.name));
            }

            foreach (var point in _spawnPoints)
            {
                D.raw(new Shape.Point(point, 1f), Color.green, .5f);
                D.raw(new Shape.Circle(point, Vector3.up, clearance * 0.5f), Color.green, .5f);
            }

            D.raw(new Shape.Circle(transform.position, Vector3.up, radius), Color.green, .5f);
        }
    }
}