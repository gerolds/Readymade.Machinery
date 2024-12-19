using System.Collections.Generic;
using Readymade.Machinery.Acting;
using UnityEngine;

namespace App.Core.Acting
{
    public class SpawnerSystem : MonoBehaviour
    {
        private HashSet<Spawner> _spawners = new();
        private Dictionary<SoProp, int> _groups = new();

        public void Register(Spawner spawner)
        {
            _spawners.Add(spawner);
            if (spawner.SpawnerGroup)
            {
                _groups.TryAdd(spawner.SpawnerGroup, 0);
            }
        }

        public void Unregister(Spawner spawner)
        {
            _spawners.Remove(spawner);
        }

        public int ClaimSpawnEvent(SoProp group, Vector3 position) => _groups[group]++;

        public int ReleaseSpawnEvent(SoProp group, Vector3 position) => _groups[group]--;

        public int GetCount(SoProp group) => _groups[group];
    }
}