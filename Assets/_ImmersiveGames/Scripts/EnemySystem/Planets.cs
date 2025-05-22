using _ImmersiveGames.Scripts.Interfaces;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using DG.Tweening;
using UnityEngine;
using ImmersiveGames;

namespace ImmersiveGames.EnemySystem
{
    public abstract class DestructibleObject : MonoBehaviour, IDamageable
    {
        [SerializeField] protected DestructibleObjectSo destructibleObject;
        private float _defense;
        private bool _destroyOnDeath = true;
        private float _destroyDelay = 2f;

        public event System.Action<DestructibleObject> OnDeath;

        public virtual void Initialize()
        {
            if (destructibleObject == null)
            {
                Debug.LogError($"DestructibleObjectSo não está definido em {gameObject.name}.");
                return;
            }

            CurrentHealth = destructibleObject.planetMaxHealth;
            _defense = destructibleObject.planetDefense;
            _destroyOnDeath = destructibleObject.planetCanDestroy;
            _destroyDelay = destructibleObject.planetDeathDelay;
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (CurrentHealth <= 0) return;

            float finalDamage = Mathf.Max(0, damageAmount - _defense);
            CurrentHealth -= finalDamage;

            OnDamageTaken();

            if (CurrentHealth <= 0)
            {
                Die();
                DebugUtility.LogVerbose<Planets>($"O planeta foi destruído", "green");
            }
            DebugUtility.LogVerbose<Planets>($"recebeu {finalDamage} de dano. Vida atual: {CurrentHealth}", "green");
        }

        public bool IsAlive => CurrentHealth > 0;

        protected virtual void OnDamageTaken() { }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);
            if (_destroyOnDeath)
            {
                Destroy(gameObject, _destroyDelay);
            }
        }

        public float CurrentHealth { get; private set; } = 100f;
        public float MaxHealth => destructibleObject.planetMaxHealth;
    }
}

namespace ImmersiveGames.EnemySystem
{
    public class Planets : DestructibleObject
    {
        private PlanetData planetData;
        private PlanetResources resource;
        private PlanetOrbit orbitController;
        private GameObject skinInstance;
        private Tween rotationTween;

        public PlanetResources Resource => resource;

        public void SetPlanetData(PlanetData data)
        {
            planetData = data;
            destructibleObject = data;
        }

        public void SetResource(PlanetResources newResource)
        {
            resource = newResource;
        }

        public void StartOrbit(Transform orbitCenter)
        {
            if (orbitController == null)
            {
                orbitController = gameObject.AddComponent<PlanetOrbit>();
            }
            orbitController.Initialize(orbitCenter, planetData.minOrbitSpeed, planetData.maxOrbitSpeed, planetData.orbitClockwise);
        }

        public override void Initialize()
        {
            if (planetData == null)
            {
                Debug.LogError($"PlanetData não está definido em {gameObject.name}.");
                return;
            }

            base.Initialize();
            SetSkinPlanet();
        }

        private void SetSkinPlanet()
        {
            if (planetData.enemyModel == null)
            {
                Debug.LogError($"EnemyModel não está definido no PlanetData para {gameObject.name}.");
                return;
            }

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Instancia o modelo
            skinInstance = Instantiate(planetData.enemyModel, transform);
            skinInstance.transform.localPosition = Vector3.zero;
            skinInstance.transform.localRotation = Quaternion.identity;

            // Configura o colisor
            Collider modelCollider = skinInstance.GetComponent<Collider>();
            if (modelCollider != null)
            {
                modelCollider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"Nenhum Collider encontrado no enemyModel de {gameObject.name}. Adicione um Collider ao prefab do modelo.");
            }

            // Aplica escala
            float scaleMultiplier = Random.Range(planetData.minScaleMultiplier, planetData.maxScaleMultiplier);
            skinInstance.transform.localScale = Vector3.one * scaleMultiplier;

            // Aplica inclinação
            float tiltX = Random.Range(planetData.minTiltAngle, planetData.maxTiltAngle);
            float tiltZ = Random.Range(planetData.minTiltAngle, planetData.maxTiltAngle);
            skinInstance.transform.localRotation = Quaternion.Euler(tiltX, 0, tiltZ);

            // Inicia translação (rotação própria)
            StartRotation();
        }

        private void StartRotation()
        {
            if (skinInstance == null) return;

            rotationTween?.Kill();
            float rotationSpeed = Random.Range(planetData.minRotationSpeed, planetData.maxRotationSpeed);
            float direction = planetData.rotateClockwise ? -1f : 1f;
            rotationTween = skinInstance.transform.DORotate(
                    new Vector3(0, 360 * direction, 0),
                    360f / rotationSpeed,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetRelative(true);
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            Debug.Log($"Planeta {gameObject.name} recebeu {damage} de dano. Vida atual: {CurrentHealth}");
        }

        protected override void Die()
        {
            if (orbitController != null)
            {
                orbitController.StopOrbit();
            }
            if (rotationTween != null)
            {
                rotationTween.Kill();
                rotationTween = null;
            }
            base.Die();
            Debug.Log($"Planeta {gameObject.name} destruído.");
        }
    }
}