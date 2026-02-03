using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Responsável por controlar a marcação visual e lógica de um planeta.
    ///
    /// Regras:
    /// - Ativa/desativa a FlagMarkPlanet associada.
    /// - Emite PlanetMarkedEvent / PlanetUnmarkedEvent.
    /// - Íntegra com PlanetMarkingManager para garantir que exista
    ///   no máximo um planeta marcado ao mesmo tempo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Planet Systems/Mark Planet")]
    public sealed class MarkPlanet : MonoBehaviour
    {
        private IPlanetActor _planetActor;
        private bool _hasSearchedForFlag;

        /// <summary>
        /// Ator associado a este planeta (ex.: PlanetsMaster).
        /// </summary>
        public IActor PlanetActor => _planetActor?.PlanetActor;

        /// <summary>
        /// Indica se este planeta está atualmente marcado.
        /// A marcação é controlada pelos métodos Mark/Unmark/ToggleMark.
        /// </summary>
        public bool IsMarked { get; private set; }

        /// <summary>
        /// Referência à flag visual (ex.: ícone, bandeira) exibida
        /// quando o planeta está marcado.
        /// </summary>
        private FlagMarkPlanet FlagMark { get; set; }

        private void Awake()
        {
            _planetActor = GetComponent<IPlanetActor>();

            if (_planetActor == null)
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"Componente IPlanetActor não encontrado em {gameObject.name}",
                    this);
                return;
            }

            IsMarked = false;
            // A flag não é buscada no Awake; será localizada sob demanda.
        }

        /// <summary>
        /// Tenta garantir que a FlagMarkPlanet foi localizada e cacheada.
        /// </summary>
        private bool EnsureFlagMark()
        {
            if (FlagMark != null)
            {
                return true;
            }

            if (_hasSearchedForFlag)
            {
                return false;
            }

            _hasSearchedForFlag = true;
            FlagMark = FindFlagMarkInChildren();

            return FlagMark != null;
        }

        private FlagMarkPlanet FindFlagMarkInChildren()
        {
            var flag = GetComponentInChildren<FlagMarkPlanet>(true);
            if (flag != null)
            {
                DebugUtility.LogVerbose<MarkPlanet>(
                    $"Bandeira encontrada em {gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<MarkPlanet>(
                    $"Nenhuma FlagMarkPlanet encontrada em {gameObject.name}",
                    this);
            }

            return flag;
        }

        private void Mark()
        {
            if (IsMarked)
            {
                DebugUtility.LogVerbose<MarkPlanet>(
                    $"Planeta {gameObject.name} já está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"Não é possível marcar - FlagMark não encontrada em {gameObject.name}",
                    this);
                return;
            }

            FlagMark.SetFlagActive(true);
            IsMarked = true;

            DebugUtility.LogVerbose<MarkPlanet>(
                $"Planeta {gameObject.name} MARCADO",
                DebugUtility.Colors.CrucialInfo,
                this);

            EventBus<PlanetMarkedEvent>.Raise(
                new PlanetMarkedEvent(PlanetActor, gameObject, this));
        }

        public void Unmark()
        {
            if (!IsMarked)
            {
                DebugUtility.LogVerbose<MarkPlanet>(
                    $"Planeta {gameObject.name} já não está marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"Não é possível desmarcar - FlagMark não encontrada em {gameObject.name}",
                    this);
                return;
            }

            FlagMark.SetFlagActive(false);
            IsMarked = false;

            DebugUtility.LogVerbose<MarkPlanet>(
                $"Planeta {gameObject.name} DESMARCADO",
                DebugUtility.Colors.CrucialInfo,
                this);

            EventBus<PlanetUnmarkedEvent>.Raise(
                new PlanetUnmarkedEvent(PlanetActor, gameObject, this));
        }

        /// <summary>
        /// Alterna o estado de marcação deste planeta.
        /// </summary>
        public void ToggleMark()
        {
            if (IsMarked)
            {
                Unmark();
            }
            else
            {
                Mark();
            }
        }

        /// <summary>
        /// Força uma nova busca pela FlagMarkPlanet.
        /// Útil se a flag for adicionada dinamicamente em runtime.
        /// </summary>
        public void RefreshFlagMark()
        {
            _hasSearchedForFlag = false;
            FlagMark = null;
            EnsureFlagMark();
        }

        private void OnDisable()
        {
            if (IsMarked)
            {
                Unmark();
            }
        }

        private void OnDestroy()
        {
            if (IsMarked)
            {
                Unmark();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsMarked)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}

