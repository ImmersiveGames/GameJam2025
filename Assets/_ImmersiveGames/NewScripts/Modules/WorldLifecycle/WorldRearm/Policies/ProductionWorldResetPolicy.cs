using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies
{
    /// <summary>
    /// Policy padrão de produção.
    /// - Strict = UNITY_EDITOR/DEVELOPMENT_BUILD
    /// - SceneScan: opt-in apenas em Strict (QA/Dev)
    /// </summary>
    public sealed class ProductionWorldResetPolicy : IWorldResetPolicy
    {
        private readonly IRuntimeModeProvider _runtimeModeProvider;
        private readonly IDegradedModeReporter _degradedModeReporter;

        public ProductionWorldResetPolicy(
            IRuntimeModeProvider runtimeModeProvider,
            IDegradedModeReporter degradedModeReporter)
        {
            _runtimeModeProvider = runtimeModeProvider;
            _degradedModeReporter = degradedModeReporter;
        }

        public string Name => IsStrict ? "Strict" : "Release";

        public bool IsStrict => _runtimeModeProvider != null && _runtimeModeProvider.IsStrict;

        public bool AllowSceneScan => IsStrict;

        public bool AllowLegacyActorKindFallback => true;

        public void ReportDegraded(string feature, string reason, string detail = null, string signature = null, string profile = null)
        {
            if (_degradedModeReporter == null)
            {
                return;
            }

            _degradedModeReporter.Report(feature, reason, detail, signature, profile);
        }
    }
}
