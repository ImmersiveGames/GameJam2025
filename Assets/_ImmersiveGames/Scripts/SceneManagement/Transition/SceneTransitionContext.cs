using System.Collections.Generic;
using _ImmersiveGames.Scripts.SceneManagement.Configs;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Descreve o plano de uma transição de cenas:
    /// - quais cenas carregar;
    /// - quais descarregar;
    /// - qual será a cena ativa ao final;
    /// - se deve usar fade;
    /// - (opcional) quais perfis/grupos de cena estão envolvidos.
    /// 
    /// A estrutura é imutável (readonly) para facilitar o raciocínio
    /// e evitar alterações durante a execução da transição.
    /// </summary>
    public readonly struct SceneTransitionContext
    {
        // Campos "legados" (mantidos para compatibilidade)
        public readonly IReadOnlyList<string> scenesToLoad;
        public readonly IReadOnlyList<string> scenesToUnload;
        public readonly string targetActiveScene;
        public readonly bool useFade;

        // Novos metadados (opcionais) para integração com perfis/grupos
        public readonly SceneTransitionProfile transitionProfile;
        private readonly SceneGroupProfile _fromGroupProfile;
        private readonly SceneGroupProfile _toGroupProfile;

        /// <summary>
        /// Construtor legado, para compatibilidade com chamadas existentes.
        /// </summary>
        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade)
        {
            this.scenesToLoad = scenesToLoad;
            this.scenesToUnload = scenesToUnload;
            this.targetActiveScene = targetActiveScene;
            this.useFade = useFade;

            transitionProfile = null;
            _fromGroupProfile = null;
            _toGroupProfile = null;
        }

        /// <summary>
        /// Construtor completo, permitindo associar perfis/grupos.
        /// </summary>
        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneTransitionProfile transitionProfile,
            SceneGroupProfile fromGroupProfile,
            SceneGroupProfile toGroupProfile)
        {
            this.scenesToLoad = scenesToLoad;
            this.scenesToUnload = scenesToUnload;
            this.targetActiveScene = targetActiveScene;
            this.useFade = useFade;

            this.transitionProfile = transitionProfile;
            _fromGroupProfile = fromGroupProfile;
            _toGroupProfile = toGroupProfile;
        }

        public override string ToString()
        {
            string loadStr = scenesToLoad == null ? "null" : string.Join(", ", scenesToLoad);
            string unloadStr = scenesToUnload == null ? "null" : string.Join(", ", scenesToUnload);
            string profileId = transitionProfile == null ? "null" : transitionProfile.name;
            string fromId = _fromGroupProfile == null ? "null" : _fromGroupProfile.Id;
            string toId = _toGroupProfile == null ? "null" : _toGroupProfile.Id;

            return $"SceneTransitionContext(Load=[{loadStr}], Unload=[{unloadStr}], " +
                   $"TargetActive='{targetActiveScene}', UseFade={useFade}, " +
                   $"Profile='{profileId}', From='{fromId}', To='{toId}')";
        }
    }
}
