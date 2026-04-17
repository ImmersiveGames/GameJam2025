using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Core.Logging;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
{
    internal sealed class CompositionPipelineStep
    {
        public CompositionPipelineStep(
            string id,
            Action<BootstrapConfigAsset> installer,
            IReadOnlyList<string> installerDependencies,
            Action<BootstrapConfigAsset> bootstrap,
            IReadOnlyList<string> bootstrapDependencies)
            : this(
                id,
                installerEntry: string.Empty,
                runtimeComposerEntry: string.Empty,
                installerDependencies: installerDependencies,
                bootstrapDependencies: bootstrapDependencies,
                installer: installer,
                bootstrap: bootstrap,
                optional: false,
                installerOnly: installer != null && bootstrap == null,
                description: null)
        {
        }

        public CompositionPipelineStep(
            string id,
            string installerEntry,
            string runtimeComposerEntry,
            IReadOnlyList<string> installerDependencies,
            IReadOnlyList<string> bootstrapDependencies,
            Action<BootstrapConfigAsset> installer,
            Action<BootstrapConfigAsset> bootstrap,
            bool optional,
            bool installerOnly,
            string description)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            InstallerEntry = installerEntry ?? string.Empty;
            RuntimeComposerEntry = runtimeComposerEntry ?? string.Empty;
            InstallerDependencies = installerDependencies ?? Array.Empty<string>();
            BootstrapDependencies = bootstrapDependencies ?? Array.Empty<string>();
            Installer = installer;
            Bootstrap = bootstrap;
            Optional = optional;
            InstallerOnly = installerOnly || (installer != null && bootstrap == null);
            Description = description;

            if (!Optional && Installer == null && Bootstrap == null)
            {
                throw new ArgumentException(
                    $"Pipeline step '{Id}' deve expor installer e/ou bootstrap, ou ser optional e explicitamente omitido.");
            }

            if (Installer == null && Bootstrap != null)
            {
                throw new ArgumentException($"Pipeline step '{Id}' nao pode expor bootstrap sem installer.");
            }

            if (InstallerOnly && Installer == null)
            {
                throw new ArgumentException($"Pipeline step '{Id}' marcou InstallerOnly=true sem installer.");
            }

            if (InstallerOnly && Bootstrap != null)
            {
                throw new ArgumentException($"Pipeline step '{Id}' marcou InstallerOnly=true com bootstrap nao nulo.");
            }
        }

        public string Id { get; }
        public string InstallerEntry { get; }
        public string RuntimeComposerEntry { get; }
        public Action<BootstrapConfigAsset> Installer { get; }
        public IReadOnlyList<string> InstallerDependencies { get; }
        public Action<BootstrapConfigAsset> Bootstrap { get; }
        public IReadOnlyList<string> BootstrapDependencies { get; }
        public bool Optional { get; }
        public bool InstallerOnly { get; }
        public string Description { get; }

        public bool HasInstaller => Installer != null;
        public bool HasBootstrap => Bootstrap != null;
        public bool IsOptionalSkip => Optional && Installer == null && Bootstrap == null;

        public static CompositionPipelineStep FromDescriptor(ICompositionModuleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new CompositionPipelineStep(
                descriptor.ModuleId,
                descriptor.InstallerEntry,
                descriptor.RuntimeComposerEntry,
                descriptor.InstallerDependencies,
                descriptor.BootstrapDependencies,
                descriptor.Installer,
                descriptor.Bootstrap,
                descriptor.Optional,
                descriptor.InstallerOnly,
                descriptor.Description);
        }
    }

    internal static class CompositionPipelineExecutor
    {
        private static bool _installerPhaseCompleted;
        private static bool _bootstrapPhaseOpen;

        public static void ExecuteInstallers(IReadOnlyList<CompositionPipelineStep> steps, BootstrapConfigAsset bootstrapConfig)
        {
            _installerPhaseCompleted = false;
            _bootstrapPhaseOpen = false;

            var plan = ResolvePhasePlan(
                steps,
                phaseLabel: "Fase 1",
                getDependencies: step => step.InstallerDependencies,
                shouldSkip: step => step.IsOptionalSkip);

            LogPhasePlan("Fase 1", plan);

            var summary = ExecutePhase(
                plan,
                bootstrapConfig,
                phaseLabel: "Fase 1",
                phaseKind: CompositionPhase.Installer,
                getAction: step => step.Installer);

            _installerPhaseCompleted = true;
            DebugUtility.Log(typeof(CompositionPipelineExecutor),
                $"[BOOT][Composition] Fase 1 concluida. executed={summary.ExecutedCount} skipped={summary.SkippedCount} order=[{JoinStepIds(plan.OrderedSteps)}].",
                DebugUtility.Colors.Info);
        }

        public static void ExecuteBootstraps(IReadOnlyList<CompositionPipelineStep> steps, BootstrapConfigAsset bootstrapConfig)
        {
            if (!_installerPhaseCompleted)
            {
                throw new InvalidOperationException("[FATAL][Composition] Fase 2 solicitada antes da conclusao da Fase 1.");
            }

            var plan = ResolvePhasePlan(
                steps,
                phaseLabel: "Fase 2",
                getDependencies: step => step.BootstrapDependencies,
                shouldSkip: step => step.IsOptionalSkip);

            LogPhasePlan("Fase 2", plan);

            CompositionPipelinePhaseExecutionSummary summary = null;
            _bootstrapPhaseOpen = true;
            try
            {
                summary = ExecutePhase(
                    plan,
                    bootstrapConfig,
                    phaseLabel: "Fase 2",
                    phaseKind: CompositionPhase.Bootstrap,
                    getAction: step => step.Bootstrap);
            }
            finally
            {
                _bootstrapPhaseOpen = false;
            }

            DebugUtility.Log(typeof(CompositionPipelineExecutor),
                $"[BOOT][Composition] Fase 2 concluida. executed={summary.ExecutedCount} skipped={summary.SkippedCount} order=[{JoinStepIds(plan.OrderedSteps)}].",
                DebugUtility.Colors.Info);
        }

        public static void RequireBootstrapPhaseOpen(string moduleName)
        {
            if (_bootstrapPhaseOpen)
            {
                return;
            }

            throw new InvalidOperationException($"[FATAL][Composition] Bootstrap canonico fora da Fase 2. module='{moduleName}'.");
        }

        private static CompositionPipelinePhaseExecutionSummary ExecutePhase(
            CompositionPipelinePhasePlan plan,
            BootstrapConfigAsset bootstrapConfig,
            string phaseLabel,
            CompositionPhase phaseKind,
            Func<CompositionPipelineStep, Action<BootstrapConfigAsset>> getAction)
        {
            if (plan == null)
            {
                throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline recebeu plano nulo.");
            }

            int executedCount = 0;
            int skippedCount = plan.SkippedSteps.Count;

            DebugUtility.Log(typeof(CompositionPipelineExecutor),
                $"[BOOT][Composition] {phaseLabel} start order=[{JoinStepIds(plan.OrderedSteps)}] skipped=[{JoinSkipped(plan.SkippedSteps)}].",
                DebugUtility.Colors.Info);

            foreach (var step in plan.OrderedSteps)
            {
                var action = getAction(step);
                if (action == null)
                {
                    string skipReason = GetSkipReason(step, phaseKind);
                    if (skipReason == null)
                    {
                        throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} encontrou passo sem entrypoint executavel. step='{step.Id}'.");
                    }

                    DebugUtility.Log(typeof(CompositionPipelineExecutor),
                        $"[BOOT][Composition] {phaseLabel} step='{step.Id}' skipped reason='{skipReason}'.",
                        DebugUtility.Colors.Info);
                    skippedCount++;
                    continue;
                }

                DebugUtility.LogVerbose(typeof(CompositionPipelineExecutor),
                    $"[BOOT][Composition] {phaseLabel} step='{step.Id}' starting.",
                    DebugUtility.Colors.Info);

                action(bootstrapConfig);

                DebugUtility.Log(typeof(CompositionPipelineExecutor),
                    $"[BOOT][Composition] {phaseLabel} step='{step.Id}' completed.",
                    DebugUtility.Colors.Info);
                executedCount++;
            }

            return new CompositionPipelinePhaseExecutionSummary(executedCount, skippedCount);
        }

        private static CompositionPipelinePhasePlan ResolvePhasePlan(
            IReadOnlyList<CompositionPipelineStep> steps,
            string phaseLabel,
            Func<CompositionPipelineStep, IReadOnlyList<string>> getDependencies,
            Func<CompositionPipelineStep, bool> shouldSkip)
        {
            if (steps == null)
            {
                throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline recebeu passos nulos.");
            }

            var activeSteps = new List<CompositionPipelineStep>();
            var skippedSteps = new List<CompositionPipelineStep>();

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline possui passo nulo na posicao {i}.");
                }

                if (string.IsNullOrWhiteSpace(step.Id))
                {
                    throw new InvalidOperationException($"[FATAL][Composition] {phaseLabel} pipeline encontrou um passo com ID vazio.");
                }

                if (shouldSkip(step))
                {
                    skippedSteps.Add(step);
                    continue;
                }

                activeSteps.Add(step);
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

            return new CompositionPipelinePhasePlan(ordered, skippedSteps);
        }

        private static void LogPhasePlan(string phaseLabel, CompositionPipelinePhasePlan plan)
        {
            DebugUtility.Log(typeof(CompositionPipelineExecutor),
                $"[BOOT][Composition] {phaseLabel} order=[{JoinStepIds(plan.OrderedSteps)}] skipped=[{JoinSkipped(plan.SkippedSteps)}].",
                DebugUtility.Colors.Info);
        }

        private static string GetSkipReason(CompositionPipelineStep step, CompositionPhase phaseKind)
        {
            if (step == null)
            {
                return null;
            }

            if (step.IsOptionalSkip)
            {
                return "optional-disabled";
            }

            if (phaseKind == CompositionPhase.Bootstrap && step.InstallerOnly && step.Bootstrap == null)
            {
                return "installer-only";
            }

            return null;
        }

        private static string JoinStepIds(IReadOnlyList<CompositionPipelineStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return string.Empty;
            }

            var values = new List<string>(steps.Count);
            for (int i = 0; i < steps.Count; i++)
            {
                values.Add(steps[i].Id);
            }

            return string.Join(", ", values);
        }

        private static string JoinSkipped(IReadOnlyList<CompositionPipelineStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return string.Empty;
            }

            var values = new List<string>(steps.Count);
            for (int i = 0; i < steps.Count; i++)
            {
                values.Add($"{steps[i].Id}:optional-disabled");
            }

            return string.Join(", ", values);
        }

        private enum CompositionPhase
        {
            Installer,
            Bootstrap,
        }

        private sealed class CompositionPipelinePhasePlan
        {
            public CompositionPipelinePhasePlan(
                IReadOnlyList<CompositionPipelineStep> orderedSteps,
                IReadOnlyList<CompositionPipelineStep> skippedSteps)
            {
                OrderedSteps = orderedSteps ?? Array.Empty<CompositionPipelineStep>();
                SkippedSteps = skippedSteps ?? Array.Empty<CompositionPipelineStep>();
            }

            public IReadOnlyList<CompositionPipelineStep> OrderedSteps { get; }
            public IReadOnlyList<CompositionPipelineStep> SkippedSteps { get; }
            public int ExecutableCount => OrderedSteps.Count;
            public int SkippedCount => SkippedSteps.Count;
        }

        private sealed class CompositionPipelinePhaseExecutionSummary
        {
            public CompositionPipelinePhaseExecutionSummary(int executedCount, int skippedCount)
            {
                ExecutedCount = executedCount;
                SkippedCount = skippedCount;
            }

            public int ExecutedCount { get; }
            public int SkippedCount { get; }
        }
    }
}

