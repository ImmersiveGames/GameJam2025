using System.Collections.Generic;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    /// <summary>
    /// Descreve o plano de uma transi��o de cenas:
    /// - quais cenas carregar;
    /// - quais descarregar;
    /// - qual ser� a cena ativa ao final;
    /// - se deve usar fade;
    /// - (opcional) quais perfis/grupos de cena est�o envolvidos.
    /// 
    /// A estrutura � imut�vel (readonly) para facilitar o racioc�nio
    /// e evitar altera��es durante a execu��o da transi��o.
    /// </summary>
    public readonly struct SceneTransitionContext
    {
        // Campos "legados" (mantidos para compatibilidade)
        public readonly IReadOnlyList<string> scenesToLoad;
        public readonly IReadOnlyList<string> scenesToUnload;
        public readonly string targetActiveScene;
        public readonly bool useFade;

        // Novos metadados (opcionais) para integra��o com perfis/grupos
        public readonly OldSceneTransitionProfile transitionProfile;
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
            OldSceneTransitionProfile transitionProfile,
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

