using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Bridges.SceneFlow
{
    /// <summary>
    /// Driver canônico (produção) para integrar SceneFlow → WorldLifecycle.
    ///
    /// Responsabilidades:
    /// - Ao receber SceneTransitionScenesReadyEvent (profile gameplay), dispara ResetWorld na cena alvo.
    /// - Publica WorldLifecycleResetCompletedEvent(signature) para liberar o completion gate do SceneFlow.
    ///
    /// Observações:
    /// - Não depende de "coordinator" obsoleto.
    /// - É best-effort: nunca deve travar o fluxo (sempre publica ResetCompleted).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleSceneFlowResetDriver : IDisposable
    {
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private bool _disposed;

        // Reasons canônicos (Contrato de Observability).
        private const string ReasonScenesReady = "SceneFlow/ScenesReady";
        private const string ReasonSkippedStartupOrFrontendPrefix = "Skipped_StartupOrFrontend";
        private const string ReasonFailedNoControllerPrefix = "Failed_NoController";

        public WorldLifecycleSceneFlowResetDriver()
        {
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                $"[WorldLifecycle] Driver registrado: SceneFlow ScenesReady -> ResetWorld -> ResetCompleted. reason='{ReasonScenesReady}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding); }
            catch { /* best-effort */ }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            // Event handler não pode ser async; delega para Task com tratamento interno.
            _ = HandleScenesReadyAsync(evt);
        }

        private async Task HandleScenesReadyAsync(SceneTransitionScenesReadyEvent evt)
        {
            var context = evt.Context;
            string signature = SceneTransitionSignatureUtil.Compute(context);

            if (string.IsNullOrWhiteSpace(signature))
            {
                // Defensivo: assinatura vazia não deve travar o SceneFlow; apenas libera.
                DebugUtility.LogWarning<WorldLifecycleSceneFlowResetDriver>(
                    "[WorldLifecycle] ScenesReady recebido com ContextSignature vazia. Liberando gate sem reset.");
                LogObsResetRequested(
                    signature: string.Empty,
                    sourceSignature: string.Empty,
                    profile: context.TransitionProfileName,
                    target: ResolveTargetSceneName(context),
                    reason: ReasonScenesReady);
                PublishResetCompleted(signature, ReasonScenesReady, context.TransitionProfileName, ResolveTargetSceneName(context));
                return;
            }

            // Regra canônica: reset determinístico de WorldLifecycle só é obrigatório em profile gameplay.
            string targetScene;
            if (!context.TransitionProfileId.IsGameplay)
            {
                targetScene = ResolveTargetSceneName(context);
                string skippedReason = $"{ReasonSkippedStartupOrFrontendPrefix}:profile={context.TransitionProfileName};scene={targetScene}";

                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    profile: context.TransitionProfileName,
                    target: targetScene,
                    reason: skippedReason);

                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] ScenesReady ignorado (profile != gameplay). signature='{signature}', profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                PublishResetCompleted(signature, skippedReason, context.TransitionProfileName, targetScene);
                return;
            }

            targetScene = ResolveTargetSceneName(context);
            var controllers = WorldLifecycleControllerLocator.FindControllersForScene(targetScene);

            if (controllers.Count == 0)
            {
                // Cena pode não ter WorldLifecycle (ex.: fluxo especial). Não travar o SceneFlow.
                string failedReason = $"{ReasonFailedNoControllerPrefix}:{targetScene}";

                LogObsResetRequested(
                    signature: signature,
                    sourceSignature: signature,
                    profile: context.TransitionProfileName,
                    target: targetScene,
                    reason: failedReason);

                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] Nenhum WorldLifecycleController encontrado para reset. signature='{signature}', targetScene='{targetScene}'. Liberando gate.",
                    DebugUtility.Colors.Info);

                PublishResetCompleted(signature, failedReason, context.TransitionProfileName, targetScene);
                return;
            }

            // Reset real (profile=gameplay).
            LogObsResetRequested(
                signature: signature,
                sourceSignature: signature,
                profile: context.TransitionProfileName,
                target: targetScene,
                reason: ReasonScenesReady);

            try
            {
				// Determinismo e robustez:
				// - remove nulls
				// - ordena por InstanceID (ordem consistente entre frames)
				// Observação: evitar LINQ aqui reduz alocações em um caminho quente (ScenesReady).
				var filteredControllers = new List<WorldLifecycleController>(controllers.Count);
				for (int i = 0; i < controllers.Count; i++)
				{
					var controller = controllers[i];
					if (controller != null)
					{
						filteredControllers.Add(controller);
					}
				}

				filteredControllers.Sort(static (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
					$"[WorldLifecycle] Disparando ResetWorld para {filteredControllers.Count} controller(s). signature='{signature}', targetScene='{targetScene}'.",
                    DebugUtility.Colors.Info);

				var tasks = new List<Task>(filteredControllers.Count);
				for (int i = 0; i < filteredControllers.Count; i++)
				{
					tasks.Add(filteredControllers[i].ResetWorldAsync(ReasonScenesReady));
				}

                await Task.WhenAll(tasks);

                DebugUtility.LogVerbose<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] ResetWorld concluído (ScenesReady). signature='{signature}', targetScene='{targetScene}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                // Best-effort: loga, mas NÃO impede liberação do gate.
                DebugUtility.LogError<WorldLifecycleSceneFlowResetDriver>(
                    $"[WorldLifecycle] Erro durante ResetWorld (ScenesReady). signature='{signature}', targetScene='{targetScene}', ex='{ex}'.");
            }
            finally
            {
                PublishResetCompleted(signature, ReasonScenesReady, context.TransitionProfileName, targetScene);
            }
        }

        private static string ResolveTargetSceneName(SceneTransitionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.TargetActiveScene))
            {
                return context.TargetActiveScene.Trim();
            }

            // Fallback: active scene atual.
            return SceneManager.GetActiveScene().name ?? string.Empty;
        }

        private static void PublishResetCompleted(string signature, string reason, string profile, string target)
        {
            // Sempre publicar: o completion gate depende disso para não degradar em timeout.
            LogObsResetCompleted(
                signature: signature,
                profile: profile,
                target: target,
                reason: reason);

            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
                new WorldLifecycleResetCompletedEvent(signature ?? string.Empty, reason));
        }

        private static void LogObsResetRequested(
            string signature,
            string sourceSignature,
            string profile,
            string target,
            string reason)
        {
            // Observabilidade canônica (Contrato): ResetRequested com sourceSignature/reason/profile/target.
            DebugUtility.LogVerbose(typeof(WorldLifecycleSceneFlowResetDriver),
                $"[OBS][WorldLifecycle] ResetRequested signature='{signature ?? string.Empty}' sourceSignature='{sourceSignature ?? string.Empty}' profile='{profile ?? string.Empty}' target='{target ?? string.Empty}' reason='{reason ?? string.Empty}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogObsResetCompleted(
            string signature,
            string profile,
            string target,
            string reason)
        {
            // Observabilidade canônica (Contrato): ResetCompleted correlacionável ao gate (signature) e reason final.
            DebugUtility.LogVerbose(typeof(WorldLifecycleSceneFlowResetDriver),
                $"[OBS][WorldLifecycle] ResetCompleted signature='{signature ?? string.Empty}' profile='{profile ?? string.Empty}' target='{target ?? string.Empty}' reason='{reason ?? string.Empty}'.",
                DebugUtility.Colors.Success);
        }

    }
}
