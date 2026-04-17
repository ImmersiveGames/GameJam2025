using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using ImmersiveGames.GameJam2025.Core.Events;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Managers
{
    /// <summary>
    /// Respons�vel por gerenciar a marca��o de planetas no jogo.
    ///
    /// Regras principais:
    /// - Sempre no m�ximo um planeta marcado ao mesmo tempo.
    /// - Interagir com um planeta j� marcado desmarca (ficando nenhum).
    /// - Interagir com um planeta diferente desmarca o anterior e marca o novo.
    ///
    /// A coordena��o � feita via eventos:
    /// - PlanetMarkedEvent
    /// - PlanetUnmarkedEvent
    /// - PlanetMarkingChangedEvent
    ///
    /// A marca��o/desmarca��o visual � responsabilidade do componente
    /// MarkPlanet em cada planeta.
    /// </summary>
    public sealed class PlanetMarkingManager
    {
        private static PlanetMarkingManager _instance;
        public static PlanetMarkingManager Instance => _instance ??= new PlanetMarkingManager();

        private MarkPlanet _currentlyMarkedPlanet;
        private EventBinding<PlanetMarkedEvent> _markedBinding;
        private EventBinding<PlanetUnmarkedEvent> _unmarkedBinding;

        public MarkPlanet CurrentlyMarkedPlanet => _currentlyMarkedPlanet;
        public IActor CurrentlyMarkedPlanetActor => _currentlyMarkedPlanet?.PlanetActor;
        public bool HasMarkedPlanet => _currentlyMarkedPlanet != null;

        private PlanetMarkingManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            _markedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _unmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);

            EventBus<PlanetMarkedEvent>.Register(_markedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_unmarkedBinding);
        }

        private void OnPlanetMarked(PlanetMarkedEvent markedEvent)
        {
            var previousMarked = _currentlyMarkedPlanet;

            // Garante unicidade: se j� havia um planeta marcado diferente, desmarca.
            if (_currentlyMarkedPlanet != null && _currentlyMarkedPlanet != markedEvent.MarkPlanet)
            {
                _currentlyMarkedPlanet.Unmark();
            }

            _currentlyMarkedPlanet = markedEvent.MarkPlanet;

            // Notifica outros sistemas sobre a troca de marca��o.
            EventBus<PlanetMarkingChangedEvent>.Raise(
                new PlanetMarkingChangedEvent(
                    markedEvent.PlanetActor,
                    previousMarked?.PlanetActor));
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent unmarkedEvent)
        {
            if (_currentlyMarkedPlanet == unmarkedEvent.MarkPlanet)
            {
                _currentlyMarkedPlanet = null;
            }
        }

        /// <summary>
        /// Tenta marcar/desmarcar um planeta a partir de um GameObject
        /// (por exemplo, a partir de um Raycast do Player).
        /// </summary>
        public bool TryMarkPlanet(GameObject planetObject)
        {
            if (planetObject == null)
            {
                return false;
            }

            var markPlanet = planetObject.GetComponentInParent<MarkPlanet>();
            if (markPlanet == null)
            {
                return false;
            }

            markPlanet.ToggleMark();
            return true;
        }

        /// <summary>
        /// Limpa qualquer marca��o ativa, se existir.
        /// </summary>
        public void ClearAllMarks()
        {
            if (_currentlyMarkedPlanet != null)
            {
                _currentlyMarkedPlanet.Unmark();
            }
        }

        /// <summary>
        /// Descadastra os bindings de evento e limpa refer�ncia interna.
        /// Deve ser usado apenas em cen�rios de teardown/controlados.
        /// </summary>
        public void Dispose()
        {
            if (_markedBinding != null)
            {
                EventBus<PlanetMarkedEvent>.Unregister(_markedBinding);
                _markedBinding = null;
            }

            if (_unmarkedBinding != null)
            {
                EventBus<PlanetUnmarkedEvent>.Unregister(_unmarkedBinding);
                _unmarkedBinding = null;
            }

            _currentlyMarkedPlanet = null;
        }
    }
}

