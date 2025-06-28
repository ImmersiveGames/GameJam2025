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

        // Inicializa referências no Awake para garantir disponibilidade
        protected override void Awake()
        {
            base.Awake();
            _skinRoot = GetComponentInParent<IHasSkin>();
        }

        protected void OnResourceDepleted(IActor byActor = null)
        {
            OnResourceDepleted();
            DebugUtility.Log<HealthResource>($"{gameObject.name} morreu!");
            OnDeath(byActor); // Chama o método de extensão
        }

        protected virtual void OnDeath(IActor byActor = null)
        {
            var spawnPoint = _skinRoot is not null ? _skinRoot.ModelTransform.position : transform.position;
            DebugUtility.Log<HealthResource>($"HealthResource {gameObject.name}: Disparando DeathEvent com posição {spawnPoint}");
            EventBus<DeathEvent>.Raise(new DeathEvent(spawnPoint, gameObject));
            _skinRoot?.SetSkinActive(false);
        }
        

        // Cura o recurso
        public virtual void Heal(float amount,  IActor byActor)
        {
            Increase(amount);
            OnHeal(amount, byActor);
        }

        public virtual void TakeDamage(float damage, IActor byActor)
        {
            Decrease(damage);
            OnTakeDamage(damage, byActor);
        }

        protected virtual void OnHeal(float amount, IActor byActor) { }
        protected virtual void OnTakeDamage(float damage, IActor byActor) { }

        // Reinicia o recurso ao estado padrão
        public virtual void Reset()
        {
            currentValue = maxValue; // Restaura ao valor máximo
            triggeredThresholds.Clear(); // Limpa limiares disparados
            modifiers.Clear(); // Remove todos os modificadores
            //TODO: isso não deveria ser feito no skin?
            _skinRoot = GetComponentInParent<IHasSkin>();
            _skinRoot?.SetSkinActive(true); // Reativa o modelo, se disponível
            float percentage = GetPercentage();
            OnEventValueChanged(percentage); // Notifica mudança
            EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, percentage)); // Dispara evento
            OnReset(); // Permite que classes derivadas adicionem comportamento
        }

        protected virtual void OnReset() { } // Ponto de extensão para classes derivadas
    }
}