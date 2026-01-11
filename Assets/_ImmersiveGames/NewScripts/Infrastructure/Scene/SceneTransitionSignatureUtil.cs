using System;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Centraliza a assinatura de correlação usada entre SceneFlow e WorldLifecycle.
    ///
    /// Importante:
    /// - A assinatura canônica é <see cref="SceneTransitionContext.ContextSignature"/>.
    /// - <see cref="SceneTransitionContext.ToString"/> deve ser tratado como string de debug/log.
    /// </summary>
    public static class SceneTransitionSignatureUtil
    {
        public static string Compute(SceneTransitionContext context)
        {
            return context.ContextSignature ?? string.Empty;
        }

        public static SceneTransitionContext BuildContext(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var loadList = NormalizeList(request.ScenesToLoad);
            var unloadList = NormalizeList(request.ScenesToUnload);
            return new SceneTransitionContext(loadList, unloadList, request.TargetActiveScene, request.UseFade,
                request.TransitionProfileId, request.ContextSignature);
        }

        private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            return source
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .ToArray();
        }
    }
}
