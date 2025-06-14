using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    // Sistema de saúde que implementa IDestructible e IResettable
    public class HealthResource : ResourceSystem, IDestructible, IResettable
    {
        protected GameObject modelRoot; // Raiz do modelo do ator
        private ActorMaster _actorMaster; // Referência ao ActorMaster

        // Inicializa referências no Awake para garantir disponibilidade
        protected override void Awake()
        {
            base.Awake();
            InitializeReferences();
        }

        // Inicializa _actorMaster e modelRoot
        private void InitializeReferences()
        {
            _actorMaster = GetComponentInParent<ActorMaster>();
            modelRoot = _actorMaster?.GetModelRoot()?.gameObject;
            if (!_actorMaster || !modelRoot)
            {
                DebugUtility.LogError<HealthResource>($"Falha na inicialização: {( !_actorMaster ? "ActorMaster" : "ModelRoot")} não encontrado em {gameObject.name}!", gameObject);
            }
            else
            {
                DebugUtility.Log<HealthResource>($"HealthResource inicializado com sucesso para {gameObject.name}.", "green", this);
            }
        }

        protected override void OnResourceDepleted()
        {
            base.OnResourceDepleted();
            DebugUtility.Log<HealthResource>($"{gameObject.name} morreu!");
            var spawnPoint = modelRoot ? modelRoot.transform.position : transform.position;
            DebugUtility.Log<HealthResource>($"HealthResource {gameObject.name}: Disparando DeathEvent com posição {spawnPoint}");
            EventBus<DeathEvent>.Raise(new DeathEvent(spawnPoint, gameObject));
            if (modelRoot)
            {
                modelRoot.SetActive(false);
            }
            OnDeath(); // Chama o método de extensão
        }

        protected virtual void OnDeath() { } // Ponto de extensão para classes derivadas
        

        // Cura o recurso
        public virtual void Heal(float amount)
        {
            Increase(amount);
            OnHeal(amount);
        }

        public virtual void TakeDamage(float damage)
        {
            Decrease(damage);
            OnTakeDamage(damage);
        }

        protected virtual void OnHeal(float amount) { }
        protected virtual void OnTakeDamage(float damage) { }

        // Reinicia o recurso ao estado padrão
        public virtual void Reset()
        {
            currentValue = maxValue; // Restaura ao valor máximo
            triggeredThresholds.Clear(); // Limpa limiares disparados
            modifiers.Clear(); // Remove todos os modificadores
            if (!modelRoot)
            {
                DebugUtility.LogWarning<HealthResource>($"modelRoot é nulo ao tentar reiniciar {gameObject.name}. Tentando reinicializar...", gameObject);
                InitializeReferences();
            }
            if (modelRoot)
            {
                modelRoot.SetActive(true); // Reativa o modelo, se disponível
            }
            else
            {
                DebugUtility.LogError<HealthResource>($"modelRoot não encontrado após reinicialização em {gameObject.name}. Verifique a configuração!", gameObject);
            }
            float percentage = GetPercentage();
            onValueChanged?.Invoke(percentage); // Notifica mudança
            EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, percentage)); // Dispara evento
            OnReset(); // Permite que classes derivadas adicionem comportamento
        }

        protected virtual void OnReset() { } // Ponto de extensão para classes derivadas
    }
}