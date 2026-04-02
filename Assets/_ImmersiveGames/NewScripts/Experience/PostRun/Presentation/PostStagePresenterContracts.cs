using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation
{
    /// <summary>
    /// Contratos de apresentação do PostRun.
    ///
    /// Ficam separados do handoff operacional para deixar claro que presenter
    /// e registry pertencem à borda visual, não ao controle do ciclo.
    /// </summary>
    public interface IPostStagePresenter
    {
        string PresenterSignature { get; }
        bool IsReady { get; }
        void BindToSession(PostStageContext context, IPostStageControlService controlService);
    }

    public interface IPostStagePresenterRegistry
    {
        bool TryGetCurrentPresenter(out IPostStagePresenter presenter);
        bool TryEnsureCurrentPresenter(PostStageContext context, string source, out IPostStagePresenter presenter);
        void Register(IPostStagePresenter presenter, string sessionSignature);
        void Unregister(IPostStagePresenter presenter);
    }

    public interface IPostStagePresenterScopeResolver
    {
        bool TryResolvePresenters(PostStageContext context, out IReadOnlyList<IPostStagePresenter> presenters);
    }
}

