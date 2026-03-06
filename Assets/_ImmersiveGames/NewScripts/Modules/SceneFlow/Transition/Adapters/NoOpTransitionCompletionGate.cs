#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Adapters
{
    /// <summary>
    /// OWNER: implementacao compat/no-op do contrato de gate.
    /// NAO E OWNER: bloqueio real por reset/preparo.
    /// PUBLISH/CONSUME: sem EventBus; apenas retorna Task.CompletedTask.
    /// Fases tocadas: nenhuma (bypass do gate).
    /// </summary>
    public sealed class NoOpTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context) => Task.CompletedTask;
    }
}
