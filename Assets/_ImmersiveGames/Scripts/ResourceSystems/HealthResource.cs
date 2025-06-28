using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
    // Sistema de saúde que implementa IDestructible e IResettable
    public class HealthResource : ResourceSystem, IDestructible, IResettable
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

        protected virtual void OnDeath()
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
        public virtual void Reset()
        {
            currentValue = maxValue; // Restaura ao valor máximo
            triggeredThresholds.Clear(); // Limpa limiares disparados
            modifiers.Clear(); // Remove todos os modificadores
            lastChanger = null;
            //TODO: isso não deveria ser feito no skin?
            _skinRoot = GetComponentInParent<IHasSkin>();
            _skinRoot?.SetSkinActive(true); // Reativa o modelo, se disponível
            float percentage = GetPercentage();
            OnEventValueChanged(percentage); // Notifica mudança
            EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, percentage)); // Dispara evento
        }
        
    }
}