#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    public partial class TransitionStyleCatalogAsset
    {
        [ContextMenu("Validate Transition Profiles")]
        partial void ValidateTransitionProfileReferences()
        {
            if (styles == null)
            {
                return;
            }

            int invalidEntriesCount = 0;
            int noFadeEntriesCount = 0;

            for (int i = 0; i < styles.Count; i++)
            {
                StyleEntry entry = styles[i];
                if (entry == null)
                {
                    invalidEntriesCount++;
                    continue;
                }

                if (!entry.profileId.IsValid || entry.profileRef == null)
                {
                    invalidEntriesCount++;
                }

                if (!entry.useFade)
                {
                    noFadeEntriesCount++;
                }
            }

            if (invalidEntriesCount > 0 || noFadeEntriesCount > 0)
            {
                DebugUtility.Log(typeof(TransitionStyleCatalogAsset),
                    $"[OBS][Config] TransitionStyleCatalog OnValidate summary: invalidEntries={invalidEntriesCount}, noFadeEntries={noFadeEntriesCount}, asset='{name}'.",
                    DebugUtility.Colors.Info);
            }
        }
    }
}
#endif
