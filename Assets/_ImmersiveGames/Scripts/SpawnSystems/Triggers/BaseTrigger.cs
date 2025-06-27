using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public abstract class BaseTrigger : ISpawnTrigger
    {
        private readonly bool _continuous;
        private readonly float _spawnInterval;
        private readonly float _rearmDelay;
        private readonly int _maxSpawns;
        protected bool isActive;
        protected SpawnPoint spawnPoint;
        private float _timer;
        private float _rearmTimer;
        protected bool isRearmed;
        private int _spawnCount;
        private bool _isFirstSpawn;

        protected BaseTrigger(EnhancedTriggerData data)
        {
            _continuous = data.GetProperty("continuous", false);
            _spawnInterval = Mathf.Max(data.GetProperty("spawnInterval", 1.0f), 0.01f);
            _rearmDelay = Mathf.Max(data.GetProperty("rearmDelay", 0.5f), 0f);
            _maxSpawns = Mathf.Max(data.GetProperty("maxSpawns", -1), -1);
            isActive = false;
            isRearmed = true;
            _timer = 0f;
            _rearmTimer = 0f;
            _spawnCount = 0;
            _isFirstSpawn = true;
        }

        public void Initialize(SpawnPoint spawnPointRef)
        {
            spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;

            if (_rearmTimer > 0f)
            {
                _rearmTimer -= Time.deltaTime;
                if (_rearmTimer <= 0f)
                    isRearmed = true;
            }

            if (!isActive || !isRearmed)
                return false;

            bool canTrigger = CheckTriggerBase(out bool isComplete);
            if (!canTrigger)
                return false;

            bool triggerResult = OnCheckTrigger(out triggerPosition, out sourceObject);
            if (triggerResult)
            {
                _spawnCount++;
                if (_isFirstSpawn)
                {
                    _isFirstSpawn = false;
                    _timer = _spawnInterval;
                }
                if (!_continuous || (_maxSpawns >= 0 && _spawnCount >= _maxSpawns))
                    SetActive(false);
                return true;
            }

            return false;
        }

        protected virtual bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;
            return false;
        }

        private bool CheckTriggerBase(out bool isComplete)
        {
            isComplete = false;
            if (!_continuous)
            {
                isComplete = true;
                return true;
            }

            if (_isFirstSpawn)
                return true;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _spawnInterval;
                return true;
            }

            return false;
        }

        public virtual void ReArm()
        {
            isRearmed = true;
            _rearmTimer = 0f;
            _timer = _spawnInterval;
            _spawnCount = 0;
            _isFirstSpawn = true;
        }

        public void SetActive(bool active)
        {
            if (isActive == active)
                return;

            isActive = active;
            if (!isActive)
            {
                OnDeactivate();
                isRearmed = false;
                _rearmTimer = _rearmDelay;
                _timer = _spawnInterval;
                _spawnCount = 0;
                _isFirstSpawn = true;
            }
            else
            {
                _isFirstSpawn = true;
            }
        }

        protected virtual void OnDeactivate()
        {
        }

        public void Reset()
        {
            isActive = false;
            isRearmed = true;
            _timer = 0f;
            _rearmTimer = 0f;
            _spawnCount = 0;
            _isFirstSpawn = true;
        }

        public bool IsActive => isActive;
    }
}