using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    /// <summary>
    /// Inicializa o sistema de dependências, registrando serviços iniciais no DependencyManager.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DependencyBootstrapper : PersistentSingleton<DependencyBootstrapper>
    {
        private const string DebugColor = "yellow";
        private bool _hasBeenBootstrapped;

        /// <summary>
        /// Executa a inicialização do sistema de dependências, se ainda não foi feita.
        /// </summary>
        public void BootstrapOnDemand()
        {
            if (_hasBeenBootstrapped)
            {
                DebugUtility.LogVerbose<DependencyBootstrapper>("Já inicializado.", DebugColor, this);
                return;
            }
            _hasBeenBootstrapped = true;
            Bootstrap();
            DebugUtility.LogVerbose<DependencyBootstrapper>("Inicialização concluída.", DebugColor, this);
        }

        /// <summary>
        /// Registra serviços iniciais no DependencyManager. Pode ser sobrescrito para personalização.
        /// </summary>
        private void Bootstrap()
        {
            DebugUtility.LogVerbose<DependencyBootstrapper>("Registrando serviços iniciais.", DebugColor, this);
            
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reseta o estado para nova execução no editor.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (Instance is null) return;
            Instance._hasBeenBootstrapped = false;
            DebugUtility.LogVerbose<DependencyBootstrapper>("Estado resetado no editor.");
        }
#endif
    }
}