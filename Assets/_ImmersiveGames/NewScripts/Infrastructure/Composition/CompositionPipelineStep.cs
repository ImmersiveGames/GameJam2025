using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    internal sealed class CompositionPipelineStep
    {
        public CompositionPipelineStep(
            string id,
            Action<BootstrapConfigAsset> installer,
            IReadOnlyList<string> installerDependencies,
            Action<BootstrapConfigAsset> bootstrap,
            IReadOnlyList<string> bootstrapDependencies)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Installer = installer;
            InstallerDependencies = installerDependencies ?? Array.Empty<string>();
            Bootstrap = bootstrap;
            BootstrapDependencies = bootstrapDependencies ?? Array.Empty<string>();
        }

        public string Id { get; }
        public Action<BootstrapConfigAsset> Installer { get; }
        public IReadOnlyList<string> InstallerDependencies { get; }
        public Action<BootstrapConfigAsset> Bootstrap { get; }
        public IReadOnlyList<string> BootstrapDependencies { get; }

        public bool HasInstaller => Installer != null;
        public bool HasBootstrap => Bootstrap != null;

        public static CompositionPipelineStep FromDescriptor(ICompositionModuleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new CompositionPipelineStep(
                descriptor.ModuleId,
                descriptor.Installer,
                descriptor.InstallerDependencies,
                descriptor.Bootstrap,
                descriptor.BootstrapDependencies);
        }
    }

    internal static class CompositionPipelineExecutor
    {
        public static void ExecuteInstallers(IReadOnlyList<CompositionPipelineStep> steps, BootstrapConfigAsset bootstrapConfig)
        {
            ExecutePhase(
                steps,
                bootstrapConfig,
                phaseLabel: "installer",
                getDependencies: step => step.InstallerDependencies,
                getAction: step => step.Installer);
        }

        public static void ExecuteBootstraps(IReadOnlyList<CompositionPipelineStep> steps, BootstrapConfigAsset bootstrapConfig)
        {
            ExecutePhase(
                steps,
                bootstrapConfig,
                phaseLabel: "bootstrap",
                getDependencies: step => step.BootstrapDependencies,
                getAction: step => step.Bootstrap);
        }

        private static void ExecutePhase(
            IReadOnlyList<CompositionPipelineStep> steps,
            BootstrapConfigAsset bootstrapConfig,
            string phaseLabel,
            Func<CompositionPipelineStep, IReadOnlyList<string>> getDependencies,
            Func<CompositionPipelineStep, Action<BootstrapConfigAsset>> getAction)
        {
            if (steps == null)
            {
                throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline recebeu passos nulos.");
            }

            var orderedSteps = ResolveOrder(steps, phaseLabel, getDependencies, getAction);

            foreach (var step in orderedSteps)
            {
                var action = getAction(step);
                if (action == null)
                {
                    continue;
                }

                DebugUtility.LogVerbose(typeof(CompositionPipelineExecutor),
                    $"[BOOT][Composition] {phaseLabel} step='{step.Id}' starting.",
                    DebugUtility.Colors.Info);

                action(bootstrapConfig);

                DebugUtility.LogVerbose(typeof(CompositionPipelineExecutor),
                    $"[BOOT][Composition] {phaseLabel} step='{step.Id}' completed.",
                    DebugUtility.Colors.Info);
            }
        }

        private static IReadOnlyList<CompositionPipelineStep> ResolveOrder(
            IReadOnlyList<CompositionPipelineStep> steps,
            string phaseLabel,
            Func<CompositionPipelineStep, IReadOnlyList<string>> getDependencies,
            Func<CompositionPipelineStep, Action<BootstrapConfigAsset>> getAction)
        {
            var activeSteps = new List<CompositionPipelineStep>();
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline possui passo nulo na posicao {i}.");
                }

                if (getAction(step) != null)
                {
                    activeSteps.Add(step);
                }
            }

            var indexById = new Dictionary<string, int>(StringComparer.Ordinal);
            var indegree = new int[activeSteps.Count];
            var outgoing = new List<int>[activeSteps.Count];
            for (int i = 0; i < activeSteps.Count; i++)
            {
                outgoing[i] = new List<int>();
            }

            for (int i = 0; i < activeSteps.Count; i++)
            {
                var step = activeSteps[i];
                if (string.IsNullOrWhiteSpace(step.Id))
                {
                    throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou um passo com ID vazio.");
                }

                if (!indexById.TryAdd(step.Id, i))
                {
                    throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou ID duplicado='{step.Id}'.");
                }
            }

            for (int i = 0; i < activeSteps.Count; i++)
            {
                var step = activeSteps[i];
                var dependencies = getDependencies(step);
                if (dependencies == null)
                {
                    continue;
                }

                var seenDependencies = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < dependencies.Count; j++)
                {
                    string dependencyId = dependencies[j];
                    if (string.IsNullOrWhiteSpace(dependencyId))
                    {
                        throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou dependencia vazia em '{step.Id}'.");
                    }

                    if (string.Equals(dependencyId, step.Id, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou auto-dependencia em '{step.Id}'.");
                    }

                    if (!seenDependencies.Add(dependencyId))
                    {
                        throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou dependencia duplicada='{dependencyId}' em '{step.Id}'.");
                    }

                    if (!indexById.TryGetValue(dependencyId, out int dependencyIndex))
                    {
                        throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou dependencia ausente='{dependencyId}' requerida por '{step.Id}'.");
                    }

                    outgoing[dependencyIndex].Add(i);
                    indegree[i]++;
                }
            }

            var available = new List<int>();
            for (int i = 0; i < activeSteps.Count; i++)
            {
                if (indegree[i] == 0)
                {
                    available.Add(i);
                }
            }

            var ordered = new List<CompositionPipelineStep>(activeSteps.Count);
            while (available.Count > 0)
            {
                int nextIndex = available[0];
                for (int i = 1; i < available.Count; i++)
                {
                    if (available[i] < nextIndex)
                    {
                        nextIndex = available[i];
                    }
                }

                available.Remove(nextIndex);

                ordered.Add(activeSteps[nextIndex]);

                var nextOutgoing = outgoing[nextIndex];
                for (int i = 0; i < nextOutgoing.Count; i++)
                {
                    int targetIndex = nextOutgoing[i];
                    indegree[targetIndex]--;
                    if (indegree[targetIndex] == 0)
                    {
                        available.Add(targetIndex);
                    }
                }
            }

            if (ordered.Count != activeSteps.Count)
            {
                var blocked = new List<string>();
                for (int i = 0; i < activeSteps.Count; i++)
                {
                    if (indegree[i] > 0)
                    {
                        blocked.Add(activeSteps[i].Id);
                    }
                }

                throw new InvalidOperationException(
                    $"[FATAL][Composition] {phaseLabel} pipeline nao pode ser resolvido por ciclo ou dependencia retroalimentada. blocked=[{string.Join(", ", blocked)}].");
            }

            return ordered;
        }
    }
}
