using System.Threading.Tasks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade
{
    /// <summary>
    /// Configuração “crua” de fade (sem depender de SceneTransitionProfile).
    /// </summary>
    public readonly struct NewScriptsFadeConfig
    {
        public NewScriptsFadeConfig(
            float fadeInDuration,
            float fadeOutDuration,
            AnimationCurve fadeInCurve,
            AnimationCurve fadeOutCurve)
        {
            FadeInDuration = fadeInDuration;
            FadeOutDuration = fadeOutDuration;
            FadeInCurve = fadeInCurve;
            FadeOutCurve = fadeOutCurve;
        }

        public float FadeInDuration { get; }
        public float FadeOutDuration { get; }
        public AnimationCurve FadeInCurve { get; }
        public AnimationCurve FadeOutCurve { get; }
    }

    public interface INewScriptsFadeService
    {
        void Configure(NewScriptsFadeConfig config);
        Task FadeInAsync();
        Task FadeOutAsync();
    }
}
