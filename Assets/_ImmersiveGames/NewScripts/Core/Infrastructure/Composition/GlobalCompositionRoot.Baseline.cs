namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Baseline (optional)
        // --------------------------------------------------------------------

#if NEWSCRIPTS_BASELINE_ASSERTS
        private static void RegisterBaselineAsserter()
        {
            if (BaselineInvariantAsserter.TryInstall())
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Baseline] BaselineInvariantAsserter ativo (NEWSCRIPTS_BASELINE_ASSERTS).",
                    DebugUtility.Colors.Info);
            }
        }
#endif

    }
}
