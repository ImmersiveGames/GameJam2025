using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public abstract class BaseTrigger : ISpawnTrigger
    {
        protected bool isActive;
        protected SpawnPoint spawnPoint;
        protected float spawnInterval;
        protected int maxSpawns;
        protected float timer;
        protected int spawnCount;

        protected BaseTrigger(EnhancedTriggerData data)
        {
            spawnInterval = Mathf.Max(data.GetProperty("spawnInterval", 1.0f), 0.01f);
            maxSpawns = Mathf.Max(data.GetProperty("maxSpawns", -1), -1);
            isActive = true;
            timer = spawnInterval;
            spawnCount = 0;
        }

        public virtual void Initialize(SpawnPoint spawnPointRef)
        {
            spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
        }

        public abstract bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);

        public virtual void SetActive(bool active)
        {
            if (isActive == active) return;
            isActive = active;
            if (!active)
            {
                timer = spawnInterval;
                spawnCount = 0;
            }
        }

        public virtual void Reset()
        {
            timer = spawnInterval;
            spawnCount = 0;
            isActive = true;
        }

        public virtual void OnDisable() { }

        public bool IsActive => isActive;
    }
}