using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.NewScripts.Core.Events;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Managers
{
    /// <summary>
    /// Responsável por gerenciar a marcação de planetas no jogo.
    ///
    /// Regras principais:
    /// - Sempre no máximo um planeta marcado ao mesmo tempo.
    /// - Interagir com um planeta já marcado desmarca (ficando nenhum).
    /// - Interagir com um planeta diferente desmarca o anterior e marca o novo.
    ///
    /// A coordenação é feita via eventos:
    /// - PlanetMarkedEvent
    /// - PlanetUnmarkedEvent
    /// - PlanetMarkingChangedEvent
    ///
    /// A marcação/desmarcação visual é responsabilidade do componente
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

            // Garante unicidade: se já havia um planeta marcado diferente, desmarca.
            if (_currentlyMarkedPlanet != null && _currentlyMarkedPlanet != markedEvent.MarkPlanet)
            {
                _currentlyMarkedPlanet.Unmark();
            }

            _currentlyMarkedPlanet = markedEvent.MarkPlanet;

            // Notifica outros sistemas sobre a troca de marcação.
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
        /// Limpa qualquer marcação ativa, se existir.
        /// </summary>
        public void ClearAllMarks()
        {
            if (_currentlyMarkedPlanet != null)
            {
                _currentlyMarkedPlanet.Unmark();
            }
        }

        /// <summary>
        /// Descadastra os bindings de evento e limpa referência interna.
        /// Deve ser usado apenas em cenários de teardown/controlados.
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

