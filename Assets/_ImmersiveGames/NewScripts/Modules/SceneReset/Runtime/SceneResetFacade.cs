using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Modules.SceneReset.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
namespace _ImmersiveGames.NewScripts.Modules.SceneReset.Runtime
{
    /// <summary>
    /// Façade fina do trilho local de reset.
    /// Mantém a superfície histórica do módulo, mas delega a execução para o pipeline explícito de SceneReset.
    /// </summary>
    public sealed class SceneResetFacade
    {
        private readonly ISimulationGateService _gateService;
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly string _sceneName;
        private readonly SceneResetHookRegistry _hookRegistry;

        public SceneResetFacade(
            ISimulationGateService gateService,
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IActorRegistry actorRegistry,
            IDependencyProvider provider = null,
            string sceneName = null,
            SceneResetHookRegistry hookRegistry = null)
        {
            _gateService = gateService;
            _spawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            _actorRegistry = actorRegistry;
            _provider = provider;
            _sceneName = sceneName;
            _hookRegistry = hookRegistry;
        }

        public Task ResetWorldAsync()
        {
            return ExecuteAsync(
                resetContext: null,
                gateToken: WorldResetTokens.WorldResetToken,
                startLog: "World Reset Started",
                completionLog: "World Reset Completed");
        }

        public Task ResetScopesAsync(IReadOnlyList<WorldResetScope> scopes, string reason)
        {
            if (scopes == null || scopes.Count == 0)
            {
                Core.Logging.DebugUtility.LogWarning(typeof(SceneResetFacade),
                    "Scoped reset ignored: scopes vazios ou nulos.");
                return Task.CompletedTask;
            }

            if (System.Linq.Enumerable.Any(scopes, scope => scope == WorldResetScope.World))
            {
                Core.Logging.DebugUtility.LogError(typeof(SceneResetFacade),
                    "WorldResetScope.World não é suportado em soft reset. Utilize ResetWorldAsync para hard reset.");
                return Task.CompletedTask;
            }

            var context = new WorldResetContext(reason, scopes, WorldResetFlags.SoftReset);
            return ExecuteAsync(
                resetContext: context,
                gateToken: SimulationGateTokens.SoftReset,
                startLog: $"Scoped Reset Started ({context})",
                completionLog: "Scoped Reset Completed");
        }

        private Task ExecuteAsync(
            WorldResetContext? resetContext,
            string gateToken,
            string startLog,
            string completionLog)
        {
            var context = new SceneResetContext(
                _gateService,
                _spawnServices,
                _actorRegistry,
                _provider,
                _sceneName,
                _hookRegistry,
                resetContext,
                gateToken,
                startLog,
                completionLog);

            SceneResetPipeline pipeline = SceneResetPipeline.CreateDefault();
            return pipeline.ExecuteAsync(context, CancellationToken.None);
        }
    }
}
