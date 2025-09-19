using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceSystemTester : MonoBehaviour
    {
        [SerializeField] private string targetActorId = "";
        [SerializeField] private string targetResourceId;
        private ResourceSystem _resourceSystem;
        private EventBinding<ResourceBindEvent> _resourceBindEvent;

        private void Awake()
        {
            if (string.IsNullOrEmpty(targetResourceId))
            {
                DebugUtility.LogError<ResourceSystemTester>($"Awake: targetResourceId não configurado! Defina o ResourceId base do ResourceConfigSo (ex.: 'Health').", this);
            }
        }

        private void OnEnable()
        {
            _resourceBindEvent = new EventBinding<ResourceBindEvent>(OnResourceBindEvent);
            EventBus<ResourceBindEvent>.Register(_resourceBindEvent);
            DebugUtility.LogVerbose<ResourceSystemTester>($"OnEnable: Registrado binding para ResourceBindEvent, TargetActorId={targetActorId}, TargetResourceId={targetResourceId}, Source={gameObject.name}");
        }

        private void OnDisable()
        {
            if (_resourceBindEvent != null)
            {
                EventBus<ResourceBindEvent>.Unregister(_resourceBindEvent);
            }
        }

        private void OnResourceBindEvent(ResourceBindEvent evt)
        {
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            if (resourceId != targetResourceId || 
                (!string.IsNullOrEmpty(targetActorId) && evt.ActorId != targetActorId))
            {
                DebugUtility.LogVerbose<ResourceSystemTester>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, ResourceId={resourceId}, Expected={targetResourceId}, ActorId={evt.ActorId}, Expected={targetActorId}, Source={evt.Source.name}, Tester Source={gameObject.name}");
                return;
            }

            if (evt.Resource is ResourceSystem resourceSystem)
            {
                _resourceSystem = resourceSystem;
                DebugUtility.LogVerbose<ResourceSystemTester>($"OnResourceBindEvent: Vinculado ResourceSystem com UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, Tester Source={gameObject.name}");
            }
        }

        public void IncreaseResource()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.Increase(10f);
                DebugUtility.LogVerbose<ResourceSystemTester>($"⏫ Aumentado 10.00 em ActorId={targetActorId}, ResourceId={targetResourceId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceSystemTester>($"IncreaseResource: Nenhum ResourceSystem vinculado para ActorId={targetActorId}, ResourceId={targetResourceId}!", this);
            }
        }

        public void DecreaseResource()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.Decrease(10f);
                DebugUtility.LogVerbose<ResourceSystemTester>($"⏬ Diminuído 10.00 em ActorId={targetActorId}, ResourceId={targetResourceId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceSystemTester>($"DecreaseResource: Nenhum ResourceSystem vinculado para ActorId={targetActorId}, ResourceId={targetResourceId}!", this);
            }
        }

        public void ResetResource()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.Reset();
                DebugUtility.LogVerbose<ResourceSystemTester>($"🔄 Resetado ActorId={targetActorId}, ResourceId={targetResourceId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceSystemTester>($"ResetResource: Nenhum ResourceSystem vinculado para ActorId={targetActorId}, ResourceId={targetResourceId}!", this);
            }
        }

        public void AddModifier()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.AddModifier(5f, 3f);
                DebugUtility.LogVerbose<ResourceSystemTester>($"➕ Adicionado modificador de 5.00/s por 3s em ActorId={targetActorId}, ResourceId={targetResourceId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceSystemTester>($"AddModifier: Nenhum ResourceSystem vinculado para ActorId={targetActorId}, ResourceId={targetResourceId}!", this);
            }
        }
    }
}