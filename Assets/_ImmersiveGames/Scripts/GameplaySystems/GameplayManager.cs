using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameplaySystems
{
    public interface IGameplayManager
    {
        Transform WorldEater { get; }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayManager : Singleton<GameplayManager>, IGameplayManager
    {
        [Header("Legacy Fallback (Opcional)")]
        [Tooltip("Fallback legado. Idealmente deixe vazio e use IEaterDomain via auto-register.")]
        [SerializeField] private Transform worldEater;

        [Header("Domain Resolution")]
        [Tooltip("Quando habilitado, tenta obter o Eater via IEaterDomain (scene-scoped).")]
        [SerializeField] private bool preferDomain = true;

        private IEaterDomain _eaterDomain;
        private string _sceneNameCached;

        public Transform WorldEater
        {
            get
            {
                if (preferDomain)
                {
                    TryResolveEaterDomainIfNeeded();

                    var eaterActor = _eaterDomain?.Eater;
                    if (eaterActor != null && eaterActor.Transform != null)
                    {
                        return eaterActor.Transform;
                    }
                }

                return worldEater;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _sceneNameCached = gameObject.scene.name;

            DependencyManager.Provider.RegisterGlobal<IGameplayManager>(this, allowOverride: true);

            DebugUtility.Log<GameplayManager>(
                "GameplayManager inicializado (resolução do Eater via Domínio quando disponível).",
                DebugUtility.Colors.Success);
        }

        private void TryResolveEaterDomainIfNeeded()
        {
            if (_eaterDomain != null)
                return;

            if (string.IsNullOrWhiteSpace(_sceneNameCached))
                _sceneNameCached = gameObject.scene.name;

            if (DependencyManager.Provider.TryGetForScene<IEaterDomain>(_sceneNameCached, out var domain) && domain != null)
            {
                _eaterDomain = domain;
                DebugUtility.LogVerbose<GameplayManager>(
                    $"IEaterDomain resolvido para a cena '{_sceneNameCached}'.");
            }
        }
    }
}

