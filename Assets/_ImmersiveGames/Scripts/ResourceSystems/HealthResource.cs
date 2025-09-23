using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    // Sistema de saúde que implementa IDestructible e IResettable
    public class HealthResource : ResourceSystem, IHealthSpecific
    {
        private IHasSkin _skinRoot; // Raiz do modelo do ator
        protected IActor lastChanger; // Último ator que causou dano

        // Inicializa referências no Awake para garantir disponibilidade
        protected override void Awake()
        {
            base.Awake();
            _skinRoot = GetComponentInParent<IHasSkin>();
        }

        protected override void OnResourceDepleted()
        {
            base.OnResourceDepleted();
            DebugUtility.LogVerbose<HealthResource>($"{gameObject.name} morreu!");
            OnDeath(); // Chama o método de extensão
        }

        public virtual void OnDeath()
        {
            var spawnPoint = _skinRoot is not null ? _skinRoot.ModelTransform.position : transform.position;
            DebugUtility.LogVerbose<HealthResource>($"HealthResource {gameObject.name}: Disparando DeathEvent com posição {spawnPoint}");
            EventBus<DeathEvent>.Raise(new DeathEvent(spawnPoint, gameObject));
            _skinRoot?.SetSkinActive(false);
        }
        

        // Cura o recurso
        public virtual void Heal(float amount,  IActor byActor)
        {
            lastChanger = byActor;
            Increase(amount);
        }

        public virtual void TakeDamage(float damage, IActor byActor)
        {
            lastChanger = byActor;
            Decrease(damage);
        }
        

        // Reinicia o recurso ao estado padrão
        public override void Reset(bool resetSkin = true)
        {
            base.Reset(resetSkin);
            lastChanger = null;
            if (resetSkin && _skinRoot != null)
            {
                _skinRoot.SetSkinActive(true);
                DebugUtility.LogVerbose<HealthResource>($"Skin reativada para {gameObject.name}");
            }
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(config.UniqueId, gameObject, config.ResourceType, GetPercentage(), true));
        }

        
    }
}