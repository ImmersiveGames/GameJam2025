using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Presentation;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Bootstrap
{
    /// <summary>
    /// Installer do RunEndRail.
    ///
    /// Responsabilidade:
    /// - registrar contratos e servicos do rail interno de run-end no boot;
    /// - nao compor runtime nem criar bridges.
    /// </summary>
    public static class RunEndRailInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterRunResultStagePresenterHost();
            RegisterRunDecisionStagePresenterHost();
            RegisterRunEndRailResultService();
            RegisterRunEndIntentOwnershipService();
            RegisterRunContinuationOwnershipService();
            RegisterRunDecisionOwnershipService();
            RegisterRunResultStageOwnershipService();

            _installed = true;

            DebugUtility.Log(typeof(RunEndRailInstaller),
                "[RunEndRail] Rail interno concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRunEndIntentOwnershipService()
        {
            RegisterIfMissing<IRunEndIntentOwnershipService>(
                () => new RunEndIntentOwnershipService(),
                "[RunEndRail] IRunEndIntentOwnershipService ja registrado no DI global.",
                "[RunEndRail] RunEndIntentOwnershipService registrado no DI global.");
        }

        private static void RegisterRunContinuationOwnershipService()
        {
            RegisterIfMissing<IRunContinuationOwnershipService>(
                () => new RunContinuationOwnershipService(),
                "[RunEndRail] IRunContinuationOwnershipService ja registrado no DI global.",
                "[RunEndRail] RunContinuationOwnershipService registrado no DI global.");
        }

        private static void RegisterRunDecisionOwnershipService()
        {
            RegisterIfMissing<IRunDecisionOwnershipService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IRunDecisionStagePresenterHost>(out var presenterHost) || presenterHost == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunEndRail] IRunDecisionStagePresenterHost ausente ao registrar IRunDecisionOwnershipService.");
                    }

                    return new RunDecisionOwnershipService(presenterHost);
                },
                "[RunEndRail] IRunDecisionOwnershipService ja registrado no DI global.",
                "[RunEndRail] RunDecisionOwnershipService registrado no DI global.");
        }

        private static void RegisterRunResultStageOwnershipService()
        {
            RegisterIfMissing<IRunResultStageOwnershipService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IRunDecisionOwnershipService>(out var decisionOwner) || decisionOwner == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunEndRail] IRunDecisionOwnershipService ausente ao registrar IRunResultStageOwnershipService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IRunResultStagePresenterHost>(out var presenterHost) || presenterHost == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][RunEndRail] IRunResultStagePresenterHost ausente ao registrar IRunResultStageOwnershipService.");
                    }

                    return new RunResultStageOwnershipService(decisionOwner, presenterHost);
                },
                "[RunEndRail] IRunResultStageOwnershipService ja registrado no DI global.",
                "[RunEndRail] RunResultStageOwnershipService registrado no DI global.");
        }

        private static void RegisterRunResultStagePresenterHost()
        {
            RegisterIfMissing<IRunResultStagePresenterHost>(
                () => new RunResultStagePresenterHost(),
                "[RunEndRail] IRunResultStagePresenterHost ja registrado no DI global.",
                "[RunEndRail] RunResultStagePresenterHost registrado no DI global.");
        }

        private static void RegisterRunDecisionStagePresenterHost()
        {
            RegisterIfMissing<IRunDecisionStagePresenterHost>(
                () => new RunDecisionStagePresenterHost(),
                "[RunEndRail] IRunDecisionStagePresenterHost ja registrado no DI global.",
                "[RunEndRail] RunDecisionStagePresenterHost registrado no DI global.");
        }

        private static void RegisterRunEndRailResultService()
        {
            RegisterIfMissing<IPostRunResultService>(
                () => new PostRunResultService(),
                "[RunEndRail] IPostRunResultService ja registrado no DI global.",
                "[RunEndRail] RunEndRailResultService registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(RunEndRailInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(RunEndRailInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
