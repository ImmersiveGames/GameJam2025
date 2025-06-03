using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public abstract class SpawnTriggerSo : ScriptableObject
    {
        protected SpawnPoint spawnPoint;

        public virtual void Initialize(SpawnPoint spawnPointRef)
        {
            spawnPoint = spawnPointRef;
        }

        public abstract void CheckTrigger(Vector3 origin, SpawnData data);
        public virtual void SetActive(bool active) { }
        public virtual void Reset() { }
    }
}