using System.Threading.Tasks;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Hooks
{
    /// <summary>
    /// Implementação base com no-op para facilitar adoção incremental.
    /// </summary>
    public abstract class SceneResetHookBase : UnityEngine.MonoBehaviour, ISceneResetHook, ISceneResetHookOrdered
    {
        public virtual int Order => 0;

        public virtual Task OnBeforeDespawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterDespawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeforeSpawnAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAfterSpawnAsync()
        {
            return Task.CompletedTask;
        }
    }
}


