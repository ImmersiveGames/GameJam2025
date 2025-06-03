using _ImmersiveGames.Scripts.ActorSystems;
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
            if (!_actorMaster)
            {
                DebugUtility.LogWarning<HealthResource>($"ActorMaster não encontrado em {gameObject.name} ou seus pais!", gameObject);
                return;
            }
            modelRoot = _actorMaster.GetModelRoot()?.gameObject;
            if (!modelRoot)
            {
                DebugUtility.LogWarning<HealthResource>($"ModelRoot não encontrado em ActorMaster de {gameObject.name}!", gameObject);
            }
        }

        // Comportamento quando saúde chega a zero
        protected override void OnDepleted()
        {
            DebugUtility.Log<HealthResource>($"{gameObject.name} morreu!");
            Deafeat(transform.position);
        }

        // Dispara evento de morte e desativa o modelo
        public virtual void Deafeat(Vector3 position)
        {
            var spawnPoint = modelRoot ? modelRoot.transform.position : transform.position;
            DebugUtility.Log<HealthResource>($"HealthResource {gameObject.name}: Disparando DeathEvent com posição {spawnPoint}");
            EventBus<DeathEvent>.Raise(new DeathEvent(spawnPoint, gameObject));
            if (modelRoot)
            {
                modelRoot.SetActive(false);
            }
        }

        // Cura o recurso
        public void Heal(float amount) => Increase(amount);

        // Causa dano ao recurso
        public void TakeDamage(float damage) => Decrease(damage);

        // Reinicia o recurso ao estado padrão
        public void Reset()
        {
            currentValue = maxValue; // Restaura ao valor máximo
            triggeredThresholds.Clear(); // Limpa limiares disparados
            modifiers.Clear(); // Remove todos os modificadores
            if (modelRoot)
            {
                modelRoot.SetActive(true); // Reativa o modelo, se disponível
            }
            else
            {
                DebugUtility.LogWarning<HealthResource>($"modelRoot é nulo ao tentar reiniciar {gameObject.name}. Verifique a inicialização!", gameObject);
                InitializeReferences(); // Tenta reinicializar
            }
            onValueChanged.Invoke(GetPercentage()); // Notifica mudança
            EventBus<ResourceEvent>.Raise(new ResourceEvent(gameObject, config.ResourceType, GetPercentage())); // Dispara evento
        }
    }
}