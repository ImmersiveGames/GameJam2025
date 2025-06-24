using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public abstract class BaseTrigger : ISpawnTrigger
    {
        protected bool _continuous;
        protected float _spawnInterval;
        protected float _rearmDelay;
        protected int _maxSpawns;
        protected bool _isActive;
        protected SpawnPoint _spawnPoint;
        protected float _timer;
        protected float _rearmTimer;
        protected bool _isRearmed;
        protected int _spawnCount;
        protected bool _isFirstSpawn; // Nova variável para controlar o primeiro spawn

        protected BaseTrigger(EnhancedTriggerData data)
        {
            _continuous = data.GetProperty("continuous", false);
            _spawnInterval = Mathf.Max(data.GetProperty("spawnInterval", 1.0f), 0.01f);
            _rearmDelay = Mathf.Max(data.GetProperty("rearmDelay", 0.5f), 0f);
            _maxSpawns = Mathf.Max(data.GetProperty("maxSpawns", -1), -1);
            _isActive = false;
            _isRearmed = true;
            _timer = 0f; // Inicializa em 0 para permitir primeiro spawn imediato
            _rearmTimer = 0f;
            _spawnCount = 0;
            _isFirstSpawn = true; // Primeiro spawn será imediato
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;

            if (_rearmTimer > 0f)
            {
                _rearmTimer -= Time.deltaTime;
                if (_rearmTimer <= 0f)
                    _isRearmed = true;
            }

            if (!_isActive || !_isRearmed)
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
                    _timer = _spawnInterval; // Inicializa o timer após o primeiro spawn
                }
                if (!_continuous || (_maxSpawns >= 0 && _spawnCount >= _maxSpawns))
                    SetActive(false);
                return true;
            }

            return false;
        }

        protected virtual bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            return false;
        }

        protected bool CheckTriggerBase(out bool isComplete)
        {
            isComplete = false;
            if (!_continuous)
            {
                isComplete = true;
                return true;
            }

            if (_isFirstSpawn) // Primeiro spawn imediato
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
            _isRearmed = true;
            _rearmTimer = 0f;
            _timer = _spawnInterval;
            _spawnCount = 0;
            _isFirstSpawn = true; // Reseta para permitir primeiro spawn imediato
        }

        public void SetActive(bool active)
        {
            if (_isActive == active)
                return;

            _isActive = active;
            if (!_isActive)
            {
                OnDeactivate();
                _isRearmed = false;
                _rearmTimer = _rearmDelay;
                _timer = _spawnInterval;
                _spawnCount = 0;
                _isFirstSpawn = true; // Reseta para próximo spawn imediato
            }
            else
            {
                _isFirstSpawn = true; // Ativação permite primeiro spawn imediato
            }
        }

        protected virtual void OnDeactivate()
        {
        }

        public void Reset()
        {
            _isActive = false;
            _isRearmed = true;
            _timer = 0f; // Reseta para permitir primeiro spawn imediato
            _rearmTimer = 0f;
            _spawnCount = 0;
            _isFirstSpawn = true;
        }

        public bool IsActive => _isActive;
    }
}