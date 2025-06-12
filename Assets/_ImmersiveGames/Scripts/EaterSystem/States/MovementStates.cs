using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal class WanderingState : IState
    {
        private float _timer;
        private Vector3 _direction;
        private Rect _gameArea;
        private readonly EaterConfigSo _config;
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        public WanderingState(EaterMovement eaterMovement, EaterConfigSo config)
        {
            _config = config;
            _gameArea = GameManager.Instance.GameConfig.gameArea;
            _eaterMovement = eaterMovement ?? throw new ArgumentNullException(nameof(eaterMovement), "EaterMovement cannot be null.");
            _transform = eaterMovement.transform;
        }
        public void Update()
        {
            _timer += Time.deltaTime;
            float currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            if (_timer >= _config.DirectionChangeInterval)
            {
                ChooseNewDirection();
                _timer = 0f;
            }

            var targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);

            float moveAmount = currentSpeed * Time.deltaTime;
            _transform.Translate(Vector3.forward * moveAmount, Space.Self);
            KeepWithinBounds();
        }
        public void FixedUpdate() { }
        public void OnEnter()
        {
            _timer = 0;
            _eaterMovement.IsOrbiting = false;
            ChooseNewDirection();
            DebugUtility.Log<WanderingState>($"Entering Wandering State for EaterMovement: {nameof(EaterMovement)}");
        }
        public void OnExit() { }
        private void ChooseNewDirection()
        {
            _direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        }
        private void KeepWithinBounds()
        {
            var pos = _transform.position;
            pos.x = Mathf.Clamp(pos.x, _gameArea.xMin, _gameArea.xMax);
            pos.z = Mathf.Clamp(pos.z, _gameArea.yMin, _gameArea.yMax);
            _transform.position = pos;
        }
    }

    internal class ChasingState : IState
    {
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;
        private float _currentSpeed;
        public ChasingState(EaterMovement eaterMovement, EaterConfigSo config)
        {
            _eaterMovement = eaterMovement;
            _transform = eaterMovement.transform;
            _config = config;
        }
        public void Update()
        {
            var target = _eaterMovement.TargetTransform;
            Vector3 direction = (target.position - _transform.position).normalized;

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);

                // ❗ Força alinhamento apenas no plano XZ (top-down)
                _transform.eulerAngles = new Vector3(0f, _transform.eulerAngles.y, 0f);
            }
            float moveAmount = _currentSpeed * Time.deltaTime;
            _transform.Translate(direction * moveAmount, Space.World);
        }
        public void OnEnter()
        {
            var targetMotion = _eaterMovement.TargetTransform.GetComponent<PlanetMotion>();
            _currentSpeed = targetMotion.GetOrbitSpeed() + _config.MinSpeed * 5f;
            DebugUtility.Log<ChasingState>($"Entering Chasing State for EaterMovement: {nameof(EaterMovement)}");
        }
    }

    internal class OrbitingState : IState
    {
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;

        private float _angle;
        private float _angularSpeed;
        private float _radius;

        public OrbitingState(EaterMovement eaterMovement, EaterConfigSo config)
        {
            _eaterMovement = eaterMovement;
            _transform = eaterMovement.transform;
            _config = config;

            float turnAroundTime = 5f;
            _angularSpeed = 360f / turnAroundTime;
        }

        public void OnEnter()
        {
            _eaterMovement.IsOrbiting = true;

            var target = _eaterMovement.TargetTransform;
            if (target != null)
            {
                _angle = GetInitialAngleXZ(target.position);

                // 🧠 Calcula raio com base na escala do modelo real
                _radius = CalculateOrbitRadius(target);
            }

            DebugUtility.Log<OrbitingState>("Entered Orbiting State");
        }

        public void Update()
        {
            var target = _eaterMovement.TargetTransform;
            if (target == null) return;

            _angle += _angularSpeed * Time.deltaTime;
            _angle %= 360f;

            float rad = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * _radius;
            Vector3 targetPos = target.position + offset;

            _transform.position = targetPos;

            Vector3 lookPos = new Vector3(target.position.x, _transform.position.y, target.position.z);
            _transform.LookAt(lookPos);
        }

        public void OnExit()
        {
            _eaterMovement.IsOrbiting = false;
            DebugUtility.Log<OrbitingState>("Exited Orbiting State");
        }

        public void SetTurnAroundTimer(float tempo)
        {
            var turnAroundTimer = Mathf.Max(tempo, 0.01f);
            _angularSpeed = 360f / turnAroundTimer;
        }

        private float GetInitialAngleXZ(Vector3 centerPosition)
        {
            Vector3 offset = _transform.position - centerPosition;
            offset.y = 0f;
            return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }

        private float CalculateOrbitRadius(Transform target)
        {
            const float orbitOffset = 2f;

            // Procura o ModelRoot
            var modelRoot = target.GetComponentInChildren<ModelRoot>();
            if (modelRoot == null)
            {
                Debug.LogWarning("ModelRoot não encontrado no alvo!");
                return 3f;
            }

            // Pega o primeiro filho do ModelRoot
            if (modelRoot.transform.childCount == 0)
            {
                Debug.LogWarning("ModelRoot não tem filhos!");
                return 3f;
            }

            Transform model = modelRoot.transform.GetChild(0);

            // Tenta pegar qualquer collider
            Collider collider = model.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("Collider não encontrado no modelo do alvo!");
                return 3f;
            }

            // Usa bounds para calcular raio, considerando escala
            float maxRadius = collider.bounds.extents.magnitude;

            float finalRadius = maxRadius + orbitOffset;

            DebugUtility.Log<OrbitingState>($"Calculated radius: {finalRadius} (collider: {collider.GetType().Name}, extents: {collider.bounds.extents})");

            return finalRadius;
        }

    }


}