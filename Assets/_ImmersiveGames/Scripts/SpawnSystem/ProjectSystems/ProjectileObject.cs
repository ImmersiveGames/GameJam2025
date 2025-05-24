using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem.ProjectSystems
{
    public class ProjectileObject : MonoBehaviour, IPoolable, IProjectile
    {
        private float lifetime;
        private float timer;
        private bool isActive;
        private ObjectPool pool;
        private PoolableObjectData data;
        private GameObject modelInstance; // Instância do modelo

        public bool IsActive => isActive;

        public void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            this.data = data;
            this.pool = pool;
            this.lifetime = data.Lifetime;
            isActive = false;
            DebugUtility.LogVerbose<ProjectileObject>($"Projétil inicializado para '{data.ObjectName}'.", "blue", this);
        }

        public void Activate(Vector3 position)
        {
            isActive = true;
            transform.position = position;
            gameObject.SetActive(true);
            timer = 0f;
            if (modelInstance != null)
            {
                modelInstance.SetActive(true);
                modelInstance.transform.position = position;
            }
            OnObjectSpawned();
            DebugUtility.LogVerbose<ProjectileObject>($"Projétil ativado em {position}.", "blue", this);
        }

        public void Deactivate()
        {
            isActive = false;
            gameObject.SetActive(false);
            if (modelInstance != null)
            {
                modelInstance.SetActive(false);
            }
            OnObjectReturned();
            DebugUtility.LogVerbose<ProjectileObject>("Projétil desativado.", "blue", this);
        }

        public void OnObjectSpawned()
        {
            var audio = modelInstance?.GetComponent<AudioSource>();
            if (audio != null) audio.Play();
            DebugUtility.LogVerbose<ProjectileObject>("Projétil spawnado.", "blue", this);
        }

        public void OnObjectReturned()
        {
            var rb = modelInstance?.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            DebugUtility.LogVerbose<ProjectileObject>("Projétil retornado ao pool.", "blue", this);
        }

        public void Configure(Vector3 direction, float speed)
        {
            if (modelInstance == null) return;
            var rb = modelInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }
            else
            {
                modelInstance.transform.position += direction * speed * Time.deltaTime;
            }
            DebugUtility.LogVerbose<ProjectileObject>($"Projétil configurado: direção {direction}, velocidade {speed}.", "blue", this);
        }

        private void Update()
        {
            if (!isActive) return;
            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                pool?.ReturnObject(this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (modelInstance != null && collision.gameObject == modelInstance)
            {
                DebugUtility.LogVerbose<ProjectileObject>($"Colisão com {collision.gameObject.name}.", "blue", this);
                pool?.ReturnObject(this);
            }
        }

        // Usado pelo IPoolableFactory para configurar o modelo
        public void SetModelInstance(GameObject model)
        {
            modelInstance = model;
            if (modelInstance != null)
            {
                modelInstance.transform.SetParent(transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
            }
        }
    }
}