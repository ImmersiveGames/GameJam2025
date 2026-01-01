using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Paths canônicos (Resources) para profiles do SceneFlow.
    /// Regra: não montar "SceneFlow/Profiles/..." espalhado no código.
    /// </summary>
    public static class SceneFlowProfilePaths
    {
        public const string ProfilesRoot = "SceneFlow/Profiles";

        /// <summary>
        /// Converte um id de profile (ex: "startup") em um caminho de Resources
        /// (ex: "SceneFlow/Profiles/startup"). Também aceita um caminho já completo.
        /// </summary>
        public static string For(string profileNameOrPath)
        {
            if (string.IsNullOrWhiteSpace(profileNameOrPath))
                return string.Empty;

            var key = profileNameOrPath.Trim();

            // Se já veio como path (contém diretórios), normalize e retorne.
            if (key.IndexOf('/') >= 0 || key.IndexOf('\\') >= 0)
                return NormalizeResourcePath(key);

            var id = SceneFlowProfileNames.Normalize(key);
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            return $"{ProfilesRoot}/{id}";
        }

        private static string NormalizeResourcePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var p = path.Replace('\\', '/').Trim();

            // Resources.Load espera path sem extensão.
            if (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                p = p.Substring(0, p.Length - ".asset".Length);

            return p;
        }
    }
}
