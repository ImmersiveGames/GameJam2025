using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Respons�vel por controlar a marca��o visual e l�gica de um planeta.
    ///
    /// Regras:
    /// - Ativa/desativa a FlagMarkPlanet associada.
    /// - Emite PlanetMarkedEvent / PlanetUnmarkedEvent.
    /// - �ntegra com PlanetMarkingManager para garantir que exista
    ///   no m�ximo um planeta marcado ao mesmo tempo.
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
        /// Indica se este planeta est� atualmente marcado.
        /// A marca��o � controlada pelos m�todos Mark/Unmark/ToggleMark.
        /// </summary>
        public bool IsMarked { get; private set; }

        /// <summary>
        /// Refer�ncia � flag visual (ex.: �cone, bandeira) exibida
        /// quando o planeta est� marcado.
        /// </summary>
        private FlagMarkPlanet FlagMark { get; set; }

        private void Awake()
        {
            _planetActor = GetComponent<IPlanetActor>();

            if (_planetActor == null)
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"Componente IPlanetActor n�o encontrado em {gameObject.name}",
                    this);
                return;
            }

            IsMarked = false;
            // A flag n�o � buscada no Awake; ser� localizada sob demanda.
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
                    $"Planeta {gameObject.name} j� est� marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"N�o � poss�vel marcar - FlagMark n�o encontrada em {gameObject.name}",
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
                    $"Planeta {gameObject.name} j� n�o est� marcado");
                return;
            }

            if (!EnsureFlagMark())
            {
                DebugUtility.LogError<MarkPlanet>(
                    $"N�o � poss�vel desmarcar - FlagMark n�o encontrada em {gameObject.name}",
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
        /// Alterna o estado de marca��o deste planeta.
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
        /// For�a uma nova busca pela FlagMarkPlanet.
        /// �til se a flag for adicionada dinamicamente em runtime.
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

