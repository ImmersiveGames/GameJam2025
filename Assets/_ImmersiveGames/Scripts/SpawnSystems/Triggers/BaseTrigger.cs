using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public abstract class BaseTrigger : ISpawnTrigger
    {
        protected bool isActive;
        protected SpawnPoint spawnPoint;
        private readonly float _spawnInterval;
        protected readonly int maxSpawns;
        protected float timer;
        protected int spawnCount;

        protected BaseTrigger(EnhancedTriggerData data)
        {
            _spawnInterval = Mathf.Max(data.GetProperty("spawnInterval", 1.0f), 0.01f);
            maxSpawns = Mathf.Max(data.GetProperty("maxSpawns", -1), -1);
            isActive = true;
            timer = _spawnInterval;
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
                timer = _spawnInterval;
                spawnCount = 0;
            }
        }

        public virtual void Reset()
        {
            timer = _spawnInterval;
            spawnCount = 0;
            isActive = true;
        }

        public virtual void OnDisable() { }

        public bool IsActive => isActive;
    }
}