using System.Collections;
using System.Threading.Tasks;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Implementação padrão do IFadeAwaiter.
    /// Usa o IFadeService existente e o ICoroutineRunner global para
    /// expor FadeInAsync / FadeOutAsync baseados em Task.
    /// </summary>
    public class FadeAwaiter : IFadeAwaiter
    {
        private readonly IFadeService _fadeService;
        private readonly ICoroutineRunner _runner;

        public FadeAwaiter(IFadeService fadeService, ICoroutineRunner runner)
        {
            _fadeService = fadeService;
            _runner = runner;
        }

        public Task FadeInAsync()
        {
            return RunFadeRoutineAsTask(_fadeService.FadeInAsync());
        }

        public Task FadeOutAsync()
        {
            return RunFadeRoutineAsTask(_fadeService.FadeOutAsync());
        }

        private Task RunFadeRoutineAsTask(IEnumerator fadeRoutine)
        {
            var tcs = new TaskCompletionSource<bool>();

            _runner.Run(Wrapper());
            return tcs.Task;

            IEnumerator Wrapper()
            {
                yield return fadeRoutine;
                tcs.SetResult(true);
            }
        }
    }
}