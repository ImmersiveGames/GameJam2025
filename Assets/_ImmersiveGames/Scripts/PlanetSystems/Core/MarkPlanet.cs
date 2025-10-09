using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    [DebugLevel(DebugLevel.Verbose)]
    public class MarkPlanet : MonoBehaviour
    {
        private FlagMarkPlanet _flagMark;
        private IPlanetActor _planetActor;
        private bool _isMarked;
        private bool _hasSearchedForFlag;

        public IActor PlanetActor => _planetActor?.PlanetActor;
        public bool IsMarked => _isMarked;
        public FlagMarkPlanet FlagMark => _flagMark;

        private void Awake()
        {
            _planetActor = GetComponent<IPlanetActor>();
            
            if (_planetActor == null)
            {
                DebugUtility.LogError<MarkPlanet>($"Componente IPlanetActor não encontrado em {gameObject.name}");
                return;
            }
            
            _isMarked = false;
            // Não busca a flag no Awake - será buscada sob demanda
        }

        private bool EnsureFlagMark()
        {
            if (_flagMark != null) return true;
            if (_hasSearchedForFlag) return false;

            _hasSearchedForFlag = true;
            _flagMark = FindFlagMarkInChildren();
            
            return _flagMark != null;
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
            if (_isMarked) 
            {
                DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} já está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>($"Não é possível marcar - FlagMark não encontrada em {gameObject.name}");
                return;
            }

            _flagMark.SetFlagActive(true);
            _isMarked = true;

            DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} MARCADO");

            EventBus<PlanetMarkedEvent>.Raise(new PlanetMarkedEvent(PlanetActor, gameObject, this));
        }

        public void Unmark()
        {
            if (!_isMarked) 
            {
                DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} já não está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>($"Não é possível desmarcar - FlagMark não encontrada em {gameObject.name}");
                return;
            }

            _flagMark.SetFlagActive(false);
            _isMarked = false;

            DebugUtility.LogVerbose<MarkPlanet>($"Planeta {gameObject.name} DESMARCADO");

            EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(PlanetActor, gameObject, this));
        }

        public void ToggleMark()
        {
            if (_isMarked)
                Unmark();
            else
                Mark();
        }

        // Método para forçar a busca da flag (útil se a flag for adicionada runtime)
        public void RefreshFlagMark()
        {
            _hasSearchedForFlag = false;
            _flagMark = null;
            EnsureFlagMark();
        }

        private void OnDestroy()
        {
            if (_isMarked)
            {
                Unmark();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_isMarked) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}