using System;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class Planets : DestructibleObject
    {
        [SerializeField, Tooltip("Ativar logs para depuração")]
        private bool debugMode;
        private PlanetResources _resource;
        private PlanetOrbit _orbitController;
        private GameObject _skinInstance;
        private Tween _rotationTween;
        
        private bool _isActive = true;
        public bool IsActive => _isActive && IsAlive;

        public PlanetResources Resource => _resource;

        public event Action<PlanetData> OnPlanetCreated;
        public event Action<PlanetData> OnPlanetDestroyed;

        public void SetPlanetData(DestructibleObjectSo data)
        {
            if (!(data is PlanetData))
            {
                DebugUtility.LogError<Planets>($"Tentativa de configurar um tipo inválido ({data?.GetType().Name}) como PlanetData em {gameObject.name}.", this);
                return;
            }
            destructibleObject = data;
            if (debugMode)
            {
                DebugUtility.LogVerbose<Planets>($"PlanetData setado para {gameObject.name}: {data.name}", "cyan", this);
            }
        }

        public void SetResource(PlanetResources newResource)
        {
            _resource = newResource;
        }

        public void StartOrbit(Transform orbitCenter)
        {
            if (_orbitController == null)
            {
                _orbitController = gameObject.AddComponent<PlanetOrbit>();
            }
            var planetData = (PlanetData)destructibleObject;
            if (planetData == null)
            {
                DebugUtility.LogError<Planets>($"PlanetData não definido em {gameObject.name} para iniciar órbita.", this);
                return;
            }
            _orbitController.Initialize(orbitCenter, planetData.minOrbitSpeed, planetData.maxOrbitSpeed, planetData.orbitClockwise);
        }

        public override void Initialize()
        {
            if (destructibleObject == null || !(destructibleObject is PlanetData))
            {
                DebugUtility.LogError<Planets>($"PlanetData não está definido ou é de tipo inválido em {gameObject.name}.", this);
                return;
            }

            base.Initialize();
            SetSkinPlanet();
            var planetData = (PlanetData)destructibleObject;
            OnPlanetCreated?.Invoke(planetData);
            if (debugMode)
            {
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} inicializado, OnPlanetCreated disparado.", "green", this);
            }
        }

        private Transform GetOrCreateModelRoot()
        {
            var modelRoot = GetComponentInChildren<ModelRoot>()?.transform;
            if (modelRoot) return modelRoot;
            var rootObj = new GameObject("ModelRoot");
            var modelRootTr = rootObj.transform;
            modelRootTr.SetParent(transform, false);
            rootObj.AddComponent<ModelRoot>();

            // Adicionar Rigidbody (isKinematic = true)
            var rb = rootObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Adicionar SphereCollider (isTrigger = true)
            var planetData = (PlanetData)destructibleObject;
            var collider = rootObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = planetData.size * 0.5f; // Metade do tamanho do planeta

            return modelRootTr;
        }

        private void SetSkinPlanet()
        {
            var planetData = (PlanetData)destructibleObject;
            if (planetData == null || !planetData.modelPrefab)
            {
                DebugUtility.LogError<Planets>($"ModelPrefab ou PlanetData não definido em {gameObject.name}.", this);
                return;
            }

            var modelRoot = GetOrCreateModelRoot();
            if (_skinInstance != null)
            {
                _skinInstance.SetActive(false); // Desativar em vez de destruir
            }

            _skinInstance = Instantiate(planetData.modelPrefab, modelRoot);
            _skinInstance.transform.localPosition = Vector3.zero;
            _skinInstance.transform.localRotation = Quaternion.identity;

            float scaleMultiplier = Random.Range(planetData.minScaleMultiplier, planetData.maxScaleMultiplier);
            _skinInstance.transform.localScale = Vector3.one * scaleMultiplier;

            float tiltX = Random.Range(planetData.minTiltAngle, planetData.maxTiltAngle);
            float tiltZ = Random.Range(planetData.minTiltAngle, planetData.maxTiltAngle);
            _skinInstance.transform.localRotation = Quaternion.Euler(tiltX, 0, tiltZ);

            StartRotation();
        }

        private void StartRotation()
        {
            if (!_skinInstance) return;

            var planetData = (PlanetData)destructibleObject;
            if (planetData == null)
            {
                DebugUtility.LogError<Planets>($"PlanetData não definido em {gameObject.name} para rotação.", this);
                return;
            }
            _rotationTween?.Kill();
            float rotationSpeed = Random.Range(planetData.minRotationSpeed, planetData.maxRotationSpeed);
            float direction = planetData.rotateClockwise ? -1f : 1f;
            _rotationTween = _skinInstance.transform.DORotate(
                    new Vector3(0, 360 * direction, 0),
                    360f / rotationSpeed,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetRelative(true);
        }

        public override void ResetState()
        {
            base.ResetState();
            _isActive = true;
            if (_skinInstance != null)
            {
                _skinInstance.SetActive(true);
            }
            if (_orbitController != null)
            {
                _orbitController.ResetState();
            }
            if (_rotationTween != null)
            {
                _rotationTween.Kill();
                StartRotation();
            }
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            if (debugMode)
            {
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} recebeu {damage:F2} de dano. Vida atual: {CurrentHealth:F2}", "green", this);
            }
        }

        protected override void Die()
        {
            _isActive = false;
            if (_skinInstance != null)
            {
                _skinInstance.SetActive(false);
            }
            if (_orbitController != null)
            {
                _orbitController.StopOrbit();
            }
            if (_rotationTween != null)
            {
                _rotationTween.Kill();
                _rotationTween = null;
            }

            var planetData = (PlanetData)destructibleObject;
            OnPlanetDestroyed?.Invoke(planetData);
            base.Die(); // Chama ReturnToPool

            if (debugMode)
            {
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} destruído, OnPlanetDestroyed disparado.", "red", this);
            }
        }
    }
}