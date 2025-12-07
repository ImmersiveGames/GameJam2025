using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    
    public class MarkPlanet : MonoBehaviour
    {
        private IPlanetActor _planetActor;
        private bool _hasSearchedForFlag;

        public IActor PlanetActor => _planetActor?.PlanetActor;
        private bool IsMarked { get; set; }
        private FlagMarkPlanet FlagMark { get; set; }

        private void Awake()
        {
            _planetActor = GetComponent<IPlanetActor>();
            
            if (_planetActor == null)
            {
                DebugUtility.LogError<MarkPlanet>($"Componente IPlanetActor não encontrado em {gameObject.name}");
                return;
            }
            
            IsMarked = false;
            // Não busca a flag no Awake - será buscada sob demanda
        }

        private bool EnsureFlagMark()
        {
            if (FlagMark != null) return true;
            if (_hasSearchedForFlag) return false;

            _hasSearchedForFlag = true;
            FlagMark = FindFlagMarkInChildren();
            
            return FlagMark != null;
        }

        private FlagMarkPlanet FindFlagMarkInChildren()
        {
            var flag = GetComponentInChildren<FlagMarkPlanet>(true);
            if (flag != null)
            {
                DebugUtility.LogVerbose<MarkPlanet>($"Bandeira encontrada em {gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<MarkPlanet>($"Nenhuma FlagMarkPlanet encontrada em {gameObject.name}");
            }
            
            return flag;
        }

        private void Mark()
        {
            if (IsMarked) 
            {
                DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} já está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>($"Não é possível marcar - FlagMark não encontrada em {gameObject.name}");
                return;
            }

            FlagMark.SetFlagActive(true);
            IsMarked = true;

            DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} MARCADO");

            EventBus<PlanetMarkedEvent>.Raise(new PlanetMarkedEvent(PlanetActor, gameObject, this));
        }

        public void Unmark()
        {
            if (!IsMarked) 
            {
                DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} já não está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>($"Não é possível desmarcar - FlagMark não encontrada em {gameObject.name}");
                return;
            }

            FlagMark.SetFlagActive(false);
            IsMarked = false;

            DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} DESMARCADO");

            EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(PlanetActor, gameObject, this));
        }

        public void ToggleMark()
        {
            if (IsMarked)
                Unmark();
            else
                Mark();
        }

        // Método para forçar a busca da flag (útil se a flag for adicionada runtime)
        public void RefreshFlagMark()
        {
            _hasSearchedForFlag = false;
            FlagMark = null;
            EnsureFlagMark();
        }

        private void OnDestroy()
        {
            if (IsMarked)
            {
                Unmark();
            }
        }

        private void OnDisable()
        {
            if (IsMarked)
            {
                Unmark();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsMarked) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}